using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace Rae.UnitTest
{
    /// <summary>
    /// 测试基类
    /// </summary>
    public class TestBase
    {

        protected void IsNull(object obj, string msg)
        {
            Assert.IsNull(obj, msg);
        }

        protected void IsNullOrEmpty(string str, string msg)
        {
            Assert.IsNullOrEmpty(str, msg);
        }

        protected void IsFalse(bool val, string msg)
        {
            Assert.IsFalse(val, msg);
        }


        protected void Show(object msg)
        {
            Console.WriteLine(msg);
        }

        protected void Show(String msg, params object[] args)
        {
            Console.WriteLine(msg, args);
        }
    }
}
