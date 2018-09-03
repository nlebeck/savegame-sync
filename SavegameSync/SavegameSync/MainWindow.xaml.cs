using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace SavegameSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SavegameSyncEngine savegameSync;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected async override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            localGameListBox.Items.Add("Loading...");

            StartOperation("Logging in...");
            savegameSync = SavegameSyncEngine.GetInstance();
            await savegameSync.Init();
            if (!savegameSync.IsLoggedIn())
            {
                await savegameSync.Login();
            }
            FinishOperation();

            savegameListControl.Initialize();

            StartOperation("Updating game lists...");
            UpdateLocalGameList();
            FinishOperation();
        }

        private void UpdateLocalGameList()
        {
            localGameListBox.Items.Clear();
            List<string> localGameNames = savegameSync.GetLocalGameNames();
            foreach (string gameName in localGameNames)
            {
                localGameListBox.Items.Add(gameName);
            }
        }

        private async void addGameButton_Click(object sender, RoutedEventArgs e)
        {
            string path = null;
            string gameName = null;

            // Apparently I have to use either Windows Forms or a separate
            // NuGet package to do this...
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                path = dialog.SelectedPath;
                Debug.WriteLine("Selected path: " + path);

                AddGameDialog addGameDialog = new AddGameDialog();
                bool? result2 = addGameDialog.ShowDialog();
                if (result2.HasValue && result2.GetValueOrDefault())
                {
                    gameName = addGameDialog.GameName;
                }
                Debug.WriteLine("Selected game name: " + gameName);
            }

            if (path != null && gameName != null)
            {
                await savegameSync.AddLocalGame(gameName, path);
                UpdateLocalGameList();
                UpdateLocalGameInfoDisplays(null);
            }
        }

        private async void localGameListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }
            Debug.Assert(e.AddedItems.Count == 1);

            string gameName = e.AddedItems[0].ToString();

            await savegameListControl.SetGameAndUpdateAsync(gameName);
            UpdateLocalGameInfoDisplays(gameName);
        }

        private void UpdateLocalGameInfoDisplays(string selectedGameName)
        {
            if (selectedGameName == null)
            {
                localSaveTimestampTextBlock.Text = "";
                installDirTextBlock.Text = "";
                return;
            }

            string timestampMessage = null;
            string installDir = savegameSync.GetLocalInstallDir(selectedGameName);
            SaveSpec saveSpec = SaveSpecRepository.GetRepository().GetSaveSpec(selectedGameName);
            if (installDir == null)
            {
                timestampMessage = "Error: game not in local game list";
            }
            else if (saveSpec == null)
            {
                timestampMessage = "Error: save spec not found";
            }
            else if (!Directory.Exists(installDir))
            {
                timestampMessage = "Error: install dir does not exist";
            }
            else
            {
                DateTime timestamp = savegameSync.GetLocalSaveTimestamp(saveSpec, installDir);
                timestampMessage = timestamp.ToString();
            }
            localSaveTimestampTextBlock.Text = timestampMessage;
            installDirTextBlock.Text = installDir;
        }

        private async void copyToCloudButton_Click(object sender, RoutedEventArgs e)
        {
            object selectedGame = localGameListBox.SelectedItem;
            if (selectedGame == null)
            {
                return;
            }
            string gameName = selectedGame.ToString();
            StartOperation("Uploading save for " + gameName + "...");
            await savegameSync.ZipAndUploadSave(gameName);
            await savegameListControl.SetGameAndUpdateAsync(gameName);
            FinishOperation();
        }

        private void StartOperation(string message)
        {
            statusTextBlock.Text = message;
            copyToCloudButton.IsEnabled = false;
            copyFromCloudButton.IsEnabled = false;
            addGameButton.IsEnabled = false;
            localGameListBox.IsEnabled = false;
            deleteLocalGameButton.IsEnabled = false;
            deleteCloudSaveButton.IsEnabled = false;
            debugButton.IsEnabled = false;
            cloudGameListButton.IsEnabled = false;
            orphanedSaveButton.IsEnabled = false;
            savegameListControl.IsEnabled = false;
        }

        private void FinishOperation()
        {
            statusTextBlock.Text = "Ready.";
            copyToCloudButton.IsEnabled = true;
            copyFromCloudButton.IsEnabled = true;
            addGameButton.IsEnabled = true;
            localGameListBox.IsEnabled = true;
            deleteLocalGameButton.IsEnabled = true;
            deleteCloudSaveButton.IsEnabled = true;
            debugButton.IsEnabled = true;
            cloudGameListButton.IsEnabled = true;
            orphanedSaveButton.IsEnabled = true;
            savegameListControl.IsEnabled = true;
        }

        private async void copyFromCloudButton_Click(object sender, RoutedEventArgs e)
        {
            object selectedGame = localGameListBox.SelectedItem;
            if (selectedGame == null)
            {
                return;
            }
            string gameName = selectedGame.ToString();

            int saveIndex = savegameListControl.GetSelectedSaveIndex();
            if (saveIndex == -1)
            {
                return;
            }

            StartOperation("Downloading save for " + gameName + "...");
            await savegameSync.DownloadAndUnzipSave(gameName, saveIndex);
            UpdateLocalGameInfoDisplays(gameName);
            FinishOperation();
        }

        private async void deleteCloudSaveButton_Click(object sender, RoutedEventArgs e)
        {
            object selectedGame = localGameListBox.SelectedItem;
            if (selectedGame == null)
            {
                return;
            }
            string gameName = selectedGame.ToString();

            int saveIndex = savegameListControl.GetSelectedSaveIndex();
            if (saveIndex == -1)
            {
                return;
            }

            StartOperation("Deleting cloud save for " + gameName + "...");
            await savegameSync.DeleteSave(gameName, saveIndex);
            await savegameListControl.SetGameAndUpdateAsync(gameName);
            FinishOperation();
        }

        private async void debugButton_Click(object sender, RoutedEventArgs e)
        {
            StartOperation("Doing debugging stuff...");
            await savegameSync.DebugPrintLocalGameListFile();
            //await savegameSync.DebugAddNonexistentLocalGame();
            await savegameSync.DebugPrintSavegameListFile();
            await savegameSync.DebugPrintAllFiles();
            FinishOperation();
        }

        private void cloudGameListButton_Click(object sender, RoutedEventArgs e)
        {
            // The window-switching code in this app is based on this StackOverflow post:
            // https://stackoverflow.com/a/21706434.

            CloudGameListWindow window = new CloudGameListWindow();
            window.Show();
            this.Close();
        }

        private async void deleteLocalGameButton_Click(object sender, RoutedEventArgs e)
        {
            object selectedGame = localGameListBox.SelectedItem;
            if (selectedGame == null)
            {
                return;
            }
            string gameName = selectedGame.ToString();

            string message = "Delete " + gameName + " from local game list? (All cloud saves for "
                                       + "this game will remain in the cloud, but you'll have to "
                                       + "add this game to the local game list again to download "
                                       + "or upload saves.)";
            ConfirmationDialog dialog = new ConfirmationDialog(message);
            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.GetValueOrDefault())
            {
                StartOperation("Deleting game from local game list...");
                await savegameSync.DeleteLocalGame(gameName);
                UpdateLocalGameList();
                await savegameListControl.SetGameAndUpdateAsync(null);
                UpdateLocalGameInfoDisplays(null);
                FinishOperation();
            }
        }

        private void orphanedSaveButton_Click(object sender, RoutedEventArgs e)
        {
            RepairFilesWindow window = new RepairFilesWindow();
            window.Show();
            Close();
        }
    }
}
