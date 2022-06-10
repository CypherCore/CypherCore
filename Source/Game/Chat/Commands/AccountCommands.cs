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
using Framework.Database;
using Game.Entities;
using System;

namespace Game.Chat
{
    [CommandGroup("account")]
    class AccountCommands
    {
        [Command("", RBACPermissions.CommandAccount)]
        static bool HandleAccountCommand(CommandHandler handler)
        {
            if (handler.GetSession() == null)
                return false;

            // GM Level
            AccountTypes securityLevel = handler.GetSession().GetSecurity();
            handler.SendSysMessage(CypherStrings.AccountLevel, securityLevel);

            // Security level required
            WorldSession session = handler.GetSession();
            bool hasRBAC = (session.HasPermission(RBACPermissions.EmailConfirmForPassChange));
            uint pwConfig = 0; // 0 - PW_NONE, 1 - PW_EMAIL, 2 - PW_RBAC

            handler.SendSysMessage(CypherStrings.AccountSecType, (pwConfig == 0 ? "Lowest level: No Email input required." :
                pwConfig == 1 ? "Highest level: Email input required." : pwConfig == 2 ? "Special level: Your account may require email input depending on settings. That is the case if another lien is printed." :
                "Unknown security level: Notify technician for details."));

            // RBAC required display - is not displayed for console
            if (pwConfig == 2 && hasRBAC)
                handler.SendSysMessage(CypherStrings.RbacEmailRequired);

            // Email display if sufficient rights
            if (session.HasPermission(RBACPermissions.MayCheckOwnEmail))
            {
                string emailoutput;
                uint accountId = session.GetAccountId();

                PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.GET_EMAIL_BY_ID);
                stmt.AddValue(0, accountId);
                SQLResult result = DB.Login.Query(stmt);

                if (!result.IsEmpty())
                {
                    emailoutput = result.Read<string>(0);
                    handler.SendSysMessage(CypherStrings.CommandEmailOutput, emailoutput);
                }
            }

            return true;
        }

        [Command("addon", RBACPermissions.CommandAccountAddon)]
        static bool HandleAccountAddonCommand(CommandHandler handler, byte expansion)
        {
            if (expansion > WorldConfig.GetIntValue(WorldCfg.Expansion))
            {
                handler.SendSysMessage(CypherStrings.ImproperValue);
                return false;
            }

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_EXPANSION);
            stmt.AddValue(0, expansion);
            stmt.AddValue(1, handler.GetSession().GetAccountId());
            DB.Login.Execute(stmt);

            handler.SendSysMessage(CypherStrings.AccountAddon, expansion);
            return true;
        }

        [Command("create", RBACPermissions.CommandAccountCreate, true)]
        static bool HandleAccountCreateCommand(CommandHandler handler, string accountName, string password, string email)
        {
            if (accountName.Contains("@"))
            {
                handler.SendSysMessage(CypherStrings.AccountUseBnetCommands);
                return false;
            }

            AccountOpResult result = Global.AccountMgr.CreateAccount(accountName, password, email);
            switch (result)
            {
                case AccountOpResult.Ok:
                    handler.SendSysMessage(CypherStrings.AccountCreated, accountName);
                    if (handler.GetSession() != null)
                    {
                        Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) created Account {4} (Email: '{5}')",
                            handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                            handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(),
                            accountName, email);
                    }
                    break;
                case AccountOpResult.NameTooLong:
                    handler.SendSysMessage(CypherStrings.AccountNameTooLong);
                    return false;
                case AccountOpResult.PassTooLong:
                    handler.SendSysMessage(CypherStrings.AccountPassTooLong);
                    return false;
                case AccountOpResult.NameAlreadyExist:
                    handler.SendSysMessage(CypherStrings.AccountAlreadyExist);
                    return false;
                case AccountOpResult.DBInternalError:
                    handler.SendSysMessage(CypherStrings.AccountNotCreatedSqlError, accountName);
                    return false;
                default:
                    handler.SendSysMessage(CypherStrings.AccountNotCreated, accountName);
                    return false;
            }

            return true;
        }

        [Command("delete", RBACPermissions.CommandAccountDelete, true)]
        static bool HandleAccountDeleteCommand(CommandHandler handler, string accountName)
        {
            uint accountId = Global.AccountMgr.GetId(accountName);
            if (accountId == 0)
            {
                handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                return false;
            }

            if (handler.HasLowerSecurityAccount(null, accountId, true))
                return false;

            AccountOpResult result = Global.AccountMgr.DeleteAccount(accountId);
            switch (result)
            {
                case AccountOpResult.Ok:
                    handler.SendSysMessage(CypherStrings.AccountDeleted, accountName);
                    break;
                case AccountOpResult.NameNotExist:
                    handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                    return false;
                case AccountOpResult.DBInternalError:
                    handler.SendSysMessage(CypherStrings.AccountNotDeletedSqlError, accountName);
                    return false;
                default:
                    handler.SendSysMessage(CypherStrings.AccountNotDeleted, accountName);
                    return false;
            }

            return true;
        }

        [Command("email", RBACPermissions.CommandAccountEmail)]
        static bool HandleAccountEmailCommand(CommandHandler handler, string oldEmail, string password, string email, string emailConfirm)
        {
            if (!Global.AccountMgr.CheckEmail(handler.GetSession().GetAccountId(), oldEmail))
            {
                handler.SendSysMessage(CypherStrings.CommandWrongemail);
                Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change email, but the provided email [{4}] is not equal to registration email [{5}].",
                    handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                    handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(),
                    email, oldEmail);
                return false;
            }

            if (!Global.AccountMgr.CheckPassword(handler.GetSession().GetAccountId(), password))
            {
                handler.SendSysMessage(CypherStrings.CommandWrongoldpassword);
                Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change email, but the provided password is wrong.",
                    handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                    handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString());
                return false;
            }

            if (email == oldEmail)
            {
                handler.SendSysMessage(CypherStrings.OldEmailIsNewEmail);
                return false;
            }

            if (email != emailConfirm)
            {
                handler.SendSysMessage(CypherStrings.NewEmailsNotMatch);
                Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change email, but the provided password is wrong.",
                    handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                    handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString());
                return false;
            }


            AccountOpResult result = Global.AccountMgr.ChangeEmail(handler.GetSession().GetAccountId(), email);
            switch (result)
            {
                case AccountOpResult.Ok:
                    handler.SendSysMessage(CypherStrings.CommandEmail);
                    Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Changed Email from [{4}] to [{5}].",
                        handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                        handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(),
                        oldEmail, email);
                    break;
                case AccountOpResult.EmailTooLong:
                    handler.SendSysMessage(CypherStrings.EmailTooLong);
                    return false;
                default:
                    handler.SendSysMessage(CypherStrings.CommandNotchangeemail);
                    return false;
            }

            return true;
        }

        [Command("password", RBACPermissions.CommandAccountPassword)]
        static bool HandleAccountPasswordCommand(CommandHandler handler, string oldPassword, string newPassword, string confirmPassword, string confirmEmail)
        {
            // First, we check config. What security type (sec type) is it ? Depending on it, the command branches out
            uint pwConfig = WorldConfig.GetUIntValue(WorldCfg.AccPasschangesec); // 0 - PW_NONE, 1 - PW_EMAIL, 2 - PW_RBAC

            // We compare the old, saved password to the entered old password - no chance for the unauthorized.
            if (!Global.AccountMgr.CheckPassword(handler.GetSession().GetAccountId(), oldPassword))
            {
                handler.SendSysMessage(CypherStrings.CommandWrongoldpassword);

                Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change password, but the provided old password is wrong.",
                    handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                    handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString());
                return false;
            }
            // This compares the old, current email to the entered email - however, only...
            if ((pwConfig == 1 || (pwConfig == 2 && handler.GetSession().HasPermission(RBACPermissions.EmailConfirmForPassChange))) // ...if either PW_EMAIL or PW_RBAC with the Permission is active...
                && !Global.AccountMgr.CheckEmail(handler.GetSession().GetAccountId(), confirmEmail)) // ... and returns false if the comparison fails.
            {
                handler.SendSysMessage(CypherStrings.CommandWrongemail);

                Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change password, but the entered email [{4}] is wrong.",
                    handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                    handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(),
                    confirmEmail);
                return false;
            }

            // Making sure that newly entered password is correctly entered.
            if (newPassword != confirmPassword)
            {
                handler.SendSysMessage(CypherStrings.NewPasswordsNotMatch);
                return false;
            }

            // Changes password and prints result.
            AccountOpResult result = Global.AccountMgr.ChangePassword(handler.GetSession().GetAccountId(), newPassword);
            switch (result)
            {
                case AccountOpResult.Ok:
                    handler.SendSysMessage(CypherStrings.CommandPassword);
                    Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Changed Password.",
                        handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                        handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString());
                    break;
                case AccountOpResult.PassTooLong:
                    handler.SendSysMessage(CypherStrings.PasswordTooLong);
                    return false;
                default:
                    handler.SendSysMessage(CypherStrings.CommandNotchangepassword);
                    return false;
            }

            return true;
        }



        [CommandGroup("lock")]
        class AccountLockCommands
        {
            [Command("country", RBACPermissions.CommandAccountLockCountry)]
            static bool HandleAccountLockCountryCommand(CommandHandler handler, bool state)
            {
                if (state)
                {
                    /*var ipBytes = System.Net.IPAddress.Parse(handler.GetSession().GetRemoteAddress()).GetAddressBytes();
                    Array.Reverse(ipBytes);

                    PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_LOGON_COUNTRY);
                    stmt.AddValue(0, BitConverter.ToUInt32(ipBytes, 0));

                    SQLResult result = DB.Login.Query(stmt);
                    if (!result.IsEmpty())
                    {
                        string country = result.Read<string>(0);
                        stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_LOCK_COUNTRY);
                        stmt.AddValue(0, country);
                        stmt.AddValue(1, handler.GetSession().GetAccountId());
                        DB.Login.Execute(stmt);
                        handler.SendSysMessage(CypherStrings.CommandAcclocklocked);
                    }
                    else
                    {
                        handler.SendSysMessage("[IP2NATION] Table empty");
                        Log.outDebug(LogFilter.Server, "[IP2NATION] Table empty");
                    }*/
                }
                else
                {
                    PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_LOCK_COUNTRY);
                    stmt.AddValue(0, "00");
                    stmt.AddValue(1, handler.GetSession().GetAccountId());
                    DB.Login.Execute(stmt);
                    handler.SendSysMessage(CypherStrings.CommandAcclockunlocked);
                }
                return true;
            }

            [Command("ip", RBACPermissions.CommandAccountLockIp)]
            static bool HandleAccountLockIpCommand(CommandHandler handler, bool state)
            {
                PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_LOCK);

                if (state)
                {
                    stmt.AddValue(0, true);                                     // locked
                    handler.SendSysMessage(CypherStrings.CommandAcclocklocked);
                }
                else
                {
                    stmt.AddValue(0, false);                                    // unlocked
                    handler.SendSysMessage(CypherStrings.CommandAcclockunlocked);
                }

                stmt.AddValue(1, handler.GetSession().GetAccountId());

                DB.Login.Execute(stmt);
                return true;
            }
        }

        [CommandGroup("onlinelist")]
        class AccountOnlineListCommands
        {
            [Command("", RBACPermissions.CommandAccountOnlineList, true)]
            static bool HandleAccountOnlineListCommand(CommandHandler handler)
            {
                return HandleAccountOnlineListCommandWithParameters(handler, null, null, null, null);
            }

            [Command("ip", RBACPermissions.CommandAccountOnlineList, true)]
            static bool HandleAccountOnlineListWithIpFilterCommand(CommandHandler handler, string ipAddress)
            {
                return HandleAccountOnlineListCommandWithParameters(handler, ipAddress, null, null, null);
            }

            [Command("limit", RBACPermissions.CommandAccountOnlineList, true)]
            static bool HandleAccountOnlineListWithLimitCommand(CommandHandler handler, uint limit)
            {
                return HandleAccountOnlineListCommandWithParameters(handler, null, limit, null, null);
            }

            [Command("map", RBACPermissions.CommandAccountOnlineList, true)]
            static bool HandleAccountOnlineListWithMapFilterCommand(CommandHandler handler, uint mapId)
            {
                return HandleAccountOnlineListCommandWithParameters(handler, null, null, mapId, null);
            }

            [Command("zone", RBACPermissions.CommandAccountOnlineList, true)]
            static bool HandleAccountOnlineListWithZoneFilterCommand(CommandHandler handler, uint zoneId)
            {
                return HandleAccountOnlineListCommandWithParameters(handler, null, null, null, zoneId);
            }

            static bool HandleAccountOnlineListCommandWithParameters(CommandHandler handler, string ipAddress, uint? limit, uint? mapId, uint? zoneId)
            {
                int sessionsMatchCount = 0;

                foreach (var session in Global.WorldMgr.GetAllSessions())
                {
                    Player player = session.GetPlayer();

                    // Ignore sessions on character selection screen
                    if (player == null)
                        continue;

                    uint playerMapId = player.GetMapId();
                    uint playerZoneId = player.GetZoneId();

                    // Apply optional ipAddress filter
                    if (!ipAddress.IsEmpty() && ipAddress != session.GetRemoteAddress())
                        continue;

                    // Apply optional mapId filter
                    if (mapId.HasValue && mapId != playerMapId)
                        continue;

                    // Apply optional zoneId filter
                    if (zoneId.HasValue && zoneId != playerZoneId)
                        continue;

                    if (sessionsMatchCount == 0)
                    {
                        ///- Display the list of account/characters online on the first matched sessions
                        handler.SendSysMessage(CypherStrings.AccountListBarHeader);
                        handler.SendSysMessage(CypherStrings.AccountListHeader);
                        handler.SendSysMessage(CypherStrings.AccountListBar);
                    }

                    handler.SendSysMessage(CypherStrings.AccountListLine,
                        session.GetAccountName(),
                        session.GetPlayerName(),
                        session.GetRemoteAddress(),
                        playerMapId,
                        playerZoneId,
                        session.GetAccountExpansion(),
                        session.GetSecurity());

                    ++sessionsMatchCount;

                    // Apply optional count limit
                    if (limit.HasValue && sessionsMatchCount >= limit)
                        break;
                }

                // Header is printed on first matched session. If it wasn't printed then no sessions matched the criteria
                if (sessionsMatchCount == 0)
                {
                    handler.SendSysMessage(CypherStrings.AccountListEmpty);
                    return true;
                }

                handler.SendSysMessage(CypherStrings.AccountListBar);
                return true;
            }
        }

        [CommandGroup("set")]
        class AccountSetCommands
        {
            [Command("addon", RBACPermissions.CommandAccountSetAddon, true)]
            static bool HandleAccountSetAddonCommand(CommandHandler handler, string accountName, byte expansion)
            {
                uint accountId;
                if (!accountName.IsEmpty())
                {
                    // Convert Account name to Upper Format
                    accountName = accountName.ToUpper();

                    accountId = Global.AccountMgr.GetId(accountName);
                    if (accountId == 0)
                    {
                        handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                        return false;
                    }
                }
                else
                {
                    Player player = handler.GetSelectedPlayer();
                    if (!player)
                        return false;

                    accountId = player.GetSession().GetAccountId();
                    Global.AccountMgr.GetName(accountId, out accountName);
                }

                // Let set addon state only for lesser (strong) security level
                // or to self account
                if (handler.GetSession() != null && handler.GetSession().GetAccountId() != accountId &&
                    handler.HasLowerSecurityAccount(null, accountId, true))
                    return false;

                if (expansion > WorldConfig.GetIntValue(WorldCfg.Expansion))
                    return false;

                PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_EXPANSION);

                stmt.AddValue(0, expansion);
                stmt.AddValue(1, accountId);

                DB.Login.Execute(stmt);

                handler.SendSysMessage(CypherStrings.AccountSetaddon, accountName, accountId, expansion);
                return true;
            }

            [Command("gmlevel", RBACPermissions.CommandAccountSetSecLevel, true)]
            static bool HandleAccountSetGmLevelCommand(CommandHandler handler, string accountName, byte securityLevel, int realmId = -1)
            {
                return HandleAccountSetSecLevelCommand(handler, accountName, securityLevel, realmId);
            }

            [Command("password", RBACPermissions.CommandAccountSetPassword, true)]
            static bool HandleAccountSetPasswordCommand(CommandHandler handler, string accountName, string password, string confirmPassword)
            {
                uint targetAccountId = Global.AccountMgr.GetId(accountName);
                if (targetAccountId == 0)
                {
                    handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                    return false;
                }

                // can set password only for target with less security
                // This also restricts setting handler's own password
                if (handler.HasLowerSecurityAccount(null, targetAccountId, true))
                    return false;

                if (!password.Equals(confirmPassword))
                {
                    handler.SendSysMessage(CypherStrings.NewPasswordsNotMatch);
                    return false;
                }

                AccountOpResult result = Global.AccountMgr.ChangePassword(targetAccountId, password);
                switch (result)
                {
                    case AccountOpResult.Ok:
                        handler.SendSysMessage(CypherStrings.CommandPassword);
                        break;
                    case AccountOpResult.NameNotExist:
                        handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                        return false;
                    case AccountOpResult.PassTooLong:
                        handler.SendSysMessage(CypherStrings.PasswordTooLong);
                        return false;
                    default:
                        handler.SendSysMessage(CypherStrings.CommandNotchangepassword);
                        return false;
                }

                return true;
            }

            [Command("seclevel", RBACPermissions.CommandAccountSetSecLevel, true)]
            static bool HandleAccountSetSecLevelCommand(CommandHandler handler, string accountName, byte securityLevel, int? realmId)
            {
                uint accountId;
                if (!accountName.IsEmpty())
                {
                    accountId = Global.AccountMgr.GetId(accountName);
                    if (accountId == 0)
                    {
                        handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                        return false;
                    }
                }
                else
                {
                    Player player = handler.GetSelectedPlayer();
                    if (!player)
                        return false;

                    accountId = player.GetSession().GetAccountId();
                    Global.AccountMgr.GetName(accountId, out accountName);
                }

                if (securityLevel > (uint)AccountTypes.Console)
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                int realmID = -1;
                if (realmId.HasValue)
                    realmID = realmId.Value;

                AccountTypes playerSecurity;
                if (handler.IsConsole())
                    playerSecurity = AccountTypes.Console;
                else
                    playerSecurity = Global.AccountMgr.GetSecurity(handler.GetSession().GetAccountId(), realmID);

                // can set security level only for target with less security and to less security that we have
                // This is also reject self apply in fact
                AccountTypes targetSecurity = Global.AccountMgr.GetSecurity(accountId, realmID);
                if (targetSecurity >= playerSecurity || (AccountTypes)securityLevel >= playerSecurity)
                {
                    handler.SendSysMessage(CypherStrings.YoursSecurityIsLow);
                    return false;
                }
                PreparedStatement stmt;
                // Check and abort if the target gm has a higher rank on one of the realms and the new realm is -1
                if (realmID == -1 && !Global.AccountMgr.IsConsoleAccount(playerSecurity))
                {
                    stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_ACCESS_SECLEVEL_TEST);
                    stmt.AddValue(0, accountId);
                    stmt.AddValue(1, securityLevel);

                    SQLResult result = DB.Login.Query(stmt);

                    if (!result.IsEmpty())
                    {
                        handler.SendSysMessage(CypherStrings.YoursSecurityIsLow);
                        return false;
                    }
                }

                // Check if provided realmID has a negative value other than -1
                if (realmID < -1)
                {
                    handler.SendSysMessage(CypherStrings.InvalidRealmid);
                    return false;
                }

                Global.AccountMgr.UpdateAccountAccess(null, accountId, (byte)securityLevel, realmID);

                handler.SendSysMessage(CypherStrings.YouChangeSecurity, accountName, securityLevel);
                return true;
            }

            [CommandGroup("sec")]
            class SetSecCommands
            {
                [Command("email", RBACPermissions.CommandAccountSetSecEmail, true)]
                static bool HandleAccountSetEmailCommand(CommandHandler handler, string accountName, string email, string confirmEmail)
                {
                    uint targetAccountId = Global.AccountMgr.GetId(accountName);
                    if (targetAccountId == 0)
                    {
                        handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                        return false;
                    }

                    // can set email only for target with less security
                    // This also restricts setting handler's own email.
                    if (handler.HasLowerSecurityAccount(null, targetAccountId, true))
                        return false;

                    if (!email.Equals(confirmEmail))
                    {
                        handler.SendSysMessage(CypherStrings.NewEmailsNotMatch);
                        return false;
                    }

                    AccountOpResult result = Global.AccountMgr.ChangeEmail(targetAccountId, email);
                    switch (result)
                    {
                        case AccountOpResult.Ok:
                            handler.SendSysMessage(CypherStrings.CommandEmail);
                            Log.outInfo(LogFilter.Player, "ChangeEmail: Account {0} [Id: {1}] had it's email changed to {2}.", accountName, targetAccountId, email);
                            break;
                        case AccountOpResult.NameNotExist:
                            handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                            return false;
                        case AccountOpResult.EmailTooLong:
                            handler.SendSysMessage(CypherStrings.EmailTooLong);
                            return false;
                        default:
                            handler.SendSysMessage(CypherStrings.CommandNotchangeemail);
                            return false;
                    }

                    return true;
                }

                [Command("regmail", RBACPermissions.CommandAccountSetSecRegmail, true)]
                static bool HandleAccountSetRegEmailCommand(CommandHandler handler, string accountName, string email, string confirmEmail)
                {
                    uint targetAccountId = Global.AccountMgr.GetId(accountName);
                    if (targetAccountId == 0)
                    {
                        handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                        return false;
                    }

                    // can set email only for target with less security
                    // This also restricts setting handler's own email.
                    if (handler.HasLowerSecurityAccount(null, targetAccountId, true))
                        return false;

                    if (!email.Equals(confirmEmail))
                    {
                        handler.SendSysMessage(CypherStrings.NewEmailsNotMatch);
                        return false;
                    }

                    AccountOpResult result = Global.AccountMgr.ChangeRegEmail(targetAccountId, email);
                    switch (result)
                    {
                        case AccountOpResult.Ok:
                            handler.SendSysMessage(CypherStrings.CommandEmail);
                            Log.outInfo(LogFilter.Player, "ChangeRegEmail: Account {0} [Id: {1}] had it's Registration Email changed to {2}.", accountName, targetAccountId, email);
                            break;
                        case AccountOpResult.NameNotExist:
                            handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                            return false;
                        case AccountOpResult.EmailTooLong:
                            handler.SendSysMessage(CypherStrings.EmailTooLong);
                            return false;
                        default:
                            handler.SendSysMessage(CypherStrings.CommandNotchangeemail);
                            return false;
                    }

                    return true;
                }
            }
        }


    }
}
