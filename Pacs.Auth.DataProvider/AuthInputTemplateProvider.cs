using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Microsoft.SqlServer.Server;
using Rae.Module.Auth.Model;

namespace Pacs.Auth.DataProvider
{
    partial class AuthDataProvider
    {
        #region 接口方法
        public List<AuthInputTemplateModel> GetAuthInputTemplates(string uid, string id)
        {
            List<AuthInputTemplateModel> result = new List<AuthInputTemplateModel>();
            string sql = "SELECT * FROM BASE.VERIFY_TEMPLATE WHERE TYPEID=:ID";
            context.CreateParameter(":ID", id);
            IDataReader reader = context.Query.ExecuteDataReader(sql);

            while (reader.Read())
            {
                AuthInputTemplateModel m = ReaderModel(reader);

                // 赋值 -- 用户的模板值
                if (m != null && !string.IsNullOrEmpty(uid))
                {
                    AuthInputValueModel val = GetAuthInputValue(uid, m.Mid);
                    m.Value = val == null ? null : val.Value;
                }
                result.Add(m);
            }

            //context.Close();
            return result;
        }

        public AuthInputTemplateModel GetAuthInputTemplate(string id)
        {
            string sql = "SELECT * FROM BASE.VERIFY_TEMPLATE WHERE MID=:MID";
            context.CreateParameter(":MID", id);
            IDataReader reader = context.Query.ExecuteDataReader(sql);
            if (reader.Read())
            {
                return ReaderModel(reader);
            }
            return null;

        }

        public AuthInputValueModel GetAuthInputValue(string uid, string inputId)
        {
            AuthInputValueModel result = null;

            string sql = "SELECT * FROM BASE.USER_VERIFY_VALUE WHERE USER_ID=:UUID AND MID=:MID";
            context.CreateParameter(":UUID", uid);
            context.CreateParameter(":MID", inputId);
            IDataReader reader = context.Query.ExecuteDataReader(sql);

            if (reader.Read())
            {
                result = ReadValueModel(reader);
            }

            //  context.Close();

            return result;
        }


        public bool AddOrUpdateTemplate(AuthInputTemplateModel m)
        {

            bool exists = ExitTemplate(m.Mid);

            StringBuilder sb = new StringBuilder();

            // 更新操作
            context.CreateParameter(":TYPEID", m.TypeId);
            context.CreateParameter(":TEXT", m.Text);
            context.CreateParameter(":CTRLID", m.Name);
            context.CreateParameter(":DEFINETYPE", Convert.ToInt32(m.DataType));
            context.CreateParameter(":REQUIRED", m.Required ? 1 : 0);
            context.CreateParameter(":MAXLENGTH", m.MaxLength);
            context.CreateParameter(":TIPS", m.Tips);
            context.CreateParameter(":VALS", m.DropDownListItems);

            if (string.IsNullOrEmpty(m.Mid) || !exists)
            {
                sb.Append("INSERT INTO BASE.VERIFY_TEMPLATE(MID,TYPEID,NAME,CTRL_ID,DEFINE_TYPE,REQUIRED,MAX_LENGTH,TIPS,VALS)");
                sb.Append("VALUES(SYS_GUID(),:TYPEID,:TEXT,:CTRLID,:DEFINETYPE,:REQUIRED,:MAXLENGTH,:TIPS,:VALS)");
                return context.Query.ExecuteNonQuery(sb.ToString()) > 0;
            }
            else // 新增操作
            {
                sb.Append("UPDATE BASE.VERIFY_TEMPLATE SET ");
                sb.Append("TYPEID=:TYPEID,NAME=:TEXT,CTRL_ID=:CTRLID,DEFINE_TYPE=:DEFINETYPE,REQUIRED=:REQUIRED,");
                sb.Append("MAX_LENGTH=:MAXLENGTH,TIPS=:TIPS,VALS=:VALS");
                sb.Append(" WHERE MID=:MID");
                context.CreateParameter(":MID", m.Mid);
                return context.Query.ExecuteNonQuery(sb.ToString()) > 0;
            }
        }

        public bool DelTemplate(string typeId)
        {
            string sql = "DELETE FROM BASE.VERIFY_TEMPLATE WHERE TYPEID=:TYPEID";
            context.Query.CreateParameter(":TYPEID", typeId);
            return context.Query.ExecuteNonQuery(sql) > 0;
        }

        public bool InsertTemplateValue(AuthInputValueModel m)
        {
            string sql;
            // 如果控件Id已经存在则只是更新
            if (TemplateValueExist(m.InputId))
            {
                sql = "UPDATE BASE.USER_VERIFY_VALUE SET VAL=:VAL WHERE MID=:MID AND USER_ID=:USERID";
                context.CreateParameter(":VAL", m.Value);
                context.CreateParameter(":MID", m.InputId);
                context.CreateParameter(":USERID", AuthUser.Uid);
            }
            else
            {
                sql = "INSERT INTO BASE.USER_VERIFY_VALUE(UVID,USER_ID,MID,VAL)VALUES(SYS_GUID(),:USERID,:MID,:VAL)";
                context.CreateParameter(":USERID", AuthUser.Uid);
                context.CreateParameter(":MID", m.InputId);
                context.CreateParameter(":VAL", m.Value);
            }
            return context.Query.ExecuteNonQuery(sql) > 0;
        }

        #endregion

        #region 私有方法

        private bool TemplateValueExist(string id)
        {
            string sql = "SELECT COUNT(*) FROM BASE.USER_VERIFY_VALUE WHERE MID=:MID AND USER_ID=:USERID";
            context.CreateParameter(":MID", id);
            context.CreateParameter(":USERID", AuthUser.Uid);
            object value = context.Query.ExecuteScalar(sql);
            return Convert.ToInt32(value) > 0;
        }

        /// <summary>
        /// 模板是否存在
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        private bool ExitTemplate(string id)
        {
            context.CreateParameter(":MID", id);
            object val = context.Query.ExecuteScalar("SELECT COUNT(*) FROM BASE.VERIFY_TEMPLATE WHERE MID=:MID");
            return Convert.ToInt32(val) > 0;
        }


        /// <summary>
        /// 转换为控件类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private DataType ConvertToDataType(string type)
        {
            DataType result = DataType.Text;
            if (!string.IsNullOrEmpty(type))
            {
                if (type.Equals("1"))
                {
                    result = DataType.MultiLineText;
                }
                else if (type.Equals("2"))
                {
                    result = DataType.Image;

                }
                else if (type.Equals("3"))
                {
                    result = DataType.DropDownList;

                }
                else
                {
                    result = DataType.Text;
                }
            }
            return result;
        }

        /// <summary>
        /// 转换为布尔值
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private bool ConvertToBoolean(string val)
        {
            if (!string.IsNullOrEmpty(val) && val.Equals("1"))
            {
                return true;
            }
            return false;
        }

        private AuthInputTemplateModel ReaderModel(IDataReader reader)
        {
            AuthInputTemplateModel m = new AuthInputTemplateModel();
            m.DataType = ConvertToDataType(reader["DEFINE_TYPE"].ToString());
            m.Mid = reader["MID"].ToString();
            m.DropDownListItems = reader["VALS"].ToString();
            m.Name = reader["CTRL_ID"].ToString();
            m.Required = ConvertToBoolean(reader["REQUIRED"].ToString());
            m.Text = reader["NAME"].ToString();
            m.Tips = reader["TIPS"].ToString();
            m.TypeId = reader["TYPEID"].ToString();
            m.MaxLength = Convert.ToInt32(reader["MAX_LENGTH"]);
            return m;
        }

        /// <summary>
        /// 读取实体
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        private AuthInputValueModel ReadValueModel(IDataReader reader)
        {
            AuthInputValueModel m = new AuthInputValueModel();
            m.DictId = reader["UVID"].ToString();
            m.InputId = reader["MID"].ToString();
            m.Value = reader["VAL"].ToString();
            return m;
        }

        #endregion
    }
}
