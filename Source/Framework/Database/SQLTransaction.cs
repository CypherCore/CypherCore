// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;

namespace Framework.Database
{
    public class SQLTransaction
    {
        public SQLTransaction()
        {
            commands = new List<MySqlCommand>();
        }

        public List<MySqlCommand> commands { get; }

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

    internal class TransactionTask : ISqlOperation
    {
        public static object _deadlockLock = new();

        private readonly SQLTransaction _trans;

        public TransactionTask(SQLTransaction trans)
        {
            _trans = trans;
        }

        public virtual bool Execute<T>(MySqlBase<T> mySqlBase)
        {
            MySqlErrorCode errorCode = TryExecute(mySqlBase);

            if (errorCode == MySqlErrorCode.None)
                return true;

            if (errorCode == MySqlErrorCode.LockDeadlock)
                // Make sure only 1 async thread retries a transaction so they don't keep dead-locking each other
                lock (_deadlockLock)
                {
                    byte loopBreaker = 5; // Handle MySQL Errno 1213 without extending deadlock to the core itself

                    for (byte i = 0; i < loopBreaker; ++i)
                        if (TryExecute(mySqlBase) == MySqlErrorCode.None)
                            return true;
                }

            return false;
        }

        public MySqlErrorCode TryExecute<T>(MySqlBase<T> mySqlBase)
        {
            return mySqlBase.DirectCommitTransaction(_trans);
        }
    }

    internal class TransactionWithResultTask : TransactionTask
    {
        private readonly TaskCompletionSource<bool> _result = new();

        public TransactionWithResultTask(SQLTransaction trans) : base(trans)
        {
        }

        public override bool Execute<T>(MySqlBase<T> mySqlBase)
        {
            MySqlErrorCode errorCode = TryExecute(mySqlBase);

            if (errorCode == MySqlErrorCode.None)
            {
                _result.SetResult(true);

                return true;
            }

            if (errorCode == MySqlErrorCode.LockDeadlock)
                // Make sure only 1 async thread retries a transaction so they don't keep dead-locking each other
                lock (_deadlockLock)
                {
                    byte loopBreaker = 5; // Handle MySQL Errno 1213 without extending deadlock to the core itself

                    for (byte i = 0; i < loopBreaker; ++i)
                        if (TryExecute(mySqlBase) == MySqlErrorCode.None)
                        {
                            _result.SetResult(true);

                            return true;
                        }
                }

            _result.SetResult(false);

            return false;
        }

        public Task<bool> GetFuture()
        {
            return _result.Task;
        }
    }

    public class TransactionCallback : ISqlCallback
    {
        private readonly Task<bool> _future;
        private Action<bool> _callback;

        public TransactionCallback(Task<bool> future)
        {
            _future = future;
        }

        public bool InvokeIfReady()
        {
            if (_future != null &&
                _future.Wait(0))
            {
                _callback(_future.Result);

                return true;
            }

            return false;
        }

        public void AfterComplete(Action<bool> callback)
        {
            _callback = callback;
        }
    }
}