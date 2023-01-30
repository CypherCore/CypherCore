// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Game.DataStorage
{
    public struct Value32
    {
        private uint _value;

        public T As<T>() where T : unmanaged
        {
            return Unsafe.As<uint, T>(ref _value);
        }
    }
}