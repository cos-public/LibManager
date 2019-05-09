using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace LibManager
{
    /// <summary>
    /// Interaction logic for LibraryConfigStateIndicator.xaml
    /// </summary>
    public partial class LibraryConfigStateIndicator : UserControl
    {
        [BindableAttribute(true)]
        public LibraryManagerDialog.LibraryConfigState State
        {
            get { return (LibraryManagerDialog.LibraryConfigState) this.GetValue(StateProperty); }
            set { this.SetValue(StateProperty, value); }
        }

        public static readonly DependencyProperty StateProperty = DependencyProperty.Register(
            "State", typeof(LibraryManagerDialog.LibraryConfigState), typeof(LibraryConfigStateIndicator), new PropertyMetadata(null, StatePropertyChanged));

        private static void StatePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((LibraryConfigStateIndicator)d).StatePropertyChanged((LibraryManagerDialog.LibraryConfigState)e.NewValue);
        }

        private void StatePropertyChanged(LibraryManagerDialog.LibraryConfigState state)
        {
            if (state != null)
            {
                this.Visibility = Visibility.Visible;
                var all_configured_style = (FindResource("AllConfiguredStyle") as Style);
                var partial_configured_style = (FindResource("PartialConfiguredStyle") as Style);
                ContainsIncludePath.Style = state.ContainsIncludePath ? all_configured_style : null;
                ContainsLibDir.Style = state.ContainsLibDir == LibraryManagerDialog.LibraryConfigState.State.All ? all_configured_style :
                    state.ContainsLibDir == LibraryManagerDialog.LibraryConfigState.State.Partial ? partial_configured_style :
                        null;
                ContainsLib.Style = state.ContainsLib == LibraryManagerDialog.LibraryConfigState.State.All ? all_configured_style :
                    state.ContainsLib == LibraryManagerDialog.LibraryConfigState.State.Partial ? partial_configured_style :
                        null;
            }
            else
            {
                this.Visibility = Visibility.Hidden;
            }
            State = state;
        }

        public LibraryConfigStateIndicator()
        {
            InitializeComponent();
        }
    }
}
