// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Arenas;
using System;

namespace Game.Chat
{
    [CommandGroup("arena")]
    class ArenaCommands
    {
        [Command("create", RBACPermissions.CommandArenaCreate, true)]
        static bool HandleArenaCreateCommand(CommandHandler handler, PlayerIdentifier captain, string name, ArenaTypes type)
        {
            if (Global.ArenaTeamMgr.GetArenaTeamByName(name) != null)
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorNameExists, name);
                return false;
            }
            
            if (captain == null)
                captain = PlayerIdentifier.FromTargetOrSelf(handler);
            if (captain == null)
                return false;

            if (Global.CharacterCacheStorage.GetCharacterArenaTeamIdByGuid(captain.GetGUID(), (byte)type) != 0)
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorSize, captain.GetName());
                return false;
            }

            ArenaTeam arena = new();

            if (!arena.Create(captain.GetGUID(), (byte)type, name, 4293102085, 101, 4293253939, 4, 4284049911))
            {
                handler.SendSysMessage(CypherStrings.BadValue);
                return false;
            }

            Global.ArenaTeamMgr.AddArenaTeam(arena);
            handler.SendSysMessage(CypherStrings.ArenaCreate, arena.GetName(), arena.GetId(), arena.GetArenaType(), arena.GetCaptain());

            return true;
        }

        [Command("disband", RBACPermissions.CommandArenaDisband, true)]
        static bool HandleArenaDisbandCommand(CommandHandler handler, uint teamId)
        {
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

            handler.SendSysMessage(CypherStrings.ArenaDisband, name, teamId);
            return true;
        }

        [Command("rename", RBACPermissions.CommandArenaRename, true)]
        static bool HandleArenaRenameCommand(CommandHandler handler, string oldName, string newName)
        {
            ArenaTeam arena = Global.ArenaTeamMgr.GetArenaTeamByName(oldName);
            if (arena == null)
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorNameNotFound, oldName);
                return false;
            }

            if (Global.ArenaTeamMgr.GetArenaTeamByName(newName) != null)
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorNameExists, oldName);
                return false;
            }

            if (arena.IsFighting())
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorCombat);
                return false;
            }

            if (!arena.SetName(newName))
            {
                handler.SendSysMessage(CypherStrings.ArenaRename, arena.GetId(), oldName, newName);
                return true;
            }

            handler.SendSysMessage(CypherStrings.BadValue);
            return false;
        }

        [Command("captain", RBACPermissions.CommandArenaCaptain)]
        static bool HandleArenaCaptainCommand(CommandHandler handler, uint teamId, PlayerIdentifier target)
        {
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

            if (target == null)
                target = PlayerIdentifier.FromTargetOrSelf(handler);
            if (target == null)
                return false;
            
            if (!arena.IsMember(target.GetGUID()))
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorNotMember, target.GetName(), arena.GetName());
                return false;
            }

            if (arena.GetCaptain() == target.GetGUID())
            {
                handler.SendSysMessage(CypherStrings.ArenaErrorCaptain, target.GetName(), arena.GetName());
                return false;
            }

            if (!Global.CharacterCacheStorage.GetCharacterNameByGuid(arena.GetCaptain(), out string oldCaptainName))
                return false;

            arena.SetCaptain(target.GetGUID());
            handler.SendSysMessage(CypherStrings.ArenaCaptain, arena.GetName(), arena.GetId(), oldCaptainName, target.GetName());

            return true;
        }

        [Command("info", RBACPermissions.CommandArenaInfo, true)]
        static bool HandleArenaInfoCommand(CommandHandler handler, uint teamId)
        {
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
        static bool HandleArenaLookupCommand(CommandHandler handler, string needle)
        {
            if (needle.IsEmpty())
                return false;

            bool found = false;
            foreach (var (_, team) in Global.ArenaTeamMgr.GetArenaTeamMap())
            {
                if (team.GetName().Equals(needle, StringComparison.OrdinalIgnoreCase))
                {
                    if (handler.GetSession() != null)
                    {
                        handler.SendSysMessage(CypherStrings.ArenaLookup, team.GetName(), team.GetId(), team.GetArenaType(), team.GetArenaType());
                        found = true;
                        continue;
                    }
                }
            }

            if (!found)
                handler.SendSysMessage(CypherStrings.ArenaErrorNameNotFound, needle);

            return true;
        }
    }
}
