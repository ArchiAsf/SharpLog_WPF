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
        private const string TimeFormat = "yyyy/MM/dd-HH:mm:ss";

        /// <summary>
        /// 校验日志必填项
        /// </summary>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="callSign">对方呼号</param>
        /// <param name="commMode">通联模式</param>
        /// <param name="freqKhz">频率（千赫兹）</param>
        /// <returns>(是否合法, 错误信息)</returns>
        public static (bool IsValid, string ErrorMessage) ValidateLogInput(
            string startTime,
            string endTime,
            string callSign,
            string commMode,
            string freqKhz)
        {
            #region 非空校验
            if (string.IsNullOrWhiteSpace(startTime))
                return (false, "【开始时间】不能为空");
            if (string.IsNullOrWhiteSpace(endTime))
                return (false, "【结束时间】不能为空");
            if (string.IsNullOrWhiteSpace(callSign))
                return (false, "【呼号】不能为空");
            if (string.IsNullOrWhiteSpace(commMode))
                return (false, "【模式】必须选择");
            if (string.IsNullOrWhiteSpace(freqKhz))
                return (false, "【频率】不能为空");
            #endregion

            #region 呼号格式校验
            if (!_callsignRegex.IsMatch(callSign))
                return (false, "【呼号】包含非法字符，仅允许字母、数字、斜杠(/)");
            #endregion

            #region 时间格式 + 先后顺序校验
            if (!DateTime.TryParseExact(startTime, TimeFormat, null, System.Globalization.DateTimeStyles.None, out DateTime startDt))
            {
                return (false, $"【开始时间】格式错误！要求格式：{TimeFormat} 示例：2026/07/09-19:45:36");
            }

            if (!DateTime.TryParseExact(endTime, TimeFormat, null, System.Globalization.DateTimeStyles.None, out DateTime endDt))
            {
                return (false, $"【结束时间】格式错误！要求格式：{TimeFormat} 示例：2026/07/09-19:45:36");
            }

            // 核心新增：结束时间不能早于开始时间
            if (endDt < startDt)
            {
                return (false, "【结束时间】不能早于【开始时间】！");
            }
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





