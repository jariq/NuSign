using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace NuSign
{
    [XmlType(TypeName = "IntegrityList")]
    public class IntegrityList : List<IntegrityEntry>
    {
        public byte[] ToByteArray()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                this.Save(ms);
                return ms.ToArray();
            }
        }

        public static IntegrityList FromByteArray(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
                return Load(ms);
        }

        private void Save(Stream outputStream)
        {
            XmlWriterSettings xmlWriterSettings = new System.Xml.XmlWriterSettings()
            {
                CloseOutput = false,
                Encoding = new UTF8Encoding(false, true),
                OmitXmlDeclaration = false,
                Indent = true
            };

            using (XmlWriter xmlWriter = XmlWriter.Create(outputStream, xmlWriterSettings))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(IntegrityList));
                xmlSerializer.Serialize(xmlWriter, this);
            }
        }

        private static IntegrityList Load(Stream inputStream)
        {
            XmlReaderSettings xmlReaderSettings = new XmlReaderSettings()
            {
                CloseInput = false,
                ConformanceLevel = ConformanceLevel.Document,
                DtdProcessing = DtdProcessing.Prohibit,
                ValidationType = ValidationType.None
            };

            using (XmlReader xmlReader = XmlReader.Create(inputStream, xmlReaderSettings))
            {
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(IntegrityList));
                return (IntegrityList)xmlSerializer.Deserialize(xmlReader);
            }
        }
    }
}
