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

        /// <summary>
        /// 日志查看表格首次显示时，触发首屏数据加载。
        ///
        /// 【为什么挂在这里而不是在 VM 的构造函数里】
        ///   TabItem 的内容是「首次选中才实例化」的，所以这个 Loaded 事件
        ///   天然就是「用户第一次切到日志查看页」的时机。
        ///   若放在窗口构造时加载，程序一启动就要白查 2000 条，
        ///   拖慢启动速度，而用户可能压根不点这个页。
        ///
        /// 【为什么由 View 调 VM 而不算违规】
        ///   项目规范禁止的是「ViewModel 反向持有并操作 UI 控件」，
        ///   这里是 View 单向调用 VM 的公开方法，属于正常方向。
        /// </summary>
        private void LogViewDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModel.MainLogWindow_VM vm)
            {
                vm.LoadLogViewFirstPage();
            }
        }
    }
}






