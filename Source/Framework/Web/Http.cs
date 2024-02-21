// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Networking.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace Framework.Web
{
    public class HttpHeader
    {
        public string Method { get; set; }
        public string Path { get; set; }
        public string Type { get; set; }
        public string Authorization { get; set; }
        public string ContentType { get; set; }
        public int ContentLength { get; set; }
        public string Content { get; set; } = "";
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string Cookie { get; set; }
        public bool KeepAlive { get; set; }
        public string Host { get; set; }
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
        public static byte[] CreateResponse(RequestContext requestContext)
        {
            var sb = new StringBuilder();

            using (var sw = new StringWriter(sb))
            {
                sw.WriteLine($"HTTP/1.1 {(int)requestContext.response.Status} {requestContext.response.Status}");
                sw.WriteLine($"Content-Length: {requestContext.response.Content.Length}");

                if (!requestContext.response.KeepAlive)
                    sw.WriteLine("Connection: close");

                if (!requestContext.response.Cookie.IsEmpty())
                    sw.WriteLine($"Set-Cookie: {requestContext.response.Cookie}");

                sw.WriteLine($"Content-Type: {requestContext.response.ContentType}");
                sw.WriteLine();

                sw.WriteLine(requestContext.response.Content);
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public static bool ParseRequest(byte[] data, int length, out HttpHeader httpHeader)
        {
            var headerValues = new Dictionary<string, object>();
            httpHeader = new HttpHeader();

            using (var sr = new StreamReader(new MemoryStream(data, 0, length)))
            {
                var info = sr.ReadLine().Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                if (info.Length != 3)
                    return false;

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
                        f.SetValue(httpHeader, Convert.ChangeType(Convert.ToInt32(val), f.PropertyType));
                    else
                        f.SetValue(httpHeader, Convert.ChangeType(val, f.PropertyType));
                }
            }

            return true;
        }
    }
}
