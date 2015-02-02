<%@ Page Language="C#" AutoEventWireup="true" Inherits="Rae.Module.Auth.UI.Apply" %>

<%@ Import Namespace="Rae.Module.Auth.Model" %>
<%@ Import Namespace="Rae.Module.Auth.UI" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>申请官方认证</title>
    <link href="../css/bootstrap-theme.min.css" rel="stylesheet" />
    <link href="../css/bootstrap.min.css" rel="stylesheet" />
    <link href="../css/verify.css" rel="stylesheet" />
    <style type="text/css">
     
    </style>
    <script src="../js/jquery-1.11.2.min.js"></script>
    <script src="../js/jquery.json.js"></script>
    <script src="../js/bootstrap.min.js"></script>
    <script src="../js/sweet-alert.min.js"></script>
    <script src="../js/verify.js"></script>

</head>
<body>
    <div class="container container-top-margin">

        <div class="W_main_cont" id="pl_home_index">
            <% if (Request["action"] == null)
               { %>

            <img src="../images/img_vcert3.jpg" />
            <div class="txt_alerWap" style="font-size: 13px;">
                微博<span style="color: #FF9900;">官方认证</span>是完全免费的，任何收费的认证行为都是虚假欺骗的。<span style="color: #888;">全球其他地区的机构申请认证可能需要承担一定的认证服务费用（材料验证费用）。费用标准视微博业务发展状况和当地具体情况而定。</span>
            </div>
            <div class="cut_line_cz_2"></div>
            <ul class="v_certUls clearfix">
                <% foreach (AuthTypeModel m in AuthTypeModels)
                   { %>
                <li>
                    <div class="imgWp_vcert">
                        <img src="<%= m.ImageUrl %>" alt="">
                    </div>
                    <div class="inf_vcert">
                        <p class="tit_name"><b><%= m.Name %>：</b></p>
                        <p>
                            <span class="gray6"><%= m.Comment %></span>
                        </p>
                        <p><a class="btn btn-success" href="?action=apply&id=<%= m.AuthTypeId %>">立即申请</a></p>
                    </div>
                </li>

                <% } %>
            </ul>

            <div class="">微博已为港澳台及海外用户开通线上申请功能，如在申请中遇到问题，请私信<a href="http://weibo.com/weibokefu" target="_blank">@微博客服。</a></div>

            <% }
               else
               { %>


            <div class="verify_pagetit">
                <%=DbContext.AuthTypeModel.Name %>
                <span class="tit_redtxt"></span>
            </div>
            <div id="pl_apply_person">
            </div>
            <div id="pl_apply_government">
            </div>
            <div id="pl_apply_enterprise">
                <ol class="v_apply_list vAppLs_zt step2 %>">
                    <li>1.选择认证类型</li>
                    <li class="here">2.填写认证信息</li>
                    <li class="next">3.审核认证信息</li>
                    <li class="next">4.认证结果</li>
                </ol>
                <form id="Form1" runat="server" enctype="multipart/form-data">
                    <div class="v_apply_info" id="editView">
                        <table class="table apply-view">
                            <tr>
                                <td colspan="3">
                                    <% if (!string.IsNullOrEmpty(ErrorMsg))
                                       { %>
                                    <span class="alert alert-danger"><%= ErrorMsg %></span>
                                    <% } %>
                                </td>
                            </tr>
                            <% foreach (TemplateJsonView m in TemplateJsonViews)
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
                                <td></td>
                                <td colspan="2">
                                    <input type="submit" class="btn btn-success" value="提交认证" onclick="return onApplySubmit(this);" />
                                    <a href="/Auth/Apply.aspx" class="btn btn-default">取消</a>
                                </td>
                            </tr>

                        </table>

                    </div>

                    <script type="text/javascript">
                        function onApplySubmit(e) {
                            var result = false;
                            $("#editView tr td *[apply]").each(function (i, val) {
                                var requried = $(val).attr("required");
                                if (requried && $(val).val() == "") {
                                    // alert("必填！");
                                }
                            });
                            return true;
                        }
                    </script>

                </form>

            </div>
            <% } %>
        </div>


    </div>
</body>
</html>
