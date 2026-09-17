using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HandyControl.Data;
using hc = HandyControl.Controls;

namespace SharpLog.ToolsBox
{
    static class MessageTools
    {
        /// <summary>
        /// 在窗口右下角的 Growl 面板显示错误提示，2 秒后自动关闭。
        ///
        /// ════════════════════════════════════════════════════════════════════════
        /// 【必须写 IsCustom = true，否则错误提示永远不会自动消失】
        /// 副作用：设为自定义后 HandyControl 不再自动填充图标，
        /// 因此下面的 IconKey / IconBrushKey 必须显式给出（取值与它的默认值一致，
        /// 视觉效果不会发生变化）。
        /// ════════════════════════════════════════════════════════════════════════
        /// </summary>
        /// <param name="Value">提示文本</param>
        public static async Task ShowErrorMess_OnMainView(string Value)
        {
            hc.Growl.Error(new GrowlInfo
            {
                Message = Value,
                ShowDateTime = false,
                StaysOpen = false,          // 不常驻；配合 IsCustom=true 才会真正生效
                IsCustom = true,            // ★ 关键：跳过 HandyControl 对 Error 的 StaysOpen 强制赋值
                WaitTime = 2,               // 显示时长（秒），与成功提示保持一致
                Token = "WarningShowBox",   // 对应 MainLogWindow.xaml 中 GrowlPanel 的 Token
                IconKey = "ErrorGeometry",
                IconBrushKey = "DangerBrush"
            });
        }

        /// <summary>
        /// 在窗口右下角的 Growl 面板显示成功提示，2 秒后自动关闭。
        ///
        /// 说明：Success 类型不需要设置 IsCustom——
        /// HandyControl 的 InitGrowlInfo 只在 Error / Fatal 分支里强制 StaysOpen，
        /// 成功分支不会，因此这里保持默认即可正常倒计时关闭。
        /// （之所以保留 IconKey / IconBrushKey 的显式写法，是为了与错误提示的风格统一。）
        /// </summary>
        /// <param name="Value">提示文本</param>
        public static async Task ShowSuccessMess_OnMainView(string Value)
        {
            hc.Growl.Success(new GrowlInfo
            {
                Message = Value,
                ShowDateTime = false,
                StaysOpen = false,
                WaitTime = 2,
                Token = "WarningShowBox",
                IconKey = "SuccessGeometry",
                IconBrushKey = "SuccessBrush"
            });
        }
    }
}
