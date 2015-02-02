using System;
using System.Collections.Generic;
using System.Text;
using Rae.Module.Auth.Model;

namespace Rae.Module.Auth.Option
{
    public class VerifyInfoOption
    {
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }

        public VerifyStatus Status { get; set; }

        public string VerifyType { get; set; }

        public string StartDate { get; set; }

        public string EndDate { get; set; }

        /// <summary>
        /// 页码
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// 分页数
        /// </summary>
        public int PageSize { get; set; }

        public VerifyInfoOption()
        {
            PageSize = 20;

            // 默认日期
            StartDate = DateTime.Now.AddDays(-30).ToString("yyyy-MM-dd");
            EndDate = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
            Status = VerifyStatus.All;
        }

        public int GetStartIndex()
        {
            return Index * PageSize;
        }

        public int GetEndIndex()
        {
            return GetStartIndex() + PageSize;
        }
    }
}
