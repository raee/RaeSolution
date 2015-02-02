using System.Collections.Generic;
using Rae.Module.Auth.Model;
using Rae.Module.Auth.Option;

namespace Rae.Module.Auth.Data
{
    /// <summary>
    ///     数据提供者
    /// </summary>
    public interface IAuthDataProvider
    {
        #region 用户接口

        /// <summary>
        ///     当前用户是否已经登录
        /// </summary>
        /// <returns></returns>
        bool IsLogin { get; }

        /// <summary>
        ///     获取已经验证的用户
        /// </summary>
        User AuthUser { get; }

        #endregion

        #region 认证类型接口

        /// <summary>
        ///     获取所有认证类型
        /// </summary>
        /// <returns></returns>
        List<AuthTypeModel> GetAuthTypeModels();


        /// <summary>
        ///     根据主键获取认证类型
        /// </summary>
        /// <param name="id">主键Id</param>
        /// <returns></returns>
        AuthTypeModel GetAuthTypeModel(string id);

        /// <summary>
        ///     删除认证类型
        /// </summary>
        /// <param name="id">认证类型Id</param>
        /// <returns></returns>
        bool DeleteAuthType(string id);

        /// <summary>
        ///     添加认证类型
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        bool AddAuthType(AuthTypeModel m);

        /// <summary>
        ///     添加认证类型
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        bool UpdateAuthType(AuthTypeModel m);

        #endregion

        #region 控件模板

        /// <summary>
        ///     获取认证类型模板
        /// </summary>
        /// <param name="uid">用户Id，用户获取控件的值。为空则不获取。</param>
        /// <param name="id">认证类型Id</param>
        /// <returns></returns>
        List<AuthInputTemplateModel> GetAuthInputTemplates(string uid, string id);

        /// <summary>
        ///     获取一条认证类型模板
        /// </summary>
        /// <param name="id">主键Id</param>
        /// <returns></returns>
        AuthInputTemplateModel GetAuthInputTemplate(string id);

        /// <summary>
        ///     获取认证类型模板控制的值
        /// </summary>
        /// <param name="uid">用户Id</param>
        /// <param name="inputId">模板控件Id</param>
        /// <returns></returns>
        AuthInputValueModel GetAuthInputValue(string uid, string inputId);

        /// <summary>
        ///     添加或更新模板
        ///     模板不存在时添加。
        /// </summary>
        /// <param name="m">实体</param>
        /// <returns></returns>
        bool AddOrUpdateTemplate(AuthInputTemplateModel m);

        /// <summary>
        ///     删除模板
        ///     删除认证类型时会删除对应的模板。
        /// </summary>
        /// <param name="typeId">认证类型Id</param>
        /// <returns></returns>
        bool DelTemplate(string typeId);

        /// <summary>
        ///     把模板的值写入
        /// </summary>
        /// <param name="m">值</param>
        /// <returns></returns>
        bool InsertTemplateValue(AuthInputValueModel m);

        #endregion

        #region 申请信息

        List<VerifyInfo> GetVerifyInfos(VerifyInfoOption option);

        VerifyInfo GetVerifyInfo(string id);

        /// <summary>
        ///     获取当前用户认证申请
        /// </summary>
        /// <param name="uid">用户Id</param>
        /// <param name="typeid">认证类型</param>
        /// <param name="status">申请状态</param>
        /// <returns></returns>
        VerifyInfo GetUserCurrentVerfiy(string uid, string typeid, VerifyStatus status);

        bool InsertVerifyInfo(VerifyInfo m);

        bool UpdateVerifyInfo(VerifyInfo m);

        #endregion
    }
}