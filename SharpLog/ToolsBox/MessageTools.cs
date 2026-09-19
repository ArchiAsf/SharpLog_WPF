using System.Threading.Tasks;
using HandyControl.Data;
using hc = HandyControl.Controls;

namespace SharpLog.ToolsBox
{
    /// <summary>
    /// 消息提示要显示在哪一页。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【为什么需要这个枚举】
    ///   界面上三个页面（日志编辑 / 日志查看 / 设置）各自有一个独立的 Growl 面板，
    ///   消息该出现在哪一页由【业务决定】，而不是由"当前正显示哪一页"决定：
    ///     · 添加日志成功  → 应出现在日志编辑页
    ///     · 删除记录成功  → 应出现在日志查看页
    ///     · 保存设置成功  → 应出现在设置页
    ///   所以每个调用点都必须明确写出目标页，不能依赖当前选中的 Tab。
    ///   （否则用户在设置页点保存，消息却跑到日志编辑页去了，那才是真的怪。）
    /// ════════════════════════════════════════════════════════════════════════
    /// </summary>
    public enum MessagePage
    {
        /// <summary>日志编辑页</summary>
        LogEdit,

        /// <summary>日志查看页</summary>
        LogView,

        /// <summary>设置页</summary>
        Settings
    }

    /// <summary>
    /// 界面消息提示工具（基于 HandyControl 的 Growl）。
    /// </summary>
    static class MessageTools
    {
        /// <summary>
        /// 把页面枚举翻译成对应 Growl 面板的 Token。
        ///
        /// ⚠ 这三个字符串必须与 MainLogWindow.xaml 里
        ///   hc:Growl.Token="..." 的值【一字不差】，
        ///   否则消息会找不到目标面板（表现为消息不显示或跑到别处）。
        /// </summary>
        private static string GetToken(MessagePage page) => page switch
        {
            MessagePage.LogView => "LogViewGrowl",
            MessagePage.Settings => "SettingGrowl",
            _ => "LogEditGrowl"
        };

        /// <summary>
        /// 在指定页面的右下角 Growl 面板显示错误提示，2 秒后自动关闭。
        ///
        /// ════════════════════════════════════════════════════════════════════════
        /// 【必须写 IsCustom = true，否则错误提示永远不会自动消失】
        /// 副作用：设为自定义后 HandyControl 不再自动填充图标，
        /// 因此下面的 IconKey / IconBrushKey 必须显式给出（取值与它的默认值一致，
        /// 视觉效果不会发生变化）。
        /// ════════════════════════════════════════════════════════════════════════
        /// </summary>
        /// <param name="page">显示在哪一页（见 MessagePage 的说明）</param>
        /// <param name="Value">提示文本</param>
        public static async Task ShowErrorMess(MessagePage page, string Value)
        {
            hc.Growl.Error(new GrowlInfo
            {
                Message = Value,
                ShowDateTime = false,
                StaysOpen = false,          // 不常驻；配合 IsCustom=true 才会真正生效
                IsCustom = true,            // ★ 关键：跳过 HandyControl 对 Error 的 StaysOpen 强制赋值
                WaitTime = 2,               // 显示时长（秒），与成功提示保持一致
                Token = GetToken(page),     // 决定去哪个页面的 Growl 面板
                IconKey = "ErrorGeometry",
                IconBrushKey = "DangerBrush"
            });
        }

        /// <summary>
        /// 在指定页面的右下角 Growl 面板显示成功提示，2 秒后自动关闭。
        ///
        /// 说明：Success 类型不需要设置 IsCustom——
        /// HandyControl 的 InitGrowlInfo 只在 Error / Fatal 分支里强制 StaysOpen，
        /// 成功分支不会，因此这里保持默认即可正常倒计时关闭。
        /// （之所以保留 IconKey / IconBrushKey 的显式写法，是为了与错误提示的风格统一。）
        /// </summary>
        /// <param name="page">显示在哪一页（见 MessagePage 的说明）</param>
        /// <param name="Value">提示文本</param>
        public static async Task ShowSuccessMess(MessagePage page, string Value)
        {
            hc.Growl.Success(new GrowlInfo
            {
                Message = Value,
                ShowDateTime = false,
                StaysOpen = false,
                WaitTime = 2,
                Token = GetToken(page),
                IconKey = "SuccessGeometry",
                IconBrushKey = "SuccessBrush"
            });
        }
    }
}
