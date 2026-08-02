using SharpLog.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Text.RegularExpressions;
using static SharpLog.Model.StaticValues;


namespace SharpLog
{

    /// <summary>
    /// 日志数据校验工具类
    /// </summary>
    public static class QsoLogValidator
    {
        // 呼号正则：仅允许大小写字母、数字、斜杠 /
        private static readonly Regex _callsignRegex = new Regex(@"^[A-Za-z0-9/]+$", RegexOptions.Compiled);

        /// <summary>
        /// 校验日志必填项
        /// </summary>
        /// <param name="callSign">对方呼号</param>
        /// <param name="freqKhz">频率（千赫兹）</param>
        /// <returns>(是否合法, 错误信息)</returns>
        public static (bool IsValid, string ErrorMessage) ValidateLogInput(string callSign, string freqKhz)
        {

            #region 呼号格式校验
            if (!_callsignRegex.IsMatch(callSign))
                return (false, "【呼号】包含非法字符，仅允许字母、数字、斜杠(/)");
            #endregion


            #region 频率校验
            if (!double.TryParse(freqKhz, out double freqKhzValue))
                return (false, "【频率】格式错误，请输入有效数字");

            double freqMhz = freqKhzValue / 1000.0;
            bool inBand = ValidAmateurBands.Values.Any(r => r.IsInRange(freqMhz));
            if (!inBand)
            {
                return (false, $"频率 {freqKhzValue} kHz（{freqMhz:F4} MHz）不在合法业余无线电频段范围内");
            }
            #endregion

            return (true, string.Empty);
        }

        /// <summary>
        /// 根据kHz频率，自动匹配波段名称，无匹配返回null
        /// </summary>
        public static string? GetBandByKhz(double freqKhz)
        {
            double mhz = freqKhz / 1000.0;
            foreach (var band in ValidAmateurBands)
            {
                if (band.Value.IsInRange(mhz))
                    return band.Key;
            }
            return null;
        }
    }
}





