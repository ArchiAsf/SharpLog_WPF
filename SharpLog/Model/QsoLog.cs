using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.AccessControl;
using System.Text;

namespace SharpLog.Model
{
    /// <summary>
    /// 通联日志表实体类（对应 SQLite 数据库中的 LogTable 表）
    ///
    /// 业余无线电 QSO 通联记录的核心模型：
    /// 一条记录 = 一次完整的业余无线电通联（时间、呼号、频率、模式、信号报告、QSL 交换等）。
    ///
    /// 命名规范：字段名已统一为 ADIF 3.1.7 标准字段名（如 CALL、FREQ、MODE、RST_SENT 等），
    /// 便于后续实现 ADIF 格式的导入/导出。
    ///
    /// 主键：自增 Id；QSO_DATE 建立普通索引用于按时间检索日志。
    /// </summary>
    [Table("LogTable")] // 显式指定数据表名称，防止 EF 自动转为复数 LogTables
    [Index(nameof(QSO_DATE))]   // 日期索引（ADIF: QSO_DATE）——按通联日期查询/排序
    [Index(nameof(CALL))]       // 呼号索引（ADIF: CALL）——按呼号检索
    [Index(nameof(MODE))]       // 模式索引（ADIF: MODE）——按模式统计
    [Index(nameof(FREQ))]       // 频率索引（ADIF: FREQ）——按频率检索
    [Index(nameof(GroupID))]    // 分组索引（项目自定义）——分组管理
    [Index(nameof(StarID))]     // 星级索引（项目自定义）——分级管理
    public class QsoLog
    {
        /// <summary>
        /// 自增唯一主键，数据库自动生成。
        /// 用来唯一标识单条通联记录，彻底规避时间重复冲突问题。
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // ==================== 通联时间（ADIF 标准） ====================
        // 说明：UI 输入使用复合 DateTime（hc:DateTimePicker），
        // 在 ModeConvertTools 转换时拆分为"日期 + 时间"分别存储，
        // 与 ADIF 规范 QSO_DATE（日期）/ TIME_ON（时间）的拆分方式一致。

        /// <summary>
        /// 通联开始日期（ADIF: QSO_DATE）。
        /// 只存储日期部分（DateTime.Date），格式 YYYY-MM-DD，SQLite 存储为 TEXT。
        /// 必填字段；与 TIME_ON 组合表示通联开始时刻。
        /// </summary>
        [Required(ErrorMessage = "通联开始日期不能为空")]
        public DateTime? QSO_DATE { get; set; }

        /// <summary>
        /// 通联开始时间（ADIF: TIME_ON）。
        /// 以文本存储 ADIF 标准格式 HHmmss（如 143000 = 14:30:00 UTC）。
        /// 可选字段（ADIF 规范允许只记日期不记时间）。
        /// </summary>
        public string? TIME_ON { get; set; }

        /// <summary>
        /// 通联结束日期（ADIF: QSO_DATE_OFF，可选）。
        /// 只存储日期部分；与 TIME_OFF 组合表示通联结束时刻。
        /// 为空表示通联在当天内结束（时长不足 24 小时）。
        /// </summary>
        public DateTime? QSO_DATE_OFF { get; set; }

        /// <summary>
        /// 通联结束时间（ADIF: TIME_OFF，可选）。
        /// 文本格式 HHmmss；与 QSO_DATE_OFF 组合使用。
        /// </summary>
        public string? TIME_OFF { get; set; }

        // ==================== 通联对象信息（对方电台） ====================

        /// <summary>
        /// 对方呼号（ADIF: CALL），必填。
        /// 写入时自动转大写（见 MainLogWindow_VM 的 OnCALLChanged）。
        /// </summary>
        [Required(ErrorMessage = "对方呼号不能为空")]
        public string CALL { get; set; }

        /// <summary>
        /// 通联频率（ADIF: FREQ）。
        /// 存储语义沿用项目习惯：输入为 kHz 数值（如 7050 表示 7050kHz = 7.05MHz）。
        /// ⚠️ 注意：ADIF 标准单位为 MHz，导入导出时需做 /1000 或 *1000 换算。
        /// </summary>
        [Required(ErrorMessage = "通联频率不能为空")]
        public double FREQ { get; set; }

        /// <summary>
        /// 通联模式（ADIF: MODE），必填，全大写。
        /// 取值参照 ADIF 官方 Mode 枚举：CW / SSB / FT8 / FT4 / FM / AM / SSTV / DMR 等，
        /// 见 StaticValues.Set_CommModeList 下拉列表。
        /// </summary>
        [Required(ErrorMessage = "通联模式不能为空")]
        public string MODE { get; set; }

        /// <summary>
        /// 通联所属操作员（项目自定义字段，ADIF 无对应字段，保留原名）。
        /// 当前由 ModeConvertTools 硬编码为 "BG2GPT"（本站呼号）。
        /// </summary>
        [Required(ErrorMessage = "通联日志所属操作员不能为空")]
        public string RadioOperator { get; set; }

        /// <summary>
        /// 记录创建时间（项目内部元数据），默认 UTC 当前时间。
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后修改时间（项目内部元数据，可选）。
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// 通联记录使用的时区（可选，示例：UTC+8、UTC-5）。
        /// 由 MainLogWindow_VM 根据 IsUseUtcTime 自动写入。
        /// </summary>
        public string? TimeZone { get; set; }

        // ==================== 信号报告与功率 ====================
        // 语义说明（与 UI 标签对应）：
        //   "信号报告_收" = 对方发给我的报告 → RST_RCVD
        //   "信号报告_发" = 我发给对方的报告 → RST_SENT

        /// <summary>
        /// 收到的信号报告（ADIF: RST_RCVD，对方电台发来的报告）。
        /// 示例格式：59、599、57 等（RST 三要素：可读性/强度/音调）。
        /// </summary>
        public string? RST_RCVD { get; set; }

        /// <summary>
        /// 发出的信号报告（ADIF: RST_SENT，我方发给对方的报告）。
        /// </summary>
        public string? RST_SENT { get; set; }

        /// <summary>
        /// 对方发射功率（ADIF: RX_PWR，单位 W，可选，如 100W、5W）。
        /// ⚠️ 命名易混淆：RX_PWR 在 ADIF 中指"对方电台的发射功率"（我方接收到的）。
        /// </summary>
        public string? RX_PWR { get; set; }

        /// <summary>
        /// 我方发射功率（ADIF: TX_PWR，单位 W，可选）。
        /// </summary>
        public string? TX_PWR { get; set; }

        // ==================== 对方电台设备与位置 ====================

        /// <summary>
        /// 对方城市（ADIF: QTH，可选）。
        /// ADIF 中 QTH 特指"城市"而非整个地理位置。
        /// </summary>
        public string? QTH { get; set; }

        /// <summary>
        /// 对方电台型号（ADIF: RIG，可选，如 IC-7300、FT-991A）。
        /// </summary>
        public string? RIG { get; set; }

        /// <summary>
        /// 对方天线类型（项目自定义字段，ADIF 无对应字段，保留原名，可选）。
        /// </summary>
        public string? ANT { get; set; }

        /// <summary>
        /// 发射高度（项目自定义字段，ADIF 无对应字段，保留原名，可选，单位 M）。
        /// </summary>
        public string? Height { get; set; }

        /// <summary>
        /// 操作员（项目自定义字段，保留原名，可选）。
        /// ⚠️ 与 ADIF OPERATOR 字段语义不同（ADIF OPERATOR 指操作员呼号而非姓名/称呼）。
        /// </summary>
        public string? OP { get; set; }

        /// <summary>
        /// 传播模式（ADIF: PROP_MODE，可选）。
        /// 取值参照 ADIF 官方 Propagation_Mode 枚举：F2、ES、MS、EME、SAT、TROPO 等，
        /// 见 StaticValues.Set_PropagationModeList 下拉列表。
        /// </summary>
        public string? PROP_MODE { get; set; }

        /// <summary>
        /// 网格定位（ADIF: GRIDSQUARE，可选，示例：FN31pr）。
        /// Maidenhead 网格系统，2/4/6/8 字符，用于计算距离与方位。
        /// </summary>
        public string? GRIDSQUARE { get; set; }

        /// <summary>
        /// DMR 数字集群通联 ID（项目自定义字段，ADIF 无对应字段，保留原名，可选，示例：123456）。
        /// </summary>
        public string DigitalID { get; set; }

        /// <summary>
        /// 备注信息（ADIF: COMMENT，可选）。
        /// 对应 UI 右上角多行备注输入框。
        /// </summary>
        public string? COMMENT { get; set; }

        // ==================== 分组 / 星级（项目自定义管理字段） ====================

        /// <summary>
        /// 通联所属分组 ID（项目自定义字段，可选，用于分组管理，0-5）。
        /// </summary>
        public int? GroupID { get; set; }

        /// <summary> 
        /// 通联所属星级 ID（项目自定义字段，可选，用于分级管理，0-5）。
        /// </summary>
        public int? StarID { get; set; }

        // ==================== QSL 卡片管理（ADIF 标准） ====================

        /// <summary>
        /// 是否已收到/交换 QSL 卡片（ADIF 命名: QSL_RCVD）。
        /// ⚠️ 当前为 bool（是/否）简化实现；ADIF 规范为五态枚举
        /// （Y/N/R/I/V = 已确认/未收到/已请求/无效/已核验），后续可升级。
        /// </summary>
        public bool QSL_RCVD { get; set; } = false;

        /// <summary>
        /// QSL 卡片接收日期（ADIF: QSLRDATE，可选）。
        /// 仅当 QSL_RCVD 为"是"时有意义。
        /// </summary>
        public DateTime? QSLRDATE { get; set; }

        /// <summary>
        /// QSL 卡片发送日期（ADIF: QSLSDATE，可选）。
        /// </summary>
        public DateTime? QSLSDATE { get; set; }

        /// <summary>
        /// QSL 卡片邮寄地址（项目自定义字段，ADIF 无对应字段，保留原名，可选）。
        /// ⚠️ ADIF 的 ADDRESS 指对方电台完整地址，与本字段语义不完全相同。
        /// </summary>
        public string? QslAddress { get; set; }

        /// <summary>
        /// QSL 卡片邮政编码（项目自定义字段，保留原名，可选）。
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// QSL 卡片接收人姓名（项目自定义字段，保留原名，可选）。
        /// </summary>
        public string? ReceiverName { get; set; }

        /// <summary>
        /// QSL 卡片接收人联系方式（项目自定义字段，保留原名，可选，如电话/邮箱）。
        /// </summary>
        public string? ReceiverContact { get; set; }

        // ==================== 通联环境信息（项目自定义字段） ====================

        /// <summary>
        /// 通联时天气情况（项目自定义字段，ADIF 无对应字段，可选，示例：晴、雨、雪）。
        /// </summary>
        public string? Weather { get; set; }

        /// <summary>
        /// 通联时温度情况（项目自定义字段，ADIF 无对应字段，可选，示例：25°C、-5°C）。
        /// </summary>
        public string? Temperature { get; set; }

        /// <summary>
        /// 是否双向确认通联（项目自定义字段，ADIF 无直接对应，可选）。
        /// 表示对方也确认了本次通联（区别于单方面记录）。
        /// </summary>
        public bool IsConfirmed { get; set; } = false;
    }
}
