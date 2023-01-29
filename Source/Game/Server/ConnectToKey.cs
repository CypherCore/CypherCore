// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game
{
    public struct ConnectToKey
    {
        public ulong Raw
        {
            get { return ((ulong)AccountId | ((ulong)connectionType << 32) | (Key << 33)); }
            set
            {
                AccountId = (uint)(value & 0xFFFFFFFF);
                connectionType = (ConnectionType)((value >> 32) & 1);
                Key = (value >> 33);
            }
        }

        public uint AccountId;
        public ConnectionType connectionType;
        public ulong Key;
    }
}
