using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace Rae.Web.Cnblogs.Api.Cnblogs
{
    public class CnblogsApi : ICnblogsApi
    {
        private string contenturl = "http://wcf.open.cnblogs.com/blog/post/body/";

        private Http.HttpRequest mRequest = new Http.HttpRequest();

        public List<Model.BlogCategory> GetCategory()
        {
            // 获取本地数据库的分类

            throw new NotImplementedException();
        }

        public List<Model.Blog> GetBlogs(string categoryId, int page)
        {
            throw new NotImplementedException("该方法用盒仔茶的！");
        }

        public List<Model.Blog> GetBlogsByLastId(string blogId, int page)
        {
            throw new NotImplementedException("该方法用盒仔茶的！");
        }


        public string GetBlogContent(string blogId)
        {
            try
            {
                string xml = mRequest.Get(contenturl + blogId);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                return doc.DocumentElement.InnerXml;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}