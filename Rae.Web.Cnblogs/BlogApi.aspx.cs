using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Rae.Web.Cnblogs
{
    public partial class BlogApi : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.ContentType = "text/json";
            Api.ICnblogsApi api = new Api.Davismy.DavismyApi();
            var result = api.GetBlogs("", 1);
            Response.Write(Api.JsonHelper.ToJson(result));


        }
    }
}