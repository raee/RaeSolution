<%@ Page Language="C#" AutoEventWireup="true" Inherits="Rae.Module.Auth.UI.Step" %>
<%@ Import Namespace="Rae.Module.Auth.Model" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>申请流程</title>
    <link href="../css/bootstrap.min.css" rel="stylesheet" />
    <link href="../css/verify.css" rel="stylesheet" />
</head>
<body>
    <div class="container container-top-margin">
        <div class="W_main_cont ">
            <div class="verify_pagetit">
                <%=DbContext.AuthTypeModel.Name %>
                <span class="tit_redtxt"></span>
            </div>
            <div id="pl_apply_person">
            </div>
            <div id="pl_apply_government">
            </div>
            <div id="pl_apply_enterprise">
                <ol class="v_apply_list vAppLs_zt <%=Index==0?"step2":"step"+Index%>">
                    <li>1.选择认证类型</li>
                    <li>2.填写认证信息</li>
                    <li class="<%=Index==3?"here":Index>3?"":"next" %>">3.审核认证信息</li>
                    <li class="<%=Index==4?"here":Index>4?"":"next" %>">4.认证结果</li>
                </ol>

                <% if (Index == 3)
                   { %>
                <div>
                    您提交的资料正在审核中，1-3个工作日内完成，请耐心等待。
                </div>
                <% }
                   else if (Index == 4)
                   { %>
                <div>
                    <% if (VerifyInfo.Status == VerifyStatus.Success)
                       { %>
                    
                    <img src="../images/icon_success.png" />
                    <% }
                       else
                       { %>
                    
                    <img src="../images/icon_info.png" />
                    <% } %>
                    <%=GetMessage() %>
                </div>
                <% }
                   else
                   { %>
                <div class="alert alert-info">错误的请求！</div>
                <% } %>
            </div>
        </div>
    </div>
</body>
</html>
