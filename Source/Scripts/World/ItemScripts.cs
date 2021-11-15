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
using Framework.GameMath;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;

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
    class item_nether_wraith_beacon : ItemScript
    {
        public item_nether_wraith_beacon() : base("item_nether_wraith_beacon") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            if (player.GetQuestStatus(10832) == QuestStatus.Incomplete)
            {
                Creature nether = player.SummonCreature(22408, player.GetPositionX(), player.GetPositionY() + 20, player.GetPositionZ(), 0, TempSummonType.TimedDespawn, 180000);
                if (nether)
                    nether.GetAI().AttackStart(player);

                nether = player.SummonCreature(22408, player.GetPositionX(), player.GetPositionY() - 20, player.GetPositionZ(), 0, TempSummonType.TimedDespawn, 180000);
                if (nether)
                    nether.GetAI().AttackStart(player);
            }
            return false;
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
    class item_incendiary_explosives : ItemScript
    {
        public item_incendiary_explosives() : base("item_incendiary_explosives") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            if (player.FindNearestCreature(26248, 15) || player.FindNearestCreature(26249, 15))
                return false;
            else
            {
                player.SendEquipError(InventoryResult.OutOfRange, item, null);
                return true;
            }
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
                player.StoreNewItem(dest, 39883, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(39883));

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
                player.StoreNewItem(dest, 44718, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(44718));

            return true;
        }
    }

    [Script]
    class item_pile_fake_furs : ItemScript
    {
        public item_pile_fake_furs() : base("item_pile_fake_furs") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            GameObject go = null;
            foreach (var id in GameObjectIds.CaribouTraps)
            {
                go = player.FindNearestGameObject(id, 5.0f);
                if (go)
                    break;
            }

            if (!go)
                return false;

            if (go.FindNearestCreature(CreatureIds.NesingwaryTrapper, 10.0f, true) || go.FindNearestCreature(CreatureIds.NesingwaryTrapper, 10.0f, false) || go.FindNearestGameObject(GameObjectIds.HighQualityFur, 2.0f))
                return true;

            go.GetClosePoint(out float x, out float y, out float z, go.GetCombatReach() / 3, 7.0f);
            go.SummonGameObject(GameObjectIds.HighQualityFur, go, Quaternion.fromEulerAnglesZYX(go.GetOrientation(), 0.0f, 0.0f), 1);
            TempSummon summon = player.SummonCreature(CreatureIds.NesingwaryTrapper, x, y, z, go.GetOrientation(), TempSummonType.DeadDespawn, 1000);
            if (summon)
            {
                summon.SetVisible(false);
                summon.SetReactState(ReactStates.Passive);
                summon.SetImmuneToPC(true);
            }
            return false;
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

            if (!player.GetTransport() || player.GetAreaId() != Misc.AreaIdShatteredStraits)
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

