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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.Database
{
    public class SQLQueryHolder<T>
    {
        public Dictionary<T, PreparedStatement> m_queries = new Dictionary<T, PreparedStatement>();
        Dictionary<T, SQLResult> _results = new Dictionary<T, SQLResult>();

        public void SetQuery(T index, string sql, params object[] args)
        {
            SetQuery(index, new PreparedStatement(string.Format(sql, args)));
        }

        public void SetQuery(T index, PreparedStatement stmt)
        {
            m_queries[index] = stmt;
        }

        public void SetResult(T index, SQLResult result)
        {
            _results[index] = result;
        }

        public SQLResult GetResult(T index)
        {
            if (!_results.ContainsKey(index))
                return new SQLResult();

            return _results[index];
        }
    }

    class SQLQueryHolderTask<R> : ISqlOperation
    {
        SQLQueryHolder<R> m_holder;
        TaskCompletionSource<SQLQueryHolder<R>> m_result;

        public SQLQueryHolderTask(SQLQueryHolder<R> holder)
        {
            m_holder = holder;
            m_result = new TaskCompletionSource<SQLQueryHolder<R>>();
        }

        public bool Execute<T>(MySqlBase<T> mySqlBase)
        {
            if (m_holder == null)
                return false;

            // execute all queries in the holder and pass the results
            foreach (var pair in m_holder.m_queries)
                m_holder.SetResult(pair.Key, mySqlBase.Query(pair.Value));

            return m_result.TrySetResult(m_holder);
        }

        public Task<SQLQueryHolder<R>> GetFuture() { return m_result.Task; }
    }
}
