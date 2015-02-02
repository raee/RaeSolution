using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Rae.Module.Auth.Model
{
    /// <summary>
    /// 类型输入模板
    /// </summary>
    public class AuthInputTemplateModel
    {
        /// <summary>
        /// 主键
        /// </summary>
        public string Mid { get; set; }

        /// <summary>
        /// 控件显示名称
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// 控件表单名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 控件所属模板类别
        /// 关联AuthTypeModel
        /// </summary>
        public string TypeId { get; set; }

        /// <summary>
        /// 控件数据类型
        /// </summary>
        public DataType DataType { get; set; }

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool Required { get; set; }

        /// <summary>
        /// 控件提示信息
        /// </summary>
        public string Tips { get; set; }

        /// <summary>
        /// 允许的最大长度
        /// 文本为长度，图像为大小。
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// 下拉列表取值
        /// 类型为键值对。｛["Text","Value"],["Text1","Value1"]｝
        /// </summary>
        [JsonIgnore]
        public string DropDownListItems { get; set; }


        /// <summary>
        /// 取值
        /// </summary>
        public string Value { get; set; }

        public AuthInputTemplateModel()
        {
        }

        public AuthInputTemplateModel(AuthInputTemplateModel m)
        {
            DataType = m.DataType;
            DropDownListItems = m.DropDownListItems;
            MaxLength = m.MaxLength;
            Name = m.Name;
            Required = m.Required;
            Text = m.Text;
            Tips = m.Tips;
            TypeId = m.TypeId;
            Value = m.Value;
            Mid = m.Mid;
        }
    }
}
