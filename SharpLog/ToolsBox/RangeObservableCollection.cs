using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;

namespace SharpLog.ToolsBox
{
    /// <summary>
    /// 支持「批量添加」的ObservableCollection。
    ///
    /// ════════════════════════════════════════════════════════════════════
    /// 【为什么需要这么一个类】
    ///   ObservableCollection 每调用一次 Add 就触发一次 CollectionChanged 通知。
    ///   日志查看页的表格一次要加载 2000 条记录，若逐条 Add，
    ///   绑定它的 DataGrid 就会收到 2000 次通知、刷新 2000 遍，
    ///   界面会卡住好几秒甚至假死。
    ///
    ///   AddRange 把整批数据先在内部放好，最后只发出【一次】通知，
    ///   界面只刷新一遍，加载 2000 条几乎感觉不到停顿。
    ///
    /// 【与「整体替换 List」做法的区别】
    ///   把绑定源整个换成新 List 也能一次性刷新，
    ///   但那会让 DataGrid 重建全部分行容器，滚动位置与选中项都会丢失——
    ///   用户点了「加载更多」之后会被弹回表格顶部，体验很差。
    ///   AddRange 是"追加"，滚动位置与选中项都能保持。
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

            // 先物化成 List：
            // 1. 避免同一序列被枚举两次（下面既要遍历、又要传给通知参数）
            // 2. 顺便拿到本次要追加的条数
            List<T> incoming = items as List<T> ?? items.ToList();
            if (incoming.Count == 0) return;

            // 记下追加的起始下标，通知里要用
            int startIndex = Items.Count;

            // ⚠ 关键：这里是往基类的底层集合 Items 里塞，
            //   而不是调用本类的 Add 方法 —— 绕开逐条触发通知的路径。
            foreach (T item in incoming)
            {
                Items.Add(item);
            }

            // 补发属性变更通知：条数变了、索引器内容也变了
            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

            // 最后只发一次集合变更通知，把整批数据连同起始下标一起告诉界面
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(
                NotifyCollectionChangedAction.Add,
                (System.Collections.IList)incoming,
                startIndex));
        }
    }
}
