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
            savegameListBox.Items.Add("Loading...");

            StartOperation("Logging in...");
            savegameSync = SavegameSyncEngine.GetInstance();
            await savegameSync.Init();
            if (!savegameSync.IsLoggedIn())
            {
                await savegameSync.Login();
            }
            FinishOperation();

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

        private async Task UpdateSavegameList(string gameName)
        {
            savegameListBox.Items.Clear();
            savegameListBox.Items.Add("Loading...");
            currentGameTextBlock.Text = gameName;

            List<SavegameEntry> saves = await savegameSync.ReadSaves(gameName);
            savegameListBox.Items.Clear();
            for (int i = saves.Count - 1; i >= 0; i--)
            {
                savegameListBox.Items.Add(saves[i].Timestamp);
            }
            if (saves.Count == 0)
            {
                savegameListBox.Items.Add("No saves found.");
            }
        }

        private int GetSaveIndexFromListBoxIndex(int listBoxIndex)
        {
            // Because UpdateSavegameList() populates the ListBox in reverse order, we need to undo
            // that mapping to calculate the index of the save in the SavegameLiset.
            return (savegameListBox.Items.Count - 1) - listBoxIndex;
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
            }
        }

        private async void localGameListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Debug.Assert(e.AddedItems.Count == 1);
            string gameName = e.AddedItems[0].ToString();

            await UpdateSavegameList(gameName);
            UpdateLocalSavegameTimestampDisplay(gameName);
        }

        private void UpdateLocalSavegameTimestampDisplay(string selectedGameName)
        {
            string message = null;
            string installDir = savegameSync.GetLocalInstallDir(selectedGameName);
            SaveSpec saveSpec = SaveSpecRepository.GetRepository().GetSaveSpec(selectedGameName);
            if (installDir == null)
            {
                message = "Error: game not in local game list";
            }
            else if (saveSpec == null)
            {
                message = "Error: save spec not found";
            }
            else if (!Directory.Exists(installDir))
            {
                message = "Error: install dir does not exist";
            }
            else
            {
                DateTime timestamp = savegameSync.GetLocalSaveTimestamp(saveSpec, installDir);
                message = timestamp.ToString();
            }
            localSaveTimestampTextBlock.Text = message;
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
            await UpdateSavegameList(gameName);
            FinishOperation();
        }

        private void StartOperation(string message)
        {
            statusTextBlock.Text = message;
            copyToCloudButton.IsEnabled = false;
            copyFromCloudButton.IsEnabled = false;
            addGameButton.IsEnabled = false;
            localGameListBox.IsEnabled = false;
            savegameListBox.IsEnabled = false;
        }

        private void FinishOperation()
        {
            statusTextBlock.Text = "Ready.";
            copyToCloudButton.IsEnabled = true;
            copyFromCloudButton.IsEnabled = true;
            addGameButton.IsEnabled = true;
            localGameListBox.IsEnabled = true;
            savegameListBox.IsEnabled = true;
        }

        private async void copyFromCloudButton_Click(object sender, RoutedEventArgs e)
        {
            object selectedGame = localGameListBox.SelectedItem;
            if (selectedGame == null)
            {
                return;
            }
            string gameName = selectedGame.ToString();

            int selectedIndex = savegameListBox.SelectedIndex;
            if (selectedIndex == -1)
            {
                return;
            }
            int saveIndex = GetSaveIndexFromListBoxIndex(selectedIndex);

            StartOperation("Downloading save for " + gameName + "...");
            await savegameSync.DownloadAndUnzipSave(gameName, saveIndex);
            UpdateLocalSavegameTimestampDisplay(gameName);
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

            int selectedIndex = savegameListBox.SelectedIndex;
            if (selectedIndex == -1)
            {
                return;
            }
            int saveIndex = GetSaveIndexFromListBoxIndex(selectedIndex);

            StartOperation("Deleting cloud save for " + gameName + "...");
            await savegameSync.DeleteSave(gameName, saveIndex);
            await UpdateSavegameList(gameName);
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
    }
}
