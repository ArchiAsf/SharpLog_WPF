using System;
using System.Collections.Generic;
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

using SharpLog.ViewModel;

namespace SharpLog.View
{
    /// <summary>
    /// EditQso.xaml 的交互逻辑
    /// </summary>
    public partial class EditQso : Window
    {
        public EditQso()
        {
            InitializeComponent();

            // 订阅 VM 的「请求关闭」事件。
            // VM 不能直接调 Window.Close()（项目规范：VM 不操作 UI 控件），
            // 所以删除成功 / 取消确认 / 保存成功后由 VM 抛事件，这里负责关窗。
            // ⚠ 用 DataContextChanged 而不是在构造函数里直接订阅：
            //   DataContext 是外部（MainLogWindow）在 new 完之后才赋值的，
            //   构造函数执行时它还是 null。
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is EditQso_VM oldVm)
            {
                oldVm.CloseRequested -= OnCloseRequested;
            }

            if (e.NewValue is EditQso_VM newVm)
            {
                newVm.CloseRequested += OnCloseRequested;
            }
        }

        /// <summary>
        /// VM 请求关闭窗口：把它的 DataChanged 作为 DialogResult 返回给主窗口，
        /// 主窗口据此决定要不要刷新列表。
        ///
        /// ⚠ 设置 DialogResult 会自动关闭窗口（仅限 ShowDialog 打开时）。
        ///   取消 / 关闭时 VM 不发这个事件，窗口由用户手动关；
        ///   那种情况下主窗口拿到的 DialogResult 是 null，同样不会刷新。
        /// </summary>
        private void OnCloseRequested(object? sender, EventArgs e)
        {
            DialogResult = ((EditQso_VM?)DataContext)?.DataChanged ?? false;
        }
    }
}
