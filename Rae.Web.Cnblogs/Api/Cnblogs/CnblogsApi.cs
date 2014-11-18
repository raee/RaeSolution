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
            throw new NotImplementedException();
        }

        public List<Model.Blog> GetBlogs(string categoryId, int page)
        {
            return null;
        }

        public List<Model.Blog> GetBlogsByLastId(string blogId, int page)
        {
            throw new NotImplementedException();
        }


        public string GetBlogContent(string blogId)
        {
            try
            {
                string xml = mRequest.Get(contenturl + blogId);
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);
                // return doc.SelectSingleNode("/string").InnerText;
                return doc.DocumentElement.InnerXml;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}