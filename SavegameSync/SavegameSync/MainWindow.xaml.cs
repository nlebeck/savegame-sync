using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            cloudGameListBox.Items.Add("Loading...");
            savegameListBox.Items.Add("Loading...");

            StartOperation("Logging in...");
            savegameSync = SavegameSyncEngine.GetInstance();
            await savegameSync.Init();
            await savegameSync.Login();
            FinishOperation();

            StartOperation("Updating game lists...");
            //await savegameSync.DebugCheckSavegameListFile();
            await savegameSync.DebugCheckLocalGameListFile();
            //await savegameSync.DebugZipAndUploadSave();
            //await savegameSync.DebugDownloadAndUnzipSave();
            Console.WriteLine("Done debugging!");

            UpdateLocalGameList();
            UpdateCloudGameList();
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

        private void UpdateCloudGameList()
        {
            cloudGameListBox.Items.Clear();
            List<string> cloudGameNames = savegameSync.GetCloudGameNames();

            if (cloudGameNames == null)
            {
                cloudGameListBox.Items.Add("Error: could not read list");
                return;
            }

            foreach (string gameName in cloudGameNames)
            {
                cloudGameListBox.Items.Add(gameName);
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
            }
        }

        private async void localGameListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            Debug.Assert(e.AddedItems.Count == 1);
            string gameName = e.AddedItems[0].ToString();
            await UpdateSavegameList(gameName);
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
            cloudGameListBox.IsEnabled = false;
            savegameListBox.IsEnabled = false;
        }

        private void FinishOperation()
        {
            statusTextBlock.Text = "Ready.";
            copyToCloudButton.IsEnabled = true;
            copyFromCloudButton.IsEnabled = true;
            addGameButton.IsEnabled = true;
            localGameListBox.IsEnabled = true;
            cloudGameListBox.IsEnabled = true;
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

            // Because UpdateSavegameList() populates the ListBox in reverse order, we need to undo
            // that mapping to calculate the index of the save in the SavegameLiset.
            int selectedIndex = savegameListBox.SelectedIndex;
            int saveIndex = (savegameListBox.Items.Count - 1) - selectedIndex;
            if (saveIndex == -1)
            {
                return;
            }

            StartOperation("Downloading save for " + gameName + "...");
            await savegameSync.DownloadAndUnzipSave(gameName, saveIndex);
            FinishOperation();
        }
    }
}
