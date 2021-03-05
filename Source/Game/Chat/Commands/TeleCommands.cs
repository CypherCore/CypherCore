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
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    [CommandGroup("tele", RBACPermissions.CommandTele)]
    internal class TeleCommands
    {
        [Command("", RBACPermissions.CommandTele)]
        private static bool HandleTeleCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var me = handler.GetPlayer();

            var tele = handler.ExtractGameTeleFromLink(args);

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
        private static bool HandleTeleAddCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var player = handler.GetSession().GetPlayer();
            if (player == null)
                return false;

            var name = args.NextString();

            if (Global.ObjectMgr.GetGameTele(name) != null)
            {
                handler.SendSysMessage(CypherStrings.CommandTpAlreadyexist);
                return false;
            }

            var tele = new GameTele();
            tele.posX = player.GetPositionX();
            tele.posY = player.GetPositionY();
            tele.posZ = player.GetPositionZ();
            tele.orientation = player.GetOrientation();
            tele.mapId = player.GetMapId();
            tele.name = name;
            tele.nameLow = name.ToLowerInvariant();

            if (Global.ObjectMgr.AddGameTele(tele))
            {
                handler.SendSysMessage(CypherStrings.CommandTpAdded);
            }
            else
            {
                handler.SendSysMessage(CypherStrings.CommandTpAddedError);
                return false;
            }

            return true;
        }

        [Command("del", RBACPermissions.CommandTeleDel, true)]
        private static bool HandleTeleDelCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            // id, or string, or [name] Shift-click form |color|Htele:id|h[name]|h|r
            var tele = handler.ExtractGameTeleFromLink(args);
            if (tele == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTeleNotfound);
                return false;
            }

            Global.ObjectMgr.DeleteGameTele(tele.name);
            handler.SendSysMessage(CypherStrings.CommandTpDeleted);
            return true;
        }

        [Command("group", RBACPermissions.CommandTeleGroup)]
        private static bool HandleTeleGroupCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var target = handler.GetSelectedPlayer();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            // id, or string, or [name] Shift-click form |color|Htele:id|h[name]|h|r
            var tele = handler.ExtractGameTeleFromLink(args);
            if (tele == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTeleNotfound);
                return false;
            }

            var map = CliDB.MapStorage.LookupByKey(tele.mapId);
            if (map == null || map.IsBattlegroundOrArena())
            {
                handler.SendSysMessage(CypherStrings.CannotTeleToBg);
                return false;
            }

            var nameLink = handler.GetNameLink(target);

            var grp = target.GetGroup();
            if (!grp)
            {
                handler.SendSysMessage(CypherStrings.NotInGroup, nameLink);
                return false;
            }

            for (var refe = grp.GetFirstMember(); refe != null; refe = refe.Next())
            {
                var player = refe.GetSource();
                if (!player || !player.GetSession())
                    continue;

                // check online security
                if (handler.HasLowerSecurity(player, ObjectGuid.Empty))
                    return false;

                var plNameLink = handler.GetNameLink(player);

                if (player.IsBeingTeleported())
                {
                    handler.SendSysMessage(CypherStrings.IsTeleported, plNameLink);
                    continue;
                }

                handler.SendSysMessage(CypherStrings.TeleportingTo, plNameLink, "", tele.name);
                if (handler.NeedReportToTarget(player))
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
        private static bool HandleTeleNameCommand(StringArguments args, CommandHandler handler)
        {
            handler.ExtractOptFirstArg(args, out var nameStr, out var teleStr);
            if (teleStr.IsEmpty())
                return false;

            if (!handler.ExtractPlayerTarget(new StringArguments(nameStr), out var target, out var targetGuid, out var targetName))
                return false;

            if (teleStr.Equals("$home"))    // References target's homebind
            {
                if (target)
                    target.TeleportTo(target.GetHomebind());
                else
                {
                    var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_HOMEBIND);
                    stmt.AddValue(0, targetGuid.GetCounter());
                    var result = DB.Characters.Query(stmt);

                    if (!result.IsEmpty())
                    {
                        var loc = new WorldLocation(result.Read<ushort>(0), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), 0.0f);
                        uint zoneId = result.Read<ushort>(1);

                        Player.SavePositionInDB(loc, zoneId, targetGuid, null);
                    }
                }

                return true;
            }

            // id, or string, or [name] Shift-click form |color|Htele:id|h[name]|h|r
            var tele = handler.ExtractGameTeleFromLink(new StringArguments(teleStr));
            if (tele == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTeleNotfound);
                return false;
            }

            if (target)
            {
                // check online security
                if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                    return false;

                var chrNameLink = handler.PlayerLink(targetName);

                if (target.IsBeingTeleported() == true)
                {
                    handler.SendSysMessage(CypherStrings.IsTeleported, chrNameLink);
                    return false;
                }

                handler.SendSysMessage(CypherStrings.TeleportingTo, chrNameLink, "", tele.name);
                if (handler.NeedReportToTarget(target))
                    target.SendSysMessage(CypherStrings.TeleportedToBy, handler.GetNameLink());

                // stop flight if need
                if (target.IsInFlight())
                {
                    target.GetMotionMaster().MovementExpired();
                    target.CleanupAfterTaxiFlight();
                }
                // save only in non-flight case
                else
                    target.SaveRecallPosition();

                target.TeleportTo(tele.mapId, tele.posX, tele.posY, tele.posZ, tele.orientation);
            }
            else
            {
                // check offline security
                if (handler.HasLowerSecurity(null, targetGuid))
                    return false;

                var nameLink = handler.PlayerLink(targetName);

                handler.SendSysMessage(CypherStrings.TeleportingTo, nameLink, handler.GetCypherString(CypherStrings.Offline), tele.name);

                Player.SavePositionInDB(new WorldLocation(tele.mapId, tele.posX, tele.posY, tele.posZ, tele.orientation),
                    Global.MapMgr.GetZoneId(PhasingHandler.EmptyPhaseShift, tele.mapId, tele.posX, tele.posY, tele.posZ), targetGuid, null);
            }

            return true;
        }
    }
}
