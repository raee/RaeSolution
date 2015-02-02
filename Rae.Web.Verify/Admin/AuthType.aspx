<%@ Page Language="C#" AutoEventWireup="true" Inherits="Rae.Module.Auth.UI.Admin.AuthTypePage" %>

<%@ Import Namespace="Rae.Module.Auth.Model" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>认证类型管理</title>
    <link href="../css/bootstrap.min.css" rel="stylesheet" />
    <link href="../css/bootstrap-theme.min.css" rel="stylesheet" />
    <link href="../css/sweet-alert.css" rel="stylesheet" />
    <link href="../css/verify.css" rel="stylesheet" />
    <script src="../js/sweet-alert.min.js"></script>
    <script src="../js/jquery-1.11.2.min.js"></script>
    <script src="../js/bootstrap.min.js"></script>
    <script src="../js/verify.js"></script>
    <script>
        $(function () {
            $("#myTab a").click(function (e) {
                e.preventDefault();
                $(this).tab('show');
            });

            // 选择TAB页
            var hash = window.location.hash;
            if (hash) {
                $("#myTab a[href='" + hash + "']").tab('show');
                if (hash == "#edit") {
                    $("#updateAuthType").show();
                    $("#myTab a[href='#edit']").html("更新认证类型");
                } else {
                    $("#updateAuthType").hide();
                }
            } else {
                $("#updateAuthType").hide();
            }
        });
    </script>
</head>
<body>
    <div class="container container-top-margin">
        <ul class="nav nav-tabs nav-justified" role="tablist" id="myTab">
            <li role="presentation" class="active"><a href="#view">认证类型</a></li>
            <li role="presentation"><a href="#edit">添加认证类型</a></li>
        </ul>
        <div class="tab-content">
            <div id="view" role="tabpanel" class="tab-pane active">
                <table class="table table-striped">
                    <thead>
                        <tr>
                            <th>认证类型</th>
                            <th>认证说明</th>
                            <th>认证图标</th>
                            <th>操作</th>
                        </tr>
                    </thead>

                    <tbody>
                        <% foreach (AuthTypeModel m in AuthTypeModels)
                           { %>
                        <tr>
                            <td><a href="?action=edit&id=<%=m.AuthTypeId %>#edit" title="修改"><%= m.Name %></a></td>
                            <td><a href="?action=edit&id=<%=m.AuthTypeId %>#edit" title="修改"><%= m.Comment %></a></td>
                            <td>
                                <img src="<%= m.Icon %>" /></td>
                            <td>
                                <a href="javascript:void(0);" id="<%=m.AuthTypeId %>" onclick="Verify.Db.VerifyType.del(this);" style="margin-right: 15px;">删除</a>
                                <a href="Template.aspx?id=<%=m.AuthTypeId %>">编辑模板</a>
                            </td>
                        </tr>
                        <% } %>
                    </tbody>
                </table>
            </div>
            <div role="tabpanel" class="tab-pane" id="edit">
                <table class="table table-striped">
                    <tr>
                        <td>认证类型</td>
                        <td>
                            <input id="name" type="text" class="form-control" value="<%=AuthTypeModel==null?"":AuthTypeModel.Name %>" /></td>
                    </tr>
                    <tr>
                        <td>认证说明</td>
                        <td>
                            <input id="comments" type="text" class="form-control" value="<%=AuthTypeModel==null?"":AuthTypeModel.Comment %>" /></td>
                    </tr>
                    <tr>
                        <td>认证图标</td>
                        <td>
                            <img src="../images/transparent.gif" />(目前默认)</td>
                    </tr>
                    <tr>
                        <td colspan="2" align="right">
                            <input type="button" class="btn btn-default" value="添加认证" id="addAuthType"
                                onclick="Verify.Db.VerifyType.add(this)" />
                            <input type="button" class="btn btn-success" value="保存" id="updateAuthType" typeid="<%=AuthTypeModel==null?"":AuthTypeModel.AuthTypeId %>"
                                onclick="Verify.Db.VerifyType.update(this)" />
                        </td>
                    </tr>
                </table>
            </div>
        </div>
    </div>
</body>
</html>
