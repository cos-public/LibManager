using Microsoft;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
//using System.Windows.Forms;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LibManager
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class LibManagerOptionsPage : UserControl
    {
        private ObservableCollection<string> _items;

        public List<string> LibraryPackFiles
        {
            get
            {
                return new List<string>(_items as IEnumerable<string>);
            }
            set
            {
                _items = new ObservableCollection<string>(value);
                LibraryList.ItemsSource = _items;
            }
        }

        public LibManagerOptionsPage()
        {
            InitializeComponent();
        }


        private void Add_Click(object sender, RoutedEventArgs e)
        {
            Assumes.NotNull(_items);
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML library definition files (*.xml)|*.xml";
            if (openFileDialog.ShowDialog() == true)
            {
                _items.Add(openFileDialog.FileName);
            }
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            Assumes.NotNull(_items);
            if (LibraryList.Items.CurrentItem != null)
            {
                int i = LibraryList.SelectedIndex;
                if (i >= 0)
                {
                    _items.RemoveAt(i);
                }
            }
        }

        private void MoveUp_Click(object sender, RoutedEventArgs e)
        {
            Assumes.NotNull(_items);
            if (LibraryList.Items.CurrentItem == null)
                return;
            int i = LibraryList.SelectedIndex;
            if (i < 1)
                return;
            _items.Move(i, i - 1);
        }

        private void MoveDown_Click(object sender, RoutedEventArgs e)
        {
            Assumes.NotNull(_items);
            if (LibraryList.Items.CurrentItem == null)
                return;
            int i = LibraryList.SelectedIndex;
            if (i < 0 || i >= LibraryList.Items.Count - 1)
                return;
            _items.Move(i, i + 1);
        }
    }
}
