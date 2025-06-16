// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Dynamic;
using Framework.IO;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;

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
            _worldPacket.WriteBit(Realmless);
            _worldPacket.WriteBit(IsDeletedCharacters);
            _worldPacket.WriteBit(IgnoreNewPlayerRestrictions);
            _worldPacket.WriteBit(IsRestrictedNewPlayer);
            _worldPacket.WriteBit(IsNewcomerChatCompleted);
            _worldPacket.WriteBit(IsRestrictedTrial);
            _worldPacket.WriteBit(ClassDisableMask.HasValue);
            _worldPacket.WriteBit(DontCreateCharacterDisplays);
            _worldPacket.WriteInt32(Characters.Count);
            _worldPacket.WriteInt32(RegionwideCharacters.Count);
            _worldPacket.WriteInt32(MaxCharacterLevel);
            _worldPacket.WriteInt32(RaceUnlockData.Count);
            _worldPacket.WriteInt32(UnlockedConditionalAppearances.Count);
            _worldPacket.WriteInt32(RaceLimitDisables.Count);
            _worldPacket.WriteInt32(WarbandGroups.Count);

            if (ClassDisableMask.HasValue)
                _worldPacket.WriteUInt32(ClassDisableMask.Value);

            foreach (UnlockedConditionalAppearance unlockedConditionalAppearance in UnlockedConditionalAppearances)
                unlockedConditionalAppearance.Write(_worldPacket);

            foreach (RaceLimitDisableInfo raceLimitDisableInfo in RaceLimitDisables)
                raceLimitDisableInfo.Write(_worldPacket);

            foreach (CharacterInfo charInfo in Characters)
                charInfo.Write(_worldPacket);

            foreach (RegionwideCharacterListEntry charInfo in RegionwideCharacters)
                charInfo.Write(_worldPacket);

            foreach (RaceUnlock raceUnlock in RaceUnlockData)
                raceUnlock.Write(_worldPacket);

            foreach (WarbandGroup warbandGroup in WarbandGroups)
                warbandGroup.Write(_worldPacket);
        }

        public bool Success;
        public bool Realmless;
        public bool IsDeletedCharacters; // used for character undelete list
        public bool IgnoreNewPlayerRestrictions; // allows client to skip new player restrictions
        public bool IsRestrictedNewPlayer; // forbids using level boost and class trials
        public bool IsNewcomerChatCompleted; // forbids hero classes and allied races
        public bool IsRestrictedTrial;
        public bool DontCreateCharacterDisplays;

        public int MaxCharacterLevel = 1;
        public uint? ClassDisableMask = new();

        public List<CharacterInfo> Characters = new(); // all characters on the list
        public List<RegionwideCharacterListEntry> RegionwideCharacters = new();
        public List<RaceUnlock> RaceUnlockData = new();
        public List<UnlockedConditionalAppearance> UnlockedConditionalAppearances = new();
        public List<RaceLimitDisableInfo> RaceLimitDisables = new();
        public List<WarbandGroup> WarbandGroups = new();

        public class CharacterInfoBasic
        {
            public CharacterInfoBasic(SQLFields fields)
            {
                Guid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(0));
                VirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
                Name = fields.Read<string>(1);
                RaceId = fields.Read<byte>(2);
                ClassId = (Class)fields.Read<byte>(3);
                SexId = fields.Read<byte>(4);
                ExperienceLevel = fields.Read<byte>(5);
                ZoneId = fields.Read<uint>(6);
                MapId = fields.Read<uint>(7);
                PreloadPos = new Vector3(fields.Read<float>(8), fields.Read<float>(9), fields.Read<float>(10));

                ulong guildId = fields.Read<ulong>(11);
                if (guildId != 0)
                    GuildGuid = ObjectGuid.Create(HighGuid.Guild, guildId);

                PlayerFlags playerFlags = (PlayerFlags)fields.Read<uint>(12);
                AtLoginFlags atLoginFlags = (AtLoginFlags)fields.Read<ushort>(13);

                if (playerFlags.HasFlag(PlayerFlags.Resting))
                    Flags |= CharacterFlags.Resting;

                if (atLoginFlags.HasAnyFlag(AtLoginFlags.Resurrect))
                    playerFlags &= ~PlayerFlags.Ghost;

                if (playerFlags.HasAnyFlag(PlayerFlags.Ghost))
                    Flags |= CharacterFlags.Ghost;

                if (atLoginFlags.HasAnyFlag(AtLoginFlags.Rename))
                    Flags |= CharacterFlags.Rename;

                if (fields.Read<uint>(18) != 0)
                    Flags |= CharacterFlags.LockedByBilling;

                if (WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed) && !string.IsNullOrEmpty(fields.Read<string>(28)))
                    Flags |= CharacterFlags.Declined;

                if (atLoginFlags.HasAnyFlag(AtLoginFlags.Customize))
                    Flags2 = CharacterCustomizeFlags.Customize;
                else if (atLoginFlags.HasAnyFlag(AtLoginFlags.ChangeFaction))
                    Flags2 = CharacterCustomizeFlags.Faction;
                else if (atLoginFlags.HasAnyFlag(AtLoginFlags.ChangeRace))
                    Flags2 = CharacterCustomizeFlags.Race;

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

                ProfessionIds[0] = 0;
                ProfessionIds[1] = 0;

                StringArray equipment = new(fields.Read<string>(17), ' ');
                ListPosition = fields.Read<byte>(19);
                LastActiveTime = fields.Read<long>(20);

                var spec = Global.DB2Mgr.GetChrSpecializationByIndex(ClassId, fields.Read<byte>(21));
                if (spec != null)
                    SpecID = (short)spec.Id;

                LastLoginVersion = fields.Read<int>(22);

                PersonalTabard.EmblemStyle = fields.Read<int>(23);
                PersonalTabard.EmblemColor = fields.Read<int>(24);
                PersonalTabard.BorderStyle = fields.Read<int>(25);
                PersonalTabard.BorderColor = fields.Read<int>(26);
                PersonalTabard.BackgroundColor = fields.Read<int>(27);

                int equipmentFieldsPerSlot = 5;

                for (var slot = 0; slot < VisualItems.Length && (slot + 1) * equipmentFieldsPerSlot <= equipment.Length; ++slot)
                {
                    int visualBase = slot * equipmentFieldsPerSlot;
                    VisualItems[slot].InvType = byte.Parse(equipment[visualBase + 0]);
                    VisualItems[slot].DisplayId = uint.Parse(equipment[visualBase + 1]);
                    VisualItems[slot].DisplayEnchantId = uint.Parse(equipment[visualBase + 2]);
                    VisualItems[slot].Subclass = byte.Parse(equipment[visualBase + 3]);
                    VisualItems[slot].SecondaryItemModifiedAppearanceID = uint.Parse(equipment[visualBase + 4]);
                }
            }

            public void Write(WorldPacket data)
            {
                data.WritePackedGuid(Guid);
                data.WriteUInt32(VirtualRealmAddress);
                data.WriteUInt8(ListPosition);
                data.WriteUInt8(RaceId);
                data.WriteUInt8(SexId);
                data.WriteUInt8((byte)ClassId);
                data.WriteInt16(SpecID);
                data.WriteInt32(Customizations.Count);

                data.WriteUInt8(ExperienceLevel);
                data.WriteUInt32(MapId);
                data.WriteUInt32(ZoneId);
                data.WriteVector3(PreloadPos);
                data.WriteUInt64(GuildClubMemberID);
                data.WritePackedGuid(GuildGuid);
                data.WriteUInt32((uint)Flags);
                data.WriteUInt32((uint)Flags2);
                data.WriteUInt32(Flags3);
                data.WriteUInt32(Flags4);
                data.WriteUInt8(CantLoginReason);

                data.WriteUInt32(PetCreatureDisplayId);
                data.WriteUInt32(PetExperienceLevel);
                data.WriteUInt32(PetCreatureFamilyId);

                foreach (var visualItem in VisualItems)
                    visualItem.Write(data);

                data.WriteInt32(SaveVersion);
                data.WriteInt64(LastActiveTime);
                data.WriteInt32(LastLoginVersion);
                PersonalTabard.Write(data);

                data.WriteUInt32(ProfessionIds[0]);
                data.WriteUInt32(ProfessionIds[1]);

                data.WriteInt32(TimerunningSeasonID);
                data.WriteUInt32(OverrideSelectScreenFileDataID);
                data.WriteUInt32(Unused1110_1);

                foreach (ChrCustomizationChoice customization in Customizations)
                {
                    data.WriteUInt32(customization.ChrCustomizationOptionID);
                    data.WriteUInt32(customization.ChrCustomizationChoiceID);
                }

                data.WriteBits(Name.GetByteCount(), 6);
                data.WriteBit(FirstLogin);
                data.WriteBit(Unused1110_2);
                data.WriteBit(Unused1110_3);
                data.FlushBits();

                data.WriteString(Name);
            }

            public ObjectGuid Guid;
            public uint VirtualRealmAddress;
            public ulong GuildClubMemberID; // same as bgs.protocol.club.v1.MemberId.unique_id, guessed basing on SMSG_QUERY_PLAYER_NAME_RESPONSE (that one is known)
            public string Name;
            public byte ListPosition; // Order of the characters in list
            public byte RaceId;
            public Class ClassId;
            public byte SexId;
            public Array<ChrCustomizationChoice> Customizations = new(250);
            public byte ExperienceLevel;
            public uint ZoneId;
            public uint MapId;
            public Vector3 PreloadPos;
            public ObjectGuid GuildGuid;
            public CharacterFlags Flags; // Character flag @see enum CharacterFlags
            public CharacterCustomizeFlags Flags2; // Character customization flags @see enum CharacterCustomizeFlags
            public uint Flags3; // Character flags 3 @todo research
            public uint Flags4; ///< Character flags 4 @todo research
            public bool FirstLogin;
            public byte CantLoginReason;
            public long LastActiveTime;
            public short SpecID;
            public int SaveVersion;
            public int LastLoginVersion;
            public uint OverrideSelectScreenFileDataID;
            public int TimerunningSeasonID;
            public uint PetCreatureDisplayId;
            public uint PetExperienceLevel;
            public uint PetCreatureFamilyId;
            public uint[] ProfessionIds = new uint[2];      // @todo
            public VisualItemInfo[] VisualItems = new VisualItemInfo[19];
            public CustomTabardInfo PersonalTabard = new();
            public uint Unused1110_1;
            public bool Unused1110_2;
            public bool Unused1110_3;

            public struct VisualItemInfo
            {
                public void Write(WorldPacket data)
                {
                    data.WriteUInt32(DisplayId);
                    data.WriteUInt8(InvType);
                    data.WriteUInt32(DisplayEnchantId);
                    data.WriteUInt8(Subclass);
                    data.WriteUInt32(SecondaryItemModifiedAppearanceID);
                    data.WriteUInt32(ItemID);
                    data.WriteUInt32(TransmogrifiedItemID);
                }

                public uint DisplayId;
                public uint DisplayEnchantId;
                public uint SecondaryItemModifiedAppearanceID; // also -1 is some special value
                public byte InvType;
                public byte Subclass;
                public uint ItemID;
                public uint TransmogrifiedItemID;
            }

            public struct PetInfo
            {
                public uint CreatureDisplayId; // PetCreatureDisplayID
                public uint Level; // PetExperienceLevel
                public uint CreatureFamily; // PetCreatureFamilyID
            }
        }

        public class CharacterRestrictionAndMailData
        {
            public bool BoostInProgress; ///< @todo
            public uint RestrictionFlags;
            public List<string> MailSenders = new();
            public List<uint> MailSenderTypes = new();
            public bool RpeResetAvailable;
            public bool RpeResetQuestClearAvailable;

            public void Write(WorldPacket data)
            {
                Cypher.Assert(MailSenders.Count == MailSenderTypes.Count);

                data.WriteBit(BoostInProgress);
                data.WriteBit(RpeResetAvailable);
                data.WriteBit(RpeResetQuestClearAvailable);
                data.FlushBits();

                data.WriteUInt32(RestrictionFlags);
                data.WriteInt32(MailSenders.Count);
                data.WriteInt32(MailSenderTypes.Count);

                if (!MailSenderTypes.Empty())
                    foreach (var type in MailSenderTypes)
                        data.WriteUInt32(type);

                foreach (string str in MailSenders)
                    data.WriteBits(str.GetByteCount() + 1, 6);

                data.FlushBits();

                foreach (string str in MailSenders)
                    if (!str.IsEmpty())
                        data.WriteCString(str);
            }
        }

        public struct CharacterInfo
        {
            public CharacterInfoBasic Basic;
            public CharacterRestrictionAndMailData RestrictionsAndMails = new();

            public CharacterInfo(SQLFields fields)
            {
                Basic = new(fields);
            }

            public void Write(WorldPacket data)
            {
                Basic.Write(data);
                RestrictionsAndMails.Write(data);
            }
        }

        public struct RegionwideCharacterListEntry
        {
            public CharacterInfoBasic Basic;
            public ulong Money;
            public float AvgEquippedItemLevel;
            public float CurrentSeasonMythicPlusOverallScore;
            public uint CurrentSeasonBestPvpRating;
            public sbyte PvpRatingBracket;
            public short PvpRatingAssociatedSpecID;

            public RegionwideCharacterListEntry(SQLFields fields)
            {
                Basic = new(fields);
            }

            public void Write(WorldPacket data)
            {
                Basic.Write(data);
                data.WriteUInt64(Money);
                data.WriteFloat(AvgEquippedItemLevel);
                data.WriteFloat(CurrentSeasonMythicPlusOverallScore);
                data.WriteUInt32(CurrentSeasonBestPvpRating);
                data.WriteInt8(PvpRatingBracket);
                data.WriteInt16(PvpRatingAssociatedSpecID);
            }
        }

        public struct RaceUnlock
        {
            public sbyte RaceID;
            public bool HasUnlockedLicense;
            public bool HasUnlockedAchievement;
            public bool HasHeritageArmorUnlockAchievement;
            public bool HideRaceOnClient;
            public bool Unused1027;

            public void Write(WorldPacket data)
            {
                data.WriteInt8(RaceID);
                data.WriteBit(HasUnlockedLicense);
                data.WriteBit(HasUnlockedAchievement);
                data.WriteBit(HasHeritageArmorUnlockAchievement);
                data.WriteBit(HideRaceOnClient);
                data.WriteBit(Unused1027);
                data.FlushBits();
            }
        }

        public struct UnlockedConditionalAppearance
        {
            public int AchievementID;
            public int ConditionalType;

            public void Write(WorldPacket data)
            {
                data.WriteInt32(AchievementID);
                data.WriteInt32(ConditionalType);
            }
        }

        public struct RaceLimitDisableInfo
        {
            public sbyte RaceID;
            public int Reason;

            public void Write(WorldPacket data)
            {
                data.WriteInt8(RaceID);
                data.WriteInt32(Reason);
            }
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
            uint nameLength = _worldPacket.ReadBits<uint>(6);
            bool hasTemplateSet = _worldPacket.HasBit();
            CreateInfo.IsTrialBoost = _worldPacket.HasBit();
            CreateInfo.UseNPE = _worldPacket.HasBit();
            CreateInfo.HardcoreSelfFound = _worldPacket.HasBit();

            CreateInfo.RaceId = (Race)_worldPacket.ReadUInt8();
            CreateInfo.ClassId = (Class)_worldPacket.ReadUInt8();
            CreateInfo.Sex = (Gender)_worldPacket.ReadUInt8();
            var customizationCount = _worldPacket.ReadUInt32();
            CreateInfo.TimerunningSeasonID = _worldPacket.ReadInt32();
            CreateInfo.Name = _worldPacket.ReadString(nameLength);
            if (CreateInfo.TemplateSet.HasValue)
                CreateInfo.TemplateSet = _worldPacket.ReadUInt32();

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
        public CharacterRenameResult() : base(ServerOpcodes.CharacterRenameResult) { }

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
        public ResponseCodes Result;
        public ObjectGuid? Guid;
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

            uint nameLength = _worldPacket.ReadBits<uint>(6);

            RaceOrFactionChangeInfo.Guid = _worldPacket.ReadPackedGuid();
            RaceOrFactionChangeInfo.SexID = (Gender)_worldPacket.ReadUInt8();
            RaceOrFactionChangeInfo.RaceID = (Race)_worldPacket.ReadUInt8();
            RaceOrFactionChangeInfo.InitialRaceID = (Race)_worldPacket.ReadUInt8();
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
        public CharFactionChangeResult() : base(ServerOpcodes.CharFactionChangeResult) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Result);
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteBit(Display != null);
            _worldPacket.FlushBits();

            if (Display != null)
            {
                _worldPacket.WriteBits(Display.Name.GetByteCount(), 6);
                _worldPacket.WriteUInt8(Display.SexID);
                _worldPacket.WriteUInt8(Display.RaceID);
                _worldPacket.WriteInt32(Display.Customizations.Count);
                _worldPacket.WriteString(Display.Name);

                foreach (ChrCustomizationChoice customization in Display.Customizations)
                {
                    _worldPacket.WriteUInt32(customization.ChrCustomizationOptionID);
                    _worldPacket.WriteUInt32(customization.ChrCustomizationChoiceID);
                }
            }
        }

        public ResponseCodes Result = 0;
        public ObjectGuid Guid;
        public CharFactionChangeDisplayInfo Display;

        public class CharFactionChangeDisplayInfo
        {
            public string Name;
            public byte SexID;
            public byte RaceID;
            public Array<ChrCustomizationChoice> Customizations = new(250);
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
            uint count = _worldPacket.ReadBits<uint>(9);
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
        SwitchGameModeData SwitchGameMode;

        public LogoutComplete() : base(ServerOpcodes.LogoutComplete) { }

        public override void Write()
        {
            _worldPacket.WriteBit(SwitchGameMode != null);
            _worldPacket.FlushBits();

            if (SwitchGameMode != null)
                SwitchGameMode.Write(_worldPacket);
        }
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
            CustomizedRace = _worldPacket.ReadInt8();
            CustomizedChrModelID = _worldPacket.ReadInt32();
            UnalteredVisualRaceID = _worldPacket.ReadInt8();

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
        public Array<ChrCustomizationChoice> Customizations = new(250);
        public sbyte CustomizedRace;
        public int CustomizedChrModelID;
        public sbyte UnalteredVisualRaceID;
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
        }

        public ObjectGuid Victim;
        public int Original;
        public PlayerLogXPReason Reason;
        public int Amount;
        public float GroupBonus;
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

        public ushort FactionIndex;
    }

    class SetFactionNotAtWar : ClientPacket
    {
        public SetFactionNotAtWar(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            FactionIndex = _worldPacket.ReadUInt8();
        }

        public ushort FactionIndex;
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
            foreach (ChrCustomizationChoice customization in Customizations)
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
        Array<ChrCustomizationChoice> Customizations = new(125);
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

            byte[] stringLengths = new byte[SharedConst.MaxDeclinedNameCases];

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

    class SavePersonalEmblem : ClientPacket
    {
        public ObjectGuid Vendor;
        public CustomTabardInfo PersonalTabard = new();

        public SavePersonalEmblem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Vendor = _worldPacket.ReadPackedGuid();
            PersonalTabard.Read(_worldPacket);
        }
    }

    class PlayerSavePersonalEmblem : ServerPacket
    {
        public GuildEmblemError Error;

        public PlayerSavePersonalEmblem(GuildEmblemError error) : base(ServerOpcodes.PlayerSavePersonalEmblem)
        {
            Error = error;
        }

        public override void Write()
        {
            _worldPacket.WriteInt32((int)Error);
        }
    }

    //Structs
    public class CharacterCreateInfo
    {
        // User specified variables
        public Race RaceId = Race.None;
        public Class ClassId = Class.None;
        public Gender Sex = Gender.None;
        public Array<ChrCustomizationChoice> Customizations = new(250);
        public uint? TemplateSet;
        public int TimerunningSeasonID;
        public bool IsTrialBoost;
        public bool UseNPE;
        public bool HardcoreSelfFound;
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
        public Array<ChrCustomizationChoice> Customizations = new(250);
    }

    public class CharRaceOrFactionChangeInfo
    {
        public Race RaceID = Race.None;
        public Race InitialRaceID = Race.None;
        public Gender SexID = Gender.None;
        public ObjectGuid Guid;
        public bool FactionChange;
        public string Name;
        public Array<ChrCustomizationChoice> Customizations = new(250);
    }

    public class CharacterUndeleteInfo
    {            // User specified variables
        public ObjectGuid CharacterGuid; // Guid of the character to restore
        public int ClientToken; // @todo: research

        // Server side data
        public string Name;
    }

    public class CustomTabardInfo
    {
        public int EmblemStyle = -1;
        public int EmblemColor = -1;
        public int BorderStyle = -1;
        public int BorderColor = -1;
        public int BackgroundColor = -1;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(EmblemStyle);
            data.WriteInt32(EmblemColor);
            data.WriteInt32(BorderStyle);
            data.WriteInt32(BorderColor);
            data.WriteInt32(BackgroundColor);
        }

        public void Read(WorldPacket data)
        {
            EmblemStyle = data.ReadInt32();
            EmblemColor = data.ReadInt32();
            BorderStyle = data.ReadInt32();
            BorderColor = data.ReadInt32();
            BackgroundColor = data.ReadInt32();
        }
    }

    public struct WarbandGroupMember
    {
        public uint WarbandScenePlacementID;
        public int Type;
        public ObjectGuid Guid;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(WarbandScenePlacementID);
            data.WriteInt32(Type);
            if (Type == 0)
                data.WritePackedGuid(Guid);
        }
    }

    public class WarbandGroup
    {
        public ulong GroupID;
        public byte OrderIndex;
        public uint WarbandSceneID;
        public uint Flags;    ///< enum WarbandGroupFlags { Collapsed = 1 }
        public List<WarbandGroupMember> Members = new();
        public string Name = "";

        public void Write(WorldPacket data)
        {
            data.WriteUInt64(GroupID);
            data.WriteUInt8(OrderIndex);
            data.WriteUInt32(WarbandSceneID);
            data.WriteUInt32(Flags);
            data.WriteInt32(Members.Count);

            foreach (WarbandGroupMember member in Members)
                member.Write(data);

            data.WriteBits(Name.GetByteCount(), 9);
            data.FlushBits();

            data.WriteString(Name);
        }
    }

    class GameModeData
    {
        public int Unknown_1107_0;
        public ObjectGuid Guid;
        public byte GameMode;
        public int MapID = 0;
        public byte Unknown_1107_1;
        public byte Unknown_1107_2;
        public byte Unknown_1107_3;
        public Array<ChrCustomizationChoice> Customizations = new(250);
        public Array<ChrCustomizationChoice> Unknown_1107_4 = new(250);

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Unknown_1107_0);
            data.WritePackedGuid(Guid);
            data.WriteUInt8(GameMode);
            data.WriteInt32(MapID);
            data.WriteUInt8(Unknown_1107_1);
            data.WriteUInt8(Unknown_1107_2);
            data.WriteUInt8(Unknown_1107_3);
            data.WriteInt32(Customizations.Count);
            data.WriteInt32(Unknown_1107_4.Count);

            foreach (ChrCustomizationChoice customization in Customizations)
            {
                data.WriteUInt32(customization.ChrCustomizationOptionID);
                data.WriteUInt32(customization.ChrCustomizationChoiceID);
            }

            foreach (ChrCustomizationChoice customization in Unknown_1107_4)
            {
                data.WriteUInt32(customization.ChrCustomizationOptionID);
                data.WriteUInt32(customization.ChrCustomizationChoiceID);
            }
        }
    }

    class SwitchGameModeData
    {
        public bool IsFastLogin;
        public GameModeData Current;
        public GameModeData New;

        public void Write(WorldPacket data)
        {
            data.WriteBits(IsFastLogin, 1);
            Current.Write(data);
            New.Write(data);
        }
    }
}
