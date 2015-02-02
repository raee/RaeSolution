using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Web;
using Rae.Module.Auth.Model;

namespace Rae.Module.Auth.UI
{
    public class Apply : BasePage
    {
        // 允许上传的类型
        private List<string> UploadType;
        public string ErrorMsg { get; set; }

        public List<AuthTypeModel> AuthTypeModels
        {
            get { return DbContext.AuthTypeModels; }
        }

        public AuthTypeModel AuthTypeModel
        {
            get { return DbContext.AuthTypeModel; }
        }

        public List<TemplateJsonView> TemplateJsonViews
        {
            get
            {
                return DbContext.TemplateJsonViews;
            }
        }

        public Apply()
        {
            UploadType = new List<string>();
            UploadType.Add("image/jpg");
            UploadType.Add("image/png");
            UploadType.Add("image/bmp");
        }

        protected override void PageLoad()
        {
            // 流程状态：获取该用户该类型下有没有正在审核的申请。
            if (AuthTypeModel != null)
            {
                string id = Request["uid"] == null ? DbContext.DataProvider.AuthUser.Uid : Request["uid"].ToString();
                VerifyInfo info = DbContext.DataProvider.GetUserCurrentVerfiy(id,
                    AuthTypeModel.AuthTypeId,
                    VerifyStatus.All);

                if (info != null)
                {
                    switch (info.Status)
                    {
                        case VerifyStatus.All:
                            break;
                        case VerifyStatus.Pendding:
                            RedirectSetup();
                            break;
                        case VerifyStatus.RePendding:
                            ShowError(info.Tips); // 再次申请错误信息
                            break;
                        case VerifyStatus.Success:
                        case VerifyStatus.Faild:
                        case VerifyStatus.Forbidden:
                            RedirectSetup();
                            ErrorMsg = info.Tips;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
            RouteInsertTemplate();
        }

        /// <summary>
        /// 生成输入控件的Html
        /// </summary>
        public virtual string GenerateInputHtml(TemplateJsonView m)
        {
            string html = string.Empty;
            m.MaxLength = m.MaxLength <= 0 ? 256 : m.MaxLength; //小于0默认为256长度。
            string comm = string.Format("maxlength='{0}' id='{1}' name='{1}'  apply=1 ", m.MaxLength, m.Name);
            if (m.Required)
            {
                comm += "required='1'";
            }

            switch (m.DataType)
            {
                case DataType.Text:
                    html = string.Format("<input type='text' class='apply-textbox' value='{1}' {0} /> ", comm, m.Value);
                    break;
                case DataType.MultiLineText:
                    html = string.Format("<textarea class='apply-textbox' {0}>{1}</textarea>", comm, m.Value);
                    break;
                case DataType.Image:
                    html = string.Format("<input type='file' class='apply-textbox'  {0}  accept='.png,.jpg,.bmp'/>{1}", comm, m.Value);
                    break;
                case DataType.DropDownList:
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("<select class='apply-textbox'   {0}>", comm);
                    foreach (KeyValueJsonView item in m.Items)
                    {
                        string selected = item.Value == m.Value ? "selected='selected'" : string.Empty;
                        sb.AppendFormat("<option value='{1}' {2}>{0}</option>", item.Text, item.Value, selected);
                    }
                    sb.Append("</select>");
                    html = sb.ToString();
                    break;
                default:
                    html = "<span>控件异常，不存在该类型的控件。</span>";
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

        private void RouteInsertTemplate()
        {
            if (!IsPostBack)
            {
                return;
            }
            if (CheckFormValue()) return; // 不通过检验，结束！
            if (!CheckFile()) return;
            SaveSetup();
        }

        // 保存流程
        private void SaveSetup()
        {
            VerifyInfo m = new VerifyInfo();
            m.Status = VerifyStatus.Pendding;
            m.TypeId = AuthTypeModel.AuthTypeId;
            m.VerfiyDate = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss");
            bool result = DbContext.DataProvider.InsertVerifyInfo(m);
            if (result)
            {
                // 跳转到步骤3
                RedirectSetup();
            }
            else
            {
                ShowError("申请失败，服务错误[-保存流程-]！");
            }
        }

        // 保存上传文件
        private bool CheckFile()
        {
            if (Request.Files.Count <= 0)
            {
                return true; //没有上传的文件
            }

            for (int i = 0; i < Request.Files.Count; i++)
            {
                HttpPostedFile file = Request.Files[i];
                string key = Request.Files.GetKey(i);

                // 过滤文件类型
                if (!UploadType.Contains(file.ContentType))
                {
                    ShowError("不允许上传文件类型，只支持图像文件jpg,png,bmp！");
                    return false;
                }

                foreach (TemplateJsonView m in TemplateJsonViews)
                {
                    if (m.Name == key)
                    {
                        if (file.ContentLength >= (m.MaxLength * 1024))
                        {
                            ShowError(string.Format("{0}文件大小超过限制：{1}k", m.Text, m.MaxLength));
                            return false;
                        }
                        // 保存文件
                        string path = Server.MapPath("~/upload/image");
                        string filename = Guid.NewGuid() + ".jpg";
                        PageHelper.CheckAndCreateDir(path);
                        path = path + "/" + filename;
                        file.SaveAs(path);

                        // 插入到数据库中
                        AuthInputValueModel model = new AuthInputValueModel();
                        model.InputId = m.Mid;
                        model.Value = filename;
                        bool result = DbContext.DataProvider.InsertTemplateValue(model);
                        if (!result)
                        {
                            ShowError("申请失败，服务错误！");
                            return false;
                        }
                        return true;
                    }
                }
            }
            return false;
        }

        // 保存表单值
        private bool CheckFormValue()
        {
            // 只获取我们的字段
            foreach (TemplateJsonView m in TemplateJsonViews)
            {
                for (int i = 0; i < Request.Form.Count; i++)
                {
                    string key = Request.Form.GetKey(i);
                    string value = Request.Form[i];

                    if (m.Name == key)
                    {
                        // 为空检验
                        if (m.Required && string.IsNullOrEmpty(value))
                        {
                            ShowError(m.Text + "不能为空，请填写内容！");
                            return true;
                        }

                        // 字符串长度校验
                        if (m.DataType != DataType.Image && value.Length > m.MaxLength)
                        {
                            ShowError(m.Text + "长度超出范围：" + m.MaxLength + "请重新填写！");
                            return true;
                        }

                        // 插入到数据库中
                        AuthInputValueModel model = new AuthInputValueModel();
                        model.InputId = m.Mid;
                        model.Value = value;
                        bool result = DbContext.DataProvider.InsertTemplateValue(model);
                        if (!result)
                        {
                            ShowError("申请失败，服务错误！");
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void ShowError(string msg)
        {
            ErrorMsg = msg;
        }

        private void RedirectSetup()
        {
            Response.Redirect("~/auth/step.aspx" + "?action=apply&id=" + AuthTypeModel.AuthTypeId);
        }

    }
}
