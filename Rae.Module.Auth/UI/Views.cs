using System;
using System.Collections.Generic;
using System.Text;
using Rae.Module.Auth.Model;

/*
  该文件保存前端交互的一些ViewModel
 */
namespace Rae.Module.Auth.UI
{
    public class TemplateJsonView : AuthInputTemplateModel
    {
        public TemplateJsonView()
        {
        }

        public TemplateJsonView(AuthInputTemplateModel m)
            : base(m)
        {

        }

        public string MaxLen { get; set; }
        public List<KeyValueJsonView> Items { get; set; }
    }

    public class KeyValueJsonView
    {
        public string Text { get; set; }
        public string Value { get; set; }
    }
}
