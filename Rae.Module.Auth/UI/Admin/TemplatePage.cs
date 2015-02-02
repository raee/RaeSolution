using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Rae.Module.Auth.Model;

namespace Rae.Module.Auth.UI.Admin
{
    /// <summary>
    /// 认证模板编辑页
    /// </summary>
    public class TemplatePage : BasePage
    {


        protected AuthTypeModel AuthTypeModel
        {
            get { return DbContext.AuthTypeModel; }
        }

        /// <summary>
        /// 模板
        /// </summary>
        public List<AuthInputTemplateModel> AuthTemplateModels
        {
            get { return DbContext.AuthTemplateModels; }
        }


        /// <summary>
        /// 转换成前端可以解析的Json
        /// </summary>
        public string AuthTemplateJsonString
        {
            get { return DbContext.AuthTemplateJsonString; }
        }


        protected override void PageLoad()
        {

            if (AuthTypeModel == null && string.IsNullOrEmpty(Request["action"]))
            {
                Response.Clear();
                Response.Write("错误的请求！");
                Response.End();
            }

            Title = string.Format("编辑{0}模版", AuthTypeModel == null ? "" : AuthTypeModel.Name);
        }

        /// <summary>
        /// 路由处理
        /// </summary>
        protected override void RouteAction(string action)
        {
            string json = Request["value"];

            // 添加模板
            if (action == "add" && !string.IsNullOrEmpty(json))
            {
                AddTemplate(json);
            }
        }

        /// <summary>
        /// 添加模板
        /// </summary>
        /// <param name="json"></param>
        private void AddTemplate(string json)
        {
            List<TemplateJsonView> obj = JsonConvert.DeserializeObject<List<TemplateJsonView>>(json);

            // 先删除所有数据
            if (obj.Count > 0)
            {
                DbContext.DataProvider.DelTemplate(Request["typeId"]);
            }

            // 转换对象
            foreach (TemplateJsonView jv in obj)
            {
                AuthInputTemplateModel m = new AuthInputTemplateModel(jv);
                m.DataType = (DataType)jv.DataType;
                m.DropDownListItems = JsonConvert.SerializeObject(jv.Items);
                m.MaxLength = Convert.ToInt32(jv.MaxLen);
                m.Name = PageHelper.ConvertToPinYin(jv.Text); // 转成拼音
                m.TypeId = Request["typeId"];
                bool result = DbContext.DataProvider.AddOrUpdateTemplate(m);
                if (!result)
                {
                    ShowMsg(false, "", "添加模板失败，在：" + m.Text);
                    return;
                }
            }

            ShowMsg(true, "模板保存成功！", "");
        }

       
    }
}
