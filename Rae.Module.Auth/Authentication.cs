using Rae.Module.Auth.Data;
using Rae.Module.Auth.Permission;

namespace Rae.Module.Auth
{
    /// <summary>
    ///     认证类
    /// </summary>
    public abstract class Authentication
    {
        private IAuthPermission _authPermission;

        /// <summary>
        ///     数据库提供
        /// </summary>
        public abstract IAuthDataProvider Provider { get; }

        public IAuthDataProvider DataProvider
        {
            get
            {
                return new AuthDataProviderProxy(DataProvider, AuthPermission);
            }
        }

        /// <summary>
        /// 权限接口
        /// </summary>
        public virtual IAuthPermission AuthPermission
        {
            get
            {
                return _authPermission ?? (_authPermission = new DefaultPermission());
            }
        }
    }
}