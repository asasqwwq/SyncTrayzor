using NLog;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SyncTrayzor.Syncthing.ApiClient
{
    public interface ISyncthingApiClientFactory
    {
        Task<ISyncthingApiClient> CreateCorrectApiClientAsync(Uri baseAddress, string apiKey, TimeSpan timeout, CancellationToken cancellationToken);
    }

    public class SyncthingApiClientFactory : ISyncthingApiClientFactory
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public async Task<ISyncthingApiClient> CreateCorrectApiClientAsync(Uri baseAddress, string apiKey, TimeSpan timeout, CancellationToken cancellationToken)
        {
            // This is a bit fugly - there's no way to determine which one we're talking to without trying a request and have it fail...
            ISyncthingApiClient client = new SyncthingApiClient(baseAddress, apiKey);

            // We abort because of the CancellationToken or because we take too long, or succeed
            // We used to measure absolute time here. However, we can be put to sleep halfway through this operation,
            // and so fail the timeout condition without actually trying for the appropriate amount of time.
            // Therefore, do it for a num iterations...
            var numAttempts = timeout.TotalSeconds; // Delay for 1 second per iteration
            bool success = false;
            Exception lastException = null;
            for (int retryCount = 0; retryCount < numAttempts; retryCount++)
            {
                try
                {
                    logger.Debug("Attempting to request API");
                    await client.FetchVersionAsync(cancellationToken);
                    success = true;
                    logger.Debug("Success!");
                    break;
                }
                catch (OperationCanceledException e) when (e.CancellationToken != cancellationToken)
                {
                    logger.Debug("Failed to connect (cancelled) on attempt {0}", retryCount);
                }
                catch (HttpRequestException e)
                {
                    logger.Debug("Failed to connect on attempt {0}", retryCount);
                    // Expected when Syncthing's still starting
                    lastException = e;
                }

                await Task.Delay(1000, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (!success)
                throw new SyncthingDidNotStartCorrectlyException($"Syncthing didn't connect after {timeout}", lastException);

            return client;
        }
    }
}
