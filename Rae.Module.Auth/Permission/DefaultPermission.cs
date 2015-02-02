using System;

namespace Rae.Module.Auth.Permission
{
    /// <summary>
    /// 默认权限接口，都可以通过！
    /// </summary>
    class DefaultPermission : IAuthPermission
    {
        public bool OnAuthPermission(string uid, string method, object option)
        {
            return true;
        }

        public void OnExecption(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
