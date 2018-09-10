using System;
using System.IO;
using System.Linq;

namespace SavegameSync
{
    public static class FileUtils
    {
        /*
         * Based on this MSDN example: https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories.
         */
        public static void CopyDirectory(string originalDir, string destDir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(originalDir);
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            FileInfo[] files = dirInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                string destFilePath = Path.Combine(destDir, file.Name);
                file.CopyTo(destFilePath);
            }

            DirectoryInfo[] subdirs = dirInfo.GetDirectories();
            foreach (DirectoryInfo subdir in subdirs)
            {
                string destSubdirPath = Path.Combine(destDir, subdir.Name);
                CopyDirectory(subdir.FullName, destSubdirPath);
            }
        }

        /// <summary>
        /// Get the latest LastWriteTime of any file in the given directory or its subdirectories.
        /// </summary>
        public static DateTime GetLatestFileWriteTime(string dir)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            DateTime latestFileWriteTime = new DateTime(1900, 1, 1);

            FileInfo[] files = dirInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                DateTime curDateTime = file.LastWriteTimeUtc;
                if (curDateTime > latestFileWriteTime)
                {
                    latestFileWriteTime = curDateTime;
                }
            }

            DirectoryInfo[] subDirs = dirInfo.GetDirectories();
            foreach (DirectoryInfo subDir in subDirs)
            {
                DateTime curDateTime = GetLatestFileWriteTime(subDir.FullName);
                if (curDateTime > latestFileWriteTime)
                {
                    latestFileWriteTime = curDateTime;
                }
            }

            return latestFileWriteTime;
        }

        public static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
    }
}
