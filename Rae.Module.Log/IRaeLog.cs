using System;

namespace Rae.Module.Log
{
    public interface IRaeLog
    {
        void Error(object msg, Exception e);

        void Debug(object msg);

        void Info(object msg);

        void Warn(object msg);
    }
}