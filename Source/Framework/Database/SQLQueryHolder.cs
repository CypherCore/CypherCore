/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

namespace Framework.Database
{
    public class SQLQueryHolder<T>
    {
        public void SetQuery(T index, PreparedStatement _stmt)
        {
            m_queries[index] = new SQLResultPair(_stmt);
        }

        public void SetQuery(T index, string sql, params object[] args)
        {
            SetQuery(index, new PreparedStatement(string.Format(sql, args)));
        }

        public void SetResult(T index, SQLResult _result)
        {
            m_queries[index].result = _result;
        }

        public SQLResult GetResult(T index)
        {
            if (!m_queries.ContainsKey(index))
                return new SQLResult();

            return m_queries[index].result;
        }

        public Dictionary<T, SQLResultPair> m_queries = new Dictionary<T, SQLResultPair>();

        public class SQLResultPair
        {
            public SQLResultPair(PreparedStatement _stmt)
            {
                stmt = _stmt;
            }

            public PreparedStatement stmt;
            public SQLResult result;
        }
    }
}
