using System;
using System.Collections.Generic;
using System.Text;

namespace SharpLog.Model
{
    /// <summary>
    /// 传播方式下拉选项模型
    /// </summary>
    public class PropagationItem
    {
        /// <summary>
        /// 界面显示中文名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// ADIF标准标识代码，存入数据库
        /// </summary>
        public string Code { get; set; } = string.Empty;

    }

}
