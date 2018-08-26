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
            FinishOperation();
        }

        private void StartOperation()
        {
            backButton.IsEnabled = false;
            deleteGameButton.IsEnabled = false;
            cloudGameListBox.IsEnabled = false;
        }

        private void FinishOperation()
        {
            backButton.IsEnabled = true;
            deleteGameButton.IsEnabled = true;
            cloudGameListBox.IsEnabled = true;
        }

        private async Task UpdateCloudGameList()
        {
            cloudGameListBox.Items.Clear();
            List<string> cloudGameNames = await savegameSync.GetCloudGameNames();

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
                await savegameSync.DeleteGameFromCloud(gameName);
                await UpdateCloudGameList();
                FinishOperation();
            }
        }
    }
}
