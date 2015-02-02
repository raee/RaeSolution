using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using Pacs.Auth.DataProvider;
using Rae.Module.Auth;
using Rae.Module.Auth.Model;

namespace Rae.UnitTest.Auth
{
    [TestFixture]
    public class TestAuth : TestBase
    {
        private Authentication auth;

        [SetUp]
        public void Setup()
        {
            AuthFactory.InitFactory(new PacsAuthentication());
            auth = AuthFactory.Authentication;
        }


        [Test]
        public void TestInit()
        {
            Show(auth.DataProvider.IsLogin);
        }

        [Test]
        public void TestAuthType()
        {
            Show("-----认证类型-----");
            foreach (var m in auth.DataProvider.GetAuthTypeModels())
            {
                Show(m.Name);
            }

            Show("---------");
            Show(auth.DataProvider.GetAuthTypeModel("TYPE_DOCTOR").Name);
        }

        [Test]
        public void TestTemplate()
        {
            foreach (var m in auth.DataProvider.GetAuthInputTemplates("TEST", "32"))
            {
                Show(m.Text + "|" + m.Value);
            }
        }

        [Test]
        public void AddOrUpdateTemplate()
        {
            AuthInputTemplateModel m = auth.DataProvider.GetAuthInputTemplate("GGG");
            m.Text = "hahah";
            bool result = auth.DataProvider.AddOrUpdateTemplate(m);
            Show(result);
            Show(m.Text);
        }



        [Test]
        public void OtherTest()
        {
            VerifyInfo info = new VerifyInfo();

            switch (info.Status)
            {
                case VerifyStatus.Pendding:
                case VerifyStatus.RePendding:
                case VerifyStatus.Success:
                case VerifyStatus.Faild:
                case VerifyStatus.Forbidden:
                    Show(info.Status);
                    break;
                default:
                    Show("other");
                    break;
            }
        }

    }
}
