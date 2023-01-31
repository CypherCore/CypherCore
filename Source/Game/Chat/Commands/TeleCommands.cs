// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;

namespace Game.Chat
{
    [CommandGroup("tele")]
    internal class TeleCommands
    {
        [CommandGroup("Name")]
        private class TeleNameCommands
        {
            [CommandGroup("npc")]
            private class TeleNameNpcCommands
            {
                [Command("Guid", RBACPermissions.CommandTeleName, true)]
                private static bool HandleTeleNameNpcSpawnIdCommand(CommandHandler handler, PlayerIdentifier player, ulong spawnId)
                {
                    if (player == null)
                        return false;

                    CreatureData spawnpoint = Global.ObjectMgr.GetCreatureData(spawnId);

                    if (spawnpoint == null)
                    {
                        handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);

                        return false;
                    }

                    CreatureTemplate creatureTemplate = Global.ObjectMgr.GetCreatureTemplate(spawnpoint.Id);

                    return DoNameTeleport(handler, player, spawnpoint.MapId, spawnpoint.SpawnPoint, creatureTemplate.Name);
                }

                [Command("Id", RBACPermissions.CommandTeleName, true)]
                private static bool HandleTeleNameNpcIdCommand(CommandHandler handler, PlayerIdentifier player, uint creatureId)
                {
                    if (player == null)
                        return false;

                    CreatureData spawnpoint = null;

                    foreach (var (id, creatureData) in Global.ObjectMgr.GetAllCreatureData())
                    {
                        if (id != creatureId)
                            continue;

                        if (spawnpoint == null)
                        {
                            spawnpoint = creatureData;
                        }
                        else
                        {
                            handler.SendSysMessage(CypherStrings.CommandGocreatmultiple);

                            break;
                        }
                    }

                    if (spawnpoint == null)
                    {
                        handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);

                        return false;
                    }

                    CreatureTemplate creatureTemplate = Global.ObjectMgr.GetCreatureTemplate(creatureId);

                    return DoNameTeleport(handler, player, spawnpoint.MapId, spawnpoint.SpawnPoint, creatureTemplate.Name);
                }

                [Command("Name", RBACPermissions.CommandTeleName, true)]
                private static bool HandleTeleNameNpcNameCommand(CommandHandler handler, PlayerIdentifier player, Tail name)
                {
                    string normalizedName = name;

                    if (player == null)
                        return false;

                    WorldDatabase.EscapeString(ref normalizedName);

                    SQLResult result = DB.World.Query($"SELECT c.position_x, c.position_y, c.position_z, c.orientation, c.map, ct.Name FROM creature c INNER JOIN creature_template ct ON c.Id = ct.entry WHERE ct.Name LIKE '{normalizedName}'");

                    if (result.IsEmpty())
                    {
                        handler.SendSysMessage(CypherStrings.CommandGocreatnotfound);

                        return false;
                    }

                    if (result.NextRow())
                        handler.SendSysMessage(CypherStrings.CommandGocreatmultiple);

                    return DoNameTeleport(handler, player, result.Read<ushort>(4), new Position(result.Read<float>(0), result.Read<float>(1), result.Read<float>(2), result.Read<float>(3)), result.Read<string>(5));
                }
            }

            [Command("", RBACPermissions.CommandTeleName, true)]
            private static bool HandleTeleNameCommand(CommandHandler handler, [OptionalArg] PlayerIdentifier player, [VariantArg(typeof(GameTele), typeof(string))] object where)
            {
                if (player == null)
                    player = PlayerIdentifier.FromTargetOrSelf(handler);

                if (player == null)
                    return false;

                Player target = player.GetConnectedPlayer();

                if (where is string &&
                    where.Equals("$home")) // References Target's _homebind
                {
                    if (target)
                    {
                        target.TeleportTo(target.GetHomebind());
                    }
                    else
                    {
                        PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_HOMEBIND);
                        stmt.AddValue(0, player.GetGUID().GetCounter());
                        SQLResult result = DB.Characters.Query(stmt);

                        if (!result.IsEmpty())
                        {
                            WorldLocation loc = new(result.Read<ushort>(0), result.Read<float>(2), result.Read<float>(3), result.Read<float>(4), 0.0f);
                            uint zoneId = result.Read<ushort>(1);

                            Player.SavePositionInDB(loc, zoneId, player.GetGUID());
                        }
                    }

                    return true;
                }

                // Id, or string, or [Name] Shift-click form |color|Htele:Id|h[Name]|h|r
                GameTele tele = where as GameTele;

                return DoNameTeleport(handler, player, tele.MapId, new Position(tele.PosX, tele.PosY, tele.PosZ, tele.Orientation), tele.Name);
            }
        }

        [Command("", RBACPermissions.CommandTele)]
        private static bool HandleTeleCommand(CommandHandler handler, GameTele tele)
        {
            if (tele == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTeleNotfound);

                return false;
            }

            Player player = handler.GetPlayer();

            if (player.IsInCombat() &&
                !handler.GetSession().HasPermission(RBACPermissions.CommandTeleName))
            {
                handler.SendSysMessage(CypherStrings.YouInCombat);

                return false;
            }

            var map = CliDB.MapStorage.LookupByKey(tele.MapId);

            if (map == null ||
                (map.IsBattlegroundOrArena() && (player.GetMapId() != tele.MapId || !player.IsGameMaster())))
            {
                handler.SendSysMessage(CypherStrings.CannotTeleToBg);

                return false;
            }

            // stop flight if need
            if (player.IsInFlight())
                player.FinishTaxiFlight();
            else
                player.SaveRecallPosition(); // save only in non-flight case

            player.TeleportTo(tele.MapId, tele.PosX, tele.PosY, tele.PosZ, tele.Orientation);

            return true;
        }

        [Command("add", RBACPermissions.CommandTeleAdd)]
        private static bool HandleTeleAddCommand(CommandHandler handler, string name)
        {
            Player player = handler.GetPlayer();

            if (player == null)
                return false;

            if (Global.ObjectMgr.GetGameTeleExactName(name) != null)
            {
                handler.SendSysMessage(CypherStrings.CommandTpAlreadyexist);

                return false;
            }

            GameTele tele = new();
            tele.PosX = player.GetPositionX();
            tele.PosY = player.GetPositionY();
            tele.PosZ = player.GetPositionZ();
            tele.Orientation = player.GetOrientation();
            tele.MapId = player.GetMapId();
            tele.Name = name;
            tele.NameLow = name.ToLowerInvariant();

            if (Global.ObjectMgr.AddGameTele(tele))
            {
                handler.SendSysMessage(CypherStrings.CommandTpAdded);
            }
            else
            {
                handler.SendSysMessage(CypherStrings.CommandTpAddedErr);

                return false;
            }

            return true;
        }

        [Command("del", RBACPermissions.CommandTeleDel, true)]
        private static bool HandleTeleDelCommand(CommandHandler handler, GameTele tele)
        {
            if (tele == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTeleNotfound);

                return false;
            }

            Global.ObjectMgr.DeleteGameTele(tele.Name);
            handler.SendSysMessage(CypherStrings.CommandTpDeleted);

            return true;
        }

        [Command("group", RBACPermissions.CommandTeleGroup)]
        private static bool HandleTeleGroupCommand(CommandHandler handler, GameTele tele)
        {
            if (tele == null)
            {
                handler.SendSysMessage(CypherStrings.CommandTeleNotfound);

                return false;
            }

            Player target = handler.GetSelectedPlayer();

            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);

                return false;
            }

            // check online security
            if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                return false;

            MapRecord map = CliDB.MapStorage.LookupByKey(tele.MapId);

            if (map == null ||
                map.IsBattlegroundOrArena())
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

            for (GroupReference refe = grp.GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player player = refe.GetSource();

                if (!player ||
                    !player.Session)
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

                handler.SendSysMessage(CypherStrings.TeleportingTo, plNameLink, "", tele.Name);

                if (handler.NeedReportToTarget(player))
                    player.SendSysMessage(CypherStrings.TeleportedToBy, nameLink);

                // stop flight if need
                if (player.IsInFlight())
                    player.FinishTaxiFlight();
                else
                    player.SaveRecallPosition(); // save only in non-flight case

                player.TeleportTo(tele.MapId, tele.PosX, tele.PosY, tele.PosZ, tele.Orientation);
            }

            return true;
        }

        private static bool DoNameTeleport(CommandHandler handler, PlayerIdentifier player, uint mapId, Position pos, string locationName)
        {
            if (!GridDefines.IsValidMapCoord(mapId, pos) ||
                Global.ObjectMgr.IsTransportMap(mapId))
            {
                handler.SendSysMessage(CypherStrings.InvalidTargetCoord, pos.GetPositionX(), pos.GetPositionY(), mapId);

                return false;
            }

            Player target = player.GetConnectedPlayer();

            if (target != null)
            {
                // check online security
                if (handler.HasLowerSecurity(target, ObjectGuid.Empty))
                    return false;

                string chrNameLink = handler.PlayerLink(target.GetName());

                if (target.IsBeingTeleported() == true)
                {
                    handler.SendSysMessage(CypherStrings.IsTeleported, chrNameLink);

                    return false;
                }

                handler.SendSysMessage(CypherStrings.TeleportingTo, chrNameLink, "", locationName);

                if (handler.NeedReportToTarget(target))
                    target.SendSysMessage(CypherStrings.TeleportedToBy, handler.GetNameLink());

                // stop flight if need
                if (target.IsInFlight())
                    target.FinishTaxiFlight();
                else
                    target.SaveRecallPosition(); // save only in non-flight case

                target.TeleportTo(new WorldLocation(mapId, pos));
            }
            else
            {
                // check offline security
                if (handler.HasLowerSecurity(null, player.GetGUID()))
                    return false;

                string nameLink = handler.PlayerLink(player.GetName());

                handler.SendSysMessage(CypherStrings.TeleportingTo, nameLink, handler.GetCypherString(CypherStrings.Offline), locationName);

                Player.SavePositionInDB(new WorldLocation(mapId, pos), Global.TerrainMgr.GetZoneId(PhasingHandler.EmptyPhaseShift, new WorldLocation(mapId, pos)), player.GetGUID(), null);
            }

            return true;
        }
    }
}