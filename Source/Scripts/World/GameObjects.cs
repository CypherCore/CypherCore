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
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.World
{
    struct GameobjectConst
    {
        //CatFigurine
        public const uint SpellSummonGhostSaber = 5968;

        //GildedBrazier
        public const uint NpcStillblade = 17716;
        public const uint QuestTheFirstTrial = 9678;

        //EthereumPrison
        public const uint SpellRepLc = 39456;
        public const uint SpellRepShat = 39457;
        public const uint SpellRepCe = 39460;
        public const uint SpellRepCon = 39474;
        public const uint SpellRepKt = 39475;
        public const uint SpellRepSpor = 39476;
        public static uint[] NpcPrisonEntry =
        {
            22810, 22811, 22812, 22813, 22814, 22815,               //Good Guys
            20783, 20784, 20785, 20786, 20788, 20789, 20790         //Bad Guys
        };

        //Ethereum Stasis
        public static uint[] NpcStasisEntry =
        {
            22825, 20888, 22827, 22826, 22828
        };

        //ResoniteCask
        public const uint NpcGoggeroc = 11920;

        //Sacredfireoflife
        public const uint NpcArikara = 10882;

        //Shrineofthebirds
        public const uint NpcHawkGuard = 22992;
        public const uint NpcEagleGuard = 22993;
        public const uint NpcFalconGuard = 22994;
        public const uint GoShrineHawk = 185551;
        public const uint GoShrineEagle = 185547;
        public const uint GoShrineFalcon = 185553;

        //Southfury
        public const uint NpcRizzle = 23002;
        public const uint SpellBlackjack = 39865; //Stuns Player
        public const uint SpellSummonRizzle = 39866;

        //Dalarancrystal
        public const uint QuestLearnLeaveReturn = 12790;
        public const uint QuestTeleCrystalFlag = 12845;
        public const string GoTeleToDalaranCrystalFailed = "This Teleport Crystal Cannot Be Used Until The Teleport Crystal In Dalaran Has Been Used At Least Once.";

        //Felcrystalforge
        public const uint SpellCreate1FlaskOfBeast = 40964;
        public const uint SpellCreate5FlaskOfBeast = 40965;
        public const uint GossipFelCrystalforgeText = 31000;
        public const uint GossipFelCrystalforgeItemTextReturn = 31001;
        public const string GossipFelCrystalforgeItem1 = "Purchase 1 Unstable Flask Of The Beast For The Cost Of 10 Apexis Shards";
        public const string GossipFelCrystalforgeItem5 = "Purchase 5 Unstable Flask Of The Beast For The Cost Of 50 Apexis Shards";
        public const string GossipFelCrystalforgeItemReturn = "Use The Fel Crystalforge To Make Another Purchase.";

        //Bashircrystalforge
        public const uint SpellCreate1FlaskOfSorcerer = 40968;
        public const uint SpellCreate5FlaskOfSorcerer = 40970;
        public const uint GossipBashirCrystalforgeText = 31100;
        public const uint GossipBashirCrystalforgeItemTextReturn = 31101;
        public const string GossipBashirCrystalforgeItem1 = "Purchase 1 Unstable Flask Of The Sorcerer For The Cost Of 10 Apexis Shards";
        public const string GossipBashirCrystalforgeItem5 = "Purchase 5 Unstable Flask Of The Sorcerer For The Cost Of 50 Apexis Shards";
        public const string GossipBashirCrystalforgeItemReturn = "Use The Bashir Crystalforge To Make Another Purchase.";

        //Matrixpunchograph
        public const uint ItemWhitePunchCard = 9279;
        public const uint ItemYellowPunchCard = 9280;
        public const uint ItemBluePunchCard = 9282;
        public const uint ItemRedPunchCard = 9281;
        public const uint ItemPrismaticPunchCard = 9316;
        public const uint SpellYellowPunchCard = 11512;
        public const uint SpellBluePunchCard = 11525;
        public const uint SpellRedPunchCard = 11528;
        public const uint SpellPrismaticPunchCard = 11545;
        public const uint MatrixPunchograph3005A = 142345;
        public const uint MatrixPunchograph3005B = 142475;
        public const uint MatrixPunchograph3005C = 142476;
        public const uint MatrixPunchograph3005D = 142696;

        //Scourgecage
        public const uint NpcScourgePrisoner = 25610;

        //Arcaneprison
        public const uint QuestPrisonBreak = 11587;
        public const uint SpellArcanePrisonerKillCredit = 45456;

        //Bloodfilledorb
        public const uint NpcZelemar = 17830;

        //Jotunheimcage
        public const uint NpcEbonBladePrisonerHuman = 30186;
        public const uint NpcEbonBladePrisonerNe = 30194;
        public const uint NpcEbonBladePrisonerTroll = 30196;
        public const uint NpcEbonBladePrisonerOrc = 30195;

        public const uint SpellSummonBladeKnightH = 56207;
        public const uint SpellSummonBladeKnightNe = 56209;
        public const uint SpellSummonBladeKnightOrc = 56212;
        public const uint SpellSummonBladeKnightTroll = 56214;

        //Tabletheka
        public const uint GossipTableTheka = 1653;
        public const uint QuestSpiderGold = 2936;

        //Inconspicuouslandmark
        public const uint SpellSummonPiratesTreasureAndTriggerMob = 11462;
        public const uint ItemCuergosKey = 9275;

        //Prisonersofwyrmskull
        public const uint QuestPrisonersOfWyrmskull = 11255;
        public const uint NpcPrisonerPriest = 24086;
        public const uint NpcPrisonerMage = 24088;
        public const uint NpcPrisonerWarrior = 24089;
        public const uint NpcPrisonerPaladin = 24090;
        public const uint NpcCapturedValgardePrisonerProxy = 24124;

        //Tadpoles
        public const uint QuestOhNoesTheTadpoles = 11560;
        public const uint NpcWinterfinTadpole = 25201;

        //Amberpineouthouse
        public const uint ItemAnderholsSliderCider = 37247;
        public const uint NpcOuthouseBunny = 27326;
        public const uint QuestDoingYourDuty = 12227;
        public const uint SpellIndisposed = 53017;
        public const uint SpellIndisposedIii = 48341;
        public const uint SpellCreateAmberseeds = 48330;
        public const uint GossipOuthouseInuse = 12775;
        public const uint GossipOuthouseVacant = 12779;

        public const string GossipUseOuthouse = "Use The Outhouse.";
        public const string GoAnderholsSliderCiderNotFound = "Quest Item Anderhol'S Slider Cider Not Found.";

        //Hives
        public const uint QuestHiveInTheTower = 9544;
        public const uint NpcHiveAmbusher = 13301;

        //Missingfriends
        public const uint QuestMissingFriends = 10852;
        public const uint NpcCaptiveChild = 22314;
        public const uint SayFree0 = 0;

        //Thecleansing
        public const uint QuestTheCleansingHorde = 11317;
        public const uint QuestTheCleansingAlliance = 11322;
        public const uint SpellCleansingSoul = 43351;
        public const uint SpellRecentMeditation = 61720;

        //Midsummerbonfire
        public const uint StampOutBonfireQuestComplete = 45458;

        //MidsummerPoleRibbon
        public const uint SpellPoleDance = 29726;
        public const uint SpellBlueFireRing = 46842;
        public const uint NpcPoleRibbonBunny = 17066;

        //Toy Train Set
        public const uint SpellToyTrainPulse = 61551;

        //BrewfestMusic
        public const uint EventBrewfestdwarf01 = 11810; // 1.35 Min
        public const uint EventBrewfestdwarf02 = 11812; // 1.55 Min 
        public const uint EventBrewfestdwarf03 = 11813; // 0.23 Min
        public const uint EventBrewfestgoblin01 = 11811; // 1.08 Min
        public const uint EventBrewfestgoblin02 = 11814; // 1.33 Min
        public const uint EventBrewfestgoblin03 = 11815; // 0.28 Min

        // These Are In Seconds
        //Brewfestmusictime
        public const uint EventBrewfestdwarf01Time = 95000;
        public const uint EventBrewfestdwarf02Time = 155000;
        public const uint EventBrewfestdwarf03Time = 23000;
        public const uint EventBrewfestgoblin01Time = 68000;
        public const uint EventBrewfestgoblin02Time = 93000;
        public const uint EventBrewfestgoblin03Time = 28000;

        //Brewfestmusicareas
        public const uint Silvermoon = 3430; // Horde
        public const uint Undercity = 1497;
        public const uint Orgrimmar1 = 1296;
        public const uint Orgrimmar2 = 14;
        public const uint Thunderbluff = 1638;
        public const uint Ironforge1 = 809; // Alliance
        public const uint Ironforge2 = 1;
        public const uint Stormwind = 12;
        public const uint Exodar = 3557;
        public const uint Darnassus = 1657;
        public const uint Shattrath = 3703; // General

        //Brewfestmusicevents
        public const uint EventBmSelectMusic = 1;
        public const uint EventBmStartMusic = 2;
    }

    [Script]
    class go_cat_figurine : GameObjectScript
    {
        public go_cat_figurine() : base("go_cat_figurine") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            player.CastSpell(player, GameobjectConst.SpellSummonGhostSaber, true);
            return false;
        }
    }

    [Script] //go_barov_journal
    class go_barov_journal : GameObjectScript
    {
        public go_barov_journal() : base("go_barov_journal") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (player.HasSkill(SkillType.Tailoring) && player.GetBaseSkillValue(SkillType.Tailoring) >= 280 && !player.HasSpell(26086))
                player.CastSpell(player, 26095, false);

            return true;
        }
    }

    [Script] //go_gilded_brazier (Paladin First Trail quest (9678))
    class go_gilded_brazier : GameObjectScript
    {
        public go_gilded_brazier() : base("go_gilded_brazier") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (go.GetGoType() == GameObjectTypes.Goober)
            {
                if (player.GetQuestStatus(GameobjectConst.QuestTheFirstTrial) == QuestStatus.Incomplete)
                {
                    Creature Stillblade = player.SummonCreature(GameobjectConst.NpcStillblade, 8106.11f, -7542.06f, 151.775f, 3.02598f, TempSummonType.DeadDespawn, 60000);
                    if (Stillblade)
                        Stillblade.GetAI().AttackStart(player);
                }
            }
            return true;
        }
    }

    [Script] //go_orb_of_command
    class go_orb_of_command : GameObjectScript
    {
        public go_orb_of_command() : base("go_orb_of_command") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (player.GetQuestRewardStatus(7761))
                player.CastSpell(player, 23460, true);

            return true;
        }
    }

    [Script] //go_tablet_of_madness
    class go_tablet_of_madness : GameObjectScript
    {
        public go_tablet_of_madness() : base("go_tablet_of_madness") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (player.HasSkill(SkillType.Alchemy) && player.GetSkillValue(SkillType.Alchemy) >= 300 && !player.HasSpell(24266))
                player.CastSpell(player, 24267, false);

            return true;
        }
    }

    [Script] //go_tablet_of_the_seven
    class go_tablet_of_the_seven : GameObjectScript
    {
        public go_tablet_of_the_seven() : base("go_tablet_of_the_seven") { }

        // @todo use gossip option ("Transcript the Tablet") instead, if Trinity adds support.
        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (go.GetGoType() != GameObjectTypes.QuestGiver)
                return true;

            if (player.GetQuestStatus(4296) == QuestStatus.Incomplete)
                player.CastSpell(player, 15065, false);

            return true;
        }
    }

    [Script] //go_jump_a_tron
    class go_jump_a_tron : GameObjectScript
    {
        public go_jump_a_tron() : base("go_jump_a_tron") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (player.GetQuestStatus(10111) == QuestStatus.Incomplete)
                player.CastSpell(player, 33382, true);

            return true;
        }
    }

    [Script] //go_ethereum_prison
    class go_ethereum_prison : GameObjectScript
    {
        public go_ethereum_prison() : base("go_ethereum_prison") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            go.UseDoorOrButton();
            int Random = (int)(RandomHelper.Rand32() % (GameobjectConst.NpcPrisonEntry.Length / sizeof(uint)));
            Creature creature = player.SummonCreature(GameobjectConst.NpcPrisonEntry[Random], go.GetPositionX(), go.GetPositionY(), go.GetPositionZ(), go.GetAngle(player), TempSummonType.TimedDespawnOOC, 30000);
            if (creature)
            {
                if (!creature.IsHostileTo(player))
                {
                    FactionTemplateRecord pFaction = creature.GetFactionTemplateEntry();
                    if (pFaction != null)
                    {
                        uint Spell = 0;

                        switch (pFaction.Faction)
                        {
                            case 1011: Spell = GameobjectConst.SpellRepLc; break;
                            case 935: Spell = GameobjectConst.SpellRepShat; break;
                            case 942: Spell = GameobjectConst.SpellRepCe; break;
                            case 933: Spell = GameobjectConst.SpellRepCon; break;
                            case 989: Spell = GameobjectConst.SpellRepKt; break;
                            case 970: Spell = GameobjectConst.SpellRepSpor; break;
                        }

                        if (Spell != 0)
                            creature.CastSpell(player, Spell, false);
                        else
                            Log.outError(LogFilter.Scripts, "go_ethereum_prison summoned Creature (entry {0}) but faction ({1}) are not expected by script.", creature.GetEntry(), creature.getFaction());
                    }
                }
            }

            return false;
        }
    }

    [Script] //go_ethereum_stasis
    class go_ethereum_stasis : GameObjectScript
    {
        public go_ethereum_stasis() : base("go_ethereum_stasis") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            go.UseDoorOrButton();
            int Random = (int)(RandomHelper.Rand32() % GameobjectConst.NpcStasisEntry.Length / sizeof(uint));

            player.SummonCreature(GameobjectConst.NpcStasisEntry[Random], go.GetPositionX(), go.GetPositionY(), go.GetPositionZ(), go.GetAngle(player), TempSummonType.TimedDespawnOOC, 30000);

            return false;
        }
    }

    [Script] //go_resonite_cask
    class go_resonite_cask : GameObjectScript
    {
        public go_resonite_cask() : base("go_resonite_cask") { }

        public override bool OnGossipHello(Player Player, GameObject go)
        {
            if (go.GetGoType() == GameObjectTypes.Goober)
                go.SummonCreature(GameobjectConst.NpcGoggeroc, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOOC, 300000);

            return false;
        }
    }

    [Script] //go_sacred_fire_of_life
    class go_sacred_fire_of_life : GameObjectScript
    {
        public go_sacred_fire_of_life() : base("go_sacred_fire_of_life") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (go.GetGoType() == GameObjectTypes.Goober)
                player.SummonCreature(GameobjectConst.NpcArikara, -5008.338f, -2118.894f, 83.657f, 0.874f, TempSummonType.TimedDespawnOOC, 30000);

            return true;
        }
    }

    [Script] //go_shrine_of_the_birds
    class go_shrine_of_the_birds : GameObjectScript
    {
        public go_shrine_of_the_birds() : base("go_shrine_of_the_birds") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            uint BirdEntry = 0;

            float fX, fY, fZ;
            go.GetClosePoint(out fX, out fY, out fZ, go.GetObjectSize(), SharedConst.InteractionDistance);

            switch (go.GetEntry())
            {
                case GameobjectConst.GoShrineHawk:
                    BirdEntry = GameobjectConst.NpcHawkGuard;
                    break;
                case GameobjectConst.GoShrineEagle:
                    BirdEntry = GameobjectConst.NpcEagleGuard;
                    break;
                case GameobjectConst.GoShrineFalcon:
                    BirdEntry = GameobjectConst.NpcFalconGuard;
                    break;
            }

            if (BirdEntry != 0)
                player.SummonCreature(BirdEntry, fX, fY, fZ, go.GetOrientation(), TempSummonType.TimedDespawnOOC, 60000);

            return false;
        }
    }

    [Script] //go_southfury_moonstone
    class go_southfury_moonstone : GameObjectScript
    {
        public go_southfury_moonstone() : base("go_southfury_moonstone") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            //implicitTarget=48 not implemented as of writing this code, and manual summon may be just ok for our purpose
            //player.CastSpell(player, SPELL_SUMMON_RIZZLE, false);

            Creature creature = player.SummonCreature(GameobjectConst.NpcRizzle, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.DeadDespawn, 0);
            if (creature)
                creature.CastSpell(player, GameobjectConst.SpellBlackjack, false);

            return false;
        }
    }

    [Script] //go_tele_to_dalaran_crystal
    class go_tele_to_dalaran_crystal : GameObjectScript
    {
        public go_tele_to_dalaran_crystal() : base("go_tele_to_dalaran_crystal") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (player.GetQuestRewardStatus(GameobjectConst.QuestTeleCrystalFlag))
                return false;

            player.GetSession().SendNotification(GameobjectConst.GoTeleToDalaranCrystalFailed);

            return true;
        }
    }

    [Script] //go_tele_to_violet_stand
    class go_tele_to_violet_stand : GameObjectScript
    {
        public go_tele_to_violet_stand() : base("go_tele_to_violet_stand") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (player.GetQuestRewardStatus(GameobjectConst.QuestLearnLeaveReturn) || player.GetQuestStatus(GameobjectConst.QuestLearnLeaveReturn) == QuestStatus.Incomplete)
                return false;

            return true;
        }
    }

    [Script] //go_fel_crystalforge
    class go_fel_crystalforge : GameObjectScript
    {
        public go_fel_crystalforge() : base("go_fel_crystalforge") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (go.GetGoType() == GameObjectTypes.QuestGiver) /* != GAMEOBJECT_TYPE_QUESTGIVER) */
                player.PrepareQuestMenu(go.GetGUID()); /* return true*/

            player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
            player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);

            player.SEND_GOSSIP_MENU(GameobjectConst.GossipFelCrystalforgeText, go.GetGUID());

            return true;
        }

        public override bool OnGossipSelect(Player player, GameObject go, uint sender, uint action)
        {
            player.PlayerTalkClass.ClearMenus();
            switch (action)
            {
                case eTradeskill.GossipActionInfoDef:
                    player.CastSpell(player, GameobjectConst.SpellCreate1FlaskOfBeast, false);
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.SEND_GOSSIP_MENU(GameobjectConst.GossipFelCrystalforgeItemTextReturn, go.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 1:
                    player.CastSpell(player, GameobjectConst.SpellCreate5FlaskOfBeast, false);
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.SEND_GOSSIP_MENU(GameobjectConst.GossipFelCrystalforgeItemTextReturn, go.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 2:
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                    player.SEND_GOSSIP_MENU(GameobjectConst.GossipFelCrystalforgeText, go.GetGUID());
                    break;
            }
            return true;
        }
    }

    [Script] //go_bashir_crystalforge
    class go_bashir_crystalforge : GameObjectScript
    {
        public go_bashir_crystalforge() : base("go_bashir_crystalforge") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (go.GetGoType() == GameObjectTypes.QuestGiver) /* != GAMEOBJECT_TYPE_QUESTGIVER) */
                player.PrepareQuestMenu(go.GetGUID()); /* return true*/

            player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
            player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);

            player.SEND_GOSSIP_MENU(GameobjectConst.GossipBashirCrystalforgeText, go.GetGUID());

            return true;
        }

        public override bool OnGossipSelect(Player player, GameObject go, uint sender, uint action)
        {
            player.PlayerTalkClass.ClearMenus();
            switch (action)
            {
                case eTradeskill.GossipActionInfoDef:
                    player.CastSpell(player, GameobjectConst.SpellCreate1FlaskOfSorcerer, false);
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.SEND_GOSSIP_MENU(GameobjectConst.GossipBashirCrystalforgeItemTextReturn, go.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 1:
                    player.CastSpell(player, GameobjectConst.SpellCreate5FlaskOfBeast, false);
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.SEND_GOSSIP_MENU(GameobjectConst.GossipBashirCrystalforgeItemTextReturn, go.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 2:
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                    player.SEND_GOSSIP_MENU(GameobjectConst.GossipBashirCrystalforgeText, go.GetGUID());
                    break;
            }
            return true;
        }
    }

    [Script] //matrix_punchograph
    class go_matrix_punchograph : GameObjectScript
    {
        public go_matrix_punchograph() : base("go_matrix_punchograph") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            switch (go.GetEntry())
            {
                case GameobjectConst.MatrixPunchograph3005A:
                    if (player.HasItemCount(GameobjectConst.ItemWhitePunchCard))
                    {
                        player.DestroyItemCount(GameobjectConst.ItemWhitePunchCard, 1, true);
                        player.CastSpell(player, GameobjectConst.SpellYellowPunchCard, true);
                    }
                    break;
                case GameobjectConst.MatrixPunchograph3005B:
                    if (player.HasItemCount(GameobjectConst.ItemYellowPunchCard))
                    {
                        player.DestroyItemCount(GameobjectConst.ItemYellowPunchCard, 1, true);
                        player.CastSpell(player, GameobjectConst.SpellBluePunchCard, true);
                    }
                    break;
                case GameobjectConst.MatrixPunchograph3005C:
                    if (player.HasItemCount(GameobjectConst.ItemBluePunchCard))
                    {
                        player.DestroyItemCount(GameobjectConst.ItemBluePunchCard, 1, true);
                        player.CastSpell(player, GameobjectConst.SpellRedPunchCard, true);
                    }
                    break;
                case GameobjectConst.MatrixPunchograph3005D:
                    if (player.HasItemCount(GameobjectConst.ItemRedPunchCard))
                    {
                        player.DestroyItemCount(GameobjectConst.ItemRedPunchCard, 1, true);
                        player.CastSpell(player, GameobjectConst.SpellPrismaticPunchCard, true);
                    }
                    break;
                default:
                    break;
            }
            return false;
        }
    }

    [Script] //go_scourge_cage
    class go_scourge_cage : GameObjectScript
    {
        public go_scourge_cage() : base("go_scourge_cage") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            go.UseDoorOrButton();
            Creature pNearestPrisoner = go.FindNearestCreature(GameobjectConst.NpcScourgePrisoner, 5.0f, true);
            if (pNearestPrisoner)
            {
                player.KilledMonsterCredit(GameobjectConst.NpcScourgePrisoner, pNearestPrisoner.GetGUID());
                pNearestPrisoner.DisappearAndDie();
            }

            return true;
        }
    }

    [Script] //go_arcane_prison
    class go_arcane_prison : GameObjectScript
    {
        public go_arcane_prison() : base("go_arcane_prison") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (player.GetQuestStatus(GameobjectConst.QuestPrisonBreak) == QuestStatus.Incomplete)
            {
                go.SummonCreature(25318, 3485.089844f, 6115.7422188f, 70.966812f, 0, TempSummonType.TimedDespawn, 60000);
                player.CastSpell(player, GameobjectConst.SpellArcanePrisonerKillCredit, true);
                return true;
            }
            return false;
        }
    }

    [Script] //go_blood_filled_orb
    class go_blood_filled_orb : GameObjectScript
    {
        public go_blood_filled_orb() : base("go_blood_filled_orb") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (go.GetGoType() == GameObjectTypes.Goober)
                player.SummonCreature(GameobjectConst.NpcZelemar, -369.746f, 166.759f, -21.50f, 5.235f, TempSummonType.TimedDespawnOOC, 30000);

            return true;
        }
    }

    [Script] //go_jotunheim_cage
    class go_jotunheim_cage : GameObjectScript
    {
        public go_jotunheim_cage() : base("go_jotunheim_cage") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            go.UseDoorOrButton();
            Creature pPrisoner = go.FindNearestCreature(GameobjectConst.NpcEbonBladePrisonerHuman, 5.0f, true);
            if (!pPrisoner)
            {
                pPrisoner = go.FindNearestCreature(GameobjectConst.NpcEbonBladePrisonerTroll, 5.0f, true);
                if (!pPrisoner)
                {
                    pPrisoner = go.FindNearestCreature(GameobjectConst.NpcEbonBladePrisonerOrc, 5.0f, true);
                    if (!pPrisoner)
                        pPrisoner = go.FindNearestCreature(GameobjectConst.NpcEbonBladePrisonerNe, 5.0f, true);
                }
            }
            if (!pPrisoner || !pPrisoner.IsAlive())
                return false;

            pPrisoner.DisappearAndDie();
            player.KilledMonsterCredit(GameobjectConst.NpcEbonBladePrisonerHuman);
            switch (pPrisoner.GetEntry())
            {
                case GameobjectConst.NpcEbonBladePrisonerHuman:
                    player.CastSpell(player, GameobjectConst.SpellSummonBladeKnightH, true);
                    break;
                case GameobjectConst.NpcEbonBladePrisonerNe:
                    player.CastSpell(player, GameobjectConst.SpellSummonBladeKnightNe, true);
                    break;
                case GameobjectConst.NpcEbonBladePrisonerTroll:
                    player.CastSpell(player, GameobjectConst.SpellSummonBladeKnightTroll, true);
                    break;
                case GameobjectConst.NpcEbonBladePrisonerOrc:
                    player.CastSpell(player, GameobjectConst.SpellSummonBladeKnightOrc, true);
                    break;
            }
            return true;
        }
    }

    [Script]
    class go_table_theka : GameObjectScript
    {
        public go_table_theka() : base("go_table_theka") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (player.GetQuestStatus(GameobjectConst.QuestSpiderGold) == QuestStatus.Incomplete)
                player.AreaExploredOrEventHappens(GameobjectConst.QuestSpiderGold);

            player.SEND_GOSSIP_MENU(GameobjectConst.GossipTableTheka, go.GetGUID());

            return true;
        }
    }

    [Script] //go_inconspicuous_landmark
    class go_inconspicuous_landmark : GameObjectScript
    {
        public go_inconspicuous_landmark() : base("go_inconspicuous_landmark") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            if (player.HasItemCount(GameobjectConst.ItemCuergosKey))
                return false;

            player.CastSpell(player, GameobjectConst.SpellSummonPiratesTreasureAndTriggerMob, true);

            return true;
        }
    }

    [Script] //go_soulwell
    class go_soulwell : GameObjectScript
    {
        public go_soulwell() : base("go_soulwell") { }

        class go_soulwellAI : GameObjectAI
        {
            public go_soulwellAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool isUse)
            {
                if (!isUse)
                    return true;

                Unit owner = go.GetOwner();
                if (!owner || !owner.IsTypeId(TypeId.Player) || !player.IsInSameRaidWith(owner.ToPlayer()))
                    return true;
                return false;
            }
        }
    }

    [Script] //go_dragonflayer_cage
    class go_dragonflayer_cage : GameObjectScript
    {
        public go_dragonflayer_cage() : base("go_dragonflayer_cage") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            go.UseDoorOrButton();
            if (player.GetQuestStatus(GameobjectConst.QuestPrisonersOfWyrmskull) != QuestStatus.Incomplete)
                return true;

            Creature pPrisoner = go.FindNearestCreature(GameobjectConst.NpcPrisonerPriest, 2.0f);
            if (!pPrisoner)
            {
                pPrisoner = go.FindNearestCreature(GameobjectConst.NpcPrisonerMage, 2.0f);
                if (!pPrisoner)
                {
                    pPrisoner = go.FindNearestCreature(GameobjectConst.NpcPrisonerWarrior, 2.0f);
                    if (!pPrisoner)
                        pPrisoner = go.FindNearestCreature(GameobjectConst.NpcPrisonerPaladin, 2.0f);
                }
            }

            if (!pPrisoner || !pPrisoner.IsAlive())
                return true;

            // @todo prisoner should help player for a short period of time
            player.KilledMonsterCredit(GameobjectConst.NpcCapturedValgardePrisonerProxy);
            pPrisoner.DespawnOrUnsummon();
            return true;
        }
    }

    [Script] //go_tadpole_cage
    class go_tadpole_cage : GameObjectScript
    {
        public go_tadpole_cage() : base("go_tadpole_cage") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            go.UseDoorOrButton();
            if (player.GetQuestStatus(GameobjectConst.QuestOhNoesTheTadpoles) == QuestStatus.Incomplete)
            {
                Creature pTadpole = go.FindNearestCreature(GameobjectConst.NpcWinterfinTadpole, 1.0f);
                if (pTadpole)
                {
                    pTadpole.DisappearAndDie();
                    player.KilledMonsterCredit(GameobjectConst.NpcWinterfinTadpole);
                    //FIX: Summon minion tadpole
                }
            }
            return true;
        }
    }

    [Script] //go_amberpine_outhouse
    class go_amberpine_outhouse : GameObjectScript
    {
        public go_amberpine_outhouse() : base("go_amberpine_outhouse") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            QuestStatus status = player.GetQuestStatus(GameobjectConst.QuestDoingYourDuty);
            if (status == QuestStatus.Incomplete || status == QuestStatus.Complete || status == QuestStatus.Rewarded)
            {
                player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipUseOuthouse, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                player.SEND_GOSSIP_MENU(GameobjectConst.GossipOuthouseVacant, go.GetGUID());
            }
            else
                player.SEND_GOSSIP_MENU(GameobjectConst.GossipOuthouseInuse, go.GetGUID());

            return true;
        }

        public override bool OnGossipSelect(Player player, GameObject go, uint sender, uint action)
        {
            player.PlayerTalkClass.ClearMenus();
            if (action == eTradeskill.GossipActionInfoDef + 1)
            {
                player.CLOSE_GOSSIP_MENU();
                Creature target = ScriptedAI.GetClosestCreatureWithEntry(player, GameobjectConst.NpcOuthouseBunny, 3.0f);
                if (target)
                {
                    target.GetAI().SetData(1, (uint)player.GetGender());
                    go.CastSpell(target, GameobjectConst.SpellIndisposedIii);
                }
                go.CastSpell(player, GameobjectConst.SpellIndisposed);
                if (player.HasItemCount(GameobjectConst.ItemAnderholsSliderCider))
                    go.CastSpell(player, GameobjectConst.SpellCreateAmberseeds);
                return true;
            }
            else
            {
                player.CLOSE_GOSSIP_MENU();
                player.GetSession().SendNotification(GameobjectConst.GoAnderholsSliderCiderNotFound);
                return false;
            }
        }
    }

    [Script] //go_hive_pod
    class go_hive_pod : GameObjectScript
    {
        public go_hive_pod() : base("go_hive_pod") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            player.SendLoot(go.GetGUID(), LootType.Corpse);
            go.SummonCreature(GameobjectConst.NpcHiveAmbusher, go.GetPositionX() + 1, go.GetPositionY(), go.GetPositionZ(), go.GetAngle(player), TempSummonType.TimedOrDeadDespawn, 60000);
            go.SummonCreature(GameobjectConst.NpcHiveAmbusher, go.GetPositionX(), go.GetPositionY() + 1, go.GetPositionZ(), go.GetAngle(player), TempSummonType.TimedOrDeadDespawn, 60000);
            return true;
        }
    }

    [Script]
    class go_massive_seaforium_charge : GameObjectScript
    {
        public go_massive_seaforium_charge() : base("go_massive_seaforium_charge") { }

        public override bool OnGossipHello(Player Player, GameObject go)
        {
            go.SetLootState(LootState.JustDeactivated);
            return true;
        }
    }

    [Script] //go_veil_skith_cage
    class go_veil_skith_cage : GameObjectScript
    {
        public go_veil_skith_cage() : base("go_veil_skith_cage") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            go.UseDoorOrButton();
            if (player.GetQuestStatus(GameobjectConst.QuestMissingFriends) == QuestStatus.Incomplete)
            {
                List<Creature> childrenList = new List<Creature>();
                go.GetCreatureListWithEntryInGrid(childrenList, GameobjectConst.NpcCaptiveChild, SharedConst.InteractionDistance);
                foreach (var creature in childrenList)
                {
                    player.KilledMonsterCredit(GameobjectConst.NpcCaptiveChild, creature.GetGUID());
                    creature.DespawnOrUnsummon(5000);
                    creature.GetMotionMaster().MovePoint(1, go.GetPositionX() + 5, go.GetPositionY(), go.GetPositionZ());
                    creature.GetAI().Talk(GameobjectConst.SayFree0);
                    creature.GetMotionMaster().Clear();
                }
            }
            return false;
        }
    }

    [Script] //go_frostblade_shrine
    class go_frostblade_shrine : GameObjectScript
    {
        public go_frostblade_shrine() : base("go_frostblade_shrine") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            go.UseDoorOrButton(10);
            if (!player.HasAura(GameobjectConst.SpellRecentMeditation))
                if (player.GetQuestStatus(GameobjectConst.QuestTheCleansingHorde) == QuestStatus.Incomplete || player.GetQuestStatus(GameobjectConst.QuestTheCleansingAlliance) == QuestStatus.Incomplete)
                {
                    player.CastSpell(player, GameobjectConst.SpellCleansingSoul);
                    player.SetStandState(UnitStandStateType.Sit);
                }
            return true;
        }
    }

    [Script] //go_midsummer_bonfire
    class go_midsummer_bonfire : GameObjectScript
    {
        public go_midsummer_bonfire() : base("go_midsummer_bonfire") { }

        public override bool OnGossipSelect(Player player, GameObject go, uint sender, uint action)
        {
            player.CastSpell(player, GameobjectConst.StampOutBonfireQuestComplete, true);
            player.CLOSE_GOSSIP_MENU();
            return false;
        }
    }

    [Script]
    class go_midsummer_ribbon_pole : GameObjectScript
    {
        public go_midsummer_ribbon_pole() : base("go_midsummer_ribbon_pole") { }

        public override bool OnGossipHello(Player player, GameObject go)
        {
            Creature creature = go.FindNearestCreature(GameobjectConst.NpcPoleRibbonBunny, 10.0f);
            if (creature)
            {
                creature.GetAI().DoAction(0);
                player.CastSpell(creature, GameobjectConst.SpellPoleDance, true);
            }
            return true;
        }
    }

    [Script]
    class go_toy_train_set : GameObjectScript
    {
        public go_toy_train_set() : base("go_toy_train_set") { }

        class go_toy_train_setAI : GameObjectAI
        {
            public go_toy_train_setAI(GameObject go) : base(go)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
                {
                    go.CastSpell(null, GameobjectConst.SpellToyTrainPulse, true);
                    task.Repeat(TimeSpan.FromSeconds(6));
                });
            }

            public override void UpdateAI(uint diff)
            {
                _scheduler.Update(diff);
            }

            // triggered on wrecker'd
            public override void DoAction(int action)
            {
                go.Delete();
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_toy_train_setAI(go);
        }
    }

    [Script]
    class go_brewfest_music : GameObjectScript
    {
        public go_brewfest_music() : base("go_brewfest_music") { }

        class go_brewfest_musicAI : GameObjectAI
        {
            public go_brewfest_musicAI(GameObject go) : base(go)
            {
                _events.ScheduleEvent(GameobjectConst.EventBmSelectMusic, 1000);
                _events.ScheduleEvent(GameobjectConst.EventBmStartMusic, 2000);
            }

            public override void UpdateAI(uint diff)
            {
                _events.Update(diff);

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case GameobjectConst.EventBmSelectMusic:
                            if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest)) // Check if Brewfest is active
                                break;
                            rnd = RandomHelper.URand(0, 2); // Select random music sample
                            _events.ScheduleEvent(GameobjectConst.EventBmSelectMusic, musicTime); // Select new song music after play time is over
                            break;
                        case GameobjectConst.EventBmStartMusic:
                            if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest)) // Check if Brewfest is active
                                break;
                            // Check if gob is correct area, play music, set time of music
                            if (go.GetAreaId() == GameobjectConst.Silvermoon || go.GetAreaId() == GameobjectConst.Undercity || go.GetAreaId() == GameobjectConst.Orgrimmar1 || go.GetAreaId() == GameobjectConst.Orgrimmar2 || go.GetAreaId() == GameobjectConst.Thunderbluff || go.GetAreaId() == GameobjectConst.Shattrath)
                            {
                                if (rnd == 0)
                                {
                                    go.PlayDirectMusic(GameobjectConst.EventBrewfestgoblin01);
                                    musicTime = GameobjectConst.EventBrewfestgoblin01Time;
                                }
                                else if (rnd == 1)
                                {
                                    go.PlayDirectMusic(GameobjectConst.EventBrewfestgoblin02);
                                    musicTime = GameobjectConst.EventBrewfestgoblin02Time;
                                }
                                else
                                {
                                    go.PlayDirectMusic(GameobjectConst.EventBrewfestgoblin03);
                                    musicTime = GameobjectConst.EventBrewfestgoblin03Time;
                                }
                            }
                            if (go.GetAreaId() == GameobjectConst.Ironforge1 || go.GetAreaId() == GameobjectConst.Ironforge2 || go.GetAreaId() == GameobjectConst.Stormwind || go.GetAreaId() == GameobjectConst.Exodar || go.GetAreaId() == GameobjectConst.Darnassus || go.GetAreaId() == GameobjectConst.Shattrath)
                            {
                                if (rnd == 0)
                                {
                                    go.PlayDirectMusic(GameobjectConst.EventBrewfestdwarf01);
                                    musicTime = GameobjectConst.EventBrewfestdwarf01Time;
                                }
                                else if (rnd == 1)
                                {
                                    go.PlayDirectMusic(GameobjectConst.EventBrewfestdwarf02);
                                    musicTime = GameobjectConst.EventBrewfestdwarf02Time;
                                }
                                else
                                {
                                    go.PlayDirectMusic(GameobjectConst.EventBrewfestdwarf03);
                                    musicTime = GameobjectConst.EventBrewfestdwarf03Time;
                                }
                            }
                            _events.ScheduleEvent(GameobjectConst.EventBmStartMusic, 5000); // Every 5 second's SMSG_PLAY_MUSIC packet (PlayDirectMusic) is pushed to the client
                            break;
                        default:
                            break;
                    }
                });
            }

            uint rnd;
            uint musicTime = 1000;
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_brewfest_musicAI(go);
        }
    }

    [Script]
    class go_midsummer_music : GameObjectScript
    {
        public go_midsummer_music() : base("go_midsummer_music") { }

        class go_midsummer_musicAI : GameObjectAI
        {
            public go_midsummer_musicAI(GameObject go) : base(go)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
                {
                    if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.FireFestival))
                        return;

                    var playersNearby = go.GetPlayerListInGrid(go.GetMap().GetVisibilityRange());
                    foreach (var player in playersNearby)
                    {
                        if (player.GetTeam() == Team.Horde)
                            go.PlayDirectMusic(12325, player);
                        else
                            go.PlayDirectMusic(12319, player);
                    }

                    task.Repeat(TimeSpan.FromSeconds(5)); // Every 5 second's SMSG_PLAY_MUSIC packet (PlayDirectMusic) is pushed to the client (sniffed value)
                });
            }

            public override void UpdateAI(uint diff)
            {
                _scheduler.Update(diff);
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_midsummer_musicAI(go);
        }
    }

    [Script]
    class go_darkmoon_faire_music : GameObjectScript
    {
        public go_darkmoon_faire_music() : base("go_darkmoon_faire_music") { }

        class go_darkmoon_faire_musicAI : GameObjectAI
        {
            public go_darkmoon_faire_musicAI(GameObject go) : base(go)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
                {
                    if (Global.GameEventMgr.IsHolidayActive(HolidayIds.DarkmoonFaireElwynn) || !Global.GameEventMgr.IsHolidayActive(HolidayIds.DarkmoonFaireThunder) || !Global.GameEventMgr.IsHolidayActive(HolidayIds.DarkmoonFaireShattrath))
                    {
                        go.PlayDirectMusic(8440);
                        task.Repeat(TimeSpan.FromSeconds(5));  // Every 5 second's SMSG_PLAY_MUSIC packet (PlayDirectMusic) is pushed to the client (sniffed value)
                    }
                });
            }

            public override void UpdateAI(uint diff)
            {
                _scheduler.Update(diff);
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_darkmoon_faire_musicAI(go);
        }
    }

    [Script]
    class go_pirate_day_music : GameObjectScript
    {
        public go_pirate_day_music() : base("go_pirate_day_music") { }

        class go_pirate_day_musicAI : GameObjectAI
        {
            public go_pirate_day_musicAI(GameObject go) : base(go)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
                {
                    if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.PiratesDay))
                        return;
                    go.PlayDirectMusic(12845);
                    task.Repeat(TimeSpan.FromSeconds(5));  // Every 5 second's SMSG_PLAY_MUSIC packet (PlayDirectMusic) is pushed to the client (sniffed value)
                });
            }

            public override void UpdateAI(uint diff)
            {
                _scheduler.Update(diff);
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_pirate_day_musicAI(go);
        }
    }
}
