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
using Game.Accounts;
using Game.Entities;
using System;

namespace Game.Chat
{
    [CommandGroup("account", RBACPermissions.CommandAccount, true)]
    class AccountCommands
    {
        [Command("", RBACPermissions.CommandAccount)]
        static bool HandleAccountCommand(StringArguments args, CommandHandler handler)
        {
            if (handler.GetSession() == null)
                return false;

            // GM Level
            AccountTypes gmLevel = handler.GetSession().GetSecurity();
            handler.SendSysMessage(CypherStrings.AccountLevel, gmLevel);

            // Security level required
            WorldSession session = handler.GetSession();
            bool hasRBAC = (session.HasPermission(RBACPermissions.EmailConfirmForPassChange) ? true : false);
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
        static bool HandleAccountAddonCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            uint accountId = handler.GetSession().GetAccountId();

            int expansion = args.NextInt32(); //get int anyway (0 if error)
            if (expansion < 0 || expansion > WorldConfig.GetIntValue(WorldCfg.Expansion))
            {
                handler.SendSysMessage(CypherStrings.ImproperValue);
                return false;
            }

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_EXPANSION);
            stmt.AddValue(0, expansion);
            stmt.AddValue(1, accountId);
            DB.Login.Execute(stmt);

            handler.SendSysMessage(CypherStrings.AccountAddon, expansion);
            return true;
        }

        [Command("create", RBACPermissions.CommandAccountCreate, true)]
        static bool HandleAccountCreateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var accountName = args.NextString().ToUpper();
            var password = args.NextString();
            string email = "";

            if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(password))
                return false;

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
        static bool HandleAccountDeleteCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string accountName = args.NextString();

            if (string.IsNullOrEmpty(accountName))
                return false;

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

        [Command("email", RBACPermissions.CommandAccountSetSecEmail)]
        static bool HandleAccountEmailCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            string oldEmail = args.NextString();
            string password = args.NextString();
            string email = args.NextString();
            string emailConfirmation = args.NextString();

            if (string.IsNullOrEmpty(oldEmail) || string.IsNullOrEmpty(password)
                || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(emailConfirmation))
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

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

            if (email != emailConfirmation)
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
        static bool HandleAccountPasswordCommand(StringArguments args, CommandHandler handler)
        {
            // If no args are given at all, we can return false right away.
            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            // First, we check config. What security type (sec type) is it ? Depending on it, the command branches out
            uint pwConfig = WorldConfig.GetUIntValue(WorldCfg.AccPasschangesec); // 0 - PW_NONE, 1 - PW_EMAIL, 2 - PW_RBAC

            // Command is supposed to be: .account password [$oldpassword] [$newpassword] [$newpasswordconfirmation] [$emailconfirmation]
            string oldPassword = args.NextString();       // This extracts [$oldpassword]
            string newPassword = args.NextString();              // This extracts [$newpassword]
            string passwordConfirmation = args.NextString();     // This extracts [$newpasswordconfirmation]
            string emailConfirmation = args.NextString();  // This defines the emailConfirmation variable, which is optional depending on sec type.

            //Is any of those variables missing for any reason ? We return false.
            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword)
                || string.IsNullOrEmpty(passwordConfirmation))
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);

                return false;
            }

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
                && !Global.AccountMgr.CheckEmail(handler.GetSession().GetAccountId(), emailConfirmation)) // ... and returns false if the comparison fails.
            {
                handler.SendSysMessage(CypherStrings.CommandWrongemail);

                Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change password, but the entered email [{4}] is wrong.",
                    handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                    handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(),
                    emailConfirmation);
                return false;
            }

            // Making sure that newly entered password is correctly entered.
            if (newPassword != passwordConfirmation)
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

        [Command("onlinelist", RBACPermissions.CommandAccountOnlineList, true)]
        static bool HandleAccountOnlineListCommand(StringArguments args, CommandHandler handler)
        {
            // Get the list of accounts ID logged to the realm
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_ONLINE);

            SQLResult result = DB.Characters.Query(stmt);

            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.AccountListEmpty);
                return true;
            }

            // Display the list of account/characters online
            handler.SendSysMessage(CypherStrings.AccountListBarHeader);
            handler.SendSysMessage(CypherStrings.AccountListHeader);
            handler.SendSysMessage(CypherStrings.AccountListBar);

            // Cycle through accounts
            do
            {
                string name = result.Read<string>(0);
                uint account = result.Read<uint>(1);

                // Get the username, last IP and GM level of each account
                // No SQL injection. account is uint32.
                stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_INFO);
                stmt.AddValue(0, account);
                SQLResult resultLogin = DB.Login.Query(stmt);

                if (!resultLogin.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.AccountListLine, resultLogin.Read<string>(0),
                        name, resultLogin.Read<string>(1), result.Read<ushort>(2), result.Read<ushort>(3),
                        resultLogin.Read<byte>(3), resultLogin.Read<byte>(2));
                }
                else
                    handler.SendSysMessage(CypherStrings.AccountListError, name);
            }
            while (result.NextRow());

            handler.SendSysMessage(CypherStrings.AccountListBar);
            return true;
        }

        [CommandGroup("set", RBACPermissions.CommandAccountSet, true)]
        class SetCommands
        {
            [Command("password", RBACPermissions.CommandAccountSetPassword, true)]
            static bool HandleSetPasswordCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.CmdSyntax);
                    return false;
                }

                // Get the command line arguments
                string accountName = args.NextString();
                string password = args.NextString();
                string passwordConfirmation = args.NextString();

                if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordConfirmation))
                    return false;

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

                if (!password.Equals(passwordConfirmation))
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

            [Command("addon", RBACPermissions.CommandAccountSetAddon, true)]
            static bool HandleSetAddonCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                // Get the command line arguments
                string account = args.NextString();
                string exp = args.NextString();

                if (string.IsNullOrEmpty(account))
                    return false;

                string accountName;
                uint accountId;

                if (string.IsNullOrEmpty(exp))
                {
                    Player player = handler.getSelectedPlayer();
                    if (!player)
                        return false;

                    accountId = player.GetSession().GetAccountId();
                    Global.AccountMgr.GetName(accountId, out accountName);
                    exp = account;
                }
                else
                {
                    // Convert Account name to Upper Format
                    accountName = account.ToUpper();

                    accountId = Global.AccountMgr.GetId(accountName);
                    if (accountId == 0)
                    {
                        handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                        return false;
                    }
                }

                // Let set addon state only for lesser (strong) security level
                // or to self account
                if (handler.GetSession() != null && handler.GetSession().GetAccountId() != accountId &&
                    handler.HasLowerSecurityAccount(null, accountId, true))
                    return false;

                if (!byte.TryParse(exp, out byte expansion))
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

            [Command("gmlevel", RBACPermissions.CommandAccountSetGmlevel, true)]
            static bool HandleSetGmLevelCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.CmdSyntax);
                    return false;
                }

                string targetAccountName = "";
                uint targetAccountId = 0;
                AccountTypes targetSecurity = 0;
                uint gm = 0;
                string arg1 = args.NextString();
                string arg2 = args.NextString();
                string arg3 = args.NextString();
                bool isAccountNameGiven = true;

                if (string.IsNullOrEmpty(arg3))
                {
                    if (!handler.getSelectedPlayer())
                        return false;
                    isAccountNameGiven = false;
                }

                if (!isAccountNameGiven && string.IsNullOrEmpty(arg2))
                    return false;

                if (isAccountNameGiven)
                {
                    targetAccountName = arg1;
                    if (Global.AccountMgr.GetId(targetAccountName) == 0)
                    {
                        handler.SendSysMessage(CypherStrings.AccountNotExist, targetAccountName);
                        return false;
                    }
                }

                // Check for invalid specified GM level.
                if (!uint.TryParse(isAccountNameGiven ? arg2 : arg1, out gm))
                    return false;

                if (gm > (uint)AccountTypes.Console)
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                // command.getSession() == NULL only for console
                targetAccountId = (isAccountNameGiven) ? Global.AccountMgr.GetId(targetAccountName) : handler.getSelectedPlayer().GetSession().GetAccountId();
                if (!int.TryParse(isAccountNameGiven ? arg3 : arg2, out int gmRealmID))
                    return false;

                AccountTypes playerSecurity;
                if (handler.GetSession() != null)
                    playerSecurity = Global.AccountMgr.GetSecurity(handler.GetSession().GetAccountId(), gmRealmID);
                else
                    playerSecurity = AccountTypes.Console;

                // can set security level only for target with less security and to less security that we have
                // This is also reject self apply in fact
                targetSecurity = Global.AccountMgr.GetSecurity(targetAccountId, gmRealmID);
                if (targetSecurity >= playerSecurity || (AccountTypes)gm >= playerSecurity)
                {
                    handler.SendSysMessage(CypherStrings.YoursSecurityIsLow);
                    return false;
                }
                PreparedStatement stmt;
                // Check and abort if the target gm has a higher rank on one of the realms and the new realm is -1
                if (gmRealmID == -1 && !Global.AccountMgr.IsConsoleAccount(playerSecurity))
                {
                    stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_ACCESS_GMLEVEL_TEST);
                    stmt.AddValue(0, targetAccountId);
                    stmt.AddValue(1, gm);

                    SQLResult result = DB.Login.Query(stmt);

                    if (!result.IsEmpty())
                    {
                        handler.SendSysMessage(CypherStrings.YoursSecurityIsLow);
                        return false;
                    }
                }

                // Check if provided realmID has a negative value other than -1
                if (gmRealmID < -1)
                {
                    handler.SendSysMessage(CypherStrings.InvalidRealmid);
                    return false;
                }

                RBACData rbac = isAccountNameGiven ? null : handler.getSelectedPlayer().GetSession().GetRBACData();
                Global.AccountMgr.UpdateAccountAccess(rbac, targetAccountId, (byte)gm, gmRealmID);
                handler.SendSysMessage(CypherStrings.YouChangeSecurity, targetAccountName, gm);
                return true;
            }

            [CommandGroup("sec", RBACPermissions.CommandAccountSetSec, true)]
            class SetSecCommands
            {
                [Command("email", RBACPermissions.CommandAccountSetSecEmail, true)]
                static bool HandleSetEmailCommand(StringArguments args, CommandHandler handler)
                {
                    if (args.Empty())
                        return false;

                    // Get the command line arguments
                    string accountName = args.NextString();
                    string email = args.NextString();
                    string emailConfirmation = args.NextString();

                    if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(emailConfirmation))
                    {
                        handler.SendSysMessage(CypherStrings.CmdSyntax);
                        return false;
                    }

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

                    if (!email.Equals(emailConfirmation))
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
                static bool HandleSetRegEmailCommand(StringArguments args, CommandHandler handler)
                {
                    if (args.Empty())
                        return false;

                    //- We do not want anything short of console to use this by default.
                    //- So we force that.
                    if (handler.GetSession())
                        return false;

                    // Get the command line arguments
                    string accountName = args.NextString();
                    string email = args.NextString();
                    string emailConfirmation = args.NextString();

                    if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(emailConfirmation))
                    {
                        handler.SendSysMessage(CypherStrings.CmdSyntax);
                        return false;
                    }

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

                    if (!email.Equals(emailConfirmation))
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

        [CommandGroup("lock", RBACPermissions.CommandAccountLock)]
        class LockCommands
        {
            [Command("country", RBACPermissions.CommandAccountLockCountry)]
            static bool HandleAccountLockCountryCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.UseBol);
                    return false;
                }

                string param = args.NextString();
                if (!param.IsEmpty())
                {
                    if (param == "on")
                    {
                        var ipBytes = System.Net.IPAddress.Parse(handler.GetSession().GetRemoteAddress()).GetAddressBytes();
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
                        }
                    }
                    else if (param == "off")
                    {
                        PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_LOCK_COUNTRY);
                        stmt.AddValue(0, "00");
                        stmt.AddValue(1, handler.GetSession().GetAccountId());
                        DB.Login.Execute(stmt);
                        handler.SendSysMessage(CypherStrings.CommandAcclockunlocked);
                    }
                    return true;
                }
                handler.SendSysMessage(CypherStrings.UseBol);
                return false;
            }

            [Command("ip", RBACPermissions.CommandAccountLockIp)]
            static bool HandleAccountLockIpCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.UseBol);
                    return false;
                }

                string param = args.NextString();
                if (!string.IsNullOrEmpty(param))
                {
                    PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_LOCK);

                    if (param == "on")
                    {
                        stmt.AddValue(0, true);                                     // locked
                        handler.SendSysMessage(CypherStrings.CommandAcclocklocked);
                    }
                    else if (param == "off")
                    {
                        stmt.AddValue(0, false);                                    // unlocked
                        handler.SendSysMessage(CypherStrings.CommandAcclockunlocked);
                    }
                    stmt.AddValue(1, handler.GetSession().GetAccountId());

                    DB.Login.Execute(stmt);
                    return true;
                }

                handler.SendSysMessage(CypherStrings.UseBol);
                return false;
            }
        }
    }
}
