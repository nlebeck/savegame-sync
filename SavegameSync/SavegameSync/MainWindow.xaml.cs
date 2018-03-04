using System;
using System.Windows;

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
            savegameSync = SavegameSyncEngine.GetInstance();
            await savegameSync.Login();
            await savegameSync.DebugCheckSavegameListFile();
            await savegameSync.DebugCheckLocalGameListFile();
            savegameSync.DebugZipAndUploadSave();
            Console.WriteLine("Done debugging!");
        }
    }
}
