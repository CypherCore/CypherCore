// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Framework.Configuration
{
    public class ConfigMgr
    {
        public static bool Load(string fileName)
        {
            string path = AppContext.BaseDirectory + fileName;
            if (!File.Exists(path))
            {
                Console.WriteLine("{0} doesn't exist!", fileName);
                return false;
            }

            string[] ConfigContent = File.ReadAllLines(path, Encoding.UTF8);

            int lineCounter = 0;
            try
            {
                foreach (var line in ConfigContent)
                {
                    lineCounter++;
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("-"))
                        continue;

                    var configOption = new StringArray(line, '=');
                    _configList.Add(configOption[0].Trim(), configOption[1].Replace("\"", "").Trim());
                }
            }
            catch
            {
                Console.WriteLine("Error in {0} on Line {1}", fileName, lineCounter);
                return false;
            }

            return true;
        }

        public static T GetDefaultValue<T>(string name, T defaultValue)
        {
            string temp = _configList.LookupByKey(name);

            var type = typeof(T).IsEnum ? typeof(T).GetEnumUnderlyingType() : typeof(T);

            if (temp.IsEmpty())
                return (T)Convert.ChangeType(defaultValue, type);

            if (Type.GetTypeCode(typeof(T)) == TypeCode.Boolean && temp.IsNumber())
                return (T)Convert.ChangeType(temp == "1", typeof(T));

            return (T)Convert.ChangeType(temp, type);
        }

        public static IEnumerable<string> GetKeysByString(string name)
        {
            return _configList.Where(p => p.Key.Contains(name)).Select(p => p.Key);
        }

        static Dictionary<string, string> _configList = new();
    }
}
