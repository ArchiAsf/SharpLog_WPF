namespace SharpLog.Model
{
    /// <summary>
    /// 日出日落计算的状态。
    ///
    /// 【为什么需要它】
    ///   日出日落并不是任何情况下都算得出来的：
    ///     · 高纬度地区夏季会出现「极昼」——太阳整日不落，没有日落时刻；
    ///     · 冬季会出现「极夜」——太阳整日不升，没有日出时刻；
    ///     · 用户尚未填写经纬度时，根本无从计算。
    ///   调用方需要区分这几种情况，才能在界面上给出正确的提示
    ///   （例如显示「极昼」而不是显示一个假的时刻或 --:--）。
    ///
    /// 【放在 Model 而非 ToolsBox 的原因】
    ///   本枚举会被 StaticValues（Model 层）引用；
    ///   若定义在 ToolsBox 层，就会形成 Model → ToolsBox 的反向依赖。
    /// </summary>
    public enum SunTimeStatus
    {
        /// <summary>正常：当日有明确的日出与日落时刻。</summary>
        Normal,

        /// <summary>极昼：太阳整日不落，当日没有日落时刻（多见于高纬度地区夏季）。</summary>
        PolarDay,

        /// <summary>极夜：太阳整日不升，当日没有日出时刻（多见于高纬度地区冬季）。</summary>
        PolarNight,

        /// <summary>无法计算：经纬度未填写、格式非法或超出有效范围。</summary>
        Invalid
    }
}
