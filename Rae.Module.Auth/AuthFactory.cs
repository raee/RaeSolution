using System;
using System.Collections.Generic;
using System.Text;
using Rae.Module.Log;

namespace Rae.Module.Auth
{
    /// <summary>
    /// 认证接口获取
    /// 从子类中获取
    /// </summary>
    public static class AuthFactory
    {
        private static Authentication _authentication;

        public static Authentication Authentication
        {
            get
            {
                if (_authentication == null)
                {
                    NullReferenceException ex = new NullReferenceException("认证接口Authentication 没有实现， 请通过Rae.Module.Auth.AuthFactory.InitFactory(Authentication auth) 来实现！——（陈睿）");
                    LogManager.Error(ex);
                    throw ex;
                }
                return _authentication;
            }
        }

        public static void InitFactory(Authentication auth)
        {
            _authentication = auth;
        }

    }
}
