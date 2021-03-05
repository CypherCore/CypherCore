/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

namespace Game.Networking.Packets
{
    internal class PartyCommandResult : ServerPacket
    {
        public PartyCommandResult() : base(ServerOpcodes.PartyCommandResult) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Name.GetByteCount(), 9);
            _worldPacket.WriteBits(Command, 4);
            _worldPacket.WriteBits(Result, 6);

            _worldPacket.WriteUInt32(ResultData);
            _worldPacket.WritePackedGuid(ResultGUID);
            _worldPacket.WriteString(Name);
        }

        public string Name;
        public byte Command;
        public byte Result;
        public uint ResultData;
        public ObjectGuid ResultGUID;
    }

    internal class PartyInviteClient : ClientPacket
    {
        public PartyInviteClient(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            ProposedRoles = _worldPacket.ReadUInt32();
            TargetGUID = _worldPacket.ReadPackedGuid();

            var targetNameLen = _worldPacket.ReadBits<uint>(9);
            var targetRealmLen = _worldPacket.ReadBits<uint>(9);

            TargetName = _worldPacket.ReadString(targetNameLen);
            TargetRealm = _worldPacket.ReadString(targetRealmLen);
        }

        public byte PartyIndex;
        public uint ProposedRoles;
        public string TargetName;
        public string TargetRealm;
        public ObjectGuid TargetGUID;
    }

    internal class PartyInvite : ServerPacket
    {
        public PartyInvite() : base(ServerOpcodes.PartyInvite) { }

        public void Initialize(Player inviter, uint proposedRoles, bool canAccept)
        {
            CanAccept = canAccept;

            InviterName = inviter.GetName();
            InviterGUID = inviter.GetGUID();
            InviterBNetAccountId = inviter.GetSession().GetAccountGUID();

            ProposedRoles = proposedRoles;

            var realm = Global.WorldMgr.GetRealm();
            InviterRealm = new VirtualRealmInfo(realm.Id.GetAddress(), true, false, realm.Name, realm.NormalizedName);
        }

        public override void Write()
        {
            _worldPacket.WriteBit(CanAccept);
            _worldPacket.WriteBit(MightCRZYou);
            _worldPacket.WriteBit(IsXRealm);
            _worldPacket.WriteBit(MustBeBNetFriend);
            _worldPacket.WriteBit(AllowMultipleRoles);
            _worldPacket.WriteBit(QuestSessionActive);
            _worldPacket.WriteBits(InviterName.GetByteCount(), 6);

            InviterRealm.Write(_worldPacket);

            _worldPacket.WritePackedGuid(InviterGUID);
            _worldPacket.WritePackedGuid(InviterBNetAccountId);
            _worldPacket.WriteUInt16(Unk1);
            _worldPacket.WriteUInt32(ProposedRoles);
            _worldPacket.WriteInt32(LfgSlots.Count);
            _worldPacket.WriteInt32(LfgCompletedMask);

            _worldPacket.WriteString(InviterName);

            foreach (var LfgSlot in LfgSlots)
                _worldPacket.WriteInt32(LfgSlot);
        }

        public bool MightCRZYou;
        public bool MustBeBNetFriend;
        public bool AllowMultipleRoles;
        public bool QuestSessionActive;
        public ushort Unk1;

        public bool CanAccept;

        // Inviter
        public VirtualRealmInfo InviterRealm;
        public ObjectGuid InviterGUID;
        public ObjectGuid InviterBNetAccountId;
        public string InviterName;

        // Realm
        public bool IsXRealm;

        // Lfg
        public uint ProposedRoles;
        public int LfgCompletedMask;
        public List<int> LfgSlots = new List<int>();
    }

    internal class PartyInviteResponse : ClientPacket
    {
        public PartyInviteResponse(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();

            Accept = _worldPacket.HasBit();

            var hasRolesDesired = _worldPacket.HasBit();
            if (hasRolesDesired)
                RolesDesired.Set(_worldPacket.ReadUInt32());
        }

        public byte PartyIndex;
        public bool Accept;
        public Optional<uint> RolesDesired;
    }

    internal class PartyUninvite : ClientPacket
    {
        public PartyUninvite(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();

            var reasonLen = _worldPacket.ReadBits<byte>(8);
            Reason = _worldPacket.ReadString(reasonLen);
        }

        public byte PartyIndex;
        public ObjectGuid TargetGUID;
        public string Reason;
    }

    internal class GroupDecline : ServerPacket
    {
        public GroupDecline(string name) : base(ServerOpcodes.GroupDecline)
        {
            Name = name;
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Name.GetByteCount(), 9);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Name);
        }

        public string Name;
    }

    internal class GroupUninvite : ServerPacket
    {
        public GroupUninvite() : base(ServerOpcodes.GroupUninvite) { }

        public override void Write() { }
    }

    internal class RequestPartyMemberStats : ClientPacket
    {
        public RequestPartyMemberStats(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();
        }

        public byte PartyIndex;
        public ObjectGuid TargetGUID;
    }

    internal class PartyMemberFullState : ServerPacket
    {
        public PartyMemberFullState() : base(ServerOpcodes.PartyMemberFullState) { }

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
                if (player.HasPlayerFlag(PlayerFlags.Ghost))
                    MemberStats.Status |= GroupMemberOnlineStatus.Ghost;
                else
                    MemberStats.Status |= GroupMemberOnlineStatus.Dead;
            }

            if (player.IsFFAPvP())
                MemberStats.Status |= GroupMemberOnlineStatus.PVPFFA;

            if (player.IsAFK())
                MemberStats.Status |= GroupMemberOnlineStatus.AFK;

            if (player.IsDND())
                MemberStats.Status |= GroupMemberOnlineStatus.DND;

            if (player.GetVehicle())
                MemberStats.Status |= GroupMemberOnlineStatus.Vehicle;

            // Level
            MemberStats.Level = (ushort)player.GetLevel();

            // Health
            MemberStats.CurrentHealth = (int)player.GetHealth();
            MemberStats.MaxHealth = (int)player.GetMaxHealth();

            // Power
            MemberStats.PowerType = (byte)player.GetPowerType();
            MemberStats.PowerDisplayID = 0;
            MemberStats.CurrentPower = (ushort)player.GetPower(player.GetPowerType());
            MemberStats.MaxPower = (ushort)player.GetMaxPower(player.GetPowerType());

            // Position
            MemberStats.ZoneID = (ushort)player.GetZoneId();
            MemberStats.PositionX = (short)player.GetPositionX();
            MemberStats.PositionY = (short)(player.GetPositionY());
            MemberStats.PositionZ = (short)(player.GetPositionZ());

            MemberStats.SpecID = (ushort)player.GetPrimarySpecialization();
            MemberStats.PartyType[0] = (sbyte)(player.m_playerData.PartyType & 0xF);
            MemberStats.PartyType[1] = (sbyte)(player.m_playerData.PartyType >> 4);
            MemberStats.WmoGroupID = 0;
            MemberStats.WmoDoodadPlacementID = 0;

            // Vehicle
            if (player.GetVehicle() && player.GetVehicle().GetVehicleInfo() != null)
                MemberStats.VehicleSeat = player.GetVehicle().GetVehicleInfo().SeatID[player.m_movementInfo.transport.seat];

            // Auras
            foreach (var aurApp in player.GetVisibleAuras())
            {
                var aura = new PartyMemberAuraStates();
                aura.SpellID = (int)aurApp.GetBase().GetId();
                aura.ActiveFlags = aurApp.GetEffectMask();
                aura.Flags = (byte)aurApp.GetFlags();

                if (aurApp.GetFlags().HasAnyFlag(AuraFlags.Scalable))
                {
                    foreach (var aurEff in aurApp.GetBase().GetAuraEffects())
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
            PhasingHandler.FillPartyMemberPhase(MemberStats.Phases, player.GetPhaseShift());

            // Pet
            if (player.GetPet())
            {
                var pet = player.GetPet();

                MemberStats.PetStats.HasValue = true;

                MemberStats.PetStats.Value.GUID = pet.GetGUID();
                MemberStats.PetStats.Value.Name = pet.GetName();
                MemberStats.PetStats.Value.ModelId = (short)pet.GetDisplayId();

                MemberStats.PetStats.Value.CurrentHealth = (int)pet.GetHealth();
                MemberStats.PetStats.Value.MaxHealth = (int)pet.GetMaxHealth();

                foreach (var aurApp in pet.GetVisibleAuras())
                {
                    var aura = new PartyMemberAuraStates();

                    aura.SpellID = (int)aurApp.GetBase().GetId();
                    aura.ActiveFlags = aurApp.GetEffectMask();
                    aura.Flags = (byte)aurApp.GetFlags();

                    if (aurApp.GetFlags().HasAnyFlag(AuraFlags.Scalable))
                    {
                        foreach (var aurEff in aurApp.GetBase().GetAuraEffects())
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

        public bool ForEnemy;
        public ObjectGuid MemberGuid;
        public PartyMemberStats MemberStats = new PartyMemberStats();
    }

    internal class SetPartyLeader : ClientPacket
    {
        public SetPartyLeader(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();
        }

        public sbyte PartyIndex;
        public ObjectGuid TargetGUID;
    }

    internal class SetRole : ClientPacket
    {
        public SetRole(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();
            Role = _worldPacket.ReadInt32();
        }

        public sbyte PartyIndex;
        public ObjectGuid TargetGUID;
        public int Role;
    }

    internal class RoleChangedInform : ServerPacket
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

        public sbyte PartyIndex;
        public ObjectGuid From;
        public ObjectGuid ChangedUnit;
        public int OldRole;
        public int NewRole;
    }

    internal class LeaveGroup : ClientPacket
    {
        public LeaveGroup(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex;
    }

    internal class GroupDestroyed : ServerPacket
    {
        public GroupDestroyed() : base(ServerOpcodes.GroupDestroyed) { }

        public override void Write() { }
    }

    internal class SetLootMethod : ClientPacket
    {
        public SetLootMethod(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            LootMethod = (LootMethod)_worldPacket.ReadUInt8();
            LootMasterGUID = _worldPacket.ReadPackedGuid();
            LootThreshold = (ItemQuality)_worldPacket.ReadUInt32();
        }

        public sbyte PartyIndex;
        public ObjectGuid LootMasterGUID;
        public LootMethod LootMethod;
        public ItemQuality LootThreshold;
    }

    internal class MinimapPingClient : ClientPacket
    {
        public MinimapPingClient(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PositionX = _worldPacket.ReadFloat();
            PositionY = _worldPacket.ReadFloat();
            PartyIndex = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex;
        public float PositionX;
        public float PositionY;
    }

    internal class MinimapPing : ServerPacket
    {
        public MinimapPing() : base(ServerOpcodes.MinimapPing) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Sender);
            _worldPacket.WriteFloat(PositionX);
            _worldPacket.WriteFloat(PositionY);
        }

        public ObjectGuid Sender;
        public float PositionX;
        public float PositionY;
    }

    internal class UpdateRaidTarget : ClientPacket
    {
        public UpdateRaidTarget(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            Target = _worldPacket.ReadPackedGuid();
            Symbol = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex;
        public ObjectGuid Target;
        public sbyte Symbol;
    }

    internal class SendRaidTargetUpdateSingle : ServerPacket
    {
        public SendRaidTargetUpdateSingle() : base(ServerOpcodes.SendRaidTargetUpdateSingle) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WriteInt8(Symbol);
            _worldPacket.WritePackedGuid(Target);
            _worldPacket.WritePackedGuid(ChangedBy);
        }

        public sbyte PartyIndex;
        public ObjectGuid Target;
        public ObjectGuid ChangedBy;
        public sbyte Symbol;
    }

    internal class SendRaidTargetUpdateAll : ServerPacket
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

        public sbyte PartyIndex;
        public Dictionary<byte, ObjectGuid> TargetIcons = new Dictionary<byte, ObjectGuid>();
    }

    internal class ConvertRaid : ClientPacket
    {
        public ConvertRaid(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Raid = _worldPacket.HasBit();
        }

        public bool Raid;
    }

    internal class RequestPartyJoinUpdates : ClientPacket
    {
        public RequestPartyJoinUpdates(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex;
    }

    internal class SetAssistantLeader : ClientPacket
    {
        public SetAssistantLeader(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            Target = _worldPacket.ReadPackedGuid();
            Apply = _worldPacket.HasBit();
        }

        public ObjectGuid Target;
        public byte PartyIndex;
        public bool Apply;
    }

    internal class SetPartyAssignment : ClientPacket
    {
        public SetPartyAssignment(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            Assignment = _worldPacket.ReadUInt8();
            Target = _worldPacket.ReadPackedGuid();
            Set = _worldPacket.HasBit();
        }

        public byte Assignment;
        public byte PartyIndex;
        public ObjectGuid Target;
        public bool Set;
    }

    internal class DoReadyCheck : ClientPacket
    {
        public DoReadyCheck(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex;
    }

    internal class ReadyCheckStarted : ServerPacket
    {
        public ReadyCheckStarted() : base(ServerOpcodes.ReadyCheckStarted) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WritePackedGuid(PartyGUID);
            _worldPacket.WritePackedGuid(InitiatorGUID);
            _worldPacket.WriteUInt32(Duration);
        }

        public sbyte PartyIndex;
        public ObjectGuid PartyGUID;
        public ObjectGuid InitiatorGUID;
        public uint Duration;
    }

    internal class ReadyCheckResponseClient : ClientPacket
    {
        public ReadyCheckResponseClient(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            IsReady = _worldPacket.HasBit();
        }

        public byte PartyIndex;
        public bool IsReady;
    }

    internal class ReadyCheckResponse : ServerPacket
    {
        public ReadyCheckResponse() : base(ServerOpcodes.ReadyCheckResponse) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PartyGUID);
            _worldPacket.WritePackedGuid(Player);

            _worldPacket.WriteBit(IsReady);
            _worldPacket.FlushBits();
        }

        public ObjectGuid PartyGUID;
        public ObjectGuid Player;
        public bool IsReady;
    }

    internal class ReadyCheckCompleted : ServerPacket
    {
        public ReadyCheckCompleted() : base(ServerOpcodes.ReadyCheckCompleted) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WritePackedGuid(PartyGUID);
        }

        public sbyte PartyIndex;
        public ObjectGuid PartyGUID;
    }

    internal class RequestRaidInfo : ClientPacket
    {
        public RequestRaidInfo(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    internal class OptOutOfLoot : ClientPacket
    {
        public OptOutOfLoot(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PassOnLoot = _worldPacket.HasBit();
        }

        public bool PassOnLoot;
    }

    internal class InitiateRolePoll : ClientPacket
    {
        public InitiateRolePoll(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }

        public sbyte PartyIndex;
    }

    internal class RolePollInform : ServerPacket
    {
        public RolePollInform() : base(ServerOpcodes.RolePollInform) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WritePackedGuid(From);
        }

        public sbyte PartyIndex;
        public ObjectGuid From;
    }

    internal class GroupNewLeader : ServerPacket
    {
        public GroupNewLeader() : base(ServerOpcodes.GroupNewLeader) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WriteBits(Name.GetByteCount(), 9);
            _worldPacket.WriteString(Name);
        }

        public sbyte PartyIndex;
        public string Name;
    }

    internal class PartyUpdate : ServerPacket
    {
        public PartyUpdate() : base(ServerOpcodes.PartyUpdate) { }

        public override void Write()
        {
            _worldPacket.WriteUInt16((ushort)PartyFlags);
            _worldPacket.WriteUInt8(PartyIndex);
            _worldPacket.WriteUInt8((byte)PartyType);
            _worldPacket.WriteInt32(MyIndex);
            _worldPacket.WritePackedGuid(PartyGUID);
            _worldPacket.WriteInt32(SequenceNum);
            _worldPacket.WritePackedGuid(LeaderGUID);
            _worldPacket.WriteInt32(PlayerList.Count);
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

        public GroupFlags PartyFlags;
        public byte PartyIndex;
        public GroupType PartyType;

        public ObjectGuid PartyGUID;
        public ObjectGuid LeaderGUID;

        public int MyIndex;
        public int SequenceNum;

        public List<PartyPlayerInfo> PlayerList = new List<PartyPlayerInfo>();

        public Optional<PartyLFGInfo> LfgInfos;
        public Optional<PartyLootSettings> LootSettings;
        public Optional<PartyDifficultySettings> DifficultySettings;
    }

    internal class SetEveryoneIsAssistant : ClientPacket
    {
        public SetEveryoneIsAssistant(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            EveryoneIsAssistant = _worldPacket.HasBit();
        }

        public byte PartyIndex;
        public bool EveryoneIsAssistant;
    }

    internal class ChangeSubGroup : ClientPacket
    {
        public ChangeSubGroup(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TargetGUID = _worldPacket.ReadPackedGuid();
            PartyIndex = _worldPacket.ReadInt8();
            NewSubGroup = _worldPacket.ReadUInt8();
        }

        public ObjectGuid TargetGUID;
        public sbyte PartyIndex;
        public byte NewSubGroup;
    }

    internal class SwapSubGroups : ClientPacket
    {
        public SwapSubGroups(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            FirstTarget = _worldPacket.ReadPackedGuid();
            SecondTarget = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid FirstTarget;
        public ObjectGuid SecondTarget;
        public sbyte PartyIndex;
    }

    internal class ClearRaidMarker : ClientPacket
    {
        public ClearRaidMarker(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MarkerId = _worldPacket.ReadUInt8();
        }

        public byte MarkerId;
    }

    internal class RaidMarkersChanged : ServerPacket
    {
        public RaidMarkersChanged() : base(ServerOpcodes.RaidMarkersChanged) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WriteUInt32(ActiveMarkers);

            _worldPacket.WriteBits(RaidMarkers.Count, 4);
            _worldPacket.FlushBits();

            foreach (var raidMarker in RaidMarkers)
            {
                _worldPacket.WritePackedGuid(raidMarker.TransportGUID);
                _worldPacket.WriteUInt32(raidMarker.Location.GetMapId());
                _worldPacket.WriteXYZ(raidMarker.Location);
            }
        }

        public sbyte PartyIndex;
        public uint ActiveMarkers;

        public List<RaidMarker> RaidMarkers = new List<RaidMarker>();
    }

    internal class PartyKillLog : ServerPacket
    {
        public PartyKillLog() : base(ServerOpcodes.PartyKillLog) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WritePackedGuid(Victim);
        }

        public ObjectGuid Player;
        public ObjectGuid Victim;
    }

    //Structs
    public struct PartyMemberPhase
    {
        public PartyMemberPhase(uint flags, uint id)
        {
            Flags = (ushort)flags;
            Id = (ushort)id;
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt16(Flags);
            data.WriteUInt16(Id);
        }

        public ushort Flags;
        public ushort Id;
    }

    public class PartyMemberPhaseStates
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(PhaseShiftFlags);
            data.WriteInt32(List.Count);
            data.WritePackedGuid(PersonalGUID);

            foreach (var phase in List)
                phase.Write(data);
        }

        public int PhaseShiftFlags;
        public ObjectGuid PersonalGUID;
        public List<PartyMemberPhase> List = new List<PartyMemberPhase>();
    }

    internal class PartyMemberAuraStates
    {      
        public int SpellID;
        public ushort Flags;
        public uint ActiveFlags;
        public List<float> Points = new List<float>();

        public void Write(WorldPacket data)
        {
            data.WriteInt32(SpellID);
            data.WriteUInt16(Flags);
            data.WriteUInt32(ActiveFlags);
            data.WriteInt32(Points.Count);
            foreach (var points in Points)
                data.WriteFloat(points);
        }
    }

    internal class PartyMemberPetStats
    {
        public void Write(WorldPacket data)
        {
            data.WritePackedGuid(GUID);
            data.WriteInt32(ModelId);
            data.WriteInt32(CurrentHealth);
            data.WriteInt32(MaxHealth);
            data.WriteInt32(Auras.Count);
            Auras.ForEach(p => p.Write(data));

            data.WriteBits(Name.GetByteCount(), 8);
            data.FlushBits();
            data.WriteString(Name);
        }

        public ObjectGuid GUID;
        public string Name;
        public short ModelId;

        public int CurrentHealth;
        public int MaxHealth;

        public List<PartyMemberAuraStates> Auras = new List<PartyMemberAuraStates>();
    }

    public struct CTROptions
    {
        public uint ContentTuningConditionMask;
        public int Unused901;
        public uint ExpansionLevelMask;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(ContentTuningConditionMask);
            data.WriteInt32(Unused901);
            data.WriteUInt32(ExpansionLevelMask);
        }
    }

    internal class PartyMemberStats
    {
        public void Write(WorldPacket data)
        {
            for (byte i = 0; i < 2; i++)
                data.WriteInt8(PartyType[i]);

            data.WriteInt16((short)Status);
            data.WriteUInt8(PowerType);
            data.WriteInt16((short)PowerDisplayID);
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
            ChromieTime.Write(data);

            foreach (var aura in Auras)
                aura.Write(data);

            data.WriteBit(PetStats.HasValue);
            data.FlushBits();

            if (PetStats.HasValue)
                PetStats.Value.Write(data);
        }

        public ushort Level;
        public GroupMemberOnlineStatus Status;

        public int CurrentHealth;
        public int MaxHealth;

        public byte PowerType;
        public ushort CurrentPower;
        public ushort MaxPower;

        public ushort ZoneID;
        public short PositionX;
        public short PositionY;
        public short PositionZ;

        public int VehicleSeat;

        public PartyMemberPhaseStates Phases = new PartyMemberPhaseStates();
        public List<PartyMemberAuraStates> Auras = new List<PartyMemberAuraStates>();
        public Optional<PartyMemberPetStats> PetStats;

        public ushort PowerDisplayID;
        public ushort SpecID;
        public ushort WmoGroupID;
        public uint WmoDoodadPlacementID;
        public sbyte[] PartyType = new sbyte[2];
        public CTROptions ChromieTime;
    }

    internal struct PartyPlayerInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteBits(Name.GetByteCount(), 6);
            data.WriteBits(VoiceStateID.GetByteCount() + 1, 6);
            data.WriteBit(FromSocialQueue);
            data.WriteBit(VoiceChatSilenced);
            data.WritePackedGuid(GUID);
            data.WriteUInt8((byte)Status);
            data.WriteUInt8(Subgroup);
            data.WriteUInt8(Flags);
            data.WriteUInt8(RolesAssigned);
            data.WriteUInt8(Class);
            data.WriteString(Name);
            if (!VoiceStateID.IsEmpty())
                data.WriteString(VoiceStateID);
        }

        public ObjectGuid GUID;
        public string Name;
        public string VoiceStateID;   // same as bgs.protocol.club.v1.MemberVoiceState.id
        public byte Class;
        public GroupMemberOnlineStatus Status;
        public byte Subgroup;
        public byte Flags;
        public byte RolesAssigned;
        public bool FromSocialQueue;
        public bool VoiceChatSilenced;
    }

    internal struct PartyLFGInfo
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

        public byte MyFlags;
        public uint Slot;
        public byte BootCount;
        public uint MyRandomSlot;
        public bool Aborted;
        public byte MyPartialClear;
        public float MyGearDiff;
        public byte MyStrangerCount;
        public byte MyKickVoteCount;
        public bool MyFirstReward;
    }

    internal struct PartyLootSettings
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt8(Method);
            data.WritePackedGuid(LootMaster);
            data.WriteUInt8(Threshold);
        }

        public byte Method;
        public ObjectGuid LootMaster;
        public byte Threshold;
    }

    internal struct PartyDifficultySettings
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(DungeonDifficultyID);
            data.WriteUInt32(RaidDifficultyID);
            data.WriteUInt32(LegacyRaidDifficultyID);
        }

        public uint DungeonDifficultyID;
        public uint RaidDifficultyID;
        public uint LegacyRaidDifficultyID;
    }
}