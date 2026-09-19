using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace SharpLog.ToolsBox
{
    /// <summary>
    /// 支持「批量添加」的 ObservableCollection。
    ///
    /// ════════════════════════════════════════════════════════════════════
    /// 【为什么需要这么一个类】
    ///   ObservableCollection 每调用一次 Add 就触发一次 CollectionChanged 通知。
    ///   日志查看页的表格一次要加载 2000 条记录，若逐条 Add，
    ///   绑定它的 DataGrid 就会收到 2000 次通知、逐条处理 2000 遍。
    ///   AddRange 把整批数据先在内部放好，最后只发出【一次】通知。
    ///
    /// 【⚠ 为什么最后发的是 Reset 而不是「带多项的 Add」】
    ///   WPF 的 CollectionView（DataGrid、ListBox 等控件绑定集合时，
    ///   框架会在背后自动创建一个）【不支持范围操作】。
    ///   若发出「一次添加多项」的 Add 通知：
    ///       NotifyCollectionChangedEventArgs(Add, 多项列表, 起始下标)
    ///   运行时会直接抛：
    ///       System.NotSupportedException: 不支持范围操作。
    ///   所以这里只能退而求其次，发一次 Reset 通知，
    ///   让界面把整个列表当作"内容已全部变化"来重建。
    ///
    /// 【Reset 的代价】
    ///   Reset 会让 DataGrid 重建全部分行容器，滚动位置会被冲掉。
    ///   本项目在 View 层（MainLogWindow.xaml.cs）做了滚动位置保存与恢复，
    ///   用户点「加载更多」之后不会被弹回表格顶部。
    ///
    /// 【与「整体替换 List」做法的区别】
    ///   把绑定源换成新 List 也能一次性刷新，但那样连集合对象都换了，
    ///   对外订阅了 CollectionChanged 的地方会全部失效。
    ///   就地追加 + Reset 则保持集合实例不变。
    /// ════════════════════════════════════════════════════════════════════
    /// </summary>
    /// <typeparam name="T">集合元素类型</typeparam>
    public class RangeObservableCollection<T> : ObservableCollection<T>
    {
        /// <summary>
        /// 批量添加一批元素，只触发一次集合变更通知。
        /// </summary>
        /// <param name="items">要追加的元素；为 null 或空集合时什么都不做</param>
        public void AddRange(IEnumerable<T>? items)
        {
            if (items == null) return;

            // 先物化成 List：避免同一序列被枚举两次，顺便拿到条数
            List<T> incoming = items as List<T> ?? items.ToList();
            if (incoming.Count == 0) return;

            // ⚠ 往基类的底层集合 Items 里塞，而不是调用本类的 Add 方法 ——
            //   绕开「每加一条就发一次通知」的路径，中途一次通知都不发。
            foreach (T item in incoming)
            {
                Items.Add(item);
            }

            // 补发属性变更通知：条数变了、索引器内容也变了
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

            // 最后只发一次 Reset。
            // ⚠ 不能用带多项的 Add（上面解释过，WPF 会抛「不支持范围操作」）
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Reset));
        }
    }
}
