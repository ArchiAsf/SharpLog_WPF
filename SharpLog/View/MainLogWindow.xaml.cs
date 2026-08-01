using HandyControl.Controls;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
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
    public partial class MainLogWindow : System.Windows.Window
    {
        public MainLogWindow()
        {
            InitializeComponent();
            DataContext = new ViewModel.MainLogWindow_VM();

        }
        private void ClosingChange(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // 窗口关闭前触发，在这里停定时器、保存数据等
            if (DataContext is ViewModel.MainLogWindow_VM vm)
            {
                vm._timer.Stop();
                // 还可以做其他清理工作
            }
        }
    }
}






