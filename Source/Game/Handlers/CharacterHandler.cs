// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.Cache;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
using Game.Miscellaneous;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.EnumCharacters, Status = SessionStatus.Authed)]
        void HandleCharEnum(EnumCharacters charEnum)
        {
            // remove expired bans
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_EXPIRED_BANS);
            DB.Characters.Execute(stmt);

            // get all the data necessary for loading all characters (along with their pets) on the account
            EnumCharactersQueryHolder holder = new();
            if (!holder.Initialize(GetAccountId(), WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed), false))
            {
                HandleCharEnum(holder);
                return;
            }

            AddQueryHolderCallback(DB.Characters.DelayQueryHolder(holder)).AfterComplete(result => HandleCharEnum((EnumCharactersQueryHolder)result));
        }

        void HandleCharEnum(EnumCharactersQueryHolder holder)
        {
            EnumCharactersResult charResult = new();
            charResult.Success = true;
            charResult.IsDeletedCharacters = holder.IsDeletedCharacters();
            charResult.ClassDisableMask = WorldConfig.GetUIntValue(WorldCfg.CharacterCreatingDisabledClassmask);

            if (!charResult.IsDeletedCharacters)
                _legitCharacters.Clear();

            MultiMap<ulong, ChrCustomizationChoice> customizations = new();
            SQLResult customizationsResult = holder.GetResult(EnumCharacterQueryLoad.Customizations);
            if (!customizationsResult.IsEmpty())
            {
                do
                {
                    ChrCustomizationChoice choice = new();
                    choice.ChrCustomizationOptionID = customizationsResult.Read<uint>(1);
                    choice.ChrCustomizationChoiceID = customizationsResult.Read<uint>(2);
                    customizations.Add(customizationsResult.Read<ulong>(0), choice);

                } while (customizationsResult.NextRow());
            }

            SQLResult result = holder.GetResult(EnumCharacterQueryLoad.Characters);
            if (!result.IsEmpty())
            {
                do
                {
                    charResult.Characters.Add(new EnumCharactersResult.CharacterInfo(result.GetFields()));


                    EnumCharactersResult.CharacterInfoBasic charInfo = charResult.Characters.Last().Basic;

                    var customizationsForChar = customizations.LookupByKey(charInfo.Guid.GetCounter());
                    if (!customizationsForChar.Empty())
                        charInfo.Customizations = new Array<ChrCustomizationChoice>(customizationsForChar.ToArray());

                    Log.outInfo(LogFilter.Network, "Loading Character {0} from account {1}.", charInfo.Guid.ToString(), GetAccountId());

                    if (!charResult.IsDeletedCharacters)
                    {
                        if (!ValidateAppearance((Race)charInfo.RaceId, charInfo.ClassId, (Gender)charInfo.SexId, charInfo.Customizations))
                        {
                            Log.outError(LogFilter.Player, "Player {0} has wrong Appearance values (Hair/Skin/Color), forcing recustomize", charInfo.Guid.ToString());

                            charInfo.Customizations.Clear();

                            if (!charInfo.Flags2.HasAnyFlag(CharacterFlags2.Customize | CharacterFlags2.FactionChange | CharacterFlags2.RaceChange))
                            {
                                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                                stmt.AddValue(0, (ushort)AtLoginFlags.Customize);
                                stmt.AddValue(1, charInfo.Guid.GetCounter());
                                DB.Characters.Execute(stmt);
                                charInfo.Flags2 = CharacterFlags2.Customize;
                            }
                        }

                        // Do not allow locked characters to login
                        if (!charInfo.Flags.HasAnyFlag(CharacterFlags.LockedForTransfer | CharacterFlags.LockedByBilling))
                            _legitCharacters.Add(charInfo.Guid);
                    }

                    if (!Global.CharacterCacheStorage.HasCharacterCacheEntry(charInfo.Guid)) // This can happen if characters are inserted into the database manually. Core hasn't loaded name data yet.
                        Global.CharacterCacheStorage.AddCharacterCacheEntry(charInfo.Guid, GetAccountId(), charInfo.Name, charInfo.SexId, charInfo.RaceId, (byte)charInfo.ClassId, charInfo.ExperienceLevel, false);

                    charResult.MaxCharacterLevel = Math.Max(charResult.MaxCharacterLevel, charInfo.ExperienceLevel);
                }
                while (result.NextRow() && charResult.Characters.Count < 200);
            }

            foreach (var requirement in Global.ObjectMgr.GetRaceUnlockRequirements())
            {
                EnumCharactersResult.RaceUnlock raceUnlock = new();
                raceUnlock.RaceID = (sbyte)requirement.Key;
                raceUnlock.HasUnlockedLicense = (byte)GetAccountExpansion() >= requirement.Value.Expansion;
                raceUnlock.HasUnlockedAchievement = requirement.Value.AchievementId != 0 && (WorldConfig.GetBoolValue(WorldCfg.CharacterCreatingDisableAlliedRaceAchievementRequirement)
                    /* || HasAccountAchievement(requirement.second.AchievementId)*/);
                charResult.RaceUnlockData.Add(raceUnlock);
            }

            SendPacket(charResult);

            if (!charResult.IsDeletedCharacters)
                _collectionMgr.SendWarbandSceneCollectionData();
        }

        [WorldPacketHandler(ClientOpcodes.EnumCharactersDeletedByClient, Status = SessionStatus.Authed)]
        void HandleCharUndeleteEnum(EnumCharacters enumCharacters)
        {
            // get all the data necessary for loading all undeleted characters (along with their pets) on the account
            EnumCharactersQueryHolder holder = new();
            if (!holder.Initialize(GetAccountId(), WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed), true))
            {
                HandleCharEnum(holder);
                return;
            }

            AddQueryHolderCallback(DB.Characters.DelayQueryHolder(holder)).AfterComplete(result => HandleCharEnum((EnumCharactersQueryHolder)result));
        }

        void HandleCharUndeleteEnumCallback(SQLResult result)
        {
            EnumCharactersResult charEnum = new();
            charEnum.Success = true;
            charEnum.IsDeletedCharacters = true;
            charEnum.ClassDisableMask = WorldConfig.GetUIntValue(WorldCfg.CharacterCreatingDisabledClassmask);

            if (!result.IsEmpty())
            {
                do
                {
                    charEnum.Characters.Add(new EnumCharactersResult.CharacterInfo(result.GetFields()));

                    EnumCharactersResult.CharacterInfoBasic charInfo = charEnum.Characters.Last().Basic;

                    Log.outInfo(LogFilter.Network, "Loading undeleted char guid {0} from account {1}.", charInfo.Guid.ToString(), GetAccountId());

                    if (!Global.CharacterCacheStorage.HasCharacterCacheEntry(charInfo.Guid)) // This can happen if characters are inserted into the database manually. Core hasn't loaded name data yet.
                        Global.CharacterCacheStorage.AddCharacterCacheEntry(charInfo.Guid, GetAccountId(), charInfo.Name, charInfo.SexId, charInfo.RaceId, (byte)charInfo.ClassId, charInfo.ExperienceLevel, true);
                }
                while (result.NextRow());
            }

            SendPacket(charEnum);
        }

        public bool MeetsChrCustomizationReq(ChrCustomizationReqRecord req, Race race, Class playerClass, bool checkRequiredDependentChoices, List<ChrCustomizationChoice> selectedChoices)
        {
            if (!req.HasFlag(ChrCustomizationReqFlag.HasRequirements))
                return true;

            if (req.ClassMask != 0 && (req.ClassMask & (1 << ((int)playerClass - 1))) == 0)
                return false;

            var raceMask = new RaceMask<long>(req.RaceMask);
            if (race != Race.None && !raceMask.IsEmpty() && raceMask.RawValue != -1 && !raceMask.HasRace(race))
                return false;

            if (req.AchievementID != 0 /*&& !HasAchieved(req->AchievementID)*/)
                return false;

            if (req.ItemModifiedAppearanceID != 0 && !GetCollectionMgr().HasItemAppearance(req.ItemModifiedAppearanceID).PermAppearance)
                return false;

            if (req.QuestID != 0)
            {
                if (_player == null)
                    return false;

                if (!_player.IsQuestRewarded((uint)req.QuestID))
                    return false;
            }

            if (checkRequiredDependentChoices)
            {
                var requiredChoices = Global.DB2Mgr.GetRequiredCustomizationChoices(req.Id);
                if (requiredChoices != null)
                {
                    foreach (var key in requiredChoices.Keys)
                    {
                        bool hasRequiredChoiceForOption = false;
                        foreach (uint requiredChoice in requiredChoices[key])
                        {
                            if (selectedChoices.Any(choice => choice.ChrCustomizationChoiceID == requiredChoice))
                            {
                                hasRequiredChoiceForOption = true;
                                break;
                            }
                        }

                        if (!hasRequiredChoiceForOption)
                            return false;
                    }
                }

            }

            return true;
        }

        public bool ValidateAppearance(Race race, Class playerClass, Gender gender, List<ChrCustomizationChoice> customizations)
        {
            var options = Global.DB2Mgr.GetCustomiztionOptions(race, gender);
            if (options.Empty())
                return false;

            uint previousOption = 0;

            foreach (var playerChoice in customizations)
            {
                // check uniqueness of options
                if (playerChoice.ChrCustomizationOptionID == previousOption)
                    return false;

                previousOption = playerChoice.ChrCustomizationOptionID;

                // check if we can use this option
                var customizationOptionData = options.Find(option => { return option.Id == playerChoice.ChrCustomizationOptionID; });

                // option not found for race/gender combination
                if (customizationOptionData == null)
                    return false;

                ChrCustomizationReqRecord req = CliDB.ChrCustomizationReqStorage.LookupByKey(customizationOptionData.ChrCustomizationReqID);
                if (req != null)
                    if (!MeetsChrCustomizationReq(req, race, playerClass, false, customizations))
                        return false;

                var choicesForOption = Global.DB2Mgr.GetCustomiztionChoices(playerChoice.ChrCustomizationOptionID);
                if (choicesForOption.Empty())
                    return false;

                var customizationChoiceData = choicesForOption.Find(choice => { return choice.Id == playerChoice.ChrCustomizationChoiceID; });

                // choice not found for option
                if (customizationChoiceData == null)
                    return false;

                ChrCustomizationReqRecord reqEntry = CliDB.ChrCustomizationReqStorage.LookupByKey(customizationChoiceData.ChrCustomizationReqID);
                if (reqEntry != null)
                    if (!MeetsChrCustomizationReq(reqEntry, race, playerClass, true, customizations))
                        return false;
            }

            return true;
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
                        case BattleGroundTeamId.Alliance:
                            disabled = Convert.ToBoolean(mask & (1 << 0));
                            break;
                        case BattleGroundTeamId.Horde:
                            disabled = Convert.ToBoolean(mask & (1 << 1));
                            break;
                        case BattleGroundTeamId.Neutral:
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
                Log.outError(LogFilter.Cheat, $"Account {GetAccountId()} tried to create character with unavailable race {charCreate.CreateInfo.RaceId}");
                SendCharCreate(ResponseCodes.CharCreateFailed);
                return;
            }

            if (raceExpansionRequirement.Expansion > (byte)GetAccountExpansion())
            {
                Log.outError(LogFilter.Cheat, $"Expansion {GetAccountExpansion()} account:[{GetAccountId()}] tried to Create character with expansion {raceExpansionRequirement.Expansion} race ({charCreate.CreateInfo.RaceId})");
                SendCharCreate(ResponseCodes.CharCreateExpansion);
                return;
            }

            //if (raceExpansionRequirement.AchievementId && !)
            //{
            //    TC_LOG_ERROR("entities.player.cheat", "Expansion %u account:[%d] tried to Create character without achievement %u race (%u)",
            //        GetAccountExpansion(), GetAccountId(), raceExpansionRequirement.AchievementId, charCreate.CreateInfo.Race);
            //    SendCharCreate(CHAR_CREATE_ALLIED_RACE_ACHIEVEMENT);
            //    return;
            //}

            // prevent character creating Expansion race without Expansion account
            ClassAvailability raceClassExpansionRequirement = Global.ObjectMgr.GetClassExpansionRequirement(charCreate.CreateInfo.RaceId, charCreate.CreateInfo.ClassId);
            if (raceClassExpansionRequirement != null)
            {
                if (raceClassExpansionRequirement.ActiveExpansionLevel > (byte)GetExpansion() || raceClassExpansionRequirement.AccountExpansionLevel > (byte)GetAccountExpansion())
                {
                    Log.outError(LogFilter.Cheat, $"Account:[{GetAccountId()}] tried to create character with race/class {charCreate.CreateInfo.RaceId}/{charCreate.CreateInfo.ClassId} without required expansion " +
                        $"(had {GetExpansion()}/{GetAccountExpansion()}, required {raceClassExpansionRequirement.ActiveExpansionLevel}/{raceClassExpansionRequirement.AccountExpansionLevel})");
                    SendCharCreate(ResponseCodes.CharCreateExpansionClass);
                    return;
                }
            }
            else
            {
                ClassAvailability classExpansionRequirement = Global.ObjectMgr.GetClassExpansionRequirementFallback((byte)charCreate.CreateInfo.ClassId);
                if (classExpansionRequirement != null)
                {
                    if (classExpansionRequirement.MinActiveExpansionLevel > (byte)GetExpansion() || classExpansionRequirement.AccountExpansionLevel > (byte)GetAccountExpansion())
                    {
                        Log.outError(LogFilter.Cheat, $"Account:[{GetAccountId()}] tried to create character with race/class {charCreate.CreateInfo.RaceId}/{charCreate.CreateInfo.ClassId} without required expansion " +
                            $"(had {GetExpansion()}/{GetAccountExpansion()}, required {classExpansionRequirement.ActiveExpansionLevel}/{classExpansionRequirement.AccountExpansionLevel})");
                        SendCharCreate(ResponseCodes.CharCreateExpansionClass);
                        return;
                    }
                }
                else
                    Log.outError(LogFilter.Cheat, $"Expansion {GetAccountExpansion()} account:[{GetAccountId()}] tried to Create character for race/class combination that is missing requirements in db ({charCreate.CreateInfo.RaceId}/{charCreate.CreateInfo.ClassId})");
                SendCharCreate(ResponseCodes.CharCreateExpansionClass);
                return;
            }

            if (!HasPermission(RBACPermissions.SkipCheckCharacterCreationRacemask))
            {
                if (raceEntry.HasFlag(ChrRacesFlag.NPCOnly))
                {
                    Log.outError(LogFilter.Network, $"Race ({charCreate.CreateInfo.RaceId}) was not playable but requested while creating new char for account (ID: {GetAccountId()}): wrong DBC files or cheater?");
                    SendCharCreate(ResponseCodes.CharCreateDisabled);
                    return;
                }

                RaceMask<ulong> raceMaskDisabled = new(WorldConfig.GetUInt64Value(WorldCfg.CharacterCreatingDisabledRacemask));
                if (raceMaskDisabled.HasRace(charCreate.CreateInfo.RaceId))
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

            if (charCreate.CreateInfo.TimerunningSeasonID != 0)
            {
                SendCharCreate(ResponseCodes.CharCreateTimerunning);
                return;
            }

            CharacterCreateInfo createInfo = charCreate.CreateInfo;
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
            stmt.AddValue(0, charCreate.CreateInfo.Name);

            _queryProcessor.AddCallback(DB.Characters.AsyncQuery(stmt).WithChainingCallback((queryCallback, result) =>
            {
                if (!result.IsEmpty())
                {
                    SendCharCreate(ResponseCodes.CharCreateNameInUse);
                    return;
                }

                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_SUM_REALM_CHARACTERS);
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

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_SUM_CHARS);
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

                int demonHunterReqLevel = WorldConfig.GetIntValue(WorldCfg.CharacterCreatingMinLevelForDemonHunter);
                bool hasDemonHunterReqLevel = demonHunterReqLevel == 0;
                uint evokerReqLevel = WorldConfig.GetUIntValue(WorldCfg.CharacterCreatingMinLevelForEvoker);
                bool hasEvokerReqLevel = (evokerReqLevel == 0);
                bool allowTwoSideAccounts = !Global.WorldMgr.IsPvPRealm() || HasPermission(RBACPermissions.TwoSideCharacterCreation);
                int skipCinematics = WorldConfig.GetIntValue(WorldCfg.SkipCinematics);
                bool checkClassLevelReqs = (createInfo.ClassId == Class.DemonHunter || createInfo.ClassId == Class.Evoker)
                                            && !HasPermission(RBACPermissions.SkipCheckCharacterCreationDemonHunter);
                int evokerLimit = WorldConfig.GetIntValue(WorldCfg.CharacterCreatingEvokersPerRealm);
                bool hasEvokerLimit = evokerLimit != 0;

                void finalizeCharacterCreation(SQLResult result1)
                {
                    bool haveSameRace = false;
                    if (result1 != null && !result1.IsEmpty())
                    {
                        Team team = Player.TeamForRace(createInfo.RaceId);
                        byte accRace = result1.Read<byte>(1);
                        byte accClass = result1.Read<byte>(2);

                        if (checkClassLevelReqs)
                        {
                            if (!hasDemonHunterReqLevel)
                            {
                                byte accLevel = result1.Read<byte>(0);
                                if (accLevel >= demonHunterReqLevel)
                                    hasDemonHunterReqLevel = true;
                            }
                            if (!hasEvokerReqLevel)
                            {
                                byte accLevel = result1.Read<byte>(0);
                                if (accLevel >= evokerReqLevel)
                                    hasEvokerReqLevel = true;
                            }
                        }

                        if (accClass == (byte)Class.Evoker)
                            --evokerLimit;

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
                        while ((skipCinematics == 1 && !haveSameRace) || createInfo.ClassId == Class.DemonHunter || createInfo.ClassId == Class.Evoker)
                        {
                            if (!result1.NextRow())
                                break;

                            accRace = result1.Read<byte>(1);
                            accClass = result1.Read<byte>(2);

                            if (!haveSameRace)
                                haveSameRace = createInfo.RaceId == (Race)accRace;

                            if (checkClassLevelReqs)
                            {
                                if (!hasDemonHunterReqLevel)
                                {
                                    byte acc_level = result1.Read<byte>(0);
                                    if (acc_level >= demonHunterReqLevel)
                                        hasDemonHunterReqLevel = true;
                                }
                                if (!hasEvokerReqLevel)
                                {
                                    byte accLevel = result1.Read<byte>(0);
                                    if (accLevel >= evokerReqLevel)
                                        hasEvokerReqLevel = true;
                                }
                            }
                            if (accClass == (byte)Class.Evoker)
                                --evokerLimit;
                        }
                    }

                    if (checkClassLevelReqs)
                    {
                        if (!hasDemonHunterReqLevel)
                        {
                            SendCharCreate(ResponseCodes.CharCreateNewPlayer);
                            return;
                        }
                        if (!hasEvokerReqLevel)
                        {
                            SendCharCreate(ResponseCodes.CharCreateDracthyrLevelRequirement);
                            return;
                        }
                    }

                    if (createInfo.ClassId == Class.Evoker && hasEvokerLimit && evokerLimit < 1)
                    {
                        SendCharCreate(ResponseCodes.CharCreateNewPlayer);
                        return;
                    }

                    // Check name uniqueness in the same step as saving to database
                    if (Global.CharacterCacheStorage.GetCharacterCacheByName(createInfo.Name) != null)
                    {
                        SendCharCreate(ResponseCodes.CharCreateDracthyrDuplicate);
                        return;
                    }

                    Player newChar = new(this);
                    newChar.GetMotionMaster().Initialize();
                    if (!newChar.Create(Global.ObjectMgr.GetGenerator(HighGuid.Player).Generate(), createInfo))
                    {
                        // Player not create (race/class/etc problem?)
                        newChar.CleanupsBeforeDelete();
                        newChar.Dispose();
                        SendCharCreate(ResponseCodes.CharCreateError);
                        return;
                    }

                    if ((haveSameRace && skipCinematics == 1) || skipCinematics == 2)
                        newChar.SetCinematic(1);                          // not show intro

                    newChar.atLoginFlags = AtLoginFlags.FirstLogin;               // First login

                    SQLTransaction characterTransaction = new();
                    SQLTransaction loginTransaction = new();

                    // Player created, save it now
                    newChar.SaveToDB(loginTransaction, characterTransaction, true);
                    createInfo.CharCount += 1;

                    stmt = LoginDatabase.GetPreparedStatement(LoginStatements.REP_REALM_CHARACTERS);
                    stmt.AddValue(0, createInfo.CharCount);
                    stmt.AddValue(1, GetAccountId());
                    stmt.AddValue(2, Global.RealmMgr.GetCurrentRealmId().Index);
                    loginTransaction.Append(stmt);

                    AddTransactionCallback(DB.Characters.AsyncCommitTransaction(characterTransaction)).AfterComplete(success =>
                    {
                        if (success)
                        {
                            DB.Login.CommitTransaction(loginTransaction);

                            Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Create Character: {2} {3}", GetAccountId(), GetRemoteAddress(), createInfo.Name, newChar.GetGUID().ToString());
                            Global.ScriptMgr.OnPlayerCreate(newChar);
                            Global.CharacterCacheStorage.AddCharacterCacheEntry(newChar.GetGUID(), GetAccountId(), newChar.GetName(), (byte)newChar.GetNativeGender(), (byte)newChar.GetRace(), (byte)newChar.GetClass(), (byte)newChar.GetLevel(), false);

                            SendCharCreate(ResponseCodes.CharCreateSuccess, newChar.GetGUID());
                        }
                        else
                            SendCharCreate(ResponseCodes.CharCreateError);

                        newChar.CleanupsBeforeDelete();
                        newChar.Dispose();
                    });
                }

                if (!allowTwoSideAccounts || skipCinematics == 1 || createInfo.ClassId == Class.DemonHunter)
                {
                    finalizeCharacterCreation(new SQLResult());
                    return;
                }

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_CREATE_INFO);
                stmt.AddValue(0, GetAccountId());
                stmt.AddValue(1, (skipCinematics == 1 || createInfo.ClassId == Class.DemonHunter || createInfo.ClassId == Class.Evoker) ? 1200 : 1); // 200 (max chars per realm) + 1000 (max deleted chars per realm)
                queryCallback.WithCallback(finalizeCharacterCreation).SetNextQuery(DB.Characters.AsyncQuery(stmt));
            }));
        }

        [WorldPacketHandler(ClientOpcodes.CharDelete, Status = SessionStatus.Authed)]
        void HandleCharDelete(CharDelete charDelete)
        {
            // Initiating
            uint initAccountId = GetAccountId();

            // can't delete loaded character
            if (Global.ObjAccessor.FindPlayer(charDelete.Guid) != null)
            {
                Global.ScriptMgr.OnPlayerFailedDelete(charDelete.Guid, initAccountId);
                return;
            }

            // is guild leader
            if (Global.GuildMgr.GetGuildByLeader(charDelete.Guid) != null)
            {
                Global.ScriptMgr.OnPlayerFailedDelete(charDelete.Guid, initAccountId);
                SendCharDelete(ResponseCodes.CharDeleteFailedGuildLeader);
                return;
            }

            // is arena team captain
            if (Global.ArenaTeamMgr.GetArenaTeamByCaptain(charDelete.Guid) != null)
            {
                Global.ScriptMgr.OnPlayerFailedDelete(charDelete.Guid, initAccountId);
                SendCharDelete(ResponseCodes.CharDeleteFailedArenaCaptain);
                return;
            }

            CharacterCacheEntry characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(charDelete.Guid);
            if (characterInfo == null)
            {
                Global.ScriptMgr.OnPlayerFailedDelete(charDelete.Guid, initAccountId);
                return;
            }

            uint accountId = characterInfo.AccountId;
            string name = characterInfo.Name;
            byte level = characterInfo.Level;

            // prevent deleting other players' characters using cheating tools
            if (accountId != GetAccountId())
            {
                Global.ScriptMgr.OnPlayerFailedDelete(charDelete.Guid, initAccountId);
                return;
            }

            string IP_str = GetRemoteAddress();
            Log.outInfo(LogFilter.Player, "Account: {0}, IP: {1} deleted character: {2}, {3}, Level: {4}", accountId, IP_str, name, charDelete.Guid.ToString(), level);

            // To prevent hook failure, place hook before removing reference from DB
            Global.ScriptMgr.OnPlayerDelete(charDelete.Guid, initAccountId); // To prevent race conditioning, but as it also makes sense, we hand the accountId over for successful delete.
            // Shouldn't interfere with character deletion though

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

            GenerateRandomCharacterNameResult result = new();
            result.Success = true;
            result.Name = Global.DB2Mgr.GetNameGenEntry(packet.Race, packet.Sex);

            SendPacket(result);
        }

        [WorldPacketHandler(ClientOpcodes.ReorderCharacters, Status = SessionStatus.Authed)]
        void HandleReorderCharacters(ReorderCharacters reorderChars)
        {
            SQLTransaction trans = new();

            foreach (var reorderInfo in reorderChars.Entries)
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_LIST_SLOT);
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
                KickPlayer("WorldSession::HandlePlayerLoginOpcode Another client logging in");
                return;
            }

            m_playerLoading = playerLogin.Guid;
            Log.outDebug(LogFilter.Network, "Character {0} logging in", playerLogin.Guid.ToString());

            if (!_legitCharacters.Contains(playerLogin.Guid))
            {
                Log.outError(LogFilter.Network, "Account ({0}) can't login with that character ({1}).", GetAccountId(), playerLogin.Guid.ToString());
                KickPlayer("WorldSession::HandlePlayerLoginOpcode Trying to login with a character of another account");
                return;
            }

            SendConnectToInstance(ConnectToSerial.WorldAttempt1);
        }

        public void HandleContinuePlayerLogin()
        {
            if (!PlayerLoading() || GetPlayer() != null)
            {
                KickPlayer("WorldSession::HandleContinuePlayerLogin incorrect player state when logging in");
                return;
            }

            LoginQueryHolder holder = new(GetAccountId(), m_playerLoading);
            holder.Initialize();

            SendPacket(new ResumeComms(ConnectionType.Instance));

            // client will respond to SMSG_RESUME_COMMS with CMSG_QUEUED_MESSAGES_END
            RegisterTimeSync(SPECIAL_RESUME_COMMS_TIME_SYNC_COUNTER);

            AddQueryHolderCallback(DB.Characters.DelayQueryHolder(holder)).AfterComplete(holder => HandlePlayerLogin((LoginQueryHolder)holder));
        }

        public void HandlePlayerLogin(LoginQueryHolder holder)
        {
            ObjectGuid playerGuid = holder.GetGuid();

            Player pCurrChar = new(this);
            if (!pCurrChar.LoadFromDB(playerGuid, holder))
            {
                SetPlayer(null);
                KickPlayer("WorldSession::HandlePlayerLogin Player::LoadFromDB failed");
                m_playerLoading.Clear();
                return;
            }

            if (!_timeSyncClockDeltaQueue.IsEmpty)
            {
                pCurrChar.SetPlayerLocalFlag(PlayerLocalFlags.OverrideTransportServerTime);
                pCurrChar.SetTransportServerTime((int)_timeSyncClockDelta);
            }

            pCurrChar.SetVirtualPlayerRealm(Global.WorldMgr.GetVirtualRealmAddress());

            SendAccountDataTimes(ObjectGuid.Empty, AccountDataTypes.GlobalCacheMask);
            SendTutorialsData();

            pCurrChar.GetMotionMaster().Initialize();
            pCurrChar.SendDungeonDifficulty();

            LoginVerifyWorld loginVerifyWorld = new();
            loginVerifyWorld.MapID = (int)pCurrChar.GetMapId();
            loginVerifyWorld.Pos = pCurrChar.GetPosition();
            SendPacket(loginVerifyWorld);

            // load player specific part before send times
            LoadAccountData(holder.GetResult(PlayerLoginQueryLoad.AccountData), AccountDataTypes.PerCharacterCacheMask);

            SendAccountDataTimes(playerGuid, AccountDataTypes.AllAccountDataCacheMask);

            SendFeatureSystemStatus();

            foreach (var motdLine in Global.WorldMgr.GetMotd())
                Global.WorldMgr.SendServerMessage(ServerMessageType.String, motdLine, pCurrChar);

            SendSetTimeZoneInformation();

            // Send PVPSeason
            {
                SeasonInfo seasonInfo = new();
                seasonInfo.PreviousArenaSeason = (WorldConfig.GetIntValue(WorldCfg.ArenaSeasonId) - (WorldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress) ? 1 : 0));

                if (WorldConfig.GetBoolValue(WorldCfg.ArenaSeasonInProgress))
                    seasonInfo.CurrentArenaSeason = WorldConfig.GetIntValue(WorldCfg.ArenaSeasonId);

                SendPacket(seasonInfo);
            }

            SQLResult resultGuild = holder.GetResult(PlayerLoginQueryLoad.Guild);
            if (!resultGuild.IsEmpty())
            {
                pCurrChar.SetInGuild(resultGuild.Read<uint>(0));
                pCurrChar.SetGuildRank(resultGuild.Read<byte>(1));
                Guild guild = Global.GuildMgr.GetGuildById(pCurrChar.GetGuildId());
                if (guild != null)
                    pCurrChar.SetGuildLevel(guild.GetLevel());
            }
            else if (pCurrChar.GetGuildId() != 0)
            {
                pCurrChar.SetInGuild(0);
                pCurrChar.SetGuildRank(0);
                pCurrChar.SetGuildLevel(0);
            }

            SendAuctionFavoriteList();

            pCurrChar.GetSession().GetBattlePetMgr().SendJournalLockStatus();

            pCurrChar.SendInitialPacketsBeforeAddToMap();

            //Show cinematic at the first time that player login
            if (pCurrChar.GetCinematic() == 0)
            {
                pCurrChar.SetCinematic(1);
                var playerInfo = Global.ObjectMgr.GetPlayerInfo(pCurrChar.GetRace(), pCurrChar.GetClass());
                if (playerInfo != null)
                {
                    switch (pCurrChar.GetCreateMode())
                    {
                        case PlayerCreateMode.Normal:
                            if (playerInfo.introMovieId.HasValue)
                                pCurrChar.SendMovieStart(playerInfo.introMovieId.Value);
                            else if (playerInfo.introSceneId.HasValue)
                                pCurrChar.GetSceneMgr().PlayScene(playerInfo.introSceneId.Value);
                            else if (CliDB.ChrClassesStorage.TryGetValue((uint)pCurrChar.GetClass(), out ChrClassesRecord chrClassesRecord) && chrClassesRecord.CinematicSequenceID != 0)
                                pCurrChar.SendCinematicStart(chrClassesRecord.CinematicSequenceID);
                            else if (CliDB.ChrRacesStorage.TryGetValue((uint)pCurrChar.GetRace(), out ChrRacesRecord chrRacesRecord) && chrRacesRecord.CinematicSequenceID != 0)
                                pCurrChar.SendCinematicStart(chrRacesRecord.CinematicSequenceID);
                            break;
                        case PlayerCreateMode.NPE:
                            if (playerInfo.introSceneIdNPE.HasValue)
                                pCurrChar.GetSceneMgr().PlayScene(playerInfo.introSceneIdNPE.Value);
                            break;
                        default:
                            break;
                    }
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
                if (guild != null)
                    guild.SendLoginInfo(this);
                else
                {
                    // remove wrong guild data
                    Log.outError(LogFilter.Server, "Player {0} ({1}) marked as member of not existing guild (id: {2}), removing guild membership for player.", pCurrChar.GetName(), pCurrChar.GetGUID().ToString(),
                        pCurrChar.GetGuildId());
                    pCurrChar.SetInGuild(0);
                }
            }

            pCurrChar.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Login);

            pCurrChar.SendInitialPacketsAfterAddToMap();

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_ONLINE);
            stmt.AddValue(0, pCurrChar.GetGUID().GetCounter());
            DB.Characters.Execute(stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_ONLINE);
            stmt.AddValue(0, GetAccountId());
            DB.Login.Execute(stmt);

            pCurrChar.SetInGameTime(GameTime.GetGameTimeMS());

            // announce group about member online (must be after add to player list to receive announce to self)
            Group group = pCurrChar.GetGroup();
            if (group != null)
            {
                group.SendUpdate();
                if (group.GetLeaderGUID() == pCurrChar.GetGUID())
                    group.StopLeaderOfflineTimer();
            }

            // friend status
            Global.SocialMgr.SendFriendStatus(pCurrChar, FriendsResult.Online, pCurrChar.GetGUID(), true);

            // Place character in world (and load zone) before some object loading
            pCurrChar.LoadCorpse(holder.GetResult(PlayerLoginQueryLoad.CorpseLocation));

            // setting Ghost+speed if dead
            if (pCurrChar.GetDeathState() == DeathState.Dead)
            {
                // not blizz like, we must correctly save and load player instead...
                if (pCurrChar.GetRace() == Race.NightElf && !pCurrChar.HasAura(20584))
                    pCurrChar.CastSpell(pCurrChar, 20584, new CastSpellExtraArgs(true));// auras SPELL_AURA_INCREASE_SPEED(+speed in wisp form), SPELL_AURA_INCREASE_SWIM_SPEED(+swim speed in wisp form), SPELL_AURA_TRANSFORM (to wisp form)

                if (!pCurrChar.HasAura(8326))
                    pCurrChar.CastSpell(pCurrChar, 8326, new CastSpellExtraArgs(true));     // auras SPELL_AURA_GHOST, SPELL_AURA_INCREASE_SPEED(why?), SPELL_AURA_INCREASE_SWIM_SPEED(why?)

                pCurrChar.SetWaterWalking(true);

                int delay = pCurrChar.CalculateCorpseReclaimDelay(true);
                if (delay > 0)
                    pCurrChar.SendCorpseReclaimDelay(delay);
            }

            pCurrChar.ContinueTaxiFlight();

            // reset for all pets before pet loading
            if (pCurrChar.HasAtLoginFlag(AtLoginFlags.ResetPetTalents))
            {
                // Delete all of the player's pet spells
                PreparedStatement stmtSpells = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ALL_PET_SPELLS_BY_OWNER);
                stmtSpells.AddValue(0, pCurrChar.GetGUID().GetCounter());
                DB.Characters.Execute(stmtSpells);

                // Then reset all of the player's pet specualizations
                PreparedStatement stmtSpec = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_PET_SPECS_BY_OWNER);
                stmtSpec.AddValue(0, pCurrChar.GetGUID().GetCounter());
                DB.Characters.Execute(stmtSpec);
            }

            // Load pet if any (if player not alive and in taxi flight or another then pet will remember as temporary unsummoned)
            pCurrChar.ResummonPetTemporaryUnSummonedIfAny();

            // Set FFA PvP for non GM in non-rest mode
            if (Global.WorldMgr.IsFFAPvPRealm() && !pCurrChar.IsGameMaster() && !pCurrChar.HasPlayerFlag(PlayerFlags.Resting))
                pCurrChar.SetPvpFlag(UnitPVPStateFlags.FFAPvp);

            if (pCurrChar.HasPlayerFlag(PlayerFlags.ContestedPVP))
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

            bool firstLogin = pCurrChar.HasAtLoginFlag(AtLoginFlags.FirstLogin);
            if (firstLogin)
            {
                pCurrChar.RemoveAtLoginFlag(AtLoginFlags.FirstLogin);

                PlayerInfo info = Global.ObjectMgr.GetPlayerInfo(pCurrChar.GetRace(), pCurrChar.GetClass());
                foreach (var spellId in info.castSpells[(int)pCurrChar.GetCreateMode()])
                    pCurrChar.CastSpell(pCurrChar, spellId, new CastSpellExtraArgs(true));

                // start with every map explored
                if (WorldConfig.GetBoolValue(WorldCfg.StartAllExplored))
                {
                    for (int i = 0; i < PlayerConst.ExploredZonesSize; i++)
                        pCurrChar.AddExploredZones(i, 0xFFFFFFFFFFFFFFFF);
                }

                //Reputations if "StartAllReputation" is enabled
                if (WorldConfig.GetBoolValue(WorldCfg.StartAllRep))
                {
                    ReputationMgr repMgr = pCurrChar.GetReputationMgr();
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(942), 42999, false); // Cenarion Expedition
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(935), 42999, false); // The Sha'tar
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(936), 42999, false); // Shattrath City
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1011), 42999, false); // Lower City
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(970), 42999, false); // Sporeggar
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(967), 42999, false); // The Violet Eye
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(989), 42999, false); // Keepers of Time
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(932), 42999, false); // The Aldor
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(934), 42999, false); // The Scryers
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1038), 42999, false); // Ogri'la
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1077), 42999, false); // Shattered Sun Offensive
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1106), 42999, false); // Argent Crusade
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1104), 42999, false); // Frenzyheart Tribe
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1090), 42999, false); // Kirin Tor
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1098), 42999, false); // Knights of the Ebon Blade
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1156), 42999, false); // The Ashen Verdict
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1073), 42999, false); // The Kalu'ak
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1105), 42999, false); // The Oracles
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1119), 42999, false); // The Sons of Hodir
                    repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1091), 42999, false); // The Wyrmrest Accord

                    // Factions depending on team, like cities and some more stuff
                    switch (pCurrChar.GetTeam())
                    {
                        case Team.Alliance:
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(72), 42999, false); // Stormwind
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(47), 42999, false); // Ironforge
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(69), 42999, false); // Darnassus
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(930), 42999, false); // Exodar
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(730), 42999, false); // Stormpike Guard
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(978), 42999, false); // Kurenai
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(54), 42999, false); // Gnomeregan Exiles
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(946), 42999, false); // Honor Hold
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1037), 42999, false); // Alliance Vanguard
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1068), 42999, false); // Explorers' League
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1126), 42999, false); // The Frostborn
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1094), 42999, false); // The Silver Covenant
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1050), 42999, false); // Valiance Expedition
                            break;
                        case Team.Horde:
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(76), 42999, false); // Orgrimmar
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(68), 42999, false); // Undercity
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(81), 42999, false); // Thunder Bluff
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(911), 42999, false); // Silvermoon City
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(729), 42999, false); // Frostwolf Clan
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(941), 42999, false); // The Mag'har
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(530), 42999, false); // Darkspear Trolls
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(947), 42999, false); // Thrallmar
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1052), 42999, false); // Horde Expedition
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1067), 42999, false); // The Hand of Vengeance
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1124), 42999, false); // The Sunreavers
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1064), 42999, false); // The Taunka
                            repMgr.SetOneFactionReputation(CliDB.FactionStorage.LookupByKey(1085), 42999, false); // Warsong Offensive
                            break;
                        default:
                            break;
                    }
                    repMgr.SendState(null);
                }
            }

            // show time before shutdown if shutdown planned.
            if (Global.WorldMgr.IsShuttingDown())
                Global.WorldMgr.ShutdownMsg(true, pCurrChar);

            if (WorldConfig.GetBoolValue(WorldCfg.AllTaxiPaths))
                pCurrChar.SetTaxiCheater(true);

            if (pCurrChar.IsGameMaster())
                SendNotification(CypherStrings.GmOn);

            string IP_str = GetRemoteAddress();
            Log.outDebug(LogFilter.Network, $"Account: {GetAccountId()} (IP: {GetRemoteAddress()}) Login Character: [{pCurrChar.GetName()}] ({pCurrChar.GetGUID()}) Level: {pCurrChar.GetLevel()}, XP: {_player.GetXP()}/{_player.GetXPForNextLevel()} ({_player.GetXPForNextLevel() - _player.GetXP()} left)");

            if (!pCurrChar.IsStandState() && !pCurrChar.HasUnitState(UnitState.Stunned))
                pCurrChar.SetStandState(UnitStandStateType.Stand);

            pCurrChar.UpdateAverageItemLevelTotal();
            pCurrChar.UpdateAverageItemLevelEquipped();

            m_playerLoading.Clear();

            _player.UpdateMountCapability();

            // Handle Login-Achievements (should be handled after loading)
            _player.UpdateCriteria(CriteriaType.Login, 1);

            Global.ScriptMgr.OnPlayerLogin(pCurrChar, firstLogin);
        }

        public void AbortLogin(LoginFailureReason reason)
        {
            if (!PlayerLoading() || GetPlayer() != null)
            {
                KickPlayer("WorldSession::AbortLogin incorrect player state when logging in");
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
            FeatureSystemStatus features = new();

            // START OF DUMMY VALUES
            features.ComplaintStatus = (byte)ComplaintStatus.EnabledWithAutoIgnore;
            features.CfgRealmID = 2;
            features.CfgRealmRecID = 0;
            features.CommercePricePollTimeSeconds = 300;
            features.VoiceEnabled = false;

            EuropaTicketConfig europaTicketSystemStatus = new();
            europaTicketSystemStatus.ThrottleState.MaxTries = 10;
            europaTicketSystemStatus.ThrottleState.PerMilliseconds = 60000;
            europaTicketSystemStatus.ThrottleState.TryCount = 1;
            europaTicketSystemStatus.ThrottleState.LastResetTimeBeforeNow = 111111;
            features.TutorialEnabled = true;
            features.NPETutorialsEnabled = true;
            // END OF DUMMY VALUES

            europaTicketSystemStatus.TicketsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportTicketsEnabled);
            europaTicketSystemStatus.BugsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportBugsEnabled);
            europaTicketSystemStatus.ComplaintsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportComplaintsEnabled);
            europaTicketSystemStatus.SuggestionsEnabled = WorldConfig.GetBoolValue(WorldCfg.SupportSuggestionsEnabled);

            features.EuropaTicketSystemStatus = europaTicketSystemStatus;

            features.CharUndeleteEnabled = WorldConfig.GetBoolValue(WorldCfg.FeatureSystemCharacterUndeleteEnabled);
            features.WarModeEnabled = WorldConfig.GetBoolValue(WorldCfg.FeatureSystemWarModeEnabled);
            features.IsChatMuted = !CanSpeak();

            features.SpeakForMeAllowed = false;

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
            GetPlayer().SetWatchedFactionIndex(packet.FactionIndex);
        }

        [WorldPacketHandler(ClientOpcodes.SetFactionInactive)]
        void HandleSetFactionInactive(SetFactionInactive packet)
        {
            GetPlayer().GetReputationMgr().SetInactive(packet.Index, packet.State);
        }

        [WorldPacketHandler(ClientOpcodes.CheckCharacterNameAvailability)]
        void HandleCheckCharacterNameAvailability(CheckCharacterNameAvailability checkCharacterNameAvailability)
        {
            // prevent character rename to invalid name
            if (!ObjectManager.NormalizePlayerName(ref checkCharacterNameAvailability.Name))
            {
                SendPacket(new CheckCharacterNameAvailabilityResult(checkCharacterNameAvailability.SequenceIndex, ResponseCodes.CharNameNoName));
                return;
            }

            ResponseCodes res = ObjectManager.CheckPlayerName(checkCharacterNameAvailability.Name, GetSessionDbcLocale(), true);
            if (res != ResponseCodes.CharNameSuccess)
            {
                SendPacket(new CheckCharacterNameAvailabilityResult(checkCharacterNameAvailability.SequenceIndex, res));
                return;
            }

            // check name limitations
            if (!HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(checkCharacterNameAvailability.Name))
            {
                SendPacket(new CheckCharacterNameAvailabilityResult(checkCharacterNameAvailability.SequenceIndex, ResponseCodes.CharNameReserved));
                return;
            }

            // Ensure that there is no character with the desired new name
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
            stmt.AddValue(0, checkCharacterNameAvailability.Name);

            var sequenceIndex = checkCharacterNameAvailability.SequenceIndex;
            _queryProcessor.AddCallback(DB.Characters.AsyncQuery(stmt).WithCallback(result =>
            {
                SendPacket(new CheckCharacterNameAvailabilityResult(sequenceIndex, !result.IsEmpty() ? ResponseCodes.CharCreateNameInUse : ResponseCodes.Success));
            }));
        }

        [WorldPacketHandler(ClientOpcodes.CharacterRenameRequest, Status = SessionStatus.Authed)]
        void HandleCharRename(CharacterRenameRequest request)
        {
            if (!_legitCharacters.Contains(request.RenameInfo.Guid))
            {
                Log.outError(LogFilter.Network, "Account {0}, IP: {1} tried to rename character {2}, but it does not belong to their account!",
                    GetAccountId(), GetRemoteAddress(), request.RenameInfo.Guid.ToString());
                KickPlayer("WorldSession::HandleCharRenameOpcode rename character from a different account");
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
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_FREE_NAME);
            stmt.AddValue(0, request.RenameInfo.Guid.GetCounter());
            stmt.AddValue(1, request.RenameInfo.NewName);

            _queryProcessor.AddCallback(DB.Characters.AsyncQuery(stmt).WithCallback(HandleCharRenameCallBack, request.RenameInfo));
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

            SQLTransaction trans = new();
            ulong lowGuid = renameInfo.Guid.GetCounter();

            // Update name and at_login flag in the db
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_NAME_AT_LOGIN);
            stmt.AddValue(0, renameInfo.NewName);
            stmt.AddValue(1, (ushort)atLoginFlags);
            stmt.AddValue(2, lowGuid);
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
            stmt.AddValue(0, lowGuid);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] ({3}) Changed name to: {4}",
                GetAccountId(), GetRemoteAddress(), oldName, renameInfo.Guid.ToString(), renameInfo.NewName);

            SendCharRename(ResponseCodes.Success, renameInfo);

            Global.CharacterCacheStorage.UpdateCharacterData(renameInfo.Guid, renameInfo.NewName);
        }

        [WorldPacketHandler(ClientOpcodes.SetPlayerDeclinedNames, Status = SessionStatus.Authed)]
        void HandleSetPlayerDeclinedNames(SetPlayerDeclinedNames packet)
        {
            // not accept declined names for unsupported languages
            string name;
            if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(packet.Player, out name))
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
                CharacterDatabase.EscapeString(ref declinedName);
                packet.DeclinedNames.name[i] = declinedName;
            }

            SQLTransaction trans = new();

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
            stmt.AddValue(0, packet.Player.GetCounter());
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_DECLINED_NAME);
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
            if (packet.CustomizedChrModelID != 0)
            {
                var conditionalChrModel = Global.DB2Mgr.GetConditionalChrModel(packet.CustomizedChrModelID);
                if (conditionalChrModel == null)
                    return;

                var req = CliDB.ChrCustomizationReqStorage.LookupByKey(conditionalChrModel.ChrCustomizationReqID);
                if (req != null)
                    if (!MeetsChrCustomizationReq(req, (Race)packet.CustomizedRace, _player.GetClass(), false, packet.Customizations))
                        return;

                if (!ConditionManager.IsPlayerMeetingCondition(_player, (uint)conditionalChrModel.PlayerConditionID))
                    return;
            }

            if (!ValidateAppearance(_player.GetRace(), _player.GetClass(), (Gender)packet.NewSex, packet.Customizations))
                return;

            GameObject go = GetPlayer().FindNearestGameObjectOfType(GameObjectTypes.BarberChair, 5.0f);
            if (go == null)
            {
                SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NotOnChair));
                return;
            }

            if (GetPlayer().GetStandState() != (UnitStandStateType)((int)UnitStandStateType.SitLowChair + go.GetGoInfo().BarberChair.chairheight))
            {
                SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NotOnChair));
                return;
            }

            long cost = GetPlayer().GetBarberShopCost(packet.Customizations);
            if (!GetPlayer().HasEnoughMoney(cost))
            {
                SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.NoMoney));
                return;
            }

            SendPacket(new BarberShopResult(BarberShopResult.ResultEnum.Success));

            _player.ModifyMoney(-cost);
            _player.UpdateCriteria(CriteriaType.MoneySpentAtBarberShop, (ulong)cost);

            if (_player.GetNativeGender() != (Gender)packet.NewSex)
            {
                _player.SetNativeGender((Gender)packet.NewSex);
                _player.InitDisplayIds();
                _player.RestoreDisplayId(false);
            }

            _player.SetCustomizations(packet.Customizations);

            _player.UpdateCriteria(CriteriaType.GotHaircut, 1);

            _player.SetStandState(UnitStandStateType.Stand);

            Global.CharacterCacheStorage.UpdateCharacterGender(_player.GetGUID(), packet.NewSex);
        }

        [WorldPacketHandler(ClientOpcodes.CharCustomize, Status = SessionStatus.Authed)]
        void HandleCharCustomize(CharCustomize packet)
        {
            if (!_legitCharacters.Contains(packet.CustomizeInfo.CharGUID))
            {
                Log.outError(LogFilter.Network, "Account {0}, IP: {1} tried to customise {2}, but it does not belong to their account!",
                    GetAccountId(), GetRemoteAddress(), packet.CustomizeInfo.CharGUID.ToString());
                KickPlayer("WorldSession::HandleCharCustomize Trying to customise character of another account");
                return;
            }

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_CUSTOMIZE_INFO);
            stmt.AddValue(0, packet.CustomizeInfo.CharGUID.GetCounter());

            _queryProcessor.AddCallback(DB.Characters.AsyncQuery(stmt).WithCallback(HandleCharCustomizeCallback, packet.CustomizeInfo));
        }

        void HandleCharCustomizeCallback(CharCustomizeInfo customizeInfo, SQLResult result)
        {
            if (result.IsEmpty())
            {
                SendCharCustomize(ResponseCodes.CharCreateError, customizeInfo);
                return;
            }

            string oldName = result.Read<string>(0);
            Race plrRace = (Race)result.Read<byte>(1);
            Class plrClass = (Class)result.Read<byte>(2);
            Gender plrGender = (Gender)result.Read<byte>(3);
            AtLoginFlags atLoginFlags = (AtLoginFlags)result.Read<ushort>(4);

            if (!ValidateAppearance(plrRace, plrClass, plrGender, customizeInfo.Customizations))
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
            ObjectGuid newGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(customizeInfo.CharName);
            if (!newGuid.IsEmpty())
            {
                if (newGuid != customizeInfo.CharGUID)
                {
                    SendCharCustomize(ResponseCodes.CharCreateNameInUse, customizeInfo);
                    return;
                }
            }

            PreparedStatement stmt;
            SQLTransaction trans = new();
            ulong lowGuid = customizeInfo.CharGUID.GetCounter();

            // Customize
            Player.SaveCustomizations(trans, lowGuid, customizeInfo.Customizations);

            // Name Change and update atLogin flags
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_NAME_AT_LOGIN);
                stmt.AddValue(0, customizeInfo.CharName);
                stmt.AddValue(1, (ushort)atLoginFlags);
                stmt.AddValue(2, lowGuid);
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
                stmt.AddValue(0, lowGuid);

                trans.Append(stmt);
            }

            DB.Characters.CommitTransaction(trans);

            Global.CharacterCacheStorage.UpdateCharacterData(customizeInfo.CharGUID, customizeInfo.CharName, (byte)customizeInfo.SexID);

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
                            if (item == null)
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

                            (bool hasAppearance, _) = GetCollectionMgr().HasItemAppearance((uint)saveEquipmentSet.Set.Appearances[i]);
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

                    if (illusion.ItemVisual == 0 || !illusion.HasFlag(SpellItemEnchantmentFlags.AllowTransmog))
                        return false;

                    if (!ConditionManager.IsPlayerMeetingCondition(_player, illusion.TransmogUseConditionID))
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

        [WorldPacketHandler(ClientOpcodes.UseEquipmentSet, Processing = PacketProcessing.Inplace)]
        void HandleUseEquipmentSet(UseEquipmentSet useEquipmentSet)
        {
            ObjectGuid ignoredItemGuid = new(0x0C00040000000000, 0xFFFFFFFFFFFFFFFF);
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
                if (item == null)
                {
                    Item uItem = GetPlayer().GetItemByPos(InventorySlots.Bag0, i);
                    if (uItem == null)
                        continue;

                    List<ItemPosCount> itemPosCount = new();
                    InventoryResult inventoryResult = GetPlayer().CanStoreItem(ItemConst.NullBag, ItemConst.NullSlot, itemPosCount, uItem, false);
                    if (inventoryResult == InventoryResult.Ok)
                    {
                        if (_player.CanUnequipItem(dstPos, true) != InventoryResult.Ok)
                            continue;

                        GetPlayer().RemoveItem(InventorySlots.Bag0, i, true);
                        GetPlayer().StoreItem(itemPosCount, uItem, true);
                    }
                    else
                        GetPlayer().SendEquipError(inventoryResult, uItem);

                    continue;
                }

                if (item.GetPos() == dstPos)
                    continue;

                if (_player.CanEquipItem(i, out dstPos, item, true) != InventoryResult.Ok)
                    continue;

                GetPlayer().SwapItem(item.GetPos(), dstPos);
            }

            UseEquipmentSetResult result = new();
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
                KickPlayer("WorldSession::HandleCharFactionOrRaceChange Trying to change faction of character of another account");
                return;
            }

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_RACE_OR_FACTION_CHANGE_INFOS);
            stmt.AddValue(0, packet.RaceOrFactionChangeInfo.Guid.GetCounter());

            _queryProcessor.AddCallback(DB.Characters.AsyncQuery(stmt).WithCallback(HandleCharRaceOrFactionChangeCallback, packet.RaceOrFactionChangeInfo));
        }

        void HandleCharRaceOrFactionChangeCallback(CharRaceOrFactionChangeInfo factionChangeInfo, SQLResult result)
        {
            if (result.IsEmpty())
            {
                SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);
                return;
            }

            // get the players old (at this moment current) race
            CharacterCacheEntry characterInfo = Global.CharacterCacheStorage.GetCharacterCacheByGuid(factionChangeInfo.Guid);
            if (characterInfo == null)
            {
                SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);
                return;
            }

            string oldName = characterInfo.Name;
            Race oldRace = characterInfo.RaceId;
            Class playerClass = characterInfo.ClassId;
            byte level = characterInfo.Level;

            if (Global.ObjectMgr.GetPlayerInfo(factionChangeInfo.RaceID, playerClass) == null)
            {
                SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);
                return;
            }

            AtLoginFlags atLoginFlags = (AtLoginFlags)result.Read<ushort>(0);
            string knownTitlesStr = result.Read<string>(1);
            uint groupId = !result.IsNull(2) ? result.Read<uint>(2) : 0;

            AtLoginFlags usedLoginFlag = (factionChangeInfo.FactionChange ? AtLoginFlags.ChangeFaction : AtLoginFlags.ChangeRace);
            if (!atLoginFlags.HasAnyFlag(usedLoginFlag))
            {
                SendCharFactionChange(ResponseCodes.CharCreateError, factionChangeInfo);
                return;
            }

            uint newTeamId = Player.TeamIdForRace(factionChangeInfo.RaceID);
            if (newTeamId == BattleGroundTeamId.Neutral)
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
                RaceMask<ulong> raceMaskDisabled = new(WorldConfig.GetUInt64Value(WorldCfg.CharacterCreatingDisabledRacemask));
                if (raceMaskDisabled.HasRace(factionChangeInfo.RaceID))
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
            ObjectGuid newGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(factionChangeInfo.Name);
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
            SQLTransaction trans = new();

            // resurrect the character in case he's dead
            Player.OfflineResurrect(factionChangeInfo.Guid, trans);

            // Name Change and update atLogin flags
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_NAME_AT_LOGIN);
                stmt.AddValue(0, factionChangeInfo.Name);
                stmt.AddValue(1, (ushort)((atLoginFlags | AtLoginFlags.Resurrect) & ~usedLoginFlag));
                stmt.AddValue(2, lowGuid);

                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
                stmt.AddValue(0, lowGuid);

                trans.Append(stmt);
            }

            // Customize
            Player.SaveCustomizations(trans, lowGuid, factionChangeInfo.Customizations);

            // Race Change
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_RACE);
                stmt.AddValue(0, (byte)factionChangeInfo.RaceID);
                stmt.AddValue(1, (ushort)PlayerExtraFlags.HasRaceChanged);
                stmt.AddValue(2, lowGuid);

                trans.Append(stmt);
            }

            Global.CharacterCacheStorage.UpdateCharacterData(factionChangeInfo.Guid, factionChangeInfo.Name, (byte)factionChangeInfo.SexID, (byte)factionChangeInfo.RaceID);

            if (oldRace != factionChangeInfo.RaceID)
            {
                // Switch Languages
                // delete all languages first
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SKILL_LANGUAGES);
                stmt.AddValue(0, lowGuid);
                trans.Append(stmt);

                // Now add them back
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_SKILL_LANGUAGE);
                stmt.AddValue(0, lowGuid);

                // Faction specific languages
                if (newTeamId == BattleGroundTeamId.Horde)
                    stmt.AddValue(1, 109);
                else
                    stmt.AddValue(1, 98);

                trans.Append(stmt);

                // Race specific languages
                Race[] racesWithoutSpecificLanguages = { Race.Orc, Race.Human, Race.MagharOrc, Race.KulTiran };
                if (!racesWithoutSpecificLanguages.Contains(factionChangeInfo.RaceID))
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_CHAR_SKILL_LANGUAGE);
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
                        case Race.MechaGnome:
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
                        case Race.ZandalariTroll:
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
                        case Race.Vulpera:
                            stmt.AddValue(1, 2776);
                            break;
                        case Race.PandarenNeutral:
                        case Race.PandarenAlliance:
                        case Race.PandarenHorde:
                            stmt.AddValue(1, 905);
                            break;
                        case Race.DracthyrAlliance:
                        case Race.DracthyrHorde:
                            stmt.AddValue(1, 138);
                            break;
                        case Race.EarthenDwarfHorde:
                        case Race.EarthenDwarfAlliance:
                            stmt.AddValue(1, 140);
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
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_TAXI_PATH);
                    stmt.AddValue(0, lowGuid);
                    trans.Append(stmt);

                    if (level > 7)
                    {
                        // Update Taxi path
                        // this doesn't seem to be 100% blizzlike... but it can't really be helped.
                        string taximaskstream = "";


                        var factionMask = newTeamId == BattleGroundTeamId.Horde ? DB2Manager.HordeTaxiNodesMask : DB2Manager.AllianceTaxiNodesMask;
                        for (int i = 0; i < factionMask.Length; ++i)
                        {
                            // i = (315 - 1) / 8 = 39
                            // m = 1 << ((315 - 1) % 8) = 4
                            int deathKnightExtraNode = playerClass != Class.Deathknight || i != 39 ? 0 : 4;
                            taximaskstream += (uint)(factionMask[i] | deathKnightExtraNode) + ' ';
                        }

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_TAXIMASK);
                        stmt.AddValue(0, taximaskstream);
                        stmt.AddValue(1, lowGuid);
                        trans.Append(stmt);
                    }

                    if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild))
                    {
                        // Reset guild
                        Guild guild = Global.GuildMgr.GetGuildById(characterInfo.GuildId);
                        if (guild != null)
                            guild.DeleteMember(trans, factionChangeInfo.Guid, false, false, true);

                        Player.LeaveAllArenaTeams(factionChangeInfo.Guid);
                    }

                    if (groupId != 0 && !WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup))
                    {
                        Group group = Global.GroupMgr.GetGroupByDbStoreId(groupId);
                        if (group != null)
                            group.RemoveMember(factionChangeInfo.Guid);
                    }

                    if (!HasPermission(RBACPermissions.TwoSideAddFriend))
                    {
                        // Delete Friend List
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_GUID);
                        stmt.AddValue(0, lowGuid);
                        trans.Append(stmt);

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SOCIAL_BY_FRIEND);
                        stmt.AddValue(0, lowGuid);
                        trans.Append(stmt);
                    }

                    // Reset homebind and position
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_PLAYER_HOMEBIND);
                    stmt.AddValue(0, lowGuid);
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_PLAYER_HOMEBIND);
                    stmt.AddValue(0, lowGuid);

                    WorldLocation loc;
                    ushort zoneId = 0;
                    if (newTeamId == BattleGroundTeamId.Alliance)
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

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_ACHIEVEMENT_BY_ACHIEVEMENT);
                        stmt.AddValue(0, (ushort)(newTeamId == BattleGroundTeamId.Alliance ? achiev_alliance : achiev_horde));
                        stmt.AddValue(1, lowGuid);
                        trans.Append(stmt);

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_ACHIEVEMENT);
                        stmt.AddValue(0, (ushort)(newTeamId == BattleGroundTeamId.Alliance ? achiev_alliance : achiev_horde));
                        stmt.AddValue(1, (ushort)(newTeamId == BattleGroundTeamId.Alliance ? achiev_horde : achiev_alliance));
                        stmt.AddValue(2, lowGuid);
                        trans.Append(stmt);
                    }

                    // Item conversion
                    var itemConversionMap = newTeamId == BattleGroundTeamId.Alliance ? Global.ObjectMgr.FactionChangeItemsHordeToAlliance : Global.ObjectMgr.FactionChangeItemsAllianceToHorde;
                    foreach (var it in itemConversionMap)
                    {
                        uint oldItemId = it.Key;
                        uint newItemId = it.Value;

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_INVENTORY_FACTION_CHANGE);
                        stmt.AddValue(0, newItemId);
                        stmt.AddValue(1, oldItemId);
                        stmt.AddValue(2, lowGuid);
                        trans.Append(stmt);
                    }

                    // Delete all current quests
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS);
                    stmt.AddValue(0, lowGuid);
                    trans.Append(stmt);

                    // Quest conversion
                    foreach (var it in Global.ObjectMgr.FactionChangeQuests)
                    {
                        uint quest_alliance = it.Key;
                        uint quest_horde = it.Value;

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_QUESTSTATUS_REWARDED_BY_QUEST);
                        stmt.AddValue(0, lowGuid);
                        stmt.AddValue(1, (newTeamId == BattleGroundTeamId.Alliance ? quest_alliance : quest_horde));
                        trans.Append(stmt);

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_FACTION_CHANGE);
                        stmt.AddValue(0, (newTeamId == BattleGroundTeamId.Alliance ? quest_alliance : quest_horde));
                        stmt.AddValue(1, (newTeamId == BattleGroundTeamId.Alliance ? quest_horde : quest_alliance));
                        stmt.AddValue(2, lowGuid);
                        trans.Append(stmt);
                    }

                    // Mark all rewarded quests as "active" (will count for completed quests achievements)
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_ACTIVE);
                    stmt.AddValue(0, lowGuid);
                    trans.Append(stmt);

                    // Disable all old-faction specific quests
                    {
                        var questTemplates = Global.ObjectMgr.GetQuestTemplates();
                        foreach (Quest quest in questTemplates.Values)
                        {
                            RaceMask<ulong> newRaceMask = newTeamId == BattleGroundTeamId.Alliance ? RaceMask.Alliance : RaceMask.Horde;
                            if (quest.AllowableRaces.RawValue != unchecked((ulong)-1) && (quest.AllowableRaces & newRaceMask).IsEmpty())
                            {
                                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_QUESTSTATUS_REWARDED_ACTIVE_BY_QUEST);
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

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_SPELL_BY_SPELL);
                        stmt.AddValue(0, (newTeamId == BattleGroundTeamId.Alliance ? spell_alliance : spell_horde));
                        stmt.AddValue(1, lowGuid);
                        trans.Append(stmt);

                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_SPELL_FACTION_CHANGE);
                        stmt.AddValue(0, (newTeamId == BattleGroundTeamId.Alliance ? spell_alliance : spell_horde));
                        stmt.AddValue(1, (newTeamId == BattleGroundTeamId.Alliance ? spell_horde : spell_alliance));
                        stmt.AddValue(2, lowGuid);
                        trans.Append(stmt);
                    }

                    // Reputation conversion
                    foreach (var it in Global.ObjectMgr.FactionChangeReputation)
                    {
                        uint reputation_alliance = it.Key;
                        uint reputation_horde = it.Value;
                        uint newReputation = (newTeamId == BattleGroundTeamId.Alliance) ? reputation_alliance : reputation_horde;
                        uint oldReputation = (newTeamId == BattleGroundTeamId.Alliance) ? reputation_horde : reputation_alliance;

                        // select old standing set in db
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_REP_BY_FACTION);
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

                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_REP_BY_FACTION);
                            stmt.AddValue(0, newReputation);
                            stmt.AddValue(1, lowGuid);
                            trans.Append(stmt);

                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_REP_FACTION_CHANGE);
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
                        List<uint> knownTitles = new();

                        var tokens = new StringArray(knownTitlesStr, ' ');
                        for (int index = 0; index < tokens.Length; ++index)
                        {
                            if (uint.TryParse(tokens[index], out uint id))
                                knownTitles.Add(id);
                        }

                        foreach (var it in Global.ObjectMgr.FactionChangeTitles)
                        {
                            uint title_alliance = it.Key;
                            uint title_horde = it.Value;

                            CharTitlesRecord atitleInfo = CliDB.CharTitlesStorage.LookupByKey(title_alliance);
                            CharTitlesRecord htitleInfo = CliDB.CharTitlesStorage.LookupByKey(title_horde);
                            // new team
                            if (newTeamId == BattleGroundTeamId.Alliance)
                            {
                                uint maskID = htitleInfo.MaskID;
                                int index = (int)maskID / 32;
                                if (index >= knownTitles.Count)
                                    continue;

                                uint old_flag = (uint)(1 << (int)(maskID % 32));
                                uint new_flag = (uint)(1 << (atitleInfo.MaskID % 32));
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
                                int index = (int)maskID / 32;
                                if (index >= knownTitles.Count)
                                    continue;

                                uint old_flag = (uint)(1 << (int)(maskID % 32));
                                uint new_flag = (uint)(1 << (htitleInfo.MaskID % 32));
                                if (Convert.ToBoolean(knownTitles[index] & old_flag))
                                {
                                    knownTitles[index] &= ~old_flag;
                                    // use index of the new title
                                    knownTitles[htitleInfo.MaskID / 32] |= new_flag;
                                }
                            }

                            string ss = "";
                            for (int index = 0; index < knownTitles.Count; ++index)
                                ss += knownTitles[index] + ' ';

                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_CHAR_TITLES_FACTION_CHANGE);
                            stmt.AddValue(0, ss);
                            stmt.AddValue(1, lowGuid);
                            trans.Append(stmt);

                            // unset any currently chosen title
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.RES_CHAR_TITLES_FACTION_CHANGE);
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
            if (GetPlayer().m_activePlayerData.XP != 0)
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
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_LAST_CHAR_UNDELETE);
            stmt.AddValue(0, GetBattlenetAccountId());

            _queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt).WithCallback(HandleUndeleteCooldownStatusCallback));
        }

        void HandleUndeleteCooldownStatusCallback(SQLResult result)
        {
            uint cooldown = 0;
            uint maxCooldown = WorldConfig.GetUIntValue(WorldCfg.FeatureSystemCharacterUndeleteCooldown);
            if (!result.IsEmpty())
            {
                uint lastUndelete = result.Read<uint>(0);
                uint now = (uint)GameTime.GetGameTime();
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

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_LAST_CHAR_UNDELETE);
            stmt.AddValue(0, GetBattlenetAccountId());

            CharacterUndeleteInfo undeleteInfo = undeleteCharacter.UndeleteInfo;
            _queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt).WithChainingCallback((queryCallback, result) =>
            {
                if (!result.IsEmpty())
                {
                    uint lastUndelete = result.Read<uint>(0);
                    uint maxCooldown = WorldConfig.GetUIntValue(WorldCfg.FeatureSystemCharacterUndeleteCooldown);
                    if (lastUndelete != 0 && (lastUndelete + maxCooldown > GameTime.GetGameTime()))
                    {
                        SendUndeleteCharacterResponse(CharacterUndeleteResult.Cooldown, undeleteInfo);
                        return;
                    }
                }

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_GUID);
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

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
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

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_SUM_CHARS);
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

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_RESTORE_DELETE_INFO);
                stmt.AddValue(0, undeleteInfo.Name);
                stmt.AddValue(1, GetAccountId());
                stmt.AddValue(2, undeleteInfo.CharacterGuid.GetCounter());
                DB.Characters.Execute(stmt);

                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_LAST_CHAR_UNDELETE);
                stmt.AddValue(0, GetBattlenetAccountId());
                DB.Login.Execute(stmt);

                Global.CharacterCacheStorage.UpdateCharacterInfoDeleted(undeleteInfo.CharacterGuid, false, undeleteInfo.Name);

                SendUndeleteCharacterResponse(CharacterUndeleteResult.Ok, undeleteInfo);
            }));
        }

        [WorldPacketHandler(ClientOpcodes.RepopRequest)]
        void HandleRepopRequest(RepopRequest packet)
        {
            if (GetPlayer().IsAlive() || GetPlayer().HasPlayerFlag(PlayerFlags.Ghost))
                return;

            if (GetPlayer().HasAuraType(AuraType.PreventResurrection))
                return; // silently return, client should display the error by itself

            // the world update order is sessions, players, creatures
            // the netcode runs in parallel with all of these
            // creatures can kill players
            // so if the server is lagging enough the player can
            // release spirit after he's killed but before he is updated
            if (GetPlayer().GetDeathState() == DeathState.JustDied)
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
            if (GetPlayer().IsAlive() || !GetPlayer().HasPlayerFlag(PlayerFlags.Ghost))
                return;
            GetPlayer().RepopAtGraveyard();
        }

        [WorldPacketHandler(ClientOpcodes.RequestCemeteryList, Processing = PacketProcessing.Inplace)]
        void HandleRequestCemeteryList(RequestCemeteryList requestCemeteryList)
        {
            uint zoneId = GetPlayer().GetZoneId();
            uint team = (uint)GetPlayer().GetTeam();

            List<uint> graveyardIds = new();
            var range = Global.ObjectMgr.GraveyardStorage.LookupByKey(zoneId);

            for (uint i = 0; i < range.Count && graveyardIds.Count < 16; ++i) // client max
            {
                var gYard = range[(int)i];
                ConditionSourceInfo conditionSource = new(_player);
                if (!gYard.Conditions.Meets(conditionSource))
                    continue;

                graveyardIds.Add(i);
            }

            if (graveyardIds.Empty())
            {
                Log.outDebug(LogFilter.Network, "No graveyards found for zone {0} for player {1} (team {2}) in CMSG_REQUEST_CEMETERY_LIST",
                    zoneId, m_GUIDLow, team);
                return;
            }

            RequestCemeteryListResponse packet = new();
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
            if (!GetPlayer().HasPlayerFlag(PlayerFlags.Ghost))
                return;

            Corpse corpse = GetPlayer().GetCorpse();
            if (corpse == null)
                return;

            // prevent resurrect before 30-sec delay after body release not finished
            if ((corpse.GetGhostTime() + GetPlayer().GetCorpseReclaimDelay(corpse.GetCorpseType() == CorpseType.ResurrectablePVP)) > GameTime.GetGameTime())
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
            if (ressPlayer != null)
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
            switch (packet.StandState)
            {
                case UnitStandStateType.Stand:
                case UnitStandStateType.Sit:
                case UnitStandStateType.Sleep:
                case UnitStandStateType.Kneel:
                    break;
                default:
                    return;
            }

            GetPlayer().SetStandState(packet.StandState);
        }

        [WorldPacketHandler(ClientOpcodes.SavePersonalEmblem)]
        void HandleSavePersonalEmblem(SavePersonalEmblem savePersonalEmblem)
        {
            if (_player.GetNPCIfCanInteractWith(savePersonalEmblem.Vendor, NPCFlags.None, NPCFlags2.PersonalTabardDesigner) == null)
            {
                SendPacket(new PlayerSavePersonalEmblem(GuildEmblemError.InvalidVendor));
                return;
            }

            if (!Guild.EmblemInfo.ValidateEmblemColors((uint)savePersonalEmblem.PersonalTabard.EmblemStyle, (uint)savePersonalEmblem.PersonalTabard.EmblemColor,
                (uint)savePersonalEmblem.PersonalTabard.BorderStyle, (uint)savePersonalEmblem.PersonalTabard.BorderColor, (uint)savePersonalEmblem.PersonalTabard.BackgroundColor))
            {
                SendPacket(new PlayerSavePersonalEmblem(GuildEmblemError.InvalidTabardColors));
                return;
            }

            _player.SetPersonalTabard(savePersonalEmblem.PersonalTabard.EmblemStyle, savePersonalEmblem.PersonalTabard.EmblemColor,
                savePersonalEmblem.PersonalTabard.BorderStyle, savePersonalEmblem.PersonalTabard.BorderColor,
                savePersonalEmblem.PersonalTabard.BackgroundColor);

            SendPacket(new PlayerSavePersonalEmblem(GuildEmblemError.Success));
        }

        void SendCharCreate(ResponseCodes result, ObjectGuid guid = default)
        {
            CreateChar response = new();
            response.Code = result;
            response.Guid = guid;

            SendPacket(response);
        }

        void SendCharDelete(ResponseCodes result)
        {
            DeleteChar response = new();
            response.Code = result;

            SendPacket(response);
        }

        void SendCharRename(ResponseCodes result, CharacterRenameInfo renameInfo)
        {
            CharacterRenameResult packet = new();
            packet.Result = result;
            packet.Name = renameInfo.NewName;
            if (result == ResponseCodes.Success)
                packet.Guid = renameInfo.Guid;

            SendPacket(packet);
        }

        void SendCharCustomize(ResponseCodes result, CharCustomizeInfo customizeInfo)
        {
            if (result == ResponseCodes.Success)
            {
                CharCustomizeSuccess response = new(customizeInfo);
                SendPacket(response);
            }
            else
            {
                CharCustomizeFailure failed = new();
                failed.Result = (byte)result;
                failed.CharGUID = customizeInfo.CharGUID;
                SendPacket(failed);
            }
        }

        void SendCharFactionChange(ResponseCodes result, CharRaceOrFactionChangeInfo factionChangeInfo)
        {
            CharFactionChangeResult packet = new();
            packet.Result = result;
            packet.Guid = factionChangeInfo.Guid;

            if (result == ResponseCodes.Success)
            {
                packet.Display = new();
                packet.Display.Name = factionChangeInfo.Name;
                packet.Display.SexID = (byte)factionChangeInfo.SexID;
                packet.Display.Customizations = factionChangeInfo.Customizations;
                packet.Display.RaceID = (byte)factionChangeInfo.RaceID;
            }

            SendPacket(packet);
        }

        void SendSetPlayerDeclinedNamesResult(DeclinedNameResult result, ObjectGuid guid)
        {
            SetPlayerDeclinedNamesResult packet = new();
            packet.ResultCode = result;
            packet.Player = guid;

            SendPacket(packet);
        }

        void SendUndeleteCooldownStatusResponse(uint currentCooldown, uint maxCooldown)
        {
            UndeleteCooldownStatusResponse response = new();
            response.OnCooldown = (currentCooldown > 0);
            response.MaxCooldown = maxCooldown;
            response.CurrentCooldown = currentCooldown;

            SendPacket(response);
        }

        void SendUndeleteCharacterResponse(CharacterUndeleteResult result, CharacterUndeleteInfo undeleteInfo)
        {
            UndeleteCharacterResponse response = new();
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

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.From, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_CUSTOMIZATIONS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Customizations, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_GROUP_MEMBER);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Group, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURAS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Auras, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURA_EFFECTS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.AuraEffects, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_AURA_STORED_LOCATIONS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.AuraStoredLocations, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELL);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Spells, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELL_FAVORITES);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.SpellFavorites, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.QuestStatus, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.QuestStatusObjectives, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.QuestStatusObjectivesCriteria, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_CRITERIA_PROGRESS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.QuestStatusObjectivesCriteriaProgress, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_OBJECTIVES_SPAWN_TRACKING);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.QuestStatusSpawnTracking, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_DAILY);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.DailyQuestStatus, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_WEEKLY);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.WeeklyQuestStatus, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_MONTHLY);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.MonthlyQuestStatus, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUS_SEASONAL);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.SeasonalQuestStatus, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_REPUTATION);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Reputation, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_INVENTORY);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Inventory, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_ARTIFACT);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Artifacts, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Azerite, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.AzeriteMilestonePowers, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.AzeriteUnlockedEssences, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.AzeriteEmpowered, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAIL);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Mails, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.MailItems, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_ARTIFACT);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.MailItemsArtifact, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.MailItemsAzerite, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_MILESTONE_POWER);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.MailItemsAzeriteMilestonePower, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_UNLOCKED_ESSENCE);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.MailItemsAzeriteUnlockedEssence, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_MAILITEMS_AZERITE_EMPOWERED);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.MailItemsAzeriteEmpowered, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SOCIALLIST);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.SocialList, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_HOMEBIND);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.HomeBind, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELLCOOLDOWNS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.SpellCooldowns, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SPELL_CHARGES);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.SpellCharges, stmt);

            if (WorldConfig.GetBoolValue(WorldCfg.DeclinedNamesUsed))
            {
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_DECLINEDNAMES);
                stmt.AddValue(0, lowGuid);
                SetQuery(PlayerLoginQueryLoad.DeclinedNames, stmt);
            }

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_GUILD_MEMBER);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Guild, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_ARENAINFO);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.ArenaInfo, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_ACHIEVEMENTS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Achievements, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_CRITERIAPROGRESS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.CriteriaProgress, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_EQUIPMENTSETS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.EquipmentSets, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_TRANSMOG_OUTFITS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.TransmogOutfits, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_CUF_PROFILES);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.CufProfiles, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_BGDATA);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.BgData, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GLYPHS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Glyphs, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_TALENTS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Talents, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_PVP_TALENTS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.PvpTalents, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PLAYER_ACCOUNT_DATA);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.AccountData, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_SKILLS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Skills, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_RANDOMBG);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.RandomBg, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_BANNED);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Banned, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_QUESTSTATUSREW);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.QuestStatusRew, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_ACCOUNT_INSTANCELOCKTIMES);
            stmt.AddValue(0, m_accountId);
            SetQuery(PlayerLoginQueryLoad.InstanceLockTimes, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PLAYER_CURRENCY);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Currency, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CORPSE_LOCATION);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.CorpseLocation, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_PETS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.PetSlots, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.Garrison, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_BLUEPRINTS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.GarrisonBlueprints, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_BUILDINGS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.GarrisonBuildings, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_FOLLOWERS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.GarrisonFollowers, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GARRISON_FOLLOWER_ABILITIES);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.GarrisonFollowerAbilities, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_TRAIT_ENTRIES);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.TraitEntries, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHAR_TRAIT_CONFIGS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.TraitConfigs, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PLAYER_DATA_ELEMENTS_CHARACTER);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.DataElements, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_PLAYER_DATA_FLAGS_CHARACTER);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.DataFlags, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_BANK_TAB_SETTINGS);
            stmt.AddValue(0, lowGuid);
            SetQuery(PlayerLoginQueryLoad.BankTabSettings, stmt);
        }

        public ObjectGuid GetGuid() { return m_guid; }

        uint GetAccountId() { return m_accountId; }

        uint m_accountId;
        ObjectGuid m_guid;
    }

    class EnumCharactersQueryHolder : SQLQueryHolder<EnumCharacterQueryLoad>
    {
        public bool Initialize(uint accountId, bool withDeclinedNames, bool isDeletedCharacters)
        {
            _isDeletedCharacters = isDeletedCharacters;

            CharStatements[][] statements =
            {
                new [] { CharStatements.SEL_ENUM, CharStatements.SEL_ENUM_DECLINED_NAME, CharStatements.SEL_ENUM_CUSTOMIZATIONS },
                new [] { CharStatements.SEL_UNDELETE_ENUM, CharStatements.SEL_UNDELETE_ENUM_DECLINED_NAME, CharStatements.SEL_UNDELETE_ENUM_CUSTOMIZATIONS }
            };

            bool result = true;
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(statements[isDeletedCharacters ? 1 : 0][withDeclinedNames ? 1 : 0]);
            stmt.AddValue(0, accountId);
            SetQuery(EnumCharacterQueryLoad.Characters, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(statements[isDeletedCharacters ? 1 : 0][2]);
            stmt.AddValue(0, accountId);
            SetQuery(EnumCharacterQueryLoad.Customizations, stmt);

            return result;
        }

        public bool IsDeletedCharacters() { return _isDeletedCharacters; }

        bool _isDeletedCharacters = false;
    }

    // used at player loading query list preparing, and later result selection
    public enum PlayerLoginQueryLoad
    {
        From,
        Customizations,
        Group,
        Auras,
        AuraEffects,
        AuraStoredLocations,
        Spells,
        SpellFavorites,
        QuestStatus,
        QuestStatusObjectives,
        QuestStatusObjectivesCriteria,
        QuestStatusObjectivesCriteriaProgress,
        QuestStatusSpawnTracking,
        DailyQuestStatus,
        Reputation,
        Inventory,
        Artifacts,
        Azerite,
        AzeriteMilestonePowers,
        AzeriteUnlockedEssences,
        AzeriteEmpowered,
        Mails,
        MailItems,
        MailItemsArtifact,
        MailItemsAzerite,
        MailItemsAzeriteMilestonePower,
        MailItemsAzeriteUnlockedEssence,
        MailItemsAzeriteEmpowered,
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
        Currency,
        CufProfiles,
        CorpseLocation,
        PetSlots,
        Garrison,
        GarrisonBlueprints,
        GarrisonBuildings,
        GarrisonFollowers,
        GarrisonFollowerAbilities,
        TraitEntries,
        TraitConfigs,
        DataElements,
        DataFlags,
        BankTabSettings,
        Max
    }

    enum EnumCharacterQueryLoad
    {
        Characters,
        Customizations
    }
}
