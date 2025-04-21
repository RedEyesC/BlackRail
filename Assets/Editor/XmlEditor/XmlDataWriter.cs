using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace GameEditor.XmlEditor
{
    public class XmlDataWriter
    {


        //TODO 单条数据的时候，表格不好看
        public static void SaveListToXml<T>(List<T> list, string filePath) where T : new()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(List<T>));
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(fs, list);
            }
        }

    }
}