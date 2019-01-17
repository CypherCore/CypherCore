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

namespace Framework.Database
{
    public class LoginDatabase : MySqlBase<LoginStatements>
    {
        public override void PreparedStatements()
        {
            const string BnetAccountInfo = "ba.id, ba.email, ba.locked, ba.lock_country, ba.last_ip, ba.LoginTicketExpiry, bab.unbandate > UNIX_TIMESTAMP() OR bab.unbandate = bab.bandate, bab.unbandate = bab.bandate";
            const string BnetGameAccountInfo = "a.id, a.username, ab.unbandate, ab.unbandate = ab.bandate, aa.gmlevel";

            PrepareStatement(LoginStatements.SEL_REALMLIST, "SELECT id, name, address, localAddress, localSubnetMask, port, icon, flag, timezone, allowedSecurityLevel, population, gamebuild, Region, Battlegroup FROM realmlist WHERE flag <> 3 ORDER BY name");
            PrepareStatement(LoginStatements.DEL_EXPIRED_IP_BANS, "DELETE FROM ip_banned WHERE unbandate<>bandate AND unbandate<=UNIX_TIMESTAMP()");
            PrepareStatement(LoginStatements.UPD_EXPIRED_ACCOUNT_BANS, "UPDATE account_banned SET active = 0 WHERE active = 1 AND unbandate<>bandate AND unbandate<=UNIX_TIMESTAMP()");
            PrepareStatement(LoginStatements.SEL_IP_INFO, "(SELECT unbandate > UNIX_TIMESTAMP() OR unbandate = bandate AS banned, NULL as country FROM ip_banned WHERE ip = ?) " +
                "UNION (SELECT NULL AS banned, country FROM ip2nation WHERE INET_NTOA(ip) = ?)");

            PrepareStatement(LoginStatements.INS_IP_AUTO_BANNED, "INSERT INTO ip_banned (ip, bandate, unbandate, bannedby, banreason) VALUES (?, UNIX_TIMESTAMP(), UNIX_TIMESTAMP()+?, 'Cypher Auth', 'Failed login autoban')");
            PrepareStatement(LoginStatements.SEL_IP_BANNED_ALL, "SELECT ip, bandate, unbandate, bannedby, banreason FROM ip_banned WHERE (bandate = unbandate OR unbandate > UNIX_TIMESTAMP()) ORDER BY unbandate");
            PrepareStatement(LoginStatements.SEL_IP_BANNED_BY_IP, "SELECT ip, bandate, unbandate, bannedby, banreason FROM ip_banned WHERE (bandate = unbandate OR unbandate > UNIX_TIMESTAMP()) AND ip LIKE CONCAT('%%', ?, '%%') ORDER BY unbandate");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_BANNED_ALL, "SELECT account.id, username FROM account, account_banned WHERE account.id = account_banned.id AND active = 1 GROUP BY account.id");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_BANNED_BY_USERNAME, "SELECT account.id, username FROM account, account_banned WHERE account.id = account_banned.id AND active = 1 AND username LIKE CONCAT('%%', ?, '%%') GROUP BY account.id");
            PrepareStatement(LoginStatements.DEL_ACCOUNT_BANNED, "DELETE FROM account_banned WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_ACCOUNT_INFO_CONTINUED_SESSION, "UPDATE account SET sessionkey = ? WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_INFO_CONTINUED_SESSION, "SELECT username, sessionkey FROM account WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_LOGON_COUNTRY, "SELECT country FROM ip2nation WHERE ip < ? ORDER BY ip DESC LIMIT 0,1");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_ID_BY_NAME, "SELECT id FROM account WHERE username = ?");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_LIST_BY_NAME, "SELECT id, username FROM account WHERE username = ?");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_INFO_BY_NAME, "SELECT a.id, a.sessionkey, ba.last_ip, ba.locked, ba.lock_country, a.expansion, a.mutetime, ba.locale, a.recruiter, a.os, ba.id, aa.gmLevel, " +
                "bab.unbandate > UNIX_TIMESTAMP() OR bab.unbandate = bab.bandate, ab.unbandate > UNIX_TIMESTAMP() OR ab.unbandate = ab.bandate, r.id " +
                "FROM account a LEFT JOIN account r ON a.id = r.recruiter LEFT JOIN battlenet_accounts ba ON a.battlenet_account = ba.id " +
                "LEFT JOIN account_access aa ON a.id = aa.id AND aa.RealmID IN (-1, ?) LEFT JOIN battlenet_account_bans bab ON ba.id = bab.id LEFT JOIN account_banned ab ON a.id = ab.id AND ab.active = 1 " +
                "WHERE a.username = ? ORDER BY aa.RealmID DESC LIMIT 1");

            PrepareStatement(LoginStatements.SEL_ACCOUNT_LIST_BY_EMAIL, "SELECT id, username FROM account WHERE email = ?");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_BY_IP, "SELECT id, username FROM account WHERE last_ip = ?");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_BY_ID, "SELECT 1 FROM account WHERE id = ?");
            PrepareStatement(LoginStatements.INS_IP_BANNED, "INSERT INTO ip_banned (ip, bandate, unbandate, bannedby, banreason) VALUES (?, UNIX_TIMESTAMP(), UNIX_TIMESTAMP()+?, ?, ?)");
            PrepareStatement(LoginStatements.DEL_IP_NOT_BANNED, "DELETE FROM ip_banned WHERE ip = ?");
            PrepareStatement(LoginStatements.INS_ACCOUNT_BANNED, "INSERT INTO account_banned VALUES (?, UNIX_TIMESTAMP(), UNIX_TIMESTAMP()+?, ?, ?, 1)");
            PrepareStatement(LoginStatements.UPD_ACCOUNT_NOT_BANNED, "UPDATE account_banned SET active = 0 WHERE id = ? AND active != 0");
            PrepareStatement(LoginStatements.DEL_REALM_CHARACTERS_BY_REALM, "DELETE FROM realmcharacters WHERE acctid = ? AND realmid = ?");
            PrepareStatement(LoginStatements.DEL_REALM_CHARACTERS, "DELETE FROM realmcharacters WHERE acctid = ?");
            PrepareStatement(LoginStatements.INS_REALM_CHARACTERS, "INSERT INTO realmcharacters (numchars, acctid, realmid) VALUES (?, ?, ?)");
            PrepareStatement(LoginStatements.SEL_SUM_REALM_CHARACTERS, "SELECT SUM(numchars) FROM realmcharacters WHERE acctid = ?");
            PrepareStatement(LoginStatements.INS_ACCOUNT, "INSERT INTO account(username, sha_pass_hash, reg_mail, email, joindate, battlenet_account, battlenet_index) VALUES(?, ?, ?, ?, NOW(), ?, ?)");
            PrepareStatement(LoginStatements.INS_REALM_CHARACTERS_INIT, "INSERT INTO realmcharacters (realmid, acctid, numchars) SELECT realmlist.id, account.id, 0 FROM realmlist, account LEFT JOIN realmcharacters ON acctid=account.id WHERE acctid IS NULL");
            PrepareStatement(LoginStatements.UPD_EXPANSION, "UPDATE account SET expansion = ? WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_ACCOUNT_LOCK, "UPDATE account SET locked = ? WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_ACCOUNT_LOCK_COUNTRY, "UPDATE account SET lock_country = ? WHERE id = ?");
            PrepareStatement(LoginStatements.INS_LOG, "INSERT INTO logs (time, realm, type, level, string) VALUES (?, ?, ?, ?, ?)");
            PrepareStatement(LoginStatements.UPD_USERNAME, "UPDATE account SET username = ?, sha_pass_hash = ? WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_PASSWORD, "UPDATE account SET sha_pass_hash = ? WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_EMAIL, "UPDATE account SET email = ? WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_REG_EMAIL, "UPDATE account SET reg_mail = ? WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_MUTE_TIME, "UPDATE account SET mutetime = ? , mutereason = ? , muteby = ? WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_MUTE_TIME_LOGIN, "UPDATE account SET mutetime = ? WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_LAST_IP, "UPDATE account SET last_ip = ? WHERE username = ?");
            PrepareStatement(LoginStatements.UPD_LAST_ATTEMPT_IP, "UPDATE account SET last_attempt_ip = ? WHERE username = ?");
            PrepareStatement(LoginStatements.UPD_ACCOUNT_ONLINE, "UPDATE account SET online = 1 WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_UPTIME_PLAYERS, "UPDATE uptime SET uptime = ?, maxplayers = ? WHERE realmid = ? AND starttime = ?");
            PrepareStatement(LoginStatements.DEL_OLD_LOGS, "DELETE FROM logs WHERE (time + ?) < ? AND realm = ?");
            PrepareStatement(LoginStatements.DEL_ACCOUNT_ACCESS, "DELETE FROM account_access WHERE id = ?");
            PrepareStatement(LoginStatements.DEL_ACCOUNT_ACCESS_BY_REALM, "DELETE FROM account_access WHERE id = ? AND (RealmID = ? OR RealmID = -1)");
            PrepareStatement(LoginStatements.INS_ACCOUNT_ACCESS, "INSERT INTO account_access (id,gmlevel,RealmID) VALUES (?, ?, ?)");
            PrepareStatement(LoginStatements.GET_ACCOUNT_ID_BY_USERNAME, "SELECT id FROM account WHERE username = ?");
            PrepareStatement(LoginStatements.GET_ACCOUNT_ACCESS_GMLEVEL, "SELECT gmlevel FROM account_access WHERE id = ?");
            PrepareStatement(LoginStatements.GET_GMLEVEL_BY_REALMID, "SELECT gmlevel FROM account_access WHERE id = ? AND (RealmID = ? OR RealmID = -1)");
            PrepareStatement(LoginStatements.GET_USERNAME_BY_ID, "SELECT username FROM account WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_CHECK_PASSWORD, "SELECT 1 FROM account WHERE id = ? AND sha_pass_hash = ?");
            PrepareStatement(LoginStatements.SEL_CHECK_PASSWORD_BY_NAME, "SELECT 1 FROM account WHERE username = ? AND sha_pass_hash = ?");
            PrepareStatement(LoginStatements.SEL_PINFO, "SELECT a.username, aa.gmlevel, a.email, a.reg_mail, a.last_ip, DATE_FORMAT(a.last_login, '%Y-%m-%d %T'), a.mutetime, a.mutereason, a.muteby, a.failed_logins, a.locked, a.OS FROM account a LEFT JOIN account_access aa ON (a.id = aa.id AND (aa.RealmID = ? OR aa.RealmID = -1)) WHERE a.id = ?");
            PrepareStatement(LoginStatements.SEL_PINFO_BANS, "SELECT unbandate, bandate = unbandate, bannedby, banreason FROM account_banned WHERE id = ? AND active ORDER BY bandate ASC LIMIT 1");
            PrepareStatement(LoginStatements.SEL_GM_ACCOUNTS, "SELECT a.username, aa.gmlevel FROM account a, account_access aa WHERE a.id=aa.id AND aa.gmlevel >= ? AND (aa.realmid = -1 OR aa.realmid = ?)");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_INFO, "SELECT a.username, a.last_ip, aa.gmlevel, a.expansion FROM account a LEFT JOIN account_access aa ON (a.id = aa.id) WHERE a.id = ?");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_ACCESS_GMLEVEL_TEST, "SELECT 1 FROM account_access WHERE id = ? AND gmlevel > ?");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_ACCESS, "SELECT a.id, aa.gmlevel, aa.RealmID FROM account a LEFT JOIN account_access aa ON (a.id = aa.id) WHERE a.username = ?");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_WHOIS, "SELECT username, email, last_ip FROM account WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_LAST_ATTEMPT_IP, "SELECT last_attempt_ip FROM account WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_LAST_IP, "SELECT last_ip FROM account WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_REALMLIST_SECURITY_LEVEL, "SELECT allowedSecurityLevel from realmlist WHERE id = ?");
            PrepareStatement(LoginStatements.DEL_ACCOUNT, "DELETE FROM account WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_IP2NATION_COUNTRY, "SELECT c.country FROM ip2nationCountries c, ip2nation i WHERE i.ip < ? AND c.code = i.country ORDER BY i.ip DESC LIMIT 0,1");
            PrepareStatement(LoginStatements.SEL_AUTOBROADCAST, "SELECT id, weight, text FROM autobroadcast WHERE realmid = ? OR realmid = -1");
            PrepareStatement(LoginStatements.GET_EMAIL_BY_ID, "SELECT email FROM account WHERE id = ?");
            // 0: uint32, 1: uint32, 2: uint8, 3: uint32, 4: string // Complete name: "Login_Insert_AccountLoginDeLete_IP_Logging"
            PrepareStatement(LoginStatements.INS_ALDL_IP_LOGGING, "INSERT INTO logs_ip_actions (account_id,character_guid,type,ip,systemnote,unixtime,time) VALUES (?, ?, ?, (SELECT last_ip FROM account WHERE id = ?), ?, unix_timestamp(NOW()), NOW())");
            // 0: uint32, 1: uint32, 2: uint8, 3: uint32, 4: string // Complete name: "Login_Insert_FailedAccountLogin_IP_Logging"
            PrepareStatement(LoginStatements.INS_FACL_IP_LOGGING, "INSERT INTO logs_ip_actions (account_id,character_guid,type,ip,systemnote,unixtime,time) VALUES (?, ?, ?, (SELECT last_attempt_ip FROM account WHERE id = ?), ?, unix_timestamp(NOW()), NOW())");
            // 0: uint32, 1: uint32, 2: uint8, 3: string, 4: string // Complete name: "Login_Insert_CharacterDelete_IP_Logging"
            PrepareStatement(LoginStatements.INS_CHAR_IP_LOGGING, "INSERT INTO logs_ip_actions (account_id,character_guid,type,ip,systemnote,unixtime,time) VALUES (?, ?, ?, ?, ?, unix_timestamp(NOW()), NOW())");
            // 0: string, 1: string, 2: string                      // Complete name: "Login_Insert_Failed_Account_Login_due_password_IP_Logging"
            PrepareStatement(LoginStatements.INS_FALP_IP_LOGGING, "INSERT INTO logs_ip_actions (account_id,character_guid,type,ip,systemnote,unixtime,time) VALUES (?, 0, 1, ?, ?, unix_timestamp(NOW()), NOW())");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_ACCESS_BY_ID, "SELECT gmlevel, RealmID FROM account_access WHERE id = ? and (RealmID = ? OR RealmID = -1) ORDER BY gmlevel desc");

            PrepareStatement(LoginStatements.SEL_RBAC_ACCOUNT_PERMISSIONS, "SELECT permissionId, granted FROM rbac_account_permissions WHERE accountId = ? AND (realmId = ? OR realmId = -1) ORDER BY permissionId, realmId");
            PrepareStatement(LoginStatements.INS_RBAC_ACCOUNT_PERMISSION, "INSERT INTO rbac_account_permissions (accountId, permissionId, granted, realmId) VALUES (?, ?, ?, ?) ON DUPLICATE KEY UPDATE granted = VALUES(granted)");
            PrepareStatement(LoginStatements.DEL_RBAC_ACCOUNT_PERMISSION, "DELETE FROM rbac_account_permissions WHERE accountId = ? AND permissionId = ? AND (realmId = ? OR realmId = -1)");

            PrepareStatement(LoginStatements.INS_ACCOUNT_MUTE, "INSERT INTO account_muted VALUES (?, UNIX_TIMESTAMP(), ?, ?, ?)");
            PrepareStatement(LoginStatements.SEL_ACCOUNT_MUTE_INFO, "SELECT mutedate, mutetime, mutereason, mutedby FROM account_muted WHERE guid = ? ORDER BY mutedate ASC");
            PrepareStatement(LoginStatements.DEL_ACCOUNT_MUTED, "DELETE FROM account_muted WHERE guid = ?");

            PrepareStatement(LoginStatements.SEL_BNET_AUTHENTICATION, "SELECT ba.id, ba.sha_pass_hash, ba.failed_logins, ba.LoginTicket, ba.LoginTicketExpiry, bab.unbandate > UNIX_TIMESTAMP() OR bab.unbandate = bab.bandate FROM battlenet_accounts ba LEFT JOIN battlenet_account_bans bab ON ba.id = bab.id WHERE email = ?");
            PrepareStatement(LoginStatements.UPD_BNET_AUTHENTICATION, "UPDATE battlenet_accounts SET LoginTicket = ?, LoginTicketExpiry = ? WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_BNET_ACCOUNT_INFO, "SELECT " + BnetAccountInfo + ", " + BnetGameAccountInfo +
                " FROM battlenet_accounts ba LEFT JOIN battlenet_account_bans bab ON ba.id = bab.id LEFT JOIN account a ON ba.id = a.battlenet_account" +
                " LEFT JOIN account_banned ab ON a.id = ab.id AND ab.active = 1 LEFT JOIN account_access aa ON a.id = aa.id AND aa.RealmID = -1 WHERE ba.LoginTicket = ? ORDER BY a.id");
            PrepareStatement(LoginStatements.UPD_BNET_LAST_LOGIN_INFO, "UPDATE battlenet_accounts SET last_ip = ?, last_login = NOW(), locale = ?, failed_logins = 0, os = ? WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_BNET_GAME_ACCOUNT_LOGIN_INFO, "UPDATE account SET sessionkey = ?, last_ip = ?, last_login = NOW(), locale = ?, failed_logins = 0, os = ? WHERE username = ?");
            PrepareStatement(LoginStatements.SEL_BNET_CHARACTER_COUNTS_BY_ACCOUNT_ID, "SELECT rc.acctid, rc.numchars, r.id, r.Region, r.Battlegroup FROM realmcharacters rc INNER JOIN realmlist r ON rc.realmid = r.id WHERE rc.acctid = ?");
            PrepareStatement(LoginStatements.SEL_BNET_CHARACTER_COUNTS_BY_BNET_ID, "SELECT rc.acctid, rc.numchars, r.id, r.Region, r.Battlegroup FROM realmcharacters rc INNER JOIN realmlist r ON rc.realmid = r.id LEFT JOIN account a ON rc.acctid = a.id WHERE a.battlenet_account = ?");
            PrepareStatement(LoginStatements.SEL_BNET_LAST_PLAYER_CHARACTERS, "SELECT lpc.accountId, lpc.region, lpc.battlegroup, lpc.realmId, lpc.characterName, lpc.characterGUID, lpc.lastPlayedTime FROM account_last_played_character lpc LEFT JOIN account a ON lpc.accountId = a.id WHERE a.battlenet_account = ?");
            PrepareStatement(LoginStatements.DEL_BNET_LAST_PLAYER_CHARACTERS, "DELETE FROM account_last_played_character WHERE accountId = ? AND region = ? AND battlegroup = ?");
            PrepareStatement(LoginStatements.INS_BNET_LAST_PLAYER_CHARACTERS, "INSERT INTO account_last_played_character (accountId, region, battlegroup, realmId, characterName, characterGUID, lastPlayedTime) VALUES (?,?,?,?,?,?,?)");
            PrepareStatement(LoginStatements.INS_BNET_ACCOUNT, "INSERT INTO battlenet_accounts (`email`,`sha_pass_hash`) VALUES (?, ?)");
            PrepareStatement(LoginStatements.SEL_BNET_ACCOUNT_EMAIL_BY_ID, "SELECT email FROM battlenet_accounts WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_BNET_ACCOUNT_ID_BY_EMAIL, "SELECT id FROM battlenet_accounts WHERE email = ?");
            PrepareStatement(LoginStatements.UPD_BNET_PASSWORD, "UPDATE battlenet_accounts SET sha_pass_hash = ? WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_BNET_CHECK_PASSWORD, "SELECT 1 FROM battlenet_accounts WHERE id = ? AND sha_pass_hash = ?");
            PrepareStatement(LoginStatements.UPD_BNET_ACCOUNT_LOCK, "UPDATE battlenet_accounts SET locked = ? WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_BNET_ACCOUNT_LOCK_CONTRY, "UPDATE battlenet_accounts SET lock_country = ? WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_BNET_ACCOUNT_ID_BY_GAME_ACCOUNT, "SELECT battlenet_account FROM account WHERE id = ?");
            PrepareStatement(LoginStatements.UPD_BNET_GAME_ACCOUNT_LINK, "UPDATE account SET battlenet_account = ?, battlenet_index = ? WHERE id = ?");
            PrepareStatement(LoginStatements.SEL_BNET_MAX_ACCOUNT_INDEX, "SELECT MAX(battlenet_index) FROM account WHERE battlenet_account = ?");
            PrepareStatement(LoginStatements.SEL_BNET_GAME_ACCOUNT_LIST, "SELECT a.id, a.username FROM account a LEFT JOIN battlenet_accounts ba ON a.battlenet_account = ba.id WHERE ba.email = ?");

            PrepareStatement(LoginStatements.UPD_BNET_FAILED_LOGINS, "UPDATE battlenet_accounts SET failed_logins = failed_logins + 1 WHERE id = ?");
            PrepareStatement(LoginStatements.INS_BNET_ACCOUNT_AUTO_BANNED, "INSERT INTO battlenet_account_bans(id, bandate, unbandate, bannedby, banreason) VALUES(?, UNIX_TIMESTAMP(), UNIX_TIMESTAMP()+?, 'Trinity Auth', 'Failed login autoban')");
            PrepareStatement(LoginStatements.DEL_BNET_EXPIRED_ACCOUNT_BANNED, "DELETE FROM battlenet_account_bans WHERE unbandate<>bandate AND unbandate<=UNIX_TIMESTAMP()");          
            PrepareStatement(LoginStatements.UPD_BNET_RESET_FAILED_LOGINS, "UPDATE battlenet_accounts SET failed_logins = 0 WHERE id = ?");

            PrepareStatement(LoginStatements.SEL_LAST_CHAR_UNDELETE, "SELECT LastCharacterUndelete FROM battlenet_accounts WHERE Id = ?");
            PrepareStatement(LoginStatements.UPD_LAST_CHAR_UNDELETE, "UPDATE battlenet_accounts SET LastCharacterUndelete = UNIX_TIMESTAMP() WHERE Id = ?");

            // Account wide toys
            PrepareStatement(LoginStatements.SEL_ACCOUNT_TOYS, "SELECT itemId, isFavourite FROM battlenet_account_toys WHERE accountId = ?");
            PrepareStatement(LoginStatements.REP_ACCOUNT_TOYS, "REPLACE INTO battlenet_account_toys (accountId, itemId, isFavourite) VALUES (?, ?, ?)");

            // Battle Pets
            PrepareStatement(LoginStatements.SEL_BATTLE_PETS, "SELECT guid, species, breed, level, exp, health, quality, flags, name FROM battle_pets WHERE battlenetAccountId = ?");
            PrepareStatement(LoginStatements.INS_BATTLE_PETS, "INSERT INTO battle_pets (guid, battlenetAccountId, species, breed, level, exp, health, quality, flags, name) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)");
            PrepareStatement(LoginStatements.DEL_BATTLE_PETS, "DELETE FROM battle_pets WHERE battlenetAccountId = ? AND guid = ?");
            PrepareStatement(LoginStatements.UPD_BATTLE_PETS, "UPDATE battle_pets SET level = ?, exp = ?, health = ?, quality = ?, flags = ?, name = ? WHERE battlenetAccountId = ? AND guid = ?");
            PrepareStatement(LoginStatements.SEL_BATTLE_PET_SLOTS, "SELECT id, battlePetGuid, locked FROM battle_pet_slots WHERE battlenetAccountId = ?");
            PrepareStatement(LoginStatements.INS_BATTLE_PET_SLOTS, "INSERT INTO battle_pet_slots (id, battlenetAccountId, battlePetGuid, locked) VALUES (?, ?, ?, ?)");
            PrepareStatement(LoginStatements.DEL_BATTLE_PET_SLOTS, "DELETE FROM battle_pet_slots WHERE battlenetAccountId = ?");

            PrepareStatement(LoginStatements.SEL_ACCOUNT_HEIRLOOMS, "SELECT itemId, flags FROM battlenet_account_heirlooms WHERE accountId = ?");
            PrepareStatement(LoginStatements.REP_ACCOUNT_HEIRLOOMS, "REPLACE INTO battlenet_account_heirlooms (accountId, itemId, flags) VALUES (?, ?, ?)");

            // Account wide mounts
            PrepareStatement(LoginStatements.SEL_ACCOUNT_MOUNTS, "SELECT mountSpellId, flags FROM battlenet_account_mounts WHERE battlenetAccountId = ?");
            PrepareStatement(LoginStatements.REP_ACCOUNT_MOUNTS, "REPLACE INTO battlenet_account_mounts (battlenetAccountId, mountSpellId, flags) VALUES (?, ?, ?)");

            // Transmog collection
            PrepareStatement(LoginStatements.SEL_BNET_ITEM_APPEARANCES, "SELECT blobIndex, appearanceMask FROM battlenet_item_appearances WHERE battlenetAccountId = ? ORDER BY blobIndex DESC");
            PrepareStatement(LoginStatements.INS_BNET_ITEM_APPEARANCES, "INSERT INTO battlenet_item_appearances (battlenetAccountId, blobIndex, appearanceMask) VALUES (?, ?, ?) " +
                "ON DUPLICATE KEY UPDATE appearanceMask = appearanceMask | VALUES(appearanceMask)");
            PrepareStatement(LoginStatements.SEL_BNET_ITEM_FAVORITE_APPEARANCES, "SELECT itemModifiedAppearanceId FROM battlenet_item_favorite_appearances WHERE battlenetAccountId = ?");
            PrepareStatement(LoginStatements.INS_BNET_ITEM_FAVORITE_APPEARANCE, "INSERT INTO battlenet_item_favorite_appearances (battlenetAccountId, itemModifiedAppearanceId) VALUES (?, ?)");
            PrepareStatement(LoginStatements.DEL_BNET_ITEM_FAVORITE_APPEARANCE, "DELETE FROM battlenet_item_favorite_appearances WHERE battlenetAccountId = ? AND itemModifiedAppearanceId = ?");
        }
    }

    public enum LoginStatements
    {
        SEL_REALMLIST,
        DEL_EXPIRED_IP_BANS,
        UPD_EXPIRED_ACCOUNT_BANS,
        SEL_IP_INFO,
        INS_IP_AUTO_BANNED,
        SEL_ACCOUNT_BANNED_ALL,
        SEL_ACCOUNT_BANNED_BY_USERNAME,
        DEL_ACCOUNT_BANNED,
        UPD_ACCOUNT_INFO_CONTINUED_SESSION,
        SEL_ACCOUNT_INFO_CONTINUED_SESSION,
        UPD_VS,
        SEL_LOGON_COUNTRY,
        SEL_ACCOUNT_ID_BY_NAME,
        SEL_ACCOUNT_LIST_BY_NAME,
        SEL_ACCOUNT_INFO_BY_NAME,
        SEL_ACCOUNT_LIST_BY_EMAIL,
        SEL_ACCOUNT_BY_IP,
        INS_IP_BANNED,
        DEL_IP_NOT_BANNED,
        SEL_IP_BANNED_ALL,
        SEL_IP_BANNED_BY_IP,
        SEL_ACCOUNT_BY_ID,
        INS_ACCOUNT_BANNED,
        UPD_ACCOUNT_NOT_BANNED,
        DEL_REALM_CHARACTERS_BY_REALM,
        DEL_REALM_CHARACTERS,
        INS_REALM_CHARACTERS,
        SEL_SUM_REALM_CHARACTERS,
        INS_ACCOUNT,
        INS_REALM_CHARACTERS_INIT,
        UPD_EXPANSION,
        UPD_ACCOUNT_LOCK,
        UPD_ACCOUNT_LOCK_COUNTRY,
        INS_LOG,
        UPD_USERNAME,
        UPD_PASSWORD,
        UPD_EMAIL,
        UPD_REG_EMAIL,
        UPD_MUTE_TIME,
        UPD_MUTE_TIME_LOGIN,
        UPD_LAST_IP,
        UPD_LAST_ATTEMPT_IP,
        UPD_ACCOUNT_ONLINE,
        UPD_UPTIME_PLAYERS,
        DEL_OLD_LOGS,
        DEL_ACCOUNT_ACCESS,
        DEL_ACCOUNT_ACCESS_BY_REALM,
        INS_ACCOUNT_ACCESS,
        GET_ACCOUNT_ID_BY_USERNAME,
        GET_ACCOUNT_ACCESS_GMLEVEL,
        GET_GMLEVEL_BY_REALMID,
        GET_USERNAME_BY_ID,
        SEL_CHECK_PASSWORD,
        SEL_CHECK_PASSWORD_BY_NAME,
        SEL_PINFO,
        SEL_PINFO_BANS,
        SEL_GM_ACCOUNTS,
        SEL_ACCOUNT_INFO,
        SEL_ACCOUNT_ACCESS_GMLEVEL_TEST,
        SEL_ACCOUNT_ACCESS,
        SEL_ACCOUNT_RECRUITER,
        SEL_BANS,
        SEL_ACCOUNT_WHOIS,
        SEL_REALMLIST_SECURITY_LEVEL,
        DEL_ACCOUNT,
        SEL_IP2NATION_COUNTRY,
        SEL_AUTOBROADCAST,
        SEL_LAST_ATTEMPT_IP,
        SEL_LAST_IP,
        GET_EMAIL_BY_ID,
        INS_ALDL_IP_LOGGING,
        INS_FACL_IP_LOGGING,
        INS_CHAR_IP_LOGGING,
        INS_FALP_IP_LOGGING,

        SEL_ACCOUNT_ACCESS_BY_ID,
        SEL_RBAC_ACCOUNT_PERMISSIONS,
        INS_RBAC_ACCOUNT_PERMISSION,
        DEL_RBAC_ACCOUNT_PERMISSION,

        INS_ACCOUNT_MUTE,
        SEL_ACCOUNT_MUTE_INFO,
        DEL_ACCOUNT_MUTED,

        SEL_BNET_AUTHENTICATION,
        UPD_BNET_AUTHENTICATION,
        SEL_BNET_ACCOUNT_INFO,
        UPD_BNET_LAST_LOGIN_INFO,
        UPD_BNET_GAME_ACCOUNT_LOGIN_INFO,
        SEL_BNET_CHARACTER_COUNTS_BY_ACCOUNT_ID,
        SEL_BNET_CHARACTER_COUNTS_BY_BNET_ID,
        SEL_BNET_LAST_PLAYER_CHARACTERS,
        DEL_BNET_LAST_PLAYER_CHARACTERS,
        INS_BNET_LAST_PLAYER_CHARACTERS,
        INS_BNET_ACCOUNT,
        SEL_BNET_ACCOUNT_EMAIL_BY_ID,
        SEL_BNET_ACCOUNT_ID_BY_EMAIL,
        UPD_BNET_PASSWORD,
        SEL_BNET_ACCOUNT_SALT_BY_ID,
        SEL_BNET_CHECK_PASSWORD,
        UPD_BNET_ACCOUNT_LOCK,
        UPD_BNET_ACCOUNT_LOCK_CONTRY,
        SEL_BNET_ACCOUNT_ID_BY_GAME_ACCOUNT,
        UPD_BNET_GAME_ACCOUNT_LINK,
        SEL_BNET_MAX_ACCOUNT_INDEX,
        SEL_BNET_GAME_ACCOUNT_LIST,

        UPD_BNET_FAILED_LOGINS,
        INS_BNET_ACCOUNT_AUTO_BANNED,
        DEL_BNET_EXPIRED_ACCOUNT_BANNED,
        UPD_BNET_RESET_FAILED_LOGINS,

        SEL_LAST_CHAR_UNDELETE,
        UPD_LAST_CHAR_UNDELETE,

        SEL_ACCOUNT_TOYS,
        REP_ACCOUNT_TOYS,

        SEL_BATTLE_PETS,
        INS_BATTLE_PETS,
        DEL_BATTLE_PETS,
        UPD_BATTLE_PETS,
        SEL_BATTLE_PET_SLOTS,
        INS_BATTLE_PET_SLOTS,
        DEL_BATTLE_PET_SLOTS,

        SEL_ACCOUNT_HEIRLOOMS,
        REP_ACCOUNT_HEIRLOOMS,

        SEL_ACCOUNT_MOUNTS,
        REP_ACCOUNT_MOUNTS,

        SEL_BNET_ITEM_APPEARANCES,
        INS_BNET_ITEM_APPEARANCES,
        SEL_BNET_ITEM_FAVORITE_APPEARANCES,
        INS_BNET_ITEM_FAVORITE_APPEARANCE,
        DEL_BNET_ITEM_FAVORITE_APPEARANCE,

        MAX_LOGINDATABASE_STATEMENTS
    }
}
