using SharpLog.Model;
using System;
using System.Globalization;

namespace SharpLog.ToolsBox
{
    /// <summary>
    /// 日出日落时刻计算工具类（静态，无状态）。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【计算的是"民用晨昏"时刻】
    ///   太阳高度角的不同，对应三种不同的"日出日落"定义：
    ///     · 民用晨昏（Civil）   太阳中心位于地平线下 6°  ← 本类采用
    ///     · 航海晨昏（Nautical）太阳中心位于地平线下 12°
    ///     · 天文晨昏（Astro）   太阳中心位于地平线下 18°
    ///   民用晨昏是日常说的"天亮/天黑"，此时地面景物尚可辨认，
    ///   也是业余无线电判断 160m/80m 波段传播窗口最常用的依据。
    ///
    /// 【算法来源】
    ///   采用标准的 Sunrise Equation（太阳正午 + 时角法）：
    ///     1. 由日期求出太阳的平近点角、黄经，进而得到【太阳赤纬】和【太阳正午】；
    ///     2. 由纬度、赤纬和太阳高度角求出【时角】ω₀；
    ///     3. 日出 = 太阳正午 − ω₀，日落 = 太阳正午 + ω₀。
    ///   该算法在南北纬 72° 以内误差约 1~2 分钟，对业余无线电足够。
    ///
    /// 【极昼 / 极夜的处理】
    ///   时角公式为 cos ω₀ = [sin(alt) − sinφ·sinδ] / [cosφ·cosδ]。
    ///   当该式算出的值超出 [−1, 1] 时，反余弦无解，说明太阳当天
    ///   要么不落（极昼）、要么不升（极夜），此时返回对应状态而非硬凑一个时刻。
    /// ════════════════════════════════════════════════════════════════════════
    /// </summary>
    public static class SunTimeCalculator
    {
        /// <summary>民用晨昏对应的太阳高度角（度）：地平线下 6 度。</summary>
        private const double CivilTwilightAltitude = -6.0;

        /// <summary>黄赤交角（度）——地球自转轴相对公转轨道面的倾角。</summary>
        private const double EarthAxialTilt = 23.44;

        /// <summary>Unix 纪元（1970-01-01 00:00 UTC），用于儒略日换算。</summary>
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// 计算结果。
        /// </summary>
        /// <param name="Status">计算状态（正常 / 极昼 / 极夜 / 无法计算）</param>
        /// <param name="Sunrise">民用日出时刻（本地时间）；非 Normal 状态时为 null</param>
        /// <param name="Sunset">民用日落时刻（本地时间）；非 Normal 状态时为 null</param>
        public readonly record struct SunTimeResult(
            SunTimeStatus Status,
            DateTime? Sunrise,
            DateTime? Sunset);

        /// <summary>
        /// 计算指定日期、指定经纬度的民用日出日落时刻。
        /// </summary>
        /// <param name="date">要计算的日期（本地日期，只有日期部分有效）</param>
        /// <param name="latitude">纬度（十进制度，北纬为正、南纬为负，范围 −90 ~ 90）</param>
        /// <param name="longitude">经度（十进制度，东经为正、西经为负，范围 −180 ~ 180）</param>
        /// <param name="utcOffset">结果采用的时区偏移（如中国为 +08:00）</param>
        /// <returns>计算结果；经纬度非法时返回 Invalid 状态</returns>
        public static SunTimeResult CalculateCivilTwilight(
            DateTime date, double latitude, double longitude, TimeSpan utcOffset)
        {
            #region 参数校验

            if (double.IsNaN(latitude) || double.IsNaN(longitude) ||
                latitude < -90.0 || latitude > 90.0 ||
                longitude < -180.0 || longitude > 180.0)
            {
                return new SunTimeResult(SunTimeStatus.Invalid, null, null);
            }

            #endregion

            #region 第 1 步：求出该日期对应的「天数 n」

            // n 表示"自 J2000 历元（2000-01-01 12:00 UT）起经过的天数"。
            //
            // ⚠ 这里必须用【本地当天的正午】换算成 UTC 时刻作为基准，
            //   不能用本地 00:00。原因：东八区的本地 00:00 其实是前一天的 UTC 16:00，
            //   直接拿它算会把结果整整差出一天。
            //   取正午则恰好落在当天日出与日落之间，不会跨错日期。
            DateTime localNoonAsUtc = date.Date.AddHours(12) - utcOffset;
            double julianDayNoon = ToJulianDay(localNoonAsUtc);

            // +0.0008 是常见的历元小修正；取整得到"整数天"
            double n = Math.Round(julianDayNoon - 2451545.0 + 0.0008);

            #endregion

            #region 第 2 步：太阳的平近点角与黄经

            // J* = 太阳正午的近似儒略日（相对 J2000），经度东正西负
            double jStar = n - longitude / 360.0;

            // 平近点角：地球绕日公转的"平均角度"，每年走完 360°
            double meanAnomaly = Normalize360(357.5291 + 0.98560028 * jStar);
            double meanAnomalyRad = ToRadians(meanAnomaly);

            // 中心差：真实位置与平均位置的偏差（源于轨道是椭圆）
            double centerDiff = 1.9148 * Math.Sin(meanAnomalyRad)
                              + 0.0200 * Math.Sin(2.0 * meanAnomalyRad)
                              + 0.0003 * Math.Sin(3.0 * meanAnomalyRad);

            // 太阳黄经（102.9372 是近日点黄经）
            double eclipticLongitude = Normalize360(meanAnomaly + centerDiff + 180.0 + 102.9372);
            double eclipticLongitudeRad = ToRadians(eclipticLongitude);

            #endregion

            #region 第 3 步：太阳赤纬与太阳正午

            // 太阳赤纬 δ：太阳直射点所在的纬度，直接决定当天的昼长
            double declination = Math.Asin(Math.Sin(ToRadians(EarthAxialTilt)) * Math.Sin(eclipticLongitudeRad));

            // 太阳正午（太阳经过当地子午线的时刻），儒略日
            // 后两项是时差（Equation of Time）的近似，使日晷时间与钟表时间对齐
            double julianDayTransit = 2451545.0 + jStar
                                    + 0.0053 * Math.Sin(meanAnomalyRad)
                                    - 0.0069 * Math.Sin(2.0 * eclipticLongitudeRad);

            #endregion

            #region 第 4 步：求时角（并判断极昼 / 极夜）

            double latitudeRad = ToRadians(latitude);

            // cos ω₀ = [sin(alt) − sinφ·sinδ] / [cosφ·cosδ]
            double cosHourAngle = (Math.Sin(ToRadians(CivilTwilightAltitude))
                                   - Math.Sin(latitudeRad) * Math.Sin(declination))
                                  / (Math.Cos(latitudeRad) * Math.Cos(declination));

            // 反余弦的定义域是 [−1, 1]，超出即表示当天太阳不落或不升
            if (cosHourAngle <= -1.0)
            {
                // 太阳整日都在地平线之上
                return new SunTimeResult(SunTimeStatus.PolarDay, null, null);
            }

            if (cosHourAngle >= 1.0)
            {
                // 太阳整日都在地平线之下
                return new SunTimeResult(SunTimeStatus.PolarNight, null, null);
            }

            double hourAngle = Math.Acos(cosHourAngle);   // 弧度

            #endregion

            #region 第 5 步：换算出日出与日落的本地时刻

            // 时角是弧度制，2π 弧度 = 1 天，故 ω/2π 即为"以天为单位"的时间跨度
            double halfDaySpan = hourAngle / (2.0 * Math.PI);

            DateTime sunrise = JulianDayToLocalTime(julianDayTransit - halfDaySpan, utcOffset);
            DateTime sunset = JulianDayToLocalTime(julianDayTransit + halfDaySpan, utcOffset);

            return new SunTimeResult(SunTimeStatus.Normal, sunrise, sunset);

            #endregion
        }

        /// <summary>
        /// 把设置页里填写的经纬度文本解析成十进制度数值。
        /// 支持："39.90"、"-33.87"、"39.9" 等写法（自动去除首尾空格与度数符号 °）。
        /// </summary>
        /// <param name="text">用户填写的文本</param>
        /// <param name="value">解析结果；失败时为 0</param>
        /// <returns>解析是否成功</returns>
        public static bool TryParseDegrees(string? text, out double value)
        {
            value = 0.0;

            if (string.IsNullOrWhiteSpace(text)) return false;

            // 去掉首尾空白与常见的度数符号，兼容用户从地图软件复制过来的文本
            string cleaned = text.Trim().TrimEnd('°', '度').Trim();

            // ⚠ 必须用 InvariantCulture：
            //   中文区域的 NumberStyles 会把小数点当成千位分隔符处理，
            //   导致 "39.90" 被解析成 3990 这类错误结果。
            return double.TryParse(
                cleaned,
                NumberStyles.Float | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture,
                out value);
        }

        /// <summary>
        /// 判断给定时刻属于一天中的哪个时段（黎明 / 日间 / 黄昏 / 夜间），
        /// 供晨昏区切换图标与文字使用。
        ///
        /// 【划分方式】以民用日出、日落时刻为基准，各自向前后延伸 transition 时长：
        ///     黎明 = [日出 − t, 日出 + t)
        ///     黄昏 = [日落 − t, 日落 + t)
        ///     日间 = [日出 + t, 日落 − t)
        ///     其余 = 夜间（含跨天的那一段）
        ///
        /// ⚠ 高纬度地区冬季昼短，可能出现"日出 + t"已经晚于"日落 − t"的情况，
        ///   此时日间区间整个为空，当日不会有任何时刻被判为日间——
        ///   这符合实际（那天确实几乎整天都是暗的），不必特殊处理。
        /// </summary>
        /// <param name="now">要判断的时刻（本地时间）</param>
        /// <param name="sunTime">当天的日出日落计算结果</param>
        /// <param name="transition">黎明与黄昏各自向两侧延伸的时长（如 30 分钟）</param>
        /// <returns>时段</returns>
        public static DayPeriod ResolveDayPeriod(DateTime now, SunTimeResult sunTime, TimeSpan transition)
        {
            #region 1. 先处理"没有日出日落"的几种情况

            switch (sunTime.Status)
            {
                case SunTimeStatus.PolarDay:
                    return DayPeriod.PolarDay;

                case SunTimeStatus.PolarNight:
                    return DayPeriod.PolarNight;

                case SunTimeStatus.Invalid:
                    return DayPeriod.Unknown;
            }

            // 走到这里说明状态是 Normal，日出与日落必定有值；
            // 仍然做一次空值防护，避免将来改动导致意外崩溃
            if (sunTime.Sunrise is not DateTime sunrise || sunTime.Sunset is not DateTime sunset)
            {
                return DayPeriod.Unknown;
            }

            #endregion

            #region 2. 按当前时刻落在哪一段来判定

            // 黎明：日出前后各 transition
            if (now >= sunrise - transition && now < sunrise + transition)
            {
                return DayPeriod.Dawn;
            }

            // 黄昏：日落前后各 transition
            if (now >= sunset - transition && now < sunset + transition)
            {
                return DayPeriod.Dusk;
            }

            // 日间：日出之后、日落之前（去掉两端过渡段）
            if (now >= sunrise + transition && now < sunset - transition)
            {
                return DayPeriod.Day;
            }

            // 其余全部归为夜间——包括"日落后到次日日出前"这段跨天的区间
            return DayPeriod.Night;

            #endregion
        }

        #region 内部辅助方法

        /// <summary>把 UTC 时刻转换为儒略日（Julian Day）。</summary>
        private static double ToJulianDay(DateTime utcTime)
            => (utcTime - UnixEpoch).TotalDays + 2440587.5;

        /// <summary>把儒略日转换为指定时区的本地时刻。</summary>
        private static DateTime JulianDayToLocalTime(double julianDay, TimeSpan utcOffset)
        {
            double unixDays = julianDay - 2440587.5;
            DateTime utc = UnixEpoch.AddDays(unixDays);

            // 加上时区偏移后即为本地钟表时间；
            // 用 SpecifyKind 标明它是"无时区概念的本地时间"，避免后续被误当作 UTC 处理
            return DateTime.SpecifyKind(utc.Add(utcOffset), DateTimeKind.Unspecified);
        }

        /// <summary>角度转弧度。</summary>
        private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

        /// <summary>把角度归一到 [0, 360) 区间。</summary>
        private static double Normalize360(double degrees)
        {
            degrees %= 360.0;
            return degrees < 0.0 ? degrees + 360.0 : degrees;
        }

        #endregion
    }
}
