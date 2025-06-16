// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Networking.Packets;
using Game.Spells;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.Misc
{
    public class GossipMenu
    {
        public uint AddMenuItem(int gossipOptionId, int orderIndex, GossipOptionNpc optionNpc, string optionText, uint language,
            GossipOptionFlags flags, int? gossipNpcOptionId, uint actionMenuId, uint actionPoiId, bool boxCoded, ulong boxMoney,
            string boxText, int? spellId, int? overrideIconId, uint sender, uint action)
        {
            Cypher.Assert(_menuItems.Count <= SharedConst.MaxGossipMenuItems);

            // Find a free new id - script case
            if (orderIndex == -1)
            {
                orderIndex = 0;
                if (_menuId != 0)
                {
                    // set baseline orderIndex as higher than whatever exists in db
                    var bounds = Global.ObjectMgr.GetGossipMenuItemsMapBounds(_menuId);
                    var itr = bounds.MaxBy(a => a.OrderIndex);
                    if (itr != null)
                        orderIndex = (int)(itr.OrderIndex + 1);
                }

                if (!_menuItems.Empty())
                {
                    foreach (var pair in _menuItems)
                    {
                        if (pair.Value.OrderIndex > orderIndex)
                            break;

                        orderIndex = (int)pair.Value.OrderIndex + 1;
                    }
                }
            }

            if (gossipOptionId == 0)
                gossipOptionId = -((int)_menuId * 100 + orderIndex);

            GossipMenuItem menuItem = new();
            menuItem.GossipOptionID = gossipOptionId;
            menuItem.OrderIndex = (uint)orderIndex;
            menuItem.OptionNpc = optionNpc;
            menuItem.OptionText = optionText;
            menuItem.Language = language;
            menuItem.Flags = flags;
            menuItem.GossipNpcOptionID = gossipNpcOptionId;
            menuItem.BoxCoded = boxCoded;
            menuItem.BoxMoney = boxMoney;
            menuItem.BoxText = boxText;
            menuItem.SpellID = spellId;
            menuItem.OverrideIconID = overrideIconId;
            menuItem.ActionMenuID = actionMenuId;
            menuItem.ActionPoiID = actionPoiId;
            menuItem.Sender = sender;
            menuItem.Action = action;

            _menuItems.Add((uint)orderIndex, menuItem);
            return (uint)orderIndex;
        }

        /// <summary>
        /// Adds a localized gossip menu item from db by menu id and menu item id.
        /// </summary>
        /// <param name="menuId">menuId Gossip menu id.</param>
        /// <param name="menuItemId">menuItemId Gossip menu item id.</param>
        /// <param name="sender">sender Identifier of the current menu.</param>
        /// <param name="action">action Custom action given to OnGossipHello.</param>
        public void AddMenuItem(uint menuId, uint menuItemId, uint sender, uint action)
        {
            // Find items for given menu id.
            var bounds = Global.ObjectMgr.GetGossipMenuItemsMapBounds(menuId);
            // Return if there are none.
            if (bounds.Empty())
                return;

            /// Find the one with the given menu item id.
            var gossipMenuItems = bounds.Find(menuItem => menuItem.OrderIndex == menuItemId);
            if (gossipMenuItems == null)
                return;

            AddMenuItem(gossipMenuItems, sender, action);
        }

        public void AddMenuItem(GossipMenuItems menuItem, uint sender, uint action)
        {
            // Store texts for localization.
            string strOptionText, strBoxText;
            BroadcastTextRecord optionBroadcastText = CliDB.BroadcastTextStorage.LookupByKey(menuItem.OptionBroadcastTextId);
            BroadcastTextRecord boxBroadcastText = CliDB.BroadcastTextStorage.LookupByKey(menuItem.BoxBroadcastTextId);

            // OptionText
            if (optionBroadcastText != null)
                strOptionText = Global.DB2Mgr.GetBroadcastTextValue(optionBroadcastText, GetLocale());
            else
            {
                strOptionText = menuItem.OptionText;

                /// Find localizations from database.
                if (GetLocale() != Locale.enUS)
                {
                    GossipMenuItemsLocale gossipMenuLocale = Global.ObjectMgr.GetGossipMenuItemsLocale(menuItem.MenuID, menuItem.OrderIndex);
                    if (gossipMenuLocale != null)
                        ObjectManager.GetLocaleString(gossipMenuLocale.OptionText, GetLocale(), ref strOptionText);
                }
            }

            // BoxText
            if (boxBroadcastText != null)
                strBoxText = Global.DB2Mgr.GetBroadcastTextValue(boxBroadcastText, GetLocale());
            else
            {
                strBoxText = menuItem.BoxText;

                // Find localizations from database.
                if (GetLocale() != Locale.enUS)
                {
                    GossipMenuItemsLocale gossipMenuLocale = Global.ObjectMgr.GetGossipMenuItemsLocale(menuItem.MenuID, menuItem.OrderIndex);
                    if (gossipMenuLocale != null)
                        ObjectManager.GetLocaleString(gossipMenuLocale.BoxText, GetLocale(), ref strOptionText);
                }
            }

            AddMenuItem(menuItem.GossipOptionID, (int)menuItem.OrderIndex, menuItem.OptionNpc, strOptionText, menuItem.Language, menuItem.Flags,
                menuItem.GossipNpcOptionID, menuItem.ActionMenuID, menuItem.ActionPoiID, menuItem.BoxCoded, menuItem.BoxMoney, strBoxText,
                menuItem.SpellID, menuItem.OverrideIconID, sender, action);
        }

        public GossipMenuItem GetItem(int gossipOptionId)
        {
            return _menuItems.Values.FirstOrDefault(item => item.GossipOptionID == gossipOptionId);
        }

        GossipMenuItem GetItemByIndex(uint orderIndex)
        {
            return _menuItems.LookupByKey(orderIndex);
        }

        public uint GetMenuItemSender(uint orderIndex)
        {
            GossipMenuItem item = GetItemByIndex(orderIndex);
            if (item != null)
                return item.Sender;

            return 0;
        }

        public uint GetMenuItemAction(uint orderIndex)
        {
            GossipMenuItem item = GetItemByIndex(orderIndex);
            if (item != null)
                return item.Action;

            return 0;
        }

        public bool IsMenuItemCoded(uint orderIndex)
        {
            GossipMenuItem item = GetItemByIndex(orderIndex);
            if (item != null)
                return item.BoxCoded;

            return false;
        }

        public void ClearMenu()
        {
            _menuItems.Clear();
        }

        public void SetMenuId(uint menu_id) { _menuId = menu_id; }
        public uint GetMenuId() { return _menuId; }
        public void SetLocale(Locale locale) { _locale = locale; }
        Locale GetLocale() { return _locale; }

        public int GetMenuItemCount()
        {
            return _menuItems.Count;
        }

        public bool IsEmpty()
        {
            return _menuItems.Empty();
        }

        public SortedDictionary<uint, GossipMenuItem> GetMenuItems()
        {
            return _menuItems;
        }

        SortedDictionary<uint, GossipMenuItem> _menuItems = new();
        uint _menuId;
        Locale _locale;
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
        public bool IsLaunchedByQuest;
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

            GossipMessagePkt packet = new();
            packet.GossipGUID = objectGUID;
            packet.GossipID = _gossipMenu.GetMenuId();

            GossipMenuAddon addon = Global.ObjectMgr.GetGossipMenuAddon(packet.GossipID);
            if (addon != null)
            {
                packet.FriendshipFactionID = addon.FriendshipFactionID;
                packet.LfgDungeonsID = addon.LfgDungeonsID;
            }
            
            NpcText text = Global.ObjectMgr.GetNpcText(titleTextId);
            if (text != null)
                packet.BroadcastTextID = (int)text.Data.SelectRandomElementByWeight(data => data.Probability).BroadcastTextID;

            foreach (var (index, item) in _gossipMenu.GetMenuItems())
            {
                ClientGossipOptions opt = new();
                opt.GossipOptionID = item.GossipOptionID;
                opt.OptionNPC = item.OptionNpc;
                opt.OptionFlags = (byte)(item.BoxCoded ? 1 : 0);     // makes pop up box password
                opt.OptionCost = item.BoxMoney;     // money required to open menu, 2.0.3
                opt.OptionLanguage = item.Language;
                opt.Flags = item.Flags;
                opt.OrderIndex = (int)item.OrderIndex;
                opt.Text = item.OptionText;            // text for gossip item
                opt.Confirm = item.BoxText;      // accept text (related to money) pop up box, 2.0.3
                opt.Status = GossipOptionStatus.Available;
                opt.SpellID = item.SpellID;
                opt.OverrideIconID = item.OverrideIconID;
                packet.GossipOptions.Add(opt);
            }

            for (byte i = 0; i < _questMenu.GetMenuItemCount(); ++i)
            {
                QuestMenuItem item = _questMenu.GetItem(i);
                uint questID = item.QuestId;
                Quest quest = Global.ObjectMgr.GetQuestTemplate(questID);
                if (quest != null)
                {
                    ClientGossipText gossipText = new();
                    gossipText.QuestID = questID;
                    gossipText.ContentTuningID = quest.ContentTuningId;
                    gossipText.QuestType = item.QuestIcon;
                    gossipText.QuestFlags = (uint)quest.Flags;
                    gossipText.QuestFlagsEx = (uint)quest.FlagsEx;
                    gossipText.QuestFlagsEx2 = (uint)quest.FlagsEx2;
                    gossipText.Repeatable = quest.IsTurnIn() && quest.IsRepeatable() && !quest.IsDailyOrWeekly() && !quest.IsMonthly();
                    gossipText.ResetByScheduler = quest.IsResetByScheduler();
                    gossipText.Important = quest.IsImportant();
                    gossipText.Meta = quest.IsMeta();

                    gossipText.QuestTitle = quest.LogTitle;
                    Locale locale = _session.GetSessionDbLocaleIndex();
                    if (locale != Locale.enUS)
                    {
                        QuestTemplateLocale localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);
                        if (localeData != null)
                            ObjectManager.GetLocaleString(localeData.LogTitle, locale, ref gossipText.QuestTitle);
                    }

                    packet.GossipText.Add(gossipText);
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

            GossipPOI packet = new();
            packet.Id = pointOfInterest.Id;
            packet.Name = pointOfInterest.Name;

            Locale locale = _session.GetSessionDbLocaleIndex();
            if (locale != Locale.enUS)
            {
                PointOfInterestLocale localeData = Global.ObjectMgr.GetPointOfInterestLocale(id);
                if (localeData != null)
                    ObjectManager.GetLocaleString(localeData.Name, locale, ref packet.Name);
            }

            packet.Flags = pointOfInterest.Flags;
            packet.Pos = pointOfInterest.Pos;
            packet.Icon = pointOfInterest.Icon;
            packet.Importance = pointOfInterest.Importance;
            packet.WMOGroupID = pointOfInterest.WMOGroupID;

            _session.SendPacket(packet);
        }

        public void SendQuestGiverQuestListMessage(WorldObject questgiver)
        {
            ObjectGuid guid = questgiver.GetGUID();
            Locale localeConstant = _session.GetSessionDbLocaleIndex();

            QuestGiverQuestListMessage questList = new();
            questList.QuestGiverGUID = guid;

            QuestGreeting questGreeting = Global.ObjectMgr.GetQuestGreeting(questgiver.GetTypeId(), questgiver.GetEntry());
            if (questGreeting != null)
            {
                questList.GreetEmoteDelay = questGreeting.EmoteDelay;
                questList.GreetEmoteType = questGreeting.EmoteType;
                questList.Greeting = questGreeting.Text;

                if (localeConstant != Locale.enUS)
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
                    ClientGossipText text = new();
                    text.QuestID = questID;
                    text.ContentTuningID = quest.ContentTuningId;
                    text.QuestType = questMenuItem.QuestIcon;
                    text.QuestFlags = (uint)quest.Flags;
                    text.QuestFlagsEx = (uint)quest.FlagsEx;
                    text.QuestFlagsEx2 = (uint)quest.FlagsEx2;
                    text.Repeatable = quest.IsTurnIn() && quest.IsRepeatable() && !quest.IsDailyOrWeekly() && !quest.IsMonthly();
                    text.ResetByScheduler = quest.IsResetByScheduler();
                    text.Important = quest.IsImportant();
                    text.Meta = quest.IsMeta();

                    text.QuestTitle = quest.LogTitle;
                    if (localeConstant != Locale.enUS)
                    {
                        QuestTemplateLocale localeData = Global.ObjectMgr.GetQuestLocale(quest.Id);
                        if (localeData != null)
                            ObjectManager.GetLocaleString(localeData.LogTitle, localeConstant, ref text.QuestTitle);
                    }

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
            QuestGiverQuestDetails packet = new();

             packet.QuestTitle = quest.LogTitle;
             packet.LogDescription = quest.LogDescription;
             packet.DescriptionText = quest.QuestDescription;
             packet.PortraitGiverText = quest.PortraitGiverText;
             packet.PortraitGiverName = quest.PortraitGiverName;
             packet.PortraitTurnInText = quest.PortraitTurnInText;
             packet.PortraitTurnInName = quest.PortraitTurnInName;

            Locale locale = _session.GetSessionDbLocaleIndex();

            packet.ConditionalDescriptionText = quest.ConditionalQuestDescription.Select(text =>
            {
                string content = text.Text[(int)Locale.enUS];
                ObjectManager.GetLocaleString(text.Text, locale, ref content);
                return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
            }).ToList();

            if (locale != Locale.enUS)
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
            packet.InformUnit = _session.GetPlayer().GetPlayerSharingQuest();
            packet.QuestID = quest.Id;
            packet.QuestPackageID = (int)quest.PackageID;
            packet.PortraitGiver = quest.QuestGiverPortrait;
            packet.PortraitGiverMount = quest.QuestGiverPortraitMount;
            packet.PortraitGiverModelSceneID = quest.QuestGiverPortraitModelSceneId;
            packet.PortraitTurnIn = quest.QuestTurnInPortrait;
            packet.QuestInfoID = (int)quest.QuestInfoID;
            packet.QuestSessionBonus = 0; //quest.GetQuestSessionBonus(); // this is only sent while quest session is active
            packet.AutoLaunched = autoLaunched;
            packet.ResetByScheduler = quest.IsResetByScheduler();
            packet.DisplayPopup = displayPopup;
            packet.QuestFlags[0] = (uint)(quest.Flags & (WorldConfig.GetBoolValue(WorldCfg.QuestIgnoreAutoAccept) ? ~QuestFlags.AutoAccept : ~QuestFlags.None));
            packet.QuestFlags[1] = (uint)quest.FlagsEx;
            packet.QuestFlags[2] = (uint)quest.FlagsEx2;
            packet.SuggestedPartyMembers = quest.SuggestedPlayers;

            // Is there a better way? what about game objects?
            Creature creature = ObjectAccessor.GetCreature(_session.GetPlayer(), npcGUID);
            if (creature != null)
                packet.QuestGiverCreatureID = (int)creature.GetCreatureTemplate().Entry;

            // RewardSpell can teach multiple spells in trigger spell effects. But not all effects must be SPELL_EFFECT_LEARN_SPELL. See example spell 33950
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(quest.RewardSpell, Difficulty.None);
            if (spellInfo != null)
            {
                foreach (var spellEffectInfo in spellInfo.GetEffects())
                    if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell))
                        packet.LearnSpells.Add(spellEffectInfo.TriggerSpell);
            }

            quest.BuildQuestRewards(packet.Rewards, _session.GetPlayer());

            for (int i = 0; i < SharedConst.QuestEmoteCount; ++i)
            {
                var emote = new QuestDescEmote((int)quest.DetailsEmote[i], quest.DetailsEmoteDelay[i]);
                packet.DescEmotes.Add(emote);
            }

            var objs = quest.Objectives;
            for (int i = 0; i < objs.Count; ++i)
            {
                var obj = new QuestObjectiveSimple();
                obj.Id = objs[i].Id;
                obj.Type = (byte)objs[i].Type;
                obj.ObjectID = objs[i].ObjectID;
                obj.Amount = objs[i].Amount;

                packet.Objectives.Add(obj);
            }

            _session.SendPacket(packet);
        }

        public void SendQuestQueryResponse(Quest quest)
        {
            if (WorldConfig.GetBoolValue(WorldCfg.CacheDataQueries))
                _session.SendPacket(quest.response[(int)_session.GetSessionDbLocaleIndex()]);
            else
            {
                var queryPacket = quest.BuildQueryData(_session.GetSessionDbLocaleIndex(), _session.GetPlayer());
                _session.SendPacket(queryPacket);
            }
        }

        public void SendQuestGiverOfferReward(Quest quest, ObjectGuid npcGUID, bool autoLaunched)
        {
            QuestGiverOfferRewardMessage packet = new();

            packet.QuestTitle = quest.LogTitle;
            packet.RewardText = quest.OfferRewardText;
            packet.PortraitGiverText = quest.PortraitGiverText;
            packet.PortraitGiverName = quest.PortraitGiverName;
            packet.PortraitTurnInText = quest.PortraitTurnInText;
            packet.PortraitTurnInName = quest.PortraitTurnInName;

            Locale locale = _session.GetSessionDbLocaleIndex();

            packet.ConditionalRewardText = quest.ConditionalOfferRewardText.Select(text =>
            {
                string content = text.Text[(int)Locale.enUS];
                ObjectManager.GetLocaleString(text.Text, locale, ref content);
                return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
            }).ToList();

            if (locale != Locale.enUS)
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

            QuestGiverOfferReward offer = new();

            quest.BuildQuestRewards(offer.Rewards, _session.GetPlayer());
            offer.QuestGiverGUID = npcGUID;

            // Is there a better way? what about game objects?
            Creature creature = ObjectAccessor.GetCreature(_session.GetPlayer(), npcGUID);
            if (creature != null)
            {
                packet.QuestGiverCreatureID = creature.GetEntry();
                offer.QuestGiverCreatureID = creature.GetCreatureTemplate().Entry;
            }

            offer.QuestID = quest.Id;
            offer.AutoLaunched = autoLaunched;
            offer.ResetByScheduler = quest.IsResetByScheduler();
            offer.SuggestedPartyMembers = quest.SuggestedPlayers;
            offer.QuestInfoID = (int)quest.QuestInfoID;

            for (uint i = 0; i < SharedConst.QuestEmoteCount && quest.OfferRewardEmote[i] != 0; ++i)
                offer.Emotes.Add(new QuestDescEmote(quest.OfferRewardEmote[i], quest.OfferRewardEmoteDelay[i]));

            offer.QuestFlags[0] = (uint)quest.Flags;
            offer.QuestFlags[1] = (uint)quest.FlagsEx;
            offer.QuestFlags[2] = (uint)quest.FlagsEx2;

            packet.PortraitTurnIn = quest.QuestTurnInPortrait;
            packet.PortraitGiver = quest.QuestGiverPortrait;
            packet.PortraitGiverMount = quest.QuestGiverPortraitMount;
            packet.PortraitGiverModelSceneID = quest.QuestGiverPortraitModelSceneId;
            packet.QuestPackageID = quest.PackageID;

            packet.QuestData = offer;

            _session.SendPacket(packet);
        }

        public void SendQuestGiverRequestItems(Quest quest, ObjectGuid npcGUID, bool canComplete, bool autoLaunched)
        {
            // We can always call to RequestItems, but this packet only goes out if there are actually
            // items.  Otherwise, we'll skip straight to the OfferReward

            if (!quest.HasQuestObjectiveType(QuestObjectiveType.Item) && canComplete)
            {
                SendQuestGiverOfferReward(quest, npcGUID, true);
                return;
            }

            QuestGiverRequestItems packet = new();

            packet.QuestTitle = quest.LogTitle;
            packet.CompletionText = quest.RequestItemsText;

            Locale locale = _session.GetSessionDbLocaleIndex();
            packet.ConditionalCompletionText = quest.ConditionalRequestItemsText.Select(text =>
            {
                string content = text.Text[(int)Locale.enUS];
                ObjectManager.GetLocaleString(text.Text, locale, ref content);
                return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
            }).ToList();

            if (locale != Locale.enUS)
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
            if (creature != null)
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
            packet.QuestFlags[2] = (uint)quest.FlagsEx2;
            packet.SuggestPartyMembers = quest.SuggestedPlayers;
            packet.QuestInfoID = (int)quest.QuestInfoID;

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
            packet.ResetByScheduler = quest.IsResetByScheduler();

            _session.SendPacket(packet);
        }

        public GossipMenu GetGossipMenu() { return _gossipMenu; }
        public QuestMenu GetQuestMenu() { return _questMenu; }
        public InteractionData GetInteractionData() { return _interactionData; }

        bool IsEmpty() { return _gossipMenu.IsEmpty() && _questMenu.IsEmpty(); }

        public uint GetGossipOptionSender(uint selection) { return _gossipMenu.GetMenuItemSender(selection); }
        public uint GetGossipOptionAction(uint selection) { return _gossipMenu.GetMenuItemAction(selection); }
        public bool IsGossipOptionCoded(uint selection) { return _gossipMenu.IsMenuItemCoded(selection); }

        GossipMenu _gossipMenu = new();
        QuestMenu _questMenu = new();
        WorldSession _session;
        InteractionData _interactionData = new();
    }

    public class QuestMenu
    {
        public void AddMenuItem(uint QuestId, byte Icon)
        {
            if (Global.ObjectMgr.GetQuestTemplate(QuestId) == null)
                return;

            QuestMenuItem questMenuItem = new();

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

        List<QuestMenuItem> _questMenuItems = new();
    }

    public struct QuestMenuItem
    {
        public uint QuestId;
        public byte QuestIcon;
    }

    public class GossipMenuItem
    {
        public int GossipOptionID;
        public uint OrderIndex;
        public GossipOptionNpc OptionNpc;
        public string OptionText;
        public uint Language;
        public GossipOptionFlags Flags;
        public int? GossipNpcOptionID;
        public bool BoxCoded;
        public ulong BoxMoney;
        public string BoxText;
        public int? SpellID;
        public int? OverrideIconID;

        // action data
        public uint ActionMenuID;
        public uint ActionPoiID;

        // additional scripting identifiers
        public uint Sender;
        public uint Action;
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
        public StringArray Text = new((int)Locale.Total);
    }

    public class GossipMenuItems
    {
        public uint MenuID;
        public int GossipOptionID;
        public uint OrderIndex;
        public GossipOptionNpc OptionNpc;
        public string OptionText;
        public uint OptionBroadcastTextId;
        public uint Language;
        public GossipOptionFlags Flags;
        public uint ActionMenuID;
        public uint ActionPoiID;
        public int? GossipNpcOptionID;
        public bool BoxCoded;
        public ulong BoxMoney;
        public string BoxText;
        public uint BoxBroadcastTextId;
        public int? SpellID;
        public int? OverrideIconID;
        public ConditionsReference Conditions;
    }

    public class GossipMenuAddon
    {
        public int FriendshipFactionID;
        public uint LfgDungeonsID;
    }

    public class PointOfInterest
    {
        public uint Id;
        public Vector3 Pos;
        public uint Icon;
        public uint Flags;
        public uint Importance;
        public string Name;
        public uint WMOGroupID;
    }

    public class PointOfInterestLocale
    {
        public StringArray Name = new((int)Locale.Total); 
    }

    public class GossipMenus
    {
        public uint MenuId;
        public uint TextId;
        public ConditionsReference Conditions;
    }
}
