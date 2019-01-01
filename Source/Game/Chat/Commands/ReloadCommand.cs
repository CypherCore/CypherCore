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
using Game.Entities;
using Game.Loots;
using Game.Spells;

namespace Game.Chat
{
    [CommandGroup("reload", RBACPermissions.CommandReload, true)]
    class ReloadCommand
    {
        [Command("access_requirement", RBACPermissions.CommandReloadAccessRequirement, true)]
        static bool HandleReloadAccessRequirementCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Access Requirement definitions...");
            Global.ObjectMgr.LoadAccessRequirements();
            handler.SendGlobalGMSysMessage("DB table `access_requirement` reloaded.");
            return true;
        }

        [Command("achievement_reward", RBACPermissions.CommandReloadAchievementReward, true)]
        static bool HandleReloadAchievementRewardCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Achievement Reward Data...");
            Global.AchievementMgr.LoadRewards();
            handler.SendGlobalGMSysMessage("DB table `achievement_reward` reloaded.");
            return true;
        }

        [Command("areatrigger_involvedrelation", RBACPermissions.CommandReloadAreatriggerInvolvedrelation, true)]
        static bool HandleReloadQuestAreaTriggersCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Quest Area Triggers...");
            Global.ObjectMgr.LoadQuestAreaTriggers();
            handler.SendGlobalGMSysMessage("DB table `areatrigger_involvedrelation` (quest area triggers) reloaded.");
            return true;
        }

        [Command("areatrigger_tavern", RBACPermissions.CommandReloadAreatriggerTavern, true)]
        static bool HandleReloadAreaTriggerTavernCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Tavern Area Triggers...");
            Global.ObjectMgr.LoadTavernAreaTriggers();
            handler.SendGlobalGMSysMessage("DB table `areatrigger_tavern` reloaded.");
            return true;
        }

        [Command("areatrigger_teleport", RBACPermissions.CommandReloadAreatriggerTeleport, true)]
        static bool HandleReloadAreaTriggerTeleportCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading AreaTrigger teleport definitions...");
            Global.ObjectMgr.LoadAreaTriggerTeleports();
            handler.SendGlobalGMSysMessage("DB table `areatrigger_teleport` reloaded.");
            return true;
        }

        [Command("areatrigger_template", RBACPermissions.CommandReloadSceneTemplate, true)]
        static bool HandleReloadAreaTriggerTemplateCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Reloading areatrigger_template table...");
            Global.AreaTriggerDataStorage.LoadAreaTriggerTemplates();
            handler.SendGlobalGMSysMessage("AreaTrigger templates reloaded. Already spawned AT won't be affected. New scriptname need a reboot.");
            return true;
        }

        [Command("auctions", RBACPermissions.CommandReloadAuctions, true)]
        static bool HandleReloadAuctionsCommand(StringArguments args, CommandHandler handler)
        {
            // Reload dynamic data tables from the database
            Log.outInfo(LogFilter.Server, "Re-Loading Auctions...");
            Global.AuctionMgr.LoadAuctionItems();
            Global.AuctionMgr.LoadAuctions();
            handler.SendGlobalGMSysMessage("Auctions reloaded.");
            return true;
        }

        [Command("autobroadcast", RBACPermissions.CommandReloadAutobroadcast, true)]
        static bool HandleReloadAutobroadcastCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Autobroadcasts...");
            Global.WorldMgr.LoadAutobroadcasts();
            handler.SendGlobalGMSysMessage("DB table `autobroadcast` reloaded.");
            return true;
        }

        [Command("battleground_template", RBACPermissions.CommandReloadBattlegroundTemplate, true)]
        static bool HandleReloadBattlegroundTemplate(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Misc, "Re-Loading Battleground Templates...");
            Global.BattlegroundMgr.LoadBattlegroundTemplates();
            handler.SendGlobalGMSysMessage("DB table `battleground_template` reloaded.");
            return true;
        }

        [Command("character_template", RBACPermissions.CommandReloadCharacterTemplate, true)]
        static bool HandleReloadCharacterTemplate(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Character Templates...");
            Global.CharacterTemplateDataStorage.LoadCharacterTemplates();
            handler.SendGlobalGMSysMessage("DB table `character_template` and `character_template_class` reloaded.");
            return true;
        }

        [Command("command", RBACPermissions.CommandReloadCommand, true)]
        static bool HandleReloadCommandCommand(StringArguments args, CommandHandler handler) { return true; }

        [Command("conditions", RBACPermissions.CommandReloadConditions, true)]
        static bool HandleReloadConditions(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Conditions...");
            Global.ConditionMgr.LoadConditions(true);
            handler.SendGlobalGMSysMessage("Conditions reloaded.");
            return true;
        }

        [Command("config", RBACPermissions.CommandReloadConfig, true)]
        static bool HandleReloadConfigCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading config settings...");
            Global.WorldMgr.LoadConfigSettings(true);
            Global.MapMgr.InitializeVisibilityDistanceInfo();
            handler.SendGlobalGMSysMessage("World config settings reloaded.");
            return true;
        }

        [Command("conversation_template", RBACPermissions.CommandReloadConversationTemplate, true)]
        static bool HandleReloadConversationTemplateCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Reloading conversation_* tables...");
            Global.ConversationDataStorage.LoadConversationTemplates();
            handler.SendGlobalGMSysMessage("Conversation templates reloaded.");
            return true;
        }

        [Command("creature_linked_respawn", RBACPermissions.CommandReloadCreatureLinkedRespawn, true)]
        static bool HandleReloadLinkedRespawnCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Loading Linked Respawns... (`creature_linked_respawn`)");
            Global.ObjectMgr.LoadLinkedRespawn();
            handler.SendGlobalGMSysMessage("DB table `creature_linked_respawn` (creature linked respawns) reloaded.");
            return true;
        }

        [Command("creature_loot_template", RBACPermissions.CommandReloadCreatureLootTemplate, true)]
        static bool HandleReloadLootTemplatesCreatureCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`creature_loot_template`)");
            LootManager.LoadLootTemplates_Creature();
            LootStorage.Creature.CheckLootRefs();
            handler.SendGlobalGMSysMessage("DB table `creature_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("creature_onkill_reputation", RBACPermissions.CommandReloadCreatureOnkillReputation, true)]
        static bool HandleReloadOnKillReputationCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading creature award reputation definitions...");
            Global.ObjectMgr.LoadReputationOnKill();
            handler.SendGlobalGMSysMessage("DB table `creature_onkill_reputation` reloaded.");
            return true;
        }

        [Command("creature_questender", RBACPermissions.CommandReloadCreatureQuestender, true)]
        static bool HandleReloadCreatureQuestEnderCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Loading Quests Relations... (`creature_questender`)");
            Global.ObjectMgr.LoadCreatureQuestEnders();
            handler.SendGlobalGMSysMessage("DB table `creature_questender` reloaded.");
            return true;
        }

        [Command("creature_queststarter", RBACPermissions.CommandReloadCreatureQueststarter, true)]
        static bool HandleReloadCreatureQuestStarterCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Loading Quests Relations... (`creature_queststarter`)");
            Global.ObjectMgr.LoadCreatureQuestStarters();
            handler.SendGlobalGMSysMessage("DB table `creature_queststarter` reloaded.");
            return true;
        }

        [Command("creature_summon_groups", RBACPermissions.CommandReloadCreatureSummonGroups, true)]
        static bool HandleReloadCreatureSummonGroupsCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Reloading creature summon groups...");
            Global.ObjectMgr.LoadTempSummons();
            handler.SendGlobalGMSysMessage("DB table `creature_summon_groups` reloaded.");
            return true;
        }

        [Command("creature_template", RBACPermissions.CommandReloadCreatureTemplate, true)]
        static bool HandleReloadCreatureTemplateCommand(StringArguments args, CommandHandler handler)
        {
            if (args.Empty())
                return false;

            uint entry = 0;
            while ((entry = args.NextUInt32()) != 0)
            {
                PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_CREATURE_TEMPLATE);
                stmt.AddValue(0, entry);
                SQLResult result = DB.World.Query(stmt);

                if (result.IsEmpty())
                {
                    handler.SendSysMessage(CypherStrings.CommandCreaturetemplateNotfound, entry);
                    continue;
                }

                CreatureTemplate cInfo = Global.ObjectMgr.GetCreatureTemplate(entry);
                if (cInfo == null)
                {
                    handler.SendSysMessage(CypherStrings.CommandCreaturestorageNotfound, entry);
                    continue;
                }

                Log.outInfo(LogFilter.Server, "Reloading creature template entry {0}", entry);

                Global.ObjectMgr.LoadCreatureTemplate(result.GetFields());
                Global.ObjectMgr.CheckCreatureTemplate(cInfo);
            }

            handler.SendGlobalGMSysMessage("Creature template reloaded.");
            return true;
        }

        [Command("creature_text", RBACPermissions.CommandReloadCreatureText, true)]
        static bool HandleReloadCreatureText(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Creature Texts...");
            Global.CreatureTextMgr.LoadCreatureTexts();
            handler.SendGlobalGMSysMessage("Creature Texts reloaded.");
            return true;
        }

        [Command("trinity_string", RBACPermissions.CommandReloadCypherString, true)]
        static bool HandleReloadCypherStringCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading trinity_string Table!");
            Global.ObjectMgr.LoadCypherStrings();
            handler.SendGlobalGMSysMessage("DB table `trinity_string` reloaded.");
            return true;
        }

        [Command("criteria_data", RBACPermissions.CommandReloadCriteriaData, true)]
        static bool HandleReloadCriteriaDataCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Additional Criteria Data...");
            Global.CriteriaMgr.LoadCriteriaData();
            handler.SendGlobalGMSysMessage("DB table `criteria_data` reloaded.");
            return true;
        }

        [Command("disables", RBACPermissions.CommandReloadDisables, true)]
        static bool HandleReloadDisablesCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading disables table...");
            Global.DisableMgr.LoadDisables();
            Log.outInfo(LogFilter.Server, "Checking quest disables...");
            Global.DisableMgr.CheckQuestDisables();
            handler.SendGlobalGMSysMessage("DB table `disables` reloaded.");
            return true;
        }

        [Command("disenchant_loot_template", RBACPermissions.CommandReloadDisenchantLootTemplate, true)]
        static bool HandleReloadLootTemplatesDisenchantCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`disenchant_loot_template`)");
            LootManager.LoadLootTemplates_Disenchant();
            LootStorage.Disenchant.CheckLootRefs();
            handler.SendGlobalGMSysMessage("DB table `disenchant_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("event_scripts", RBACPermissions.CommandReloadEventScripts, true)]
        static bool HandleReloadEventScriptsCommand(StringArguments args, CommandHandler handler)
        {
            if (Global.MapMgr.IsScriptScheduled())
            {
                handler.SendSysMessage("DB scripts used currently, please attempt reload later.");
                return false;
            }

            if (args != null)
                Log.outInfo(LogFilter.Server, "Re-Loading Scripts from `event_scripts`...");

            Global.ObjectMgr.LoadEventScripts();

            if (args != null)
                handler.SendGlobalGMSysMessage("DB table `event_scripts` reloaded.");

            return true;
        }

        [Command("fishing_loot_template", RBACPermissions.CommandReloadFishingLootTemplate, true)]
        static bool HandleReloadLootTemplatesFishingCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`fishing_loot_template`)");
            LootManager.LoadLootTemplates_Fishing();
            LootStorage.Fishing.CheckLootRefs();
            handler.SendGlobalGMSysMessage("DB table `fishing_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("graveyard_zone", RBACPermissions.CommandReloadGraveyardZone, true)]
        static bool HandleReloadGameGraveyardZoneCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Graveyard-zone links...");

            Global.ObjectMgr.LoadGraveyardZones();

            handler.SendGlobalGMSysMessage("DB table `game_graveyard_zone` reloaded.");

            return true;
        }

        [Command("game_tele", RBACPermissions.CommandReloadGameTele, true)]
        static bool HandleReloadGameTeleCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Game Tele coordinates...");

            Global.ObjectMgr.LoadGameTele();

            handler.SendGlobalGMSysMessage("DB table `game_tele` reloaded.");

            return true;
        }

        [Command("gameobject_loot_template", RBACPermissions.CommandReloadGameobjectQuestLootTemplate, true)]
        static bool HandleReloadLootTemplatesGameobjectCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`gameobject_loot_template`)");
            LootManager.LoadLootTemplates_Gameobject();
            LootStorage.Gameobject.CheckLootRefs();
            handler.SendGlobalGMSysMessage("DB table `gameobject_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("gameobject_questender", RBACPermissions.CommandReloadGameobjectQuestender, true)]
        static bool HandleReloadGOQuestEnderCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Loading Quests Relations... (`gameobject_questender`)");
            Global.ObjectMgr.LoadGameobjectQuestEnders();
            handler.SendGlobalGMSysMessage("DB table `gameobject_questender` reloaded.");
            return true;
        }

        [Command("gameobject_queststarter", RBACPermissions.CommandReloadGameobjectQueststarter, true)]
        static bool HandleReloadGOQuestStarterCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Loading Quests Relations... (`gameobject_queststarter`)");
            Global.ObjectMgr.LoadGameobjectQuestStarters();
            handler.SendGlobalGMSysMessage("DB table `gameobject_queststarter` reloaded.");
            return true;
        }

        [Command("gossip_menu", RBACPermissions.CommandReloadGossipMenu, true)]
        static bool HandleReloadGossipMenuCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading `gossip_menu` Table!");
            Global.ObjectMgr.LoadGossipMenu();
            handler.SendGlobalGMSysMessage("DB table `gossip_menu` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("gossip_menu_option", RBACPermissions.CommandReloadGossipMenuOption, true)]
        static bool HandleReloadGossipMenuOptionCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading `gossip_menu_option` Table!");
            Global.ObjectMgr.LoadGossipMenuItems();
            handler.SendGlobalGMSysMessage("DB table `gossip_menu_option` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("item_enchantment_template", RBACPermissions.CommandReloadItemEnchantmentTemplate, true)]
        static bool HandleReloadItemEnchantementsCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Item Random Enchantments Table...");
            ItemEnchantment.LoadRandomEnchantmentsTable();
            handler.SendGlobalGMSysMessage("DB table `item_enchantment_template` reloaded.");
            return true;
        }

        [Command("item_loot_template", RBACPermissions.CommandReloadItemLootTemplate, true)]
        static bool HandleReloadLootTemplatesItemCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`item_loot_template`)");
            LootManager.LoadLootTemplates_Item();
            LootStorage.Items.CheckLootRefs();
            handler.SendGlobalGMSysMessage("DB table `item_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("lfg_dungeon_rewards", RBACPermissions.CommandReloadLfgDungeonRewards, true)]
        static bool HandleReloadLfgRewardsCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading lfg dungeon rewards...");
            Global.LFGMgr.LoadRewards();
            handler.SendGlobalGMSysMessage("DB table `lfg_dungeon_rewards` reloaded.");
            return true;
        }

        [Command("locales_achievement_reward", RBACPermissions.CommandReloadLocalesAchievementReward, true)]
        static bool HandleReloadLocalesAchievementRewardCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Locales Achievement Reward Data...");
            Global.AchievementMgr.LoadRewardLocales();
            handler.SendGlobalGMSysMessage("DB table `locales_achievement_reward` reloaded.");
            return true;
        }

        [Command("locales_creature", RBACPermissions.CommandReloadLocalesCreature, true)]
        static bool HandleReloadLocalesCreatureCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Locales Creature ...");
            Global.ObjectMgr.LoadCreatureLocales();
            handler.SendGlobalGMSysMessage("DB table `locales_creature` reloaded.");
            return true;
        }

        [Command("locales_creature_text", RBACPermissions.CommandReloadLocalesCreatureText, true)]
        static bool HandleReloadLocalesCreatureTextCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Locales Creature Texts...");
            Global.CreatureTextMgr.LoadCreatureTextLocales();
            handler.SendGlobalGMSysMessage("DB table `locales_creature_text` reloaded.");
            return true;
        }

        [Command("locales_gameobject", RBACPermissions.CommandReloadLocalesGameobject, true)]
        static bool HandleReloadLocalesGameobjectCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Locales Gameobject ... ");
            Global.ObjectMgr.LoadGameObjectLocales();
            handler.SendGlobalGMSysMessage("DB table `locales_gameobject` reloaded.");
            return true;
        }

        [Command("locales_gossip_menu_option", RBACPermissions.CommandReloadLocalesGossipMenuOption, true)]
        static bool HandleReloadLocalesGossipMenuOptionCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Locales Gossip Menu Option ... ");
            Global.ObjectMgr.LoadGossipMenuItemsLocales();
            handler.SendGlobalGMSysMessage("DB table `locales_gossip_menu_option` reloaded.");
            return true;
        }

        [Command("locales_page_text", RBACPermissions.CommandReloadLocalesPageText, true)]
        static bool HandleReloadLocalesPageTextCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Locales Page Text ... ");
            Global.ObjectMgr.LoadPageTextLocales();
            handler.SendGlobalGMSysMessage("DB table `locales_page_text` reloaded.");
            return true;
        }

        [Command("locales_points_of_interest", RBACPermissions.CommandReloadLocalesPointsOfInterest, true)]
        static bool HandleReloadLocalesPointsOfInterestCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Locales Points Of Interest ... ");
            Global.ObjectMgr.LoadPointOfInterestLocales();
            handler.SendGlobalGMSysMessage("DB table `locales_points_of_interest` reloaded.");
            return true;
        }

        [Command("mail_level_reward", RBACPermissions.CommandReloadMailLevelReward, true)]
        static bool HandleReloadMailLevelRewardCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Player level dependent mail rewards...");
            Global.ObjectMgr.LoadMailLevelRewards();
            handler.SendGlobalGMSysMessage("DB table `mail_level_reward` reloaded.");
            return true;
        }

        [Command("mail_loot_template", RBACPermissions.CommandReloadMailLootTemplate, true)]
        static bool HandleReloadLootTemplatesMailCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`mail_loot_template`)");
            LootManager.LoadLootTemplates_Mail();
            LootStorage.Mail.CheckLootRefs();
            handler.SendGlobalGMSysMessage("DB table `mail_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("milling_loot_template", RBACPermissions.CommandReloadMillingLootTemplate, true)]
        static bool HandleReloadLootTemplatesMillingCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`milling_loot_template`)");
            LootManager.LoadLootTemplates_Milling();
            LootStorage.Milling.CheckLootRefs();
            handler.SendGlobalGMSysMessage("DB table `milling_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("npc_spellclick_spells", RBACPermissions.CommandReloadNpcSpellclickSpells, true)]
        static bool HandleReloadSpellClickSpellsCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading `npc_spellclick_spells` Table!");
            Global.ObjectMgr.LoadNPCSpellClickSpells();
            handler.SendGlobalGMSysMessage("DB table `npc_spellclick_spells` reloaded.");
            return true;
        }

        [Command("npc_vendor", RBACPermissions.CommandReloadNpcVendor, true)]
        static bool HandleReloadNpcVendorCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading `npc_vendor` Table!");
            Global.ObjectMgr.LoadVendors();
            handler.SendGlobalGMSysMessage("DB table `npc_vendor` reloaded.");
            return true;
        }

        [Command("page_text", RBACPermissions.CommandReloadPageText, true)]
        static bool HandleReloadPageTextsCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Page Text...");
            Global.ObjectMgr.LoadPageTexts();
            handler.SendGlobalGMSysMessage("DB table `page_text` reloaded.");
            return true;
        }

        [Command("pickpocketing_loot_template", RBACPermissions.CommandReloadPickpocketingLootTemplate, true)]
        static bool HandleReloadLootTemplatesPickpocketingCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`pickpocketing_loot_template`)");
            LootManager.LoadLootTemplates_Pickpocketing();
            LootStorage.Pickpocketing.CheckLootRefs();
            handler.SendGlobalGMSysMessage("DB table `pickpocketing_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("points_of_interest", RBACPermissions.CommandReloadPointsOfInterest, true)]
        static bool HandleReloadPointsOfInterestCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading `points_of_interest` Table!");
            Global.ObjectMgr.LoadPointsOfInterest();
            handler.SendGlobalGMSysMessage("DB table `points_of_interest` reloaded.");
            return true;
        }

        [Command("prospecting_loot_template", RBACPermissions.CommandReloadProspectingLootTemplate, true)]
        static bool HandleReloadLootTemplatesProspectingCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`prospecting_loot_template`)");
            LootManager.LoadLootTemplates_Prospecting();
            LootStorage.Prospecting.CheckLootRefs();
            handler.SendGlobalGMSysMessage("DB table `prospecting_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("quest_greeting", RBACPermissions.CommandReloadQuestGreeting, true)]
        static bool HandleReloadQuestGreetingCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Quest Greeting ... ");
            Global.ObjectMgr.LoadQuestGreetings();
            handler.SendGlobalGMSysMessage("DB table `quest_greeting` reloaded.");
            return true;
        }

        [Command("quest_locale", RBACPermissions.CommandReloadQuestLocale, true)]
        static bool HandleReloadQuestLocaleCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Quest Locale ... ");
            Global.ObjectMgr.LoadQuestTemplateLocale();
            Global.ObjectMgr.LoadQuestObjectivesLocale();
            Global.ObjectMgr.LoadQuestGreetingLocales();
            Global.ObjectMgr.LoadQuestOfferRewardLocale();
            Global.ObjectMgr.LoadQuestRequestItemsLocale();
            handler.SendGlobalGMSysMessage("DB table `quest_template_locale` reloaded.");
            handler.SendGlobalGMSysMessage("DB table `quest_objectives_locale` reloaded.");
            handler.SendGlobalGMSysMessage("DB table `quest_greeting_locale` reloaded.");
            handler.SendGlobalGMSysMessage("DB table `quest_offer_reward_locale` reloaded.");
            handler.SendGlobalGMSysMessage("DB table `quest_request_items_locale` reloaded.");
            return true;
        }

        [Command("quest_poi", RBACPermissions.CommandReloadQuestPoi, true)]
        static bool HandleReloadQuestPOICommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Quest POI ...");
            Global.ObjectMgr.LoadQuestPOI();
            handler.SendGlobalGMSysMessage("DB Table `quest_poi` and `quest_poi_points` reloaded.");
            return true;
        }

        [Command("quest_template", RBACPermissions.CommandReloadQuestTemplate, true)]
        static bool HandleReloadQuestTemplateCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Quest Templates...");
            Global.ObjectMgr.LoadQuests();
            handler.SendGlobalGMSysMessage("DB table `quest_template` (quest definitions) reloaded.");

            // dependent also from `gameobject` but this table not reloaded anyway
            Log.outInfo(LogFilter.Server, "Re-Loading GameObjects for quests...");
            Global.ObjectMgr.LoadGameObjectForQuests();
            handler.SendGlobalGMSysMessage("Data GameObjects for quests reloaded.");
            return true;
        }

        [Command("rbac", RBACPermissions.CommandReloadRbac, true)]
        static bool HandleReloadRBACCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Reloading RBAC tables...");
            Global.AccountMgr.LoadRBAC();
            Global.WorldMgr.ReloadRBAC();
            handler.SendGlobalGMSysMessage("RBAC data reloaded.");
            return true;
        }

        [Command("reference_loot_template", RBACPermissions.CommandReloadReferenceLootTemplate, true)]
        static bool HandleReloadLootTemplatesReferenceCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`reference_loot_template`)");
            LootManager.LoadLootTemplates_Reference();
            handler.SendGlobalGMSysMessage("DB table `reference_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("reputation_reward_rate", RBACPermissions.CommandReloadReputationRewardRate, true)]
        static bool HandleReloadReputationRewardRateCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading `reputation_reward_rate` Table!");
            Global.ObjectMgr.LoadReputationRewardRate();
            handler.SendGlobalSysMessage("DB table `reputation_reward_rate` reloaded.");
            return true;
        }

        [Command("reputation_spillover_template", RBACPermissions.CommandReloadSpilloverTemplate, true)]
        static bool HandleReloadReputationSpilloverTemplateCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading `reputation_spillover_template` Table!");
            Global.ObjectMgr.LoadReputationSpilloverTemplate();
            handler.SendGlobalSysMessage("DB table `reputation_spillover_template` reloaded.");
            return true;
        }

        [Command("reserved_name", RBACPermissions.CommandReloadReservedName, true)]
        static bool HandleReloadReservedNameCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Loading ReservedNames... (`reserved_name`)");
            Global.ObjectMgr.LoadReservedPlayersNames();
            handler.SendGlobalGMSysMessage("DB table `reserved_name` (player reserved names) reloaded.");
            return true;
        }

        [Command("scene_template", RBACPermissions.CommandReloadSceneTemplate, true)]
        static bool HandleReloadSceneTemplateCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Misc, "Reloading scene_template table...");
            Global.ObjectMgr.LoadSceneTemplates();
            handler.SendGlobalGMSysMessage("Scenes templates reloaded. New scriptname need a reboot.");
            return true;
        }

        [Command("skill_discovery_template", RBACPermissions.CommandReloadSkillDiscoveryTemplate, true)]
        static bool HandleReloadSkillDiscoveryTemplateCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Skill Discovery Table...");
            SkillDiscovery.LoadSkillDiscoveryTable();
            handler.SendGlobalGMSysMessage("DB table `skill_discovery_template` (recipes discovered at crafting) reloaded.");
            return true;
        }

        static bool HandleReloadSkillPerfectItemTemplateCommand(StringArguments args, CommandHandler handler)
        { // latched onto HandleReloadSkillExtraItemTemplateCommand as it's part of that table group (and i don't want to chance all the command IDs)
            Log.outInfo(LogFilter.Misc, "Re-Loading Skill Perfection Data Table...");
            SkillPerfectItems.LoadSkillPerfectItemTable();
            handler.SendGlobalGMSysMessage("DB table `skill_perfect_item_template` (perfect item procs when crafting) reloaded.");
            return true;
        }

        [Command("skill_extra_item_template", RBACPermissions.CommandReloadSkillExtraItemTemplate, true)]
        static bool HandleReloadSkillExtraItemTemplateCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Skill Extra Item Table...");
            SkillExtraItems.LoadSkillExtraItemTable();
            handler.SendGlobalGMSysMessage("DB table `skill_extra_item_template` (extra item creation when crafting) reloaded.");

            return HandleReloadSkillPerfectItemTemplateCommand(args, handler);
        }

        [Command("skill_fishing_base_level", RBACPermissions.CommandReloadSkillFishingBaseLevel, true)]
        static bool HandleReloadSkillFishingBaseLevelCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Skill Fishing base level requirements...");
            Global.ObjectMgr.LoadFishingBaseSkillLevel();
            handler.SendGlobalGMSysMessage("DB table `skill_fishing_base_level` (fishing base level for zone/subzone) reloaded.");
            return true;
        }

        [Command("skinning_loot_template", RBACPermissions.CommandReloadSkinningLootTemplate, true)]
        static bool HandleReloadLootTemplatesSkinningCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`skinning_loot_template`)");
            LootManager.LoadLootTemplates_Skinning();
            LootStorage.Skinning.CheckLootRefs();
            handler.SendGlobalGMSysMessage("DB table `skinning_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("smart_scripts", RBACPermissions.CommandReloadSmartScripts, true)]
        static bool HandleReloadSmartScripts(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Smart Scripts...");
            Global.SmartAIMgr.LoadFromDB();
            handler.SendGlobalGMSysMessage("Smart Scripts reloaded.");
            return true;
        }

        [Command("spell_area", RBACPermissions.CommandReloadSpellArea, true)]
        static bool HandleReloadSpellAreaCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading SpellArea Data...");
            Global.SpellMgr.LoadSpellAreas();
            handler.SendGlobalGMSysMessage("DB table `spell_area` (spell dependences from area/quest/auras state) reloaded.");
            return true;
        }

        [Command("spell_group", RBACPermissions.CommandReloadSpellGroup, true)]
        static bool HandleReloadSpellGroupsCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Spell Groups...");
            Global.SpellMgr.LoadSpellGroups();
            handler.SendGlobalGMSysMessage("DB table `spell_group` (spell groups) reloaded.");
            return true;
        }

        [Command("spell_group_stack_rules", RBACPermissions.CommandReloadSpellGroupStackRules, true)]
        static bool HandleReloadSpellGroupStackRulesCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Spell Group Stack Rules...");
            Global.SpellMgr.LoadSpellGroupStackRules();
            handler.SendGlobalGMSysMessage("DB table `spell_group_stack_rules` (spell stacking definitions) reloaded.");
            return true;
        }

        [Command("spell_learn_spell", RBACPermissions.CommandReloadSpellLearnSpell, true)]
        static bool HandleReloadSpellLearnSpellCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Spell Learn Spells...");
            Global.SpellMgr.LoadSpellLearnSpells();
            handler.SendGlobalGMSysMessage("DB table `spell_learn_spell` reloaded.");
            return true;
        }

        [Command("spell_linked_spell", RBACPermissions.CommandReloadSpellLinkedSpell, true)]
        static bool HandleReloadSpellLinkedSpellCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Spell Linked Spells...");
            Global.SpellMgr.LoadSpellLinked();
            handler.SendGlobalGMSysMessage("DB table `spell_linked_spell` reloaded.");
            return true;
        }

        [Command("spell_loot_template", RBACPermissions.CommandReloadSpellLootTemplate, true)]
        static bool HandleReloadLootTemplatesSpellCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`spell_loot_template`)");
            LootManager.LoadLootTemplates_Spell();
            LootStorage.Spell.CheckLootRefs();
            handler.SendGlobalGMSysMessage("DB table `spell_loot_template` reloaded.");
            Global.ConditionMgr.LoadConditions(true);
            return true;
        }

        [Command("spell_pet_auras", RBACPermissions.CommandReloadSpellPetAuras, true)]
        static bool HandleReloadSpellPetAurasCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Spell pet auras...");
            Global.SpellMgr.LoadSpellPetAuras();
            handler.SendGlobalGMSysMessage("DB table `spell_pet_auras` reloaded.");
            return true;
        }

        [Command("spell_proc", RBACPermissions.CommandReloadSpellProc, true)]
        static bool HandleReloadSpellProcsCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Spell Proc conditions and data...");
            Global.SpellMgr.LoadSpellProcs();
            handler.SendGlobalGMSysMessage("DB table `spell_proc` (spell proc conditions and data) reloaded.");
            return true;
        }

        [Command("spell_required", RBACPermissions.CommandReloadSpellRequired, true)]
        static bool HandleReloadSpellRequiredCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Spell Required Data... ");
            Global.SpellMgr.LoadSpellRequired();
            handler.SendGlobalGMSysMessage("DB table `spell_required` reloaded.");
            return true;
        }

        [Command("spell_scripts", RBACPermissions.CommandReloadSpellScripts, true)]
        static bool HandleReloadSpellScriptsCommand(StringArguments args, CommandHandler handler)
        {
            if (Global.MapMgr.IsScriptScheduled())
            {
                handler.SendSysMessage("DB scripts used currently, please attempt reload later.");
                return false;
            }

            if (args != null)
                Log.outInfo(LogFilter.Server, "Re-Loading Scripts from `spell_scripts`...");

            Global.ObjectMgr.LoadSpellScripts();

            if (args != null)
                handler.SendGlobalGMSysMessage("DB table `spell_scripts` reloaded.");

            return true;
        }

        [Command("spell_target_position", RBACPermissions.CommandReloadSpellTargetPosition, true)]
        static bool HandleReloadSpellTargetPositionCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Spell target coordinates...");
            Global.SpellMgr.LoadSpellTargetPositions();
            handler.SendGlobalGMSysMessage("DB table `spell_target_position` (destination coordinates for spell targets) reloaded.");
            return true;
        }

        [Command("spell_threats", RBACPermissions.CommandReloadSpellThreats, true)]
        static bool HandleReloadSpellThreatsCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Aggro Spells Definitions...");
            Global.SpellMgr.LoadSpellThreats();
            handler.SendGlobalGMSysMessage("DB table `spell_threat` (spell aggro definitions) reloaded.");
            return true;
        }

        [Command("support", RBACPermissions.CommandReloadSupportSystem, true)]
        static bool HandleReloadSupportSystemCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Re-Loading Support System Tables...");
            Global.SupportMgr.LoadBugTickets();
            Global.SupportMgr.LoadComplaintTickets();
            Global.SupportMgr.LoadSuggestionTickets();
            handler.SendGlobalGMSysMessage("DB tables `gm_*` reloaded.");
            return true;
        }

        [Command("trainer", RBACPermissions.CommandReloadTrainer, true)]
        static bool HandleReloadTrainerCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Misc, "Re-Loading `trainer` Table!");
            Global.ObjectMgr.LoadTrainers();
            Global.ObjectMgr.LoadCreatureDefaultTrainers();
            handler.SendGlobalGMSysMessage("DB table `trainer` reloaded.");
            handler.SendGlobalGMSysMessage("DB table `trainer_locale` reloaded.");
            handler.SendGlobalGMSysMessage("DB table `trainer_spell` reloaded.");
            handler.SendGlobalGMSysMessage("DB table `creature_default_trainer` reloaded.");
            return true;
        }

        [Command("vehicle_accessory", RBACPermissions.CommandReloadVehicleAccesory, true)]
        static bool HandleReloadVehicleAccessoryCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Reloading vehicle_accessory table...");
            Global.ObjectMgr.LoadVehicleAccessories();
            handler.SendGlobalGMSysMessage("Vehicle accessories reloaded.");
            return true;
        }

        [Command("vehicle_template_accessory", RBACPermissions.CommandReloadVehicleTemplateAccessory, true)]
        static bool HandleReloadVehicleTemplateAccessoryCommand(StringArguments args, CommandHandler handler)
        {
            Log.outInfo(LogFilter.Server, "Reloading vehicle_template_accessory table...");
            Global.ObjectMgr.LoadVehicleTemplateAccessories();
            handler.SendGlobalGMSysMessage("Vehicle template accessories reloaded.");
            return true;
        }

        [Command("warden_action", RBACPermissions.CommandReloadWardenAction, true)]
        static bool HandleReloadWardenactionCommand(StringArguments args, CommandHandler handler)
        {
            if (!WorldConfig.GetBoolValue(WorldCfg.WardenEnabled))
            {
                handler.SendSysMessage("Warden system disabled by config - reloading warden_action skipped.");
                return false;
            }

            //Log.outInfo(LogFilter.Misc, "Re-Loading warden_action Table!");
            //Global.WardenCheckMgr.LoadWardenOverrides();
            //handler.SendGlobalGMSysMessage("DB table `warden_action` reloaded.");
            return true;
        }

        [Command("waypoint_data", RBACPermissions.CommandReloadWaypointData, true)]
        static bool HandleReloadWpCommand(StringArguments args, CommandHandler handler)
        {
            if (args != null)
                Log.outInfo(LogFilter.Server, "Re-Loading Waypoints data from 'waypoints_data'");

            Global.WaypointMgr.Load();

            if (args != null)
                handler.SendGlobalGMSysMessage("DB Table 'waypoint_data' reloaded.");

            return true;
        }

        [Command("waypoint_scripts", RBACPermissions.CommandReloadWaypointScripts, true)]
        static bool HandleReloadWpScriptsCommand(StringArguments args, CommandHandler handler)
        {
            if (Global.MapMgr.IsScriptScheduled())
            {
                handler.SendSysMessage("DB scripts used currently, please attempt reload later.");
                return false;
            }

            if (args != null)
                Log.outInfo(LogFilter.Server, "Re-Loading Scripts from `waypoint_scripts`...");

            Global.ObjectMgr.LoadWaypointScripts();

            if (args != null)
                handler.SendGlobalGMSysMessage("DB table `waypoint_scripts` reloaded.");

            return true;
        }

        [CommandGroup("all", RBACPermissions.CommandReloadAll, true)]
        class AllCommand
        {
            [Command("", RBACPermissions.CommandReloadAll, true)]
            static bool HandleReloadAllCommand(StringArguments args, CommandHandler handler)
            {
                HandleReloadSkillFishingBaseLevelCommand(args, handler);

                HandleReloadAllAchievementCommand(args, handler);
                HandleReloadAllAreaCommand(args, handler);
                HandleReloadAllLootCommand(args, handler);
                HandleReloadAllNpcCommand(args, handler);
                HandleReloadAllQuestCommand(args, handler);
                HandleReloadAllSpellCommand(args, handler);
                HandleReloadAllItemCommand(args, handler);
                HandleReloadAllGossipsCommand(args, handler);
                HandleReloadAllLocalesCommand(args, handler);

                HandleReloadAccessRequirementCommand(args, handler);
                HandleReloadMailLevelRewardCommand(args, handler);
                HandleReloadReservedNameCommand(args, handler);
                HandleReloadCypherStringCommand(args, handler);
                HandleReloadGameTeleCommand(args, handler);

                HandleReloadCreatureSummonGroupsCommand(args, handler);

                HandleReloadVehicleAccessoryCommand(args, handler);
                HandleReloadVehicleTemplateAccessoryCommand(args, handler);

                HandleReloadAutobroadcastCommand(args, handler);
                HandleReloadBattlegroundTemplate(args, handler);
                HandleReloadCharacterTemplate(args, handler);
                return true;
            }

            [Command("achievement", RBACPermissions.CommandReloadAllAchievement, true)]
            static bool HandleReloadAllAchievementCommand(StringArguments args, CommandHandler handler)
            {
                HandleReloadCriteriaDataCommand(args, handler);
                HandleReloadAchievementRewardCommand(args, handler);
                return true;
            }

            [Command("area", RBACPermissions.CommandReloadAllArea, true)]
            static bool HandleReloadAllAreaCommand(StringArguments args, CommandHandler handler)
            {
                HandleReloadAreaTriggerTeleportCommand(args, handler);
                HandleReloadAreaTriggerTavernCommand(args, handler);
                HandleReloadGameGraveyardZoneCommand(args, handler);
                return true;
            }

            [Command("gossips", RBACPermissions.CommandReloadAllGossip, true)]
            static bool HandleReloadAllGossipsCommand(StringArguments args, CommandHandler handler)
            {
                HandleReloadGossipMenuCommand(null, handler);
                HandleReloadGossipMenuOptionCommand(null, handler);
                if (args == null)                                          // already reload from all_scripts
                    HandleReloadPointsOfInterestCommand(null, handler);
                return true;
            }

            [Command("item", RBACPermissions.CommandReloadAllItem, true)]
            static bool HandleReloadAllItemCommand(StringArguments args, CommandHandler handler)
            {
                HandleReloadPageTextsCommand(null, handler);
                HandleReloadItemEnchantementsCommand(null, handler);
                return true;
            }

            [Command("locales", RBACPermissions.CommandReloadAllLocales, true)]
            static bool HandleReloadAllLocalesCommand(StringArguments args, CommandHandler handler)
            {
                HandleReloadLocalesAchievementRewardCommand(null, handler);
                HandleReloadLocalesCreatureCommand(null, handler);
                HandleReloadLocalesCreatureTextCommand(null, handler);
                HandleReloadLocalesGameobjectCommand(null, handler);
                HandleReloadLocalesGossipMenuOptionCommand(null, handler);
                HandleReloadLocalesPageTextCommand(null, handler);
                HandleReloadLocalesPointsOfInterestCommand(null, handler);
                HandleReloadQuestLocaleCommand(null, handler);
                return true;
            }

            [Command("loot", RBACPermissions.CommandReloadAllLoot, true)]
            static bool HandleReloadAllLootCommand(StringArguments args, CommandHandler handler)
            {
                Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables...");
                LootManager.LoadLootTables();
                handler.SendGlobalGMSysMessage("DB tables `*_loot_template` reloaded.");
                Global.ConditionMgr.LoadConditions(true);
                return true;
            }

            [Command("npc", RBACPermissions.CommandReloadAllNpc, true)]
            static bool HandleReloadAllNpcCommand(StringArguments args, CommandHandler handler)
            {
                if (args != null)                                          // will be reloaded from all_gossips
                {
                    HandleReloadTrainerCommand(null, handler);
                    HandleReloadNpcVendorCommand(null, handler);
                    HandleReloadPointsOfInterestCommand(null, handler);
                    HandleReloadSpellClickSpellsCommand(null, handler);
                }
                return true;
            }

            [Command("quest", RBACPermissions.CommandReloadAllQuest, true)]
            static bool HandleReloadAllQuestCommand(StringArguments args, CommandHandler handler)
            {
                HandleReloadQuestAreaTriggersCommand(null, handler);
                HandleReloadQuestGreetingCommand(null, handler);
                HandleReloadQuestPOICommand(null, handler);
                HandleReloadQuestTemplateCommand(null, handler);

                Log.outInfo(LogFilter.Server, "Re-Loading Quests Relations...");
                Global.ObjectMgr.LoadQuestStartersAndEnders();
                handler.SendGlobalGMSysMessage("DB tables `*_queststarter` and `*_questender` reloaded.");
                return true;
            }

            [Command("scripts", RBACPermissions.CommandReloadAllScripts, true)]
            static bool HandleReloadAllScriptsCommand(StringArguments args, CommandHandler handler)
            {
                if (Global.MapMgr.IsScriptScheduled())
                {
                    handler.SendSysMessage("DB scripts used currently, please attempt reload later.");
                    return false;
                }

                Log.outInfo(LogFilter.Server, "Re-Loading Scripts...");
                HandleReloadEventScriptsCommand(null, handler);
                HandleReloadSpellScriptsCommand(null, handler);
                handler.SendGlobalGMSysMessage("DB tables `*_scripts` reloaded.");
                HandleReloadWpScriptsCommand(null, handler);
                HandleReloadWpCommand(null, handler);
                return true;
            }

            [Command("spell", RBACPermissions.CommandReloadAllSpell, true)]
            static bool HandleReloadAllSpellCommand(StringArguments args, CommandHandler handler)
            {
                HandleReloadSkillDiscoveryTemplateCommand(null, handler);
                HandleReloadSkillExtraItemTemplateCommand(null, handler);
                HandleReloadSpellRequiredCommand(null, handler);
                HandleReloadSpellAreaCommand(null, handler);
                HandleReloadSpellGroupsCommand(null, handler);
                HandleReloadSpellLearnSpellCommand(null, handler);
                HandleReloadSpellLinkedSpellCommand(null, handler);
                HandleReloadSpellProcsCommand(null, handler);
                HandleReloadSpellTargetPositionCommand(null, handler);
                HandleReloadSpellThreatsCommand(null, handler);
                HandleReloadSpellGroupStackRulesCommand(null, handler);
                HandleReloadSpellPetAurasCommand(null, handler);
                return true;
            }
        }
    }
}
