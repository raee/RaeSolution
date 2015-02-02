using System;
using System.Collections.Generic;
using System.Text;

namespace Rae.Module.Log
{
    /// <summary>
    /// 该类是对Log4Net的的简单封装
    /// 通过文件及数据库记录Log日志
    /// </summary>
    public static class LogManager
    {
        public static readonly string DefaultLoggerName = "RaeLog";

        /// <summary>
        /// 首次启动需要配置
        /// 一般写在程序开始的地方
        /// 配置通过log4net来配置
        /// </summary>
        public static void Config()
        {

        }

        public static IRaeLog GetLog(string tag)
        {
            return new DefaultLogger();
        }

        public static IRaeLog GetLog(Type tag)
        {
            return new DefaultLogger();
        }


        private static IRaeLog DefaultLogger
        {
            get { return GetLog(DefaultLoggerName); }
        }


        public static void Error(object msg, Exception e)
        {
            DefaultLogger.Error(msg, e);
        }

        public static void Error(Exception e)
        {
            DefaultLogger.Error("", e);
        }

        public static void Debug(object msg)
        {
            DefaultLogger.Debug(msg);
        }

        public static void Info(object msg)
        {
            DefaultLogger.Info(msg);
        }

        public static void Warn(object msg)
        {
            DefaultLogger.Warn(msg);
        }

    }
}
