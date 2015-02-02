using System.Collections.Generic;
using NUnit.Framework;
using Pacs.Auth.DataProvider;
using Rae.Module.Auth;
using Rae.Module.Auth.Data;
using Rae.Module.Auth.Model;
using Rae.Module.Auth.Option;

namespace Rae.UnitTest.Auth
{
    [TestFixture]
    public class VerifyInfoTest : TestBase
    {
        private IAuthDataProvider db;
        private string testId = "2B6253BE009E4635908852711737860C";

        [SetUp]
        public void Setup()
        {
            AuthFactory.InitFactory(new PacsAuthentication());
            db = AuthFactory.Authentication.DataProvider;
        }

        [Test]
        public void TestInfo()
        {
            VerifyInfoOption option = new VerifyInfoOption();
            List<VerifyInfo> datas = db.GetVerifyInfos(option);

            Show(datas.Count);
        }

        [Test]
        public void TestAdd()
        {
            var datas = db.GetVerifyInfo(testId);
            datas.UserName = "ADD";
            bool result = db.InsertVerifyInfo(new VerifyInfo());
            Assert.IsTrue(result, "插入失败！");
        }

        [Test]
        public void TestUpdate()
        {
            var datas = db.GetVerifyInfo(testId);
            datas.UserName = "CODE";
            bool result = db.UpdateVerifyInfo(datas);
            Assert.IsTrue(result, "更新失败");
        }
        [Test]
        public void TestSigle()
        {
            var datas = db.GetVerifyInfo(testId);
            Show(datas.UserName);
        }

        [Test]
        public void TestUserVerify()
        {
            var datas = db.GetUserCurrentVerfiy("SYSTEM", "", VerifyStatus.All);
            Show(datas.UserName);
        }
    }
}