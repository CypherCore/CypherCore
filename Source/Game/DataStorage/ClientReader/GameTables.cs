/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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

using Framework.Collections;
using System;
using System.Collections.Generic;
using System.IO;

namespace Game.DataStorage
{
    public class GameTableReader
    {
        internal static GameTable<T> Read<T>(string fileName) where T : new()
        {
            GameTable<T> storage = new GameTable<T>();

            if (!File.Exists(CliDB.DataPath + fileName))
            {
                Log.outError(LogFilter.ServerLoading, "File {0} not found.", fileName);
                return storage;
            }
            using (var reader = new StreamReader(CliDB.DataPath + fileName))
            {
                string headers = reader.ReadLine();
                if (headers.IsEmpty())
                {
                    Log.outError(LogFilter.ServerLoading, "GameTable file {0} is empty.", fileName);
                    return storage;
                }

                List<T> data = new List<T>();
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

            CliDB.LoadedFileCount++;
            return storage;
        }
    }

    public class GameTable<T> where T : new()
    {
        public T GetRow(uint row)
        {
            if (row >= _data.Count)
                return default(T);

            return _data[(int)row];
        }

        public int GetTableRowCount() { return _data.Count; }

        public void SetData(List<T> data) { _data = data; }

        List<T> _data = new List<T>();
    }
}
