using System;
using System.Collections.Generic;
using System.Text;
using Rae.Module.Auth;
using Rae.Module.Auth.Data;
using Rae.Module.Auth.Model;
using Rae.Module.Auth.Permission;

namespace Pacs.Auth.DataProvider
{
    partial class AuthDataProvider : IAuthDataProvider
    {
        private AuthDataContext context = new AuthDataContext();
        private User user;
        protected IAuthPermission permission;


        public AuthDataProvider(IAuthPermission per)
        {
            this.permission = per;
            user = new User();
            user.Uid = "SYSTEM";
            user.Name = "开发者";
            user.UserName = "开发者";
            user.Password = "d9ell8";
        }

        private bool OnAuthPermission(string method)
        {
            if (!permission.OnAuthPermission(AuthUser.Uid, method, null))
            {
                throw new Exception(string.Format("您没有权限操作！-[{0}]", method));
            }
            return true;
        }

        public bool IsLogin
        {
            get
            {
                // TODO:开发时暂时为空，实际用是实现。
                return true;
            }
        }

        public User AuthUser
        {
            get
            {
                // TODO:开发时赋值。
                return user;
            }
        }
    }
}
