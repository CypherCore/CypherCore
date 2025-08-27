// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Networking.Packets
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

        public int ChannelSpell;
        public int Reason;       // 40 = /run SpellStopCasting(), 16 = movement/AURA_INTERRUPT_FLAG_MOVE, 41 = turning/AURA_INTERRUPT_FLAG_TURNING
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

    public class SpellCategoryCooldown : ServerPacket
    {
        public List<CategoryCooldownInfo> CategoryCooldowns = new();

        public SpellCategoryCooldown() : base(ServerOpcodes.SpellCategoryCooldown, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(CategoryCooldowns.Count);

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
            _worldPacket.WriteInt32(KnownSpells.Count);
            _worldPacket.WriteInt32(FavoriteSpells.Count);

            foreach (var spellId in KnownSpells)
                _worldPacket.WriteUInt32(spellId);

            foreach (var spellId in FavoriteSpells)
                _worldPacket.WriteUInt32(spellId);
        }

        public bool InitialLogin;
        public List<uint> KnownSpells = new();
        public List<uint> FavoriteSpells = new(); // tradeskill recipes
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

        public uint GetButtonAction() { return (uint)(Action & 0x00FFFFFFFFFFFFFF); }
        public uint GetButtonType() { return (uint)((Action & 0xFF00000000000000) >> 56); }

        public ulong Action; // two packed values (action and type)
        public byte Index;
    }

    public class SendUnlearnSpells : ServerPacket
    {
        List<uint> Spells = new();

        public SendUnlearnSpells() : base(ServerOpcodes.SendUnlearnSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Spells.Count);
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
        public List<AuraInfo> Auras = new();
    }

    public class CastSpell : ClientPacket
    {
        public SpellCastRequestPkt Cast;

        public CastSpell(WorldPacket packet) : base(packet)
        {
            Cast = new SpellCastRequestPkt();
        }

        public override void Read()
        {
            Cast.Read(_worldPacket);
        }
    }

    public class PetCastSpell : ClientPacket
    {
        public ObjectGuid PetGUID;
        public SpellCastRequestPkt Cast;

        public PetCastSpell(WorldPacket packet) : base(packet)
        {
            Cast = new SpellCastRequestPkt();
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
        public SpellCastRequestPkt Cast;

        public UseItem(WorldPacket packet) : base(packet)
        {
            Cast = new SpellCastRequestPkt();
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

        public SpellCastData Cast = new();
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
            _worldPacket.WriteInt32(ClientLearnedSpellData.Count);

            foreach (LearnedSpellInfo spell in ClientLearnedSpellData)
                spell.Write(_worldPacket);
        }

        public List<LearnedSpellInfo> ClientLearnedSpellData = new();
    }

    public class LearnedSpells : ServerPacket
    {
        public LearnedSpells() : base(ServerOpcodes.LearnedSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(ClientLearnedSpellData.Count);
            _worldPacket.WriteUInt32(SpecializationID);
            _worldPacket.WriteBit(SuppressMessaging);
            _worldPacket.FlushBits();

            foreach (LearnedSpellInfo spell in ClientLearnedSpellData)
                spell.Write(_worldPacket);
        }

        public List<LearnedSpellInfo> ClientLearnedSpellData = new();
        public uint SpecializationID;
        public bool SuppressMessaging;
    }

    public class SpellFailure : ServerPacket
    {
        public SpellFailure() : base(ServerOpcodes.SpellFailure, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CasterUnit);
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WriteUInt32(SpellID);

            Visual.Write(_worldPacket);

            _worldPacket.WriteUInt16(Reason);
        }

        public ObjectGuid CasterUnit;
        public uint SpellID;
        public SpellCastVisual Visual;
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

            Visual.Write(_worldPacket);

            _worldPacket.WriteUInt8(Reason);
        }

        public ObjectGuid CasterUnit;
        public uint SpellID;
        public SpellCastVisual Visual;
        public byte Reason;
        public ObjectGuid CastID;
    }

    class CastFailedBase : ServerPacket
    {
        public ObjectGuid CastID;
        public int SpellID;
        public SpellCastResult Reason;
        public int FailedArg1 = -1;
        public int FailedArg2 = -1;

        public CastFailedBase(ServerOpcodes opcode, ConnectionType connectionType) : base(opcode, connectionType) { }

        public override void Write()
        {
            throw new NotImplementedException();
        }
    }

    class CastFailed : CastFailedBase
    {
        public SpellCastVisual Visual;

        public CastFailed() : base(ServerOpcodes.CastFailed, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WriteInt32(SpellID);

            Visual.Write(_worldPacket);

            _worldPacket.WriteInt32((int)Reason);
            _worldPacket.WriteInt32(FailedArg1);
            _worldPacket.WriteInt32(FailedArg2);
        }
    }

    class PetCastFailed : CastFailedBase
    {
        public PetCastFailed() : base(ServerOpcodes.PetCastFailed, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WriteInt32(SpellID);
            _worldPacket.WriteInt32((int)Reason);
            _worldPacket.WriteInt32(FailedArg1);
            _worldPacket.WriteInt32(FailedArg2);
        }
    }

    public class SetSpellModifier : ServerPacket
    {
        public SetSpellModifier(ServerOpcodes opcode) : base(opcode, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Modifiers.Count);
            foreach (SpellModifierInfo spellMod in Modifiers)
                spellMod.Write(_worldPacket);
        }

        public List<SpellModifierInfo> Modifiers = new();
    }

    public class UnlearnedSpells : ServerPacket
    {
        public UnlearnedSpells() : base(ServerOpcodes.UnlearnedSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(SpellID.Count);
            foreach (uint spellId in SpellID)
                _worldPacket.WriteUInt32(spellId);

            _worldPacket.WriteBit(SuppressMessaging);
            _worldPacket.FlushBits();
        }

        public List<uint> SpellID = new();
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
            _worldPacket.WriteUInt32(SpellID);
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
            _worldPacket.WriteInt32(SpellID.Count);
            if (!SpellID.Empty())
                SpellID.ForEach(p => _worldPacket.WriteUInt32(p));

            _worldPacket.WriteBit(IsPet);
            _worldPacket.FlushBits();
        }

        public List<uint> SpellID = new();
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
            _worldPacket.WriteBit(SkipCategory);
            _worldPacket.FlushBits();
        }

        public bool IsPet;
        public bool SkipCategory;
        public int DeltaTime;
        public uint SpellID;
    }

    class UpdateCooldown : ServerPacket
    {
        public uint SpellID;
        public float ModChange = 1.0f;
        public float ModRate = 1.0f;

        public UpdateCooldown() : base(ServerOpcodes.UpdateCooldown, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteFloat(ModChange);
            _worldPacket.WriteFloat(ModRate);
        }
    }

    class UpdateChargeCategoryCooldown : ServerPacket
    {
        public uint Category;
        public float ModChange = 1.0f;
        public float ModRate = 1.0f;
        public bool Snapshot;

        public UpdateChargeCategoryCooldown() : base(ServerOpcodes.UpdateChargeCategoryCooldown) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Category);
            _worldPacket.WriteFloat(ModChange);
            _worldPacket.WriteFloat(ModRate);
            _worldPacket.WriteBit(Snapshot);
            _worldPacket.FlushBits();
        }
    }

    public class SpellCooldownPkt : ServerPacket
    {
        public SpellCooldownPkt() : base(ServerOpcodes.SpellCooldown, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Caster);
            _worldPacket.WriteUInt8((byte)Flags);
            _worldPacket.WriteInt32(SpellCooldowns.Count);
            SpellCooldowns.ForEach(p => p.Write(_worldPacket));
        }

        public List<SpellCooldownStruct> SpellCooldowns = new();
        public ObjectGuid Caster;
        public SpellCooldownFlags Flags;
    }

    public class SendSpellHistory : ServerPacket
    {
        public SendSpellHistory() : base(ServerOpcodes.SendSpellHistory, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Entries.Count);
            Entries.ForEach(p => p.Write(_worldPacket));
        }

        public List<SpellHistoryEntry> Entries = new();
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
            _worldPacket.WriteInt32(Entries.Count);
            Entries.ForEach(p => p.Write(_worldPacket));
        }

        public List<SpellChargeEntry> Entries = new();
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
            _worldPacket.WriteBit(MountedVisual);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Source;
        public uint SpellVisualKitID;
        public bool MountedVisual;
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
            _worldPacket.WritePackedGuid(TargetTransport);
            _worldPacket.WriteUInt32(SpellVisualID);
            _worldPacket.WriteFloat(TravelSpeed);
            _worldPacket.WriteFloat(LaunchDelay);
            _worldPacket.WriteFloat(MinDuration);
            _worldPacket.WriteBit(SpeedAsTime);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Target; // Exclusive with TargetLocation
        public ObjectGuid TargetTransport;
        public Position SourceLocation;
        public uint SpellVisualID;
        public bool SpeedAsTime;
        public float TravelSpeed;
        public float LaunchDelay; // Always zero
        public float MinDuration;
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
            _worldPacket.WritePackedGuid(Transport);
            _worldPacket.WriteVector3(TargetPosition);
            _worldPacket.WriteUInt32(SpellVisualID);
            _worldPacket.WriteFloat(TravelSpeed);
            _worldPacket.WriteUInt16(HitReason);
            _worldPacket.WriteUInt16(MissReason);
            _worldPacket.WriteUInt16(ReflectStatus);
            _worldPacket.WriteFloat(LaunchDelay);
            _worldPacket.WriteFloat(MinDuration);
            _worldPacket.WriteBit(SpeedAsTime);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Source;
        public ObjectGuid Target;
        public ObjectGuid Transport; // Used when Target = Empty && (SpellVisual::Flags & 0x400) == 0
        public Vector3 TargetPosition; // Overrides missile destination for SpellVisual::SpellVisualMissileSetID
        public uint SpellVisualID;
        public float TravelSpeed;
        public ushort HitReason;
        public ushort MissReason;
        public ushort ReflectStatus;
        public float LaunchDelay;
        public float MinDuration;
        public bool SpeedAsTime;
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
            _worldPacket.WriteBit(MountedVisual);
            _worldPacket.FlushBits();
        }

        public ObjectGuid Unit;
        public uint KitRecID;
        public uint KitType;
        public uint Duration;
        public bool MountedVisual;
    }

    class SpellVisualLoadScreen : ServerPacket
    {
        public int SpellVisualKitID;
        public int Delay;

        public SpellVisualLoadScreen(int spellVisualKitId, int delay) : base(ServerOpcodes.SpellVisualLoadScreen, ConnectionType.Instance)
        {
            SpellVisualKitID = spellVisualKitId;
            Delay = delay;
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(SpellVisualKitID);
            _worldPacket.WriteInt32(Delay);
        }
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

            Visual.Write(_worldPacket);

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
        public SpellCastVisual Visual;
        public SpellChannelStartInterruptImmunities? InterruptImmunities;
        public ObjectGuid CasterGUID;
        public SpellTargetedHealPrediction? HealPrediction;
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

    class SpellEmpowerStart : ServerPacket
    {
        public ObjectGuid CastID;
        public ObjectGuid CasterGUID;
        public int SpellID;
        public SpellCastVisual Visual;
        public TimeSpan EmpowerDuration;
        public TimeSpan MinHoldTime;
        public TimeSpan HoldAtMaxTime;
        public List<ObjectGuid> Targets = new();
        public List<TimeSpan> StageDurations = new();
        public SpellChannelStartInterruptImmunities? InterruptImmunities;
        public SpellTargetedHealPrediction? HealPrediction;

        public SpellEmpowerStart() : base(ServerOpcodes.SpellEmpowerStart) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WritePackedGuid(CasterGUID);
            _worldPacket.WriteInt32(Targets.Count);
            _worldPacket.WriteInt32(SpellID);
            Visual.Write(_worldPacket);
            _worldPacket.WriteUInt32((uint)EmpowerDuration.TotalMilliseconds);
            _worldPacket.WriteUInt32((uint)MinHoldTime.TotalMilliseconds);
            _worldPacket.WriteUInt32((uint)HoldAtMaxTime.TotalMilliseconds);
            _worldPacket.WriteInt32(StageDurations.Count);

            foreach (var target in Targets)
                _worldPacket.WritePackedGuid(target);

            foreach (var stageDuration in StageDurations)
                _worldPacket.WriteUInt32((uint)stageDuration.TotalMilliseconds);

            _worldPacket.WriteBit(InterruptImmunities.HasValue);
            _worldPacket.WriteBit(HealPrediction.HasValue);
            _worldPacket.FlushBits();

            if (InterruptImmunities.HasValue)
                InterruptImmunities.Value.Write(_worldPacket);

            if (HealPrediction.HasValue)
                HealPrediction.Value.Write(_worldPacket);
        }
    }

    class SpellEmpowerUpdate : ServerPacket
    {
        public ObjectGuid CastID;
        public ObjectGuid CasterGUID;
        public TimeSpan TimeRemaining;
        public List<TimeSpan> StageDurations = new();
        public byte Status;

        public SpellEmpowerUpdate() : base(ServerOpcodes.SpellEmpowerUpdate) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WritePackedGuid(CasterGUID);
            _worldPacket.WriteUInt32((uint)TimeRemaining.TotalMilliseconds);
            _worldPacket.WriteInt32(StageDurations.Count);
            _worldPacket.WriteUInt8(Status);
            _worldPacket.FlushBits();

            foreach (var stageDuration in StageDurations)
                _worldPacket.WriteUInt32((uint)stageDuration.TotalMilliseconds);
        }
    }

    class SetEmpowerMinHoldStagePercent : ClientPacket
    {
        public float MinHoldStagePercent = 1.0f;

        public SetEmpowerMinHoldStagePercent(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MinHoldStagePercent = _worldPacket.ReadFloat();
        }
    }

    class SpellEmpowerRelease : ClientPacket
    {
        public int SpellID;

        public SpellEmpowerRelease(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SpellID = _worldPacket.ReadInt32();
        }
    }

    class SpellEmpowerRestart : ClientPacket
    {
        public int SpellID;

        public SpellEmpowerRestart(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SpellID = _worldPacket.ReadInt32();
        }
    }

    class SpellEmpowerSetStage : ServerPacket
    {
        public ObjectGuid CastID;
        public ObjectGuid CasterGUID;
        public int Stage;

        public SpellEmpowerSetStage() : base(ServerOpcodes.SpellEmpowerSetStage) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CastID);
            _worldPacket.WritePackedGuid(CasterGUID);
            _worldPacket.WriteInt32(Stage);
        }
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
        }

        public ObjectGuid UnitGUID;
    }

    class MirrorImageComponentedData : ServerPacket
    {
        public MirrorImageComponentedData() : base(ServerOpcodes.MirrorImageComponentedData) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteInt32(ChrModelID);
            _worldPacket.WriteUInt8(RaceID);
            _worldPacket.WriteUInt8(Gender);
            _worldPacket.WriteUInt8(ClassID);
            _worldPacket.WriteInt32(Customizations.Count);
            _worldPacket.WritePackedGuid(GuildGUID);
            _worldPacket.WriteInt32(ItemDisplayID.Count);
            _worldPacket.WriteInt32(SpellVisualKitID);
            _worldPacket.WriteInt32(Unused_1115);

            foreach (ChrCustomizationChoice customization in Customizations)
            {
                _worldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
                _worldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
            }

            foreach (var itemDisplayId in ItemDisplayID)
                _worldPacket.WriteInt32(itemDisplayId);
        }

        public ObjectGuid UnitGUID;
        public int ChrModelID;
        public int SpellVisualKitID;
        public int Unused_1115;
        public byte RaceID;
        public byte Gender;
        public byte ClassID;
        public List<ChrCustomizationChoice> Customizations = new();
        public ObjectGuid GuildGUID;

        public List<int> ItemDisplayID = new();
    }

    class MirrorImageCreatureData : ServerPacket
    {
        public MirrorImageCreatureData() : base(ServerOpcodes.MirrorImageCreatureData) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteInt32(DisplayID);
            _worldPacket.WriteInt32(SpellVisualKitID);
        }

        public ObjectGuid UnitGUID;
        public int DisplayID;
        public int SpellVisualKitID;
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

        public RuneData Runes = new();
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
            CastID = _worldPacket.ReadPackedGuid();
            MoveMsgID = _worldPacket.ReadUInt32();
            SpellID = _worldPacket.ReadUInt32();
            Pitch = _worldPacket.ReadFloat();
            Speed = _worldPacket.ReadFloat();
            FirePos = _worldPacket.ReadVector3();
            ImpactPos = _worldPacket.ReadVector3();
            bool hasStatus = _worldPacket.HasBit();

            _worldPacket.ResetBitPos();
            if (hasStatus)
                Status = MovementExtensions.ReadMovementInfo(_worldPacket);
        }

        public ObjectGuid Guid;
        public ObjectGuid CastID;
        public uint MoveMsgID;
        public uint SpellID;
        public float Pitch;
        public float Speed;
        public Vector3 FirePos;
        public Vector3 ImpactPos;
        public MovementInfo Status;
    }

    class UpdateAuraVisual : ClientPacket
    {
        public uint SpellID;
        public SpellCastVisual Visual;
        public ObjectGuid TargetGUID;

        public UpdateAuraVisual(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SpellID = _worldPacket.ReadUInt32();
            Visual.Read(_worldPacket);
            TargetGUID = _worldPacket.ReadPackedGuid();
        }
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
        public List<uint> FailedSpells = new();
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

    class MountResultPacket : ServerPacket
    {
        public MountResultPacket() : base(ServerOpcodes.MountResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Result);
        }

        public uint Result;
    }

    class ApplyMountEquipmentResult : ServerPacket
    {
        public ObjectGuid ItemGUID;
        public uint ItemID;
        public ApplyResult Result = ApplyResult.Success;

        public enum ApplyResult
        {
            Success = 0,
            Failure = 1
        }

        public ApplyMountEquipmentResult() : base(ServerOpcodes.ApplyMountEquipmentResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ItemGUID);
            _worldPacket.WriteUInt32(ItemID);
            _worldPacket.WriteBits(Result, 1);
            _worldPacket.FlushBits();
        }
    }

    class MissileCancel : ServerPacket
    {
        public MissileCancel() : base(ServerOpcodes.MissileCancel) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(OwnerGUID);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteBit(Reverse);
            _worldPacket.FlushBits();
        }

        public ObjectGuid OwnerGUID;
        public bool Reverse;
        public uint SpellID;
    }

    class TradeSkillSetFavorite : ClientPacket
    {
        public uint RecipeID;
        public bool IsFavorite;

        public TradeSkillSetFavorite(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RecipeID = _worldPacket.ReadUInt32();
            IsFavorite = _worldPacket.HasBit();
        }
    }

    class KeyboundOverride : ClientPacket
    {
        public KeyboundOverride(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            OverrideID = _worldPacket.ReadUInt16();
        }

        public ushort OverrideID;
    }

    class CancelQueuedSpell : ClientPacket
    {
        public CancelQueuedSpell(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    //Structs
    public struct SpellLogPowerData
    {
        public SpellLogPowerData(sbyte powerType, int amount, int cost)
        {
            PowerType = powerType;
            Amount = amount;
            Cost = cost;
        }

        public sbyte PowerType;
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
            PowerData.Add(new SpellLogPowerData((sbyte)unit.GetPowerType(), unit.GetPower(unit.GetPowerType()), 0));
        }

        public void Initialize(Spell spell)
        {
            Unit unitCaster = spell.GetCaster().ToUnit();
            if (unitCaster != null)
            {
                Health = (long)unitCaster.GetHealth();
                AttackPower = (int)unitCaster.GetTotalAttackPowerValue(unitCaster.GetClass() == Class.Hunter ? WeaponAttackType.RangedAttack : WeaponAttackType.BaseAttack);
                SpellPower = unitCaster.SpellBaseDamageBonusDone(SpellSchoolMask.Spell);
                Armor = unitCaster.GetArmor();
                PowerType primaryPowerType = unitCaster.GetPowerType();
                bool primaryPowerAdded = false;
                foreach (SpellPowerCost cost in spell.GetPowerCost())
                {
                    PowerData.Add(new SpellLogPowerData((sbyte)cost.Power, unitCaster.GetPower(cost.Power), (int)cost.Amount));
                    if (cost.Power == primaryPowerType)
                        primaryPowerAdded = true;
                }

                if (!primaryPowerAdded)
                    PowerData.Insert(0, new SpellLogPowerData((sbyte)primaryPowerType, unitCaster.GetPower(primaryPowerType), 0));
            }
        }

        public void Write(WorldPacket data)
        {
            data.WriteInt64(Health);
            data.WriteInt32(AttackPower);
            data.WriteInt32(SpellPower);
            data.WriteUInt32(Armor);
            data.WriteInt32(Unknown_1105_1);
            data.WriteInt32(Unknown_1105_2);
            data.WriteBits(PowerData.Count, 9);
            data.FlushBits();

            foreach (SpellLogPowerData powerData in PowerData)
            {
                data.WriteInt8(powerData.PowerType);
                data.WriteInt32(powerData.Amount);
                data.WriteInt32(powerData.Cost);
            }
        }

        long Health;
        int AttackPower;
        int SpellPower;
        uint Armor;
        int Unknown_1105_1;
        int Unknown_1105_2;
        List<SpellLogPowerData> PowerData = new();
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
            CreatureDifficulty creatureDifficulty = creatureTemplate.GetDifficulty(attacker.GetMap().GetDifficultyID());

            TuningType = ContentTuningType.CreatureToPlayerDamage;
            PlayerLevelDelta = (short)target.m_activePlayerData.ScalingPlayerLevelDelta;
            PlayerItemLevel = (ushort)target.GetAverageItemLevel();
            var contentTuning = CliDB.ContentTuningStorage.LookupByKey(creatureDifficulty.ContentTuningID);
            if (contentTuning != null)
            {
                ScalingHealthItemLevelCurveID = contentTuning.HealthItemLevelCurveID;
                ScalingHealthPrimaryStatCurveID = contentTuning.HealthPrimaryStatCurveID;
                TargetContentTuningID = contentTuning.Id;
            }
            TargetLevel = (byte)target.GetLevel();
            Expansion = (byte)creatureDifficulty.HealthScalingExpansion;
            TargetScalingLevelDelta = (sbyte)attacker.m_unitData.ScalingLevelDelta;
            return true;
        }

        bool GenerateDataPlayerToCreature(Player attacker, Creature target)
        {
            CreatureTemplate creatureTemplate = target.GetCreatureTemplate();
            CreatureDifficulty creatureDifficulty = creatureTemplate.GetDifficulty(target.GetMap().GetDifficultyID());

            TuningType = ContentTuningType.PlayerToCreatureDamage;
            PlayerLevelDelta = (short)attacker.m_activePlayerData.ScalingPlayerLevelDelta;
            PlayerItemLevel = (ushort)attacker.GetAverageItemLevel();
            var contentTuning = CliDB.ContentTuningStorage.LookupByKey(creatureDifficulty.ContentTuningID);
            if (contentTuning != null)
            {
                ScalingHealthItemLevelCurveID = contentTuning.HealthItemLevelCurveID;
                ScalingHealthPrimaryStatCurveID = contentTuning.HealthPrimaryStatCurveID;
                TargetContentTuningID = contentTuning.Id;
            }
            TargetLevel = (byte)target.GetLevel();
            Expansion = (byte)creatureDifficulty.HealthScalingExpansion;
            TargetScalingLevelDelta = (sbyte)target.m_unitData.ScalingLevelDelta;
            return true;
        }

        bool GenerateDataCreatureToCreature(Creature attacker, Creature target)
        {
            Creature accessor = target.HasScalableLevels() ? target : attacker;
            CreatureTemplate creatureTemplate = accessor.GetCreatureTemplate();
            CreatureDifficulty creatureDifficulty = creatureTemplate.GetDifficulty(accessor.GetMap().GetDifficultyID());

            TuningType = ContentTuningType.CreatureToCreatureDamage;
            PlayerLevelDelta = 0;
            PlayerItemLevel = 0;
            TargetLevel = (byte)target.GetLevel();
            Expansion = (byte)creatureDifficulty.HealthScalingExpansion;
            TargetScalingLevelDelta = (sbyte)accessor.m_unitData.ScalingLevelDelta;
            TargetContentTuningID = creatureDifficulty.ContentTuningID;
            return true;
        }

        public bool GenerateDataForUnits(Unit attacker, Unit target)
        {
            Player playerAttacker = attacker?.ToPlayer();
            Creature creatureAttacker = attacker?.ToCreature();
            if (playerAttacker != null)
            {
                Player playerTarget = target?.ToPlayer();
                Creature creatureTarget = target?.ToCreature();
                if (playerTarget != null)
                    return GenerateDataPlayerToPlayer(playerAttacker, playerTarget);
                else if (creatureTarget != null)
                {
                    if (creatureTarget.HasScalableLevels())
                        return GenerateDataPlayerToCreature(playerAttacker, creatureTarget);
                }
            }
            else if (creatureAttacker != null)
            {
                Player playerTarget = target?.ToPlayer();
                Creature creatureTarget = target?.ToCreature();
                if (playerTarget != null)
                {
                    if (creatureAttacker.HasScalableLevels())
                        return GenerateDataCreatureToPlayer(creatureAttacker, playerTarget);
                }
                else if (creatureTarget != null)
                {
                    if (creatureAttacker.HasScalableLevels() || creatureTarget.HasScalableLevels())
                        return GenerateDataCreatureToCreature(creatureAttacker, creatureTarget);
                }
            }

            return false;
        }

        public void Write(WorldPacket data)
        {
            data.WriteFloat(PlayerItemLevel);
            data.WriteFloat(TargetItemLevel);
            data.WriteInt16(PlayerLevelDelta);
            data.WriteInt32(ScalingHealthItemLevelCurveID);
            data.WriteInt32(Unused1117);
            data.WriteInt32(ScalingHealthPrimaryStatCurveID);
            data.WriteUInt8(TargetLevel);
            data.WriteUInt8(Expansion);
            data.WriteInt8(TargetScalingLevelDelta);
            data.WriteUInt32((uint)Flags);
            data.WriteUInt32(PlayerContentTuningID);
            data.WriteUInt32(TargetContentTuningID);
            data.WriteInt32(TargetHealingContentTuningID);
            data.WriteFloat(PlayerPrimaryStatToExpectedRatio);
            data.WriteBits(TuningType, 4);
            data.FlushBits();
        }

        public ContentTuningType TuningType;
        public short PlayerLevelDelta;
        public float PlayerItemLevel;
        public float TargetItemLevel;
        public int ScalingHealthItemLevelCurveID = 0;
        public int Unused1117 = 0;
        public int ScalingHealthPrimaryStatCurveID = 0;
        public byte TargetLevel;
        public byte Expansion;
        public sbyte TargetScalingLevelDelta;
        public ContentTuningFlags Flags = ContentTuningFlags.NoLevelScaling | ContentTuningFlags.NoItemLevelScaling;
        public uint PlayerContentTuningID;
        public uint TargetContentTuningID;
        public int TargetHealingContentTuningID = 0; // direct heal only, not periodic
        public float PlayerPrimaryStatToExpectedRatio = 1.0f;

        public enum ContentTuningType
        {
            CreatureToPlayerDamage = 1,
            PlayerToCreatureDamage = 2,
            CreatureToPlayerHealing = 3,
            PlayerToCreatureHealing = 4,
            CreatureToCreatureDamage = 5,
            CreatureToCreatureHealing = 6,
            PlayerToPlayerDamage = 7, // Nyi
            PlayerToPlayerHealing = 8,
        }

        public enum ContentTuningFlags
        {
            NoLevelScaling = 0x1,
            NoItemLevelScaling = 0x2
        }
    }

    struct CombatWorldTextViewerInfo
    {
        public ObjectGuid ViewerGUID;
        public byte? ColorType;
        public byte? ScaleType;

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(ViewerGUID);
            data.WriteBit(ColorType.HasValue);
            data.WriteBit(ScaleType.HasValue);
            data.FlushBits();

            if (ColorType.HasValue)
                data.WriteUInt8(ColorType.Value);

            if (ScaleType.HasValue)
                data.WriteUInt8(ScaleType.Value);
        }
    }

    public struct SpellSupportInfo
    {
        public ObjectGuid Supporter;
        public int SupportSpellID;
        public int AmountRaw;
        public float AmountPortion;

        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(Supporter);
            data.WriteInt32(SupportSpellID);
            data.WriteInt32(AmountRaw);
            data.WriteFloat(AmountPortion);
        }
    }

    public struct SpellCastVisual
    {
        public uint SpellXSpellVisualID;
        public uint ScriptVisualID;

        public SpellCastVisual(uint spellXSpellVisualID, uint scriptVisualID)
        {
            SpellXSpellVisualID = spellXSpellVisualID;
            ScriptVisualID = scriptVisualID;
        }

        public void Read(WorldPacket data)
        {
            SpellXSpellVisualID = data.ReadUInt32();
            ScriptVisualID = data.ReadUInt32();
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(SpellXSpellVisualID);
            data.WriteUInt32(ScriptVisualID);
        }

        public static implicit operator SpellCastVisualField(SpellCastVisual spellCastVisual)
        {
            SpellCastVisualField visual = new();
            visual.SpellXSpellVisualID = spellCastVisual.SpellXSpellVisualID;
            visual.ScriptVisualID = spellCastVisual.ScriptVisualID;
            return visual;
        }
    }

    public class AuraDataInfo
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(CastID);
            data.WriteInt32(SpellID);

            Visual.Write(data);

            data.WriteUInt16((ushort)Flags);
            data.WriteUInt32(ActiveFlags);
            data.WriteUInt16(CastLevel);
            data.WriteUInt8(Applications);
            data.WriteInt32(ContentTuningID);
            data.WriteVector3(DstLocation);
            data.WriteBit(CastUnit.HasValue);
            data.WriteBit(Duration.HasValue);
            data.WriteBit(Remaining.HasValue);
            data.WriteBit(TimeMod.HasValue);
            data.WriteBits(Points.Count, 6);
            data.WriteBits(EstimatedPoints.Count, 6);
            data.WriteBit(ContentTuning != null);

            if (ContentTuning != null)
                ContentTuning.Write(data);

            if (CastUnit.HasValue)
                data.WritePackedGuid(CastUnit.Value);

            if (Duration.HasValue)
                data.WriteInt32(Duration.Value);

            if (Remaining.HasValue)
                data.WriteInt32(Remaining.Value);

            if (TimeMod.HasValue)
                data.WriteFloat(TimeMod.Value);

            foreach (var point in Points)
                data.WriteFloat(point);

            foreach (var point in EstimatedPoints)
                data.WriteFloat(point);
        }

        public ObjectGuid CastID;
        public int SpellID;
        public SpellCastVisual Visual;
        public AuraFlags Flags;
        public uint ActiveFlags;
        public ushort CastLevel = 1;
        public byte Applications = 1;
        public int ContentTuningID;
        ContentTuningParams ContentTuning;
        public ObjectGuid? CastUnit;
        public int? Duration;
        public int? Remaining;
        float? TimeMod;
        public List<float> Points = new();
        public List<float> EstimatedPoints = new();
        public Vector3 DstLocation;
    }

    public struct AuraInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt16(Slot);
            data.WriteBit(AuraData != null);
            data.FlushBits();

            if (AuraData != null)
                AuraData.Write(data);
        }

        public ushort Slot;
        public AuraDataInfo AuraData;
    }

    public class TargetLocation
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
            data.ResetBitPos();
            Flags = (SpellCastTargetFlags)data.ReadBits<uint>(28);
            if (data.HasBit())
                SrcLocation = new();

            if (data.HasBit())
                DstLocation = new();

            bool hasOrientation = data.HasBit();
            bool hasMapId = data.HasBit();

            uint nameLength = data.ReadBits<uint>(7);

            Unit = data.ReadPackedGuid();
            Item = data.ReadPackedGuid();

            if (SrcLocation != null)
                SrcLocation.Read(data);

            if (DstLocation != null)
                DstLocation.Read(data);

            if (hasOrientation)
                Orientation = data.ReadFloat();

            if (hasMapId)
                MapID = data.ReadInt32();

            Name = data.ReadString(nameLength);
        }

        public void Write(WorldPacket data)
        {
            data.WriteBits((uint)Flags, 28);
            data.WriteBit(SrcLocation != null);
            data.WriteBit(DstLocation != null);
            data.WriteBit(Orientation.HasValue);
            data.WriteBit(MapID.HasValue);
            data.WriteBits(Name.GetByteCount(), 7);
            data.FlushBits();

            data.WritePackedGuid(Unit);
            data.WritePackedGuid(Item);

            if (SrcLocation != null)
                SrcLocation.Write(data);

            if (DstLocation != null)
                DstLocation.Write(data);

            if (Orientation.HasValue)
                data.WriteFloat(Orientation.Value);

            if (MapID.HasValue)
                data.WriteInt32(MapID.Value);

            data.WriteString(Name);
        }

        public SpellCastTargetFlags Flags;
        public ObjectGuid Unit;
        public ObjectGuid Item;
        public TargetLocation SrcLocation;
        public TargetLocation DstLocation;
        public float? Orientation;
        public int? MapID;
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

    public struct SpellCraftingReagent
    {
        public int ItemID;
        public int DataSlotIndex;
        public int Quantity;
        public byte? Source;

        public void Read(WorldPacket data)
        {
            ItemID = data.ReadInt32();
            DataSlotIndex = data.ReadInt32();
            Quantity = data.ReadInt32();
            if (data.HasBit())
                Source = data.ReadUInt8();
        }
    }

    public struct SpellExtraCurrencyCost
    {
        public int CurrencyID;
        public int Count;

        public void Read(WorldPacket data)
        {
            CurrencyID = data.ReadInt32();
            Count = data.ReadInt32();
        }
    }

    public class SpellCastRequestPkt
    {
        public ObjectGuid CastID;
        public uint SpellID;
        public SpellCastVisual Visual;
        public uint SendCastFlags;
        public SpellTargetData Target = new();
        public MissileTrajectoryRequest MissileTrajectory;
        public MovementInfo MoveUpdate;
        public List<SpellWeight> Weight = new();
        public Array<SpellCraftingReagent> OptionalReagents = new(6);
        public Array<SpellCraftingReagent> RemovedModifications = new(6);
        public Array<SpellExtraCurrencyCost> OptionalCurrencies = new(5 /*MAX_ITEM_EXT_COST_CURRENCIES*/);
        public ulong? CraftingOrderID;
        public byte CraftingFlags; // 1 = ApplyConcentration
        public ObjectGuid CraftingNPC;
        public uint[] Misc = new uint[2];

        public void Read(WorldPacket data)
        {
            CastID = data.ReadPackedGuid();
            Misc[0] = data.ReadUInt32();
            Misc[1] = data.ReadUInt32();
            SpellID = data.ReadUInt32();

            Visual.Read(data);

            MissileTrajectory.Read(data);
            CraftingNPC = data.ReadPackedGuid();

            var optionalCurrenciesCount = data.ReadUInt32();
            var optionalReagentsCount = data.ReadUInt32();
            var removedModificationsCount = data.ReadUInt32();
            CraftingFlags = data.ReadUInt8();

            for (var i = 0; i < optionalCurrenciesCount; ++i)
                OptionalCurrencies[i].Read(data);

            SendCastFlags = data.ReadBits<uint>(5);
            bool hasMoveUpdate = data.HasBit();
            var weightCount = data.ReadBits<uint>(2);
            bool hasCraftingOrderID = data.HasBit();

            Target.Read(data);

            if (hasCraftingOrderID)
                CraftingOrderID = data.ReadUInt64();

            for (var i = 0; i < optionalReagentsCount; ++i)
                OptionalReagents[i].Read(data);

            for (var i = 0; i < removedModificationsCount; ++i)
                RemovedModifications[i].Read(data);

            if (hasMoveUpdate)
                MoveUpdate = MovementExtensions.ReadMovementInfo(data);

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
    }

    public struct SpellHitStatus
    {
        public SpellHitStatus(SpellMissInfo reason)
        {
            Reason = reason;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt8((byte)Reason);
        }

        public SpellMissInfo Reason;
    }

    public struct SpellMissStatus
    {
        public SpellMissStatus(SpellMissInfo reason, SpellMissInfo reflectStatus)
        {
            Reason = reason;
            ReflectStatus = reflectStatus;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt8((byte)Reason);
            if (Reason == SpellMissInfo.Reflect)
                data.WriteUInt8((byte)ReflectStatus);
        }

        public SpellMissInfo Reason;
        public SpellMissInfo ReflectStatus;
    }

    public struct SpellPowerData
    {
        public int Cost;
        public PowerType Type;

        public void Write(WorldPacket data)
        {
            data.WriteInt8((sbyte)Type);
            data.WriteInt32(Cost);
        }
    }

    public class RuneData
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt8(Start);
            data.WriteUInt8(Count);
            data.WriteInt32(Cooldowns.Count);

            foreach (byte cd in Cooldowns)
                data.WriteUInt8(cd);
        }

        public byte Start;
        public byte Count;
        public List<byte> Cooldowns = new();
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

    public struct SpellHealPrediction
    {
        public ObjectGuid BeaconGUID;
        public uint Points;
        public SpellHealPredictionType Type;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(Points);
            data.WriteUInt8((byte)Type);
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

            Visual.Write(data);

            data.WriteUInt32((uint)CastFlags);
            data.WriteUInt32((uint)CastFlagsEx);
            data.WriteUInt32(CastFlagsEx2);
            data.WriteUInt32(CastTime);

            MissileTrajectory.Write(data);

            data.WriteInt32(AmmoDisplayID);
            data.WriteUInt8(DestLocSpellCastIndex);

            Immunities.Write(data);
            Predict.Write(data);

            data.WriteBits(HitTargets.Count, 16);
            data.WriteBits(MissTargets.Count, 16);
            data.WriteBits(HitStatus.Count, 16);
            data.WriteBits(MissStatus.Count, 16);
            data.WriteBits(RemainingPower.Count, 9);
            data.WriteBit(RemainingRunes != null);
            data.WriteBits(TargetPoints.Count, 16);
            data.FlushBits();

            Target.Write(data);

            foreach (ObjectGuid hitTarget in HitTargets)
                data.WritePackedGuid(hitTarget);

            foreach (ObjectGuid missTarget in MissTargets)
                data.WritePackedGuid(missTarget);

            foreach (SpellHitStatus hitStatus in HitStatus)
                hitStatus.Write(data);

            foreach (SpellMissStatus missStatus in MissStatus)
                missStatus.Write(data);

            foreach (SpellPowerData power in RemainingPower)
                power.Write(data);

            if (RemainingRunes != null)
                RemainingRunes.Write(data);

            foreach (TargetLocation targetLoc in TargetPoints)
                targetLoc.Write(data);
        }

        public ObjectGuid CasterGUID;
        public ObjectGuid CasterUnit;
        public ObjectGuid CastID;
        public ObjectGuid OriginalCastID;
        public int SpellID;
        public SpellCastVisual Visual;
        public SpellCastFlags CastFlags;
        public SpellCastFlagsEx CastFlagsEx;
        public uint CastFlagsEx2;
        public uint CastTime;
        public List<ObjectGuid> HitTargets = new();
        public List<ObjectGuid> MissTargets = new();
        public List<SpellHitStatus> HitStatus = new();
        public List<SpellMissStatus> MissStatus = new();
        public SpellTargetData Target = new();
        public List<SpellPowerData> RemainingPower = new();
        public RuneData RemainingRunes;
        public MissileTrajectoryResult MissileTrajectory;
        public int AmmoDisplayID;
        public byte DestLocSpellCastIndex;
        public List<TargetLocation> TargetPoints = new();
        public CreatureImmunities Immunities;
        public SpellHealPrediction Predict;

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
    }

    public struct LearnedSpellInfo
    {
        public uint SpellID;
        public bool Favorite;
        public int? EquipableSpellInvSlot;
        public int? Superceded;
        public int? TraitDefinitionID;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(SpellID);
            data.WriteBit(Favorite);
            data.WriteBit(EquipableSpellInvSlot.HasValue);
            data.WriteBit(Superceded.HasValue);
            data.WriteBit(TraitDefinitionID.HasValue);
            data.FlushBits();

            if (EquipableSpellInvSlot.HasValue)
                data.WriteInt32(EquipableSpellInvSlot.Value);

            if (Superceded.HasValue)
                data.WriteInt32(Superceded.Value);

            if (TraitDefinitionID.HasValue)
                data.WriteInt32(TraitDefinitionID.Value);
        }
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
        public List<SpellModifierData> ModifierData = new();

        public void Write(WorldPacket data)
        {
            data.WriteUInt8(ModIndex);
            data.WriteInt32(ModifierData.Count);
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
            data.WriteBit(RecoveryTimeStartOffset.HasValue);
            data.WriteBit(CategoryRecoveryTimeStartOffset.HasValue);
            data.WriteBit(OnHold);
            data.FlushBits();

            if (RecoveryTimeStartOffset.HasValue)
                data.WriteUInt32(RecoveryTimeStartOffset.Value);
            if (CategoryRecoveryTimeStartOffset.HasValue)
                data.WriteUInt32(CategoryRecoveryTimeStartOffset.Value);
        }

        public uint SpellID;
        public uint ItemID;
        public uint Category;
        public int RecoveryTime;
        public int CategoryRecoveryTime;
        public float ModRate = 1.0f;
        public bool OnHold;
        uint? RecoveryTimeStartOffset;
        uint? CategoryRecoveryTimeStartOffset;
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
