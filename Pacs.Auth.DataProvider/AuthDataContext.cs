using System.Configuration;
using Pacs.Core.Data;
using Pacs.Core.Exceptions;

namespace Pacs.Auth.DataProvider
{
    internal class AuthDataContext : DataContext
    {
        public AuthDataContext()
            : base("")
        {
            ConnectionStringSettings config = ConfigurationManager.ConnectionStrings["Verify"];
            if (config == null)
            {
                throw new PacsDataException("认证平台需要Verify 数据库连接字符串，请在*.config配置Verify连接字符串！（陈睿）");
            }

            ConnectionString = config.ConnectionString;
        }
    }
}