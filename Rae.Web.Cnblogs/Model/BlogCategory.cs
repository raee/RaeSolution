using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Rae.Web.Cnblogs.Model
{
    /// <summary>
    /// 博客分类
    /// </summary>
    public class BlogCategory
    {
        /// <summary>
        /// 分类ID
        /// </summary>
        public string CategoryId { get; set; }

        /// <summary>
        /// 分类名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 博客真实路径
        /// </summary>
        public string Address { get; set; }
    }
}