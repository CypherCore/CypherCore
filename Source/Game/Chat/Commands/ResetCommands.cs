// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.Achievements;
using Game.DataStorage;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Chat
{
    [CommandGroup("reset")]
    class ResetCommands
    {
        [Command("achievements", RBACPermissions.CommandResetAchievements, true)]
        static bool HandleResetAchievementsCommand(CommandHandler handler, PlayerIdentifier player)
        {
            if (player == null)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player == null)
                return false;

            if (player.IsConnected())
                player.GetConnectedPlayer().ResetAchievements();
            else
                PlayerAchievementMgr.DeleteFromDB(player.GetGUID());

            return true;
        }

        [Command("honor", RBACPermissions.CommandResetHonor, true)]
        static bool HandleResetHonorCommand(CommandHandler handler, PlayerIdentifier player)
        {
            if (player == null)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player == null || !player.IsConnected())
                return false;

            player.GetConnectedPlayer().ResetHonorStats();
            player.GetConnectedPlayer().UpdateCriteria(CriteriaType.HonorableKills);

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

            player.SetFactionForRace(player.GetRace());
            player.SetPowerType(powerType);

            // reset only if player not in some form;
            if (player.GetShapeshiftForm() == ShapeShiftForm.None)
                player.InitDisplayIds();

            player.ReplaceAllPvpFlags(UnitPVPStateFlags.PvP);

            player.ReplaceAllUnitFlags(UnitFlags.PlayerControlled);

            //-1 is default value
            player.SetWatchedFactionIndex(0xFFFFFFFF);
            return true;
        }

        [Command("level", RBACPermissions.CommandResetLevel, true)]
        static bool HandleResetLevelCommand(CommandHandler handler, PlayerIdentifier player)
        {
            if (player == null)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player == null || !player.IsConnected())
                return false;

            Player target = player.GetConnectedPlayer();

            if (!HandleResetStatsOrLevelHelper(target))
                return false;

            byte oldLevel = (byte)target.GetLevel();

            // set starting level
            uint startLevel = target.GetStartLevel(target.GetRace(), target.GetClass());

            target._ApplyAllLevelScaleItemMods(false);
            target.SetLevel(startLevel);
            target.InitRunes();
            target.InitStatsForLevel(true);
            target.InitTaxiNodesForLevel();
            target.InitTalentForLevel();
            target.SetXP(0);

            target._ApplyAllLevelScaleItemMods(true);

            // reset level for pet
            Pet pet = target.GetPet();
            if (pet)
                pet.SynchronizeLevelWithOwner();

            Global.ScriptMgr.OnPlayerLevelChanged(target, oldLevel);

            return true;
        }

        [Command("spells", RBACPermissions.CommandResetSpells, true)]
        static bool HandleResetSpellsCommand(CommandHandler handler, PlayerIdentifier player)
        {
            if (player == null)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player == null)
                return false;

            if (player.IsConnected())
            {
                var target = player.GetConnectedPlayer();
                target.ResetSpells();

                target.SendSysMessage(CypherStrings.ResetSpells);
                if (handler.GetSession() == null || handler.GetSession().GetPlayer() != target)
                    handler.SendSysMessage(CypherStrings.ResetSpellsOnline, handler.GetNameLink(target));
            }
            else
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                stmt.AddValue(0, (ushort)AtLoginFlags.ResetSpells);
                stmt.AddValue(1, player.GetGUID().GetCounter());
                DB.Characters.Execute(stmt);

                handler.SendSysMessage(CypherStrings.ResetSpellsOffline, player.GetName());
            }

            return true;
        }

        [Command("stats", RBACPermissions.CommandResetStats, true)]
        static bool HandleResetStatsCommand(CommandHandler handler, PlayerIdentifier player)
        {
            if (player == null)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player == null || !player.IsConnected())
                return false;

            var target = player.GetConnectedPlayer();

            if (!HandleResetStatsOrLevelHelper(target))
                return false;

            target.InitRunes();
            target.InitStatsForLevel(true);
            target.InitTaxiNodesForLevel();
            target.InitTalentForLevel();

            return true;
        }

        [Command("talents", RBACPermissions.CommandResetTalents, true)]
        static bool HandleResetTalentsCommand(CommandHandler handler, PlayerIdentifier player)
        {
            if (player == null)
                player = PlayerIdentifier.FromTargetOrSelf(handler);
            if (player == null)
                return false;

            if (player.IsConnected())
            {
                var target = player.GetConnectedPlayer();
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
            else if (!player.GetGUID().IsEmpty())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ADD_AT_LOGIN_FLAG);
                stmt.AddValue(0, (ushort)(AtLoginFlags.None | AtLoginFlags.ResetPetTalents));
                stmt.AddValue(1, player.GetGUID().GetCounter());
                DB.Characters.Execute(stmt);

                string nameLink = handler.PlayerLink(player.GetName());
                handler.SendSysMessage(CypherStrings.ResetTalentsOffline, nameLink);
                return true;
            }

            handler.SendSysMessage(CypherStrings.NoCharSelected);
            return false;
        }

        [Command("all", RBACPermissions.CommandResetAll, true)]
        static bool HandleResetAllCommand(CommandHandler handler, string subCommand)
        {
            AtLoginFlags atLogin;

            // Command specially created as single command to prevent using short case names
            if (subCommand == "spells")
            {
                atLogin = AtLoginFlags.ResetSpells;
                Global.WorldMgr.SendWorldText(CypherStrings.ResetallSpells);
                if (handler.GetSession() == null)
                    handler.SendSysMessage(CypherStrings.ResetallSpells);
            }
            else if (subCommand == "talents")
            {
                atLogin = AtLoginFlags.ResetTalents | AtLoginFlags.ResetPetTalents;
                Global.WorldMgr.SendWorldText(CypherStrings.ResetallTalents);
                if (handler.GetSession() == null)
                    handler.SendSysMessage(CypherStrings.ResetallTalents);
            }
            else
            {
                handler.SendSysMessage(CypherStrings.ResetallUnknownCase, subCommand);
                return false;
            }

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ALL_AT_LOGIN_FLAGS);
            stmt.AddValue(0, (ushort)atLogin);
            DB.Characters.Execute(stmt);

            var plist = Global.ObjAccessor.GetPlayers();
            foreach (var player in plist)
                player.SetAtLoginFlag(atLogin);

            return true;
        }
    }
}
