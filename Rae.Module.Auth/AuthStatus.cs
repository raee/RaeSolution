namespace Rae.Module.Auth
{
    /// <summary>
    ///     认证状态
    /// </summary>
    public enum AuthStatus
    {
        /// <summary>
        ///     认证成功，通过认证。
        /// </summary>
        Ok,

        /// <summary>
        ///     待审核，或者审核中。
        /// </summary>
        Pending,

        /// <summary>
        ///     二次认证，当信息填写有误时。
        /// </summary>
        Second,

        /// <summary>
        ///     拒绝申请，拒绝后不能发起本次申请。
        /// </summary>
        Forbidden
    }
}