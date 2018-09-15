using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
    /// Interaction logic for RepairFilesWindow.xaml
    /// </summary>
    public partial class RepairFilesWindow : Window
    {
        private const string EMPTY_ORPHANED_LIST_MESSAGE = "(No orphaned save files found.)";
        private const string EMPTY_MISSING_ENTRY_LIST_MESSAGE = "(No missing savegame entries found.)";

        SavegameSyncEngine savegameSync;

        public RepairFilesWindow()
        {
            InitializeComponent();
        }

        protected async override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            StartOperation();
            savegameSync = SavegameSyncEngine.GetInstance();
            Debug.Assert(savegameSync.IsLoggedIn());
            await UpdateOrphanedSaveList();
            await UpdateMissingEntriesListAsync();
            FinishOperation();
        }

        private void StartOperation()
        {
            downloadSaveButton.IsEnabled = false;
            deleteSaveButton.IsEnabled = false;
            orphanedSaveListBox.IsEnabled = false;
            backButton.IsEnabled = false;
            missingEntriesListBox.IsEnabled = false;
            deleteMissingEntriesButton.IsEnabled = false;
            downloadAllFilesButton.IsEnabled = false;
            deleteAllFilesButton.IsEnabled = false;
        }

        private void FinishOperation()
        {
            downloadSaveButton.IsEnabled = true;
            deleteSaveButton.IsEnabled = true;
            orphanedSaveListBox.IsEnabled = true;
            backButton.IsEnabled = true;
            missingEntriesListBox.IsEnabled = true;
            deleteMissingEntriesButton.IsEnabled = true;
            downloadAllFilesButton.IsEnabled = true;
            deleteAllFilesButton.IsEnabled = true;
        }

        private async Task UpdateOrphanedSaveList()
        {
            orphanedSaveListBox.Items.Clear();

            List<string> orphanedSaveFileNames;
            try
            {
                orphanedSaveFileNames = await savegameSync.GetOrphanedSaveFileNames();
            }
            catch (SavegameSyncException e)
            {
                orphanedSaveListBox.Items.Add("Error reading list:");
                orphanedSaveListBox.Items.Add(e.Message);
                return;
            }

            foreach (string name in orphanedSaveFileNames)
            {
                orphanedSaveListBox.Items.Add(name);
            }
            if (orphanedSaveFileNames.Count == 0)
            {
                orphanedSaveListBox.Items.Add(EMPTY_ORPHANED_LIST_MESSAGE);
            }
        }

        private async Task UpdateMissingEntriesListAsync()
        {
            missingEntriesListBox.Items.Clear();

            Dictionary<string, List<SavegameEntry>> missingEntries;
            try
            {
                missingEntries = await savegameSync.GetMissingSaveEntriesAsync();
            }
            catch (SavegameSyncException e)
            {
                missingEntriesListBox.Items.Add("Error reading list:");
                missingEntriesListBox.Items.Add(e.Message);
                return;
            }

            foreach (string gameName in missingEntries.Keys)
            {
                foreach (SavegameEntry save in missingEntries[gameName])
                {
                    missingEntriesListBox.Items.Add(save.Timestamp + " - " + gameName);
                }
            }
            if (missingEntries.Keys.Count == 0)
            {
                missingEntriesListBox.Items.Add(EMPTY_MISSING_ENTRY_LIST_MESSAGE);
            }
        }

        private async void deleteSaveButton_Click(object sender, RoutedEventArgs e)
        {
            object selection = orphanedSaveListBox.SelectedItem;
            if (selection == null)
            {
                return;
            }
            string saveFileName = selection.ToString();
            if (saveFileName == EMPTY_ORPHANED_LIST_MESSAGE)
            {
                return;
            }

            StartOperation();
            await SavegameSyncUtils.RunWithChecks(async () =>
            {
                await savegameSync.DeleteOrphanedSaveFile(saveFileName);
            });
            await UpdateOrphanedSaveList();
            FinishOperation();
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = new MainWindow();
            window.Show();
            Close();
        }

        private async void downloadSaveButton_Click(object sender, RoutedEventArgs e)
        {
            object selection = orphanedSaveListBox.SelectedItem;
            if (selection == null)
            {
                return;
            }
            string saveFileName = selection.ToString();
            if (saveFileName == EMPTY_ORPHANED_LIST_MESSAGE)
            {
                return;
            }

            string message = "Download orphaned save file? (The zip file will be downloaded into"
                           + " the directory in which this app was launched.)";
            ConfirmationDialog dialog = new ConfirmationDialog(message);
            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.GetValueOrDefault())
            {
                StartOperation();
                await SavegameSyncUtils.RunWithChecks(async () =>
                {
                    await savegameSync.DownloadOrphanedSaveFile(saveFileName);
                });
                FinishOperation();
            }
        }

        private async void deleteMissingEntriesButton_Click(object sender, RoutedEventArgs e)
        {
            StartOperation();
            await SavegameSyncUtils.RunWithChecks(async () =>
            {
                await savegameSync.DeleteMissingSaveEntriesAsync();
            });
            await UpdateMissingEntriesListAsync();
            FinishOperation();
        }

        private async void downloadAllFilesButton_Click(object sender, RoutedEventArgs e)
        {
            string directoryName = $"SavegameSync-all-files-{DateTime.Now.Day}-{DateTime.Now.Month}-{DateTime.Now.Year}-{DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}";

            string message = "Download all files? The files will be downloaded into a directory"
                           + " named " + directoryName + " located in the directory in which this"
                           + " app was launched.";

            ConfirmationDialog dialog = new ConfirmationDialog(message);
            bool? result = dialog.ShowDialog();
            if (result.HasValue && result.Value)
            {
                StartOperation();
                await SavegameSyncUtils.RunWithChecks(async () =>
                {
                    Directory.CreateDirectory(directoryName);
                    await savegameSync.DownloadAllFilesToDirectoryAsync(directoryName);
                });
                FinishOperation();
            }
        }

        private async void deleteAllFilesButton_Click(object sender, RoutedEventArgs e)
        {
            string message = "Delete all files stored in the cloud?";
            ConfirmationDialog dialog = new ConfirmationDialog(message);
            bool? result = dialog.ShowDialog();
            if (!result.HasValue || !result.Value)
            {
                return;
            }

            string message2 = "Are you sure you want to delete all files? (All your cloud saves will be lost!)";
            ConfirmationDialog dialog2 = new ConfirmationDialog(message2);
            bool? result2 = dialog2.ShowDialog();
            if (!result2.HasValue || !result2.Value)
            {
                return;
            }

            StartOperation();
            await SavegameSyncUtils.RunWithChecks(async () =>
            {
                await savegameSync.DeleteAllFilesAsync();
            });
            await UpdateOrphanedSaveList();
            await UpdateMissingEntriesListAsync();
            FinishOperation();
        }
    }
}
