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
    /// 通联日志表实体类（对应SQLite中的LogTable表）
    /// 业余无线电QSO通联记录
    /// 主键：自增Id；QSO_DATE建立普通索引用于时间检索
    /// 字段命名已统一为 ADIF 3.1.7 规范名称（如 CALL/FREQ/MODE/RST_SENT 等）
    /// </summary>
    [Table("LogTable")] // 显式指定数据表名称，防止EF自动转为复数LogTables
    [Index(nameof(QSO_DATE))]          // 日期索引（ADIF: QSO_DATE）
    [Index(nameof(CALL))]              // 呼号索引（ADIF: CALL）
    [Index(nameof(MODE))]              // 模式索引（ADIF: MODE）
    [Index(nameof(FREQ))]              // 频率索引（ADIF: FREQ）
    [Index(nameof(GroupID))]           // 分组索引
    [Index(nameof(StarID))]            // 星级索引
    public class QsoLog
    {
        /// <summary>
        /// 自增唯一主键，数据库自动生成
        /// 用来唯一标识单条通联记录，彻底规避时间重复冲突问题
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 通联开始日期（ADIF: QSO_DATE，格式 YYYY-MM-DD，SQLite存储TEXT）
        /// 拆分自原 StartTime 字段，与 TIME_ON 组合表示开始时间
        /// </summary>
        [Required(ErrorMessage = "通联开始日期不能为空")]
        public DateTime? QSO_DATE { get; set; }

        /// <summary>
        /// 通联开始时间（ADIF: TIME_ON，格式 HHMMSS 文本，如 143000）
        /// </summary>
        public string? TIME_ON { get; set; }

        /// <summary>
        /// 通联结束日期（ADIF: QSO_DATE_OFF，可选）
        /// 拆分自原 EndTime 字段，与 TIME_OFF 组合表示结束时间
        /// </summary>
        public DateTime? QSO_DATE_OFF { get; set; }

        /// <summary>
        /// 通联结束时间（ADIF: TIME_OFF，格式 HHMMSS 文本，可选）
        /// </summary>
        public string? TIME_OFF { get; set; }

        /// <summary>
        /// 对方呼号（ADIF: CALL）
        /// </summary>
        [Required(ErrorMessage = "对方呼号不能为空")]
        public string CALL { get; set; }

        /// <summary>
        /// 通联频率（ADIF: FREQ；存储语义沿用原 Frequency：输入为 kHz，如 7050 表示 7050kHz）
        /// 注意：ADIF 标准单位为 MHz，导入导出时需 /1000 或 *1000 转换
        /// </summary>
        [Required(ErrorMessage = "通联频率不能为空")]
        public double FREQ { get; set; }

        /// <summary>
        /// 通联模式（ADIF: MODE，CW / SSB / FT8 / FT4 等，全大写）
        /// </summary>
        [Required(ErrorMessage = "通联模式不能为空")]
        public string MODE { get; set; }

        /// <summary>
        /// 通联所属操作员（项目自定义字段，ADIF 无对应字段，保留原名）
        /// </summary>
        [Required(ErrorMessage = "通联日志所属操作员不能为空")]
        public string RadioOperator { get; set; }

        /// <summary>
        /// 记录创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime? LastUpdatedAt { get; set; }

        /// <summary>
        /// 时区（可选，示例：UTC+8、UTC-5）
        /// </summary>
        public string? TimeZone { get; set; }

        /// <summary>
        /// 收到的信号报告（ADIF: RST_RCVD，对方发来的报告）
        /// 注意：按 UI「信号报告_收」语义映射，原 RST 注释「我方送出」已修正
        /// </summary>
        public string? RST_RCVD { get; set; }

        /// <summary>
        /// 发出的信号报告（ADIF: RST_SENT，我方发给对方的报告）
        /// 按 UI「信号报告_发」语义映射，原 ReceivedRST 对应
        /// </summary>
        public string? RST_SENT { get; set; }

        /// <summary>
        /// 对方QTH地理位置（ADIF: QTH，对方城市）
        /// </summary>
        public string? QTH { get; set; }

        /// <summary>
        /// 电台型号（ADIF: RIG）
        /// </summary>
        public string? RIG { get; set; }


        /// <summary>
        /// 对方发射功率（ADIF: RX_PWR，单位W，可选）
        /// </summary>
        public string? RX_PWR { get; set; }

        /// <summary>
        /// 我方发射功率（ADIF: TX_PWR，单位W，可选）
        /// </summary>
        public string? TX_PWR { get; set; }

        /// <summary>
        /// 天线类型（项目自定义字段，ADIF 无对应字段，保留原名）
        /// </summary>
        public string? ANT { get; set; }

        /// <summary>
        /// 发射高度（项目自定义字段，ADIF 无对应字段，保留原名）
        /// </summary>
        public string? Height { get; set; }

        /// <summary>
        /// 操作员（项目自定义字段，ADIF 的 OPERATOR 指操作员呼号而非姓名，语义不符保留原名）
        /// </summary>
        public string? OP { get; set; }

        /// <summary>
        /// 传播模式（ADIF: PROP_MODE，示例：SAT、EME、Meteor Scatter）
        /// </summary>
        public string? PROP_MODE { get; set; }

        /// <summary>
        /// 网格定位（ADIF: GRIDSQUARE，Maidenhead 网格，示例：FN31pr）
        /// </summary>
        public string? GRIDSQUARE { get; set; }

        /// <summary>
        /// DMR数字集群通联ID（项目自定义字段，ADIF 无对应字段，保留原名，示例：123456）
        /// </summary>
        public string DigitalID { get; set; }

        /// <summary>
        /// 备注信息（ADIF: COMMENT，QSO备注）
        /// </summary>
        public string? COMMENT { get; set; }




        /// <summary>
        /// 通联所属分组ID（项目自定义字段，用于分组管理）
        /// </summary>
        public int? GroupID { get; set; }

        /// <summary>
        /// 通联所属星级ID（项目自定义字段，用于分级管理）
        /// </summary>
        public int? StarID { get; set; }

        /// <summary>
        /// 是否收到/交换了QSL卡片（ADIF: QSL_RCVD 命名；当前为 bool 是/否，后续可升级为 Y/N/R/I/V 五态枚举）
        /// </summary>
        public bool QSL_RCVD { get; set; } = false;

        /// <summary>
        /// QSL卡片接收日期（ADIF: QSLRDATE，可选）
        /// </summary>
        public DateTime? QSLRDATE { get; set; }

        /// <summary>
        /// QSL卡片发送日期（ADIF: QSLSDATE，可选）
        /// </summary>
        public DateTime? QSLSDATE { get; set; }

        /// <summary>
        /// QSL卡片接收地址（项目自定义字段，ADIF 无对应字段，保留原名，可选）
        /// </summary>
        public string? QslAddress { get; set; }

        /// <summary>
        /// QSL卡片邮政编码（项目自定义字段，保留原名，可选）
        /// </summary>
        public string? PostalCode { get; set; }

        /// <summary>
        /// QSL卡片接收人姓名（项目自定义字段，保留原名，可选）
        /// </summary>
        public string? ReceiverName { get; set; }

        /// <summary>
        /// QSL卡片接收人联系方式（项目自定义字段，保留原名，可选）
        /// </summary>
        public string? ReceiverContact { get; set; }

        /// <summary>
        /// 天气情况（项目自定义字段，ADIF 无对应字段，可选，示例：晴、雨、雪）
        /// </summary>
        public string? Weather { get; set; }

        /// <summary>
        /// 温度情况（项目自定义字段，ADIF 无对应字段，可选，示例：25°C、-5°C）
        /// </summary>
        public string? Temperature { get; set; }


        /// <summary>
        /// 是否双向确认通联（项目自定义字段，ADIF 无直接对应，可选）
        /// </summary>
        public bool IsConfirmed { get; set; } = false;
    }

}
