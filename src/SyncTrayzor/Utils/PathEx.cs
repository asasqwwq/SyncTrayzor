using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace SyncTrayzor.Utils
{
    public static class PathEx
    {
        /// <summary>
        /// Converts a short path to a long path.
        /// </summary>
        /// <param name="shortPath">A path that may contain short path elements (~1).</param>
        /// <returns>The long path.  Null or empty if the input is null or empty.</returns>
        // Adapted from http://pinvoke.net/default.aspx/kernel32/GetLongPathName.html
        public static string GetLongPathName(string shortPath)
        {
            if (String.IsNullOrWhiteSpace(shortPath))
                return shortPath;

            var builder = new StringBuilder(255);
            int result = NativeMethods.GetLongPathName(shortPath, builder, builder.Capacity);
            if (result > 0 && result < builder.Capacity)
            {
                return builder.ToString(0, result);
            }
            else
            {
                if (result > 0)
                {
                    builder = new StringBuilder(result);
                    result = NativeMethods.GetLongPathName(shortPath, builder, builder.Capacity);
                    return builder.ToString(0, result);
                }
                else
                {
                    throw new FileNotFoundException($"File {shortPath} not found");
                }
            }
        }

        private class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            [return: MarshalAs(UnmanagedType.U4)]
            public static extern int GetLongPathName(
                [MarshalAs(UnmanagedType.LPTStr)] string lpszShortPath,
                [MarshalAs(UnmanagedType.LPTStr)] StringBuilder lpszLongPath,
                [MarshalAs(UnmanagedType.U4)] int cchBuffer);
        }
    }
}
