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
using Framework.GameMath;
using Game.Entities;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class CancelAura : ClientPacket
    {
        public CancelAura(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SpellID = _worldPacket.ReadUInt32();
            CasterGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid CasterGUID { get; set; }
        public uint SpellID { get; set; }
    }

    class CancelAutoRepeatSpell : ClientPacket
    {
        public CancelAutoRepeatSpell(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class CancelChannelling : ClientPacket
    {
        public CancelChannelling(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ChannelSpell = _worldPacket.ReadInt32();
        }

        int ChannelSpell;
    }

    class CancelGrowthAura : ClientPacket
    {
        public CancelGrowthAura(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class CancelMountAura : ClientPacket
    {
        public CancelMountAura(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class RequestCategoryCooldowns : ClientPacket
    {
        public RequestCategoryCooldowns(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class SpellCategoryCooldown : ServerPacket
    {
        public List<CategoryCooldownInfo> CategoryCooldowns { get; set; } = new List<CategoryCooldownInfo>();

        public SpellCategoryCooldown() : base(ServerOpcodes.CategoryCooldown, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(CategoryCooldowns.Count);

            foreach (CategoryCooldownInfo cooldown in CategoryCooldowns)
            {
                _worldPacket.WriteUInt32(cooldown.Category);
                _worldPacket.WriteInt32(cooldown.ModCooldown);
            }
        }

        public class CategoryCooldownInfo
        {
            public CategoryCooldownInfo(uint category, int cooldown)
            {
                Category = category;
                ModCooldown = cooldown;
            }

            public uint Category { get; set; } = 0; // SpellCategory Id
            public int ModCooldown { get; set; } = 0; // Reduced Cooldown in ms
        }
    }

    public class SendKnownSpells : ServerPacket
    {
        public SendKnownSpells() : base(ServerOpcodes.SendKnownSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(InitialLogin);
            _worldPacket.WriteUInt32(KnownSpells.Count);
            _worldPacket.WriteUInt32(FavoriteSpells.Count);

            foreach (var spellId in KnownSpells)
                _worldPacket.WriteUInt32(spellId);

            foreach (var spellId in FavoriteSpells)
                _worldPacket.WriteUInt32(spellId);
        }

        public bool InitialLogin { get; set; }
        public List<uint> KnownSpells { get; set; } = new List<uint>();
        public List<uint> FavoriteSpells { get; set; } = new List<uint>(); // tradeskill recipes
    }

    public class UpdateActionButtons : ServerPacket
    {
        public ulong[] ActionButtons { get; set; } = new ulong[PlayerConst.MaxActionButtons];
        public byte Reason { get; set; }

        public UpdateActionButtons() : base(ServerOpcodes.UpdateActionButtons, ConnectionType.Instance) { }

        public override void Write()
        {
            for (var i = 0; i < PlayerConst.MaxActionButtons; ++i)
                _worldPacket.WriteUInt64(ActionButtons[i]);

            _worldPacket.WriteUInt8(Reason);
        }
    }

    public class SetActionButton : ClientPacket
    {
        public SetActionButton(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Action = _worldPacket.ReadUInt64();
            Index = _worldPacket.ReadUInt8();
        }

        public uint GetButtonAction() => (uint)(Action & 0x00000000FFFFFFFF);
        public uint GetButtonType() => (uint)((Action & 0xFFFFFFFF00000000) >> 56);

        public ulong Action; // two packed public uint (action and type)
        public byte Index { get; set; }
    }

    public class SendUnlearnSpells : ServerPacket
    {
        List<uint> Spells = new List<uint>();

        public SendUnlearnSpells() : base(ServerOpcodes.SendUnlearnSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Spells.Count);
            foreach (var spell in Spells)
                _worldPacket.WriteUInt32(spell);
        }
    }

    public class AuraUpdate : ServerPacket
    {
        public AuraUpdate() : base(ServerOpcodes.AuraUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(UpdateAll);
            _worldPacket.WriteBits(Auras.Count, 9);
            foreach (AuraInfo aura in Auras)
                aura.Write(_worldPacket);

            _worldPacket.WritePackedGuid(UnitGUID);
        }

        public bool UpdateAll { get; set; }
        public ObjectGuid UnitGUID { get; set; }
        public List<AuraInfo> Auras { get; set; } = new List<AuraInfo>();
    }

    public class CastSpell : ClientPacket
    {
        public SpellCastRequest Cast { get; set; }

        public CastSpell(WorldPacket packet) : base(packet)
        {
            Cast = new SpellCastRequest();
        }

        public override void Read()
        {
            Cast.Read(_worldPacket);
        }
    }

    public class PetCastSpell : ClientPacket
    {
        public ObjectGuid PetGUID { get; set; }
        public SpellCastRequest Cast { get; set; }

        public PetCastSpell(WorldPacket packet) : base(packet)
        {
            Cast = new SpellCastRequest();
        }

        public override void Read()
        {
            PetGUID = _worldPacket.ReadPackedGuid();
            Cast.Read(_worldPacket);
        }
    }

    public class UseItem : ClientPacket
    {
        public byte PackSlot { get; set; }
        public byte Slot { get; set; }
        public ObjectGuid CastItem { get; set; }
        public SpellCastRequest Cast { get; set; }

        public UseItem(WorldPacket packet) : base(packet)
        {
            Cast = new SpellCastRequest();
        }

        public override void Read()
        {
            PackSlot = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
            CastItem = _worldPacket.ReadPackedGuid();
            Cast.Read(_worldPacket);
        }
    }

    class SpellPrepare : ServerPacket
    {
        public SpellPrepare() : base(ServerOpcodes.SpellPrepare) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ClientCastID);
            _worldPacket.WritePackedGuid(ServerCastID);
        }

        public ObjectGuid ClientCastID { get; set; }
        public ObjectGuid ServerCastID { get; set; }
    }

    class SpellGo : CombatLogServerPacket
    {
        public SpellGo() : base(ServerOpcodes.SpellGo, ConnectionType.Instance) { }

        public override void Write()
        {
            Cast.Write(_worldPacket);

            WriteLogDataBit();
            FlushBits();

            WriteLogData();
        }

        public SpellCastData Cast { get; set; } = new SpellCastData();
    }

    public class SpellStart : ServerPacket
    {
        public SpellCastData Cast { get; set; }

        public SpellStart() : base(ServerOpcodes.SpellStart, ConnectionType.Instance)
        {
            Cast = new SpellCastData();
        }

        public override void Write()
        {
            Cast.Write(_worldPacket);
        }
    }

    public class SupercededSpells : ServerPacket
    {
        public SupercededSpells() : base(ServerOpcodes.SupercededSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID.Count);
            _worldPacket.WriteUInt32(Superceded.Count);
            _worldPacket.WriteUInt32(FavoriteSpellID.Count);

            foreach (var spellId in SpellID)
                _worldPacket.WriteUInt32(spellId);

            foreach (var spellId in Superceded)
                _worldPacket.WriteUInt32(spellId);

            foreach (var spellId in FavoriteSpellID)
                _worldPacket.WriteInt32(spellId);
        }

        public List<uint> SpellID { get; set; } = new List<uint>();
        public List<uint> Superceded { get; set; } = new List<uint>();
        public List<int> FavoriteSpellID { get; set; } = new List<int>();
    }

    public class LearnedSpells : ServerPacket
    {
        public LearnedSpells() : base(ServerOpcodes.LearnedSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID.Count);
            _worldPacket.WriteUInt32(FavoriteSpellID.Count);

            foreach (int spell in SpellID)
                _worldPacket.WriteInt32(spell);

            foreach (int spell in FavoriteSpellID)
                _worldPacket.WriteInt32(spell);

            _worldPacket.WriteBit(SuppressMessaging);
            _worldPacket.FlushBits();
        }

        public List<uint> SpellID { get; set; } = new List<uint>();
        public List<int> FavoriteSpellID { get; set; } = new List<int>();
        public bool SuppressMessaging { get; set; }
    }

    public class SpellFailure : ServerPacket
    {
        public SpellFailure() : base(ServerOpcodes.SpellFailure, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CasterUnit);
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WriteInt32(SpellID);
            _worldPacket.WriteUInt32(SpellXSpellVisualID);
            _worldPacket.WriteUInt16(Reason);
        }

        public ObjectGuid CasterUnit { get; set; }
        public uint SpellID { get; set; }
        public uint SpellXSpellVisualID { get; set; }
        public ushort Reason { get; set; }
        public ObjectGuid CastID { get; set; }
    }

    public class SpellFailedOther : ServerPacket
    {
        public SpellFailedOther() : base(ServerOpcodes.SpellFailedOther, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CasterUnit);
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt32(SpellXSpellVisualID);
            _worldPacket.WriteUInt16(Reason);
        }

        public ObjectGuid CasterUnit { get; set; }
        public uint SpellID { get; set; }
        public uint SpellXSpellVisualID { get; set; }
        public ushort Reason { get; set; }
        public ObjectGuid CastID { get; set; }
    }

    class CastFailedBase : ServerPacket
    {
        public CastFailedBase(ServerOpcodes serverOpcodes, ConnectionType connectionType) : base(serverOpcodes, connectionType) { }

        public override void Write() { throw new NotImplementedException(); }

        public ObjectGuid CastID { get; set; }
        public int SpellID { get; set; }
        public SpellCastResult Reason { get; set; }
        public int FailedArg1 { get; set; } = -1;
        public int FailedArg2 { get; set; } = -1;
    }

    class CastFailed : CastFailedBase
    {
        public CastFailed() : base(ServerOpcodes.CastFailed, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WriteInt32(SpellID);
            _worldPacket.WriteInt32(SpellXSpellVisualID);
            _worldPacket.WriteInt32(Reason);
            _worldPacket.WriteInt32(FailedArg1);
            _worldPacket.WriteInt32(FailedArg2);
        }

        public int SpellXSpellVisualID { get; set; }
    }

    class PetCastFailed : CastFailedBase
    {
        public PetCastFailed() : base(ServerOpcodes.PetCastFailed, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WriteInt32(SpellID);
            _worldPacket.WriteInt32(Reason);
            _worldPacket.WriteInt32(FailedArg1);
            _worldPacket.WriteInt32(FailedArg2);
        }
    }

    public class SetSpellModifier : ServerPacket
    {
        public SetSpellModifier(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Modifiers.Count);
            foreach (SpellModifierInfo spellMod in Modifiers)
                spellMod.Write(_worldPacket);
        }

        public List<SpellModifierInfo> Modifiers { get; set; } = new List<SpellModifierInfo>();
    }

    public class UnlearnedSpells : ServerPacket
    {
        public UnlearnedSpells() : base(ServerOpcodes.UnlearnedSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID.Count);
            foreach (uint spellId in SpellID)
                _worldPacket.WriteUInt32(spellId);

            _worldPacket.WriteBit(SuppressMessaging);
            _worldPacket.FlushBits();
        }

        public List<uint> SpellID { get; set; } = new List<uint>();
        public bool SuppressMessaging { get; set; }
    }

    public class CooldownEvent : ServerPacket
    {
        public CooldownEvent(bool isPet, uint spellId) : base(ServerOpcodes.CooldownEvent, ConnectionType.Instance)
        {
            IsPet = isPet;
            SpellID = spellId;
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(SpellID);
            _worldPacket.WriteBit(IsPet);
            _worldPacket.FlushBits();
        }

        public bool IsPet { get; set; }
        public uint SpellID { get; set; }
    }

    public class ClearCooldowns : ServerPacket
    {
        public ClearCooldowns() : base(ServerOpcodes.ClearCooldowns, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID.Count);
            if (!SpellID.Empty())
                SpellID.ForEach(p => _worldPacket.WriteUInt32(p));

            _worldPacket.WriteBit(IsPet);
            _worldPacket.FlushBits();
        }

        public List<uint> SpellID { get; set; } = new List<uint>();
        public bool IsPet { get; set; }
    }

    public class ClearCooldown : ServerPacket
    {
        public ClearCooldown() : base(ServerOpcodes.ClearCooldown, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteBit(ClearOnHold);
            _worldPacket.WriteBit(IsPet);
            _worldPacket.FlushBits();
        }

        public bool IsPet { get; set; }
        public uint SpellID { get; set; }
        public bool ClearOnHold { get; set; }
    }

    public class ModifyCooldown : ServerPacket
    {
        public ModifyCooldown() : base(ServerOpcodes.ModifyCooldown, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteInt32(DeltaTime);
            _worldPacket.WriteBit(IsPet);
            _worldPacket.FlushBits();
        }

        public bool IsPet { get; set; }
        public int DeltaTime { get; set; }
        public uint SpellID { get; set; }
    }

    public class SpellCooldownPkt : ServerPacket
    {
        public SpellCooldownPkt() : base(ServerOpcodes.SpellCooldown, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WriteUInt8(Flags);
            _worldPacket.WriteUInt32(SpellCooldowns.Count);
            SpellCooldowns.ForEach(p => p.Write(_worldPacket));
        }

        public List<SpellCooldownStruct> SpellCooldowns { get; set; } = new List<SpellCooldownStruct>();
        public ObjectGuid Caster { get; set; }
        public SpellCooldownFlags Flags { get; set; }
    }

    public class SendSpellHistory : ServerPacket
    {
        public SendSpellHistory() : base(ServerOpcodes.SendSpellHistory, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Entries.Count);
            Entries.ForEach(p => p.Write(_worldPacket));
        }

        public List<SpellHistoryEntry> Entries { get; set; } = new List<SpellHistoryEntry>();
    }

    public class ClearAllSpellCharges : ServerPacket
    {
        public ClearAllSpellCharges() : base(ServerOpcodes.ClearAllSpellCharges, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsPet);
            _worldPacket.FlushBits();
        }

        public bool IsPet { get; set; }
    }

    public class ClearSpellCharges : ServerPacket
    {
        public ClearSpellCharges() : base(ServerOpcodes.ClearSpellCharges, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Category);
            _worldPacket.WriteBit(IsPet);
            _worldPacket.FlushBits();
        }

        public bool IsPet { get; set; }
        public uint Category { get; set; }
    }

    public class SetSpellCharges : ServerPacket
    {
        public SetSpellCharges() : base(ServerOpcodes.SetSpellCharges) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Category);
            _worldPacket.WriteUInt32(NextRecoveryTime);
            _worldPacket.WriteUInt8(ConsumedCharges);
            _worldPacket.WriteFloat(ChargeModRate);
            _worldPacket.WriteBit(IsPet);
            _worldPacket.FlushBits();
        }

        public bool IsPet { get; set; }
        public uint Category { get; set; }
        public uint NextRecoveryTime { get; set; }
        public byte ConsumedCharges { get; set; }
        public float ChargeModRate { get; set; } = 1.0f;
    }

    public class SendSpellCharges : ServerPacket
    {
        public SendSpellCharges() : base(ServerOpcodes.SendSpellCharges, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Entries.Count);
            Entries.ForEach(p => p.Write(_worldPacket));
        }

        public List<SpellChargeEntry> Entries { get; set; } = new List<SpellChargeEntry>();
    }

    public class ClearTarget : ServerPacket
    {
        public ClearTarget() : base(ServerOpcodes.ClearTarget) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid { get; set; }
    }

    public class CancelOrphanSpellVisual : ServerPacket
    {
        public CancelOrphanSpellVisual() : base(ServerOpcodes.CancelOrphanSpellVisual) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellVisualID);
        }

        public uint SpellVisualID { get; set; }
    }

    public class CancelSpellVisual : ServerPacket
    {
        public CancelSpellVisual() : base(ServerOpcodes.CancelSpellVisual) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Source);
            _worldPacket.WriteUInt32(SpellVisualID);
        }

        public ObjectGuid Source { get; set; }
        public uint SpellVisualID { get; set; }
    }

    class CancelSpellVisualKit : ServerPacket
    {
        public CancelSpellVisualKit() : base(ServerOpcodes.CancelSpellVisualKit) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Source);
            _worldPacket.WriteUInt32(SpellVisualKitID);
        }

        public ObjectGuid Source { get; set; }
        public uint SpellVisualKitID { get; set; }
    }

    class PlayOrphanSpellVisual : ServerPacket
    {
        public PlayOrphanSpellVisual() : base(ServerOpcodes.PlayOrphanSpellVisual) { }

        public override void Write()
        {
            _worldPacket.WriteXYZ(SourceLocation);
            _worldPacket.WriteVector3(SourceRotation);
            _worldPacket.WriteVector3(TargetLocation);
            _worldPacket.WritePackedGuid(Target);
            _worldPacket.WriteUInt32(SpellVisualID);
            _worldPacket.WriteFloat(TravelSpeed);
            _worldPacket.WriteFloat(UnkZero);
            _worldPacket.WriteBit(SpeedAsTime);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Target; // Exclusive with TargetLocation
        public Position SourceLocation { get; set; }
        public uint SpellVisualID { get; set; }
        public bool SpeedAsTime { get; set; }
        public float TravelSpeed { get; set; }
        public float UnkZero; // Always zero
        public Vector3 SourceRotation; // Vector of rotations, Orientation is z
        public Vector3 TargetLocation; // Exclusive with Target
    }

    class PlaySpellVisual : ServerPacket
    {
        public PlaySpellVisual() : base(ServerOpcodes.PlaySpellVisual) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Source);
            _worldPacket.WritePackedGuid(Target);
            _worldPacket.WriteVector3(TargetPosition);
            _worldPacket.WriteUInt32(SpellVisualID);
            _worldPacket.WriteFloat(TravelSpeed);
            _worldPacket.WriteUInt16(MissReason);
            _worldPacket.WriteUInt16(ReflectStatus);
            _worldPacket.WriteFloat(Orientation);
            _worldPacket.WriteBit(SpeedAsTime);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Source { get; set; }
        public ObjectGuid Target; // Exclusive with TargetPosition
        public ushort MissReason { get; set; }
        public uint SpellVisualID { get; set; }
        public bool SpeedAsTime { get; set; }
        public ushort ReflectStatus { get; set; }
        public float TravelSpeed { get; set; }
        public Vector3 TargetPosition; // Exclusive with Target
        public float Orientation { get; set; }
    }

    class PlaySpellVisualKit : ServerPacket
    {
        public PlaySpellVisualKit() : base(ServerOpcodes.PlaySpellVisualKit) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
            _worldPacket.WriteUInt32(KitRecID);
            _worldPacket.WriteUInt32(KitType);
            _worldPacket.WriteUInt32(Duration);
        }

        public ObjectGuid Unit { get; set; }
        public uint KitRecID { get; set; }
        public uint KitType { get; set; }
        public uint Duration { get; set; }
    }

    public class CancelCast : ClientPacket
    {
        public CancelCast(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CastID = _worldPacket.ReadPackedGuid();
            SpellID = _worldPacket.ReadUInt32();
        }

        public uint SpellID { get; set; }
        public ObjectGuid CastID { get; set; }
    }

    public class OpenItem : ClientPacket
    {
        public OpenItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Slot = _worldPacket.ReadUInt8();
            PackSlot = _worldPacket.ReadUInt8();
        }

        public byte Slot { get; set; }
        public byte PackSlot { get; set; }
    }

    public class SpellChannelStart : ServerPacket
    {
        public SpellChannelStart() : base(ServerOpcodes.SpellChannelStart, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CasterGUID);
            _worldPacket.WriteInt32(SpellID);
            _worldPacket.WriteInt32(SpellXSpellVisualID);
            _worldPacket.WriteUInt32(ChannelDuration);
            _worldPacket.WriteBit(InterruptImmunities.HasValue);
            _worldPacket.WriteBit(HealPrediction.HasValue);
            _worldPacket.FlushBits();

            if (InterruptImmunities.HasValue)
                InterruptImmunities.Value.Write(_worldPacket);

            if (HealPrediction.HasValue)
                HealPrediction.Value.Write(_worldPacket);
        }

        public int SpellID { get; set; }
        public int SpellXSpellVisualID { get; set; }
        public Optional<SpellChannelStartInterruptImmunities> InterruptImmunities;
        public ObjectGuid CasterGUID { get; set; }
        public Optional<SpellTargetedHealPrediction> HealPrediction { get; set; }
        public uint ChannelDuration { get; set; }
    }

    public class SpellChannelUpdate : ServerPacket
    {
        public SpellChannelUpdate() : base(ServerOpcodes.SpellChannelUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CasterGUID);
            _worldPacket.WriteInt32(TimeRemaining);
        }

        public ObjectGuid CasterGUID { get; set; }
        public int TimeRemaining { get; set; }
    }

    class ResurrectRequest : ServerPacket
    {
        public ResurrectRequest() : base(ServerOpcodes.ResurrectRequest) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ResurrectOffererGUID);
            _worldPacket.WriteUInt32(ResurrectOffererVirtualRealmAddress);
            _worldPacket.WriteUInt32(PetNumber);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteBits(Name.Length, 11);
            _worldPacket.WriteBit(UseTimer);
            _worldPacket.WriteBit(Sickness);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(Name);
        }

        public ObjectGuid ResurrectOffererGUID { get; set; }
        public uint ResurrectOffererVirtualRealmAddress { get; set; }
        public uint PetNumber { get; set; }
        public uint SpellID { get; set; }
        public bool UseTimer { get; set; }
        public bool Sickness { get; set; }
        public string Name { get; set; }
    }

    class UnlearnSkill : ClientPacket
    {
        public UnlearnSkill(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SkillLine = _worldPacket.ReadUInt32();
        }

        public uint SkillLine { get; set; }
    }

    class SelfRes : ClientPacket
    {
        public SelfRes(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class GetMirrorImageData : ClientPacket
    {
        public GetMirrorImageData(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            UnitGUID = _worldPacket.ReadPackedGuid();
            DisplayID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid UnitGUID { get; set; }
        public uint DisplayID { get; set; }
    }

    class MirrorImageComponentedData : ServerPacket
    {
        public MirrorImageComponentedData() : base(ServerOpcodes.MirrorImageComponentedData) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteUInt32(DisplayID);
            _worldPacket.WriteUInt8(RaceID);
            _worldPacket.WriteUInt8(Gender);
            _worldPacket.WriteUInt8(ClassID);
            _worldPacket.WriteUInt8(SkinColor);
            _worldPacket.WriteUInt8(FaceVariation);
            _worldPacket.WriteUInt8(HairVariation);
            _worldPacket.WriteUInt8(HairColor);
            _worldPacket.WriteUInt8(BeardVariation);

            CustomDisplay.ForEach(id => _worldPacket.WriteUInt8(id));

            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteUInt32(ItemDisplayID.Count);

            foreach (var itemDisplayId in ItemDisplayID)
                _worldPacket.WriteUInt32(itemDisplayId);
        }

        public ObjectGuid UnitGUID { get; set; }
        public int DisplayID { get; set; }
        public byte RaceID { get; set; }
        public byte Gender { get; set; }
        public byte ClassID { get; set; }
        public byte SkinColor { get; set; }
        public byte FaceVariation { get; set; }
        public byte HairVariation { get; set; }
        public byte HairColor { get; set; }
        public byte BeardVariation { get; set; }
        public Array<byte> CustomDisplay { get; set; } = new Array<byte>(PlayerConst.CustomDisplaySize);
        public ObjectGuid GuildGUID { get; set; }

        public List<int> ItemDisplayID { get; set; } = new List<int>();
    }

    class MirrorImageCreatureData : ServerPacket
    {
        public MirrorImageCreatureData() : base(ServerOpcodes.MirrorImageCreatureData) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteInt32(DisplayID);
        }

        public ObjectGuid UnitGUID { get; set; }
        public int DisplayID { get; set; }
    }

    class SpellClick : ClientPacket
    {
        public SpellClick(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SpellClickUnitGuid = _worldPacket.ReadPackedGuid();
            TryAutoDismount = _worldPacket.HasBit();
        }

        public ObjectGuid SpellClickUnitGuid { get; set; }
        public bool TryAutoDismount { get; set; }
    }

    class ResyncRunes : ServerPacket
    {
        public ResyncRunes() : base(ServerOpcodes.ResyncRunes) { }

        public override void Write()
        {
            Runes.Write(_worldPacket);
        }

        public RuneData Runes { get; set; } = new RuneData();
    }

    class MissileTrajectoryCollision : ClientPacket
    {
        public MissileTrajectoryCollision(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Target = _worldPacket.ReadPackedGuid();
            SpellID = _worldPacket.ReadUInt32();
            CastID = _worldPacket.ReadPackedGuid();
            CollisionPos = _worldPacket.ReadVector3();
        }

        public ObjectGuid Target { get; set; }
        public uint SpellID { get; set; }
        public ObjectGuid CastID { get; set; }
        public Vector3 CollisionPos { get; set; }
    }

    class NotifyMissileTrajectoryCollision : ServerPacket
    {
        public NotifyMissileTrajectoryCollision() : base(ServerOpcodes.NotifyMissileTrajectoryCollision) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WriteVector3(CollisionPos);
        }

        public ObjectGuid Caster { get; set; }
        public ObjectGuid CastID { get; set; }
        public Vector3 CollisionPos { get; set; }
    }

    class UpdateMissileTrajectory : ClientPacket
    {
        public UpdateMissileTrajectory(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
            MoveMsgID = _worldPacket.ReadUInt16();
            SpellID = _worldPacket.ReadUInt32();
            Pitch = _worldPacket.ReadFloat();
            Speed = _worldPacket.ReadFloat();
            FirePos = _worldPacket.ReadVector3();
            ImpactPos = _worldPacket.ReadVector3();
            bool hasStatus = _worldPacket.HasBit();

            _worldPacket.ResetBitPos();
            if (hasStatus)
                Status.Set(MovementExtensions.ReadMovementInfo(_worldPacket));
        }

        public ObjectGuid Guid { get; set; }
        public ushort MoveMsgID { get; set; }
        public uint SpellID { get; set; }
        public float Pitch { get; set; }
        public float Speed { get; set; }
        public Vector3 FirePos { get; set; }
        public Vector3 ImpactPos { get; set; }
        public Optional<MovementInfo> Status { get; set; }
    }

    public class SpellDelayed : ServerPacket
    {
        public SpellDelayed() : base(ServerOpcodes.SpellDelayed, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WriteInt32(ActualDelay);
        }

        public ObjectGuid Caster { get; set; }
        public int ActualDelay { get; set; }
    }

    class DispelFailed : ServerPacket
    {
        public DispelFailed() : base(ServerOpcodes.DispelFailed) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CasterGUID);
            _worldPacket.WritePackedGuid(VictimGUID);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteInt32(FailedSpells.Count);

            FailedSpells.ForEach(FailedSpellID => _worldPacket.WriteUInt32(FailedSpellID));
        }

        public ObjectGuid CasterGUID { get; set; }
        public ObjectGuid VictimGUID { get; set; }
        public uint SpellID { get; set; }
        public List<uint> FailedSpells { get; set; } = new List<uint>();
    }

    class CustomLoadScreen : ServerPacket
    {
        public CustomLoadScreen(uint teleportSpellId, uint loadingScreenId) : base(ServerOpcodes.CustomLoadScreen)
        {
            TeleportSpellID = teleportSpellId;
            LoadingScreenID = loadingScreenId;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(TeleportSpellID);
            _worldPacket.WriteUInt32(LoadingScreenID);
        }

        uint TeleportSpellID;
        uint LoadingScreenID;
    }

    //Structs
    public struct SpellLogPowerData
    {
        public SpellLogPowerData(int powerType, int amount, int cost)
        {
            PowerType = powerType;
            Amount = amount;
            Cost = cost;
        }

        public int PowerType { get; set; }
        public int Amount { get; set; }
        public int Cost { get; set; }
    }

    public class SpellCastLogData
    {
        public void Initialize(Unit unit)
        {
            Health = (long)unit.GetHealth();
            AttackPower = (int)unit.GetTotalAttackPowerValue(unit.GetClass() == Class.Hunter ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack);
            SpellPower = unit.SpellBaseDamageBonusDone(SpellSchoolMask.Spell);
            PowerData.Add(new SpellLogPowerData((int)unit.getPowerType(), unit.GetPower(unit.getPowerType()), 0));
        }

        public void Initialize(Spell spell)
        {
            Health = (long)spell.GetCaster().GetHealth();
            AttackPower = (int)spell.GetCaster().GetTotalAttackPowerValue(spell.GetCaster().GetClass() == Class.Hunter ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack);
            SpellPower = spell.GetCaster().SpellBaseDamageBonusDone(SpellSchoolMask.Spell);
            PowerType primaryPowerType = spell.GetCaster().getPowerType();
            bool primaryPowerAdded = false;
            foreach (SpellPowerCost cost in spell.GetPowerCost())
            {
                PowerData.Add(new SpellLogPowerData((int)cost.Power, spell.GetCaster().GetPower(cost.Power), cost.Amount));
                if (cost.Power == primaryPowerType)
                    primaryPowerAdded = true;
            }

            if (!primaryPowerAdded)
                PowerData.Insert(0, new SpellLogPowerData((int)primaryPowerType, spell.GetCaster().GetPower(primaryPowerType), 0));
        }

        public void Write(WorldPacket data)
        {
            data.WriteInt64(Health);
            data.WriteInt32(AttackPower);
            data.WriteInt32(SpellPower);
            data.WriteBits(PowerData.Count, 9);
            data.FlushBits();

            foreach (SpellLogPowerData powerData in PowerData)
            {
                data.WriteInt32(powerData.PowerType);
                data.WriteInt32(powerData.Amount);
                data.WriteInt32(powerData.Cost);
            }
        }

        long Health;
        int AttackPower;
        int SpellPower;
        List<SpellLogPowerData> PowerData = new List<SpellLogPowerData>();
    }

    class SandboxScalingData
    {
        bool GenerateDataPlayerToPlayer(Player attacker, Player target)
        {
            return false;
        }

        bool GenerateDataCreatureToPlayer(Creature attacker, Player target)
        {
            CreatureTemplate creatureTemplate = attacker.GetCreatureTemplate();

            Type = SandboxScalingDataType.CreatureToPlayerDamage;
            PlayerLevelDelta = (short)target.GetInt32Value(PlayerFields.ScalingLevelDelta);
            PlayerItemLevel = (ushort)target.GetAverageItemLevel();
            TargetLevel = (byte)target.getLevel();
            Expansion = (byte)creatureTemplate.RequiredExpansion;
            Class = (byte)creatureTemplate.UnitClass;
            TargetMinScalingLevel = (byte)creatureTemplate.levelScaling.Value.MinLevel;
            TargetMaxScalingLevel = (byte)creatureTemplate.levelScaling.Value.MaxLevel;
            TargetScalingLevelDelta = (sbyte)creatureTemplate.levelScaling.Value.DeltaLevel;
            return true;
        }

        bool GenerateDataPlayerToCreature(Player attacker, Creature target)
        {
            CreatureTemplate creatureTemplate = target.GetCreatureTemplate();

            Type = SandboxScalingDataType.PlayerToCreatureDamage;
            PlayerLevelDelta = (short)attacker.GetInt32Value(PlayerFields.ScalingLevelDelta);
            PlayerItemLevel = (ushort)attacker.GetAverageItemLevel();
            TargetLevel = (byte)target.getLevel();
            Expansion = (byte)creatureTemplate.RequiredExpansion;
            Class = (byte)creatureTemplate.UnitClass;
            TargetMinScalingLevel = (byte)creatureTemplate.levelScaling.Value.MinLevel;
            TargetMaxScalingLevel = (byte)creatureTemplate.levelScaling.Value.MaxLevel;
            TargetScalingLevelDelta = (sbyte)creatureTemplate.levelScaling.Value.DeltaLevel;
            return true;
        }

        bool GenerateDataCreatureToCreature(Creature attacker, Creature target)
        {
            CreatureTemplate creatureTemplate = target.HasScalableLevels() ? target.GetCreatureTemplate() : attacker.GetCreatureTemplate();

            Type = SandboxScalingDataType.CreatureToCreatureDamage;
            PlayerLevelDelta = 0;
            PlayerItemLevel = 0;
            TargetLevel = (byte)target.getLevel();
            Expansion = (byte)creatureTemplate.RequiredExpansion;
            Class = (byte)creatureTemplate.UnitClass;
            TargetMinScalingLevel = (byte)creatureTemplate.levelScaling.Value.MinLevel;
            TargetMaxScalingLevel = (byte)creatureTemplate.levelScaling.Value.MaxLevel;
            TargetScalingLevelDelta = (sbyte)creatureTemplate.levelScaling.Value.DeltaLevel;
            return true;
        }

        public bool GenerateDataForUnits(Unit attacker, Unit target)
        {
            Player playerAttacker = attacker.ToPlayer();
            Creature creatureAttacker = attacker.ToCreature();
            if (playerAttacker)
            {
                Player playerTarget = target.ToPlayer();
                Creature creatureTarget = target.ToCreature();
                if (playerTarget)
                    return GenerateDataPlayerToPlayer(playerAttacker, playerTarget);
                else if (creatureTarget)
                {
                    if (creatureTarget.HasScalableLevels())
                        return GenerateDataPlayerToCreature(playerAttacker, creatureTarget);
                }
            }
            else if (creatureAttacker)
            {
                Player playerTarget = target.ToPlayer();
                Creature creatureTarget = target.ToCreature();
                if (playerTarget)
                {
                    if (creatureAttacker.HasScalableLevels())
                        return GenerateDataCreatureToPlayer(creatureAttacker, playerTarget);
                }
                else if (creatureTarget)
                {
                    if (creatureAttacker.HasScalableLevels() || creatureTarget.HasScalableLevels())
                        return GenerateDataCreatureToCreature(creatureAttacker, creatureTarget);
                }
            }

            return false;
        }

        public void Write(WorldPacket data)
        {
            data.WriteBits(Type, 3);
            data.WriteInt16(PlayerLevelDelta);
            data.WriteUInt16(PlayerItemLevel);
            data.WriteUInt8(TargetLevel);
            data.WriteUInt8(Expansion);
            data.WriteUInt8(Class);
            data.WriteUInt8(TargetMinScalingLevel);
            data.WriteUInt8(TargetMaxScalingLevel);
            data.WriteInt8(TargetScalingLevelDelta);
        }

        public SandboxScalingDataType Type { get; set; }
        public short PlayerLevelDelta { get; set; }
        public ushort PlayerItemLevel { get; set; }
        public byte TargetLevel { get; set; }
        public byte Expansion { get; set; }
        public byte Class { get; set; }
        public byte TargetMinScalingLevel { get; set; }
        public byte TargetMaxScalingLevel { get; set; }
        public sbyte TargetScalingLevelDelta { get; set; }

        public enum SandboxScalingDataType
        {
            PlayerToPlayer = 1, // NYI
            CreatureToPlayerDamage = 2,
            PlayerToCreatureDamage = 3,
            CreatureToCreatureDamage = 4
        }
    }

    public class AuraDataInfo
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(CastID);
            data.WriteInt32(SpellID);
            data.WriteInt32(SpellXSpellVisualID);
            data.WriteUInt8(Flags);
            data.WriteUInt32(ActiveFlags);
            data.WriteUInt16(CastLevel);
            data.WriteUInt8(Applications);
            data.WriteBit(CastUnit.HasValue);
            data.WriteBit(Duration.HasValue);
            data.WriteBit(Remaining.HasValue);
            data.WriteBit(TimeMod.HasValue);
            data.WriteBits(Points.Length, 6);
            data.WriteBits(EstimatedPoints.Count, 6);
            data.WriteBit(SandboxScaling.HasValue);

            if (SandboxScaling.HasValue)
                SandboxScaling.Value.Write(data);

            if (CastUnit.HasValue)
                data.WritePackedGuid(CastUnit.Value);

            if (Duration.HasValue)
                data.WriteUInt32(Duration.Value);

            if (Remaining.HasValue)
                data.WriteUInt32(Remaining.Value);

            if (TimeMod.HasValue)
                data.WriteFloat(TimeMod.Value);

            foreach (var point in Points)
                data.WriteFloat(point);

            foreach (var point in EstimatedPoints)
                data.WriteFloat(point);
        }

        public ObjectGuid CastID { get; set; }
        public int SpellID { get; set; }
        public int SpellXSpellVisualID { get; set; }
        public AuraFlags Flags { get; set; }
        public uint ActiveFlags { get; set; }
        public ushort CastLevel { get; set; } = 1;
        public byte Applications { get; set; } = 1;
        Optional<SandboxScalingData> SandboxScaling;
        public Optional<ObjectGuid> CastUnit { get; set; }
        public Optional<int> Duration { get; set; }
        public Optional<int> Remaining { get; set; }
        Optional<float> TimeMod;
        public float[] Points { get; set; } = new float[0];
        public List<float> EstimatedPoints { get; set; } = new List<float>();
    }

    public struct AuraInfo
    {
        public void Write(WorldPacket data)
        {
            data .WriteUInt8(Slot);
            data.WriteBit(AuraData.HasValue);
            data.FlushBits();

            if (AuraData.HasValue)
                AuraData.Value.Write(data);
        }

        public byte Slot { get; set; }
        public Optional<AuraDataInfo> AuraData;
    }

    public struct TargetLocation
    {
        public ObjectGuid Transport { get; set; }
        public Position Location { get; set; }

        public void Read(WorldPacket data)
        {
            Transport = data.ReadPackedGuid();
            Location = new Position();
            Location.posX = data.ReadFloat();
            Location.posY = data.ReadFloat();
            Location.posZ = data.ReadFloat();
        }

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(Transport);
            data.WriteFloat(Location.posX);
            data.WriteFloat(Location.posY);
            data.WriteFloat(Location.posZ);
        }
    }

    public class SpellTargetData
    {
        public void Read(WorldPacket data)
        {
            Flags = (SpellCastTargetFlags)data.ReadBits<uint>(25);
            SrcLocation.HasValue = data.HasBit();
            DstLocation.HasValue = data.HasBit();
            Orientation.HasValue = data.HasBit();
            MapID.HasValue = data.HasBit();
            uint nameLength = data.ReadBits<uint>(7);

            Unit = data.ReadPackedGuid();
            Item = data.ReadPackedGuid();

            if (SrcLocation.HasValue)
                SrcLocation.Value.Read(data);

            if (DstLocation.HasValue)
                DstLocation.Value.Read(data);

            if (Orientation.HasValue)
                Orientation.Value = data.ReadFloat();

            if (MapID.HasValue)
                MapID.Value = data.ReadInt32();

            Name = data.ReadString(nameLength);
        }

        public void Write(WorldPacket data)
        {
            data.WriteBits((uint)Flags, 25);
            data.WriteBit(SrcLocation.HasValue);
            data.WriteBit(DstLocation.HasValue);
            data.WriteBit(Orientation.HasValue);
            data.WriteBit(MapID.HasValue);
            data.WriteBits(Name.Length, 7);
            data.FlushBits();

            data.WritePackedGuid(Unit);
            data.WritePackedGuid(Item);

            if (SrcLocation.HasValue)
                SrcLocation.Value.Write(data);

            if (DstLocation.HasValue)
                DstLocation.Value.Write(data);

            if (Orientation.HasValue)
                data.WriteFloat(Orientation.Value);

            if (MapID.HasValue)
                data.WriteInt32(MapID.Value);

            data.WriteString(Name);
        }

        public SpellCastTargetFlags Flags { get; set; }
        public ObjectGuid Unit { get; set; }
        public ObjectGuid Item { get; set; }
        public Optional<TargetLocation> SrcLocation;
        public Optional<TargetLocation> DstLocation;
        public Optional<float> Orientation;
        public Optional<int> MapID;
        public string Name = "";
    }

    public struct MissileTrajectoryRequest
    {
        public float Pitch { get; set; }
        public float Speed { get; set; }

        public void Read(WorldPacket data)
        {
            Pitch = data.ReadFloat();
            Speed = data.ReadFloat();
        }
    }

    public struct SpellWeight
    {
        public uint Type { get; set; }
        public int ID { get; set; }
        public uint Quantity { get; set; }
    }

    public class SpellCastRequest
    {
        public void Read(WorldPacket data)
        {
            CastID = data.ReadPackedGuid();
            Misc[0] = data.ReadUInt32();
            Misc[1] = data.ReadUInt32();
            SpellID = data.ReadUInt32();
            SpellXSpellVisualID = data.ReadUInt32();
            MissileTrajectory.Read(data);
            Charmer = data.ReadPackedGuid();
            SendCastFlags = data.ReadBits<uint>(5);
            MoveUpdate.HasValue = data.HasBit();
            var weightCount = data.ReadBits<uint>(2);
            Target.Read(data);

            if (MoveUpdate.HasValue)
                MoveUpdate.Value = MovementExtensions.ReadMovementInfo(data);

            for (var i = 0; i < weightCount; ++i)
            {
                data.ResetBitPos();
                SpellWeight weight = new SpellWeight();
                weight.Type = data.ReadBits<uint>(2);
                weight.ID = data.ReadInt32();
                weight.Quantity = data.ReadUInt32();
                Weight.Add(weight);
            }
        }

        public ObjectGuid CastID { get; set; }
        public uint SpellID { get; set; }
        public uint SpellXSpellVisualID { get; set; }
        public uint SendCastFlags { get; set; }
        public SpellTargetData Target { get; set; } = new SpellTargetData();
        public MissileTrajectoryRequest MissileTrajectory { get; set; }
        public Optional<MovementInfo> MoveUpdate;
        public List<SpellWeight> Weight { get; set; } = new List<SpellWeight>();
        public ObjectGuid Charmer { get; set; }
        public uint[] Misc { get; set; } = new uint[2];
    }

    public struct SpellMissStatus
    {
        public void Write(WorldPacket data)
        {
            data.WriteBits(Reason, 4);
            if (Reason == (byte)SpellMissInfo.Reflect)
                data.WriteBits(ReflectStatus, 4);

            data.FlushBits();
        }

        public byte Reason { get; set; }
        public byte ReflectStatus { get; set; }
    }

    public struct SpellPowerData
    {
        public int Cost { get; set; }
        public PowerType Type { get; set; }

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Cost);
            data.WriteInt8(Type);
        }
    }

    public class RuneData
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt8(Start);
            data.WriteUInt8(Count);
            data.WriteUInt32(Cooldowns.Count);

            foreach (byte cd in Cooldowns)
                data.WriteUInt8(cd);
        }

        public byte Start { get; set; }
        public byte Count { get; set; }
        public List<byte> Cooldowns { get; set; } = new List<byte>();
    }

    public struct MissileTrajectoryResult
    {
        public uint TravelTime { get; set; }
        public float Pitch { get; set; }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(TravelTime);
            data.WriteFloat(Pitch);
        }
    }

    public struct SpellAmmo
    {
        public int DisplayID { get; set; }
        public sbyte InventoryType { get; set; }

        public void Write(WorldPacket data)
        {
            data.WriteInt32(DisplayID);
            data.WriteInt8(InventoryType);
        }
    }

    public struct CreatureImmunities
    {
        public uint School { get; set; }
        public uint Value { get; set; }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(School);
            data.WriteUInt32(Value);
        }
    }

    public struct SpellHealPrediction
    {
        public ObjectGuid BeaconGUID { get; set; }
        public uint Points { get; set; }
        public byte Type { get; set; }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Points);
            data.WriteUInt8(Type);
            data.WritePackedGuid(BeaconGUID);
        }
    }

    public class SpellCastData
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(CasterGUID);
            data.WritePackedGuid(CasterUnit);
            data.WritePackedGuid(CastID);
            data.WritePackedGuid(OriginalCastID);
            data.WriteInt32(SpellID);
            data.WriteUInt32(SpellXSpellVisualID);
            data.WriteUInt32(CastFlags);
            data.WriteUInt32(CastTime);

            MissileTrajectory.Write(data);

            data.WriteInt32(Ammo.DisplayID);
            data.WriteUInt8(DestLocSpellCastIndex);

            Immunities.Write(data);
            Predict.Write(data);

            data.WriteBits(CastFlagsEx, 22);
            data.WriteBits(HitTargets.Count, 16);
            data.WriteBits(MissTargets.Count, 16);
            data.WriteBits(MissStatus.Count, 16);
            data.WriteBits(RemainingPower.Count, 9);
            data.WriteBit(RemainingRunes.HasValue);
            data.WriteBits(TargetPoints.Count, 16);
            data.FlushBits();

            foreach (SpellMissStatus status in MissStatus)
                status.Write(data);

            Target.Write(data);

            foreach (ObjectGuid target in HitTargets)
                data.WritePackedGuid(target);

            foreach (ObjectGuid target in MissTargets)
                data.WritePackedGuid(target);

            foreach (SpellPowerData power in RemainingPower)
                power.Write(data);

            if (RemainingRunes.HasValue)
                RemainingRunes.Value.Write(data);

            foreach (TargetLocation targetLoc in TargetPoints)
                targetLoc.Write(data);
        }

        public ObjectGuid CasterGUID { get; set; }
        public ObjectGuid CasterUnit { get; set; }
        public ObjectGuid CastID { get; set; }
        public ObjectGuid OriginalCastID { get; set; }
        public int SpellID { get; set; }
        public uint SpellXSpellVisualID { get; set; }
        public SpellCastFlags CastFlags { get; set; }
        public SpellCastFlagsEx CastFlagsEx { get; set; }
        public uint CastTime { get; set; }
        public List<ObjectGuid> HitTargets { get; set; } = new List<ObjectGuid>();
        public List<ObjectGuid> MissTargets { get; set; } = new List<ObjectGuid>();
        public List<SpellMissStatus> MissStatus { get; set; } = new List<SpellMissStatus>();
        public SpellTargetData Target { get; set; } = new SpellTargetData();
        public List<SpellPowerData> RemainingPower { get; set; } = new List<SpellPowerData>();
        public Optional<RuneData> RemainingRunes;
        public MissileTrajectoryResult MissileTrajectory;
        public SpellAmmo Ammo { get; set; }
        public byte DestLocSpellCastIndex { get; set; }
        public List<TargetLocation> TargetPoints { get; set; } = new List<TargetLocation>();
        public CreatureImmunities Immunities;
        public SpellHealPrediction Predict { get; set; }
    }

    public struct SpellModifierData
    {
        public float ModifierValue { get; set; }
        public byte ClassIndex { get; set; }

        public void Write(WorldPacket data)
        {
            data.WriteFloat(ModifierValue);
            data.WriteUInt8(ClassIndex);
        }
    }

    public class SpellModifierInfo
    {
        public byte ModIndex { get; set; }
        public List<SpellModifierData> ModifierData { get; set; } = new List<SpellModifierData>();

        public void Write(WorldPacket data)
        {
            data.WriteUInt8(ModIndex);
            data.WriteUInt32(ModifierData.Count);
            foreach (SpellModifierData modData in ModifierData)
                modData.Write(data);
        }
    }

    public class SpellCooldownStruct
    {
        public SpellCooldownStruct(uint spellId, uint forcedCooldown)
        {
            SrecID = spellId;
            ForcedCooldown = forcedCooldown;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(SrecID);
            data.WriteUInt32(ForcedCooldown);
            data.WriteFloat(ModRate);
        }

        public uint SrecID { get; set; }
        public uint ForcedCooldown { get; set; }
        public float ModRate { get; set; } = 1.0f;
    }

    public class SpellHistoryEntry
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(SpellID);
            data.WriteUInt32(ItemID);
            data.WriteUInt32(Category);
            data.WriteInt32(RecoveryTime);
            data.WriteInt32(CategoryRecoveryTime);
            data.WriteFloat(ModRate);
            data.WriteBit(unused622_1.HasValue);
            data.WriteBit(unused622_2.HasValue);
            data.WriteBit(OnHold);
            data.FlushBits();

            if (unused622_1.HasValue)
                data .WriteUInt32(unused622_1.Value);
            if (unused622_2.HasValue)
                data.WriteUInt32(unused622_2.Value);
        }

        public uint SpellID { get; set; }
        public uint ItemID { get; set; }
        public uint Category { get; set; }
        public int RecoveryTime { get; set; }
        public int CategoryRecoveryTime { get; set; }
        public float ModRate { get; set; } = 1.0f;
        public bool OnHold { get; set; }
        Optional<uint> unused622_1;   // This field is not used for anything in the client in 6.2.2.20444
        Optional<uint> unused622_2;   // This field is not used for anything in the client in 6.2.2.20444
    }

    public class SpellChargeEntry
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Category);
            data.WriteUInt32(NextRecoveryTime);
            data.WriteFloat(ChargeModRate);
            data.WriteUInt8(ConsumedCharges);
        }

        public uint Category { get; set; }
        public uint NextRecoveryTime { get; set; }
        public float ChargeModRate { get; set; } = 1.0f;
        public byte ConsumedCharges { get; set; }
    }

    public struct SpellChannelStartInterruptImmunities
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(SchoolImmunities);
            data.WriteInt32(Immunities);
        }

        public int SchoolImmunities { get; set; }
        public int Immunities { get; set; }
    }

    public struct SpellTargetedHealPrediction
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(TargetGUID);
            Predict.Write(data);
        }

        public ObjectGuid TargetGUID { get; set; }
        public SpellHealPrediction Predict { get; set; }
    }
}
