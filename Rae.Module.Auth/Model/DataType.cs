using System;
using System.Collections.Generic;
using System.Text;

namespace Rae.Module.Auth.Model
{
    /// <summary>
    /// 控件数据类型枚举
    /// </summary>
    public enum DataType
    {
        /// <summary>
        /// 普通文本
        /// </summary>
        Text = 0,


        /// <summary>
        /// 多行文本
        /// </summary>
        MultiLineText = 1,

        /// <summary>
        /// 图像
        /// </summary>
        Image = 2,

        /// <summary>
        /// 下拉列表
        /// </summary>
        DropDownList = 3

    }
}
