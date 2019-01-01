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
using Game.Achievements;
using Game.DataStorage;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Chat
{
    [CommandGroup("reset", RBACPermissions.CommandReset, true)]
    class ResetCommands
    {
        [Command("achievements", RBACPermissions.CommandResetAchievements, true)]
        static bool HandleResetAchievementsCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            ObjectGuid targetGuid;
            if (!handler.extractPlayerTarget(args, out target, out targetGuid))
                return false;

            if (target)
                target.ResetAchievements();
            else
                PlayerAchievementMgr.DeleteFromDB(targetGuid);

            return true;
        }

        [Command("honor", RBACPermissions.CommandResetHonor, true)]
        static bool HandleResetHonorCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            if (!handler.extractPlayerTarget(args, out target))
                return false;

            target.SetUInt32Value(ActivePlayerFields.Kills, 0);
            target.SetUInt32Value(ActivePlayerFields.LifetimeHonorableKills, 0);
            target.UpdateCriteria(CriteriaTypes.EarnHonorableKill);

            return true;
        }

        static bool HandleResetStatsOrLevelHelper(Player player)
        {
            ChrClassesRecord classEntry = CliDB.ChrClassesStorage.LookupByKey(player.GetClass());
            if (classEntry == null)
            {
                Log.outError(LogFilter.Server, "Class {0} not found in DBC (Wrong DBC files?)", player.GetClass());
                return false;
            }

            PowerType powerType = classEntry.DisplayPower;

            // reset m_form if no aura
            if (!player.HasAuraType(AuraType.ModShapeshift))
                player.SetShapeshiftForm(ShapeShiftForm.None);

            player.setFactionForRace(player.GetRace());
            player.SetUInt32Value(UnitFields.DisplayPower, (uint)powerType);

            // reset only if player not in some form;
            if (player.GetShapeshiftForm() == ShapeShiftForm.None)
                player.InitDisplayIds();

            player.SetByteValue(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, (byte)UnitBytes2Flags.PvP);

            player.SetUInt32Value(UnitFields.Flags, (uint)UnitFlags.PvpAttackable);

            //-1 is default value
            player.SetUInt32Value(ActivePlayerFields.WatchedFactionIndex, 0xFFFFFFFF);
            return true;
        }

        [Command("level", RBACPermissions.CommandResetLevel, true)]
        static bool HandleResetLevelCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            if (!handler.extractPlayerTarget(args, out target))
                return false;

            if (!HandleResetStatsOrLevelHelper(target))
                return false;

            byte oldLevel = (byte)target.getLevel();

            // set starting level
            uint startLevel = (uint)(target.GetClass() != Class.Deathknight ? WorldConfig.GetIntValue(WorldCfg.StartPlayerLevel) : WorldConfig.GetIntValue(WorldCfg.StartDeathKnightPlayerLevel));

            target._ApplyAllLevelScaleItemMods(false);
            target.SetLevel(startLevel);
            target.InitRunes();
            target.InitStatsForLevel(true);
            target.InitTaxiNodesForLevel();
            target.InitTalentForLevel();
            target.SetUInt32Value(ActivePlayerFields.Xp, 0);

            target._ApplyAllLevelScaleItemMods(true);

            // reset level for pet
            Pet pet = target.GetPet();
            if (pet)
                pet.SynchronizeLevelWithOwner();

            Global.ScriptMgr.OnPlayerLevelChanged(target, oldLevel);

            return true;
        }

        [Command("spells", RBACPermissions.CommandResetSpells, true)]
        static bool HandleResetSpellsCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            ObjectGuid targetGuid;
            string targetName;
            if (!handler.extractPlayerTarget(args, out target, out targetGuid, out targetName))
                return false;

            if (target)
            {
                target.ResetSpells();

                target.SendSysMessage(CypherStrings.ResetSpells);
                if (handler.GetSession() == null || handler.GetSession().GetPlayer() != target)
                    handler.SendSysMessage(CypherStrings.ResetSpellsOnline, handler.GetNameLink(target));
            }
            else
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                stmt.AddValue(0, (ushort)AtLoginFlags.ResetSpells);
                stmt.AddValue(1, targetGuid.GetCounter());
                DB.Characters.Execute(stmt);

                handler.SendSysMessage(CypherStrings.ResetSpellsOffline, targetName);
            }

            return true;
        }

        [Command("stats", RBACPermissions.CommandResetStats, true)]
        static bool HandleResetStatsCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            if (!handler.extractPlayerTarget(args, out target))
                return false;

            if (!HandleResetStatsOrLevelHelper(target))
                return false;

            target.InitRunes();
            target.InitStatsForLevel(true);
            target.InitTaxiNodesForLevel();
            target.InitTalentForLevel();

            return true;
        }

        [Command("talents", RBACPermissions.CommandResetTalents, true)]
        static bool HandleResetTalentsCommand(StringArguments args, CommandHandler handler)
        {
            Player target;
            ObjectGuid targetGuid;
            string targetName;

            if (!handler.extractPlayerTarget(args, out target, out targetGuid, out targetName))
            {
                /* TODO: 6.x remove/update pet talents
                // Try reset talents as Hunter Pet
                Creature* creature = handler.getSelectedCreature();
                if (!*args && creature && creature.IsPet())
                {
                    Unit* owner = creature.GetOwner();
                    if (owner && owner.GetTypeId() == TYPEID_PLAYER && creature.ToPet().IsPermanentPetFor(owner.ToPlayer()))
                    {
                        creature.ToPet().resetTalents();
                        owner.ToPlayer().SendTalentsInfoData(true);

                        ChatHandler(owner.ToPlayer().GetSession()).SendSysMessage(LANG_RESET_PET_TALENTS);
                        if (!handler.GetSession() || handler.GetSession().GetPlayer() != owner.ToPlayer())
                            handler.PSendSysMessage(LANG_RESET_PET_TALENTS_ONLINE, handler.GetNameLink(owner.ToPlayer()).c_str());
                    }
                    return true;
                }
                */

                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            if (target)
            {
                target.ResetTalents(true);
                target.ResetTalentSpecialization();
                target.SendTalentsInfoData();
                target.SendSysMessage(CypherStrings.ResetTalents);
                if (handler.GetSession() == null || handler.GetSession().GetPlayer() != target)
                    handler.SendSysMessage(CypherStrings.ResetTalentsOnline, handler.GetNameLink(target));

                /* TODO: 6.x remove/update pet talents
                Pet* pet = target.GetPet();
                Pet.resetTalentsForAllPetsOf(target, pet);
                if (pet)
                    target.SendTalentsInfoData(true);
                */
                return true;
            }
            else if (!targetGuid.IsEmpty())
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                stmt.AddValue(0, (ushort)(AtLoginFlags.None | AtLoginFlags.ResetPetTalents));
                stmt.AddValue(1, targetGuid.GetCounter());
                DB.Characters.Execute(stmt);

                string nameLink = handler.playerLink(targetName);
                handler.SendSysMessage(CypherStrings.ResetTalentsOffline, nameLink);
                return true;
            }

            handler.SendSysMessage(CypherStrings.NoCharSelected);
            return false;
        }

        [Command("all", RBACPermissions.CommandResetAll, true)]
        static bool HandleResetAllCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            string caseName = args.NextString();

            AtLoginFlags atLogin;

            // Command specially created as single command to prevent using short case names
            if (caseName == "spells")
            {
                atLogin = AtLoginFlags.ResetSpells;
                Global.WorldMgr.SendWorldText(CypherStrings.ResetallSpells);
                if (handler.GetSession() == null)
                    handler.SendSysMessage(CypherStrings.ResetallSpells);
            }
            else if (caseName == "talents")
            {
                atLogin = AtLoginFlags.ResetTalents | AtLoginFlags.ResetPetTalents;
                Global.WorldMgr.SendWorldText(CypherStrings.ResetallTalents);
                if (handler.GetSession() == null)
                    handler.SendSysMessage(CypherStrings.ResetallTalents);
            }
            else
            {
                handler.SendSysMessage(CypherStrings.ResetallUnknownCase, args);
                return false;
            }

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ALL_AT_LOGIN_FLAGS);
            stmt.AddValue(0, (ushort)atLogin);
            DB.Characters.Execute(stmt);

            var plist = Global.ObjAccessor.GetPlayers();
            foreach (var player in plist)
                player.SetAtLoginFlag(atLogin);

            return true;
        }
    }
}
