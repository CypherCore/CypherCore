// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
