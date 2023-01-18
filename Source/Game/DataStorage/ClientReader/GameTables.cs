// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using System;
using System.Collections.Generic;
using System.IO;

namespace Game.DataStorage
{
    public class GameTableReader
    {
        internal static GameTable<T> Read<T>(string path, string fileName, ref uint loadedFileCount) where T : new()
        {
            GameTable<T> storage = new();

            if (!File.Exists(path + fileName))
            {
                Log.outError(LogFilter.ServerLoading, "File {0} not found.", fileName);
                return storage;
            }
            using (var reader = new StreamReader(path + fileName))
            {
                string headers = reader.ReadLine();
                if (headers.IsEmpty())
                {
                    Log.outError(LogFilter.ServerLoading, "GameTable file {0} is empty.", fileName);
                    return storage;
                }

                List<T> data = new();
                data.Add(new T()); // row id 0, unused

                string line;
                while (!(line = reader.ReadLine()).IsEmpty())
                {
                    var values = new StringArray(line, '\t');
                    if (values.IsEmpty())
                        break;

                    var obj = new T();
                    var fields = obj.GetType().GetFields();
                    for (int fieldIndex = 0, valueIndex = 1; fieldIndex < fields.Length && valueIndex < values.Length; ++fieldIndex, ++valueIndex)
                    {
                        var field = fields[fieldIndex];
                        if (field.FieldType.IsArray)
                        {
                            Array array = (Array)field.GetValue(obj);
                            for (var i = 0; i < array.Length; ++i)
                                array.SetValue(float.Parse(values[valueIndex++]), i);
                        }
                        else
                            fields[fieldIndex].SetValue(obj, float.Parse(values[valueIndex]));
                    }

                    data.Add(obj);
                }

                storage.SetData(data);
            }

            loadedFileCount++;
            return storage;
        }
    }

    public class GameTable<T> where T : new()
    {
        public T GetRow(uint row)
        {
            if (row >= _data.Count)
                return default;

            return _data[(int)row];
        }

        public int GetTableRowCount() { return _data.Count; }

        public void SetData(List<T> data) { _data = data; }

        List<T> _data = new();
    }
}
