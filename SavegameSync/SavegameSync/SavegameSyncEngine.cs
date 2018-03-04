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

        public static SavegameSyncEngine GetInstance()
        {
            if (singleton == null)
            {
                singleton = new SavegameSyncEngine();
            }
            return singleton;
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

            SavegameList list = await ReadSavegameList(id);
            list.DebugPrintGames();
            list.DebugPrintSaves("MOHAA");
            //list.AddSave("MOHAA", "23094sdlkj", "100102330");
            await WriteSavegameList(list, id);
        }

        public async Task DebugCheckLocalGameListFile()
        {
            LocalGameList localGameList = new LocalGameList();
            FileStream localGameListStream = File.Open(LocalGameListFileName, FileMode.OpenOrCreate);
            await localGameList.ReadFromStream(localGameListStream);
            localGameListStream.Close();
            localGameList.DebugPrintGames();
            if (!localGameList.ContainsGame("MadeUpGame"))
            {
                localGameList.AddGame("MadeUpGame", "C:\\Games\\MadeUpGame");
            }
            FileStream localGameListWriteStream = File.Open(LocalGameListFileName, FileMode.Open);
            await localGameList.WriteToStream(localGameListWriteStream);
        }

        public void DebugZipAndUploadSave()
        {
            string installDir = "C:\\Program Files (x86)\\GOG Galaxy\\Games\\Medal of Honor - Allied Assault War Chest";
            SaveSpec mohaaSpec = SaveSpecRepository.GetRepository().GetSaveSpec("Medal of Honor Allied Assault War Chest");
            string destDir = "C:\\Users\\niell\\Git\\testmohaa";
            CopySaveFiles(mohaaSpec, installDir, destDir);

            DateTime latestFileWriteTime = FileUtils.GetLatestFileWriteTime(destDir);
            Console.WriteLine("Latest write time: " + latestFileWriteTime);

            Guid saveGuid = Guid.NewGuid();
            Console.WriteLine("Guid: " + saveGuid);
            string zipFile = "C:\\Users\\niell\\Git\\" + saveGuid + ".zip";
            FileUtils.DeleteIfExists(zipFile);
            ZipFile.CreateFromDirectory(destDir, zipFile);

            //TODO: upload save

            //TODO: download latest version of SavegameList

            //TODO: add save to SavegameList

            //TODO: upload SavegameList
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

        private void CopySaveFiles(SaveSpec saveSpec, string rootDir, string destDir)
        {
            FileUtils.DeleteIfExists(destDir);
            Directory.CreateDirectory(destDir);
            foreach (string subPath in saveSpec.SavePaths)
            {
                string originalPath = System.IO.Path.Combine(rootDir, subPath);
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
                    Console.WriteLine("Skipping missing subpath " + subPath);
                }
            }
        }

        private async Task<SavegameList> ReadSavegameList(string fileId)
        {
            MemoryStream stream = new MemoryStream();
            await DownloadFile(fileId, stream);
            SavegameList savegameList = new SavegameList();
            await savegameList.ReadFromStream(stream);
            return savegameList;
        }

        private async Task WriteSavegameList(SavegameList list, string fileId)
        {
            MemoryStream stream = new MemoryStream();

            await list.WriteToStream(stream);
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
