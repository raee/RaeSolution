using System;
using Rae.Module.Auth.Model;

namespace Rae.Module.Auth.UI
{
    public class Step : BasePage
    {
        private int _step;
        private VerifyInfo _verifyInfo;

        public VerifyInfo VerifyInfo
        {
            get
            {
                if (DbContext.AuthTypeModel == null)
                {
                    EndResponse();
                    return null;
                }

                if (_verifyInfo == null)
                {
                    _verifyInfo = DbContext.DataProvider.GetUserCurrentVerfiy(DbContext.DataProvider.AuthUser.Uid,
                        DbContext.AuthTypeModel.AuthTypeId,
                        VerifyStatus.All);
                }
                return _verifyInfo;
            }
        }

        public int Index
        {
            get { return _step; }
            set { _step = value; }
        }

        protected override void PageLoad()
        {
            if (VerifyInfo == null)
            {
                Response.Redirect("~/auth/apply.aspx");
                EndResponse();
                return;
            }

            switch (VerifyInfo.Status)
            {
                case VerifyStatus.All:
                case VerifyStatus.Pendding:
                    Index = 3;
                    break;
                case VerifyStatus.RePendding:
                case VerifyStatus.Success:
                case VerifyStatus.Faild:
                case VerifyStatus.Forbidden:
                    Index = 4;
                    break;
                default:
                    break;
            }
        }

        private void EndResponse()
        {
            Response.Clear();
            Response.Write("Error： Forbidden！");
            Response.End();
        }

        protected string GetMessage()
        {
            string msg = "";
            switch (VerifyInfo.Status)
            {
                case VerifyStatus.RePendding:
                    msg = "<p class='tips_title'>认证资料有误！</p><p>您填写的资料有以下问题，请<a href='/auth/apply.aspx?action=apply&id=" + VerifyInfo.TypeId + "'>重新填写；</a></p><p>" + VerifyInfo.Tips + "</p>";
                    break;
                case VerifyStatus.Success:
                    msg = "<p class='tips_title'>认证申请成功！</p><p>申请成功后，该认证类型不需要重新认证了~！</p>";
                    break;
                case VerifyStatus.Faild:
                    msg = "<p  class='tips_title'>认证失败！原因：</p><p>" + VerifyInfo.Tips + "</p>";
                    break;
                case VerifyStatus.Forbidden:
                    msg = "<p  class='tips_title'>认证被拒绝</><p>您不能再申请：" + DbContext.AuthTypeModel.Name + "，请联系管理解锁该帐号限制！</p>";
                    break;
                default:
                    EndResponse();
                    break;
            }

            return msg;
        }
    }
}