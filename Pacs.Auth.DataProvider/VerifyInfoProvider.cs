using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using Rae.Module.Auth.Model;
using Rae.Module.Auth.Option;

namespace Pacs.Auth.DataProvider
{
    partial class AuthDataProvider
    {
        public List<VerifyInfo> GetVerifyInfos(VerifyInfoOption option)
        {


            List<VerifyInfo> result = new List<VerifyInfo>();
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM ( ");
            sb.Append("SELECT ROWNUM NO,T.* FROM BASE.VERIFY_INFO T WHERE ");

            sb.Append("to_date(VERIFY_DATE,'yyyy-MM-dd HH24:mi:ss')>=to_date(:STARTDATE,'yyyy-MM-dd') AND to_date(VERIFY_DATE,'yyyy-MM-dd HH24:mi:ss')<=to_date(:ENDDATE,'yyyy-MM-dd') ");
            context.CreateParameter(":STARTDATE", option.StartDate);
            context.CreateParameter(":ENDDATE", option.EndDate);

            if (!string.IsNullOrEmpty(option.Name))
            {
                sb.Append("AND USER_NAME=:USERNAME ");
                context.CreateParameter(":USERNAME", option.Name);
            }
            if (!string.IsNullOrEmpty(option.VerifyType))
            {
                sb.Append("AND TYPEID=:TYPEID ");
                context.CreateParameter(":TYPEID", option.VerifyType);
            }

            // 状态筛选
            if (option.Status != VerifyStatus.All)
            {
                sb.Append("AND STATUS=:VERIFYSTATUS ");
                context.CreateParameter(":VERIFYSTATUS", Convert.ToInt32(option.Status));
            }


            sb.Append(") ");
            sb.Append("WHERE NO BETWEEN :STARTINDEX AND :ENDINDEX");

            context.CreateParameter(":STARTINDEX", option.GetStartIndex());
            context.CreateParameter(":ENDINDEX", option.GetEndIndex());


            IDataReader reader = context.Query.ExecuteDataReader(sb.ToString());

            while (reader.Read())
            {
                result.Add(ReadModel(reader));
            }

            return result;
        }

        public VerifyInfo GetVerifyInfo(string id)
        {
            string sql = "SELECT * FROM BASE.VERIFY_INFO WHERE VID=:VID";
            context.CreateParameter(":VID", id);

            IDataReader reader = context.Query.ExecuteDataReader(sql);
            if (reader.Read())
            {
                return ReadModel(reader);
            }

            return null;
        }

        public VerifyInfo GetUserCurrentVerfiy(string uid, string typeid, VerifyStatus status)
        {
            string sql = "SELECT * FROM BASE.VERIFY_INFO WHERE USER_ID=:VERIFYUID AND TYPEID=:TYPEID ";
            context.CreateParameter(":VERIFYUID", uid);
            context.CreateParameter(":TYPEID", typeid);

            if (status != VerifyStatus.All)
            {
                sql += "AND STATUS=:VERIFYSTATUS";
                context.CreateParameter(":VERIFYSTATUS", Convert.ToInt32(status));
            }

            IDataReader reader = context.Query.ExecuteDataReader(sql);
            if (reader.Read())
            {
                return ReadModel(reader);
            }

            return null;
        }

        public bool InsertVerifyInfo(VerifyInfo m)
        {
            StringBuilder sb = new StringBuilder();
            m.Uid = AuthUser.Uid;
            m.UserName = AuthUser.UserName;


            // 如果该申请已经存在则更新状态
            VerifyInfo inserted = this.GetUserCurrentVerfiy(m.Uid, m.TypeId, VerifyStatus.All);
            if (inserted != null && !string.IsNullOrEmpty(inserted.Vid))
            {
                // 更新
                sb.Append("UPDATE BASE.VERIFY_INFO SET STATUS=:STATUS ,VERIFY_DATE=:VERIFYDATE WHERE VID=:VID");
                context.CreateParameter(":STATUS", m.Status);
                context.CreateParameter(":VERIFYDATE", m.VerfiyDate);
                context.CreateParameter(":VID", inserted.Vid);
            }
            else
            {
                sb.Append(
                    "INSERT INTO BASE.VERIFY_INFO (VID, USER_ID, USER_NAME, STATUS, TYPEID, VERIFY_DATE, AGREE_UID, AGREE_DATE)");
                sb.Append(
                    "VALUES (SYS_GUID(), :USERID, :USERNAME, :STATUS, :TYPEID, :VERIFYDATE, :AGREEUID, :AGREEDATE)");
                context.CreateParameter(":USERID", m.Uid);
                context.CreateParameter(":USERNAME", m.UserName);
                context.CreateParameter(":STATUS", m.Status);
                context.CreateParameter(":TYPEID", m.TypeId);
                context.CreateParameter(":VERIFYDATE", m.VerfiyDate);
                context.CreateParameter(":AGREEUID", m.AgreeUid);
                context.CreateParameter(":AGREEDATE", m.AgreeDate);
            }

            return context.Query.ExecuteNonQuery(sb.ToString()) > 0;
        }

        public bool UpdateVerifyInfo(VerifyInfo m)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE BASE.VERIFY_INFO ");
            sb.Append("SET USER_ID=:USERID,USER_NAME=:USERNAME,STATUS=:STATUS,TYPEID=:TYPEID,VERIFY_DATE=:VERIFYDATE,AGREE_UID=:AGREEUID,AGREE_DATE=:AGREEDATE,TIPS=:TIPS ");
            sb.Append("WHERE VID=:VID");
            context.CreateParameter(":USERID", m.Uid);
            context.CreateParameter(":USERNAME", m.UserName);
            context.CreateParameter(":STATUS", m.Status);
            context.CreateParameter(":TYPEID", m.TypeId);
            context.CreateParameter(":VERIFYDATE", m.VerfiyDate);
            context.CreateParameter(":AGREEUID", m.AgreeUid);
            context.CreateParameter(":AGREEDATE", m.AgreeDate);
            context.CreateParameter(":TIPS", m.Tips);
            context.CreateParameter(":VID", m.Vid);

            return context.Query.ExecuteNonQuery(sb.ToString()) > 0;
        }


        private VerifyInfo ReadModel(IDataReader reader)
        {
            VerifyInfo m = new VerifyInfo();
            m.AgreeDate = reader["AGREE_DATE"].ToString();
            m.AgreeUid = reader["AGREE_UID"].ToString();
            m.Status = (VerifyStatus)Convert.ToInt32(reader["STATUS"].ToString());
            m.TypeId = reader["TYPEID"].ToString();
            m.Uid = reader["USER_ID"].ToString();
            m.UserName = reader["USER_NAME"].ToString();
            m.VerfiyDate = reader["VERIFY_DATE"].ToString();
            m.Vid = reader["VID"].ToString();
            m.Tips = reader["TIPS"].ToString();
            return m;
        }
    }
}
