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
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game.Chat
{
    [CommandGroup("character", RBACPermissions.CommandCharacter, true)]
    class CharacterCommands
    {
        [Command("titles", RBACPermissions.CommandCharacterTitles, true)]
        static bool HandleCharacterTitlesCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target;
            if (!handler.extractPlayerTarget(args, out target))
                return false;

            LocaleConstant loc = handler.GetSessionDbcLocale();
            string targetName = target.GetName();
            string knownStr = handler.GetCypherString(CypherStrings.Known);

            // Search in CharTitles.dbc
            foreach (var titleInfo in CliDB.CharTitlesStorage.Values)
            {
                if (target.HasTitle(titleInfo))
                {
                    string name = (target.GetGender() == Gender.Male ? titleInfo.Name : titleInfo.Name1)[handler.GetSessionDbcLocale()];
                    if (string.IsNullOrEmpty(name))
                        continue;

                    string activeStr = target.GetUInt32Value(PlayerFields.ChosenTitle) == titleInfo.MaskID
                    ? handler.GetCypherString(CypherStrings.Active) : "";

                    string titleNameStr = string.Format(name.ConvertFormatSyntax(), targetName);

                    // send title in "id (idx:idx) - [namedlink locale]" format
                    if (handler.GetSession() != null)
                        handler.SendSysMessage(CypherStrings.TitleListChat, titleInfo.Id, titleInfo.MaskID, titleInfo.Id, titleNameStr, loc, knownStr, activeStr);
                    else
                        handler.SendSysMessage(CypherStrings.TitleListConsole, titleInfo.Id, titleInfo.MaskID, name, loc, knownStr, activeStr);
                }
            }

            return true;
        }

        //rename characters
        [Command("rename", RBACPermissions.CommandCharacterRename, true)]
        static bool HandleCharacterRenameCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            ObjectGuid targetGuid;
            string targetName;
            if (!handler.extractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            string newNameStr = args.NextString();

            if (!string.IsNullOrEmpty(newNameStr))
            {
                string playerOldName;
                string newName = newNameStr;

                if (target)
                {
                    // check online security
                    if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                        return false;

                    playerOldName = target.GetName();
                }
                else
                {
                    // check offline security
                    if (handler.HasLowerSecurity(null, targetGuid))
                        return false;

                    ObjectManager.GetPlayerNameByGUID(targetGuid, out playerOldName);
                }

                if (!ObjectManager.NormalizePlayerName(ref newName))
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                if (ObjectManager.CheckPlayerName(newName, target ? target.GetSession().GetSessionDbcLocale() : Global.WorldMgr.GetDefaultDbcLocale(), true) != ResponseCodes.CharNameSuccess)
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                WorldSession session = handler.GetSession();
                if (session != null)
                {
                    if (!session.HasPermission(RBACPermissions.SkipCheckCharacterCreationReservedname) && Global.ObjectMgr.IsReservedName(newName))
                    {
                        handler.SendSysMessage(CypherStrings.ReservedName);
                        return false;
                    }
                }

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHECK_NAME);
                stmt.AddValue(0, newName);
                SQLResult result = DB.Characters.Query(stmt);
                if (!result.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.RenamePlayerAlreadyExists, newName);
                    return false;
                }

                // Remove declined name from db
                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_DECLINED_NAME);
                stmt.AddValue(0, targetGuid.GetCounter());
                DB.Characters.Execute(stmt);

                if (target)
                {
                    target.SetName(newName);
                    session = target.GetSession();
                    if (session != null)
                        session.KickPlayer();
                }
                else
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_NAME_BY_GUID);
                    stmt.AddValue(0, newName);
                    stmt.AddValue(1, targetGuid.GetCounter());
                    DB.Characters.Execute(stmt);
                }

                Global.WorldMgr.UpdateCharacterInfo(targetGuid, newName);

                handler.SendSysMessage(CypherStrings.RenamePlayerWithNewName, playerOldName, newName);

                Player player = handler.GetPlayer();
                if (player)
                    Log.outCommand(session.GetAccountId(), "GM {0} (Account: {1}) forced rename {2} to player {3} (Account: {4})", player.GetName(), session.GetAccountId(), newName, playerOldName, ObjectManager.GetPlayerAccountIdByGUID(targetGuid));
                else
                    Log.outCommand(0, "CONSOLE forced rename '{0}' to '{1}' ({2})", playerOldName, newName, targetGuid.ToString());
            }
            else
            {
                if (target)
                {
                    // check online security
                    if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                        return false;

                    handler.SendSysMessage(CypherStrings.RenamePlayer, handler.GetNameLink(target));
                    target.SetAtLoginFlag(AtLoginFlags.Rename);
                }
                else
                {
                    // check offline security
                    if (handler.HasLowerSecurity(null, targetGuid))
                        return false;

                    string oldNameLink = handler.playerLink(targetName);
                    handler.SendSysMessage(CypherStrings.RenamePlayerGuid, oldNameLink, targetGuid.ToString());

                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                    stmt.AddValue(0, AtLoginFlags.Rename);
                    stmt.AddValue(1, targetGuid.GetCounter());
                    DB.Characters.Execute(stmt);
                }
            }

            return true;
        }

        [Command("level", RBACPermissions.CommandCharacterLevel, true)]
        static bool HandleCharacterLevelCommand(StringArguments args, CommandHandler handler)
        {
            string nameStr;
            string levelStr;
            handler.extractOptFirstArg(args, out nameStr, out levelStr);
            if (string.IsNullOrEmpty(levelStr))
                return false;

            // exception opt second arg: .character level $name
            if (!levelStr.IsNumber())
            {
                nameStr = levelStr;
                levelStr = null;                                    // current level will used
            }

            Player target;
            ObjectGuid targetGuid;
            string targetName;
            if (!handler.extractPlayerTarget(new StringArguments(nameStr), out target, out targetGuid, out targetName))
                return false;

            int oldlevel = (int)(target ? target.getLevel() : Player.GetLevelFromDB(targetGuid));

            if (!int.TryParse(levelStr, out int newlevel))
                newlevel = oldlevel;

            if (newlevel < 1)
                return false;                                       // invalid level

            if (newlevel > SharedConst.StrongMaxLevel)                         // hardcoded maximum level
                newlevel = SharedConst.StrongMaxLevel;

            HandleCharacterLevel(target, targetGuid, oldlevel, newlevel, handler);
            if (handler.GetSession() == null || handler.GetSession().GetPlayer() != target)      // including player == NULL
            {
                string nameLink = handler.playerLink(targetName);
                handler.SendSysMessage(CypherStrings.YouChangeLvl, nameLink, newlevel);
            }

            return true;
        }

        // customize characters
        [Command("customize", RBACPermissions.CommandCharacterCustomize, true)]
        static bool HandleCharacterCustomizeCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            ObjectGuid targetGuid;
            string targetName;
            if (!handler.extractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
            stmt.AddValue(0, AtLoginFlags.Customize);
            if (target)
            {
                handler.SendSysMessage(CypherStrings.CustomizePlayer, handler.GetNameLink(target));
                target.SetAtLoginFlag(AtLoginFlags.Customize);
                stmt.AddValue(1, target.GetGUID().GetCounter());
            }
            else
            {
                string oldNameLink = handler.playerLink(targetName);
                stmt.AddValue(1, targetGuid.GetCounter());
                handler.SendSysMessage(CypherStrings.CustomizePlayerGuid, oldNameLink, targetGuid.ToString());
            }
            DB.Characters.Execute(stmt);

            return true;
        }

        [Command("changeaccount", RBACPermissions.CommandCharacterChangeaccount, true)]
        static bool HandleCharacterChangeAccountCommand(StringArguments args, CommandHandler handler)
        {
            string playerNameStr;
            string accountName;
            handler.extractOptFirstArg(args, out playerNameStr, out accountName);
            if (accountName.IsEmpty())
                return false;

            ObjectGuid targetGuid;
            string targetName;
            Player playerNotUsed;
            if (!handler.extractPlayerTarget(new StringArguments(playerNameStr), out playerNotUsed, out targetGuid, out targetName))
                return false;

            CharacterInfo characterInfo = Global.WorldMgr.GetCharacterInfo(targetGuid);
            if (characterInfo == null)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            uint oldAccountId = characterInfo.AccountId;
            uint newAccountId = oldAccountId;

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_ID_BY_NAME);
            stmt.AddValue(0, accountName);
            SQLResult result = DB.Login.Query(stmt);
            if (!result.IsEmpty())
                newAccountId = result.Read<uint>(0);
            else
            {
                handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                return false;
            }

            // nothing to do :)
            if (newAccountId == oldAccountId)
                return true;

            uint charCount = Global.AccountMgr.GetCharactersCount(newAccountId);
            if (charCount != 0)
            {
                if (charCount >= WorldConfig.GetIntValue(WorldCfg.CharactersPerRealm))
                {
                    handler.SendSysMessage(CypherStrings.AccountCharacterListFull, accountName, newAccountId);
                    return false;
                }
            }

            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ACCOUNT_BY_GUID);
            stmt.AddValue(0, newAccountId);
            stmt.AddValue(1, targetGuid.GetCounter());
            DB.Characters.DirectExecute(stmt);

            Global.WorldMgr.UpdateRealmCharCount(oldAccountId);
            Global.WorldMgr.UpdateRealmCharCount(newAccountId);

            Global.WorldMgr.UpdateCharacterInfoAccount(targetGuid, newAccountId);

            handler.SendSysMessage(CypherStrings.ChangeAccountSuccess, targetName, accountName);

            string logString = $"changed ownership of player {targetName} ({targetGuid.ToString()}) from account {oldAccountId} to account {newAccountId}";
            WorldSession session = handler.GetSession();
            if (session != null)
            {
                Player player = session.GetPlayer();
                if (player != null)
                    Log.outCommand(session.GetAccountId(), $"GM {player.GetName()} (Account: {session.GetAccountId()}) {logString}");
            }
            else
                Log.outCommand(0, $"{handler.GetCypherString(CypherStrings.Console)} {logString}");
            return true;
        }

        [Command("changefaction", RBACPermissions.CommandCharacterChangefaction, true)]
        static bool HandleCharacterChangeFactionCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            ObjectGuid targetGuid;
            string targetName;
            if (!handler.extractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
            stmt.AddValue(0, AtLoginFlags.ChangeFaction);
            if (target)
            {
                handler.SendSysMessage(CypherStrings.CustomizePlayer, handler.GetNameLink(target));
                target.SetAtLoginFlag(AtLoginFlags.ChangeFaction);
                stmt.AddValue(1, target.GetGUID().GetCounter());
            }
            else
            {
                string oldNameLink = handler.playerLink(targetName);
                handler.SendSysMessage(CypherStrings.CustomizePlayerGuid, oldNameLink, targetGuid.ToString());
                stmt.AddValue(1, targetGuid.GetCounter());
            }
            DB.Characters.Execute(stmt);

            return true;
        }

        [Command("changerace", RBACPermissions.CommandCharacterChangerace, true)]
        static bool HandleCharacterChangeRaceCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            ObjectGuid targetGuid;
            string targetName;
            if (!handler.extractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
            stmt.AddValue(0, AtLoginFlags.ChangeRace);
            if (target)
            {
                // @todo add text into database
                handler.SendSysMessage(CypherStrings.CustomizePlayer, handler.GetNameLink(target));
                target.SetAtLoginFlag(AtLoginFlags.ChangeRace);
                stmt.AddValue(1, target.GetGUID().GetCounter());
            }
            else
            {
                string oldNameLink = handler.playerLink(targetName);
                // @todo add text into database
                handler.SendSysMessage(CypherStrings.CustomizePlayerGuid, oldNameLink, targetGuid.ToString());
                stmt.AddValue(1, targetGuid.GetCounter());
            }
            DB.Characters.Execute(stmt);

            return true;
        }

        [Command("reputation", RBACPermissions.CommandCharacterReputation, true)]
        static bool HandleCharacterReputationCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            if (!handler.extractPlayerTarget(args, out target))
                return false;

            LocaleConstant loc = handler.GetSessionDbcLocale();

            var targetFSL = target.GetReputationMgr().GetStateList();
            foreach (var pair in targetFSL)
            {
                FactionState faction = pair.Value;
                FactionRecord factionEntry = CliDB.FactionStorage.LookupByKey(faction.ID);
                string factionName = factionEntry != null ? factionEntry.Name[loc] : "#Not found#";
                ReputationRank rank = target.GetReputationMgr().GetRank(factionEntry);
                string rankName = handler.GetCypherString(ReputationMgr.ReputationRankStrIndex[(int)rank]);
                StringBuilder ss = new StringBuilder();
                if (handler.GetSession() != null)
                    ss.AppendFormat("{0} - |cffffffff|Hfaction:{0}|h[{1} {2}]|h|r", faction.ID, factionName, loc);
                else
                    ss.AppendFormat("{0} - {1} {2}", faction.ID, factionName, loc);

                ss.AppendFormat(" {0} ({1})", rankName, target.GetReputationMgr().GetReputation(factionEntry));

                if (faction.Flags.HasAnyFlag(FactionFlags.Visible))
                    ss.Append(handler.GetCypherString(CypherStrings.FactionVisible));
                if (faction.Flags.HasAnyFlag(FactionFlags.AtWar))
                    ss.Append(handler.GetCypherString(CypherStrings.FactionAtwar));
                if (faction.Flags.HasAnyFlag(FactionFlags.PeaceForced))
                    ss.Append(handler.GetCypherString(CypherStrings.FactionPeaceForced));
                if (faction.Flags.HasAnyFlag(FactionFlags.Hidden))
                    ss.Append(handler.GetCypherString(CypherStrings.FactionHidden));
                if (faction.Flags.HasAnyFlag(FactionFlags.InvisibleForced))
                    ss.Append(handler.GetCypherString(CypherStrings.FactionInvisibleForced));
                if (faction.Flags.HasAnyFlag(FactionFlags.Inactive))
                    ss.Append(handler.GetCypherString(CypherStrings.FactionInactive));

                handler.SendSysMessage(ss.ToString());
            }

            return true;
        }

        [Command("erase", RBACPermissions.CommandCharacterErase, true)]
        static bool HandleCharacterEraseCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string characterName = args.NextString();
            if (string.IsNullOrEmpty(characterName))
                return false;

            if (!ObjectManager.NormalizePlayerName(ref characterName))
                return false;

            ObjectGuid characterGuid;
            uint accountId;

            Player player = Global.ObjAccessor.FindPlayerByName(characterName);
            if (player)
            {
                characterGuid = player.GetGUID();
                accountId = player.GetSession().GetAccountId();
                player.GetSession().KickPlayer();
            }
            else
            {
                characterGuid = ObjectManager.GetPlayerGUIDByName(characterName);
                if (characterGuid.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.NoPlayer, characterName);
                    return false;
                }
                accountId = ObjectManager.GetPlayerAccountIdByGUID(characterGuid);
            }

            string accountName;
            Global.AccountMgr.GetName(accountId, out accountName);

            Player.DeleteFromDB(characterGuid, accountId, true, true);
            handler.SendSysMessage(CypherStrings.CharacterDeleted, characterName, characterGuid.ToString(), accountName, accountId);

            return true;
        }

        [CommandGroup("deleted", RBACPermissions.CommandCharacterDeleted, true)]
        class DeletedCommands
        {
            [Command("delete", RBACPermissions.CommandCharacterDeletedDelete, true)]
            static bool HandleCharacterDeletedDeleteCommand(StringArguments args, CommandHandler handler)
            {
                // It is required to submit at least one argument
                if (args.Empty())
                    return false;

                List<DeletedInfo> foundList = new List<DeletedInfo>();
                if (!GetDeletedCharacterInfoList(foundList, args.NextString()))
                    return false;

                if (foundList.Empty())
                {
                    handler.SendSysMessage(CypherStrings.CharacterDeletedListEmpty);
                    return false;
                }

                handler.SendSysMessage(CypherStrings.CharacterDeletedDelete);
                HandleCharacterDeletedListHelper(foundList, handler);

                // Call the appropriate function to delete them (current account for deleted characters is 0)
                foreach (var info in foundList)
                    Player.DeleteFromDB(info.guid, 0, false, true);

                return true;
            }

            [Command("list", RBACPermissions.CommandCharacterDeletedList, true)]
            static bool HandleCharacterDeletedListCommand(StringArguments args, CommandHandler handler)
            {
                List<DeletedInfo> foundList = new List<DeletedInfo>();
                if (!GetDeletedCharacterInfoList(foundList, args.NextString()))
                    return false;

                // if no characters have been found, output a warning
                if (foundList.Empty())
                {
                    handler.SendSysMessage(CypherStrings.CharacterDeletedListEmpty);
                    return false;
                }

                HandleCharacterDeletedListHelper(foundList, handler);

                return true;
            }

            [Command("restore", RBACPermissions.CommandCharacterDeletedRestore, true)]
            static bool HandleCharacterDeletedRestoreCommand(StringArguments args, CommandHandler handler)
            {
                // It is required to submit at least one argument
                if (args.Empty())
                    return false;

                string searchString = args.NextString();
                string newCharName = args.NextString();
                uint newAccount = args.NextUInt32();

                List<DeletedInfo> foundList = new List<DeletedInfo>();
                if (!GetDeletedCharacterInfoList(foundList, searchString))
                    return false;

                if (foundList.Empty())
                {
                    handler.SendSysMessage(CypherStrings.CharacterDeletedListEmpty);
                    return false;
                }

                handler.SendSysMessage(CypherStrings.CharacterDeletedRestore);
                HandleCharacterDeletedListHelper(foundList, handler);

                if (newCharName.IsEmpty())
                {
                    // Drop not existed account cases
                    foreach (var info in foundList)
                        HandleCharacterDeletedRestoreHelper(info, handler);
                }
                else if (foundList.Count == 1 && ObjectManager.NormalizePlayerName(ref newCharName))
                {
                    DeletedInfo delInfo = foundList[0];

                    // update name
                    delInfo.name = newCharName;

                    // if new account provided update deleted info
                    if (newAccount != 0 && newAccount != delInfo.accountId)
                    {
                        delInfo.accountId = newAccount;
                        Global.AccountMgr.GetName(newAccount, out delInfo.accountName);
                    }

                    HandleCharacterDeletedRestoreHelper(delInfo, handler);
                }
                else
                    handler.SendSysMessage(CypherStrings.CharacterDeletedErrRename);

                return true;
            }

            [Command("old", RBACPermissions.CommandCharacterDeletedOld, true)]
            static bool HandleCharacterDeletedOldCommand(StringArguments args, CommandHandler handler)
            {
                int keepDays = WorldConfig.GetIntValue(WorldCfg.ChardeleteKeepDays);

                string daysStr = args.NextString();
                if (!daysStr.IsEmpty())
                {
                    if (!daysStr.IsNumber())
                        return false;

                    if (!int.TryParse(daysStr, out keepDays) || keepDays < 0)
                        return false;
                }
                // config option value 0 -> disabled and can't be used
                else if (keepDays <= 0)
                    return false;

                Player.DeleteOldCharacters(keepDays);

                return true;
            }

            static bool GetDeletedCharacterInfoList(List<DeletedInfo> foundList, string searchString)
            {
                SQLResult result;
                PreparedStatement stmt;
                if (!searchString.IsEmpty())
                {
                    // search by GUID
                    if (searchString.IsNumber())
                    {
                        if (!ulong.TryParse(searchString, out ulong guid))
                            return false;

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_GUID);
                        stmt.AddValue(0, guid);
                        result = DB.Characters.Query(stmt);
                    }
                    // search by name
                    else
                    {
                        if (!ObjectManager.NormalizePlayerName(ref searchString))
                            return false;

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO_BY_NAME);
                        stmt.AddValue(0, searchString);
                        result = DB.Characters.Query(stmt);
                    }
                }
                else
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_DEL_INFO);
                    result = DB.Characters.Query(stmt);
                }

                if (!result.IsEmpty())
                {
                    do
                    {
                        DeletedInfo info;

                        info.guid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));
                        info.name = result.Read<string>(1);
                        info.accountId = result.Read<uint>(2);

                        // account name will be empty for not existed account
                        Global.AccountMgr.GetName(info.accountId, out info.accountName);
                        info.deleteDate = result.Read<uint>(3);
                        foundList.Add(info);
                    }
                    while (result.NextRow());
                }

                return true;
            }
            static void HandleCharacterDeletedListHelper(List<DeletedInfo> foundList, CommandHandler handler)
            {
                if (handler.GetSession() == null)
                {
                    handler.SendSysMessage(CypherStrings.CharacterDeletedListBar);
                    handler.SendSysMessage(CypherStrings.CharacterDeletedListHeader);
                    handler.SendSysMessage(CypherStrings.CharacterDeletedListBar);
                }

                foreach (var info in foundList)
                {
                    string dateStr = Time.UnixTimeToDateTime(info.deleteDate).ToShortDateString();

                    if (!handler.GetSession())
                        handler.SendSysMessage(CypherStrings.CharacterDeletedListLineConsole,
                            info.guid.ToString(), info.name, info.accountName.IsEmpty() ? "<Not existed>" : info.accountName,
                            info.accountId, dateStr);
                    else
                        handler.SendSysMessage(CypherStrings.CharacterDeletedListLineChat,
                            info.guid.ToString(), info.name, info.accountName.IsEmpty() ? "<Not existed>" : info.accountName,
                            info.accountId, dateStr);
                }

                if (!handler.GetSession())
                    handler.SendSysMessage(CypherStrings.CharacterDeletedListBar);
            }
            static void HandleCharacterDeletedRestoreHelper(DeletedInfo delInfo, CommandHandler handler)
            {
                if (delInfo.accountName.IsEmpty())                    // account not exist
                {
                    handler.SendSysMessage(CypherStrings.CharacterDeletedSkipAccount, delInfo.name, delInfo.guid.ToString(), delInfo.accountId);
                    return;
                }

                // check character count
                uint charcount = Global.AccountMgr.GetCharactersCount(delInfo.accountId);
                if (charcount >= WorldConfig.GetIntValue(WorldCfg.CharactersPerRealm))
                {
                    handler.SendSysMessage(CypherStrings.CharacterDeletedSkipFull, delInfo.name, delInfo.guid.ToString(), delInfo.accountId);
                    return;
                }

                if (!ObjectManager.GetPlayerGUIDByName(delInfo.name).IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.CharacterDeletedSkipName, delInfo.name, delInfo.guid.ToString(), delInfo.accountId);
                    return;
                }

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_RESTORE_DELETE_INFO);
                stmt.AddValue(0, delInfo.name);
                stmt.AddValue(1, delInfo.accountId);
                stmt.AddValue(2, delInfo.guid.GetCounter());
                DB.Characters.Execute(stmt);

                Global.WorldMgr.UpdateCharacterInfoDeleted(delInfo.guid, false, delInfo.name);
            }

            struct DeletedInfo
            {
                public ObjectGuid guid; // the GUID from the character
                public string name; // the character name
                public uint accountId; // the account id
                public string accountName; // the account name
                public long deleteDate; // the date at which the character has been deleted
            }
        }

        [CommandNonGroup("levelup", RBACPermissions.CommandLevelup)]
        static bool LevelUp(StringArguments args, CommandHandler handler)
        {
            string nameStr;
            string levelStr;
            handler.extractOptFirstArg(args, out nameStr, out levelStr);

            // exception opt second arg: .character level $name
            if (!string.IsNullOrEmpty(levelStr) && !levelStr.IsNumber())
            {
                nameStr = levelStr;
                levelStr = null;                                    // current level will used
            }

            Player target;
            ObjectGuid targetGuid;
            string targetName;
            if (!handler.extractPlayerTarget(new StringArguments(nameStr), out target, out targetGuid, out targetName))
                return false;

            int oldlevel = (int)(target ? target.getLevel() : Player.GetLevelFromDB(targetGuid));
            if (!int.TryParse(levelStr, out int addlevel))
                addlevel = 1;

            int newlevel = oldlevel + addlevel;
            if (newlevel < 1)
                newlevel = 1;

            if (newlevel > SharedConst.StrongMaxLevel)                         // hardcoded maximum level
                newlevel = SharedConst.StrongMaxLevel;

            HandleCharacterLevel(target, targetGuid, oldlevel, newlevel, handler);

            if (handler.GetSession() == null || handler.GetSession().GetPlayer() != target)      // including chr == NULL
            {
                string nameLink = handler.playerLink(targetName);
                handler.SendSysMessage(CypherStrings.YouChangeLvl, nameLink, newlevel);
            }

            return true;
        }

        public static void HandleCharacterLevel(Player player, ObjectGuid playerGuid, int oldLevel, int newLevel, CommandHandler handler)
        {
            if (player)
            {
                player.GiveLevel((uint)newLevel);
                player.InitTalentForLevel();
                player.SetUInt32Value(ActivePlayerFields.Xp, 0);

                if (handler.needReportToTarget(player))
                {
                    if (oldLevel == newLevel)
                        player.SendSysMessage(CypherStrings.YoursLevelProgressReset, handler.GetNameLink());
                    else if (oldLevel < newLevel)
                        player.SendSysMessage(CypherStrings.YoursLevelUp, handler.GetNameLink(), newLevel);
                    else                                                // if (oldlevel > newlevel)
                        player.SendSysMessage(CypherStrings.YoursLevelDown, handler.GetNameLink(), newLevel);
                }
            }
            else
            {
                // Update level and reset XP, everything else will be updated at login
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_LEVEL);
                stmt.AddValue(0, newLevel);
                stmt.AddValue(1, playerGuid.GetCounter());
                DB.Characters.Execute(stmt);
            }
        }
    }

    [CommandGroup("pdump", RBACPermissions.CommandPdump, true)]
    class PdumpCommand
    {
        [Command("load", RBACPermissions.CommandPdumpLoad, true)]
        static bool HandlePDumpLoadCommand(StringArguments args, CommandHandler handler)
        {
            /*
            if (args.Empty())
                return false;

            string fileStr = strtok((char*)args, " ");
            if (!fileStr)
                return false;

            char* accountStr = strtok(NULL, " ");
            if (!accountStr)
                return false;

            string accountName = accountStr;
            if (!AccountMgr.normalizeString(accountName))
            {
                handler.SendSysMessage(LANG_ACCOUNT_NOT_EXIST, accountName);
                handler.SetSentErrorMessage(true);
                return false;
            }

            public uint accountId = AccountMgr.GetId(accountName);
            if (!accountId)
            {
                accountId = atoi(accountStr);                             // use original string
                if (!accountId)
                {
                    handler.SendSysMessage(LANG_ACCOUNT_NOT_EXIST, accountName);

                    return false;
                }
            }

            if (!AccountMgr.GetName(accountId, accountName))
            {
                handler.SendSysMessage(LANG_ACCOUNT_NOT_EXIST, accountName);
                handler.SetSentErrorMessage(true);
                return false;
            }

            char* guidStr = NULL;
            char* nameStr = strtok(NULL, " ");

            string name;
            if (nameStr)
            {
                name = nameStr;
                // normalize the name if specified and check if it exists
                if (!ObjectManager.NormalizePlayerName(name))
                {
                    handler.SendSysMessage(LANG_INVALID_CHARACTER_NAME);

                    return false;
                }

                if (ObjectMgr.CheckPlayerName(name, true) != CHAR_NAME_SUCCESS)
                {
                    handler.SendSysMessage(LANG_INVALID_CHARACTER_NAME);

                    return false;
                }

                guidStr = strtok(NULL, " ");
            }

            public uint guid = 0;

            if (guidStr)
            {
                guid = uint32(atoi(guidStr));
                if (!guid)
                {
                    handler.SendSysMessage(LANG_INVALID_CHARACTER_GUID);

                    return false;
                }

                if (Global.ObjectMgr.GetPlayerAccountIdByGUID(guid))
                {
                    handler.SendSysMessage(LANG_CHARACTER_GUID_IN_USE, guid);

                    return false;
                }
            }

            switch (PlayerDumpReader().LoadDump(fileStr, accountId, name, guid))
            {
                case DUMP_SUCCESS:
                    handler.SendSysMessage(LANG_COMMAND_IMPORT_SUCCESS);
                    break;
                case DUMP_FILE_OPEN_ERROR:
                    handler.SendSysMessage(LANG_FILE_OPEN_FAIL, fileStr);

                    return false;
                case DUMP_FILE_BROKEN:
                    handler.SendSysMessage(LANG_DUMP_BROKEN, fileStr);

                    return false;
                case DUMP_TOO_MANY_CHARS:
                    handler.SendSysMessage(LANG_ACCOUNT_CHARACTER_LIST_FULL, accountName, accountId);

                    return false;
                default:
                    handler.SendSysMessage(LANG_COMMAND_IMPORT_FAILED);

                    return false;
            }
            */
            return true;
        }

        [Command("write", RBACPermissions.CommandPdumpWrite, true)]
        static bool HandlePDumpWriteCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;
            /*
            char* fileStr = strtok((char*)args, " ");
            char* playerStr = strtok(NULL, " ");

            if (!fileStr || !playerStr)
                return false;

            uint64 guid;
            // character name can't start from number
            if (isNumeric(playerStr))
                guid = MAKE_NEW_GUID(atoi(playerStr), 0, HIGHGUID_PLAYER);
            else
            {
                string name = handler.extractPlayerNameFromLink(playerStr);
                if (name.empty())
                {
                    handler.SendSysMessage(LANG_PLAYER_NOT_FOUND);

                    return false;
                }

                guid = Global.ObjectMgr.GetPlayerGUIDByName(name);
            }

            if (!Global.ObjectMgr.GetPlayerAccountIdByGUID(guid))
            {
                handler.SendSysMessage(LANG_PLAYER_NOT_FOUND);
                handler.SetSentErrorMessage(true);
                return false;
            }

            switch (PlayerDumpWriter().WriteDump(fileStr, uint32(guid)))
            {
                case DUMP_SUCCESS:
                    handler.SendSysMessage(LANG_COMMAND_EXPORT_SUCCESS);
                    break;
                case DUMP_FILE_OPEN_ERROR:
                    handler.SendSysMessage(LANG_FILE_OPEN_FAIL, fileStr);

                    return false;
                case DUMP_CHARACTER_DELETED:
                    handler.SendSysMessage(LANG_COMMAND_EXPORT_DELETED_CHAR);

                    return false;
                default:
                    handler.SendSysMessage(LANG_COMMAND_EXPORT_FAILED);

                    return false;
            }
            */
            return true;
        }
    }
}
