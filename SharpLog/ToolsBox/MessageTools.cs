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
        ///
        /// 这是 HandyControl 3.5.1 的一个内置行为（反编译 Growl.InitGrowlInfo 可见）：
        ///
        ///     case InfoType.Error:
        ///         if (!growlInfo.IsCustom)          // ← 未设置 IsCustom 时进入
        ///         {
        ///             growlInfo.IconKey      = "ErrorGeometry";
        ///             growlInfo.IconBrushKey = "DangerBrush";
        ///             growlInfo.StaysOpen    = true;   // ★ 覆盖掉调用方设置的值
        ///         }
        ///         break;
        ///
        /// 而 Growl.Update() 的结尾是：
        ///
        ///     if (!_staysOpen) StartTimer();        // 常驻时不启动关闭计时器
        ///
        /// 两处合起来的结果：Error 类型的消息被强制设为 StaysOpen = true，
        /// 关闭计时器压根不会启动，提示框就一直挂在右下角。
        /// 调用方写 StaysOpen = false 是无效的——它会被上面那段代码覆盖。
        ///
        /// 对比：InfoType.Success / Info / Warning 三个分支中没有 set_StaysOpen 这一句，
        /// 所以成功提示能正常倒计时关闭，只有 Error 和 Fatal 会常驻。
        ///
        /// 【解决办法】
        /// 设置 IsCustom = true，跳过整个 if (!IsCustom) 代码块，
        /// StaysOpen 便保持为 false，关闭计时器正常启动。
        ///
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
