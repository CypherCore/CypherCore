/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace Framework.Database
{
    public class MySqlConnectionInfo
    {
        public MySqlConnectionInfo(string info, int poolSize = 10)
        {
            var lines = new StringArray(info, ';');
            Host = lines[0];
            Port = lines[1];
            Username = lines[2];
            Password = lines[3];
            Database = lines[4];
            Poolsize = poolSize;
        }

	    public MySqlConnectionInfo(ConnectionObject connectionObject, int poolSize = 10)
	    {
		    Host = connectionObject.Host;
		    Port = connectionObject.Port;
		    Username = connectionObject.Username;
		    Password = connectionObject.Password;
		    Database = connectionObject.Database;
		    Poolsize = poolSize;
	    }

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(string.Format("Server={0};Port={1};User Id={2};Password={3};Database={4};Allow Zero Datetime=True;Allow User Variables=True;Pooling=true;MaximumPoolSize={5};",
                Host, Port, Username, Password, Database, Poolsize));
        }

        public MySqlConnection GetConnectionNoDatabase()
        {
            return new MySqlConnection(string.Format("Server={0};Port={1};User Id={2};Password={3};Allow Zero Datetime=True;Allow User Variables=True;Pooling=true;MaximumPoolSize={4};",
                Host, Port, Username, Password, Poolsize));
        }

        public string Host;
        public string Port;
        public string Username;
        public string Password;
        public string Database;
        public int Poolsize;
    }

    public abstract class MySqlBase<T>
    {
        public MySqlErrorCode Initialize(string infoString)
        {
            _connectionInfo = new MySqlConnectionInfo(infoString);
            _updater = new DatabaseUpdater<T>(this);

            using (var connection = _connectionInfo.GetConnection())
            {
                try
                {
                    connection.Open();
                    Log.outInfo(LogFilter.SqlDriver, "Connected to MySQL(ver: {0}) Database: {1}", connection.ServerVersion, _connectionInfo.Database);
                    return MySqlErrorCode.None;
                }
                catch (MySqlException ex)
                {
                    return (MySqlErrorCode)((MySqlException)ex.InnerException).Number;
                }
            }
        }

	    public MySqlErrorCode Initialize(ConnectionObject connectionObject)
	    {
		    _connectionInfo = new MySqlConnectionInfo(connectionObject);
		    _updater = new DatabaseUpdater<T>(this);

		    using (var connection = _connectionInfo.GetConnection())
		    {
			    try
			    {
				    connection.Open();
				    Log.outInfo(LogFilter.SqlDriver, "Connected to MySQL(ver: {0}) Database: {1}", connection.ServerVersion, _connectionInfo.Database);
				    return MySqlErrorCode.None;
			    }
			    catch (MySqlException ex)
			    {
				    return (MySqlErrorCode)((MySqlException)ex.InnerException).Number;
			    }
		    }
	    }

        public void Execute(string sql, params object[] args)
        {
            Execute(new PreparedStatement(string.Format(sql, args)));
        }
        public void Execute(PreparedStatement stmt)
        {
            using (var Connection = _connectionInfo.GetConnection())
            {
                Connection.Open();
                using (MySqlCommand cmd = Connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = stmt.CommandText;
                        foreach (var parameter in stmt.Parameters)
                            cmd.Parameters.AddWithValue("@" + parameter.Key, parameter.Value);

                        cmd.ExecuteNonQuery();
                    }
                    catch (MySqlException ex)
                    {
                        HandleMySQLException(ex, stmt.CommandText);
                    }
                }
            }
        }

        public void ExecuteOrAppend(SQLTransaction trans, PreparedStatement stmt)
        {
            if (trans == null)
                Execute(stmt);
            else
                trans.Append(stmt);
        }

        public SQLResult Query(string sql, params object[] args)
        {
            return Query(new PreparedStatement(string.Format(sql, args)));
        }

        public SQLResult Query(PreparedStatement stmt)
        {
            List<object[]> rows = new List<object[]>();
            using (var Connection = _connectionInfo.GetConnection())
            {
                Connection.Open();
                using (MySqlCommand cmd = Connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = stmt.CommandText;
                        foreach (var parameter in stmt.Parameters)
                            cmd.Parameters.AddWithValue("@" + parameter.Key, parameter.Value);

                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read() && reader.HasRows)
                            {
                                do
                                {
                                    var row = new object[reader.FieldCount];

                                    reader.GetValues(row);
                                    rows.Add(row);
                                }
                                while (reader.Read());
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        HandleMySQLException(ex, stmt.CommandText);
                    }
                }
            }

            return new SQLResult(rows);
        }

        public QueryCallback AsyncQuery(string sql, params object[] args)
        {
            return AsyncQuery(new PreparedStatement(string.Format(sql, args)));
        }

        public QueryCallback AsyncQuery(PreparedStatement stmt)
        {
            return new QueryCallback(_AsyncQuery(stmt));
        }

        async Task<SQLResult> _AsyncQuery(PreparedStatement stmt)
        {
            List<object[]> rows = new List<object[]>();

            try
            {
                using (var Connection = _connectionInfo.GetConnection())
                {
                    Connection.Open();
                    using (MySqlCommand cmd = Connection.CreateCommand())
                    {
                        cmd.CommandText = stmt.CommandText;
                        foreach (var parameter in stmt.Parameters)
                            cmd.Parameters.AddWithValue("@" + parameter.Key, parameter.Value);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (reader.Read() && reader.HasRows)
                            {
                                do
                                {
                                    var row = new object[reader.FieldCount];

                                    reader.GetValues(row);
                                    rows.Add(row);
                                }
                                while (reader.Read());
                            }
                        }
                    }
                }
            }
            catch (MySqlException ex)
            {
                HandleMySQLException(ex, stmt.CommandText);
            }

            return new SQLResult(rows);
        }

        public async Task<SQLQueryHolder<R>> DelayQueryHolder<R>(SQLQueryHolder<R> holder)
        {
            return await Task.Run(async () =>
            {
                string query = "";

                try
                {
                    using (var Connection = _connectionInfo.GetConnection())
                    {
                        Connection.Open();

                        foreach (var pair in holder.m_queries)
                        {
                            List<object[]> rows = new List<object[]>();
                            using (MySqlCommand cmd = Connection.CreateCommand())
                            {
                                cmd.CommandText = pair.Value.stmt.CommandText;
                                foreach (var parameter in pair.Value.stmt.Parameters)
                                    cmd.Parameters.AddWithValue("@" + parameter.Key, parameter.Value);

                                query = cmd.CommandText;
                                using (var reader = await cmd.ExecuteReaderAsync())
                                {
                                    if (reader.Read() && reader.HasRows)
                                    {
                                        do
                                        {
                                            var row = new object[reader.FieldCount];

                                            reader.GetValues(row);
                                            rows.Add(row);
                                        }
                                        while (reader.Read());
                                    }
                                }
                            }

                            holder.SetResult(pair.Key, new SQLResult(rows));
                        }
                    }

                    return holder;
                }
                catch (MySqlException ex)
                {
                    HandleMySQLException(ex, query);
                    return holder;
                }
            });
        }

        public void LoadPreparedStatements()
        {
            PreparedStatements();
        }

        public void PrepareStatement(T statement, string sql)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            for (var i = 0; i < sql.Length; i++)
            {
                if (sql[i].Equals('?'))
                    sb.Append("@" + index++);
                else
                    sb.Append(sql[i]);
            }

            _queries[statement] = sb.ToString();
        }

        public PreparedStatement GetPreparedStatement(T statement)
        {
            return new PreparedStatement(_queries[statement]);
        }

        public bool Apply(string sql)
        {
            using (var Connection = _connectionInfo.GetConnectionNoDatabase())
            {
                using (MySqlCommand cmd = Connection.CreateCommand())
                {
                    try
                    {
                        Connection.Open();
                        cmd.CommandText = sql;
                        return cmd.ExecuteNonQuery() > 0;
                    }
                    catch (MySqlException ex)
                    {
                        HandleMySQLException(ex, sql);
                        return false;
                    }
                }
            }
        }

        public bool ApplyFile(string path)
        {
            using (var connection = _connectionInfo.GetConnection())
            {
                using (MySqlCommand cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = File.ReadAllText(path);
                        return cmd.ExecuteNonQuery() > 0;
                    }
                    catch (MySqlException ex)
                    {
                        HandleMySQLException(ex, path);
                        return false;
                    }
                }
            }
        }

        public void EscapeString(ref string str)
        {
            str = MySqlHelper.EscapeString(str);
        }

        public void CommitTransaction(SQLTransaction transaction)
        {
            using (var Connection = _connectionInfo.GetConnection())
            {
                string query = "";
                Connection.Open();
                using (MySqlTransaction trans = Connection.BeginTransaction())
                {
                    try
                    {
                        using (var scope = new TransactionScope())
                        {
                            foreach (var cmd in transaction.commands)
                            {
                                cmd.Transaction = trans;
                                cmd.Connection = Connection;
                                cmd.ExecuteNonQuery();
                                query = cmd.CommandText;
                            }

                            trans.Commit();
                            scope.Complete();
                        }
                    }
                    catch (MySqlException ex) //error occurred
                    {
                        HandleMySQLException(ex, query);
                        trans.Rollback();
                    }
                }
            }
        }

        void HandleMySQLException(MySqlException ex, string query)
        {
            int code = ex.Number;
            if (ex.InnerException != null)
                code = ((MySqlException)ex.InnerException).Number;

            switch ((MySqlErrorCode)code)
            {
                case MySqlErrorCode.BadFieldError:
                case MySqlErrorCode.NoSuchTable:
                    Log.outError(LogFilter.Sql, "Your database structure is not up to date. Please make sure you've executed all queries in the sql/updates folders.");
                    break;
                case MySqlErrorCode.ParseError:
                    Log.outError(LogFilter.Sql, "Error while parsing SQL. Core fix required.");
                    break;
            }

            Log.outError(LogFilter.Sql, "SqlException: {0} SqlQuery: {1}", ex.Message, query);
        }

        public DatabaseUpdater<T> GetUpdater()
        {
            return _updater;
        }

        public bool IsAutoUpdateEnabled(DatabaseTypeFlags updateMask)
        {
            switch (GetType().Name)
            {
                case "LoginDatabase":
                    return updateMask.HasAnyFlag(DatabaseTypeFlags.Login);
                case "CharacterDatabase":
                    return updateMask.HasAnyFlag(DatabaseTypeFlags.Character);
                case "WorldDatabase":
                    return updateMask.HasAnyFlag(DatabaseTypeFlags.World);
                case "HotfixDatabase":
                    return updateMask.HasAnyFlag(DatabaseTypeFlags.Hotfix);
            }
            return false;
        }

        public string GetDatabaseName()
        {
            return _connectionInfo.Database;
        }

        public abstract void PreparedStatements();

        Dictionary<T, string> _queries = new Dictionary<T, string>();
        MySqlConnectionInfo _connectionInfo;
        DatabaseUpdater<T> _updater;
    }
}
