namespace Rae.Module.Auth
{
    /// <summary>
    ///     认证回调接口
    ///     用于查询认证信息
    /// </summary>
    public interface IAuthCallback
    {
        /// <summary>
        ///     查询是否已经认证成功。
        /// </summary>
        /// <param name="id">认证唯一Id</param>
        /// <returns>认证状态</returns>
        AuthStatus CheckAuthed(string id);
    }
}