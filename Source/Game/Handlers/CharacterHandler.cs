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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.EnumCharacters, Status = SessionStatus.Authed)]
        void HandleCharEnum(EnumCharacters charEnum)
        {
            // remove expired bans
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_EXPIRED_BANS);
            DB.Characters.Execute(stmt);

            // get all the data necessary for loading all characters (along with their pets) on the account
            if (WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed))
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ENUM_DECLINED_NAME);
            else
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ENUM);

            stmt.AddValue(0, PetSaveMode.AsCurrent);
            stmt.AddValue(1, GetAccountId());

            _queryProcessor.AddQuery(DB.Characters.AsyncQuery(stmt).WithCallback(HandleCharEnumCallback));
        }

        void HandleCharEnumCallback(SQLResult result)
        {
            byte demonHunterCount = 0; // We use this counter to allow multiple demon hunter creations when allowed in config
            bool canAlwaysCreateDemonHunter = HasPermission(RBACPermissions.SkipCheckCharacterCreationDemonHunter);
            if (WorldConfig.GetIntValue(WorldCfg.CharacterCreatingMinLevelForDemonHunter) == 0) // char level = 0 means this check is disabled, so always true
                canAlwaysCreateDemonHunter = true;

            EnumCharactersResult charResult = new EnumCharactersResult();
            charResult.Success = true;
            charResult.IsDeletedCharacters = false;
            charResult.DisabledClassesMask.Set(WorldConfig.GetUIntValue(WorldCfg.CharacterCreatingDisabledClassmask));

            _legitCharacters.Clear();
            if (!result.IsEmpty())
            {
                do
                {
                    EnumCharactersResult.CharacterInfo charInfo = new EnumCharactersResult.CharacterInfo(result.GetFields());

                    Log.outInfo(LogFilter.Network, "Loading Character {0} from account {1}.", charInfo.Guid.ToString(), GetAccountId());

                    if (!Player.ValidateAppearance((Race)charInfo.RaceId, charInfo.ClassId, (Gender)charInfo.Sex, charInfo.HairStyle, charInfo.HairColor, charInfo.Face, charInfo.FacialHair, charInfo.Skin, charInfo.CustomDisplay))
                    {
                        Log.outError(LogFilter.Player, "Player {0} has wrong Appearance values (Hair/Skin/Color), forcing recustomize", charInfo.Guid.ToString());

                        // Make sure customization always works properly - send all zeroes instead
                        charInfo.Skin = 0;
                        charInfo.Face = 0;
                        charInfo.HairStyle = 0;
                        charInfo.HairColor = 0;
                        charInfo.FacialHair = 0;

                        if (charInfo.CustomizationFlag != CharacterCustomizeFlags.Customize)
                        {
                            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                            stmt.AddValue(0, (ushort)AtLoginFlags.Customize);
                            stmt.AddValue(1, charInfo.Guid.GetCounter());
                            DB.Characters.Execute(stmt);
                            charInfo.CustomizationFlag = CharacterCustomizeFlags.Customize;
                        }
                    }

                    // Do not allow locked characters to login
                    if (!charInfo.Flags.HasAnyFlag(CharacterFlags.CharacterLockedForTransfer | CharacterFlags.LockedByBilling))
                        _legitCharacters.Add(charInfo.Guid);

                    if (!Global.WorldMgr.HasCharacterInfo(charInfo.Guid)) // This can happen if characters are inserted into the database manually. Core hasn't loaded name data yet.
                        Global.WorldMgr.AddCharacterInfo(charInfo.Guid, GetAccountId(), charInfo.Name, charInfo.Sex, charInfo.RaceId, (byte)charInfo.ClassId, charInfo.Level, false);

                    if (charInfo.ClassId == Class.DemonHunter)
                        demonHunterCount++;

                    if (demonHunterCount >= WorldConfig.GetIntValue(WorldCfg.DemonHuntersPerRealm) && !canAlwaysCreateDemonHunter)
                        charResult.HasDemonHunterOnRealm = true;
                    else
                        charResult.HasDemonHunterOnRealm = false;

                    charResult.MaxCharacterLevel = Math.Max(charResult.MaxCharacterLevel, charInfo.Level);

                    charResult.Characters.Add(charInfo);
                }
                while (result.NextRow());
            }

            charResult.IsTestDemonHunterCreationAllowed = canAlwaysCreateDemonHunter;
            charResult.IsDemonHunterCreationAllowed = GetAccountExpansion() >= Expansion.Legion || canAlwaysCreateDemonHunter;
            charResult.IsAlliedRacesCreationAllowed = GetAccountExpansion() >= Expansion.BattleForAzeroth;

            foreach (var requirement in Global.ObjectMgr.GetRaceUnlockRequirements())
            {
                EnumCharactersResult.RaceUnlock raceUnlock = new EnumCharactersResult.RaceUnlock();
                raceUnlock.RaceID = requirement.Key;
                raceUnlock.HasExpansion = (byte)GetAccountExpansion() >= requirement.Value.Expansion;
                charResult.RaceUnlockData.Add(raceUnlock);
            }

            SendPacket(charResult);
        }

        [WorldPacketHandler(ClientOpcodes.EnumCharactersDeletedByClient, Status = SessionStatus.Authed)]
        void HandleCharUndeleteEnum(EnumCharacters enumCharacters)
        {
            // get all the data necessary for loading all undeleted characters (along with their pets) on the account
            PreparedStatement stmt;
            if (WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed))
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_UNDELETE_ENUM_DECLINED_NAME);
            else
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_UNDELETE_ENUM);

            stmt.AddValue(0, (uint)PetSaveMode.AsCurrent);
            stmt.AddValue(1, GetAccountId());

            _queryProcessor.AddQuery(DB.Characters.AsyncQuery(stmt).WithCallback(HandleCharUndeleteEnumCallback));
        }

        void HandleCharUndeleteEnumCallback(SQLResult result)
        {
            EnumCharactersResult charEnum = new EnumCharactersResult();
            charEnum.Success = true;
            charEnum.IsDeletedCharacters = true;
            charEnum.DisabledClassesMask.Set(WorldConfig.GetUIntValue(WorldCfg.CharacterCreatingDisabledClassmask));

            if (!result.IsEmpty())
            {
                do
                {
                    EnumCharactersResult.CharacterInfo charInfo = new EnumCharactersResult.CharacterInfo(result.GetFields());

                    Log.outInfo(LogFilter.Network, "Loading undeleted char guid {0} from account {1}.", charInfo.Guid.ToString(), GetAccountId());

                    if (!Global.WorldMgr.HasCharacterInfo(charInfo.Guid)) // This can happen if characters are inserted into the database manually. Core hasn't loaded name data yet.
                        Global.WorldMgr.AddCharacterInfo(charInfo.Guid, GetAccountId(), charInfo.Name, charInfo.Sex, charInfo.RaceId, (byte)charInfo.ClassId, charInfo.Level, true);

                    charEnum.Characters.Add(charInfo);
                }
                while (result.NextRow());
            }

            SendPacket(charEnum);
        }

        [WorldPacketHandler(ClientOpcodes.CreateCharacter, Status = SessionStatus.Authed)]
        void HandleCharCreate(CreateCharacter charCreate)
        {
            if (!HasPermission(RBACPermissions.SkipCheckCharacterCreationTeammask))
            {
                int mask = WorldConfig.GetIntValue(WorldCfg.CharacterCreatingDisabled);
                if (mask != 0)
                {
                    bool disabled = false;

                    uint team = Player.TeamIdForRace(charCreate.CreateInfo.RaceId);
                    switch (team)
                    {
                        case TeamId.Alliance:
                            disabled = Convert.ToBoolean(mask & (1 << 0));
                            break;
                        case TeamId.Horde:
                            disabled = Convert.ToBoolean(mask & (1 << 1));
                            break;
                        case TeamId.Neutral:
                            disabled = Convert.ToBoolean(mask & (1 << 2));
                            break;
                    }

                    if (disabled)
                    {
                        SendCharCreate(ResponseCodes.CharCreateDisabled);
                        return;
                    }
                }
            }

            ChrClassesRecord classEntry = CliDB.ChrClassesStorage.LookupByKey(charCreate.CreateInfo.ClassId);
            if (classEntry == null)
            {
                Log.outError(LogFilter.Network, "Class ({0}) not found in DBC while creating new char for account (ID: {1}): wrong DBC files or cheater?", charCreate.CreateInfo.ClassId, GetAccountId());
                SendCharCreate(ResponseCodes.CharCreateFailed);
                return;
            }

            ChrRacesRecord raceEntry = CliDB.ChrRacesStorage.LookupByKey(charCreate.CreateInfo.RaceId);
            if (raceEntry == null)
            {
                Log.outError(LogFilter.Network, "Race ({0}) not found in DBC while creating new char for account (ID: {1}): wrong DBC files or cheater?", charCreate.CreateInfo.RaceId, GetAccountId());
                SendCharCreate(ResponseCodes.CharCreateFailed);
                return;
            }

            // prevent character creating Expansion race without Expansion account
            RaceUnlockRequirement raceExpansionRequirement = Global.ObjectMgr.GetRaceUnlockRequirement(charCreate.CreateInfo.RaceId);
            if (raceExpansionRequirement == null)
            {
                Log.outError(LogFilter.Player, "Account {GetAccountId()} tried to create character with unavailable race {charCreate.CreateInfo.RaceId}");
                SendCharCreate(ResponseCodes.AccountCreateFailed);
                return;
            }

            if (raceExpansionRequirement.Expansion > (byte)GetAccountExpansion())
            {
                Log.outError(LogFilter.Player, $"Expansion {GetAccountExpansion()} account:[{GetAccountId()}] tried to Create character with expansion {raceExpansionRequirement.Expansion} race ({charCreate.CreateInfo.RaceId})");
                SendCharCreate(ResponseCodes.CharCreateExpansion);
                return;
            }

            //if (raceExpansionRequirement->AchievementId && !)
            //{
            //    TC_LOG_ERROR("entities.player.cheat", "Expansion %u account:[%d] tried to Create character without achievement %u race (%u)",
            //        GetAccountExpansion(), GetAccountId(), raceExpansionRequirement->AchievementId, charCreate.CreateInfo->Race);
            //    SendCharCreate(CHAR_CREATE_ALLIED_RACE_ACHIEVEMENT);
            //    return;
            //}

            // prevent character creating Expansion class without Expansion account
            var classExpansionRequirement = Global.ObjectMgr.GetClassExpansionRequirement(charCreate.CreateInfo.ClassId);
            if (classExpansionRequirement > GetAccountExpansion())
            {
                Log.outError(LogFilter.Network, $"Expansion {GetAccountExpansion()} account:[{GetAccountId()}] tried to Create character with expansion {classExpansionRequirement} class ({charCreate.CreateInfo.ClassId})");
                SendCharCreate(ResponseCodes.CharCreateExpansionClass);
                return;
            }

            if (!HasPermission(RBACPermissions.SkipCheckCharacterCreationRacemask))
            {
                ulong raceMaskDisabled = WorldConfig.GetUInt64Value(WorldCfg.CharacterCreatingDisabledRacemask);
                if (Convert.ToBoolean((1ul << ((int)charCreate.CreateInfo.RaceId - 1)) & raceMaskDisabled))
                {
                    SendCharCreate(ResponseCodes.CharCreateDisabled);
                    return;
                }
            }

            if (!HasPermission(RBACPermissions.SkipCheckCharacterCreationClassmask))
            {
                int classMaskDisabled = WorldConfig.GetIntValue(WorldCfg.CharacterCreatingDisabledClassmask);
                if (Convert.ToBoolean((1 << ((int)charCreate.CreateInfo.ClassId - 1)) & classMaskDisabled))
                {
                    SendCharCreate(ResponseCodes.CharCreateDisabled);
                    return;
                }
            }

            // prevent character creating with invalid name
            if (!ObjectManager.NormalizePlayerName(ref charCreate.CreateInfo.Name))
            {
                Log.outError(LogFilter.Network, "Account:[{0}] but tried to Create character with empty [name] ", GetAccountId());
                SendCharCreate(ResponseCodes.CharNameNoName);
                return;
            }

            // check name limitations
            ResponseCodes res = ObjectManager.CheckPlayerName(charCreate.CreateInfo.Name, GetSessionDbcLocale(), true);
            if (res != ResponseCodes.CharNameSuccess)
            {
                SendCharCreate(res);
                return;
            }

            if (!HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(charCreate.CreateInfo.Name))
            {
                SendCharCreate(ResponseCodes.CharNameReserved);
                return;
            }

            CharacterCreateInfo createInfo = charCreate.CreateInfo;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
            stmt.AddValue(0, charCreate.CreateInfo.Name);

            _queryProcessor.AddQuery(DB.Characters.AsyncQuery(stmt).WithChainingCallback((queryCallback, result) =>
            {
                if (!result.IsEmpty())
                {
                    SendCharCreate(ResponseCodes.CharCreateNameInUse);
                    return;
                }

                stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_SUM_REALM_CHARACTERS);
                stmt.AddValue(0, GetAccountId());
                queryCallback.SetNextQuery(DB.Login.AsyncQuery(stmt));

            }).WithChainingCallback((queryCallback, result) =>
            {
                ulong acctCharCount = 0;
                if (!result.IsEmpty())
                    acctCharCount = result.Read<ulong>(0);

                if (acctCharCount >= WorldConfig.GetUIntValue(WorldCfg.CharactersPerAccount))
                {
                    SendCharCreate(ResponseCodes.CharCreateAccountLimit);
                    return;
                }

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_SUM_CHARS);
                stmt.AddValue(0, GetAccountId());
                queryCallback.SetNextQuery(DB.Characters.AsyncQuery(stmt));
            }).WithChainingCallback((queryCallback, result) =>
            {
                if (!result.IsEmpty())
                {
                    createInfo.CharCount = (byte)result.Read<ulong>(0); // SQL's COUNT() returns uint64 but it will always be less than uint8.Max

                    if (createInfo.CharCount >= WorldConfig.GetIntValue(WorldCfg.CharactersPerRealm))
                    {
                        SendCharCreate(ResponseCodes.CharCreateServerLimit);
                        return;
                    }
                }

                bool allowTwoSideAccounts = !Global.WorldMgr.IsPvPRealm() || HasPermission(RBACPermissions.TwoSideCharacterCreation);
                int skipCinematics = WorldConfig.GetIntValue(WorldCfg.SkipCinematics);

                void finalizeCharacterCreation(SQLResult result1)
                {
                    bool haveSameRace = false;
                    int demonHunterReqLevel = WorldConfig.GetIntValue(WorldCfg.CharacterCreatingMinLevelForDemonHunter);
                    bool hasDemonHunterReqLevel = (demonHunterReqLevel == 0);
                    bool checkDemonHunterReqs = createInfo.ClassId == Class.DemonHunter && !HasPermission(RBACPermissions.SkipCheckCharacterCreationDemonHunter);

                    if (result1 != null && !result1.IsEmpty())
                    {
                        Team team = Player.TeamForRace(createInfo.RaceId);
                        int freeDemonHunterSlots = WorldConfig.GetIntValue(WorldCfg.DemonHuntersPerRealm);

                        byte accRace = result1.Read<byte>(1);

                        if (checkDemonHunterReqs)
                        {
                            byte accClass = result1.Read<byte>(2);
                            if (accClass == (byte)Class.DemonHunter)
                            {
                                if (freeDemonHunterSlots > 0)
                                    --freeDemonHunterSlots;

                                if (freeDemonHunterSlots == 0)
                                {
                                    SendCharCreate(ResponseCodes.CharCreateFailed);
                                    return;
                                }
                            }

                            if (!hasDemonHunterReqLevel)
                            {
                                byte accLevel = result1.Read<byte>(0);
                                if (accLevel >= demonHunterReqLevel)
                                    hasDemonHunterReqLevel = true;
                            }
                        }

                        // need to check team only for first character
                        // @todo what to if account already has characters of both races?
                        if (!allowTwoSideAccounts)
                        {
                            Team accTeam = 0;
                            if (accRace > 0)
                                accTeam = Player.TeamForRace((Race)accRace);

                            if (accTeam != team)
                            {
                                SendCharCreate(ResponseCodes.CharCreatePvpTeamsViolation);
                                return;
                            }
                        }

                        // search same race for cinematic or same class if need
                        // @todo check if cinematic already shown? (already logged in?; cinematic field)
                        while ((skipCinematics == 1 && !haveSameRace) || createInfo.ClassId == Class.DemonHunter)
                        {
                            if (!result1.NextRow())
                                break;

                            accRace = result1.Read<byte>(1);

                            if (!haveSameRace)
                                haveSameRace = createInfo.RaceId == (Race)accRace;

                            if (checkDemonHunterReqs)
                            {
                                byte acc_class = result1.Read<byte>(2);
                                if (acc_class == (byte)Class.DemonHunter)
                                {
                                    if (freeDemonHunterSlots > 0)
                                        --freeDemonHunterSlots;

                                    if (freeDemonHunterSlots == 0)
                                    {
                                        SendCharCreate(ResponseCodes.CharCreateFailed);
                                        return;
                                    }
                                }

                                if (!hasDemonHunterReqLevel)
                                {
                                    byte acc_level = result1.Read<byte>(0);
                                    if (acc_level >= demonHunterReqLevel)
                                        hasDemonHunterReqLevel = true;
                                }
                            }
                        }
                    }

                    if (checkDemonHunterReqs && !hasDemonHunterReqLevel)
                    {
                        SendCharCreate(ResponseCodes.CharCreateLevelRequirementDemonHunter);
                        return;
                    }

                    Player newChar = new Player(this);
                    newChar.GetMotionMaster().Initialize();
                    if (!newChar.Create(Global.ObjectMgr.GetGenerator(HighGuid.Player).Generate(), createInfo))
                    {
                        // Player not create (race/class/etc problem?)
                        newChar.CleanupsBeforeDelete();

                        SendCharCreate(ResponseCodes.CharCreateError);
                        return;
                    }

                    if ((haveSameRace && skipCinematics == 1) || skipCinematics == 2)
                        newChar.setCinematic(1);                          // not show intro

                    newChar.atLoginFlags = AtLoginFlags.FirstLogin;               // First login

                    // Player created, save it now
                    newChar.SaveToDB(true);
                    createInfo.CharCount += 1;

                    SQLTransaction trans = new SQLTransaction();

                    stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_REALM_CHARACTERS_BY_REALM);
                    stmt.AddValue(0, GetAccountId());
                    stmt.AddValue(1, Global.WorldMgr.GetRealm().Id.Realm);
                    trans.Append(stmt);

                    stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_REALM_CHARACTERS);
                    stmt.AddValue(0, createInfo.CharCount);
                    stmt.AddValue(1, GetAccountId());
                    stmt.AddValue(2, Global.WorldMgr.GetRealm().Id.Realm);
                    trans.Append(stmt);

                    DB.Login.CommitTransaction(trans);

                    // Success
                    SendCharCreate(ResponseCodes.CharCreateSuccess, newChar.GetGUID());

                    Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Create Character: {2} {3}", GetAccountId(), GetRemoteAddress(), createInfo.Name, newChar.GetGUID().ToString());
                    Global.ScriptMgr.OnPlayerCreate(newChar);
                    Global.WorldMgr.AddCharacterInfo(newChar.GetGUID(), GetAccountId(), newChar.GetName(), newChar.GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender), (byte)newChar.GetRace(), (byte)newChar.GetClass(), (byte)newChar.getLevel(), false);

                    newChar.CleanupsBeforeDelete();
                }

                if (!allowTwoSideAccounts || skipCinematics == 1 || createInfo.ClassId == Class.DemonHunter)
                {
                    finalizeCharacterCreation(new SQLResult());
                    return;
                }

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_CREATE_INFO);
                stmt.AddValue(0, GetAccountId());
                stmt.AddValue(1, (skipCinematics == 1 || createInfo.ClassId == Class.DemonHunter) ? 12 : 1);
                queryCallback.WithCallback(finalizeCharacterCreation).SetNextQuery(DB.Characters.AsyncQuery(stmt));
            }));
        }

        [WorldPacketHandler(ClientOpcodes.CharDelete, Status = SessionStatus.Authed)]
        void HandleCharDelete(CharDelete charDelete)
        {
            // can't delete loaded character
            if (Global.ObjAccessor.FindPlayer(charDelete.Guid))
                return;

            // is guild leader
            if (Global.GuildMgr.GetGuildByLeader(charDelete.Guid))
            {
                SendCharDelete(ResponseCodes.CharDeleteFailedGuildLeader);
                return;
            }

            // is arena team captain
            if (Global.ArenaTeamMgr.GetArenaTeamByCaptain(charDelete.Guid) != null)
            {
                SendCharDelete(ResponseCodes.CharDeleteFailedArenaCaptain);
                return;
            }

            CharacterInfo characterInfo = Global.WorldMgr.GetCharacterInfo(charDelete.Guid);
            if (characterInfo == null)
            {
                //Global.ScriptMgr.OnPlayerFailedDelete(charDelete.Guid, initAccountId);
                return;
            }

            uint accountId = characterInfo.AccountId;
            string name = characterInfo.Name;
            byte level = characterInfo.Level;

            // prevent deleting other players' characters using cheating tools
            if (accountId != GetAccountId())
                return;

            string IP_str = GetRemoteAddress();
            Log.outInfo(LogFilter.Player, "Account: {0}, IP: {1} deleted character: {2}, {3}, Level: {4}", accountId, IP_str, name, charDelete.Guid.ToString(), level);
            Global.ScriptMgr.OnPlayerDelete(charDelete.Guid);

            Global.GuildFinderMgr.RemoveAllMembershipRequestsFromPlayer(charDelete.Guid);
            Global.CalendarMgr.RemoveAllPlayerEventsAndInvites(charDelete.Guid);
            Player.DeleteFromDB(charDelete.Guid, accountId);

            SendCharDelete(ResponseCodes.CharDeleteSuccess);
        }

        [WorldPacketHandler(ClientOpcodes.GenerateRandomCharacterName, Status = SessionStatus.Authed)]
        void HandleRandomizeCharName(GenerateRandomCharacterName packet)
        {
            if (!Player.IsValidRace((Race)packet.Race))
            {
                Log.outError(LogFilter.Network, "Invalid race ({0}) sent by accountId: {1}", packet.Race, GetAccountId());
                return;
            }

            if (!Player.IsValidGender((Gender)packet.Sex))
            {
                Log.outError(LogFilter.Network, "Invalid gender ({0}) sent by accountId: {1}", packet.Sex, GetAccountId());
                return;
            }

            GenerateRandomCharacterNameResult result = new GenerateRandomCharacterNameResult();
            result.Success = true;
            result.Name = Global.DB2Mgr.GetNameGenEntry(packet.Race, packet.Sex);

            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.ReorderCharacters, Status = SessionStatus.Authed)]
        void HandleReorderCharacters(ReorderCharacters reorderChars)
        {
            SQLTransaction trans = new SQLTransaction();

            foreach (var reorderInfo in reorderChars.Entries)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_LIST_SLOT);
                stmt.AddValue(0, reorderInfo.NewPosition);
                stmt.AddValue(1, reorderInfo.PlayerGUID.GetCounter());
                stmt.AddValue(2, GetAccountId());
                trans.Append(stmt);
            }

            DB.Characters.CommitTransaction(trans);
        }

        [WorldPacketHandler(ClientOpcodes.PlayerLogin, Status = SessionStatus.Authed)]
        void HandlePlayerLogin(PlayerLogin playerLogin)
        {
            if (PlayerLoading() || GetPlayer() != null)
            {
                Log.outError(LogFilter.Network, "Player tries to login again, AccountId = {0}", GetAccountId());
                return;
            }

            m_playerLoading = playerLogin.Guid;
            Log.outDebug(LogFilter.Network, "Character {0} logging in", playerLogin.Guid.ToString());

            if (!_legitCharacters.Contains(playerLogin.Guid))
            {
                Log.outError(LogFilter.Network, "Account ({0}) can't login with that character ({1}).", GetAccountId(), playerLogin.Guid.ToString());
                KickPlayer();
                return;
            }

            SendConnectToInstance(ConnectToSerial.WorldAttempt1);
        }

        public void HandleContinuePlayerLogin()
        {
            if (!PlayerLoading() || GetPlayer())
            {
                KickPlayer();
                return;
            }

            LoginQueryHolder holder = new LoginQueryHolder(GetAccountId(), m_playerLoading);
            holder.Initialize();

            SendPacket(new ResumeComms(ConnectionType.Instance));

            _charLoginCallback = DB.Characters.DelayQueryHolder(holder);
        }

        public void HandlePlayerLogin(LoginQueryHolder holder)
        {
            ObjectGuid playerGuid = holder.GetGuid();

            Player pCurrChar = new Player(this);
            if (!pCurrChar.LoadFromDB(playerGuid, holder))
            {
                SetPlayer(null);
                KickPlayer();
                m_playerLoading.Clear();
                return;
            }

            pCurrChar.SetUInt32Value(PlayerFields.VirtualRealm, Global.WorldMgr.GetVirtualRealmAddress());

            SendTutorialsData();

            pCurrChar.GetMotionMaster().Initialize();
            pCurrChar.SendDungeonDifficulty();

            LoginVerifyWorld loginVerifyWorld = new LoginVerifyWorld();
            loginVerifyWorld.MapID = (int)pCurrChar.GetMapId();
            loginVerifyWorld.Pos = pCurrChar.GetPosition();
            SendPacket(loginVerifyWorld);

            LoadAccountData(holder.GetResult(PlayerLoginQueryLoad.AccountData), AccountDataTypes.PerCharacterCacheMask);

            AccountDataTimes accountDataTimes = new AccountDataTimes();
            accountDataTimes.PlayerGuid = playerGuid;
            accountDataTimes.ServerTime = (uint)Global.WorldMgr.GetGameTime();
            for (AccountDataTypes i = 0; i < AccountDataTypes.Max; ++i)
                accountDataTimes.AccountTimes[(int)i] = (uint)GetAccountData(i).Time;

            SendPacket(accountDataTimes);

            SendFeatureSystemStatus();

            MOTD motd = new MOTD();
            motd.Text = Global.WorldMgr.GetMotd();
            SendPacket(motd);

            SendSetTimeZoneInformation();

            // Send PVPSeason
            {
                PVPSeason season = new PVPSeason();
                season.PreviousSeason = (WorldConfig.GetUIntValue(WorldCfg.ArenaSeasonId) - (WorldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress) ? 1u : 0u));

                if (WorldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress))
                    season.CurrentSeason = WorldConfig.GetUIntValue(WorldCfg.ArenaSeasonId);

                SendPacket(season);
            }

            SQLResult resultGuild = holder.GetResult(PlayerLoginQueryLoad.Guild);
            if (!resultGuild.IsEmpty())
            {
                pCurrChar.SetInGuild(resultGuild.Read<uint>(0));
                pCurrChar.SetGuildRank(resultGuild.Read<byte>(1));
                Guild guild = Global.GuildMgr.GetGuildById(pCurrChar.GetGuildId());
                if (guild)
                    pCurrChar.SetGuildLevel(guild.GetLevel());
            }
            else if (pCurrChar.GetGuildId() != 0)
            {
                pCurrChar.SetInGuild(0);
                pCurrChar.SetGuildRank(0);
                pCurrChar.SetGuildLevel(0);
            }

            // TODO: Move this to BattlePetMgr::SendJournalLock() just to have all packets in one file
            SendPacket(new BattlePetJournalLockAcquired());

            pCurrChar.SendInitialPacketsBeforeAddToMap();

            //Show cinematic at the first time that player login
            if (pCurrChar.getCinematic() == 0)
            {
                pCurrChar.setCinematic(1);
                ChrClassesRecord cEntry = CliDB.ChrClassesStorage.LookupByKey(pCurrChar.GetClass());
                if (cEntry != null)
                {
                    ChrRacesRecord rEntry = CliDB.ChrRacesStorage.LookupByKey(pCurrChar.GetRace());
                    if (pCurrChar.GetClass() == Class.DemonHunter) // @todo: find a more generic solution
                        pCurrChar.SendMovieStart(469);
                    else if (cEntry.CinematicSequenceID != 0)
                        pCurrChar.SendCinematicStart(cEntry.CinematicSequenceID);
                    else if (rEntry != null)
                        pCurrChar.SendCinematicStart(rEntry.CinematicSequenceID);
                }
            }

            if (!pCurrChar.GetMap().AddPlayerToMap(pCurrChar))
            {
                var at = Global.ObjectMgr.GetGoBackTrigger(pCurrChar.GetMapId());
                if (at != null)
                    pCurrChar.TeleportTo(at.target_mapId, at.target_X, at.target_Y, at.target_Z, pCurrChar.Orientation);
                else
                    pCurrChar.TeleportTo(pCurrChar.GetHomebind());
            }
            Global.ObjAccessor.AddObject(pCurrChar);

            if (pCurrChar.GetGuildId() != 0)
            {
                Guild guild = Global.GuildMgr.GetGuildById(pCurrChar.GetGuildId());
                if (guild)
                    guild.SendLoginInfo(this);
                else
                {
                    // remove wrong guild data
                    Log.outError(LogFilter.Server, "Player {0} ({1}) marked as member of not existing guild (id: {2}), removing guild membership for player.", pCurrChar.GetName(), pCurrChar.GetGUID().ToString(),
                        pCurrChar.GetGuildId());
                    pCurrChar.SetInGuild(0);
                }
            }
            pCurrChar.SendInitialPacketsAfterAddToMap();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_ONLINE);
            stmt.AddValue(0, pCurrChar.GetGUID().GetCounter());
            DB.Characters.Execute(stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_ONLINE);
            stmt.AddValue(0, GetAccountId());
            DB.Login.Execute(stmt);

            pCurrChar.SetInGameTime(Time.GetMSTime());

            // announce group about member online (must be after add to player list to receive announce to self)
            Group group = pCurrChar.GetGroup();
            if (group)
            {
                group.SendUpdate();
                group.ResetMaxEnchantingLevel();
            }

            // friend status
            Global.SocialMgr.SendFriendStatus(pCurrChar, FriendsResult.Online, pCurrChar.GetGUID(), true);

            // Place character in world (and load zone) before some object loading
            pCurrChar.LoadCorpse(holder.GetResult(PlayerLoginQueryLoad.CorpseLocation));

            // setting Ghost+speed if dead
            if (pCurrChar.getDeathState() != DeathState.Alive)
            {
                // not blizz like, we must correctly save and load player instead...
                if (pCurrChar.GetRace() == Race.NightElf && !pCurrChar.HasAura(20584))
                    pCurrChar.CastSpell(pCurrChar, 20584, true);// auras SPELL_AURA_INCREASE_SPEED(+speed in wisp form), SPELL_AURA_INCREASE_SWIM_SPEED(+swim speed in wisp form), SPELL_AURA_TRANSFORM (to wisp form)

                if (!pCurrChar.HasAura(8326))
                    pCurrChar.CastSpell(pCurrChar, 8326, true, null);     // auras SPELL_AURA_GHOST, SPELL_AURA_INCREASE_SPEED(why?), SPELL_AURA_INCREASE_SWIM_SPEED(why?)

                pCurrChar.SetWaterWalking(true);
            }

            pCurrChar.ContinueTaxiFlight();

            // reset for all pets before pet loading
            if (pCurrChar.HasAtLoginFlag(AtLoginFlags.ResetPetTalents))
            {
                // Delete all of the player's pet spells
                PreparedStatement stmtSpells = DB.Characters.GetPreparedStatement(CharStatements.DEL_ALL_PET_SPELLS_BY_OWNER);
                stmtSpells.AddValue(0, pCurrChar.GetGUID().GetCounter());
                DB.Characters.Execute(stmtSpells);

                // Then reset all of the player's pet specualizations
                PreparedStatement stmtSpec = DB.Characters.GetPreparedStatement(CharStatements.UPD_PET_SPECS_BY_OWNER);
                stmtSpec.AddValue(0, pCurrChar.GetGUID().GetCounter());
                DB.Characters.Execute(stmtSpec);
            }

            // Load pet if any (if player not alive and in taxi flight or another then pet will remember as temporary unsummoned)
            pCurrChar.LoadPet();

            // Set FFA PvP for non GM in non-rest mode
            if (Global.WorldMgr.IsFFAPvPRealm() && !pCurrChar.IsGameMaster() && !pCurrChar.HasFlag(PlayerFields.Flags, PlayerFlags.Resting))
                pCurrChar.SetByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.FFAPvp);

            if (pCurrChar.HasFlag(PlayerFields.Flags, PlayerFlags.ContestedPVP))
                pCurrChar.SetContestedPvP();

            // Apply at_login requests
            if (pCurrChar.HasAtLoginFlag(AtLoginFlags.ResetSpells))
            {
                pCurrChar.ResetSpells();
                SendNotification(CypherStrings.ResetSpells);
            }

            if (pCurrChar.HasAtLoginFlag(AtLoginFlags.ResetTalents))
            {
                pCurrChar.ResetTalents(true);
                pCurrChar.ResetTalentSpecialization();
                pCurrChar.SendTalentsInfoData();              // original talents send already in to SendInitialPacketsBeforeAddToMap, resend reset state
                SendNotification(CypherStrings.ResetTalents);
            }

            if (pCurrChar.HasAtLoginFlag(AtLoginFlags.FirstLogin))
            {
                pCurrChar.RemoveAtLoginFlag(AtLoginFlags.FirstLogin);

                PlayerInfo info = Global.ObjectMgr.GetPlayerInfo(pCurrChar.GetRace(), pCurrChar.GetClass());
                foreach (var spellId in info.castSpells)
                    pCurrChar.CastSpell(pCurrChar, spellId, true);
            }

            // show time before shutdown if shutdown planned.
            if (Global.WorldMgr.IsShuttingDown())
                Global.WorldMgr.ShutdownMsg(true, pCurrChar);

            if (WorldConfig.GetBoolValue(WorldCfg.AllTaxiPaths))
                pCurrChar.SetTaxiCheater(true);

            if (pCurrChar.IsGameMaster())
                SendNotification(CypherStrings.GmOn);

            string IP_str = GetRemoteAddress();
            Log.outDebug(LogFilter.Network, "Account: {0} (IP: {1}) Login Character:[{2}] ({3}) Level: {4}",
                GetAccountId(), IP_str, pCurrChar.GetName(), pCurrChar.GetGUID().ToString(), pCurrChar.getLevel());

            if (!pCurrChar.IsStandState() && !pCurrChar.HasUnitState(UnitState.Stunned))
                pCurrChar.SetStandState(UnitStandStateType.Stand);

            m_playerLoading.Clear();

            Global.ScriptMgr.OnPlayerLogin(pCurrChar);
        }

        public void AbortLogin(LoginFailureReason reason)
        {
            if (!PlayerLoading() || GetPlayer())
            {
                KickPlayer();
                return;
            }

            m_playerLoading.Clear();
            SendPacket(new CharacterLoginFailed(reason));
        }

        [WorldPacketHandler(ClientOpcodes.LoadingScreenNotify, Status = SessionStatus.Authed)]
        void HandleLoadScreen(LoadingScreenNotify loadingScreenNotify)
        {
            // TODO: Do something with this packet
        }

        public void SendFeatureSystemStatus()
        {
            FeatureSystemStatus features = new FeatureSystemStatus();

            // START OF DUMMY VALUES
            features.ComplaintStatus = 2;
            features.ScrollOfResurrectionRequestsRemaining = 1;
            features.ScrollOfResurrectionMaxRequestsPerDay = 1;
            features.TwitterPostThrottleLimit = 60;
            features.TwitterPostThrottleCooldown = 20;
            features.CfgRealmID = 2;
            features.CfgRealmRecID = 0;
            features.TokenPollTimeSeconds = 300;
            features.TokenRedeemIndex = 0;
            features.VoiceEnabled = false;
            features.BrowserEnabled = false; // Has to be false, otherwise client will crash if "Customer Support" is opened

            features.EuropaTicketSystemStatus.HasValue = true;
            features.EuropaTicketSystemStatus.Value.ThrottleState.MaxTries = 10;
            features.EuropaTicketSystemStatus.Value.ThrottleState.PerMilliseconds = 60000;
            features.EuropaTicketSystemStatus.Value.ThrottleState.TryCount = 1;
            features.EuropaTicketSystemStatus.Value.ThrottleState.LastResetTimeBeforeNow = 111111;
            features.ComplaintStatus = 0;
            features.TutorialsEnabled = true;
            features.NPETutorialsEnabled = true;
            // END OF DUMMY VALUES

            features.EuropaTicketSystemStatus.Value.TicketsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled);
            features.EuropaTicketSystemStatus.Value.BugsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportBugsEnabled);
            features.EuropaTicketSystemStatus.Value.ComplaintsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportComplaintsEnabled);
            features.EuropaTicketSystemStatus.Value.SuggestionsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportSuggestionsEnabled);

            SendPacket(features);
        }

        [WorldPacketHandler(ClientOpcodes.SetFactionAtWar)]
        void HandleSetFactionAtWar(SetFactionAtWar packet)
        {
            GetPlayer().GetReputationMgr().SetAtWar(packet.FactionIndex, true);
        }

        [WorldPacketHandler(ClientOpcodes.SetFactionNotAtWar)]
        void HandleSetFactionNotAtWar(SetFactionNotAtWar packet)
        {
            GetPlayer().GetReputationMgr().SetAtWar(packet.FactionIndex, false);
        }

        [WorldPacketHandler(ClientOpcodes.Tutorial)]
        void HandleTutorialFlag(TutorialSetFlag packet)
        {
            switch (packet.Action)
            {
                case TutorialAction.Update:
                    {
                        byte index = (byte)(packet.TutorialBit >> 5);
                        if (index >= SharedConst.MaxAccountTutorialValues)
                        {
                            Log.outError(LogFilter.Network, "CMSG_TUTORIAL_FLAG received bad TutorialBit {0}.", packet.TutorialBit);
                            return;
                        }
                        uint flag = GetTutorialInt(index);
                        flag |= (uint)(1 << (int)(packet.TutorialBit & 0x1F));
                        SetTutorialInt(index, flag);
                        break;
                    }
                case TutorialAction.Clear:
                    for (byte i = 0; i < SharedConst.MaxAccountTutorialValues; ++i)
                        SetTutorialInt(i, 0xFFFFFFFF);
                    break;
                case TutorialAction.Reset:
                    for (byte i = 0; i < SharedConst.MaxAccountTutorialValues; ++i)
                        SetTutorialInt(i, 0x00000000);
                    break;
                default:
                    Log.outError(LogFilter.Network, "CMSG_TUTORIAL_FLAG received unknown TutorialAction {0}.", packet.Action);
                    return;
            }
        }

        [WorldPacketHandler(ClientOpcodes.SetWatchedFaction)]
        void HandleSetWatchedFaction(SetWatchedFaction packet)
        {
            GetPlayer().SetInt32Value(ActivePlayerFields.WatchedFactionIndex, (int)packet.FactionIndex);
        }

        [WorldPacketHandler(ClientOpcodes.SetFactionInactive)]
        void HandleSetFactionInactive(SetFactionInactive packet)
        {
            GetPlayer().GetReputationMgr().SetInactive(packet.Index, packet.State);
        }

        [WorldPacketHandler(ClientOpcodes.RequestForcedReactions)]
        void HandleRequestForcedReactions(RequestForcedReactions requestForcedReactions)
        {
            GetPlayer().GetReputationMgr().SendForceReactions();
        }

        [WorldPacketHandler(ClientOpcodes.CharacterRenameRequest, Status = SessionStatus.Authed)]
        void HandleCharRename(CharacterRenameRequest request)
        {
            if (!_legitCharacters.Contains(request.RenameInfo.Guid))
            {
                Log.outError(LogFilter.Network, "Account {0}, IP: {1} tried to rename character {2}, but it does not belong to their account!",
                    GetAccountId(), GetRemoteAddress(), request.RenameInfo.Guid.ToString());
                KickPlayer();
                return;
            }

            // prevent character rename to invalid name
            if (!ObjectManager.NormalizePlayerName(ref request.RenameInfo.NewName))
            {
                SendCharRename(ResponseCodes.CharNameNoName, request.RenameInfo);
                return;
            }

            ResponseCodes res = ObjectManager.CheckPlayerName(request.RenameInfo.NewName, GetSessionDbcLocale(), true);
            if (res != ResponseCodes.CharNameSuccess)
            {
                SendCharRename(res, request.RenameInfo);
                return;
            }

            if (!HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(request.RenameInfo.NewName))
            {
                SendCharRename(ResponseCodes.CharNameReserved, request.RenameInfo);
                return;
            }

            // Ensure that there is no character with the desired new name
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_FREE_NAME);
            stmt.AddValue(0, request.RenameInfo.Guid.GetCounter());
            stmt.AddValue(1, request.RenameInfo.NewName);

            _queryProcessor.AddQuery(DB.Characters.AsyncQuery(stmt).WithCallback(HandleCharRenameCallBack, request.RenameInfo));
        }

        void HandleCharRenameCallBack(CharacterRenameInfo renameInfo, SQLResult result)
        {
            if (result.IsEmpty())
            {
                SendCharRename(ResponseCodes.CharNameFailure, renameInfo);
                return;
            }

            string oldName = result.Read<string>(0);
            // check name limitations
            AtLoginFlags atLoginFlags = (AtLoginFlags)result.Read<uint>(1);
            if (!atLoginFlags.HasAnyFlag(AtLoginFlags.Rename))
            {
                SendCharRename(ResponseCodes.CharCreateError, renameInfo);
                return;
            }
            atLoginFlags &= ~AtLoginFlags.Rename;

            SQLTransaction trans = new SQLTransaction();
            ulong lowGuid = renameInfo.Guid.GetCounter();

            // Update name and at_login flag in the db
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_NAME_AT_LOGIN);
            stmt.AddValue(0, renameInfo.NewName);
            stmt.AddValue(1, atLoginFlags);
            stmt.AddValue(2, lowGuid);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
            stmt.AddValue(0, lowGuid);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] ({3}) Changed name to: {4}",
                GetAccountId(), GetRemoteAddress(), oldName, renameInfo.Guid.ToString(), renameInfo.NewName);

            SendCharRename(ResponseCodes.Success, renameInfo);

            Global.WorldMgr.UpdateCharacterInfo(renameInfo.Guid, renameInfo.NewName);
        }

        [WorldPacketHandler(ClientOpcodes.SetPlayerDeclinedNames, Status = SessionStatus.Authed)]
        void HandleSetPlayerDeclinedNames(SetPlayerDeclinedNames packet)
        {
            // not accept declined names for unsupported languages
            string name;
            if (!ObjectManager.GetPlayerNameByGUID(packet.Player, out name))
            {
                SendSetPlayerDeclinedNamesResult(DeclinedNameResult.Error, packet.Player);
                return;
            }

            if (!char.IsLetter(name[0]))                      // name already stored as only single alphabet using
            {
                SendSetPlayerDeclinedNamesResult(DeclinedNameResult.Error, packet.Player);
                return;
            }

            for (int i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
            {
                string declinedName = packet.DeclinedNames.name[i];
                if (!ObjectManager.NormalizePlayerName(ref declinedName))
                {
                    SendSetPlayerDeclinedNamesResult(DeclinedNameResult.Error, packet.Player);
                    return;
                }
                packet.DeclinedNames.name[i] = declinedName;
            }

            for (int i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
            {
                string declinedName = packet.DeclinedNames.name[i];
                DB.Characters.EscapeString(ref declinedName);
                packet.DeclinedNames.name[i] = declinedName;
            }

            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
            stmt.AddValue(0, packet.Player.GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_DECLINED_NAME);
            stmt.AddValue(0, packet.Player.GetCounter());

            for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                stmt.AddValue(i + 1, packet.DeclinedNames.name[i]);

            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            SendSetPlayerDeclinedNamesResult(DeclinedNameResult.Success, packet.Player);
        }

        [WorldPacketHandler(ClientOpcodes.AlterAppearance)]
        void HandleAlterAppearance(AlterApperance packet)
        {
            BarberShopStyleRecord bs_hair = CliDB.BarberShopStyleStorage.LookupByKey(packet.NewHairStyle);
            if (bs_hair == null || bs_hair.Type != 0 || bs_hair.Race != (byte)GetPlayer().GetRace() || bs_hair.Sex != GetPlayer().GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender))
                return;

            BarberShopStyleRecord bs_facialHair = CliDB.BarberShopStyleStorage.LookupByKey(packet.NewFacialHair);
            if (bs_facialHair == null || bs_facialHair.Type != 2 || bs_facialHair.Race != (byte)GetPlayer().GetRace() || bs_facialHair.Sex != GetPlayer().GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender))
                return;

            BarberShopStyleRecord bs_skinColor = CliDB.BarberShopStyleStorage.LookupByKey(packet.NewSkinColor);
            if (bs_skinColor != null && (bs_skinColor.Type != 3 || bs_skinColor.Race != (byte)GetPlayer().GetRace() || bs_skinColor.Sex != GetPlayer().GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender)))
                return;

            BarberShopStyleRecord bs_face = CliDB.BarberShopStyleStorage.LookupByKey(packet.NewFace);
            if (bs_face != null && (bs_face.Type != 4 || bs_face.Race != (byte)GetPlayer().GetRace() || bs_face.Sex != GetPlayer().GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender)))
                return;

            Array<BarberShopStyleRecord> customDisplayEntries = new Array<BarberShopStyleRecord>(PlayerConst.CustomDisplaySize);
            Array<byte> customDisplay = new Array<byte>(PlayerConst.CustomDisplaySize);
            for (int i = 0; i < PlayerConst.CustomDisplaySize; ++i)
            {
                BarberShopStyleRecord bs_customDisplay = CliDB.BarberShopStyleStorage.LookupByKey(packet.NewCustomDisplay[i]);
                if (bs_customDisplay != null && (bs_customDisplay.Type != 5 + i || bs_customDisplay.Race != (byte)_player.GetRace() || bs_customDisplay.Sex != _player.GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender)))
                    return;

                customDisplayEntries[i] = bs_customDisplay;
                customDisplay[i] = (byte)(bs_customDisplay != null ? bs_customDisplay.Data : 0);
            }

            if (!Player.ValidateAppearance(GetPlayer().GetRace(), GetPlayer().GetClass(), (Gender)GetPlayer().GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender),
                bs_hair.Data, (byte)packet.NewHairColor, bs_face != null ? bs_face.Data : GetPlayer().GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetFaceId),
                bs_facialHair.Data, (bs_skinColor != null ? bs_skinColor.Data : GetPlayer().GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetSkinId)), customDisplay))
                return;

            GameObject go = GetPlayer().FindNearestGameObjectOfType(GameObjectTypes.BarberChair, 5.0f);
            if (!go)
            {
                SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NotOnChair));
                return;
            }

            if (GetPlayer().GetStandState() != (UnitStandStateType)((int)UnitStandStateType.SitLowChair + go.GetGoInfo().BarberChair.chairheight))
            {
                SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NotOnChair));
                return;
            }

            uint cost = GetPlayer().GetBarberShopCost(bs_hair, packet.NewHairColor, bs_facialHair, bs_skinColor, bs_face, customDisplayEntries);
            if (!GetPlayer().HasEnoughMoney((ulong)cost))
            {
                SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NoMoney));
                return;
            }

            SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.Success));

            _player.ModifyMoney(-cost);
            _player.UpdateCriteria(CriteriaTypes.GoldSpentAtBarber, cost);

            _player.SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairStyleId, bs_hair.Data);
            _player.SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairColorId, (byte)packet.NewHairColor);
            _player.SetByteValue(PlayerFields.Bytes2, PlayerFieldOffsets.Bytes2OffsetFacialStyle, bs_facialHair.Data);
            if (bs_skinColor != null)
                GetPlayer().SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetSkinId, bs_skinColor.Data);
            if (bs_face != null)
                _player.SetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetFaceId, bs_face.Data);

            for (int i = 0; i < PlayerConst.CustomDisplaySize; ++i)
                _player.SetByteValue(PlayerFields.Bytes2, (byte)(PlayerFieldOffsets.Bytes2OffsetCustomDisplayOption + i), customDisplay[i]);

            _player.UpdateCriteria(CriteriaTypes.VisitBarberShop, 1);

            _player.SetStandState(UnitStandStateType.Stand);
        }

        [WorldPacketHandler(ClientOpcodes.CharCustomize, Status = SessionStatus.Authed)]
        void HandleCharCustomize(CharCustomize packet)
        {
            if (!_legitCharacters.Contains(packet.CustomizeInfo.CharGUID))
            {
                Log.outError(LogFilter.Network, "Account {0}, IP: {1} tried to customise {2}, but it does not belong to their account!",
                    GetAccountId(), GetRemoteAddress(), packet.CustomizeInfo.CharGUID.ToString());
                KickPlayer();
                return;
            }

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_CUSTOMIZE_INFO);
            stmt.AddValue(0, packet.CustomizeInfo.CharGUID.GetCounter());

            _queryProcessor.AddQuery(DB.Characters.AsyncQuery(stmt).WithCallback(HandleCharCustomizeCallback, packet.CustomizeInfo));
        }

        void HandleCharCustomizeCallback(CharCustomizeInfo customizeInfo, SQLResult result)
        {
            if (result.IsEmpty())
            {
                SendCharCustomize(ResponseCodes.CharCreateError, customizeInfo);
                return;
            }

            string oldName = result.Read<string>(0);
            byte plrRace = result.Read<byte>(1);
            byte plrClass = result.Read<byte>(2);
            byte plrGender = result.Read<byte>(3);
            AtLoginFlags atLoginFlags = (AtLoginFlags)result.Read<ushort>(4);

            if (!Player.ValidateAppearance((Race)plrRace, (Class)plrClass, (Gender)plrGender, customizeInfo.HairStyleID, customizeInfo.HairColorID, customizeInfo.FaceID, 
                customizeInfo.FacialHairStyleID, customizeInfo.SkinID, customizeInfo.CustomDisplay))
            {
                SendCharCustomize(ResponseCodes.CharCreateError, customizeInfo);
                return;
            }

            if (!atLoginFlags.HasAnyFlag(AtLoginFlags.Customize))
            {
                SendCharCustomize(ResponseCodes.CharCreateError, customizeInfo);
                return;
            }

            // prevent character rename
            if (WorldConfig.GetBoolValue(WorldCfg.PreventRenameCustomization) && (customizeInfo.CharName != oldName))
            {
                SendCharCustomize(ResponseCodes.CharNameFailure, customizeInfo);
                return;
            }

            atLoginFlags &= ~AtLoginFlags.Customize;

            // prevent character rename to invalid name
            if (!ObjectManager.NormalizePlayerName(ref customizeInfo.CharName))
            {
                SendCharCustomize(ResponseCodes.CharNameNoName, customizeInfo);
                return;
            }

            ResponseCodes res = ObjectManager.CheckPlayerName(customizeInfo.CharName, GetSessionDbcLocale(), true);
            if (res != ResponseCodes.CharNameSuccess)
            {
                SendCharCustomize(res, customizeInfo);
                return;
            }

            // check name limitations
            if (!HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(customizeInfo.CharName))
            {
                SendCharCustomize(ResponseCodes.CharNameReserved, customizeInfo);
                return;
            }

            // character with this name already exist
            // @todo: make async
            ObjectGuid newGuid = ObjectManager.GetPlayerGUIDByName(customizeInfo.CharName);
            if (!newGuid.IsEmpty())
            {
                if (newGuid != customizeInfo.CharGUID)
                {
                    SendCharCustomize(ResponseCodes.CharCreateNameInUse, customizeInfo);
                    return;
                }
            }

            PreparedStatement stmt;
            SQLTransaction trans = new SQLTransaction();
            ulong lowGuid = customizeInfo.CharGUID.GetCounter();

            // Customize
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GENDER_AND_APPEARANCE);

                stmt.AddValue(0, customizeInfo.SexID);
                stmt.AddValue(1, customizeInfo.SkinID);
                stmt.AddValue(2, customizeInfo.FaceID);
                stmt.AddValue(3, customizeInfo.HairStyleID);
                stmt.AddValue(4, customizeInfo.HairColorID);
                stmt.AddValue(5, customizeInfo.FacialHairStyleID);
                stmt.AddValue(6, customizeInfo.CustomDisplay[0]);
                stmt.AddValue(7, customizeInfo.CustomDisplay[1]);
                stmt.AddValue(8, customizeInfo.CustomDisplay[2]);
                stmt.AddValue(9, lowGuid);

                trans.Append(stmt);
            }

            // Name Change and update atLogin flags
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_NAME_AT_LOGIN);
                stmt.AddValue(0, customizeInfo.CharName);
                stmt.AddValue(1, atLoginFlags);
                stmt.AddValue(2, lowGuid);
                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
                stmt.AddValue(0, lowGuid);

                trans.Append(stmt);
            }

            DB.Characters.CommitTransaction(trans);

            Global.WorldMgr.UpdateCharacterInfo(customizeInfo.CharGUID, customizeInfo.CharName, customizeInfo.SexID);

            SendCharCustomize(ResponseCodes.Success, customizeInfo);

            Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}), Character[{2}] ({3}) Customized to: {4}",
                GetAccountId(), GetRemoteAddress(), oldName, customizeInfo.CharGUID.ToString(), customizeInfo.CharName);
        }

        [WorldPacketHandler(ClientOpcodes.SaveEquipmentSet)]
        void HandleEquipmentSetSave(SaveEquipmentSet saveEquipmentSet)
        {
            if (saveEquipmentSet.Set.SetID >= ItemConst.MaxEquipmentSetIndex) // client set slots amount
                return;

            if (saveEquipmentSet.Set.Type > EquipmentSetInfo.EquipmentSetType.Transmog)
                return;

            for (byte i = 0; i < EquipmentSlot.End; ++i)
            {
                if (!Convert.ToBoolean(saveEquipmentSet.Set.IgnoreMask & (1 << i)))
                {
                    if (saveEquipmentSet.Set.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
                    {
                        saveEquipmentSet.Set.Appearances[i] = 0;

                        ObjectGuid itemGuid = saveEquipmentSet.Set.Pieces[i];
                        if (!itemGuid.IsEmpty())
                        {
                            Item item = _player.GetItemByPos(InventorySlots.Bag0, i);

                            // cheating check 1 (item equipped but sent empty guid)
                            if (!item)
                                return;

                            // cheating check 2 (sent guid does not match equipped item)
                            if (item.GetGUID() != itemGuid)
                                return;
                        }
                        else
                            saveEquipmentSet.Set.IgnoreMask |= 1u << i;
                    }
                    else
                    {
                        saveEquipmentSet.Set.Pieces[i].Clear();
                        if (saveEquipmentSet.Set.Appearances[i] != 0)
                        {
                            if (!CliDB.ItemModifiedAppearanceStorage.ContainsKey(saveEquipmentSet.Set.Appearances[i]))
                                return;

                            (bool hasAppearance, bool isTemporary) = GetCollectionMgr().HasItemAppearance((uint)saveEquipmentSet.Set.Appearances[i]);
                            if (!hasAppearance)
                                return;
                        }
                        else
                            saveEquipmentSet.Set.IgnoreMask |= 1u << i;
                    }
                }
                else
                {
                    saveEquipmentSet.Set.Pieces[i].Clear();
                    saveEquipmentSet.Set.Appearances[i] = 0;
                }
            }
            saveEquipmentSet.Set.IgnoreMask &= 0x7FFFF; // clear invalid bits (i > EQUIPMENT_SLOT_END)
            if (saveEquipmentSet.Set.Type == EquipmentSetInfo.EquipmentSetType.Equipment)
            {
                saveEquipmentSet.Set.Enchants[0] = 0;
                saveEquipmentSet.Set.Enchants[1] = 0;
            }
            else
            {
                var validateIllusion = new Func<uint, bool>(enchantId =>
                {
                    SpellItemEnchantmentRecord illusion = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchantId);
                    if (illusion == null)
                        return false;

                    if (illusion.ItemVisual == 0 || !illusion.Flags.HasAnyFlag(EnchantmentSlotMask.Collectable))
                        return false;

                    PlayerConditionRecord condition = CliDB.PlayerConditionStorage.LookupByKey(illusion.TransmogPlayerConditionID);
                    if (condition != null)
                        if (!ConditionManager.IsPlayerMeetingCondition(_player, condition))
                            return false;

                    if (illusion.ScalingClassRestricted > 0 && illusion.ScalingClassRestricted != (byte)_player.GetClass())
                        return false;

                    return true;
                });

                if (saveEquipmentSet.Set.Enchants[0] != 0 && !validateIllusion((uint)saveEquipmentSet.Set.Enchants[0]))
                    return;

                if (saveEquipmentSet.Set.Enchants[1] != 0 && !validateIllusion((uint)saveEquipmentSet.Set.Enchants[1]))
                    return;
            }

            GetPlayer().SetEquipmentSet(saveEquipmentSet.Set);
        }

        [WorldPacketHandler(ClientOpcodes.DeleteEquipmentSet)]
        void HandleDeleteEquipmentSet(DeleteEquipmentSet packet)
        {
            GetPlayer().DeleteEquipmentSet(packet.ID);
        }

        [WorldPacketHandler(ClientOpcodes.UseEquipmentSet)]
        void HandleUseEquipmentSet(UseEquipmentSet useEquipmentSet)
        {
            ObjectGuid ignoredItemGuid = new ObjectGuid(0x0C00040000000000, 0xFFFFFFFFFFFFFFFF);
            for (byte i = 0; i < EquipmentSlot.End; ++i)
            {
                Log.outDebug(LogFilter.Player, "{0}: ContainerSlot: {1}, Slot: {2}", useEquipmentSet.Items[i].Item.ToString(), useEquipmentSet.Items[i].ContainerSlot, useEquipmentSet.Items[i].Slot);

                // check if item slot is set to "ignored" (raw value == 1), must not be unequipped then
                if (useEquipmentSet.Items[i].Item == ignoredItemGuid)
                    continue;

                // Only equip weapons in combat
                if (GetPlayer().IsInCombat() && i != EquipmentSlot.MainHand && i != EquipmentSlot.OffHand)
                    continue;

                Item item = GetPlayer().GetItemByGuid(useEquipmentSet.Items[i].Item);

                ushort dstPos = (ushort)(i | (InventorySlots.Bag0 << 8));
                if (!item)
                {
                    Item uItem = GetPlayer().GetItemByPos(InventorySlots.Bag0, i);
                    if (!uItem)
                        continue;

                    List<ItemPosCount> itemPosCount = new List<ItemPosCount>();
                    InventoryResult inventoryResult = GetPlayer().CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, itemPosCount, uItem, false);
                    if (inventoryResult == InventoryResult.Ok)
                    {
                        GetPlayer().RemoveItem(InventorySlots.Bag0, i, true);
                        GetPlayer().StoreItem(itemPosCount, uItem, true);
                    }
                    else
                        GetPlayer().SendEquipError(inventoryResult, uItem);

                    continue;
                }

                if (item.GetPos() == dstPos)
                    continue;

                GetPlayer().SwapItem(item.GetPos(), dstPos);
            }

            UseEquipmentSetResult result = new UseEquipmentSetResult();
            result.GUID = useEquipmentSet.GUID;
            result.Reason = 0; // 4 - equipment swap failed - inventory is full
            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.CharRaceOrFactionChange, Status = SessionStatus.Authed)]
        void HandleCharRaceOrFactionChange(CharRaceOrFactionChange packet)
        {
            if (!_legitCharacters.Contains(packet.RaceOrFactionChangeInfo.Guid))
            {
                Log.outError(LogFilter.Network, "Account {0}, IP: {1} tried to factionchange character {2}, but it does not belong to their account!",
                    GetAccountId(), GetRemoteAddress(), packet.RaceOrFactionChangeInfo.Guid.ToString());
                KickPlayer();
                return;
            }

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_RACE_OR_FACTION_CHANGE_INFOS);
            stmt.AddValue(0, packet.RaceOrFactionChangeInfo.Guid.GetCounter());

            _queryProcessor.AddQuery(DB.Characters.AsyncQuery(stmt).WithCallback(HandleCharRaceOrFactionChangeCallback, packet.RaceOrFactionChangeInfo));
        }

        void HandleCharRaceOrFactionChangeCallback(CharRaceOrFactionChangeInfo factionChangeInfo, SQLResult result)
        {
            if (result.IsEmpty())
            {
                SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);
                return;
            }

            // get the players old (at this moment current) race
            CharacterInfo characterInfo = Global.WorldMgr.GetCharacterInfo(factionChangeInfo.Guid);
            if (characterInfo == null)
            {
                SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);
                return;
            }

            string oldName = characterInfo.Name;
            Race oldRace = characterInfo.RaceID;
            Class playerClass = characterInfo.ClassID;
            byte level = characterInfo.Level;

            if (Global.ObjectMgr.GetPlayerInfo(factionChangeInfo.RaceID, playerClass) == null)
            {
                SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);
                return;
            }

            AtLoginFlags atLoginFlags = (AtLoginFlags)result.Read<ushort>(0);
            string knownTitlesStr = result.Read<string>(1);

            AtLoginFlags usedLoginFlag = (factionChangeInfo.FactionChange ? AtLoginFlags.ChangeFaction : AtLoginFlags.ChangeRace);
            if (!atLoginFlags.HasAnyFlag(usedLoginFlag))
            {
                SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);
                return;
            }

            uint newTeamId = Player.TeamIdForRace(factionChangeInfo.RaceID);
            if (newTeamId == TeamId.Neutral)
            {
                SendCharFactionChange(ResponseCodes.CharCreateRestrictedRaceclass, factionChangeInfo);
                return;
            }

            if (factionChangeInfo.FactionChange == (Player.TeamIdForRace(oldRace) == newTeamId))
            {
                SendCharFactionChange(factionChangeInfo.FactionChange ? ResponseCodes.CharCreateCharacterSwapFaction : ResponseCodes.CharCreateCharacterRaceOnly, factionChangeInfo);
                return;
            }

            if (!HasPermission(RBACPermissions.SkipCheckCharacterCreationRacemask))
            {
                ulong raceMaskDisabled = WorldConfig.GetUInt64Value(WorldCfg.CharacterCreatingDisabledRacemask);
                if (Convert.ToBoolean(1ul << ((int)factionChangeInfo.RaceID - 1) & raceMaskDisabled))
                {
                    SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);
                    return;
                }
            }

            // prevent character rename
            if (WorldConfig.GetBoolValue(WorldCfg.PreventRenameCustomization) && (factionChangeInfo.Name != oldName))
            {
                SendCharFactionChange(ResponseCodes.CharNameFailure, factionChangeInfo);
                return;
            }

            // prevent character rename to invalid name
            if (!ObjectManager.NormalizePlayerName(ref factionChangeInfo.Name))
            {
                SendCharFactionChange(ResponseCodes.CharNameNoName, factionChangeInfo);
                return;
            }

            ResponseCodes res = ObjectManager.CheckPlayerName(factionChangeInfo.Name, GetSessionDbcLocale(), true);
            if (res != ResponseCodes.CharNameSuccess)
            {
                SendCharFactionChange(res, factionChangeInfo);
                return;
            }

            // check name limitations
            if (!HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(factionChangeInfo.Name))
            {
                SendCharFactionChange(ResponseCodes.CharNameReserved, factionChangeInfo);
                return;
            }

            // character with this name already exist
            ObjectGuid newGuid = ObjectManager.GetPlayerGUIDByName(factionChangeInfo.Name);
            if (!newGuid.IsEmpty())
            {
                if (newGuid != factionChangeInfo.Guid)
                {
                    SendCharFactionChange(ResponseCodes.CharCreateNameInUse, factionChangeInfo);
                    return;
                }
            }

            if (Global.ArenaTeamMgr.GetArenaTeamByCaptain(factionChangeInfo.Guid) != null)
            {
                SendCharFactionChange(ResponseCodes.CharCreateCharacterArenaLeader, factionChangeInfo);
                return;
            }

            // All checks are fine, deal with race change now
            ulong lowGuid = factionChangeInfo.Guid.GetCounter();

            PreparedStatement stmt;
            SQLTransaction trans = new SQLTransaction();

            // resurrect the character in case he's dead
            Player.OfflineResurrect(factionChangeInfo.Guid, trans);

            // Name Change and update atLogin flags
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_NAME_AT_LOGIN);
                stmt.AddValue(0, factionChangeInfo.Name);
                stmt.AddValue(1, (ushort)((atLoginFlags | AtLoginFlags.Resurrect) & ~usedLoginFlag));
                stmt.AddValue(2, lowGuid);

                trans.Append(stmt);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
                stmt.AddValue(0, lowGuid);

                trans.Append(stmt);
            }

            // Customize
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GENDER_AND_APPEARANCE);
                stmt.AddValue(0, factionChangeInfo.SexID);
                stmt.AddValue(1, factionChangeInfo.SkinID);
                stmt.AddValue(2, factionChangeInfo.FaceID);
                stmt.AddValue(3, factionChangeInfo.HairStyleID);
                stmt.AddValue(4, factionChangeInfo.HairColorID);
                stmt.AddValue(5, factionChangeInfo.FacialHairStyleID);
                stmt.AddValue(6, factionChangeInfo.CustomDisplay[0]);
                stmt.AddValue(7, factionChangeInfo.CustomDisplay[1]);
                stmt.AddValue(8, factionChangeInfo.CustomDisplay[2]);
                stmt.AddValue(9, lowGuid);

                trans.Append(stmt);
            }

            // Race Change
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_RACE);
                stmt.AddValue(0, factionChangeInfo.RaceID);
                stmt.AddValue(1, lowGuid);

                trans.Append(stmt);
            }

            Global.WorldMgr.UpdateCharacterInfo(factionChangeInfo.Guid, factionChangeInfo.Name, factionChangeInfo.SexID, factionChangeInfo.RaceID);

            if (oldRace != factionChangeInfo.RaceID)
            {
                // Switch Languages
                // delete all languages first
                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SKILL_LANGUAGES);
                stmt.AddValue(0, lowGuid);
                trans.Append(stmt);

                // Now add them back
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SKILL_LANGUAGE);
                stmt.AddValue(0, lowGuid);

                // Faction specific languages
                if (newTeamId == TeamId.Horde)
                    stmt.AddValue(1, 109);
                else
                    stmt.AddValue(1, 98);

                trans.Append(stmt);

                // Race specific languages
                if (factionChangeInfo.RaceID != Race.Orc && factionChangeInfo.RaceID != Race.Human)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_SKILL_LANGUAGE);
                    stmt.AddValue(0, lowGuid);

                    switch (factionChangeInfo.RaceID)
                    {
                        case Race.Dwarf:
                            stmt.AddValue(1, 111);
                            break;
                        case Race.Draenei:
                        case Race.LightforgedDraenei:
                            stmt.AddValue(1, 759);
                            break;
                        case Race.Gnome:
                            stmt.AddValue(1, 313);
                            break;
                        case Race.NightElf:
                            stmt.AddValue(1, 113);
                            break;
                        case Race.Worgen:
                            stmt.AddValue(1, 791);
                            break;
                        case Race.Undead:
                            stmt.AddValue(1, 673);
                            break;
                        case Race.Tauren:
                        case Race.HighmountainTauren:
                            stmt.AddValue(1, 115);
                            break;
                        case Race.Troll:
                            stmt.AddValue(1, 315);
                            break;
                        case Race.BloodElf:
                        case Race.VoidElf:
                            stmt.AddValue(1, 137);
                            break;
                        case Race.Goblin:
                            stmt.AddValue(1, 792);
                            break;
                        case Race.Nightborne:
                            stmt.AddValue(1, 2464);
                            break;
                        default:
                            Log.outError(LogFilter.Player, $"Could not find language data for race ({factionChangeInfo.RaceID}).");
                            SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);
                            return;
                    }

                    trans.Append(stmt);
                }

                // Team Conversation
                if (factionChangeInfo.FactionChange)
                {
                    // Delete all Flypaths
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_TAXI_PATH);
                    stmt.AddValue(0, lowGuid);
                    trans.Append(stmt);

                    if (level > 7)
                    {
                        // Update Taxi path
                        // this doesn't seem to be 100% blizzlike... but it can't really be helped.
                        string taximaskstream = "";


                        var factionMask = newTeamId == TeamId.Horde ? CliDB.HordeTaxiNodesMask : CliDB.AllianceTaxiNodesMask;
                        for (int i = 0; i < PlayerConst.TaxiMaskSize; ++i)
                        {
                            // i = (315 - 1) / 8 = 39
                            // m = 1 << ((315 - 1) % 8) = 4
                            int deathKnightExtraNode = playerClass != Class.Deathknight || i != 39 ? 0 : 4;
                            taximaskstream += (uint)(factionMask[i] | deathKnightExtraNode) + ' ';
                        }

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_TAXIMASK);
                        stmt.AddValue(0, taximaskstream);
                        stmt.AddValue(1, lowGuid);
                        trans.Append(stmt);
                    }

                    // @todo: make this part asynch
                    if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild))
                    {
                        // Reset guild
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_MEMBER);
                        stmt.AddValue(0, lowGuid);

                        result = DB.Characters.Query(stmt);
                        if (!result.IsEmpty())
                        {
                            Guild guild = Global.GuildMgr.GetGuildById(result.Read<ulong>(0));
                            if (guild)
                                guild.DeleteMember(trans, factionChangeInfo.Guid, false, false, true);
                        }

                        Player.LeaveAllArenaTeams(factionChangeInfo.Guid);
                    }

                    if (!HasPermission(RBACPermissions.TwoSideAddFriend))
                    {
                        // Delete Friend List
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_GUID);
                        stmt.AddValue(0, lowGuid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_FRIEND);
                        stmt.AddValue(0, lowGuid);
                        trans.Append(stmt);
                    }

                    // Reset homebind and position
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
                    stmt.AddValue(0, lowGuid);
                    trans.Append(stmt);

                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_PLAYER_HOMEBIND);
                    stmt.AddValue(0, lowGuid);

                    WorldLocation loc;
                    ushort zoneId = 0;
                    if (newTeamId == TeamId.Alliance)
                    {
                        loc = new WorldLocation(0, -8867.68f, 673.373f, 97.9034f, 0.0f);
                        zoneId = 1519;
                    }
                    else
                    {
                        loc = new WorldLocation(1, 1633.33f, -4439.11f, 15.7588f, 0.0f);
                        zoneId = 1637;
                    }

                    stmt.AddValue(1, loc.GetMapId());
                    stmt.AddValue(2, zoneId);
                    stmt.AddValue(3, loc.GetPositionX());
                    stmt.AddValue(4, loc.GetPositionY());
                    stmt.AddValue(5, loc.GetPositionZ());
                    trans.Append(stmt);

                    Player.SavePositionInDB(loc, zoneId, factionChangeInfo.Guid, trans);

                    // Achievement conversion
                    foreach (var it in Global.ObjectMgr.FactionChangeAchievements)
                    {
                        uint achiev_alliance = it.Key;
                        uint achiev_horde = it.Value;

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_BY_ACHIEVEMENT);
                        stmt.AddValue(0, (ushort)(newTeamId == TeamId.Alliance ? achiev_alliance : achiev_horde));
                        stmt.AddValue(1, lowGuid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_ACHIEVEMENT);
                        stmt.AddValue(0, (ushort)(newTeamId == TeamId.Alliance ? achiev_alliance : achiev_horde));
                        stmt.AddValue(1, (ushort)(newTeamId == TeamId.Alliance ? achiev_horde : achiev_alliance));
                        stmt.AddValue(2, lowGuid);
                        trans.Append(stmt);
                    }

                    // Item conversion
                    foreach (var it in Global.ObjectMgr.FactionChangeItems)
                    {
                        uint item_alliance = it.Key;
                        uint item_horde = it.Value;

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_INVENTORY_FACTION_CHANGE);
                        stmt.AddValue(0, (newTeamId == TeamId.Alliance ? item_alliance : item_horde));
                        stmt.AddValue(1, (newTeamId == TeamId.Alliance ? item_horde : item_alliance));
                        stmt.AddValue(2, lowGuid);
                        trans.Append(stmt);
                    }

                    // Delete all current quests
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS);
                    stmt.AddValue(0, lowGuid);
                    trans.Append(stmt);

                    // Quest conversion
                    foreach (var it in Global.ObjectMgr.FactionChangeQuests)
                    {
                        uint quest_alliance = it.Key;
                        uint quest_horde = it.Value;

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED_BY_QUEST);
                        stmt.AddValue(0, lowGuid);
                        stmt.AddValue(1, (newTeamId == TeamId.Alliance ? quest_alliance : quest_horde));
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_FACTION_CHANGE);
                        stmt.AddValue(0, (newTeamId == TeamId.Alliance ? quest_alliance : quest_horde));
                        stmt.AddValue(1, (newTeamId == TeamId.Alliance ? quest_horde : quest_alliance));
                        stmt.AddValue(2, lowGuid);
                        trans.Append(stmt);
                    }

                    // Mark all rewarded quests as "active" (will count for completed quests achievements)
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_ACTIVE);
                    stmt.AddValue(0, lowGuid);
                    trans.Append(stmt);

                    // Disable all old-faction specific quests
                    {
                        var questTemplates = Global.ObjectMgr.GetQuestTemplates();
                        foreach (Quest quest in questTemplates.Values)
                        {
                            long newRaceMask = (long)(newTeamId == TeamId.Alliance ? Race.RaceMaskAlliance : Race.RaceMaskHorde);
                            if (quest.AllowableRaces != -1 && !Convert.ToBoolean(quest.AllowableRaces & newRaceMask))
                            {
                                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_ACTIVE_BY_QUEST);
                                stmt.AddValue(0, lowGuid);
                                stmt.AddValue(1, quest.Id);
                                trans.Append(stmt);
                            }
                        }
                    }

                    // Spell conversion
                    foreach (var it in Global.ObjectMgr.FactionChangeSpells)
                    {
                        uint spell_alliance = it.Key;
                        uint spell_horde = it.Value;

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_BY_SPELL);
                        stmt.AddValue(0, (newTeamId == TeamId.Alliance ? spell_alliance : spell_horde));
                        stmt.AddValue(1, lowGuid);
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_SPELL_FACTION_CHANGE);
                        stmt.AddValue(0, (newTeamId == TeamId.Alliance ? spell_alliance : spell_horde));
                        stmt.AddValue(1, (newTeamId == TeamId.Alliance ? spell_horde : spell_alliance));
                        stmt.AddValue(2, lowGuid);
                        trans.Append(stmt);
                    }

                    // Reputation conversion
                    foreach (var it in Global.ObjectMgr.FactionChangeReputation)
                    {
                        uint reputation_alliance = it.Key;
                        uint reputation_horde = it.Value;
                        uint newReputation = (newTeamId == TeamId.Alliance) ? reputation_alliance : reputation_horde;
                        uint oldReputation = (newTeamId == TeamId.Alliance) ? reputation_horde : reputation_alliance;

                        // select old standing set in db
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_REP_BY_FACTION);
                        stmt.AddValue(0, oldReputation);
                        stmt.AddValue(1, lowGuid);

                        result = DB.Characters.Query(stmt);
                        if (!result.IsEmpty())
                        {
                            int oldDBRep = result.Read<int>(0);
                            FactionRecord factionEntry = CliDB.FactionStorage.LookupByKey(oldReputation);

                            // old base reputation
                            int oldBaseRep = ReputationMgr.GetBaseReputationOf(factionEntry, oldRace, playerClass);

                            // new base reputation
                            int newBaseRep = ReputationMgr.GetBaseReputationOf(CliDB.FactionStorage.LookupByKey(newReputation), factionChangeInfo.RaceID, playerClass);

                            // final reputation shouldnt change
                            int FinalRep = oldDBRep + oldBaseRep;
                            int newDBRep = FinalRep - newBaseRep;

                            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_REP_BY_FACTION);
                            stmt.AddValue(0, newReputation);
                            stmt.AddValue(1, lowGuid);
                            trans.Append(stmt);

                            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_REP_FACTION_CHANGE);
                            stmt.AddValue(0, (ushort)newReputation);
                            stmt.AddValue(1, newDBRep);
                            stmt.AddValue(2, (ushort)oldReputation);
                            stmt.AddValue(3, lowGuid);
                            trans.Append(stmt);
                        }
                    }

                    // Title conversion
                    if (!string.IsNullOrEmpty(knownTitlesStr))
                    {
                        uint ktcount = PlayerConst.KnowTitlesSize * 2;
                        uint[] knownTitles = new uint[ktcount];

                        var tokens = new StringArray(knownTitlesStr, ' ');
                        if (tokens.Length != ktcount)
                        {
                            SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);
                            return;
                        }

                        for (int index = 0; index < ktcount; ++index)
                        {
                            if (uint.TryParse(tokens[index], out uint id))
                                knownTitles[index] = id;
                        }

                        foreach (var it in Global.ObjectMgr.FactionChangeTitles)
                        {
                            uint title_alliance = it.Key;
                            uint title_horde = it.Value;

                            CharTitlesRecord atitleInfo = CliDB.CharTitlesStorage.LookupByKey(title_alliance);
                            CharTitlesRecord htitleInfo = CliDB.CharTitlesStorage.LookupByKey(title_horde);
                            // new team
                            if (newTeamId == TeamId.Alliance)
                            {
                                uint maskID = htitleInfo.MaskID;
                                uint index = maskID / 32;
                                uint old_flag = (uint)(1 << (int)(maskID % 32));
                                uint new_flag = (uint)(1 << (int)(atitleInfo.MaskID % 32));
                                if (Convert.ToBoolean(knownTitles[index] & old_flag))
                                {
                                    knownTitles[index] &= ~old_flag;
                                    // use index of the new title
                                    knownTitles[atitleInfo.MaskID / 32] |= new_flag;
                                }
                            }
                            else
                            {
                                uint maskID = atitleInfo.MaskID;
                                uint index = maskID / 32;
                                uint old_flag = (uint)(1 << (int)(maskID % 32));
                                uint new_flag = (uint)(1 << (int)(htitleInfo.MaskID % 32));
                                if (Convert.ToBoolean(knownTitles[index] & old_flag))
                                {
                                    knownTitles[index] &= ~old_flag;
                                    // use index of the new title
                                    knownTitles[htitleInfo.MaskID / 32] |= new_flag;
                                }
                            }

                            string ss = "";
                            for (uint index = 0; index < ktcount; ++index)
                                ss += knownTitles[index] + ' ';

                            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_TITLES_FACTION_CHANGE);
                            stmt.AddValue(0, ss);
                            stmt.AddValue(1, lowGuid);
                            trans.Append(stmt);

                            // unset any currently chosen title
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.RES_CHAR_TITLES_FACTION_CHANGE);
                            stmt.AddValue(0, lowGuid);
                            trans.Append(stmt);
                        }
                    }
                }
            }

            DB.Characters.CommitTransaction(trans);

            Log.outDebug(LogFilter.Player, "{0} (IP: {1}) changed race from {2} to {3}", GetPlayerInfo(), GetRemoteAddress(), oldRace, factionChangeInfo.RaceID);

            SendCharFactionChange(ResponseCodes.Success, factionChangeInfo);
        }

        [WorldPacketHandler(ClientOpcodes.OpeningCinematic)]
        void HandleOpeningCinematic(OpeningCinematic packet)
        {
            // Only players that has not yet gained any experience can use this
            if (GetPlayer().GetUInt32Value(ActivePlayerFields.Xp) != 0)
                return;

            ChrClassesRecord classEntry = CliDB.ChrClassesStorage.LookupByKey(GetPlayer().GetClass());
            if (classEntry != null)
            {
                ChrRacesRecord raceEntry = CliDB.ChrRacesStorage.LookupByKey(GetPlayer().GetRace());
                if (classEntry.CinematicSequenceID != 0)
                    GetPlayer().SendCinematicStart(classEntry.CinematicSequenceID);
                else if (raceEntry != null)
                    GetPlayer().SendCinematicStart(raceEntry.CinematicSequenceID);
            }
        }

        [WorldPacketHandler(ClientOpcodes.GetUndeleteCharacterCooldownStatus, Status = SessionStatus.Authed)]
        void HandleGetUndeleteCooldownStatus(GetUndeleteCharacterCooldownStatus getCooldown)
        {
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_LAST_CHAR_UNDELETE);
            stmt.AddValue(0, GetBattlenetAccountId());

            _queryProcessor.AddQuery(DB.Login.AsyncQuery(stmt).WithCallback(HandleUndeleteCooldownStatusCallback));
        }

        void HandleUndeleteCooldownStatusCallback(SQLResult result)
        {
            uint cooldown = 0;
            uint maxCooldown = WorldConfig.GetUIntValue(WorldCfg.FeatureSystemCharacterUndeleteCooldown);
            if (!result.IsEmpty())
            {
                uint lastUndelete = result.Read<uint>(0);
                uint now = (uint)Time.UnixTime;
                if (lastUndelete + maxCooldown > now)
                    cooldown = Math.Max(0, lastUndelete + maxCooldown - now);
            }

            SendUndeleteCooldownStatusResponse(cooldown, maxCooldown);
        }

        [WorldPacketHandler(ClientOpcodes.UndeleteCharacter, Status = SessionStatus.Authed)]
        void HandleCharUndelete(UndeleteCharacter undeleteCharacter)
        {
            if (!WorldConfig.GetBoolValue(WorldCfg.FeatureSystemCharacterUndeleteEnabled))
            {
                SendUndeleteCharacterResponse(CharacterUndeleteResult.Disabled, undeleteCharacter.UndeleteInfo);
                return;
            }

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_LAST_CHAR_UNDELETE);
            stmt.AddValue(0, GetBattlenetAccountId());

            CharacterUndeleteInfo undeleteInfo = undeleteCharacter.UndeleteInfo;
            _queryProcessor.AddQuery(DB.Login.AsyncQuery(stmt).WithChainingCallback((queryCallback, result) =>
            {
                if (!result.IsEmpty())
                {
                    uint lastUndelete = result.Read<uint>(0);
                    uint maxCooldown = WorldConfig.GetUIntValue(WorldCfg.FeatureSystemCharacterUndeleteCooldown);
                    if (lastUndelete != 0 && (lastUndelete + maxCooldown > Time.UnixTime))
                    {
                        SendUndeleteCharacterResponse(CharacterUndeleteResult.Cooldown, undeleteInfo);
                        return;
                    }
                }

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_GUID);
                stmt.AddValue(0, undeleteInfo.CharacterGuid.GetCounter());
                queryCallback.SetNextQuery(DB.Characters.AsyncQuery(stmt));
            }).WithChainingCallback((queryCallback, result) =>
            {
                if (result.IsEmpty())
                {
                    SendUndeleteCharacterResponse(CharacterUndeleteResult.CharCreate, undeleteInfo);
                    return;
                }

                undeleteInfo.Name = result.Read<string>(1);
                uint account = result.Read<uint>(2);
                if (account != GetAccountId())
                {
                    SendUndeleteCharacterResponse(CharacterUndeleteResult.Unknown, undeleteInfo);
                    return;
                }

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
                stmt.AddValue(0, undeleteInfo.Name);
                queryCallback.SetNextQuery(DB.Characters.AsyncQuery(stmt));
            }).WithChainingCallback((queryCallback, result) =>
            {
                if (!result.IsEmpty())
                {
                    SendUndeleteCharacterResponse(CharacterUndeleteResult.NameTakenByThisAccount, undeleteInfo);
                    return;
                }

                // @todo: add more safety checks
                // * max char count per account
                // * max death knight count
                // * max demon hunter count
                // * team violation

                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_SUM_CHARS);
                stmt.AddValue(0, GetAccountId());
                queryCallback.SetNextQuery(DB.Characters.AsyncQuery(stmt));
            }).WithCallback(result =>
            {
                if (!result.IsEmpty())
                {
                    if (result.Read<ulong>(0) >= WorldConfig.GetUIntValue(WorldCfg.CharactersPerRealm)) // SQL's COUNT() returns uint64 but it will always be less than uint8.Max
                    {
                        SendUndeleteCharacterResponse(CharacterUndeleteResult.CharCreate, undeleteInfo);
                        return;
                    }
                }

                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_RESTORE_DELETE_INFO);
                stmt.AddValue(0, undeleteInfo.Name);
                stmt.AddValue(1, GetAccountId());
                stmt.AddValue(2, undeleteInfo.CharacterGuid.GetCounter());
                DB.Characters.Execute(stmt);

                stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_LAST_CHAR_UNDELETE);
                stmt.AddValue(0, GetBattlenetAccountId());
                DB.Login.Execute(stmt);

                Global.WorldMgr.UpdateCharacterInfoDeleted(undeleteInfo.CharacterGuid, false, undeleteInfo.Name);

                SendUndeleteCharacterResponse(CharacterUndeleteResult.Ok, undeleteInfo);
            }));
        }

        [WorldPacketHandler(ClientOpcodes.RepopRequest)]
        void HandleRepopRequest(RepopRequest packet)
        {
            if (GetPlayer().IsAlive() || GetPlayer().HasFlag(PlayerFields.Flags, PlayerFlags.Ghost))
                return;

            if (GetPlayer().HasAuraType(AuraType.PreventResurrection))
                return; // silently return, client should display the error by itself

            // the world update order is sessions, players, creatures
            // the netcode runs in parallel with all of these
            // creatures can kill players
            // so if the server is lagging enough the player can
            // release spirit after he's killed but before he is updated
            if (GetPlayer().getDeathState() == DeathState.JustDied)
            {
                Log.outDebug(LogFilter.Network, "HandleRepopRequestOpcode: got request after player {0} ({1}) was killed and before he was updated",
                    GetPlayer().GetName(), GetPlayer().GetGUID().ToString());
                GetPlayer().KillPlayer();
            }

            //this is spirit release confirm?
            GetPlayer().RemovePet(null, PetSaveMode.NotInSlot, true);
            GetPlayer().BuildPlayerRepop();
            GetPlayer().RepopAtGraveyard();
        }

        [WorldPacketHandler(ClientOpcodes.ClientPortGraveyard)]
        void HandlePortGraveyard(PortGraveyard packet)
        {
            if (GetPlayer().IsAlive() || !GetPlayer().HasFlag(PlayerFields.Flags, PlayerFlags.Ghost))
                return;
            GetPlayer().RepopAtGraveyard();
        }

        [WorldPacketHandler(ClientOpcodes.RequestCemeteryList, Processing = PacketProcessing.Inplace)]
        void HandleRequestCemeteryList(RequestCemeteryList requestCemeteryList)
        {
            uint zoneId = GetPlayer().GetZoneId();
            uint team = (uint)GetPlayer().GetTeam();

            List<uint> graveyardIds = new List<uint>();
            var range = Global.ObjectMgr.GraveYardStorage.LookupByKey(zoneId);

            for (uint i = 0; i < range.Count && graveyardIds.Count < 16; ++i) // client max
            {
                var gYard = range[(int)i];
                if (gYard.team == 0 || gYard.team == team)
                    graveyardIds.Add(i);
            }

            if (graveyardIds.Empty())
            {
                Log.outDebug(LogFilter.Network, "No graveyards found for zone {0} for player {1} (team {2}) in CMSG_REQUEST_CEMETERY_LIST",
                    zoneId, m_GUIDLow, team);
                return;
            }

            RequestCemeteryListResponse packet = new RequestCemeteryListResponse();
            packet.IsGossipTriggered = false;

            foreach (uint id in graveyardIds)
                packet.CemeteryID.Add(id);

            SendPacket(packet);
        }

        [WorldPacketHandler(ClientOpcodes.ReclaimCorpse)]
        void HandleReclaimCorpse(ReclaimCorpse packet)
        {
            if (GetPlayer().IsAlive())
                return;

            // do not allow corpse reclaim in arena
            if (GetPlayer().InArena())
                return;

            // body not released yet
            if (!GetPlayer().HasFlag(PlayerFields.Flags, PlayerFlags.Ghost))
                return;

            Corpse corpse = GetPlayer().GetCorpse();
            if (!corpse)
                return;

            // prevent resurrect before 30-sec delay after body release not finished
            if ((corpse.GetGhostTime() + GetPlayer().GetCorpseReclaimDelay(corpse.GetCorpseType() == CorpseType.ResurrectablePVP)) > Time.UnixTime)
                return;

            if (!corpse.IsWithinDistInMap(GetPlayer(), 39, true))
                return;

            // resurrect
            GetPlayer().ResurrectPlayer(GetPlayer().InBattleground() ? 1.0f : 0.5f);

            // spawn bones
            GetPlayer().SpawnCorpseBones();
        }

        [WorldPacketHandler(ClientOpcodes.ResurrectResponse)]
        void HandleResurrectResponse(ResurrectResponse packet)
        {
            if (GetPlayer().IsAlive())
                return;

            if (packet.Response != 0) // Accept = 0 Decline = 1 Timeout = 2
            {
                GetPlayer().ClearResurrectRequestData(); // reject
                return;
            }

            if (!GetPlayer().IsRessurectRequestedBy(packet.Resurrecter))
                return;

            Player ressPlayer = Global.ObjAccessor.GetPlayer(GetPlayer(), packet.Resurrecter);
            if (ressPlayer)
            {
                InstanceScript instance = ressPlayer.GetInstanceScript();
                if (instance != null)
                {
                    if (instance.IsEncounterInProgress())
                    {
                        if (instance.GetCombatResurrectionCharges() == 0)
                            return;
                        else
                            instance.UseCombatResurrection();
                    }
                }
            }

            GetPlayer().ResurrectUsingRequestData();
        }

        [WorldPacketHandler(ClientOpcodes.StandStateChange)]
        void HandleStandStateChange(StandStateChange packet)
        {
            GetPlayer().SetStandState(packet.StandState);
        }

        void SendCharCreate(ResponseCodes result, ObjectGuid guid = default(ObjectGuid))
        {
            CreateChar response = new CreateChar();
            response.Code = result;
            response.Guid = guid;

            SendPacket(response);
        }

        void SendCharDelete(ResponseCodes result)
        {
            DeleteChar response = new DeleteChar();
            response.Code = result;

            SendPacket(response);
        }

        void SendCharRename(ResponseCodes result, CharacterRenameInfo renameInfo)
        {
            CharacterRenameResult packet = new CharacterRenameResult();
            packet.Result = result;
            packet.Name = renameInfo.NewName;
            if (result == ResponseCodes.Success)
                packet.Guid.Set(renameInfo.Guid);

            SendPacket(packet);
        }

        void SendCharCustomize(ResponseCodes result, CharCustomizeInfo customizeInfo)
        {
            if (result == ResponseCodes.Success)
            {
                CharCustomizeResponse response = new CharCustomizeResponse(customizeInfo);
                SendPacket(response);
            }
            else
            {
                CharCustomizeFailed failed = new CharCustomizeFailed();
                failed.Result = (byte)result;
                failed.CharGUID = customizeInfo.CharGUID;
                SendPacket(failed);
            }
        }

        void SendCharFactionChange(ResponseCodes result, CharRaceOrFactionChangeInfo factionChangeInfo)
        {
            CharFactionChangeResult packet = new CharFactionChangeResult();
            packet.Result = result;
            packet.Guid = factionChangeInfo.Guid;

            if (result == ResponseCodes.Success)
            {
                packet.Display.HasValue = true;
                packet.Display.Value.Name = factionChangeInfo.Name;
                packet.Display.Value.SexID = (byte)factionChangeInfo.SexID;
                packet.Display.Value.SkinID = factionChangeInfo.SkinID;
                packet.Display.Value.HairColorID = factionChangeInfo.HairColorID;
                packet.Display.Value.HairStyleID = factionChangeInfo.HairStyleID;
                packet.Display.Value.FacialHairStyleID = factionChangeInfo.FacialHairStyleID;
                packet.Display.Value.FaceID = factionChangeInfo.FaceID;
                packet.Display.Value.RaceID = (byte)factionChangeInfo.RaceID;
                packet.Display.Value.CustomDisplay = factionChangeInfo.CustomDisplay;
            }

            SendPacket(packet);
        }

        void SendSetPlayerDeclinedNamesResult(DeclinedNameResult result, ObjectGuid guid)
        {
            SetPlayerDeclinedNamesResult packet = new SetPlayerDeclinedNamesResult();
            packet.ResultCode = result;
            packet.Player = guid;

            SendPacket(packet);
        }

        void SendUndeleteCooldownStatusResponse(uint currentCooldown, uint maxCooldown)
        {
            UndeleteCooldownStatusResponse response = new UndeleteCooldownStatusResponse();
            response.OnCooldown = (currentCooldown > 0);
            response.MaxCooldown = maxCooldown;
            response.CurrentCooldown = currentCooldown;

            SendPacket(response);
        }

        void SendUndeleteCharacterResponse(CharacterUndeleteResult result, CharacterUndeleteInfo undeleteInfo)
        {
            UndeleteCharacterResponse response = new UndeleteCharacterResponse();
            response.UndeleteInfo = undeleteInfo;
            response.Result = result;

            SendPacket(response);
        }
    }

    public class LoginQueryHolder : SQLQueryHolder<PlayerLoginQueryLoad>
    {
        public LoginQueryHolder(uint accountId, ObjectGuid guid)
        {
            m_accountId = accountId;
            m_guid = guid;
        }

        public void Initialize()
        {
            ulong lowGuid = m_guid.GetCounter();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.From, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Group, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_INSTANCE);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.BoundInstances, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURAS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Auras, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURA_EFFECTS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.AuraEffects, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELL);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Spells, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.QuestStatus, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.QuestStatusObjectives, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.QuestStatusObjectivesCriteria, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.QuestStatusObjectivesCriteriaProgress, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_DAILY);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.DailyQuestStatus, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_WEEKLY);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.WeeklyQuestStatus, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_MONTHLY);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.MonthlyQuestStatus, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_SEASONAL);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.SeasonalQuestStatus, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_REPUTATION);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Reputation, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_INVENTORY);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Inventory, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_ARTIFACT);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Artifacts, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_VOID_STORAGE);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.VoidStorage, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_ACTIONS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Actions, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_MAILCOUNT);
            stmt.AddValue(0, lowGuid);
            stmt.AddValue(1, Time.UnixTime);
            SetQuery(PlayerLoginQueryLoad.MailCount, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_MAILDATE);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.MailDate, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_SOCIALLIST);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.SocialList, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_HOMEBIND);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.HomeBind, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELLCOOLDOWNS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.SpellCooldowns, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELL_CHARGES);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.SpellCharges, stmt);

            if (WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed))
            {
                stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_DECLINEDNAMES);
                stmt.AddValue(0, lowGuid);
                SetQuery(PlayerLoginQueryLoad.DeclinedNames, stmt);
            }

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUILD_MEMBER);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Guild, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_ARENAINFO);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.ArenaInfo, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_ACHIEVEMENTS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Achievements, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_CRITERIAPROGRESS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.CriteriaProgress, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_EQUIPMENTSETS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.EquipmentSets, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_TRANSMOG_OUTFITS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.TransmogOutfits, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_CUF_PROFILES);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.CufProfiles, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_BGDATA);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.BgData, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GLYPHS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Glyphs, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_TALENTS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Talents, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_PVP_TALENTS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.PvpTalents, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PLAYER_ACCOUNT_DATA);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.AccountData, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_SKILLS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Skills, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_RANDOMBG);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.RandomBg, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_BANNED);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Banned, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUSREW);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.QuestStatusRew, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ACCOUNT_INSTANCELOCKTIMES);
            stmt.AddValue(0, m_accountId);
            SetQuery(PlayerLoginQueryLoad.InstanceLockTimes, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PLAYER_CURRENCY);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Currency, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CORPSE_LOCATION);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.CorpseLocation, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Garrison, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_BLUEPRINTS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.GarrisonBlueprints, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_BUILDINGS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.GarrisonBuildings, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_FOLLOWERS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.GarrisonFollowers, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_FOLLOWER_ABILITIES);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.GarrisonFollowerAbilities, stmt);
        }

        public ObjectGuid GetGuid() { return m_guid; }

        uint GetAccountId() { return m_accountId; }

        uint m_accountId;
        ObjectGuid m_guid;
    }

    // used at player loading query list preparing, and later result selection
    public enum PlayerLoginQueryLoad
    {
        From,
        Group,
        BoundInstances,
        Auras,
        AuraEffects,
        Spells,
        QuestStatus,
        QuestStatusObjectives,
        QuestStatusObjectivesCriteria,
        QuestStatusObjectivesCriteriaProgress,
        DailyQuestStatus,
        Reputation,
        Inventory,
        Artifacts,
        Actions,
        MailCount,
        MailDate,
        SocialList,
        HomeBind,
        SpellCooldowns,
        SpellCharges,
        DeclinedNames,
        Guild,
        ArenaInfo,
        Achievements,
        CriteriaProgress,
        EquipmentSets,
        TransmogOutfits,
        BgData,
        Glyphs,
        Talents,
        PvpTalents,
        AccountData,
        Skills,
        WeeklyQuestStatus,
        RandomBg,
        Banned,
        QuestStatusRew,
        InstanceLockTimes,
        SeasonalQuestStatus,
        MonthlyQuestStatus,
        VoidStorage,
        Currency,
        CufProfiles,
        CorpseLocation,
        Garrison,
        GarrisonBlueprints,
        GarrisonBuildings,
        GarrisonFollowers,
        GarrisonFollowerAbilities,
        Max
    }
}
