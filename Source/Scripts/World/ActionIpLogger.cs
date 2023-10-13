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

    class AccountActionIpLogger : AccountScript
    {
        public AccountActionIpLogger() : base("AccountActionIpLogger") { }

        // We log last_ip instead of last_attempt_ip, as login was successful
        // AccountLogin = 0
        void OnAccountLogin(uint accountId)
        {
            AccountIPLogAction(accountId, AccountLogin);
        }

        // We log last_attempt_ip instead of last_ip, as failed login doesn't necessarily mean approperiate user
        // AccountFailLogin = 1
        void OnFailedAccountLogin(uint accountId)
        {
            AccountIPLogAction(accountId, AccountFailLogin);
        }

        // AccountChangePw = 2
        void OnPasswordChange(uint accountId)
        {
            AccountIPLogAction(accountId, AccountChangePw);
        }

        // AccountChangePwFail = 3
        void OnFailedPasswordChange(uint accountId)
        {
            AccountIPLogAction(accountId, AccountChangePwFail);
        }

        // Registration Email can Not be changed apart from Gm level users. Thus, we do not require to log them...
        // AccountChangeEmail = 4
        void OnEmailChange(uint accountId)
        {
            AccountIPLogAction(accountId, AccountChangeEmail); // ... they get logged by gm command logger anyway
        }

        // AccountChangeEmailFail = 5
        void OnFailedEmailChange(uint accountId)
        {
            AccountIPLogAction(accountId, AccountChangeEmailFail);
        }

        // AccountLogout = 6
        void AccountIPLogAction(uint accountId, IPLoggingTypes aType)
        {
            // Action Ip Logger is only intialized if config is set up
            // Else, this script isn't loaded in the first place: We require no config check.

            // We declare all the required variables
            uint playerGuid = accountId;
            uint realmId = realm.Id.Realm;
            std.string systemNote = "Error"; // "Error" is a placeholder here. We change it later.

            // With this switch, we change systemNote so that we have a more accurate phraMath.Sing of what type it is.
            // Avoids Magicnumbers in Sql table
            switch (aType)
            {
                case AccountLogin:
                    systemNote = "Logged into WoW";
                    break;
                case AccountFailLogin:
                    systemNote = "Login to WoW Failed";
                    break;
                case AccountChangePw:
                    systemNote = "Password Reset Completed";
                    break;
                case AccountChangePwFail:
                    systemNote = "Password Reset Failed";
                    break;
                case AccountChangeEmail:
                    systemNote = "Email Change Completed";
                    break;
                case AccountChangeEmailFail:
                    systemNote = "Email Change Failed";
                    break;
                case AccountLogout:
                    systemNote = "Logged on AccountLogout"; //Can not be logged
                    break;
                // Neither should happen. Ever. Period. If it does, call Ghostbusters and all your local software defences to investigate.
                case UnknownAction:
                default:
                    systemNote = "Error! Unknown action!";
                    break;
            }

            // Once we have done everything, we can Add the new log.
            // Seeing as the time differences should be minimal, we do not get unixtime and the timestamp right now;
            // Rather, we let it be added with the Sql query.
            if (aType != AccountFailLogin)
            {
                // As we can assume most account actions are Not failed login, so this is the more accurate check.
                // For those, we need last_ip...

                stmt.setUint(0, playerGuid);
                stmt.setUInt64(1, 0);
                stmt.setUint(2, realmId);
                stmt.setUInt8(3, aType);
                stmt.setUint(4, playerGuid);
                stmt.setString(5, systemNote);
                LoginDatabase.Execute(stmt);
            }
            else // ... but for failed login, we query last_attempt_ip from account table. Which we do with an unique query
            {

                stmt.setUint(0, playerGuid);
                stmt.setUInt64(1, 0);
                stmt.setUint(2, realmId);
                stmt.setUInt8(3, aType);
                stmt.setUint(4, playerGuid);
                stmt.setString(5, systemNote);
                LoginDatabase.Execute(stmt);
            }
            return;
        }
    }

    class CharacterActionIpLogger : PlayerScript
    {
        public CharacterActionIpLogger() : base("CharacterActionIpLogger") { }

        // CharacterCreate = 7
        void OnCreate(Player player)
        {
            CharacterIPLogAction(player, CharacterCreate);
        }

        // CharacterLogin = 8
        void OnLogin(Player player, bool firstLogin)
        {
            CharacterIPLogAction(player, CharacterLogin);
        }

        // CharacterLogout = 9
        void OnLogout(Player player)
        {
            CharacterIPLogAction(player, CharacterLogout);
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
            uint realmId = realm.Id.Realm;
            std.string currentIp = player.GetSession().GetRemoteAddress();
            std.string systemNote = "Error"; // "Error" is a placeholder here. We change it...

            // ... with this switch, so that we have a more accurate phraMath.Sing of what type it is
            switch (aType)
            {
                case CharacterCreate:
                    systemNote = "Character Created";
                    break;
                case CharacterLogin:
                    systemNote = "Logged onto Character";
                    break;
                case CharacterLogout:
                    systemNote = "Logged out of Character";
                    break;
                case CharacterDelete:
                    systemNote = "Character Deleted";
                    break;
                case CharacterFailedDelete:
                    systemNote = "Character Deletion Failed";
                    break;
                // Neither should happen. Ever. Period. If it does, call Mythbusters.
                case UnknownAction:
                default:
                    systemNote = "Error! Unknown action!";
                    break;
            }

            // Once we have done everything, we can Add the new log.

            stmt.setUint(0, playerGuid);
            stmt.setUInt64(1, player.GetGUID().GetCounter());
            stmt.setUint(2, realmId);
            stmt.setUInt8(3, aType);
            stmt.setString(4, currentIp); // We query the ip here.
            stmt.setString(5, systemNote);
            // Seeing as the time differences should be minimal, we do not get unixtime and the timestamp right now;
            // Rather, we let it be added with the Sql query.

            LoginDatabase.Execute(stmt);
            return;
        }
    }

    class CharacterDeleteActionIpLogger : PlayerScript
    {
        public CharacterDeleteActionIpLogger() : base("CharacterDeleteActionIpLogger") { }

        // CharacterDelete = 10
        void OnDelete(ObjectGuid guid, uint accountId)
        {
            DeleteIPLogAction(guid, accountId, CharacterDelete);
        }

        // CharacterFailedDelete = 11
        void OnFailedDelete(ObjectGuid guid, uint accountId)
        {
            DeleteIPLogAction(guid, accountId, CharacterFailedDelete);
        }

        void DeleteIPLogAction(ObjectGuid guid, uint playerGuid, IPLoggingTypes aType)
        {
            // Action Ip Logger is only intialized if config is set up
            // Else, this script isn't loaded in the first place: We require no config check.

            uint realmId = realm.Id.Realm;
            // Query playerGuid/accountId, as we only have characterGuid
            std.string systemNote = "Error"; // "Error" is a placeholder here. We change it later.

            // With this switch, we change systemNote so that we have a more accurate phraMath.Sing of what type it is.
            // Avoids Magicnumbers in Sql table
            switch (aType)
            {
                case CharacterDelete:
                    systemNote = "Character Deleted";
                    break;
                case CharacterFailedDelete:
                    systemNote = "Character Deletion Failed";
                    break;
                // Neither should happen. Ever. Period. If it does, call to whatever god you have for mercy and guidance.
                case UnknownAction:
                default:
                    systemNote = "Error! Unknown action!";
                    break;
            }

            // Once we have done everything, we can Add the new log.

            stmt.setUint(0, playerGuid);
            stmt.setUInt64(1, guid.GetCounter());
            stmt.setUint(2, realmId);
            stmt.setUInt8(3, aType);
            stmt.setUint(4, playerGuid);
            stmt.setString(5, systemNote);

            // Seeing as the time differences should be minimal, we do not get unixtime and the timestamp right now;
            // Rather, we let it be added with the Sql query.

            LoginDatabase.Execute(stmt);
            return;
        }
    }
}

