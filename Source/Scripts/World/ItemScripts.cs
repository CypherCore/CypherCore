// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IItem;
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
    class item_only_for_flight : ScriptObjectAutoAdd, IItemOnUse
    {
        public item_only_for_flight() : base("item_only_for_flight") { }

        public bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
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
    class item_gor_dreks_ointment : ScriptObjectAutoAdd, IItemOnUse
    {
        public item_gor_dreks_ointment() : base("item_gor_dreks_ointment") { }

        public bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            if (targets.GetUnitTarget() && targets.GetUnitTarget().IsTypeId(TypeId.Unit) &&
                targets.GetUnitTarget().GetEntry() == 20748 && !targets.GetUnitTarget().HasAura(32578))
                return false;

            player.SendEquipError(InventoryResult.ClientLockedOut, item, null);
            return true;
        }
    }

    [Script]
    class item_mysterious_egg : ScriptObjectAutoAdd, IItemOnExpire
    {
        public item_mysterious_egg() : base("item_mysterious_egg") { }

        public bool OnExpire(Player player, ItemTemplate pItemProto)
        {
            List<ItemPosCount> dest = new();
            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, 39883, 1); // Cracked Egg
            if (msg == InventoryResult.Ok)
                player.StoreNewItem(dest, 39883, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(39883));

            return true;
        }
    }

    [Script]
    class item_disgusting_jar : ScriptObjectAutoAdd, IItemOnExpire
    {
        public item_disgusting_jar() : base("item_disgusting_jar") { }

        public bool OnExpire(Player player, ItemTemplate pItemProto)
        {
            List<ItemPosCount> dest = new();
            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, 44718, 1); // Ripe Disgusting Jar
            if (msg == InventoryResult.Ok)
                player.StoreNewItem(dest, 44718, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(44718));

            return true;
        }
    }

    [Script]
    class item_petrov_cluster_bombs : ScriptObjectAutoAdd, IItemOnUse
    {
        public item_petrov_cluster_bombs() : base("item_petrov_cluster_bombs") { }

        public bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
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
    class item_captured_frog : ScriptObjectAutoAdd, IItemOnUse
    {
        public item_captured_frog() : base("item_captured_frog") { }

        public bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
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
    class item_generic_limit_chance_above_60 : ScriptObjectAutoAdd, IItemOnCastItemCombatSpell
    {
        public item_generic_limit_chance_above_60() : base("item_generic_limit_chance_above_60") { }

        public bool OnCastItemCombatSpell(Player player, Unit victim, SpellInfo spellInfo, Item item)
        {
            // spell proc chance gets severely reduced on victims > 60 (formula unknown)
            if (victim.GetLevel() > 60)
            {
                // gives ~0.1% proc chance at lvl 70
                float lvlPenaltyFactor = 9.93f;
                float failureChance = (victim.GetLevelForTarget(player) - 60) * lvlPenaltyFactor;

                // base ppm chance was already rolled, only roll success chance
                return !RandomHelper.randChance(failureChance);
            }

            return true;
        }
    }
}

