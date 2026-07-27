using System.Text;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SharpLog
{
    /// <summary>
    /// Interaction logic for MainLogWindow.xaml
    /// </summary>
    public partial class MainLogWindow : Window
    {
        public MainLogWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel.MainLogWindow_VM();
        }


  


        



    }
}