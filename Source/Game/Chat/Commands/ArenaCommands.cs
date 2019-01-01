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
using Game.Arenas;
using Game.Entities;

namespace Game.Chat
{
    [CommandGroup("arena", RBACPermissions.CommandArena)]
    class ArenaCommands
    {
        [Command("create", RBACPermissions.CommandArenaCreate, true)]
        static bool HandleArenaCreateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            Player target;
            if (!handler.extractPlayerTarget(args[0] != '"' ? args : null, out target))
                return false;

            string name = handler.extractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(name))
                return false;

            byte type = args.NextByte();
            if (type == 0)
                return false;

            if (Global.ArenaTeamMgr.GetArenaTeamByName(name) != null)
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorNameExists, name);
                return false;
            }

            if (type == 2 || type == 3 || type == 5)
            {
                if (Player.GetArenaTeamIdFromDB(target.GetGUID(), type) != 0)
                {
                    handler.SendSysMessage(CypherStrings.ArenaErrorSize, target.GetName());
                    return false;
                }

                ArenaTeam arena = new ArenaTeam();

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
        static bool HandleArenaDisbandCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            uint teamId = args.NextUInt32();
            if (teamId == 0)
                return false;

            ArenaTeam arena = Global.ArenaTeamMgr.GetArenaTeamById(teamId);

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

            string name = arena.GetName();
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
        static bool HandleArenaRenameCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string oldArenaStr = handler.extractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(oldArenaStr))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            string newArenaStr = handler.extractQuotedArg(args.NextString());
            if (string.IsNullOrEmpty(newArenaStr))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            ArenaTeam arena = Global.ArenaTeamMgr.GetArenaTeamByName(oldArenaStr);
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
        static bool HandleArenaCaptainCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string idStr;
            string nameStr;
            handler.extractOptFirstArg(args, out idStr, out nameStr);
            if (string.IsNullOrEmpty(idStr))
                return false;

            if (!uint.TryParse(idStr, out uint teamId) || teamId == 0)
                return false;

            Player target;
            ObjectGuid targetGuid;
            if (!handler.extractPlayerTarget(new StringArguments(nameStr), out target, out targetGuid))
                return false;

            ArenaTeam arena = Global.ArenaTeamMgr.GetArenaTeamById(teamId);

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

            CharacterInfo oldCaptainNameData = Global.WorldMgr.GetCharacterInfo(arena.GetCaptain());
            if (oldCaptainNameData == null)
                return false;

            handler.SendSysMessage(CypherStrings.ArenaCaptain, arena.GetName(), arena.GetId(), oldCaptainNameData.Name, target.GetName());
            if (handler.GetSession() != null)
                Log.outDebug(LogFilter.Arena, "GameMaster: {0} [GUID: {1}] promoted player: {2} [GUID: {3}] to leader of arena team \"{4}\"[Id: {5}]",
                    handler.GetSession().GetPlayer().GetName(), handler.GetSession().GetPlayer().GetGUID().ToString(), target.GetName(), target.GetGUID().ToString(), arena.GetName(), arena.GetId());
            else
                Log.outDebug(LogFilter.Arena, "Console: promoted player: {0} [GUID: {1}] to leader of arena team \"{2}\"[Id: {3}]",
                    target.GetName(), target.GetGUID().ToString(), arena.GetName(), arena.GetId());

            return true;
        }

        [Command("info", RBACPermissions.CommandArenaInfo, true)]
        static bool HandleArenaInfoCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            uint teamId = args.NextUInt32();
            if (teamId == 0)
                return false;

            ArenaTeam arena = Global.ArenaTeamMgr.GetArenaTeamById(teamId);

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
        static bool HandleArenaLookupCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string name = args.NextString().ToLower();

            bool found = false;
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
