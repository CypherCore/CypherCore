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

using Framework.Constants;
using Framework.GameMath;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.World
{
    struct ItemScriptConst
    {
        //Onlyforflight
        public const uint SpellArcaneCharges = 45072;

        //Pilefakefur
        public const uint GoCaribouTrap1 = 187982;
        public const uint GoCaribouTrap2 = 187995;
        public const uint GoCaribouTrap3 = 187996;
        public const uint GoCaribouTrap4 = 187997;
        public const uint GoCaribouTrap5 = 187998;
        public const uint GoCaribouTrap6 = 187999;
        public const uint GoCaribouTrap7 = 188000;
        public const uint GoCaribouTrap8 = 188001;
        public const uint GoCaribouTrap9 = 188002;
        public const uint GoCaribouTrap10 = 188003;
        public const uint GoCaribouTrap11 = 188004;
        public const uint GoCaribouTrap12 = 188005;
        public const uint GoCaribouTrap13 = 188006;
        public const uint GoCaribouTrap14 = 188007;
        public const uint GoCaribouTrap15 = 188008;
        public const uint GoHighQualityFur = 187983;
        public const uint NpcNesingwaryTrapper = 25835;

        public static uint[] CaribouTraps =
        {
            GoCaribouTrap1, GoCaribouTrap2, GoCaribouTrap3, GoCaribouTrap4, GoCaribouTrap5,
            GoCaribouTrap6, GoCaribouTrap7, GoCaribouTrap8, GoCaribouTrap9, GoCaribouTrap10,
            GoCaribouTrap11, GoCaribouTrap12, GoCaribouTrap13, GoCaribouTrap14, GoCaribouTrap15,
        };

        //Petrovclusterbombs
        public const uint SpellPetrovBomb = 42406;
        public const uint AreaIdShatteredStraits = 4064;
        public const uint ZoneIdHowling = 495;

        //Helpthemselves
        public const uint QuestCannotHelpThemselves = 11876;
        public const uint NpcTrappedMammothCalf = 25850;
        public const uint GoMammothTrap1 = 188022;
        public const uint GoMammothTrap2 = 188024;
        public const uint GoMammothTrap3 = 188025;
        public const uint GoMammothTrap4 = 188026;
        public const uint GoMammothTrap5 = 188027;
        public const uint GoMammothTrap6 = 188028;
        public const uint GoMammothTrap7 = 188029;
        public const uint GoMammothTrap8 = 188030;
        public const uint GoMammothTrap9 = 188031;
        public const uint GoMammothTrap10 = 188032;
        public const uint GoMammothTrap11 = 188033;
        public const uint GoMammothTrap12 = 188034;
        public const uint GoMammothTrap13 = 188035;
        public const uint GoMammothTrap14 = 188036;
        public const uint GoMammothTrap15 = 188037;
        public const uint GoMammothTrap16 = 188038;
        public const uint GoMammothTrap17 = 188039;
        public const uint GoMammothTrap18 = 188040;
        public const uint GoMammothTrap19 = 188041;
        public const uint GoMammothTrap20 = 188042;
        public const uint GoMammothTrap21 = 188043;
        public const uint GoMammothTrap22 = 188044;

        public static uint[] MammothTraps =
        {
            GoMammothTrap1, GoMammothTrap2, GoMammothTrap3, GoMammothTrap4, GoMammothTrap5,
            GoMammothTrap6, GoMammothTrap7, GoMammothTrap8, GoMammothTrap9, GoMammothTrap10,
            GoMammothTrap11, GoMammothTrap12, GoMammothTrap13, GoMammothTrap14, GoMammothTrap15,
            GoMammothTrap16, GoMammothTrap17, GoMammothTrap18, GoMammothTrap19, GoMammothTrap20,
            GoMammothTrap21, GoMammothTrap22
        };

        //Theemissary
        public const uint QuestTheEmissary = 11626;
        public const uint NpcLeviroth = 26452;

        //Capturedfrog
        public const uint QuestThePerfectSpies = 25444;
        public const uint NpcVanirasSentryTotem = 40187;
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
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(ItemScriptConst.SpellArcaneCharges);
                    if (spellInfo != null)
                        Spell.SendCastResult(player, spellInfo, 0, castId, SpellCastResult.NotOnGround);
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

    [Script] //item_nether_wraith_beacon
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

                Creature nether1 = player.SummonCreature(22408, player.GetPositionX(), player.GetPositionY() - 20, player.GetPositionZ(), 0, TempSummonType.TimedDespawn, 180000);
                if (nether1)
                    nether1.GetAI().AttackStart(player);
            }
            return false;
        }
    }

    [Script] //item_gor_dreks_ointment
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

    [Script] //item_incendiary_explosives
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

    [Script] //item_mysterious_egg
    class item_mysterious_egg : ItemScript
    {
        public item_mysterious_egg() : base("item_mysterious_egg") { }

        public override bool OnExpire(Player player, ItemTemplate pItemProto)
        {
            List<ItemPosCount> dest = new List<ItemPosCount>();
            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, 39883, 1); // Cracked Egg
            if (msg == InventoryResult.Ok)
                player.StoreNewItem(dest, 39883, true, ItemEnchantment.GenerateItemRandomPropertyId(39883));

            return true;
        }
    }

    [Script] //item_disgusting_jar
    class item_disgusting_jar : ItemScript
    {
        public item_disgusting_jar() : base("item_disgusting_jar") { }

        public override bool OnExpire(Player player, ItemTemplate pItemProto)
        {
            List<ItemPosCount> dest = new List<ItemPosCount>();
            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, 44718, 1); // Ripe Disgusting Jar
            if (msg == InventoryResult.Ok)
                player.StoreNewItem(dest, 44718, true, ItemEnchantment.GenerateItemRandomPropertyId(44718));

            return true;
        }
    }

    [Script] //item_pile_fake_furs
    class item_pile_fake_furs : ItemScript
    {
        public item_pile_fake_furs() : base("item_pile_fake_furs") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            GameObject go = null;
            for (byte i = 0; i < ItemScriptConst.CaribouTraps.Length; ++i)
            {
                go = player.FindNearestGameObject(ItemScriptConst.CaribouTraps[i], 5.0f);
                if (go)
                    break;
            }

            if (!go)
                return false;

            if (go.FindNearestCreature(ItemScriptConst.NpcNesingwaryTrapper, 10.0f, true) || go.FindNearestCreature(ItemScriptConst.NpcNesingwaryTrapper, 10.0f, false) || go.FindNearestGameObject(ItemScriptConst.GoHighQualityFur, 2.0f))
                return true;

            float x, y, z;
            go.GetClosePoint(out x, out y, out z, go.GetObjectSize() / 3, 7.0f);
            go.SummonGameObject(ItemScriptConst.GoHighQualityFur, go, Quaternion.fromEulerAnglesZYX(go.GetOrientation(), 0.0f, 0.0f), 1);
            TempSummon summon = player.SummonCreature(ItemScriptConst.NpcNesingwaryTrapper, x, y, z, go.GetOrientation(), TempSummonType.DeadDespawn, 1000);
            if (summon)
            {
                summon.SetVisible(false);
                summon.SetReactState(ReactStates.Passive);
                summon.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc);
            }
            return false;
        }
    }

    [Script] //item_petrov_cluster_bombs
    class item_petrov_cluster_bombs : ItemScript
    {
        public item_petrov_cluster_bombs() : base("item_petrov_cluster_bombs") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            if (player.GetZoneId() != ItemScriptConst.ZoneIdHowling)
                return false;

            if (!player.GetTransport() || player.GetAreaId() != ItemScriptConst.AreaIdShatteredStraits)
            {
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(ItemScriptConst.SpellPetrovBomb);
                if (spellInfo != null)
                    Spell.SendCastResult(player, spellInfo, 0, castId, SpellCastResult.NotHere);

                return true;
            }

            return false;
        }
    }

    //item_dehta_trap_smasher
    [Script] //For quest 11876, Help Those That Cannot Help Themselves
    class item_dehta_trap_smasher : ItemScript
    {
        public item_dehta_trap_smasher() : base("item_dehta_trap_smasher") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            if (player.GetQuestStatus(ItemScriptConst.QuestCannotHelpThemselves) != QuestStatus.Incomplete)
                return false;

            Creature pMammoth = player.FindNearestCreature(ItemScriptConst.NpcTrappedMammothCalf, 5.0f);
            if (!pMammoth)
                return false;

            GameObject pTrap = null;
            for (byte i = 0; i < ItemScriptConst.MammothTraps.Length; ++i)
            {
                pTrap = player.FindNearestGameObject(ItemScriptConst.MammothTraps[i], 11.0f);
                if (pTrap)
                {
                    pMammoth.GetAI().DoAction(1);
                    pTrap.SetGoState(GameObjectState.Ready);
                    player.KilledMonsterCredit(ItemScriptConst.NpcTrappedMammothCalf);
                    return true;
                }
            }
            return false;
        }
    }

    [Script]
    class item_trident_of_nazjan : ItemScript
    {
        public item_trident_of_nazjan() : base("item_Trident_of_Nazjan") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            if (player.GetQuestStatus(ItemScriptConst.QuestTheEmissary) == QuestStatus.Incomplete)
            {
                Creature pLeviroth = player.FindNearestCreature(ItemScriptConst.NpcLeviroth, 10.0f);
                if (pLeviroth) // spell range
                {
                    pLeviroth.GetAI().AttackStart(player);
                    return false;
                }
                else
                    player.SendEquipError(InventoryResult.OutOfRange, item, null);
            }
            else
                player.SendEquipError(InventoryResult.ClientLockedOut, item, null);
            return true;
        }
    }

    [Script]
    class item_captured_frog : ItemScript
    {
        public item_captured_frog() : base("item_captured_frog") { }

        public override bool OnUse(Player player, Item item, SpellCastTargets targets, ObjectGuid castId)
        {
            if (player.GetQuestStatus(ItemScriptConst.QuestThePerfectSpies) == QuestStatus.Incomplete)
            {
                if (player.FindNearestCreature(ItemScriptConst.NpcVanirasSentryTotem, 10.0f))
                    return false;
                else
                    player.SendEquipError(InventoryResult.OutOfRange, item, null);
            }
            else
                player.SendEquipError(InventoryResult.ClientLockedOut, item, null);
            return true;
        }
    }
}
