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
using Framework.IO;
using System;

namespace Game.Chat.Commands
{
    [CommandGroup("bnetaccount", RBACPermissions.CommandBnetAccount, true)]
    internal class BNetAccountCommands
    {
        [Command("create", RBACPermissions.CommandBnetAccountCreate, true)]
        private static bool HandleAccountCreateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            // Parse the command line arguments
            var accountName = args.NextString();
            var password = args.NextString();
            if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(password))
                return false;

            if (!accountName.Contains('@'))
            {
                handler.SendSysMessage(CypherStrings.AccountInvalidBnetName);
                return false;
            }

            if (!bool.TryParse(args.NextString(), out var createGameAccount))
                createGameAccount = true;

            string gameAccountName;
            switch (Global.BNetAccountMgr.CreateBattlenetAccount(accountName, password, createGameAccount, out gameAccountName))
            {
                case AccountOpResult.Ok:
                    if (createGameAccount)
                        handler.SendSysMessage(CypherStrings.AccountCreatedBnetWithGame, accountName, gameAccountName);
                    else
                        handler.SendSysMessage(CypherStrings.AccountCreated, accountName);

                    if (handler.GetSession() != null)
                    {
                        Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] ({3}) created Battle.net account {4}{5}{6}",
                            handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(), handler.GetSession().GetPlayer().GetName(), 
                            handler.GetSession().GetPlayer().GetGUID().ToString(), accountName, createGameAccount ? " with game account " : "", createGameAccount ? gameAccountName : "");
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
                default:
                    break;
            }

            return true;
        }

        [Command("gameaccountcreate", RBACPermissions.CommandBnetAccountCreateGame, true)]
        private static bool HandleGameAccountCreateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            var bnetAccountName = args.NextString();
            var accountId = Global.BNetAccountMgr.GetId(bnetAccountName);
            if (accountId == 0)
            {
                handler.SendSysMessage(CypherStrings.AccountNotExist, bnetAccountName);
                return false;
            }

            var index = (byte)(Global.BNetAccountMgr.GetMaxIndex(accountId) + 1);
            var accountName = accountId.ToString() + '#' + index;

            // Generate random hex string for password, these accounts must not be logged on with GRUNT
            var randPassword = new byte[0].GenerateRandomKey(8);
            switch (Global.AccountMgr.CreateAccount(accountName, randPassword.ToHexString(), bnetAccountName, accountId, index))
            {
                case AccountOpResult.Ok:
                    handler.SendSysMessage(CypherStrings.AccountCreated, accountName);
                    if (handler.GetSession() != null)
                    {
                        Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] ({3}) created Account {4} (Email: '{5}')",
                            handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(), handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(),
                            accountName, bnetAccountName);
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

        [Command("link", RBACPermissions.CommandBnetAccountLink, true)]
        private static bool HandleAccountLinkCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            var bnetAccountName = args.NextString();
            var gameAccountName = args.NextString();

            switch (Global.BNetAccountMgr.LinkWithGameAccount(bnetAccountName, gameAccountName))
            {
                case AccountOpResult.Ok:
                    handler.SendSysMessage(CypherStrings.AccountBnetLinked, bnetAccountName, gameAccountName);
                    break;
                case AccountOpResult.NameNotExist:
                    handler.SendSysMessage(CypherStrings.AccountOrBnetDoesNotExist, bnetAccountName, gameAccountName);
                    break;
                case AccountOpResult.BadLink:
                    handler.SendSysMessage( CypherStrings.AccountAlreadyLinked, gameAccountName);
                    break;
                default:
                    break;
            }

            return true;
        }

        [Command("listgameaccounts", RBACPermissions.CommandBnetAccountListGameAccounts, true)]
        private static bool HandleListGameAccountsCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var battlenetAccountName = args.NextString();
            var stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_GAME_ACCOUNT_LIST);
            stmt.AddValue(0, battlenetAccountName);

            var accountList = DB.Login.Query(stmt);
            if (!accountList.IsEmpty())
            {
                var formatDisplayName = new Func<string, string>(name =>
                {
                    var index = name.IndexOf('#');
                    if (index > 0)
                        return "WoW" + name.Substring(++index);
                    else
                        return name;
                });

                handler.SendSysMessage("----------------------------------------------------");
                handler.SendSysMessage(CypherStrings.AccountBnetListHeader);
                handler.SendSysMessage("----------------------------------------------------");
                do
                {
                    handler.SendSysMessage("| {10:0} | {1} | {2} |", accountList.Read<uint>(0), accountList.Read<string>(1), formatDisplayName(accountList.Read<string>(1)));
                } while (accountList.NextRow());
                handler.SendSysMessage("----------------------------------------------------");
            }
            else
                handler.SendSysMessage(CypherStrings.AccountBnetListNoAccounts, battlenetAccountName);

            return true;
        }

        [Command("password", RBACPermissions.CommandBnetAccountPassword, true)]
        private static bool HandleAccountPasswordCommand(StringArguments args, CommandHandler handler)
        {
            // If no args are given at all, we can return false right away.
            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            // Command is supposed to be: .account password [$oldpassword] [$newpassword] [$newpasswordconfirmation] [$emailconfirmation]
            var oldPassword = args.NextString();       // This extracts [$oldpassword]
            var newPassword = args.NextString();              // This extracts [$newpassword]
            var passwordConfirmation = args.NextString();     // This extracts [$newpasswordconfirmation]

            //Is any of those variables missing for any reason ? We return false.
            if (string.IsNullOrEmpty(oldPassword) || string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(passwordConfirmation))
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            // We compare the old, saved password to the entered old password - no chance for the unauthorized.
            if (!Global.BNetAccountMgr.CheckPassword(handler.GetSession().GetBattlenetAccountId(), oldPassword))
            {
                handler.SendSysMessage(CypherStrings.CommandWrongoldpassword);

                Log.outInfo(LogFilter.Player, "Battle.net account: {0} (IP: {1}) Character:[{2}] ({3}) Tried to change password, but the provided old password is wrong.",
                    handler.GetSession().GetBattlenetAccountId(), handler.GetSession().GetRemoteAddress(), handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString());
                return false;
            }

            // Making sure that newly entered password is correctly entered.
            if (newPassword != passwordConfirmation)
            {
                handler.SendSysMessage(CypherStrings.NewPasswordsNotMatch);
                return false;
            }

            // Changes password and prints result.
            var result = Global.BNetAccountMgr.ChangePassword(handler.GetSession().GetBattlenetAccountId(), newPassword);
            switch (result)
            {
                case AccountOpResult.Ok:
                    handler.SendSysMessage(CypherStrings.CommandPassword);
                    Log.outInfo(LogFilter.Player, "Battle.net account: {0} (IP: {1}) Character:[{2}] ({3}) Changed Password.",
                        handler.GetSession().GetBattlenetAccountId(), handler.GetSession().GetRemoteAddress(), handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString());
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

        [Command("unlink", RBACPermissions.CommandBnetAccountUnlink, true)]
        private static bool HandleAccountUnlinkCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
            {
                handler.SendSysMessage(CypherStrings.CmdSyntax);
                return false;
            }

            var gameAccountName = args.NextString();
            switch (Global.BNetAccountMgr.UnlinkGameAccount(gameAccountName))
            {
                case AccountOpResult.Ok:
                    handler.SendSysMessage(CypherStrings.AccountBnetUnlinked, gameAccountName);
                    break;
                case AccountOpResult.NameNotExist:
                    handler.SendSysMessage(CypherStrings.AccountNotExist, gameAccountName);
                    break;
                case AccountOpResult.BadLink:
                    handler.SendSysMessage(CypherStrings.AccountBnetNotLinked, gameAccountName);
                    break;
                default:
                    break;
            }

            return true;
        }

        [CommandGroup("lock", RBACPermissions.CommandBnetAccount, true)]
        private class LockCommands
        {
            [Command("country", RBACPermissions.CommandBnetAccountLockCountry, true)]
            private static bool HandleLockCountryCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.UseBol);
                    return false;
                }

                var param = args.NextString();
                if (!string.IsNullOrEmpty(param))
                {
                    if (param == "on")
                    {
                        /*PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_LOGON_COUNTRY);
                        var ipBytes = System.Net.IPAddress.Parse(handler.GetSession().GetRemoteAddress()).GetAddressBytes();
                        Array.Reverse(ipBytes);
                        stmt.AddValue(0, BitConverter.ToUInt32(ipBytes, 0));
                        SQLResult result = DB.Login.Query(stmt);
                        if (!result.IsEmpty())
                        {
                            string country = result.Read<string>(0);
                            stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_ACCOUNT_LOCK_CONTRY);
                            stmt.AddValue(0, country);
                            stmt.AddValue(1, handler.GetSession().GetBattlenetAccountId());
                            DB.Login.Execute(stmt);
                            handler.SendSysMessage(CypherStrings.CommandAcclocklocked);
                        }
                        else
                        {
                            handler.SendSysMessage("[IP2NATION] Table empty");
                            Log.outDebug(LogFilter.Server, "[IP2NATION] Table empty");
                        }*/
                    }
                    else if (param == "off")
                    {
                        var stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_ACCOUNT_LOCK_CONTRY);
                        stmt.AddValue(0, "00");
                        stmt.AddValue(1, handler.GetSession().GetBattlenetAccountId());
                        DB.Login.Execute(stmt);
                        handler.SendSysMessage(CypherStrings.CommandAcclockunlocked);
                    }
                    return true;
                }

                handler.SendSysMessage(CypherStrings.UseBol);
                return false;
            }

            [Command("ip", RBACPermissions.CommandBnetAccountLockIp, true)]
            private static bool HandleLockIpCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.UseBol);
                    return false;
                }

                var param = args.NextString();

                if (!string.IsNullOrEmpty(param))
                {
                    var stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_ACCOUNT_LOCK);
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

                    stmt.AddValue(1, handler.GetSession().GetBattlenetAccountId());
                    DB.Login.Execute(stmt);
                    return true;
                }

                handler.SendSysMessage(CypherStrings.UseBol);
                return false;
            }
        }

        [CommandGroup("set", RBACPermissions.CommandBnetAccountSet, true)]
        private class SetCommands
        {
            [Command("password", RBACPermissions.CommandBnetAccountSetPassword, true)]
            private static bool HandleSetPasswordCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                {
                    handler.SendSysMessage(CypherStrings.CmdSyntax);
                    return false;
                }

                // Get the command line arguments
                var accountName = args.NextString();
                var password = args.NextString();
                var passwordConfirmation = args.NextString();

                if (string.IsNullOrEmpty(accountName) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(passwordConfirmation))
                    return false;

                var targetAccountId = Global.BNetAccountMgr.GetId(accountName);
                if (targetAccountId == 0)
                {
                    handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                    return false;
                }

                if (password != passwordConfirmation)
                {
                    handler.SendSysMessage(CypherStrings.NewPasswordsNotMatch);
                    return false;
                }

                var result = Global.BNetAccountMgr.ChangePassword(targetAccountId, password);
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
                        break;
                }
                return true;
            }
        }


    }
}
