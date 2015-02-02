using System;

namespace Rae.Module.Auth.Permission
{
    /// <summary>
    /// 权限接口
    /// </summary>
    public interface IAuthPermission
    {
        /// <summary>
        /// 认证权限
        /// <param name="uid">用户Id</param>
        /// <param name="method">当前方法</param>
        /// <param name="option">方法带的一些参数，每个方法传递的都不同，可以为空，注意判断空值和类型！</param>
        /// </summary>
        /// <returns></returns>
        bool OnAuthPermission(string uid, string method, object option);

        /// <summary>
        /// 权限发生异常时
        /// </summary>
        /// <param name="ex"></param>
        void OnExecption(Exception ex);
    }
}
