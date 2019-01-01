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
using Framework.Database;
using Framework.IO;
using Game.DataStorage;

namespace Game.Chat.Commands
{
    [CommandGroup("disable", RBACPermissions.CommandDisable)]
    class DisableCommands
    {
        [CommandGroup("add", RBACPermissions.CommandDisableAdd, true)]
        class DisableAddCommands
        {
            static bool HandleAddDisables(StringArguments args, CommandHandler handler, DisableType disableType)
            {
                uint entry = args.NextUInt32();
                if (entry == 0)
                    return false;

                uint flags = args.NextUInt32();

                string disableComment = args.NextString("");
                if (string.IsNullOrEmpty(disableComment))
                    return false;

                string disableTypeStr = "";
                switch (disableType)
                {
                    case DisableType.Spell:
                        {
                            if (!Global.SpellMgr.HasSpellInfo(entry))
                            {
                                handler.SendSysMessage(CypherStrings.CommandNospellfound);
                                return false;
                            }
                            disableTypeStr = "spell";
                            break;
                        }
                    case DisableType.Quest:
                        {
                            if (Global.ObjectMgr.GetQuestTemplate(entry) == null)
                            {
                                handler.SendSysMessage(CypherStrings.CommandNoquestfound, entry);
                                return false;
                            }
                            disableTypeStr = "quest";
                            break;
                        }
                    case DisableType.Map:
                        {
                            if (!CliDB.MapStorage.ContainsKey(entry))
                            {
                                handler.SendSysMessage(CypherStrings.CommandNomapfound);
                                return false;
                            }
                            disableTypeStr = "map";
                            break;
                        }
                    case DisableType.Battleground:
                        {
                            if (!CliDB.BattlemasterListStorage.ContainsKey(entry))
                            {
                                handler.SendSysMessage(CypherStrings.CommandNoBattlegroundFound);
                                return false;
                            }
                            disableTypeStr = "Battleground";
                            break;
                        }
                    case DisableType.Criteria:
                        {
                            if (Global.CriteriaMgr.GetCriteria(entry) == null)
                            {
                                handler.SendSysMessage(CypherStrings.CommandNoAchievementCriteriaFound);
                                return false;
                            }
                            disableTypeStr = "criteria";
                            break;
                        }
                    case DisableType.OutdoorPVP:
                        {
                            if (entry > (int)OutdoorPvPTypes.Max)
                            {
                                handler.SendSysMessage(CypherStrings.CommandNoOutdoorPvpForund);
                                return false;
                            }
                            disableTypeStr = "outdoorpvp";
                            break;
                        }
                    case DisableType.VMAP:
                        {
                            if (!CliDB.MapStorage.ContainsKey(entry))
                            {
                                handler.SendSysMessage(CypherStrings.CommandNomapfound);
                                return false;
                            }
                            disableTypeStr = "vmap";
                            break;
                        }
                    case DisableType.MMAP:
                        {
                            if (!CliDB.MapStorage.ContainsKey(entry))
                            {
                                handler.SendSysMessage(CypherStrings.CommandNomapfound);
                                return false;
                            }
                            disableTypeStr = "mmap";
                            break;
                        }
                    default:
                        break;
                }

                PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_DISABLES);
                stmt.AddValue(0, entry);
                stmt.AddValue(1, disableType);
                SQLResult result = DB.World.Query(stmt);
                if (!result.IsEmpty())
                {
                    handler.SendSysMessage("This {0} (Id: {1}) is already disabled.", disableTypeStr, entry);
                    return false;
                }

                stmt = DB.World.GetPreparedStatement(WorldStatements.INS_DISABLES);
                stmt.AddValue(0, entry);
                stmt.AddValue(1, disableType);
                stmt.AddValue(2, flags);
                stmt.AddValue(3, disableComment);
                DB.World.Execute(stmt);

                handler.SendSysMessage("Add Disabled {0} (Id: {1}) for reason {2}", disableTypeStr, entry, disableComment);
                return true;
            }

            [Command("spell", RBACPermissions.CommandDisableAddSpell, true)]
            static bool HandleAddDisableSpellCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleAddDisables(args, handler, DisableType.Spell);
            }

            [Command("quest", RBACPermissions.CommandDisableAddQuest, true)]
            static bool HandleAddDisableQuestCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleAddDisables(args, handler, DisableType.Quest);
            }

            [Command("map", RBACPermissions.CommandDisableAddMap, true)]
            static bool HandleAddDisableMapCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleAddDisables(args, handler, DisableType.Map);
            }

            [Command("Battleground", RBACPermissions.CommandDisableAddBattleground, true)]
            static bool HandleAddDisableBattlegroundCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleAddDisables(args, handler, DisableType.Battleground);
            }

            [Command("criteria", RBACPermissions.CommandDisableAddCriteria, true)]
            static bool HandleAddDisableCriteriaCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleAddDisables(args, handler, DisableType.Criteria);
            }

            [Command("outdoorpvp", RBACPermissions.CommandDisableAddOutdoorpvp, true)]
            static bool HandleAddDisableOutdoorPvPCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                HandleAddDisables(args, handler, DisableType.OutdoorPVP);
                return true;
            }

            [Command("vmap", RBACPermissions.CommandDisableAddVmap, true)]
            static bool HandleAddDisableVmapCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleAddDisables(args, handler, DisableType.VMAP);
            }

            [Command("mmap", RBACPermissions.CommandDisableAddMmap, true)]
            static bool HandleAddDisableMMapCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleAddDisables(args, handler, DisableType.MMAP);
            }
        }

        [CommandGroup("remove", RBACPermissions.CommandDisableRemove, true)]
        class DisableRemoveCommands
        {
            static bool HandleRemoveDisables(StringArguments args, CommandHandler handler, DisableType disableType)
            {
                if (!uint.TryParse(args.NextString(), out uint entry) || entry == 0)
                    return false;

                string disableTypeStr = "";
                switch (disableType)
                {
                    case DisableType.Spell:
                        disableTypeStr = "spell";
                        break;
                    case DisableType.Quest:
                        disableTypeStr = "quest";
                        break;
                    case DisableType.Map:
                        disableTypeStr = "map";
                        break;
                    case DisableType.Battleground:
                        disableTypeStr = "Battleground";
                        break;
                    case DisableType.Criteria:
                        disableTypeStr = "criteria";
                        break;
                    case DisableType.OutdoorPVP:
                        disableTypeStr = "outdoorpvp";
                        break;
                    case DisableType.VMAP:
                        disableTypeStr = "vmap";
                        break;
                    case DisableType.MMAP:
                        disableTypeStr = "mmap";
                        break;
                }

                PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_DISABLES);
                stmt.AddValue(0, entry);
                stmt.AddValue(1, disableType);
                SQLResult result = DB.World.Query(stmt);
                if (result.IsEmpty())
                {
                    handler.SendSysMessage("This {0} (Id: {1}) is not disabled.", disableTypeStr, entry);
                    return false;
                }

                stmt = DB.World.GetPreparedStatement(WorldStatements.DEL_DISABLES);
                stmt.AddValue(0, entry);
                stmt.AddValue(1, disableType);
                DB.World.Execute(stmt);

                handler.SendSysMessage("Remove Disabled {0} (Id: {1})", disableTypeStr, entry);
                return true;
            }

            [Command("spell", RBACPermissions.CommandDisableRemoveSpell, true)]
            static bool HandleRemoveDisableSpellCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleRemoveDisables(args, handler, DisableType.Spell);
            }

            [Command("quest", RBACPermissions.CommandDisableRemoveQuest, true)]
            static bool HandleRemoveDisableQuestCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleRemoveDisables(args, handler, DisableType.Quest);
            }

            [Command("map", RBACPermissions.CommandDisableRemoveMap, true)]
            static bool HandleRemoveDisableMapCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleRemoveDisables(args, handler, DisableType.Map);
            }

            [Command("Battleground", RBACPermissions.CommandDisableRemoveBattleground, true)]
            static bool HandleRemoveDisableBattlegroundCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleRemoveDisables(args, handler, DisableType.Battleground);
            }

            [Command("criteria", RBACPermissions.CommandDisableRemoveCriteria, true)]
            static bool HandleRemoveDisableCriteriaCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleRemoveDisables(args, handler, DisableType.Criteria);
            }

            [Command("outdoorpvp", RBACPermissions.CommandDisableRemoveOutdoorpvp, true)]
            static bool HandleRemoveDisableOutdoorPvPCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleRemoveDisables(args, handler, DisableType.OutdoorPVP);
            }

            [Command("vmap", RBACPermissions.CommandDisableRemoveVmap, true)]
            static bool HandleRemoveDisableVmapCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleRemoveDisables(args, handler, DisableType.VMAP);
            }

            [Command("mmap", RBACPermissions.CommandDisableRemoveMmap, true)]
            static bool HandleRemoveDisableMMapCommand(StringArguments args, CommandHandler handler)
            {
                if (args.Empty())
                    return false;

                return HandleRemoveDisables(args, handler, DisableType.MMAP);
            }
        }
    }
}
