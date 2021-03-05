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
using Framework.IO;
using Game.Arenas;
using Game.Entities;

namespace Game.Chat
{
    [CommandGroup("arena", RBACPermissions.CommandArena)]
    internal class ArenaCommands
    {
        [Command("create", RBACPermissions.CommandArenaCreate, true)]
        private static bool HandleArenaCreateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target;
            if (!handler.ExtractPlayerTarget(args[0] != '"' ? args : null, out target))
                return false;

            var name = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(name))
                return false;

            var type = args.NextByte();
            if (type == 0)
                return false;

            if (Global.ArenaTeamMgr.GetArenaTeamByName(name) != null)
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorNameExists, name);
                return false;
            }

            if (type == 2 || type == 3 || type == 5)
            {
                if (Global.CharacterCacheStorage.GetCharacterArenaTeamIdByGuid(target.GetGUID(), type) != 0)
                {
                    handler.SendSysMessage(CypherStrings.ArenaErrorSize, target.GetName());
                    return false;
                }

                var arena = new ArenaTeam();

                if (!arena.Create(target.GetGUID(), type, name, 4293102085, 101, 4293253939, 4, 4284049911))
                {
                    handler.SendSysMessage(CypherStrings.BadValue);
                    return false;
                }

                Global.ArenaTeamMgr.AddArenaTeam(arena);
                handler.SendSysMessage(CypherStrings.ArenaCreate, arena.GetName(), arena.GetId(), arena.GetArenaType(), arena.GetCaptain());
            }
            else
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            return true;
        }

        [Command("disband", RBACPermissions.CommandArenaDisband, true)]
        private static bool HandleArenaDisbandCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var teamId = args.NextUInt32();
            if (teamId == 0)
                return false;

            var arena = Global.ArenaTeamMgr.GetArenaTeamById(teamId);

            if (arena == null)
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorNotFound, teamId);
                return false;
            }

            if (arena.IsFighting())
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorCombat);
                return false;
            }

            var name = arena.GetName();
            arena.Disband();
            if (handler.GetSession() != null)
                Log.outDebug(LogFilter.Arena, "GameMaster: {0} [GUID: {1}] disbanded arena team type: {2} [Id: {3}].",
                      handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(), arena.GetArenaType(), teamId);
            else
                Log.outDebug(LogFilter.Arena, "Console: disbanded arena team type: {0} [Id: {1}].", arena.GetArenaType(), teamId);

            handler.SendSysMessage(CypherStrings.ArenaDisband, name, teamId);
            return true;
        }

        [Command("rename", RBACPermissions.CommandArenaRename, true)]
        private static bool HandleArenaRenameCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var oldArenaStr = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(oldArenaStr))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            var newArenaStr = handler.ExtractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(newArenaStr))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            var arena = Global.ArenaTeamMgr.GetArenaTeamByName(oldArenaStr);
            if (arena == null)
            {
                handler.SendSysMessage(CypherStrings.AreanErrorNameNotFound, oldArenaStr);
                return false;
            }

            if (Global.ArenaTeamMgr.GetArenaTeamByName(newArenaStr) != null)
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorNameExists, oldArenaStr);
                return false;
            }

            if (arena.IsFighting())
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorCombat);
                return false;
            }

            if (!arena.SetName(newArenaStr))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            handler.SendSysMessage(CypherStrings.ArenaRename, arena.GetId(), oldArenaStr, newArenaStr);
            if (handler.GetSession() != null)
                Log.outDebug(LogFilter.Arena, "GameMaster: {0} [GUID: {1}] rename arena team \"{2}\"[Id: {3}] to \"{4}\"",
                    handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(), oldArenaStr, arena.GetId(), newArenaStr);
            else
                Log.outDebug(LogFilter.Arena, "Console: rename arena team \"{0}\"[Id: {1}] to \"{2}\"", oldArenaStr, arena.GetId(), newArenaStr);

            return true;
        }

        [Command("captain", RBACPermissions.CommandArenaCaptain)]
        private static bool HandleArenaCaptainCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string idStr;
            string nameStr;
            handler.ExtractOptFirstArg(args, out idStr, out nameStr);
            if (string.IsNullOrEmpty(idStr))
                return false;

            if (!uint.TryParse(idStr, out var teamId) || teamId == 0)
                return false;

            Player target;
            ObjectGuid targetGuid;
            if (!handler.ExtractPlayerTarget(new StringArguments(nameStr), out target, out targetGuid))
                return false;

            var arena = Global.ArenaTeamMgr.GetArenaTeamById(teamId);

            if (arena == null)
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorNotFound, teamId);
                return false;
            }

            if (!target)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotExistOrOffline, nameStr);
                return false;
            }

            if (arena.IsFighting())
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorCombat);
                return false;
            }

            if (!arena.IsMember(targetGuid))
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorNotMember, nameStr, arena.GetName());
                return false;
            }

            if (arena.GetCaptain() == targetGuid)
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorCaptain, nameStr, arena.GetName());
                return false;
            }

            arena.SetCaptain(targetGuid);

            string oldCaptainName;
            if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(arena.GetCaptain(), out oldCaptainName))
                return false;

            handler.SendSysMessage(CypherStrings.ArenaCaptain, arena.GetName(), arena.GetId(), oldCaptainName, target.GetName());
            if (handler.GetSession() != null)
                Log.outDebug(LogFilter.Arena, "GameMaster: {0} [GUID: {1}] promoted player: {2} [GUID: {3}] to leader of arena team \"{4}\"[Id: {5}]",
                    handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(), target.GetName(), target.GetGUID().ToString(), arena.GetName(), arena.GetId());
            else
                Log.outDebug(LogFilter.Arena, "Console: promoted player: {0} [GUID: {1}] to leader of arena team \"{2}\"[Id: {3}]",
                    target.GetName(), target.GetGUID().ToString(), arena.GetName(), arena.GetId());

            return true;
        }

        [Command("info", RBACPermissions.CommandArenaInfo, true)]
        private static bool HandleArenaInfoCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var teamId = args.NextUInt32();
            if (teamId == 0)
                return false;

            var arena = Global.ArenaTeamMgr.GetArenaTeamById(teamId);

            if (arena == null)
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorNotFound, teamId);
                return false;
            }

            handler.SendSysMessage(CypherStrings.ArenaInfoHeader, arena.GetName(), arena.GetId(), arena.GetRating(), arena.GetArenaType(), arena.GetArenaType());
            foreach (var member in arena.GetMembers())
                handler.SendSysMessage(CypherStrings.ArenaInfoMembers, member.Name, member.Guid, member.PersonalRating, (arena.GetCaptain() == member.Guid ? "- Captain" : ""));

            return true;
        }

        [Command("lookup", RBACPermissions.CommandArenaLookup)]
        private static bool HandleArenaLookupCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            var name = args.NextString().ToLower();

            var found = false;
            foreach (var arena in Global.ArenaTeamMgr.GetArenaTeamMap().Values)
            {
                if (arena.GetName() == name)
                {
                    if (handler.GetSession() != null)
                    {
                        handler.SendSysMessage(CypherStrings.ArenaLookup, arena.GetName(), arena.GetId(), arena.GetArenaType(), arena.GetArenaType());
                        found = true;
                        continue;
                    }
                }
            }

            if (!found)
                handler.SendSysMessage(CypherStrings.AreanErrorNameNotFound, name);

            return true;
        }
    }
}
