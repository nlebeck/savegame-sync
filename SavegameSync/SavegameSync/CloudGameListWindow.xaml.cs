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

            savegameSync = SavegameSyncEngine.GetInstance();
            Debug.Assert(savegameSync.IsLoggedIn());
            await UpdateCloudGameList();
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
    }
}
