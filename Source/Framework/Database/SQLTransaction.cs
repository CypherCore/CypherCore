// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using MySqlConnector;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace Framework.Database
{
    public class SQLTransaction
    {
        public List<MySqlCommand> commands { get; }

        public SQLTransaction()
        {
            commands = new List<MySqlCommand>();
        }

        public void Append(PreparedStatement stmt)
        {
            MySqlCommand cmd = new(stmt.CommandText);
            foreach (var parameter in stmt.Parameters)
                cmd.Parameters.AddWithValue("@" + parameter.Key, parameter.Value);

            commands.Add(cmd);
        }

        public void Append(string sql, params object[] args)
        {
            commands.Add(new MySqlCommand(string.Format(sql, args)));
        }
    }

    class TransactionTask : ISqlOperation
    {
        public TransactionTask(SQLTransaction trans)
        {
            m_trans = trans;
        }

        public virtual bool Execute<T>(MySqlBase<T> mySqlBase)
        {
            MySqlErrorCode errorCode = TryExecute(mySqlBase);
            if (errorCode == MySqlErrorCode.None)
                return true;

            if (errorCode == MySqlErrorCode.LockDeadlock)
            {
                // Make sure only 1 async thread retries a transaction so they don't keep dead-locking each other
                lock (_deadlockLock)
                {
                    byte loopBreaker = 5;  // Handle MySQL Errno 1213 without extending deadlock to the core itself
                    for (byte i = 0; i < loopBreaker; ++i)
                        if (TryExecute(mySqlBase) == MySqlErrorCode.None)
                            return true;
                }
            }

            return false;
        }

        public MySqlErrorCode TryExecute<T>(MySqlBase<T> mySqlBase)
        {
            return mySqlBase.DirectCommitTransaction(m_trans);
        }

        SQLTransaction m_trans;
        public static object _deadlockLock = new();
    }

    class TransactionWithResultTask : TransactionTask
    {
        public TransactionWithResultTask(SQLTransaction trans) : base(trans) { }

        public override bool Execute<T>(MySqlBase<T> mySqlBase)
        {
            MySqlErrorCode errorCode = TryExecute(mySqlBase);
            if (errorCode == MySqlErrorCode.None)
            {
                m_result.SetResult(true);
                return true;
            }

            if (errorCode == MySqlErrorCode.LockDeadlock)
            {
                // Make sure only 1 async thread retries a transaction so they don't keep dead-locking each other
                lock (_deadlockLock)
                {
                    byte loopBreaker = 5;  // Handle MySQL Errno 1213 without extending deadlock to the core itself
                    for (byte i = 0; i < loopBreaker; ++i)
                    {
                        if (TryExecute(mySqlBase) == MySqlErrorCode.None)
                        {
                            m_result.SetResult(true);
                            return true;
                        }
                    }
                }
            }

            m_result.SetResult(false);
            return false;
        }

        public Task<bool> GetFuture() { return m_result.Task; }

        TaskCompletionSource<bool> m_result = new();
    }

    public class TransactionCallback : ISqlCallback
    {
        public TransactionCallback(Task<bool> future)
        {
            m_future = future;
        }

        public void AfterComplete(Action<bool> callback)
        {
            m_callback = callback;
        }

        public bool InvokeIfReady()
        {
            if (m_future != null && m_future.Wait(0))
            {
                m_callback(m_future.Result);
                return true;
            }

            return false;
        }

        Task<bool> m_future;
        Action<bool> m_callback;
    }
}
