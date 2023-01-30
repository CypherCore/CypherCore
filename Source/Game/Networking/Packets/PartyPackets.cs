// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Spells;

namespace Game.Networking.Packets
{
    internal class PartyCommandResult : ServerPacket
    {
        public byte Command;

        public string Name;
        public byte Result;
        public uint ResultData;
        public ObjectGuid ResultGUID;

        public PartyCommandResult() : base(ServerOpcodes.PartyCommandResult)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBits(Name.GetByteCount(), 9);
            _worldPacket.WriteBits(Command, 4);
            _worldPacket.WriteBits(Result, 6);

            _worldPacket.WriteUInt32(ResultData);
            _worldPacket.WritePackedGuid(ResultGUID);
            _worldPacket.WriteString(Name);
        }
    }

    internal class PartyInviteClient : ClientPacket
    {
        public byte PartyIndex;
        public uint ProposedRoles;
        public ObjectGuid TargetGUID;
        public string TargetName;
        public string TargetRealm;

        public PartyInviteClient(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();

            uint targetNameLen = _worldPacket.ReadBits<uint>(9);
            uint targetRealmLen = _worldPacket.ReadBits<uint>(9);

            ProposedRoles = _worldPacket.ReadUInt32();
            TargetGUID = _worldPacket.ReadPackedGuid();

            TargetName = _worldPacket.ReadString(targetNameLen);
            TargetRealm = _worldPacket.ReadString(targetRealmLen);
        }
    }

    internal class PartyInvite : ServerPacket
    {
        public bool AllowMultipleRoles;

        public bool CanAccept;
        public ObjectGuid InviterBNetAccountId;
        public ObjectGuid InviterGUID;
        public string InviterName;

        // Inviter
        public VirtualRealmInfo InviterRealm;

        // Realm
        public bool IsXRealm;
        public int LfgCompletedMask;
        public List<int> LfgSlots = new();

        public bool MightCRZYou;
        public bool MustBeBNetFriend;

        // Lfg
        public uint ProposedRoles;
        public bool QuestSessionActive;
        public ushort Unk1;

        public PartyInvite() : base(ServerOpcodes.PartyInvite)
        {
        }

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

            foreach (int LfgSlot in LfgSlots)
                _worldPacket.WriteInt32(LfgSlot);
        }
    }

    internal class PartyInviteResponse : ClientPacket
    {
        public bool Accept;

        public byte PartyIndex;
        public uint? RolesDesired;

        public PartyInviteResponse(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();

            Accept = _worldPacket.HasBit();

            bool hasRolesDesired = _worldPacket.HasBit();

            if (hasRolesDesired)
                RolesDesired = _worldPacket.ReadUInt32();
        }
    }

    internal class PartyUninvite : ClientPacket
    {
        public byte PartyIndex;
        public string Reason;
        public ObjectGuid TargetGUID;

        public PartyUninvite(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();

            byte reasonLen = _worldPacket.ReadBits<byte>(8);
            Reason = _worldPacket.ReadString(reasonLen);
        }
    }

    internal class GroupDecline : ServerPacket
    {
        public string Name;

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
    }

    internal class GroupUninvite : ServerPacket
    {
        public GroupUninvite() : base(ServerOpcodes.GroupUninvite)
        {
        }

        public override void Write()
        {
        }
    }

    internal class RequestPartyMemberStats : ClientPacket
    {
        public byte PartyIndex;
        public ObjectGuid TargetGUID;

        public RequestPartyMemberStats(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();
        }
    }

    internal class PartyMemberFullState : ServerPacket
    {
        public bool ForEnemy;
        public ObjectGuid MemberGuid;
        public PartyMemberStats MemberStats = new();

        public PartyMemberFullState() : base(ServerOpcodes.PartyMemberFullState)
        {
        }

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
            MemberStats.PartyType[0] = (sbyte)(player.PlayerData.PartyType & 0xF);
            MemberStats.PartyType[1] = (sbyte)(player.PlayerData.PartyType >> 4);
            MemberStats.WmoGroupID = 0;
            MemberStats.WmoDoodadPlacementID = 0;

            // Vehicle
            Vehicle vehicle = player.GetVehicle();

            if (vehicle != null)
            {
                VehicleSeatRecord vehicleSeat = vehicle.GetSeatForPassenger(player);

                if (vehicleSeat != null)
                    MemberStats.VehicleSeat = (int)vehicleSeat.Id;
            }

            // Auras
            foreach (AuraApplication aurApp in player.GetVisibleAuras())
            {
                PartyMemberAuraStates aura = new();
                aura.SpellID = (int)aurApp.GetBase().GetId();
                aura.ActiveFlags = aurApp.GetEffectMask();
                aura.Flags = (byte)aurApp.GetFlags();

                if (aurApp.GetFlags().HasAnyFlag(AuraFlags.Scalable))
                    foreach (AuraEffect aurEff in aurApp.GetBase().GetAuraEffects())
                    {
                        if (aurEff == null)
                            continue;

                        if (aurApp.HasEffect(aurEff.GetEffIndex()))
                            aura.Points.Add((float)aurEff.GetAmount());
                    }

                MemberStats.Auras.Add(aura);
            }

            // Phases
            PhasingHandler.FillPartyMemberPhase(MemberStats.Phases, player.GetPhaseShift());

            // Pet
            if (player.GetPet())
            {
                Pet pet = player.GetPet();

                MemberStats.PetStats = new PartyMemberPetStats();

                MemberStats.PetStats.GUID = pet.GetGUID();
                MemberStats.PetStats.Name = pet.GetName();
                MemberStats.PetStats.ModelId = (short)pet.GetDisplayId();

                MemberStats.PetStats.CurrentHealth = (int)pet.GetHealth();
                MemberStats.PetStats.MaxHealth = (int)pet.GetMaxHealth();

                foreach (AuraApplication aurApp in pet.GetVisibleAuras())
                {
                    PartyMemberAuraStates aura = new();

                    aura.SpellID = (int)aurApp.GetBase().GetId();
                    aura.ActiveFlags = aurApp.GetEffectMask();
                    aura.Flags = (byte)aurApp.GetFlags();

                    if (aurApp.GetFlags().HasAnyFlag(AuraFlags.Scalable))
                        foreach (AuraEffect aurEff in aurApp.GetBase().GetAuraEffects())
                        {
                            if (aurEff == null)
                                continue;

                            if (aurApp.HasEffect(aurEff.GetEffIndex()))
                                aura.Points.Add((float)aurEff.GetAmount());
                        }

                    MemberStats.PetStats.Auras.Add(aura);
                }
            }
        }
    }

    internal class SetPartyLeader : ClientPacket
    {
        public sbyte PartyIndex;
        public ObjectGuid TargetGUID;

        public SetPartyLeader(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();
        }
    }

    internal class SetRole : ClientPacket
    {
        public sbyte PartyIndex;
        public int Role;
        public ObjectGuid TargetGUID;

        public SetRole(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            TargetGUID = _worldPacket.ReadPackedGuid();
            Role = _worldPacket.ReadInt32();
        }
    }

    internal class RoleChangedInform : ServerPacket
    {
        public ObjectGuid ChangedUnit;
        public ObjectGuid From;
        public int NewRole;
        public int OldRole;

        public sbyte PartyIndex;

        public RoleChangedInform() : base(ServerOpcodes.RoleChangedInform)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WritePackedGuid(From);
            _worldPacket.WritePackedGuid(ChangedUnit);
            _worldPacket.WriteInt32(OldRole);
            _worldPacket.WriteInt32(NewRole);
        }
    }

    internal class LeaveGroup : ClientPacket
    {
        public sbyte PartyIndex;

        public LeaveGroup(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }
    }

    internal class GroupDestroyed : ServerPacket
    {
        public GroupDestroyed() : base(ServerOpcodes.GroupDestroyed)
        {
        }

        public override void Write()
        {
        }
    }

    internal class SetLootMethod : ClientPacket
    {
        public ObjectGuid LootMasterGUID;
        public LootMethod LootMethod;
        public ItemQuality LootThreshold;

        public sbyte PartyIndex;

        public SetLootMethod(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            LootMethod = (LootMethod)_worldPacket.ReadUInt8();
            LootMasterGUID = _worldPacket.ReadPackedGuid();
            LootThreshold = (ItemQuality)_worldPacket.ReadUInt32();
        }
    }

    internal class MinimapPingClient : ClientPacket
    {
        public sbyte PartyIndex;
        public float PositionX;
        public float PositionY;

        public MinimapPingClient(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PositionX = _worldPacket.ReadFloat();
            PositionY = _worldPacket.ReadFloat();
            PartyIndex = _worldPacket.ReadInt8();
        }
    }

    internal class MinimapPing : ServerPacket
    {
        public float PositionX;
        public float PositionY;

        public ObjectGuid Sender;

        public MinimapPing() : base(ServerOpcodes.MinimapPing)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Sender);
            _worldPacket.WriteFloat(PositionX);
            _worldPacket.WriteFloat(PositionY);
        }
    }

    internal class UpdateRaidTarget : ClientPacket
    {
        public sbyte PartyIndex;
        public sbyte Symbol;
        public ObjectGuid Target;

        public UpdateRaidTarget(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            Target = _worldPacket.ReadPackedGuid();
            Symbol = _worldPacket.ReadInt8();
        }
    }

    internal class SendRaidTargetUpdateSingle : ServerPacket
    {
        public ObjectGuid ChangedBy;

        public sbyte PartyIndex;
        public sbyte Symbol;
        public ObjectGuid Target;

        public SendRaidTargetUpdateSingle() : base(ServerOpcodes.SendRaidTargetUpdateSingle)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WriteInt8(Symbol);
            _worldPacket.WritePackedGuid(Target);
            _worldPacket.WritePackedGuid(ChangedBy);
        }
    }

    internal class SendRaidTargetUpdateAll : ServerPacket
    {
        public sbyte PartyIndex;
        public Dictionary<byte, ObjectGuid> TargetIcons = new();

        public SendRaidTargetUpdateAll() : base(ServerOpcodes.SendRaidTargetUpdateAll)
        {
        }

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
    }

    internal class ConvertRaid : ClientPacket
    {
        public bool Raid;

        public ConvertRaid(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Raid = _worldPacket.HasBit();
        }
    }

    internal class RequestPartyJoinUpdates : ClientPacket
    {
        public sbyte PartyIndex;

        public RequestPartyJoinUpdates(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }
    }

    internal class SetAssistantLeader : ClientPacket
    {
        public bool Apply;
        public byte PartyIndex;

        public ObjectGuid Target;

        public SetAssistantLeader(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            Target = _worldPacket.ReadPackedGuid();
            Apply = _worldPacket.HasBit();
        }
    }

    internal class SetPartyAssignment : ClientPacket
    {
        public byte Assignment;
        public byte PartyIndex;
        public bool Set;
        public ObjectGuid Target;

        public SetPartyAssignment(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            Assignment = _worldPacket.ReadUInt8();
            Target = _worldPacket.ReadPackedGuid();
            Set = _worldPacket.HasBit();
        }
    }

    internal class DoReadyCheck : ClientPacket
    {
        public sbyte PartyIndex;

        public DoReadyCheck(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }
    }

    internal class ReadyCheckStarted : ServerPacket
    {
        public uint Duration;
        public ObjectGuid InitiatorGUID;
        public ObjectGuid PartyGUID;

        public sbyte PartyIndex;

        public ReadyCheckStarted() : base(ServerOpcodes.ReadyCheckStarted)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WritePackedGuid(PartyGUID);
            _worldPacket.WritePackedGuid(InitiatorGUID);
            _worldPacket.WriteUInt32(Duration);
        }
    }

    internal class ReadyCheckResponseClient : ClientPacket
    {
        public bool IsReady;

        public byte PartyIndex;

        public ReadyCheckResponseClient(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            IsReady = _worldPacket.HasBit();
        }
    }

    internal class ReadyCheckResponse : ServerPacket
    {
        public bool IsReady;

        public ObjectGuid PartyGUID;
        public ObjectGuid Player;

        public ReadyCheckResponse() : base(ServerOpcodes.ReadyCheckResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PartyGUID);
            _worldPacket.WritePackedGuid(Player);

            _worldPacket.WriteBit(IsReady);
            _worldPacket.FlushBits();
        }
    }

    internal class ReadyCheckCompleted : ServerPacket
    {
        public ObjectGuid PartyGUID;

        public sbyte PartyIndex;

        public ReadyCheckCompleted() : base(ServerOpcodes.ReadyCheckCompleted)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WritePackedGuid(PartyGUID);
        }
    }

    internal class RequestRaidInfo : ClientPacket
    {
        public RequestRaidInfo(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    internal class OptOutOfLoot : ClientPacket
    {
        public bool PassOnLoot;

        public OptOutOfLoot(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PassOnLoot = _worldPacket.HasBit();
        }
    }

    internal class InitiateRolePoll : ClientPacket
    {
        public sbyte PartyIndex;

        public InitiateRolePoll(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
        }
    }

    internal class RolePollInform : ServerPacket
    {
        public ObjectGuid From;

        public sbyte PartyIndex;

        public RolePollInform() : base(ServerOpcodes.RolePollInform)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WritePackedGuid(From);
        }
    }

    internal class GroupNewLeader : ServerPacket
    {
        public string Name;

        public sbyte PartyIndex;

        public GroupNewLeader() : base(ServerOpcodes.GroupNewLeader)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt8(PartyIndex);
            _worldPacket.WriteBits(Name.GetByteCount(), 9);
            _worldPacket.WriteString(Name);
        }
    }

    internal class PartyUpdate : ServerPacket
    {
        public PartyDifficultySettings? DifficultySettings;
        public byte LeaderFactionGroup;
        public ObjectGuid LeaderGUID;

        public PartyLFGInfo? LfgInfos;
        public PartyLootSettings? LootSettings;

        public int MyIndex;

        public GroupFlags PartyFlags;

        public ObjectGuid PartyGUID;
        public byte PartyIndex;
        public GroupType PartyType;

        public List<PartyPlayerInfo> PlayerList = new();
        public int SequenceNum;

        public PartyUpdate() : base(ServerOpcodes.PartyUpdate)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt16((ushort)PartyFlags);
            _worldPacket.WriteUInt8(PartyIndex);
            _worldPacket.WriteUInt8((byte)PartyType);
            _worldPacket.WriteInt32(MyIndex);
            _worldPacket.WritePackedGuid(PartyGUID);
            _worldPacket.WriteInt32(SequenceNum);
            _worldPacket.WritePackedGuid(LeaderGUID);
            _worldPacket.WriteUInt8(LeaderFactionGroup);
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
    }

    internal class SetEveryoneIsAssistant : ClientPacket
    {
        public bool EveryoneIsAssistant;

        public byte PartyIndex;

        public SetEveryoneIsAssistant(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadUInt8();
            EveryoneIsAssistant = _worldPacket.HasBit();
        }
    }

    internal class ChangeSubGroup : ClientPacket
    {
        public byte NewSubGroup;
        public sbyte PartyIndex;

        public ObjectGuid TargetGUID;

        public ChangeSubGroup(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            TargetGUID = _worldPacket.ReadPackedGuid();
            PartyIndex = _worldPacket.ReadInt8();
            NewSubGroup = _worldPacket.ReadUInt8();
        }
    }

    internal class SwapSubGroups : ClientPacket
    {
        public ObjectGuid FirstTarget;
        public sbyte PartyIndex;
        public ObjectGuid SecondTarget;

        public SwapSubGroups(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PartyIndex = _worldPacket.ReadInt8();
            FirstTarget = _worldPacket.ReadPackedGuid();
            SecondTarget = _worldPacket.ReadPackedGuid();
        }
    }

    internal class ClearRaidMarker : ClientPacket
    {
        public byte MarkerId;

        public ClearRaidMarker(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            MarkerId = _worldPacket.ReadUInt8();
        }
    }

    internal class RaidMarkersChanged : ServerPacket
    {
        public uint ActiveMarkers;

        public sbyte PartyIndex;

        public List<RaidMarker> RaidMarkers = new();

        public RaidMarkersChanged() : base(ServerOpcodes.RaidMarkersChanged)
        {
        }

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
    }

    internal class PartyKillLog : ServerPacket
    {
        public ObjectGuid Player;
        public ObjectGuid Victim;

        public PartyKillLog() : base(ServerOpcodes.PartyKillLog)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Player);
            _worldPacket.WritePackedGuid(Victim);
        }
    }

    internal class BroadcastSummonCast : ServerPacket
    {
        public ObjectGuid Target;

        public BroadcastSummonCast() : base(ServerOpcodes.BroadcastSummonCast)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Target);
        }
    }

    internal class BroadcastSummonResponse : ServerPacket
    {
        public bool Accepted;
        public ObjectGuid Target;

        public BroadcastSummonResponse() : base(ServerOpcodes.BroadcastSummonResponse)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Target);
            _worldPacket.WriteBit(Accepted);
            _worldPacket.FlushBits();
        }
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
        public List<PartyMemberPhase> List = new();
        public ObjectGuid PersonalGUID;

        public int PhaseShiftFlags;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(PhaseShiftFlags);
            data.WriteInt32(List.Count);
            data.WritePackedGuid(PersonalGUID);

            foreach (PartyMemberPhase phase in List)
                phase.Write(data);
        }
    }

    internal class PartyMemberAuraStates
    {
        public uint ActiveFlags;
        public ushort Flags;
        public List<float> Points = new();
        public int SpellID;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(SpellID);
            data.WriteUInt16(Flags);
            data.WriteUInt32(ActiveFlags);
            data.WriteInt32(Points.Count);

            foreach (float points in Points)
                data.WriteFloat(points);
        }
    }

    internal class PartyMemberPetStats
    {
        public List<PartyMemberAuraStates> Auras = new();

        public int CurrentHealth;

        public ObjectGuid GUID;
        public int MaxHealth;
        public short ModelId;
        public string Name;

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
        public List<PartyMemberAuraStates> Auras = new();
        public CTROptions ChromieTime;

        public int CurrentHealth;
        public ushort CurrentPower;
        public DungeonScoreSummary DungeonScore = new();

        public ushort Level;
        public int MaxHealth;
        public ushort MaxPower;
        public sbyte[] PartyType = new sbyte[2];
        public PartyMemberPetStats PetStats;

        public PartyMemberPhaseStates Phases = new();
        public short PositionX;
        public short PositionY;
        public short PositionZ;

        public ushort PowerDisplayID;

        public byte PowerType;
        public ushort SpecID;
        public GroupMemberOnlineStatus Status;

        public int VehicleSeat;
        public uint WmoDoodadPlacementID;
        public ushort WmoGroupID;

        public ushort ZoneID;

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

            foreach (PartyMemberAuraStates aura in Auras)
                aura.Write(data);

            data.WriteBit(PetStats != null);
            data.FlushBits();

            DungeonScore.Write(data);

            PetStats?.Write(data);
        }
    }

    internal struct PartyPlayerInfo
    {
        public void Write(WorldPacket data)
        {
            data.WriteBits(Name.GetByteCount(), 6);
            data.WriteBits(VoiceStateID.GetByteCount() + 1, 6);
            data.WriteBit(Connected);
            data.WriteBit(VoiceChatSilenced);
            data.WriteBit(FromSocialQueue);
            data.WritePackedGuid(GUID);
            data.WriteUInt8(Subgroup);
            data.WriteUInt8(Flags);
            data.WriteUInt8(RolesAssigned);
            data.WriteUInt8(Class);
            data.WriteUInt8(FactionGroup);
            data.WriteString(Name);

            if (!VoiceStateID.IsEmpty())
                data.WriteString(VoiceStateID);
        }

        public ObjectGuid GUID;
        public string Name;
        public string VoiceStateID; // same as bgs.protocol.club.v1.MemberVoiceState.Id
        public byte Class;
        public byte Subgroup;
        public byte Flags;
        public byte RolesAssigned;
        public byte FactionGroup;
        public bool FromSocialQueue;
        public bool VoiceChatSilenced;
        public bool Connected;
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