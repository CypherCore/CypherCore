// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Configuration;
using Framework.Constants;
using Game.DataStorage;
using System;
using System.Collections.Generic;

namespace Game
{
    public class WorldConfig : ConfigMgr
    {
        public static void Load(bool reload = false)
        {
            if (reload)
                Load("WorldServer.conf");

            uint databaseCacheVersion = (uint)Values[WorldCfg.ClientCacheVersion];

            ConfigOptionLoadDefinition<bool>[] bools =
            [
                new() { Name = "Support.Enabled", DefaultValue = true, Index = WorldCfg.SupportEnabled },
                new() { Name = "Support.TicketsEnabled", DefaultValue = false, Index = WorldCfg.SupportTicketsEnabled },
                new() { Name = "Support.BugsEnabled", DefaultValue = false, Index = WorldCfg.SupportBugsEnabled },
                new() { Name = "Support.ComplaintsEnabled", DefaultValue = false, Index = WorldCfg.SupportComplaintsEnabled },
                new() { Name = "Support.SuggestionsEnabled", DefaultValue = false, Index = WorldCfg.SupportSuggestionsEnabled },
                new() { Name = "DurabilityLoss.InPvP", DefaultValue = false, Index = WorldCfg.DurabilityLossInPvp },
                new() { Name = "AddonChannel", DefaultValue = true, Index = WorldCfg.AddonChannel },
                new() { Name = "CleanCharacterDB", DefaultValue = false, Index = WorldCfg.CleanCharacterDb },
                new() { Name = "PreserveCustomChannels", DefaultValue = false, Index = WorldCfg.PreserveCustomChannels },
                new() { Name = "GridUnload", DefaultValue = true, Index = WorldCfg.GridUnload },
                new() { Name = "BaseMapLoadAllGrids", DefaultValue = false, Index = WorldCfg.BasemapLoadGrids },
                new() { Name = "InstanceMapLoadAllGrids", DefaultValue = false, Index = WorldCfg.InstancemapLoadGrids },
                new() { Name = "BattlegroundMapLoadAllGrids", DefaultValue = true, Index = WorldCfg.BattlegroundMapLoadGrids },
                new() { Name = "PlayerSave.Stats.SaveOnlyOnLogout", DefaultValue = true, Index = WorldCfg.StatsSaveOnlyOnLogout },
                new() { Name = "Creature.RegenHPCannotReachTargetInRaid", DefaultValue = true, Index = WorldCfg.RegenHpCannotReachTargetInRaid },
                new() { Name = "AllowTwoSide.Interaction.Calendar", DefaultValue = false, Index = WorldCfg.AllowTwoSideInteractionCalendar },
                new() { Name = "AllowTwoSide.Interaction.Channel", DefaultValue = false, Index = WorldCfg.AllowTwoSideInteractionChannel },
                new() { Name = "AllowTwoSide.Interaction.Group", DefaultValue = false, Index = WorldCfg.AllowTwoSideInteractionGroup },
                new() { Name = "AllowTwoSide.Interaction.Guild", DefaultValue = false, Index = WorldCfg.AllowTwoSideInteractionGuild },
                new() { Name = "AllowTwoSide.Interaction.Auction", DefaultValue = true, Index = WorldCfg.AllowTwoSideInteractionAuction },
                new() { Name = "AllowTwoSide.Trade", DefaultValue = false, Index = WorldCfg.AllowTwoSideTrade },
                new() { Name = "CharacterCreating.DisableAlliedRaceAchievementRequirement", DefaultValue = false, Index = WorldCfg.CharacterCreatingDisableAlliedRaceAchievementRequirement },
                new() { Name = "AllFlightPaths", DefaultValue = false, Index = WorldCfg.AllTaxiPaths },
                new() { Name = "InstantFlightPaths", DefaultValue = false, Index = WorldCfg.InstantTaxi },
                new() { Name = "Instance.IgnoreLevel", DefaultValue = false, Index = WorldCfg.InstanceIgnoreLevel },
                new() { Name = "Instance.IgnoreRaid", DefaultValue = false, Index = WorldCfg.InstanceIgnoreRaid },
                new() { Name = "CastUnstuck", DefaultValue = true, Index = WorldCfg.CastUnstuck },
                new() { Name = "GM.AllowInvite", DefaultValue = false, Index = WorldCfg.AllowGmGroup },
                new() { Name = "GM.LowerSecurity", DefaultValue = false, Index = WorldCfg.GmLowerSecurity },
                new() { Name = "SkillChance.Prospecting", DefaultValue = false, Index = WorldCfg.SkillProspecting },
                new() { Name = "SkillChance.Milling", DefaultValue = false, Index = WorldCfg.SkillMilling },
                new() { Name = "ActivateWeather", DefaultValue = true, Index = WorldCfg.Weather },
                new() { Name = "Event.Announce", DefaultValue = false, Index = WorldCfg.EventAnnounce },
                new() { Name = "Quests.EnableQuestTracker", DefaultValue = false, Index = WorldCfg.QuestEnableQuestTracker },
                new() { Name = "Quests.IgnoreRaid", DefaultValue = false, Index = WorldCfg.QuestIgnoreRaid },
                new() { Name = "Quests.IgnoreAutoAccept", DefaultValue = false, Index = WorldCfg.QuestIgnoreAutoAccept },
                new() { Name = "Quests.IgnoreAutoComplete", DefaultValue = false, Index = WorldCfg.QuestIgnoreAutoComplete },
                new() { Name = "DetectPosCollision", DefaultValue = true, Index = WorldCfg.DetectPosCollision },
                new() { Name = "Channel.RestrictedLfg", DefaultValue = true, Index = WorldCfg.RestrictedLfgChannel },
                new() { Name = "ChatFakeMessagePreventing", DefaultValue = false, Index = WorldCfg.ChatFakeMessagePreventing },
                new() { Name = "Death.CorpseReclaimDelay.PvP", DefaultValue = true, Index = WorldCfg.DeathCorpseReclaimDelayPvp },
                new() { Name = "Death.CorpseReclaimDelay.PvE", DefaultValue = true, Index = WorldCfg.DeathCorpseReclaimDelayPve },
                new() { Name = "Death.Bones.World", DefaultValue = true, Index = WorldCfg.DeathBonesWorld },
                new() { Name = "Death.Bones.BattlegroundOrArena", DefaultValue = true, Index = WorldCfg.DeathBonesBgOrArena },
                new() { Name = "Die.Command.Mode", DefaultValue = true, Index = WorldCfg.DieCommandMode },
                new() { Name = "DeclinedNames", DefaultValue = false, Index = WorldCfg.DeclinedNamesUsed },
                new() { Name = "Battleground.CastDeserter", DefaultValue = true, Index = WorldCfg.BattlegroundCastDeserter },
                new() { Name = "Battleground.QueueAnnouncer.Enable", DefaultValue = false, Index = WorldCfg.BattlegroundQueueAnnouncerEnable },
                new() { Name = "Battleground.QueueAnnouncer.PlayerOnly", DefaultValue = false, Index = WorldCfg.BattlegroundQueueAnnouncerPlayeronly },
                new() { Name = "Battleground.StoreStatistics.Enable", DefaultValue = false, Index = WorldCfg.BattlegroundStoreStatisticsEnable },
                new() { Name = "Battleground.GiveXPForKills", DefaultValue = false, Index = WorldCfg.BgXpForKill },
                new() { Name = "Arena.QueueAnnouncer.Enable", DefaultValue = false, Index = WorldCfg.ArenaQueueAnnouncerEnable },
                new() { Name = "Arena.ArenaSeason.InProgress", DefaultValue = false, Index = WorldCfg.ArenaSeasonInProgress },
                new() { Name = "ArenaLog.ExtendedInfo", DefaultValue = false, Index = WorldCfg.ArenaLogExtendedInfo },
                new() { Name = "OffhandCheckAtSpellUnlearn", DefaultValue = true, Index = WorldCfg.OffhandCheckAtSpellUnlearn },
                new() { Name = "Respawn.DynamicEscortNPC", DefaultValue = false, Index = WorldCfg.RespawnDynamicEscortNpc },
                new() { Name = "mmap.enablePathFinding", DefaultValue = true, Index = WorldCfg.EnableMmaps },
                new() { Name = "vmap.enableIndoorCheck", DefaultValue = true, Index = WorldCfg.VmapIndoorCheck },
                new() { Name = "PlayerStart.AllSpells", DefaultValue = false, Index = WorldCfg.StartAllSpells },
                new() { Name = "ResetDuelCooldowns", DefaultValue = false, Index = WorldCfg.ResetDuelCooldowns },
                new() { Name = "ResetDuelHealthMana", DefaultValue = false, Index = WorldCfg.ResetDuelHealthMana },
                new() { Name = "PlayerStart.MapsExplored", DefaultValue = false, Index = WorldCfg.StartAllExplored },
                new() { Name = "PlayerStart.AllReputation", DefaultValue = false, Index = WorldCfg.StartAllRep },
                new() { Name = "PvPToken.Enable", DefaultValue = false, Index = WorldCfg.PvpTokenEnable },
                new() { Name = "NoResetTalentsCost", DefaultValue = false, Index = WorldCfg.NoResetTalentCost },
                new() { Name = "ShowKickInWorld", DefaultValue = false, Index = WorldCfg.ShowKickInWorld },
                new() { Name = "ShowMuteInWorld", DefaultValue = false, Index = WorldCfg.ShowMuteInWorld },
                new() { Name = "ShowBanInWorld", DefaultValue = false, Index = WorldCfg.ShowBanInWorld },
                new() { Name = "Warden.Enabled", DefaultValue = false, Index = WorldCfg.WardenEnabled },
                new() { Name = "FeatureSystem.CharacterUndelete.Enabled", DefaultValue = false, Index = WorldCfg.FeatureSystemCharacterUndeleteEnabled },
                new() { Name = "DBC.EnforceItemAttributes", DefaultValue = true, Index = WorldCfg.DbcEnforceItemAttributes },
                new() { Name = "InstancesResetAnnounce", DefaultValue = false, Index = WorldCfg.InstancesResetAnnounce },
                new() { Name = "AutoBroadcast.On", DefaultValue = false, Index = WorldCfg.AutoBroadcast },
                new() { Name = "PlayerDump.DisallowPaths", DefaultValue = true, Index = WorldCfg.PdumpNoPaths },
                new() { Name = "PlayerDump.DisallowOverwrite", DefaultValue = true, Index = WorldCfg.PdumpNoOverwrite },
                new() { Name = "Wintergrasp.Enable", DefaultValue = false, Index = WorldCfg.WintergraspEnable },
                new() { Name = "TolBarad.Enable", DefaultValue = true, Index = WorldCfg.TolbaradEnable },
                new() { Name = "Stats.Limits.Enable", DefaultValue = false, Index = WorldCfg.StatsLimitsEnable },
                new() { Name = "Allow.IP.Based.Action.Logging", DefaultValue = false, Index = WorldCfg.IpBasedActionLogging },
                new() { Name = "Calculate.Creature.Zone.Area.Data", DefaultValue = false, Index = WorldCfg.CalculateCreatureZoneAreaData },
                new() { Name = "Calculate.Gameoject.Zone.Area.Data", DefaultValue = false, Index = WorldCfg.CalculateGameobjectZoneAreaData },
                new() { Name = "BlackMarket.Enabled", DefaultValue = true, Index = WorldCfg.BlackmarketEnabled },
                new() { Name = "PreventRenameCharacterOnCustomization", DefaultValue = false, Index = WorldCfg.PreventRenameCustomization },
                new() { Name = "PartyRaidWarnings", DefaultValue = false, Index = WorldCfg.ChatPartyRaidWarnings },
                new() { Name = "CacheDataQueries", DefaultValue = true, Index = WorldCfg.CacheDataQueries },
                new() { Name = "Creature.CheckInvalidPosition", DefaultValue = false, Index = WorldCfg.CreatureCheckInvalidPostion },
                new() { Name = "GameObject.CheckInvalidPosition", DefaultValue = false, Index = WorldCfg.GameobjectCheckInvalidPostion },
                new() { Name = "CheckGameObjectLoS", DefaultValue = true, Index = WorldCfg.CheckGobjectLos },
                new() { Name = "AllowLoggingIPAddressesInDatabase", DefaultValue = true, Index = WorldCfg.AllowLogginIpAddressesInDatabase },
                new() { Name = "Loot.EnableAELoot", DefaultValue = true, Index = WorldCfg.EnableAeLoot },
                new() { Name = "Load.Locales", DefaultValue = true, Index = WorldCfg.LoadLocales },
            ];

            ConfigOptionLoadDefinition<uint>[] ints =
            [
                new() { Name = "Server.LoginInfo", DefaultValue = 0, Index = WorldCfg.EnableSinfoLogin },
                new() { Name = "XP.Boost.Daymask", DefaultValue = 0, Index = WorldCfg.XpBoostDaymask },
                new() { Name = "Compression", DefaultValue = 1, Index = WorldCfg.Compression, Min = 1, Max = 9 },
                new() { Name = "PersistentCharacterCleanFlags", DefaultValue = 0, Index = WorldCfg.PersistentCharacterCleanFlags },
                new() { Name = "Auction.ReplicateItemsCooldown", DefaultValue = 900, Index = WorldCfg.AuctionReplicateDelay },
                new() { Name = "Auction.SearchDelay", DefaultValue = 300, Index = WorldCfg.AuctionSearchDelay, Min = 100, Max = 10000 },
                new() { Name = "Auction.TaintedSearchDelay", DefaultValue = 3000, Index = WorldCfg.AuctionTaintedSearchDelay, Min = 100, Max = 10000 },
                new() { Name = "ChatLevelReq.Channel", DefaultValue = 1, Index = WorldCfg.ChatChannelLevelReq },
                new() { Name = "ChatLevelReq.Whisper", DefaultValue = 1, Index = WorldCfg.ChatWhisperLevelReq },
                new() { Name = "ChatLevelReq.Emote", DefaultValue = 1, Index = WorldCfg.ChatEmoteLevelReq },
                new() { Name = "ChatLevelReq.Say", DefaultValue = 1, Index = WorldCfg.ChatSayLevelReq },
                new() { Name = "ChatLevelReq.Yell", DefaultValue = 1, Index = WorldCfg.ChatYellLevelReq },
                new() { Name = "PartyLevelReq", DefaultValue = 1, Index = WorldCfg.PartyLevelReq },
                new() { Name = "LevelReq.Trade", DefaultValue = 1, Index = WorldCfg.TradeLevelReq },
                new() { Name = "LevelReq.Auction", DefaultValue = 1, Index = WorldCfg.AuctionLevelReq },
                new() { Name = "LevelReq.Mail", DefaultValue = 1, Index = WorldCfg.MailLevelReq },
                new() { Name = "PreserveCustomChannelDuration", DefaultValue = 14, Index = WorldCfg.PreserveCustomChannelDuration },
                new() { Name = "PreserveCustomChannelInterval", DefaultValue = 5, Index = WorldCfg.PreserveCustomChannelInterval },
                new() { Name = "PlayerSaveInterval", DefaultValue = 15 * Time.Minute * Time.InMilliseconds, Index = WorldCfg.IntervalSave },
                new() { Name = "DisconnectToleranceInterval", DefaultValue = 0, Index = WorldCfg.IntervalDisconnectTolerance },
                new() { Name = "PlayerSave.StatsMinLevel", DefaultValue = 0, Index = WorldCfg.MinLevelStatSave, Max = SharedConst.StrongMaxLevel },
                new() { Name = "GridCleanUpDelay", DefaultValue = 5 * Time.Minute * Time.InMilliseconds, Index = WorldCfg.IntervalGridclean, Min = MapConst.MinGridDelay },
                new() { Name = "MapUpdateInterval", DefaultValue = 10, Index = WorldCfg.IntervalMapupdate, Min = MapConst.MinMapUpdateDelay },
                new() { Name = "ChangeWeatherInterval", DefaultValue = 10 * Time.Minute * Time.InMilliseconds, Index = WorldCfg.IntervalChangeweather },
                new() { Name = "WorldServerPort", DefaultValue = 8085, Index = WorldCfg.PortWorld, Min = 1, Max = ushort.MaxValue, Reloadable = false },
                new() { Name = "SocketTimeOutTime", DefaultValue = 900000, Index = WorldCfg.SocketTimeoutTime },
                new() { Name = "SocketTimeOutTimeActive", DefaultValue = 60000, Index = WorldCfg.SocketTimeoutTimeActive },
                new() { Name = "SessionAddDelay", DefaultValue = 10000, Index = WorldCfg.SessionAddDelay },
                new() { Name = "MinQuestScaledXPRatio", DefaultValue = 0, Index = WorldCfg.MinQuestScaledXpRatio, Max = 100 },
                new() { Name = "MinCreatureScaledXPRatio", DefaultValue = 0, Index = WorldCfg.MinCreatureScaledXpRatio, Max = 100 },
                new() { Name = "MinDiscoveredScaledXPRatio", DefaultValue = 0, Index = WorldCfg.MinDiscoveredScaledXpRatio, Max = 100 },
                new() { Name = "GameType", DefaultValue = 0, Index = WorldCfg.GameType, Reloadable = false },
                new() { Name = "RealmZone", DefaultValue = RealmManager.HardcodedDevelopmentRealmCategoryId, Index = WorldCfg.RealmZone, Reloadable = false },
                new() { Name = "StrictPlayerNames", DefaultValue = 0, Index = WorldCfg.StrictPlayerNames },
                new() { Name = "StrictCharterNames", DefaultValue = 0, Index = WorldCfg.StrictCharterNames },
                new() { Name = "StrictPetNames", DefaultValue = 0, Index = WorldCfg.StrictPetNames },
                new() { Name = "MinPlayerName", DefaultValue = 2, Index = WorldCfg.MinPlayerName, Min = 1, Max =  12 },
                new() { Name = "MinCharterName", DefaultValue = 2, Index = WorldCfg.MinCharterName, Min = 1, Max = 24 },
                new() { Name = "MinPetName", DefaultValue = 2, Index = WorldCfg.MinPetName, Min = 1, Max = 12 },
                new() { Name = "Guild.CharterCost", DefaultValue = 1000, Index = WorldCfg.CharterCostGuild },
                new() { Name = "ArenaTeam.CharterCost.2v2", DefaultValue = 800000, Index = WorldCfg.CharterCostArena2v2 },
                new() { Name = "ArenaTeam.CharterCost.3v3", DefaultValue = 1200000, Index = WorldCfg.CharterCostArena3v3 },
                new() { Name = "ArenaTeam.CharterCost.5v5", DefaultValue = 2000000, Index = WorldCfg.CharterCostArena5v5 },
                new() { Name = "CharacterCreating.Disabled", DefaultValue = 0, Index = WorldCfg.CharacterCreatingDisabled },
                new() { Name = "CharacterCreating.Disabled.ClassMask", DefaultValue = 0, Index = WorldCfg.CharacterCreatingDisabledClassmask },
                new() { Name = "CharactersPerRealm", DefaultValue = 60, Index = WorldCfg.CharactersPerRealm, Min = 1, Max = SharedConst.MaxCharactersPerRealm },
                new() { Name = "CharactersPerAccount", DefaultValue = 60, Index = WorldCfg.CharactersPerAccount, Min = 1, Max = SharedConst.MaxCharactersPerRealm },
                new() { Name = "CharacterCreating.EvokersPerRealm", DefaultValue = 1, Index = WorldCfg.CharacterCreatingEvokersPerRealm, Min = 1, Max = SharedConst.MaxCharactersPerRealm },
                new() { Name = "CharacterCreatingMinLevelForDemonHunter", DefaultValue = 0, Index = WorldCfg.CharacterCreatingMinLevelForDemonHunter },
                new() { Name = "CharacterCreatingMinLevelForEvoker", DefaultValue = 50, Index = WorldCfg.CharacterCreatingMinLevelForEvoker },
                new() { Name = "SkipCinematics", DefaultValue = 0, Index = WorldCfg.SkipCinematics, Min = 0, Max = 2 },
                new() { Name = "MaxPlayerLevel", DefaultValue = SharedConst.DefaultMaxLevel, Index = WorldCfg.MaxPlayerLevel, Min = 1, Max = SharedConst.MaxLevel, Reloadable = false },
                new() { Name = "MinDualSpecLevel", DefaultValue = 40, Index = WorldCfg.MinDualspecLevel },
                new() { Name = "StartPlayerLevel", DefaultValue = 1, Index = WorldCfg.StartPlayerLevel, Min = 1 },
                new() { Name = "StartDeathKnightPlayerLevel", DefaultValue = 8, Index = WorldCfg.StartDeathKnightPlayerLevel, Min = 1 },
                new() { Name = "StartDemonHunterPlayerLevel", DefaultValue = 8, Index = WorldCfg.StartDemonHunterPlayerLevel, Min = 1 },
                new() { Name = "StartEvokerPlayerLevel", DefaultValue = 10, Index = WorldCfg.StartEvokerPlayerLevel, Min = 1 },
                new() { Name = "StartAlliedRacePlayerLevel", DefaultValue = 10, Index = WorldCfg.StartAlliedRaceLevel, Min = 1 },
                new() { Name = "Currency.ResetHour", DefaultValue = 3, Index = WorldCfg.CurrencyResetHour, Min = 0, Max = 23 },
                new() { Name = "Currency.ResetDay", DefaultValue = 3, Index = WorldCfg.CurrencyResetDay, Min = 0, Max = 6 },
                new() { Name = "Currency.ResetInterval", DefaultValue = 7, Index = WorldCfg.CurrencyResetInterval, Min = 1 },
                new() { Name = "RecruitAFriendMaxLevel", DefaultValue = SharedConst.DefaultMaxLevel, Index = WorldCfg.MaxRecruitAFriendBonusPlayerLevel, Min = 1 },
                new() { Name = "RecruitAFriendMaxDifference", DefaultValue = 4, Index = WorldCfg.MaxRecruitAFriendBonusPlayerLevelDifference },
                new() { Name = "ResetSchedule.WeekDay", DefaultValue = 2, Index = WorldCfg.ResetScheduleWeekDay, Min = 0, Max = 6 },
                new() { Name = "ResetSchedule.Hour", DefaultValue = 8, Index = WorldCfg.ResetScheduleHour, Min = 0, Max = 23 },
                new() { Name = "Instance.UnloadDelay", DefaultValue = 30 * Time.Minute * Time.InMilliseconds, Index = WorldCfg.InstanceUnloadDelay },
                new() { Name = "Quests.DailyResetTime", DefaultValue = 3, Index = WorldCfg.DailyQuestResetTimeHour, Min = 0, Max = 23 },
                new() { Name = "Quests.WeeklyResetWDay", DefaultValue = 3, Index = WorldCfg.WeeklyQuestResetTimeWDay, Min = 0, Max = 6 },
                new() { Name = "MaxPrimaryTradeSkill", DefaultValue = 2, Index = WorldCfg.MaxPrimaryTradeSkill },
                new() { Name = "MinPetitionSigns", DefaultValue = 4, Index = WorldCfg.MinPetitionSigns, Max = 4 },
                new() { Name = "GM.LoginState", DefaultValue = 2, Index = WorldCfg.GmLoginState },
                new() { Name = "GM.Visible", DefaultValue = 2, Index = WorldCfg.GmVisibleState },
                new() { Name = "GM.Chat", DefaultValue = 2, Index = WorldCfg.GmChat },
                new() { Name = "GM.WhisperingTo", DefaultValue = 2, Index = WorldCfg.GmWhisperingTo },
                new() { Name = "GM.FreezeAuraDuration", DefaultValue = 0, Index = WorldCfg.GmFreezeDuration },
                new() { Name = "GM.InGMList.Level", DefaultValue = (uint)AccountTypes.Administrator, Index = WorldCfg.GmLevelInGmList },
                new() { Name = "GM.InWhoList.Level", DefaultValue = (uint)AccountTypes.Administrator, Index = WorldCfg.GmLevelInWhoList },
                new() { Name = "GM.StartLevel", DefaultValue = 1, Index = WorldCfg.StartGmLevel, Min = 1, Max = SharedConst.MaxLevel },
                new() { Name = "GM.ForceShutdownThreshold", DefaultValue = 30, Index = WorldCfg.ForceShutdownThreshold },
                new() { Name = "Visibility.GroupMode", DefaultValue = 1, Index = WorldCfg.GroupVisibility, Min = 0, Max = 3 },
                new() { Name = "MailDeliveryDelay", DefaultValue = Time.Hour, Index = WorldCfg.MailDeliveryDelay },
                new() { Name = "CleanOldMailTime", DefaultValue = 4, Index = WorldCfg.CleanOldMailTime, Min = 0, Max = 23 },
                new() { Name = "UpdateUptimeInterval", DefaultValue = 10, Index = WorldCfg.UptimeUpdate, Min = 1 },
                new() { Name = "LogDB.Opt.ClearInterval", DefaultValue = 10, Index = WorldCfg.LogdbClearinterval, Min = 1 },
                new() { Name = "LogDB.Opt.ClearTime", DefaultValue = 1209600, Index = WorldCfg.LogdbCleartime, Min = 1 },
                new() { Name = "SkillChance.Orange", DefaultValue = 100, Index = WorldCfg.SkillChanceOrange },
                new() { Name = "SkillChance.Yellow", DefaultValue = 75, Index = WorldCfg.SkillChanceYellow },
                new() { Name = "SkillChance.Green", DefaultValue = 25, Index = WorldCfg.SkillChanceGreen },
                new() { Name = "SkillChance.Grey", DefaultValue = 0, Index = WorldCfg.SkillChanceGrey },
                new() { Name = "SkillChanceMiningSteps", DefaultValue = 75, Index = WorldCfg.SkillChanceMiningSteps },
                new() { Name = "SkillChance.SkinningSteps", DefaultValue = 75, Index = WorldCfg.SkillChanceSkinningSteps },
                new() { Name = "SkillGain.Crafting", DefaultValue = 1, Index = WorldCfg.SkillGainCrafting },
                new() { Name = "SkillGain.Gathering", DefaultValue = 1, Index = WorldCfg.SkillGainGathering },
                new() { Name = "MaxOverspeedPings", DefaultValue = 2, Index = WorldCfg.MaxOverspeedPings },
                new() { Name = "ClientCacheVersion", DefaultValue = 0, Index = WorldCfg.ClientCacheVersion },
                new() { Name = "DisableWaterBreath", DefaultValue = (uint)AccountTypes.Console, Index = WorldCfg.DisableBreathing },
                new() { Name = "Expansion", DefaultValue = (uint)PlayerConst.CurrentExpansion, Index = WorldCfg.Expansion, Min = 0, Max = (uint)Expansion.Max, Reloadable = false },
                new() { Name = "ChatFlood.MessageCount", DefaultValue = 10, Index = WorldCfg.ChatFloodMessageCount },
                new() { Name = "ChatFlood.MessageDelay", DefaultValue = 1, Index = WorldCfg.ChatFloodMessageDelay },
                new() { Name = "ChatFlood.AddonMessageCount", DefaultValue = 100, Index = WorldCfg.ChatFloodAddonMessageCount },
                new() { Name = "ChatFlood.AddonMessageDelay", DefaultValue = 1, Index = WorldCfg.ChatFloodAddonMessageDelay },
                new() { Name = "ChatFlood.MuteTime", DefaultValue = 10, Index = WorldCfg.ChatFloodMuteTime },
                new() { Name = "CreatureFamilyAssistanceDelay", DefaultValue = 1500, Index = WorldCfg.CreatureFamilyAssistanceDelay },
                new() { Name = "CreatureFamilyFleeDelay", DefaultValue = 7000, Index = WorldCfg.CreatureFamilyFleeDelay },
                new() { Name = "WorldBossLevelDiff", DefaultValue = 3, Index = WorldCfg.WorldBossLevelDiff },
                new() { Name = "Quests.LowLevelHideDiff", DefaultValue = 4, Index = WorldCfg.QuestLowLevelHideDiff, Max = SharedConst.MaxLevel },
                new() { Name = "Quests.HighLevelHideDiff", DefaultValue = 7, Index = WorldCfg.QuestHighLevelHideDiff, Max = SharedConst.MaxLevel },
                new() { Name = "Battleground.Random.ResetHour", DefaultValue = 6, Index = WorldCfg.RandomBgResetHour, Min = 0, Max = 23 },
                new() { Name = "Calendar.DeleteOldEventsHour", DefaultValue = 6, Index = WorldCfg.CalendarDeleteOldEventsHour, Min = 0, Max = 23 },
                new() { Name = "Guild.ResetHour", DefaultValue = 6, Index = WorldCfg.GuildResetHour, Min = 0, Max = 23 },
                new() { Name = "TalentsInspecting", DefaultValue = 1, Index = WorldCfg.TalentsInspecting },
                new() { Name = "ChatStrictLinkChecking.Severity", DefaultValue = 0, Index = WorldCfg.ChatStrictLinkCheckingSeverity },
                new() { Name = "ChatStrictLinkChecking.Kick", DefaultValue = 0, Index = WorldCfg.ChatStrictLinkCheckingKick },
                new() { Name = "Corpse.Decay.Normal", DefaultValue = 60, Index = WorldCfg.CorpseDecayNormal },
                new() { Name = "Corpse.Decay.Elite", DefaultValue = 300, Index = WorldCfg.CorpseDecayElite },
                new() { Name = "Corpse.Decay.RareElite", DefaultValue = 300, Index = WorldCfg.CorpseDecayRareelite },
                new() { Name = "Corpse.Decay.Obsolete", DefaultValue = 3600, Index = WorldCfg.CorpseDecayObsolete },
                new() { Name = "Corpse.Decay.Rare", DefaultValue = 300, Index = WorldCfg.CorpseDecayRare },
                new() { Name = "Corpse.Decay.Trivial", DefaultValue = 300, Index = WorldCfg.CorpseDecayTrivial },
                new() { Name = "Corpse.DecayMinusMob", DefaultValue = 150, Index = WorldCfg.CorpseDecayMinusMob },
                new() { Name = "Death.SicknessLevel", DefaultValue = 11, Index = WorldCfg.DeathSicknessLevel },
                new() { Name = "Battleground.ReportAFK", DefaultValue = 3, Index = WorldCfg.BattlegroundReportAfk, Min = 1, Max = 9 },
                new() { Name = "Battleground.InvitationType", DefaultValue = 0, Index = WorldCfg.BattlegroundInvitationType },
                new() { Name = "Battleground.PrematureFinishTimer", DefaultValue = 5 * Time.Minute * Time.InMilliseconds, Index = WorldCfg.BattlegroundPrematureFinishTimer },
                new() { Name = "Battleground.PremadeGroupWaitForMatch", DefaultValue = 30 * Time.Minute * Time.InMilliseconds, Index = WorldCfg.BattlegroundPremadeGroupWaitForMatch },
                new() { Name = "ArenaMaxRatingDifference", DefaultValue = 150, Index = WorldCfg.ArenaMaxRatingDifference },
                new() { Name = "Arena.RatingDiscardTimer", DefaultValue = 10 * Time.Minute * Time.InMilliseconds, Index = WorldCfg.ArenaRatingDiscardTimer },
                new() { Name = "Arena.RatedUpdateTimer", DefaultValue = 5 * Time.InMilliseconds, Index = WorldCfg.ArenaRatedUpdateTimer },
                new() { Name = "Arena.ArenaSeason.ID", DefaultValue = 32, Index = WorldCfg.ArenaSeasonId },
                new() { Name = "Arena.ArenaStartRating", DefaultValue = 0, Index = WorldCfg.ArenaStartRating },
                new() { Name = "Arena.ArenaStartPersonalRating", DefaultValue = 1000, Index = WorldCfg.ArenaStartPersonalRating },
                new() { Name = "Arena.ArenaStartMatchmakerRating", DefaultValue = 1500, Index = WorldCfg.ArenaStartMatchmakerRating },
                new() { Name = "Creature.PickPocketRefillDelay", DefaultValue = 10 * Time.Minute, Index = WorldCfg.CreaturePickpocketRefill },
                new() { Name = "Creature.MovingStopTimeForPlayer", DefaultValue = 3 * Time.Minute * Time.InMilliseconds, Index = WorldCfg.CreatureStopForPlayer },
                new() { Name = "Guild.NewsLogRecordsCount", DefaultValue = GuildConst.NewsLogMaxRecords, Index = WorldCfg.GuildNewsLogCount, Max = GuildConst.NewsLogMaxRecords },
                new() { Name = "Guild.EventLogRecordsCount", DefaultValue = GuildConst.EventLogMaxRecords, Index = WorldCfg.GuildEventLogCount, Max = GuildConst.EventLogMaxRecords },
                new() { Name = "Guild.BankEventLogRecordsCount", DefaultValue = GuildConst.BankLogMaxRecords, Index = WorldCfg.GuildBankEventLogCount, Max = GuildConst.BankLogMaxRecords },
                new() { Name = "Visibility.Notify.Period.OnContinents", DefaultValue = SharedConst.DefaultVisibilityNotifyPeriod, Index = WorldCfg.VisibilityNotifyPeriodContinent },
                new() { Name = "Visibility.Notify.Period.InInstances", DefaultValue = SharedConst.DefaultVisibilityNotifyPeriod, Index = WorldCfg.VisibilityNotifyPeriodInstance },
                new() { Name = "Visibility.Notify.Period.InBG", DefaultValue = SharedConst.DefaultVisibilityNotifyPeriod, Index = WorldCfg.VisibilityNotifyPeriodBattleground },
                new() { Name = "Visibility.Notify.Period.InArenas", DefaultValue = SharedConst.DefaultVisibilityNotifyPeriod, Index = WorldCfg.VisibilityNotifyPeriodArena },
                new() { Name = "CharDelete.Method", DefaultValue = 0, Index = WorldCfg.ChardeleteMethod },
                new() { Name = "CharDeleteMinLevel", DefaultValue = 0, Index = WorldCfg.ChardeleteMinLevel },
                new() { Name = "CharDelete.DeathKnightMinLevel", DefaultValue = 0, Index = WorldCfg.ChardeleteDeathKnightMinLevel },
                new() { Name = "CharDelete.DemonHunterMinLevel", DefaultValue = 0, Index = WorldCfg.ChardeleteDemonHunterMinLevel },
                new() { Name = "CharDelete.KeepDays", DefaultValue = 30, Index = WorldCfg.ChardeleteKeepDays },
                new() { Name = "NoGrayAggro.Above", DefaultValue = 0, Index = WorldCfg.NoGrayAggroAbove },
                new() { Name = "NoGrayAggro.Below", DefaultValue = 0, Index = WorldCfg.NoGrayAggroBelow },
                new() { Name = "RespawnMinCheckIntervalMS", DefaultValue = 5000, Index = WorldCfg.RespawnMinCheckIntervalMs },
                new() { Name = "Respawn.DynamicMode", DefaultValue = 0, Index = WorldCfg.RespawnDynamicMode, Min = 0, Max = 1 },
                new() { Name = "Respawn.GuidWarnLevel", DefaultValue = 12000000, Index = WorldCfg.RespawnGuidWarnLevel, Min = 0, Max = 16777215 },
                new() { Name = "Respawn.GuidAlertLevel", DefaultValue = 16000000, Index = WorldCfg.RespawnGuidAlertLevel, Min = 0, Max = 16777215 },
                new() { Name = "Respawn.RestartQuietTime", DefaultValue = 3, Index = WorldCfg.RespawnRestartQuietTime, Min = 0, Max = 23 },
                new() { Name = "Respawn.DynamicMinimumCreature", DefaultValue = 10, Index = WorldCfg.RespawnDynamicMinimumCreature },
                new() { Name = "Respawn.DynamicMinimumGameObject", DefaultValue = 10, Index = WorldCfg.RespawnDynamicMinimumGameObject },
                new() { Name = "Respawn.WarningFrequency", DefaultValue = 1800, Index = WorldCfg.RespawnGuidWarningFrequency },
                new() { Name = "MaxWhoListReturns", DefaultValue = 49, Index = WorldCfg.MaxWho },
                new() { Name = "HonorPointsAfterDuel", DefaultValue = 0, Index = WorldCfg.HonorAfterDuel },
                new() { Name = "PvPToken.MapAllowType", DefaultValue = 4, Index = WorldCfg.PvpTokenMapType, Min = 1, Max = 4 },
                new() { Name = "PvPToken.ItemID", DefaultValue = 29434, Index = WorldCfg.PvpTokenId },
                new() { Name = "PvPToken.ItemCount", DefaultValue = 1, Index = WorldCfg.PvpTokenCount, Min = 1 },
                new() { Name = "MapUpdate.Threads", DefaultValue = 1, Index = WorldCfg.Numthreads, Min = 1 },
                new() { Name = "Command.LookupMaxResults", DefaultValue = 0, Index = WorldCfg.MaxResultsLookupCommands },
                new() { Name = "Warden.NumInjectionChecks", DefaultValue = 9, Index = WorldCfg.WardenNumInjectChecks },
                new() { Name = "Warden.NumLuaSandboxChecks", DefaultValue = 1, Index = WorldCfg.WardenNumLuaChecks },
                new() { Name = "Warden.NumClientModChecks", DefaultValue = 1, Index = WorldCfg.WardenNumClientModChecks },
                new() { Name = "Warden.BanDuration", DefaultValue = 86400, Index = WorldCfg.WardenClientBanDuration },
                new() { Name = "Warden.ClientCheckHoldOff", DefaultValue = 30, Index = WorldCfg.WardenClientCheckHoldoff },
                new() { Name = "Warden.ClientCheckFailAction", DefaultValue = 0, Index = WorldCfg.WardenClientFailAction },
                new() { Name = "Warden.ClientResponseDelay", DefaultValue = 600, Index = WorldCfg.WardenClientResponseDelay },
                new() { Name = "FeatureSystem.CharacterUndelete.Cooldown", DefaultValue = 2592000, Index = WorldCfg.FeatureSystemCharacterUndeleteCooldown },
                new() { Name = "DungeonFinder.OptionsMask", DefaultValue = 1, Index = WorldCfg.LfgOptionsmask },
                new() { Name = "Account.PasswordChangeSecurity", DefaultValue = 0, Index = WorldCfg.AccPasschangesec },
                new() { Name = "Battleground.RewardWinnerHonorFirst", DefaultValue = 27000, Index = WorldCfg.BgRewardWinnerHonorFirst },
                new() { Name = "Battleground.RewardWinnerConquestFirst", DefaultValue = 10000, Index = WorldCfg.BgRewardWinnerConquestFirst },
                new() { Name = "Battleground.RewardWinnerHonorLast", DefaultValue = 13500, Index = WorldCfg.BgRewardWinnerHonorLast },
                new() { Name = "Battleground.RewardWinnerConquestLast", DefaultValue = 5000, Index = WorldCfg.BgRewardWinnerConquestLast },
                new() { Name = "Battleground.RewardLoserHonorFirst", DefaultValue = 4500, Index = WorldCfg.BgRewardLoserHonorFirst },
                new() { Name = "Battleground.RewardLoserHonorLast", DefaultValue = 3500, Index = WorldCfg.BgRewardLoserHonorLast },
                new() { Name = "AccountInstancesPerHour", DefaultValue = 10, Index = WorldCfg.MaxInstancesPerHour },
                new() { Name = "AutoBroadcast.Center", DefaultValue = 0, Index = WorldCfg.AutoBroadcastCenter },
                new() { Name = "AutoBroadcast.Timer", DefaultValue = 60000, Index = WorldCfg.AutoBroadcastInterval },
                new() { Name = "MaxPingTime", DefaultValue = 30, Index = WorldCfg.DbPingInterval },
                new() { Name = "Guild.SaveInterval", DefaultValue = 15, Index = WorldCfg.GuildSaveInterval },
                new() { Name = "Wintergrasp.PlayerMax", DefaultValue = 100, Index = WorldCfg.WintergraspPlrMax },
                new() { Name = "Wintergrasp.PlayerMin", DefaultValue = 0, Index = WorldCfg.WintergraspPlrMin },
                new() { Name = "Wintergrasp.PlayerMinLvl", DefaultValue = 77, Index = WorldCfg.WintergraspPlrMinLvl },
                new() { Name = "Wintergrasp.BattleTimer", DefaultValue = 30, Index = WorldCfg.WintergraspBattletime },
                new() { Name = "Wintergrasp.NoBattleTimer", DefaultValue = 150, Index = WorldCfg.WintergraspNobattletime },
                new() { Name = "Wintergrasp.CrashRestartTimer", DefaultValue = 10, Index = WorldCfg.WintergraspRestartAfterCrash },
                new() { Name = "TolBarad.PlayerMax", DefaultValue = 100, Index = WorldCfg.TolbaradPlrMax },
                new() { Name = "TolBarad.PlayerMin", DefaultValue = 0, Index = WorldCfg.TolbaradPlrMin },
                new() { Name = "TolBarad.PlayerMinLvl", DefaultValue = 85, Index = WorldCfg.TolbaradPlrMinLvl },
                new() { Name = "TolBarad.BattleTimer", DefaultValue = 15, Index = WorldCfg.TolbaradBattleTime },
                new() { Name = "TolBarad.BonusTime", DefaultValue = 5, Index = WorldCfg.TolbaradBonusTime },
                new() { Name = "TolBarad.NoBattleTimer", DefaultValue = 150, Index = WorldCfg.TolbaradNoBattleTime },
                new() { Name = "TolBarad.CrashRestartTimer", DefaultValue = 10, Index = WorldCfg.TolbaradRestartAfterCrash },
                new() { Name = "PacketSpoof.Policy", DefaultValue = (uint)DosProtection.Policy.Kick, Index = WorldCfg.PacketSpoofPolicy },
                new() { Name = "PacketSpoof.BanMode", DefaultValue = (uint)BanMode.Account, Index = WorldCfg.PacketSpoofBanmode, Min = (uint)BanMode.Account, Max = (uint)BanMode.IP },
                new() { Name = "PacketSpoof.BanDuration", DefaultValue = 86400, Index = WorldCfg.PacketSpoofBanduration },
                new() { Name = "AuctionHouseBot.Update.Interval", DefaultValue = 20, Index = WorldCfg.AhbotUpdateInterval },
                new() { Name = "BlackMarketMaxAuctions", DefaultValue = 12, Index = WorldCfg.BlackmarketMaxAuctions },
                new() { Name = "BlackMarket.UpdatePeriod", DefaultValue = 24, Index = WorldCfg.BlackmarketUpdatePeriod },
                new() { Name = "Pvp.FactionBalance.LevelCheckDiff", DefaultValue = 0, Index = WorldCfg.FactionBalanceLevelCheckDiff },
            ];

            ConfigOptionLoadDefinition<ulong>[] int64s =
            [
                new() { Name = "CharacterCreating.Disabled.RaceMask", DefaultValue = 0, Index = WorldCfg.CharacterCreatingDisabledRacemask },
                new() { Name = "StartPlayerMoney", DefaultValue = 0, Index = WorldCfg.StartPlayerMoney, Min = 0, Max = PlayerConst.MaxMoneyAmount },
            ];

            ConfigOptionLoadDefinition<float>[] floats =
            [
                new() { Name = "MaxGroupXPDistance", DefaultValue = 74.0f, Index = WorldCfg.GroupXpDistance },
                new() { Name = "MaxRecruitAFriendBonusDistance", DefaultValue = 100.0f, Index = WorldCfg.MaxRecruitAFriendDistance },
                new() { Name = "MonsterSight", DefaultValue = 50.0f, Index = WorldCfg.SightMonster },
                new() { Name = "CreatureFamilyFleeAssistanceRadius", DefaultValue = 30.0f, Index = WorldCfg.CreatureFamilyFleeAssistanceRadius },
                new() { Name = "CreatureFamilyAssistanceRadius", DefaultValue = 10.0f, Index = WorldCfg.CreatureFamilyAssistanceRadius },
                new() { Name = "ThreatRadius", DefaultValue = 60.0f, Index = WorldCfg.ThreatRadius },
                new() { Name = "ListenRange.Say", DefaultValue = 25.0f, Index = WorldCfg.ListenRangeSay },
                new() { Name = "ListenRange.TextEmote", DefaultValue = 25.0f, Index = WorldCfg.ListenRangeTextemote },
                new() { Name = "ListenRange.Yell", DefaultValue = 300.0f, Index = WorldCfg.ListenRangeYell },
                new() { Name = "Arena.ArenaWinRatingModifier1", DefaultValue = 48.0f, Index = WorldCfg.ArenaWinRatingModifier1 },
                new() { Name = "Arena.ArenaWinRatingModifier2", DefaultValue = 24.0f, Index = WorldCfg.ArenaWinRatingModifier2 },
                new() { Name = "Arena.ArenaLoseRatingModifier", DefaultValue = 24.0f, Index = WorldCfg.ArenaLoseRatingModifier },
                new() { Name = "Arena.ArenaMatchmakerRatingModifier", DefaultValue = 24.0f, Index = WorldCfg.ArenaMatchmakerRatingModifier },
                new() { Name = "Visibility.Distance.Continents", DefaultValue = SharedConst.DefaultVisibilityDistance, Index = WorldCfg.MaxVisibilityDistanceContinent, Min = 0.0f, Max = SharedConst.MaxVisibilityDistance },
                new() { Name = "Visibility.Distance.Instances", DefaultValue = SharedConst.DefaultVisibilityInstance, Index = WorldCfg.MaxVisibilityDistanceInstance, Min = 0.0f, Max = SharedConst.MaxVisibilityDistance },
                new() { Name = "Visibility.Distance.BG", DefaultValue = SharedConst.DefaultVisibilityBGAreans, Index = WorldCfg.MaxVisibilityDistanceBattleground, Min = 0.0f, Max = SharedConst.MaxVisibilityDistance },
                new() { Name = "Visibility.Distance.Arenas", DefaultValue = SharedConst.DefaultVisibilityBGAreans, Index = WorldCfg.MaxVisibilityDistanceArena, Min = 0.0f, Max = SharedConst.MaxVisibilityDistance },
                new() { Name = "Respawn.DynamicRateCreature", DefaultValue = 10.0f, Index = WorldCfg.RespawnDynamicRateCreature, Min = 0.0f },
                new() { Name = "Respawn.DynamicRateGameObject", DefaultValue = 10.0f, Index = WorldCfg.RespawnDynamicRateGameobject, Min = 0.0f },
                new() { Name = "Stats.Limits.Dodge", DefaultValue = 95.0f, Index = WorldCfg.StatsLimitsDodge },
                new() { Name = "Stats.Limits.Parry", DefaultValue = 95.0f, Index = WorldCfg.StatsLimitsParry },
                new() { Name = "Stats.Limits.Block", DefaultValue = 95.0f, Index = WorldCfg.StatsLimitsBlock },
                new() { Name = "Stats.Limits.Crit", DefaultValue = 95.0f, Index = WorldCfg.StatsLimitsCrit },
                new() { Name = "Pvp.FactionBalance.Pct5", DefaultValue = 0.6f, Index = WorldCfg.CallToArms5Pct },
                new() { Name = "Pvp.FactionBalance.Pct10", DefaultValue = 0.7f, Index = WorldCfg.CallToArms10Pct },
                new() { Name = "Pvp.FactionBalance.Pct20", DefaultValue = 0.8f, Index = WorldCfg.CallToArms20Pct },
            ];

            ConfigOptionLoadDefinition<float>[] rates =
            [
                new() { Name = "Rate.Health", DefaultValue = 1.0f, Index = WorldCfg.RateHealth, Min = 0.01f },
                new() { Name = "Rate.Mana", DefaultValue = 1.0f, Index = WorldCfg.RatePowerMana, Min = 0.01f },
                new() { Name = "Rate.Rage.Gain", DefaultValue = 1.0f, Index = WorldCfg.RatePowerRageIncome, Min = 0.01f },
                new() { Name = "Rate.Rage.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerRageLoss, Min = 0.01f },
                new() { Name = "Rate.Focus", DefaultValue = 1.0f, Index = WorldCfg.RatePowerFocus, Min = 0.01f },
                new() { Name = "Rate.Energy", DefaultValue = 1.0f, Index = WorldCfg.RatePowerEnergy, Min = 0.01f },
                new() { Name = "Rate.ComboPoints.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerComboPointsLoss, Min = 0.01f },
                new() { Name = "Rate.RunicPower.Gain", DefaultValue = 1.0f, Index = WorldCfg.RatePowerRunicPowerIncome, Min = 0.01f },
                new() { Name = "Rate.RunicPower.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerRunicPowerLoss, Min = 0.01f },
                new() { Name = "Rate.SoulShards.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerSoulShards, Min = 0.01f },
                new() { Name = "Rate.LunarPower.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerLunarPower, Min = 0.01f },
                new() { Name = "Rate.HolyPower.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerHolyPower, Min = 0.01f },
                new() { Name = "Rate.Maelstrom.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerMaelstrom, Min = 0.01f },
                new() { Name = "Rate.Chi.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerChi, Min = 0.01f },
                new() { Name = "Rate.Insanity.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerInsanity, Min = 0.01f },
                new() { Name = "Rate.ArcaneCharges.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerArcaneCharges, Min = 0.01f },
                new() { Name = "Rate.Fury.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerFury, Min = 0.01f },
                new() { Name = "Rate.Pain.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerPain, Min = 0.01f },
                new() { Name = "Rate.Essence.Loss", DefaultValue = 1.0f, Index = WorldCfg.RatePowerEssence, Min = 0.01f },
                new() { Name = "Rate.Skill.Discovery", DefaultValue = 1.0f, Index = WorldCfg.RateSkillDiscovery },
                new() { Name = "Rate.Drop.Item.Poor", DefaultValue = 1.0f, Index = WorldCfg.RateDropItemPoor },
                new() { Name = "Rate.Drop.Item.Normal", DefaultValue = 1.0f, Index = WorldCfg.RateDropItemNormal },
                new() { Name = "Rate.Drop.Item.Uncommon", DefaultValue = 1.0f, Index = WorldCfg.RateDropItemUncommon },
                new() { Name = "Rate.Drop.Item.Rare", DefaultValue = 1.0f, Index = WorldCfg.RateDropItemRare },
                new() { Name = "Rate.Drop.Item.Epic", DefaultValue = 1.0f, Index = WorldCfg.RateDropItemEpic },
                new() { Name = "Rate.Drop.Item.Legendary", DefaultValue = 1.0f, Index = WorldCfg.RateDropItemLegendary },
                new() { Name = "Rate.Drop.Item.Artifact", DefaultValue = 1.0f, Index = WorldCfg.RateDropItemArtifact },
                new() { Name = "Rate.Drop.Item.Referenced", DefaultValue = 1.0f, Index = WorldCfg.RateDropItemReferenced },
                new() { Name = "Rate.Drop.Item.ReferencedAmount", DefaultValue = 1.0f, Index = WorldCfg.RateDropItemReferencedAmount },
                new() { Name = "Rate.Drop.Money", DefaultValue = 1.0f, Index = WorldCfg.RateDropMoney },
                new() { Name = "Rate.XP.Kill", DefaultValue = 1.0f, Index = WorldCfg.RateXpKill },
                new() { Name = "Rate.XP.BattlegroundKill", DefaultValue = 1.0f, Index = WorldCfg.RateXpBgKill },
                new() { Name = "Rate.XP.Quest", DefaultValue = 1.0f, Index = WorldCfg.RateXpQuest },
                new() { Name = "Rate.XP.Explore", DefaultValue = 1.0f, Index = WorldCfg.RateXpExplore },
                new() { Name = "XP.Boost.Rate", DefaultValue = 2.0f, Index = WorldCfg.RateXpBoost },
                new() { Name = "Rate.RepairCost", DefaultValue = 1.0f, Index = WorldCfg.RateRepaircost, Min = 0.0f },
                new() { Name = "Rate.Reputation.Gain", DefaultValue = 1.0f, Index = WorldCfg.RateReputationGain },
                new() { Name = "Rate.Reputation.LowLevel.Kill", DefaultValue = 1.0f, Index = WorldCfg.RateReputationLowLevelKill },
                new() { Name = "Rate.Reputation.LowLevel.Quest", DefaultValue = 1.0f, Index = WorldCfg.RateReputationLowLevelQuest },
                new() { Name = "Rate.Reputation.RecruitAFriendBonus", DefaultValue = 0.1f, Index = WorldCfg.RateReputationRecruitAFriendBonus},
                new() { Name = "Rate.Creature.HP.Normal", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureHpNormal },
                new() { Name = "Rate.Creature.HP.Elite", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureHpElite },
                new() { Name = "Rate.Creature.HP.RareElite", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureHpRareelite },
                new() { Name = "Rate.Creature.HP.Obsolete", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureHpObsolete },
                new() { Name = "Rate.Creature.HP.Rare", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureHpRare },
                new() { Name = "Rate.Creature.HP.Trivial", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureHpTrivial },
                new() { Name = "Rate.Creature.HPMinusMob", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureHpMinusmob },
                new() { Name = "Rate.Creature.Damage.Normal", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureDamageNormal },
                new() { Name = "Rate.Creature.Damage.Elite", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureDamageElite },
                new() { Name = "Rate.Creature.Damage.RareElite", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureDamageRareelite },
                new() { Name = "Rate.Creature.Damage.Obsolete", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureDamageObsolete },
                new() { Name = "Rate.Creature.Damage.Rare", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureDamageRare },
                new() { Name = "Rate.Creature.Damage.Trivial", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureDamageTrivial },
                new() { Name = "Rate.Creature.DamageMinusMob", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureDamageMinusmob },
                new() { Name = "Rate.Creature.SpellDamage.Normal", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureSpelldamageNormal },
                new() { Name = "Rate.Creature.SpellDamage.Elite", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureSpelldamageElite },
                new() { Name = "Rate.Creature.SpellDamage.RareElite", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureSpelldamageRareelite },
                new() { Name = "Rate.Creature.SpellDamage.Obsolete", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureSpelldamageObsolete },
                new() { Name = "Rate.Creature.SpellDamage.Rare", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureSpelldamageRare },
                new() { Name = "Rate.Creature.SpellDamage.Trivial", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureSpelldamageTrivial },
                new() { Name = "Rate.Creature.SpellDamageMinusMob", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureSpelldamageMinusmob },
                new() { Name = "Rate.Creature.Aggro", DefaultValue = 1.0f, Index = WorldCfg.RateCreatureAggro },
                new() { Name = "Rate.Rest.InGame", DefaultValue = 1.0f, Index = WorldCfg.RateRestIngame },
                new() { Name = "Rate.Rest.Offline.InTavernOrCity", DefaultValue = 1.0f, Index = WorldCfg.RateRestOfflineInTavernOrCity },
                new() { Name = "Rate.Rest.Offline.InWilderness", DefaultValue = 1.0f, Index = WorldCfg.RateRestOfflineInWilderness },
                new() { Name = "Rate.Damage.Fall", DefaultValue = 1.0f, Index = WorldCfg.RateDamageFall },
                new() { Name = "Rate.Auction.Time", DefaultValue = 1.0f, Index = WorldCfg.RateAuctionTime },
                new() { Name = "Rate.Auction.Deposit", DefaultValue = 1.0f, Index = WorldCfg.RateAuctionDeposit },
                new() { Name = "Rate.Auction.Cut", DefaultValue = 1.0f, Index = WorldCfg.RateAuctionCut },
                new() { Name = "Rate.Honor", DefaultValue = 1.0f, Index = WorldCfg.RateHonor },
                new() { Name = "Rate.InstanceResetTime", DefaultValue = 1.0f, Index = WorldCfg.RateInstanceResetTime },
                new() { Name = "Rate.MoveSpeed", DefaultValue = 1.0f, Index = WorldCfg.RateMovespeed, Min = 0.01f },
                new() { Name = "Rate.Corpse.Decay.Looted", DefaultValue = 0.5f, Index = WorldCfg.RateCorpseDecayLooted },
                new() { Name = "DurabilityLoss.OnDeath", DefaultValue = 10.0f, Index = WorldCfg.RateDurabilityLossOnDeath, Min = 0.0f, Max = 100.0f },
                new() { Name = "DurabilityLossChance.Damage", DefaultValue = 0.5f, Index = WorldCfg.RateDurabilityLossDamage, Min = 0.0f },
                new() { Name = "DurabilityLossChance.Absorb", DefaultValue = 0.5f, Index = WorldCfg.RateDurabilityLossAbsorb, Min = 0.0f },
                new() { Name = "DurabilityLossChance.Parry", DefaultValue = 0.05f, Index = WorldCfg.RateDurabilityLossParry, Min = 0.0f },
                new() { Name = "DurabilityLossChance.Block", DefaultValue = 0.05f, Index = WorldCfg.RateDurabilityLossBlock, Min = 0.0f },
                new() { Name = "Rate.Quest.Money.Reward", DefaultValue = 1.0f, Index = WorldCfg.RateMoneyQuest, Min = 0.0f },
                new() { Name = "Rate.Quest.MoneyMax.Level.Reward", DefaultValue = 1.0f, Index = WorldCfg.RateMoneyMaxLevelQuest, Min = 0.0f },
            ];

            foreach (ConfigOptionLoadDefinition<bool> definition in bools)
                StoreConfigValue(GetDefaultValue(definition.Name, definition.DefaultValue), definition, reload);

            foreach (ConfigOptionLoadDefinition<uint> definition in ints)
                StoreConfigValue(GetDefaultValue(definition.Name, definition.DefaultValue), definition, reload);

            foreach (ConfigOptionLoadDefinition<ulong> definition in int64s)
                StoreConfigValue(GetDefaultValue(definition.Name, definition.DefaultValue), definition, reload);

            foreach (ConfigOptionLoadDefinition<float> definition in floats)
                StoreConfigValue(GetDefaultValue(definition.Name, definition.DefaultValue), definition, reload);

            foreach (ConfigOptionLoadDefinition<float> definition in rates)
                StoreConfigValue(GetDefaultValue(definition.Name, definition.DefaultValue), definition, reload);

            Values[WorldCfg.RateDurabilityLossOnDeath] = (float)Values[WorldCfg.RateDurabilityLossOnDeath] / 100.0f;

            if ((bool)Values[WorldCfg.GridUnload])
            {
                if ((bool)Values[WorldCfg.BasemapLoadGrids])
                {
                    Log.outError(LogFilter.ServerLoading, "BaseMapLoadAllGrids enabled, but GridUnload also enabled. GridUnload must be disabled to enable base map pre-loading. Base map pre-loading disabled");
                    Values[WorldCfg.BasemapLoadGrids] = false;
                }
                if ((bool)Values[WorldCfg.InstancemapLoadGrids])
                {
                    Log.outError(LogFilter.ServerLoading, "InstanceMapLoadAllGrids enabled, but GridUnload also enabled. GridUnload must be disabled to enable instance map pre-loading. Instance map pre-loading disabled");
                    Values[WorldCfg.InstancemapLoadGrids] = false;
                }
            }

            // Config values are in "milliseconds" but we handle SocketTimeOut only as "seconds" so divide by 1000
            Values[WorldCfg.SocketTimeoutTime] = (uint)Values[WorldCfg.SocketTimeoutTime] / 1000;
            Values[WorldCfg.SocketTimeoutTimeActive] = (uint)Values[WorldCfg.SocketTimeoutTime] / 1000;

            // must be after CONFIG_CHARACTERS_PER_REALM
            if ((uint)Values[WorldCfg.CharactersPerAccount] < (uint)Values[WorldCfg.CharactersPerRealm])
            {
                Log.outError(LogFilter.ServerLoading, $"CharactersPerAccount ({Values[WorldCfg.CharactersPerAccount]}) can't be less than CharactersPerRealm ({Values[WorldCfg.CharactersPerRealm]}).");
                Values[WorldCfg.CharactersPerAccount] = Values[WorldCfg.CharactersPerRealm];
            }

            void validateStartLevel(WorldCfg config, string name)
            {
                uint maxLevel = (uint)Values[WorldCfg.MaxPlayerLevel];
                if ((uint)Values[config] > maxLevel)
                {
                    Log.outError(LogFilter.ServerLoading, $"{name} ({Values[config]}) must be in range 1..MaxPlayerLevel({maxLevel}). Set to {maxLevel}.");
                    Values[config] = maxLevel;
                }
            }

            validateStartLevel(WorldCfg.StartPlayerLevel, "StartPlayerLevel");
            validateStartLevel(WorldCfg.StartDeathKnightPlayerLevel, "StartDeathKnightPlayerLevel");
            validateStartLevel(WorldCfg.StartDemonHunterPlayerLevel, "StartDemonHunterPlayerLevel");
            validateStartLevel(WorldCfg.StartEvokerPlayerLevel, "StartEvokerPlayerLevel");
            validateStartLevel(WorldCfg.StartAlliedRaceLevel, "StartDemonHunterPlayerLevel");
            validateStartLevel(WorldCfg.MaxRecruitAFriendBonusPlayerLevel, "RecruitAFriend.MaxLevel");

            if ((uint)Values[WorldCfg.StartGmLevel] < (uint)Values[WorldCfg.StartPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, $"GM.StartLevel ({Values[WorldCfg.StartGmLevel]}) must be in range StartPlayerLevel({Values[WorldCfg.StartPlayerLevel]})..{SharedConst.MaxLevel}. Set to {Values[WorldCfg.StartPlayerLevel]}.");
                Values[WorldCfg.StartGmLevel] = Values[WorldCfg.StartPlayerLevel];
            }

            Log.outInfo(LogFilter.ServerLoading, $"Will clear `logs` table of entries older than {Values[WorldCfg.LogdbCleartime]} seconds every {Values[WorldCfg.LogdbClearinterval]} minutes.");

            if ((uint)Values[WorldCfg.MaxOverspeedPings] != 0 && (uint)Values[WorldCfg.MaxOverspeedPings] < 2)
            {
                Log.outError(LogFilter.ServerLoading, $"MaxOverspeedPings ({Values[WorldCfg.MaxOverspeedPings]}) must be in range 2..infinity (or 0 to disable check). Set to 2.");
                Values[WorldCfg.MaxOverspeedPings] = 2;
            }

            if ((uint)Values[WorldCfg.ClientCacheVersion] == 0)
                Values[WorldCfg.ClientCacheVersion] = databaseCacheVersion;

            void validateVisibilityDistance(WorldCfg config, string name)
            {
                float minVisibilityDistance = 45.0f * (float)Values[WorldCfg.RateCreatureAggro];
                if ((float)Values[config] < minVisibilityDistance)
                {
                    Log.outError(LogFilter.ServerLoading, $"{name} can't be less max aggro radius {minVisibilityDistance}");
                    Values[config] = minVisibilityDistance;
                }
            }

            // visibility on continents
            validateVisibilityDistance(WorldCfg.MaxVisibilityDistanceContinent, "Visibility.Distance.Continents");

            // visibility in instances
            validateVisibilityDistance(WorldCfg.MaxVisibilityDistanceInstance, "Visibility.Distance.Instances");

            // visibility in BG
            validateVisibilityDistance(WorldCfg.MaxVisibilityDistanceBattleground, "Visibility.Distance.BG");

            // Visibility in Arenas
            validateVisibilityDistance(WorldCfg.MaxVisibilityDistanceArena, "Visibility.Distance.Arenas");

            // No aggro from gray mobs
            if ((uint)Values[WorldCfg.NoGrayAggroAbove] > (uint)Values[WorldCfg.MaxPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, $"NoGrayAggro.Above ({Values[WorldCfg.NoGrayAggroAbove]}) must be in range 0..{Values[WorldCfg.MaxPlayerLevel]}. Set to {Values[WorldCfg.MaxPlayerLevel]}.");
                Values[WorldCfg.NoGrayAggroAbove] = Values[WorldCfg.MaxPlayerLevel];
            }
            if ((uint)Values[WorldCfg.NoGrayAggroBelow] > (uint)Values[WorldCfg.MaxPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, $"NoGrayAggro.Below ({Values[WorldCfg.NoGrayAggroBelow]}) must be in range 0..{Values[WorldCfg.MaxPlayerLevel]}. Set to {Values[WorldCfg.MaxPlayerLevel]}.");
                Values[WorldCfg.NoGrayAggroBelow] = Values[WorldCfg.MaxPlayerLevel];
            }
            if ((uint)Values[WorldCfg.NoGrayAggroAbove] > 0 && (uint)Values[WorldCfg.NoGrayAggroAbove] < (uint)Values[WorldCfg.NoGrayAggroBelow])
            {
                Log.outError(LogFilter.ServerLoading, $"NoGrayAggro.Below ({Values[WorldCfg.NoGrayAggroBelow]}) cannot be greater than NoGrayAggro.Above ({Values[WorldCfg.NoGrayAggroAbove]}). Set to {Values[WorldCfg.NoGrayAggroAbove]}.");
                Values[WorldCfg.NoGrayAggroBelow] = Values[WorldCfg.NoGrayAggroAbove];
            }

            int clientCacheId = GetDefaultValue("ClientCacheVersion", 0);
            if (clientCacheId != 0)
            {
                // overwrite DB/old value
                if (clientCacheId > 0)
                    Values[WorldCfg.ClientCacheVersion] = clientCacheId;
                else
                    Log.outError(LogFilter.ServerLoading, "ClientCacheVersion can't be negative {0}, ignored.", clientCacheId);
            }
            Log.outInfo(LogFilter.ServerLoading, "Client cache version set to: {0}", clientCacheId);

            // call ScriptMgr if we're reloading the configuration
            if (reload)
                Global.ScriptMgr.OnConfigLoad(reload);
        }

        static void StoreConfigValue<T>(dynamic value, ConfigOptionLoadDefinition<T> definition, bool reload) where T : struct
        {
            if (typeof(T) == typeof(bool))
            {
                if (definition.Min.HasValue && value < definition.Min)
                {
                    Log.outError(LogFilter.Sql, $"{definition.Name} {value} must be >= {definition.Min}. Using {definition.Min} instead.");
                    value = definition.Min;
                }
                if (definition.Max.HasValue && value > definition.Max)
                {
                    Log.outError(LogFilter.ServerLoading, $"{definition.Name} {value} must be <= {definition.Max}. Using {definition.Max} instead.");
                    value = definition.Max;
                }
            }
            if (reload && !definition.Reloadable)
            {
                if (value != Values[definition.Index])
                    Log.outError(LogFilter.ServerLoading, $"{definition.Name} option can't be changed at worldserver.conf reload, using current value ({Values[definition.Index]}).");
                return;
            }
            Values[definition.Index] = value;
        }

        public static uint GetUIntValue(WorldCfg confi)
        {
            return Convert.ToUInt32(Values.LookupByKey(confi));
        }

        public static int GetIntValue(WorldCfg confi)
        {
            return Convert.ToInt32(Values.LookupByKey(confi));
        }

        public static ulong GetUInt64Value(WorldCfg confi)
        {
            return Convert.ToUInt64(Values.LookupByKey(confi));
        }

        public static bool GetBoolValue(WorldCfg confi)
        {
            return Convert.ToBoolean(Values.LookupByKey(confi));
        }

        public static float GetFloatValue(WorldCfg confi)
        {
            return Convert.ToSingle(Values.LookupByKey(confi));
        }

        public static void SetValue(WorldCfg confi, object value)
        {
            Values[confi] = value;
        }

        static Dictionary<WorldCfg, object> Values = new();
    }

    struct ConfigOptionLoadDefinition<T>() where T : struct
    {
        public string Name;
        public T DefaultValue;
        public WorldCfg Index;
        public T? Min;
        public T? Max;
        public bool Reloadable = true;
    }
}
