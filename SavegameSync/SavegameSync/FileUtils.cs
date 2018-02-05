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

        //TODO: Figure out if this method is unnecessary
        public static string GetPathSuffix(string path, string pathPrefix)
        {
            if (path.StartsWith(pathPrefix))
            {
                return path.Substring(pathPrefix.Length);
            }
            return null;
        }

        //TODO: Figure out if this method is unnecessary
        public static string GetFullPath(string pathPrefix, string pathSuffix)
        {
            string sep = "";
            if (!pathPrefix.EndsWith("\\") && !pathSuffix.StartsWith("\\"))
            {
                sep = "\\";
            }
            return pathPrefix + sep + pathSuffix;
        }

        //TODO: Figure out if this method is unnecessary
        /// <summary>
        /// Create a new directory at the given path, creating any necessary parent directories.
        /// </summary>
        public static void CreateDirectories(string dirPath)
        {
            //TODO: implement this
        }

        //TODO: Figure out if this method is unnecessary
        public static string GetParentDirectoryPath(string path)
        {
            string[] split = path.Split('\\');
            int fileNamePosition = split.Length - 1;
            string fileName = split[fileNamePosition];
            while (fileName.Count() == 0)
            {
                fileNamePosition--;
                fileName = split[fileNamePosition];
            }

            int fileNameIndex = path.LastIndexOf(fileName);
            string untrimmedPath = path.Substring(0, fileNameIndex);
            string trimmedPath = untrimmedPath;
            while (trimmedPath.EndsWith("\\"))
            {
                trimmedPath = trimmedPath.Substring(0, trimmedPath.Length - 1);
            }
            return trimmedPath;
        }
    }
}
