using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net;
using System.IO;
using System.Text;

namespace Rae.Web.Cnblogs.Http
{
    public class HttpRequest
    {
        public string Get(string url)
        {
            try
            {
                Console.WriteLine("URl:" + url);
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.CreateDefault(new Uri(url));
                request.Timeout = 3000;
             //   request.Proxy = new WebProxy("221.223.84.79", 9000); // 代理
                Stream stream = request.GetResponse().GetResponseStream();
                StreamReader reader = new StreamReader(stream);
                string result = reader.ReadToEnd();

                reader.Close();
                stream.Close();

                return result;
            }
            catch (Exception ex)
            {
                //throw ex;
                return "Http请求错误，访问不到：" + url;
            }

        }


        public string Get(string url, Dictionary<string, object> param)
        {
            // 组合参数
            // http://www.baidu.com?a=1&b=2
            StringBuilder sb = new StringBuilder(url);
            if (!url.Contains("?"))
            {
                sb.Append("?");
            }
            foreach (var item in param)
            {
                sb.AppendFormat("{0}={1}&", item.Key, item.Value);
            }


            string result = sb.ToString();
            result = result.Substring(0, result.Length - 1);
            result = Get(result);
            return result;
        }
    }
}