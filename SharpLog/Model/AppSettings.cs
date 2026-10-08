using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SharpLog.Model
{
    /// <summary>
    /// 软件设置实体（对应 SQLite 数据库中的 AppSettingsTable 表）。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【职责】
    ///   保存"本地台站设置"页里的 16 项内容，让用户填一次就能长期使用，
    ///   而不是每次启动程序都要重新输入。
    ///
    /// 【字段命名】
    ///   与 MainLogWindow_VM 中对应的绑定属性【完全同名】，
    ///   这样实体与 ViewModel 之间可以用 PropertyCopyTools 按名批量互拷，
    ///   将来增删字段时只需要在两边各加一个同名属性，不必再写映射代码。
    ///
    /// 【为什么全部可空】
    ///   用户未填写的项存 NULL 而不是空串，语义上表示"没有设置过"，
    ///   与"设置成了空字符串"区分开；导出 ADIF 时该字段直接省略。
    ///
    /// 【单行表约定】
    ///   本表在数据库中【永远只有一行】，首次保存时插入，之后都是覆盖更新。
    ///   这个约定由 AppSettingsDbTools.SaveAsync 负责维护，调用方不必关心。
    /// ════════════════════════════════════════════════════════════════════════
    /// </summary>
    [Table("AppSettingsTable")]   // 显式指定表名，与 LogTable 命名风格保持一致
    public class AppSettings
    {
        /// <summary>
        /// 自增主键。因为本表只保存一行，实际取值恒为 1。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // ==================== 台站基本信息 ====================

        /// <summary>本站呼号（ADIF: STATION_CALLSIGN）。</summary>
        public string? STATION_CALLSIGN { get; set; }

        /// <summary>日志操作员呼号（ADIF: OPERATOR）。</summary>
        public string? OPERATOR { get; set; }

        /// <summary>本站 DXCC 实体代码（ADIF: MY_DXCC）。</summary>
        public string? MY_DXCC { get; set; }

        /// <summary>本站 CQ 分区，1~40（ADIF: MY_CQ_ZONE）。</summary>
        public string? MY_CQ_ZONE { get; set; }

        /// <summary>本站 ITU 分区，1~90（ADIF: MY_ITU_ZONE）。</summary>
        public string? MY_ITU_ZONE { get; set; }

        // ==================== 台站位置 ====================

        /// <summary>本站 Maidenhead 网格定位（ADIF: MY_GRIDSQUARE）。</summary>
        public string? MY_GRIDSQUARE { get; set; }

        /// <summary>本站所属 DXCC 实体名称（ADIF: MY_COUNTRY）。</summary>
        public string? MY_COUNTRY { get; set; }

        /// <summary>本站经度（ADIF: MY_LON）。</summary>
        public string? MY_LON { get; set; }

        /// <summary>本站纬度（ADIF: MY_LAT）。</summary>
        public string? MY_LAT { get; set; }

        /// <summary>本站邮政编码（ADIF: MY_POSTAL_CODE）。</summary>
        public string? MY_POSTAL_CODE { get; set; }

        // ==================== 地址与联系方式 ====================

        /// <summary>台站地址（ADIF: MY_STREET）。</summary>
        public string? MY_STREET { get; set; }

        /// <summary>通讯地址（项目自定义，ADIF 无对应的本站字段）。</summary>
        public string? MY_MailAddress { get; set; }

        /// <summary>QSL 卡片邮寄地址（项目自定义）。</summary>
        public string? MY_QslAddress { get; set; }

        /// <summary>本站电子邮箱（项目自定义）。</summary>
        public string? MY_Email { get; set; }

        // ==================== 设备 ====================

        /// <summary>本站天线描述（ADIF: MY_ANTENNA）。</summary>
        public string? MY_ANTENNA { get; set; }

        /// <summary>本站设备描述（ADIF: MY_RIG）。</summary>
        public string? MY_RIG { get; set; }

        // ==================== 日志查看页 · 筛选器（持久化） ====================
        // 用户要求：「筛选器是持续生效的，即使软件关了再开也保留」。
        // 因此筛选条件必须落库，而不是只放在内存里。
        // ⚠ 属性名与 MainLogWindow_VM 中的同名绑定属性配对，
        //    由 PropertyCopyTools 按名批量互拷，增删字段时两边各加一个同名属性即可。

        /// <summary>
        /// 筛选器：对方呼号（模糊匹配）。null 或空串表示不按呼号筛选。
        /// </summary>
        public string? FilterCall { get; set; }

        /// <summary>
        /// 筛选器：通联模式（ADIF: MODE）。null 或空串表示全部模式。
        /// </summary>
        public string? FilterMode { get; set; }

        /// <summary>
        /// 筛选器：波段（如 "20m"）。null 或空串表示全部波段。
        /// ⚠ 波段不是数据库列，查询时需先由 ADIF 波段表换算成频率区间。
        /// </summary>
        public string? FilterBand { get; set; }

        /// <summary>
        /// 筛选器：是否只显示已双向确认（IsConfirmed）的记录。
        /// </summary>
        public bool FilterOnlyConfirmed { get; set; } = false;

        /// <summary>
        /// 筛选器：是否只显示今天的通联记录（按 QSO_DATE 匹配本地当天）。
        /// </summary>
        public bool FilterOnlyToday { get; set; } = false;

        // ==================== 元数据 ====================

        /// <summary>
        /// 最后一次保存的时间（UTC）。
        /// 仅作记录用，不在界面上展示；将来可用于"上次保存于 xx"的提示。
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建一个"默认设置"实例。
        ///
        /// ════════════════════════════════════════════════════════════════════
        /// 【用途】「恢复到默认设置」按钮就是用它来重置界面的。
        ///
        /// 【将来要加默认值就改这里】
        /// 如果某个字段希望有初始值（例如 MY_COUNTRY 默认填"中国"），
        /// 只要在下面写：MY_COUNTRY = "中国"，界面上就会自动带出该值，
        /// 其他任何代码都不需要改。
        ///
        /// 目前所有字段均无初始值（全部留空）。
        /// ════════════════════════════════════════════════════════════════════
        /// </summary>
        /// <returns>各项均为空的默认设置</returns>
        public static AppSettings CreateDefault()
        {
            return new AppSettings
            {
                // 目前暂无带初始值的字段。
                // 将来如需默认值，在此处赋值即可，例如：
                // MY_COUNTRY = "中国",
                // MY_ANTENNA = "DP 偶极天线",
            };
        }
    }
}
