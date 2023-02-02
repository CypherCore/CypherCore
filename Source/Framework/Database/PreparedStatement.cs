// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        TaskCompletionSource<SQLResult> m_result;

        public PreparedStatementTask(PreparedStatement stmt, bool needsResult = false)
        {
            m_stmt = stmt;
            _needsResult = needsResult;
            if (needsResult)
                m_result = new TaskCompletionSource<SQLResult>();
        }

        public bool Execute<T>(MySqlBase<T> mySqlBase)
        {
            if (_needsResult)
            {
                SQLResult result = mySqlBase.Query(m_stmt);
                if (result == null)
                {
                    m_result.SetResult(new SQLResult());
                    return false;
                }

                m_result.SetResult(result);
                return true;
            }

            return mySqlBase.DirectExecute(m_stmt);
        }

        public Task<SQLResult> GetFuture() { return m_result.Task; }
    }
}
