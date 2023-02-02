// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Framework.Database
{
    public interface ISqlCallback
    {
        bool InvokeIfReady();
    }

    public class AsyncCallbackProcessor<T> where T : ISqlCallback
    {   
        List<T> _callbacks = new();

        public T AddCallback(T query)
        {
            _callbacks.Add(query);
            return query;
        }

        public void ProcessReadyCallbacks()
        {
            if (_callbacks.Empty())
                return;

            _callbacks.RemoveAll(callback => callback.InvokeIfReady());
        }
    }
}
