using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SavegameSync
{
    public class GoogleDriveWrapper
    {
        private const string ApplicationName = "Savegame Sync";

        private DriveService service;

        private GoogleDriveWrapper(DriveService driveService)
        {
            service = driveService;
        }

        public static async Task<GoogleDriveWrapper> Create()
        {
            DriveService service = await LoginToGoogleDrive();
            GoogleDriveWrapper wrapper = new GoogleDriveWrapper(service);
            return wrapper;
        }

        /// <summary>
        /// Search for files in the appDataFolder having the given name.
        /// </summary>
        /// <param name="name">The filename to search for.</param>
        /// <returns>A list of File objects matching the given filename.</returns>
        public async Task<List<Google.Apis.Drive.v3.Data.File>> SearchFileByNameAsync(string name)
        {
            return await ListFilesHelperAsync(string.Format("name = '{0}'", name));
        }

        /// <summary>
        /// List all files in the appDataFolder.
        /// </summary>
        /// <returns>A list of File objects corresponding to all files in the appDataFolder.
        /// </returns>
        public async Task<List<Google.Apis.Drive.v3.Data.File>> GetAllFilesAsync()
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
        public async Task<string> CreateFileAsync(string name)
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
        public async Task DeleteFileAsync(string fileId)
        {
            FilesResource.DeleteRequest deleteRequest = service.Files.Delete(fileId);
            await deleteRequest.ExecuteAsync();
        }

        /// <summary>
        /// Delete all files in the appDataFolder with the given name.
        /// </summary>
        /// <param name="fileName">The name of files to delete.</param>
        /// <returns>The number of files deleted.</returns>
        public async Task<int> DeleteAllFilesWithNameAsync(string fileName)
        {
            var files = await SearchFileByNameAsync(fileName);
            foreach (var file in files)
            {
                string fileId = file.Id;
                await DeleteFileAsync(fileId);
            }
            return files.Count;
        }

        /// <summary>
        /// Delete all files in the appDataFolder.
        /// </summary>
        public async Task DeleteAllFilesAsync()
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
        private static async Task<DriveService> LoginToGoogleDrive()
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

        public async Task DownloadFileAsync(string fileId, Stream stream)
        {
            FilesResource.GetRequest getRequest = service.Files.Get(fileId);
            await getRequest.DownloadAsync(stream);
        }

        public async Task UploadFileAsync(string fileId, Stream stream, Google.Apis.Drive.v3.Data.File file = null)
        {
            if (file == null)
            {
                file = new Google.Apis.Drive.v3.Data.File();
            }
            FilesResource.UpdateMediaUpload updateMediaUpload = service.Files.Update(file, fileId, stream, file.MimeType);
            await updateMediaUpload.UploadAsync();
        }
    }
}
