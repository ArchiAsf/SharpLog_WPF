using System;
using System.Windows;

namespace SharpLog.ToolsBox
{
    /// <summary>
    /// 界面语言切换工具（中 / 英）。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【原理】
    ///   界面上的文字全部写成 {DynamicResource Text_Xxx}，
    ///   而 Text_Xxx 这个键由两个资源字典分别提供：
    ///       Languages\LangCN.xaml  —— 中文
    ///       Languages\LangEN.xaml  —— 英文
    ///   切换时只要把 Application 的 MergedDictionaries 里那个语言字典
    ///   换成另一个，WPF 会自动把所有 DynamicResource 引用刷新一遍。
    ///
    /// 【⚠ 为什么必须用 DynamicResource 不能用 StaticResource】
    ///   StaticResource 在【界面加载时】就把值取走了，之后再换字典也不会变；
    ///   只有 DynamicResource 会持续监听资源变化。
    ///   ⇒ 以后新增界面文字时，一律写 DynamicResource。
    ///
    /// 【为什么要解耦成静态工具类】
    ///   语言状态是【全局】的，不属于某一个 ViewModel。
    ///   放这里之后，任何页面（主窗口、EditQso）都能直接调用，
    ///   换语言的实现改了也只动这一个文件。
    /// ════════════════════════════════════════════════════════════════════════
    /// </summary>
    public static class LanguageTools
    {
        /// <summary>语言字典所在位置（pack URI）。</summary>
        private const string DictionaryFolder = "pack://application:,,,/Languages/";

        /// <summary>
        /// 当前是否为英文界面。
        /// ⚠ 只作显示用 —— 真正的界面刷新由资源字典替换触发，不靠这个字段。
        /// </summary>
        public static bool IsEnglish { get; private set; }

        /// <summary>
        /// 切换到指定语言。
        /// </summary>
        /// <param name="english">true = 英文；false = 中文</param>
        public static void SwitchTo(bool english)
        {
            if (Application.Current == null)
            {
                return;
            }

            string fileName = english ? "LangEN.xaml" : "LangCN.xaml";
            var newDictionary = new ResourceDictionary
            {
                Source = new Uri(DictionaryFolder + fileName, UriKind.Absolute)
            };

            var merged = Application.Current.Resources.MergedDictionaries;

            // 先移除旧的语言字典。
            // ⚠ 从后往前删 —— 边遍历边删会让下标错位。
            // ⚠ 只按文件名含 "Lang" 判断，这样不管 App.xaml 里写的是
            //   相对路径还是 pack URI 都能认出来。
            for (int i = merged.Count - 1; i >= 0; i--)
            {
                string? source = merged[i].Source?.OriginalString;
                if (source != null && source.Contains("Lang", StringComparison.OrdinalIgnoreCase))
                {
                    merged.RemoveAt(i);
                }
            }

            // 放到最后 —— 保证它的键能覆盖前面字典里的同名键
            merged.Add(newDictionary);

            IsEnglish = english;
        }

        /// <summary>在中英文之间来回切。</summary>
        public static void Toggle()
        {
            SwitchTo(!IsEnglish);
        }
    }
}
