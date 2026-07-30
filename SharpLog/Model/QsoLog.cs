using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace SharpLog.Model
{
    /// <summary>
    /// 通联日志表实体类（对应SQLite中的LogTable表）
    /// 业余无线电QSO通联记录
    /// 主键：自增Id；StartTime建立普通索引用于时间检索
    /// </summary>
    [Table("LogTable")] // 显式指定数据表名称，防止EF自动转为复数LogTables
    public class LogTable
    {
        /// <summary>
        /// 自增唯一主键，数据库自动生成
        /// 用来唯一标识单条通联记录，彻底规避时间重复冲突问题
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>
        /// 通联开始时间
        /// 格式统一：YYYY-MM-DD HH:MM:SS，SQLite存储TEXT
        /// </summary>
        [Required(ErrorMessage = "通联开始时间不能为空")]
        public string StartTime { get; set; } = string.Empty;

        /// <summary>
        /// 通联结束时间
        /// 格式统一：YYYY-MM-DD HH:MM:SS
        /// </summary>
        [Required(ErrorMessage = "通联结束时间不能为空")]
        public string EndTime { get; set; } = string.Empty;

        /// <summary>
        /// 对方呼号
        /// </summary>
        [Required(ErrorMessage = "对方呼号不能为空")]
        public string CallSign { get; set; } = string.Empty;

        /// <summary>
        /// 通联频率（单位Hz，示例7050000 = 7.050MHz）
        /// 使用string兼容ADIF导入导出各种不规则文本格式
        /// </summary>
        [Required(ErrorMessage = "通联频率不能为空")]
        public string Frequency { get; set; } = string.Empty;

        /// <summary>
        /// 通联模式 CW / SSB / FT8 / FT4 等
        /// </summary>
        [Required(ErrorMessage = "通联模式不能为空")]
        public string Mode { get; set; } = string.Empty;

        /// <summary>
        /// 时区（可选，示例：UTC+8、UTC-5）
        /// </summary>
        public string TimeZone { get; set; } = string.Empty;

        /// <summary>
        /// 我方送出RST信号报告
        /// </summary>
        public string RST { get; set; } = string.Empty;

        /// <summary>
        /// 对方回传RST信号报告
        /// </summary>

        public string RRST { get; set; } = string.Empty;

        /// <summary>
        /// 对方QTH地理位置（可选）
        /// </summary>
        public string? QTH { get; set; }

        /// <summary>
        /// 电台型号RIG（可选）
        /// </summary>
        public string? RIG { get; set; }


        /// <summary>
        /// 发射功率（可选，例如100W、5W）
        /// </summary>
        public string? Pow { get; set; }

        /// <summary>
        /// 天线类型ANT（可选）
        /// </summary>
        public string? ANT { get; set; }

        /// <summary>
        /// 发射高度（可选）
        /// </summary>
        public string? Height { get; set; }

        /// <summary>
        /// 操作员OP（可选）
        /// </summary>
        public string? OP { get; set; }

        /// <summary>
        /// 网格定位（可选，示例：FN31pr）
        /// </summary>
        public string? Locator { get; set; }

        /// <summary>
        /// DMR数字集群通联ID（可选，示例：123456）
        /// </summary>
        public string DigitalID { get; set; }

        /// <summary>
        /// 备注信息RMKS（可选）
        /// </summary>
        public string? RMKS { get; set; }

        /// <summary>
        /// 是否交换了QSL卡片
        /// </summary>
        public bool IsQSL { get; set; }

        /// <summary>
        /// QRP低功率通联
        /// </summary>
        public bool IsQRP { get; set; }

        /// <summary>
        /// 是否双向确认通联
        /// </summary>
        public bool IsConfirmation { get; set; }
    }

}
