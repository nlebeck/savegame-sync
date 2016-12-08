using Microsoft.OneDrive.Sdk;
using Microsoft.OneDrive.Sdk.Authentication;
using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SavegameSyncWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OneDriveClient client;

        public MainWindow()
        {
            InitializeComponent();
        }

        protected async override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            await LoginToOneDrive();
        }

        private async Task LoginToOneDrive()
        {
            // I am not sure what best practices are regarding saving OAuth client IDs in code,
            // so for now, I will leave the client ID out of the code that appears online
            string clientId = "xxx";
            string redirectUrl = "urn:ietf:wg:oauth:2.0:oob";
            string[] scopes = { "onedrive.appfolder" };
            var msaAuthenticationProvider = new MsaAuthenticationProvider(
                clientId,
                redirectUrl,
                scopes,
                new CredentialVault(clientId));
            await msaAuthenticationProvider.RestoreMostRecentFromCacheOrAuthenticateUserAsync();
            client = new OneDriveClient("https://api.onedrive.com/v1.0", msaAuthenticationProvider);
            loginStatusTextBlock.Text = "Logged in";
        }
    }
}
