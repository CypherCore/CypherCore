// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.Database
{
    public class PreparedStatement
    {
        public string CommandText;
        public Dictionary<int, object> Parameters = new();

        public PreparedStatement(string commandText)
        {
            CommandText = commandText;
        }

        public void AddValue(int index, sbyte value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, byte value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, short value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, ushort value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, int value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, uint value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, long value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, ulong value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, float value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, double value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, byte[] value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, string value)
        {
            Parameters.Add(index, value);
        }

        public void AddValue(int index, bool value)
        {
            Parameters.Add(index, value);
        }

        public void AddNull(int index)
        {
            Parameters.Add(index, null);
        }

        public void Clear()
        {
            Parameters.Clear();
        }
    }

    public class PreparedStatementTask : ISqlOperation
    {
        PreparedStatement m_stmt;
        bool _needsResult;
        public SQLResult Result { get; private set; }

        public PreparedStatementTask(PreparedStatement stmt, bool needsResult = false)
        {
            m_stmt = stmt;
            _needsResult = needsResult;
        }

        public bool Execute<T>(MySqlBase<T> mySqlBase)
        {
            if (_needsResult)
            {
                SQLResult result = mySqlBase.Query(m_stmt);
                if (result == null)
                {
                    Result = new SQLResult();
                    return false;
                }

                Result = result;
                return true;
            }

            return mySqlBase.DirectExecute(m_stmt);
        }
    }
}
