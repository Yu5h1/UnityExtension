using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Yu5h1Lib.WebSupport
{
    public static class IndexHtmlEditor 
    {
        public static void ExtensionMethod(string outputPath, string propertyName,string newValue)
        {
            string indexPath = Path.Combine(outputPath, "index.html");
            if (!File.Exists(indexPath))
            {
                Debug.LogError("index.html not found at: " + indexPath);
                return;
            }
            string html = File.ReadAllText(indexPath);

            var propertyRegex = new Regex(@$"const {propertyName}\s*=\s*['""]([^'""]+)['""];?");

            var match = propertyRegex.Match(html);

            string prop = "";

            if (match.Success)
            {
                prop = match.Groups[1].Value;
            }
            if (newValue == prop)
                return;
            prop = newValue;

            string newPropScript = $"const {propertyName}='{prop}';";

            if (match.Success)
            {
                html = propertyRegex.Replace(html, newPropScript);
            }
            else
            {
                // �S��쪩�����A���մ��J�� </head> �e
                var headEndRegex = new Regex(@"</head>", RegexOptions.IgnoreCase);
                if (headEndRegex.IsMatch(html))
                {
                    html = headEndRegex.Replace(html, $"\t<script>{newPropScript}</script>\n</head>", 1);
                }
                else
                {
                    // �p�G�S�� </head>�A���J�� <body> �e
                    var bodyStartRegex = new Regex(@"<body[^>]*>", RegexOptions.IgnoreCase);
                    if (bodyStartRegex.IsMatch(html))
                    {
                        html = bodyStartRegex.Replace(html, match => $"<script>{newPropScript}</script>\n{match.Value}", 1);
                    }
                    else
                    {
                        // �̫��q�G���J����}�Y
                        html = $"<script>{newPropScript}</script>\n" + html;
                    }
                }
            }
            Debug.Log($" WebGL �����w��s���G{newPropScript} \n �g�J�ɮסG{indexPath}");
        }
    } 
}
