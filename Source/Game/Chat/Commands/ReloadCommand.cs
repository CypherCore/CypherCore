// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.Entities;
using Game.Loots;
using Game.Spells;

namespace Game.Chat
{
	[CommandGroup("reload")]
	internal class ReloadCommand
	{
		[Command("access_requirement", RBACPermissions.CommandReloadAccessRequirement, true)]
		private static bool HandleReloadAccessRequirementCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Access Requirement definitions...");
			Global.ObjectMgr.LoadAccessRequirements();
			handler.SendGlobalGMSysMessage("DB table `access_requirement` reloaded.");

			return true;
		}

		[Command("achievement_reward", RBACPermissions.CommandReloadAchievementReward, true)]
		private static bool HandleReloadAchievementRewardCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Achievement Reward Data...");
			Global.AchievementMgr.LoadRewards();
			handler.SendGlobalGMSysMessage("DB table `achievement_reward` reloaded.");

			return true;
		}

		[Command("areatrigger_involvedrelation", RBACPermissions.CommandReloadAreatriggerInvolvedrelation, true)]
		private static bool HandleReloadQuestAreaTriggersCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Quest Area Triggers...");
			Global.ObjectMgr.LoadQuestAreaTriggers();
			handler.SendGlobalGMSysMessage("DB table `areatrigger_involvedrelation` (quest area triggers) reloaded.");

			return true;
		}

		[Command("areatrigger_tavern", RBACPermissions.CommandReloadAreatriggerTavern, true)]
		private static bool HandleReloadAreaTriggerTavernCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Tavern Area Triggers...");
			Global.ObjectMgr.LoadTavernAreaTriggers();
			handler.SendGlobalGMSysMessage("DB table `areatrigger_tavern` reloaded.");

			return true;
		}

		[Command("areatrigger_teleport", RBACPermissions.CommandReloadAreatriggerTeleport, true)]
		private static bool HandleReloadAreaTriggerTeleportCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading AreaTrigger teleport definitions...");
			Global.ObjectMgr.LoadAreaTriggerTeleports();
			handler.SendGlobalGMSysMessage("DB table `areatrigger_teleport` reloaded.");

			return true;
		}

		[Command("areatrigger_template", RBACPermissions.CommandReloadSceneTemplate, true)]
		private static bool HandleReloadAreaTriggerTemplateCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Reloading areatrigger_template table...");
			Global.AreaTriggerDataStorage.LoadAreaTriggerTemplates();
			handler.SendGlobalGMSysMessage("AreaTrigger templates reloaded. Already spawned AT won't be affected. New scriptname need a reboot.");

			return true;
		}

		[Command("auctions", RBACPermissions.CommandReloadAuctions, true)]
		private static bool HandleReloadAuctionsCommand(CommandHandler handler)
		{
			// Reload dynamic _data tables from the database
			Log.outInfo(LogFilter.Server, "Re-Loading Auctions...");
			Global.AuctionHouseMgr.LoadAuctions();
			handler.SendGlobalGMSysMessage("Auctions reloaded.");

			return true;
		}

		[Command("autobroadcast", RBACPermissions.CommandReloadAutobroadcast, true)]
		private static bool HandleReloadAutobroadcastCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Autobroadcasts...");
			Global.WorldMgr.LoadAutobroadcasts();
			handler.SendGlobalGMSysMessage("DB table `autobroadcast` reloaded.");

			return true;
		}

		[Command("battleground_template", RBACPermissions.CommandReloadBattlegroundTemplate, true)]
		private static bool HandleReloadBattlegroundTemplate(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Misc, "Re-Loading Battleground Templates...");
			Global.BattlegroundMgr.LoadBattlegroundTemplates();
			handler.SendGlobalGMSysMessage("DB table `battleground_template` reloaded.");

			return true;
		}

		[Command("character_template", RBACPermissions.CommandReloadCharacterTemplate, true)]
		private static bool HandleReloadCharacterTemplate(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Character Templates...");
			Global.CharacterTemplateDataStorage.LoadCharacterTemplates();
			handler.SendGlobalGMSysMessage("DB table `character_template` and `character_template_class` reloaded.");

			return true;
		}

		[Command("conditions", RBACPermissions.CommandReloadConditions, true)]
		private static bool HandleReloadConditions(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Conditions...");
			Global.ConditionMgr.LoadConditions(true);
			handler.SendGlobalGMSysMessage("Conditions reloaded.");

			return true;
		}

		[Command("config", RBACPermissions.CommandReloadConfig, true)]
		private static bool HandleReloadConfigCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading config settings...");
			Global.WorldMgr.LoadConfigSettings(true);
			Global.MapMgr.InitializeVisibilityDistanceInfo();
			handler.SendGlobalGMSysMessage("World config settings reloaded.");

			return true;
		}

		[Command("conversation_template", RBACPermissions.CommandReloadConversationTemplate, true)]
		private static bool HandleReloadConversationTemplateCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Reloading conversation_* tables...");
			Global.ConversationDataStorage.LoadConversationTemplates();
			handler.SendGlobalGMSysMessage("Conversation templates reloaded.");

			return true;
		}

		[Command("creature_linked_respawn", RBACPermissions.CommandReloadCreatureLinkedRespawn, true)]
		private static bool HandleReloadLinkedRespawnCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Loading Linked Respawns... (`creature_linked_respawn`)");
			Global.ObjectMgr.LoadLinkedRespawn();
			handler.SendGlobalGMSysMessage("DB table `creature_linked_respawn` (creature linked respawns) reloaded.");

			return true;
		}

		[Command("creature_loot_template", RBACPermissions.CommandReloadCreatureLootTemplate, true)]
		private static bool HandleReloadLootTemplatesCreatureCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`creature_loot_template`)");
			LootManager.LoadLootTemplates_Creature();
			LootStorage.Creature.CheckLootRefs();
			handler.SendGlobalGMSysMessage("DB table `creature_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("creature_movement_override", RBACPermissions.CommandReloadCreatureMovementOverride, true)]
		private static bool HandleReloadCreatureMovementOverrideCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Creature movement overrides...");
			Global.ObjectMgr.LoadCreatureMovementOverrides();
			handler.SendGlobalGMSysMessage("DB table `creature_movement_override` reloaded.");

			return true;
		}

		[Command("creature_onkill_reputation", RBACPermissions.CommandReloadCreatureOnkillReputation, true)]
		private static bool HandleReloadOnKillReputationCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading creature award reputation definitions...");
			Global.ObjectMgr.LoadReputationOnKill();
			handler.SendGlobalGMSysMessage("DB table `creature_onkill_reputation` reloaded.");

			return true;
		}

		[Command("creature_questender", RBACPermissions.CommandReloadCreatureQuestender, true)]
		private static bool HandleReloadCreatureQuestEnderCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Loading Quests Relations... (`creature_questender`)");
			Global.ObjectMgr.LoadCreatureQuestEnders();
			handler.SendGlobalGMSysMessage("DB table `creature_questender` reloaded.");

			return true;
		}

		[Command("creature_queststarter", RBACPermissions.CommandReloadCreatureQueststarter, true)]
		private static bool HandleReloadCreatureQuestStarterCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Loading Quests Relations... (`creature_queststarter`)");
			Global.ObjectMgr.LoadCreatureQuestStarters();
			handler.SendGlobalGMSysMessage("DB table `creature_queststarter` reloaded.");

			return true;
		}

		[Command("creature_summon_groups", RBACPermissions.CommandReloadCreatureSummonGroups, true)]
		private static bool HandleReloadCreatureSummonGroupsCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Reloading creature summon groups...");
			Global.ObjectMgr.LoadTempSummons();
			handler.SendGlobalGMSysMessage("DB table `creature_summon_groups` reloaded.");

			return true;
		}

		[Command("creature_template", RBACPermissions.CommandReloadCreatureTemplate, true)]
		private static bool HandleReloadCreatureTemplateCommand(CommandHandler handler, StringArguments args)
		{
			if (args.Empty())
				return false;

			uint entry;

			while ((entry = args.NextUInt32()) != 0)
			{
				PreparedStatement stmt = DB.World.GetPreparedStatement(WorldStatements.SEL_CREATURE_TEMPLATE);
				stmt.AddValue(0, entry);
				stmt.AddValue(1, 0);
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

			Global.ObjectMgr.InitializeQueriesData(QueryDataGroup.Creatures);
			handler.SendGlobalGMSysMessage("Creature template reloaded.");

			return true;
		}

		[Command("creature_text", RBACPermissions.CommandReloadCreatureText, true)]
		private static bool HandleReloadCreatureText(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Creature Texts...");
			Global.CreatureTextMgr.LoadCreatureTexts();
			handler.SendGlobalGMSysMessage("Creature Texts reloaded.");

			return true;
		}

		[Command("trinity_string", RBACPermissions.CommandReloadCypherString, true)]
		private static bool HandleReloadCypherStringCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading trinity_string Table!");
			Global.ObjectMgr.LoadCypherStrings();
			handler.SendGlobalGMSysMessage("DB table `trinity_string` reloaded.");

			return true;
		}

		[Command("criteria_data", RBACPermissions.CommandReloadCriteriaData, true)]
		private static bool HandleReloadCriteriaDataCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Additional Criteria Data...");
			Global.CriteriaMgr.LoadCriteriaData();
			handler.SendGlobalGMSysMessage("DB table `criteria_data` reloaded.");

			return true;
		}

		[Command("disables", RBACPermissions.CommandReloadDisables, true)]
		private static bool HandleReloadDisablesCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading disables table...");
			Global.DisableMgr.LoadDisables();
			Log.outInfo(LogFilter.Server, "Checking quest disables...");
			Global.DisableMgr.CheckQuestDisables();
			handler.SendGlobalGMSysMessage("DB table `disables` reloaded.");

			return true;
		}

		[Command("disenchant_loot_template", RBACPermissions.CommandReloadDisenchantLootTemplate, true)]
		private static bool HandleReloadLootTemplatesDisenchantCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`disenchant_loot_template`)");
			LootManager.LoadLootTemplates_Disenchant();
			LootStorage.Disenchant.CheckLootRefs();
			handler.SendGlobalGMSysMessage("DB table `disenchant_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("event_scripts", RBACPermissions.CommandReloadEventScripts, true)]
		private static bool HandleReloadEventScriptsCommand(CommandHandler handler, StringArguments args)
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
		private static bool HandleReloadLootTemplatesFishingCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`fishing_loot_template`)");
			LootManager.LoadLootTemplates_Fishing();
			LootStorage.Fishing.CheckLootRefs();
			handler.SendGlobalGMSysMessage("DB table `fishing_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("graveyard_zone", RBACPermissions.CommandReloadGraveyardZone, true)]
		private static bool HandleReloadGameGraveyardZoneCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Graveyard-zone links...");

			Global.ObjectMgr.LoadGraveyardZones();

			handler.SendGlobalGMSysMessage("DB table `game_graveyard_zone` reloaded.");

			return true;
		}

		[Command("game_tele", RBACPermissions.CommandReloadGameTele, true)]
		private static bool HandleReloadGameTeleCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Game Tele coordinates...");

			Global.ObjectMgr.LoadGameTele();

			handler.SendGlobalGMSysMessage("DB table `game_tele` reloaded.");

			return true;
		}

		[Command("gameobject_loot_template", RBACPermissions.CommandReloadGameobjectQuestLootTemplate, true)]
		private static bool HandleReloadLootTemplatesGameobjectCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`gameobject_loot_template`)");
			LootManager.LoadLootTemplates_Gameobject();
			LootStorage.Gameobject.CheckLootRefs();
			handler.SendGlobalGMSysMessage("DB table `gameobject_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("gameobject_questender", RBACPermissions.CommandReloadGameobjectQuestender, true)]
		private static bool HandleReloadGOQuestEnderCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Loading Quests Relations... (`gameobject_questender`)");
			Global.ObjectMgr.LoadGameobjectQuestEnders();
			handler.SendGlobalGMSysMessage("DB table `gameobject_questender` reloaded.");

			return true;
		}

		[Command("gameobject_queststarter", RBACPermissions.CommandReloadGameobjectQueststarter, true)]
		private static bool HandleReloadGOQuestStarterCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Loading Quests Relations... (`gameobject_queststarter`)");
			Global.ObjectMgr.LoadGameobjectQuestStarters();
			handler.SendGlobalGMSysMessage("DB table `gameobject_queststarter` reloaded.");

			return true;
		}

		[Command("gossip_menu", RBACPermissions.CommandReloadGossipMenu, true)]
		private static bool HandleReloadGossipMenuCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading `gossip_menu` Table!");
			Global.ObjectMgr.LoadGossipMenu();
			handler.SendGlobalGMSysMessage("DB table `gossip_menu` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("gossip_menu_option", RBACPermissions.CommandReloadGossipMenuOption, true)]
		private static bool HandleReloadGossipMenuOptionCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading `gossip_menu_option` Table!");
			Global.ObjectMgr.LoadGossipMenuItems();
			handler.SendGlobalGMSysMessage("DB table `gossip_menu_option` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("item_random_bonus_list_template", RBACPermissions.CommandReloadItemRandomBonusListTemplate, true)]
		private static bool HandleReloadItemRandomBonusListTemplatesCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Random Item bonus list definitions...");
			ItemEnchantmentManager.LoadItemRandomBonusListTemplates();
			handler.SendGlobalGMSysMessage("DB table `item_random_bonus_list_template` reloaded.");

			return true;
		}

		[Command("item_loot_template", RBACPermissions.CommandReloadItemLootTemplate, true)]
		private static bool HandleReloadLootTemplatesItemCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`item_loot_template`)");
			LootManager.LoadLootTemplates_Item();
			LootStorage.Items.CheckLootRefs();
			handler.SendGlobalGMSysMessage("DB table `item_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("lfg_dungeon_rewards", RBACPermissions.CommandReloadLfgDungeonRewards, true)]
		private static bool HandleReloadLfgRewardsCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading lfg dungeon rewards...");
			Global.LFGMgr.LoadRewards();
			handler.SendGlobalGMSysMessage("DB table `lfg_dungeon_rewards` reloaded.");

			return true;
		}

		[Command("achievement_reward_locale", RBACPermissions.CommandReloadAchievementRewardLocale, true)]
		private static bool HandleReloadAchievementRewardLocaleCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Achievement Reward Data Locale...");
			Global.AchievementMgr.LoadRewardLocales();
			handler.SendGlobalGMSysMessage("DB table `achievement_reward_locale` reloaded.");

			return true;
		}

		[Command("creature_template_locale", RBACPermissions.CommandReloadCreatureTemplateLocale, true)]
		private static bool HandleReloadCreatureTemplateLocaleCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Creature Template Locale...");
			Global.ObjectMgr.LoadCreatureLocales();
			handler.SendGlobalGMSysMessage("DB table `Creature Template Locale` reloaded.");

			return true;
		}

		[Command("creature_text_locale", RBACPermissions.CommandReloadCreatureTextLocale, true)]
		private static bool HandleReloadCreatureTextLocaleCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Creature Texts Locale...");
			Global.CreatureTextMgr.LoadCreatureTextLocales();
			handler.SendGlobalGMSysMessage("DB table `creature_text_locale` reloaded.");

			return true;
		}

		[Command("gameobject_template_locale", RBACPermissions.CommandReloadGameobjectTemplateLocale, true)]
		private static bool HandleReloadGameobjectTemplateLocaleCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Gameobject Template Locale... ");
			Global.ObjectMgr.LoadGameObjectLocales();
			handler.SendGlobalGMSysMessage("DB table `gameobject_template_locale` reloaded.");

			return true;
		}

		[Command("gossip_menu_option_locale", RBACPermissions.CommandReloadGossipMenuOptionLocale, true)]
		private static bool HandleReloadGossipMenuOptionLocaleCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Gossip Menu Option Locale... ");
			Global.ObjectMgr.LoadGossipMenuItemsLocales();
			handler.SendGlobalGMSysMessage("DB table `gossip_menu_option_locale` reloaded.");

			return true;
		}

		[Command("page_text_locale", RBACPermissions.CommandReloadPageTextLocale, true)]
		private static bool HandleReloadPageTextLocaleCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Page Text Locale... ");
			Global.ObjectMgr.LoadPageTextLocales();
			handler.SendGlobalGMSysMessage("DB table `page_text_locale` reloaded.");

			return true;
		}

		[Command("points_of_interest_locale", RBACPermissions.CommandReloadPointsOfInterestLocale, true)]
		private static bool HandleReloadPointsOfInterestLocaleCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Points Of Interest Locale... ");
			Global.ObjectMgr.LoadPointOfInterestLocales();
			handler.SendGlobalGMSysMessage("DB table `points_of_interest_locale` reloaded.");

			return true;
		}

		[Command("mail_level_reward", RBACPermissions.CommandReloadMailLevelReward, true)]
		private static bool HandleReloadMailLevelRewardCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Player level dependent mail rewards...");
			Global.ObjectMgr.LoadMailLevelRewards();
			handler.SendGlobalGMSysMessage("DB table `mail_level_reward` reloaded.");

			return true;
		}

		[Command("mail_loot_template", RBACPermissions.CommandReloadMailLootTemplate, true)]
		private static bool HandleReloadLootTemplatesMailCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`mail_loot_template`)");
			LootManager.LoadLootTemplates_Mail();
			LootStorage.Mail.CheckLootRefs();
			handler.SendGlobalGMSysMessage("DB table `mail_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("milling_loot_template", RBACPermissions.CommandReloadMillingLootTemplate, true)]
		private static bool HandleReloadLootTemplatesMillingCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`milling_loot_template`)");
			LootManager.LoadLootTemplates_Milling();
			LootStorage.Milling.CheckLootRefs();
			handler.SendGlobalGMSysMessage("DB table `milling_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("npc_spellclick_spells", RBACPermissions.CommandReloadNpcSpellclickSpells, true)]
		private static bool HandleReloadSpellClickSpellsCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading `npc_spellclick_spells` Table!");
			Global.ObjectMgr.LoadNPCSpellClickSpells();
			handler.SendGlobalGMSysMessage("DB table `npc_spellclick_spells` reloaded.");

			return true;
		}

		[Command("npc_vendor", RBACPermissions.CommandReloadNpcVendor, true)]
		private static bool HandleReloadNpcVendorCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading `npc_vendor` Table!");
			Global.ObjectMgr.LoadVendors();
			handler.SendGlobalGMSysMessage("DB table `npc_vendor` reloaded.");

			return true;
		}

		[Command("page_text", RBACPermissions.CommandReloadPageText, true)]
		private static bool HandleReloadPageTextsCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Page Text...");
			Global.ObjectMgr.LoadPageTexts();
			handler.SendGlobalGMSysMessage("DB table `page_text` reloaded.");

			return true;
		}

		[Command("pickpocketing_loot_template", RBACPermissions.CommandReloadPickpocketingLootTemplate, true)]
		private static bool HandleReloadLootTemplatesPickpocketingCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`pickpocketing_loot_template`)");
			LootManager.LoadLootTemplates_Pickpocketing();
			LootStorage.Pickpocketing.CheckLootRefs();
			handler.SendGlobalGMSysMessage("DB table `pickpocketing_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("points_of_interest", RBACPermissions.CommandReloadPointsOfInterest, true)]
		private static bool HandleReloadPointsOfInterestCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading `points_of_interest` Table!");
			Global.ObjectMgr.LoadPointsOfInterest();
			handler.SendGlobalGMSysMessage("DB table `points_of_interest` reloaded.");

			return true;
		}

		[Command("prospecting_loot_template", RBACPermissions.CommandReloadProspectingLootTemplate, true)]
		private static bool HandleReloadLootTemplatesProspectingCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`prospecting_loot_template`)");
			LootManager.LoadLootTemplates_Prospecting();
			LootStorage.Prospecting.CheckLootRefs();
			handler.SendGlobalGMSysMessage("DB table `prospecting_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("quest_greeting", RBACPermissions.CommandReloadQuestGreeting, true)]
		private static bool HandleReloadQuestGreetingCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Quest Greeting ... ");
			Global.ObjectMgr.LoadQuestGreetings();
			handler.SendGlobalGMSysMessage("DB table `quest_greeting` reloaded.");

			return true;
		}

		[Command("quest_locale", RBACPermissions.CommandReloadQuestTemplateLocale, true)]
		private static bool HandleReloadQuestTemplateLocaleCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Quest Locale... ");
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
		private static bool HandleReloadQuestPOICommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Quest POI ...");
			Global.ObjectMgr.LoadQuestPOI();
			Global.ObjectMgr.InitializeQueriesData(QueryDataGroup.POIs);
			handler.SendGlobalGMSysMessage("DB Table `quest_poi` and `quest_poi_points` reloaded.");

			return true;
		}

		[Command("quest_template", RBACPermissions.CommandReloadQuestTemplate, true)]
		private static bool HandleReloadQuestTemplateCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Quest Templates...");
			Global.ObjectMgr.LoadQuests();
			Global.ObjectMgr.InitializeQueriesData(QueryDataGroup.Quests);
			handler.SendGlobalGMSysMessage("DB table `quest_template` (quest definitions) reloaded.");

			// dependent also from `gameobject` but this table not reloaded anyway
			Log.outInfo(LogFilter.Server, "Re-Loading GameObjects for quests...");
			Global.ObjectMgr.LoadGameObjectForQuests();
			handler.SendGlobalGMSysMessage("Data GameObjects for quests reloaded.");

			return true;
		}

		[Command("rbac", RBACPermissions.CommandReloadRbac, true)]
		private static bool HandleReloadRBACCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Reloading RBAC tables...");
			Global.AccountMgr.LoadRBAC();
			Global.WorldMgr.ReloadRBAC();
			handler.SendGlobalGMSysMessage("RBAC _data reloaded.");

			return true;
		}

		[Command("reference_loot_template", RBACPermissions.CommandReloadReferenceLootTemplate, true)]
		private static bool HandleReloadLootTemplatesReferenceCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`reference_loot_template`)");
			LootManager.LoadLootTemplates_Reference();
			handler.SendGlobalGMSysMessage("DB table `reference_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("reputation_reward_rate", RBACPermissions.CommandReloadReputationRewardRate, true)]
		private static bool HandleReloadReputationRewardRateCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading `reputation_reward_rate` Table!");
			Global.ObjectMgr.LoadReputationRewardRate();
			handler.SendGlobalSysMessage("DB table `reputation_reward_rate` reloaded.");

			return true;
		}

		[Command("reputation_spillover_template", RBACPermissions.CommandReloadSpilloverTemplate, true)]
		private static bool HandleReloadReputationSpilloverTemplateCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading `reputation_spillover_template` Table!");
			Global.ObjectMgr.LoadReputationSpilloverTemplate();
			handler.SendGlobalSysMessage("DB table `reputation_spillover_template` reloaded.");

			return true;
		}

		[Command("reserved_name", RBACPermissions.CommandReloadReservedName, true)]
		private static bool HandleReloadReservedNameCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Loading ReservedNames... (`reserved_name`)");
			Global.ObjectMgr.LoadReservedPlayersNames();
			handler.SendGlobalGMSysMessage("DB table `reserved_name` (player reserved names) reloaded.");

			return true;
		}

		[Command("scene_template", RBACPermissions.CommandReloadSceneTemplate, true)]
		private static bool HandleReloadSceneTemplateCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Misc, "Reloading scene_template table...");
			Global.ObjectMgr.LoadSceneTemplates();
			handler.SendGlobalGMSysMessage("Scenes templates reloaded. New scriptname need a reboot.");

			return true;
		}

		[Command("skill_discovery_template", RBACPermissions.CommandReloadSkillDiscoveryTemplate, true)]
		private static bool HandleReloadSkillDiscoveryTemplateCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Skill Discovery Table...");
			SkillDiscovery.LoadSkillDiscoveryTable();
			handler.SendGlobalGMSysMessage("DB table `skill_discovery_template` (recipes discovered at crafting) reloaded.");

			return true;
		}

		private static bool HandleReloadSkillPerfectItemTemplateCommand(CommandHandler handler)
		{
			// latched onto HandleReloadSkillExtraItemTemplateCommand as it's part of that table group (and i don't want to chance all the command IDs)
			Log.outInfo(LogFilter.Misc, "Re-Loading Skill Perfection Data Table...");
			SkillPerfectItems.LoadSkillPerfectItemTable();
			handler.SendGlobalGMSysMessage("DB table `skill_perfect_item_template` (perfect Item procs when crafting) reloaded.");

			return true;
		}

		[Command("skill_extra_item_template", RBACPermissions.CommandReloadSkillExtraItemTemplate, true)]
		private static bool HandleReloadSkillExtraItemTemplateCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Skill Extra Item Table...");
			SkillExtraItems.LoadSkillExtraItemTable();
			handler.SendGlobalGMSysMessage("DB table `skill_extra_item_template` (extra Item creation when crafting) reloaded.");

			return HandleReloadSkillPerfectItemTemplateCommand(handler);
		}

		[Command("skill_fishing_base_level", RBACPermissions.CommandReloadSkillFishingBaseLevel, true)]
		private static bool HandleReloadSkillFishingBaseLevelCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Skill Fishing base level requirements...");
			Global.ObjectMgr.LoadFishingBaseSkillLevel();
			handler.SendGlobalGMSysMessage("DB table `skill_fishing_base_level` (fishing base level for zone/subzone) reloaded.");

			return true;
		}

		[Command("skinning_loot_template", RBACPermissions.CommandReloadSkinningLootTemplate, true)]
		private static bool HandleReloadLootTemplatesSkinningCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`skinning_loot_template`)");
			LootManager.LoadLootTemplates_Skinning();
			LootStorage.Skinning.CheckLootRefs();
			handler.SendGlobalGMSysMessage("DB table `skinning_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("smart_scripts", RBACPermissions.CommandReloadSmartScripts, true)]
		private static bool HandleReloadSmartScripts(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Smart Scripts...");
			Global.SmartAIMgr.LoadFromDB();
			handler.SendGlobalGMSysMessage("Smart Scripts reloaded.");

			return true;
		}

		[Command("spell_area", RBACPermissions.CommandReloadSpellArea, true)]
		private static bool HandleReloadSpellAreaCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading SpellArea Data...");
			Global.SpellMgr.LoadSpellAreas();
			handler.SendGlobalGMSysMessage("DB table `spell_area` (spell dependences from area/quest/Auras State) reloaded.");

			return true;
		}

		[Command("spell_group", RBACPermissions.CommandReloadSpellGroup, true)]
		private static bool HandleReloadSpellGroupsCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Spell Groups...");
			Global.SpellMgr.LoadSpellGroups();
			handler.SendGlobalGMSysMessage("DB table `spell_group` (spell groups) reloaded.");

			return true;
		}

		[Command("spell_group_stack_rules", RBACPermissions.CommandReloadSpellGroupStackRules, true)]
		private static bool HandleReloadSpellGroupStackRulesCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Spell Group Stack Rules...");
			Global.SpellMgr.LoadSpellGroupStackRules();
			handler.SendGlobalGMSysMessage("DB table `spell_group_stack_rules` (spell stacking definitions) reloaded.");

			return true;
		}

		[Command("spell_learn_spell", RBACPermissions.CommandReloadSpellLearnSpell, true)]
		private static bool HandleReloadSpellLearnSpellCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Spell Learn Spells...");
			Global.SpellMgr.LoadSpellLearnSpells();
			handler.SendGlobalGMSysMessage("DB table `spell_learn_spell` reloaded.");

			return true;
		}

		[Command("spell_linked_spell", RBACPermissions.CommandReloadSpellLinkedSpell, true)]
		private static bool HandleReloadSpellLinkedSpellCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Spell Linked Spells...");
			Global.SpellMgr.LoadSpellLinked();
			handler.SendGlobalGMSysMessage("DB table `spell_linked_spell` reloaded.");

			return true;
		}

		[Command("spell_loot_template", RBACPermissions.CommandReloadSpellLootTemplate, true)]
		private static bool HandleReloadLootTemplatesSpellCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables... (`spell_loot_template`)");
			LootManager.LoadLootTemplates_Spell();
			LootStorage.Spell.CheckLootRefs();
			handler.SendGlobalGMSysMessage("DB table `spell_loot_template` reloaded.");
			Global.ConditionMgr.LoadConditions(true);

			return true;
		}

		[Command("spell_pet_auras", RBACPermissions.CommandReloadSpellPetAuras, true)]
		private static bool HandleReloadSpellPetAurasCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Spell pet Auras...");
			Global.SpellMgr.LoadSpellPetAuras();
			handler.SendGlobalGMSysMessage("DB table `spell_pet_auras` reloaded.");

			return true;
		}

		[Command("spell_proc", RBACPermissions.CommandReloadSpellProc, true)]
		private static bool HandleReloadSpellProcsCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Spell Proc conditions and _data...");
			Global.SpellMgr.LoadSpellProcs();
			handler.SendGlobalGMSysMessage("DB table `spell_proc` (spell proc conditions and _data) reloaded.");

			return true;
		}

		[Command("spell_required", RBACPermissions.CommandReloadSpellRequired, true)]
		private static bool HandleReloadSpellRequiredCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Spell Required Data... ");
			Global.SpellMgr.LoadSpellRequired();
			handler.SendGlobalGMSysMessage("DB table `spell_required` reloaded.");

			return true;
		}

		[Command("spell_scripts", RBACPermissions.CommandReloadSpellScripts, true)]
		private static bool HandleReloadSpellScriptsCommand(CommandHandler handler, StringArguments args)
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

		[Command("spell_script_names", RBACPermissions.CommandReloadSpellScriptNames, true)]
		private static bool HandleReloadSpellScriptNamesCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Misc, "Reloading spell_script_names table...");
			Global.ObjectMgr.LoadSpellScriptNames();
			//Global.ScriptMgr.NotifyScriptIDUpdate();
			Global.ObjectMgr.ValidateSpellScripts();
			handler.SendGlobalGMSysMessage("Spell scripts reloaded.");

			return true;
		}

		[Command("spell_target_position", RBACPermissions.CommandReloadSpellTargetPosition, true)]
		private static bool HandleReloadSpellTargetPositionCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Spell Target coordinates...");
			Global.SpellMgr.LoadSpellTargetPositions();
			handler.SendGlobalGMSysMessage("DB table `spell_target_position` (destination coordinates for spell targets) reloaded.");

			return true;
		}

		[Command("spell_threats", RBACPermissions.CommandReloadSpellThreats, true)]
		private static bool HandleReloadSpellThreatsCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Aggro Spells Definitions...");
			Global.SpellMgr.LoadSpellThreats();
			handler.SendGlobalGMSysMessage("DB table `spell_threat` (spell aggro definitions) reloaded.");

			return true;
		}

		[Command("support", RBACPermissions.CommandReloadSupportSystem, true)]
		private static bool HandleReloadSupportSystemCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Re-Loading Support System Tables...");
			Global.SupportMgr.LoadBugTickets();
			Global.SupportMgr.LoadComplaintTickets();
			Global.SupportMgr.LoadSuggestionTickets();
			handler.SendGlobalGMSysMessage("DB tables `gm_*` reloaded.");

			return true;
		}

		[Command("trainer", RBACPermissions.CommandReloadTrainer, true)]
		private static bool HandleReloadTrainerCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Misc, "Re-Loading `trainer` Table!");
			Global.ObjectMgr.LoadTrainers();
			Global.ObjectMgr.LoadCreatureTrainers();
			handler.SendGlobalGMSysMessage("DB table `trainer` reloaded.");
			handler.SendGlobalGMSysMessage("DB table `trainer_locale` reloaded.");
			handler.SendGlobalGMSysMessage("DB table `trainer_spell` reloaded.");
			handler.SendGlobalGMSysMessage("DB table `creature_trainer` reloaded.");

			return true;
		}

		[Command("vehicle_accessory", RBACPermissions.CommandReloadVehicleAccesory, true)]
		private static bool HandleReloadVehicleAccessoryCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Reloading vehicle_accessory table...");
			Global.ObjectMgr.LoadVehicleAccessories();
			handler.SendGlobalGMSysMessage("Vehicle accessories reloaded.");

			return true;
		}

		[Command("vehicle_template", RBACPermissions.CommandReloadVehicleTemplate, true)]
		private static bool HandleReloadVehicleTemplateCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Reloading vehicle_template table...");
			Global.ObjectMgr.LoadVehicleTemplate();
			handler.SendGlobalGMSysMessage("Vehicle templates reloaded.");

			return true;
		}

		[Command("vehicle_template_accessory", RBACPermissions.CommandReloadVehicleTemplateAccessory, true)]
		private static bool HandleReloadVehicleTemplateAccessoryCommand(CommandHandler handler)
		{
			Log.outInfo(LogFilter.Server, "Reloading vehicle_template_accessory table...");
			Global.ObjectMgr.LoadVehicleTemplateAccessories();
			handler.SendGlobalGMSysMessage("Vehicle template accessories reloaded.");

			return true;
		}

		[Command("waypoint_data", RBACPermissions.CommandReloadWaypointData, true)]
		private static bool HandleReloadWpCommand(CommandHandler handler, StringArguments args)
		{
			if (args != null)
				Log.outInfo(LogFilter.Server, "Re-Loading Waypoints _data from 'waypoints_data'");

			Global.WaypointMgr.Load();

			if (args != null)
				handler.SendGlobalGMSysMessage("DB Table 'waypoint_data' reloaded.");

			return true;
		}

		[Command("waypoint_scripts", RBACPermissions.CommandReloadWaypointScripts, true)]
		private static bool HandleReloadWpScriptsCommand(CommandHandler handler, StringArguments args)
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

		[CommandGroup("all")]
		private class AllCommand
		{
			[Command("", RBACPermissions.CommandReloadAll, true)]
			private static bool HandleReloadAllCommand(CommandHandler handler)
			{
				HandleReloadSkillFishingBaseLevelCommand(handler);

				HandleReloadAllAchievementCommand(handler);
				HandleReloadAllAreaCommand(handler);
				HandleReloadAllLootCommand(handler);
				HandleReloadAllNpcCommand(handler);
				HandleReloadAllQuestCommand(handler);
				HandleReloadAllSpellCommand(handler);
				HandleReloadAllItemCommand(handler);
				HandleReloadAllGossipsCommand(handler);
				HandleReloadAllLocalesCommand(handler);

				HandleReloadAccessRequirementCommand(handler);
				HandleReloadMailLevelRewardCommand(handler);
				HandleReloadReservedNameCommand(handler);
				HandleReloadCypherStringCommand(handler);
				HandleReloadGameTeleCommand(handler);

				HandleReloadCreatureMovementOverrideCommand(handler);
				HandleReloadCreatureSummonGroupsCommand(handler);

				HandleReloadVehicleAccessoryCommand(handler);
				HandleReloadVehicleTemplateAccessoryCommand(handler);

				HandleReloadAutobroadcastCommand(handler);
				HandleReloadBattlegroundTemplate(handler);
				HandleReloadCharacterTemplate(handler);

				return true;
			}

			[Command("Achievement", RBACPermissions.CommandReloadAllAchievement, true)]
			private static bool HandleReloadAllAchievementCommand(CommandHandler handler)
			{
				HandleReloadCriteriaDataCommand(handler);
				HandleReloadAchievementRewardCommand(handler);

				return true;
			}

			[Command("area", RBACPermissions.CommandReloadAllArea, true)]
			private static bool HandleReloadAllAreaCommand(CommandHandler handler)
			{
				HandleReloadAreaTriggerTeleportCommand(handler);
				HandleReloadAreaTriggerTavernCommand(handler);
				HandleReloadGameGraveyardZoneCommand(handler);

				return true;
			}

			[Command("gossips", RBACPermissions.CommandReloadAllGossip, true)]
			private static bool HandleReloadAllGossipsCommand(CommandHandler handler)
			{
				HandleReloadGossipMenuCommand(handler);
				HandleReloadGossipMenuOptionCommand(handler);
				HandleReloadPointsOfInterestCommand(handler);

				return true;
			}

			[Command("Item", RBACPermissions.CommandReloadAllItem, true)]
			private static bool HandleReloadAllItemCommand(CommandHandler handler)
			{
				HandleReloadPageTextsCommand(handler);
				HandleReloadItemRandomBonusListTemplatesCommand(handler);

				return true;
			}

			[Command("locales", RBACPermissions.CommandReloadAllLocales, true)]
			private static bool HandleReloadAllLocalesCommand(CommandHandler handler)
			{
				HandleReloadAchievementRewardLocaleCommand(handler);
				HandleReloadCreatureTemplateLocaleCommand(handler);
				HandleReloadCreatureTextLocaleCommand(handler);
				HandleReloadGameobjectTemplateLocaleCommand(handler);
				HandleReloadGossipMenuOptionLocaleCommand(handler);
				HandleReloadPageTextLocaleCommand(handler);
				HandleReloadPointsOfInterestCommand(handler);
				HandleReloadQuestTemplateLocaleCommand(handler);

				return true;
			}

			[Command("loot", RBACPermissions.CommandReloadAllLoot, true)]
			private static bool HandleReloadAllLootCommand(CommandHandler handler)
			{
				Log.outInfo(LogFilter.Server, "Re-Loading Loot Tables...");
				LootManager.LoadLootTables();
				handler.SendGlobalGMSysMessage("DB tables `*_loot_template` reloaded.");
				Global.ConditionMgr.LoadConditions(true);

				return true;
			}

			[Command("npc", RBACPermissions.CommandReloadAllNpc, true)]
			private static bool HandleReloadAllNpcCommand(CommandHandler handler)
			{
				HandleReloadTrainerCommand(handler);
				HandleReloadNpcVendorCommand(handler);
				HandleReloadPointsOfInterestCommand(handler);
				HandleReloadSpellClickSpellsCommand(handler);

				return true;
			}

			[Command("quest", RBACPermissions.CommandReloadAllQuest, true)]
			private static bool HandleReloadAllQuestCommand(CommandHandler handler)
			{
				HandleReloadQuestAreaTriggersCommand(handler);
				HandleReloadQuestGreetingCommand(handler);
				HandleReloadQuestPOICommand(handler);
				HandleReloadQuestTemplateCommand(handler);

				Log.outInfo(LogFilter.Server, "Re-Loading Quests Relations...");
				Global.ObjectMgr.LoadQuestStartersAndEnders();
				handler.SendGlobalGMSysMessage("DB tables `*_queststarter` and `*_questender` reloaded.");

				return true;
			}

			[Command("scripts", RBACPermissions.CommandReloadAllScripts, true)]
			private static bool HandleReloadAllScriptsCommand(CommandHandler handler)
			{
				if (Global.MapMgr.IsScriptScheduled())
				{
					handler.SendSysMessage("DB scripts used currently, please attempt reload later.");

					return false;
				}

				Log.outInfo(LogFilter.Server, "Re-Loading Scripts...");
				HandleReloadEventScriptsCommand(handler, null);
				HandleReloadSpellScriptsCommand(handler, null);
				handler.SendGlobalGMSysMessage("DB tables `*_scripts` reloaded.");
				HandleReloadWpScriptsCommand(handler, null);
				HandleReloadWpCommand(handler, null);

				return true;
			}

			[Command("spell", RBACPermissions.CommandReloadAllSpell, true)]
			private static bool HandleReloadAllSpellCommand(CommandHandler handler)
			{
				HandleReloadSkillDiscoveryTemplateCommand(handler);
				HandleReloadSkillExtraItemTemplateCommand(handler);
				HandleReloadSpellRequiredCommand(handler);
				HandleReloadSpellAreaCommand(handler);
				HandleReloadSpellGroupsCommand(handler);
				HandleReloadSpellLearnSpellCommand(handler);
				HandleReloadSpellLinkedSpellCommand(handler);
				HandleReloadSpellProcsCommand(handler);
				HandleReloadSpellTargetPositionCommand(handler);
				HandleReloadSpellThreatsCommand(handler);
				HandleReloadSpellGroupStackRulesCommand(handler);
				HandleReloadSpellPetAurasCommand(handler);

				return true;
			}
		}
	}
}