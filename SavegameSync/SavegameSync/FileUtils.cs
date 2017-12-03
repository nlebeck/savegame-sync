using System.Linq;

namespace SavegameSync
{
    public static class FileUtils
    {
        public static void CopyDirectoryContents(string sourceDir, string destDir)
        {
            //TODO: implement this
        }

        public static string GetPathSuffix(string path, string pathPrefix)
        {
            if (path.StartsWith(pathPrefix))
            {
                return path.Substring(pathPrefix.Length);
            }
            return null;
        }

        public static string GetFullPath(string pathPrefix, string pathSuffix)
        {
            string sep = "";
            if (!pathPrefix.EndsWith("\\") && !pathSuffix.StartsWith("\\"))
            {
                sep = "\\";
            }
            return pathPrefix + sep + pathSuffix;
        }

        /// <summary>
        /// Create a new directory at the given path, creating any necessary parent directories.
        /// </summary>
        public static void CreateDirectories(string dirPath)
        {
            //TODO: implement this
        }

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
