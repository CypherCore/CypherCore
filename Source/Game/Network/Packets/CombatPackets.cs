/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
    public class AttackSwing : ClientPacket
    {
        public AttackSwing(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Victim = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Victim;
    }

    public class AttackSwingError : ServerPacket
    {
        public AttackSwingError(AttackSwingErr reason = AttackSwingErr.CantAttack) : base(ServerOpcodes.AttackSwingError)
        {
            Reason = reason;
        }

        public override void Write()
        {
            _worldPacket.WriteBits((uint)Reason, 3);
            _worldPacket.FlushBits();
        }

        AttackSwingErr Reason;
    }

    public class AttackStop : ClientPacket
    {
        public AttackStop(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class AttackStart : ServerPacket
    {
        public AttackStart() : base(ServerOpcodes.AttackStart, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Attacker);
            _worldPacket.WritePackedGuid(Victim);
        }

        public ObjectGuid Attacker;
        public ObjectGuid Victim;
    }

    public class SAttackStop : ServerPacket
    {
        public SAttackStop(Unit attacker, Unit victim) : base(ServerOpcodes.AttackStop, ConnectionType.Instance)
        {
            Attacker = attacker.GetGUID();
            if (victim)
            {
                Victim = victim.GetGUID();
                NowDead = victim.IsDead();
            }
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Attacker);
            _worldPacket.WritePackedGuid(Victim);
            _worldPacket.WriteBit(NowDead);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Attacker;
        public ObjectGuid Victim;
        public bool NowDead;
    }

    public class ThreatUpdate : ServerPacket
    {
        public ThreatUpdate() : base(ServerOpcodes.ThreatUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteInt32(ThreatList.Count);
            foreach (ThreatInfo threatInfo in ThreatList)
            {
                _worldPacket.WritePackedGuid(threatInfo.UnitGUID);
                _worldPacket.WriteInt64(threatInfo.Threat);
            }
        }

        public ObjectGuid UnitGUID;
        public List<ThreatInfo> ThreatList = new List<ThreatInfo>();
    }

    public class HighestThreatUpdate : ServerPacket
    {
        public HighestThreatUpdate() : base(ServerOpcodes.HighestThreatUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WritePackedGuid(HighestThreatGUID);
            _worldPacket.WriteInt32(ThreatList.Count);
            foreach (ThreatInfo threatInfo in ThreatList)
            {
                _worldPacket.WritePackedGuid(threatInfo.UnitGUID);
                _worldPacket.WriteInt64(threatInfo.Threat);
            }
        }

        public ObjectGuid UnitGUID;
        public List<ThreatInfo> ThreatList = new List<ThreatInfo>();
        public ObjectGuid HighestThreatGUID;
    }

    public class ThreatRemove : ServerPacket
    {
        public ThreatRemove() : base(ServerOpcodes.ThreatRemove, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WritePackedGuid(AboutGUID);
        }

        public ObjectGuid AboutGUID; // Unit to remove threat from (e.g. player, pet, guardian)
        public ObjectGuid UnitGUID; // Unit being attacked (e.g. creature, boss)
    }

    public class AIReaction : ServerPacket
    {
        public AIReaction() : base(ServerOpcodes.AiReaction, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteUInt32(Reaction);
        }

        public ObjectGuid UnitGUID;
        public AiReaction Reaction;
    }

    public class CancelCombat : ServerPacket
    {
        public CancelCombat() : base(ServerOpcodes.CancelCombat) { }

        public override void Write() { }
    }

    public class PowerUpdate : ServerPacket
    {
        public PowerUpdate() : base(ServerOpcodes.PowerUpdate)
        {
            Powers = new List<PowerUpdatePower>();
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteUInt32(Powers.Count);
            foreach (var power in Powers)
            {
                _worldPacket.WriteInt32(power.Power);
                _worldPacket.WriteUInt8(power.PowerType);
            }
        }

        public ObjectGuid Guid;
        public List<PowerUpdatePower> Powers;
    }

    public class SetSheathed : ClientPacket
    {
        public SetSheathed(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CurrentSheathState = _worldPacket.ReadInt32();
            Animate = _worldPacket.HasBit();
        }

        public int CurrentSheathState;
        public bool Animate = true;
    }

    public class CancelAutoRepeat : ServerPacket
    {
        public CancelAutoRepeat() : base(ServerOpcodes.CancelAutoRepeat) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid;
    }

    public class HealthUpdate : ServerPacket
    {
        public HealthUpdate() : base(ServerOpcodes.HealthUpdate) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteInt64(Health);
        }

        public ObjectGuid Guid;
        public long Health;
    }

    public class ThreatClear : ServerPacket
    {
        public ThreatClear() : base(ServerOpcodes.ThreatClear) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
        }

        public ObjectGuid UnitGUID;
    }

    class PvPCredit : ServerPacket
    {
        public PvPCredit() : base(ServerOpcodes.PvpCredit) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(OriginalHonor);
            _worldPacket.WriteInt32(Honor);
            _worldPacket.WritePackedGuid(Target);
            _worldPacket.WriteUInt32(Rank);
        }

        public int OriginalHonor;
        public int Honor;
        public ObjectGuid Target;
        public uint Rank;
    }

    class BreakTarget : ServerPacket
    {
        public BreakTarget() : base(ServerOpcodes.BreakTarget) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
        }

        public ObjectGuid UnitGUID;
    }

    //Structs
    public struct ThreatInfo
    {
        public ObjectGuid UnitGUID;
        public long Threat;
    }

    public struct PowerUpdatePower
    {
        public PowerUpdatePower(int power, byte powerType)
        {
            Power = power;
            PowerType = powerType;
        }

        public int Power;
        public byte PowerType;
    }
}
