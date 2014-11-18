using Rae.Web.Cnblogs.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Rae.Web.Cnblogs.Api
{
    /// <summary>
    /// 博客园API
    /// </summary>
    public interface ICnblogsApi
    {
        /// <summary>
        /// 获取博客分类
        /// </summary>
        /// <returns></returns>
        List<BlogCategory> GetCategory();

        /// <summary>
        /// 获取分页博客，默认分页20条。
        /// </summary>
        /// <param name="categoryId">分类ID</param>
        /// <param name="page">页数</param>
        /// <returns></returns>
        List<Blog> GetBlogs(string categoryId, int page);

        /// <summary>
        /// 通过上一条ID获取分类博客，默认
        /// </summary>
        /// <param name="page"></param>
        /// <returns></returns>
        List<Blog> GetBlogsByLastId(string blogId, int page);

        /// <summary>
        /// 获取博客内容
        /// </summary>
        /// <param name="blogId"></param>
        /// <returns></returns>
        string GetBlogContent(string blogId);
    }
}