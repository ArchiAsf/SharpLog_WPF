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
    /// 主键：自增Id；StartTime建立普通索引用于时间检索
    /// </summary>
    [Table("LogTable")] // 显式指定数据表名称，防止EF自动转为复数LogTables
    [Index(nameof(StartTime))]           // 时间索引
    [Index(nameof(CallSign))]              // 呼号索引
    [Index(nameof(Mode))]                  // 模式索引
    [Index(nameof(Frequency))]             // 频率索引
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
        /// 通联开始时间
        /// 格式统一：YYYY-MM-DD HH:MM:SS，SQLite存储TEXT
        /// </summary>
        [Required(ErrorMessage = "通联开始时间不能为空")]
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 通联结束时间
        /// 格式统一：YYYY-MM-DD HH:MM:SS
        /// </summary>
        [Required(ErrorMessage = "通联结束时间不能为空")]
        public DateTime EndTime { get; set; } 

        /// <summary>
        /// 对方呼号
        /// </summary>
        [Required(ErrorMessage = "对方呼号不能为空")]
        public string CallSign { get; set; } 

        /// <summary>
        /// 通联频率（单位khz，示例7050000 = 7050kHz）
        /// 使用string兼容ADIF导入导出各种不规则文本格式
        /// </summary>
        [Required(ErrorMessage = "通联频率不能为空")]
        public double Frequency { get; set; } 

        /// <summary>
        /// 通联模式 CW / SSB / FT8 / FT4 等
        /// </summary>
        [Required(ErrorMessage = "通联模式不能为空")]
        public string Mode { get; set; }

        /// <summary>
        /// 通联所属操作员
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
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 时区（可选，示例：UTC+8、UTC-5）
        /// </summary>
        public string? TimeZone { get; set; } 

        /// <summary>
        /// 我方送出RST信号报告
        /// </summary>
        public string? RST { get; set; } 

        /// <summary>
        /// 对方回传RST信号报告
        /// </summary>

        public string? ReceivedRST { get; set; } 

        /// <summary>
        /// 对方QTH地理位置（可选）
        /// </summary>
        public string? QTH { get; set; }

        /// <summary>
        /// 电台型号RIG（可选）
        /// </summary>
        public string? Rig { get; set; }


        /// <summary>
        /// 发射功率（可选，例如100W、5W）
        /// </summary>
        public string? Power { get; set; }

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
        public string? Remarks { get; set; }


        public int? GroupID { get; set; }

        public int? StarID { get; set; }

        /// <summary>
        /// 是否交换了QSL卡片
        /// </summary>
        public bool IsQSL { get; set; } = false;

        /// <summary>
        /// QSL卡片接收时间（可选，格式：YYYY-MM-DD HH:MM:SS）
        /// </summary>
        public string? QslReceiveTime { get; set; } 

        /// <summary>
        /// QSL卡片发送时间（可选，格式：YYYY-MM-DD HH:MM:SS）
        /// </summary>
        public string? QslSendTime { get; set; } 

        /// <summary>
        /// QSL卡片接收地址（可选）
        /// </summary>
        public string? QslAddress { get; set; }

        /// <summary>
        /// QSL卡片邮政编码（可选）
        /// </summary>
        public string? PostalCode { get; set; } 

        /// <summary>
        /// QSL卡片接收人姓名（可选）
        /// </summary>
        public string? ReceiverName { get; set; }

        /// <summary>
        /// QSL卡片接收人联系方式（可选）
        /// </summary>
        public string? ReceiverContact { get; set; }

        /// <summary>
        /// 天气情况（可选，示例：晴、雨、雪）
        /// </summary>
        public string? Weather { get; set; }

        /// <summary>
        /// 温度情况（可选，示例：25°C、-5°C）
        /// </summary>
        public string? Temperature { get; set; }


        /// <summary>
        /// 是否双向确认通联
        /// </summary>
        public bool IsConfirmed { get; set; } = false;
    }

}
