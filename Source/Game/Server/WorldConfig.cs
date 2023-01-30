// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Configuration;
using Framework.Constants;
using Game.Scripting.Interfaces.IWorld;

namespace Game
{
    public class WorldConfig : ConfigMgr
    {
        private static readonly Dictionary<WorldCfg, object> _values = new();

        public static void Load(bool reload = false)
        {
            if (reload)
                Load("WorldServer.conf");

            // Read support system setting from the config file
            _values[WorldCfg.SupportEnabled] = GetDefaultValue("Support.Enabled", true);
            _values[WorldCfg.SupportTicketsEnabled] = GetDefaultValue("Support.TicketsEnabled", false);
            _values[WorldCfg.SupportBugsEnabled] = GetDefaultValue("Support.BugsEnabled", false);
            _values[WorldCfg.SupportComplaintsEnabled] = GetDefaultValue("Support.ComplaintsEnabled", false);
            _values[WorldCfg.SupportSuggestionsEnabled] = GetDefaultValue("Support.SuggestionsEnabled", false);

            // Send server info on login?
            _values[WorldCfg.EnableSinfoLogin] = GetDefaultValue("Server.LoginInfo", 0);

            // Read all rates from the config file
            static void SetRegenRate(WorldCfg rate, string configKey)
            {
                _values[rate] = GetDefaultValue(configKey, 1.0f);
                if ((float)_values[rate] < 0.0f)
                {
                    Log.outError(LogFilter.ServerLoading, "{0} ({1}) must be > 0. Using 1 instead.", configKey, _values[rate]);
                    _values[rate] = 1;
                }
            }

            SetRegenRate(WorldCfg.RateHealth, "Rate.Health");
            SetRegenRate(WorldCfg.RatePowerMana, "Rate.Mana");
            SetRegenRate(WorldCfg.RatePowerRageIncome, "Rate.Rage.Gain");
            SetRegenRate(WorldCfg.RatePowerRageLoss, "Rate.Rage.Loss");
            SetRegenRate(WorldCfg.RatePowerFocus, "Rate.Focus");
            SetRegenRate(WorldCfg.RatePowerEnergy, "Rate.Energy");
            SetRegenRate(WorldCfg.RatePowerComboPointsLoss, "Rate.ComboPoints.Loss");
            SetRegenRate(WorldCfg.RatePowerRunicPowerIncome, "Rate.RunicPower.Gain");
            SetRegenRate(WorldCfg.RatePowerRunicPowerLoss, "Rate.RunicPower.Loss");
            SetRegenRate(WorldCfg.RatePowerSoulShards, "Rate.SoulShards.Loss");
            SetRegenRate(WorldCfg.RatePowerLunarPower, "Rate.LunarPower.Loss");
            SetRegenRate(WorldCfg.RatePowerHolyPower, "Rate.HolyPower.Loss");
            SetRegenRate(WorldCfg.RatePowerMaelstrom, "Rate.Maelstrom.Loss");
            SetRegenRate(WorldCfg.RatePowerChi, "Rate.Chi.Loss");
            SetRegenRate(WorldCfg.RatePowerInsanity, "Rate.Insanity.Loss");
            SetRegenRate(WorldCfg.RatePowerArcaneCharges, "Rate.ArcaneCharges.Loss");
            SetRegenRate(WorldCfg.RatePowerFury, "Rate.Fury.Loss");
            SetRegenRate(WorldCfg.RatePowerPain, "Rate.Pain.Loss");

            _values[WorldCfg.RateSkillDiscovery] = GetDefaultValue("Rate.Skill.Discovery", 1.0f);
            _values[WorldCfg.RateDropItemPoor] = GetDefaultValue("Rate.Drop.Item.Poor", 1.0f);
            _values[WorldCfg.RateDropItemNormal] = GetDefaultValue("Rate.Drop.Item.Normal", 1.0f);
            _values[WorldCfg.RateDropItemUncommon] = GetDefaultValue("Rate.Drop.Item.Uncommon", 1.0f);
            _values[WorldCfg.RateDropItemRare] = GetDefaultValue("Rate.Drop.Item.Rare", 1.0f);
            _values[WorldCfg.RateDropItemEpic] = GetDefaultValue("Rate.Drop.Item.Epic", 1.0f);
            _values[WorldCfg.RateDropItemLegendary] = GetDefaultValue("Rate.Drop.Item.Legendary", 1.0f);
            _values[WorldCfg.RateDropItemArtifact] = GetDefaultValue("Rate.Drop.Item.Artifact", 1.0f);
            _values[WorldCfg.RateDropItemReferenced] = GetDefaultValue("Rate.Drop.Item.Referenced", 1.0f);
            _values[WorldCfg.RateDropItemReferencedAmount] = GetDefaultValue("Rate.Drop.Item.ReferencedAmount", 1.0f);
            _values[WorldCfg.RateDropMoney] = GetDefaultValue("Rate.Drop.Money", 1.0f);
            _values[WorldCfg.RateXpKill] = GetDefaultValue("Rate.XP.Kill", 1.0f);
            _values[WorldCfg.RateXpBgKill] = GetDefaultValue("Rate.XP.BattlegroundKill", 1.0f);
            _values[WorldCfg.RateXpQuest] = GetDefaultValue("Rate.XP.Quest", 1.0f);
            _values[WorldCfg.RateXpExplore] = GetDefaultValue("Rate.XP.Explore", 1.0f);

            _values[WorldCfg.XpBoostDaymask] = GetDefaultValue("XP.Boost.Daymask", 0);
            _values[WorldCfg.RateXpBoost] = GetDefaultValue("XP.Boost.Rate", 2.0f);

            _values[WorldCfg.RateRepaircost] = GetDefaultValue("Rate.RepairCost", 1.0f);
            if ((float)_values[WorldCfg.RateRepaircost] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, "Rate.RepairCost ({0}) must be >=0. Using 0.0 instead.", _values[WorldCfg.RateRepaircost]);
                _values[WorldCfg.RateRepaircost] = 0.0f;
            }
            _values[WorldCfg.RateReputationGain] = GetDefaultValue("Rate.Reputation.Gain", 1.0f);
            _values[WorldCfg.RateReputationLowLevelKill] = GetDefaultValue("Rate.Reputation.LowLevel.Kill", 1.0f);
            _values[WorldCfg.RateReputationLowLevelQuest] = GetDefaultValue("Rate.Reputation.LowLevel.Quest", 1.0f);
            _values[WorldCfg.RateReputationRecruitAFriendBonus] = GetDefaultValue("Rate.Reputation.RecruitAFriendBonus", 0.1f);
            _values[WorldCfg.RateCreatureNormalDamage] = GetDefaultValue("Rate.Creature.Normal.Damage", 1.0f);
            _values[WorldCfg.RateCreatureEliteEliteDamage] = GetDefaultValue("Rate.Creature.Elite.Elite.Damage", 1.0f);
            _values[WorldCfg.RateCreatureEliteRareeliteDamage] = GetDefaultValue("Rate.Creature.Elite.RAREELITE.Damage", 1.0f);
            _values[WorldCfg.RateCreatureEliteWorldbossDamage] = GetDefaultValue("Rate.Creature.Elite.WORLDBOSS.Damage", 1.0f);
            _values[WorldCfg.RateCreatureEliteRareDamage] = GetDefaultValue("Rate.Creature.Elite.RARE.Damage", 1.0f);
            _values[WorldCfg.RateCreatureNormalHp] = GetDefaultValue("Rate.Creature.Normal.HP", 1.0f);
            _values[WorldCfg.RateCreatureEliteEliteHp] = GetDefaultValue("Rate.Creature.Elite.Elite.HP", 1.0f);
            _values[WorldCfg.RateCreatureEliteRareeliteHp] = GetDefaultValue("Rate.Creature.Elite.RAREELITE.HP", 1.0f);
            _values[WorldCfg.RateCreatureEliteWorldbossHp] = GetDefaultValue("Rate.Creature.Elite.WORLDBOSS.HP", 1.0f);
            _values[WorldCfg.RateCreatureEliteRareHp] = GetDefaultValue("Rate.Creature.Elite.RARE.HP", 1.0f);
            _values[WorldCfg.RateCreatureNormalSpelldamage] = GetDefaultValue("Rate.Creature.Normal.SpellDamage", 1.0f);
            _values[WorldCfg.RateCreatureEliteEliteSpelldamage] = GetDefaultValue("Rate.Creature.Elite.Elite.SpellDamage", 1.0f);
            _values[WorldCfg.RateCreatureEliteRareeliteSpelldamage] = GetDefaultValue("Rate.Creature.Elite.RAREELITE.SpellDamage", 1.0f);
            _values[WorldCfg.RateCreatureEliteWorldbossSpelldamage] = GetDefaultValue("Rate.Creature.Elite.WORLDBOSS.SpellDamage", 1.0f);
            _values[WorldCfg.RateCreatureEliteRareSpelldamage] = GetDefaultValue("Rate.Creature.Elite.RARE.SpellDamage", 1.0f);
            _values[WorldCfg.RateCreatureAggro] = GetDefaultValue("Rate.Creature.Aggro", 1.0f);
            _values[WorldCfg.RateRestIngame] = GetDefaultValue("Rate.Rest.InGame", 1.0f);
            _values[WorldCfg.RateRestOfflineInTavernOrCity] = GetDefaultValue("Rate.Rest.Offline.InTavernOrCity", 1.0f);
            _values[WorldCfg.RateRestOfflineInWilderness] = GetDefaultValue("Rate.Rest.Offline.InWilderness", 1.0f);
            _values[WorldCfg.RateDamageFall] = GetDefaultValue("Rate.Damage.Fall", 1.0f);
            _values[WorldCfg.RateAuctionTime] = GetDefaultValue("Rate.Auction.Time", 1.0f);
            _values[WorldCfg.RateAuctionDeposit] = GetDefaultValue("Rate.Auction.Deposit", 1.0f);
            _values[WorldCfg.RateAuctionCut] = GetDefaultValue("Rate.Auction.Cut", 1.0f);
            _values[WorldCfg.RateHonor] = GetDefaultValue("Rate.Honor", 1.0f);
            _values[WorldCfg.RateInstanceResetTime] = GetDefaultValue("Rate.InstanceResetTime", 1.0f);
            _values[WorldCfg.RateTalent] = GetDefaultValue("Rate.Talent", 1.0f);
            if ((float)_values[WorldCfg.RateTalent] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, "Rate.Talent ({0}) must be > 0. Using 1 instead.", _values[WorldCfg.RateTalent]);
                _values[WorldCfg.RateTalent] = 1.0f;
            }
            _values[WorldCfg.RateMovespeed] = GetDefaultValue("Rate.MoveSpeed", 1.0f);
            if ((float)_values[WorldCfg.RateMovespeed] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, "Rate.MoveSpeed ({0}) must be > 0. Using 1 instead.", _values[WorldCfg.RateMovespeed]);
                _values[WorldCfg.RateMovespeed] = 1.0f;
            }

            _values[WorldCfg.RateCorpseDecayLooted] = GetDefaultValue("Rate.Corpse.Decay.Looted", 0.5f);

            _values[WorldCfg.RateDurabilityLossOnDeath] = GetDefaultValue("DurabilityLoss.OnDeath", 10.0f);
            if ((float)_values[WorldCfg.RateDurabilityLossOnDeath] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, "DurabilityLoss.OnDeath ({0}) must be >=0. Using 0.0 instead.", _values[WorldCfg.RateDurabilityLossOnDeath]);
                _values[WorldCfg.RateDurabilityLossOnDeath] = 0.0f;
            }
            if ((float)_values[WorldCfg.RateDurabilityLossOnDeath] > 100.0f)
            {
                Log.outError(LogFilter.ServerLoading, "DurabilityLoss.OnDeath ({0}) must be <= 100. Using 100.0 instead.", _values[WorldCfg.RateDurabilityLossOnDeath]);
                _values[WorldCfg.RateDurabilityLossOnDeath] = 0.0f;
            }
            _values[WorldCfg.RateDurabilityLossOnDeath] = (float)_values[WorldCfg.RateDurabilityLossOnDeath] / 100.0f;

            _values[WorldCfg.RateDurabilityLossDamage] = GetDefaultValue("DurabilityLossChance.Damage", 0.5f);
            if ((float)_values[WorldCfg.RateDurabilityLossDamage] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, "DurabilityLossChance.Damage ({0}) must be >=0. Using 0.0 instead.", _values[WorldCfg.RateDurabilityLossDamage]);
                _values[WorldCfg.RateDurabilityLossDamage] = 0.0f;
            }
            _values[WorldCfg.RateDurabilityLossAbsorb] = GetDefaultValue("DurabilityLossChance.Absorb", 0.5f);
            if ((float)_values[WorldCfg.RateDurabilityLossAbsorb] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, "DurabilityLossChance.Absorb ({0}) must be >=0. Using 0.0 instead.", _values[WorldCfg.RateDurabilityLossAbsorb]);
                _values[WorldCfg.RateDurabilityLossAbsorb] = 0.0f;
            }
            _values[WorldCfg.RateDurabilityLossParry] = GetDefaultValue("DurabilityLossChance.Parry", 0.05f);
            if ((float)_values[WorldCfg.RateDurabilityLossParry] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, "DurabilityLossChance.Parry ({0}) must be >=0. Using 0.0 instead.", _values[WorldCfg.RateDurabilityLossParry]);
                _values[WorldCfg.RateDurabilityLossParry] = 0.0f;
            }
            _values[WorldCfg.RateDurabilityLossBlock] = GetDefaultValue("DurabilityLossChance.Block", 0.05f);
            if ((float)_values[WorldCfg.RateDurabilityLossBlock] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, "DurabilityLossChance.Block ({0}) must be >=0. Using 0.0 instead.", _values[WorldCfg.RateDurabilityLossBlock]);
                _values[WorldCfg.RateDurabilityLossBlock] = 0.0f;
            }
            _values[WorldCfg.RateMoneyQuest] = GetDefaultValue("Rate.Quest.Money.Reward", 1.0f);
            if ((float)_values[WorldCfg.RateMoneyQuest] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, "Rate.Quest.Money.Reward ({0}) must be >=0. Using 0 instead.", _values[WorldCfg.RateMoneyQuest]);
                _values[WorldCfg.RateMoneyQuest] = 0.0f;
            }
            _values[WorldCfg.RateMoneyMaxLevelQuest] = GetDefaultValue("Rate.Quest.Money.Max.Level.Reward", 1.0f);
            if ((float)_values[WorldCfg.RateMoneyMaxLevelQuest] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, "Rate.Quest.Money.Max.Level.Reward ({0}) must be >=0. Using 0 instead.", _values[WorldCfg.RateMoneyMaxLevelQuest]);
                _values[WorldCfg.RateMoneyMaxLevelQuest] = 0.0f;
            }

            // Read other configuration items from the config file
            _values[WorldCfg.DurabilityLossInPvp] = GetDefaultValue("DurabilityLoss.InPvP", false);

            _values[WorldCfg.Compression] = GetDefaultValue("Compression", 1);
            if ((int)_values[WorldCfg.Compression] < 1 || (int)_values[WorldCfg.Compression] > 9)
            {
                Log.outError(LogFilter.ServerLoading, "Compression Level ({0}) must be in range 1..9. Using default compression Level (1).", _values[WorldCfg.Compression]);
                _values[WorldCfg.Compression] = 1;
            }
            _values[WorldCfg.AddonChannel] = GetDefaultValue("AddonChannel", true);
            _values[WorldCfg.CleanCharacterDb] = GetDefaultValue("CleanCharacterDB", false);
            _values[WorldCfg.PersistentCharacterCleanFlags] = GetDefaultValue("PersistentCharacterCleanFlags", 0);
            _values[WorldCfg.AuctionReplicateDelay] = GetDefaultValue("Auction.ReplicateItemsCooldown", 900);
            _values[WorldCfg.AuctionSearchDelay] = GetDefaultValue("Auction.SearchDelay", 300);
            if ((int)_values[WorldCfg.AuctionSearchDelay] < 100 || (int)_values[WorldCfg.AuctionSearchDelay] > 10000)
            {
                Log.outError(LogFilter.ServerLoading, "Auction.SearchDelay ({0}) must be between 100 and 10000. Using default of 300ms", _values[WorldCfg.AuctionSearchDelay]);
                _values[WorldCfg.AuctionSearchDelay] = 300;
            }
            _values[WorldCfg.AuctionTaintedSearchDelay] = GetDefaultValue("Auction.TaintedSearchDelay", 3000);
            if ((int)_values[WorldCfg.AuctionTaintedSearchDelay] < 100 || (int)_values[WorldCfg.AuctionTaintedSearchDelay] > 10000)
            {
                Log.outError(LogFilter.ServerLoading, $"Auction.TaintedSearchDelay ({_values[WorldCfg.AuctionTaintedSearchDelay]}) must be between 100 and 10000. Using default of 3s");
                _values[WorldCfg.AuctionTaintedSearchDelay] = 3000;
            }
            _values[WorldCfg.ChatChannelLevelReq] = GetDefaultValue("ChatLevelReq.Channel", 1);
            _values[WorldCfg.ChatWhisperLevelReq] = GetDefaultValue("ChatLevelReq.Whisper", 1);
            _values[WorldCfg.ChatEmoteLevelReq] = GetDefaultValue("ChatLevelReq.Emote", 1);
            _values[WorldCfg.ChatSayLevelReq] = GetDefaultValue("ChatLevelReq.Say", 1);
            _values[WorldCfg.ChatYellLevelReq] = GetDefaultValue("ChatLevelReq.Yell", 1);
            _values[WorldCfg.PartyLevelReq] = GetDefaultValue("PartyLevelReq", 1);
            _values[WorldCfg.TradeLevelReq] = GetDefaultValue("LevelReq.Trade", 1);
            _values[WorldCfg.AuctionLevelReq] = GetDefaultValue("LevelReq.Auction", 1);
            _values[WorldCfg.MailLevelReq] = GetDefaultValue("LevelReq.Mail", 1);
            _values[WorldCfg.PreserveCustomChannels] = GetDefaultValue("PreserveCustomChannels", false);
            _values[WorldCfg.PreserveCustomChannelDuration] = GetDefaultValue("PreserveCustomChannelDuration", 14);
            _values[WorldCfg.PreserveCustomChannelInterval] = GetDefaultValue("PreserveCustomChannelInterval", 5);
            _values[WorldCfg.GridUnload] = GetDefaultValue("GridUnload", true);
            _values[WorldCfg.BasemapLoadGrids] = GetDefaultValue("BaseMapLoadAllGrids", false);
            if ((bool)_values[WorldCfg.BasemapLoadGrids] && (bool)_values[WorldCfg.GridUnload])
            {
                Log.outError(LogFilter.ServerLoading, "BaseMapLoadAllGrids enabled, but GridUnload also enabled. GridUnload must be disabled to enable base map pre-loading. Base map pre-loading disabled");
                _values[WorldCfg.BasemapLoadGrids] = false;
            }
            _values[WorldCfg.InstancemapLoadGrids] = GetDefaultValue("InstanceMapLoadAllGrids", false);
            if ((bool)_values[WorldCfg.InstancemapLoadGrids] && (bool)_values[WorldCfg.GridUnload])
            {
                Log.outError(LogFilter.ServerLoading, "InstanceMapLoadAllGrids enabled, but GridUnload also enabled. GridUnload must be disabled to enable instance map pre-loading. Instance map pre-loading disabled");
                _values[WorldCfg.InstancemapLoadGrids] = false;
            }

            _values[WorldCfg.IntervalSave] = GetDefaultValue("PlayerSaveInterval", 15 * Time.Minute * Time.InMilliseconds);
            _values[WorldCfg.IntervalDisconnectTolerance] = GetDefaultValue("DisconnectToleranceInterval", 0);
            _values[WorldCfg.StatsSaveOnlyOnLogout] = GetDefaultValue("PlayerSave.Stats.SaveOnlyOnLogout", true);

            _values[WorldCfg.MinLevelStatSave] = GetDefaultValue("PlayerSave.Stats.MinLevel", 0);
            if ((int)_values[WorldCfg.MinLevelStatSave] > SharedConst.MaxLevel)
            {
                Log.outError(LogFilter.ServerLoading, "PlayerSave.Stats.MinLevel ({0}) must be in range 0..80. Using default, do not save character Stats (0).", _values[WorldCfg.MinLevelStatSave]);
                _values[WorldCfg.MinLevelStatSave] = 0;
            }

            _values[WorldCfg.IntervalGridclean] = GetDefaultValue("GridCleanUpDelay", 5 * Time.Minute * Time.InMilliseconds);
            if ((int)_values[WorldCfg.IntervalGridclean] < MapConst.MinGridDelay)
            {
                Log.outError(LogFilter.ServerLoading, "GridCleanUpDelay ({0}) must be greater {1} Use this minimal value.", _values[WorldCfg.IntervalGridclean], MapConst.MinGridDelay);
                _values[WorldCfg.IntervalGridclean] = MapConst.MinGridDelay;
            }

            _values[WorldCfg.IntervalMapupdate] = GetDefaultValue("MapUpdateInterval", 10);
            if ((int)_values[WorldCfg.IntervalMapupdate] < MapConst.MinMapUpdateDelay)
            {
                Log.outError(LogFilter.ServerLoading, "MapUpdateInterval ({0}) must be greater {1}. Use this minimal value.", _values[WorldCfg.IntervalMapupdate], MapConst.MinMapUpdateDelay);
                _values[WorldCfg.IntervalMapupdate] = MapConst.MinMapUpdateDelay;
            }

            _values[WorldCfg.IntervalChangeweather] = GetDefaultValue("ChangeWeatherInterval", 10 * Time.Minute * Time.InMilliseconds);
            if (reload)
            {
                int val = GetDefaultValue("WorldServerPort", 8085);
                if (val != (int)_values[WorldCfg.PortWorld])
                    Log.outError(LogFilter.ServerLoading, "WorldServerPort option can't be changed at worldserver.conf reload, using current value ({0}).", _values[WorldCfg.PortWorld]);

                val = GetDefaultValue("InstanceServerPort", 8086);
                if (val != (int)_values[WorldCfg.PortInstance])
                    Log.outError(LogFilter.ServerLoading, "InstanceServerPort option can't be changed at worldserver.conf reload, using current value ({0}).", _values[WorldCfg.PortInstance]);
            }
            else
            {
                _values[WorldCfg.PortWorld] = GetDefaultValue("WorldServerPort", 8085);
                _values[WorldCfg.PortInstance] = GetDefaultValue("InstanceServerPort", 8086);
            }

            // Config values are in "milliseconds" but we handle SocketTimeOut only as "seconds" so divide by 1000
            _values[WorldCfg.SocketTimeoutTime] = GetDefaultValue("SocketTimeOutTime", 900000) / 1000;
            _values[WorldCfg.SocketTimeoutTimeActive] = GetDefaultValue("SocketTimeOutTimeActive", 60000) / 1000;
            _values[WorldCfg.SessionAddDelay] = GetDefaultValue("SessionAddDelay", 10000);

            _values[WorldCfg.GroupXpDistance] = GetDefaultValue("MaxGroupXPDistance", 74.0f);
            _values[WorldCfg.MaxRecruitAFriendDistance] = GetDefaultValue("MaxRecruitAFriendBonusDistance", 100.0f);
            _values[WorldCfg.MinQuestScaledXpRatio] = GetDefaultValue("MinQuestScaledXPRatio", 0);
            if ((int)_values[WorldCfg.MinQuestScaledXpRatio] > 100)
            {
                Log.outError(LogFilter.ServerLoading, $"MinQuestScaledXPRatio ({_values[WorldCfg.MinQuestScaledXpRatio]}) must be in range 0..100. Set to 0.");
                _values[WorldCfg.MinQuestScaledXpRatio] = 0;
            }

            _values[WorldCfg.MinCreatureScaledXpRatio] = GetDefaultValue("MinCreatureScaledXPRatio", 0);
            if ((int)_values[WorldCfg.MinCreatureScaledXpRatio] > 100)
            {
                Log.outError(LogFilter.ServerLoading, $"MinCreatureScaledXPRatio ({_values[WorldCfg.MinCreatureScaledXpRatio]}) must be in range 0..100. Set to 0.");
                _values[WorldCfg.MinCreatureScaledXpRatio] = 0;
            }

            _values[WorldCfg.MinDiscoveredScaledXpRatio] = GetDefaultValue("MinDiscoveredScaledXPRatio", 0);
            if ((int)_values[WorldCfg.MinDiscoveredScaledXpRatio] > 100)
            {
                Log.outError(LogFilter.ServerLoading, $"MinDiscoveredScaledXPRatio ({_values[WorldCfg.MinDiscoveredScaledXpRatio]}) must be in range 0..100. Set to 0.");
                _values[WorldCfg.MinDiscoveredScaledXpRatio] = 0;
            }

            /// @todo Add MonsterSight (with meaning) in worldserver.conf or put them as define
            _values[WorldCfg.SightMonster] = GetDefaultValue("MonsterSight", 50.0f);

            _values[WorldCfg.RegenHpCannotReachTargetInRaid] = GetDefaultValue("Creature.RegenHPCannotReachTargetInRaid", true);

            if (reload)
            {
                int val = GetDefaultValue("GameType", 0);
                if (val != (int)_values[WorldCfg.GameType])
                    Log.outError(LogFilter.ServerLoading, "GameType option can't be changed at worldserver.conf reload, using current value ({0}).", _values[WorldCfg.GameType]);
            }
            else
                _values[WorldCfg.GameType] = GetDefaultValue("GameType", 0);

            if (reload)
            {
                int val = (int)GetDefaultValue("RealmZone", RealmZones.Development);
                if (val != (int)_values[WorldCfg.RealmZone])
                    Log.outError(LogFilter.ServerLoading, "RealmZone option can't be changed at worldserver.conf reload, using current value ({0}).", _values[WorldCfg.RealmZone]);
            }
            else
                _values[WorldCfg.RealmZone] = GetDefaultValue("RealmZone", (int)RealmZones.Development);

            _values[WorldCfg.AllowTwoSideInteractionCalendar] = GetDefaultValue("AllowTwoSide.Interaction.Calendar", false);
            _values[WorldCfg.AllowTwoSideInteractionChannel] = GetDefaultValue("AllowTwoSide.Interaction.Channel", false);
            _values[WorldCfg.AllowTwoSideInteractionGroup] = GetDefaultValue("AllowTwoSide.Interaction.Group", false);
            _values[WorldCfg.AllowTwoSideInteractionGuild] = GetDefaultValue("AllowTwoSide.Interaction.Guild", false);
            _values[WorldCfg.AllowTwoSideInteractionAuction] = GetDefaultValue("AllowTwoSide.Interaction.Auction", true);
            _values[WorldCfg.AllowTwoSideTrade] = GetDefaultValue("AllowTwoSide.Trade", false);
            _values[WorldCfg.StrictPlayerNames] = GetDefaultValue("StrictPlayerNames", 0);
            _values[WorldCfg.StrictCharterNames] = GetDefaultValue("StrictCharterNames", 0);
            _values[WorldCfg.StrictPetNames] = GetDefaultValue("StrictPetNames", 0);

            _values[WorldCfg.MinPlayerName] = GetDefaultValue("MinPlayerName", 2);
            if ((int)_values[WorldCfg.MinPlayerName] < 1 || (int)_values[WorldCfg.MinPlayerName] > 12)
            {
                Log.outError(LogFilter.ServerLoading, "MinPlayerName ({0}) must be in range 1..{1}. Set to 2.", _values[WorldCfg.MinPlayerName], 12);
                _values[WorldCfg.MinPlayerName] = 2;
            }

            _values[WorldCfg.MinCharterName] = GetDefaultValue("MinCharterName", 2);
            if ((int)_values[WorldCfg.MinCharterName] < 1 || (int)_values[WorldCfg.MinCharterName] > 24)
            {
                Log.outError(LogFilter.ServerLoading, "MinCharterName ({0}) must be in range 1..{1}. Set to 2.", _values[WorldCfg.MinCharterName], 24);
                _values[WorldCfg.MinCharterName] = 2;
            }

            _values[WorldCfg.MinPetName] = GetDefaultValue("MinPetName", 2);
            if ((int)_values[WorldCfg.MinPetName] < 1 || (int)_values[WorldCfg.MinPetName] > 12)
            {
                Log.outError(LogFilter.ServerLoading, "MinPetName ({0}) must be in range 1..{1}. Set to 2.", _values[WorldCfg.MinPetName], 12);
                _values[WorldCfg.MinPetName] = 2;
            }

            _values[WorldCfg.CharterCostGuild] = GetDefaultValue("Guild.CharterCost", 1000);
            _values[WorldCfg.CharterCostArena2v2] = GetDefaultValue("ArenaTeam.CharterCost.2v2", 800000);
            _values[WorldCfg.CharterCostArena3v3] = GetDefaultValue("ArenaTeam.CharterCost.3v3", 1200000);
            _values[WorldCfg.CharterCostArena5v5] = GetDefaultValue("ArenaTeam.CharterCost.5v5", 2000000);

            _values[WorldCfg.CharacterCreatingDisabled] = GetDefaultValue("CharacterCreating.Disabled", 0);
            _values[WorldCfg.CharacterCreatingDisabledRacemask] = GetDefaultValue("CharacterCreating.Disabled.RaceMask", 0);
            _values[WorldCfg.CharacterCreatingDisabledClassmask] = GetDefaultValue("CharacterCreating.Disabled.ClassMask", 0);

            _values[WorldCfg.CharactersPerRealm] = GetDefaultValue("CharactersPerRealm", 60);
            if ((int)_values[WorldCfg.CharactersPerRealm] < 1 || (int)_values[WorldCfg.CharactersPerRealm] > 200)
            {
                Log.outError(LogFilter.ServerLoading, "CharactersPerRealm ({0}) must be in range 1..200. Set to 200.", _values[WorldCfg.CharactersPerRealm]);
                _values[WorldCfg.CharactersPerRealm] = 200;
            }

            // must be after CharactersPerRealm
            _values[WorldCfg.CharactersPerAccount] = GetDefaultValue("CharactersPerAccount", 60);
            if ((int)_values[WorldCfg.CharactersPerAccount] < (int)_values[WorldCfg.CharactersPerRealm])
            {
                Log.outError(LogFilter.ServerLoading, "CharactersPerAccount ({0}) can't be less than CharactersPerRealm ({1}).", _values[WorldCfg.CharactersPerAccount], _values[WorldCfg.CharactersPerRealm]);
                _values[WorldCfg.CharactersPerAccount] = _values[WorldCfg.CharactersPerRealm];
            }

            _values[WorldCfg.CharacterCreatingEvokersPerRealm] = GetDefaultValue("CharacterCreating.EvokersPerRealm", 1);
            if ((int)_values[WorldCfg.CharacterCreatingEvokersPerRealm] < 0 || (int)_values[WorldCfg.CharacterCreatingEvokersPerRealm] > 10)
            {
                Log.outError(LogFilter.ServerLoading, $"CharacterCreating.EvokersPerRealm ({_values[WorldCfg.CharacterCreatingEvokersPerRealm]}) must be in range 0..10. Set to 1.");
                _values[WorldCfg.CharacterCreatingEvokersPerRealm] = 1;
            }

            _values[WorldCfg.CharacterCreatingMinLevelForDemonHunter] = GetDefaultValue("CharacterCreating.MinLevelForDemonHunter", 0);
            _values[WorldCfg.CharacterCreatingMinLevelForEvoker] = GetDefaultValue("CharacterCreating.MinLevelForEvoker", 50);
            _values[WorldCfg.CharacterCreatingDisableAlliedRaceAchievementRequirement] = GetDefaultValue("CharacterCreating.DisableAlliedRaceAchievementRequirement", false);

            _values[WorldCfg.SkipCinematics] = GetDefaultValue("SkipCinematics", 0);
            if ((int)_values[WorldCfg.SkipCinematics] < 0 || (int)_values[WorldCfg.SkipCinematics] > 2)
            {
                Log.outError(LogFilter.ServerLoading, "SkipCinematics ({0}) must be in range 0..2. Set to 0.", _values[WorldCfg.SkipCinematics]);
                _values[WorldCfg.SkipCinematics] = 0;
            }

            if (reload)
            {
                int val = GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);
                if (val != (int)_values[WorldCfg.MaxPlayerLevel])
                    Log.outError(LogFilter.ServerLoading, "MaxPlayerLevel option can't be changed at config reload, using current value ({0}).", _values[WorldCfg.MaxPlayerLevel]);
            }
            else
                _values[WorldCfg.MaxPlayerLevel] = GetDefaultValue("MaxPlayerLevel", SharedConst.DefaultMaxLevel);

            if ((int)_values[WorldCfg.MaxPlayerLevel] > SharedConst.MaxLevel)
            {
                Log.outError(LogFilter.ServerLoading, "MaxPlayerLevel ({0}) must be in range 1..{1}. Set to {1}.", _values[WorldCfg.MaxPlayerLevel], SharedConst.MaxLevel);
                _values[WorldCfg.MaxPlayerLevel] = SharedConst.MaxLevel;
            }

            _values[WorldCfg.MinDualspecLevel] = GetDefaultValue("MinDualSpecLevel", 40);

            _values[WorldCfg.StartPlayerLevel] = GetDefaultValue("StartPlayerLevel", 1);
            if ((int)_values[WorldCfg.StartPlayerLevel] < 1)
            {
                Log.outError(LogFilter.ServerLoading, "StartPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to 1.", _values[WorldCfg.StartPlayerLevel], _values[WorldCfg.MaxPlayerLevel]);
                _values[WorldCfg.StartPlayerLevel] = 1;
            }
            else if ((int)_values[WorldCfg.StartPlayerLevel] > (int)_values[WorldCfg.MaxPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, "StartPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to {2}.", _values[WorldCfg.StartPlayerLevel], _values[WorldCfg.MaxPlayerLevel], _values[WorldCfg.MaxPlayerLevel]);
                _values[WorldCfg.StartPlayerLevel] = _values[WorldCfg.MaxPlayerLevel];
            }

            _values[WorldCfg.StartDeathKnightPlayerLevel] = GetDefaultValue("StartDeathKnightPlayerLevel", 8);
            if ((int)_values[WorldCfg.StartDeathKnightPlayerLevel] < 1)
            {
                Log.outError(LogFilter.ServerLoading, "StartDeathKnightPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to 1.",
                    _values[WorldCfg.StartDeathKnightPlayerLevel], _values[WorldCfg.MaxPlayerLevel]);
                _values[WorldCfg.StartDeathKnightPlayerLevel] = 1;
            }
            else if ((int)_values[WorldCfg.StartDeathKnightPlayerLevel] > (int)_values[WorldCfg.MaxPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, "StartDeathKnightPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to {2}.",
                    _values[WorldCfg.StartDeathKnightPlayerLevel], _values[WorldCfg.MaxPlayerLevel], _values[WorldCfg.MaxPlayerLevel]);
                _values[WorldCfg.StartDeathKnightPlayerLevel] = _values[WorldCfg.MaxPlayerLevel];
            }

            _values[WorldCfg.StartDemonHunterPlayerLevel] = GetDefaultValue("StartDemonHunterPlayerLevel", 8);
            if ((int)_values[WorldCfg.StartDemonHunterPlayerLevel] < 1)
            {
                Log.outError(LogFilter.ServerLoading, "StartDemonHunterPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to 1.",
                    _values[WorldCfg.StartDemonHunterPlayerLevel], _values[WorldCfg.MaxPlayerLevel]);
                _values[WorldCfg.StartDemonHunterPlayerLevel] = 1;
            }
            else if ((int)_values[WorldCfg.StartDemonHunterPlayerLevel] > (int)_values[WorldCfg.MaxPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, "StartDemonHunterPlayerLevel ({0}) must be in range 1..MaxPlayerLevel({1}). Set to {2}.",
                    _values[WorldCfg.StartDemonHunterPlayerLevel], _values[WorldCfg.MaxPlayerLevel], _values[WorldCfg.MaxPlayerLevel]);
                _values[WorldCfg.StartDemonHunterPlayerLevel] = _values[WorldCfg.MaxPlayerLevel];
            }

            _values[WorldCfg.StartEvokerPlayerLevel] = GetDefaultValue("StartEvokerPlayerLevel", 58);
            if ((int)_values[WorldCfg.StartEvokerPlayerLevel] < 1)
            {
                Log.outError(LogFilter.ServerLoading, $"StartEvokerPlayerLevel ({_values[WorldCfg.StartEvokerPlayerLevel]}) must be in range 1..MaxPlayerLevel({_values[WorldCfg.MaxPlayerLevel]}). Set to 1.");
                _values[WorldCfg.StartEvokerPlayerLevel] = 1;
            }
            else if ((int)_values[WorldCfg.StartEvokerPlayerLevel] > (int)_values[WorldCfg.MaxPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, $"StartEvokerPlayerLevel ({_values[WorldCfg.StartEvokerPlayerLevel]}) must be in range 1..MaxPlayerLevel({_values[WorldCfg.MaxPlayerLevel]}). Set to {_values[WorldCfg.MaxPlayerLevel]}.");
                _values[WorldCfg.StartEvokerPlayerLevel] = _values[WorldCfg.MaxPlayerLevel];
            }

            _values[WorldCfg.StartAlliedRaceLevel] = GetDefaultValue("StartAlliedRacePlayerLevel", 10);
            if ((int)_values[WorldCfg.StartAlliedRaceLevel] < 1)
            {
                Log.outError(LogFilter.ServerLoading, $"StartAlliedRaceLevel ({_values[WorldCfg.StartAlliedRaceLevel]}) must be in range 1..MaxPlayerLevel({_values[WorldCfg.MaxPlayerLevel]}). Set to 1.");
                _values[WorldCfg.StartAlliedRaceLevel] = 1;
            }
            else if ((int)_values[WorldCfg.StartAlliedRaceLevel] > (int)_values[WorldCfg.MaxPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, $"StartAlliedRaceLevel ({_values[WorldCfg.StartAlliedRaceLevel]}) must be in range 1..MaxPlayerLevel({_values[WorldCfg.MaxPlayerLevel]}). Set to {_values[WorldCfg.MaxPlayerLevel]}.");
                _values[WorldCfg.StartAlliedRaceLevel] = _values[WorldCfg.MaxPlayerLevel];
            }

            _values[WorldCfg.StartPlayerMoney] = GetDefaultValue("StartPlayerMoney", 0);
            if ((int)_values[WorldCfg.StartPlayerMoney] < 0)
            {
                Log.outError(LogFilter.ServerLoading, "StartPlayerMoney ({0}) must be in range 0..{1}. Set to {2}.", _values[WorldCfg.StartPlayerMoney], PlayerConst.MaxMoneyAmount, 0);
                _values[WorldCfg.StartPlayerMoney] = 0;
            }
            else if ((int)_values[WorldCfg.StartPlayerMoney] > 0x7FFFFFFF - 1) // TODO: (See MaxMoneyAMOUNT)
            {
                Log.outError(LogFilter.ServerLoading, "StartPlayerMoney ({0}) must be in range 0..{1}. Set to {2}.",
                    _values[WorldCfg.StartPlayerMoney], 0x7FFFFFFF - 1, 0x7FFFFFFF - 1);
                _values[WorldCfg.StartPlayerMoney] = 0x7FFFFFFF - 1;
            }

            _values[WorldCfg.CurrencyResetHour] = GetDefaultValue("Currency.ResetHour", 3);
            if ((int)_values[WorldCfg.CurrencyResetHour] > 23)
            {
                Log.outError(LogFilter.ServerLoading, "StartPlayerMoney ({0}) must be in range 0..{1}. Set to {2}.", _values[WorldCfg.CurrencyResetHour] = 3);
            }
            _values[WorldCfg.CurrencyResetDay] = GetDefaultValue("Currency.ResetDay", 3);
            if ((int)_values[WorldCfg.CurrencyResetDay] > 6)
            {
                Log.outError(LogFilter.ServerLoading, "Currency.ResetDay ({0}) can't be load. Set to 3.", _values[WorldCfg.CurrencyResetDay]);
                _values[WorldCfg.CurrencyResetDay] = 3;
            }
            _values[WorldCfg.CurrencyResetInterval] = GetDefaultValue("Currency.ResetInterval", 7);
            if ((int)_values[WorldCfg.CurrencyResetInterval] <= 0)
            {
                Log.outError(LogFilter.ServerLoading, "Currency.ResetInterval ({0}) must be > 0, set to default 7.", _values[WorldCfg.CurrencyResetInterval]);
                _values[WorldCfg.CurrencyResetInterval] = 7;
            }

            _values[WorldCfg.CurrencyStartApexisCrystals] = GetDefaultValue("Currency.StartApexisCrystals", 0);
            if ((int)_values[WorldCfg.CurrencyStartApexisCrystals] < 0)
            {
                Log.outError(LogFilter.ServerLoading, "Currency.StartApexisCrystals ({0}) must be >= 0, set to default 0.", _values[WorldCfg.CurrencyStartApexisCrystals]);
                _values[WorldCfg.CurrencyStartApexisCrystals] = 0;
            }
            _values[WorldCfg.CurrencyMaxApexisCrystals] = GetDefaultValue("Currency.MaxApexisCrystals", 20000);
            if ((int)_values[WorldCfg.CurrencyMaxApexisCrystals] < 0)
            {
                Log.outError(LogFilter.ServerLoading, "Currency.MaxApexisCrystals ({0}) can't be negative. Set to default 20000.", _values[WorldCfg.CurrencyMaxApexisCrystals]);
                _values[WorldCfg.CurrencyMaxApexisCrystals] = 20000;
            }
            _values[WorldCfg.CurrencyMaxApexisCrystals] = (int)_values[WorldCfg.CurrencyMaxApexisCrystals] * 100;     //precision mod

            _values[WorldCfg.CurrencyStartJusticePoints] = GetDefaultValue("Currency.StartJusticePoints", 0);
            if ((int)_values[WorldCfg.CurrencyStartJusticePoints] < 0)
            {
                Log.outError(LogFilter.ServerLoading, "Currency.StartJusticePoints ({0}) must be >= 0, set to default 0.", _values[WorldCfg.CurrencyStartJusticePoints]);
                _values[WorldCfg.CurrencyStartJusticePoints] = 0;
            }
            _values[WorldCfg.CurrencyMaxJusticePoints] = GetDefaultValue("Currency.MaxJusticePoints", 4000);
            if ((int)_values[WorldCfg.CurrencyMaxJusticePoints] < 0)
            {
                Log.outError(LogFilter.ServerLoading, "Currency.MaxJusticePoints ({0}) can't be negative. Set to default 4000.", _values[WorldCfg.CurrencyMaxJusticePoints]);
                _values[WorldCfg.CurrencyMaxJusticePoints] = 4000;
            }
            _values[WorldCfg.CurrencyMaxJusticePoints] = (int)_values[WorldCfg.CurrencyMaxJusticePoints] * 100;     //precision mod

            _values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel] = GetDefaultValue("RecruitAFriend.MaxLevel", 60);
            if ((int)_values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel] > (int)_values[WorldCfg.MaxPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, "RecruitAFriend.MaxLevel ({0}) must be in the range 0..MaxLevel({1}). Set to {2}.",
                    _values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel], _values[WorldCfg.MaxPlayerLevel], 60);
                _values[WorldCfg.MaxRecruitAFriendBonusPlayerLevel] = 60;
            }

            _values[WorldCfg.MaxRecruitAFriendBonusPlayerLevelDifference] = GetDefaultValue("RecruitAFriend.MaxDifference", 4);
            _values[WorldCfg.AllTaxiPaths] = GetDefaultValue("AllFlightPaths", false);
            _values[WorldCfg.InstantTaxi] = GetDefaultValue("InstantFlightPaths", false);

            _values[WorldCfg.InstanceIgnoreLevel] = GetDefaultValue("Instance.IgnoreLevel", false);
            _values[WorldCfg.InstanceIgnoreRaid] = GetDefaultValue("Instance.IgnoreRaid", false);

            _values[WorldCfg.CastUnstuck] = GetDefaultValue("CastUnstuck", true);
            _values[WorldCfg.ResetScheduleWeekDay] = GetDefaultValue("ResetSchedule.WeekDay", 2);
            _values[WorldCfg.ResetScheduleHour] = GetDefaultValue("ResetSchedule.Hour", 8);
            _values[WorldCfg.InstanceUnloadDelay] = GetDefaultValue("Instance.UnloadDelay", 30 * Time.Minute * Time.InMilliseconds);
            _values[WorldCfg.DailyQuestResetTimeHour] = GetDefaultValue("Quests.DailyResetTime", 3);
            if ((int)_values[WorldCfg.DailyQuestResetTimeHour] > 23)
            {
                Log.outError(LogFilter.ServerLoading, $"Quests.DailyResetTime ({_values[WorldCfg.DailyQuestResetTimeHour]}) must be in range 0..23. Set to 3.");
                _values[WorldCfg.DailyQuestResetTimeHour] = 3;
            }

            _values[WorldCfg.WeeklyQuestResetTimeWDay] = GetDefaultValue("Quests.WeeklyResetWDay", 3);
            if ((int)_values[WorldCfg.WeeklyQuestResetTimeWDay] > 6)
            {
                Log.outError(LogFilter.ServerLoading, $"Quests.WeeklyResetDay ({_values[WorldCfg.WeeklyQuestResetTimeWDay]}) must be in range 0..6. Set to 3 (Wednesday).");
                _values[WorldCfg.WeeklyQuestResetTimeWDay] = 3;
            }

            _values[WorldCfg.MaxPrimaryTradeSkill] = GetDefaultValue("MaxPrimaryTradeSkill", 2);
            _values[WorldCfg.MinPetitionSigns] = GetDefaultValue("MinPetitionSigns", 4);
            if ((int)_values[WorldCfg.MinPetitionSigns] > 4)
            {
                Log.outError(LogFilter.ServerLoading, "MinPetitionSigns ({0}) must be in range 0..4. Set to 4.", _values[WorldCfg.MinPetitionSigns]);
                _values[WorldCfg.MinPetitionSigns] = 4;
            }

            _values[WorldCfg.GmLoginState] = GetDefaultValue("GM.LoginState", 2);
            _values[WorldCfg.GmVisibleState] = GetDefaultValue("GM.Visible", 2);
            _values[WorldCfg.GmChat] = GetDefaultValue("GM.Chat", 2);
            _values[WorldCfg.GmWhisperingTo] = GetDefaultValue("GM.WhisperingTo", 2);
            _values[WorldCfg.GmFreezeDuration] = GetDefaultValue("GM.FreezeAuraDuration", 0);

            _values[WorldCfg.GmLevelInGmList] = GetDefaultValue("GM.InGMList.Level", (int)AccountTypes.Administrator);
            _values[WorldCfg.GmLevelInWhoList] = GetDefaultValue("GM.InWhoList.Level", (int)AccountTypes.Administrator);
            _values[WorldCfg.StartGmLevel] = GetDefaultValue("GM.StartLevel", 1);
            if ((int)_values[WorldCfg.StartGmLevel] < (int)_values[WorldCfg.StartPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, "GM.StartLevel ({0}) must be in range StartPlayerLevel({1})..{2}. Set to {3}.",
                    _values[WorldCfg.StartGmLevel], _values[WorldCfg.StartPlayerLevel], SharedConst.MaxLevel, _values[WorldCfg.StartPlayerLevel]);
                _values[WorldCfg.StartGmLevel] = _values[WorldCfg.StartPlayerLevel];
            }
            else if ((int)_values[WorldCfg.StartGmLevel] > SharedConst.MaxLevel)
            {
                Log.outError(LogFilter.ServerLoading, "GM.StartLevel ({0}) must be in range 1..{1}. Set to {1}.", _values[WorldCfg.StartGmLevel], SharedConst.MaxLevel);
                _values[WorldCfg.StartGmLevel] = SharedConst.MaxLevel;
            }
            _values[WorldCfg.AllowGmGroup] = GetDefaultValue("GM.AllowInvite", false);
            _values[WorldCfg.GmLowerSecurity] = GetDefaultValue("GM.LowerSecurity", false);
            _values[WorldCfg.ForceShutdownThreshold] = GetDefaultValue("GM.ForceShutdownThreshold", 30);

            _values[WorldCfg.GroupVisibility] = GetDefaultValue("Visibility.GroupMode", 1);

            _values[WorldCfg.MailDeliveryDelay] = GetDefaultValue("MailDeliveryDelay", Time.Hour);
            _values[WorldCfg.CleanOldMailTime] = GetDefaultValue("CleanOldMailTime", 4);
            if ((int)_values[WorldCfg.CleanOldMailTime] > 23)
            {
                Log.outError(LogFilter.ServerLoading, $"CleanOldMailTime ({_values[WorldCfg.CleanOldMailTime]}) must be an hour, between 0 and 23. Set to 4.");
                _values[WorldCfg.CleanOldMailTime] = 4;
            }

            _values[WorldCfg.UptimeUpdate] = GetDefaultValue("UpdateUptimeInterval", 10);
            if ((int)_values[WorldCfg.UptimeUpdate] <= 0)
            {
                Log.outError(LogFilter.ServerLoading, "UpdateUptimeInterval ({0}) must be > 0, set to default 10.", _values[WorldCfg.UptimeUpdate]);
                _values[WorldCfg.UptimeUpdate] = 10;
            }

            // log db cleanup interval
            _values[WorldCfg.LogdbClearinterval] = GetDefaultValue("LogDB.Opt.ClearInterval", 10);
            if ((int)_values[WorldCfg.LogdbClearinterval] <= 0)
            {
                Log.outError(LogFilter.ServerLoading, "LogDB.Opt.ClearInterval ({0}) must be > 0, set to default 10.", _values[WorldCfg.LogdbClearinterval]);
                _values[WorldCfg.LogdbClearinterval] = 10;
            }
            _values[WorldCfg.LogdbCleartime] = GetDefaultValue("LogDB.Opt.ClearTime", 1209600); // 14 days default
            Log.outInfo(LogFilter.ServerLoading, "Will clear `logs` table of entries older than {0} seconds every {1} minutes.", _values[WorldCfg.LogdbCleartime], _values[WorldCfg.LogdbClearinterval]);

            _values[WorldCfg.SkillChanceOrange] = GetDefaultValue("SkillChance.Orange", 100);
            _values[WorldCfg.SkillChanceYellow] = GetDefaultValue("SkillChance.Yellow", 75);
            _values[WorldCfg.SkillChanceGreen] = GetDefaultValue("SkillChance.Green", 25);
            _values[WorldCfg.SkillChanceGrey] = GetDefaultValue("SkillChance.Grey", 0);

            _values[WorldCfg.SkillChanceMiningSteps] = GetDefaultValue("SkillChance.MiningSteps", 75);
            _values[WorldCfg.SkillChanceSkinningSteps] = GetDefaultValue("SkillChance.SkinningSteps", 75);

            _values[WorldCfg.SkillProspecting] = GetDefaultValue("SkillChance.Prospecting", false);
            _values[WorldCfg.SkillMilling] = GetDefaultValue("SkillChance.Milling", false);

            _values[WorldCfg.SkillGainCrafting] = GetDefaultValue("SkillGain.Crafting", 1);

            _values[WorldCfg.SkillGainGathering] = GetDefaultValue("SkillGain.Gathering", 1);

            _values[WorldCfg.MaxOverspeedPings] = GetDefaultValue("MaxOverspeedPings", 2);
            if ((int)_values[WorldCfg.MaxOverspeedPings] != 0 && (int)_values[WorldCfg.MaxOverspeedPings] < 2)
            {
                Log.outError(LogFilter.ServerLoading, "MaxOverspeedPings ({0}) must be in range 2..infinity (or 0 to disable check). Set to 2.", _values[WorldCfg.MaxOverspeedPings]);
                _values[WorldCfg.MaxOverspeedPings] = 2;
            }

            _values[WorldCfg.Weather] = GetDefaultValue("ActivateWeather", true);

            _values[WorldCfg.DisableBreathing] = GetDefaultValue("DisableWaterBreath", (int)AccountTypes.Console);

            if (reload)
            {
                int val = GetDefaultValue("Expansion", (int)Expansion.WarlordsOfDraenor);
                if (val != (int)_values[WorldCfg.Expansion])
                    Log.outError(LogFilter.ServerLoading, "Expansion option can't be changed at worldserver.conf reload, using current value ({0}).", _values[WorldCfg.Expansion]);
            }
            else
                _values[WorldCfg.Expansion] = GetDefaultValue("Expansion", Expansion.WarlordsOfDraenor);

            _values[WorldCfg.ChatFloodMessageCount] = GetDefaultValue("ChatFlood.MessageCount", 10);
            _values[WorldCfg.ChatFloodMessageDelay] = GetDefaultValue("ChatFlood.MessageDelay", 1);
            _values[WorldCfg.ChatFloodMuteTime] = GetDefaultValue("ChatFlood.MuteTime", 10);

            _values[WorldCfg.EventAnnounce] = GetDefaultValue("Event.Announce", false);

            _values[WorldCfg.CreatureFamilyFleeAssistanceRadius] = GetDefaultValue("CreatureFamilyFleeAssistanceRadius", 30.0f);
            _values[WorldCfg.CreatureFamilyAssistanceRadius] = GetDefaultValue("CreatureFamilyAssistanceRadius", 10.0f);
            _values[WorldCfg.CreatureFamilyAssistanceDelay] = GetDefaultValue("CreatureFamilyAssistanceDelay", 1500);
            _values[WorldCfg.CreatureFamilyFleeDelay] = GetDefaultValue("CreatureFamilyFleeDelay", 7000);

            _values[WorldCfg.WorldBossLevelDiff] = GetDefaultValue("WorldBossLevelDiff", 3);

            _values[WorldCfg.QuestEnableQuestTracker] = GetDefaultValue("Quests.EnableQuestTracker", false);

            // note: disable value (-1) will assigned as 0xFFFFFFF, to prevent overflow at calculations limit it to max possible player Level MaxLevel(100)
            _values[WorldCfg.QuestLowLevelHideDiff] = GetDefaultValue("Quests.LowLevelHideDiff", 4);
            if ((int)_values[WorldCfg.QuestLowLevelHideDiff] > SharedConst.MaxLevel)
                _values[WorldCfg.QuestLowLevelHideDiff] = SharedConst.MaxLevel;
            _values[WorldCfg.QuestHighLevelHideDiff] = GetDefaultValue("Quests.HighLevelHideDiff", 7);
            if ((int)_values[WorldCfg.QuestHighLevelHideDiff] > SharedConst.MaxLevel)
                _values[WorldCfg.QuestHighLevelHideDiff] = SharedConst.MaxLevel;
            _values[WorldCfg.QuestIgnoreRaid] = GetDefaultValue("Quests.IgnoreRaid", false);
            _values[WorldCfg.QuestIgnoreAutoAccept] = GetDefaultValue("Quests.IgnoreAutoAccept", false);
            _values[WorldCfg.QuestIgnoreAutoComplete] = GetDefaultValue("Quests.IgnoreAutoComplete", false);

            _values[WorldCfg.RandomBgResetHour] = GetDefaultValue("Battleground.Random.ResetHour", 6);
            if ((int)_values[WorldCfg.RandomBgResetHour] > 23)
            {
                Log.outError(LogFilter.ServerLoading, "Battleground.Random.ResetHour ({0}) can't be load. Set to 6.", _values[WorldCfg.RandomBgResetHour]);
                _values[WorldCfg.RandomBgResetHour] = 6;
            }

            _values[WorldCfg.CalendarDeleteOldEventsHour] = GetDefaultValue("Calendar.DeleteOldEventsHour", 6);
            if ((int)_values[WorldCfg.CalendarDeleteOldEventsHour] > 23)
            {
                Log.outError(LogFilter.Misc, $"Calendar.DeleteOldEventsHour ({_values[WorldCfg.CalendarDeleteOldEventsHour]}) can't be load. Set to 6.");
                _values[WorldCfg.CalendarDeleteOldEventsHour] = 6;
            }

            _values[WorldCfg.GuildResetHour] = GetDefaultValue("Guild.ResetHour", 6);
            if ((int)_values[WorldCfg.GuildResetHour] > 23)
            {
                Log.outError(LogFilter.Server, "Guild.ResetHour ({0}) can't be load. Set to 6.", _values[WorldCfg.GuildResetHour]);
                _values[WorldCfg.GuildResetHour] = 6;
            }

            _values[WorldCfg.DetectPosCollision] = GetDefaultValue("DetectPosCollision", true);

            _values[WorldCfg.RestrictedLfgChannel] = GetDefaultValue("Channel.RestrictedLfg", true);
            _values[WorldCfg.TalentsInspecting] = GetDefaultValue("TalentsInspecting", 1);
            _values[WorldCfg.ChatFakeMessagePreventing] = GetDefaultValue("ChatFakeMessagePreventing", false);
            _values[WorldCfg.ChatStrictLinkCheckingSeverity] = GetDefaultValue("ChatStrictLinkChecking.Severity", 0);
            _values[WorldCfg.ChatStrictLinkCheckingKick] = GetDefaultValue("ChatStrictLinkChecking.Kick", 0);

            _values[WorldCfg.CorpseDecayNormal] = GetDefaultValue("Corpse.Decay.NORMAL", 60);
            _values[WorldCfg.CorpseDecayRare] = GetDefaultValue("Corpse.Decay.RARE", 300);
            _values[WorldCfg.CorpseDecayElite] = GetDefaultValue("Corpse.Decay.ELITE", 300);
            _values[WorldCfg.CorpseDecayRareelite] = GetDefaultValue("Corpse.Decay.RAREELITE", 300);
            _values[WorldCfg.CorpseDecayWorldboss] = GetDefaultValue("Corpse.Decay.WORLDBOSS", 3600);

            _values[WorldCfg.DeathSicknessLevel] = GetDefaultValue("Death.SicknessLevel", 11);
            _values[WorldCfg.DeathCorpseReclaimDelayPvp] = GetDefaultValue("Death.CorpseReclaimDelay.PvP", true);
            _values[WorldCfg.DeathCorpseReclaimDelayPve] = GetDefaultValue("Death.CorpseReclaimDelay.PvE", true);
            _values[WorldCfg.DeathBonesWorld] = GetDefaultValue("Death.Bones.World", true);
            _values[WorldCfg.DeathBonesBgOrArena] = GetDefaultValue("Death.Bones.BattlegroundOrArena", true);

            _values[WorldCfg.DieCommandMode] = GetDefaultValue("Die.Command.Mode", true);

            _values[WorldCfg.ThreatRadius] = GetDefaultValue("ThreatRadius", 60.0f);

            // always use declined names in the russian client
            _values[WorldCfg.DeclinedNamesUsed] = (RealmZones)_values[WorldCfg.RealmZone] == RealmZones.Russian || GetDefaultValue("DeclinedNames", false);

            _values[WorldCfg.ListenRangeSay] = GetDefaultValue("ListenRange.Say", 25.0f);
            _values[WorldCfg.ListenRangeTextemote] = GetDefaultValue("ListenRange.TextEmote", 25.0f);
            _values[WorldCfg.ListenRangeYell] = GetDefaultValue("ListenRange.Yell", 300.0f);

            _values[WorldCfg.BattlegroundCastDeserter] = GetDefaultValue("Battleground.CastDeserter", true);
            _values[WorldCfg.BattlegroundQueueAnnouncerEnable] = GetDefaultValue("Battleground.QueueAnnouncer.Enable", false);
            _values[WorldCfg.BattlegroundQueueAnnouncerPlayeronly] = GetDefaultValue("Battleground.QueueAnnouncer.PlayerOnly", false);
            _values[WorldCfg.BattlegroundStoreStatisticsEnable] = GetDefaultValue("Battleground.StoreStatistics.Enable", false);
            _values[WorldCfg.BattlegroundReportAfk] = GetDefaultValue("Battleground.ReportAFK", 3);
            if ((int)_values[WorldCfg.BattlegroundReportAfk] < 1)
            {
                Log.outError(LogFilter.ServerLoading, "Battleground.ReportAFK ({0}) must be >0. Using 3 instead.", _values[WorldCfg.BattlegroundReportAfk]);
                _values[WorldCfg.BattlegroundReportAfk] = 3;
            }
            if ((int)_values[WorldCfg.BattlegroundReportAfk] > 9)
            {
                Log.outError(LogFilter.ServerLoading, "Battleground.ReportAFK ({0}) must be <10. Using 3 instead.", _values[WorldCfg.BattlegroundReportAfk]);
                _values[WorldCfg.BattlegroundReportAfk] = 3;
            }
            _values[WorldCfg.BattlegroundInvitationType] = GetDefaultValue("Battleground.InvitationType", 0);
            _values[WorldCfg.BattlegroundPrematureFinishTimer] = GetDefaultValue("Battleground.PrematureFinishTimer", 5 * Time.Minute * Time.InMilliseconds);
            _values[WorldCfg.BattlegroundPremadeGroupWaitForMatch] = GetDefaultValue("Battleground.PremadeGroupWaitForMatch", 30 * Time.Minute * Time.InMilliseconds);
            _values[WorldCfg.BgXpForKill] = GetDefaultValue("Battleground.GiveXPForKills", false);
            _values[WorldCfg.ArenaMaxRatingDifference] = GetDefaultValue("Arena.MaxRatingDifference", 150);
            _values[WorldCfg.ArenaRatingDiscardTimer] = GetDefaultValue("Arena.RatingDiscardTimer", 10 * Time.Minute * Time.InMilliseconds);
            _values[WorldCfg.ArenaRatedUpdateTimer] = GetDefaultValue("Arena.RatedUpdateTimer", 5 * Time.InMilliseconds);
            _values[WorldCfg.ArenaQueueAnnouncerEnable] = GetDefaultValue("Arena.QueueAnnouncer.Enable", false);
            _values[WorldCfg.ArenaSeasonId] = GetDefaultValue("Arena.ArenaSeason.ID", 32);
            _values[WorldCfg.ArenaStartRating] = GetDefaultValue("Arena.ArenaStartRating", 0);
            _values[WorldCfg.ArenaStartPersonalRating] = GetDefaultValue("Arena.ArenaStartPersonalRating", 1000);
            _values[WorldCfg.ArenaStartMatchmakerRating] = GetDefaultValue("Arena.ArenaStartMatchmakerRating", 1500);
            _values[WorldCfg.ArenaSeasonInProgress] = GetDefaultValue("Arena.ArenaSeason.InProgress", false);
            _values[WorldCfg.ArenaLogExtendedInfo] = GetDefaultValue("ArenaLog.ExtendedInfo", false);
            _values[WorldCfg.ArenaWinRatingModifier1] = GetDefaultValue("Arena.ArenaWinRatingModifier1", 48.0f);
            _values[WorldCfg.ArenaWinRatingModifier2] = GetDefaultValue("Arena.ArenaWinRatingModifier2", 24.0f);
            _values[WorldCfg.ArenaLoseRatingModifier] = GetDefaultValue("Arena.ArenaLoseRatingModifier", 24.0f);
            _values[WorldCfg.ArenaMatchmakerRatingModifier] = GetDefaultValue("Arena.ArenaMatchmakerRatingModifier", 24.0f);

            if (reload)
            {
                Global.WorldStateMgr.SetValue(WorldStates.CurrentPvpSeasonId, GetBoolValue(WorldCfg.ArenaSeasonInProgress) ? GetIntValue(WorldCfg.ArenaSeasonId) : 0, false, null);
                Global.WorldStateMgr.SetValue(WorldStates.PreviousPvpSeasonId, GetIntValue(WorldCfg.ArenaSeasonId) - (GetBoolValue(WorldCfg.ArenaSeasonInProgress) ? 1 : 0), false, null);
            }

            _values[WorldCfg.OffhandCheckAtSpellUnlearn] = GetDefaultValue("OffhandCheckAtSpellUnlearn", true);

            _values[WorldCfg.CreaturePickpocketRefill] = GetDefaultValue("Creature.PickPocketRefillDelay", 10 * Time.Minute);
            _values[WorldCfg.CreatureStopForPlayer] = GetDefaultValue("Creature.MovingStopTimeForPlayer", 3 * Time.Minute * Time.InMilliseconds);

            int clientCacheId = GetDefaultValue("ClientCacheVersion", 0);
            if (clientCacheId != 0)
            {
                // overwrite DB/old value
                if (clientCacheId > 0)
                    _values[WorldCfg.ClientCacheVersion] = clientCacheId;
                else
                    Log.outError(LogFilter.ServerLoading, "ClientCacheVersion can't be negative {0}, ignored.", clientCacheId);
            }
            Log.outInfo(LogFilter.ServerLoading, "Client cache version set to: {0}", clientCacheId);

            _values[WorldCfg.GuildNewsLogCount] = GetDefaultValue("Guild.NewsLogRecordsCount", GuildConst.NewsLogMaxRecords);
            if ((int)_values[WorldCfg.GuildNewsLogCount] > GuildConst.NewsLogMaxRecords)
                _values[WorldCfg.GuildNewsLogCount] = GuildConst.NewsLogMaxRecords;

            _values[WorldCfg.GuildEventLogCount] = GetDefaultValue("Guild.EventLogRecordsCount", GuildConst.EventLogMaxRecords);
            if ((int)_values[WorldCfg.GuildEventLogCount] > GuildConst.EventLogMaxRecords)
                _values[WorldCfg.GuildEventLogCount] = GuildConst.EventLogMaxRecords;

            _values[WorldCfg.GuildBankEventLogCount] = GetDefaultValue("Guild.BankEventLogRecordsCount", GuildConst.BankLogMaxRecords);
            if ((int)_values[WorldCfg.GuildBankEventLogCount] > GuildConst.BankLogMaxRecords)
                _values[WorldCfg.GuildBankEventLogCount] = GuildConst.BankLogMaxRecords;

            // Load the CharDelete related config options
            _values[WorldCfg.ChardeleteMethod] = GetDefaultValue("CharDelete.Method", 0);
            _values[WorldCfg.ChardeleteMinLevel] = GetDefaultValue("CharDelete.MinLevel", 0);
            _values[WorldCfg.ChardeleteDeathKnightMinLevel] = GetDefaultValue("CharDelete.DeathKnight.MinLevel", 0);
            _values[WorldCfg.ChardeleteDemonHunterMinLevel] = GetDefaultValue("CharDelete.DemonHunter.MinLevel", 0);
            _values[WorldCfg.ChardeleteKeepDays] = GetDefaultValue("CharDelete.KeepDays", 30);

            // No aggro from gray mobs
            _values[WorldCfg.NoGrayAggroAbove] = GetDefaultValue("NoGrayAggro.Above", 0);
            _values[WorldCfg.NoGrayAggroBelow] = GetDefaultValue("NoGrayAggro.Below", 0);
            if ((int)_values[WorldCfg.NoGrayAggroAbove] > (int)_values[WorldCfg.MaxPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, "NoGrayAggro.Above ({0}) must be in range 0..{1}. Set to {1}.", _values[WorldCfg.NoGrayAggroAbove], _values[WorldCfg.MaxPlayerLevel]);
                _values[WorldCfg.NoGrayAggroAbove] = _values[WorldCfg.MaxPlayerLevel];
            }
            if ((int)_values[WorldCfg.NoGrayAggroBelow] > (int)_values[WorldCfg.MaxPlayerLevel])
            {
                Log.outError(LogFilter.ServerLoading, "NoGrayAggro.Below ({0}) must be in range 0..{1}. Set to {1}.", _values[WorldCfg.NoGrayAggroBelow], _values[WorldCfg.MaxPlayerLevel]);
                _values[WorldCfg.NoGrayAggroBelow] = _values[WorldCfg.MaxPlayerLevel];
            }
            if ((int)_values[WorldCfg.NoGrayAggroAbove] > 0 && (int)_values[WorldCfg.NoGrayAggroAbove] < (int)_values[WorldCfg.NoGrayAggroBelow])
            {
                Log.outError(LogFilter.ServerLoading, "NoGrayAggro.Below ({0}) cannot be greater than NoGrayAggro.Above ({1}). Set to {1}.", _values[WorldCfg.NoGrayAggroBelow], _values[WorldCfg.NoGrayAggroAbove]);
                _values[WorldCfg.NoGrayAggroBelow] = _values[WorldCfg.NoGrayAggroAbove];
            }

            // Respawn Settings
            _values[WorldCfg.RespawnMinCheckIntervalMs] = GetDefaultValue("Respawn.MinCheckIntervalMS", 5000);
            _values[WorldCfg.RespawnDynamicMode] = GetDefaultValue("Respawn.DynamicMode", 0);
            if ((int)_values[WorldCfg.RespawnDynamicMode] > 1)
            {
                Log.outError(LogFilter.ServerLoading, $"Invalid value for Respawn.DynamicMode ({_values[WorldCfg.RespawnDynamicMode]}). Set to 0.");
                _values[WorldCfg.RespawnDynamicMode] = 0;
            }
            _values[WorldCfg.RespawnDynamicEscortNpc] = GetDefaultValue("Respawn.DynamicEscortNPC", false);
            _values[WorldCfg.RespawnGuidWarnLevel] = GetDefaultValue("Respawn.GuidWarnLevel", 12000000);
            if ((int)_values[WorldCfg.RespawnGuidWarnLevel] > 16777215)
            {
                Log.outError(LogFilter.ServerLoading, $"Respawn.GuidWarnLevel ({_values[WorldCfg.RespawnGuidWarnLevel]}) cannot be greater than maximum GUID (16777215). Set to 12000000.");
                _values[WorldCfg.RespawnGuidWarnLevel] = 12000000;
            }
            _values[WorldCfg.RespawnGuidAlertLevel] = GetDefaultValue("Respawn.GuidAlertLevel", 16000000);
            if ((int)_values[WorldCfg.RespawnGuidAlertLevel] > 16777215)
            {
                Log.outError(LogFilter.ServerLoading, $"Respawn.GuidWarnLevel ({_values[WorldCfg.RespawnGuidAlertLevel]}) cannot be greater than maximum GUID (16777215). Set to 16000000.");
                _values[WorldCfg.RespawnGuidAlertLevel] = 16000000;
            }
            _values[WorldCfg.RespawnRestartQuietTime] = GetDefaultValue("Respawn.RestartQuietTime", 3);
            if ((int)_values[WorldCfg.RespawnRestartQuietTime] > 23)
            {
                Log.outError(LogFilter.ServerLoading, $"Respawn.RestartQuietTime ({_values[WorldCfg.RespawnRestartQuietTime]}) must be an hour, between 0 and 23. Set to 3.");
                _values[WorldCfg.RespawnRestartQuietTime] = 3;
            }
            _values[WorldCfg.RespawnDynamicRateCreature] = GetDefaultValue("Respawn.DynamicRateCreature", 10.0f);
            if ((float)_values[WorldCfg.RespawnDynamicRateCreature] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, $"Respawn.DynamicRateCreature ({_values[WorldCfg.RespawnDynamicRateCreature]}) must be positive. Set to 10.");
                _values[WorldCfg.RespawnDynamicRateCreature] = 10.0f;
            }
            _values[WorldCfg.RespawnDynamicMinimumCreature] = GetDefaultValue("Respawn.DynamicMinimumCreature", 10);
            _values[WorldCfg.RespawnDynamicRateGameobject] = GetDefaultValue("Respawn.DynamicRateGameObject", 10.0f);
            if ((float)_values[WorldCfg.RespawnDynamicRateGameobject] < 0.0f)
            {
                Log.outError(LogFilter.ServerLoading, $"Respawn.DynamicRateGameObject ({_values[WorldCfg.RespawnDynamicRateGameobject]}) must be positive. Set to 10.");
                _values[WorldCfg.RespawnDynamicRateGameobject] = 10.0f;
            }
            _values[WorldCfg.RespawnDynamicMinimumGameObject] = GetDefaultValue("Respawn.DynamicMinimumGameObject", 10);
            _values[WorldCfg.RespawnGuidWarningFrequency] = GetDefaultValue("Respawn.WarningFrequency", 1800);

            _values[WorldCfg.EnableMmaps] = GetDefaultValue("mmap.EnablePathFinding", true);
            _values[WorldCfg.VmapIndoorCheck] = GetDefaultValue("vmap.EnableIndoorCheck", false);

            _values[WorldCfg.MaxWho] = GetDefaultValue("MaxWhoListReturns", 49);
            _values[WorldCfg.StartAllSpells] = GetDefaultValue("PlayerStart.AllSpells", false);
            if ((bool)_values[WorldCfg.StartAllSpells])
                Log.outWarn(LogFilter.ServerLoading, "PlayerStart.AllSpells Enabled - may not function as intended!");

            _values[WorldCfg.HonorAfterDuel] = GetDefaultValue("HonorPointsAfterDuel", 0);
            _values[WorldCfg.ResetDuelCooldowns] = GetDefaultValue("ResetDuelCooldowns", false);
            _values[WorldCfg.ResetDuelHealthMana] = GetDefaultValue("ResetDuelHealthMana", false);
            _values[WorldCfg.StartAllExplored] = GetDefaultValue("PlayerStart.MapsExplored", false);
            _values[WorldCfg.StartAllRep] = GetDefaultValue("PlayerStart.AllReputation", false);
            _values[WorldCfg.PvpTokenEnable] = GetDefaultValue("PvPToken.Enable", false);
            _values[WorldCfg.PvpTokenMapType] = GetDefaultValue("PvPToken.MapAllowType", 4);
            _values[WorldCfg.PvpTokenId] = GetDefaultValue("PvPToken.ItemID", 29434);
            _values[WorldCfg.PvpTokenCount] = GetDefaultValue("PvPToken.ItemCount", 1);
            if ((int)_values[WorldCfg.PvpTokenCount] < 1)
                _values[WorldCfg.PvpTokenCount] = 1;

            _values[WorldCfg.NoResetTalentCost] = GetDefaultValue("NoResetTalentsCost", false);
            _values[WorldCfg.ShowKickInWorld] = GetDefaultValue("ShowKickInWorld", false);
            _values[WorldCfg.ShowMuteInWorld] = GetDefaultValue("ShowMuteInWorld", false);
            _values[WorldCfg.ShowBanInWorld] = GetDefaultValue("ShowBanInWorld", false);
            _values[WorldCfg.Numthreads] = GetDefaultValue("MapUpdate.Threads", 1);
            _values[WorldCfg.MaxResultsLookupCommands] = GetDefaultValue("Command.LookupMaxResults", 0);

            // Warden
            _values[WorldCfg.WardenEnabled] = GetDefaultValue("Warden.Enabled", false);
            _values[WorldCfg.WardenNumInjectChecks] = GetDefaultValue("Warden.NumInjectionChecks", 9);
            _values[WorldCfg.WardenNumLuaChecks] = GetDefaultValue("Warden.NumLuaSandboxChecks", 1);
            _values[WorldCfg.WardenNumClientModChecks] = GetDefaultValue("Warden.NumClientModChecks", 1);
            _values[WorldCfg.WardenClientBanDuration] = GetDefaultValue("Warden.BanDuration", 86400);
            _values[WorldCfg.WardenClientCheckHoldoff] = GetDefaultValue("Warden.ClientCheckHoldOff", 30);
            _values[WorldCfg.WardenClientFailAction] = GetDefaultValue("Warden.ClientCheckFailAction", 0);
            _values[WorldCfg.WardenClientResponseDelay] = GetDefaultValue("Warden.ClientResponseDelay", 600);

            // Feature System
            _values[WorldCfg.FeatureSystemBpayStoreEnabled] = GetDefaultValue("FeatureSystem.BpayStore.Enabled", false);
            _values[WorldCfg.FeatureSystemCharacterUndeleteEnabled] = GetDefaultValue("FeatureSystem.CharacterUndelete.Enabled", false);
            _values[WorldCfg.FeatureSystemCharacterUndeleteCooldown] = GetDefaultValue("FeatureSystem.CharacterUndelete.Cooldown", 2592000);
            _values[WorldCfg.FeatureSystemWarModeEnabled] = GetDefaultValue("FeatureSystem.WarMode.Enabled", false);

            // Dungeon finder
            _values[WorldCfg.LfgOptionsmask] = GetDefaultValue("DungeonFinder.OptionsMask", 1);

            // DBC_ItemAttributes
            _values[WorldCfg.DbcEnforceItemAttributes] = GetDefaultValue("DBC.EnforceItemAttributes", true);

            // Accountpassword Secruity
            _values[WorldCfg.AccPasschangesec] = GetDefaultValue("Account.PasswordChangeSecurity", 0);

            // Random Battleground Rewards
            _values[WorldCfg.BgRewardWinnerHonorFirst] = GetDefaultValue("Battleground.RewardWinnerHonorFirst", 27000);
            _values[WorldCfg.BgRewardWinnerConquestFirst] = GetDefaultValue("Battleground.RewardWinnerConquestFirst", 10000);
            _values[WorldCfg.BgRewardWinnerHonorLast] = GetDefaultValue("Battleground.RewardWinnerHonorLast", 13500);
            _values[WorldCfg.BgRewardWinnerConquestLast] = GetDefaultValue("Battleground.RewardWinnerConquestLast", 5000);
            _values[WorldCfg.BgRewardLoserHonorFirst] = GetDefaultValue("Battleground.RewardLoserHonorFirst", 4500);
            _values[WorldCfg.BgRewardLoserHonorLast] = GetDefaultValue("Battleground.RewardLoserHonorLast", 3500);

            // Max instances per hour
            _values[WorldCfg.MaxInstancesPerHour] = GetDefaultValue("AccountInstancesPerHour", 5);

            // Anounce reset of instance to whole party
            _values[WorldCfg.InstancesResetAnnounce] = GetDefaultValue("InstancesResetAnnounce", false);

            // Autobroadcast
            //AutoBroadcast.On
            _values[WorldCfg.AutoBroadcast] = GetDefaultValue("AutoBroadcast.On", false);
            _values[WorldCfg.AutoBroadcastCenter] = GetDefaultValue("AutoBroadcast.Center", 0);
            _values[WorldCfg.AutoBroadcastInterval] = GetDefaultValue("AutoBroadcast.Timer", 60000);

            // Guild save interval
            _values[WorldCfg.GuildSaveInterval] = GetDefaultValue("Guild.SaveInterval", 15);

            // misc
            _values[WorldCfg.PdumpNoPaths] = GetDefaultValue("PlayerDump.DisallowPaths", true);
            _values[WorldCfg.PdumpNoOverwrite] = GetDefaultValue("PlayerDump.DisallowOverwrite", true);

            // Wintergrasp battlefield
            _values[WorldCfg.WintergraspEnable] = GetDefaultValue("Wintergrasp.Enable", false);
            _values[WorldCfg.WintergraspPlrMax] = GetDefaultValue("Wintergrasp.PlayerMax", 100);
            _values[WorldCfg.WintergraspPlrMin] = GetDefaultValue("Wintergrasp.PlayerMin", 0);
            _values[WorldCfg.WintergraspPlrMinLvl] = GetDefaultValue("Wintergrasp.PlayerMinLvl", 77);
            _values[WorldCfg.WintergraspBattletime] = GetDefaultValue("Wintergrasp.BattleTimer", 30);
            _values[WorldCfg.WintergraspNobattletime] = GetDefaultValue("Wintergrasp.NoBattleTimer", 150);
            _values[WorldCfg.WintergraspRestartAfterCrash] = GetDefaultValue("Wintergrasp.CrashRestartTimer", 10);

            // Tol Barad battlefield
            _values[WorldCfg.TolbaradEnable] = GetDefaultValue("TolBarad.Enable", true);
            _values[WorldCfg.TolbaradPlrMax] = GetDefaultValue("TolBarad.PlayerMax", 100);
            _values[WorldCfg.TolbaradPlrMin] = GetDefaultValue("TolBarad.PlayerMin", 0);
            _values[WorldCfg.TolbaradPlrMinLvl] = GetDefaultValue("TolBarad.PlayerMinLvl", 85);
            _values[WorldCfg.TolbaradBattleTime] = GetDefaultValue("TolBarad.BattleTimer", 15);
            _values[WorldCfg.TolbaradBonusTime] = GetDefaultValue("TolBarad.BonusTime", 5);
            _values[WorldCfg.TolbaradNoBattleTime] = GetDefaultValue("TolBarad.NoBattleTimer", 150);
            _values[WorldCfg.TolbaradRestartAfterCrash] = GetDefaultValue("TolBarad.CrashRestartTimer", 10);

            // Stats limits
            _values[WorldCfg.StatsLimitsEnable] = GetDefaultValue("Stats.Limits.Enable", false);
            _values[WorldCfg.StatsLimitsDodge] = GetDefaultValue("Stats.Limits.Dodge", 95.0f);
            _values[WorldCfg.StatsLimitsParry] = GetDefaultValue("Stats.Limits.Parry", 95.0f);
            _values[WorldCfg.StatsLimitsBlock] = GetDefaultValue("Stats.Limits.Block", 95.0f);
            _values[WorldCfg.StatsLimitsCrit] = GetDefaultValue("Stats.Limits.Crit", 95.0f);

            //packet spoof punishment
            _values[WorldCfg.PacketSpoofPolicy] = GetDefaultValue("PacketSpoof.Policy", 1);//Kick
            _values[WorldCfg.PacketSpoofBanmode] = GetDefaultValue("PacketSpoof.BanMode", (int)BanMode.Account);
            if ((int)_values[WorldCfg.PacketSpoofBanmode] == 1 || (int)_values[WorldCfg.PacketSpoofBanmode] > 2)
                _values[WorldCfg.PacketSpoofBanmode] = (int)BanMode.Account;

            _values[WorldCfg.PacketSpoofBanduration] = GetDefaultValue("PacketSpoof.BanDuration", 86400);

            _values[WorldCfg.IpBasedActionLogging] = GetDefaultValue("Allow.IP.Based.Action.Logging", false);

            // AHBot
            _values[WorldCfg.AhbotUpdateInterval] = GetDefaultValue("AuctionHouseBot.Update.Interval", 20);

            _values[WorldCfg.CalculateCreatureZoneAreaData] = GetDefaultValue("Calculate.Creature.Zone.Area.Data", false);
            _values[WorldCfg.CalculateGameobjectZoneAreaData] = GetDefaultValue("Calculate.Gameoject.Zone.Area.Data", false);

            // Black Market
            _values[WorldCfg.BlackmarketEnabled] = GetDefaultValue("BlackMarket.Enabled", true);

            _values[WorldCfg.BlackmarketMaxAuctions] = GetDefaultValue("BlackMarket.MaxAuctions", 12);
            _values[WorldCfg.BlackmarketUpdatePeriod] = GetDefaultValue("BlackMarket.UpdatePeriod", 24);

            // prevent character rename on character customization
            _values[WorldCfg.PreventRenameCustomization] = GetDefaultValue("PreventRenameCharacterOnCustomization", false);

            // Allow 5-man parties to use raid warnings
            _values[WorldCfg.ChatPartyRaidWarnings] = GetDefaultValue("PartyRaidWarnings", false);

            // Allow to cache _data queries
            _values[WorldCfg.CacheDataQueries] = GetDefaultValue("CacheDataQueries", true);

            // Check Invalid Position
            _values[WorldCfg.CreatureCheckInvalidPostion] = GetDefaultValue("Creature.CheckInvalidPosition", false);
            _values[WorldCfg.GameobjectCheckInvalidPostion] = GetDefaultValue("GameObject.CheckInvalidPosition", false);

            // Whether to use LoS from game objects
            _values[WorldCfg.CheckGobjectLos] = GetDefaultValue("CheckGameObjectLoS", true);

            // FactionBalance
            _values[WorldCfg.FactionBalanceLevelCheckDiff] = GetDefaultValue("Pvp.FactionBalance.LevelCheckDiff", 0);
            _values[WorldCfg.CallToArms5Pct] = GetDefaultValue("Pvp.FactionBalance.Pct5", 0.6f);
            _values[WorldCfg.CallToArms10Pct] = GetDefaultValue("Pvp.FactionBalance.Pct10", 0.7f);
            _values[WorldCfg.CallToArms20Pct] = GetDefaultValue("Pvp.FactionBalance.Pct20", 0.8f);

            // Specifies if IP addresses can be logged to the database
            _values[WorldCfg.AllowLogginIpAddressesInDatabase] = GetDefaultValue("AllowLoggingIPAddressesInDatabase", true);

            // call ScriptMgr if we're reloading the configuration
            if (reload)
                Global.ScriptMgr.ForEach<IWorldOnConfigLoad>(p => p.OnConfigLoad(reload));
        }

        public static uint GetUIntValue(WorldCfg confi)
        {
            return Convert.ToUInt32(_values.LookupByKey(confi));
        }

        public static int GetIntValue(WorldCfg confi)
        {
            return Convert.ToInt32(_values.LookupByKey(confi));
        }

        public static ulong GetUInt64Value(WorldCfg confi)
        {
            return Convert.ToUInt64(_values.LookupByKey(confi));
        }

        public static bool GetBoolValue(WorldCfg confi)
        {
            return Convert.ToBoolean(_values.LookupByKey(confi));
        }

        public static float GetFloatValue(WorldCfg confi)
        {
            return Convert.ToSingle(_values.LookupByKey(confi));
        }

        public static void SetValue(WorldCfg confi, object value)
        {
            _values[confi] = value;
        }
    }
}
