using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;

namespace Rae.Module.Auth.UI
{
    public class BasePage : Page
    {
        private VerifyDataSource _db;
        private Authentication _authentication;

        protected Authentication Authentication
        {
            get
            {
                return _authentication ?? (_authentication = AuthFactory.Authentication);
            }
        }

        protected VerifyDataSource DbContext
        {
            get
            {
                return _db ?? (_db = new VerifyDataSource(Context));
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            if (!AuthFactory.Authentication.DataProvider.IsLogin)
            {
                StopResponse("请登录后再操作！");
                return;
            }
            PageLoad();
            RouteAction(Request["action"]);
        }

        protected virtual void PageLoad() { }

        protected virtual void RouteAction(string action)
        {
        }


        protected virtual void ShowJson(string json)
        {
            Response.ContentType = "application/x-javascript";
            Response.Write(json);
            Response.End();
        }

        protected virtual void ShowMsg(bool result, string success, string error)
        {
            if (result)
            {
                string json = PageHelper.ShowJsonSuccess(success);
                ShowJson(json);
            }
            else
            {
                string json = PageHelper.ShowJsonError(error);
                ShowJson(json);
            }
        }

        public void StopResponse(string msg)
        {
            Response.Clear();
            Response.Write(msg);
            Response.End();
        }

        public void StopResponse()
        {
            StopResponse(string.Empty);
        }
    }
}
