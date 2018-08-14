using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace SavegameSync
{
    /// <summary>
    /// Singleton class containing the main app logic.
    /// </summary>
    public class SavegameSyncEngine
    {
        public const int SavesPerGame = 5;

        private const string ApplicationName = "Savegame Sync";
        private const string SavegameListFileName = "savegame-list.txt";
        private const string LocalGameListFileName = "local-game-list.txt";

        private static SavegameSyncEngine singleton;

        private DriveService service;
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
            service = await LoginToGoogleDrive();
        }

        /// <summary>
        /// Search for files in the appDataFolder having the given name.
        /// </summary>
        /// <param name="name">The filename to search for.</param>
        /// <returns>A list of File objects matching the given filename.</returns>
        private async Task<List<Google.Apis.Drive.v3.Data.File>> SearchFileByNameAsync(string name)
        {
            return await ListFilesHelperAsync(string.Format("name = '{0}'", name));
        }

        /// <summary>
        /// List all files in the appDataFolder.
        /// </summary>
        /// <returns>A list of File objects corresponding to all files in the appDataFolder.
        /// </returns>
        private async Task<List<Google.Apis.Drive.v3.Data.File>> GetAllFilesAsync()
        {
            return await ListFilesHelperAsync(null);
        }

        /// <summary>
        /// List files in the appDataFolder matching the given query string.
        /// </summary>
        /// <param name="query">The query string to use when searching. Set to null to list all
        /// files.</param>
        /// <returns>A list of File objects matching the given query string.</returns>
        /// <remarks>
        /// See https://developers.google.com/drive/v3/web/search-parameters for documentation of
        /// the query string.
        /// 
        /// TODO: Figure out how to tell if the initial request or subsequent pagination failed,
        ///       and handle those cases appropriately.
        /// </remarks>
        private async Task<List<Google.Apis.Drive.v3.Data.File>> ListFilesHelperAsync(string query)
        {
            List<Google.Apis.Drive.v3.Data.File> result = new List<Google.Apis.Drive.v3.Data.File>();

            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Spaces = "appDataFolder";
            if (query != null)
            {
                listRequest.Q = query;
            }

            bool done = false;
            Google.Apis.Drive.v3.Data.FileList fileList = await listRequest.ExecuteAsync();
            while (!done)
            {
                IList<Google.Apis.Drive.v3.Data.File> files = fileList.Files;
                result.AddRange(files);
                if (fileList.NextPageToken == null)
                {
                    Debug.WriteLine("Processed last page");
                    done = true;
                }
                else
                {
                    Debug.WriteLine("Retrieving another page");
                    FilesResource.ListRequest newListRequest = service.Files.List();
                    newListRequest.Spaces = "appDataFolder";
                    newListRequest.PageSize = 10;
                    newListRequest.PageToken = fileList.NextPageToken;
                    fileList = await newListRequest.ExecuteAsync();
                }
            }
            return result;
        }

        /// <summary>
        /// Create a file in the appDataFolder.
        /// </summary>
        /// <param name="name">The name of the new file</param>
        /// <returns>The ID of the new file</returns>
        private async Task<string> CreateFileAsync(string name)
        {
            List<string> parents = new List<string>();
            parents.Add("appDataFolder");
            var file = new Google.Apis.Drive.v3.Data.File()
            {
                Name = name,
                Parents = parents
            };
            FilesResource.CreateRequest createRequest = service.Files.Create(file);
            createRequest.Fields = "id";
            var responseFile = await createRequest.ExecuteAsync();
            return responseFile.Id;
        }

        /// <summary>
        /// Delete a file in the appDataFolder.
        /// </summary>
        /// <param name="fileId">The ID of the file to delete.</param>
        private async Task DeleteFileAsync(string fileId)
        {
            FilesResource.DeleteRequest deleteRequest = service.Files.Delete(fileId);
            await deleteRequest.ExecuteAsync();
        }

        /// <summary>
        /// Delete all files in the appDataFolder.
        /// </summary>
        private async Task DeleteAllFilesAsync()
        {
            List<Google.Apis.Drive.v3.Data.File> files = await GetAllFilesAsync();
            foreach (Google.Apis.Drive.v3.Data.File file in files)
            {
                await DeleteFileAsync(file.Id);
            }
        }

        /*
         * Based on the code in the .NET Quickstart in the Google Drive API documentation
         * (https://developers.google.com/drive/v3/web/quickstart/dotnet).
         */
        private async Task<DriveService> LoginToGoogleDrive()
        {
            string[] scopes = { DriveService.Scope.DriveAppdata };

            UserCredential credential;
            using (var stream = new FileStream("google-drive-client-secret.json", FileMode.Open, FileAccess.Read))
            {
                string credPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                credPath = System.IO.Path.Combine(credPath, ".credentials/drive-dotnet-quickstart.json");

                credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            DriveService service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            return service;
        }

        private async Task DownloadFile(string fileId, Stream stream)
        {
            FilesResource.GetRequest getRequest = service.Files.Get(fileId);
            await getRequest.DownloadAsync(stream);
        }

        private async Task UploadFile(string fileId, Stream stream, Google.Apis.Drive.v3.Data.File file = null)
        {
            if (file == null)
            {
                file = new Google.Apis.Drive.v3.Data.File();
            }
            FilesResource.UpdateMediaUpload updateMediaUpload = service.Files.Update(file, fileId, stream, file.MimeType);
            await updateMediaUpload.UploadAsync();
        }

        public async Task DebugCheckSavegameListFile()
        {
            await ReadSavegameList();
            savegameList.DebugPrintGameNames();
            savegameList.DebugPrintSaves("MOHAA");
            //savegameList.AddSave("MOHAA", Guid.NewGuid(), DateTime.Now);
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
            FileStream localGameListWriteStream = File.Open(LocalGameListFileName, FileMode.Open);
            await localGameList.WriteToStream(localGameListWriteStream);
            localGameListWriteStream.Close();
        }

        public List<string> GetLocalGameNames()
        {
            return localGameList.GetGameNames();
        }

        public async Task AddLocalGame(string gameName, string installDir)
        {
            localGameList.AddGame(gameName, installDir);
            await WriteLocalGameList();
        }

        public async Task DebugCheckLocalGameListFile()
        {
            await ReadLocalGameList();
            localGameList.DebugPrintGames();
            if (!localGameList.ContainsGame("MadeUpGame"))
            {
                await AddLocalGame("MadeUpGame", "C:\\Games\\MadeUpGame");

            }
        }

        public async Task DebugZipAndUploadSave()
        {
            // Wipe Google Drive app folder
            await DeleteAllFilesAsync();

            // Copy save files from the game's install directory into a temp directory according
            // to the spec
            string installDir = "C:\\Program Files (x86)\\GOG Galaxy\\Games\\Medal of Honor - Allied Assault War Chest";
            SaveSpec mohaaSpec = SaveSpecRepository.GetRepository().GetSaveSpec("Medal of Honor Allied Assault War Chest");
            string destDir = @"C:\Users\niell\Git\savegame-sync\tempMohaaUpload\testmohaa";
            CopySaveFilesFromInstallDir(mohaaSpec, installDir, destDir);

            // Find the last write time of the save
            DateTime latestFileWriteTime = FileUtils.GetLatestFileWriteTime(destDir);
            Console.WriteLine("Latest write time: " + latestFileWriteTime);

            // Assign the save a guid and make it into a zip file
            Guid saveGuid = Guid.NewGuid();
            Console.WriteLine("Guid: " + saveGuid);
            string zipFile = @"C:\Users\niell\Git\savegame-sync\tempMohaaUpload\" + saveGuid + ".zip";
            FileUtils.DeleteIfExists(zipFile);
            ZipFile.CreateFromDirectory(destDir, zipFile);

            // Upload save
            string remoteFileName = SavegameSyncUtils.GetSavegameFileNameFromGuid(saveGuid);
            string fileId = await CreateFileAsync(remoteFileName);
            using (FileStream fileStream = File.OpenRead(zipFile))
            {
                await UploadFile(fileId, fileStream);
            }

            // Download latest version of SavegameList
            await ReadSavegameList();

            // Add save to SavegameList
            savegameList.AddSave("Medal of Honor Allied Assault War Chest", saveGuid, latestFileWriteTime);

            // Upload SavegameList
            await WriteSavegameList();

            // Print data from the local copy of the savegameList
            savegameList.DebugPrintGameNames();
            savegameList.DebugPrintSaves("Medal of Honor Allied Assault War Chest");

            // Print all files in the Google Drive app folder
            Console.WriteLine("Listing all files: ");
            var files = await GetAllFilesAsync();
            for (int i = 0; i < files.Count; i++)
            {
                Console.WriteLine(string.Format("{0}, {1}, {2}", i, files[i].Name, files[i].Size));
            }
            Console.WriteLine("Done listing all files");
        }

        public async Task DebugDownloadAndUnzipSave()
        {
            // Download latest version of SavegameList
            await ReadSavegameList();

            // Read file name from SavegameList
            Guid saveGuid = savegameList.ReadSaves("Medal of Honor Allied Assault War Chest")[0].Guid;
            string saveFileName = SavegameSyncUtils.GetSavegameFileNameFromGuid(saveGuid);
            Console.WriteLine("Downloading save file " + saveFileName);

            // Download zipped save from Google Drive
            var files = await SearchFileByNameAsync(saveFileName);
            Debug.Assert(files.Count == 1);
            string saveFileId = files[0].Id;
            string zipFilePath = @"C:\Users\niell\Git\savegame-sync\tempMohaaDownload\" + saveFileName;
            using (FileStream fileStream = File.OpenWrite(zipFilePath))
            {
                await DownloadFile(saveFileId, fileStream);
            }

            // Unzip zipped save
            string tempSaveDir = @"C:\Users\niell\Git\savegame-sync\tempMohaaDownload\testmohaa";
            FileUtils.DeleteIfExists(tempSaveDir);
            ZipFile.ExtractToDirectory(zipFilePath, tempSaveDir);

            // TODO: Copy unzipped files/directories into game install directory
            string installDir = "C:\\Program Files (x86)\\GOG Galaxy\\Games\\Medal of Honor - Allied Assault War Chest";
            SaveSpec mohaaSpec = SaveSpecRepository.GetRepository().GetSaveSpec("Medal of Honor Allied Assault War Chest");
            CopySaveFilesIntoInstallDir(mohaaSpec, tempSaveDir, installDir);
        }

        public async Task DebugGoogleDriveFunctions()
        {
            List<string> dummyFileIds = new List<string>();
            for (int i = 0; i < 20; i++)
            {
                Console.WriteLine("Creating dummy file " + i);
                dummyFileIds.Add(await CreateFileAsync("DummyFile" + i));
            }

            var fileList = await GetAllFilesAsync();
            Console.WriteLine("Printing all files for the first time");
            foreach (Google.Apis.Drive.v3.Data.File file in fileList)
            {
                Console.WriteLine($"{file.Name}, {file.Id}");
            }
            Console.WriteLine("Done printing all files for the first time");

            for (int i = 0; i < dummyFileIds.Count; i++)
            {
                Console.WriteLine("Deleting dummy file " + i);
                await DeleteFileAsync(dummyFileIds[i]);
            }

            fileList = await GetAllFilesAsync();
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
            List<Google.Apis.Drive.v3.Data.File> files = await SearchFileByNameAsync(SavegameListFileName);
            string id = null;
            if (files.Count == 0)
            {
                id = await CreateFileAsync(SavegameListFileName);
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

        public List<string> GetCloudGameNames()
        {
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
            await DownloadFile(fileId, stream);
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
            await UploadFile(fileId, stream);
            stream.Close();
        }

        private async Task DebugCheckFileDownloadUpload(string fileId)
        {
            MemoryStream stream = new MemoryStream();
            await DownloadFile(fileId, stream);
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

            await UploadFile(fileId, newContentStream);
            streamWriter.Close();
        }
    }
}
