using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Xml;

namespace Rae.Data.Config
{
    public class ConfigurationSectionHandler : IConfigurationSectionHandler
    {
        private List<Factory> _factories;

        public ConfigurationSectionHandler()
        {
            _factories = new List<Factory>();
        }

        public object Create(object parent, object configContext, XmlNode section)
        {
            Configuration config = new Configuration();
            foreach (XmlNode note in section.ChildNodes)
            {
                switch (note.Name)
                {
                    case "factory":
                        break;
                }
            }
            return config;
        }
    }
}
