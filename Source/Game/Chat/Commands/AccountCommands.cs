// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Entities;
using System;

namespace Game.Chat
{
    [CommandGroup("account")]
    class AccountCommands
    {
        [Command("", CypherStrings.CommandAccountHelp, RBACPermissions.CommandAccount)]
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

                PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.GET_EMAIL_BY_ID);
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

        [Command("2fa remove", CypherStrings.CommandAcc2faRemoveHelp, RBACPermissions.CommandAccount2FaRemove)]
        static bool HandleAccount2FARemoveCommand(CommandHandler handler, OptionalArg<uint> token)
        {
            /*var masterKey = Global.SecretMgr.GetSecret(Secrets.TOTPMasterKey);
            if (!masterKey.IsAvailable())
            {
                handler.SendSysMessage(CypherStrings.TwoFACommandsNotSetup);
                return false;
            }

            uint accountId = handler.GetSession().GetAccountId();
            byte[] secret;
            { // get current TOTP secret
                PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_TOTP_SECRET);
                stmt.AddValue(0, accountId);
                SQLResult result = DB.Login.Query(stmt);

                if (result.IsEmpty())
                {
                    Log.outError(LogFilter.Misc, $"Account {accountId} not found in login database when processing .account 2fa setup command.");
                    handler.SendSysMessage(CypherStrings.UnknownError);
                    return false;
                }

                if (result.IsNull(0))
                { // 2FA not enabled
                    handler.SendSysMessage(CypherStrings.TwoFANotSetup);
                    return false;
                }

                secret = result.Read<byte[]>(0);
            }

            if (token.HasValue)
            {
                if (masterKey.IsValid())
                {
                    bool success = AES.Decrypt(secret, masterKey.GetValue());
                    if (!success)
                    {
                        Log.outError(LogFilter.Misc, $"Account {accountId} has invalid ciphertext in TOTP token.");
                        handler.SendSysMessage(CypherStrings.UnknownError);
                        return false;
                    }
                }

                if (TOTP.ValidateToken(secret, token.Value))
                {
                    PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_TOTP_SECRET);
                    stmt.AddNull(0);
                    stmt.AddValue(1, accountId);
                    DB.Login.Execute(stmt);
                    handler.SendSysMessage(CypherStrings.TwoFARemoveComplete);
                    return true;
                }
                else
                    handler.SendSysMessage(CypherStrings.TwoFAInvalidToken);
            }

            handler.SendSysMessage(CypherStrings.TwoFARemoveNeedToken);*/
            return false;
        }

        [Command("2fa setup", CypherStrings.CommandAcc2faSetupHelp, RBACPermissions.CommandAccount2FaSetup)]
        static bool HandleAccount2FASetupCommand(CommandHandler handler, OptionalArg<uint> token)
        {
            /*var masterKey = Global.SecretMgr.GetSecret(Secrets.TOTPMasterKey);
            if (!masterKey.IsAvailable())
            {
                handler.SendSysMessage(CypherStrings.TwoFACommandsNotSetup);
                return false;
            }

            uint accountId = handler.GetSession().GetAccountId();

            { // check if 2FA already enabled
                PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_TOTP_SECRET);
                stmt.AddValue(0, accountId);
                SQLResult result = DB.Login.Query(stmt);

                if (result.IsEmpty())
                {
                    Log.outError(LogFilter.Misc, $"Account {accountId} not found in login database when processing .account 2fa setup command.");
                    handler.SendSysMessage(CypherStrings.UnknownError);
                    return false;
                }

                if (!result.IsNull(0))
                {
                    handler.SendSysMessage(CypherStrings.TwoFAAlreadySetup);
                    return false;
                }
            }

            // store random suggested secrets
            Dictionary<uint, byte[]> suggestions = new();
            var pair = suggestions.TryAdd(accountId, new byte[20]); // std::vector 1-argument size_t constructor invokes resize
            if (pair) // no suggestion yet, generate random secret
                suggestions[accountId] = new byte[0].GenerateRandomKey(20);

            if (!pair && token.HasValue) // suggestion already existed and token specified - validate
            {
                if (TOTP.ValidateToken(suggestions[accountId], token.Value))
                {
                    if (masterKey.IsValid())
                        AES.Encrypt(suggestions[accountId], masterKey.GetValue());

                    PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_TOTP_SECRET);
                    stmt.AddValue(0, suggestions[accountId]);
                    stmt.AddValue(1, accountId);
                    DB.Login.Execute(stmt);
                    suggestions.Remove(accountId);
                    handler.SendSysMessage(CypherStrings.TwoFASetupComplete);
                    return true;
                }
                else
                    handler.SendSysMessage(CypherStrings.TwoFAInvalidToken);
            }

            // new suggestion, or no token specified, output TOTP parameters
            handler.SendSysMessage(CypherStrings.TwoFASecretSuggestion, suggestions[accountId].ToBase32());*/
            return false;
        }

        [Command("addon", CypherStrings.CommandAccAddonHelp, RBACPermissions.CommandAccountAddon)]
        static bool HandleAccountAddonCommand(CommandHandler handler, byte expansion)
        {
            if (expansion > WorldConfig.GetIntValue(WorldCfg.Expansion))
            {
                handler.SendSysMessage(CypherStrings.ImproperValue);
                return false;
            }

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_EXPANSION);
            stmt.AddValue(0, expansion);
            stmt.AddValue(1, handler.GetSession().GetAccountId());
            DB.Login.Execute(stmt);

            handler.SendSysMessage(CypherStrings.AccountAddon, expansion);
            return true;
        }

        [Command("create", CypherStrings.CommandAccCreateHelp, RBACPermissions.CommandAccountCreate, true)]
        static bool HandleAccountCreateCommand(CommandHandler handler, string accountName, string password, OptionalArg<string> email)
        {
            if (accountName.Contains("@"))
            {
                handler.SendSysMessage(CypherStrings.AccountUseBnetCommands);
                return false;
            }

            AccountOpResult result = Global.AccountMgr.CreateAccount(accountName, password, email.GetValueOrDefault(""));
            switch (result)
            {
                case AccountOpResult.Ok:
                    handler.SendSysMessage(CypherStrings.AccountCreated, accountName);
                    if (handler.GetSession() != null)
                    {
                        Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) created Account {4} (Email: '{5}')",
                            handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                            handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(),
                            accountName, email.GetValueOrDefault(""));
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

        [Command("delete", CypherStrings.CommandAccDeleteHelp, RBACPermissions.CommandAccountDelete, true)]
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

        [Command("email", CypherStrings.CommandAccEmailHelp, RBACPermissions.CommandAccountEmail)]
        static bool HandleAccountEmailCommand(CommandHandler handler, string oldEmail, string password, string email, string emailConfirm)
        {
            if (!Global.AccountMgr.CheckEmail(handler.GetSession().GetAccountId(), oldEmail))
            {
                handler.SendSysMessage(CypherStrings.CommandWrongemail);
                Global.ScriptMgr.OnFailedEmailChange(handler.GetSession().GetAccountId());
                Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change email, but the provided email [{4}] is not equal to registration email [{5}].",
                    handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                    handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(),
                    email, oldEmail);
                return false;
            }

            if (!Global.AccountMgr.CheckPassword(handler.GetSession().GetAccountId(), password))
            {
                handler.SendSysMessage(CypherStrings.CommandWrongoldpassword);
                Global.ScriptMgr.OnFailedEmailChange(handler.GetSession().GetAccountId());
                Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Tried to change email, but the provided password is wrong.",
                    handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                    handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString());
                return false;
            }

            if (email == oldEmail)
            {
                handler.SendSysMessage(CypherStrings.OldEmailIsNewEmail);
                Global.ScriptMgr.OnFailedEmailChange(handler.GetSession().GetAccountId());
                return false;
            }

            if (email != emailConfirm)
            {
                handler.SendSysMessage(CypherStrings.NewEmailsNotMatch);
                Global.ScriptMgr.OnFailedEmailChange(handler.GetSession().GetAccountId());
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
                    Global.ScriptMgr.OnEmailChange(handler.GetSession().GetAccountId());
                    Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Changed Email from [{4}] to [{5}].",
                        handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                        handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(),
                        oldEmail, email);
                    break;
                case AccountOpResult.EmailTooLong:
                    handler.SendSysMessage(CypherStrings.EmailTooLong);
                    Global.ScriptMgr.OnFailedEmailChange(handler.GetSession().GetAccountId());
                    return false;
                default:
                    handler.SendSysMessage(CypherStrings.CommandNotchangeemail);
                    return false;
            }

            return true;
        }

        [Command("password", CypherStrings.CommandAccPasswordHelp, RBACPermissions.CommandAccountPassword)]
        static bool HandleAccountPasswordCommand(CommandHandler handler, string oldPassword, string newPassword, string confirmPassword, OptionalArg<string> confirmEmail)
        {
            // First, we check config. What security type (sec type) is it ? Depending on it, the command branches out
            uint pwConfig = WorldConfig.GetUIntValue(WorldCfg.AccPasschangesec); // 0 - PW_NONE, 1 - PW_EMAIL, 2 - PW_RBAC

            // We compare the old, saved password to the entered old password - no chance for the unauthorized.
            if (!Global.AccountMgr.CheckPassword(handler.GetSession().GetAccountId(), oldPassword))
            {
                handler.SendSysMessage(CypherStrings.CommandWrongoldpassword);
                Global.ScriptMgr.OnFailedPasswordChange(handler.GetSession().GetAccountId());
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
                Global.ScriptMgr.OnFailedPasswordChange(handler.GetSession().GetAccountId());
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
                Global.ScriptMgr.OnFailedPasswordChange(handler.GetSession().GetAccountId());
                return false;
            }

            // Changes password and prints result.
            AccountOpResult result = Global.AccountMgr.ChangePassword(handler.GetSession().GetAccountId(), newPassword);
            switch (result)
            {
                case AccountOpResult.Ok:
                    handler.SendSysMessage(CypherStrings.CommandPassword);
                    Global.ScriptMgr.OnPasswordChange(handler.GetSession().GetAccountId());
                    Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Character:[{2}] (GUID: {3}) Changed Password.",
                        handler.GetSession().GetAccountId(), handler.GetSession().GetRemoteAddress(),
                        handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString());
                    break;
                case AccountOpResult.PassTooLong:
                    handler.SendSysMessage(CypherStrings.PasswordTooLong);
                    Global.ScriptMgr.OnFailedPasswordChange(handler.GetSession().GetAccountId());
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
            [Command("country", CypherStrings.CommandAccLockCountryHelp, RBACPermissions.CommandAccountLockCountry)]
            static bool HandleAccountLockCountryCommand(CommandHandler handler, bool state)
            {
                if (state)
                {
                    /*var ipBytes = System.Net.IPAddress.Parse(handler.GetSession().GetRemoteAddress()).GetAddressBytes();
                    Array.Reverse(ipBytes);

                    PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_LOGON_COUNTRY);
                    stmt.AddValue(0, BitConverter.ToUInt32(ipBytes, 0));

                    SQLResult result = DB.Login.Query(stmt);
                    if (!result.IsEmpty())
                    {
                        string country = result.Read<string>(0);
                        stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_LOCK_COUNTRY);
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
                    PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_LOCK_COUNTRY);
                    stmt.AddValue(0, "00");
                    stmt.AddValue(1, handler.GetSession().GetAccountId());
                    DB.Login.Execute(stmt);
                    handler.SendSysMessage(CypherStrings.CommandAcclockunlocked);
                }
                return true;
            }

            [Command("ip", CypherStrings.CommandAccLockIpHelp, RBACPermissions.CommandAccountLockIp)]
            static bool HandleAccountLockIpCommand(CommandHandler handler, bool state)
            {
                PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_LOCK);

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
            [Command("", CypherStrings.CommandAccOnlinelistHelp, RBACPermissions.CommandAccountOnlineList, true)]
            static bool HandleAccountOnlineListCommand(CommandHandler handler)
            {
                return HandleAccountOnlineListCommandWithParameters(handler, null, default, default, default);
            }

            [Command("ip", CypherStrings.CommandAccOnlinelistHelp, RBACPermissions.CommandAccountOnlineList, true)]
            static bool HandleAccountOnlineListWithIpFilterCommand(CommandHandler handler, string ipAddress)
            {
                return HandleAccountOnlineListCommandWithParameters(handler, ipAddress, default, default, default);
            }

            [Command("limit", CypherStrings.CommandAccOnlinelistHelp, RBACPermissions.CommandAccountOnlineList, true)]
            static bool HandleAccountOnlineListWithLimitCommand(CommandHandler handler, uint limit)
            {
                return HandleAccountOnlineListCommandWithParameters(handler, null, limit, default, default);
            }

            [Command("map", CypherStrings.CommandAccOnlinelistHelp, RBACPermissions.CommandAccountOnlineList, true)]
            static bool HandleAccountOnlineListWithMapFilterCommand(CommandHandler handler, uint mapId)
            {
                return HandleAccountOnlineListCommandWithParameters(handler, null, default, mapId, default);
            }

            [Command("zone", CypherStrings.CommandAccOnlinelistHelp, RBACPermissions.CommandAccountOnlineList, true)]
            static bool HandleAccountOnlineListWithZoneFilterCommand(CommandHandler handler, uint zoneId)
            {
                return HandleAccountOnlineListCommandWithParameters(handler, null, default, default, zoneId);
            }

            static bool HandleAccountOnlineListCommandWithParameters(CommandHandler handler, string ipAddress, OptionalArg<uint> limit, OptionalArg<uint> mapId, OptionalArg<uint> zoneId)
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
            [Command("2fa", CypherStrings.CommandAccSet2faHelp, RBACPermissions.CommandAccountSet2Fa, true)]
            static bool HandleAccountSet2FACommand(CommandHandler handler, string accountName, string secret)
            {
                /*uint targetAccountId = Global.AccountMgr.GetId(accountName);
                if (targetAccountId == 0)
                {
                    handler.SendSysMessage(CypherStrings.AccountNotExist, accountName);
                    return false;
                }

                if (handler.HasLowerSecurityAccount(null, targetAccountId, true))
                    return false;

                PreparedStatement stmt;
                if (secret == "off")
                {
                    stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_TOTP_SECRET);
                    stmt.AddNull(0);
                    stmt.AddValue(1, targetAccountId);
                    DB.Login.Execute(stmt);
                    handler.SendSysMessage(CypherStrings.TwoFARemoveComplete);
                    return true;
                }

                var masterKey = Global.SecretMgr.GetSecret(Secrets.TOTPMasterKey);
                if (!masterKey.IsAvailable())
                {
                    handler.SendSysMessage(CypherStrings.TwoFACommandsNotSetup);
                    return false;
                }

                var decoded = secret.FromBase32();
                if (decoded == null)
                {
                    handler.SendSysMessage(CypherStrings.TwoFASecretInvalid);
                    return false;
                }
                if (128 < (decoded.Length + 12 + 12))
                {
                    handler.SendSysMessage(CypherStrings.TwoFASecretTooLong);
                    return false;
                }

                if (masterKey.IsValid())
                    AES.Encrypt(decoded, masterKey.GetValue());

                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_TOTP_SECRET);
                stmt.AddValue(0, decoded);
                stmt.AddValue(1, targetAccountId);
                DB.Login.Execute(stmt);
                handler.SendSysMessage(CypherStrings.TwoFASecretSetComplete, accountName);*/
                return true;
            }

            [Command("addon", CypherStrings.CommandAccSetAddonHelp, RBACPermissions.CommandAccountSetAddon, true)]
            static bool HandleAccountSetAddonCommand(CommandHandler handler, OptionalArg<string> accountName, byte expansion)
            {
                uint accountId;
                if (accountName.HasValue)
                {
                    // Convert Account name to Upper Format
                    accountName = accountName.Value.ToUpper();

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
                    if (player == null)
                        return false;

                    accountId = player.GetSession().GetAccountId();
                    Global.AccountMgr.GetName(accountId, out string tempAccountName);
                    accountName = tempAccountName;
                }

                // Let set addon state only for lesser (strong) security level
                // or to self account
                if (handler.GetSession() != null && handler.GetSession().GetAccountId() != accountId &&
                    handler.HasLowerSecurityAccount(null, accountId, true))
                    return false;

                if (expansion > WorldConfig.GetIntValue(WorldCfg.Expansion))
                    return false;

                PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_EXPANSION);

                stmt.AddValue(0, expansion);
                stmt.AddValue(1, accountId);

                DB.Login.Execute(stmt);

                handler.SendSysMessage(CypherStrings.AccountSetaddon, accountName, accountId, expansion);
                return true;
            }

            [Command("password", CypherStrings.CommandAccSetPasswordHelp, RBACPermissions.CommandAccountSetPassword, true)]
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

            [Command("seclevel", CypherStrings.CommandAccSetSeclevelHelp, RBACPermissions.CommandAccountSetSecLevel, true)]
            [Command("gmlevel", CypherStrings.CommandAccSetSeclevelHelp, RBACPermissions.CommandAccountSetSecLevel, true)]
            static bool HandleAccountSetSecLevelCommand(CommandHandler handler, OptionalArg<string> accountName, byte securityLevel, OptionalArg<int> realmId)
            {
                uint accountId;
                if (accountName.HasValue)
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
                    if (player == null)
                        return false;

                    accountId = player.GetSession().GetAccountId();
                    Global.AccountMgr.GetName(accountId, out string tempAccountName);
                    accountName = tempAccountName;
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
                    stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_ACCESS_SECLEVEL_TEST);
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
                [Command("email", CypherStrings.CommandAccSetSecEmailHelp, RBACPermissions.CommandAccountSetSecEmail, true)]
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

                [Command("regmail", CypherStrings.CommandAccSetSecRegmailHelp, RBACPermissions.CommandAccountSetSecRegmail, true)]
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
