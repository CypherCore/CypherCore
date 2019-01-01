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

using Framework.Database;
using System;
using System.Security.Cryptography;
using System.Text;

namespace Game
{
    public sealed class BNetAccountManager : Singleton<BNetAccountManager>
    {
        BNetAccountManager() { }

        public AccountOpResult CreateBattlenetAccount(string email, string password, bool withGameAccount, out string gameAccountName)
        {
            gameAccountName = "";

            if (email.Length > 320)
                return AccountOpResult.NameTooLong;

            if (password.Length > 16)
                return AccountOpResult.PassTooLong;

            if (GetId(email) != 0)
                return AccountOpResult.NameAlreadyExist;

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_ACCOUNT);
            stmt.AddValue(0, email);
            stmt.AddValue(1, CalculateShaPassHash(email, password));
            DB.Login.DirectExecute(stmt);

            uint newAccountId = GetId(email);
            Cypher.Assert(newAccountId != 0);

            if (withGameAccount)
            {
                gameAccountName = newAccountId + "#1";
                Global.AccountMgr.CreateAccount(gameAccountName, password, email, newAccountId, 1);
            }

            return AccountOpResult.Ok;
        }

        public AccountOpResult ChangePassword(uint accountId, string newPassword)
        {
            string username;
            if (!GetName(accountId, out username))
                return AccountOpResult.NameNotExist;

            if (newPassword.Length > 16)
                return AccountOpResult.PassTooLong;

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_PASSWORD);
            stmt.AddValue(0, CalculateShaPassHash(username, newPassword));
            stmt.AddValue(1, accountId);
            DB.Login.Execute(stmt);

            return AccountOpResult.Ok;
        }

        public bool CheckPassword(uint accountId, string password)
        {
            string username;
            if (!GetName(accountId, out username))
                return false;

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_CHECK_PASSWORD);
            stmt.AddValue(0, accountId);
            stmt.AddValue(1, CalculateShaPassHash(username, password));

            return !DB.Login.Query(stmt).IsEmpty();
        }

        public AccountOpResult LinkWithGameAccount(string email, string gameAccountName)
        {
            uint bnetAccountId = GetId(email);
            if (bnetAccountId == 0)
                return AccountOpResult.NameNotExist;

            uint gameAccountId = Global.AccountMgr.GetId(gameAccountName);
            if (gameAccountId == 0)
                return AccountOpResult.NameNotExist;

            if (GetIdByGameAccount(gameAccountId) != 0)
                return AccountOpResult.BadLink;

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_GAME_ACCOUNT_LINK);
            stmt.AddValue(0, bnetAccountId);
            stmt.AddValue(1, GetMaxIndex(bnetAccountId) + 1);
            stmt.AddValue(2, gameAccountId);
            DB.Login.Execute(stmt);
            return AccountOpResult.Ok;
        }

        public AccountOpResult UnlinkGameAccount(string gameAccountName)
        {
            uint gameAccountId = Global.AccountMgr.GetId(gameAccountName);
            if (gameAccountId == 0)
                return AccountOpResult.NameNotExist;

            if (GetIdByGameAccount(gameAccountId) == 0)
                return AccountOpResult.BadLink;

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_GAME_ACCOUNT_LINK);
            stmt.AddValue(0, null);
            stmt.AddValue(1, null);
            stmt.AddValue(2, gameAccountId);
            DB.Login.Execute(stmt);
            return AccountOpResult.Ok;
        }

        public uint GetId(string username)
        {
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ACCOUNT_ID_BY_EMAIL);
            stmt.AddValue(0, username);
            SQLResult result = DB.Login.Query(stmt);
            if (!result.IsEmpty())
                return result.Read<uint>(0);

            return 0;
        }

        public bool GetName(uint accountId, out string name)
        {
            name = "";
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ACCOUNT_EMAIL_BY_ID);
            stmt.AddValue(0, accountId);
            SQLResult result = DB.Login.Query(stmt);
            if (!result.IsEmpty())
            {
                name = result.Read<string>(0);
                return true;
            }

            return false;
        }

        public uint GetIdByGameAccount(uint gameAccountId)
        {
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ACCOUNT_ID_BY_GAME_ACCOUNT);
            stmt.AddValue(0, gameAccountId);
            SQLResult result = DB.Login.Query(stmt);
            if (!result.IsEmpty())
                return result.Read<uint>(0);

            return 0;
        }

        public byte GetMaxIndex(uint accountId)
        {
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_MAX_ACCOUNT_INDEX);
            stmt.AddValue(0, accountId);
            SQLResult result = DB.Login.Query(stmt);
            if (!result.IsEmpty())
                return result.Read<byte>(0);

            return 0;
        }

        public string CalculateShaPassHash(string name, string password)
        {
            SHA256 sha256 = SHA256.Create();
            var i = sha256.ComputeHash(Encoding.UTF8.GetBytes(name));
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(i.ToHexString() + ":" + password)).ToHexString();
        }
    }
}
