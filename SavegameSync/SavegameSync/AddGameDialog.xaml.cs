using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
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
    /// Interaction logic for AddGameDialog.xaml
    /// </summary>
    public partial class AddGameDialog : Window
    {
        public string GameName { get; set; }

        public AddGameDialog()
        {
            InitializeComponent();
            foreach (SaveSpec spec in SaveSpecRepository.GetRepository().GetAllSaveSpecs())
            {
                listBox.Items.Add(spec.GameName);
            }
            listBox.Items.Add("Testing");
        }

        private void OK_Button_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(listBox.SelectedItem);
            GameName = (String)listBox.SelectedItem;
            DialogResult = true;
        }

        private void Cancel_Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
