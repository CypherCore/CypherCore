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

namespace Framework.Database
{
    public interface ISqlCallback
    {
        bool InvokeIfReady();
    }

    public class AsyncCallbackProcessor<T> where T : ISqlCallback
    {   
        List<T> _callbacks = new List<T>();

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
