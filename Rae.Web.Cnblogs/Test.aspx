<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Test.aspx.cs" Inherits="Rae.Web.Cnblogs.Test" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            用户名：<asp:TextBox ID="tbUserName" runat="server"></asp:TextBox>
            <br />
            密码：<asp:TextBox ID="tbPwd" runat="server"></asp:TextBox>
            <br />
            <br />
            <asp:Button ID="btnLogin" runat="server" Height="41px" OnClick="btnLogin_Click" Text="登录" Width="207px" />
            <br />
            <br />
            <asp:Label ID="Label1" runat="server" Text="Label" ForeColor="red"></asp:Label>
            <br />
            <br />
            <br />
            <asp:Label ID="lbHtml" runat="server" Text="Label"></asp:Label>
            <br />

        </div>
    </form>
</body>
</html>
