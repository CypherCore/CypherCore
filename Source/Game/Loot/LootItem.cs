// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Conditions;
using Game.Entities;

namespace Game.Loots
{
    public class LootItem
    {
        public List<ObjectGuid> AllowedGUIDs { get; set; } = new();
        public List<uint> BonusListIDs { get; set; } = new();
        public List<Condition> Conditions { get; set; } = new(); // additional loot condition
        public ItemContext Context { get; set; }
        public byte Count { get; set; }
        public bool Follow_loot_rules { get; set; }
        public bool Freeforall { get; set; } // free for all
        public bool Is_blocked { get; set; }
        public bool Is_counted { get; set; }
        public bool Is_looted { get; set; }
        public bool Is_underthreshold { get; set; }

        public uint Itemid { get; set; }
        public uint LootListId { get; set; }
        public bool Needs_quest { get; set; } // quest drop
        public uint RandomBonusListId { get; set; }
        public ObjectGuid RollWinnerGUID; // Stores the Guid of person who won loot, if his bags are full only he can see the Item in loot list!

        public LootItem()
        {
        }

        public LootItem(LootStoreItem li)
        {
            Itemid = li.itemid;
            Conditions = li.conditions;

            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(Itemid);
            Freeforall = proto != null && proto.HasFlag(ItemFlags.MultiDrop);
            Follow_loot_rules = !li.needs_quest || (proto != null && proto.FlagsCu.HasAnyFlag(ItemFlagsCustom.FollowLootRules));

            Needs_quest = li.needs_quest;

            RandomBonusListId = ItemEnchantmentManager.GenerateItemRandomBonusListId(Itemid);
        }

        /// <summary>
        ///  Basic checks for player/Item compatibility - if false no chance to see the Item in the loot - used only for loot generation
        /// </summary>
        /// <param Name="player"></param>
        /// <param Name="loot"></param>
        /// <returns></returns>
        public bool AllowedForPlayer(Player player, Loot loot)
        {
            return AllowedForPlayer(player, loot, Itemid, Needs_quest, Follow_loot_rules, false, Conditions);
        }

        public static bool AllowedForPlayer(Player player, Loot loot, uint itemid, bool needs_quest, bool follow_loot_rules, bool strictUsabilityCheck, List<Condition> conditions)
        {
            // DB conditions check
            if (!Global.ConditionMgr.IsObjectMeetToConditions(player, conditions))
                return false;

            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(itemid);

            if (pProto == null)
                return false;

            // not show loot for not own team
            if (pProto.HasFlag(ItemFlags2.FactionHorde) &&
                player.GetTeam() != Team.Horde)
                return false;

            if (pProto.HasFlag(ItemFlags2.FactionAlliance) &&
                player.GetTeam() != Team.Alliance)
                return false;

            // Master looter can see all items even if the character can't loot them
            if (loot != null &&
                loot.GetLootMethod() == LootMethod.MasterLoot &&
                follow_loot_rules &&
                loot.GetLootMasterGUID() == player.GetGUID())
                return true;

            // Don't allow loot for players without profession or those who already know the recipe
            if (pProto.HasFlag(ItemFlags.HideUnusableRecipe))
            {
                if (!player.HasSkill((SkillType)pProto.GetRequiredSkill()))
                    return false;

                foreach (var itemEffect in pProto.Effects)
                {
                    if (itemEffect.TriggerType != ItemSpelltriggerType.OnLearn)
                        continue;

                    if (player.HasSpell((uint)itemEffect.SpellID))
                        return false;
                }
            }

            // check quest requirements
            if (!pProto.FlagsCu.HasAnyFlag(ItemFlagsCustom.IgnoreQuestStatus) &&
                ((needs_quest || (pProto.GetStartQuest() != 0 && player.GetQuestStatus(pProto.GetStartQuest()) != QuestStatus.None)) && !player.HasQuestForItem(itemid)))
                return false;

            if (strictUsabilityCheck)
            {
                if ((pProto.IsWeapon() || pProto.IsArmor()) &&
                    !pProto.IsUsableByLootSpecialization(player, true))
                    return false;

                if (player.CanRollNeedForItem(pProto, null, false) != InventoryResult.Ok)
                    return false;
            }

            return true;
        }

        public void AddAllowedLooter(Player player)
        {
            AllowedGUIDs.Add(player.GetGUID());
        }

        public bool HasAllowedLooter(ObjectGuid looter)
        {
            return AllowedGUIDs.Contains(looter);
        }

        public LootSlotType? GetUiTypeForPlayer(Player player, Loot loot)
        {
            if (Is_looted)
                return null;

            if (!AllowedGUIDs.Contains(player.GetGUID()))
                return null;

            if (Freeforall)
            {
                var ffaItems = loot.GetPlayerFFAItems().LookupByKey(player.GetGUID());

                if (ffaItems != null)
                {
                    var ffaItemItr = ffaItems.Find(ffaItem => ffaItem.LootListId == LootListId);

                    if (ffaItemItr != null &&
                        !ffaItemItr.Is_looted)
                        return loot.GetLootMethod() == LootMethod.FreeForAll ? LootSlotType.Owner : LootSlotType.AllowLoot;
                }

                return null;
            }

            if (Needs_quest && !Follow_loot_rules)
                return loot.GetLootMethod() == LootMethod.FreeForAll ? LootSlotType.Owner : LootSlotType.AllowLoot;

            switch (loot.GetLootMethod())
            {
                case LootMethod.FreeForAll:
                    return LootSlotType.Owner;
                case LootMethod.RoundRobin:
                    if (!loot.RoundRobinPlayer.IsEmpty() &&
                        loot.RoundRobinPlayer != player.GetGUID())
                        return null;

                    return LootSlotType.AllowLoot;
                case LootMethod.MasterLoot:
                    if (Is_underthreshold)
                    {
                        if (!loot.RoundRobinPlayer.IsEmpty() &&
                            loot.RoundRobinPlayer != player.GetGUID())
                            return null;

                        return LootSlotType.AllowLoot;
                    }

                    return loot.GetLootMasterGUID() == player.GetGUID() ? LootSlotType.Master : LootSlotType.Locked;
                case LootMethod.GroupLoot:
                case LootMethod.NeedBeforeGreed:
                    if (Is_underthreshold)
                        if (!loot.RoundRobinPlayer.IsEmpty() &&
                            loot.RoundRobinPlayer != player.GetGUID())
                            return null;

                    if (Is_blocked)
                        return LootSlotType.RollOngoing;

                    if (RollWinnerGUID.IsEmpty()) // all passed
                        return LootSlotType.AllowLoot;

                    if (RollWinnerGUID == player.GetGUID())
                        return LootSlotType.Owner;

                    return null;
                case LootMethod.PersonalLoot:
                    return LootSlotType.Owner;
                default:
                    break;
            }

            return null;
        }

        public List<ObjectGuid> GetAllowedLooters()
        {
            return AllowedGUIDs;
        }
    }
}