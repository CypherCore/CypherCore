// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Networking;
using System;

namespace Game.Entities
{
    class AaBox : IEquatable<AaBox>
    {
        public Position Low;
        public Position High;

        public void WriteCreate(WorldPacket data, BaseEntity owner, Player receiver)
        {
            data.WriteVector3(Low);
            data.WriteVector3(High);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, BaseEntity owner)
        {
            data.WriteVector3(Low);
            data.WriteVector3(High);
        }

        public bool Equals(AaBox right)
        {
            return Low == right.Low
                && High == right.High;
        }
    }
}
