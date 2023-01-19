﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
