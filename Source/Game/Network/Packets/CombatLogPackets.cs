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
using Framework.Dynamic;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class CombatLogServerPacket : ServerPacket
    {
        public CombatLogServerPacket(ServerOpcodes opcode, ConnectionType connection = ConnectionType.Realm) : base(opcode, connection)
        {
            LogData = new SpellCastLogData();
        }

        public override void Write() { }

        public void DisableAdvancedCombatLogging()
        {
            LogData = null;
        }

        public void WriteLogDataBit()
        {
            _worldPacket.WriteBit(LogData != null);
        }

        public void FlushBits()
        {
            _worldPacket.FlushBits();
        }

        public void WriteLogData()
        {
            if (LogData != null)
                LogData.Write(_worldPacket);
        }

        internal SpellCastLogData LogData;
    }

    class SpellNonMeleeDamageLog : CombatLogServerPacket
    {
        public SpellNonMeleeDamageLog() : base(ServerOpcodes.SpellNonMeleeDamageLog, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Me);
            _worldPacket.WritePackedGuid(CasterGUID);
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WriteInt32(SpellID);
            _worldPacket.WriteInt32(SpellXSpellVisualID);
            _worldPacket.WriteInt32(Damage);
            _worldPacket.WriteInt32(Overkill);
            _worldPacket.WriteUInt8(SchoolMask);
            _worldPacket.WriteInt32(ShieldBlock);
            _worldPacket.WriteInt32(Resisted);
            _worldPacket.WriteInt32(Absorbed);

            _worldPacket.WriteBit(Periodic);
            _worldPacket.WriteBits(Flags, 7);
            _worldPacket.WriteBit(false); // Debug info
            WriteLogDataBit();
            _worldPacket.WriteBit(SandboxScaling.HasValue);
            FlushBits();
            WriteLogData();
            if (SandboxScaling.HasValue)
                SandboxScaling.Value.Write(_worldPacket);
        }

        public ObjectGuid Me { get; set; }
        public ObjectGuid CasterGUID { get; set; }
        public ObjectGuid CastID { get; set; }
        public int SpellID { get; set; }
        public int SpellXSpellVisualID { get; set; }
        public int Damage { get; set; }
        public int Overkill { get; set; }
        public byte SchoolMask { get; set; }
        public int ShieldBlock { get; set; }
        public int Resisted { get; set; }
        public bool Periodic { get; set; }
        public int Absorbed { get; set; }
        public int Flags { get; set; }
        // Optional<SpellNonMeleeDamageLogDebugInfo> DebugInfo;
        public Optional<SandboxScalingData> SandboxScaling { get; set; } = new Optional<SandboxScalingData>();
    }

    class EnvironmentalDamageLog : CombatLogServerPacket
    {
        public EnvironmentalDamageLog() : base(ServerOpcodes.EnvironmentalDamageLog) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Victim);
            _worldPacket.WriteUInt8(Type);
            _worldPacket.WriteInt32(Amount);
            _worldPacket.WriteInt32(Resisted);
            _worldPacket.WriteInt32(Absorbed);

            WriteLogDataBit();
            FlushBits();
            WriteLogData();
        }

        public ObjectGuid Victim { get; set; }
        public EnviromentalDamage Type { get; set; }
        public int Amount { get; set; }
        public int Resisted { get; set; }
        public int Absorbed { get; set; }
    }

    class SpellExecuteLog : CombatLogServerPacket
    {
        public SpellExecuteLog() : base(ServerOpcodes.SpellExecuteLog, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt32(Effects.Count);

            foreach (SpellLogEffect effect in Effects)
            {
                _worldPacket.WriteUInt32(effect.Effect);

                _worldPacket.WriteUInt32(effect.PowerDrainTargets.Count);
                _worldPacket.WriteUInt32(effect.ExtraAttacksTargets.Count);
                _worldPacket.WriteUInt32(effect.DurabilityDamageTargets.Count);
                _worldPacket.WriteUInt32(effect.GenericVictimTargets.Count);
                _worldPacket.WriteUInt32(effect.TradeSkillTargets.Count);
                _worldPacket.WriteUInt32(effect.FeedPetTargets.Count);

                foreach (SpellLogEffectPowerDrainParams powerDrainTarget in effect.PowerDrainTargets)
                {
                    _worldPacket.WritePackedGuid(powerDrainTarget.Victim);
                    _worldPacket.WriteUInt32(powerDrainTarget.Points);
                    _worldPacket.WriteUInt32(powerDrainTarget.PowerType);
                    _worldPacket.WriteFloat(powerDrainTarget.Amplitude);
                }

                foreach (SpellLogEffectExtraAttacksParams extraAttacksTarget in effect.ExtraAttacksTargets)
                {
                    _worldPacket.WritePackedGuid(extraAttacksTarget.Victim);
                    _worldPacket.WriteUInt32(extraAttacksTarget.NumAttacks);
                }

                foreach (SpellLogEffectDurabilityDamageParams durabilityDamageTarget in effect.DurabilityDamageTargets)
                {
                    _worldPacket.WritePackedGuid(durabilityDamageTarget.Victim);
                    _worldPacket.WriteInt32(durabilityDamageTarget.ItemID);
                    _worldPacket.WriteInt32(durabilityDamageTarget.Amount);
                }

                foreach (SpellLogEffectGenericVictimParams genericVictimTarget in effect.GenericVictimTargets)
                    _worldPacket.WritePackedGuid(genericVictimTarget.Victim);

                foreach (SpellLogEffectTradeSkillItemParams tradeSkillTarget in effect.TradeSkillTargets)
                    _worldPacket.WriteInt32(tradeSkillTarget.ItemID);


                foreach (SpellLogEffectFeedPetParams feedPetTarget in effect.FeedPetTargets)
                    _worldPacket.WriteInt32(feedPetTarget.ItemID);
            }

            WriteLogDataBit();
            FlushBits();
            WriteLogData();
        }

        public ObjectGuid Caster { get; set; }
        public uint SpellID { get; set; }
        public List<SpellLogEffect> Effects { get; set; } = new List<SpellLogEffect>();

        public class SpellLogEffect
        {
            public int Effect { get; set; }

            public List<SpellLogEffectPowerDrainParams> PowerDrainTargets { get; set; } = new List<SpellLogEffectPowerDrainParams>();
            public List<SpellLogEffectExtraAttacksParams> ExtraAttacksTargets { get; set; } = new List<SpellLogEffectExtraAttacksParams>();
            public List<SpellLogEffectDurabilityDamageParams> DurabilityDamageTargets { get; set; } = new List<SpellLogEffectDurabilityDamageParams>();
            public List<SpellLogEffectGenericVictimParams> GenericVictimTargets { get; set; } = new List<SpellLogEffectGenericVictimParams>();
            public List<SpellLogEffectTradeSkillItemParams> TradeSkillTargets { get; set; } = new List<SpellLogEffectTradeSkillItemParams>();
            public List<SpellLogEffectFeedPetParams> FeedPetTargets { get; set; } = new List<SpellLogEffectFeedPetParams>();
        }
    }

    class SpellHealLog : CombatLogServerPacket
    {
        public SpellHealLog() : base(ServerOpcodes.SpellHealLog, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(TargetGUID);
            _worldPacket.WritePackedGuid(CasterGUID);

            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt32(Health);
            _worldPacket.WriteUInt32(OverHeal);
            _worldPacket.WriteUInt32(Absorbed);

            _worldPacket.WriteBit(Crit);

            _worldPacket.WriteBit(CritRollMade.HasValue);
            _worldPacket.WriteBit(CritRollNeeded.HasValue);
            WriteLogDataBit();
            _worldPacket.WriteBit(SandboxScaling.HasValue);
            FlushBits();

            WriteLogData();

            if (CritRollMade.HasValue)
                _worldPacket.WriteFloat(CritRollMade.Value);

            if (CritRollNeeded.HasValue)
                _worldPacket.WriteFloat(CritRollNeeded.Value);

            if (SandboxScaling.HasValue)
                SandboxScaling.Value.Write(_worldPacket);
        }

        public ObjectGuid CasterGUID { get; set; }
        public ObjectGuid TargetGUID { get; set; }
        public uint SpellID { get; set; }
        public uint Health { get; set; }
        public uint OverHeal { get; set; }
        public uint Absorbed { get; set; }
        public bool Crit { get; set; }
        public Optional<float> CritRollMade { get; set; }
        public Optional<float> CritRollNeeded { get; set; }
        Optional<SandboxScalingData> SandboxScaling = new Optional<SandboxScalingData>();
    }

    class SpellPeriodicAuraLog : CombatLogServerPacket
    {
        public SpellPeriodicAuraLog() : base(ServerOpcodes.SpellPeriodicAuraLog, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(TargetGUID);
            _worldPacket.WritePackedGuid(CasterGUID);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt32(Effects.Count);
            WriteLogDataBit();
            FlushBits();

            Effects.ForEach(p => p.Write(_worldPacket));

            WriteLogData();
        }

        public ObjectGuid TargetGUID { get; set; }
        public ObjectGuid CasterGUID { get; set; }
        public uint SpellID { get; set; }
        public List<SpellLogEffect> Effects { get; set; } = new List<SpellLogEffect>();

        public struct PeriodicalAuraLogEffectDebugInfo
        {
            public float CritRollMade { get; set; }
            public float CritRollNeeded { get; set; }
        }

        public class SpellLogEffect
        {
            public void Write(WorldPacket data)
            {
                data.WriteUInt32(Effect);
                data.WriteUInt32(Amount);
                data.WriteUInt32(OverHealOrKill);
                data.WriteUInt32(SchoolMaskOrPower);
                data.WriteUInt32(AbsorbedOrAmplitude);
                data.WriteUInt32(Resisted);

                data.WriteBit(Crit);
                data.WriteBit(DebugInfo.HasValue);
                data.WriteBit(SandboxScaling.HasValue);
                data.FlushBits();

                if (SandboxScaling.HasValue)
                    SandboxScaling.Value.Write(data);

                if (DebugInfo.HasValue)
                {
                    data.WriteFloat(DebugInfo.Value.CritRollMade);
                    data.WriteFloat(DebugInfo.Value.CritRollNeeded);
                }
            }

            public uint Effect { get; set; }
            public uint Amount { get; set; }
            public uint OverHealOrKill { get; set; }
            public uint SchoolMaskOrPower { get; set; }
            public uint AbsorbedOrAmplitude { get; set; }
            public uint Resisted { get; set; }
            public bool Crit { get; set; }
            public Optional<PeriodicalAuraLogEffectDebugInfo> DebugInfo { get; set; }
            public Optional<SandboxScalingData> SandboxScaling { get; set; } = new Optional<SandboxScalingData>();
        }
    }

    class SpellInterruptLog : ServerPacket
    {
        public SpellInterruptLog() : base(ServerOpcodes.SpellInterruptLog, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WritePackedGuid(Victim);
            _worldPacket.WriteUInt32(InterruptedSpellID);
            _worldPacket.WriteUInt32(SpellID);
        }

        public ObjectGuid Caster { get; set; }
        public ObjectGuid Victim { get; set; }
        public uint InterruptedSpellID { get; set; }
        public uint SpellID { get; set; }
    }

    class SpellDispellLog : ServerPacket
    {
        public SpellDispellLog() : base(ServerOpcodes.SpellDispellLog, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsSteal);
            _worldPacket.WriteBit(IsBreak);
            _worldPacket.WritePackedGuid(TargetGUID);
            _worldPacket.WritePackedGuid(CasterGUID);
            _worldPacket.WriteUInt32(DispelledBySpellID);

            _worldPacket.WriteInt32(DispellData.Count);
            foreach (var data in DispellData)
            {
                _worldPacket.WriteUInt32(data.SpellID);
                _worldPacket.WriteBit(data.Harmful);
                _worldPacket.WriteBit(data.Rolled.HasValue);
                _worldPacket.WriteBit(data.Needed.HasValue);
                if (data.Rolled.HasValue)
                    _worldPacket.WriteUInt32(data.Rolled.Value);
                if (data.Needed.HasValue)
                    _worldPacket.WriteUInt32(data.Needed.Value);

                _worldPacket.FlushBits();
            }
        }

        public List<SpellDispellData> DispellData { get; set; } = new List<SpellDispellData>();
        public ObjectGuid CasterGUID { get; set; }
        public ObjectGuid TargetGUID { get; set; }
        public uint DispelledBySpellID { get; set; }
        public bool IsBreak { get; set; }
        public bool IsSteal { get; set; }
    }


    class SpellEnergizeLog : CombatLogServerPacket
    {
        public SpellEnergizeLog() : base(ServerOpcodes.SpellEnergizeLog, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(TargetGUID);
            _worldPacket.WritePackedGuid(CasterGUID);

            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt32(Type);
            _worldPacket.WriteInt32(Amount);
            _worldPacket.WriteInt32(OverEnergize);

            WriteLogDataBit();
            FlushBits();
            WriteLogData();
        }

        public ObjectGuid TargetGUID { get; set; }
        public ObjectGuid CasterGUID { get; set; }
        public uint SpellID { get; set; }
        public PowerType Type { get; set; }
        public int Amount { get; set; }
        public int OverEnergize { get; set; }
    }

    class SpellInstakillLog : ServerPacket
    {
        public SpellInstakillLog() : base(ServerOpcodes.SpellInstakillLog, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Target);
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WriteUInt32(SpellID);
        }

        public ObjectGuid Target { get; set; }
        public ObjectGuid Caster { get; set; }
        public uint SpellID { get; set; }
    }

    class SpellMissLog : ServerPacket
    {
        public SpellMissLog() : base(ServerOpcodes.SpellMissLog, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WriteUInt32(Entries);

            foreach (SpellLogMissEntry missEntry in Entries)
                missEntry.Write(_worldPacket);
        }

        public uint SpellID { get; set; }
        public ObjectGuid Caster { get; set; }
        public List<SpellLogMissEntry> Entries { get; set; } = new List<SpellLogMissEntry>();
    }

    class ProcResist : ServerPacket
    {
        public ProcResist() : base(ServerOpcodes.ProcResist) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WritePackedGuid(Target);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteBit(Rolled.HasValue);
            _worldPacket.WriteBit(Needed.HasValue);
            _worldPacket.FlushBits();

            if (Rolled.HasValue)
                _worldPacket.WriteFloat(Rolled.Value);

            if (Needed.HasValue)
                _worldPacket.WriteFloat(Needed.Value);
        }

        public ObjectGuid Caster { get; set; }
        public ObjectGuid Target { get; set; }
        public uint SpellID { get; set; }
        public Optional<float> Rolled { get; set; }
        public Optional<float> Needed { get; set; }
    }

    class SpellOrDamageImmune : ServerPacket
    {
        public SpellOrDamageImmune() : base(ServerOpcodes.SpellOrDamageImmune, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CasterGUID);
            _worldPacket.WritePackedGuid(VictimGUID);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteBit(IsPeriodic);
            _worldPacket.FlushBits();
        }

        public ObjectGuid CasterGUID { get; set; }
        public ObjectGuid VictimGUID { get; set; }
        public uint SpellID { get; set; }
        public bool IsPeriodic { get; set; }
    }

    class SpellDamageShield : CombatLogServerPacket
    {
        public SpellDamageShield() : base(ServerOpcodes.SpellDamageShield, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Attacker);
            _worldPacket.WritePackedGuid(Defender);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt32(TotalDamage);
            _worldPacket.WriteUInt32(OverKill);
            _worldPacket.WriteUInt32(SchoolMask);
            _worldPacket.WriteUInt32(LogAbsorbed);

            WriteLogDataBit();
            FlushBits();
            WriteLogData();
        }

        public ObjectGuid Attacker { get; set; }
        public ObjectGuid Defender { get; set; }
        public uint SpellID { get; set; }
        public uint TotalDamage { get; set; }
        public uint OverKill { get; set; }
        public uint SchoolMask { get; set; }
        public uint LogAbsorbed { get; set; }
    }

    class AttackerStateUpdate : CombatLogServerPacket
    {
        public AttackerStateUpdate() : base(ServerOpcodes.AttackerStateUpdate, ConnectionType.Instance)
        {
            SubDmg = new Optional<SubDamage>();
        }

        public override void Write()
        {
            WorldPacket attackRoundInfo = new WorldPacket();
            attackRoundInfo.WriteUInt32(hitInfo);
            attackRoundInfo.WritePackedGuid(AttackerGUID);
            attackRoundInfo.WritePackedGuid(VictimGUID);
            attackRoundInfo.WriteInt32(Damage);
            attackRoundInfo.WriteInt32(OverDamage);
            attackRoundInfo.WriteUInt8(SubDmg.HasValue);

            if (SubDmg.HasValue)
            {
                attackRoundInfo.WriteInt32(SubDmg.Value.SchoolMask);
                attackRoundInfo.WriteFloat(SubDmg.Value.FDamage);
                attackRoundInfo.WriteInt32(SubDmg.Value.Damage);
                if (hitInfo.HasAnyFlag(HitInfo.FullAbsorb | HitInfo.PartialAbsorb))
                    attackRoundInfo.WriteInt32(SubDmg.Value.Absorbed);
                if (hitInfo.HasAnyFlag(HitInfo.FullResist | HitInfo.PartialResist))
                    attackRoundInfo.WriteInt32(SubDmg.Value.Resisted);
            }

            attackRoundInfo.WriteUInt8(VictimState);
            attackRoundInfo.WriteInt32(AttackerState);
            attackRoundInfo.WriteInt32(MeleeSpellID);

            if (hitInfo.HasAnyFlag(HitInfo.Block))
                attackRoundInfo.WriteInt32(BlockAmount);

            if (hitInfo.HasAnyFlag(HitInfo.RageGain))
                attackRoundInfo.WriteInt32(RageGained);

            if (hitInfo.HasAnyFlag(HitInfo.Unk1))
            {
                attackRoundInfo.WriteUInt32(UnkState.State1);
                attackRoundInfo.WriteFloat(UnkState.State2);
                attackRoundInfo.WriteFloat(UnkState.State3);
                attackRoundInfo.WriteFloat(UnkState.State4);
                attackRoundInfo.WriteFloat(UnkState.State5);
                attackRoundInfo.WriteFloat(UnkState.State6);
                attackRoundInfo.WriteFloat(UnkState.State7);
                attackRoundInfo.WriteFloat(UnkState.State8);
                attackRoundInfo.WriteFloat(UnkState.State9);
                attackRoundInfo.WriteFloat(UnkState.State10);
                attackRoundInfo.WriteFloat(UnkState.State11);
                attackRoundInfo.WriteUInt32(UnkState.State12);
            }

            if (hitInfo.HasAnyFlag(HitInfo.Block | HitInfo.Unk12))
                attackRoundInfo.WriteFloat(Unk);

            attackRoundInfo.WriteUInt8(SandboxScaling.Type);
            attackRoundInfo.WriteUInt8(SandboxScaling.TargetLevel);
            attackRoundInfo.WriteUInt8(SandboxScaling.Expansion);
            attackRoundInfo.WriteUInt8(SandboxScaling.Class);
            attackRoundInfo.WriteUInt8(SandboxScaling.TargetMinScalingLevel);
            attackRoundInfo.WriteUInt8(SandboxScaling.TargetMaxScalingLevel);
            attackRoundInfo.WriteInt16(SandboxScaling.PlayerLevelDelta);
            attackRoundInfo.WriteInt8(SandboxScaling.TargetScalingLevelDelta);
            attackRoundInfo.WriteUInt16(SandboxScaling.PlayerItemLevel);

            WriteLogDataBit();
            FlushBits();
            WriteLogData();

            _worldPacket.WriteInt32(attackRoundInfo.GetSize());
            _worldPacket.WriteBytes(attackRoundInfo);
        }

        public HitInfo hitInfo; // Flags
        public ObjectGuid AttackerGUID { get; set; }
        public ObjectGuid VictimGUID { get; set; }
        public int Damage { get; set; }
        public int OverDamage { get; set; } = -1; // (damage - health) or -1 if unit is still alive
        public Optional<SubDamage> SubDmg { get; set; }
        public byte VictimState { get; set; }
        public uint AttackerState { get; set; }
        public uint MeleeSpellID { get; set; }
        public int BlockAmount { get; set; }
        public int RageGained { get; set; }
        public UnkAttackerState UnkState { get; set; }
        public float Unk { get; set; }
        public SandboxScalingData SandboxScaling { get; set; } = new SandboxScalingData();
    }

    //Structs
    struct SpellLogEffectPowerDrainParams
    {
        public ObjectGuid Victim { get; set; }
        public uint Points { get; set; }
        public uint PowerType { get; set; }
        public float Amplitude { get; set; }
    }

    struct SpellLogEffectExtraAttacksParams
    {
        public ObjectGuid Victim { get; set; }
        public uint NumAttacks { get; set; }
    }

    struct SpellLogEffectDurabilityDamageParams
    {
        public ObjectGuid Victim { get; set; }
        public int ItemID { get; set; }
        public int Amount { get; set; }
    }

    struct SpellLogEffectGenericVictimParams
    {
        public ObjectGuid Victim { get; set; }
    }

    struct SpellLogEffectTradeSkillItemParams
    {
        public int ItemID { get; set; }
    }

    struct SpellLogEffectFeedPetParams
    {
        public int ItemID { get; set; }
    }

    struct SpellLogMissDebug
    {
        public void Write(WorldPacket data)
        {
            data.WriteFloat(HitRoll);
            data.WriteFloat(HitRollNeeded);
        }

        public float HitRoll { get; set; }
        public float HitRollNeeded { get; set; }
    }

    public struct SpellLogMissEntry
    {
        public SpellLogMissEntry(ObjectGuid victim, byte missReason)
        {
            Victim = victim;
            MissReason = missReason;
            Debug = new Optional<SpellLogMissDebug>();
        }

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(Victim);
            data.WriteUInt8(MissReason);
            if (data.WriteBit(Debug.HasValue))
                Debug.Value.Write(data);

            data.FlushBits();
        }

        public ObjectGuid Victim { get; set; }
        public byte MissReason { get; set; }
        Optional<SpellLogMissDebug> Debug;
    }

    struct SpellDispellData
    {
        public uint SpellID { get; set; }
        public bool Harmful { get; set; }
        public Optional<int> Rolled { get; set; }
        public Optional<int> Needed { get; set; }
    }

    public struct SubDamage
    {
        public int SchoolMask { get; set; }
        public float FDamage; // Float damage (Most of the time equals to Damage)
        public int Damage { get; set; }
        public int Absorbed { get; set; }
        public int Resisted { get; set; }
    }

    public struct UnkAttackerState
    {
        public uint State1 { get; set; }
        public float State2 { get; set; }
        public float State3 { get; set; }
        public float State4 { get; set; }
        public float State5 { get; set; }
        public float State6 { get; set; }
        public float State7 { get; set; }
        public float State8 { get; set; }
        public float State9 { get; set; }
        public float State10 { get; set; }
        public float State11 { get; set; }
        public uint State12 { get; set; }
    }
}
