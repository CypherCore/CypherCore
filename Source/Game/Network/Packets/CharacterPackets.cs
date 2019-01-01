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
using Framework.Database;
using Framework.Dynamic;
using Framework.GameMath;
using Framework.IO;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
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
            _worldPacket.WriteBit(IsTestDemonHunterCreationAllowed);
            _worldPacket.WriteBit(HasDemonHunterOnRealm);
            _worldPacket.WriteBit(IsDemonHunterCreationAllowed);
            _worldPacket.WriteBit(DisabledClassesMask.HasValue);
            _worldPacket.WriteBit(IsAlliedRacesCreationAllowed);
            _worldPacket.WriteUInt32(Characters.Count);
            _worldPacket.WriteInt32(MaxCharacterLevel);
            _worldPacket.WriteUInt32(RaceUnlockData.Count);

            if (DisabledClassesMask.HasValue)
                _worldPacket.WriteUInt32(DisabledClassesMask.Value);

            foreach (CharacterInfo charInfo in Characters)
                charInfo.Write(_worldPacket);

            foreach (RaceUnlock raceUnlock in RaceUnlockData)
                raceUnlock.Write(_worldPacket);
        }

        public bool Success;
        public bool IsDeletedCharacters; // used for character undelete list
        public bool IsTestDemonHunterCreationAllowed = false; //allows client to skip 1 per realm and level 70 requirements
        public bool HasDemonHunterOnRealm = false;
        public bool IsDemonHunterCreationAllowed = false; //used for demon hunter early access
        public bool IsAlliedRacesCreationAllowed = false;

        public int MaxCharacterLevel = 1;
        public Optional<uint> DisabledClassesMask = new Optional<uint>();

        public List<CharacterInfo> Characters = new List<CharacterInfo>(); // all characters on the list
        public List<RaceUnlock> RaceUnlockData = new List<RaceUnlock>(); //

        public class CharacterInfo
        {
            public CharacterInfo(SQLFields fields)
            {
                //         0                1                2                3                 4                  5                6                7
                // "SELECT characters.guid, characters.name, characters.race, characters.class, characters.gender, characters.skin, characters.face, characters.hairStyle, "
                //  8                     9                       10                         11                         12                         13
                // "characters.hairColor, characters.facialStyle, characters.customDisplay1, characters.customDisplay2, characters.customDisplay3, characters.level, "
                //  14               15              16                     17                     18
                // "characters.zone, characters.map, characters.position_x, characters.position_y, characters.position_z, "
                //  19                    20                      21                   22                   23                     24                   25
                // "guild_member.guildid, characters.playerFlags, characters.at_login, character_pet.entry, character_pet.modelid, character_pet.level, characters.equipmentCache, "
                //  26                     27               28                      29                            30                         31
                // "character_banned.guid, characters.slot, characters.logout_time, characters.activeTalentGroup, characters.lastLoginBuild, character_declinedname.genitive"

                Guid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(0));
                Name = fields.Read<string>(1);
                RaceId = fields.Read<byte>(2);
                ClassId = (Class)fields.Read<byte>(3);
                Sex = fields.Read<byte>(4);
                Skin = fields.Read<byte>(5);
                Face = fields.Read<byte>(6);
                HairStyle = fields.Read<byte>(7);
                HairColor = fields.Read<byte>(8);
                FacialHair = fields.Read<byte>(9);
                CustomDisplay[0] = fields.Read<byte>(10);
                CustomDisplay[1] = fields.Read<byte>(11);
                CustomDisplay[2] = fields.Read<byte>(12);
                Level = fields.Read<byte>(13);
                ZoneId = fields.Read<uint>(14);
                MapId = fields.Read<uint>(15);
                PreLoadPosition = new Vector3(fields.Read<float>(16), fields.Read<float>(17), fields.Read<float>(18));

                ulong guildId = fields.Read<ulong>(19);
                if (guildId != 0)
                    GuildGuid = ObjectGuid.Create(HighGuid.Guild, guildId);

                PlayerFlags playerFlags = (PlayerFlags)fields.Read<uint>(20);
                AtLoginFlags atLoginFlags = (AtLoginFlags)fields.Read<ushort>(21);

                if (atLoginFlags.HasAnyFlag(AtLoginFlags.Resurrect))
                    playerFlags &= ~PlayerFlags.Ghost;

                if (playerFlags.HasAnyFlag(PlayerFlags.HideHelm))
                    Flags |= CharacterFlags.HideHelm;

                if (playerFlags.HasAnyFlag(PlayerFlags.HideCloak))
                    Flags |= CharacterFlags.HideCloak;

                if (playerFlags.HasAnyFlag(PlayerFlags.Ghost))
                    Flags |= CharacterFlags.Ghost;

                if (atLoginFlags.HasAnyFlag(AtLoginFlags.Rename))
                    Flags |= CharacterFlags.Rename;

                if (fields.Read<uint>(26) != 0)
                    Flags |= CharacterFlags.LockedByBilling;

                if (WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed) && !string.IsNullOrEmpty(fields.Read<string>(31)))
                    Flags |= CharacterFlags.Declined;

                if (atLoginFlags.HasAnyFlag(AtLoginFlags.Customize))
                    CustomizationFlag = CharacterCustomizeFlags.Customize;
                else if (atLoginFlags.HasAnyFlag(AtLoginFlags.ChangeFaction))
                    CustomizationFlag = CharacterCustomizeFlags.Faction;
                else if (atLoginFlags.HasAnyFlag(AtLoginFlags.ChangeRace))
                    CustomizationFlag = CharacterCustomizeFlags.Race;

                Flags3 = 0;
                Flags4 = 0;
                FirstLogin = atLoginFlags.HasAnyFlag(AtLoginFlags.FirstLogin);

                // show pet at selection character in character list only for non-ghost character
                if (!playerFlags.HasAnyFlag(PlayerFlags.Ghost) && (ClassId == Class.Warlock || ClassId == Class.Hunter || ClassId == Class.Deathknight))
                {
                    CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(fields.Read<uint>(22));
                    if (creatureInfo != null)
                    {
                        Pet.CreatureDisplayId = fields.Read<uint>(23);
                        Pet.Level = fields.Read<ushort>(24);
                        Pet.CreatureFamily = (uint)creatureInfo.Family;
                    }
                }

                BoostInProgress = false;
                ProfessionIds[0] = 0;
                ProfessionIds[1] = 0;

                StringArguments equipment = new StringArguments(fields.Read<string>(25));
                ListPosition = fields.Read<byte>(27);
                LastPlayedTime = fields.Read<uint>(28);

                var spec = Global.DB2Mgr.GetChrSpecializationByIndex(ClassId, fields.Read<byte>(29));
                if (spec != null)
                    SpecID = (ushort)spec.Id;

                LastLoginBuild = fields.Read<uint>(30);

                for (byte slot = 0; slot < InventorySlots.BagEnd; ++slot)
                {
                    VisualItems[slot].InventoryType = (byte)equipment.NextUInt32();
                    VisualItems[slot].DisplayId = equipment.NextUInt32();
                    VisualItems[slot].DisplayEnchantId = equipment.NextUInt32();
                }
            }

            public void Write(WorldPacket data)
            {
                data.WritePackedGuid(Guid);
                data.WriteUInt64(GuildClubMemberID);
                data.WriteUInt8(ListPosition);
                data.WriteUInt8(RaceId);
                data.WriteUInt8(ClassId);
                data.WriteUInt8(Sex);
                data.WriteUInt8(Skin);
                data.WriteUInt8(Face);
                data.WriteUInt8(HairStyle);
                data.WriteUInt8(HairColor);
                data.WriteUInt8(FacialHair);

                foreach (var display in CustomDisplay)
                    data.WriteUInt8(display);

                data.WriteUInt8(Level);
                data.WriteUInt32(ZoneId);
                data.WriteUInt32(MapId);
                data.WriteVector3(PreLoadPosition);
                data.WritePackedGuid(GuildGuid);
                data.WriteUInt32(Flags);
                data.WriteUInt32(CustomizationFlag);
                data.WriteUInt32(Flags3);
                data.WriteUInt32(Pet.CreatureDisplayId);
                data.WriteUInt32(Pet.Level);
                data.WriteUInt32(Pet.CreatureFamily);

                data.WriteUInt32(ProfessionIds[0]);
                data.WriteUInt32(ProfessionIds[1]);

                foreach (var visualItem in VisualItems)
                    visualItem.Write(data);

                data.WriteUInt32(LastPlayedTime);
                data.WriteUInt16(SpecID);
                data.WriteUInt32(Unknown703);
                data.WriteUInt32(LastLoginBuild);
                data.WriteUInt32(Flags4);
                data.WriteBits(Name.GetByteCount(), 6);
                data.WriteBit(FirstLogin);
                data.WriteBit(BoostInProgress);
                data.WriteBits(unkWod61x, 5);
                data.FlushBits();

                data.WriteString(Name);
            }

            public ObjectGuid Guid;
            public ulong GuildClubMemberID; // same as bgs.protocol.club.v1.MemberId.unique_id, guessed basing on SMSG_QUERY_PLAYER_NAME_RESPONSE (that one is known)
            public string Name;
            public byte ListPosition; // Order of the characters in list
            public byte RaceId;
            public Class ClassId;
            public byte Sex;
            public byte Skin;
            public byte Face;
            public byte HairStyle;
            public byte HairColor;
            public byte FacialHair;
            public Array<byte> CustomDisplay = new Array<byte>(PlayerConst.CustomDisplaySize);
            public byte Level;
            public uint ZoneId;
            public uint MapId;
            public Vector3 PreLoadPosition;
            public ObjectGuid GuildGuid;
            public CharacterFlags Flags; // Character flag @see enum CharacterFlags
            public CharacterCustomizeFlags CustomizationFlag; // Character customization flags @see enum CharacterCustomizeFlags
            public uint Flags3; // Character flags 3 @todo research
            public uint Flags4;
            public bool FirstLogin;
            public byte unkWod61x;
            public uint LastPlayedTime;
            public ushort SpecID;
            public uint Unknown703;
            public uint LastLoginBuild;
            public PetInfo Pet = new PetInfo();
            public bool BoostInProgress; // @todo
            public uint[] ProfessionIds = new uint[2];      // @todo
            public VisualItemInfo[] VisualItems = new VisualItemInfo[InventorySlots.BagEnd];

            public struct VisualItemInfo
            {
                public void Write(WorldPacket data)
                {
                    data.WriteUInt32(DisplayId);
                    data.WriteUInt32(DisplayEnchantId);
                    data.WriteUInt8(InventoryType);
                }

                public uint DisplayId;
                public uint DisplayEnchantId;
                public byte InventoryType;
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

            CreateInfo.RaceId = (Race)_worldPacket.ReadUInt8();
            CreateInfo.ClassId = (Class)_worldPacket.ReadUInt8();
            CreateInfo.Sex = (Gender)_worldPacket.ReadUInt8();
            CreateInfo.Skin = _worldPacket.ReadUInt8();
            CreateInfo.Face = _worldPacket.ReadUInt8();
            CreateInfo.HairStyle = _worldPacket.ReadUInt8();
            CreateInfo.HairColor = _worldPacket.ReadUInt8();
            CreateInfo.FacialHairStyle = _worldPacket.ReadUInt8();
            CreateInfo.OutfitId = _worldPacket.ReadUInt8();

            for (var i = 0; i < CreateInfo.CustomDisplay.GetLimit(); ++i)
                CreateInfo.CustomDisplay[i] = _worldPacket.ReadUInt8();

            CreateInfo.Name = _worldPacket.ReadString(nameLength);
            if (CreateInfo.TemplateSet.HasValue)
                CreateInfo.TemplateSet.Set(_worldPacket.ReadUInt32());
        }

        public CharacterCreateInfo CreateInfo;
    }

    public class CreateChar : ServerPacket
    {
        public CreateChar() : base(ServerOpcodes.CreateChar) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Code);
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
            _worldPacket.WriteUInt8(Code);
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
            _worldPacket.WriteUInt8(Result);
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
            CustomizeInfo.SkinID = _worldPacket.ReadUInt8();
            CustomizeInfo.HairColorID = _worldPacket.ReadUInt8();
            CustomizeInfo.HairStyleID = _worldPacket.ReadUInt8();
            CustomizeInfo.FacialHairStyleID = _worldPacket.ReadUInt8();
            CustomizeInfo.FaceID = _worldPacket.ReadUInt8();

            for (var i = 0; i < CustomizeInfo.CustomDisplay.GetLimit(); ++i)
                CustomizeInfo.CustomDisplay[i] = _worldPacket.ReadUInt8();

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

            RaceOrFactionChangeInfo.SkinID = _worldPacket.ReadUInt8();
            RaceOrFactionChangeInfo.HairColorID = _worldPacket.ReadUInt8();
            RaceOrFactionChangeInfo.HairStyleID = _worldPacket.ReadUInt8();
            RaceOrFactionChangeInfo.FacialHairStyleID = _worldPacket.ReadUInt8();
            RaceOrFactionChangeInfo.FaceID = _worldPacket.ReadUInt8();

            for (var i = 0; i < RaceOrFactionChangeInfo.CustomDisplay.GetLimit(); ++i)
                RaceOrFactionChangeInfo.CustomDisplay[i] = _worldPacket.ReadUInt8();

            RaceOrFactionChangeInfo.Name = _worldPacket.ReadString(nameLength);
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
            _worldPacket.WriteUInt8(Result);
            _worldPacket.WritePackedGuid(Guid);
            _worldPacket.WriteBit(Display.HasValue);
            _worldPacket.FlushBits();

            if (Display.HasValue)
            {
                _worldPacket.WriteBits(Display.Value.Name.GetByteCount(), 6);
                _worldPacket.WriteUInt8(Display.Value.SexID);
                _worldPacket.WriteUInt8(Display.Value.SkinID);
                _worldPacket.WriteUInt8(Display.Value.HairColorID);
                _worldPacket.WriteUInt8(Display.Value.HairStyleID);
                _worldPacket.WriteUInt8(Display.Value.FacialHairStyleID);
                _worldPacket.WriteUInt8(Display.Value.FaceID);
                _worldPacket.WriteUInt8(Display.Value.RaceID);
                Display.Value.CustomDisplay.ForEach(id => _worldPacket.WriteUInt8(id));
                _worldPacket.WriteString(Display.Value.Name);
            }
        }

        public ResponseCodes Result = 0;
        public ObjectGuid Guid;
        public Optional<CharFactionChangeDisplayInfo> Display;

        public class CharFactionChangeDisplayInfo
        {
            public string Name;
            public byte SexID;
            public byte SkinID;
            public byte HairColorID;
            public byte HairStyleID;
            public byte FacialHairStyleID;
            public byte FaceID;
            public byte RaceID;
            public Array<byte> CustomDisplay = new Array<byte>(PlayerConst.CustomDisplaySize);
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

        public ReorderInfo[] Entries = new ReorderInfo[WorldConfig.GetIntValue(WorldCfg.CharactersPerRealm)];

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
            _worldPacket.WriteUInt32(UndeleteInfo.ClientToken);
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
            _worldPacket.WriteUInt32(Result);
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
        float FarClip; // Visibility distance (for terrain)
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
            _worldPacket.WriteUInt8(Code);
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

        bool IdleLogout;
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

        int MapID = -1;
        bool Showing;
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
            NewHairStyle = _worldPacket.ReadUInt32();
            NewHairColor = _worldPacket.ReadUInt32();
            NewFacialHair = _worldPacket.ReadUInt32();
            NewSkinColor = _worldPacket.ReadUInt32();
            NewFace = _worldPacket.ReadUInt32();

            for (var i = 0; i < NewCustomDisplay.GetLimit(); ++i)
                NewCustomDisplay[i] = _worldPacket.ReadUInt32();
        }

        public uint NewHairStyle;
        public uint NewHairColor;
        public uint NewFacialHair;
        public uint NewSkinColor;
        public uint NewFace;
        public Array<uint> NewCustomDisplay = new Array<uint>(PlayerConst.CustomDisplaySize);
    }

    public class BarberShopResult : ServerPacket
    {
        public BarberShopResult(ResultEnum result) : base(ServerOpcodes.BarberShopResult)
        {
            Result = result;
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Result);
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
            _worldPacket.WriteUInt8(Reason);
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

    class CharCustomizeResponse : ServerPacket
    {
        public CharCustomizeResponse(CharCustomizeInfo customizeInfo) : base(ServerOpcodes.CharCustomize)
        {
            CharGUID = customizeInfo.CharGUID;
            SexID = (byte)customizeInfo.SexID;
            SkinID = customizeInfo.SkinID;
            HairColorID = customizeInfo.HairColorID;
            HairStyleID = customizeInfo.HairStyleID;
            FacialHairStyleID = customizeInfo.FacialHairStyleID;
            FaceID = customizeInfo.FaceID;
            CharName = customizeInfo.CharName;
            CustomDisplay = customizeInfo.CustomDisplay;
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(CharGUID);
            _worldPacket.WriteUInt8(SexID);
            _worldPacket.WriteUInt8(SkinID);
            _worldPacket.WriteUInt8(HairColorID);
            _worldPacket.WriteUInt8(HairStyleID);
            _worldPacket.WriteUInt8(FacialHairStyleID);
            _worldPacket.WriteUInt8(FaceID);
            CustomDisplay.ForEach(id => _worldPacket.WriteUInt8(id));
            _worldPacket.WriteBits(CharName.GetByteCount(), 6);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(CharName);
        }

        ObjectGuid CharGUID;
        string CharName = "";
        byte SexID;
        byte SkinID;
        byte HairColorID;
        byte HairStyleID;
        byte FacialHairStyleID;
        byte FaceID;
        public Array<byte> CustomDisplay = new Array<byte>(PlayerConst.CustomDisplaySize);
    }

    class CharCustomizeFailed : ServerPacket
    {
        public CharCustomizeFailed() : base(ServerOpcodes.CharCustomizeFailed) { }

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
            _worldPacket.WriteInt32(ResultCode);
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
        public byte Skin;
        public byte Face;
        public byte HairStyle;
        public byte HairColor;
        public byte FacialHairStyle;
        public Array<byte> CustomDisplay = new Array<byte>(PlayerConst.CustomDisplaySize);
        public byte OutfitId;
        public Optional<uint> TemplateSet = new Optional<uint>();
        public bool IsTrialBoost;
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
        public byte HairStyleID;
        public byte FaceID;
        public ObjectGuid CharGUID;
        public Gender SexID = Gender.None;
        public string CharName;
        public byte HairColorID;
        public byte FacialHairStyleID;
        public byte SkinID;
        public Array<byte> CustomDisplay = new Array<byte>(PlayerConst.CustomDisplaySize);
    }

    public class CharRaceOrFactionChangeInfo
    {
        public byte HairColorID;
        public Race RaceID = Race.None;
        public Gender SexID = Gender.None;
        public byte SkinID;
        public byte FacialHairStyleID;
        public ObjectGuid Guid;
        public bool FactionChange;
        public string Name;
        public byte FaceID;
        public byte HairStyleID;
        public Array<byte> CustomDisplay = new Array<byte>(PlayerConst.CustomDisplaySize);
    }

    public class CharacterUndeleteInfo
    {            // User specified variables
        public ObjectGuid CharacterGuid; // Guid of the character to restore
        public int ClientToken = 0; // @todo: research

        // Server side data
        public string Name;
    }
}
