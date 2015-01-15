using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Rae.Web.Cnblogs
{
    internal class ResponseContent
    {
        public HttpStatusCode StatusCode { get; set; }

        public CookieCollection Cookie { get; set; }

        public string Html { get; set; }
    }

    internal class HttpClientManger
    {
        public static ResponseContent Get(string url, string method, Dictionary<string, object> param)
        {
            var content = new ResponseContent();

            var request = (HttpWebRequest)WebRequest.CreateDefault(new Uri("http://passport.cnblogs.com/login.aspx"));
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.3; WOW64; rv:34.0) Gecko/20100101 Firefox/34.0";
            request.Method = method;

            if (param != null)
            {
                var sb = new StringBuilder();
                int i = 0;
                foreach (var item in param)
                {
                    sb.AppendFormat("{0}={1}", item.Key, HttpUtility.UrlEncode(item.Value.ToString()));
                    if (i < param.Count)
                    {
                        sb.AppendFormat("&");
                    }
                    i++;
                }
                // 写入参数
                request.GetRequestStream().Write(Encoding.UTF8.GetBytes(sb.ToString()), 0, sb.ToString().Length);
            }

            var response = (HttpWebResponse)request.GetResponse();
            content.StatusCode = response.StatusCode;
            if (content.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = response.GetResponseStream();
                if (stream != null)
                {
                    var reader = new StreamReader(stream);
                    content.Html = reader.ReadToEnd();
                    response.Close();
                }

                var cookie = response.Headers["Set-Cookie"];
                if (!string.IsNullOrEmpty(cookie))
                {
                    var items = cookie.Split(';');
                    content.Cookie = new CookieCollection();
                    foreach (var item in items)
                    {
                        var keyvalue = item.Split('=');
                        if (keyvalue.Length != 2)
                        {
                            continue;
                        }

                        content.Cookie.Add(new Cookie(HttpUtility.HtmlEncode(keyvalue[0]), HttpUtility.HtmlEncode(keyvalue[1])));
                    }
                }

                content.Cookie = response.Cookies;
            }
            return content;
        }
    }

    public partial class Test : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {

        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string username = tbUserName.Text.Trim();
            string pwd = tbPwd.Text.Trim();

            string url = "http://passport.cnblogs.com/login.aspx";
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("tbUserName", username);
            dic.Add("tbPassword", pwd);
            var response = HttpClientManger.Get(url, "POST", dic);
            Show(response.Html);

        }

        private void Show(object msg)
        {
            Label1.Text = msg.ToString();
        }
    }
}