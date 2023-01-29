// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.DataStorage;

namespace Game.Misc
{
    public class GossipMenu
    {
        private Locale _locale;
        private uint _menuId;

        private readonly SortedDictionary<uint, GossipMenuItem> _menuItems = new();

        public uint AddMenuItem(int gossipOptionId, int orderIndex, GossipOptionNpc optionNpc, string optionText, uint language,
                                GossipOptionFlags flags, int? gossipNpcOptionId, uint actionMenuId, uint actionPoiId, bool boxCoded, uint boxMoney,
                                string boxText, int? spellId, int? overrideIconId, uint sender, uint action)
        {
            Cypher.Assert(_menuItems.Count <= SharedConst.MaxGossipMenuItems);

            // Find a free new Id - script case
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
                    foreach (var pair in _menuItems)
                    {
                        if (pair.Value.OrderIndex > orderIndex)
                            break;

                        orderIndex = (int)pair.Value.OrderIndex + 1;
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
        ///  Adds a localized gossip menu Item from db by menu Id and menu Item Id.
        /// </summary>
        /// <param Name="menuId">menuId Gossip menu Id.</param>
        /// <param Name="menuItemId">menuItemId Gossip menu Item Id.</param>
        /// <param Name="sender">sender Identifier of the current menu.</param>
        /// <param Name="action">Action Custom Action given to OnGossipHello.</param>
        public void AddMenuItem(uint menuId, uint menuItemId, uint sender, uint action)
        {
            // Find items for given menu Id.
            var bounds = Global.ObjectMgr.GetGossipMenuItemsMapBounds(menuId);

            // Return if there are none.
            if (bounds.Empty())
                return;

            /// Find the one with the given menu Item Id.
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
            {
                strOptionText = Global.DB2Mgr.GetBroadcastTextValue(optionBroadcastText, GetLocale());
            }
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
            {
                strBoxText = Global.DB2Mgr.GetBroadcastTextValue(boxBroadcastText, GetLocale());
            }
            else
            {
                strBoxText = menuItem.BoxText;

                // Find localizations from database.
                if (GetLocale() != Locale.enUS)
                {
                    GossipMenuItemsLocale gossipMenuLocale = Global.ObjectMgr.GetGossipMenuItemsLocale(menuItem.MenuID, menuItem.OrderIndex);

                    if (gossipMenuLocale != null)
                        ObjectManager.GetLocaleString(gossipMenuLocale.BoxText, GetLocale(), ref strBoxText);
                }
            }

            AddMenuItem(menuItem.GossipOptionID,
                        (int)menuItem.OrderIndex,
                        menuItem.OptionNpc,
                        strOptionText,
                        menuItem.Language,
                        menuItem.Flags,
                        menuItem.GossipNpcOptionID,
                        menuItem.ActionMenuID,
                        menuItem.ActionPoiID,
                        menuItem.BoxCoded,
                        menuItem.BoxMoney,
                        strBoxText,
                        menuItem.SpellID,
                        menuItem.OverrideIconID,
                        sender,
                        action);
        }

        public GossipMenuItem GetItem(int gossipOptionId)
        {
            return _menuItems.Values.FirstOrDefault(item => item.GossipOptionID == gossipOptionId);
        }

        private GossipMenuItem GetItemByIndex(uint orderIndex)
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

        public void SetMenuId(uint menu_id)
        {
            _menuId = menu_id;
        }

        public uint GetMenuId()
        {
            return _menuId;
        }

        public void SetLocale(Locale locale)
        {
            _locale = locale;
        }

        private Locale GetLocale()
        {
            return _locale;
        }

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
    }
}