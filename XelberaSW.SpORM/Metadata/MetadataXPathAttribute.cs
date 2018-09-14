using System;

namespace XelberaSW.SpORM.Metadata
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MetadataXPathAttribute : Attribute
    {
        public string XPath { get; }

        public MetadataXPathAttribute(string xPath)
        {
            XPath = xPath;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class MetadataRootAttribute : Attribute
    {}
}
