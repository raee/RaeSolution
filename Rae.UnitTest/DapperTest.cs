using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using NUnit.Framework;
using Rae.Data.Dapper;

namespace Rae.UnitTest
{
    [TestFixture]
    class DapperTest : TestBase
    {
        private string connectionString = @"Data Source=192.168.162.11;Initial Catalog=cr_news;User ID=test;Password=d9ell8;timeout=1000";


        class UserInfo
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public int Age { get; set; }
            public string Sex { get; set; }
        }

        [Test]
        public void TestLoadAll()
        {

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                IEnumerable<UserInfo> userInfos = connection.Query<UserInfo>("select * from userinfo");
                Show("count:{0}", userInfos.Count());
                foreach (var userInfo in userInfos)
                {
                    Show("Id:{0};name:{1}", userInfo.Id, userInfo.Name);
                }
            }
        }
    }
}
