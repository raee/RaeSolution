using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Rae.Module.Auth.Model;
using Rae.Module.Auth.Option;

namespace Rae.Module.Auth.UI.Admin
{
    public class VerifyList : BasePage
    {
        protected override void PageLoad()
        {
            if (Request["action"] == "list")
            {
                // 获取所有认证类型
                List<AuthTypeModel> types = DbContext.DataProvider.GetAuthTypeModels();
                VerifyInfoOption option = new VerifyInfoOption();
                List<VerifyInfo> datas = DbContext.DataProvider.GetVerifyInfos(option);

                // 格式化认证类型
                foreach (VerifyInfo info in datas)
                {
                    foreach (var m in types)
                    {
                        if (info.TypeId == m.AuthTypeId)
                        {
                            info.TypeId = m.Name;
                            break;
                        }
                    }
                }


                StringBuilder sb = new StringBuilder();
                sb.Append("{\"total\":");
                sb.Append(datas.Count);
                sb.Append(",\"rows\":");
                sb.Append(JsonConvert.SerializeObject(datas));
                sb.Append("}");
                JsonView(sb.ToString());
            }
        }

        private void JsonView(string json)
        {
            Response.ContentType = "application/json";
            Response.Write(json);
            Response.End();
        }
    }
}
