using System;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections;
using System.Collections.Generic;

public class IOUtility {
    // encoding to use
    private static UTF8Encoding _utf8Ecoding = new UTF8Encoding();

    /// <summary>
    /// Convert UTF8 Byte Array to String
    /// </summary>
    /// <param name="characters"></param>
    /// <returns></returns>
    public static string UTF8ByteArrayToString(byte[] characters) {
        string constructedString = _utf8Ecoding.GetString(characters);
        return (constructedString);
    }

    /// <summary>
    /// Convert string to UTF8 Byte Array
    /// </summary>
    /// <param name="xmlString"></param>
    /// <returns></returns>
    public static byte[] StringToUTF8ByteArray(string xmlString) {
        return _utf8Ecoding.GetBytes(xmlString);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serializeObject"></param> Your object
    /// <param name="type"></param> The type to serialize your object to
    /// <returns></returns>
    public static string SerializeObject(object serializeObject, Type type) {
        // set up serializing 
        MemoryStream memoryStream = new MemoryStream();
        XmlSerializer xs = new XmlSerializer(type);
        XmlTextWriter xmlTextWriter = new XmlTextWriter(memoryStream, Encoding.UTF8);

        // serialize the object
        xs.Serialize(xmlTextWriter, serializeObject);

        // read from xml stream
        memoryStream = (MemoryStream)xmlTextWriter.BaseStream;

        // get string back from memory stream
        return UTF8ByteArrayToString(memoryStream.ToArray());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="xmlizedString"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    public static object DeserializeObject(string xmlizedString, Type type) {
        XmlSerializer xs = new XmlSerializer(type);

        MemoryStream memoryStream = new MemoryStream(StringToUTF8ByteArray(xmlizedString));

        return xs.Deserialize(memoryStream);
    }

    /// <summary>
    /// Create xml at path with data
    /// </summary>
    /// <param name="a_data"></param>
    /// <param name="a_path"></param>
    /// <returns></returns>
    public static string CreateXML(string data, string path) {
        StreamWriter writer;

        // construct path
        path += (path.Substring(path.Length - 4).Equals(".xml") ? "" : ".xml");

        // set up new file
        FileInfo t = new FileInfo(path);
        if (!t.Exists) {
            writer = t.CreateText();
        }
        else {
            t.Delete();
            writer = t.CreateText();
        }

        // write and close file
        writer.Write(data);
        writer.Close();

        // return the path
        return path + ".xml";
    }

    /// <summary>
    /// Load xml from path
    /// </summary>
    /// <param name="a_path"></param>
    /// <returns></returns>
    public static string LoadXML(string a_path) {
        StreamReader streamReader = File.OpenText(a_path);
        string data = streamReader.ReadToEnd();
        streamReader.Close();
        return data;
    }
}
