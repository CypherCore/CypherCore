/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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

using Framework.Constants;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class UpdateLastInstance : ServerPacket
    {
        public UpdateLastInstance() : base(ServerOpcodes.UpdateLastInstance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MapID);
        }

        public uint MapID { get; set; }
    }

    class InstanceInfoPkt : ServerPacket
    {
        public InstanceInfoPkt() : base(ServerOpcodes.InstanceInfo) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(LockList.Count);

            foreach (InstanceLockInfos lockInfos in LockList)
                lockInfos.Write(_worldPacket);
        }

        public List<InstanceLockInfos> LockList { get; set; } = new List<InstanceLockInfos>();
    }

    class ResetInstances : ClientPacket
    {
        public ResetInstances(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class InstanceReset : ServerPacket
    {
        public InstanceReset() : base(ServerOpcodes.InstanceReset) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MapID);
        }

        public uint MapID { get; set; }
    }

    class InstanceResetFailed : ServerPacket
    {
        public InstanceResetFailed() : base(ServerOpcodes.InstanceResetFailed) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MapID);
            _worldPacket.WriteBits(ResetFailedReason, 2);
            _worldPacket.FlushBits();
        }

        public uint MapID { get; set; }
        public ResetFailedReason ResetFailedReason { get; set; }
    }

    class ResetFailedNotify : ServerPacket
    {
        public ResetFailedNotify() : base(ServerOpcodes.ResetFailedNotify) { }

        public override void Write() { }
    }

    class InstanceSaveCreated : ServerPacket
    {
        public InstanceSaveCreated() : base(ServerOpcodes.InstanceSaveCreated) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Gm);
            _worldPacket.FlushBits();
        }

        public bool Gm { get; set; }
    }

    class InstanceLockResponse : ClientPacket
    {
        public InstanceLockResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            AcceptLock = _worldPacket.HasBit();
        }

        public bool AcceptLock { get; set; }
    }

    class RaidGroupOnly : ServerPacket
    {
        public RaidGroupOnly() : base(ServerOpcodes.RaidGroupOnly) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Delay);
            _worldPacket.WriteUInt32(Reason);
        }

        public int Delay { get; set; }
        public RaidGroupReason Reason { get; set; }
    }

    class PendingRaidLock : ServerPacket
    {
        public PendingRaidLock() : base(ServerOpcodes.PendingRaidLock) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(TimeUntilLock);
            _worldPacket.WriteUInt32(CompletedMask);
            _worldPacket.WriteBit(Extending);
            _worldPacket.WriteBit(WarningOnly);
            _worldPacket.FlushBits();
        }

        public int TimeUntilLock { get; set; }
        public uint CompletedMask { get; set; }
        public bool Extending { get; set; }
        public bool WarningOnly { get; set; }
    }

    class RaidInstanceMessage : ServerPacket
    {
        public RaidInstanceMessage() : base(ServerOpcodes.RaidInstanceMessage) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Type);
            _worldPacket.WriteUInt32(MapID);
            _worldPacket.WriteUInt32(DifficultyID);
            _worldPacket.WriteBit(Locked);
            _worldPacket.WriteBit(Extended);
            _worldPacket.FlushBits();
        }

        public InstanceResetWarningType Type { get; set; }
        public uint MapID { get; set; }
        public Difficulty DifficultyID { get; set; }
        public bool Locked { get; set; }
        public bool Extended { get; set; }
    }

    class InstanceEncounterEngageUnit : ServerPacket
    {
        public InstanceEncounterEngageUnit() : base(ServerOpcodes.InstanceEncounterEngageUnit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt8(TargetFramePriority);
        }

        public ObjectGuid Unit { get; set; }
        public byte TargetFramePriority; // used to set the initial position of the frame if multiple frames are sent
    }

    class InstanceEncounterDisengageUnit : ServerPacket
    {
        public InstanceEncounterDisengageUnit() : base(ServerOpcodes.InstanceEncounterDisengageUnit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
        }

        public ObjectGuid Unit { get; set; }
    }

    class InstanceEncounterChangePriority : ServerPacket
    {
        public InstanceEncounterChangePriority() : base(ServerOpcodes.InstanceEncounterChangePriority, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt8(TargetFramePriority);
        }

        public ObjectGuid Unit { get; set; }
        public byte TargetFramePriority; // used to update the position of the unit's current frame
    }

    class InstanceEncounterStart : ServerPacket
    {
        public InstanceEncounterStart() : base(ServerOpcodes.InstanceEncounterStart, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(InCombatResCount);
            _worldPacket.WriteUInt32(MaxInCombatResCount);
            _worldPacket.WriteUInt32(CombatResChargeRecovery);
            _worldPacket.WriteUInt32(NextCombatResChargeTime);
        }

        public uint InCombatResCount; // amount of usable battle ressurection.
        public uint MaxInCombatResCount { get; set; }
        public uint CombatResChargeRecovery { get; set; }
        public uint NextCombatResChargeTime { get; set; }
    }

    class InstanceEncounterEnd : ServerPacket
    {
        public InstanceEncounterEnd() : base(ServerOpcodes.InstanceEncounterEnd, ConnectionType.Instance) { }

        public override void Write() { }
    }

    class InstanceEncounterInCombatResurrection : ServerPacket
    {
        public InstanceEncounterInCombatResurrection() : base(ServerOpcodes.InstanceEncounterInCombatResurrection, ConnectionType.Instance) { }

        public override void Write() { }
    }

    class InstanceEncounterGainCombatResurrectionCharge : ServerPacket
    {
        public InstanceEncounterGainCombatResurrectionCharge() : base(ServerOpcodes.InstanceEncounterGainCombatResurrectionCharge, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(InCombatResCount);
            _worldPacket.WriteUInt32(CombatResChargeRecovery);
        }

        public int InCombatResCount { get; set; }
        public uint CombatResChargeRecovery { get; set; }
    }

    class BossKillCredit : ServerPacket
    {
        public BossKillCredit() : base(ServerOpcodes.BossKillCredit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(DungeonEncounterID);
        }

        public uint DungeonEncounterID { get; set; }
    }

    //Structs
    public struct InstanceLockInfos
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(MapID);
            data.WriteUInt32(DifficultyID);
            data.WriteUInt64(InstanceID);
            data.WriteInt32(TimeRemaining);
            data.WriteUInt32(CompletedMask);

            data.WriteBit(Locked);
            data.WriteBit(Extended);
            data.FlushBits();
        }

        public ulong InstanceID { get; set; }
        public uint MapID { get; set; }
        public uint DifficultyID { get; set; }
        public int TimeRemaining { get; set; }
        public uint CompletedMask { get; set; }

        public bool Locked { get; set; }
        public bool Extended { get; set; }
    }
}
