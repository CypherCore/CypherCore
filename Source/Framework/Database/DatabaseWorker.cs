// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Threading;
using Framework.Threading;

namespace Framework.Database
{
	public interface ISqlOperation
	{
		bool Execute<T>(MySqlBase<T> mySqlBase);
	}

	internal class DatabaseWorker<T>
	{
		private volatile bool _cancelationToken;
		private MySqlBase<T> _mySqlBase;
		private ProducerConsumerQueue<ISqlOperation> _queue;
		private Thread _workerThread;

		public DatabaseWorker(ProducerConsumerQueue<ISqlOperation> newQueue, MySqlBase<T> mySqlBase)
		{
			_queue            = newQueue;
			_mySqlBase        = mySqlBase;
			_cancelationToken = false;
			_workerThread     = new Thread(WorkerThread);
			_workerThread.Start();
		}

		private void WorkerThread()
		{
			if (_queue == null)
				return;

			for (;;)
			{
				ISqlOperation operation;

				_queue.WaitAndPop(out operation);

				if (_cancelationToken || operation == null)
					return;

				operation.Execute(_mySqlBase);
			}
		}
	}
}