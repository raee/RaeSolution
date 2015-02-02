using System;
using System.Collections.Generic;
using System.Text;

namespace Rae.Module.Auth.Model
{
    /// <summary>
    /// 认证信息
    /// </summary>
    public class VerifyInfo
    {
        public string Vid { get; set; }

        public string Uid { get; set; }

        public string UserName { get; set; }

        public VerifyStatus Status { get; set; }

        public string TypeId { get; set; }

        public string VerfiyDate { get; set; }

        public string AgreeUid { get; set; }

        public string AgreeDate { get; set; }

        public string Tips { get; set; }
    }
}

