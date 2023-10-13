// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using static Global;

namespace Scripts.World.NpcProfessions
{
    struct SpellIds
    {
        public const uint Weapon = 9787;
        public const uint Armor = 9788;
        public const uint Hammer = 17040;
        public const uint Axe = 17041;
        public const uint Sword = 17039;

        public const uint LearnWeapon = 9789;
        public const uint LearnArmor = 9790;
        public const uint LearnHammer = 39099;
        public const uint LearnAxe = 39098;
        public const uint LearnSword = 39097;

        public const uint UnlearnWeapon = 36436;
        public const uint UnlearnArmor = 36435;
        public const uint UnlearnHammer = 36441;
        public const uint UnlearnAxe = 36439;
        public const uint UnlearnSword = 36438;

        public const uint RepArmor = 17451;
        public const uint RepWeapon = 17452;

        public const uint Dragon = 10656;
        public const uint Elemental = 10658;
        public const uint Tribal = 10660;

        public const uint LearnDragon = 10657;
        public const uint LearnElemental = 10659;
        public const uint LearnTribal = 10661;

        public const uint UnlearnDragon = 36434;
        public const uint UnlearnElemental = 36328;
        public const uint UnlearnTribal = 36433;

        public const uint Goblin = 20222;
        public const uint Gnomish = 20219;

        public const uint LearnGoblin = 20221;
        public const uint LearnGnomish = 20220;

        public const uint Spellfire = 26797;
        public const uint Mooncloth = 26798;
        public const uint Shadoweave = 26801;

        public const uint LearnSpellfire = 26796;
        public const uint LearnMooncloth = 26799;
        public const uint LearnShadoweave = 26800;

        public const uint UnlearnSpellfire = 41299;
        public const uint UnlearnMooncloth = 41558;
        public const uint UnlearnShadoweave = 41559;

        public const uint Transmute = 28672;
        public const uint Elixir = 28677;
        public const uint Potion = 28675;

        public const uint LearnTransmute = 28674;
        public const uint LearnElixir = 28678;
        public const uint LearnPotion = 28676;

        public const uint UnlearnTransmute = 41565;
        public const uint UnlearnElixir = 41564;
        public const uint UnlearnPotion = 41563;

        // EngineeringTrinkets
        public const uint LearnToEverlook = 23490;
        public const uint LearnToGadget = 23491;
        public const uint LearnToArea52 = 36956;
        public const uint LearnToToshley = 36957;

        public const uint ToEverlook = 23486;
        public const uint ToGadget = 23489;
        public const uint ToArea52 = 36954;
        public const uint ToToshley = 36955;
    }

    struct CreatureIds
    {
        //Alchemy
        public const uint TrainerTransmute = 22427; // Zarevhi
        public const uint TrainerElixir = 19052; // Lorokeem
        public const uint TrainerPotion = 17909; // Lauranna Thar'well

        //Blacksmithing
        public const uint TrainerSmithomni1 = 11145; // Myolor Sunderfury
        public const uint TrainerSmithomni2 = 11176; // Krathok Moltenfist
        public const uint TrainerWeapon1 = 11146; // Ironus Coldsteel
        public const uint TrainerWeapon2 = 11178; // Borgosh Corebender
        public const uint TrainerArmor1 = 5164; // Grumnus Steelshaper
        public const uint TrainerArmor2 = 11177; // Okothos Ironrager
        public const uint TrainerHammer = 11191; // Lilith the Lithe
        public const uint TrainerAxe = 11192; // Kilram
        public const uint TrainerSword = 11193; // Seril Scourgebane

        //Leatherworking
        public const uint TrainerDragon1 = 7866; // Peter Galen
        public const uint TrainerDragon2 = 7867; // Thorkaf Dragoneye
        public const uint TrainerElemental1 = 7868; // Sarah Tanner
        public const uint TrainerElemental2 = 7869; // Brumn Winterhoof
        public const uint TrainerTribal1 = 7870; // Caryssia Moonhunter
        public const uint TrainerTribal2 = 7871; // Se'Jib

        //Tailoring
        public const uint TrainerSpellfire = 22213; // Gidge Spellweaver
        public const uint TrainerMooncloth = 22208; // Nasmara Moonsong
        public const uint TrainerShadoweave = 22212; // Andrion Darkspinner

        // EngineeringTrinkets
        public const uint Zap = 14742;
        public const uint Jhordy = 14743;
        public const uint Kablam = 21493;
        public const uint Smiles = 21494;
    }

    struct QuestIds
    {
        //Alchemy
        public const uint MasterTransmute = 10899;
        public const uint MasterElixir = 10902;
        public const uint MasterPotion = 10897;
    }

    struct ProfessionConst
    {
        public const string GossipTextBrowseGoods = "I'd like to browse your goods.";
        public const string GossipTextTrain = "Train me!";

        public const uint RepArmor = 46;
        public const uint RepWeapon = 289;
        public const uint RepHammer = 569;
        public const uint RepAxe = 570;
        public const uint RepSword = 571;

        public const uint TrainerIdAlchemy = 122;
        public const uint TrainerIdBlacksmithing = 80;
        public const uint TrainerIdLeatherworking = 103;
        public const uint TrainerIdTailoring = 117;

        public const string TalkMustUnlearnWeapon = "You must forget your weapon type specialty before I can help you. Go to Everlook in Winterspring and seek help there.";

        public const string TalkHammerLearn = "Ah, a seasoned veteran you once were. I know you are capable, you merely need to ask and I shall teach you the way of the hammersmith.";
        public const string TalkAxeLearn = "Ah, a seasoned veteran you once were. I know you are capable, you merely need to ask and I shall teach you the way of the axesmith.";
        public const string TalkSwordLearn = "Ah, a seasoned veteran you once were. I know you are capable, you merely need to ask and I shall teach you the way of the swordsmith.";

        public const string TalkHammerUnlearn = "Forgetting your Hammersmithing skill is not something to do lightly. If you choose to abandon it you will forget all recipes that require Hammersmithing to create!";
        public const string TalkAxeUnlearn = "Forgetting your Axesmithing skill is not something to do lightly. If you choose to abandon it you will forget all recipes that require Axesmithing to create!";
        public const string TalkSwordUnlearn = "Forgetting your Swordsmithing skill is not something to do lightly. If you choose to abandon it you will forget all recipes that require Swordsmithing to create!";

        public const uint GossipSenderLearn = 50;
        public const uint GossipSenderUnlearn = 51;
        public const uint GossipSenderCheck = 52;

        public const string GossipLearnPotion = "Please teach me how to become a Master of Potions, Lauranna";
        public const string GossipUnlearnPotion = "I wish to unlearn Potion Mastery";
        public const string GossipLearnTransmute = "Please teach me how to become a Master of Transmutations, Zarevhi";
        public const string GossipUnlearnTransmute = "I wish to unlearn Transmutation Mastery";
        public const string GossipLearnElixir = "Please teach me how to become a Master of Elixirs, Lorokeem";
        public const string GossipUnlearnElixir = "I wish to unlearn Elixir Mastery";

        public const string BoxUnlearnAlchemySpec = "Do you really want to unlearn your alchemy specialty and lose all associated recipes? \n Cost: ";

        public const string GossipWeaponLearn = "Please teach me how to become a Weaponsmith";
        public const string GossipWeaponUnlearn = "I wish to unlearn the art of Weaponsmithing";
        public const string GossipArmorLearn = "Please teach me how to become a Armorsmith";
        public const string GossipArmorUnlearn = "I wish to unlearn the art of Armorsmithing";

        public const string GossipUnlearnSmithSpec = "I wish to unlearn my blacksmith specialty";
        public const string BoxUnlearnAmorOrWeapon = "Do you really want to unlearn your blacksmith specialty and lose all associated recipes? \n Cost: ";

        public const string GossipLearnHammer = "Please teach me how to become a Hammersmith, Lilith";
        public const string GossipUnlearnHammer = "I wish to unlearn Hammersmithing";
        public const string GossipLearnAxe = "Please teach me how to become a Axesmith, Kilram";
        public const string GossipUnlearnAxe = "I wish to unlearn Axesmithing";
        public const string GossipLearnSword = "Please teach me how to become a Swordsmith, Seril";
        public const string GossipUnlearnSword = "I wish to unlearn Swordsmithing";

        public const string BoxUnlearnWeaponSpec = "Do you really want to unlearn your weaponsmith specialty and lose all associated recipes? \n Cost: ";

        public const string GossipUnlearnDragon = "I wish to unlearn Dragonscale Leatherworking";
        public const string GossipUnlearnElemental = "I wish to unlearn Elemental Leatherworking";
        public const string GossipUnlearnTribal = "I wish to unlearn Tribal Leatherworking";

        public const string BoxUnlearnLeatherSpec = "Do you really want to unlearn your leatherworking specialty and lose all associated recipes? \n Cost: ";

        public const string GossipLearnSpellfire = "Please teach me how to become a Spellcloth tailor";
        public const string GossipUnlearnSpellfire = "I wish to unlearn Spellfire Tailoring";
        public const string GossipLearnMooncloth = "Please teach me how to become a Mooncloth tailor";
        public const string GossipUnlearnMooncloth = "I wish to unlearn Mooncloth Tailoring";
        public const string GossipLearnShadoweave = "Please teach me how to become a Shadoweave tailor";
        public const string GossipUnlearnShadoweave = "I wish to unlearn Shadoweave Tailoring";

        public const string BoxUnlearnTailorSpec = "Do you really want to unlearn your tailoring specialty and lose all associated recipes? \n Cost: ";

        public const uint GossipOptionAlchemy = 0;
        public const uint GossipOptionBlacksmithing = 1;
        public const uint GossipOptionEnchanting = 2;
        public const uint GossipOptionEngineering = 3;
        public const uint GossipOptionHerbalism = 4;
        public const uint GossipOptionInscription = 5;
        public const uint GossipOptionJewelcrafting = 6;
        public const uint GossipOptionLeatherworking = 7;
        public const uint GossipOptionMining = 8;
        public const uint GossipOptionSkinning = 9;
        public const uint GossipOptionTailoring = 10;
        public const uint GossipOptionMulti = 11;

        public const uint GossipMenuHerbalism = 12188;
        public const uint GossipMenuMining = 12189;
        public const uint GossipMenuSkinning = 12190;
        public const uint GossipMenuAlchemy = 12191;
        public const uint GossipMenuBlacksmithing = 12192;
        public const uint GossipMenuEnchanting = 12193;
        public const uint GossipMenuEngineering = 12195;
        public const uint GossipMenuInscription = 12196;
        public const uint GossipMenuJewelcrafting = 12197;
        public const uint GossipMenuLeatherworking = 12198;
        public const uint GossipMenuTailoring = 12199;

        public const string GossipItemZap = "This Dimensional Imploder sounds dangerous! How can I make one?";
        public const string GossipItemJhordy = "I must build a beacon for this marvelous device!";
        public const string GossipItemKablam = "[PH] Unknown";

        public static int DoLearnCost(Player player)                      //tailor, alchemy
        {
            return 200000;
        }

        public static int DoHighUnlearnCost(Player player)                //tailor, alchemy
        {
            return 1500000;
        }

        public static int DoMedUnlearnCost(Player player)                     //blacksmith, leatherwork
        {
            uint level = player.GetLevel();
            if (level < 51)
                return 250000;
            else if (level < 66)
                return 500000;
            else
                return 1000000;
        }

        public static int DoLowUnlearnCost(Player player)                     //blacksmith
        {
            uint level = player.GetLevel();
            if (level < 66)
                return 50000;
            else
                return 100000;
        }

        public static void ProcessCastaction(Player player, Creature creature, uint spellId, uint triggeredSpellId, int Cost)
        {
            if (!(spellId != 0 && player.HasSpell(spellId)) && player.HasEnoughMoney(Cost))
            {
                player.CastSpell(player, triggeredSpellId, true);
                player.ModifyMoney(-Cost);
            }
            else
                player.SendBuyError(BuyResult.NotEnoughtMoney, creature, 0);
            player.CloseGossipMenu();
        }

        static bool EquippedOk(Player player, uint spellId)
        {
            SpellInfo spell = SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            if (spell == null)
                return false;

            foreach (SpellEffectInfo spellEffectInfo in spell.GetEffects())
            {
                uint reqSpell = spellEffectInfo.TriggerSpell;
                if (reqSpell == 0)
                    continue;

                Item item;
                for (byte j = EquipmentSlot.Start; j < EquipmentSlot.End; ++j)
                {
                    item = player.GetItemByPos(InventorySlots.Bag0, j);
                    if (item != null && item.GetTemplate().GetRequiredSpell() == reqSpell)
                    {
                        //player has item equipped that require specialty. Not allow to unlearn, player has to unequip first
                        Log.outDebug(LogFilter.Scripts, $"player attempt to unlearn spell {reqSpell}, but item {item.GetEntry()} is equipped.");
                        return false;
                    }
                }
            }
            return true;
        }

        static void ProfessionUnlearnSpells(Player player, uint type)
        {
            switch (type)
            {
                case SpellIds.UnlearnWeapon:                              // SUnlearnWeapon
                    player.RemoveSpell(36125);                     // Light Earthforged Blade
                    player.RemoveSpell(36128);                     // Light Emberforged Hammer
                    player.RemoveSpell(36126);                     // Light Skyforged Axe
                    break;
                case SpellIds.UnlearnArmor:                               // SUnlearnArmor
                    player.RemoveSpell(36122);                     // Earthforged Leggings
                    player.RemoveSpell(36129);                     // Heavy Earthforged Breastplate
                    player.RemoveSpell(36130);                     // Stormforged Hauberk
                    player.RemoveSpell(34533);                     // Breastplate of Kings
                    player.RemoveSpell(34529);                     // Nether Chain Shirt
                    player.RemoveSpell(34534);                     // Bulwark of Kings
                    player.RemoveSpell(36257);                     // Bulwark of the Ancient Kings
                    player.RemoveSpell(36256);                     // Embrace of the Twisting Nether
                    player.RemoveSpell(34530);                     // Twisting Nether Chain Shirt
                    player.RemoveSpell(36124);                     // Windforged Leggings
                    break;
                case SpellIds.UnlearnHammer:                              // SUnlearnHammer
                    player.RemoveSpell(36262);                     // Dragonstrike
                    player.RemoveSpell(34546);                     // Dragonmaw
                    player.RemoveSpell(34545);                     // Drakefist Hammer
                    player.RemoveSpell(36136);                     // Lavaforged Warhammer
                    player.RemoveSpell(34547);                     // Thunder
                    player.RemoveSpell(34567);                     // Deep Thunder
                    player.RemoveSpell(36263);                     // Stormherald
                    player.RemoveSpell(36137);                     // Great Earthforged Hammer
                    break;
                case SpellIds.UnlearnAxe:                                 // SUnlearnAxe
                    player.RemoveSpell(36260);                     // Wicked Edge of the Planes
                    player.RemoveSpell(34562);                     // Black Planar Edge
                    player.RemoveSpell(34541);                     // The Planar Edge
                    player.RemoveSpell(36134);                     // Stormforged Axe
                    player.RemoveSpell(36135);                     // Skyforged Great Axe
                    player.RemoveSpell(36261);                     // Bloodmoon
                    player.RemoveSpell(34543);                     // Lunar Crescent
                    player.RemoveSpell(34544);                     // Mooncleaver
                    break;
                case SpellIds.UnlearnSword:                               // SUnlearnSword
                    player.RemoveSpell(36258);                     // Blazefury
                    player.RemoveSpell(34537);                     // Blazeguard
                    player.RemoveSpell(34535);                     // Fireguard
                    player.RemoveSpell(36131);                     // Windforged Rapier
                    player.RemoveSpell(36133);                     // Stoneforged Claymore
                    player.RemoveSpell(34538);                     // Lionheart Blade
                    player.RemoveSpell(34540);                     // Lionheart Chapion
                    player.RemoveSpell(36259);                     // Lionheart Executioner
                    break;
                case SpellIds.UnlearnDragon:                              // SUnlearnDragon
                    player.RemoveSpell(36076);                     // Dragonstrike Leggings
                    player.RemoveSpell(36079);                     // Golden Dragonstrike Breastplate
                    player.RemoveSpell(35576);                     // Ebon Netherscale Belt
                    player.RemoveSpell(35577);                     // Ebon Netherscale Bracers
                    player.RemoveSpell(35575);                     // Ebon Netherscale Breastplate
                    player.RemoveSpell(35582);                     // Netherstrike Belt
                    player.RemoveSpell(35584);                     // Netherstrike Bracers
                    player.RemoveSpell(35580);                     // Netherstrike Breastplate
                    break;
                case SpellIds.UnlearnElemental:                           // SUnlearnElemental
                    player.RemoveSpell(36074);                     // Blackstorm Leggings
                    player.RemoveSpell(36077);                     // Primalstorm Breastplate
                    player.RemoveSpell(35590);                     // Primalstrike Belt
                    player.RemoveSpell(35591);                     // Primalstrike Bracers
                    player.RemoveSpell(35589);                     // Primalstrike Vest
                    break;
                case SpellIds.UnlearnTribal:                              // SUnlearnTribal
                    player.RemoveSpell(35585);                     // Windhawk Hauberk
                    player.RemoveSpell(35587);                     // Windhawk Belt
                    player.RemoveSpell(35588);                     // Windhawk Bracers
                    player.RemoveSpell(36075);                     // Wildfeather Leggings
                    player.RemoveSpell(36078);                     // Living Crystal Breastplate
                    break;
                case SpellIds.UnlearnSpellfire:                           // SUnlearnSpellfire
                    player.RemoveSpell(26752);                     // Spellfire Belt
                    player.RemoveSpell(26753);                     // Spellfire Gloves
                    player.RemoveSpell(26754);                     // Spellfire Robe
                    break;
                case SpellIds.UnlearnMooncloth:                           // SUnlearnMooncloth
                    player.RemoveSpell(26760);                     // Primal Mooncloth Belt
                    player.RemoveSpell(26761);                     // Primal Mooncloth Shoulders
                    player.RemoveSpell(26762);                     // Primal Mooncloth Robe
                    break;
                case SpellIds.UnlearnShadoweave:                          // SUnlearnShadoweave
                    player.RemoveSpell(26756);                     // Frozen Shadoweave Shoulders
                    player.RemoveSpell(26757);                     // Frozen Shadoweave Boots
                    player.RemoveSpell(26758);                     // Frozen Shadoweave Robe
                    break;
            }
        }

        public static void ProcessUnlearnAction(Player player, Creature creature, uint spellId, uint alternativeSpellId, int Cost)
        {
            if (EquippedOk(player, spellId))
            {
                if (player.HasEnoughMoney(Cost))
                {
                    player.CastSpell(player, spellId, true);
                    ProfessionUnlearnSpells(player, spellId);
                    player.ModifyMoney(-Cost);
                    if (alternativeSpellId != 0)
                        creature.CastSpell(player, alternativeSpellId, true);
                }
                else
                    player.SendBuyError(BuyResult.NotEnoughtMoney, creature, 0);
            }
            else
                player.SendEquipError(InventoryResult.ClientLockedOut, null, null);
            player.CloseGossipMenu();
        }

    }
    [Script]
    class npc_prof_alchemy : ScriptedAI
    {
        public npc_prof_alchemy(Creature creature) : base(creature) { }

        bool HasAlchemySpell(Player player)
        {
            return player.HasSpell(SpellIds.Transmute) || player.HasSpell(SpellIds.Elixir) || player.HasSpell(SpellIds.Potion);
        }

        public override bool OnGossipHello(Player player)
        {
            if (me.IsQuestGiver())

                if (me.IsVendor())
                    player.AddGossipItem(GossipOptionNpc.Vendor, "I'd like to browse your goods.", eTradeskill.GossipSenderMain, eTradeskill.GossipActionTrade);

            if (me.IsTrainer())
                player.AddGossipItem(GossipOptionNpc.Trainer, "Train me!", eTradeskill.GossipSenderMain, eTradeskill.GossipActionTrain);

            if (player.HasSkill(SkillType.Alchemy) && player.GetBaseSkillValue(SkillType.Alchemy) >= 350 && player.GetLevel() > 67)
            {
                if (player.GetQuestRewardStatus(QuestIds.MasterTransmute) || player.GetQuestRewardStatus(QuestIds.MasterElixir) || player.GetQuestRewardStatus(QuestIds.MasterPotion))
                {
                    switch (me.GetEntry())
                    {
                        case CreatureIds.TrainerTransmute:                                 //Zarevhi
                            if (!HasAlchemySpell(player))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnTransmute, ProfessionConst.GossipSenderLearn, eTradeskill.GossipActionInfoDef + 1);
                            if (player.HasSpell(SpellIds.Transmute))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnTransmute, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 4);
                            break;
                        case CreatureIds.TrainerElixir:                                 //Lorokeem
                            if (!HasAlchemySpell(player))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnElixir, ProfessionConst.GossipSenderLearn, eTradeskill.GossipActionInfoDef + 2);
                            if (player.HasSpell(SpellIds.Elixir))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnElixir, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 5);
                            break;
                        case CreatureIds.TrainerPotion:                                 //Lauranna Thar'well
                            if (!HasAlchemySpell(player))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnPotion, ProfessionConst.GossipSenderLearn, eTradeskill.GossipActionInfoDef + 3);
                            if (player.HasSpell(SpellIds.Potion))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnPotion, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 6);
                            break;
                    }
                }
            }

            player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
            return true;
        }

        void SendActionMenu(Player player, uint action)
        {
            switch (action)
            {
                case eTradeskill.GossipActionTrade:
                    player.GetSession().SendListInventory(me.GetGUID());
                    break;
                case eTradeskill.GossipActionTrain:
                    player.GetSession().SendTrainerList(me, ProfessionConst.TrainerIdAlchemy);
                    break;
                //Learn Alchemy
                case eTradeskill.GossipActionInfoDef + 1:
                    ProfessionConst.ProcessCastaction(player, me, SpellIds.Transmute, SpellIds.LearnTransmute, ProfessionConst.DoLearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 2:
                    ProfessionConst.ProcessCastaction(player, me, SpellIds.Elixir, SpellIds.LearnElixir, ProfessionConst.DoLearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 3:
                    ProfessionConst.ProcessCastaction(player, me, SpellIds.Potion, SpellIds.LearnPotion, ProfessionConst.DoLearnCost(player));
                    break;
                //Unlearn Alchemy
                case eTradeskill.GossipActionInfoDef + 4:
                    ProfessionConst.ProcessCastaction(player, me, 0, SpellIds.UnlearnTransmute, ProfessionConst.DoHighUnlearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 5:
                    ProfessionConst.ProcessCastaction(player, me, 0, SpellIds.UnlearnElixir, ProfessionConst.DoHighUnlearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 6:
                    ProfessionConst.ProcessCastaction(player, me, 0, SpellIds.UnlearnPotion, ProfessionConst.DoHighUnlearnCost(player));
                    break;
            }
        }

        void SendConfirmLearn(Player player, uint action)
        {
            if (action != 0)
            {
                switch (me.GetEntry())
                {
                    case CreatureIds.TrainerTransmute:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnTransmute, ProfessionConst.GossipSenderCheck, action);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerElixir:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnElixir, ProfessionConst.GossipSenderCheck, action);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerPotion:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnPotion, ProfessionConst.GossipSenderCheck, action);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                }
            }
        }

        void SendConfirmUnlearn(Player player, uint action)
        {
            if (action != 0)
            {
                switch (me.GetEntry())
                {
                    case CreatureIds.TrainerTransmute:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnTransmute, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnAlchemySpec, (uint)ProfessionConst.DoHighUnlearnCost(player), false);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerElixir:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnElixir, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnAlchemySpec, (uint)ProfessionConst.DoHighUnlearnCost(player), false);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerPotion:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnPotion, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnAlchemySpec, (uint)ProfessionConst.DoHighUnlearnCost(player), false);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                }
            }
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint sender = player.PlayerTalkClass.GetGossipOptionSender(gossipListId);
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();
            switch (sender)
            {
                case eTradeskill.GossipSenderMain:
                    SendActionMenu(player, action);
                    break;

                case ProfessionConst.GossipSenderLearn:
                    SendConfirmLearn(player, action);
                    break;

                case ProfessionConst.GossipSenderUnlearn:
                    SendConfirmUnlearn(player, action);
                    break;

                case ProfessionConst.GossipSenderCheck:
                    SendActionMenu(player, action);
                    break;
            }
            return true;
        }
    }

    [Script]
    class npc_prof_blacksmith : ScriptedAI
    {
        public npc_prof_blacksmith(Creature creature) : base(creature) { }

        bool HasWeaponSub(Player player)
        {
            return (player.HasSpell(SpellIds.Hammer) || player.HasSpell(SpellIds.Axe) || player.HasSpell(SpellIds.Sword));
        }

        public override bool OnGossipHello(Player player)
        {
            if (me.IsQuestGiver())

                if (me.IsVendor())
                    player.AddGossipItem(GossipOptionNpc.Vendor, ProfessionConst.GossipTextBrowseGoods, eTradeskill.GossipSenderMain, eTradeskill.GossipActionTrade);

            if (me.IsTrainer())
                player.AddGossipItem(GossipOptionNpc.Trainer, ProfessionConst.GossipTextTrain, eTradeskill.GossipSenderMain, eTradeskill.GossipActionTrain);

            uint creatureId = me.GetEntry();
            //Weaponsmith & Armorsmith
            if (player.GetBaseSkillValue(SkillType.Blacksmithing) >= 225)
            {
                switch (creatureId)
                {
                    case CreatureIds.TrainerSmithomni1:
                    case CreatureIds.TrainerSmithomni2:
                        if (!player.HasSpell(SpellIds.Armor) && !player.HasSpell(SpellIds.Weapon) && player.GetReputationRank(ProfessionConst.RepArmor) >= ReputationRank.Friendly)
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipArmorLearn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                        if (!player.HasSpell(SpellIds.Weapon) && !player.HasSpell(SpellIds.Armor) && player.GetReputationRank(ProfessionConst.RepWeapon) >= ReputationRank.Friendly)
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipWeaponLearn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                        break;
                    case CreatureIds.TrainerWeapon1:
                    case CreatureIds.TrainerWeapon2:
                        if (player.HasSpell(SpellIds.Weapon))
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipWeaponUnlearn, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 3);
                        break;
                    case CreatureIds.TrainerArmor1:
                    case CreatureIds.TrainerArmor2:
                        if (player.HasSpell(SpellIds.Armor))
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipArmorUnlearn, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 4);
                        break;
                }
            }
            //Weaponsmith Spec
            if (player.HasSpell(SpellIds.Weapon) && player.GetLevel() > 49 && player.GetBaseSkillValue(SkillType.Blacksmithing) >= 250)
            {
                switch (creatureId)
                {
                    case CreatureIds.TrainerHammer:
                        if (!HasWeaponSub(player))
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnHammer, ProfessionConst.GossipSenderLearn, eTradeskill.GossipActionInfoDef + 5);
                        if (player.HasSpell(SpellIds.Hammer))
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnHammer, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 8);
                        break;
                    case CreatureIds.TrainerAxe:
                        if (!HasWeaponSub(player))
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnAxe, ProfessionConst.GossipSenderLearn, eTradeskill.GossipActionInfoDef + 6);
                        if (player.HasSpell(SpellIds.Axe))
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnAxe, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 9);
                        break;
                    case CreatureIds.TrainerSword:
                        if (!HasWeaponSub(player))
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnSword, ProfessionConst.GossipSenderLearn, eTradeskill.GossipActionInfoDef + 7);
                        if (player.HasSpell(SpellIds.Sword))
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnSword, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 10);
                        break;
                }
            }

            player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
            return true;
        }

        void SendActionMenu(Player player, uint action)
        {
            switch (action)
            {
                case eTradeskill.GossipActionTrade:
                    player.GetSession().SendListInventory(me.GetGUID());
                    break;
                case eTradeskill.GossipActionTrain:
                    player.GetSession().SendTrainerList(me, ProfessionConst.TrainerIdBlacksmithing);
                    break;
                //Learn Armor/Weapon
                case eTradeskill.GossipActionInfoDef + 1:
                    if (!player.HasSpell(SpellIds.Armor))
                    {
                        player.CastSpell(player, SpellIds.LearnArmor, true);
                        //_Creature.CastSpell(player, SRepArmor, true);
                    }
                    player.CloseGossipMenu();
                    break;
                case eTradeskill.GossipActionInfoDef + 2:
                    if (!player.HasSpell(SpellIds.Weapon))
                    {
                        player.CastSpell(player, SpellIds.LearnWeapon, true);
                        //_Creature.CastSpell(player, SRepWeapon, true);
                    }
                    player.CloseGossipMenu();
                    break;
                //Unlearn Armor/Weapon
                case eTradeskill.GossipActionInfoDef + 3:
                    if (HasWeaponSub(player))
                    {
                        //unknown textID (TalkMustUnlearnWeapon)
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                    }
                    else
                        ProfessionConst.ProcessUnlearnAction(player, me, SpellIds.UnlearnWeapon, SpellIds.RepArmor, ProfessionConst.DoLowUnlearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 4:
                    ProfessionConst.ProcessUnlearnAction(player, me, SpellIds.UnlearnArmor, SpellIds.RepWeapon, ProfessionConst.DoLowUnlearnCost(player));
                    break;
                //Learn Hammer/Axe/Sword
                case eTradeskill.GossipActionInfoDef + 5:
                    player.CastSpell(player, SpellIds.LearnHammer, true);
                    player.CloseGossipMenu();
                    break;
                case eTradeskill.GossipActionInfoDef + 6:
                    player.CastSpell(player, SpellIds.LearnAxe, true);
                    player.CloseGossipMenu();
                    break;
                case eTradeskill.GossipActionInfoDef + 7:
                    player.CastSpell(player, SpellIds.LearnSword, true);
                    player.CloseGossipMenu();
                    break;
                //Unlearn Hammer/Axe/Sword
                case eTradeskill.GossipActionInfoDef + 8:
                    ProfessionConst.ProcessUnlearnAction(player, me, SpellIds.UnlearnHammer, 0, ProfessionConst.DoMedUnlearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 9:
                    ProfessionConst.ProcessUnlearnAction(player, me, SpellIds.UnlearnAxe, 0, ProfessionConst.DoMedUnlearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 10:
                    ProfessionConst.ProcessUnlearnAction(player, me, SpellIds.UnlearnSword, 0, ProfessionConst.DoMedUnlearnCost(player));
                    break;
            }
        }

        void SendConfirmLearn(Player player, uint action)
        {
            if (action != 0)
            {
                switch (me.GetEntry())
                {
                    case CreatureIds.TrainerHammer:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnHammer, ProfessionConst.GossipSenderCheck, action);
                        //unknown textID (TalkHammerLearn)
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerAxe:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnAxe, ProfessionConst.GossipSenderCheck, action);
                        //unknown textID (TalkAxeLearn)
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerSword:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnSword, ProfessionConst.GossipSenderCheck, action);
                        //unknown textID (TalkSwordLearn)
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                }
            }
        }

        void SendConfirmUnlearn(Player player, uint action)
        {
            if (action != 0)
            {
                switch (me.GetEntry())
                {
                    case CreatureIds.TrainerWeapon1:
                    case CreatureIds.TrainerWeapon2:
                    case CreatureIds.TrainerArmor1:
                    case CreatureIds.TrainerArmor2:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnSmithSpec, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnAmorOrWeapon, (uint)ProfessionConst.DoLowUnlearnCost(player), false);
                        //unknown textID (TalkUnlearnAxeorweapon)
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;

                    case CreatureIds.TrainerHammer:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnHammer, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnWeaponSpec, (uint)ProfessionConst.DoMedUnlearnCost(player), false);
                        //unknown textID (TalkHammerUnlearn)
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerAxe:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnAxe, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnWeaponSpec, (uint)ProfessionConst.DoMedUnlearnCost(player), false);
                        //unknown textID (TalkAxeUnlearn)
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerSword:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnSword, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnWeaponSpec, (uint)ProfessionConst.DoMedUnlearnCost(player), false);
                        //unknown textID (TalkSwordUnlearn)
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                }
            }
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint sender = player.PlayerTalkClass.GetGossipOptionSender(gossipListId);
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();
            switch (sender)
            {
                case eTradeskill.GossipSenderMain:
                    SendActionMenu(player, action);
                    break;

                case ProfessionConst.GossipSenderLearn:
                    SendConfirmLearn(player, action);
                    break;

                case ProfessionConst.GossipSenderUnlearn:
                    SendConfirmUnlearn(player, action);
                    break;

                case ProfessionConst.GossipSenderCheck:
                    SendActionMenu(player, action);
                    break;
            }
            return true;
        }
    }

    [Script]
    class npc_engineering_tele_trinket : ScriptedAI
    {
        public npc_engineering_tele_trinket(Creature creature) : base(creature) { }

        bool CanLearn(Player player, uint textId, uint altTextId, uint skillValue, uint reqSpellId, uint spellId, ref uint npcTextId)
        {
            bool res = false;
            npcTextId = textId;
            if (player.GetBaseSkillValue(SkillType.Engineering) >= skillValue && player.HasSpell(reqSpellId))
            {
                if (!player.HasSpell(spellId))
                    res = true;
                else
                    npcTextId = altTextId;
            }
            return res;
        }

        public override bool OnGossipHello(Player player)
        {
            uint npcTextId = 0;
            string gossipItem = "";
            bool canLearn = false;

            if (player.HasSkill(SkillType.Engineering))
            {
                switch (me.GetEntry())
                {
                    case CreatureIds.Zap:
                        canLearn = CanLearn(player, 6092, 0, 260, SpellIds.Goblin, SpellIds.ToEverlook, ref npcTextId);
                        if (canLearn)
                            gossipItem = ProfessionConst.GossipItemZap;
                        break;
                    case CreatureIds.Jhordy:
                        canLearn = CanLearn(player, 7251, 7252, 260, SpellIds.Gnomish, SpellIds.ToGadget, ref npcTextId);
                        if (canLearn)
                            gossipItem = ProfessionConst.GossipItemJhordy;
                        break;
                    case CreatureIds.Kablam:
                        canLearn = CanLearn(player, 10365, 0, 350, SpellIds.Goblin, SpellIds.ToArea52, ref npcTextId);
                        if (canLearn)
                            gossipItem = ProfessionConst.GossipItemKablam;
                        break;
                    case CreatureIds.Smiles:
                        canLearn = CanLearn(player, 10363, 0, 350, SpellIds.Gnomish, SpellIds.ToToshley, ref npcTextId);
                        if (canLearn)
                            gossipItem = ProfessionConst.GossipItemKablam;
                        break;
                }
            }

            if (canLearn)
                player.AddGossipItem(GossipOptionNpc.None, gossipItem, me.GetEntry(), eTradeskill.GossipActionInfoDef + 1);

            player.SendGossipMenu(npcTextId != 0 ? npcTextId : player.GetGossipTextId(me), me.GetGUID());
            return true;
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint sender = player.PlayerTalkClass.GetGossipOptionSender(gossipListId);
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();
            if (action == eTradeskill.GossipActionInfoDef + 1)
                player.CloseGossipMenu();

            if (sender != me.GetEntry())
                return true;

            switch (sender)
            {
                case CreatureIds.Zap:
                    player.CastSpell(player, SpellIds.LearnToEverlook, false);
                    break;
                case CreatureIds.Jhordy:
                    player.CastSpell(player, SpellIds.LearnToGadget, false);
                    break;
                case CreatureIds.Kablam:
                    player.CastSpell(player, SpellIds.LearnToArea52, false);
                    break;
                case CreatureIds.Smiles:
                    player.CastSpell(player, SpellIds.LearnToToshley, false);
                    break;
            }

            return true;
        }
    }

    [Script]
    class npc_prof_leather : ScriptedAI
    {
        public npc_prof_leather(Creature creature) : base(creature) { }

        public override bool OnGossipHello(Player player)
        {
            if (me.IsQuestGiver())

                if (me.IsVendor())
                    player.AddGossipItem(GossipOptionNpc.Vendor, ProfessionConst.GossipTextBrowseGoods, eTradeskill.GossipSenderMain, eTradeskill.GossipActionTrade);

            if (me.IsTrainer())
                player.AddGossipItem(GossipOptionNpc.Trainer, ProfessionConst.GossipTextTrain, eTradeskill.GossipSenderMain, eTradeskill.GossipActionTrain);

            if (player.HasSkill(SkillType.Tailoring) && player.GetBaseSkillValue(SkillType.Tailoring) >= 250 && player.GetLevel() > 49)
            {
                switch (me.GetEntry())
                {
                    case CreatureIds.TrainerDragon1:
                    case CreatureIds.TrainerDragon2:
                        if (player.HasSpell(SpellIds.Dragon))
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnDragon, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 1);
                        break;
                    case CreatureIds.TrainerElemental1:
                    case CreatureIds.TrainerElemental2:
                        if (player.HasSpell(SpellIds.Elemental))
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnElemental, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 2);
                        break;
                    case CreatureIds.TrainerTribal1:
                    case CreatureIds.TrainerTribal2:
                        if (player.HasSpell(SpellIds.Tribal))
                            player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnTribal, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 3);
                        break;
                }
            }

            player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
            return true;
        }

        void SendActionMenu(Player player, uint action)
        {
            switch (action)
            {
                case eTradeskill.GossipActionTrade:
                    player.GetSession().SendListInventory(me.GetGUID());
                    break;
                case eTradeskill.GossipActionTrain:
                    player.GetSession().SendTrainerList(me, ProfessionConst.TrainerIdLeatherworking);
                    break;
                //Unlearn Leather
                case eTradeskill.GossipActionInfoDef + 1:
                    ProfessionConst.ProcessUnlearnAction(player, me, SpellIds.UnlearnDragon, 0, ProfessionConst.DoMedUnlearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 2:
                    ProfessionConst.ProcessUnlearnAction(player, me, SpellIds.UnlearnElemental, 0, ProfessionConst.DoMedUnlearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 3:
                    ProfessionConst.ProcessUnlearnAction(player, me, SpellIds.UnlearnTribal, 0, ProfessionConst.DoMedUnlearnCost(player));
                    break;
            }
        }

        void SendConfirmUnlearn(Player player, uint action)
        {
            if (action != 0)
            {
                switch (me.GetEntry())
                {
                    case CreatureIds.TrainerDragon1:
                    case CreatureIds.TrainerDragon2:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnDragon, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnLeatherSpec, (uint)ProfessionConst.DoMedUnlearnCost(player), false);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerElemental1:
                    case CreatureIds.TrainerElemental2:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnElemental, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnLeatherSpec, (uint)ProfessionConst.DoMedUnlearnCost(player), false);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerTribal1:
                    case CreatureIds.TrainerTribal2:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnTribal, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnLeatherSpec, (uint)ProfessionConst.DoMedUnlearnCost(player), false);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                }
            }
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint sender = player.PlayerTalkClass.GetGossipOptionSender(gossipListId);
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();
            switch (sender)
            {
                case eTradeskill.GossipSenderMain:
                    SendActionMenu(player, action);
                    break;

                case ProfessionConst.GossipSenderUnlearn:
                    SendConfirmUnlearn(player, action);
                    break;

                case ProfessionConst.GossipSenderCheck:
                    SendActionMenu(player, action);
                    break;
            }
            return true;
        }
    }

    [Script]
    class npc_prof_tailor : ScriptedAI
    {
        public npc_prof_tailor(Creature creature) : base(creature) { }

        bool HasTailorSpell(Player player)
        {
            return (player.HasSpell(SpellIds.Mooncloth) || player.HasSpell(SpellIds.Shadoweave) || player.HasSpell(SpellIds.Spellfire));
        }

        public override bool OnGossipHello(Player player)
        {
            if (me.IsQuestGiver())

                if (me.IsVendor())
                    player.AddGossipItem(GossipOptionNpc.Vendor, ProfessionConst.GossipTextBrowseGoods, eTradeskill.GossipSenderMain, eTradeskill.GossipActionTrade);

            if (me.IsTrainer())
                player.AddGossipItem(GossipOptionNpc.Trainer, ProfessionConst.GossipTextTrain, eTradeskill.GossipSenderMain, eTradeskill.GossipActionTrain);

            //Tailoring Spec
            if (player.HasSkill(SkillType.Tailoring) && player.GetBaseSkillValue(SkillType.Tailoring) >= 350 && player.GetLevel() > 59)
            {
                if (player.GetQuestRewardStatus(10831) || player.GetQuestRewardStatus(10832) || player.GetQuestRewardStatus(10833))
                {
                    switch (me.GetEntry())
                    {
                        case CreatureIds.TrainerSpellfire:
                            if (!HasTailorSpell(player))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnSpellfire, ProfessionConst.GossipSenderLearn, eTradeskill.GossipActionInfoDef + 1);
                            if (player.HasSpell(SpellIds.Spellfire))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnSpellfire, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 4);
                            break;
                        case CreatureIds.TrainerMooncloth:
                            if (!HasTailorSpell(player))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnMooncloth, ProfessionConst.GossipSenderLearn, eTradeskill.GossipActionInfoDef + 2);
                            if (player.HasSpell(SpellIds.Mooncloth))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnMooncloth, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 5);
                            break;
                        case CreatureIds.TrainerShadoweave:
                            if (!HasTailorSpell(player))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnShadoweave, ProfessionConst.GossipSenderLearn, eTradeskill.GossipActionInfoDef + 3);
                            if (player.HasSpell(SpellIds.Shadoweave))
                                player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnShadoweave, ProfessionConst.GossipSenderUnlearn, eTradeskill.GossipActionInfoDef + 6);
                            break;
                    }
                }
            }

            player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
            return true;
        }

        void SendActionMenu(Player player, uint action)
        {
            switch (action)
            {
                case eTradeskill.GossipActionTrade:
                    player.GetSession().SendListInventory(me.GetGUID());
                    break;
                case eTradeskill.GossipActionTrain:
                    player.GetSession().SendTrainerList(me, ProfessionConst.TrainerIdTailoring);
                    break;
                //Learn Tailor
                case eTradeskill.GossipActionInfoDef + 1:
                    ProfessionConst.ProcessCastaction(player, me, SpellIds.Spellfire, SpellIds.LearnSpellfire, ProfessionConst.DoLearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 2:
                    ProfessionConst.ProcessCastaction(player, me, SpellIds.Mooncloth, SpellIds.LearnMooncloth, ProfessionConst.DoLearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 3:
                    ProfessionConst.ProcessCastaction(player, me, SpellIds.Shadoweave, SpellIds.LearnShadoweave, ProfessionConst.DoLearnCost(player));
                    break;
                //Unlearn Tailor
                case eTradeskill.GossipActionInfoDef + 4:
                    ProfessionConst.ProcessUnlearnAction(player, me, SpellIds.UnlearnSpellfire, 0, ProfessionConst.DoHighUnlearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 5:
                    ProfessionConst.ProcessUnlearnAction(player, me, SpellIds.UnlearnMooncloth, 0, ProfessionConst.DoHighUnlearnCost(player));
                    break;
                case eTradeskill.GossipActionInfoDef + 6:
                    ProfessionConst.ProcessUnlearnAction(player, me, SpellIds.UnlearnShadoweave, 0, ProfessionConst.DoHighUnlearnCost(player));
                    break;
            }
        }

        void SendConfirmLearn(Player player, uint action)
        {
            if (action != 0)
            {
                switch (me.GetEntry())
                {
                    case CreatureIds.TrainerSpellfire:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnSpellfire, ProfessionConst.GossipSenderCheck, action);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerMooncloth:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnMooncloth, ProfessionConst.GossipSenderCheck, action);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerShadoweave:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipLearnShadoweave, ProfessionConst.GossipSenderCheck, action);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                }
            }
        }

        void SendConfirmUnlearn(Player player, uint action)
        {
            if (action != 0)
            {
                switch (me.GetEntry())
                {
                    case CreatureIds.TrainerSpellfire:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnSpellfire, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnTailorSpec, (uint)ProfessionConst.DoHighUnlearnCost(player), false);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerMooncloth:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnMooncloth, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnTailorSpec, (uint)ProfessionConst.DoHighUnlearnCost(player), false);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                    case CreatureIds.TrainerShadoweave:
                        player.AddGossipItem(GossipOptionNpc.None, ProfessionConst.GossipUnlearnShadoweave, ProfessionConst.GossipSenderCheck, action, ProfessionConst.BoxUnlearnTailorSpec, (uint)ProfessionConst.DoHighUnlearnCost(player), false);
                        //unknown textID ()
                        player.SendGossipMenu(player.GetGossipTextId(me), me.GetGUID());
                        break;
                }
            }
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint sender = player.PlayerTalkClass.GetGossipOptionSender(gossipListId);
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();
            switch (sender)
            {
                case eTradeskill.GossipSenderMain:
                    SendActionMenu(player, action);
                    break;

                case ProfessionConst.GossipSenderLearn:
                    SendConfirmLearn(player, action);
                    break;

                case ProfessionConst.GossipSenderUnlearn:
                    SendConfirmUnlearn(player, action);
                    break;

                case ProfessionConst.GossipSenderCheck:
                    SendActionMenu(player, action);
                    break;
            }
            return true;
        }
    }
}