using System.Xml;
using UnityEngine;
using UnityEditor;

namespace Yu5h1Lib.EditorExtension
{
    public static class AssetImporterEx
    {
        public static XmlDocument GetXmlUserData(this AssetImporter assetImporter)
        {
            XmlDocument result = new XmlDocument();
            result.LoadXml(assetImporter.userData);
            
            return result;
            
        }
    } 
}