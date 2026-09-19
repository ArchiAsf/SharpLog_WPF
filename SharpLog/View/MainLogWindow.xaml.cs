using HandyControl.Controls;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
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
using System.Windows.Threading;
// HandyControl 里也有一个同名的 ScrollViewer，直接写会报「不明确的引用」，
// 这里明确别名指向 WPF 自带的那个（我们要操作的是 DataGrid 模板内部的标准 ScrollViewer）
using ScrollViewer = System.Windows.Controls.ScrollViewer;

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

            var vm = new ViewModel.MainLogWindow_VM();
            DataContext = vm;

            // 订阅「请求编辑」事件 ——
            // VM 那边不碰任何 Window，弹窗这件事由 View 层负责（见 OnEditRequested）
            vm.EditRequested += OnEditRequested;
        }

        /// <summary>
        /// 响应 ViewModel 的「请求编辑」事件：弹出编辑窗口。
        /// 双击表格整行、右键菜单「更改该记录」最终都会走到这里。
        ///
        /// 【为什么弹窗写在 View 而不是 VM】
        ///   项目规范禁止 ViewModel 持有并操作 UI 控件实例，而 Window 正是 UI 控件。
        ///   VM 只负责广播事件，怎么显示由这里决定。
        ///
        /// 【为什么用 ShowDialog 而不是 Show】
        ///   模态能挡住主界面，避免同一条记录被同时开成两个编辑窗；
        ///   同时 Owner 一旦指定，编辑窗会跟随主窗口最小化 / 关闭，并默认居中于它。
        /// </summary>
        private void OnEditRequested(object? sender, Model.QsoLog row)
        {
            // 为这条记录建一个编辑用的 ViewModel 并注入窗口。
            // ⚠ 转换工作（QSO_DATE+TIME_ON 合成 DateTime、MODE+SUBMODE 反查下拉项等）
            //    全部由 EditQso_VM 负责，这里只传原始实体。
            var editVm = new ViewModel.EditQso_VM(row);

            var editWindow = new View.EditQso
            {
                Owner = this,
                DataContext = editVm
            };

            editWindow.ShowDialog();

            // 编辑窗口一关就重新加载日志查看列表 ——
            // 不管是点「删除 / 取消 / 保存」哪个按钮，还是直接点右上角的 ×。
            // 否则删除或保存完成后，列表里显示的还是旧数据，看起来像根本没生效。
            //
            // ⚠ 必须用 ReloadLogView 而不是 LoadLogViewFirstPage：
            //   后者带着「首屏只加载一次」的哨兵，拿它当刷新用会被直接 return 掉。
            // ⚠ 代价：取消或直接关窗时也会白查一次库（一页 2000 条），可接受；
            //   若日后在意，可以改成「只有编辑窗口回传 DataChanged 为 true 才刷新」。
            if (DataContext is ViewModel.MainLogWindow_VM mainVm)
            {
                mainVm.ReloadLogView();
            }
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

        /// <summary>日志查看表格的当前滚动位置，用于集合 Reset 之后恢复。</summary>
        private double _logViewScrollOffset;

        /// <summary>
        /// 是否暂停记录滚动位置。
        /// 集合 Reset 后 DataGrid 会重建行容器，这段时间 offset 会被冲成 0；
        /// 若不暂停记录，用户真实的滚动位置就会被这个 0 覆盖掉。
        /// </summary>
        private bool _suppressScrollRecord;

        /// <summary>集合变更事件是否已挂上（Loaded 可能触发多次，避免重复订阅）。</summary>
        private bool _logViewRowsHooked;

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
                // 订阅集合变更。批量追加发的是一次 Reset 通知，会把滚动位置冲掉，
                // 需要在界面重建完成后恢复回来（详见 LogViewRows_CollectionChanged）
                if (!_logViewRowsHooked)
                {
                    _logViewRowsHooked = true;
                    vm.LogViewRows.CollectionChanged += LogViewRows_CollectionChanged;
                }

                vm.LoadLogViewFirstPage();
            }
        }

        /// <summary>
        /// 随时记下用户滚到了哪里，供 Reset 之后恢复。
        /// </summary>
        private void LogViewDataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_suppressScrollRecord) return;
            _logViewScrollOffset = e.VerticalOffset;
        }

        /// <summary>
        /// 集合变更处理：批量追加发的是 Reset 通知，DataGrid 收到后会重建全部
        /// 行容器，滚动位置被冲回顶部。
        /// 这里在界面重建完成之后把位置恢复回去 ——
        /// 否则用户点了「加载更多」，会从表格底部被弹回最上面，
        /// 得重新滚两千行才能接着往下看。
        /// </summary>
        private void LogViewRows_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // 只有 Reset 会重建行容器；单条增加/删除不影响滚动位置，不必处理
            if (e.Action != NotifyCollectionChangedAction.Reset) return;

            double targetOffset = _logViewScrollOffset;
            _suppressScrollRecord = true;

            // 用 Loaded 优先级排队：执行到这里时 DataGrid 的新行容器已经生成、
            // 布局也已完成，此时改滚动位置才不会被随后的重建覆盖掉
            Dispatcher.BeginInvoke(new Action(() =>
            {
                ScrollViewer? viewer = FindVisualChild<ScrollViewer>(LogViewDataGrid);
                viewer?.ScrollToVerticalOffset(targetOffset);
                _suppressScrollRecord = false;
            }), DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 在视觉树里深度优先查找指定类型的子元素。
        /// DataGrid 的滚动条藏在它模板内部的 ScrollViewer 里，
        /// 只有拿到那个 ScrollViewer 才能直接设置滚动位置。
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(root);

            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(root, i);

                if (child is T matched) return matched;

                T? found = FindVisualChild<T>(child);
                if (found != null) return found;
            }

            return null;
        }
    }
}






