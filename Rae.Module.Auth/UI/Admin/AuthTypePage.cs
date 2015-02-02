using System.Collections.Generic;
using Rae.Module.Auth.Model;

namespace Rae.Module.Auth.UI.Admin
{
    /// <summary>
    /// 认证类型编辑页
    /// </summary>
    public class AuthTypePage : BasePage
    {

        protected override void RouteAction(string action)
        {
            // 删除认证类型
            if (!IsPostBack && "del".Equals(action) && Request["id"] != null)
            {
                bool result = DbContext.DataProvider.DeleteAuthType(Request["id"]);
                ShowMsg(result, "删除成功！", "删除失败！");
            }

            // 添加类型
            if (!IsPostBack && "add".Equals(action) && Request["name"] != null)
            {
                AuthTypeModel m = new AuthTypeModel();
                m.Name = Request["name"];
                m.Comment = Request["comments"];
                bool result = DbContext.DataProvider.AddAuthType(m);
                ShowMsg(result, "添加成功！", "添加失败");
            }

            // 更新类型
            if (!IsPostBack && "update".Equals(action) && Request["id"] != null)
            {
                AuthTypeModel m = new AuthTypeModel();
                m.Name = Request["name"];
                m.Comment = Request["comments"];
                m.AuthTypeId = Request["id"];
                bool result = DbContext.DataProvider.UpdateAuthType(m);
                ShowMsg(result, "修改成功！", "修改失败！");
            }
        }


        /// <summary>
        /// 获取所有的认证类型
        /// </summary>
        public List<AuthTypeModel> AuthTypeModels
        {
            get { return DbContext.AuthTypeModels; }
        }

        /// <summary>
        /// 获取某一个认证类型
        /// 请求：id（必须）
        /// </summary>
        public AuthTypeModel AuthTypeModel
        {
            get { return DbContext.AuthTypeModel; }
        }
    }
}
