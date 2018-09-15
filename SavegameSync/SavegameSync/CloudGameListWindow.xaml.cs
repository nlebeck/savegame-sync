using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SavegameSync
{
    /// <summary>
    /// Interaction logic for CloudGameListWindow.xaml
    /// </summary>
    public partial class CloudGameListWindow : Window
    {
        private SavegameSyncEngine savegameSync;

        public CloudGameListWindow()
        {
            InitializeComponent();
        }

        protected override async void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            cloudGameListBox.Items.Add("Loading...");

            StartOperation();
            savegameSync = SavegameSyncEngine.GetInstance();
            Debug.Assert(savegameSync.IsLoggedIn());
            await UpdateCloudGameList();
            savegameListControl.Initialize();
            FinishOperation();
        }

        private void StartOperation()
        {
            backButton.IsEnabled = false;
            deleteGameButton.IsEnabled = false;
            cloudGameListBox.IsEnabled = false;
            deleteSaveButton.IsEnabled = false;
            downloadSaveButton.IsEnabled = false;
            savegameListControl.IsEnabled = false;
        }

        private void FinishOperation()
        {
            backButton.IsEnabled = true;
            deleteGameButton.IsEnabled = true;
            cloudGameListBox.IsEnabled = true;
            deleteSaveButton.IsEnabled = true;
            downloadSaveButton.IsEnabled = true;
            savegameListControl.IsEnabled = true;
        }

        private async Task UpdateCloudGameList()
        {
            cloudGameListBox.Items.Clear();
            List<string> cloudGameNames;

            try
            {
                cloudGameNames = await savegameSync.GetCloudGameNames();
            }
            catch (SavegameSyncException e)
            {
                cloudGameListBox.Items.Add("Error reading cloud game list:");
                cloudGameListBox.Items.Add(e.Message);
                return;
            }

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

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = new MainWindow();
            window.Show();
            this.Close();
        }

        private async void deleteGameButton_Click(object sender, RoutedEventArgs e)
        {
            if (cloudGameListBox.SelectedItem == null)
            {
                return;
            }
            string gameName = cloudGameListBox.SelectedItem.ToString();

            string message = "Delete all save files stored in the cloud for "
                + gameName + "?";
            ConfirmationDialog dialog = new ConfirmationDialog(message);
            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.GetValueOrDefault())
            {
                StartOperation();
                await SavegameSyncUtils.RunWithChecks(async () =>
                {
                    await savegameSync.DeleteGameFromCloud(gameName);
                });
                await UpdateCloudGameList();
                await savegameListControl.SetGameAndUpdateAsync(null);
                FinishOperation();
            }
        }

        private async void deleteSaveButton_Click(object sender, RoutedEventArgs e)
        {
            object selection = cloudGameListBox.SelectedItem;
            if (selection == null)
            {
                return;
            }
            string gameName = selection.ToString();

            int saveIndex = savegameListControl.GetSelectedSaveIndex();
            if (saveIndex == -1)
            {
                return;
            }

            StartOperation();
            await SavegameSyncUtils.RunWithChecks(async () =>
            {
                await savegameSync.DeleteSave(gameName, saveIndex);
            });
            await savegameListControl.SetGameAndUpdateAsync(gameName);
            FinishOperation();
        }

        private async void cloudGameListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0)
            {
                return;
            }
            Debug.Assert(e.AddedItems.Count == 1);

            string gameName = e.AddedItems[0].ToString();

            await savegameListControl.SetGameAndUpdateAsync(gameName);
        }

        private async void downloadSaveButton_Click(object sender, RoutedEventArgs e)
        {
            object selection = cloudGameListBox.SelectedItem;
            if (selection == null)
            {
                return;
            }
            string gameName = selection.ToString();

            int saveIndex = savegameListControl.GetSelectedSaveIndex();
            if (saveIndex == -1)
            {
                return;
            }

            string downloadedFileName = savegameSync.GetSpecificSaveFileDownloadPath(gameName, saveIndex);
            string message = "Download selected save? (The downloaded zip file will be downloaded"
                + " into the directory in which this app was launched and will be named \""
                + downloadedFileName + ".\")";
            ConfirmationDialog dialog = new ConfirmationDialog(message);
            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.GetValueOrDefault())
            {
                StartOperation();
                await SavegameSyncUtils.RunWithChecks(async () =>
                {
                    await savegameSync.DownloadSpecificSaveFileAsync(gameName, saveIndex);
                });
                FinishOperation();
            }
        }
    }
}
