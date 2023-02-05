// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Entities;
using System;
using System.Net;

namespace Game.Chat.Commands
{
    [CommandGroup("ban")]
    class BanCommands
    {
        [Command("account", RBACPermissions.CommandBanAccount, true)]
        static bool HandleBanAccountCommand(CommandHandler handler, string playerName, uint duration, string reason)
        {
            return HandleBanHelper(BanMode.Account, playerName, duration, reason, handler);
        }

        [Command("character", RBACPermissions.CommandBanCharacter, true)]
        static bool HandleBanCharacterCommand(CommandHandler handler, string playerName, uint duration, string reason)
        {
            if (playerName.IsEmpty())
                return false;

            if (duration == 0)
                return false;

            if (reason.IsEmpty())
                return false;

            if (!ObjectManager.NormalizePlayerName(ref playerName))
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            string author = handler.GetSession() != null ? handler.GetSession().GetPlayerName() : "Server";

            switch (Global.WorldMgr.BanCharacter(playerName, duration, reason, author))
            {
                case BanReturn.Success:
                {
                    if (duration > 0)
                    {
                        if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
                            Global.WorldMgr.SendWorldText(CypherStrings.BanCharacterYoubannedmessageWorld, author, playerName, Time.secsToTimeString(duration, TimeFormat.ShortText), reason);
                        else
                            handler.SendSysMessage(CypherStrings.BanYoubanned, playerName, Time.secsToTimeString(duration, TimeFormat.ShortText), reason);
                    }
                    else
                    {
                        if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
                            Global.WorldMgr.SendWorldText(CypherStrings.BanCharacterYoupermbannedmessageWorld, author, playerName, reason);
                        else
                            handler.SendSysMessage(CypherStrings.BanYoupermbanned, playerName, reason);
                    }
                    break;
                }
                case BanReturn.Notfound:
                {
                    handler.SendSysMessage(CypherStrings.BanNotfound, "character", playerName);
                    return false;
                }
                default:
                    break;
            }

            return true;
        }

        [Command("playeraccount", RBACPermissions.CommandBanPlayeraccount, true)]
        static bool HandleBanAccountByCharCommand(CommandHandler handler, string playerName, uint duration, string reason)
        {
            return HandleBanHelper(BanMode.Character, playerName, duration, reason, handler);
        }

        [Command("ip", RBACPermissions.CommandBanIp, true)]
        static bool HandleBanIPCommand(CommandHandler handler, string ipAddress, uint duration, string reason)
        {
            return HandleBanHelper(BanMode.IP, ipAddress, duration, reason, handler);
        }

        static bool HandleBanHelper(BanMode mode, string nameOrIP, uint duration, string reason, CommandHandler handler)
        {
            if (nameOrIP.IsEmpty())
                return false;

            if (reason.IsEmpty())
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
                    if (!IPAddress.TryParse(nameOrIP, out _))
                        return false;
                    break;
            }

            string author = handler.GetSession() ? handler.GetSession().GetPlayerName() : "Server";
            switch (Global.WorldMgr.BanAccount(mode, nameOrIP, duration, reason, author))
            {
                case BanReturn.Success:
                    if (duration > 0)
                    {
                        if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
                            Global.WorldMgr.SendWorldText(CypherStrings.BanAccountYoubannedmessageWorld, author, nameOrIP, Time.secsToTimeString(duration), reason);
                        else
                            handler.SendSysMessage(CypherStrings.BanYoubanned, nameOrIP, Time.secsToTimeString(duration, TimeFormat.ShortText), reason);
                    }
                    else
                    {
                        if (WorldConfig.GetBoolValue(WorldCfg.ShowBanInWorld))
                            Global.WorldMgr.SendWorldText(CypherStrings.BanAccountYoupermbannedmessageWorld, author, nameOrIP, reason);
                        else
                            handler.SendSysMessage(CypherStrings.BanYoupermbanned, nameOrIP, reason);
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

    [CommandGroup("baninfo")]
    class BanInfoCommands
    {
        [Command("account", RBACPermissions.CommandBaninfoAccount, true)]
        static bool HandleBanInfoAccountCommand(CommandHandler handler, string accountName)
        {
            if (accountName.IsEmpty())
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
        static bool HandleBanInfoCharacterCommand(CommandHandler handler, string name)
        {
            if (!ObjectManager.NormalizePlayerName(ref name))
            {
                handler.SendSysMessage(CypherStrings.BaninfoNocharacter);
                return false;
            }

            Player target = Global.ObjAccessor.FindPlayerByName(name);
            ObjectGuid targetGuid;

            if (!target)
            {
                targetGuid = Global.CharacterCacheStorage.GetCharacterGuidByName(name);
                if (targetGuid.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.BaninfoNocharacter);
                    return false;
                }
            }
            else
                targetGuid = target.GetGUID();

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_BANINFO);
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
                long unbanDate = result.Read<long>(3);
                bool active = false;
                if (result.Read<bool>(2) && (result.Read<long>(1) == 0L || unbanDate >= GameTime.GetGameTime()))
                    active = true;
                bool permanent = (result.Read<long>(1) == 0L);
                string banTime = permanent ? handler.GetCypherString(CypherStrings.BaninfoInfinite) : Time.secsToTimeString(result.Read<ulong>(1), TimeFormat.ShortText);
                handler.SendSysMessage(CypherStrings.BaninfoHistoryentry, Time.UnixTimeToDateTime(result.Read<long>(0)).ToShortTimeString(), banTime,
                    active ? handler.GetCypherString(CypherStrings.Yes) : handler.GetCypherString(CypherStrings.No), result.Read<string>(4), result.Read<string>(5));
            }
            while (result.NextRow());

            return true;
        }

        [Command("ip", RBACPermissions.CommandBaninfoIp, true)]
        static bool HandleBanInfoIPCommand(CommandHandler handler, string ip)
        {
            if (ip.IsEmpty())
                return false;

            SQLResult result = DB.Login.Query("SELECT ip, FROM_UNIXTIME(bandate), FROM_UNIXTIME(unbandate), unbandate-UNIX_TIMESTAMP(), banreason, bannedby, unbandate-bandate FROM ip_banned WHERE ip = '{0}'", ip);
            if (result.IsEmpty())
            {
                handler.SendSysMessage(CypherStrings.BaninfoNoip);
                return true;
            }

            bool permanent = result.Read<ulong>(6) == 0;
            handler.SendSysMessage(CypherStrings.BaninfoIpentry, result.Read<string>(0), result.Read<string>(1), permanent ? handler.GetCypherString(CypherStrings.BaninfoNever) : result.Read<string>(2),
                permanent ? handler.GetCypherString(CypherStrings.BaninfoInfinite) : Time.secsToTimeString(result.Read<ulong>(3), TimeFormat.ShortText), result.Read<string>(4), result.Read<string>(5));

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
                if (result.Read<bool>(2) && (result.Read<ulong>(1) == 0 || unbanDate >= GameTime.GetGameTime()))
                    active = true;
                bool permanent = (result.Read<ulong>(1) == 0);
                string banTime = permanent ? handler.GetCypherString(CypherStrings.BaninfoInfinite) : Time.secsToTimeString(result.Read<ulong>(1), TimeFormat.ShortText);
                handler.SendSysMessage(CypherStrings.BaninfoHistoryentry,
                    result.Read<string>(0), banTime, active ? handler.GetCypherString(CypherStrings.Yes) : handler.GetCypherString(CypherStrings.No), result.Read<string>(4), result.Read<string>(5));
            }
            while (result.NextRow());

            return true;
        }
    }

    [CommandGroup("banlist")]
    class BanListCommands
    {
        [Command("account", RBACPermissions.CommandBanlistAccount, true)]
        static bool HandleBanListAccountCommand(CommandHandler handler, [OptionalArg] string filter)
        {
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DelExpiredIpBans);
            DB.Login.Execute(stmt);

            SQLResult result;
            if (filter.IsEmpty())
            {
                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BANNED_ALL);
                result = DB.Login.Query(stmt);
            }
            else
            {
                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BANNED_BY_FILTER);
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
        static bool HandleBanListCharacterCommand(CommandHandler handler, string filter)
        {
            if (filter.IsEmpty())
                return false;

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_GUID_BY_NAME_FILTER);
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

                    PreparedStatement stmt2 = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_BANNED_NAME);
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

                    PreparedStatement stmt2 = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_BANINFO_LIST);
                    stmt2.AddValue(0, result.Read<ulong>(0));
                    SQLResult banInfo = DB.Characters.Query(stmt2);
                    if (!banInfo.IsEmpty())
                    {
                        do
                        {
                            long timeBan = banInfo.Read<long>(0);
                            DateTime tmBan = Time.UnixTimeToDateTime(timeBan);
                            string bannedby = banInfo.Read<string>(2).Substring(0, 15);
                            string banreason = banInfo.Read<string>(3).Substring(0, 15);

                            if (banInfo.Read<long>(0) == banInfo.Read<long>(1))
                            {
                                handler.SendSysMessage("|{0}|{1:D2}-{2:D2}-{3:D2} {4:D2}:{5:D2}|   permanent  |{6}|{7}|",
                                    char_name, tmBan.Year % 100, tmBan.Month + 1, tmBan.Day, tmBan.Hour, tmBan.Minute,
                                    bannedby, banreason);
                            }
                            else
                            {
                                long timeUnban = banInfo.Read<long>(1);
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
        static bool HandleBanListIPCommand(CommandHandler handler, [OptionalArg] string filter)
        {
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DelExpiredIpBans);
            DB.Login.Execute(stmt);

            SQLResult result;

            if (filter.IsEmpty())
            {
                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_IP_BANNED_ALL);
                result = DB.Login.Query(stmt);
            }
            else
            {
                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_IP_BANNED_BY_IP);
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
                                long timeUnban = banInfo.Read<uint>(1);
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

    [CommandGroup("unban")]
    class UnBanCommands
    {
        [Command("account", RBACPermissions.CommandUnbanAccount, true)]
        static bool HandleUnBanAccountCommand(CommandHandler handler, string name)
        {
            return HandleUnBanHelper(BanMode.Account, name, handler);
        }

        [Command("character", RBACPermissions.CommandUnbanCharacter, true)]
        static bool HandleUnBanCharacterCommand(CommandHandler handler, string name)
        {
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

            handler.SendSysMessage(CypherStrings.UnbanUnbanned, name);
            return true;
        }

        [Command("playeraccount", RBACPermissions.CommandUnbanPlayeraccount, true)]
        static bool HandleUnBanAccountByCharCommand(CommandHandler handler, string name)
        {
            return HandleUnBanHelper(BanMode.Character, name, handler);
        }

        [Command("ip", RBACPermissions.CommandUnbanIp, true)]
        static bool HandleUnBanIPCommand(CommandHandler handler, string ip)
        {
            return HandleUnBanHelper(BanMode.IP, ip, handler);
        }

        static bool HandleUnBanHelper(BanMode mode, string nameOrIp, CommandHandler handler)
        {
            if (nameOrIp.IsEmpty())
                return false;

            switch (mode)
            {
                case BanMode.Character:
                    if (!ObjectManager.NormalizePlayerName(ref nameOrIp))
                    {
                        handler.SendSysMessage(CypherStrings.PlayerNotFound);
                        return false;
                    }
                    break;
                case BanMode.IP:
                    if (!IPAddress.TryParse(nameOrIp, out _))
                        return false;
                    break;
            }

            if (Global.WorldMgr.RemoveBanAccount(mode, nameOrIp))
                handler.SendSysMessage(CypherStrings.UnbanUnbanned, nameOrIp);
            else
                handler.SendSysMessage(CypherStrings.UnbanError, nameOrIp);

            return true;
        }
    }
}