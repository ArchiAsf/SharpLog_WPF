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
    /// 日志数据校验工具类（静态，无状态）。
    ///
    /// 职责：在写入数据库之前校验用户输入的合法性：
    /// 1. 呼号格式校验（仅允许字母、数字、斜杠）；
    /// 2. 频率格式与波段合法性校验（kHz 输入 → MHz 对照业余频段表）。
    ///
    /// 注意：本类命名空间为 SharpLog（非 ViewModel），
    /// 为独立静态工具类，不依赖任何 UI 与数据库。
    /// </summary>
    public static class QsoLogValidator
    {
        /// <summary>
        /// 呼号正则：仅允许大小写字母、数字、斜杠（/）。
        /// 例：BG2GPT、JA1ABC/MM、BY1WXD/QRP 均合法。
        /// 编译一次提升性能（RegexOptions.Compiled）。
        /// </summary>
        private static readonly Regex _callsignRegex = new Regex(@"^[A-Za-z0-9/]+$", RegexOptions.Compiled);

        /// <summary>
        /// 校验日志必填项（呼号格式 + 频率合法性）。
        /// </summary>
        /// <param name="callSign">对方呼号</param>
        /// <param name="freqKhz">频率（千赫兹文本，如 "7050"）</param>
        /// <returns>(是否合法, 错误信息)；合法时错误信息为空串</returns>
        public static (bool IsValid, string ErrorMessage) ValidateLogInput(string callSign, string freqKhz)
        {
            #region 呼号格式校验
            if (!_callsignRegex.IsMatch(callSign))
                return (false, "【呼号】包含非法字符，仅允许字母、数字、斜杠(/)");
            #endregion

            #region 频率校验
            // 频率必须是有效数字
            if (!double.TryParse(freqKhz, out double freqKhzValue))
                return (false, "【频率】格式错误，请输入有效数字");

            // 频率单位换算：kHz → MHz（业余频段表以 MHz 为单位）
            double freqMhz = freqKhzValue / 1000.0;

            // 判断是否落在任一合法业余频段内（StaticValues.ValidAmateurBands）
            bool inBand = ValidAmateurBands.Values.Any(r => r.IsInRange(freqMhz));
            if (!inBand)
            {
                return (false, $"频率 {freqKhzValue} kHz（{freqMhz:F4} MHz）不在合法业余无线电频段范围内");
            }
            #endregion

            return (true, string.Empty);
        }

        /// <summary>
        /// 根据 kHz 频率自动匹配波段名称（如 7050 → "40m"）。
        /// 无匹配时返回 null。
        /// </summary>
        /// <param name="freqKhz">频率（kHz）</param>
        /// <returns>波段名（如 "40m"、"20m"），无匹配返回 null</returns>
        public static string? GetBandByKhz(double freqKhz)
        {
            // kHz → MHz
            double mhz = freqKhz / 1000.0;
            // 遍历波段字典，返回第一个包含该频率的波段名
            foreach (var band in ValidAmateurBands)
            {
                if (band.Value.IsInRange(mhz))
                    return band.Key;
            }
            return null;
        }
    }
}
