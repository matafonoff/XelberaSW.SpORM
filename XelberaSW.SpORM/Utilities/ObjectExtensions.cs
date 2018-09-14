using System.Data.SqlTypes;
using System.IO;
using System.Xml;

namespace XelberaSW.SpORM.Utilities
{
    public static class ObjectExtensions
    {
        public static SqlXml ToSqlXml(this CustomXmlSerializer serializer, object obj, string rootName = null)
        {
            if (obj == null)
            {
                return null;
            }

            var element = serializer.Serialize(obj, rootName);

            var xml = element?.ToString();

            if (string.IsNullOrWhiteSpace(xml))
            {
                return null;
            }

            return new SqlXml(XmlReader.Create(new StringReader(xml)));
        }

    }
}