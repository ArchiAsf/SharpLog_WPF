using CommunityToolkit.Mvvm.Input;
using SharpLog.ToolsBox;

// ══════════════════════════════════════════════════════════════════════════
//  MainLogWindow_VM 的「界面语言」部分（partial）
//  ──────────────────────────────────────────────────────────────────────────
//  只放一个命令：中英文切换按钮。
//
//  【为什么逻辑这么薄】
//    真正干活的是 ToolsBox\LanguageTools —— 它替换 App 的语言资源字典，
//    WPF 之后会自动刷新所有 {DynamicResource} 引用。
//    ViewModel 这边不需要维护"当前是什么语言"的副本，
//    也不需要通过属性通知去推界面，所以这里只留一个转调。
// ══════════════════════════════════════════════════════════════════════════

namespace SharpLog.ViewModel
{
    public partial class MainLogWindow_VM
    {
        /// <summary>
        /// 中英文切换（日志编辑页顶部那颗按钮）。
        ///
        /// ⚠ 按钮自己显示的文字也是资源（Text_LanguageToggle），
        ///   切换字典后它会一起变成另一种语言 —— 不需要在这里手工改。
        /// </summary>
        [RelayCommand]
        private void ToggleLanguage()
        {
            LanguageTools.Toggle();
        }
    }
}
