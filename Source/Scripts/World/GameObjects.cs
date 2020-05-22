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

        //Bells
        //BellHourlySoundFX
        public const uint BellTollhorde = 6595; // Horde
        public const uint BellTolltribal = 6675;
        public const uint BellTollalliance = 6594; // Alliance
        public const uint BellTollnightelf = 6674;
        public const uint BellTolldwarfgnome = 7234;
        public const uint Belltollkharazhan = 9154; // Kharazhan

        //Bellhourlysoundareas
        public const uint UndercityArea = 1497;
        public const uint Ironforge1Area = 809;
        public const uint Ironforge2Area = 1;
        public const uint DarnassusArea = 1657;
        public const uint TeldrassilZone = 141;
        public const uint KharazhanMapid = 532;

        //Bellhourlyobjects
        public const uint GoHordeBell = 175885;
        public const uint GoAllianceBell = 176573;
        public const uint GoKharazhanBell = 182064;

        //Bellhourlymisc
        public const uint GameEventHourlyBells = 73;
        public const uint EventRingBell = 1;
    }

    [Script]
    class go_cat_figurine : GameObjectScript
    {
        public go_cat_figurine() : base("go_cat_figurine") { }

        class go_cat_figurineAI : GameObjectAI
        {
            public go_cat_figurineAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                player.CastSpell(player, GameobjectConst.SpellSummonGhostSaber, true);
                return false;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_cat_figurineAI(go);
        }
    }

    [Script] //go_barov_journal
    class go_barov_journal : GameObjectScript
    {
        public go_barov_journal() : base("go_barov_journal") { }

        class go_barov_journalAI : GameObjectAI
        {
            public go_barov_journalAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (player.HasSkill(SkillType.Tailoring) && player.GetBaseSkillValue(SkillType.Tailoring) >= 280 && !player.HasSpell(26086))
                    player.CastSpell(player, 26095, false);

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_barov_journalAI(go);
        }
    }

    [Script] //go_gilded_brazier (Paladin First Trail quest (9678))
    class go_gilded_brazier : GameObjectScript
    {
        public go_gilded_brazier() : base("go_gilded_brazier") { }

        class go_gilded_brazierAI : GameObjectAI
        {
            public go_gilded_brazierAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (me.GetGoType() == GameObjectTypes.Goober)
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

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_gilded_brazierAI(go);
        }
    }

    [Script] //go_orb_of_command
    class go_orb_of_command : GameObjectScript
    {
        public go_orb_of_command() : base("go_orb_of_command") { }

        class go_orb_of_commandAI : GameObjectAI
        {
            public go_orb_of_commandAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (player.GetQuestRewardStatus(7761))
                    player.CastSpell(player, 23460, true);

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_orb_of_commandAI(go);
        }
    }

    [Script] //go_tablet_of_madness
    class go_tablet_of_madness : GameObjectScript
    {
        public go_tablet_of_madness() : base("go_tablet_of_madness") { }

        class go_tablet_of_madnessAI : GameObjectAI
        {
            public go_tablet_of_madnessAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (player.HasSkill(SkillType.Alchemy) && player.GetSkillValue(SkillType.Alchemy) >= 300 && !player.HasSpell(24266))
                    player.CastSpell(player, 24267, false);

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_tablet_of_madnessAI(go);
        }
    }

    [Script] //go_tablet_of_the_seven
    class go_tablet_of_the_seven : GameObjectScript
    {
        public go_tablet_of_the_seven() : base("go_tablet_of_the_seven") { }

        class go_tablet_of_the_sevenAI : GameObjectAI
        {
            public go_tablet_of_the_sevenAI(GameObject go) : base(go) { }

            // @todo use gossip option ("Transcript the Tablet") instead, if Trinity adds support.
            public override bool GossipHello(Player player, bool reportUse)
            {
                if (me.GetGoType() != GameObjectTypes.QuestGiver)
                    return true;

                if (player.GetQuestStatus(4296) == QuestStatus.Incomplete)
                    player.CastSpell(player, 15065, false);

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_tablet_of_the_sevenAI(go);
        }
    }

    [Script] //go_jump_a_tron
    class go_jump_a_tron : GameObjectScript
    {
        public go_jump_a_tron() : base("go_jump_a_tron") { }

        class go_jump_a_tronAI : GameObjectAI
        {
            public go_jump_a_tronAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (player.GetQuestStatus(10111) == QuestStatus.Incomplete)
                    player.CastSpell(player, 33382, true);

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_jump_a_tronAI(go);
        }
    }

    [Script] //go_ethereum_prison
    class go_ethereum_prison : GameObjectScript
    {
        public go_ethereum_prison() : base("go_ethereum_prison") { }

        class go_ethereum_prisonAI : GameObjectAI
        {
            public go_ethereum_prisonAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                me.UseDoorOrButton();
                int Random = (int)(RandomHelper.Rand32() % (GameobjectConst.NpcPrisonEntry.Length / sizeof(uint)));
                Creature creature = player.SummonCreature(GameobjectConst.NpcPrisonEntry[Random], me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetAngle(player), TempSummonType.TimedDespawnOOC, 30000);
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
                                Log.outError(LogFilter.Scripts, "go_ethereum_prison summoned Creature (entry {0}) but faction ({1}) are not expected by script.", creature.GetEntry(), creature.GetFaction());
                        }
                    }
                }

                return false;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_ethereum_prisonAI(go);
        }
    }

    [Script] //go_ethereum_stasis
    class go_ethereum_stasis : GameObjectScript
    {
        public go_ethereum_stasis() : base("go_ethereum_stasis") { }

        class go_ethereum_stasisAI : GameObjectAI
        {
            public go_ethereum_stasisAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                me.UseDoorOrButton();
                int Random = (int)(RandomHelper.Rand32() % GameobjectConst.NpcStasisEntry.Length / sizeof(uint));

                player.SummonCreature(GameobjectConst.NpcStasisEntry[Random], me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetAngle(player), TempSummonType.TimedDespawnOOC, 30000);

                return false;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_ethereum_stasisAI(go);
        }
    }

    [Script] //go_resonite_cask
    class go_resonite_cask : GameObjectScript
    {
        public go_resonite_cask() : base("go_resonite_cask") { }

        class go_resonite_caskAI : GameObjectAI
        {
            public go_resonite_caskAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player Player, bool reportUse)
            {
                if (me.GetGoType() == GameObjectTypes.Goober)
                    me.SummonCreature(GameobjectConst.NpcGoggeroc, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOOC, 300000);

                return false;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_resonite_caskAI(go);
        }
    }

    [Script] //go_sacred_fire_of_life
    class go_sacred_fire_of_life : GameObjectScript
    {
        public go_sacred_fire_of_life() : base("go_sacred_fire_of_life") { }

        class go_sacred_fire_of_lifeAI : GameObjectAI
        {
            public go_sacred_fire_of_lifeAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (me.GetGoType() == GameObjectTypes.Goober)
                    player.SummonCreature(GameobjectConst.NpcArikara, -5008.338f, -2118.894f, 83.657f, 0.874f, TempSummonType.TimedDespawnOOC, 30000);

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_sacred_fire_of_lifeAI(go);
        }
    }

    [Script] //go_shrine_of_the_birds
    class go_shrine_of_the_birds : GameObjectScript
    {
        public go_shrine_of_the_birds() : base("go_shrine_of_the_birds") { }

        class go_shrine_of_the_birdsAI : GameObjectAI
        {
            public go_shrine_of_the_birdsAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                uint BirdEntry = 0;

                float fX, fY, fZ;
                me.GetClosePoint(out fX, out fY, out fZ, me.GetCombatReach(), SharedConst.InteractionDistance);

                switch (me.GetEntry())
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
                    player.SummonCreature(BirdEntry, fX, fY, fZ, me.GetOrientation(), TempSummonType.TimedDespawnOOC, 60000);

                return false;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_shrine_of_the_birdsAI(go);
        }
    }

    [Script] //go_southfury_moonstone
    class go_southfury_moonstone : GameObjectScript
    {
        public go_southfury_moonstone() : base("go_southfury_moonstone") { }

        class go_southfury_moonstoneAI : GameObjectAI
        {
            public go_southfury_moonstoneAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                //implicitTarget=48 not implemented as of writing this code, and manual summon may be just ok for our purpose
                //player.CastSpell(player, SPELL_SUMMON_RIZZLE, false);

                Creature creature = player.SummonCreature(GameobjectConst.NpcRizzle, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.DeadDespawn, 0);
                if (creature)
                    creature.CastSpell(player, GameobjectConst.SpellBlackjack, false);

                return false;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_southfury_moonstoneAI(go);
        }
    }

    [Script] //go_tele_to_dalaran_crystal
    class go_tele_to_dalaran_crystal : GameObjectScript
    {
        public go_tele_to_dalaran_crystal() : base("go_tele_to_dalaran_crystal") { }

        class go_tele_to_dalaran_crystalAI : GameObjectAI
        {
            public go_tele_to_dalaran_crystalAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (player.GetQuestRewardStatus(GameobjectConst.QuestTeleCrystalFlag))
                    return false;

                player.GetSession().SendNotification(GameobjectConst.GoTeleToDalaranCrystalFailed);

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_tele_to_dalaran_crystalAI(go);
        }
    }

    [Script] //go_tele_to_violet_stand
    class go_tele_to_violet_stand : GameObjectScript
    {
        public go_tele_to_violet_stand() : base("go_tele_to_violet_stand") { }

        class go_tele_to_violet_standAI : GameObjectAI
        {
            public go_tele_to_violet_standAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (player.GetQuestRewardStatus(GameobjectConst.QuestLearnLeaveReturn) || player.GetQuestStatus(GameobjectConst.QuestLearnLeaveReturn) == QuestStatus.Incomplete)
                    return false;

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_tele_to_violet_standAI(go);
        }
    }

    [Script] //go_fel_crystalforge
    class go_fel_crystalforge : GameObjectScript
    {
        public go_fel_crystalforge() : base("go_fel_crystalforge") { }

        class go_fel_crystalforgeAI : GameObjectAI
        {
            public go_fel_crystalforgeAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (me.GetGoType() == GameObjectTypes.QuestGiver) /* != GAMEOBJECT_TYPE_QUESTGIVER) */
                    player.PrepareQuestMenu(me.GetGUID()); /* return true*/

                player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
                player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);

                player.SEND_GOSSIP_MENU(GameobjectConst.GossipFelCrystalforgeText, me.GetGUID());

                return true;
            }

            public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
            {
                uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
                player.PlayerTalkClass.ClearMenus();
                switch (action)
                {
                    case eTradeskill.GossipActionInfoDef:
                        player.CastSpell(player, GameobjectConst.SpellCreate1FlaskOfBeast, false);
                        player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                        player.SEND_GOSSIP_MENU(GameobjectConst.GossipFelCrystalforgeItemTextReturn, me.GetGUID());
                        break;
                    case eTradeskill.GossipActionInfoDef + 1:
                        player.CastSpell(player, GameobjectConst.SpellCreate5FlaskOfBeast, false);
                        player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                        player.SEND_GOSSIP_MENU(GameobjectConst.GossipFelCrystalforgeItemTextReturn, me.GetGUID());
                        break;
                    case eTradeskill.GossipActionInfoDef + 2:
                        player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
                        player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipFelCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                        player.SEND_GOSSIP_MENU(GameobjectConst.GossipFelCrystalforgeText, me.GetGUID());
                        break;
                }
                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_fel_crystalforgeAI(go);
        }
    }

    [Script] //go_bashir_crystalforge
    class go_bashir_crystalforge : GameObjectScript
    {
        public go_bashir_crystalforge() : base("go_bashir_crystalforge") { }

        class go_bashir_crystalforgeAI : GameObjectAI
        {
            public go_bashir_crystalforgeAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (me.GetGoType() == GameObjectTypes.QuestGiver) /* != GAMEOBJECT_TYPE_QUESTGIVER) */
                    player.PrepareQuestMenu(me.GetGUID()); /* return true*/

                player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
                player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);

                player.SEND_GOSSIP_MENU(GameobjectConst.GossipBashirCrystalforgeText, me.GetGUID());

                return true;
            }

            public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
            {
                uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
                player.PlayerTalkClass.ClearMenus();
                switch (action)
                {
                    case eTradeskill.GossipActionInfoDef:
                        player.CastSpell(player, GameobjectConst.SpellCreate1FlaskOfSorcerer, false);
                        player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                        player.SEND_GOSSIP_MENU(GameobjectConst.GossipBashirCrystalforgeItemTextReturn, me.GetGUID());
                        break;
                    case eTradeskill.GossipActionInfoDef + 1:
                        player.CastSpell(player, GameobjectConst.SpellCreate5FlaskOfBeast, false);
                        player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                        player.SEND_GOSSIP_MENU(GameobjectConst.GossipBashirCrystalforgeItemTextReturn, me.GetGUID());
                        break;
                    case eTradeskill.GossipActionInfoDef + 2:
                        player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
                        player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipBashirCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                        player.SEND_GOSSIP_MENU(GameobjectConst.GossipBashirCrystalforgeText, me.GetGUID());
                        break;
                }
                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_bashir_crystalforgeAI(go);
        }
    }

    [Script] //matrix_punchograph
    class go_matrix_punchograph : GameObjectScript
    {
        public go_matrix_punchograph() : base("go_matrix_punchograph") { }

        class go_matrix_punchographAI : GameObjectAI
        {
            public go_matrix_punchographAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                switch (me.GetEntry())
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

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_matrix_punchographAI(go);
        }
    }

    [Script] //go_scourge_cage
    class go_scourge_cage : GameObjectScript
    {
        public go_scourge_cage() : base("go_scourge_cage") { }

        class go_scourge_cageAI : GameObjectAI
        {
            public go_scourge_cageAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                me.UseDoorOrButton();
                Creature pNearestPrisoner = me.FindNearestCreature(GameobjectConst.NpcScourgePrisoner, 5.0f, true);
                if (pNearestPrisoner)
                {
                    player.KilledMonsterCredit(GameobjectConst.NpcScourgePrisoner, pNearestPrisoner.GetGUID());
                    pNearestPrisoner.DisappearAndDie();
                }

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_scourge_cageAI(go);
        }
    }

    [Script] //go_arcane_prison
    class go_arcane_prison : GameObjectScript
    {
        public go_arcane_prison() : base("go_arcane_prison") { }

        class go_arcane_prisonAI : GameObjectAI
        {
            public go_arcane_prisonAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (player.GetQuestStatus(GameobjectConst.QuestPrisonBreak) == QuestStatus.Incomplete)
                {
                    me.SummonCreature(25318, 3485.089844f, 6115.7422188f, 70.966812f, 0, TempSummonType.TimedDespawn, 60000);
                    player.CastSpell(player, GameobjectConst.SpellArcanePrisonerKillCredit, true);
                    return true;
                }
                return false;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_arcane_prisonAI(go);
        }
    }

    [Script] //go_blood_filled_orb
    class go_blood_filled_orb : GameObjectScript
    {
        public go_blood_filled_orb() : base("go_blood_filled_orb") { }

        class go_blood_filled_orbAI : GameObjectAI
        {
            public go_blood_filled_orbAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (me.GetGoType() == GameObjectTypes.Goober)
                    player.SummonCreature(GameobjectConst.NpcZelemar, -369.746f, 166.759f, -21.50f, 5.235f, TempSummonType.TimedDespawnOOC, 30000);

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_blood_filled_orbAI(go);
        }
    }

    [Script] //go_jotunheim_cage
    class go_jotunheim_cage : GameObjectScript
    {
        public go_jotunheim_cage() : base("go_jotunheim_cage") { }

        class go_jotunheim_cageAI : GameObjectAI
        {
            public go_jotunheim_cageAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                me.UseDoorOrButton();
                Creature pPrisoner = me.FindNearestCreature(GameobjectConst.NpcEbonBladePrisonerHuman, 5.0f, true);
                if (!pPrisoner)
                {
                    pPrisoner = me.FindNearestCreature(GameobjectConst.NpcEbonBladePrisonerTroll, 5.0f, true);
                    if (!pPrisoner)
                    {
                        pPrisoner = me.FindNearestCreature(GameobjectConst.NpcEbonBladePrisonerOrc, 5.0f, true);
                        if (!pPrisoner)
                            pPrisoner = me.FindNearestCreature(GameobjectConst.NpcEbonBladePrisonerNe, 5.0f, true);
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

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_jotunheim_cageAI(go);
        }
    }

    [Script]
    class go_table_theka : GameObjectScript
    {
        public go_table_theka() : base("go_table_theka") { }

        class go_table_thekaAI : GameObjectAI
        {
            public go_table_thekaAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (player.GetQuestStatus(GameobjectConst.QuestSpiderGold) == QuestStatus.Incomplete)
                    player.AreaExploredOrEventHappens(GameobjectConst.QuestSpiderGold);

                player.SEND_GOSSIP_MENU(GameobjectConst.GossipTableTheka, me.GetGUID());

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_table_thekaAI(go);
        }
    }

    [Script] //go_inconspicuous_landmark
    class go_inconspicuous_landmark : GameObjectScript
    {
        public go_inconspicuous_landmark() : base("go_inconspicuous_landmark") { }

        class go_inconspicuous_landmarkAI : GameObjectAI
        {
            public go_inconspicuous_landmarkAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (player.HasItemCount(GameobjectConst.ItemCuergosKey))
                    return false;

                player.CastSpell(player, GameobjectConst.SpellSummonPiratesTreasureAndTriggerMob, true);

                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_inconspicuous_landmarkAI(go);
        }
    }

    [Script] //go_soulwell
    class go_soulwell : GameObjectScript
    {
        public go_soulwell() : base("go_soulwell") { }

        class go_soulwellAI : GameObjectAI
        {
            public go_soulwellAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                if (!reportUse)
                    return true;

                Unit owner = me.GetOwner();
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

        class go_dragonflayer_cageAI : GameObjectAI
        {
            public go_dragonflayer_cageAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                me.UseDoorOrButton();
                if (player.GetQuestStatus(GameobjectConst.QuestPrisonersOfWyrmskull) != QuestStatus.Incomplete)
                    return true;

                Creature pPrisoner = me.FindNearestCreature(GameobjectConst.NpcPrisonerPriest, 2.0f);
                if (!pPrisoner)
                {
                    pPrisoner = me.FindNearestCreature(GameobjectConst.NpcPrisonerMage, 2.0f);
                    if (!pPrisoner)
                    {
                        pPrisoner = me.FindNearestCreature(GameobjectConst.NpcPrisonerWarrior, 2.0f);
                        if (!pPrisoner)
                            pPrisoner = me.FindNearestCreature(GameobjectConst.NpcPrisonerPaladin, 2.0f);
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

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_dragonflayer_cageAI(go);
        }
    }

    [Script] //go_tadpole_cage
    class go_tadpole_cage : GameObjectScript
    {
        public go_tadpole_cage() : base("go_tadpole_cage") { }

        class go_tadpole_cageAI : GameObjectAI
        {
            public go_tadpole_cageAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                me.UseDoorOrButton();
                if (player.GetQuestStatus(GameobjectConst.QuestOhNoesTheTadpoles) == QuestStatus.Incomplete)
                {
                    Creature pTadpole = me.FindNearestCreature(GameobjectConst.NpcWinterfinTadpole, 1.0f);
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

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_tadpole_cageAI(go);
        }
    }

    [Script] //go_amberpine_outhouse
    class go_amberpine_outhouse : GameObjectScript
    {
        public go_amberpine_outhouse() : base("go_amberpine_outhouse") { }

        class go_amberpine_outhouseAI : GameObjectAI
        {
            public go_amberpine_outhouseAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                QuestStatus status = player.GetQuestStatus(GameobjectConst.QuestDoingYourDuty);
                if (status == QuestStatus.Incomplete || status == QuestStatus.Complete || status == QuestStatus.Rewarded)
                {
                    player.ADD_GOSSIP_ITEM(GossipOptionIcon.Chat, GameobjectConst.GossipUseOuthouse, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                    player.SEND_GOSSIP_MENU(GameobjectConst.GossipOuthouseVacant, me.GetGUID());
                }
                else
                    player.SEND_GOSSIP_MENU(GameobjectConst.GossipOuthouseInuse, me.GetGUID());

                return true;
            }

            public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
            {
                uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
                player.PlayerTalkClass.ClearMenus();
                if (action == eTradeskill.GossipActionInfoDef + 1)
                {
                    player.CLOSE_GOSSIP_MENU();
                    Creature target = ScriptedAI.GetClosestCreatureWithEntry(player, GameobjectConst.NpcOuthouseBunny, 3.0f);
                    if (target)
                    {
                        target.GetAI().SetData(1, (uint)player.GetGender());
                        me.CastSpell(target, GameobjectConst.SpellIndisposedIii);
                    }
                    me.CastSpell(player, GameobjectConst.SpellIndisposed);
                    if (player.HasItemCount(GameobjectConst.ItemAnderholsSliderCider))
                        me.CastSpell(player, GameobjectConst.SpellCreateAmberseeds);
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

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_amberpine_outhouseAI(go);
        }
    }

    [Script] //go_hive_pod
    class go_hive_pod : GameObjectScript
    {
        public go_hive_pod() : base("go_hive_pod") { }

        class go_hive_podAI : GameObjectAI
        {
            public go_hive_podAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                player.SendLoot(me.GetGUID(), LootType.Corpse);
                me.SummonCreature(GameobjectConst.NpcHiveAmbusher, me.GetPositionX() + 1, me.GetPositionY(), me.GetPositionZ(), me.GetAngle(player), TempSummonType.TimedOrDeadDespawn, 60000);
                me.SummonCreature(GameobjectConst.NpcHiveAmbusher, me.GetPositionX(), me.GetPositionY() + 1, me.GetPositionZ(), me.GetAngle(player), TempSummonType.TimedOrDeadDespawn, 60000);
                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_hive_podAI(go);
        }
    }

    [Script]
    class go_massive_seaforium_charge : GameObjectScript
    {
        public go_massive_seaforium_charge() : base("go_massive_seaforium_charge") { }

        class go_massive_seaforium_chargeAI : GameObjectAI
        {
            public go_massive_seaforium_chargeAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player Player, bool reportUse)
            {
                me.SetLootState(LootState.JustDeactivated);
                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_massive_seaforium_chargeAI(go);
        }
    }

    [Script] //go_veil_skith_cage
    class go_veil_skith_cage : GameObjectScript
    {
        public go_veil_skith_cage() : base("go_veil_skith_cage") { }

        class go_veil_skith_cageAI : GameObjectAI
        {
            public go_veil_skith_cageAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                me.UseDoorOrButton();
                if (player.GetQuestStatus(GameobjectConst.QuestMissingFriends) == QuestStatus.Incomplete)
                {
                    List<Creature> childrenList = new List<Creature>();
                    me.GetCreatureListWithEntryInGrid(childrenList, GameobjectConst.NpcCaptiveChild, SharedConst.InteractionDistance);
                    foreach (var creature in childrenList)
                    {
                        player.KilledMonsterCredit(GameobjectConst.NpcCaptiveChild, creature.GetGUID());
                        creature.DespawnOrUnsummon(5000);
                        creature.GetMotionMaster().MovePoint(1, me.GetPositionX() + 5, me.GetPositionY(), me.GetPositionZ());
                        creature.GetAI().Talk(GameobjectConst.SayFree0);
                        creature.GetMotionMaster().Clear();
                    }
                }
                return false;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_veil_skith_cageAI(go);
        }
    }

    [Script] //go_frostblade_shrine
    class go_frostblade_shrine : GameObjectScript
    {
        public go_frostblade_shrine() : base("go_frostblade_shrine") { }

        class go_frostblade_shrineAI : GameObjectAI
        {
            public go_frostblade_shrineAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                me.UseDoorOrButton(10);
                if (!player.HasAura(GameobjectConst.SpellRecentMeditation))
                    if (player.GetQuestStatus(GameobjectConst.QuestTheCleansingHorde) == QuestStatus.Incomplete || player.GetQuestStatus(GameobjectConst.QuestTheCleansingAlliance) == QuestStatus.Incomplete)
                    {
                        player.CastSpell(player, GameobjectConst.SpellCleansingSoul);
                        player.SetStandState(UnitStandStateType.Sit);
                    }
                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_frostblade_shrineAI(go);
        }
    }

    [Script] //go_midsummer_bonfire
    class go_midsummer_bonfire : GameObjectScript
    {
        public go_midsummer_bonfire() : base("go_midsummer_bonfire") { }

        class go_midsummer_bonfireAI : GameObjectAI
        {
            public go_midsummer_bonfireAI(GameObject go) : base(go) { }

            public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
            {
                player.CastSpell(player, GameobjectConst.StampOutBonfireQuestComplete, true);
                player.CLOSE_GOSSIP_MENU();
                return false;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_midsummer_bonfireAI(go);
        }
    }

    [Script]
    class go_midsummer_ribbon_pole : GameObjectScript
    {
        public go_midsummer_ribbon_pole() : base("go_midsummer_ribbon_pole") { }

        class go_midsummer_ribbon_poleAI : GameObjectAI
        {
            public go_midsummer_ribbon_poleAI(GameObject go) : base(go) { }

            public override bool GossipHello(Player player, bool reportUse)
            {
                Creature creature = me.FindNearestCreature(GameobjectConst.NpcPoleRibbonBunny, 10.0f);
                if (creature)
                {
                    creature.GetAI().DoAction(0);
                    player.CastSpell(creature, GameobjectConst.SpellPoleDance, true);
                }
                return true;
            }
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_midsummer_ribbon_poleAI(go);
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
                    me.CastSpell(null, GameobjectConst.SpellToyTrainPulse, true);
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
                me.Delete();
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
                            if (me.GetAreaId() == GameobjectConst.Silvermoon || me.GetAreaId() == GameobjectConst.Undercity || me.GetAreaId() == GameobjectConst.Orgrimmar1 || me.GetAreaId() == GameobjectConst.Orgrimmar2 || me.GetAreaId() == GameobjectConst.Thunderbluff || me.GetAreaId() == GameobjectConst.Shattrath)
                            {
                                if (rnd == 0)
                                {
                                    me.PlayDirectMusic(GameobjectConst.EventBrewfestgoblin01);
                                    musicTime = GameobjectConst.EventBrewfestgoblin01Time;
                                }
                                else if (rnd == 1)
                                {
                                    me.PlayDirectMusic(GameobjectConst.EventBrewfestgoblin02);
                                    musicTime = GameobjectConst.EventBrewfestgoblin02Time;
                                }
                                else
                                {
                                    me.PlayDirectMusic(GameobjectConst.EventBrewfestgoblin03);
                                    musicTime = GameobjectConst.EventBrewfestgoblin03Time;
                                }
                            }
                            if (me.GetAreaId() == GameobjectConst.Ironforge1 || me.GetAreaId() == GameobjectConst.Ironforge2 || me.GetAreaId() == GameobjectConst.Stormwind || me.GetAreaId() == GameobjectConst.Exodar || me.GetAreaId() == GameobjectConst.Darnassus || me.GetAreaId() == GameobjectConst.Shattrath)
                            {
                                if (rnd == 0)
                                {
                                    me.PlayDirectMusic(GameobjectConst.EventBrewfestdwarf01);
                                    musicTime = GameobjectConst.EventBrewfestdwarf01Time;
                                }
                                else if (rnd == 1)
                                {
                                    me.PlayDirectMusic(GameobjectConst.EventBrewfestdwarf02);
                                    musicTime = GameobjectConst.EventBrewfestdwarf02Time;
                                }
                                else
                                {
                                    me.PlayDirectMusic(GameobjectConst.EventBrewfestdwarf03);
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

                    var playersNearby = me.GetPlayerListInGrid(me.GetMap().GetVisibilityRange());
                    foreach (var player in playersNearby)
                    {
                        if (player.GetTeam() == Team.Horde)
                            me.PlayDirectMusic(12325, player);
                        else
                            me.PlayDirectMusic(12319, player);
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
                        me.PlayDirectMusic(8440);
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
                    me.PlayDirectMusic(12845);
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

    [Script]
    class go_bells : GameObjectScript
    {
        public go_bells() : base("go_bells") { }

        class go_bellsAI : GameObjectAI
        {
            public go_bellsAI(GameObject go) : base(go) { }

            public override void InitializeAI()
            {
                switch (me.GetEntry())
                {
                    case GameobjectConst.GoHordeBell:
                        _soundId = me.GetAreaId() == GameobjectConst.UndercityArea ? GameobjectConst.BellTollhorde : GameobjectConst.BellTolltribal;
                        break;
                    case GameobjectConst.GoAllianceBell:
                        {
                            if (me.GetAreaId() == GameobjectConst.Ironforge1Area || me.GetAreaId() == GameobjectConst.Ironforge2Area)
                                _soundId = GameobjectConst.BellTolldwarfgnome;
                            else if (me.GetAreaId() == GameobjectConst.DarnassusArea || me.GetZoneId() == GameobjectConst.TeldrassilZone)
                                _soundId = GameobjectConst.BellTollnightelf;
                            else
                                _soundId = GameobjectConst.BellTollalliance;

                            break;
                        }
                    case GameobjectConst.GoKharazhanBell:
                        _soundId = GameobjectConst.Belltollkharazhan;
                        break;
                }
            }

            public override void OnGameEvent(bool start, ushort eventId)
            {
                if (eventId == GameobjectConst.GameEventHourlyBells && start)
                {
                    var localTm = Time.UnixTimeToDateTime(GameTime.GetGameTime()).ToLocalTime();
                    int _rings = (localTm.Hour - 1) % 12 + 1;

                    for (var i = 0; i < _rings; ++i)
                        _events.ScheduleEvent(GameobjectConst.EventRingBell, TimeSpan.FromSeconds(i * 4 + 1));
                }
            }

            public override void UpdateAI(uint diff)
            {
                _events.Update(diff);

                _events.ExecuteEvents(eventId =>
                {
                    switch (eventId)
                    {
                        case GameobjectConst.EventRingBell:
                            me.PlayDirectSound(_soundId);
                            break;
                        default:
                            break;
                    }
                });
            }

            uint _soundId;
        }

        public override GameObjectAI GetAI(GameObject go)
        {
            return new go_bellsAI(go);
        }
    }
}
