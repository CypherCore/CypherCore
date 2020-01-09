/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Framework.Serialization
{
    public class Json
    {
        public static string CreateString<T>(T dataObject)
        {
            return Encoding.UTF8.GetString(CreateArray(dataObject));
        }

        public static byte[] CreateArray<T>(T dataObject)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var stream = new MemoryStream();

            serializer.WriteObject(stream, dataObject);

            return stream.ToArray();
        }

        public static T CreateObject<T>(Stream jsonData)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));

            return (T)serializer.ReadObject(jsonData);
        }

        public static T CreateObject<T>(string jsonData, bool split = false)
        {
            return CreateObject<T>(Encoding.UTF8.GetBytes(split ? jsonData.Split(new[] { ':' }, 2)[1] : jsonData));
        }

        public static T CreateObject<T>(byte[] jsonData) => CreateObject<T>(new MemoryStream(jsonData));

        public static object CreateObject(Stream jsonData, Type type)
        {
            var serializer = new DataContractJsonSerializer(type);

            return serializer.ReadObject(jsonData);
        }

        public static object CreateObject(string jsonData, Type type, bool split = false)
        {
            return CreateObject(Encoding.UTF8.GetBytes(split ? jsonData.Split(new[] { ':' }, 2)[1] : jsonData), type);
        }

        public static object CreateObject(byte[] jsonData, Type type) => CreateObject(new MemoryStream(jsonData), type);

        // Used for protobuf json strings.
        public static byte[] Deflate<T>(string name, T data)
        {
            var jsonData = Encoding.UTF8.GetBytes(name + ":" + CreateString(data) + "\0");
            var compressedData = IO.ZLib.Compress(jsonData);

            return BitConverter.GetBytes(jsonData.Length).Combine(compressedData);
        }
    }
}
