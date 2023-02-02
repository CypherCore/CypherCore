// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    class UpdateLastInstance : ServerPacket
    {
        public UpdateLastInstance() : base(ServerOpcodes.UpdateLastInstance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(MapID);
        }

        public uint MapID;
    }

    class InstanceInfoPkt : ServerPacket
    {
        public InstanceInfoPkt() : base(ServerOpcodes.InstanceInfo) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(LockList.Count);

            foreach (InstanceLockPkt lockInfos in LockList)
                lockInfos.Write(_worldPacket);
        }

        public List<InstanceLockPkt> LockList = new();
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

        public uint MapID;
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

        public uint MapID;
        public ResetFailedReason ResetFailedReason;
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

        public bool Gm;
    }

    class InstanceLockResponse : ClientPacket
    {
        public InstanceLockResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            AcceptLock = _worldPacket.HasBit();
        }

        public bool AcceptLock;
    }

    class RaidGroupOnly : ServerPacket
    {
        public RaidGroupOnly() : base(ServerOpcodes.RaidGroupOnly) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Delay);
            _worldPacket.WriteUInt32((uint)Reason);
        }

        public int Delay;
        public RaidGroupReason Reason;
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

        public int TimeUntilLock;
        public uint CompletedMask;
        public bool Extending;
        public bool WarningOnly;
    }

    class RaidInstanceMessage : ServerPacket
    {
        public RaidInstanceMessage() : base(ServerOpcodes.RaidInstanceMessage) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Type);
            _worldPacket.WriteUInt32(MapID);
            _worldPacket.WriteUInt32((uint)DifficultyID);
            _worldPacket.WriteBit(Locked);
            _worldPacket.WriteBit(Extended);
            _worldPacket.FlushBits();
        }

        public InstanceResetWarningType Type;
        public uint MapID;
        public Difficulty DifficultyID;
        public bool Locked;
        public bool Extended;
    }

    class InstanceEncounterEngageUnit : ServerPacket
    {
        public InstanceEncounterEngageUnit() : base(ServerOpcodes.InstanceEncounterEngageUnit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt8(TargetFramePriority);
        }

        public ObjectGuid Unit;
        public byte TargetFramePriority; // used to set the initial position of the frame if multiple frames are sent
    }

    class InstanceEncounterDisengageUnit : ServerPacket
    {
        public InstanceEncounterDisengageUnit() : base(ServerOpcodes.InstanceEncounterDisengageUnit, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
        }

        public ObjectGuid Unit;
    }

    class InstanceEncounterChangePriority : ServerPacket
    {
        public InstanceEncounterChangePriority() : base(ServerOpcodes.InstanceEncounterChangePriority, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt8(TargetFramePriority);
        }

        public ObjectGuid Unit;
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
            _worldPacket.WriteBit(InProgress);
            _worldPacket.FlushBits();
        }

        public uint InCombatResCount; // amount of usable battle ressurections
        public uint MaxInCombatResCount;
        public uint CombatResChargeRecovery;
        public uint NextCombatResChargeTime;
        public bool InProgress = true;
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

        public int InCombatResCount;
        public uint CombatResChargeRecovery;
    }

    class BossKill : ServerPacket
    {
        public BossKill() : base(ServerOpcodes.BossKill, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(DungeonEncounterID);
        }

        public uint DungeonEncounterID;
    }

    //Structs
    public struct InstanceLockPkt
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

        public ulong InstanceID;
        public uint MapID;
        public uint DifficultyID;
        public int TimeRemaining;
        public uint CompletedMask;

        public bool Locked;
        public bool Extended;
    }
}
