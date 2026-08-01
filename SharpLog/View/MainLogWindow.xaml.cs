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

            // 尝试获取CalendarWithClock的默认样式
            var style = Application.Current.TryFindResource(typeof(CalendarWithClock));
            Debug.WriteLine($"CalendarWithClock样式是否存在：{style != null}");

            // 尝试获取基础控件样式
            var btnStyle = Application.Current.TryFindResource(typeof(Button));
            Debug.WriteLine($"Button样式是否存在：{btnStyle != null}");

            // 列出所有合并资源字典
            Debug.WriteLine($"合并资源字典数量：{Application.Current.Resources.MergedDictionaries.Count}");
            foreach (var dict in Application.Current.Resources.MergedDictionaries)
            {
                Debug.WriteLine($"  - {dict.Source}");
            }


            //try
            //{
            //    var uri = new Uri("pack://application:,,,/HandyControl;component/Themes/SkinDark.xaml");
            //    var dict = new ResourceDictionary { Source = uri };
            //    Debug.WriteLine("✅ HC资源字典加载成功");
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine($"❌ 资源加载失败：{ex.Message}");
            //}
        }

    }
}