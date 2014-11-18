using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NUnit.Framework;

namespace Rae.Web.Cnblogs
{
    [TestFixture]
    public class TestApi
    {
        [Test]
        public void TestHttp()
        {
            string url = "http://www.baidu.com/s";
            Show("测试网络下载:" + url);
            Dictionary<string, object> param = new Dictionary<string, object>();
            param.Add("word", "微博");
            Http.HttpRequest request = new Http.HttpRequest();
            string html = request.Get(url, param);
            Assert.IsNotNullOrEmpty(html, "网页内容不能为空");
            Show(html);
        }


        [Test]
        public void TestBlogList()
        {
            Api.ICnblogsApi api = new Api.Davismy.DavismyApi();
            var result = api.GetBlogs("", 1);
            Assert.IsTrue(result.Count > 0, "列表为空！");
            foreach (var item in result)
            {
                Show(item.Title);
                Show("-------------->" + item.Content);
            }
        }

        [Test]
        public void TestBlogContent()
        {
            Api.ICnblogsApi api = new Api.Cnblogs.CnblogsApi();
            var result = api.GetBlogContent("4104482");

            Show(result);
        }


        private void Show(string msg)
        {
            Console.WriteLine(msg);
        }
    }
}