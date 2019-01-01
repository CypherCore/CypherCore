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
using Game.Entities;
using System;
using System.Net;

namespace Game.Chat.Commands
{
    [CommandGroup("ban", RBACPermissions.CommandBan, true)]
    class BanCommands
    {
        [Command("account", RBACPermissions.CommandBanAccount, true)]
        static bool HandleBanAccountCommand(StringArguments args, CommandHandler handler)
        {
            return HandleBanHelper(BanMode.Account, args, handler);
        }

        [Command("character", RBACPermissions.CommandBanCharacter, true)]
        static bool HandleBanCharacterCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string name = args.NextString();
            if (string.IsNullOrEmpty(name))
                return false;

            string durationStr = args.NextString();
            if (string.IsNullOrEmpty(durationStr))
                return false;

            if (!uint.TryParse(durationStr, out uint duration))
                return false;

            string reasonStr = args.NextString("");
            if (string.IsNullOrEmpty(reasonStr))
                return false;

            if (!ObjectManager.NormalizePlayerName(ref name))
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            string author = handler.GetSession() != null ? handler.GetSession().GetPlayerName() : "Server";

            switch (Global.WorldMgr.BanCharacter(name, durationStr, reasonStr, author))
            {
                case BanReturn.Success:
                    {
                        if (duration > 0)
                        {
                            if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
                                Global.WorldMgr.SendWorldText(CypherStrings.BanCharacterYoubannedmessageWorld, author, name, Time.secsToTimeString(Time.TimeStringToSecs(durationStr), true), reasonStr);
                            else
                                handler.SendSysMessage(CypherStrings.BanYoubanned, name, Time.secsToTimeString(Time.TimeStringToSecs(durationStr), true), reasonStr);
                        }
                        else
                        {
                            if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
                                Global.WorldMgr.SendWorldText(CypherStrings.BanCharacterYoupermbannedmessageWorld, author, name, reasonStr);
                            else
                                handler.SendSysMessage(CypherStrings.BanYoupermbanned, name, reasonStr);
                        }
                        break;
                    }
                case BanReturn.Notfound:
                    {
                        handler.SendSysMessage(CypherStrings.BanNotfound, "character", name);
                        return false;
                    }
                default:
                    break;
            }

            return true;
        }

        [Command("playeraccount", RBACPermissions.CommandBanPlayeraccount, true)]
        static bool HandleBanAccountByCharCommand(StringArguments args, CommandHandler handler)
        {
            return HandleBanHelper(BanMode.Character, args, handler);
        }

        [Command("ip", RBACPermissions.CommandBanIp, true)]
        static bool HandleBanIPCommand(StringArguments args, CommandHandler handler)
        {
            return HandleBanHelper(BanMode.IP, args, handler);
        }

        static bool HandleBanHelper(BanMode mode, StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string nameOrIP = args.NextString();
            if (string.IsNullOrEmpty(nameOrIP))
                return false;

            string durationStr = args.NextString();
            if (!uint.TryParse(durationStr, out uint duration))
                return false;

            string reasonStr = args.NextString("");
            if (string.IsNullOrEmpty(reasonStr))
                return false;

            switch (mode)
            {
                case BanMode.Character:
                    if (!ObjectManager.NormalizePlayerName(ref nameOrIP))
                    {
                        handler.SendSysMessage(CypherStrings.PlayerNotFound);
                        return false;
                    }
                    break;
                case BanMode.IP:
                    IPAddress address;
                    if (!IPAddress.TryParse(nameOrIP, out address))
                        return false;
                    break;
            }

            string author = handler.GetSession() ? handler.GetSession().GetPlayerName() : "Server";
            switch (Global.WorldMgr.BanAccount(mode, nameOrIP, durationStr, reasonStr, author))
            {
                case BanReturn.Success:
                    if (!uint.TryParse(durationStr, out uint tempValue) || tempValue > 0)
                    {
                        if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
                            Global.WorldMgr.SendWorldText(CypherStrings.BanAccountYoubannedmessageWorld, author, nameOrIP, Time.secsToTimeString(Time.TimeStringToSecs(durationStr)), reasonStr);
                        else
                            handler.SendSysMessage(CypherStrings.BanYoubanned, nameOrIP, Time.secsToTimeString(Time.TimeStringToSecs(durationStr), true), reasonStr);
                    }
                    else
                    {
                        if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
                            Global.WorldMgr.SendWorldText(CypherStrings.BanAccountYoupermbannedmessageWorld, author, nameOrIP, reasonStr);
                        else
                            handler.SendSysMessage(CypherStrings.BanYoupermbanned, nameOrIP, reasonStr);
                    }
                    break;
                case BanReturn.SyntaxError:
                    return false;
                case BanReturn.Notfound:
                    switch (mode)
                    {
                        default:
                            handler.SendSysMessage(CypherStrings.BanNotfound, "account", nameOrIP);
                            break;
                        case BanMode.Character:
                            handler.SendSysMessage(CypherStrings.BanNotfound, "character", nameOrIP);
                            break;
                        case BanMode.IP:
                            handler.SendSysMessage(CypherStrings.BanNotfound, "ip", nameOrIP);
                            break;
                    }
                    return false;
                case BanReturn.Exists:
                    handler.SendSysMessage(CypherStrings.BanExists);
                    break;
            }

            return true;
        }
    }

    [CommandGroup("baninfo", RBACPermissions.CommandBaninfo, true)]
    class BanInfoCommands
    {
        [Command("account", RBACPermissions.CommandBaninfoAccount, true)]
        static bool HandleBanInfoAccountCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string accountName = args.NextString("");
            if (string.IsNullOrEmpty(accountName))
                return false;

            uint accountId = Global.AccountMgr.GetId(accountName);
            if (accountId == 0)
            {
                handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                return true;
            }

            return HandleBanInfoHelper(accountId, accountName, handler);
        }

        [Command("character", RBACPermissions.CommandBaninfoCharacter, true)]
        static bool HandleBanInfoCharacterCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string name = args.NextString();
            Player target = Global.ObjAccessor.FindPlayerByName(name);
            ObjectGuid targetGuid;

            if (!target)
            {
                targetGuid = ObjectManager.GetPlayerGUIDByName(name);
                if (targetGuid.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.BaninfoNocharacter);
                    return false;
                }
            }
            else
                targetGuid = target.GetGUID();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_BANINFO);
            stmt.AddValue(0, targetGuid.GetCounter());
            SQLResult result = DB.Characters.Query(stmt);
            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.CharNotBanned, name);
                return true;
            }

            handler.SendSysMessage(CypherStrings.BaninfoBanhistory, name);
            do
            {
                long unbanDate = result.Read<uint>(3);
                bool active = false;
                if (result.Read<bool>(2) && (result.Read<uint>(1) == 0 || unbanDate >= Time.UnixTime))
                    active = true;
                bool permanent = (result.Read<uint>(1) == 0);
                string banTime = permanent ? handler.GetCypherString(CypherStrings.BaninfoInfinite) : Time.secsToTimeString(result.Read<uint>(1), true);
                handler.SendSysMessage(CypherStrings.BaninfoHistoryentry, Time.UnixTimeToDateTime(result.Read<uint>(0)).ToShortTimeString(), banTime, 
                    active ? handler.GetCypherString(CypherStrings.Yes) : handler.GetCypherString(CypherStrings.No), result.Read<string>(4), result.Read<string>(5));
            }
            while (result.NextRow());

            return true;
        }

        [Command("ip", RBACPermissions.CommandBaninfoIp, true)]
        static bool HandleBanInfoIPCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string ip = args.NextString("");
            if (string.IsNullOrEmpty(ip))
                return false;

            SQLResult result = DB.Login.Query("SELECT ip, FROM_UNIXTIME(bandate), FROM_UNIXTIME(unbandate), unbandate-UNIX_TIMESTAMP(), banreason, bannedby, unbandate-bandate FROM ip_banned WHERE ip = '{0}'", ip);
            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.BaninfoNoip);
                return true;
            }

            bool permanent = result.Read<ulong>(6) == 0;
            handler.SendSysMessage(CypherStrings.BaninfoIpentry, result.Read<string>(0), result.Read<string>(1), permanent ? handler.GetCypherString( CypherStrings.BaninfoNever) : result.Read<string>(2),
                permanent ? handler.GetCypherString( CypherStrings.BaninfoInfinite) : Time.secsToTimeString(result.Read<ulong>(3), true), result.Read<string>(4), result.Read<string>(5));

            return true;
        }

        static bool HandleBanInfoHelper(uint accountId, string accountName, CommandHandler handler)
        {
            SQLResult result = DB.Login.Query("SELECT FROM_UNIXTIME(bandate), unbandate-bandate, active, unbandate, banreason, bannedby FROM account_banned WHERE id = '{0}' ORDER BY bandate ASC", accountId);
            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.BaninfoNoaccountban, accountName);
                return true;
            }

            handler.SendSysMessage(CypherStrings.BaninfoBanhistory, accountName);
            do
            {
                long unbanDate = result.Read<uint>(3);
                bool active = false;
                if (result.Read<bool>(2) && (result.Read<ulong>(1) == 0 || unbanDate >= Time.UnixTime))
                    active = true;
                bool permanent = (result.Read<ulong>(1) == 0);
                string banTime = permanent ? handler.GetCypherString(CypherStrings.BaninfoInfinite) : Time.secsToTimeString(result.Read<ulong>(1), true);
                handler.SendSysMessage(CypherStrings.BaninfoHistoryentry,
                    result.Read<string>(0), banTime, active ? handler.GetCypherString(CypherStrings.Yes) : handler.GetCypherString(CypherStrings.No), result.Read<string>(4), result.Read<string>(5));
            }
            while (result.NextRow());

            return true;
        }

    }

    [CommandGroup("banlist", RBACPermissions.CommandBanlist, true)]
    class BanListCommands
    {
        [Command("account", RBACPermissions.CommandBanlistAccount, true)]
        static bool HandleBanListAccountCommand(StringArguments args, CommandHandler handler)
        {
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_EXPIRED_IP_BANS);
            DB.Login.Execute(stmt);

            string filterStr = args.NextString();
            string filter = !string.IsNullOrEmpty(filterStr) ? filterStr : "";

            SQLResult result;
            if (string.IsNullOrEmpty(filter))
            {
                stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BANNED_ALL);
                result = DB.Login.Query(stmt);
            }
            else
            {
                stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BANNED_BY_USERNAME);
                stmt.AddValue(0, filter);
                result = DB.Login.Query(stmt);
            }

            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.BanlistNoaccount);
                return true;
            }

            return HandleBanListHelper(result, handler);
        }

        [Command("character", RBACPermissions.CommandBanlistCharacter, true)]
        static bool HandleBanListCharacterCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string filter = args.NextString();
            if (string.IsNullOrEmpty(filter))
                return false;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_GUID_BY_NAME_FILTER);
            stmt.AddValue(0, filter);
            SQLResult result = DB.Characters.Query(stmt);
            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.BanlistNocharacter);
                return true;
            }

            handler.SendSysMessage(CypherStrings.BanlistMatchingcharacter);

            // Chat short output
            if (handler.GetSession())
            {
                do
                {

                    PreparedStatement stmt2 = DB.Characters.GetPreparedStatement(CharStatements.SEL_BANNED_NAME);
                    stmt2.AddValue(0, result.Read<ulong>(0));
                    SQLResult banResult = DB.Characters.Query(stmt2);
                    if (!banResult.IsEmpty())
                        handler.SendSysMessage(banResult.Read<string>(0));
                }
                while (result.NextRow());
            }
            // Console wide output
            else
            {
                handler.SendSysMessage(CypherStrings.BanlistCharacters);
                handler.SendSysMessage(" =============================================================================== ");
                handler.SendSysMessage(CypherStrings.BanlistCharactersHeader);
                do
                {
                    handler.SendSysMessage("-------------------------------------------------------------------------------");

                    string char_name = result.Read<string>(1);

                    PreparedStatement stmt2 = DB.Characters.GetPreparedStatement(CharStatements.SEL_BANINFO_LIST);
                    stmt2.AddValue(0, result.Read<ulong>(0));
                    SQLResult banInfo = DB.Characters.Query(stmt2);
                    if (!banInfo.IsEmpty())
                    {
                        do
                        {
                            long timeBan = banInfo.Read<uint>(0);
                            DateTime tmBan = Time.UnixTimeToDateTime(timeBan);
                            string bannedby = banInfo.Read<string>(2).Substring(0, 15);
                            string banreason = banInfo.Read<string>(3).Substring(0, 15);

                            if (banInfo.Read<uint>(0) == banInfo.Read<uint>(1))
                            {
                                handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|   permanent  |{6}|{7}|",
                                    char_name, tmBan.Year % 100, tmBan.Month + 1, tmBan.Day, tmBan.Hour, tmBan.Minute,
                                    bannedby, banreason);
                            }
                            else
                            {
                                long timeUnban = banInfo.Read<uint>(1);
                                DateTime tmUnban = Time.UnixTimeToDateTime(timeUnban);
                                handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|{6:D2}-{7:D2}-{8:D2} {9:D2}:{10:D2}|{11}|{12}|",
                                    char_name, tmBan.Year % 100, tmBan.Month + 1, tmBan.Day, tmBan.Hour, tmBan.Minute,
                                    tmUnban.Year % 100, tmUnban.Month + 1, tmUnban.Day, tmUnban.Hour, tmUnban.Minute,
                                    bannedby, banreason);
                            }
                        }
                        while (banInfo.NextRow());
                    }
                }
                while (result.NextRow());
                handler.SendSysMessage(" =============================================================================== ");
            }

            return true;
        }

        [Command("ip", RBACPermissions.CommandBanlistIp, true)]
        static bool HandleBanListIPCommand(StringArguments args, CommandHandler handler)
        {
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.DEL_EXPIRED_IP_BANS);
            DB.Login.Execute(stmt);

            string filterStr = args.NextString();
            string filter = !string.IsNullOrEmpty(filterStr) ? filterStr : "";

            SQLResult result;

            if (string.IsNullOrEmpty(filter))
            {
                stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_IP_BANNED_ALL);
                result = DB.Login.Query(stmt);
            }
            else
            {
                stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_IP_BANNED_BY_IP);
                stmt.AddValue(0, filter);
                result = DB.Login.Query(stmt);
            }

            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.BanlistNoip);
                return true;
            }

            handler.SendSysMessage(CypherStrings.BanlistMatchingip);
            // Chat short output
            if (handler.GetSession())
            {
                do
                {
                    handler.SendSysMessage("{0}", result.Read<string>(0));
                }
                while (result.NextRow());
            }
            // Console wide output
            else
            {
                handler.SendSysMessage(CypherStrings.BanlistIps);
                handler.SendSysMessage(" ===============================================================================");
                handler.SendSysMessage(CypherStrings.BanlistIpsHeader);
                do
                {
                    handler.SendSysMessage("-------------------------------------------------------------------------------");

                    long timeBan = result.Read<uint>(1);
                    DateTime tmBan = Time.UnixTimeToDateTime(timeBan);
                    string bannedby = result.Read<string>(3).Substring(0, 15);
                    string banreason = result.Read<string>(4).Substring(0, 15);

                    if (result.Read<uint>(1) == result.Read<uint>(2))
                    {
                        handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|   permanent  |{6}|{7}|",
                            result.Read<string>(0), tmBan.Year % 100, tmBan.Month + 1, tmBan.Day, tmBan.Hour, tmBan.Minute,
                            bannedby, banreason);
                    }
                    else
                    {
                        long timeUnban = result.Read<uint>(2);
                        DateTime tmUnban;
                        tmUnban = Time.UnixTimeToDateTime(timeUnban);
                        handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|{6:D2}-{7:D2}-{8:D2} {9:D2}:{10:D2}|{11}|{12}|",
                            result.Read<string>(0), tmBan.Year % 100, tmBan.Month + 1, tmBan.Day, tmBan.Hour, tmBan.Minute,
                            tmUnban.Year % 100, tmUnban.Month + 1, tmUnban.Day, tmUnban.Hour, tmUnban.Minute,
                            bannedby, banreason);
                    }
                }
                while (result.NextRow());

                handler.SendSysMessage(" ===============================================================================");
            }

            return true;
        }

        static bool HandleBanListHelper(SQLResult result, CommandHandler handler)
        {
            handler.SendSysMessage(CypherStrings.BanlistMatchingaccount);

            // Chat short output
            if (handler.GetSession())
            {
                do
                {

                    uint accountid = result.Read<uint>(0);

                    SQLResult banResult = DB.Login.Query("SELECT account.username FROM account, account_banned WHERE account_banned.id='{0}' AND account_banned.id=account.id", accountid);
                    if (!banResult.IsEmpty())
                    {
                        handler.SendSysMessage(banResult.Read<string>(0));
                    }
                }
                while (result.NextRow());
            }
            // Console wide output
            else
            {
                handler.SendSysMessage(CypherStrings.BanlistAccounts);
                handler.SendSysMessage(" ===============================================================================");
                handler.SendSysMessage(CypherStrings.BanlistAccountsHeader);
                do
                {
                    handler.SendSysMessage("-------------------------------------------------------------------------------");

                    uint accountId = result.Read<uint>(0);

                    string accountName;

                    // "account" case, name can be get in same query
                    if (result.GetFieldCount() > 1)
                        accountName = result.Read<string>(1);
                    // "character" case, name need extract from another DB
                    else
                        Global.AccountMgr.GetName(accountId, out accountName);

                    // No SQL injection. id is uint32.
                    SQLResult banInfo = DB.Login.Query("SELECT bandate, unbandate, bannedby, banreason FROM account_banned WHERE id = {0} ORDER BY unbandate", accountId);
                    if (!banInfo.IsEmpty())
                    {
                        do
                        {
                            long timeBan = banInfo.Read<uint>(0);
                            DateTime tmBan;
                            tmBan = Time.UnixTimeToDateTime(timeBan);
                            string bannedby = banInfo.Read<string>(2).Substring(0, 15);
                            string banreason = banInfo.Read<string>(3).Substring(0, 15);

                            if (banInfo.Read<uint>(0) == banInfo.Read<uint>(1))
                            {
                                handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|   permanent  |{6}|{7}|",
                                    accountName.Substring(0, 15), tmBan.Year % 100, tmBan.Month + 1, tmBan.Day, tmBan.Hour, tmBan.Minute,
                                    bannedby, banreason);
                            }
                            else
                            {
                                long timeUnban = banInfo.Read <uint>(1);
                                DateTime tmUnban;
                                tmUnban = Time.UnixTimeToDateTime(timeUnban);
                                handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|{6:D2}-{7:D2}-{8:D2} {9:D2}:{10:D2}|{11}|{12}|",
                                    accountName.Substring(0, 15), tmBan.Year % 100, tmBan.Month + 1, tmBan.Day, tmBan.Hour, tmBan.Minute,
                                    tmUnban.Year % 100, tmUnban.Month + 1, tmUnban.Day, tmUnban.Hour, tmUnban.Minute,
                                    bannedby, banreason);
                            }
                        }
                        while (banInfo.NextRow());
                    }
                }
                while (result.NextRow());

                handler.SendSysMessage(" ===============================================================================");
            }

            return true;
        }
    }

    [CommandGroup("unban", RBACPermissions.CommandUnban, true)]
    class UnBanCommands
    {
        [Command("account", RBACPermissions.CommandUnbanAccount, true)]
        static bool HandleUnBanAccountCommand(StringArguments args, CommandHandler handler)
        {
            return HandleUnBanHelper(BanMode.Account, args, handler);
        }

        [Command("character", RBACPermissions.CommandUnbanCharacter, true)]
        static bool HandleUnBanCharacterCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string name = args.NextString();
            if (!ObjectManager.NormalizePlayerName(ref name))
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            if (!Global.WorldMgr.RemoveBanCharacter(name))
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            return true;
        }

        [Command("playeraccount", RBACPermissions.CommandUnbanPlayeraccount, true)]
        static bool HandleUnBanAccountByCharCommand(StringArguments args, CommandHandler handler)
        {
            return HandleUnBanHelper(BanMode.Character, args, handler);
        }

        [Command("ip", RBACPermissions.CommandUnbanIp, true)]
        static bool HandleUnBanIPCommand(StringArguments args, CommandHandler handler)
        {
            return HandleUnBanHelper(BanMode.IP, args, handler);
        }

        static bool HandleUnBanHelper(BanMode mode, StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string nameOrIP = args.NextString();
            if (string.IsNullOrEmpty(nameOrIP))
                return false;

            switch (mode)
            {
                case BanMode.Character:
                    if (!ObjectManager.NormalizePlayerName(ref nameOrIP))
                    {
                        handler.SendSysMessage(CypherStrings.PlayerNotFound);
                        return false;
                    }
                    break;
                case BanMode.IP:
                    IPAddress address;
                    if (!IPAddress.TryParse(nameOrIP, out address))
                        return false;
                    break;
            }

            if (Global.WorldMgr.RemoveBanAccount(mode, nameOrIP))
                handler.SendSysMessage(CypherStrings.UnbanUnbanned, nameOrIP);
            else
                handler.SendSysMessage(CypherStrings.UnbanError, nameOrIP);

            return true;
        }
    }
}
