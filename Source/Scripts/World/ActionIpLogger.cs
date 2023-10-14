// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Configuration;
using Framework.Constants;
using Framework.Database;
using Framework.Networking;
using Game.Entities;
using System.Collections.Generic;
using Game.AI;
using Game.Scripting;
using Game.Spells;

namespace Scripts.World.Achievements
{
    enum IPLoggingTypes
    {
        // AccountActionIpLogger();
        AccountLogin = 0,
        AccountFailLogin = 1,
        AccountChangePw = 2,
        AccountChangePwFail = 3, // Only two types of account changes exist...
        AccountChangeEmail = 4,
        AccountChangeEmailFail = 5, // ...so we log them individually
                                    // Obsolete - AccountLogout = 6,  Can not be logged. We still keep the type however 
                                    // CharacterActionIpLogger();
        CharacterCreate = 7,
        CharacterLogin = 8,
        CharacterLogout = 9,
        // CharacterDeleteActionIpLogger();
        CharacterDelete = 10,
        CharacterFailedDelete = 11,
        // AccountActionIpLogger(), CharacterActionIpLogger(), CharacterActionIpLogger();
        UnknownAction = 12
    }

    [Script]
    class AccountActionIpLogger : AccountScript
    {
        public AccountActionIpLogger() : base("AccountActionIpLogger") { }

        // We log last_ip instead of last_attempt_ip, as login was successful
        // AccountLogin = 0
        public override void OnAccountLogin(uint accountId)
        {
            AccountIPLogAction(accountId, IPLoggingTypes.AccountLogin);
        }

        // We log last_attempt_ip instead of last_ip, as failed login doesn't necessarily mean approperiate user
        // AccountFailLogin = 1
        public override void OnFailedAccountLogin(uint accountId)
        {
            AccountIPLogAction(accountId, IPLoggingTypes.AccountFailLogin);
        }

        // AccountChangePw = 2
        public override void OnPasswordChange(uint accountId)
        {
            AccountIPLogAction(accountId, IPLoggingTypes.AccountChangePw);
        }

        // AccountChangePwFail = 3
        public override void OnFailedPasswordChange(uint accountId)
        {
            AccountIPLogAction(accountId, IPLoggingTypes.AccountChangePwFail);
        }

        // Registration Email can Not be changed apart from Gm level users. Thus, we do not require to log them...
        // AccountChangeEmail = 4
        public override void OnEmailChange(uint accountId)
        {
            AccountIPLogAction(accountId, IPLoggingTypes.AccountChangeEmail); // ... they get logged by gm command logger anyway
        }

        // AccountChangeEmailFail = 5
        public override void OnFailedEmailChange(uint accountId)
        {
            AccountIPLogAction(accountId, IPLoggingTypes.AccountChangeEmailFail);
        }

        // AccountLogout = 6
        void AccountIPLogAction(uint accountId, IPLoggingTypes aType)
        {
            // Action Ip Logger is only intialized if config is set up
            // Else, this script isn't loaded in the first place: We require no config check.

            // We declare all the required variables
            uint playerGuid = accountId;
            uint realmId = Global.WorldMgr.GetRealmId().Index;
            string systemNote = "Error"; // "Error" is a placeholder here. We change it later.

            // With this switch, we change systemNote so that we have a more accurate phraMath.Sing of what type it is.
            // Avoids Magicnumbers in Sql table
            switch (aType)
            {
                case IPLoggingTypes.AccountLogin:
                    systemNote = "Logged into WoW";
                    break;
                case IPLoggingTypes.AccountFailLogin:
                    systemNote = "Login to WoW Failed";
                    break;
                case IPLoggingTypes.AccountChangePw:
                    systemNote = "Password Reset Completed";
                    break;
                case IPLoggingTypes.AccountChangePwFail:
                    systemNote = "Password Reset Failed";
                    break;
                case IPLoggingTypes.AccountChangeEmail:
                    systemNote = "Email Change Completed";
                    break;
                case IPLoggingTypes.AccountChangeEmailFail:
                    systemNote = "Email Change Failed";
                    break;
                /*case IPLoggingTypes.AccountLogout:
                    systemNote = "Logged on AccountLogout"; //Can not be logged
                    break;*/
                // Neither should happen. Ever. Period. If it does, call Ghostbusters and all your local software defences to investigate.
                case IPLoggingTypes.UnknownAction:
                default:
                    systemNote = "Error! Unknown action!";
                    break;
            }

            // Once we have done everything, we can Add the new log.
            // Seeing as the time differences should be minimal, we do not get unixtime and the timestamp right now;
            // Rather, we let it be added with the Sql query.
            if (aType != IPLoggingTypes.AccountFailLogin)
            {
                // As we can assume most account actions are Not failed login, so this is the more accurate check.
                // For those, we need last_ip...
                PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_ALDL_IP_LOGGING);

                stmt.AddValue(0, playerGuid);
                stmt.AddValue(1, 0ul);
                stmt.AddValue(2, realmId);
                stmt.AddValue(3, (byte)aType);
                stmt.AddValue(4, playerGuid);
                stmt.AddValue(5, systemNote);
                DB.Login.Execute(stmt);
            }
            else // ... but for failed login, we query last_attempt_ip from account table. Which we do with an unique query
            {
                PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_FACL_IP_LOGGING);

                stmt.AddValue(0, playerGuid);
                stmt.AddValue(1, 0ul);
                stmt.AddValue(2, realmId);
                stmt.AddValue(3, (byte)aType);
                stmt.AddValue(4, playerGuid);
                stmt.AddValue(5, systemNote);
                DB.Login.Execute(stmt);
            }
            return;
        }
    }

    [Script]
    class CharacterActionIpLogger : PlayerScript
    {
        public CharacterActionIpLogger() : base("CharacterActionIpLogger") { }

        // CharacterCreate = 7
        public override void OnCreate(Player player)
        {
            CharacterIPLogAction(player, IPLoggingTypes.CharacterCreate);
        }

        // CharacterLogin = 8
        public override void OnLogin(Player player, bool firstLogin)
        {
            CharacterIPLogAction(player, IPLoggingTypes.CharacterLogin);
        }

        // CharacterLogout = 9
        public override void OnLogout(Player player)
        {
            CharacterIPLogAction(player, IPLoggingTypes.CharacterLogout);
        }

        // CharacterDelete = 10
        // CharacterFailedDelete = 11
        // We don't log either here - they require a guid

        // UnknownAction = 12
        // There is no real hook we could use for that.
        // Shouldn't happen anyway, should it ? Nothing to see here.

        /// Logs a number of actions done by players with an Ip
        void CharacterIPLogAction(Player player, IPLoggingTypes aType)
        {
            // Action Ip Logger is only intialized if config is set up
            // Else, this script isn't loaded in the first place: We require no config check.

            // We declare all the required variables
            uint playerGuid = player.GetSession().GetAccountId();
            uint realmId = Global.WorldMgr.GetRealmId().Index;
            string currentIp = player.GetSession().GetRemoteAddress();
            string systemNote;

            // ... with this switch, so that we have a more accurate phraMath.Sing of what type it is
            switch (aType)
            {
                case IPLoggingTypes.CharacterCreate:
                    systemNote = "Character Created";
                    break;
                case IPLoggingTypes.CharacterLogin:
                    systemNote = "Logged onto Character";
                    break;
                case IPLoggingTypes.CharacterLogout:
                    systemNote = "Logged out of Character";
                    break;
                case IPLoggingTypes.CharacterDelete:
                    systemNote = "Character Deleted";
                    break;
                case IPLoggingTypes.CharacterFailedDelete:
                    systemNote = "Character Deletion Failed";
                    break;
                // Neither should happen. Ever. Period. If it does, call Mythbusters.
                case IPLoggingTypes.UnknownAction:
                default:
                    systemNote = "Error! Unknown action!";
                    break;
            }

            // Once we have done everything, we can Add the new log.
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_CHAR_IP_LOGGING);

            stmt.AddValue(0, playerGuid);
            stmt.AddValue(1, player.GetGUID().GetCounter());
            stmt.AddValue(2, realmId);
            stmt.AddValue(3, (byte)aType);
            stmt.AddValue(4, currentIp); // We query the ip here.
            stmt.AddValue(5, systemNote);
            // Seeing as the time differences should be minimal, we do not get unixtime and the timestamp right now;
            // Rather, we let it be added with the Sql query.

            DB.Login.Execute(stmt);
            return;
        }
    }

    [Script]
    class CharacterDeleteActionIpLogger : PlayerScript
    {
        public CharacterDeleteActionIpLogger() : base("CharacterDeleteActionIpLogger") { }

        // CharacterDelete = 10
        public override void OnDelete(ObjectGuid guid, uint accountId)
        {
            DeleteIPLogAction(guid, accountId, IPLoggingTypes.CharacterDelete);
        }

        // CharacterFailedDelete = 11
        public override void OnFailedDelete(ObjectGuid guid, uint accountId)
        {
            DeleteIPLogAction(guid, accountId, IPLoggingTypes.CharacterFailedDelete);
        }

        void DeleteIPLogAction(ObjectGuid guid, uint playerGuid, IPLoggingTypes aType)
        {
            // Action Ip Logger is only intialized if config is set up
            // Else, this script isn't loaded in the first place: We require no config check.

            uint realmId = Global.WorldMgr.GetRealmId().Index;
            // Query playerGuid/accountId, as we only have characterGuid
            string systemNote;

            // With this switch, we change systemNote so that we have a more accurate phraMath.Sing of what type it is.
            // Avoids Magicnumbers in Sql table
            switch (aType)
            {
                case IPLoggingTypes.CharacterDelete:
                    systemNote = "Character Deleted";
                    break;
                case IPLoggingTypes.CharacterFailedDelete:
                    systemNote = "Character Deletion Failed";
                    break;
                // Neither should happen. Ever. Period. If it does, call to whatever god you have for mercy and guidance.
                case IPLoggingTypes.UnknownAction:
                default:
                    systemNote = "Error! Unknown action!";
                    break;
            }

            // Once we have done everything, we can Add the new log.
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_ALDL_IP_LOGGING);

            stmt.AddValue(0, playerGuid);
            stmt.AddValue(1, guid.GetCounter());
            stmt.AddValue(2, realmId);
            stmt.AddValue(3, (byte)aType);
            stmt.AddValue(4, playerGuid);
            stmt.AddValue(5, systemNote);

            // Seeing as the time differences should be minimal, we do not get unixtime and the timestamp right now;
            // Rather, we let it be added with the Sql query.

            DB.Login.Execute(stmt);
            return;
        }
    }
}

