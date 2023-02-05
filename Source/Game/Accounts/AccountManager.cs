// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Cryptography;
using Framework.Database;
using Game.Accounts;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Game
{
    public sealed class AccountManager : Singleton<AccountManager>
    {
        const int MaxAccountLength = 16;
        const int MaxEmailLength = 64;

        readonly Dictionary<uint, RBACPermission> _permissions = new();
        readonly MultiMap<byte, uint> _defaultPermissions = new();

        AccountManager() { }

        public AccountOpResult CreateAccount(string username, string password, string email = "", uint bnetAccountId = 0, byte bnetIndex = 0)
        {
            if (username.Length > MaxAccountLength)
                return AccountOpResult.NameTooLong;

            if (password.Length > MaxAccountLength)
                return AccountOpResult.PassTooLong;

            if (GetId(username) != 0)
                return AccountOpResult.NameAlreadyExist;

            (byte[] salt, byte[] verifier) = SRP6.MakeRegistrationData(username, password);

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_ACCOUNT);
            stmt.AddValue(0, username);
            stmt.AddValue(1, salt);
            stmt.AddValue(2, verifier);
            stmt.AddValue(3, email);
            stmt.AddValue(4, email);
            if (bnetAccountId != 0 && bnetIndex != 0)
            {
                stmt.AddValue(5, bnetAccountId);
                stmt.AddValue(6, bnetIndex);
            }
            else
            {
                stmt.AddNull(5);
                stmt.AddNull(6);
            }
            DB.Login.DirectExecute(stmt); // Enforce saving, otherwise AddGroup can fail

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_REALM_CHARACTERS_INIT);
            DB.Login.Execute(stmt);

            return AccountOpResult.Ok;
        }

        public AccountOpResult DeleteAccount(uint accountId)
        {
            // Check if accounts exists
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BY_ID);
            stmt.AddValue(0, accountId);
            SQLResult result = DB.Login.Query(stmt);
            if (result.IsEmpty())
                return AccountOpResult.NameNotExist;

            // Obtain accounts characters
            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARS_BY_ACCOUNT_ID);
            stmt.AddValue(0, accountId);
            result = DB.Characters.Query(stmt);
            if (!result.IsEmpty())
            {
                do
                {
                    ObjectGuid guid = ObjectGuid.Create(HighGuid.Player, result.Read<ulong>(0));

                    // Kick if player is online
                    Player p = Global.ObjAccessor.FindPlayer(guid);
                    if (p)
                    {
                        WorldSession s = p.GetSession();
                        s.KickPlayer("AccountMgr::DeleteAccount Deleting the account");                            // mark session to remove at next session list update
                        s.LogoutPlayer(false);                     // logout player without waiting next session list update
                    }

                    Player.DeleteFromDB(guid, accountId, false);       // no need to update realm characters
                } while (result.NextRow());
            }

            // table realm specific but common for all characters of account for realm
            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_TUTORIALS);
            stmt.AddValue(0, accountId);
            DB.Characters.Execute(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ACCOUNT_DATA);
            stmt.AddValue(0, accountId);
            DB.Characters.Execute(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHARACTER_BAN);
            stmt.AddValue(0, accountId);
            DB.Characters.Execute(stmt);

            SQLTransaction trans = new();

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT);
            stmt.AddValue(0, accountId);
            trans.Append(stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT_ACCESS);
            stmt.AddValue(0, accountId);
            trans.Append(stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_REALM_CHARACTERS);
            stmt.AddValue(0, accountId);
            trans.Append(stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT_BANNED);
            stmt.AddValue(0, accountId);
            trans.Append(stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT_MUTED);
            stmt.AddValue(0, accountId);
            trans.Append(stmt);

            DB.Login.CommitTransaction(trans);

            return AccountOpResult.Ok;
        }

        public AccountOpResult ChangeUsername(uint accountId, string newUsername, string newPassword)
        {
            // Check if accounts exists
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BY_ID);
            stmt.AddValue(0, accountId);
            SQLResult result = DB.Login.Query(stmt);
            if (result.IsEmpty())
                return AccountOpResult.NameNotExist;

            if (newUsername.Length > MaxAccountLength)
                return AccountOpResult.NameTooLong;

            if (newPassword.Length > MaxAccountLength)
                return AccountOpResult.PassTooLong;

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_USERNAME);
            stmt.AddValue(0, newUsername);
            stmt.AddValue(1, accountId);
            DB.Login.Execute(stmt);

            (byte[] salt, byte[] verifier) = SRP6.MakeRegistrationData(newUsername, newPassword);
            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_LOGON);
            stmt.AddValue(0, salt);
            stmt.AddValue(1, verifier);
            stmt.AddValue(2, accountId);
            DB.Login.Execute(stmt);

            return AccountOpResult.Ok;
        }

        public AccountOpResult ChangePassword(uint accountId, string newPassword)
        {
            string username;

            if (!GetName(accountId, out username))
                return AccountOpResult.NameNotExist;                          // account doesn't exist

            if (newPassword.Length > MaxAccountLength)
                return AccountOpResult.PassTooLong;

            (byte[] salt, byte[] verifier) = SRP6.MakeRegistrationData(username, newPassword);

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_LOGON);
            stmt.AddValue(0, salt);
            stmt.AddValue(1, verifier);
            stmt.AddValue(2, accountId);
            DB.Login.Execute(stmt);

            return AccountOpResult.Ok;
        }

        public AccountOpResult ChangeEmail(uint accountId, string newEmail)
        {
            if (!GetName(accountId, out _))
                return AccountOpResult.NameNotExist;                          // account doesn't exist

            if (newEmail.Length > MaxEmailLength)
                return AccountOpResult.EmailTooLong;

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_EMAIL);
            stmt.AddValue(0, newEmail);
            stmt.AddValue(1, accountId);
            DB.Login.Execute(stmt);

            return AccountOpResult.Ok;
        }

        public AccountOpResult ChangeRegEmail(uint accountId, string newEmail)
        {
            if (!GetName(accountId, out _))
                return AccountOpResult.NameNotExist;                          // account doesn't exist

            if (newEmail.Length > MaxEmailLength)
                return AccountOpResult.EmailTooLong;

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_REG_EMAIL);
            stmt.AddValue(0, newEmail);
            stmt.AddValue(1, accountId);
            DB.Login.Execute(stmt);

            return AccountOpResult.Ok;
        }

        public uint GetId(string username)
        {
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.GET_ACCOUNT_ID_BY_USERNAME);
            stmt.AddValue(0, username);
            SQLResult result = DB.Login.Query(stmt);
            return !result.IsEmpty() ? result.Read<uint>(0) : 0;
        }

        public AccountTypes GetSecurity(uint accountId, int realmId)
        {
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.GET_GMLEVEL_BY_REALMID);
            stmt.AddValue(0, accountId);
            stmt.AddValue(1, realmId);
            SQLResult result = DB.Login.Query(stmt);
            return !result.IsEmpty() ? (AccountTypes)result.Read<uint>(0) : AccountTypes.Player;
        }

        public QueryCallback GetSecurityAsync(uint accountId, int realmId, Action<uint> callback)
        {
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.GET_GMLEVEL_BY_REALMID);
            stmt.AddValue(0, accountId);
            stmt.AddValue(1, realmId);
            return DB.Login.AsyncQuery(stmt).WithCallback(result =>
            {
                callback(!result.IsEmpty() ? result.Read<byte>(0) : (uint)AccountTypes.Player);
            });
        }

        public bool GetName(uint accountId, out string name)
        {
            name = "";
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.GET_USERNAME_BY_ID);
            stmt.AddValue(0, accountId);
            SQLResult result = DB.Login.Query(stmt);
            if (!result.IsEmpty())
            {
                name = result.Read<string>(0);
                return true;
            }

            return false;
        }

        public bool GetEmail(uint accountId, out string email)
        {
            email = "";
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.GET_EMAIL_BY_ID);
            stmt.AddValue(0, accountId);
            SQLResult result = DB.Login.Query(stmt);
            if (!result.IsEmpty())
            {
                email = result.Read<string>(0);
                return true;
            }

            return false;
        }

        public bool CheckPassword(uint accountId, string password)
        {
            string username;

            if (!GetName(accountId, out username))
                return false;

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_CHECK_PASSWORD);
            stmt.AddValue(0, accountId);
            SQLResult result = DB.Login.Query(stmt);
            if (!result.IsEmpty())
            {
                byte[] salt = result.Read<byte[]>(0);
                byte[] verifier = result.Read<byte[]>(1);
                if (SRP6.CheckLogin(username, password, salt, verifier))
                    return true;
            }

            return false;
        }

        public bool CheckEmail(uint accountId, string newEmail)
        {
            string oldEmail;

            // We simply return false for a non-existing email
            if (!GetEmail(accountId, out oldEmail))
                return false;

            if (oldEmail == newEmail)
                return true;

            return false;
        }

        public uint GetCharactersCount(uint accountId)
        {
            // check character count
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_SUM_CHARS);
            stmt.AddValue(0, accountId);
            SQLResult result = DB.Characters.Query(stmt);
            return result.IsEmpty() ? 0 : (uint)result.Read<ulong>(0);
        }

        public bool IsBannedAccount(string name)
        {
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_BANNED_BY_USERNAME);
            stmt.AddValue(0, name);
            SQLResult result = DB.Login.Query(stmt);
            return !result.IsEmpty();
        }

        public bool IsPlayerAccount(AccountTypes gmlevel)
        {
            return gmlevel == AccountTypes.Player;
        }

        public bool IsAdminAccount(AccountTypes gmlevel)
        {
            return gmlevel >= AccountTypes.Administrator && gmlevel <= AccountTypes.Console;
        }

        public bool IsConsoleAccount(AccountTypes gmlevel)
        {
            return gmlevel == AccountTypes.Console;
        }

        public void LoadRBAC()
        {
            _permissions.Clear();
            _defaultPermissions.Clear();

            Log.outDebug(LogFilter.Rbac, "AccountMgr:LoadRBAC");
            uint oldMSTime = Time.GetMSTime();
            uint count1 = 0;
            uint count2 = 0;
            uint count3 = 0;

            Log.outDebug(LogFilter.Rbac, "AccountMgr:LoadRBAC: Loading permissions");
            SQLResult result = DB.Login.Query("SELECT id, name FROM rbac_permissions");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 account permission definitions. DB table `rbac_permissions` is empty.");
                return;
            }

            do
            {
                uint id = result.Read<uint>(0);
                _permissions[id] = new RBACPermission(id, result.Read<string>(1));
                ++count1;
            }
            while (result.NextRow());

            Log.outDebug(LogFilter.Rbac, "AccountMgr:LoadRBAC: Loading linked permissions");
            result = DB.Login.Query("SELECT id, linkedId FROM rbac_linked_permissions ORDER BY id ASC");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 linked permissions. DB table `rbac_linked_permissions` is empty.");
                return;
            }

            uint permissionId = 0;
            RBACPermission permission = null;

            do
            {
                uint newId = result.Read<uint>(0);
                if (permissionId != newId)
                {
                    permissionId = newId;
                    permission = _permissions[newId];
                }

                uint linkedPermissionId = result.Read<uint>(1);
                if (linkedPermissionId == permissionId)
                {
                    Log.outError(LogFilter.Sql, "RBAC Permission {0} has itself as linked permission. Ignored", permissionId);
                    continue;
                }
                permission.AddLinkedPermission(linkedPermissionId);
                ++count2;
            }
            while (result.NextRow());

            Log.outDebug(LogFilter.Rbac, "AccountMgr:LoadRBAC: Loading default permissions");
            result = DB.Login.Query("SELECT secId, permissionId FROM rbac_default_permissions ORDER BY secId ASC");
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 default permission definitions. DB table `rbac_default_permissions` is empty.");
                return;
            }

            uint secId = 255;
            do
            {
                uint newId = result.Read<uint>(0);
                if (secId != newId)
                    secId = newId;

                _defaultPermissions.Add((byte)secId, result.Read<uint>(1));
                ++count3;
            }
            while (result.NextRow());

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} permission definitions, {1} linked permissions and {2} default permissions in {3} ms", count1, count2, count3, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public void UpdateAccountAccess(RBACData rbac, uint accountId, byte securityLevel, int realmId)
        {
            if (rbac != null && securityLevel != rbac.GetSecurityLevel())
                rbac.SetSecurityLevel(securityLevel);

            PreparedStatement stmt;
            SQLTransaction trans = new();
            // Delete old security level from DB
            if (realmId == -1)
            {
                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT_ACCESS);
                stmt.AddValue(0, accountId);
                trans.Append(stmt);
            }
            else
            {
                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.DEL_ACCOUNT_ACCESS_BY_REALM);
                stmt.AddValue(0, accountId);
                stmt.AddValue(1, realmId);
                trans.Append(stmt);
            }

            // Add new security level
            if (securityLevel != 0)
            {
                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_ACCOUNT_ACCESS);
                stmt.AddValue(0, accountId);
                stmt.AddValue(1, securityLevel);
                stmt.AddValue(2, realmId);
                trans.Append(stmt);
            }

            DB.Login.CommitTransaction(trans);
        }

        public RBACPermission GetRBACPermission(uint permissionId)
        {
            Log.outDebug(LogFilter.Rbac, "AccountMgr:GetRBACPermission: {0}", permissionId);
            return _permissions.LookupByKey(permissionId);
        }

        public bool HasPermission(uint accountId, RBACPermissions permissionId, uint realmId)
        {
            if (accountId == 0)
            {
                Log.outError(LogFilter.Rbac, "AccountMgr:HasPermission: Wrong accountId 0");
                return false;
            }

            RBACData rbac = new(accountId, "", (int)realmId, (byte)GetSecurity(accountId, (int)realmId));
            rbac.LoadFromDB();
            bool hasPermission = rbac.HasPermission(permissionId);

            Log.outDebug(LogFilter.Rbac, "AccountMgr:HasPermission [AccountId: {0}, PermissionId: {1}, realmId: {2}]: {3}",
                           accountId, permissionId, realmId, hasPermission);
            return hasPermission;
        }

        public List<uint> GetRBACDefaultPermissions(byte secLevel)
        {
            return _defaultPermissions[secLevel];
        }

        public Dictionary<uint, RBACPermission> GetRBACPermissionList() { return _permissions; }
    }

    public enum AccountOpResult
    {
        Ok,
        NameTooLong,
        PassTooLong,
        EmailTooLong,
        NameAlreadyExist,
        NameNotExist,
        DBInternalError,
        BadLink
    }
}
