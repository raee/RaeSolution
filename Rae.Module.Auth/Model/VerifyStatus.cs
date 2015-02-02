using System;
using System.Collections.Generic;
using System.Text;

namespace Rae.Module.Auth.Model
{
    /// <summary>
    /// 认证状态
    /// </summary>
    public enum VerifyStatus
    {
        /// <summary>
        /// 所有状态
        /// </summary>
        All = -1,

        /// <summary>
        /// 申请中
        /// </summary>
        Pendding = 0,

        /// <summary>
        /// 重新申请中
        /// 上传资料、填写信息出错时
        /// </summary>
        RePendding = 1,

        /// <summary>
        /// 认证失败
        /// </summary>
        Faild = 2,

        /// <summary>
        /// 认证拒绝
        /// 恶意申请
        /// </summary>
        Forbidden = 3,

        /// <summary>
        /// 认证成功！
        /// </summary>
        Success = 4,

    }
}
