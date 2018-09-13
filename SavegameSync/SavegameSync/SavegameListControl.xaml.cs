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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SavegameSync
{
    /// <summary>
    /// Interaction logic for SavegameListControl.xaml
    /// </summary>
    public partial class SavegameListControl : UserControl
    {
        // The name of this class is a bit confusing right now, since it shows the last of saves
        // for a particular game, whereas the SavegameList class holds the per-game save list for
        // all games.
        //
        // TODO: Change this control's name to something better?

        private SavegameSyncEngine savegameSync;

        public SavegameListControl()
        {
            InitializeComponent();
        }

        public void Initialize()
        {
            savegameListBox.Items.Add("Loading...");

            savegameSync = SavegameSyncEngine.GetInstance();
            if (!savegameSync.IsLoggedIn())
            {
                throw new InvalidOperationException("The SavegameSyncEngine must be logged in before calling this method.");
            }
        }

        public async Task SetGameAndUpdateAsync(string gameName)
        {
            savegameListBox.Items.Clear();

            if (gameName == null)
            {
                return;
            }

            savegameListBox.Items.Add("Loading...");
            currentGameTextBlock.Text = gameName;

            List<SavegameEntry> saves = new List<SavegameEntry>();
            try
            {
                saves = await savegameSync.ReadSaves(gameName);
            }
            catch (SavegameSyncException e)
            {
                savegameListBox.Items.Clear();
                savegameListBox.Items.Add("Error reading saves:");
                savegameListBox.Items.Add(e.Message);
                return;
            }
            catch (Exception e)
            {
                savegameListBox.Items.Clear();
                savegameListBox.Items.Add("Exception thrown while reading saves:");
                savegameListBox.Items.Add(e.GetType());
                savegameListBox.Items.Add(e.Message);
                return;
            }


            savegameListBox.Items.Clear();
            for (int i = saves.Count - 1; i >= 0; i--)
            {
                savegameListBox.Items.Add(i + " - " + saves[i].Timestamp);
            }
            if (saves.Count == 0)
            {
                savegameListBox.Items.Add("No saves found.");
            }
        }

        public int GetSelectedSaveIndex()
        {
            int selectedIndex = savegameListBox.SelectedIndex;
            if (selectedIndex == -1)
            {
                return -1;
            }
            int saveIndex = GetSaveIndexFromListBoxIndex(selectedIndex);
            return saveIndex;
        }

        private int GetSaveIndexFromListBoxIndex(int listBoxIndex)
        {
            // Because UpdateSavegameList() populates the ListBox in reverse order, we need to undo
            // that mapping to calculate the index of the save in the SavegameLiset.
            return (savegameListBox.Items.Count - 1) - listBoxIndex;
        }

        private void Enable()
        {
            savegameListBox.IsEnabled = true;
        }

        private void Disable()
        {
            savegameListBox.IsEnabled = false;
        }

        private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if ((bool)e.NewValue)
            {
                Enable();
            }
            else
            {
                Disable();
            }
        }
    }
}
