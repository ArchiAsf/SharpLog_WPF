using System;
using System.Collections.Generic;
using System.Text;

namespace SharpLog.Model
{
    /// <summary>
    /// 传播模式下拉选项模型 —— 连接「界面显示」与「ADIF 存储」的桥梁。
    ///
    /// ════════════════════════════════════════════════════════════════════════
    /// 【与通联模式模型（ModeItem）的区别】
    /// ADIF 中通联模式采用「MODE + SUBMODE」两级体系，所以 ModeItem 有三个属性；
    /// 而传播模式 PROP_MODE 是**单值枚举**，没有子级结构，
    /// 因此本模型只需要两个属性：显示名 + ADIF 代码。
    ///
    /// 【数据来源】
    /// Code 取值全部来自 ADIF 3.1.7 官方 enumerations_propagation_mode.json
    /// （程序目录 ADIFCode_Json，由 AdifDataLoader 加载，共 20 条标准值）。
    ///
    /// 【关于 Code 为 null 的一项】
    /// 列表首项「（不指定）」的 Code 为 null，表示本次通联未记录传播方式。
    /// PROP_MODE 在 ADIF 中是可选字段，不填即为空；
    /// 绝不可用 "Simplex"（单工）之类的非官方值冒充，那会造成数据污染。
    /// ════════════════════════════════════════════════════════════════════════
    /// </summary>
    public class PropagationItem
    {
        /// <summary>
        /// 界面显示名称，格式为「中文 + 代码」，例如 "F2层反射 F2"、"中继/转发器 RPT"。
        /// 首项为 "（不指定）"，表示不记录传播方式。
        /// 该属性同时作为下拉框显示文本与日志卡片上的显示文本
        /// （见 ModeConvertTools.ResolvePropDisplayName）。
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// ADIF 标准代码（PROP_MODE 字段值），全大写，如 "F2"、"ES"、"RPT"。
        /// 「（不指定）」项为 null → 存库时 QsoLog.PROP_MODE 写 NULL，
        /// 导出 ADIF 文件时该字段整体省略。
        /// </summary>
        public string? Code { get; set; }

        /// <summary>
        /// 重写 ToString：让未设置 DisplayMemberPath 的下拉框也能显示正常文字，
        /// 避免出现 "SharpLog.Model.PropagationItem" 这样的类型名。
        /// </summary>
        public override string ToString() => DisplayName;
    }
}
