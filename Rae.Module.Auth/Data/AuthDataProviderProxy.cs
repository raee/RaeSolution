using System;
using System.Collections.Generic;
using System.Text;
using Rae.Module.Auth.Model;
using Rae.Module.Auth.Option;
using Rae.Module.Auth.Permission;

namespace Rae.Module.Auth.Data
{
    /// <summary>
    /// 数据库代理
    /// </summary>
    public class AuthDataProviderProxy : IAuthDataProvider
    {
        private readonly IAuthDataProvider _provider;
        private readonly IAuthPermission _permission;

        public AuthDataProviderProxy(IAuthDataProvider src, IAuthPermission permission)
        {
            this._provider = src;
            this._permission = permission;
        }

        private bool AuthPermission(string method)
        {
            if (!_permission.OnAuthPermission(AuthUser.Uid, method, null))
            {
                throw new Exception("权限校验失败！--" + method);
            }
            return false;
        }

        public bool IsLogin
        {
            get { return _provider.IsLogin; }
        }

        public User AuthUser
        {
            get { return _provider.AuthUser; }
        }


        public List<AuthTypeModel> GetAuthTypeModels()
        {
            AuthPermission(PermissionList.GetAuthTypeModels);
            return _provider.GetAuthTypeModels();
        }

        public AuthTypeModel GetAuthTypeModel(string id)
        {
            AuthPermission(PermissionList.GetAuthTypeModel);
            return _provider.GetAuthTypeModel(id);
        }

        public bool DeleteAuthType(string id)
        {
            AuthPermission(PermissionList.DeleteAuthType);
            return _provider.DeleteAuthType(id);
        }

        public bool AddAuthType(AuthTypeModel m)
        {
            AuthPermission(PermissionList.AddAuthType);
            return _provider.AddAuthType(m);
        }

        public bool UpdateAuthType(AuthTypeModel m)
        {
            AuthPermission(PermissionList.UpdateAuthType);
            return _provider.UpdateAuthType(m);
        }

        public List<AuthInputTemplateModel> GetAuthInputTemplates(string uid, string id)
        {
            AuthPermission(PermissionList.GetAuthInputTemplates);
            return _provider.GetAuthInputTemplates(uid, id);
        }

        public AuthInputTemplateModel GetAuthInputTemplate(string id)
        {
            AuthPermission(PermissionList.GetAuthInputTemplate);
            return _provider.GetAuthInputTemplate(id);
        }

        public AuthInputValueModel GetAuthInputValue(string uid, string inputId)
        {
            AuthPermission(PermissionList.GetAuthInputValue);
            return _provider.GetAuthInputValue(uid, inputId);
        }

        public bool AddOrUpdateTemplate(AuthInputTemplateModel m)
        {
            AuthPermission(PermissionList.AddOrUpdateTemplate);
            return _provider.AddOrUpdateTemplate(m);
        }

        public bool DelTemplate(string typeId)
        {
            AuthPermission(PermissionList.DelTemplate);
            return _provider.DelTemplate(typeId);
        }

        public bool InsertTemplateValue(AuthInputValueModel m)
        {
            AuthPermission(PermissionList.InsertTemplateValue);
            return _provider.InsertTemplateValue(m);
        }

        public List<VerifyInfo> GetVerifyInfos(VerifyInfoOption option)
        {
            AuthPermission(PermissionList.GetVerifyInfos);
            return _provider.GetVerifyInfos(option);
        }

        public VerifyInfo GetVerifyInfo(string id)
        {
            AuthPermission(PermissionList.GetVerifyInfo);
            return _provider.GetVerifyInfo(id);
        }

        public VerifyInfo GetUserCurrentVerfiy(string uid, string typeid, VerifyStatus status)
        {
            AuthPermission(PermissionList.GetUserCurrentVerfiy);
            return _provider.GetUserCurrentVerfiy(uid, typeid, status);
        }

        public bool InsertVerifyInfo(VerifyInfo m)
        {
            AuthPermission(PermissionList.InsertVerifyInfo);
            return _provider.InsertVerifyInfo(m);
        }

        public bool UpdateVerifyInfo(VerifyInfo m)
        {
            AuthPermission(PermissionList.UpdateVerifyInfo);
            return _provider.UpdateVerifyInfo(m);
        }
    }
}
