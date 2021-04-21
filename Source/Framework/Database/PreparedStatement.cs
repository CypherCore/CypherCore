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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.Database
{
    public class PreparedStatement
    {
        public string CommandText;
        public Dictionary<int, object> Parameters = new Dictionary<int, object>();

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
