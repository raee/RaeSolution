using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Rae.Web.Cnblogs.Model
{
    /// <summary>
    /// 博客实体
    /// </summary>
    public class Blog
    {
        public string BlogId { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }
        public string Summary { get; set; }
        public string CategoryId { get; set; }

        public string Autor { get; set; }
        public string AutorUrl { get; set; }
        public string AutorImage { get; set; }

        public string BlogApp { get; set; }

        public int ViewCount { get; set; }
        public int CommentCount { get; set; }
        public int DiggsCount { get; set; }

        public string SendDate { get; set; }
        public string UpdateDate { get; set; }

    }
}