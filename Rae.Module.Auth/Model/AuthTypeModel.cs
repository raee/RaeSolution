using System;
using System.Collections.Generic;
using System.Text;

namespace Rae.Module.Auth.Model
{
    /// <summary>
    /// 认证类型实体
    /// </summary>
    public class AuthTypeModel
    {
        /// <summary>
        /// 认证类型Id
        /// </summary>
        public string AuthTypeId { get; set; }

        /// <summary>
        /// 类型名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 类型说明
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 图像地址
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// 创建人
        /// </summary>
        public string Creator { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public string CreateDate { get; set; }

        /// <summary>
        /// 图标
        /// </summary>
        public string Icon { get; set; }

    }
}
