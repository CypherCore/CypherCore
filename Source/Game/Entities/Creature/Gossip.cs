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

using Framework.Collections;
using Framework.Constants;
using Framework.GameMath;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Network.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Game.Misc
{
    public class GossipMenu
    {
        public uint AddMenuItem(int optionIndex, GossipOptionIcon icon, string message, uint sender, uint action, string boxMessage, uint boxMoney, bool coded = false)
        {
            Cypher.Assert(_menuItems.Count <= SharedConst.MaxGossipMenuItems);

            // Find a free new id - script case
            if (optionIndex == -1)
            {
                optionIndex = 0;
                if (!_menuItems.Empty())
                {
                    foreach (var item in _menuItems)
                    {
                        if (item.Key > optionIndex)
                            break;

                        optionIndex = (int)item.Key + 1;
                    }
                }
            }

            GossipMenuItem menuItem = new GossipMenuItem();

            menuItem.MenuItemIcon = (byte)icon;
            menuItem.Message = message;
            menuItem.IsCoded = coded;
            menuItem.Sender = sender;
            menuItem.OptionType = action;
            menuItem.BoxMessage = boxMessage;
            menuItem.BoxMoney = boxMoney;

            _menuItems[(uint)optionIndex] = menuItem;
            return (uint)optionIndex;
        }

        /// <summary>
        /// Adds a localized gossip menu item from db by menu id and menu item id.
        /// </summary>
        /// <param name="menuId">menuId Gossip menu id.</param>
        /// <param name="optionIndex">menuItemId Gossip menu item id.</param>
        /// <param name="sender">sender Identifier of the current menu.</param>
        /// <param name="action">action Custom action given to OnGossipHello.</param>
        public void AddMenuItem(uint menuId, uint optionIndex, uint sender, uint action)
        {
            // Find items for given menu id.
            var bounds = Global.ObjectMgr.GetGossipMenuItemsMapBounds(menuId);
            // Return if there are none.
            if (bounds.Empty())
                return;

            // Iterate over each of them.
            foreach (var item in bounds)
            {
                // Find the one with the given menu item id.
                if (item.OptionIndex != optionIndex)
                    continue;

                // Store texts for localization.
                string strOptionText = "", strBoxText = "";
                BroadcastTextRecord optionBroadcastText = CliDB.BroadcastTextStorage.LookupByKey(item.OptionBroadcastTextId);
                BroadcastTextRecord boxBroadcastText = CliDB.BroadcastTextStorage.LookupByKey(item.BoxBroadcastTextId);

                // OptionText
                if (optionBroadcastText != null)
                    strOptionText = Global.DB2Mgr.GetBroadcastTextValue(optionBroadcastText, GetLocale());
                else
                    strOptionText = item.OptionText;

                // BoxText
                if (boxBroadcastText != null)
                    strBoxText = Global.DB2Mgr.GetBroadcastTextValue(boxBroadcastText, GetLocale());
                else
                    strBoxText = item.BoxText;

                // Check need of localization.
                if (GetLocale() != LocaleConstant.enUS)
                {
                    if (optionBroadcastText == null)
                    {
                        // Find localizations from database.
                        GossipMenuItemsLocale gossipMenuLocale = Global.ObjectMgr.GetGossipMenuItemsLocale(menuId, optionIndex);
                        if (gossipMenuLocale != null)
                            ObjectManager.GetLocaleString(gossipMenuLocale.OptionText, GetLocale(), ref strOptionText);
                    }

                    if (boxBroadcastText == null)
                    {
                        // Find localizations from database.
                        GossipMenuItemsLocale gossipMenuLocale = Global.ObjectMgr.GetGossipMenuItemsLocale(menuId, optionIndex);
                        if (gossipMenuLocale != null)
                            ObjectManager.GetLocaleString(gossipMenuLocale.BoxText, GetLocale(), ref strBoxText);
                    }

                }

                // Add menu item with existing method. Menu item id -1 is also used in ADD_GOSSIP_ITEM macro.
                uint newOptionIndex = AddMenuItem(-1, item.OptionIcon, strOptionText, sender, action, strBoxText, item.BoxMoney, item.BoxCoded);
                AddGossipMenuItemData(newOptionIndex, item.ActionMenuId, item.ActionPoiId, item.TrainerId);
            }
        }

        public void AddGossipMenuItemData(uint optionIndex, uint gossipActionMenuId, uint gossipActionPoi, uint trainerId)
        {
            GossipMenuItemData itemData = new GossipMenuItemData();

            itemData.GossipActionMenuId = gossipActionMenuId;
            itemData.GossipActionPoi = gossipActionPoi;
            itemData.TrainerId = trainerId;

            _menuItemData[optionIndex] = itemData;
        }

        public uint GetMenuItemSender(uint menuItemId)
        {
            if (_menuItems.ContainsKey(menuItemId))
                return _menuItems.LookupByKey(menuItemId).Sender;
            else
                return 0;
        }

        public uint GetMenuItemAction(uint menuItemId)
        {
            if (_menuItems.ContainsKey(menuItemId))
                return _menuItems.LookupByKey(menuItemId).OptionType;
            else
                return 0;
        }

        public bool IsMenuItemCoded(uint menuItemId)
        {
            if (_menuItems.ContainsKey(menuItemId))
                return _menuItems.LookupByKey(menuItemId).IsCoded;
            else
                return false;
        }

        public bool HasMenuItemType(uint optionType)
        {
            foreach (var menuItemPair in _menuItems)
                if (menuItemPair.Value.OptionType == optionType)
                    return true;

            return false;
        }

        public void ClearMenu()
        {
            _menuItems.Clear();
            _menuItemData.Clear();
        }

        public void SetMenuId(uint menu_id) { _menuId = menu_id; }
        public uint GetMenuId() { return _menuId; }
        public void SetLocale(LocaleConstant locale) { _locale = locale; }
        LocaleConstant GetLocale() { return _locale; }

        public int GetMenuItemCount()
        {
            return _menuItems.Count;
        }

        public bool IsEmpty()
        {
            return _menuItems.Empty();
        }

        public GossipMenuItem GetItem(uint id)
        {
            return _menuItems.LookupByKey(id);
        }

        public GossipMenuItemData GetItemData(uint indexId)
        {
            return _menuItemData.LookupByKey(indexId);
        }

        public Dictionary<uint, GossipMenuItem> GetMenuItems()
        {
            return _menuItems;
        }

        Dictionary<uint, GossipMenuItem> _menuItems = new Dictionary<uint, GossipMenuItem>();
        Dictionary<uint, GossipMenuItemData> _menuItemData = new Dictionary<uint, GossipMenuItemData>();
        uint _menuId;
        LocaleConstant _locale;
    }

    public class InteractionData
    {
        public void Reset()
        {
            SourceGuid.Clear();
            TrainerId = 0;
        }

        public ObjectGuid SourceGuid;
        public uint TrainerId;
        public uint PlayerChoiceId;
    }

    public class PlayerMenu
    {
        public PlayerMenu(WorldSession session)
        {
            _session = session;
            if (_session != null)
                _gossipMenu.SetLocale(_session.GetSessionDbLocaleIndex());
        }

        public void ClearMenus()
        {
            _gossipMenu.ClearMenu();
            _questMenu.ClearMenu();
        }

        public void SendGossipMenu(uint titleTextId, ObjectGuid objectGUID)
        {
            _interactionData.Reset();
            _interactionData.SourceGuid = objectGUID;

            GossipMessagePkt packet = new GossipMessagePkt();
            packet.GossipGUID = objectGUID;
            packet.GossipID = (int)_gossipMenu.GetMenuId();
            packet.TextID = (int)titleTextId;

            foreach (var pair in _gossipMenu.GetMenuItems())
            {
                ClientGossipOptions opt = new ClientGossipOptions();
                GossipMenuItem item = pair.Value;
                opt.ClientOption = (int)pair.Key;
                opt.OptionNPC = item.MenuItemIcon;
                opt.OptionFlags = (byte)(item.IsCoded ? 1 : 0);     // makes pop up box password
                opt.OptionCost = (int)item.BoxMoney;     // money required to open menu, 2.0.3
                opt.Text = item.Message;            // text for gossip item
                opt.Confirm = item.BoxMessage;      // accept text (related to money) pop up box, 2.0.3
                packet.GossipOptions.Add(opt);

            }

            for (byte i = 0; i < _questMenu.GetMenuItemCount(); ++i)
            {
                QuestMenuItem item = _questMenu.GetItem(i);
                uint questID = item.QuestId;
                Quest quest = Global.ObjectMgr.GetQuestTemplate(questID);
                if (quest != null)
                {
                    ClientGossipText text = new ClientGossipText();
                    text.QuestID = (int)questID;
                    text.QuestType = item.QuestIcon;
                    text.QuestLevel = quest.Level;
                    text.QuestMaxScalingLevel = quest.MaxScalingLevel;
                    text.QuestFlags = (int)quest.Flags;
                    text.QuestFlagsEx = (int)quest.FlagsEx;
                    text.Repeatable = quest.IsRepeatable();

                    text.QuestTitle = quest.LogTitle;
                    LocaleConstant locale = _session.GetSessionDbLocaleIndex();
                    if (locale != LocaleConstant.enUS)
                    {
                        QuestTemplateLocale localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);
                        if (localeData != null)
                            ObjectManager.GetLocaleString(localeData.LogTitle, locale, ref text.QuestTitle);
                    }

                    packet.GossipText.Add(text);
                }
            }

            _session.SendPacket(packet);
        }

        public void SendCloseGossip()
        {
            _interactionData.Reset();

            _session.SendPacket(new GossipComplete());
        }

        public void SendPointOfInterest(uint id)
        {
            PointOfInterest pointOfInterest = Global.ObjectMgr.GetPointOfInterest(id);
            if (pointOfInterest == null)
            {
                Log.outError(LogFilter.Sql, "Request to send non-existing PointOfInterest (Id: {0}), ignored.", id);
                return;
            }

            GossipPOI packet = new GossipPOI();
            packet.ID = pointOfInterest.ID;
            packet.Name = pointOfInterest.Name;

            LocaleConstant locale = _session.GetSessionDbLocaleIndex();
            if (locale != LocaleConstant.enUS)
            {
                PointOfInterestLocale localeData = Global.ObjectMgr.GetPointOfInterestLocale(id);
                if (localeData != null)
                    ObjectManager.GetLocaleString(localeData.Name, locale, ref packet.Name);
            }

            packet.Flags = pointOfInterest.Flags;
            packet.Pos = pointOfInterest.Pos;
            packet.Icon = pointOfInterest.Icon;
            packet.Importance = pointOfInterest.Importance;

            _session.SendPacket(packet);
        }

        public void SendQuestGiverQuestListMessage(WorldObject questgiver)
        {
            ObjectGuid guid = questgiver.GetGUID();
            LocaleConstant localeConstant = _session.GetSessionDbLocaleIndex();

            QuestGiverQuestListMessage questList = new QuestGiverQuestListMessage();
            questList.QuestGiverGUID = guid;

            QuestGreeting questGreeting = Global.ObjectMgr.GetQuestGreeting(questgiver.GetTypeId(), questgiver.GetEntry());
            if (questGreeting != null)
            {
                questList.GreetEmoteDelay = questGreeting.EmoteDelay;
                questList.GreetEmoteType = questGreeting.EmoteType;
                questList.Greeting = questGreeting.Text;

                if (localeConstant != LocaleConstant.enUS)
                {
                    QuestGreetingLocale questGreetingLocale = Global.ObjectMgr.GetQuestGreetingLocale(questgiver.GetTypeId(), questgiver.GetEntry());
                    if (questGreetingLocale != null)
                        ObjectManager.GetLocaleString(questGreetingLocale.Greeting, localeConstant, ref questList.Greeting);
                }
            }

            for (var i = 0; i < _questMenu.GetMenuItemCount(); ++i)
            {
                QuestMenuItem questMenuItem = _questMenu.GetItem(i);

                uint questID = questMenuItem.QuestId;
                Quest quest = Global.ObjectMgr.GetQuestTemplate(questID);
                if (quest != null)
                {
                    string title = quest.LogTitle;

                    if (localeConstant != LocaleConstant.enUS)
                    {
                        QuestTemplateLocale localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);
                        if (localeData != null)
                            ObjectManager.GetLocaleString(localeData.LogTitle, localeConstant, ref title);
                    }

                    GossipText text = new GossipText();
                    text.QuestID = questID;
                    text.QuestType = questMenuItem.QuestIcon;
                    text.QuestLevel = (uint)quest.Level;
                    text.QuestMaxScalingLevel = (uint)quest.MaxScalingLevel;
                    text.QuestFlags = (uint)quest.Flags;
                    text.QuestFlagsEx = (uint)quest.FlagsEx;
                    text.Repeatable = false; // NYI
                    text.QuestTitle = title;
                    questList.QuestDataText.Add(text);
                }
            }

            _session.SendPacket(questList);
        }

        public void SendQuestGiverStatus(QuestGiverStatus questStatus, ObjectGuid npcGUID)
        {
            var packet = new QuestGiverStatusPkt();
            packet.QuestGiver.Guid = npcGUID;
            packet.QuestGiver.Status = questStatus;

            _session.SendPacket(packet);
        }

        public void SendQuestGiverQuestDetails(Quest quest, ObjectGuid npcGUID, bool autoLaunched, bool displayPopup)
        {
            QuestGiverQuestDetails packet = new QuestGiverQuestDetails();

             packet.QuestTitle = quest.LogTitle;
             packet.LogDescription = quest.LogDescription;
             packet.DescriptionText = quest.QuestDescription;
             packet.PortraitGiverText = quest.PortraitGiverText;
             packet.PortraitGiverName = quest.PortraitGiverName;
             packet.PortraitTurnInText = quest.PortraitTurnInText;
             packet.PortraitTurnInName = quest.PortraitTurnInName;

            LocaleConstant locale = _session.GetSessionDbLocaleIndex();
            if (locale != LocaleConstant.enUS)
            {
                QuestTemplateLocale localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);
                if (localeData != null)
                {
                    ObjectManager.GetLocaleString(localeData.LogTitle, locale, ref packet.QuestTitle);
                    ObjectManager.GetLocaleString(localeData.LogDescription, locale, ref packet.LogDescription);
                    ObjectManager.GetLocaleString(localeData.QuestDescription, locale, ref packet.DescriptionText);
                    ObjectManager.GetLocaleString(localeData.PortraitGiverText, locale, ref packet.PortraitGiverText);
                    ObjectManager.GetLocaleString(localeData.PortraitGiverName, locale, ref packet.PortraitGiverName);
                    ObjectManager.GetLocaleString(localeData.PortraitTurnInText, locale, ref packet.PortraitTurnInText);
                    ObjectManager.GetLocaleString(localeData.PortraitTurnInName, locale, ref packet.PortraitTurnInName);
                }
            }

            packet.QuestGiverGUID = npcGUID;
            packet.InformUnit = _session.GetPlayer().GetDivider();
            packet.QuestID = quest.Id;
            packet.PortraitGiver = quest.QuestGiverPortrait;
            packet.PortraitGiverMount = quest.QuestGiverPortraitMount;
            packet.PortraitTurnIn = quest.QuestTurnInPortrait;
            packet.AutoLaunched = autoLaunched;
            packet.DisplayPopup = displayPopup;
            packet.QuestFlags[0] = (uint)(quest.Flags & (WorldConfig.GetBoolValue(WorldCfg.QuestIgnoreAutoAccept) ? ~QuestFlags.AutoAccept : ~QuestFlags.None));
            packet.QuestFlags[1] = (uint)quest.FlagsEx;
            packet.SuggestedPartyMembers = quest.SuggestedPlayers;

            if (quest.SourceSpellID != 0)
                packet.LearnSpells.Add(quest.SourceSpellID);

            quest.BuildQuestRewards(packet.Rewards, _session.GetPlayer());

            for (int i = 0; i < SharedConst.QuestEmoteCount; ++i)
            {
                var emote = new QuestDescEmote(quest.DetailsEmote[i], quest.DetailsEmoteDelay[i]);
                packet.DescEmotes.Add(emote);
            }

            var objs = quest.Objectives;
            for (int i = 0; i < objs.Count; ++i)
            {
                var obj = new QuestObjectiveSimple();
                obj.ID = objs[i].ID;
                obj.ObjectID = objs[i].ObjectID;
                obj.Amount = objs[i].Amount;
                obj.Type = (byte)objs[i].Type;
                packet.Objectives.Add(obj);
            }

            _session.SendPacket(packet);
        }

        public void SendQuestQueryResponse(Quest quest)
        {
            QueryQuestInfoResponse packet = new QueryQuestInfoResponse();

            packet.Allow = true;
            packet.QuestID = quest.Id;

            packet.Info.LogTitle = quest.LogTitle;
            packet.Info.LogDescription = quest.LogDescription;
            packet.Info.QuestDescription = quest.QuestDescription;
            packet.Info.AreaDescription = quest.AreaDescription;
            packet.Info.QuestCompletionLog = quest.QuestCompletionLog;
            packet.Info.PortraitGiverText = quest.PortraitGiverText;
            packet.Info.PortraitGiverName = quest.PortraitGiverName;
            packet.Info.PortraitTurnInText = quest.PortraitTurnInText;
            packet.Info.PortraitTurnInName = quest.PortraitTurnInName;

            LocaleConstant locale = _session.GetSessionDbLocaleIndex();
            if (locale != LocaleConstant.enUS)
            {
                QuestTemplateLocale questTemplateLocale = Global.ObjectMgr.GetQuestLocale(quest.Id);
                if (questTemplateLocale != null)
                {
                    ObjectManager.GetLocaleString(questTemplateLocale.LogTitle, locale, ref packet.Info.LogTitle);
                    ObjectManager.GetLocaleString(questTemplateLocale.LogDescription, locale, ref packet.Info.LogDescription);
                    ObjectManager.GetLocaleString(questTemplateLocale.QuestDescription, locale, ref packet.Info.QuestDescription);
                    ObjectManager.GetLocaleString(questTemplateLocale.AreaDescription, locale, ref packet.Info.AreaDescription);
                    ObjectManager.GetLocaleString(questTemplateLocale.QuestCompletionLog, locale, ref packet.Info.QuestCompletionLog);
                    ObjectManager.GetLocaleString(questTemplateLocale.PortraitGiverText, locale, ref packet.Info.PortraitGiverText);
                    ObjectManager.GetLocaleString(questTemplateLocale.PortraitGiverName, locale, ref packet.Info.PortraitGiverName);
                    ObjectManager.GetLocaleString(questTemplateLocale.PortraitTurnInText, locale, ref packet.Info.PortraitTurnInText);
                    ObjectManager.GetLocaleString(questTemplateLocale.PortraitTurnInName, locale, ref packet.Info.PortraitTurnInName);
                }
            }

            packet.Info.QuestID = quest.Id;
            packet.Info.QuestType = (int)quest.Type;
            packet.Info.QuestLevel = quest.Level;
            packet.Info.QuestScalingFactionGroup = quest.ScalingFactionGroup;
            packet.Info.QuestMaxScalingLevel = quest.MaxScalingLevel;
            packet.Info.QuestPackageID = quest.PackageID;
            packet.Info.QuestMinLevel = quest.MinLevel;
            packet.Info.QuestSortID = quest.QuestSortID;
            packet.Info.QuestInfoID = quest.QuestInfoID;
            packet.Info.SuggestedGroupNum = quest.SuggestedPlayers;
            packet.Info.RewardNextQuest = quest.NextQuestInChain;
            packet.Info.RewardXPDifficulty = quest.RewardXPDifficulty;
            packet.Info.RewardXPMultiplier = quest.RewardXPMultiplier;

            if (!quest.HasFlag(QuestFlags.HiddenRewards))
                packet.Info.RewardMoney = quest.RewardMoney < 0 ? quest.RewardMoney : (int)_session.GetPlayer().GetQuestMoneyReward(quest);

            packet.Info.RewardMoneyDifficulty = quest.RewardMoneyDifficulty;
            packet.Info.RewardMoneyMultiplier = quest.RewardMoneyMultiplier;
            packet.Info.RewardBonusMoney = quest.RewardBonusMoney;
            for (byte i = 0; i < SharedConst.QuestRewardDisplaySpellCount; ++i)
                packet.Info.RewardDisplaySpell[i] = quest.RewardDisplaySpell[i];

            packet.Info.RewardSpell = quest.RewardSpell;

            packet.Info.RewardHonor = quest.RewardHonor;
            packet.Info.RewardKillHonor = quest.RewardKillHonor;

            packet.Info.RewardArtifactXPDifficulty = (int)quest.RewardArtifactXPDifficulty;
            packet.Info.RewardArtifactXPMultiplier = quest.RewardArtifactXPMultiplier;
            packet.Info.RewardArtifactCategoryID = (int)quest.RewardArtifactCategoryID;

            packet.Info.StartItem = quest.SourceItemId;
            packet.Info.Flags = (uint)quest.Flags;
            packet.Info.FlagsEx = (uint)quest.FlagsEx;
            packet.Info.FlagsEx2 = (uint)quest.FlagsEx2;
            packet.Info.RewardTitle = quest.RewardTitleId;
            packet.Info.RewardArenaPoints = quest.RewardArenaPoints;
            packet.Info.RewardSkillLineID = quest.RewardSkillId;
            packet.Info.RewardNumSkillUps = quest.RewardSkillPoints;
            packet.Info.RewardFactionFlags = quest.RewardReputationMask;
            packet.Info.PortraitGiver = quest.QuestGiverPortrait;
            packet.Info.PortraitGiverMount = quest.QuestGiverPortraitMount;
            packet.Info.PortraitTurnIn = quest.QuestTurnInPortrait;

            for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
            {
                packet.Info.ItemDrop[i] = (int)quest.ItemDrop[i];
                packet.Info.ItemDropQuantity[i] = (int)quest.ItemDropQuantity[i];
            }

            if (!quest.HasFlag(QuestFlags.HiddenRewards))
            {
                for (byte i = 0; i < SharedConst.QuestRewardItemCount; ++i)
                {
                    packet.Info.RewardItems[i] = quest.RewardItemId[i];
                    packet.Info.RewardAmount[i] = quest.RewardItemCount[i];
                }
                for (byte i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                {
                    packet.Info.UnfilteredChoiceItems[i].ItemID = quest.RewardChoiceItemId[i];
                    packet.Info.UnfilteredChoiceItems[i].Quantity = quest.RewardChoiceItemCount[i];
                }
            }

            for (byte i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
            {
                packet.Info.RewardFactionID[i] = quest.RewardFactionId[i];
                packet.Info.RewardFactionValue[i] = quest.RewardFactionValue[i];
                packet.Info.RewardFactionOverride[i] = quest.RewardFactionOverride[i];
                packet.Info.RewardFactionCapIn[i] = (int)quest.RewardFactionCapIn[i];
            }

            packet.Info.POIContinent = quest.POIContinent;
            packet.Info.POIx = quest.POIx;
            packet.Info.POIy = quest.POIy;
            packet.Info.POIPriority = quest.POIPriority;

            packet.Info.AllowableRaces = quest.AllowableRaces;
            packet.Info.TreasurePickerID = (int)quest.TreasurePickerID;
            packet.Info.Expansion = quest.Expansion;

            foreach (QuestObjective questObjective in quest.Objectives)
            {
                if (locale != LocaleConstant.enUS)
                {
                    QuestObjectivesLocale questObjectivesLocaleData = Global.ObjectMgr.GetQuestObjectivesLocale(questObjective.ID);
                    if (questObjectivesLocaleData != null)
                        ObjectManager.GetLocaleString(questObjectivesLocaleData.Description, locale, ref questObjective.Description);
                }

                packet.Info.Objectives.Add(questObjective);
            }

            for (int i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                packet.Info.RewardCurrencyID[i] = quest.RewardCurrencyId[i];
                packet.Info.RewardCurrencyQty[i] = quest.RewardCurrencyCount[i];
            }

            packet.Info.AcceptedSoundKitID = quest.SoundAccept;
            packet.Info.CompleteSoundKitID = quest.SoundTurnIn;
            packet.Info.AreaGroupID = quest.AreaGroupID;
            packet.Info.TimeAllowed = quest.LimitTime;

            _session.SendPacket(packet);
        }

        public void SendQuestGiverOfferReward(Quest quest, ObjectGuid npcGUID, bool autoLaunched)
        {
            QuestGiverOfferRewardMessage packet = new QuestGiverOfferRewardMessage();

            packet.QuestTitle = quest.LogTitle;
            packet.RewardText = quest.OfferRewardText;
            packet.PortraitGiverText = quest.PortraitGiverText;
            packet.PortraitGiverName = quest.PortraitGiverName;
            packet.PortraitTurnInText = quest.PortraitTurnInText;
            packet.PortraitTurnInName = quest.PortraitTurnInName;

            LocaleConstant locale = _session.GetSessionDbLocaleIndex();
            if (locale != LocaleConstant.enUS)
            {
                QuestTemplateLocale localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);
                if (localeData != null)
                {
                    ObjectManager.GetLocaleString(localeData.LogTitle, locale, ref packet.QuestTitle);
                    ObjectManager.GetLocaleString(localeData.PortraitGiverText, locale, ref packet.PortraitGiverText);
                    ObjectManager.GetLocaleString(localeData.PortraitGiverName, locale, ref packet.PortraitGiverName);
                    ObjectManager.GetLocaleString(localeData.PortraitTurnInText, locale, ref packet.PortraitTurnInText);
                    ObjectManager.GetLocaleString(localeData.PortraitTurnInName, locale, ref packet.PortraitTurnInName);
                }

                QuestOfferRewardLocale questOfferRewardLocale = Global.ObjectMgr.GetQuestOfferRewardLocale(quest.Id);
                if (questOfferRewardLocale != null)
                    ObjectManager.GetLocaleString(questOfferRewardLocale.RewardText, locale, ref packet.RewardText);
            }

            QuestGiverOfferReward offer = new QuestGiverOfferReward();

            quest.BuildQuestRewards(offer.Rewards, _session.GetPlayer());
            offer.QuestGiverGUID = npcGUID;

            // Is there a better way? what about game objects?
            Creature creature = ObjectAccessor.GetCreature(_session.GetPlayer(), npcGUID);
            if (creature)
                offer.QuestGiverCreatureID = creature.GetCreatureTemplate().Entry;

            offer.QuestID = quest.Id;
            offer.AutoLaunched = autoLaunched;
            offer.SuggestedPartyMembers = quest.SuggestedPlayers;

            for (uint i = 0; i < SharedConst.QuestEmoteCount && quest.OfferRewardEmote[i] != 0; ++i)
                offer.Emotes.Add(new QuestDescEmote(quest.OfferRewardEmote[i], quest.OfferRewardEmoteDelay[i]));

            offer.QuestFlags[0] = (uint)quest.Flags;
            offer.QuestFlags[1] = (uint)quest.FlagsEx;

            packet.PortraitTurnIn = quest.QuestTurnInPortrait;
            packet.PortraitGiver = quest.QuestGiverPortrait;
            packet.PortraitGiverMount = quest.QuestGiverPortraitMount;
            packet.QuestPackageID = quest.PackageID;

            packet.QuestData = offer;

            _session.SendPacket(packet);
        }

        public void SendQuestGiverRequestItems(Quest quest, ObjectGuid npcGUID, bool canComplete, bool autoLaunched)
        {
            // We can always call to RequestItems, but this packet only goes out if there are actually
            // items.  Otherwise, we'll skip straight to the OfferReward

            if (!quest.HasSpecialFlag(QuestSpecialFlags.Deliver) && canComplete)
            {
                SendQuestGiverOfferReward(quest, npcGUID, true);
                return;
            }

            QuestGiverRequestItems packet = new QuestGiverRequestItems();

            packet.QuestTitle = quest.LogTitle;
            packet.CompletionText = quest.RequestItemsText;

            LocaleConstant locale = _session.GetSessionDbLocaleIndex();
            if (locale != LocaleConstant.enUS)
            {
                QuestTemplateLocale localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);
                if (localeData != null)
                    ObjectManager.GetLocaleString(localeData.LogTitle, locale, ref packet.QuestTitle);

                QuestRequestItemsLocale questRequestItemsLocale = Global.ObjectMgr.GetQuestRequestItemsLocale(quest.Id);
                if (questRequestItemsLocale != null)
                    ObjectManager.GetLocaleString(questRequestItemsLocale.CompletionText, locale, ref packet.CompletionText);
            }

            packet.QuestGiverGUID = npcGUID;

            // Is there a better way? what about game objects?
            Creature creature = ObjectAccessor.GetCreature(_session.GetPlayer(), npcGUID);
            if (creature)
                packet.QuestGiverCreatureID = creature.GetCreatureTemplate().Entry;

            packet.QuestID = quest.Id;

            if (canComplete)
            {
                packet.CompEmoteDelay = quest.EmoteOnCompleteDelay;
                packet.CompEmoteType = quest.EmoteOnComplete;
            }
            else
            {
                packet.CompEmoteDelay = quest.EmoteOnIncompleteDelay;
                packet.CompEmoteType = quest.EmoteOnIncomplete;
            }

            packet.QuestFlags[0] = (uint)quest.Flags;
            packet.QuestFlags[1] = (uint)quest.FlagsEx;
            packet.SuggestPartyMembers = quest.SuggestedPlayers;

            // incomplete: FD
            // incomplete quest with item objective but item objective is complete DD
            packet.StatusFlags = canComplete ? 0xFF : 0xFD;

            packet.MoneyToGet = 0;
            foreach (QuestObjective obj in quest.Objectives)
            {
                switch (obj.Type)
                {
                    case QuestObjectiveType.Item:
                        packet.Collect.Add(new QuestObjectiveCollect((uint)obj.ObjectID, obj.Amount, (uint)obj.Flags));
                        break;
                    case QuestObjectiveType.Currency:
                        packet.Currency.Add(new QuestCurrency((uint)obj.ObjectID, obj.Amount));
                        break;
                    case QuestObjectiveType.Money:
                        packet.MoneyToGet += obj.Amount;
                        break;
                    default:
                        break;
                }
            }

            packet.AutoLaunched = autoLaunched;

            _session.SendPacket(packet);
        }

        public GossipMenu GetGossipMenu() { return _gossipMenu; }
        public QuestMenu GetQuestMenu() { return _questMenu; }
        public InteractionData GetInteractionData() { return _interactionData; }

        bool IsEmpty() { return _gossipMenu.IsEmpty() && _questMenu.IsEmpty(); }

        public uint GetGossipOptionSender(uint selection) { return _gossipMenu.GetMenuItemSender(selection); }
        public uint GetGossipOptionAction(uint selection) { return _gossipMenu.GetMenuItemAction(selection); }
        public bool IsGossipOptionCoded(uint selection) { return _gossipMenu.IsMenuItemCoded(selection); }

        GossipMenu _gossipMenu = new GossipMenu();
        QuestMenu _questMenu = new QuestMenu();
        WorldSession _session;
        InteractionData _interactionData = new InteractionData();
    }

    public class QuestMenu
    {
        public void AddMenuItem(uint QuestId, byte Icon)
        {
            if (Global.ObjectMgr.GetQuestTemplate(QuestId) == null)
                return;

            QuestMenuItem questMenuItem = new QuestMenuItem();

            questMenuItem.QuestId = QuestId;
            questMenuItem.QuestIcon = Icon;

            _questMenuItems.Add(questMenuItem);
        }

        bool HasItem(uint questId)
        {
            foreach (var item in _questMenuItems)
                if (item.QuestId == questId)
                    return true;

            return false;
        }

        public void ClearMenu()
        {
            _questMenuItems.Clear();
        }

        public int GetMenuItemCount()
        {
            return _questMenuItems.Count;
        }

        public bool IsEmpty()
        {
            return _questMenuItems.Empty();
        }

        public QuestMenuItem GetItem(int index)
        {
            return _questMenuItems.LookupByIndex(index);
        }

        List<QuestMenuItem> _questMenuItems = new List<QuestMenuItem>();
    }

    public struct QuestMenuItem
    {
        public uint QuestId;
        public byte QuestIcon;
    }

    public class GossipMenuItem
    {
        public byte MenuItemIcon;
        public bool IsCoded;
        public string Message;
        public uint Sender;
        public uint OptionType;
        public string BoxMessage;
        public uint BoxMoney;
    }

    public class GossipMenuItemData
    {
        public uint GossipActionMenuId;  // MenuId of the gossip triggered by this action
        public uint GossipActionPoi;
        public uint TrainerId;
    }

    public struct NpcTextData
    {
        public float Probability;
        public uint BroadcastTextID;
    }

    public class NpcText
    {
        public NpcTextData[] Data = new NpcTextData[SharedConst.MaxNpcTextOptions];
    }

    public class PageTextLocale
    {
        public StringArray Text = new StringArray((int)LocaleConstant.Total);
    }

    public class GossipMenuItems
    {
        public uint MenuId;
        public uint OptionIndex;
        public GossipOptionIcon OptionIcon;
        public string OptionText;
        public uint OptionBroadcastTextId;
        public GossipOption OptionType;
        public NPCFlags OptionNpcFlag;
        public uint ActionMenuId;
        public uint ActionPoiId;
        public bool BoxCoded;
        public uint BoxMoney;
        public string BoxText;
        public uint BoxBroadcastTextId;
        public uint TrainerId;
        public List<Condition> Conditions = new List<Condition>();
    }

    public class PointOfInterest
    {
        public uint ID;
        public Vector2 Pos;
        public uint Icon;
        public uint Flags;
        public uint Importance;
        public string Name;
    }

    public class PointOfInterestLocale
    {
        public StringArray Name = new StringArray((int)LocaleConstant.Total); 
    }

    public class GossipMenus
    {
        public uint MenuId;
        public uint TextId;
        public List<Condition> Conditions = new List<Condition>();
    }
}
