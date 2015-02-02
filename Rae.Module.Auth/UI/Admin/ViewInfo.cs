using System;
using Rae.Module.Auth.Model;

namespace Rae.Module.Auth.UI.Admin
{
    public class ViewInfo : BasePage
    {
        private VerifyInfo _info;

        public VerifyInfo VerifyInfo
        {
            get
            {
                return _info ?? (_info = DbContext.DataProvider.GetVerifyInfo(Request["vid"]));
            }
        }


        protected override void PageLoad()
        {
            if (Request["Vid"] == null  || Request["uid"] == null)
            {
                StopResponse();
                return;
            }

            if ("post" == Request["action"])
            {
                // 更新状态
                this.VerifyInfo.AgreeUid = DbContext.DataProvider.AuthUser.Uid;
                this.VerifyInfo.AgreeDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
                this.VerifyInfo.Tips = Request["tips"];
                this.VerifyInfo.Status = (VerifyStatus)Convert.ToInt32(Request["status"]);

                bool result = DbContext.DataProvider.UpdateVerifyInfo(VerifyInfo);
                ShowMsg(result, "操作成功！", "操作失败！");
            }
        }

        public string GenerateInputHtml(TemplateJsonView m)
        {
            string html = string.Empty;
            switch (m.DataType)
            {
                case DataType.Image:
                    html = "<image src='../upload/image/" + m.Value + "' />";
                    break;
                //case DataType.DropDownList:
                //    StringBuilder sb = new StringBuilder();
                //    sb.AppendFormat("<select class='apply-textbox'>");
                //    foreach (KeyValueJsonView item in m.Items)
                //    {
                //        string selected = item.Value == m.Value ? "selected='selected'" : string.Empty;
                //        sb.AppendFormat("<option value='{1}' {2}>{0}</option>", item.Text, item.Value, selected);
                //    }
                //    sb.Append("</select>");
                //    html = sb.ToString();
                //    break;
                default:
                    html = m.Value;
                    break;
            }

            return html;
        }


        /// <summary>
        /// 生成控件的名称
        /// </summary>
        /// <returns></returns>
        public string GenerateNameHtml(TemplateJsonView m)
        {
            return m.Required ? string.Format("<span>*</span>{0}", m.Text) : m.Text;
        }

        protected string GetStatus()
        {
            string result = "";
            switch (VerifyInfo.Status)
            {
                case VerifyStatus.All:
                case VerifyStatus.Pendding:
                    result = "待审核";
                    break;
                case VerifyStatus.RePendding:
                    result = "资料重填";
                    break;
                case VerifyStatus.Success:
                    result = "成功";
                    break;
                case VerifyStatus.Faild:
                    result = "认证失败：" + VerifyInfo.Tips;
                    break;
                case VerifyStatus.Forbidden:
                    result = "禁止认证：" + VerifyInfo.Tips;
                    break;
                default:
                    result = "未知状态";
                    break;
            }
            return result;
        }
    }
}
