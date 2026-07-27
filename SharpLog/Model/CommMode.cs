using System;
using System.Collections.Generic;
using System.Text;

namespace SharpLog.Model
{
    /// <summary>
    /// 通联模式对象模型
    /// </summary>
    public class CommMode
    {
        /// <summary>
        /// 通联模式名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 通联模式代码
        /// </summary>
         public string Code { get; set; } = string.Empty;
    }
}
