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

        public ObjectGuid CasterGUID;
        public uint SpellID;
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
            Reason = _worldPacket.ReadInt32();
        }

        int ChannelSpell;
        int Reason = 0;       // 40 = /run SpellStopCasting(), 16 = movement/AURA_INTERRUPT_FLAG_MOVE, 41 = turning/AURA_INTERRUPT_FLAG_TURNING
                                // does not match SpellCastResult enum
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
        public List<CategoryCooldownInfo> CategoryCooldowns = new List<CategoryCooldownInfo>();

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

            public uint Category; // SpellCategory Id
            public int ModCooldown; // Reduced Cooldown in ms
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

        public bool InitialLogin;
        public List<uint> KnownSpells = new List<uint>();
        public List<uint> FavoriteSpells = new List<uint>(); // tradeskill recipes
    }

    public class UpdateActionButtons : ServerPacket
    {
        public ulong[] ActionButtons = new ulong[PlayerConst.MaxActionButtons];
        public byte Reason;

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

        public uint GetButtonAction() { return (uint)(Action & 0x00000000FFFFFFFF); }
        public uint GetButtonType() { return (uint)((Action & 0xFFFFFFFF00000000) >> 56); }

        public ulong Action; // two packed public uint (action and type)
        public byte Index;
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

        public bool UpdateAll;
        public ObjectGuid UnitGUID;
        public List<AuraInfo> Auras = new List<AuraInfo>();
    }

    public class CastSpell : ClientPacket
    {
        public SpellCastRequest Cast;

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
        public ObjectGuid PetGUID;
        public SpellCastRequest Cast;

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
        public byte PackSlot;
        public byte Slot;
        public ObjectGuid CastItem;
        public SpellCastRequest Cast;

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

        public ObjectGuid ClientCastID;
        public ObjectGuid ServerCastID;
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

        public SpellCastData Cast = new SpellCastData();
    }

    public class SpellStart : ServerPacket
    {
        public SpellCastData Cast;

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

        public List<uint> SpellID = new List<uint>();
        public List<uint> Superceded = new List<uint>();
        public List<int> FavoriteSpellID = new List<int>();
    }

    public class LearnedSpells : ServerPacket
    {
        public LearnedSpells() : base(ServerOpcodes.LearnedSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID.Count);
            _worldPacket.WriteUInt32(FavoriteSpellID.Count);

            foreach (uint spell in SpellID)
                _worldPacket.WriteUInt32(spell);

            foreach (int spell in FavoriteSpellID)
                _worldPacket.WriteInt32(spell);

            _worldPacket.WriteBit(SuppressMessaging);
            _worldPacket.FlushBits();
        }

        public List<uint> SpellID = new List<uint>();
        public List<int> FavoriteSpellID = new List<int>();
        public bool SuppressMessaging;
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

        public ObjectGuid CasterUnit;
        public uint SpellID;
        public uint SpellXSpellVisualID;
        public ushort Reason;
        public ObjectGuid CastID;
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

        public ObjectGuid CasterUnit;
        public uint SpellID;
        public uint SpellXSpellVisualID;
        public ushort Reason;
        public ObjectGuid CastID;
    }

    class CastFailedBase : ServerPacket
    {
        public CastFailedBase(ServerOpcodes serverOpcodes, ConnectionType connectionType) : base(serverOpcodes, connectionType) { }

        public override void Write() { throw new NotImplementedException(); }

        public ObjectGuid CastID;
        public int SpellID;
        public SpellCastResult Reason;
        public int FailedArg1 = -1;
        public int FailedArg2 = -1;
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

        public int SpellXSpellVisualID;
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

        public List<SpellModifierInfo> Modifiers = new List<SpellModifierInfo>();
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

        public List<uint> SpellID = new List<uint>();
        public bool SuppressMessaging;
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

        public bool IsPet;
        public uint SpellID;
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

        public List<uint> SpellID = new List<uint>();
        public bool IsPet;
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

        public bool IsPet;
        public uint SpellID;
        public bool ClearOnHold;
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

        public bool IsPet;
        public int DeltaTime;
        public uint SpellID;
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

        public List<SpellCooldownStruct> SpellCooldowns = new List<SpellCooldownStruct>();
        public ObjectGuid Caster;
        public SpellCooldownFlags Flags;
    }

    public class SendSpellHistory : ServerPacket
    {
        public SendSpellHistory() : base(ServerOpcodes.SendSpellHistory, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Entries.Count);
            Entries.ForEach(p => p.Write(_worldPacket));
        }

        public List<SpellHistoryEntry> Entries = new List<SpellHistoryEntry>();
    }

    public class ClearAllSpellCharges : ServerPacket
    {
        public ClearAllSpellCharges() : base(ServerOpcodes.ClearAllSpellCharges, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsPet);
            _worldPacket.FlushBits();
        }

        public bool IsPet;
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

        public bool IsPet;
        public uint Category;
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

        public bool IsPet;
        public uint Category;
        public uint NextRecoveryTime;
        public byte ConsumedCharges;
        public float ChargeModRate = 1.0f;
    }

    public class SendSpellCharges : ServerPacket
    {
        public SendSpellCharges() : base(ServerOpcodes.SendSpellCharges, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Entries.Count);
            Entries.ForEach(p => p.Write(_worldPacket));
        }

        public List<SpellChargeEntry> Entries = new List<SpellChargeEntry>();
    }

    public class ClearTarget : ServerPacket
    {
        public ClearTarget() : base(ServerOpcodes.ClearTarget) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid;
    }

    public class CancelOrphanSpellVisual : ServerPacket
    {
        public CancelOrphanSpellVisual() : base(ServerOpcodes.CancelOrphanSpellVisual) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellVisualID);
        }

        public uint SpellVisualID;
    }

    public class CancelSpellVisual : ServerPacket
    {
        public CancelSpellVisual() : base(ServerOpcodes.CancelSpellVisual) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Source);
            _worldPacket.WriteUInt32(SpellVisualID);
        }

        public ObjectGuid Source;
        public uint SpellVisualID;
    }

    class CancelSpellVisualKit : ServerPacket
    {
        public CancelSpellVisualKit() : base(ServerOpcodes.CancelSpellVisualKit) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Source);
            _worldPacket.WriteUInt32(SpellVisualKitID);
        }

        public ObjectGuid Source;
        public uint SpellVisualKitID;
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
            _worldPacket.WriteFloat(Unk801);
            _worldPacket.WriteBit(SpeedAsTime);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Target; // Exclusive with TargetLocation
        public Position SourceLocation;
        public uint SpellVisualID;
        public bool SpeedAsTime;
        public float TravelSpeed;
        public float UnkZero; // Always zero
        public float Unk801;
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
            _worldPacket.WritePackedGuid(Unk801_1);
            _worldPacket.WriteVector3(TargetPosition);
            _worldPacket.WriteUInt32(SpellVisualID);
            _worldPacket.WriteFloat(TravelSpeed);
            _worldPacket.WriteUInt16(MissReason);
            _worldPacket.WriteUInt16(ReflectStatus);
            _worldPacket.WriteFloat(Orientation);
            _worldPacket.WriteFloat(Unk801_2);
            _worldPacket.WriteBit(SpeedAsTime);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Source;
        public ObjectGuid Target; // Exclusive with TargetPosition
        public ObjectGuid Unk801_1;
        public ushort MissReason;
        public uint SpellVisualID;
        public bool SpeedAsTime;
        public ushort ReflectStatus;
        public float TravelSpeed;
        public Vector3 TargetPosition; // Exclusive with Target
        public float Orientation;
        public float Unk801_2;
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

        public ObjectGuid Unit;
        public uint KitRecID;
        public uint KitType;
        public uint Duration;
    }

    public class CancelCast : ClientPacket
    {
        public CancelCast(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CastID = _worldPacket.ReadPackedGuid();
            SpellID = _worldPacket.ReadUInt32();
        }

        public uint SpellID;
        public ObjectGuid CastID;
    }

    public class OpenItem : ClientPacket
    {
        public OpenItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Slot = _worldPacket.ReadUInt8();
            PackSlot = _worldPacket.ReadUInt8();
        }

        public byte Slot;
        public byte PackSlot;
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

        public int SpellID;
        public int SpellXSpellVisualID;
        public Optional<SpellChannelStartInterruptImmunities> InterruptImmunities;
        public ObjectGuid CasterGUID;
        public Optional<SpellTargetedHealPrediction> HealPrediction;
        public uint ChannelDuration;
    }

    public class SpellChannelUpdate : ServerPacket
    {
        public SpellChannelUpdate() : base(ServerOpcodes.SpellChannelUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CasterGUID);
            _worldPacket.WriteInt32(TimeRemaining);
        }

        public ObjectGuid CasterGUID;
        public int TimeRemaining;
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
            _worldPacket.WriteBits(Name.GetByteCount(), 11);
            _worldPacket.WriteBit(UseTimer);
            _worldPacket.WriteBit(Sickness);
            _worldPacket.FlushBits();

            _worldPacket.WriteString(Name);
        }

        public ObjectGuid ResurrectOffererGUID;
        public uint ResurrectOffererVirtualRealmAddress;
        public uint PetNumber;
        public uint SpellID;
        public bool UseTimer;
        public bool Sickness;
        public string Name;
    }

    class UnlearnSkill : ClientPacket
    {
        public UnlearnSkill(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SkillLine = _worldPacket.ReadUInt32();
        }

        public uint SkillLine;
    }

    class SelfRes : ClientPacket
    {
        public SelfRes(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SpellId = _worldPacket.ReadUInt32();
        }

        public uint SpellId;
    }

    class GetMirrorImageData : ClientPacket
    {
        public GetMirrorImageData(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            UnitGUID = _worldPacket.ReadPackedGuid();
            DisplayID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid UnitGUID;
        public uint DisplayID;
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

        public ObjectGuid UnitGUID;
        public int DisplayID;
        public byte RaceID;
        public byte Gender;
        public byte ClassID;
        public byte SkinColor;
        public byte FaceVariation;
        public byte HairVariation;
        public byte HairColor;
        public byte BeardVariation;
        public Array<byte> CustomDisplay = new Array<byte>(PlayerConst.CustomDisplaySize);
        public ObjectGuid GuildGUID;

        public List<int> ItemDisplayID = new List<int>();
    }

    class MirrorImageCreatureData : ServerPacket
    {
        public MirrorImageCreatureData() : base(ServerOpcodes.MirrorImageCreatureData) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteInt32(DisplayID);
        }

        public ObjectGuid UnitGUID;
        public int DisplayID;
    }

    class SpellClick : ClientPacket
    {
        public SpellClick(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SpellClickUnitGuid = _worldPacket.ReadPackedGuid();
            TryAutoDismount = _worldPacket.HasBit();
        }

        public ObjectGuid SpellClickUnitGuid;
        public bool TryAutoDismount;
    }

    class ResyncRunes : ServerPacket
    {
        public ResyncRunes() : base(ServerOpcodes.ResyncRunes) { }

        public override void Write()
        {
            Runes.Write(_worldPacket);
        }

        public RuneData Runes = new RuneData();
    }

    class AddRunePower : ServerPacket
    {
        public AddRunePower() : base(ServerOpcodes.AddRunePower, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(AddedRunesMask);
        }

        public uint AddedRunesMask;
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

        public ObjectGuid Target;
        public uint SpellID;
        public ObjectGuid CastID;
        public Vector3 CollisionPos;
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

        public ObjectGuid Caster;
        public ObjectGuid CastID;
        public Vector3 CollisionPos;
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

        public ObjectGuid Guid;
        public ushort MoveMsgID;
        public uint SpellID;
        public float Pitch;
        public float Speed;
        public Vector3 FirePos;
        public Vector3 ImpactPos;
        public Optional<MovementInfo> Status;
    }

    public class SpellDelayed : ServerPacket
    {
        public SpellDelayed() : base(ServerOpcodes.SpellDelayed, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WriteInt32(ActualDelay);
        }

        public ObjectGuid Caster;
        public int ActualDelay;
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

        public ObjectGuid CasterGUID;
        public ObjectGuid VictimGUID;
        public uint SpellID;
        public List<uint> FailedSpells = new List<uint>();
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

        public int PowerType;
        public int Amount;
        public int Cost;
    }

    public class SpellCastLogData
    {
        public void Initialize(Unit unit)
        {
            Health = (long)unit.GetHealth();
            AttackPower = (int)unit.GetTotalAttackPowerValue(unit.GetClass() == Class.Hunter ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack);
            SpellPower = unit.SpellBaseDamageBonusDone(SpellSchoolMask.Spell);
            Armor = unit.GetArmor();
            PowerData.Add(new SpellLogPowerData((int)unit.GetPowerType(), unit.GetPower(unit.GetPowerType()), 0));
        }

        public void Initialize(Spell spell)
        {
            Health = (long)spell.GetCaster().GetHealth();
            AttackPower = (int)spell.GetCaster().GetTotalAttackPowerValue(spell.GetCaster().GetClass() == Class.Hunter ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack);
            SpellPower = spell.GetCaster().SpellBaseDamageBonusDone(SpellSchoolMask.Spell);
            Armor = spell.GetCaster().GetArmor();
            PowerType primaryPowerType = spell.GetCaster().GetPowerType();
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
            data.WriteUInt32(Armor);
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
        uint Armor;
        List<SpellLogPowerData> PowerData = new List<SpellLogPowerData>();
    }

    class ContentTuningParams
    {
        bool GenerateDataPlayerToPlayer(Player attacker, Player target)
        {
            return false;
        }

        bool GenerateDataCreatureToPlayer(Creature attacker, Player target)
        {
            CreatureTemplate creatureTemplate = attacker.GetCreatureTemplate();

            TuningType = ContentTuningType.CreatureToPlayerDamage;
            PlayerLevelDelta = (short)target.GetInt32Value(ActivePlayerFields.ScalingPlayerLevelDelta);
            PlayerItemLevel = (ushort)target.GetAverageItemLevel();
            ScalingHealthItemLevelCurveID = (ushort)target.GetUInt32Value(UnitFields.ScalingHealthItemLevelCurveId);
            TargetLevel = (byte)target.getLevel();
            Expansion = (byte)creatureTemplate.RequiredExpansion;
            TargetMinScalingLevel = (byte)creatureTemplate.levelScaling.Value.MinLevel;
            TargetMaxScalingLevel = (byte)creatureTemplate.levelScaling.Value.MaxLevel;
            TargetScalingLevelDelta = (sbyte)attacker.GetInt32Value(UnitFields.ScalingLevelDelta);
            return true;
        }

        bool GenerateDataPlayerToCreature(Player attacker, Creature target)
        {
            CreatureTemplate creatureTemplate = target.GetCreatureTemplate();

            TuningType = ContentTuningType.PlayerToCreatureDamage;
            PlayerLevelDelta = (short)attacker.GetInt32Value(ActivePlayerFields.ScalingPlayerLevelDelta);
            PlayerItemLevel = (ushort)attacker.GetAverageItemLevel();
            ScalingHealthItemLevelCurveID = (ushort)target.GetUInt32Value(UnitFields.ScalingHealthItemLevelCurveId);
            TargetLevel = (byte)target.getLevel();
            Expansion = (byte)creatureTemplate.RequiredExpansion;
            TargetMinScalingLevel = (byte)creatureTemplate.levelScaling.Value.MinLevel;
            TargetMaxScalingLevel = (byte)creatureTemplate.levelScaling.Value.MaxLevel;
            TargetScalingLevelDelta = (sbyte)target.GetInt32Value(UnitFields.ScalingLevelDelta);
            return true;
        }

        bool GenerateDataCreatureToCreature(Creature attacker, Creature target)
        {
            Creature accessor = target.HasScalableLevels() ? target : attacker;
            CreatureTemplate creatureTemplate = accessor.GetCreatureTemplate();

            TuningType = ContentTuningType.CreatureToCreatureDamage;
            PlayerLevelDelta = 0;
            PlayerItemLevel = 0;
            TargetLevel = (byte)target.getLevel();
            Expansion = (byte)creatureTemplate.RequiredExpansion;
            TargetMinScalingLevel = (byte)creatureTemplate.levelScaling.Value.MinLevel;
            TargetMaxScalingLevel = (byte)creatureTemplate.levelScaling.Value.MaxLevel;
            TargetScalingLevelDelta = (sbyte)accessor.GetInt32Value(UnitFields.ScalingLevelDelta);
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
            data.WriteInt16(PlayerLevelDelta);
            data.WriteUInt16(PlayerItemLevel);
            data.WriteUInt16(ScalingHealthItemLevelCurveID);
            data.WriteUInt8(TargetLevel);
            data.WriteUInt8(Expansion);
            data.WriteUInt8(TargetMinScalingLevel);
            data.WriteUInt8(TargetMaxScalingLevel);
            data.WriteInt8(TargetScalingLevelDelta);
            data.WriteBits(TuningType, 4);
            data.WriteBit(ScalesWithItemLevel);
            data.FlushBits();
        }

        public ContentTuningType TuningType;
        public short PlayerLevelDelta;
        public ushort PlayerItemLevel;
        public ushort ScalingHealthItemLevelCurveID;
        public byte TargetLevel;
        public byte Expansion;
        public byte TargetMinScalingLevel;
        public byte TargetMaxScalingLevel;
        public sbyte TargetScalingLevelDelta;
        public bool ScalesWithItemLevel;

        public enum ContentTuningType
        {
            PlayerToPlayer = 7, // NYI
            CreatureToPlayerDamage = 1,
            PlayerToCreatureDamage = 2,
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
            data.WriteInt32(ContentTuningID);
            data.WriteBit(CastUnit.HasValue);
            data.WriteBit(Duration.HasValue);
            data.WriteBit(Remaining.HasValue);
            data.WriteBit(TimeMod.HasValue);
            data.WriteBits(Points.Length, 6);
            data.WriteBits(EstimatedPoints.Count, 6);
            data.WriteBit(ContentTuning.HasValue);

            if (ContentTuning.HasValue)
                ContentTuning.Value.Write(data);

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

        public ObjectGuid CastID;
        public int SpellID;
        public int SpellXSpellVisualID;
        public AuraFlags Flags;
        public uint ActiveFlags;
        public ushort CastLevel = 1;
        public byte Applications = 1;
        public int ContentTuningID;
        Optional<ContentTuningParams> ContentTuning;
        public Optional<ObjectGuid> CastUnit;
        public Optional<int> Duration;
        public Optional<int> Remaining;
        Optional<float> TimeMod;
        public float[] Points = new float[0];
        public List<float> EstimatedPoints = new List<float>();
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

        public byte Slot;
        public Optional<AuraDataInfo> AuraData;
    }

    public struct TargetLocation
    {
        public ObjectGuid Transport;
        public Position Location;

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
            data.WriteBits(Name.GetByteCount(), 7);
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

        public SpellCastTargetFlags Flags;
        public ObjectGuid Unit;
        public ObjectGuid Item;
        public Optional<TargetLocation> SrcLocation;
        public Optional<TargetLocation> DstLocation;
        public Optional<float> Orientation;
        public Optional<int> MapID;
        public string Name = "";
    }

    public struct MissileTrajectoryRequest
    {
        public float Pitch;
        public float Speed;

        public void Read(WorldPacket data)
        {
            Pitch = data.ReadFloat();
            Speed = data.ReadFloat();
        }
    }

    public struct SpellWeight
    {
        public uint Type;
        public int ID;
        public uint Quantity;
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
            CraftingNPC = data.ReadPackedGuid();
            SendCastFlags = data.ReadBits<uint>(5);
            MoveUpdate.HasValue = data.HasBit();
            var weightCount = data.ReadBits<uint>(2);
            Target.Read(data);

            if (MoveUpdate.HasValue)
                MoveUpdate.Value = MovementExtensions.ReadMovementInfo(data);

            for (var i = 0; i < weightCount; ++i)
            {
                data.ResetBitPos();
                SpellWeight weight;
                weight.Type = data.ReadBits<uint>(2);
                weight.ID = data.ReadInt32();
                weight.Quantity = data.ReadUInt32();
                Weight.Add(weight);
            }
        }

        public ObjectGuid CastID;
        public uint SpellID;
        public uint SpellXSpellVisualID;
        public uint SendCastFlags;
        public SpellTargetData Target = new SpellTargetData();
        public MissileTrajectoryRequest MissileTrajectory;
        public Optional<MovementInfo> MoveUpdate;
        public List<SpellWeight> Weight = new List<SpellWeight>();
        public ObjectGuid CraftingNPC;
        public uint[] Misc = new uint[2];
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

        public byte Reason;
        public byte ReflectStatus;
    }

    public struct SpellPowerData
    {
        public int Cost;
        public PowerType Type;

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

        public byte Start;
        public byte Count;
        public List<byte> Cooldowns = new List<byte>();
    }

    public struct MissileTrajectoryResult
    {
        public uint TravelTime;
        public float Pitch;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(TravelTime);
            data.WriteFloat(Pitch);
        }
    }

    public struct SpellAmmo
    {
        public int DisplayID;
        public sbyte InventoryType;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(DisplayID);
            data.WriteInt8(InventoryType);
        }
    }

    public struct CreatureImmunities
    {
        public uint School;
        public uint Value;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(School);
            data.WriteUInt32(Value);
        }
    }

    public struct SpellHealPrediction
    {
        public ObjectGuid BeaconGUID;
        public uint Points;
        public byte Type;

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
            data.WriteUInt32(CastFlagsEx);
            data.WriteUInt32(CastTime);

            MissileTrajectory.Write(data);

            data.WriteInt32(Ammo.DisplayID);
            data.WriteUInt8(DestLocSpellCastIndex);

            Immunities.Write(data);
            Predict.Write(data);
 
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

        public ObjectGuid CasterGUID;
        public ObjectGuid CasterUnit;
        public ObjectGuid CastID;
        public ObjectGuid OriginalCastID;
        public int SpellID;
        public uint SpellXSpellVisualID;
        public SpellCastFlags CastFlags;
        public SpellCastFlagsEx CastFlagsEx;
        public uint CastTime;
        public List<ObjectGuid> HitTargets = new List<ObjectGuid>();
        public List<ObjectGuid> MissTargets = new List<ObjectGuid>();
        public List<SpellMissStatus> MissStatus = new List<SpellMissStatus>();
        public SpellTargetData Target = new SpellTargetData();
        public List<SpellPowerData> RemainingPower = new List<SpellPowerData>();
        public Optional<RuneData> RemainingRunes;
        public MissileTrajectoryResult MissileTrajectory;
        public SpellAmmo Ammo;
        public byte DestLocSpellCastIndex;
        public List<TargetLocation> TargetPoints = new List<TargetLocation>();
        public CreatureImmunities Immunities;
        public SpellHealPrediction Predict;
    }

    public struct SpellModifierData
    {
        public float ModifierValue;
        public byte ClassIndex;

        public void Write(WorldPacket data)
        {
            data.WriteFloat(ModifierValue);
            data.WriteUInt8(ClassIndex);
        }
    }

    public class SpellModifierInfo
    {
        public byte ModIndex;
        public List<SpellModifierData> ModifierData = new List<SpellModifierData>();

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

        public uint SrecID;
        public uint ForcedCooldown;
        public float ModRate = 1.0f;
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

        public uint SpellID;
        public uint ItemID;
        public uint Category;
        public int RecoveryTime;
        public int CategoryRecoveryTime;
        public float ModRate = 1.0f;
        public bool OnHold;
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

        public uint Category;
        public uint NextRecoveryTime;
        public float ChargeModRate = 1.0f;
        public byte ConsumedCharges;
    }

    public struct SpellChannelStartInterruptImmunities
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(SchoolImmunities);
            data.WriteInt32(Immunities);
        }

        public int SchoolImmunities;
        public int Immunities;
    }

    public struct SpellTargetedHealPrediction
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(TargetGUID);
            Predict.Write(data);
        }

        public ObjectGuid TargetGUID;
        public SpellHealPrediction Predict;
    }
}
