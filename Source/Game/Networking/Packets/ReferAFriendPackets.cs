// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System;

namespace Game.Networking.Packets
{
    public class RecruitAFriendFailure : ServerPacket
    {
        public RecruitAFriendFailure() : base(ServerOpcodes.RecruitAFriendFailure) { }

        public override void Write()
        {
            _worldPacket .WriteInt32((int)Reason);
            // Client uses this string only if Reason == ERR_REFER_A_FRIEND_NOT_IN_GROUP || Reason == ERR_REFER_A_FRIEND_SUMMON_OFFLINE_S
            // but always reads it from packet
            _worldPacket.WriteBits(Str.GetByteCount(), 6);
            _worldPacket.WriteString(Str);
        }

        public string Str;
        public ReferAFriendError Reason;
    }
}
