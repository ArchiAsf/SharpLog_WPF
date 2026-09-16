using HandyControl.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using hc = HandyControl.Controls;
using HandyControl.Data;

namespace SharpLog.ToolsBox
{
    static class MessageTools
    {
        /// <summary>
        /// 显示错误消息提示，2 秒后自动隐藏。
        /// </summary>
        /// <param name="Value">提示文本</param>
        public static async Task ShowErrorMess_OnMainView(string Value)
        {
            //MessageShowStr = Value;
            //MessShowISVisibility = Visibility.Visible;
            //await Task.Delay(2000);
            //MessShowISVisibility = Visibility.Hidden;
            //MessageShowStr = string.Empty;

            hc.Growl.Error(new GrowlInfo
            {
                Message = Value,
                ShowDateTime = false,
                StaysOpen = false,
                WaitTime = 2,
                Token = "WarningShowBox",
                IconKey = "ErrorGeometry",
                IconBrushKey = "DangerBrush"


            });
        }

        /// <summary>
        /// 显示成功消息提示，2 秒后自动隐藏。
        /// </summary>
        /// <param name="Value">提示文本</param>
        public static async Task ShowSuccessMess_OnMainView(string Value)
        {
            //MessageShowStr = Value;
            //MessShowISVisibility = Visibility.Visible;
            //await Task.Delay(2000);
            //MessShowISVisibility = Visibility.Hidden;
            //MessageShowStr = string.Empty;

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
