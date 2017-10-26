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
using Game.Groups;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class PartyCommandResult : ServerPacket
    {
        public PartyCommandResult() : base(ServerOpcodes.PartyCommandResult) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Name.Length, 9);
            _worldPacket.WriteBits(Command, 4);
            _worldPacket.WriteBits(Result, 6);

            _worldPacket.WriteUInt32(ResultData);
            _worldPacket.WritePackedGuid(ResultGUID);
            _worldPacket.WriteString(Name);
        }

        public string Name { get; set; }
        public byte Command { get; set; }
        public byte Result { get; set; }
        public uint ResultData { get; set; }
        public ObjectGuid ResultGUID { get; set; }
    }

    class PartyInviteClient : ClientPacket
    {
        public PartyInviteClient(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            ProposedRoles = _worldPacket.ReadInt32();
            TargetGUID = _worldPacket.ReadPackedGuid();

            uint targetNameLen = _worldPacket.ReadBits<uint>(9);
            uint targetRealmLen = _worldPacket.ReadBits<uint>(9);

            TargetName = _worldPacket.ReadString(targetNameLen);
            TargetRealm = _worldPacket.ReadString(targetRealmLen);
        }

        public sbyte PartyIndex { get; set; }
        public int ProposedRoles { get; set; }
        public string TargetName { get; set; }
        public string TargetRealm { get; set; }
        public ObjectGuid TargetGUID { get; set; }
    }

    class PartyInvite : ServerPacket
    {
        public PartyInvite() : base(ServerOpcodes.PartyInvite) { }

        public void Initialize(Player inviter, int proposedRoles, bool canAccept)
        {
            CanAccept = canAccept;

            InviterName = inviter.GetName();
            InviterGUID = inviter.GetGUID();
            InviterBNetAccountId = inviter.GetSession().GetAccountGUID();

            ProposedRoles = proposedRoles;

            InviterVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            InviterRealmNameActual = Global.WorldMgr.GetRealm().Name;
            InviterRealmNameNormalized = Global.WorldMgr.GetRealm().NormalizedName;
        }

        public override void Write()
        {
            _worldPacket.WriteBit(CanAccept);
            _worldPacket.WriteBit(MightCRZYou);
            _worldPacket.WriteBit(IsXRealm);
            _worldPacket.WriteBit(MustBeBNetFriend);
            _worldPacket.WriteBit(AllowMultipleRoles);
            _worldPacket.WriteBits(InviterName.Length, 6);

            _worldPacket.WriteUInt32(InviterVirtualRealmAddress);
            _worldPacket.WriteBit(IsLocal);
            _worldPacket.WriteBit(Unk2);
            _worldPacket.WriteBits(InviterRealmNameActual.Length, 8);
            _worldPacket.WriteBits(InviterRealmNameNormalized.Length, 8);
            _worldPacket.WriteString(InviterRealmNameActual);
            _worldPacket.WriteString(InviterRealmNameNormalized);

            _worldPacket.WritePackedGuid(InviterGUID);
            _worldPacket.WritePackedGuid(InviterBNetAccountId);
            _worldPacket.WriteUInt16(Unk1);
            _worldPacket.WriteInt32(ProposedRoles);
            _worldPacket.WriteInt32(LfgSlots.Count);
            _worldPacket.WriteInt32(LfgCompletedMask);

            _worldPacket.WriteString(InviterName);

            foreach (int LfgSlot in LfgSlots)
                _worldPacket.WriteInt32(LfgSlot);
        }

        public bool MightCRZYou { get; set; }
        public bool MustBeBNetFriend { get; set; }
        public bool AllowMultipleRoles { get; set; }
        public bool Unk2 { get; set; }
        public ushort Unk1 { get; set; }

        public bool CanAccept { get; set; }

        // Inviter
        public ObjectGuid InviterGUID { get; set; }
        public ObjectGuid InviterBNetAccountId { get; set; }
        public string InviterName { get; set; }

        // Realm
        public bool IsXRealm { get; set; }
        public bool IsLocal { get; set; } = true;
        public uint InviterVirtualRealmAddress { get; set; }
        public string InviterRealmNameActual { get; set; }
        public string InviterRealmNameNormalized { get; set; }

        // Lfg
        public int ProposedRoles { get; set; }
        public int LfgCompletedMask { get; set; }
        public List<int> LfgSlots { get; set; } = new List<int>();
    }

    class PartyInviteResponse : ClientPacket
    {
        public PartyInviteResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();

            Accept = _worldPacket.HasBit();

            bool hasRolesDesired = _worldPacket.HasBit();
            if (hasRolesDesired)
                RolesDesired.Set(_worldPacket.ReadInt32());
        }

        public sbyte PartyIndex { get; set; }
        public bool Accept { get; set; }
        public Optional<int> RolesDesired { get; set; }
    }

    class PartyUninvite : ClientPacket
    {
        public PartyUninvite(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();

            byte reasonLen = _worldPacket.ReadBits<byte>(8);
            Reason = _worldPacket.ReadString(reasonLen);
        }

        public sbyte PartyIndex { get; set; }
        public ObjectGuid TargetGUID { get; set; }
        public string Reason { get; set; }
    }

    class GroupDecline : ServerPacket
    {
        public GroupDecline(string name) : base(ServerOpcodes.GroupDecline)
        {
            Name = name;
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Name.Length, 9);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Name);
        }

        public string Name { get; set; }
    }

    class RequestPartyMemberStats : ClientPacket
    {
        public RequestPartyMemberStats(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();
        }

        public sbyte PartyIndex { get; set; }
        public ObjectGuid TargetGUID { get; set; }
    }

    class PartyMemberState : ServerPacket
    {
        public PartyMemberState() : base(ServerOpcodes.PartyMemberState) { }

        public override void Write()
        {
            _worldPacket.WriteBit(ForEnemy);
            _worldPacket.FlushBits();

            MemberStats.Write(_worldPacket);
            _worldPacket.WritePackedGuid(MemberGuid);
        }

        public void Initialize(Player player)
        {
            ForEnemy = false;

            MemberGuid = player.GetGUID();

            // Status
            MemberStats.Status = GroupMemberOnlineStatus.Online;

            if (player.IsPvP())
                MemberStats.Status |= GroupMemberOnlineStatus.PVP;

            if (!player.IsAlive())
            {
                if (player.HasFlag(PlayerFields.Flags, PlayerFlags.Ghost))
                    MemberStats.Status |= GroupMemberOnlineStatus.Ghost;
                else
                    MemberStats.Status |= GroupMemberOnlineStatus.Dead;
            }

            if (player.IsFFAPvP())
                MemberStats.Status |= GroupMemberOnlineStatus.PVPFFA;

            if (player.isAFK())
                MemberStats.Status |= GroupMemberOnlineStatus.AFK;

            if (player.isDND())
                MemberStats.Status |= GroupMemberOnlineStatus.DND;

            if (player.GetVehicle())
                MemberStats.Status |= GroupMemberOnlineStatus.Vehicle;

            // Level
            MemberStats.Level = (ushort)player.getLevel();

            // Health
            MemberStats.CurrentHealth = (int)player.GetHealth();
            MemberStats.MaxHealth = (int)player.GetMaxHealth();

            // Power
            MemberStats.PowerType = (byte)player.getPowerType();
            MemberStats.PowerDisplayID = 0;
            MemberStats.CurrentPower = (ushort)player.GetPower(player.getPowerType());
            MemberStats.MaxPower = (ushort)player.GetMaxPower(player.getPowerType());

            // Position
            MemberStats.ZoneID = (ushort)player.GetZoneId();
            MemberStats.PositionX = (short)player.GetPositionX();
            MemberStats.PositionY = (short)(player.GetPositionY());
            MemberStats.PositionZ = (short)(player.GetPositionZ());

            MemberStats.SpecID = (ushort)player.GetUInt32Value(PlayerFields.CurrentSpecId);
            MemberStats.PartyType[0] = (sbyte)(player.GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetPartyType) & 0xF);
            MemberStats.PartyType[1] = (sbyte)(player.GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetPartyType) >> 4);
            MemberStats.WmoGroupID = 0;
            MemberStats.WmoDoodadPlacementID = 0;

            // Vehicle
            if (player.GetVehicle() && player.GetVehicle().GetVehicleInfo() != null)
                MemberStats.VehicleSeat = player.GetVehicle().GetVehicleInfo().SeatID[player.m_movementInfo.transport.seat];

            // Auras
            foreach (AuraApplication aurApp in player.GetVisibleAuras())
            {
                PartyMemberAuraStates aura = new PartyMemberAuraStates();
                aura.SpellID = (int)aurApp.GetBase().GetId();
                aura.ActiveFlags = aurApp.GetEffectMask();
                aura.Flags = (byte)aurApp.GetFlags();

                if (aurApp.GetFlags().HasAnyFlag(AuraFlags.Scalable))
                {
                    foreach (AuraEffect aurEff in aurApp.GetBase().GetAuraEffects())
                    {
                        if (aurEff == null)
                            continue;

                        if (aurApp.HasEffect(aurEff.GetEffIndex()))
                            aura.Points.Add((float)aurEff.GetAmount());
                    }
                }

                MemberStats.Auras.Add(aura);
            }

            // Phases
            var phases = player.GetPhases();
            MemberStats.Phases.PhaseShiftFlags = 0x08 | (!phases.Empty() ? 0x10 : 0);
            MemberStats.Phases.PersonalGUID = ObjectGuid.Empty;
            foreach (uint phaseId in phases)
            {
                PartyMemberPhase phase = new PartyMemberPhase();
                phase.Id = (ushort)phaseId;
                phase.Flags = 1;
                MemberStats.Phases.List.Add(phase);
            }

            // Pet
            if (player.GetPet())
            {
                Pet pet = player.GetPet();

                MemberStats.PetStats.HasValue = true;

                MemberStats.PetStats.Value.GUID = pet.GetGUID();
                MemberStats.PetStats.Value.Name = pet.GetName();
                MemberStats.PetStats.Value.ModelId = (short)pet.GetDisplayId();

                MemberStats.PetStats.Value.CurrentHealth = (int)pet.GetHealth();
                MemberStats.PetStats.Value.MaxHealth = (int)pet.GetMaxHealth();

                foreach (AuraApplication aurApp in pet.GetVisibleAuras())
                {
                    PartyMemberAuraStates aura = new PartyMemberAuraStates();

                    aura.SpellID = (int)aurApp.GetBase().GetId();
                    aura.ActiveFlags = aurApp.GetEffectMask();
                    aura.Flags = (byte)aurApp.GetFlags();

                    if (aurApp.GetFlags().HasAnyFlag(AuraFlags.Scalable))
                    {
                        foreach (AuraEffect aurEff in aurApp.GetBase().GetAuraEffects())
                        {
                            if (aurEff == null)
                                continue;

                            if (aurApp.HasEffect(aurEff.GetEffIndex()))
                                aura.Points.Add((float)aurEff.GetAmount());
                        }
                    }

                    MemberStats.PetStats.Value.Auras.Add(aura);
                }

            }
        }

        public bool ForEnemy { get; set; }
        public ObjectGuid MemberGuid { get; set; }
        public PartyMemberStats MemberStats { get; set; } = new PartyMemberStats();
    }

    class SetPartyLeader : ClientPacket
    {
        public SetPartyLeader(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();
        }

        public sbyte PartyIndex { get; set; }
        public ObjectGuid TargetGUID { get; set; }
    }

    class SetRole : ClientPacket
    {
        public SetRole(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();
            Role = _worldPacket.ReadInt32();
        }

        public sbyte PartyIndex { get; set; }
        public ObjectGuid TargetGUID { get; set; }
        public int Role { get; set; }
    }

    class RoleChangedInform : ServerPacket
    {
        public RoleChangedInform() : base(ServerOpcodes.RoleChangedInform) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WritePackedGuid(From);
            _worldPacket.WritePackedGuid(ChangedUnit);
            _worldPacket.WriteInt32(OldRole);
            _worldPacket.WriteInt32(NewRole);
        }

        public sbyte PartyIndex { get; set; }
        public ObjectGuid From { get; set; }
        public ObjectGuid ChangedUnit { get; set; }
        public int OldRole { get; set; }
        public int NewRole { get; set; }
    }

    class LeaveGroup : ClientPacket
    {
        public LeaveGroup(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex { get; set; }
    }

    class GroupUninvite : ServerPacket
    {
        public GroupUninvite() : base(ServerOpcodes.GroupUninvite) { }

        public override void Write() { }
    }

    class GroupDestroyed : ServerPacket
    {
        public GroupDestroyed() : base(ServerOpcodes.GroupDestroyed) { }

        public override void Write() { }
    }

    class SetLootMethod : ClientPacket
    {
        public SetLootMethod(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            LootMethod = (LootMethod)_worldPacket.ReadUInt8();
            LootMasterGUID = _worldPacket.ReadPackedGuid();
            LootThreshold = (ItemQuality)_worldPacket.ReadUInt32();
        }

        public sbyte PartyIndex { get; set; }
        public ObjectGuid LootMasterGUID { get; set; }
        public LootMethod LootMethod { get; set; }
        public ItemQuality LootThreshold { get; set; }
    }

    class MinimapPingClient : ClientPacket
    {
        public MinimapPingClient(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PositionX = _worldPacket.ReadFloat();
            PositionY = _worldPacket.ReadFloat();
            PartyIndex = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
    }

    class MinimapPing : ServerPacket
    {
        public MinimapPing() : base(ServerOpcodes.MinimapPing) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Sender);
            _worldPacket.WriteFloat(PositionX);
            _worldPacket.WriteFloat(PositionY);
        }

        public ObjectGuid Sender { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
    }

    class UpdateRaidTarget : ClientPacket
    {
        public UpdateRaidTarget(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            Target = _worldPacket.ReadPackedGuid();
            Symbol = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex { get; set; }
        public ObjectGuid Target { get; set; }
        public sbyte Symbol { get; set; }
    }

    class SendRaidTargetUpdateSingle : ServerPacket
    {
        public SendRaidTargetUpdateSingle() : base(ServerOpcodes.SendRaidTargetUpdateSingle) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WriteInt8(Symbol);
            _worldPacket.WritePackedGuid(Target);
            _worldPacket.WritePackedGuid(ChangedBy);
        }

        public sbyte PartyIndex { get; set; }
        public ObjectGuid Target { get; set; }
        public ObjectGuid ChangedBy { get; set; }
        public sbyte Symbol { get; set; }
    }

    class SendRaidTargetUpdateAll : ServerPacket
    {
        public SendRaidTargetUpdateAll() : base(ServerOpcodes.SendRaidTargetUpdateAll) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);

            _worldPacket.WriteInt32(TargetIcons.Count);

            foreach (var pair in TargetIcons)
            {
                _worldPacket.WritePackedGuid(pair.Value);
                _worldPacket.WriteUInt8(pair.Key);
            }
        }

        public sbyte PartyIndex { get; set; }
        public Dictionary<byte, ObjectGuid> TargetIcons { get; set; } = new Dictionary<byte, ObjectGuid>();
    }

    class ConvertRaid : ClientPacket
    {
        public ConvertRaid(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Raid = _worldPacket.HasBit();
        }

        public bool Raid { get; set; }
    }

    class RequestPartyJoinUpdates : ClientPacket
    {
        public RequestPartyJoinUpdates(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex { get; set; }
    }

    class SetAssistantLeader : ClientPacket
    {
        public SetAssistantLeader(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            Target = _worldPacket.ReadPackedGuid();
            Apply = _worldPacket.HasBit();
        }

        public ObjectGuid Target { get; set; }
        public sbyte PartyIndex { get; set; }
        public bool Apply { get; set; }
    }

    class SetPartyAssignment : ClientPacket
    {
        public SetPartyAssignment(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            Assignment = _worldPacket.ReadUInt8();
            Target = _worldPacket.ReadPackedGuid();
            Set = _worldPacket.HasBit();
        }

        public byte Assignment { get; set; }
        public byte PartyIndex { get; set; }
        public ObjectGuid Target { get; set; }
        public bool Set { get; set; }
    }

    class DoReadyCheck : ClientPacket
    {
        public DoReadyCheck(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex { get; set; }
    }

    class ReadyCheckStarted : ServerPacket
    {
        public ReadyCheckStarted() : base(ServerOpcodes.ReadyCheckStarted) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WritePackedGuid(PartyGUID);
            _worldPacket.WritePackedGuid(InitiatorGUID);
            _worldPacket.WriteUInt32(Duration);
        }

        public sbyte PartyIndex { get; set; }
        public ObjectGuid PartyGUID { get; set; }
        public ObjectGuid InitiatorGUID { get; set; }
        public uint Duration { get; set; }
    }

    class ReadyCheckResponseClient : ClientPacket
    {
        public ReadyCheckResponseClient(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            IsReady = _worldPacket.HasBit();
        }

        public sbyte PartyIndex { get; set; }
        public bool IsReady { get; set; }
    }

    class ReadyCheckResponse : ServerPacket
    {
        public ReadyCheckResponse() : base(ServerOpcodes.ReadyCheckResponse) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PartyGUID);
            _worldPacket.WritePackedGuid(Player);

            _worldPacket.WriteBit(IsReady);
            _worldPacket.FlushBits();
        }

        public ObjectGuid PartyGUID { get; set; }
        public ObjectGuid Player { get; set; }
        public bool IsReady { get; set; }
    }

    class ReadyCheckCompleted : ServerPacket
    {
        public ReadyCheckCompleted() : base(ServerOpcodes.ReadyCheckCompleted) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WritePackedGuid(PartyGUID);
        }

        public sbyte PartyIndex { get; set; }
        public ObjectGuid PartyGUID { get; set; }
    }

    class RequestRaidInfo : ClientPacket
    {
        public RequestRaidInfo(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class OptOutOfLoot : ClientPacket
    {
        public OptOutOfLoot(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PassOnLoot = _worldPacket.HasBit();
        }

        public bool PassOnLoot { get; set; }
    }

    class InitiateRolePoll : ClientPacket
    {
        public InitiateRolePoll(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex { get; set; }
    }

    class RolePollInform : ServerPacket
    {
        public RolePollInform() : base(ServerOpcodes.RolePollInform) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WritePackedGuid(From);
        }

        public sbyte PartyIndex { get; set; }
        public ObjectGuid From { get; set; }
    }

    class GroupNewLeader : ServerPacket
    {
        public GroupNewLeader() : base(ServerOpcodes.GroupNewLeader) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WriteBits(Name.Length, 6);
            _worldPacket.WriteString(Name);
        }

        public sbyte PartyIndex { get; set; }
        public string Name { get; set; }
    }

    class PartyUpdate : ServerPacket
    {
        public PartyUpdate() : base(ServerOpcodes.PartyUpdate) { }

        public override void Write()
        {
            _worldPacket.WriteUInt16(PartyFlags);
            _worldPacket.WriteUInt8(PartyIndex);
            _worldPacket.WriteUInt8(PartyType);
            _worldPacket.WriteInt32(MyIndex);
            _worldPacket.WritePackedGuid(PartyGUID);
            _worldPacket.WriteInt32(SequenceNum);
            _worldPacket.WritePackedGuid(LeaderGUID);
            _worldPacket.WriteUInt32(PlayerList.Count);
            _worldPacket.WriteBit(LfgInfos.HasValue);
            _worldPacket.WriteBit(LootSettings.HasValue);
            _worldPacket.WriteBit(DifficultySettings.HasValue);
            _worldPacket.FlushBits();

            foreach (var playerInfo in PlayerList)
                playerInfo.Write(_worldPacket);

            if (LootSettings.HasValue)
                LootSettings.Value.Write(_worldPacket);

            if (DifficultySettings.HasValue)
                DifficultySettings.Value.Write(_worldPacket);

            if (LfgInfos.HasValue)
                LfgInfos.Value.Write(_worldPacket);
        }

        public GroupFlags PartyFlags { get; set; }
        public byte PartyIndex { get; set; }
        public GroupType PartyType { get; set; }

        public ObjectGuid PartyGUID { get; set; }
        public ObjectGuid LeaderGUID { get; set; }

        public int MyIndex { get; set; }
        public int SequenceNum { get; set; }

        public List<PartyPlayerInfo> PlayerList { get; set; } = new List<PartyPlayerInfo>();

        public Optional<PartyLFGInfo> LfgInfos;
        public Optional<PartyLootSettings> LootSettings;
        public Optional<PartyDifficultySettings> DifficultySettings;
    }

    class SetEveryoneIsAssistant : ClientPacket
    {
        public SetEveryoneIsAssistant(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            EveryoneIsAssistant = _worldPacket.HasBit();
        }

        public sbyte PartyIndex { get; set; }
        public bool EveryoneIsAssistant { get; set; }
    }

    class ChangeSubGroup : ClientPacket
    {
        public ChangeSubGroup(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TargetGUID = _worldPacket.ReadPackedGuid();
            PartyIndex = _worldPacket.ReadInt8();
            NewSubGroup = _worldPacket.ReadUInt8();
        }

        public ObjectGuid TargetGUID { get; set; }
        public sbyte PartyIndex { get; set; }
        public byte NewSubGroup { get; set; }
    }

    class SwapSubGroups : ClientPacket
    {
        public SwapSubGroups(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            FirstTarget = _worldPacket.ReadPackedGuid();
            SecondTarget = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid FirstTarget { get; set; }
        public ObjectGuid SecondTarget { get; set; }
        public sbyte PartyIndex { get; set; }
    }

    class ClearRaidMarker : ClientPacket
    {
        public ClearRaidMarker(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MarkerId = _worldPacket.ReadUInt8();
        }

        public byte MarkerId { get; set; }
    }

    class RaidMarkersChanged : ServerPacket
    {
        public RaidMarkersChanged() : base(ServerOpcodes.RaidMarkersChanged) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WriteUInt32(ActiveMarkers);

            _worldPacket.WriteBits(RaidMarkers.Count, 4);
            _worldPacket.FlushBits();

            foreach (RaidMarker raidMarker in RaidMarkers)
            {
                _worldPacket.WritePackedGuid(raidMarker.TransportGUID);
                _worldPacket.WriteUInt32(raidMarker.Location.GetMapId());
                _worldPacket.WriteXYZ(raidMarker.Location);
            }
        }

        public sbyte PartyIndex { get; set; }
        public uint ActiveMarkers { get; set; }

        public List<RaidMarker> RaidMarkers { get; set; } = new List<RaidMarker>();
    }

    class PartyKillLog : ServerPacket
    {
        public PartyKillLog() : base(ServerOpcodes.PartyKillLog) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WritePackedGuid(Victim);
        }

        public ObjectGuid Player { get; set; }
        public ObjectGuid Victim { get; set; }
    }

    //Structs
    struct PartyMemberPhase
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt16(Flags);
            data.WriteUInt16(Id);
        }

        public ushort Flags { get; set; }
        public ushort Id { get; set; }
    }

    class PartyMemberPhaseStates
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(PhaseShiftFlags);
            data.WriteUInt32(List.Count);
            data.WritePackedGuid(PersonalGUID);

            foreach (PartyMemberPhase phase in List)
                phase.Write(data);
        }

        public int PhaseShiftFlags { get; set; }
        public ObjectGuid PersonalGUID { get; set; }
        public List<PartyMemberPhase> List { get; set; } = new List<PartyMemberPhase>();
    }

    class PartyMemberAuraStates
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(SpellID);
            data.WriteUInt8(Flags);
            data.WriteUInt32(ActiveFlags);
            data.WriteInt32(Points.Count);
            foreach (float points in Points)
                data.WriteFloat(points);
        }

        public int SpellID { get; set; }
        public byte Flags { get; set; }
        public uint ActiveFlags { get; set; }
        public List<float> Points { get; set; } = new List<float>();
    }

    class PartyMemberPetStats
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(GUID);
            data.WriteInt32(ModelId);
            data.WriteInt32(CurrentHealth);
            data.WriteInt32(MaxHealth);
            data.WriteUInt32(Auras.Count);
            Auras.ForEach(p => p.Write(data));

            data.WriteBits(Name.Length, 8);
            data.FlushBits();
            data.WriteString(Name);
        }

        public ObjectGuid GUID { get; set; }
        public string Name { get; set; }
        public short ModelId { get; set; }

        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }

        public List<PartyMemberAuraStates> Auras { get; set; } = new List<PartyMemberAuraStates>();
    }

    class PartyMemberStats
    {
        public void Write(WorldPacket data)
        {
            for (byte i = 0; i < 2; i++)
                data.WriteInt8(PartyType[i]);

            data.WriteInt16(Status);
            data.WriteUInt8(PowerType);
            data.WriteInt16(PowerDisplayID);
            data.WriteInt32(CurrentHealth);
            data.WriteInt32(MaxHealth);
            data.WriteUInt16(CurrentPower);
            data.WriteUInt16(MaxPower);
            data.WriteUInt16(Level);
            data.WriteUInt16(SpecID);
            data.WriteUInt16(ZoneID);
            data.WriteUInt16(WmoGroupID);
            data.WriteUInt32(WmoDoodadPlacementID);
            data.WriteInt16(PositionX);
            data.WriteInt16(PositionY);
            data.WriteInt16(PositionZ);
            data.WriteInt32(VehicleSeat);
            data.WriteInt32(Auras.Count);

            Phases.Write(data);

            foreach (PartyMemberAuraStates aura in Auras)
                aura.Write(data);

            data.WriteBit(PetStats.HasValue);
            data.FlushBits();

            if (PetStats.HasValue)
                PetStats.Value.Write(data);
        }

        public ushort Level { get; set; }
        public GroupMemberOnlineStatus Status { get; set; }

        public int CurrentHealth { get; set; }
        public int MaxHealth { get; set; }

        public byte PowerType { get; set; }
        public ushort CurrentPower { get; set; }
        public ushort MaxPower { get; set; }

        public ushort ZoneID { get; set; }
        public short PositionX { get; set; }
        public short PositionY { get; set; }
        public short PositionZ { get; set; }

        public int VehicleSeat { get; set; }

        public PartyMemberPhaseStates Phases { get; set; } = new PartyMemberPhaseStates();
        public List<PartyMemberAuraStates> Auras { get; set; } = new List<PartyMemberAuraStates>();
        public Optional<PartyMemberPetStats> PetStats;

        public ushort PowerDisplayID { get; set; }
        public ushort SpecID { get; set; }
        public ushort WmoGroupID { get; set; }
        public uint WmoDoodadPlacementID { get; set; }
        public sbyte[] PartyType { get; set; } = new sbyte[2];
    }

    struct PartyPlayerInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteBits(Name.Length, 6);
            data.WriteBit(FromSocialQueue);
            data.WritePackedGuid(GUID);
            data.WriteUInt8(Status);
            data.WriteUInt8(Subgroup);
            data.WriteUInt8(Flags);
            data.WriteUInt8(RolesAssigned);
            data.WriteUInt8(Class);
            data.WriteString(Name);
        }

        public ObjectGuid GUID { get; set; }
        public string Name { get; set; }
        public byte Class { get; set; }

        public GroupMemberOnlineStatus Status { get; set; }
        public byte Subgroup { get; set; }
        public byte Flags { get; set; }
        public byte RolesAssigned { get; set; }
        public bool FromSocialQueue { get; set; }
    }

    struct PartyLFGInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt8(MyFlags);
            data.WriteUInt32(Slot);
            data.WriteUInt32(MyRandomSlot);
            data.WriteUInt8(MyPartialClear);
            data.WriteFloat(MyGearDiff);
            data.WriteUInt8(MyStrangerCount);
            data.WriteUInt8(MyKickVoteCount);
            data.WriteUInt8(BootCount);
            data.WriteBit(Aborted);
            data.WriteBit(MyFirstReward);
            data.FlushBits();
        }

        public byte MyFlags { get; set; }
        public uint Slot { get; set; }
        public byte BootCount { get; set; }
        public uint MyRandomSlot { get; set; }
        public bool Aborted { get; set; }
        public byte MyPartialClear { get; set; }
        public float MyGearDiff { get; set; }
        public byte MyStrangerCount { get; set; }
        public byte MyKickVoteCount { get; set; }
        public bool MyFirstReward { get; set; }
    }

    struct PartyLootSettings
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt8(Method);
            data.WritePackedGuid(LootMaster);
            data.WriteUInt8(Threshold);
        }

        public byte Method { get; set; }
        public ObjectGuid LootMaster { get; set; }
        public byte Threshold { get; set; }
    }

    struct PartyDifficultySettings
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(DungeonDifficultyID);
            data.WriteUInt32(RaidDifficultyID);
            data.WriteUInt32(LegacyRaidDifficultyID);
        }

        public uint DungeonDifficultyID { get; set; }
        public uint RaidDifficultyID { get; set; }
        public uint LegacyRaidDifficultyID { get; set; }
    }
}
