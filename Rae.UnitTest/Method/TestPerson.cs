using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using Rae.UnitTest.Method.SpringNet;
using Spring.Aop.Framework;

namespace Rae.UnitTest.Method
{
    [TestFixture]
    public class TestPerson
    {
        [Test]
        public void TestTall()
        {
            ProxyFactory factory = new ProxyFactory(new Person());
            factory.AddAdvice(new MethodAdvice());
            Person p =(Person) factory.GetProxy();
            p.Tall();
        }
    }
}
