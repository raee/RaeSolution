<%@ Page Language="C#" AutoEventWireup="true" Inherits="Rae.Module.Auth.UI.Admin.TemplatePage" %>

<%@ Import Namespace="Rae.Module.Auth.Model" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>模板编辑</title>
    <link href="../css/bootstrap.min.css" rel="stylesheet" />
    <link href="../css/verify.css" rel="stylesheet" />
    <link href="../css/sweet-alert.css" rel="stylesheet" />
    <script src="../js/jquery-1.11.2.min.js"></script>
    <script src="../js/jquery.json.js"></script>
    <script src="../js/bootstrap.min.js"></script>
    <script src="../js/sweet-alert.min.js"></script>
    <script src="../js/verify.js"></script>
    <script type="text/javascript">

        $(function () {

            // 初始化认证类型
            Verify.Template.typeId = '<%=AuthTypeModel.AuthTypeId %>';
            Verify.Template.init($("#editView table"), '<%=AuthTemplateJsonString %>');

            if ($("#ctrlType").val() == 3) {
                $(".ddl").show();
            }
        });

        function addItem() {
            var text = $("#ddlText").val();
            var val = $("#ddlValue").val();
            var e = $("#ddl");
            Verify.Template.addItem(e, text, val);
        }

        // 清空下拉列表
        function clearItems() {
            $("#ddl").children("option").remove();
        }

        function onCtrlTypeChange(e) {
            var val = $(e).val();
            if (val == 3) { // 显示下拉列表
                $(".ddl").show();
            } else {
                $(".ddl").hide();
            }
        }

        function onTextChange(e) {
            $("#ddlValue").val($(e).val());
        }

        function addEditView() {
            Verify.Template.addToEditView($("#editView table"));
        }

    </script>
</head>
<body>
    <div class="container container-top-margin">

        <table class="table table-striped">
            <thead>
                <tr>
                    <td class="h2-title" colspan="3">
                        <h2><%=AuthTypeModel.Name %></h2>
                    </td>
                    <td valign="middle">
                        <a href="AuthType.aspx">返回认证列表</a>
                    </td>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td>显示名称</td>
                    <td>
                        <input type="text" class="form-control" id="name" placeholder="名称" /></td>
                    <td>
                        <div class="checkbox">
                            <label class="tips">
                                <input type="checkbox" id="requried" />
                                必填
                            </label>
                        </div>
                    </td>
                    <td>
                        <input type="button" class="btn btn-success text-line" id="add" value="添加字段" onclick="addEditView();" />
                        <input type="button" class="btn btn-warning text-line" id="save" value="保存模板" onclick="Verify.Template.save();" /></td>
                </tr>
                <tr>
                    <td>控件类型</td>
                    <td>
                        <select class="dropdown form-control" id="ctrlType" onchange="onCtrlTypeChange(this);">
                            <option value="0" selected="selected">普通文本</option>
                            <option value="1">长文本</option>
                            <option value="2">图片</option>
                            <option value="3">下拉列表</option>
                        </select></td>
                    <td colspan="2">
                        <div class="ddl">
                            <select class="form-control" id="ddl">
                                <option value="">-请在下面添加下拉项目-</option>
                            </select>
                        </div>
                    </td>
                </tr>
                <!--下拉列表选项 -->
                <tr class="alert alert-info ddl">
                    <td></td>
                    <td></td>
                    <td colspan="2">
                        <input type="text" class="form-control text-line" id="ddlText" placeholder="显示文本" style="width: 120px" onchange="onTextChange(this);" onclick="this.value = '';" />
                        <input type="text" class="form-control text-line" id="ddlValue" placeholder="取值" style="width: 120px" />
                        <input type="button" id="addDropDownList" class="btn btn-info btn-sm" value="添加" onclick="addItem()" />
                        <input type="button" class="btn btn-info btn-sm" value="清空项" onclick="clearItems();" />
                    </td>
                </tr>

                <!--结束下拉列表选项 -->
                <tr>
                    <td>最大长度</td>
                    <td>
                        <input type="number" class="form-control" value="128" placeholder="最大长度" id="maxLength" style="width: 120px;" /></td>
                    <td colspan="2">
                        <span class="tips">文本类型：最大字符长度，图片类型：图片允许上传的大小，单位：字节（1M=1024字节）。</span></td>
                </tr>
                <tr>
                    <td>申请时提示的信息</td>
                    <td colspan="3">
                        <textarea class="form-control" placeholder="申请时提示的信息" id="tipsMsg"></textarea></td>
                </tr>

            </tbody>
        </table>

        <div id="editView">
            <table class="table">
              
            </table>
        </div>
    </div>
</body>
</html>
