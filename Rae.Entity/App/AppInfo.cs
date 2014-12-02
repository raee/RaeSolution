using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rae.Entity.App
{
    /// <summary>
    /// 应用信息
    /// </summary>
    public class AppInfo
    {
        /// <summary>
        /// 应用ID，值为Guid
        /// </summary>
        public string AppId { get; set; }

        /// <summary>
        /// 应用名称
        /// </summary>
        public string AppName { get; set; }

        /// <summary>
        /// 应用包名
        /// </summary>
        public string PackName { get; set; }

        /// <summary>
        /// 内部版本号
        /// </summary>
        public string VersionCode { get; set; }

        /// <summary>
        /// 版本名称
        /// </summary>
        public string VersionName { get; set; }

        /// <summary>
        /// 应用图标
        /// </summary>
        public string AppIcon { get; set; }

        /// <summary>
        /// 应用描述
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// 更新说明
        /// </summary>
        public string UpdateComment { get; set; }

        /// <summary>
        /// 应用截图
        /// </summary>
        public string AppScreenshot { get; set; }

        /// <summary>
        /// 下载路径
        /// </summary>
        public string AppDownloadUrl { get; set; }

        /// <summary>
        /// 应用分类
        /// </summary>
        public string AppCategory { get; set; }

        /// <summary>
        /// 应用类型，一般客户端类型
        /// </summary>
        public string AppType { get; set; }

        /// <summary>
        /// 支持的最低版本
        /// </summary>
        public string MinVersion { get; set; }

        /// <summary>
        /// 下载次数
        /// </summary>
        public string AppDownloadCount { get; set; }

    }
}
