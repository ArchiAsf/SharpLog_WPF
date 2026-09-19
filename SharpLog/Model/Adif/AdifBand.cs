namespace SharpLog.Model
{
    /// <summary>
    /// 业余无线电波段模型（来源于 ADIF 官方 enumerations_band.json 的 Band 枚举）。
    ///
    /// 【用途】
    ///   · 频率合法性校验（判断用户输入的频率是否落在某个合法业余波段内）
    ///   · 由频率反查所属波段名称（如 7050kHz → "40m"）
    ///
    /// 【数据来源】
    ///   程序运行目录下：ADIFCode_Json\enumerations_band.json
    ///   由 AdifDataLoader 在程序启动时一次性加载并转换为本类的实例集合。
    ///
    /// 【单位说明】（重要）
    ///   本类的上下限频率单位均为【MHz】，与 ADIF 官方数据保持一致。
    ///   而本项目的界面输入框使用【kHz】，因此校验前必须先换算：
    ///       频率(MHz) = 频率(kHz) ÷ 1000
    ///   例如界面输入 7050（kHz），换算后为 7.05 MHz，落在 40m 波段内。
    ///
    /// 【与旧实现的区别】
    ///   项目原先使用 Model\StaticValues.cs 中手工维护的 ValidAmateurBands 字典，
    ///   该表与 ADIF 官方数据存在边界不一致的问题
    ///   （例如 80m 旧表为 3.5-3.9 MHz，官方为 3.5-4.0 MHz）。
    ///   现改为完全采用官方数据，已删除旧的硬编码波段表。
    /// </summary>
    public class AdifBand
    {
        /// <summary>
        /// 波段名称（ADIF 官方命名，如 "2190m"、"160m"、"20m"、"70cm"）。
        /// 该名称即为 ADIF 文件中 BAND 字段应写入的值。
        /// </summary>
        public string Band { get; init; } = string.Empty;

        /// <summary>
        /// 波段下限频率，单位 MHz（含该边界值）。
        /// </summary>
        public double LowerFreqMhz { get; init; }

        /// <summary>
        /// 波段上限频率，单位 MHz（含该边界值）。
        /// </summary>
        public double UpperFreqMhz { get; init; }

        /// <summary>
        /// 判断给定频率是否落在本波段范围内（闭区间，两端边界值都算在内）。
        /// </summary>
        /// <param name="freqMhz">待判断的频率，单位【MHz】</param>
        /// <returns>true = 在波段内；false = 不在波段内</returns>
        public bool IsInRange(double freqMhz)
        {
            return freqMhz >= LowerFreqMhz && freqMhz <= UpperFreqMhz;
        }

        /// <summary>
        /// 返回便于阅读的文本形式，如 "20m (14 - 14.35 MHz)"。
        /// 主要用于调试与日志输出。
        /// </summary>
        public override string ToString()
        {
            return $"{Band} ({LowerFreqMhz} - {UpperFreqMhz} MHz)";
        }
    }
}
