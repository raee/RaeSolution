using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Rae.Web.Cnblogs.Api
{
    public static class JsonHelper
    {
        public static string ToJson(object obj)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.SerializeObject(obj);

            }
            catch
            {
                return ToErrorJson("Json转化失败！");
            }

        }


        public static string ToErrorJson(string msg)
        {
            var obj = new
            {
                Message = msg
            };

            return Newtonsoft.Json.JsonConvert.SerializeObject(obj);
        }
    }
}