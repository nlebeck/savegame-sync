using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace SavegameSync
{
    /// <summary>
    /// Singleton class containing the main app logic.
    /// </summary>
    public class SavegameSyncEngine
    {
        private const string SavegameListFileName = "savegame-list.txt";
        private const string LocalGameListFileName = "local-game-list.txt";

        private static SavegameSyncEngine singleton;

        private GoogleDriveWrapper googleDriveWrapper;
        private LocalGameList localGameList;
        private SavegameList savegameList;
        private string savegameListFileId;

        public static SavegameSyncEngine GetInstance()
        {
            if (singleton == null)
            {
                singleton = new SavegameSyncEngine();
            }
            return singleton;
        }

        public async Task Init()
        {
            await ReadLocalGameList();
        }

        public async Task Login()
        {
            googleDriveWrapper = await GoogleDriveWrapper.Create();
        }

        public bool IsLoggedIn()
        {
            return googleDriveWrapper != null;
        }

        public async Task DebugPrintSavegameListFile()
        {
            await ReadSavegameList();
            savegameList.DebugPrintGameNames();
            foreach (string game in savegameList.GetGames())
            {
                savegameList.DebugPrintSaves(game);
            }
            await WriteSavegameList();
        }

        public async Task ReadLocalGameList()
        {
            localGameList = new LocalGameList();
            FileStream localGameListStream = File.Open(LocalGameListFileName, FileMode.OpenOrCreate);
            await localGameList.ReadFromStream(localGameListStream);
            localGameListStream.Close();
        }

        public async Task WriteLocalGameList()
        {
            FileStream localGameListWriteStream = File.Open(LocalGameListFileName, FileMode.Create);
            await localGameList.WriteToStream(localGameListWriteStream);
            localGameListWriteStream.Close();
        }

        public List<string> GetLocalGameNames()
        {
            return localGameList.GetGameNames();
        }

        public string GetLocalInstallDir(string gameName)
        {
            return localGameList.GetInstallDir(gameName);
        }

        public async Task AddLocalGame(string gameName, string installDir)
        {
            localGameList.AddGame(gameName, installDir);
            await WriteLocalGameList();
        }

        public async Task DeleteLocalGame(string gameName)
        {
            localGameList.DeleteGame(gameName);
            await WriteLocalGameList();
        }

        public async Task<List<SavegameEntry>> ReadSaves(string gameName)
        {
            await ReadSavegameList();
            return savegameList.ReadSaves(gameName);
        }

        public async Task DebugPrintLocalGameListFile()
        {
            await ReadLocalGameList();
            localGameList.DebugPrintGames();
        }

        public async Task DebugAddNonexistentLocalGame()
        {
            await ReadLocalGameList();
            if (!localGameList.ContainsGame("MadeUpGame"))
            {
                await AddLocalGame("MadeUpGame", "C:\\Games\\MadeUpGame");
            }
            await WriteLocalGameList();
        }

        public async Task ZipAndUploadSave(string gameName)
        {
            // Copy save files from the game's install directory into a temp directory according
            // to the spec
            string installDir = localGameList.GetInstallDir(gameName);
            SaveSpec saveSpec = SaveSpecRepository.GetRepository().GetSaveSpec(gameName);
            string destDir = @"C:\Users\niell\Git\savegame-sync\tempUpload\temp";
            FileUtils.DeleteIfExists(destDir);
            Directory.CreateDirectory(destDir);
            CopySaveFilesFromInstallDir(saveSpec, installDir, destDir);
            Debug.WriteLine("Dirs: " + installDir + " " + destDir);

            // Find the last write time of the save
            DateTime latestFileWriteTime = GetLocalSaveTimestamp(saveSpec, installDir);
            Debug.WriteLine("Latest write time: " + latestFileWriteTime);

            // Assign the save a guid and make it into a zip file
            Guid saveGuid = Guid.NewGuid();
            Debug.WriteLine("Guid: " + saveGuid);
            string zipFile = @"C:\Users\niell\Git\savegame-sync\tempUpload\" + saveGuid + ".zip";
            FileUtils.DeleteIfExists(zipFile);
            ZipFile.CreateFromDirectory(destDir, zipFile);

            // Upload save
            string remoteFileName = SavegameSyncUtils.GetSavegameFileNameFromGuid(saveGuid);
            string fileId = await googleDriveWrapper.CreateFileAsync(remoteFileName);
            using (FileStream fileStream = File.OpenRead(zipFile))
            {
                await googleDriveWrapper.UploadFileAsync(fileId, fileStream);
            }

            // Download latest version of SavegameList
            await ReadSavegameList();

            // Add save to SavegameList
            savegameList.AddSave(gameName, saveGuid, latestFileWriteTime);

            // Upload SavegameList
            await WriteSavegameList();
        }

        public async Task DownloadAndUnzipSave(string gameName, int saveIndex)
        {
            // Download latest version of SavegameList
            await ReadSavegameList();

            // Read file name from SavegameList
            List<SavegameEntry> saves = savegameList.ReadSaves(gameName);
            SavegameEntry save = saves[saveIndex];
            Guid saveGuid = save.Guid;
            string saveFileName = SavegameSyncUtils.GetSavegameFileNameFromGuid(saveGuid);
            Debug.WriteLine("Downloading save file " + saveFileName + " with index " + saveIndex + " and timestamp " + save.Timestamp);

            // Download zipped save from Google Drive
            var files = await googleDriveWrapper.SearchFileByNameAsync(saveFileName);
            Debug.Assert(files.Count == 1);
            string saveFileId = files[0].Id;
            string tempDir = @"C:\Users\niell\Git\savegame-sync\tempDownload\";
            string zipFilePath = Path.Combine(tempDir, saveFileName);
            Directory.CreateDirectory(tempDir);
            using (FileStream fileStream = File.OpenWrite(zipFilePath))
            {
                await googleDriveWrapper.DownloadFileAsync(saveFileId, fileStream);
            }

            // Unzip zipped save
            string tempSaveDir = Path.Combine(tempDir, "temp");
            FileUtils.DeleteIfExists(tempSaveDir);
            ZipFile.ExtractToDirectory(zipFilePath, tempSaveDir);

            // Copy unzipped files/directories into game install directory
            string installDir = localGameList.GetInstallDir(gameName);
            SaveSpec saveSpec = SaveSpecRepository.GetRepository().GetSaveSpec(gameName);
            CopySaveFilesIntoInstallDir(saveSpec, tempSaveDir, installDir);
        }

        public async Task DeleteSave(string gameName, int saveIndex)
        {
            // Download latest version of SavegameList
            await ReadSavegameList();

            // Get guid of zipped save file to use later
            List<SavegameEntry> saves = savegameList.ReadSaves(gameName);
            SavegameEntry save = saves[saveIndex];
            Guid saveGuid = save.Guid;
            Debug.WriteLine("Deleting save file with guid " + saveGuid + ", index " + saveIndex + ", and timestamp " + save.Timestamp);

            // Delete save from SavegameList
            savegameList.DeleteSave(gameName, saveIndex);

            // Upload SavegameList
            await WriteSavegameList();

            // Delete zipped save file
            string saveFileName = SavegameSyncUtils.GetSavegameFileNameFromGuid(saveGuid);
            int filesDeleted = await googleDriveWrapper.DeleteAllFilesWithNameAsync(saveFileName);
            Debug.Assert(filesDeleted == 1);
        }

        public async Task DeleteGameFromCloud(string gameName)
        {
            await ReadSavegameList();
            List<SavegameEntry> saves = savegameList.ReadSaves(gameName);
            savegameList.DeleteGame(gameName);
            await WriteSavegameList();

            foreach (SavegameEntry save in saves)
            {
                string saveFileName = SavegameSyncUtils.GetSavegameFileNameFromGuid(save.Guid);
                int filesDeleted = await googleDriveWrapper.DeleteAllFilesWithNameAsync(saveFileName);
                Debug.Assert(filesDeleted == 1);
            }
        }

        public async Task DebugPrintAllFiles()
        {
            // Print all files in the Google Drive app folder
            Console.WriteLine("Listing all files: ");
            var files = await googleDriveWrapper.GetAllFilesAsync();
            for (int i = 0; i < files.Count; i++)
            {
                Console.WriteLine(string.Format("{0}, {1}, {2}", i, files[i].Name, files[i].Size));
            }
            Console.WriteLine("Done listing all files");
        }

        public async Task DebugZipAndUploadSave()
        {
            // Wipe Google Drive app folder
            await googleDriveWrapper.DeleteAllFilesAsync();

            await ZipAndUploadSave("Medal of Honor Allied Assault War Chest");

            // Print data from the local copy of the savegameList
            savegameList.DebugPrintGameNames();
            savegameList.DebugPrintSaves("Medal of Honor Allied Assault War Chest");

            await DebugPrintAllFiles();
        }

        public async Task DebugDownloadAndUnzipSave()
        {
            await DownloadAndUnzipSave("Medal of Honor Allied Assault War Chest", 0);
        }

        public async Task DebugGoogleDriveFunctions()
        {
            List<string> dummyFileIds = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine("Creating dummy file " + i);
                dummyFileIds.Add(await googleDriveWrapper.CreateFileAsync("DummyFile" + i));
            }

            var fileList = await googleDriveWrapper.GetAllFilesAsync();
            Console.WriteLine("Printing all files for the first time");
            foreach (Google.Apis.Drive.v3.Data.File file in fileList)
            {
                Console.WriteLine($"{file.Name}, {file.Id}");
            }
            Console.WriteLine("Done printing all files for the first time");

            for (int i = 0; i < dummyFileIds.Count; i++)
            {
                Console.WriteLine("Deleting dummy file " + i);
                await googleDriveWrapper.DeleteFileAsync(dummyFileIds[i]);
            }

            fileList = await googleDriveWrapper.GetAllFilesAsync();
            Console.WriteLine("Printing all files for the second time");
            foreach (Google.Apis.Drive.v3.Data.File file in fileList)
            {
                Console.WriteLine($"{file.Name}, {file.Id}");
            }
            Console.WriteLine("Done printing all files for the second time");
        }

        private void CopySaveFilesFromInstallDir(SaveSpec saveSpec, string installDir, string destDir)
        {
            FileUtils.DeleteIfExists(destDir);
            Directory.CreateDirectory(destDir);
            foreach (string subPath in saveSpec.SavePaths)
            {
                string originalPath = System.IO.Path.Combine(installDir, subPath);
                string destPath = System.IO.Path.Combine(destDir, subPath);
                if (Directory.Exists(originalPath))
                {
                    FileUtils.CopyDirectory(originalPath, destPath);
                }
                else if (File.Exists(originalPath))
                {
                    File.Copy(originalPath, destPath);
                }
                else
                {
                    Console.WriteLine("Skipping missing subpath " + subPath
                        + " while copying save files for " + saveSpec.GameName
                        + " out of install dir");
                }
            }
        }

        public DateTime GetLocalSaveTimestamp(SaveSpec saveSpec, string installDir)
        {
            DateTime timestamp = new DateTime(0);
            foreach (string subPath in saveSpec.SavePaths)
            {
                string fullSubPath = Path.Combine(installDir, subPath);
                DateTime subPathTimestamp = FileUtils.GetLatestFileWriteTime(fullSubPath);
                if (subPathTimestamp > timestamp)
                {
                    timestamp = subPathTimestamp;
                }
            }
            return timestamp;
        }

        /// <summary>
        /// Copy the files and directories named in a SaveSpec from a source directory into a
        /// game's install directory, first deleting the existing copies of those items from the
        /// install directory.
        /// </summary>
        /// <remarks>
        /// Note that if an item named in the SaveSpec is present in the install directory but not
        /// in the source directory, this method will delete that item from the install directory.
        /// </remarks>
        private void CopySaveFilesIntoInstallDir(SaveSpec saveSpec, string sourceDir, string installDir)
        {
            foreach (string subPath in saveSpec.SavePaths)
            {
                string sourcePath = Path.Combine(sourceDir, subPath);
                string destPath = Path.Combine(installDir, subPath);
                FileUtils.DeleteIfExists(destPath);
                if (Directory.Exists(sourcePath))
                {
                    FileUtils.CopyDirectory(sourcePath, destPath);
                }
                else if (File.Exists(sourcePath))
                {
                    FileUtils.CopyDirectory(sourcePath, destPath);
                }
                else
                {
                    Console.WriteLine("Skipping missing subpath " + subPath
                        + " while copying save files for " + saveSpec.GameName
                        + " into install dir");
                }
            }
        }

        private async Task<string> GetSavegameListFileIdOrCreate()
        {
            if (savegameListFileId != null)
            {
                return savegameListFileId;
            }

            Debug.WriteLine("Looking up savegame list fileId (no fileId cached)");
            List<Google.Apis.Drive.v3.Data.File> files = await googleDriveWrapper.SearchFileByNameAsync(SavegameListFileName);
            string id = null;
            if (files.Count == 0)
            {
                id = await googleDriveWrapper.CreateFileAsync(SavegameListFileName);
                Debug.WriteLine("Created new savegame list with Id " + id);
            }
            else if (files.Count == 1)
            {
                id = files[0].Id;
                Debug.WriteLine("Savegame list exists already");
            }
            else
            {
                Debug.WriteLine("Error: have " + files.Count + " savegame list files");
            }

            savegameListFileId = id;
            return id;
        }

        public async Task<List<string>> GetCloudGameNames()
        {
            await ReadSavegameList();
            if (savegameList == null)
            {
                return null;
            }

            return savegameList.GetGames();
        }

        private async Task ReadSavegameList()
        {
            string fileId = await GetSavegameListFileIdOrCreate();
            if (fileId == null)
            {
                savegameList = null;
                Debug.WriteLine("Error: savegame list fileId is null");
                return;
            }

            MemoryStream stream = new MemoryStream();
            await googleDriveWrapper.DownloadFileAsync(fileId, stream);
            savegameList = new SavegameList();
            await savegameList.ReadFromStream(stream);
        }

        private async Task WriteSavegameList()
        {
            string fileId = await GetSavegameListFileIdOrCreate();

            if (fileId == null)
            {
                Debug.WriteLine("Error: savegame list fileId is null");
                return;
            }

            if (savegameList == null)
            {
                Debug.WriteLine("Error: savegame list is null");
                return;
            }

            MemoryStream stream = new MemoryStream();

            await savegameList.WriteToStream(stream);
            await googleDriveWrapper.UploadFileAsync(fileId, stream);
            stream.Close();
        }

        private async Task DebugCheckFileDownloadUpload(string fileId)
        {
            MemoryStream stream = new MemoryStream();
            await googleDriveWrapper.DownloadFileAsync(fileId, stream);
            Debug.WriteLine("Stream length: " + stream.Length);
            Debug.WriteLine("Stream position: " + stream.Position);
            stream.Position = 0;
            StreamReader streamReader = new StreamReader(stream);
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                Debug.WriteLine(line);
            }
            streamReader.Close();

            MemoryStream newContentStream = new MemoryStream();
            string testStr = "Testing";
            StreamWriter streamWriter = new StreamWriter(newContentStream);
            streamWriter.WriteLine(testStr);
            streamWriter.Flush();

            await googleDriveWrapper.UploadFileAsync(fileId, newContentStream);
            streamWriter.Close();
        }
    }
}
