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

            savegameSync = SavegameSyncEngine.GetInstance();
            await savegameSync.Init();
            await savegameSync.Login();

            loginStatusTextBlock.Text = "";

            //await savegameSync.DebugCheckSavegameListFile();
            await savegameSync.DebugCheckLocalGameListFile();
            await savegameSync.DebugZipAndUploadSave();
            await savegameSync.DebugDownloadAndUnzipSave();
            Console.WriteLine("Done debugging!");

            UpdateLocalGameList();
            UpdateCloudGameList();
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
            savegameListBox.Items.Clear();
            savegameListBox.Items.Add("Loading...");
            currentGameTextBlock.Text = gameName;

            List<SavegameEntry> saves = await savegameSync.ReadSaves(gameName);
            savegameListBox.Items.Clear();
            for (int i = saves.Count - 1; i >= 0; i--) {
                savegameListBox.Items.Add(saves[i].Timestamp);
            }
            if (saves.Count == 0)
            {
                savegameListBox.Items.Add("No saves found.");
            }
        }
    }
}
