using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Pacs.Core;
using Rae.Module.Auth.Model;

namespace Pacs.Auth.DataProvider
{
    partial class AuthDataProvider
    {

        public List<AuthTypeModel> GetAuthTypeModels()
        {
            List<AuthTypeModel> result = new List<AuthTypeModel>();
            IDataReader reader = context.Query.ExecuteDataReader("SELECT * FROM BASE.VERIFY_TYPE");
            while (reader.Read())
            {
                result.Add(ReadAuthTypeModel(reader));
            }
            return result;
        }

        public AuthTypeModel GetAuthTypeModel(string id)
        {
            context.CreateParameter(":VID", id);
            IDataReader reader = context.Query.ExecuteDataReader("SELECT * FROM BASE.VERIFY_TYPE WHERE VID=:VID");

            if (reader.Read())
            {
                return ReadAuthTypeModel(reader);
            }
            return null;
        }

        public bool DeleteAuthType(string id)
        {
            context.CreateParameter(":VID", id);
            return context.Query.ExecuteNonQuery("DELETE FROM BASE.VERIFY_TYPE WHERE VID=:VID") > 0;
        }

        public bool AddAuthType(AuthTypeModel m)
        {
            if (string.IsNullOrEmpty(m.Name))
            {
                Log.Info(string.Format("添加认证返回，{0}为空！", m.Name));
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO BASE.VERIFY_TYPE(VID,NAME,COMMENTS,IMAGE_URL,ICON,CREATOR,CREATE_DATE)");
            sb.Append("VALUES(SYS_GUID(),:TYPENAME,:COMMENTS,'../images/img_vp2.png','../images/transparent.gif',:CREATOR,SYSDATE)");
            string sql = sb.ToString();

            context.CreateParameter(":TYPENAME", m.Name);
            context.CreateParameter(":COMMENTS", m.Comment);
            context.CreateParameter(":CREATOR", AuthUser.Uid);
            return context.Query.ExecuteNonQuery(sql) > 0;
        }

        public bool UpdateAuthType(AuthTypeModel m)
        {
            if (string.IsNullOrEmpty(m.Name) || string.IsNullOrEmpty(m.AuthTypeId))
            {
                Log.Info(string.Format("认证类型更新返回，Name:{0},Id:{1} 为空！", m.Name, m.AuthTypeId));
                return false;
            }

            StringBuilder sb = new StringBuilder();
            sb.Append("UPDATE BASE.VERIFY_TYPE SET ");
            sb.Append("NAME=:TYPENAME,COMMENTS=:COMMENTS WHERE VID=:VID");
            string sql = sb.ToString();
            context.CreateParameter(":TYPENAME", m.Name);
            context.CreateParameter(":COMMENTS", m.Comment);
            context.CreateParameter(":VID", m.AuthTypeId);
            return context.Query.ExecuteNonQuery(sql) > 0;
        }

        private AuthTypeModel ReadAuthTypeModel(IDataReader reader)
        {
            AuthTypeModel m = new AuthTypeModel();
            m.Name = reader["NAME"].ToString();
            m.Comment = reader["COMMENTS"].ToString();
            m.AuthTypeId = reader["VID"].ToString();
            m.Creator = reader["CREATOR"].ToString();
            m.ImageUrl = reader["IMAGE_URL"].ToString();
            m.Icon = reader["ICON"].ToString();
            m.CreateDate = reader["CREATE_DATE"].ToString();
            return m;
        }
    }
}
