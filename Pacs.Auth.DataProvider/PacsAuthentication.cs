using System;
using System.Collections.Generic;
using System.Text;
using Rae.Module.Auth;
using Rae.Module.Auth.Data;

namespace Pacs.Auth.DataProvider
{
    /// <summary>
    /// PACS 认证数据库提供
    /// </summary>
    public class PacsAuthentication : Authentication
    {
        public override IAuthDataProvider Provider
        {
            get { return new AuthDataProvider(AuthPermission); }
        }


    }
}
