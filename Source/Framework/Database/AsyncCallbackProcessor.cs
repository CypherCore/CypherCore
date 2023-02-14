// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
