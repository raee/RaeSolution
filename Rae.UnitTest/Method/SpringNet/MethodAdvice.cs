using System;
using System.Collections.Generic;
using System.Text;
using AopAlliance.Intercept;

namespace Rae.UnitTest.Method.SpringNet
{
    class MethodAdvice : IMethodInterceptor
    {
        public object Invoke(IMethodInvocation invocation)
        {
            Console.WriteLine("拦截了方法：" + invocation.Method.Name);
            return invocation.Proceed();
        }
    }
}
