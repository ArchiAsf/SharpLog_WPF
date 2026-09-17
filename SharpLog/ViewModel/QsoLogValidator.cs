using SharpLog.ToolsBox;
using System.Text.RegularExpressions;

namespace SharpLog
{
    /// <summary>
    /// 日志数据校验工具类（静态，无状态）。
    ///
    /// 职责：在写入数据库之前校验用户输入的合法性：
    ///   1. 呼号格式校验（仅允许字母、数字、斜杠）；
    ///   2. 频率格式与波段合法性校验。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【波段数据来源的变更说明】
    /// 本项目原先使用 Model\StaticValues.cs 中手工维护的波段表
    /// （ValidAmateurBands 字典），但该表与 ADIF 官方数据存在边界不一致，
    /// 例如 80m 旧表为 3.5-3.9 MHz、40m 旧表为 7.0-7.2 MHz，
    /// 而 ADIF 官方分别为 3.5-4.0 MHz、7.0-7.3 MHz。
    /// 这会导致"本项目校验通过的频率，导出到其他软件时被判为非法波段"。
    ///
    /// 现已改为直接采用 ADIF 官方 enumerations_band.json 的数据：
    ///   · 数据由 ToolsBox\AdifDataLoader 在程序启动时一次性加载到内存；
    ///   · 校验时直接查询内存数据，不读取磁盘文件，速度很快；
    ///   · 旧的硬编码波段表已从 StaticValues 与 RadioValues 中删除。
    /// ════════════════════════════════════════════════════════════════════════
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
        /// 校验日志必填项（呼号格式 + 频率合法性与波段归属）。
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

            #region 频率格式校验
            // 频率必须是有效数字
            if (!double.TryParse(freqKhz, out double freqKhzValue))
                return (false, "【频率】格式错误，请输入有效数字");
            #endregion

            #region 频率波段校验
            // 单位换算：kHz → MHz
            // 界面输入框的单位是 kHz（如输入 7050 表示 7050 kHz），
            // 而 ADIF 官方波段表以 MHz 为单位（如 40m 波段为 7.0 - 7.3 MHz），
            // 因此比对前必须除以 1000
            double freqMhz = freqKhzValue / 1000.0;

            // 查询 ADIF 官方波段表（内存数据），判断是否落在任一合法波段内
            bool inBand = AdifDataLoader.Bands.Any(b => b.IsInRange(freqMhz));
            if (!inBand)
            {
                return (false, $"频率 {freqKhzValue} kHz（{freqMhz:F4} MHz）不在合法业余无线电频段范围内");
            }
            #endregion

            return (true, string.Empty);
        }

        /// <summary>
        /// 根据 kHz 频率自动匹配所属波段名称（如输入 7050 返回 "40m"）。
        /// 无匹配时返回 null。
        ///
        /// 用途示例：录入日志时自动识别波段、按波段做统计等。
        /// </summary>
        /// <param name="freqKhz">频率（kHz）</param>
        /// <returns>波段名（如 "40m"、"20m"）；不在任何波段内时返回 null</returns>
        public static string? GetBandByKhz(double freqKhz)
        {
            // 单位换算：kHz → MHz（与校验方法保持一致）
            double freqMhz = freqKhz / 1000.0;

            // 遍历 ADIF 官方波段表（数据按频率从低到高排列），
            // 返回第一个包含该频率的波段名
            foreach (var band in AdifDataLoader.Bands)
            {
                if (band.IsInRange(freqMhz))
                {
                    return band.Band;
                }
            }

            return null;
        }
    }
}
