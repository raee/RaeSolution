using System;
using System.Collections.Generic;
using System.Text;

namespace Rae.Module.Log
{
    class DefaultLogger : IRaeLog
    {
        public void Error(object msg, Exception e)
        {
            Console.WriteLine(msg + e.Message);
        }

        public void Debug(object msg)
        {
            Console.WriteLine(msg);
        }

        public void Info(object msg)
        {
            Console.WriteLine(msg);
        }

        public void Warn(object msg)
        {
            Console.WriteLine(msg);
        }
    }
}
