/*
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
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Scripts.World.ItemScripts
{
    struct  SpellIds
    {  
        //Onlyforflight
        public const uint ArcaneCharges = 45072;

        //Petrovclusterbombs
        public const uint PetrovBomb = 42406;
    }

    struct CreatureIds
    {
        //Pilefakefur
        public const uint NesingwaryTrapper = 25835;

        //Helpthemselves
        public const uint TrappedMammothCalf = 25850;

        //Theemissary
        public const uint Leviroth = 26452;

        //Capturedfrog
        public const uint VanirasSentryTotem = 40187;
    }

    struct GameObjectIds
    {
        //Pilefakefur
        public const uint HighQualityFur = 187983;
        public static uint[] CaribouTraps =
        {
            187982, 187995, 187996, 187997, 187998,
            187999, 188000, 188001, 188002, 188003,
            188004, 188005, 188006, 188007, 188008,
        };

        //Helpthemselves
        public static uint[] MammothTraps =
        {
            188022, 188024, 188025, 188026, 188027,
            188028, 188029, 188030, 188031, 188032,
            188033, 188034, 188035, 188036, 188037,
            188038, 188039, 188040, 188041, 188042,
            188043, 188044
        };
    }

    struct QuestIds
    {
        //Helpthemselves
        public const uint CannotHelpThemselves = 11876;

        //Theemissary
        public const uint TheEmissary = 11626;

        //Capturedfrog
        public const uint ThePerfectSpies = 25444;
    }

    struct Misc
    {
        //Petrovclusterbombs
        public const uint AreaIdShatteredStraits = 4064;
        public const uint ZoneIdHowling = 495;
    }

    [Script]
    class item_only_for_flight : ItemScript
    {
        public item_only_for_flight() : base("item_only_for_flight") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            uint itemId = item.GetEntry();
            bool disabled = false;

            //for special scripts
            switch (itemId)
            {
                case 24538:
                    if (player.GetAreaId() != 3628)
                        disabled = true;
                    break;
                case 34489:
                    if (player.GetZoneId() != 4080)
                        disabled = true;
                    break;
                case 34475:
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.ArcaneCharges, player.GetMap().GetDifficultyID());
                    if (spellInfo != null)
                        Spell.SendCastResult(player, spellInfo, default, castId, SpellCastResult.NotOnGround);
                    break;
            }

            // allow use in flight only
            if (player.IsInFlight() && !disabled)
                return false;

            // error
            player.SendEquipError(InventoryResult.ClientLockedOut, item, null);
            return true;
        }
    }

    [Script]
    class item_gor_dreks_ointment : ItemScript
    {
        public item_gor_dreks_ointment() : base("item_gor_dreks_ointment") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            if (targets.GetUnitTarget() && targets.GetUnitTarget().IsTypeId(TypeId.Unit) &&
                targets.GetUnitTarget().GetEntry() == 20748 && !targets.GetUnitTarget().HasAura(32578))
                return false;

            player.SendEquipError(InventoryResult.ClientLockedOut, item, null);
            return true;
        }
    }

    [Script]
    class item_mysterious_egg : ItemScript
    {
        public item_mysterious_egg() : base("item_mysterious_egg") { }

        public override bool OnExpire(Player player, ItemTemplate pItemProto)
        {
            List<ItemPosCount> dest = new();
            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, 39883, 1); // Cracked Egg
            if (msg == InventoryResult.Ok)
                player.StoreNewItem(dest, 39883, true, ItemEnchantmentManager.GenerateItemRandomPropertyId(39883));

            return true;
        }
    }

    [Script]
    class item_disgusting_jar : ItemScript
    {
        public item_disgusting_jar() : base("item_disgusting_jar") { }

        public override bool OnExpire(Player player, ItemTemplate pItemProto)
        {
            List<ItemPosCount> dest = new();
            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, 44718, 1); // Ripe Disgusting Jar
            if (msg == InventoryResult.Ok)
                player.StoreNewItem(dest, 44718, true, ItemEnchantmentManager.GenerateItemRandomPropertyId(44718));

            return true;
        }
    }

    [Script]
    class item_petrov_cluster_bombs : ItemScript
    {
        public item_petrov_cluster_bombs() : base("item_petrov_cluster_bombs") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            if (player.GetZoneId() != Misc.ZoneIdHowling)
                return false;

            if (player.GetTransport() == null || player.GetAreaId() != Misc.AreaIdShatteredStraits)
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(SpellIds.PetrovBomb, Difficulty.None);
                if (spellInfo != null)
                    Spell.SendCastResult(player, spellInfo, default, castId, SpellCastResult.NotHere);

                return true;
            }

            return false;
        }
    }

    [Script]
    class item_dehta_trap_smasher : ItemScript
    {
        public item_dehta_trap_smasher() : base("item_dehta_trap_smasher") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            if (player.GetQuestStatus(QuestIds.CannotHelpThemselves) != QuestStatus.Incomplete)
                return false;

            Creature pMammoth = player.FindNearestCreature(CreatureIds.TrappedMammothCalf, 5.0f);
            if (!pMammoth)
                return false;

            foreach (var id in GameObjectIds.MammothTraps)
            {
                GameObject pTrap = player.FindNearestGameObject(id, 11.0f);
                if (pTrap)
                {
                    pMammoth.GetAI().DoAction(1);
                    pTrap.SetGoState(GameObjectState.Ready);
                    player.KilledMonsterCredit(CreatureIds.TrappedMammothCalf);
                    return true;
                }
            }
            return false;
        }
    }

    [Script]
    class item_captured_frog : ItemScript
    {
        public item_captured_frog() : base("item_captured_frog") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            if (player.GetQuestStatus(QuestIds.ThePerfectSpies) == QuestStatus.Incomplete)
            {
                if (player.FindNearestCreature(CreatureIds.VanirasSentryTotem, 10.0f))
                    return false;
                else
                    player.SendEquipError(InventoryResult.OutOfRange, item, null);
            }
            else
                player.SendEquipError(InventoryResult.ClientLockedOut, item, null);
            return true;
        }
    }

    [Script] // Only used currently for
    // 19169: Nightfall
    class item_generic_limit_chance_above_60 : ItemScript
    {
        public item_generic_limit_chance_above_60() : base("item_generic_limit_chance_above_60") { }

        public override bool OnCastItemCombatSpell(Player player, Unit victim, SpellInfo spellInfo, Item item)
        {
            // spell proc Chance gets severely reduced on victims > 60 (formula unknown)
            if (victim.GetLevel() > 60)
            {
                // gives ~0.1% proc Chance at lvl 70
                float lvlPenaltyFactor = 9.93f;
                float failureChance = (victim.GetLevelForTarget(player) - 60) * lvlPenaltyFactor;

                // base ppm Chance was already rolled, only roll success Chance
                return !RandomHelper.randChance(failureChance);
            }

            return true;
        }
    }
}

