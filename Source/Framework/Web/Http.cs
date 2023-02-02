// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Framework.Web
{
    public class HttpHeader
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public string Host { get; set; }
        public string DeviceId { get; set; }
        public string ContentType { get; set; }
        public int ContentLength { get; set; }
        public string AcceptLanguage { get; set; }
        public string Accept { get; set; }
        public string UserAgent { get; set; }
        public string Content { get; set; }
    }

    public enum HttpCode
    {
        Ok = 200,
        BadRequest = 400,
        NotFound = 404,
        InternalServerError = 500
    }

    public class HttpHelper
    {
        public static byte[] CreateResponse(HttpCode httpCode, string content, bool closeConnection = false)
        {
            var sb = new StringBuilder();

            using (var sw = new StringWriter(sb))
            {
                sw.WriteLine($"HTTP/1.1 {(int)httpCode} {httpCode}");

                //sw.WriteLine($"Date: {DateTime.Now.ToUniversalTime():r}");
                //sw.WriteLine("Server: Arctium-Emulation");
                //sw.WriteLine("Retry-After: 600");
                sw.WriteLine($"Content-Length: {content.Length}");
                //sw.WriteLine("Vary: Accept-Encoding");

                if (closeConnection)
                    sw.WriteLine("Connection: close");

                sw.WriteLine("Content-Type: application/json;charset=UTF-8");
                sw.WriteLine();

                sw.WriteLine(content);
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static HttpHeader ParseRequest(byte[] data, int length)
        {
            var headerValues = new Dictionary<string, object>();
            var header = new HttpHeader();

            using (var sr = new StreamReader(new MemoryStream(data, 0, length)))
            {
                var info = sr.ReadLine().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                if (info.Length != 3)
                    return null;

                headerValues.Add("method", info[0]);
                headerValues.Add("path", info[1]);
                headerValues.Add("type", info[2]);

                while (!sr.EndOfStream)
                {
                    info = sr.ReadLine().Split(new string[] { ": " }, StringSplitOptions.RemoveEmptyEntries);

                    if (info.Length == 2)
                        headerValues.Add(info[0].Replace("-", "").ToLower(), info[1]);
                    else if (info.Length > 2)
                    {
                        var val = "";

                        info.Skip(1);

                        headerValues.Add(info[0].Replace("-", "").ToLower(), val);
                    }
                    else
                    {
                        // We are at content here.
                        var content = sr.ReadLine();

                        headerValues.Add("content", content);

                        // There shouldn't be anything after the content!
                        break;
                    }
                }
            }

            var httpFields = typeof(HttpHeader).GetTypeInfo().GetProperties();

            foreach (var f in httpFields)
            {
                object val;

                if (headerValues.TryGetValue(f.Name.ToLower(), out val))
                {
                    if (f.PropertyType == typeof(int))
                        f.SetValue(header, Convert.ChangeType(Convert.ToInt32(val), f.PropertyType));
                    else
                        f.SetValue(header, Convert.ChangeType(val, f.PropertyType));
                }
            }

            return header;
        }
    }
}
