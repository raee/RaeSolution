using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using Rae.Web.Cnblogs.Model;
using System.Threading;

namespace Rae.Web.Cnblogs.Api.Davismy
{

    public class DavismyJsonModel
    {
        public List<DavismyBlogJsonModel> data { get; set; }
        public string op { get; set; }
    }


    public class DavismyBlogJsonModel
    {
        public string author { get; set; }
        public string blog_id { get; set; }
        public string blog_url { get; set; }
        public string blogapp { get; set; }
        public string comment { get; set; }
        public string content { get; set; }
        public string hit { get; set; }
        public string public_time { get; set; }
        public string title { get; set; }
    }

    public class DavismyJsonParser
    {
        private ICnblogsApi contentApi = new Cnblogs.CnblogsApi();

        public List<Model.Blog> Parse(string json)
        {
            List<Model.Blog> result = new List<Model.Blog>();
            try
            {
                DavismyJsonModel model = JsonConvert.DeserializeObject<DavismyJsonModel>(json);

                foreach (var item in model.data)
                {
                    Blog m = new Blog()
                    {
                        Autor = item.author,
                        BlogId = item.blog_id,
                        BlogApp = item.blogapp,
                        CommentCount = Convert.ToInt32(item.comment),
                        Summary = item.content,
                        ViewCount = Convert.ToInt32(item.hit),
                        SendDate = item.public_time,
                        Title = item.title,
                        AutorUrl = string.Empty,
                        AutorImage = string.Empty,
                        UpdateDate = string.Empty,
                        CategoryId = string.Empty
                    };

                    //// 多线程下载
                    //Thread thread = new Thread(GetBlogContentThreadMethod);
                    //thread.Start(m);

                    result.Add(m);
                }

            }
            catch
            {
            }

            return result;
        }

        public void GetBlogContentThreadMethod(object m)
        {
            Blog model = (Blog)m;
            model.Content = contentApi.GetBlogContent(model.BlogId);

            Console.WriteLine(model.Content);
        }
    }
}