<%@ Page Language="C#" AutoEventWireup="true" Inherits="Rae.Module.Auth.UI.Admin.VerifyList" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title>认证列表</title>
    <link href="../css/bootstrap-theme.min.css" rel="stylesheet" />
    <link href="../css/bootstrap.min.css" rel="stylesheet" />
    <link href="../js/easyui/themes/default/easyui.css" rel="stylesheet" />
    <script src="../js/easyui/jquery-1.8.0.min.js"></script>
    <script src="../js/easyui/jquery.easyui.min.js"></script>
    <script type="text/javascript">
        function formatStatus(val, row, index) {
            if ("0" == val) {
                return "待审核";
            }
            else if ("1" == val) {
                return "重新提交资料中";
            }
            else if ("2" == val) {
                return "认证失败";
            }
            else if ("3" == val) {
                return "认证拒绝";
            }
            else if ("4" == val) {
                return "认证成功";
            }
            else {
                return "未知！";
            }
        }

        function formatOp(val, row, index) {
            return "<a href='viewinfo.aspx?vid=" + row.Vid + "&uid=" + row.Uid + "'>查看资料</a>";
        }

        function formatUserName(val,row,index) {
            return "<a href='viewinfo.aspx?vid=" + row.Vid + "&uid=" + row.Uid + "'>"+row.UserName+"</a>";
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container container-top-margin">
            <table id="tg" class="easyui-datagrid table table-striped" style="height: 680px; margin-bottom: 25px; width: 960px;" data-options="
                url:'VerifyList.aspx?action=list',
                rownumbers: true,
                collapsible:true,
                animate: false,
                fitColumns: true,
                method: 'get',
                idField: 'Vid',
                treeField: 'TypeId',
                loadMsg:'正在加载数据...'
            ">
                <thead>
                    <tr>
                        <th data-options="field:'UserName',formatter:formatUserName">申请人
                        </th>
                        <th data-options="field:'Status',formatter:formatStatus">认证状态
                        </th>
                        <th data-options="field:'TypeId'">认证类型
                        </th>
                        <th data-options="field:'VerfiyDate'">申请时间
                        </th>
                        <th data-options="field:'AgreeUid'">受理人
                        </th>
                        <th data-options="field:'AgreeDate'">受理时间
                        </th>
                        <th data-options="field:'Tips',width:'260'">提示信息
                        </th>
                        <th data-options="field:'undefined',formatter:formatOp">操作</th>
                    </tr>
                </thead>
            </table>
        </div>
    </form>
</body>
</html>
