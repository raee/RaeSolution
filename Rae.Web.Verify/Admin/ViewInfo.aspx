<%@ Page Language="C#" AutoEventWireup="true" Inherits="Rae.Module.Auth.UI.Admin.ViewInfo" %>

<%@ Import Namespace="Rae.Module.Auth.UI" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>查看认证资料</title>
    <link href="../css/bootstrap-theme.min.css" rel="stylesheet" />
    <link href="../css/bootstrap.min.css" rel="stylesheet" />
    <link href="../css/verify.css" rel="stylesheet" />
    <script src="../js/jquery-1.11.2.min.js"></script>
    <script src="../js/jquery.json.js"></script>
    <script type="text/javascript">
        function postform(e) {
            var status = $(e).attr("status");
            var tips = $("#tips").val();
            $.post(window.location.href, { action: "post", status: status, tips: tips });
        }

        function _Callback(e) {
            alert(e.message);
        }
    </script>
</head>
<body>
    <div class="container container-top-margin">
        <h2 class="h2-title"><%=VerifyInfo.UserName %> - <%=GetStatus() %></h2>
        <div class="v_apply_info" id="editView">
            <table class="table apply-view">
                <% foreach (TemplateJsonView m in DbContext.TemplateJsonViews)
                   {%>
                <tr>
                    <th>
                        <%=GenerateNameHtml(m) %>：
                    </th>
                    <td>
                        <%=GenerateInputHtml(m) %>
                    </td>
                    <td>
                        <span class="tips"><%=m.Tips %></span>
                    </td>
                </tr>
                <%} %>
                <tr>
                    <td align="right">认证信息：</td>
                    <td colspan="2">
                        <textarea name="tips" id="tips" class="form-control" maxlength="2048" rows="5">
<%=VerifyInfo.Tips %>
</textarea>
                    </td>
                </tr>
                <tr>
                    <td></td>
                    <td colspan="2">
                        <input type="submit" class="btn btn-success" value="同意" status="4" onclick="postform(this)" />
                        <input type="submit" class="btn btn-warning" value="拒绝" status="2" onclick="postform(this)" />
                        <input type="submit" class="btn btn-danger" value="禁止" status="3" onclick="postform(this)" />
                        <input type="submit" class="btn btn-info" value="解禁" status="0" onclick="postform(this)" />
                        <input type="submit" class="btn btn-warning" value="信息重填" status="1" onclick="postform(this)" />
                        <a href="VerifyList.aspx" class="btn btn-default">返回</a>
                    </td>
                </tr>

            </table>
        </div>
    </div>
</body>
</html>
