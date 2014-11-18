using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Rae.Web.Cnblogs.Api.Davismy
{
    /// <summary>
    /// 合仔茶博客接口
    /// </summary>
    public class DavismyApi : ICnblogsApi
    {
        private Http.HttpRequest mRequest = new Http.HttpRequest();
        private string baseUrl = "http://cnblogs.davismy.com/Handler.ashx?op=GetTimeLine";
        private DavismyJsonParser parser = new DavismyJsonParser();

        public DavismyApi()
        {
        }

        public string GetUrl(string page, string cateAddress)
        {
            int time = DateTime.Now.Millisecond;
            return string.Format("{0}&page={1}&channelpath={2}&since_id=&max_id=&t={3}", baseUrl, page, cateAddress, time);
        }

        public List<Model.BlogCategory> GetCategory()
        {
            throw new NotImplementedException();
        }

        public List<Model.Blog> GetBlogs(string categoryId, int page)
        {
            string json = mRequest.Get(GetUrl(page.ToString(), categoryId));
            if (json.Equals("{\"data\":\"\";\"op\":\"\"}"))
            {
                return new List<Model.Blog>();
            }
            else
            {
                return parser.Parse(json);
            }

        }

        public List<Model.Blog> GetBlogsByLastId(string blogId, int page)
        {
            throw new NotImplementedException();
        }


        public string GetBlogContent(string blogId)
        {
            throw new NotImplementedException();
        }
    }
}