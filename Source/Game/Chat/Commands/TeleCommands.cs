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
using Framework.IO;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using System.Collections.Generic;

namespace Game.Chat
{
    [CommandGroup("tele", RBACPermissions.CommandTele)]
    class TeleCommands
    {
        [Command("", RBACPermissions.CommandTele)]
        static bool HandleTeleCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player me = handler.GetPlayer();

            GameTele tele = handler.extractGameTeleFromLink(args);

            if (tele == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTeleNotfound);
                return false;
            }

            if (me.IsInCombat())
            {
                handler.SendSysMessage(CypherStrings.YouInCombat);
                return false;
            }

            var map = CliDB.MapStorage.LookupByKey(tele.mapId);
            if (map == null || (map.IsBattlegroundOrArena() && (me.GetMapId() != tele.mapId || !me.IsGameMaster())))
            {
                handler.SendSysMessage(CypherStrings.CannotTeleToBg);
                return false;
            }

            // stop flight if need
            if (me.IsInFlight())
            {
                me.GetMotionMaster().MovementExpired();
                me.CleanupAfterTaxiFlight();
            }
            // save only in non-flight case
            else
                me.SaveRecallPosition();

            me.TeleportTo(tele.mapId, tele.posX, tele.posY, tele.posZ, tele.orientation);
            return true;
        }

        [Command("add", RBACPermissions.CommandTeleAdd)]
        static bool HandleTeleAddCommand(StringArguments args, CommandHandler handler)
        {
            return false;
        }

        [Command("del", RBACPermissions.CommandTeleDel, true)]
        static bool HandleTeleDelCommand(StringArguments args, CommandHandler handler)
        {
            return false;
        }

        [Command("group", RBACPermissions.CommandTeleGroup)]
        static bool HandleTeleGroupCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target = handler.getSelectedPlayer();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            // id, or string, or [name] Shift-click form |color|Htele:id|h[name]|h|r
            GameTele tele = handler.extractGameTeleFromLink(args);
            if (tele == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTeleNotfound);
                return false;
            }

            MapRecord map = CliDB.MapStorage.LookupByKey(tele.mapId);
            if (map == null || map.IsBattlegroundOrArena())
            {
                handler.SendSysMessage(CypherStrings.CannotTeleToBg);
                return false;
            }

            string nameLink = handler.GetNameLink(target);

            Group grp = target.GetGroup();
            if (!grp)
            {
                handler.SendSysMessage(CypherStrings.NotInGroup, nameLink);
                return false;
            }

            for (GroupReference refe = grp.GetFirstMember(); refe != null; refe = refe.next())
            {
                Player player = refe.GetSource();
                if (!player || !player.GetSession())
                    continue;

                // check online security
                if (handler.HasLowerSecurity(player, ObjectGuid.Empty))
                    return false;

                string plNameLink = handler.GetNameLink(player);

                if (player.IsBeingTeleported())
                {
                    handler.SendSysMessage(CypherStrings.IsTeleported, plNameLink);
                    continue;
                }

                handler.SendSysMessage(CypherStrings.TeleportingTo, plNameLink, "", tele.name);
                if (handler.needReportToTarget(player))
                    player.SendSysMessage(CypherStrings.TeleportedToBy, nameLink);

                // stop flight if need
                if (player.IsInFlight())
                {
                    player.GetMotionMaster().MovementExpired();
                    player.CleanupAfterTaxiFlight();
                }
                // save only in non-flight case
                else
                    player.SaveRecallPosition();

                player.TeleportTo(tele.mapId, tele.posX, tele.posY, tele.posZ, tele.orientation);
            }

            return true;
        }

        [Command("name", RBACPermissions.CommandTeleName, true)]
        static bool HandleTeleNameCommand(StringArguments args, CommandHandler handler)
        {
            return false;
        }
    }
}
