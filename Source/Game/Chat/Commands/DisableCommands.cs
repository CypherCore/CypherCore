// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using System;

namespace Game.Chat.Commands
{
    [CommandGroup("disable")]
    class DisableCommands
    {
        [CommandGroup("add")]
        class DisableAddCommands
        {
            static bool HandleAddDisables(CommandHandler handler, DisableType disableType, uint entry, OptionalArg<uint> flags, Tail disableComment)
            {
                switch (disableType)
                {
                    case DisableType.Spell:
                    {
                        if (!Global.SpellMgr.HasSpellInfo(entry, Difficulty.None))
                        {
                            handler.SendSysMessage(CypherStrings.CommandNospellfound);
                            return false;
                        }
                        break;
                    }
                    case DisableType.Quest:
                    {
                        if (Global.ObjectMgr.GetQuestTemplate(entry) == null)
                        {
                            handler.SendSysMessage(CypherStrings.CommandNoquestfound, entry);
                            return false;
                        }
                        break;
                    }
                    case DisableType.Map:
                    {
                        if (!CliDB.MapStorage.ContainsKey(entry))
                        {
                            handler.SendSysMessage(CypherStrings.CommandNomapfound);
                            return false;
                        }
                        break;
                    }
                    case DisableType.Battleground:
                    {
                        if (!CliDB.BattlemasterListStorage.ContainsKey(entry))
                        {
                            handler.SendSysMessage(CypherStrings.CommandNoBattlegroundFound);
                            return false;
                        }
                        break;
                    }
                    case DisableType.Criteria:
                    {
                        if (Global.CriteriaMgr.GetCriteria(entry) == null)
                        {
                            handler.SendSysMessage(CypherStrings.CommandNoAchievementCriteriaFound);
                            return false;
                        }
                        break;
                    }
                    case DisableType.OutdoorPVP:
                    {
                        if (entry > (int)OutdoorPvPTypes.Max)
                        {
                            handler.SendSysMessage(CypherStrings.CommandNoOutdoorPvpForund);
                            return false;
                        }
                        break;
                    }
                    case DisableType.VMAP:
                    {
                        if (!CliDB.MapStorage.ContainsKey(entry))
                        {
                            handler.SendSysMessage(CypherStrings.CommandNomapfound);
                            return false;
                        }
                        break;
                    }
                    case DisableType.MMAP:
                    {
                        if (!CliDB.MapStorage.ContainsKey(entry))
                        {
                            handler.SendSysMessage(CypherStrings.CommandNomapfound);
                            return false;
                        }
                        break;
                    }
                    case DisableType.LFGMap:
                    {
                        if (!CliDB.MapStorage.ContainsKey(entry))
                        {
                            handler.SendSysMessage(CypherStrings.CommandNomapfound);
                            return false;
                        }
                        break;
                    }
                    default:
                        break;
                }

                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_DISABLES);
                stmt.AddValue(0, entry);
                stmt.AddValue(1, (byte)disableType);
                SQLResult result = DB.World.Query(stmt);
                if (!result.IsEmpty())
                {
                    handler.SendSysMessage($"This {disableType} (Id: {entry}) is already disabled.");
                    return false;
                }

                stmt = WorldDatabase.GetPreparedStatement(WorldStatements.INS_DISABLES);
                stmt.AddValue(0, entry);
                stmt.AddValue(1, (byte)disableType);
                stmt.AddValue(2, flags.GetValueOrDefault(0));
                stmt.AddValue(3, disableComment);
                DB.World.Execute(stmt);

                handler.SendSysMessage($"Add Disabled {disableType} (Id: {entry}) for reason {disableComment}");
                return true;
            }

            [Command("spell", RBACPermissions.CommandDisableAddSpell, true)]
            static bool HandleAddDisableSpellCommand(CommandHandler handler, uint entry, OptionalArg<uint> flags, Tail disableComment)
            {
                return HandleAddDisables(handler, DisableType.Spell, entry, flags, disableComment);
            }

            [Command("quest", RBACPermissions.CommandDisableAddQuest, true)]
            static bool HandleAddDisableQuestCommand(CommandHandler handler, uint entry, OptionalArg<uint> flags, Tail disableComment)
            {
                return HandleAddDisables(handler, DisableType.Quest, entry, flags, disableComment);
            }

            [Command("map", RBACPermissions.CommandDisableAddMap, true)]
            static bool HandleAddDisableMapCommand(CommandHandler handler, uint entry, OptionalArg<uint> flags, Tail disableComment)
            {
                return HandleAddDisables(handler, DisableType.Map, entry, flags, disableComment);
            }

            [Command("Battleground", RBACPermissions.CommandDisableAddBattleground, true)]
            static bool HandleAddDisableBattlegroundCommand(CommandHandler handler, uint entry, uint flags, Tail disableComment)
            {
                return HandleAddDisables(handler, DisableType.Battleground, entry, flags, disableComment);
            }

            [Command("criteria", RBACPermissions.CommandDisableAddCriteria, true)]
            static bool HandleAddDisableCriteriaCommand(CommandHandler handler, uint entry, uint flags, Tail disableComment)
            {
                return HandleAddDisables(handler, DisableType.Criteria, entry, flags, disableComment);
            }

            [Command("outdoorpvp", RBACPermissions.CommandDisableAddOutdoorpvp, true)]
            static bool HandleAddDisableOutdoorPvPCommand(CommandHandler handler, uint entry, uint flags, Tail disableComment)
            {
                return HandleAddDisables(handler, DisableType.OutdoorPVP, entry, flags, disableComment);
            }

            [Command("vmap", RBACPermissions.CommandDisableAddVmap, true)]
            static bool HandleAddDisableVmapCommand(CommandHandler handler, uint entry, uint flags, Tail disableComment)
            {
                return HandleAddDisables(handler, DisableType.VMAP, entry, flags, disableComment);
            }

            [Command("mmap", RBACPermissions.CommandDisableAddMmap, true)]
            static bool HandleAddDisableMMapCommand(CommandHandler handler, uint entry, uint flags, Tail disableComment)
            {
                return HandleAddDisables(handler, DisableType.MMAP, entry, flags, disableComment);
            }
        }

        [CommandGroup("remove")]
        class DisableRemoveCommands
        {
            static bool HandleRemoveDisables(CommandHandler handler, DisableType disableType, uint entry)
            {
                PreparedStatement stmt = WorldDatabase.GetPreparedStatement(WorldStatements.SEL_DISABLES);
                stmt.AddValue(0, entry);
                stmt.AddValue(1, (byte)disableType);
                SQLResult result = DB.World.Query(stmt);
                if (result.IsEmpty())
                {
                    handler.SendSysMessage($"This {disableType} (Id: {entry}) is not disabled.");
                    return false;
                }

                stmt = WorldDatabase.GetPreparedStatement(WorldStatements.DEL_DISABLES);
                stmt.AddValue(0, entry);
                stmt.AddValue(1, (byte)disableType);
                DB.World.Execute(stmt);

                handler.SendSysMessage($"Remove Disabled {disableType} (Id: {entry})");
                return true;
            }

            [Command("spell", RBACPermissions.CommandDisableRemoveSpell, true)]
            static bool HandleRemoveDisableSpellCommand(CommandHandler handler, uint entry)
            {
                return HandleRemoveDisables(handler, DisableType.Spell, entry);
            }

            [Command("quest", RBACPermissions.CommandDisableRemoveQuest, true)]
            static bool HandleRemoveDisableQuestCommand(CommandHandler handler, uint entry)
            {
                return HandleRemoveDisables(handler, DisableType.Quest, entry);
            }

            [Command("map", RBACPermissions.CommandDisableRemoveMap, true)]
            static bool HandleRemoveDisableMapCommand(CommandHandler handler, uint entry)
            {
                return HandleRemoveDisables(handler, DisableType.Map, entry);
            }

            [Command("Battleground", RBACPermissions.CommandDisableRemoveBattleground, true)]
            static bool HandleRemoveDisableBattlegroundCommand(CommandHandler handler, uint entry)
            {
                return HandleRemoveDisables(handler, DisableType.Battleground, entry);
            }

            [Command("criteria", RBACPermissions.CommandDisableRemoveCriteria, true)]
            static bool HandleRemoveDisableCriteriaCommand(CommandHandler handler, uint entry)
            {
                return HandleRemoveDisables(handler, DisableType.Criteria, entry);
            }

            [Command("outdoorpvp", RBACPermissions.CommandDisableRemoveOutdoorpvp, true)]
            static bool HandleRemoveDisableOutdoorPvPCommand(CommandHandler handler, uint entry)
            {
                return HandleRemoveDisables(handler, DisableType.OutdoorPVP, entry);
            }

            [Command("vmap", RBACPermissions.CommandDisableRemoveVmap, true)]
            static bool HandleRemoveDisableVmapCommand(CommandHandler handler, uint entry)
            {
                return HandleRemoveDisables(handler, DisableType.VMAP, entry);
            }

            [Command("mmap", RBACPermissions.CommandDisableRemoveMmap, true)]
            static bool HandleRemoveDisableMMapCommand(CommandHandler handler, uint entry)
            {
                return HandleRemoveDisables(handler, DisableType.MMAP, entry);
            }
        }
    }
}
