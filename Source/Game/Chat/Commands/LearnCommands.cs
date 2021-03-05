﻿/*
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
using Game.DataStorage;
using Game.Entities;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Chat.Commands
{
    [CommandGroup("learn", RBACPermissions.CommandLearn)]
    class LearnCommands
    {
        [Command("", RBACPermissions.CommandLearn)]
        static bool HandleLearnCommand(StringArguments args, CommandHandler handler)
        {
            var targetPlayer = handler.GetSelectedPlayerOrSelf();

            if (!targetPlayer)
            {
                handler.SendSysMessage(CypherStrings.PlayerNotFound);
                return false;
            }

            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r or Htalent form
            var spell = handler.ExtractSpellIdFromLink(args);
            if (spell == 0 || !Global.SpellMgr.HasSpellInfo(spell, Difficulty.None))
                return false;

            var all = args.NextString();
            var allRanks = !string.IsNullOrEmpty(all) && all == "all";

            var spellInfo = Global.SpellMgr.GetSpellInfo(spell, Difficulty.None);
            if (spellInfo == null || !Global.SpellMgr.IsSpellValid(spellInfo, handler.GetSession().GetPlayer()))
            {
                handler.SendSysMessage(CypherStrings.CommandSpellBroken, spell);
                return false;
            }

            if (!allRanks && targetPlayer.HasSpell(spell))
            {
                if (targetPlayer == handler.GetSession().GetPlayer())
                    handler.SendSysMessage(CypherStrings.YouKnownSpell);
                else
                    handler.SendSysMessage(CypherStrings.TargetKnownSpell, handler.GetNameLink(targetPlayer));

                return false;
            }

            if (allRanks)
                targetPlayer.LearnSpellHighestRank(spell);
            else
                targetPlayer.LearnSpell(spell, false);

            return true;
        }

        [CommandGroup("all", RBACPermissions.CommandLearnAll)]
        class LearnAllCommands
        {
            [Command("gm", RBACPermissions.CommandLearnAllGm)]
            static bool HandleLearnAllGMCommand(StringArguments args, CommandHandler handler)
            {
                foreach (var skillSpell in Global.SpellMgr.GetSkillLineAbilityMapBounds((uint)SkillType.Internal))
                {
                    var spellInfo = Global.SpellMgr.GetSpellInfo(skillSpell.Spell, Difficulty.None);
                    if (spellInfo == null || !Global.SpellMgr.IsSpellValid(spellInfo, handler.GetSession().GetPlayer(), false))
                        continue;

                    handler.GetSession().GetPlayer().LearnSpell(skillSpell.Spell, false);
                }

                handler.SendSysMessage(CypherStrings.LearningGmSkills);
                return true;
            }

            [Command("lang", RBACPermissions.CommandLearnAllLang)]
            static bool HandleLearnAllLangCommand(StringArguments args, CommandHandler handler)
            {
                // skipping UNIVERSAL language (0)
                for (byte i = 1; i < Enum.GetValues(typeof(Language)).Length; ++i)
                    handler.GetSession().GetPlayer().LearnSpell(ObjectManager.lang_description[i].spell_id, false);

                handler.SendSysMessage(CypherStrings.CommandLearnAllLang);
                return true;
            }

            [Command("default", RBACPermissions.CommandLearnAllDefault)]
            static bool HandleLearnAllDefaultCommand(StringArguments args, CommandHandler handler)
            {
                Player target;
                if (!handler.ExtractPlayerTarget(args, out target))
                    return false;

                target.LearnDefaultSkills();
                target.LearnCustomSpells();
                target.LearnQuestRewardedSpells();

                handler.SendSysMessage(CypherStrings.CommandLearnAllDefaultAndQuest, handler.GetNameLink(target));
                return true;
            }

            [Command("crafts", RBACPermissions.CommandLearnAllCrafts)]
            static bool HandleLearnAllCraftsCommand(StringArguments args, CommandHandler handler)
            {
                Player target;
                if (!handler.ExtractPlayerTarget(args, out target))
                    return false;

                foreach (var skillInfo in CliDB.SkillLineStorage.Values)
                {
                    if ((skillInfo.CategoryID == SkillCategory.Profession || skillInfo.CategoryID == SkillCategory.Secondary) && skillInfo.CanLink != 0)                             // only prof. with recipes have
                    {
                        HandleLearnSkillRecipesHelper(target, skillInfo.Id);
                    }
                }

                handler.SendSysMessage(CypherStrings.CommandLearnAllCraft);
                return true;
            }

            [Command("recipes", RBACPermissions.CommandLearnAllRecipes)]
            static bool HandleLearnAllRecipesCommand(StringArguments args, CommandHandler handler)
            {
                //  Learns all recipes of specified profession and sets skill to max
                //  Example: .learn all_recipes enchanting

                var target = handler.GetSelectedPlayer();
                if (!target)
                {
                    handler.SendSysMessage(CypherStrings.PlayerNotFound);
                    return false;
                }

                if (args.Empty())
                    return false;

                // converting string that we try to find to lower case
                var namePart = args.NextString().ToLower();

                var name = "";
                uint skillId = 0;
                foreach (var skillInfo in CliDB.SkillLineStorage.Values)
                {
                    if ((skillInfo.CategoryID != SkillCategory.Profession &&
                        skillInfo.CategoryID != SkillCategory.Secondary) ||
                        skillInfo.CanLink == 0)                            // only prof with recipes have set
                        continue;

                    var locale = handler.GetSessionDbcLocale();
                    name = skillInfo.DisplayName[locale];
                    if (string.IsNullOrEmpty(name))
                        continue;

                    if (!name.Like(namePart))
                    {
                        locale = 0;
                        for (; locale < Locale.Total; ++locale)
                        {
                            if (locale == handler.GetSessionDbcLocale())
                                continue;

                            name = skillInfo.DisplayName[locale];
                            if (name.IsEmpty())
                                continue;

                            if (name.Like(namePart))
                                break;
                        }
                    }

                    if (locale < Locale.Total)
                    {
                        skillId = skillInfo.Id;
                        break;
                    }
                }

                if (skillId == 0)
                    return false;

                HandleLearnSkillRecipesHelper(target, skillId);

                var maxLevel = target.GetPureMaxSkillValue((SkillType)skillId);
                target.SetSkill(skillId, target.GetSkillStep((SkillType)skillId), maxLevel, maxLevel);
                handler.SendSysMessage(CypherStrings.CommandLearnAllRecipes, name);
                return true;
            }

            static void HandleLearnSkillRecipesHelper(Player player, uint skillId)
            {
                var classmask = player.GetClassMask();

                foreach (var skillLine in CliDB.SkillLineAbilityStorage.Values)
                {
                    // wrong skill
                    if (skillLine.SkillLine != skillId)
                        continue;

                    // not high rank
                    if (skillLine.SupercedesSpell != 0)
                        continue;

                    // skip racial skills
                    if (skillLine.RaceMask != 0)
                        continue;

                    // skip wrong class skills
                    if (skillLine.ClassMask != 0 && (skillLine.ClassMask & classmask) == 0)
                        continue;

                    var spellInfo = Global.SpellMgr.GetSpellInfo(skillLine.Spell, Difficulty.None);
                    if (spellInfo == null || !Global.SpellMgr.IsSpellValid(spellInfo, player, false))
                        continue;

                    player.LearnSpell(skillLine.Spell, false);
                }
            }

            [CommandGroup("my", RBACPermissions.CommandLearnAllMy)]
            class LearnAllMyCommands
            {
                [Command("class", RBACPermissions.CommandLearnAllMyClass)]
                static bool HandleLearnAllMyClassCommand(StringArguments args, CommandHandler handler)
                {
                    HandleLearnAllMySpellsCommand(args, handler);
                    HandleLearnAllMyTalentsCommand(args, handler);
                    return true;
                }

                [Command("spells", RBACPermissions.CommandLearnAllMySpells)]
                static bool HandleLearnAllMySpellsCommand(StringArguments args, CommandHandler handler)
                {
                    var classEntry = CliDB.ChrClassesStorage.LookupByKey(handler.GetSession().GetPlayer().GetClass());
                    if (classEntry == null)
                        return true;
                    uint family = classEntry.SpellClassSet;

                    foreach (var entry in CliDB.SkillLineAbilityStorage.Values)
                    {
                        var spellInfo = Global.SpellMgr.GetSpellInfo(entry.Spell, Difficulty.None);
                        if (spellInfo == null)
                            continue;

                        // skip server-side/triggered spells
                        if (spellInfo.SpellLevel == 0)
                            continue;

                        // skip wrong class/race skills
                        if (!handler.GetSession().GetPlayer().IsSpellFitByClassAndRace(spellInfo.Id))
                            continue;

                        // skip other spell families
                        if ((uint)spellInfo.SpellFamilyName != family)
                            continue;

                        // skip broken spells
                        if (!Global.SpellMgr.IsSpellValid(spellInfo, handler.GetSession().GetPlayer(), false))
                            continue;

                        handler.GetSession().GetPlayer().LearnSpell(spellInfo.Id, false);
                    }

                    handler.SendSysMessage(CypherStrings.CommandLearnClassSpells);
                    return true;
                }

                [Command("talents", RBACPermissions.CommandLearnAllMyTalents)]
                static bool HandleLearnAllMyTalentsCommand(StringArguments args, CommandHandler handler)
                {
                    var player = handler.GetSession().GetPlayer();
                    var playerClass = (uint)player.GetClass();

                    foreach (var talentInfo in CliDB.TalentStorage.Values)
                    {
                        if (playerClass != talentInfo.ClassID)
                            continue;

                        var spellInfo = Global.SpellMgr.GetSpellInfo(talentInfo.SpellID, Difficulty.None);
                        if (spellInfo == null || !Global.SpellMgr.IsSpellValid(spellInfo, handler.GetSession().GetPlayer(), false))
                            continue;

                        // learn highest rank of talent and learn all non-talent spell ranks (recursive by tree)
                        player.LearnSpellHighestRank(talentInfo.SpellID);
                        player.AddTalent(talentInfo, player.GetActiveTalentGroup(), true);
                    }

                    handler.SendSysMessage(CypherStrings.CommandLearnClassTalents);
                    return true;
                }

                [Command("pettalents", RBACPermissions.CommandLearnAllMyPettalents)]
                static bool HandleLearnAllMyPetTalentsCommand(StringArguments args, CommandHandler handler) { return true; }
            }
        }

        [CommandNonGroup("unlearn", RBACPermissions.CommandUnlearn)]
        static bool HandleUnLearnCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            // number or [name] Shift-click form |color|Hspell:spell_id|h[name]|h|r
            var spellId = handler.ExtractSpellIdFromLink(args);
            if (spellId == 0)
                return false;

            var allStr = args.NextString();
            var allRanks = !string.IsNullOrEmpty(allStr) && allStr == "all";

            var target = handler.GetSelectedPlayer();
            if (!target)
            {
                handler.SendSysMessage(CypherStrings.NoCharSelected);
                return false;
            }

            if (allRanks)
                spellId = Global.SpellMgr.GetFirstSpellInChain(spellId);

            if (target.HasSpell(spellId))
                target.RemoveSpell(spellId, false, !allRanks);
            else
                handler.SendSysMessage(CypherStrings.ForgetSpell);

            return true;
        }
    }
}
