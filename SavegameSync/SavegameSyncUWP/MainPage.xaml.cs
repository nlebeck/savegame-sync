using Microsoft.OneDrive.Sdk;
using Microsoft.OneDrive.Sdk.Authentication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Xml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace SavegameSyncUWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private OneDriveClient client;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            await LoginToOneDrive();
        }

        private async Task LoginToOneDrive()
        {
            string clientId = null;
            using (FileStream configFileStream = System.IO.File.OpenRead("Config.xml"))
            {
                XmlDocument configFileDoc = new XmlDocument();
                configFileDoc.Load(configFileStream);
                XmlNodeList nodes = configFileDoc.DocumentElement.ChildNodes;
                foreach (XmlNode node in nodes)
                {
                    if (node.Name == "clientId")
                    {
                        clientId = node.InnerText;
                    }
                }
            }

            string redirectUrl = "urn:ietf:wg:oauth:2.0:oob";
            string[] scopes = { "onedrive.appfolder" };
            var msaAuthenticationProvider = new MsaAuthenticationProvider(
                clientId,
                redirectUrl,
                scopes,
                /*CredentialCache*/ null,
                new CredentialVault(clientId));
            await msaAuthenticationProvider.RestoreMostRecentFromCacheOrAuthenticateUserAsync();
            client = new OneDriveClient("https://api.onedrive.com/v1.0", msaAuthenticationProvider);
            loginStatusTextBlock.Text = "Logged in";
        }
    }
}
