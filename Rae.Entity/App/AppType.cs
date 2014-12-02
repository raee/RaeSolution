using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rae.Entity.App
{
    /// <summary>
    ///  应用类型枚举
    /// </summary>
    public enum AppTypeEnum
    {
        /// <summary>
        /// 安卓设备
        /// </summary>
        Android = 0,

        /// <summary>
        /// 网页应用
        /// </summary>
        Web,

        /// <summary>
        /// 苹果手机
        /// </summary>
        Iphone,

        /// <summary>
        /// 苹果平板
        /// </summary>
        Ipad,

        /// <summary>
        /// 微软手机
        /// </summary>
        WindowsPhone,


    }

    /// <summary>
    ///  应用类型
    /// </summary>
    public class AppType
    {
        /// <summary>
        /// 类型ID
        /// </summary>
        public string AppTypeId { get; set; }

        /// <summary>
        /// 类型名称
        /// </summary>
        public string AppTypeName { get; set; }
    }
}
