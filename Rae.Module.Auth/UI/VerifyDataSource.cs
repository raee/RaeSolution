using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using Newtonsoft.Json;
using Rae.Module.Auth.Data;
using Rae.Module.Auth.Model;

namespace Rae.Module.Auth.UI
{
    /// <summary>
    /// 这里提供数据库接口，给统一的页面调用
    /// </summary>
    public class VerifyDataSource
    {
        private readonly IAuthDataProvider _db = AuthFactory.Authentication.DataProvider;
        private List<AuthTypeModel> _authTypeModels;
        private AuthTypeModel _authTypeModel;
        private List<AuthInputTemplateModel> _authTemplateModels;
        private List<TemplateJsonView> _templateJsonViews;

        private HttpRequest Request { get; set; }

        public VerifyDataSource(HttpContext context)
        {
            Request = context.Request;
        }

        public IAuthDataProvider DataProvider
        {
            get { return _db; }
        }

        /// <summary>
        /// 获取所有的认证类型
        /// </summary>
        public List<AuthTypeModel> AuthTypeModels
        {
            get { return _authTypeModels ?? (_authTypeModels = _db.GetAuthTypeModels()); }
        }

        /// <summary>
        /// 获取某一个认证类型
        /// 请求：id（必须）
        /// </summary>
        public AuthTypeModel AuthTypeModel
        {
            get { return _authTypeModel ?? (_authTypeModel = _db.GetAuthTypeModel(Request["id"])); }
        }
        /// <summary>
        /// 模板
        /// </summary>
        public List<AuthInputTemplateModel> AuthTemplateModels
        {
            get
            {
                // 用户ID
                string uid = Request["uid"];
                uid = uid ?? DataProvider.AuthUser.Uid;

                // 认证类型ID
                string typeId ="";
                if (Request["Vid"] != null)
                {
                    typeId = DataProvider.GetVerifyInfo(Request["Vid"].ToString()).TypeId;
                }
                else
                {
                    typeId = AuthTypeModel.AuthTypeId;
                }

                return _authTemplateModels ?? (_authTemplateModels = DataProvider.GetAuthInputTemplates(uid, typeId));
            }
        }

        /// <summary>
        /// 转换成前端可以解析的Json
        /// </summary>
        public string AuthTemplateJsonString
        {
            get
            {
                return JsonConvert.SerializeObject(TemplateJsonViews);
            }
        }

        /// <summary>
        /// 同AuthTemplateModels属性，返回的对象为AuthInputTemplateModel的子类TemplateJsonView
        /// </summary>
        public List<TemplateJsonView> TemplateJsonViews
        {
            get
            {
                if (_templateJsonViews == null)
                {
                    List<TemplateJsonView> list = new List<TemplateJsonView>();
                    foreach (var item in AuthTemplateModels)
                    {
                        TemplateJsonView m = new TemplateJsonView(item);
                        m.Items = JsonConvert.DeserializeObject<List<KeyValueJsonView>>(m.DropDownListItems);
                        m.MaxLen = m.MaxLength.ToString();
                        list.Add(m);
                    }
                    _templateJsonViews = list;
                }

                return _templateJsonViews;
            }
        }

    }
}
