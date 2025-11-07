using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace GameEditor.XmlEditor
{
    public class XmlDataWriter
    {
        public static void SaveListToXml<T>(List<T> list, string filePath)
            where T : new()
        {
            //单条数据的时候，wps打开表格不好看，添加一个空对象
            if (list.Count == 1)
            {
                list.Add(new T());
            }

            XmlSerializer serializer = new XmlSerializer(typeof(List<T>));
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            {
                serializer.Serialize(fs, list);
            }
        }
    }
}
