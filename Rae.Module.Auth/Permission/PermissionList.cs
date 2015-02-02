namespace Rae.Module.Auth.Permission
{
    /// <summary>
    /// 当前权限列表
    /// </summary>
    public static class PermissionList
    {


        #region 数据库操作权限

        public static readonly string GetAuthTypeModels = "GetAuthTypeModels";
        public static readonly string GetAuthTypeModel = "GetAuthTypeModel";
        public static readonly string DeleteAuthType = "DeleteAuthType";
        public static readonly string AddAuthType = "AddAuthType";
        public static readonly string UpdateAuthType = "UpdateAuthType";
        public static readonly string GetAuthInputTemplates = "GetAuthInputTemplates";
        public static readonly string GetAuthInputTemplate = "GetAuthInputTemplate";
        public static readonly string GetAuthInputValue = "GetAuthInputValue";
        public static readonly string AddOrUpdateTemplate = "AddOrUpdateTemplate";
        public static readonly string DelTemplate = "DelTemplate";
        public static readonly string InsertTemplateValue = "InsertTemplateValue";
        public static readonly string GetVerifyInfos = "GetVerifyInfos";
        public static readonly string GetVerifyInfo = "GetVerifyInfo";
        public static readonly string GetUserCurrentVerfiy = "GetUserCurrentVerfiy";
        public static readonly string InsertVerifyInfo = "InsertVerifyInfo";
        public static readonly string UpdateVerifyInfo = "UpdateVerifyInfo";

        #endregion
    }
}
