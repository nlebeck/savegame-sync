using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.OneDrive.Sdk;
using Microsoft.OneDrive.Sdk.Authentication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SavegameSyncWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string applicationName = "Savegame Sync";

        private DriveService service;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected async override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            service = await LoginToGoogleDrive();
            await CheckSavegameListFile();
        }

        /// <summary>
        /// Search for files in the appDataFolder having the given name.
        /// </summary>
        /// <param name="name">The filename to search for</param>
        /// <returns>A list of file IDs matching the given filename</returns>
        /// <remarks>
        /// TODO: Figure out how to tell if the initial request or subsequent pagination failed,
        ///       and handle those cases appropriately.
        /// </remarks>
        private async Task<List<string>> SearchFileByNameAsync(string name)
        {
            List<string> fileIds = new List<string>();

            FilesResource.ListRequest listRequest = service.Files.List();
            listRequest.Spaces = "appDataFolder";
            listRequest.PageSize = 10;
            listRequest.Fields = "nextPageToken, files(id, name)";
            listRequest.Q = string.Format("name = '{0}'", name);

            bool done = false;
            Google.Apis.Drive.v3.Data.FileList fileList = await listRequest.ExecuteAsync();
            while (!done)
            {
                IList<Google.Apis.Drive.v3.Data.File> files = fileList.Files;
                if (files != null)
                {
                    foreach (var file in files)
                    {
                        fileIds.Add(file.Id);
                    }
                }
                if (fileList.NextPageToken == null)
                {
                    Debug.WriteLine("Processed last page");
                    done = true;
                }
                else
                {
                    Debug.WriteLine("Retrieving another page");
                    FilesResource.ListRequest newListRequest = service.Files.List();
                    newListRequest.PageToken = fileList.NextPageToken;
                    fileList = await newListRequest.ExecuteAsync();
                }
            }
            return fileIds;
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
                ApplicationName = applicationName,
            });

            return service;
        }

        private async Task CheckSavegameListFile()
        {
            List<string> fileIds = await SearchFileByNameAsync("savegame-list.txt");
            if (fileIds.Count == 0)
            {
                string id = await CreateFileAsync("savegame-list.txt");
                Debug.WriteLine("Created new file with Id " + id);
            }
            else if (fileIds.Count == 1)
            {
                Debug.WriteLine("savegame-list.txt exists already");
            }
            else
            {
                Debug.WriteLine("Error: have " + fileIds.Count + " savegame-list.txt files");
            }
        }
    }
}
