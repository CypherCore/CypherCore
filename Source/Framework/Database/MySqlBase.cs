﻿/*
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

using Framework.Threading;
using MySqlConnector;
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
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection($"Server={Host};Port={Port};User Id={Username};Password={Password};Database={Database};Allow User Variables=True;Pooling=true;");
        }

        public MySqlConnection GetConnectionNoDatabase()
        {
            return new MySqlConnection($"Server={Host};Port={Port};User Id={Username};Password={Password};Allow User Variables=True;Pooling=true;");
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
        Dictionary<T, string> _preparedQueries = new Dictionary<T, string>();
        ProducerConsumerQueue<ISqlOperation> _queue = new ProducerConsumerQueue<ISqlOperation>();

        MySqlConnectionInfo _connectionInfo;
        DatabaseUpdater<T> _updater;
        DatabaseWorker<T> _worker;

        public MySqlErrorCode Initialize(MySqlConnectionInfo connectionInfo)
        {
            _connectionInfo = connectionInfo;
            _updater = new DatabaseUpdater<T>(this);
            _worker = new DatabaseWorker<T>(_queue, this);

            try
            {
                using (var connection = _connectionInfo.GetConnection())
                {
                    connection.Open();
                    Log.outInfo(LogFilter.SqlDriver, $"Connected to MySQL(ver: {connection.ServerVersion}) Database: {_connectionInfo.Database}");
                    return MySqlErrorCode.None;
                }
            }
            catch (MySqlException ex)
            {
                return HandleMySQLException(ex);
            }
        }

        public bool DirectExecute(string sql, params object[] args)
        {
            return DirectExecute(new PreparedStatement(string.Format(sql, args)));
        }

        public bool DirectExecute(PreparedStatement stmt)
        {
            try
            {
                using (var Connection = _connectionInfo.GetConnection())
                {
                    Connection.Open();
                    using (var cmd = Connection.CreateCommand())
                    {
                        cmd.CommandText = stmt.CommandText;
                        foreach (var parameter in stmt.Parameters)
                            cmd.Parameters.AddWithValue("@" + parameter.Key, parameter.Value);

                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (MySqlException ex)
            {
                HandleMySQLException(ex, stmt.CommandText, stmt.Parameters);
                return false;
            }
        }

        public void Execute(string sql, params object[] args)
        {
            Execute(new PreparedStatement(string.Format(sql, args)));
        }

        public void Execute(PreparedStatement stmt)
        {
            var task = new PreparedStatementTask(stmt);
            _queue.Push(task);
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
            try
            {
                var Connection = _connectionInfo.GetConnection();
                Connection.Open();

                var cmd = Connection.CreateCommand();
                cmd.CommandText = stmt.CommandText;
                foreach (var parameter in stmt.Parameters)
                    cmd.Parameters.AddWithValue("@" + parameter.Key, parameter.Value);

                return new SQLResult(cmd.ExecuteReader(System.Data.CommandBehavior.CloseConnection));
            }
            catch (MySqlException ex)
            {
                HandleMySQLException(ex, stmt.CommandText, stmt.Parameters);
                return new SQLResult();
            }
        }

        public QueryCallback AsyncQuery(PreparedStatement stmt)
        {
            var task = new PreparedStatementTask(stmt, true);
            // Store future result before enqueueing - task might get already processed and deleted before returning from this method
            var result = task.GetFuture();
            _queue.Push(task);
            return new QueryCallback(result);
        }

        public Task<SQLQueryHolder<R>> DelayQueryHolder<R>(SQLQueryHolder<R> holder)
        {
            var task = new SQLQueryHolderTask<R>(holder);
            // Store future result before enqueueing - task might get already processed and deleted before returning from this method
            var result = task.GetFuture();
            _queue.Push(task);
            return result;
        }

        public void LoadPreparedStatements()
        {
            PreparedStatements();
        }

        public void PrepareStatement(T statement, string sql)
        {
            var sb = new StringBuilder();
            var index = 0;
            for (var i = 0; i < sql.Length; i++)
            {
                if (sql[i].Equals('?'))
                    sb.Append("@" + index++);
                else
                    sb.Append(sql[i]);
            }

            _preparedQueries[statement] = sb.ToString();
        }

        public PreparedStatement GetPreparedStatement(T statement)
        {
            return new PreparedStatement(_preparedQueries[statement]);
        }

        public bool Apply(string sql)
        {
            try
            {
                using (var Connection = _connectionInfo.GetConnectionNoDatabase())
                {
                    Connection.Open();
                    using (var cmd = Connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (MySqlException ex)
            {
                HandleMySQLException(ex, sql);
                return false;
            }
        }

        public bool ApplyFile(string path)
        {
            try
            {
                var query = File.ReadAllText(path);
                if (query.IsEmpty())
                    return false;

                if (Encoding.UTF8.GetByteCount(query) > 1048576) //Default size limit of querys
                    Apply("SET GLOBAL max_allowed_packet=1073741824;");

                using (var connection = _connectionInfo.GetConnection())
                {
                    connection.Open();
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandTimeout = 120;
                        cmd.CommandText = query;
                        cmd.ExecuteNonQuery();
                        return true;
                    }
                }
            }
            catch (MySqlException ex)
            {
                HandleMySQLException(ex, path);
                return false;
            }
        }

        public void EscapeString(ref string str)
        {
            str = MySqlHelper.EscapeString(str);
        }

        public void CommitTransaction(SQLTransaction transaction)
        {
            _queue.Push(new TransactionTask(transaction));
        }

        public TransactionCallback AsyncCommitTransaction(SQLTransaction transaction)
        {
            var task = new TransactionWithResultTask(transaction);
            var result = task.GetFuture();
            _queue.Push(task);
            return new TransactionCallback(result);
        }

        public MySqlErrorCode DirectCommitTransaction(SQLTransaction transaction)
        {
            using (var Connection = _connectionInfo.GetConnection())
            {
                var query = "";

                Connection.Open();
                using (var trans = Connection.BeginTransaction())
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
                        return  MySqlErrorCode.None;
                    }
                    catch (MySqlException ex) //error occurred
                    {
                        trans.Rollback();
                        return HandleMySQLException(ex, query);
                    }
                }
            }
        }

        MySqlErrorCode HandleMySQLException(MySqlException ex, string query = "", Dictionary<int, object> parameters = null)
        {
            var code = (MySqlErrorCode)ex.Number;
            if (ex.InnerException is MySqlException)
                code = (MySqlErrorCode)((MySqlException)ex.InnerException).Number;

            var stringBuilder = new StringBuilder($"SqlException: MySqlErrorCode: {code} Message: {ex.Message} SqlQuery: {query} ");
            if (parameters != null)
            {
                stringBuilder.Append("Parameters: ");
                foreach (var pair in parameters)
                    stringBuilder.Append($"{pair.Key} : {pair.Value}");
            }

            Log.outError(LogFilter.Sql, stringBuilder.ToString());

            switch (code)
            {
                case MySqlErrorCode.BadFieldError:
                case MySqlErrorCode.NoSuchTable:
                    Log.outError(LogFilter.Sql, "Your database structure is not up to date. Please make sure you've executed all queries in the sql/updates folders.");
                    break;
                case MySqlErrorCode.ParseError:
                    Log.outError(LogFilter.Sql, "Error while parsing SQL. Core fix required.");
                    break;
            }

            return code;
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
    }
}
