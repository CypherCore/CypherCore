﻿/*
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
using Framework.Database;
using Framework.Dynamic;
using Framework.GameMath;
using Framework.IO;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    public class EnumCharacters : ClientPacket
    {
        public EnumCharacters(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class EnumCharactersResult : ServerPacket
    {
        public EnumCharactersResult() : base(ServerOpcodes.EnumCharactersResult) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Success);
            _worldPacket.WriteBit(IsDeletedCharacters);
            _worldPacket.WriteBit(IsNewPlayerRestrictionSkipped);
            _worldPacket.WriteBit(IsNewPlayerRestricted);
            _worldPacket.WriteBit(IsNewPlayer);
            _worldPacket.WriteBit(DisabledClassesMask.HasValue);
            _worldPacket.WriteBit(IsAlliedRacesCreationAllowed);
            _worldPacket.WriteInt32(Characters.Count);
            _worldPacket.WriteInt32(MaxCharacterLevel);
            _worldPacket.WriteInt32(RaceUnlockData.Count);
            _worldPacket.WriteInt32(UnlockedConditionalAppearances.Count);

            if (DisabledClassesMask.HasValue)
                _worldPacket.WriteUInt32(DisabledClassesMask.Value);

            foreach (var unlockedConditionalAppearance in UnlockedConditionalAppearances)
                unlockedConditionalAppearance.Write(_worldPacket);

            foreach (var charInfo in Characters)
                charInfo.Write(_worldPacket);

            foreach (var raceUnlock in RaceUnlockData)
                raceUnlock.Write(_worldPacket);
        }

        public bool Success;
        public bool IsDeletedCharacters; // used for character undelete list
        public bool IsNewPlayerRestrictionSkipped; // allows client to skip new player restrictions
        public bool IsNewPlayerRestricted; // forbids using level boost and class trials
        public bool IsNewPlayer; // forbids hero classes and allied races
        public bool IsAlliedRacesCreationAllowed;

        public int MaxCharacterLevel = 1;
        public Optional<uint> DisabledClassesMask = new Optional<uint>();

        public List<CharacterInfo> Characters = new List<CharacterInfo>(); // all characters on the list
        public List<RaceUnlock> RaceUnlockData = new List<RaceUnlock>(); //
        public List<UnlockedConditionalAppearance> UnlockedConditionalAppearances = new List<UnlockedConditionalAppearance>();

        public class CharacterInfo
        {
            public CharacterInfo(SQLFields fields)
            {
                Guid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(0));
                Name = fields.Read<string>(1);
                RaceId = fields.Read<byte>(2);
                ClassId = (Class)fields.Read<byte>(3);
                SexId = fields.Read<byte>(4);
                ExperienceLevel = fields.Read<byte>(5);
                ZoneId = fields.Read<uint>(6);
                MapId = fields.Read<uint>(7);
                PreloadPos = new Vector3(fields.Read<float>(8), fields.Read<float>(9), fields.Read<float>(10));

                var guildId = fields.Read<ulong>(11);
                if (guildId != 0)
                    GuildGuid = ObjectGuid.Create(HighGuid.Guild, guildId);

                var playerFlags = (PlayerFlags)fields.Read<uint>(12);
                var atLoginFlags = (AtLoginFlags)fields.Read<ushort>(13);

                if (atLoginFlags.HasAnyFlag(AtLoginFlags.Resurrect))
                    playerFlags &= ~PlayerFlags.Ghost;

                if (playerFlags.HasAnyFlag(PlayerFlags.Ghost))
                    Flags |= CharacterFlags.Ghost;

                if (atLoginFlags.HasAnyFlag(AtLoginFlags.Rename))
                    Flags |= CharacterFlags.Rename;

                if (fields.Read<uint>(18) != 0)
                    Flags |= CharacterFlags.LockedByBilling;

                if (WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed) && !string.IsNullOrEmpty(fields.Read<string>(23)))
                    Flags |= CharacterFlags.Declined;

                if (atLoginFlags.HasAnyFlag(AtLoginFlags.Customize))
                    Flags2 = CharacterCustomizeFlags.Customize;
                else if (atLoginFlags.HasAnyFlag(AtLoginFlags.ChangeFaction))
                    Flags2 = CharacterCustomizeFlags.Faction;
                else if (atLoginFlags.HasAnyFlag(AtLoginFlags.ChangeRace))
                    Flags2 = CharacterCustomizeFlags.Race;

                Flags3 = 0;
                Flags4 = 0;
                FirstLogin = atLoginFlags.HasAnyFlag(AtLoginFlags.FirstLogin);

                // show pet at selection character in character list only for non-ghost character
                if (!playerFlags.HasAnyFlag(PlayerFlags.Ghost) && (ClassId == Class.Warlock || ClassId == Class.Hunter || ClassId == Class.Deathknight))
                {
                    CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(fields.Read<uint>(14));
                    if (creatureInfo != null)
                    {
                        PetCreatureDisplayId = fields.Read<uint>(15);
                        PetExperienceLevel = fields.Read<ushort>(16);
                        PetCreatureFamilyId = (uint)creatureInfo.Family;
                    }
                }

                BoostInProgress = false;
                ProfessionIds[0] = 0;
                ProfessionIds[1] = 0;

                var equipment = new StringArguments(fields.Read<string>(17));
                ListPosition = fields.Read<byte>(19);
                LastPlayedTime = fields.Read<uint>(20);

                var spec = Global.DB2Mgr.GetChrSpecializationByIndex(ClassId, fields.Read<byte>(21));
                if (spec != null)
                    SpecID = (ushort)spec.Id;

                LastLoginVersion = fields.Read<uint>(22);

                for (byte slot = 0; slot < InventorySlots.BagEnd; ++slot)
                {
                    VisualItems[slot].InvType = (byte)equipment.NextUInt32();
                    VisualItems[slot].DisplayId = equipment.NextUInt32();
                    VisualItems[slot].DisplayEnchantId = equipment.NextUInt32();
                    VisualItems[slot].Subclass = (byte)equipment.NextUInt32();
                }
            }

            public void Write(WorldPacket data)
            {
                data.WritePackedGuid(Guid);
                data.WriteUInt64(GuildClubMemberID);
                data.WriteUInt8(ListPosition);
                data.WriteUInt8(RaceId);
                data.WriteUInt8((byte)ClassId);
                data.WriteUInt8(SexId);
                data.WriteInt32(Customizations.Count);

                data.WriteUInt8(ExperienceLevel);
                data.WriteUInt32(ZoneId);
                data.WriteUInt32(MapId);
                data.WriteVector3(PreloadPos);
                data.WritePackedGuid(GuildGuid);
                data.WriteUInt32((uint)Flags);
                data.WriteUInt32((uint)Flags2);
                data.WriteUInt32(Flags3);
                data.WriteUInt32(PetCreatureDisplayId);
                data.WriteUInt32(PetExperienceLevel);
                data.WriteUInt32(PetCreatureFamilyId);

                data.WriteUInt32(ProfessionIds[0]);
                data.WriteUInt32(ProfessionIds[1]);

                foreach (var visualItem in VisualItems)
                    visualItem.Write(data);

                data.WriteUInt32(LastPlayedTime);
                data.WriteUInt16(SpecID);
                data.WriteUInt32(Unknown703);
                data.WriteUInt32(LastLoginVersion);
                data.WriteUInt32(Flags4);
                data.WriteInt32(MailSenders.Count);
                data.WriteInt32(MailSenderTypes.Count);
                data.WriteUInt32(OverrideSelectScreenFileDataID);

                foreach (var customization in Customizations)
                {
                    data.WriteUInt32(customization.ChrCustomizationOptionID);
                    data.WriteUInt32(customization.ChrCustomizationChoiceID);
                }

                foreach (var mailSenderType in MailSenderTypes)
                    data.WriteUInt32(mailSenderType);

                data.WriteBits(Name.GetByteCount(), 6);
                data.WriteBit(FirstLogin);
                data.WriteBit(BoostInProgress);
                data.WriteBits(unkWod61x, 5);

                foreach (var str in MailSenders)
                    data.WriteBits(str.GetByteCount() + 1, 6);

                data.FlushBits();

                foreach (var str in MailSenders)
                    if (!str.IsEmpty())
                        data.WriteCString(str);

                data.WriteString(Name);
            }

            public ObjectGuid Guid;
            public ulong GuildClubMemberID; // same as bgs.protocol.club.v1.MemberId.unique_id, guessed basing on SMSG_QUERY_PLAYER_NAME_RESPONSE (that one is known)
            public string Name;
            public byte ListPosition; // Order of the characters in list
            public byte RaceId;
            public Class ClassId;
            public byte SexId;
            public Array<ChrCustomizationChoice> Customizations = new Array<ChrCustomizationChoice>(50);
            public byte ExperienceLevel;
            public uint ZoneId;
            public uint MapId;
            public Vector3 PreloadPos;
            public ObjectGuid GuildGuid;
            public CharacterFlags Flags; // Character flag @see enum CharacterFlags
            public CharacterCustomizeFlags Flags2; // Character customization flags @see enum CharacterCustomizeFlags
            public uint Flags3; // Character flags 3 @todo research
            public uint Flags4;
            public bool FirstLogin;
            public byte unkWod61x;
            public uint LastPlayedTime;
            public ushort SpecID;
            public uint Unknown703;
            public uint LastLoginVersion;
            public uint OverrideSelectScreenFileDataID;
            public uint PetCreatureDisplayId;
            public uint PetExperienceLevel;
            public uint PetCreatureFamilyId;
            public bool BoostInProgress; // @todo
            public uint[] ProfessionIds = new uint[2];      // @todo
            public VisualItemInfo[] VisualItems = new VisualItemInfo[InventorySlots.BagEnd];
            public List<string> MailSenders = new List<string>();
            public List<uint> MailSenderTypes = new List<uint>();

            public struct VisualItemInfo
            {
                public void Write(WorldPacket data)
                {
                    data.WriteUInt32(DisplayId);
                    data.WriteUInt32(DisplayEnchantId);
                    data.WriteInt32(ItemModifiedAppearanceID);
                    data.WriteUInt8(InvType);
                    data.WriteUInt8(Subclass);
                }

                public uint DisplayId;
                public uint DisplayEnchantId;
                public int ItemModifiedAppearanceID; // also -1 is some special value
                public byte InvType;
                public byte Subclass;
            }

            public struct PetInfo
            {
                public uint CreatureDisplayId; // PetCreatureDisplayID
                public uint Level; // PetExperienceLevel
                public uint CreatureFamily; // PetCreatureFamilyID
            }
        }

        public struct RaceUnlock
        {
            public void Write(WorldPacket data)
            {
                data.WriteInt32(RaceID);
                data.WriteBit(HasExpansion);
                data.WriteBit(HasAchievement);
                data.WriteBit(HasHeritageArmor);
                data.FlushBits();
            }

            public int RaceID;
            public bool HasExpansion;
            public bool HasAchievement;
            public bool HasHeritageArmor;
        }

        public struct UnlockedConditionalAppearance
        {
            public void Write(WorldPacket data)
            {
                data.WriteInt32(AchievementID);
                data.WriteInt32(Unused);
            }

            public int AchievementID;
            public int Unused;
        }
    }

    class CheckCharacterNameAvailability : ClientPacket
    {     
        public uint SequenceIndex;
        public string Name;

        public CheckCharacterNameAvailability(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            SequenceIndex = _worldPacket.ReadUInt32();
            Name = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
        }
    }

    class CheckCharacterNameAvailabilityResult : ServerPacket
    {
        public uint SequenceIndex;
        public ResponseCodes Result;

        public CheckCharacterNameAvailabilityResult(uint sequenceIndex, ResponseCodes result) : base(ServerOpcodes.CheckCharacterNameAvailabilityResult)
        {
            SequenceIndex = sequenceIndex;
            Result = result;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SequenceIndex);
            _worldPacket.WriteUInt32((uint)Result);
        }
    }
    
    public class CreateCharacter : ClientPacket
    {
        public CreateCharacter(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CreateInfo = new CharacterCreateInfo();
            var nameLength = _worldPacket.ReadBits<uint>(6);
            var hasTemplateSet = _worldPacket.HasBit();
            CreateInfo.IsTrialBoost = _worldPacket.HasBit();
            CreateInfo.UseNPE = _worldPacket.HasBit();

            CreateInfo.RaceId = (Race)_worldPacket.ReadUInt8();
            CreateInfo.ClassId = (Class)_worldPacket.ReadUInt8();
            CreateInfo.Sex = (Gender)_worldPacket.ReadUInt8();
            var customizationCount = _worldPacket.ReadUInt32();

            CreateInfo.Name = _worldPacket.ReadString(nameLength);
            if (CreateInfo.TemplateSet.HasValue)
                CreateInfo.TemplateSet.Set(_worldPacket.ReadUInt32());

            for (var i = 0; i < customizationCount; ++i)
            {
                CreateInfo.Customizations[i] = new ChrCustomizationChoice()
                {
                    ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
                    ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
                };
            }

            CreateInfo.Customizations.Sort();
        }

        public CharacterCreateInfo CreateInfo;
    }

    public class CreateChar : ServerPacket
    {
        public CreateChar() : base(ServerOpcodes.CreateChar) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Code);
            _worldPacket.WritePackedGuid(Guid);
        }

        public ResponseCodes Code;
        public ObjectGuid Guid;
    }

    public class CharDelete : ClientPacket
    {
        public CharDelete(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid; // Guid of the character to delete
    }

    public class DeleteChar : ServerPacket
    {
        public DeleteChar() : base(ServerOpcodes.DeleteChar) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Code);
        }

        public ResponseCodes Code;
    }

    public class CharacterRenameRequest : ClientPacket
    {
        public CharacterRenameRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RenameInfo = new CharacterRenameInfo();
            RenameInfo.Guid = _worldPacket.ReadPackedGuid();
            RenameInfo.NewName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
        }

        public CharacterRenameInfo RenameInfo;
    }

    public class CharacterRenameResult : ServerPacket
    {
        public CharacterRenameResult() : base(ServerOpcodes.CharacterRenameResult)
        {
            Guid = new Optional<ObjectGuid>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Result);
            _worldPacket.WriteBit(Guid.HasValue);
            _worldPacket.WriteBits(Name.GetByteCount(), 6);
            _worldPacket.FlushBits();

            if (Guid.HasValue)
                _worldPacket.WritePackedGuid(Guid.Value);

            _worldPacket.WriteString(Name);
        }

        public string Name;
        public ResponseCodes Result = 0;
        public Optional<ObjectGuid> Guid;
    }

    public class CharCustomize : ClientPacket
    {
        public CharCustomize(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CustomizeInfo = new CharCustomizeInfo();
            CustomizeInfo.CharGUID = _worldPacket.ReadPackedGuid();
            CustomizeInfo.SexID = (Gender)_worldPacket.ReadUInt8();
            var customizationCount = _worldPacket.ReadUInt32();

            for (var i = 0; i < customizationCount; ++i)
            {
                CustomizeInfo.Customizations[i] = new ChrCustomizationChoice()
                {
                    ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
                    ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
                };
            }

            CustomizeInfo.Customizations.Sort();

            CustomizeInfo.CharName = _worldPacket.ReadString(_worldPacket.ReadBits<uint>(6));
        }

        public CharCustomizeInfo CustomizeInfo;
    }

    // @todo: CharCustomizeResult

    public class CharRaceOrFactionChange : ClientPacket
    {
        public CharRaceOrFactionChange(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RaceOrFactionChangeInfo = new CharRaceOrFactionChangeInfo();

            RaceOrFactionChangeInfo.FactionChange = _worldPacket.HasBit();

            var nameLength = _worldPacket.ReadBits<uint>(6);

            RaceOrFactionChangeInfo.Guid = _worldPacket.ReadPackedGuid();
            RaceOrFactionChangeInfo.SexID = (Gender)_worldPacket.ReadUInt8();
            RaceOrFactionChangeInfo.RaceID = (Race)_worldPacket.ReadUInt8();
            var customizationCount = _worldPacket.ReadUInt32();
            RaceOrFactionChangeInfo.Name = _worldPacket.ReadString(nameLength);

            for (var i = 0; i < customizationCount; ++i)
            {
                RaceOrFactionChangeInfo.Customizations[i] = new ChrCustomizationChoice()
                {
                    ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
                    ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
                };
            }

            RaceOrFactionChangeInfo.Customizations.Sort();
        }

        public CharRaceOrFactionChangeInfo RaceOrFactionChangeInfo;
    }

    public class CharFactionChangeResult : ServerPacket
    {
        public CharFactionChangeResult() : base(ServerOpcodes.CharFactionChangeResult)
        {
            Display = new Optional<CharFactionChangeDisplayInfo>();
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Result);
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteBit(Display.HasValue);
            _worldPacket.FlushBits();

            if (Display.HasValue)
            {
                _worldPacket.WriteBits(Display.Value.Name.GetByteCount(), 6);
                _worldPacket.WriteUInt8(Display.Value.SexID);
                _worldPacket.WriteUInt8(Display.Value.RaceID);
                _worldPacket.WriteInt32(Display.Value.Customizations.Count);
                _worldPacket.WriteString(Display.Value.Name);

                foreach (var customization in Display.Value.Customizations)
                {
                    _worldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
                    _worldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
                }
            }
        }

        public ResponseCodes Result = 0;
        public ObjectGuid Guid;
        public Optional<CharFactionChangeDisplayInfo> Display;

        public class CharFactionChangeDisplayInfo
        {
            public string Name;
            public byte SexID;
            public byte RaceID;
            public Array<ChrCustomizationChoice> Customizations = new Array<ChrCustomizationChoice>(50);
        }
    }

    public class GenerateRandomCharacterName : ClientPacket
    {
        public GenerateRandomCharacterName(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Race = _worldPacket.ReadUInt8();
            Sex = _worldPacket.ReadUInt8();
        }

        public byte Sex;
        public byte Race;
    }

    public class GenerateRandomCharacterNameResult : ServerPacket
    {
        public GenerateRandomCharacterNameResult() : base(ServerOpcodes.GenerateRandomCharacterNameResult) { }

        public override void Write()
        {
            _worldPacket.WriteBit(Success);
            _worldPacket.WriteBits(Name.GetByteCount(), 6);

            _worldPacket.WriteString(Name);
        }

        public string Name;
        public bool Success;
    }

    public class ReorderCharacters : ClientPacket
    {
        public ReorderCharacters(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            var count = _worldPacket.ReadBits<uint>(9);
            for (var i = 0; i < count && i < WorldConfig.GetIntValue(WorldCfg.CharactersPerRealm); ++i)
            {
                ReorderInfo reorderInfo;
                reorderInfo.PlayerGUID = _worldPacket.ReadPackedGuid();
                reorderInfo.NewPosition = _worldPacket.ReadUInt8();
                Entries[i] = reorderInfo;
            }
        }

        public ReorderInfo[] Entries = new ReorderInfo[200];

        public struct ReorderInfo
        {
            public ObjectGuid PlayerGUID;
            public byte NewPosition;
        }
    }

    public class UndeleteCharacter : ClientPacket
    {
        public UndeleteCharacter(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            UndeleteInfo = new CharacterUndeleteInfo();
            _worldPacket.WriteInt32(UndeleteInfo.ClientToken);
            _worldPacket.WritePackedGuid(UndeleteInfo.CharacterGuid);
        }

        public CharacterUndeleteInfo UndeleteInfo;
    }

    public class UndeleteCharacterResponse : ServerPacket
    {
        public UndeleteCharacterResponse() : base(ServerOpcodes.UndeleteCharacterResponse) { }

        public override void Write()
        {
            Cypher.Assert(UndeleteInfo != null);
            _worldPacket.WriteInt32(UndeleteInfo.ClientToken);
            _worldPacket.WriteUInt32((uint)Result);
            _worldPacket.WritePackedGuid(UndeleteInfo.CharacterGuid);
        }

        public CharacterUndeleteInfo UndeleteInfo;
        public CharacterUndeleteResult Result;
    }

    public class GetUndeleteCharacterCooldownStatus : ClientPacket
    {
        public GetUndeleteCharacterCooldownStatus(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class UndeleteCooldownStatusResponse : ServerPacket
    {
        public UndeleteCooldownStatusResponse() : base(ServerOpcodes.UndeleteCooldownStatusResponse) { }

        public override void Write()
        {
            _worldPacket.WriteBit(OnCooldown);
            _worldPacket.WriteUInt32(MaxCooldown);
            _worldPacket.WriteUInt32(CurrentCooldown);
        }

        public bool OnCooldown; //
        public uint MaxCooldown; // Max. cooldown until next free character restoration. Displayed in undelete confirm message. (in sec)
        public uint CurrentCooldown; // Current cooldown until next free character restoration. (in sec)
    }

    public class PlayerLogin : ClientPacket
    {
        public PlayerLogin(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
            FarClip = _worldPacket.ReadFloat();
        }

        public ObjectGuid Guid;      // Guid of the player that is logging in
        public float FarClip; // Visibility distance (for terrain)
    }

    public class LoginVerifyWorld : ServerPacket
    {
        public LoginVerifyWorld() : base(ServerOpcodes.LoginVerifyWorld, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(MapID);
            _worldPacket.WriteFloat(Pos.GetPositionX());
            _worldPacket.WriteFloat(Pos.GetPositionY());
            _worldPacket.WriteFloat(Pos.GetPositionZ());
            _worldPacket.WriteFloat(Pos.GetOrientation());
            _worldPacket.WriteUInt32(Reason);
        }

        public int MapID = -1;
        public Position Pos;
        public uint Reason = 0;
    }

    public class CharacterLoginFailed : ServerPacket
    {
        public CharacterLoginFailed(LoginFailureReason code) : base(ServerOpcodes.CharacterLoginFailed)
        {
            Code = code;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Code);
        }

        LoginFailureReason Code;
    }

    public class LogoutRequest : ClientPacket
    {
        public LogoutRequest(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            IdleLogout = _worldPacket.HasBit();
        }

        public bool IdleLogout;
    }

    public class LogoutResponse : ServerPacket
    {
        public LogoutResponse() : base(ServerOpcodes.LogoutResponse, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(LogoutResult);
            _worldPacket.WriteBit(Instant);
            _worldPacket.FlushBits();
        }

        public int LogoutResult;
        public bool Instant = false;
    }

    public class LogoutComplete : ServerPacket
    {
        public LogoutComplete() : base(ServerOpcodes.LogoutComplete) { }

        public override void Write() { }
    }

    public class LogoutCancel : ClientPacket
    {
        public LogoutCancel(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    public class LogoutCancelAck : ServerPacket
    {
        public LogoutCancelAck() : base(ServerOpcodes.LogoutCancelAck, ConnectionType.Instance) { }

        public override void Write() { }
    }

    public class LoadingScreenNotify : ClientPacket
    {
        public LoadingScreenNotify(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            MapID = _worldPacket.ReadInt32();
            Showing = _worldPacket.HasBit();
        }

        public int MapID = -1;
        public bool Showing;
    }

    public class InitialSetup : ServerPacket
    {
        public InitialSetup() : base(ServerOpcodes.InitialSetup, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(ServerExpansionLevel);
            _worldPacket.WriteUInt8(ServerExpansionTier);
        }

        public byte ServerExpansionTier;
        public byte ServerExpansionLevel;
    }

    public class SetActionBarToggles : ClientPacket
    {
        public SetActionBarToggles(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Mask = _worldPacket.ReadUInt8();
        }

        public byte Mask;
    }

    public class RequestPlayedTime : ClientPacket
    {
        public RequestPlayedTime(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TriggerScriptEvent = _worldPacket.HasBit();
        }

        public bool TriggerScriptEvent;
    }

    public class PlayedTime : ServerPacket
    {
        public PlayedTime() : base(ServerOpcodes.PlayedTime, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(TotalTime);
            _worldPacket.WriteUInt32(LevelTime);
            _worldPacket.WriteBit(TriggerEvent);
            _worldPacket.FlushBits();
        }

        public uint TotalTime;
        public uint LevelTime;
        public bool TriggerEvent;
    }

    public class SetTitle : ClientPacket
    {
        public int TitleID;

        public SetTitle(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TitleID = _worldPacket.ReadInt32();
        }
    }

    public class AlterApperance : ClientPacket
    {
        public AlterApperance(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            var customizationCount = _worldPacket.ReadUInt32();
            NewSex = _worldPacket.ReadUInt8();

            for (var i = 0; i < customizationCount; ++i)
            {
                Customizations[i] = new ChrCustomizationChoice()
                {
                    ChrCustomizationOptionID = _worldPacket.ReadUInt32(),
                    ChrCustomizationChoiceID = _worldPacket.ReadUInt32()
                };
            }

            Customizations.Sort();
        }

        public byte NewSex;
        public Array<ChrCustomizationChoice> Customizations = new Array<ChrCustomizationChoice>(50);
    }

    public class BarberShopResult : ServerPacket
    {
        public BarberShopResult(ResultEnum result) : base(ServerOpcodes.BarberShopResult)
        {
            Result = result;
        }

        public override void Write()
        {
            _worldPacket.WriteInt32((int)Result);
        }

        public ResultEnum Result;

        public enum ResultEnum
        {
            Success = 0,
            NoMoney = 1,
            NotOnChair = 2,
            NoMoney2 = 3
        }
    }

    class LogXPGain : ServerPacket
    {
        public LogXPGain() : base(ServerOpcodes.LogXpGain) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Victim);
            _worldPacket.WriteInt32(Original);
            _worldPacket.WriteUInt8((byte)Reason);
            _worldPacket.WriteInt32(Amount);
            _worldPacket.WriteFloat(GroupBonus);
            _worldPacket.WriteUInt8(ReferAFriendBonusType);
        }

        public ObjectGuid Victim;
        public int Original;
        public PlayerLogXPReason Reason;
        public int Amount;
        public float GroupBonus;
        public byte ReferAFriendBonusType; // 1 - 300% of normal XP; 2 - 150% of normal XP
    }

    class TitleEarned : ServerPacket
    {
        public TitleEarned(ServerOpcodes opcode) : base(opcode) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Index);
        }

        public uint Index;
    }

    class SetFactionAtWar : ClientPacket
    {
        public SetFactionAtWar(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            FactionIndex = _worldPacket.ReadUInt8();
        }

        public byte FactionIndex;
    }

    class SetFactionNotAtWar : ClientPacket
    {
        public SetFactionNotAtWar(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            FactionIndex = _worldPacket.ReadUInt8();
        }

        public byte FactionIndex;
    }

    class SetFactionInactive : ClientPacket
    {
        public SetFactionInactive(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Index = _worldPacket.ReadUInt32();
            State = _worldPacket.HasBit();
        }

        public uint Index;
        public bool State;
    }

    class SetWatchedFaction : ClientPacket
    {
        public SetWatchedFaction(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            FactionIndex = _worldPacket.ReadUInt32();
        }

        public uint FactionIndex;
    }

    class SetFactionVisible : ServerPacket
    {
        public SetFactionVisible(bool visible) : base(visible ? ServerOpcodes.SetFactionVisible : ServerOpcodes.SetFactionNotVisible, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(FactionIndex);
        }

        public uint FactionIndex;
    }

    class CharCustomizeSuccess : ServerPacket
    {
        public CharCustomizeSuccess(CharCustomizeInfo customizeInfo) : base(ServerOpcodes.CharCustomizeSuccess)
        {
            CharGUID = customizeInfo.CharGUID;
            SexID = (byte)customizeInfo.SexID;
            CharName = customizeInfo.CharName;
            Customizations = customizeInfo.Customizations;
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CharGUID);
            _worldPacket.WriteUInt8(SexID);
            _worldPacket.WriteInt32(Customizations.Count);
            foreach (var customization in Customizations)
            {
                _worldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
                _worldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
            }

            _worldPacket.WriteBits(CharName.GetByteCount(), 6);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(CharName);
        }

        ObjectGuid CharGUID;
        string CharName = "";
        byte SexID;
        Array<ChrCustomizationChoice> Customizations = new Array<ChrCustomizationChoice>(50);
    }

    class CharCustomizeFailure : ServerPacket
    {
        public CharCustomizeFailure() : base(ServerOpcodes.CharCustomizeFailure) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Result);
            _worldPacket.WritePackedGuid(CharGUID);
        }

        public byte Result;
        public ObjectGuid CharGUID;
    }

    class SetPlayerDeclinedNames : ClientPacket
    {
        public SetPlayerDeclinedNames(WorldPacket packet) : base(packet)
        {
            DeclinedNames = new DeclinedName();
        }

        public override void Read()
        {
            Player = _worldPacket.ReadPackedGuid();

            var stringLengths = new byte[SharedConst.MaxDeclinedNameCases];

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                stringLengths[i] = _worldPacket.ReadBits<byte>(7);

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                DeclinedNames.name[i] = _worldPacket.ReadString(stringLengths[i]);
        }

        public ObjectGuid Player;
        public DeclinedName DeclinedNames;
    }

    class SetPlayerDeclinedNamesResult : ServerPacket
    {
        public SetPlayerDeclinedNamesResult() : base(ServerOpcodes.SetPlayerDeclinedNamesResult) { }

        public override void Write()
        {
            _worldPacket.WriteInt32((int)ResultCode);
            _worldPacket.WritePackedGuid(Player);
        }

        public ObjectGuid Player;
        public DeclinedNameResult ResultCode;
    }

    //Structs
    public class CharacterCreateInfo
    {
        // User specified variables
        public Race RaceId = Race.None;
        public Class ClassId = Class.None;
        public Gender Sex = Gender.None;
        public Array<ChrCustomizationChoice> Customizations = new Array<ChrCustomizationChoice>(50);
        public Optional<uint> TemplateSet = new Optional<uint>();
        public bool IsTrialBoost;
        public bool UseNPE;
        public string Name;

        // Server side data
        public byte CharCount = 0;
    }

    public class CharacterRenameInfo
    {
        public string NewName;
        public ObjectGuid Guid;
    }

    public class CharCustomizeInfo
    {
        public ObjectGuid CharGUID;
        public Gender SexID = Gender.None;
        public string CharName;
        public Array<ChrCustomizationChoice> Customizations = new Array<ChrCustomizationChoice>(50);
    }

    public class CharRaceOrFactionChangeInfo
    {
        public Race RaceID = Race.None;
        public Gender SexID = Gender.None;
        public ObjectGuid Guid;
        public bool FactionChange;
        public string Name;
        public Array<ChrCustomizationChoice> Customizations = new Array<ChrCustomizationChoice>(50);
    }

    public class CharacterUndeleteInfo
    {            // User specified variables
        public ObjectGuid CharacterGuid; // Guid of the character to restore
        public int ClientToken = 0; // @todo: research

        // Server side data
        public string Name;
    }
}
