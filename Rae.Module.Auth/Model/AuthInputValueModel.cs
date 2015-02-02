using System;
using System.Collections.Generic;
using System.Text;

namespace Rae.Module.Auth.Model
{
    /// <summary>
    /// 控件值实体
    /// </summary>
    public class AuthInputValueModel
    {
        public string DictId { get; set; }

        /// <summary>
        /// 控件唯一Id
        /// </summary>
        public string InputId { get; set; }

        /// <summary>
        /// 控件取值
        /// </summary>
        public string Value { get; set; }
    }
}
