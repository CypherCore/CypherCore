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

namespace Scripts.World.GameObjects
{
    struct SpellIds
    {
        //CatFigurine
        public const uint SummonGhostSaber = 5968;

        //EthereumPrison
        public const uint RepLc = 39456;
        public const uint RepShat = 39457;
        public const uint RepCe = 39460;
        public const uint RepCon = 39474;
        public const uint RepKt = 39475;
        public const uint RepSpor = 39476;

        //Southfury
        public const uint Blackjack = 39865; //Stuns Player
        public const uint SummonRizzle = 39866;

        //Felcrystalforge
        public const uint Create1FlaskOfBeast = 40964;
        public const uint Create5FlaskOfBeast = 40965;

        //Bashircrystalforge
        public const uint Create1FlaskOfSorcerer = 40968;
        public const uint Create5FlaskOfSorcerer = 40970;

        //Matrixpunchograph
        public const uint YellowPunchCard = 11512;
        public const uint BluePunchCard = 11525;
        public const uint RedPunchCard = 11528;
        public const uint PrismaticPunchCard = 11545;

        //Arcaneprison
        public const uint ArcanePrisonerKillCredit = 45456;

        //Jotunheimcage
        public const uint SummonBladeKnightH = 56207;
        public const uint SummonBladeKnightNe = 56209;
        public const uint SummonBladeKnightOrc = 56212;
        public const uint SummonBladeKnightTroll = 56214;

        //Inconspicuouslandmark
        public const uint SummonPiratesTreasureAndTriggerMob = 11462;

        //Amberpineouthouse
        public const uint Indisposed = 53017;
        public const uint IndisposedIii = 48341;
        public const uint CreateAmberseeds = 48330;

        //Thecleansing
        public const uint CleansingSoul = 43351;
        public const uint RecentMeditation = 61720;

        //Midsummerbonfire
        public const uint StampOutBonfireQuestComplete = 45458;

        //MidsummerPoleRibbon
        public static uint[] RibbonPoleSpells = { 29705, 29726, 29727 };

        //Toy Train Set
        public const uint ToyTrainPulse = 61551;
    }

    struct CreatureIds
    {
        //GildedBrazier
        public const uint Stillblade = 17716;

        //EthereumPrison
        public static uint[] PrisonEntry =
        {
            22810, 22811, 22812, 22813, 22814, 22815,               //Good Guys
            20783, 20784, 20785, 20786, 20788, 20789, 20790         //Bad Guys
        };

        //Ethereum Stasis
        public static uint[] StasisEntry =
        {
            22825, 20888, 22827, 22826, 22828
        };

        //ResoniteCask
        public const uint Goggeroc = 11920;

        //Sacredfireoflife
        public const uint Arikara = 10882;

        //Shrineofthebirds
        public const uint HawkGuard = 22992;
        public const uint EagleGuard = 22993;
        public const uint FalconGuard = 22994;

        //Southfury
        public const uint Rizzle = 23002;

        //Scourgecage
        public const uint ScourgePrisoner = 25610;

        //Bloodfilledorb
        public const uint Zelemar = 17830;

        //Jotunheimcage
        public const uint EbonBladePrisonerHuman = 30186;
        public const uint EbonBladePrisonerNe = 30194;
        public const uint EbonBladePrisonerTroll = 30196;
        public const uint EbonBladePrisonerOrc = 30195;

        //Prisonersofwyrmskull
        public const uint PrisonerPriest = 24086;
        public const uint PrisonerMage = 24088;
        public const uint PrisonerWarrior = 24089;
        public const uint PrisonerPaladin = 24090;
        public const uint CapturedValgardePrisonerProxy = 24124;

        //Tadpoles
        public const uint WinterfinTadpole = 25201;

        //Amberpineouthouse
        public const uint OuthouseBunny = 27326;

        //Hives
        public const uint HiveAmbusher = 13301;

        //Missingfriends
        public const uint CaptiveChild = 22314;

        //MidsummerPoleRibbon
        public const uint PoleRibbonBunny = 17066;
    }

    struct GameObjectIds
    {
        //Shrineofthebirds
        public const uint ShrineHawk = 185551;
        public const uint ShrineEagle = 185547;
        public const uint ShrineFalcon = 185553;

        //Bellhourlyobjects
        public const uint HordeBell = 175885;
        public const uint AllianceBell = 176573;
        public const uint KharazhanBell = 182064;
    }

    struct ItemIds
    {
        //Matrixpunchograph
        public const uint WhitePunchCard = 9279;
        public const uint YellowPunchCard = 9280;
        public const uint BluePunchCard = 9282;
        public const uint RedPunchCard = 9281;
        public const uint PrismaticPunchCard = 9316;
        public const uint MatrixPunchograph3005A = 142345;
        public const uint MatrixPunchograph3005B = 142475;
        public const uint MatrixPunchograph3005C = 142476;
        public const uint MatrixPunchograph3005D = 142696;

        //Inconspicuouslandmark
        public const uint CuergosKey = 9275;

        //Amberpineouthouse
        public const uint AnderholsSliderCider = 37247;
    }

    struct QuestIds
    {
        //GildedBrazier
        public const uint TheFirstTrial = 9678;

        //Dalarancrystal
        public const uint LearnLeaveReturn = 12790;
        public const uint TeleCrystalFlag = 12845;

        //Arcaneprison
        public const uint PrisonBreak = 11587;

        //Tabletheka
        public const uint SpiderGold = 2936;

        //Prisonersofwyrmskull
        public const uint PrisonersOfWyrmskull = 11255;

        //Tadpoles
        public const uint OhNoesTheTadpoles = 11560;

        //Amberpineouthouse
        public const uint DoingYourDuty = 12227;

        //Hives
        public const uint HiveInTheTower = 9544;

        //Missingfriends
        public const uint MissingFriends = 10852;

        //Thecleansing
        public const uint TheCleansingHorde = 11317;
        public const uint TheCleansingAlliance = 11322;
    }

    struct TextIds
    {
        //Missingfriends
        public const uint SayFree0 = 0;
    }

    struct GossipConst
    {
        //Dalarancrystal
        public const string GoTeleToDalaranCrystalFailed = "This Teleport Crystal Cannot Be Used Until The Teleport Crystal In Dalaran Has Been Used At Least Once.";

        //Felcrystalforge
        public const uint GossipFelCrystalforgeText = 31000;
        public const uint GossipFelCrystalforgeItemTextReturn = 31001;
        public const string GossipFelCrystalforgeItem1 = "Purchase 1 Unstable Flask Of The Beast For The Cost Of 10 Apexis Shards";
        public const string GossipFelCrystalforgeItem5 = "Purchase 5 Unstable Flask Of The Beast For The Cost Of 50 Apexis Shards";
        public const string GossipFelCrystalforgeItemReturn = "Use The Fel Crystalforge To Make Another Purchase.";

        //Bashircrystalforge
        public const uint GossipBashirCrystalforgeText = 31100;
        public const uint GossipBashirCrystalforgeItemTextReturn = 31101;
        public const string GossipBashirCrystalforgeItem1 = "Purchase 1 Unstable Flask Of The Sorcerer For The Cost Of 10 Apexis Shards";
        public const string GossipBashirCrystalforgeItem5 = "Purchase 5 Unstable Flask Of The Sorcerer For The Cost Of 50 Apexis Shards";
        public const string GossipBashirCrystalforgeItemReturn = "Use The Bashir Crystalforge To Make Another Purchase.";

        //Tabletheka
        public const uint GossipTableTheka = 1653;

        //Amberpineouthouse
        public const uint GossipOuthouseInuse = 12775;
        public const uint GossipOuthouseVacant = 12779;

        public const string GossipUseOuthouse = "Use The Outhouse.";
        public const string AnderholsSliderCiderNotFound = "Quest Item Anderhol'S Slider Cider Not Found.";
    }

    struct SoundIds
    {
        //BrewfestMusic
        public const uint EventBrewfestdwarf01 = 11810; // 1.35 Min
        public const uint EventBrewfestdwarf02 = 11812; // 1.55 Min 
        public const uint EventBrewfestdwarf03 = 11813; // 0.23 Min
        public const uint EventBrewfestgoblin01 = 11811; // 1.08 Min
        public const uint EventBrewfestgoblin02 = 11814; // 1.33 Min
        public const uint EventBrewfestgoblin03 = 11815; // 0.28 Min

        //Brewfestmusicevents
        public const uint EventBmSelectMusic = 1;
        public const uint EventBmStartMusic = 2;

        //Bells
        //BellHourlySoundFX
        public const uint BellTollHorde = 6595; // Horde
        public const uint BellTollTribal = 6675;
        public const uint BellTollAlliance = 6594; // Alliance
        public const uint BellTollNightelf = 6674;
        public const uint BellTolldwarfgnome = 7234;
        public const uint BellTollKharazhan = 9154; // Kharazhan
    }

    struct AreaIds
    {
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
        public const uint TeldrassilZone = 141;
        public const uint KharazhanMapid = 532;
    }

    struct Misc
    {
        // These Are In Seconds
        //Brewfestmusictime
        public const uint EventBrewfestdwarf01Time = 95000;
        public const uint EventBrewfestdwarf02Time = 155000;
        public const uint EventBrewfestdwarf03Time = 23000;
        public const uint EventBrewfestgoblin01Time = 68000;
        public const uint EventBrewfestgoblin02Time = 93000;
        public const uint EventBrewfestgoblin03Time = 28000;

        //Bellhourlymisc
        public const uint GameEventHourlyBells = 73;
        public const uint EventRingBell = 1;
    }

    [Script]
    class go_cat_figurine : GameObjectAI
    {
        public go_cat_figurine(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            player.CastSpell(player, SpellIds.SummonGhostSaber, true);
            return false;
        }
    }

    [Script]
    class go_barov_journal : GameObjectAI
    {
        public go_barov_journal(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (player.HasSkill(SkillType.Tailoring) && player.GetBaseSkillValue(SkillType.Tailoring) >= 280 && !player.HasSpell(26086))
                player.CastSpell(player, 26095, false);

            return true;
        }
    }

    [Script]
    class go_gilded_brazier : GameObjectAI
    {
        public go_gilded_brazier(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (me.GetGoType() == GameObjectTypes.Goober)
            {
                if (player.GetQuestStatus(QuestIds.TheFirstTrial) == QuestStatus.Incomplete)
                {
                    Creature Stillblade = player.SummonCreature(CreatureIds.Stillblade, 8106.11f, -7542.06f, 151.775f, 3.02598f, TempSummonType.DeadDespawn, 60000);
                    if (Stillblade)
                        Stillblade.GetAI().AttackStart(player);
                }
            }
            return true;
        }
    }

    [Script]
    class go_orb_of_command : GameObjectAI
    {
        public go_orb_of_command(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (player.GetQuestRewardStatus(7761))
                player.CastSpell(player, 23460, true);

            return true;
        }
    }

    [Script]
    class go_tablet_of_madness : GameObjectAI
    {
        public go_tablet_of_madness(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (player.HasSkill(SkillType.Alchemy) && player.GetSkillValue(SkillType.Alchemy) >= 300 && !player.HasSpell(24266))
                player.CastSpell(player, 24267, false);

            return true;
        }
    }

    [Script]
    class go_tablet_of_the_seven : GameObjectAI
    {
        public go_tablet_of_the_seven(GameObject go) : base(go) { }

        /// @todo use gossip option ("Transcript the Tablet") instead, if Trinity adds support.
        public override bool GossipHello(Player player)
        {
            if (me.GetGoType() != GameObjectTypes.QuestGiver)
                return true;

            if (player.GetQuestStatus(4296) == QuestStatus.Incomplete)
                player.CastSpell(player, 15065, false);

            return true;
        }
    }

    [Script]
    class go_jump_a_tron : GameObjectAI
    {
        public go_jump_a_tron(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (player.GetQuestStatus(10111) == QuestStatus.Incomplete)
                player.CastSpell(player, 33382, true);

            return true;
        }
    }

    [Script]
    class go_ethereum_prison : GameObjectAI
    {
        public go_ethereum_prison(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            me.UseDoorOrButton();
            int Random = (int)(RandomHelper.Rand32() % (CreatureIds.PrisonEntry.Length / sizeof(uint)));

            Creature creature = player.SummonCreature(CreatureIds.PrisonEntry[Random], me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetAbsoluteAngle(player), TempSummonType.TimedDespawnOutOfCombat, 30000);
            if (creature)
            {
                if (!creature.IsHostileTo(player))
                {
                    FactionTemplateRecord pFaction = creature.GetFactionTemplateEntry();
                    if (pFaction != null)
                    {
                        uint spellId = 0;

                        switch (pFaction.Faction)
                        {
                            case 1011:
                                spellId = SpellIds.RepLc;
                                break;
                            case 935:
                                spellId = SpellIds.RepShat;
                                break;
                            case 942:
                                spellId = SpellIds.RepCe;
                                break;
                            case 933:
                                spellId = SpellIds.RepCon;
                                break;
                            case 989:
                                spellId = SpellIds.RepKt;
                                break;
                            case 970:
                                spellId = SpellIds.RepSpor;
                                break;
                        }

                        if (spellId != 0)
                            creature.CastSpell(player, spellId, false);
                        else
                            Log.outError(LogFilter.Scripts, $"go_ethereum_prison summoned Creature (entry {creature.GetEntry()}) but faction ({creature.GetFaction()}) are not expected by script.");
                    }
                }
            }

            return false;
        }
    }

    [Script]
    class go_ethereum_stasis : GameObjectAI
    {
        public go_ethereum_stasis(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            me.UseDoorOrButton();
            int Random = (int)(RandomHelper.Rand32() % CreatureIds.StasisEntry.Length / sizeof(uint));

            player.SummonCreature(CreatureIds.StasisEntry[Random], me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetAbsoluteAngle(player), TempSummonType.TimedDespawnOutOfCombat, 30000);

            return false;
        }
    }

    [Script]
    class go_resonite_cask : GameObjectAI
    {
        public go_resonite_cask(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (me.GetGoType() == GameObjectTypes.Goober)
                me.SummonCreature(CreatureIds.Goggeroc, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, 300000);

            return false;
        }
    }

    [Script]
    class go_sacred_fire_of_life : GameObjectAI
    {
        public go_sacred_fire_of_life(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (me.GetGoType() == GameObjectTypes.Goober)
                player.SummonCreature(CreatureIds.Arikara, -5008.338f, -2118.894f, 83.657f, 0.874f, TempSummonType.TimedDespawnOutOfCombat, 30000);

            return true;
        }
    }

    [Script]
    class go_shrine_of_the_birds : GameObjectAI
    {
        public go_shrine_of_the_birds(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            uint birdEntryId = 0;

            me.GetClosePoint(out float fX, out float fY, out float fZ, me.GetCombatReach(), SharedConst.InteractionDistance);

            switch (me.GetEntry())
            {
                case GameObjectIds.ShrineHawk:
                    birdEntryId = CreatureIds.HawkGuard;
                    break;
                case GameObjectIds.ShrineEagle:
                    birdEntryId = CreatureIds.EagleGuard;
                    break;
                case GameObjectIds.ShrineFalcon:
                    birdEntryId = CreatureIds.FalconGuard;
                    break;
            }

            if (birdEntryId != 0)
                player.SummonCreature(birdEntryId, fX, fY, fZ, me.GetOrientation(), TempSummonType.TimedDespawnOutOfCombat, 60000);

            return false;
        }
    }

    [Script]
    class go_southfury_moonstone : GameObjectAI
    {
        public go_southfury_moonstone(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            //implicitTarget=48 not implemented as of writing this code, and manual summon may be just ok for our purpose
            //player.CastSpell(player, SpellSummonRizzle, false);

            Creature creature = player.SummonCreature(CreatureIds.Rizzle, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.DeadDespawn, 0);
            if (creature)
                creature.CastSpell(player, SpellIds.Blackjack, false);

            return false;
        }
    }

    [Script]
    class go_tele_to_dalaran_crystal : GameObjectAI
    {
        public go_tele_to_dalaran_crystal(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (player.GetQuestRewardStatus(QuestIds.TeleCrystalFlag))
                return false;

            player.GetSession().SendNotification(GossipConst.GoTeleToDalaranCrystalFailed);
            return true;
        }
    }

    [Script]
    class go_tele_to_violet_stand : GameObjectAI
    {
        public go_tele_to_violet_stand(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (player.GetQuestRewardStatus(QuestIds.LearnLeaveReturn) || player.GetQuestStatus(QuestIds.LearnLeaveReturn) == QuestStatus.Incomplete)
                return false;

            return true;
        }
    }

    [Script]
    class go_fel_crystalforge : GameObjectAI
    {
        public go_fel_crystalforge(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (me.GetGoType() == GameObjectTypes.QuestGiver) /* != GAMEOBJECT_TYPE_QUESTGIVER) */
                player.PrepareQuestMenu(me.GetGUID()); /* return true*/

            player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipFelCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
            player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipFelCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);

            player.SendGossipMenu(GossipConst.GossipFelCrystalforgeText, me.GetGUID());

            return true;
        }

        public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();
            switch (action)
            {
                case eTradeskill.GossipActionInfoDef:
                    player.CastSpell(player, SpellIds.Create1FlaskOfBeast, false);
                    player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipFelCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.SendGossipMenu(GossipConst.GossipFelCrystalforgeItemTextReturn, me.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 1:
                    player.CastSpell(player, SpellIds.Create5FlaskOfBeast, false);
                    player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipFelCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.SendGossipMenu(GossipConst.GossipFelCrystalforgeItemTextReturn, me.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 2:
                    player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipFelCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
                    player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipFelCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                    player.SendGossipMenu(GossipConst.GossipFelCrystalforgeText, me.GetGUID());
                    break;
            }
            return true;
        }
    }

    [Script]
    class go_bashir_crystalforge : GameObjectAI
    {
        public go_bashir_crystalforge(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (me.GetGoType() == GameObjectTypes.QuestGiver) /* != GAMEOBJECT_TYPE_QUESTGIVER) */
                player.PrepareQuestMenu(me.GetGUID()); /* return true*/

            player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipBashirCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
            player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipBashirCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);

            player.SendGossipMenu(GossipConst.GossipBashirCrystalforgeText, me.GetGUID());

            return true;
        }

        public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();
            switch (action)
            {
                case eTradeskill.GossipActionInfoDef:
                    player.CastSpell(player, SpellIds.Create1FlaskOfSorcerer, false);
                    player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipBashirCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.SendGossipMenu(GossipConst.GossipBashirCrystalforgeItemTextReturn, me.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 1:
                    player.CastSpell(player, SpellIds.Create5FlaskOfSorcerer, false);
                    player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipBashirCrystalforgeItemReturn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.SendGossipMenu(GossipConst.GossipBashirCrystalforgeItemTextReturn, me.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 2:
                    player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipBashirCrystalforgeItem1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef);
                    player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipBashirCrystalforgeItem5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                    player.SendGossipMenu(GossipConst.GossipBashirCrystalforgeText, me.GetGUID());
                    break;
            }
            return true;
        }
    }

    [Script]
    class go_matrix_punchograph : GameObjectAI
    {
        public go_matrix_punchograph(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            switch (me.GetEntry())
            {
                case ItemIds.MatrixPunchograph3005A:
                    if (player.HasItemCount(ItemIds.WhitePunchCard))
                    {
                        player.DestroyItemCount(ItemIds.WhitePunchCard, 1, true);
                        player.CastSpell(player, SpellIds.YellowPunchCard, true);
                    }
                    break;
                case ItemIds.MatrixPunchograph3005B:
                    if (player.HasItemCount(ItemIds.YellowPunchCard))
                    {
                        player.DestroyItemCount(ItemIds.YellowPunchCard, 1, true);
                        player.CastSpell(player, SpellIds.BluePunchCard, true);
                    }
                    break;
                case ItemIds.MatrixPunchograph3005C:
                    if (player.HasItemCount(ItemIds.BluePunchCard))
                    {
                        player.DestroyItemCount(ItemIds.BluePunchCard, 1, true);
                        player.CastSpell(player, SpellIds.RedPunchCard, true);
                    }
                    break;
                case ItemIds.MatrixPunchograph3005D:
                    if (player.HasItemCount(ItemIds.RedPunchCard))
                    {
                        player.DestroyItemCount(ItemIds.RedPunchCard, 1, true);
                        player.CastSpell(player, SpellIds.PrismaticPunchCard, true);
                    }
                    break;
                default:
                    break;
            }
            return false;
        }
    }

    [Script]
    class go_scourge_cage : GameObjectAI
    {
        public go_scourge_cage(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            me.UseDoorOrButton();
            Creature pNearestPrisoner = me.FindNearestCreature(CreatureIds.ScourgePrisoner, 5.0f, true);
            if (pNearestPrisoner)
            {
                player.KilledMonsterCredit(CreatureIds.ScourgePrisoner, pNearestPrisoner.GetGUID());
                pNearestPrisoner.DisappearAndDie();
            }

            return true;
        }
    }

    [Script]
    class go_arcane_prison : GameObjectAI
    {
        public go_arcane_prison(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (player.GetQuestStatus(QuestIds.PrisonBreak) == QuestStatus.Incomplete)
            {
                me.SummonCreature(25318, 3485.089844f, 6115.7422188f, 70.966812f, 0, TempSummonType.TimedDespawn, 60000);
                player.CastSpell(player, SpellIds.ArcanePrisonerKillCredit, true);
                return true;
            }
            return false;
        }
    }

    [Script]
    class go_blood_filled_orb : GameObjectAI
    {
        public go_blood_filled_orb(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (me.GetGoType() == GameObjectTypes.Goober)
                player.SummonCreature(CreatureIds.Zelemar, -369.746f, 166.759f, -21.50f, 5.235f, TempSummonType.TimedDespawnOutOfCombat, 30000);

            return true;
        }
    }

    [Script]
    class go_jotunheim_cage : GameObjectAI
    {
        public go_jotunheim_cage(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            me.UseDoorOrButton();
            Creature pPrisoner = me.FindNearestCreature(CreatureIds.EbonBladePrisonerHuman, 5.0f, true);
            if (!pPrisoner)
            {
                pPrisoner = me.FindNearestCreature(CreatureIds.EbonBladePrisonerTroll, 5.0f, true);
                if (!pPrisoner)
                {
                    pPrisoner = me.FindNearestCreature(CreatureIds.EbonBladePrisonerOrc, 5.0f, true);
                    if (!pPrisoner)
                        pPrisoner = me.FindNearestCreature(CreatureIds.EbonBladePrisonerNe, 5.0f, true);
                }
            }
            if (!pPrisoner || !pPrisoner.IsAlive())
                return false;

            pPrisoner.DisappearAndDie();
            player.KilledMonsterCredit(CreatureIds.EbonBladePrisonerHuman);
            switch (pPrisoner.GetEntry())
            {
                case CreatureIds.EbonBladePrisonerHuman:
                    player.CastSpell(player, SpellIds.SummonBladeKnightH, true);
                    break;
                case CreatureIds.EbonBladePrisonerNe:
                    player.CastSpell(player, SpellIds.SummonBladeKnightNe, true);
                    break;
                case CreatureIds.EbonBladePrisonerTroll:
                    player.CastSpell(player, SpellIds.SummonBladeKnightTroll, true);
                    break;
                case CreatureIds.EbonBladePrisonerOrc:
                    player.CastSpell(player, SpellIds.SummonBladeKnightOrc, true);
                    break;
            }
            return true;
        }
    }

    [Script]
    class go_table_theka : GameObjectAI
    {
        public go_table_theka(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (player.GetQuestStatus(QuestIds.SpiderGold) == QuestStatus.Incomplete)
                player.AreaExploredOrEventHappens(QuestIds.SpiderGold);

            player.SendGossipMenu(GossipConst.GossipTableTheka, me.GetGUID());
            return true;
        }
    }

    [Script]
    class go_inconspicuous_landmark : GameObjectAI
    {
        public go_inconspicuous_landmark(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            if (player.HasItemCount(ItemIds.CuergosKey))
                return false;

            player.CastSpell(player, SpellIds.SummonPiratesTreasureAndTriggerMob, true);
            return true;
        }
    }

    [Script]
    class go_soulwell : GameObjectAI
    {
        public go_soulwell(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            Unit owner = me.GetOwner();
            if (!owner || !owner.IsTypeId(TypeId.Player) || !player.IsInSameRaidWith(owner.ToPlayer()))
                return true;
            return false;
        }
    }

    [Script]
    class go_dragonflayer_cage : GameObjectAI
    {
        public go_dragonflayer_cage(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            me.UseDoorOrButton();
            if (player.GetQuestStatus(QuestIds.PrisonersOfWyrmskull) != QuestStatus.Incomplete)
                return true;

            Creature pPrisoner = me.FindNearestCreature(CreatureIds.PrisonerPriest, 2.0f);
            if (!pPrisoner)
            {
                pPrisoner = me.FindNearestCreature(CreatureIds.PrisonerMage, 2.0f);
                if (!pPrisoner)
                {
                    pPrisoner = me.FindNearestCreature(CreatureIds.PrisonerWarrior, 2.0f);
                    if (!pPrisoner)
                        pPrisoner = me.FindNearestCreature(CreatureIds.PrisonerPaladin, 2.0f);
                }
            }

            if (!pPrisoner || !pPrisoner.IsAlive())
                return true;

            /// @todo prisoner should help player for a short period of time
            player.KilledMonsterCredit(CreatureIds.CapturedValgardePrisonerProxy);
            pPrisoner.DespawnOrUnsummon();
            return true;
        }
    }

    [Script]
    class go_amberpine_outhouse : GameObjectAI
    {
        public go_amberpine_outhouse(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            QuestStatus status = player.GetQuestStatus(QuestIds.DoingYourDuty);
            if (status == QuestStatus.Incomplete || status == QuestStatus.Complete || status == QuestStatus.Rewarded)
            {
                player.AddGossipItem(GossipOptionIcon.Chat, GossipConst.GossipUseOuthouse, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                player.SendGossipMenu(GossipConst.GossipOuthouseVacant, me.GetGUID());
            }
            else
                player.SendGossipMenu(GossipConst.GossipOuthouseInuse, me.GetGUID());

            return true;
        }

        public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();
            if (action == eTradeskill.GossipActionInfoDef + 1)
            {
                player.CloseGossipMenu();
                Creature target = ScriptedAI.GetClosestCreatureWithEntry(player, CreatureIds.OuthouseBunny, 3.0f);
                if (target)
                {
                    target.GetAI().SetData(1, (uint)player.GetGender());
                    me.CastSpell(target, SpellIds.IndisposedIii);
                }
                me.CastSpell(player, SpellIds.Indisposed);
                if (player.HasItemCount(ItemIds.AnderholsSliderCider))
                    me.CastSpell(player, SpellIds.CreateAmberseeds);
                return true;
            }
            else
            {
                player.CloseGossipMenu();
                player.GetSession().SendNotification(GossipConst.AnderholsSliderCiderNotFound);
                return false;
            }
        }
    }

    [Script]
    class go_hive_pod : GameObjectAI
    {
        public go_hive_pod(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            player.SendLoot(me.GetGUID(), LootType.Corpse);
            me.SummonCreature(CreatureIds.HiveAmbusher, me.GetPositionX() + 1, me.GetPositionY(), me.GetPositionZ(), me.GetAbsoluteAngle(player), TempSummonType.TimedOrDeadDespawn, 60000);
            me.SummonCreature(CreatureIds.HiveAmbusher, me.GetPositionX(), me.GetPositionY() + 1, me.GetPositionZ(), me.GetAbsoluteAngle(player), TempSummonType.TimedOrDeadDespawn, 60000);
            return true;
        }
    }

    [Script]
    class go_massive_seaforium_charge : GameObjectAI
    {
        public go_massive_seaforium_charge(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            me.SetLootState(LootState.JustDeactivated);
            return true;
        }
    }

    [Script]
    class go_veil_skith_cage : GameObjectAI
    {
        public go_veil_skith_cage(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            me.UseDoorOrButton();
            if (player.GetQuestStatus(QuestIds.MissingFriends) == QuestStatus.Incomplete)
            {
                List<Creature> childrenList = new List<Creature>();
                me.GetCreatureListWithEntryInGrid(childrenList, CreatureIds.CaptiveChild, SharedConst.InteractionDistance);
                foreach (Creature creature in childrenList)
                {
                    player.KilledMonsterCredit(CreatureIds.CaptiveChild, creature.GetGUID());
                    creature.DespawnOrUnsummon(5000);
                    creature.GetMotionMaster().MovePoint(1, me.GetPositionX() + 5, me.GetPositionY(), me.GetPositionZ());
                    creature.GetAI().Talk(TextIds.SayFree0);
                    creature.GetMotionMaster().Clear();
                }
            }
            return false;
        }
    }

    [Script]
    class go_frostblade_shrine : GameObjectAI
    {
        public go_frostblade_shrine(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            me.UseDoorOrButton(10);
            if (!player.HasAura(SpellIds.RecentMeditation))
            {
                if (player.GetQuestStatus(QuestIds.TheCleansingHorde) == QuestStatus.Incomplete || player.GetQuestStatus(QuestIds.TheCleansingAlliance) == QuestStatus.Incomplete)
                {
                    player.CastSpell(player, SpellIds.CleansingSoul);
                    player.SetStandState(UnitStandStateType.Sit);
                }
            }
            return true;
        }
    }

    [Script]
    class go_midsummer_bonfire : GameObjectAI
    {
        public go_midsummer_bonfire(GameObject go) : base(go) { }

        public override bool GossipSelect(Player player, uint menuId, uint ssipListId)
        {
            player.CastSpell(player, SpellIds.StampOutBonfireQuestComplete, true);
            player.CloseGossipMenu();
            return false;
        }
    }

    [Script]
    class go_midsummer_ribbon_pole : GameObjectAI
    {
        public go_midsummer_ribbon_pole(GameObject go) : base(go) { }

        public override bool GossipHello(Player player)
        {
            Creature creature = me.FindNearestCreature(CreatureIds.PoleRibbonBunny, 10.0f);
            if (creature)
            {
                creature.GetAI().DoAction(0);
                player.CastSpell(player, SpellIds.RibbonPoleSpells[RandomHelper.IRand(0, 2)], true);
            }
            return true;
        }
    }

    [Script]
    class go_toy_train_set : GameObjectAI
    {
        uint _pulseTimer;

        public go_toy_train_set(GameObject go) : base(go)
        {
            _pulseTimer = 3 * Time.InMilliseconds;
        }

        public override void UpdateAI(uint diff)
        {
            if (diff < _pulseTimer)
                _pulseTimer -= diff;
            else
            {
                me.CastSpell(null, SpellIds.ToyTrainPulse, true);
                _pulseTimer = 6 * Time.InMilliseconds;
            }
        }

        // triggered on wrecker'd
        public override void DoAction(int action)
        {
            me.Delete();
        }
    }

    [Script]
    class go_brewfest_music : GameObjectAI
    {
        uint rnd = 0;
        uint musicTime = 1000;

        public go_brewfest_music(GameObject go) : base(go)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                if (Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest)) // Check if Brewfest is active
                {
                    rnd = RandomHelper.URand(0, 2); // Select random music sample
                    task.Repeat(TimeSpan.FromMilliseconds(musicTime)); // Select new song music after play time is over
                }
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                if (Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest)) // Check if Brewfest is active
                {
                    switch (me.GetAreaId())
                    {
                        case AreaIds.Silvermoon:
                        case AreaIds.Undercity:
                        case AreaIds.Orgrimmar1:
                        case AreaIds.Orgrimmar2:
                        case AreaIds.Thunderbluff:
                            switch (rnd)
                            {
                                case 0:
                                    me.PlayDirectMusic(SoundIds.EventBrewfestgoblin01);
                                    musicTime = Misc.EventBrewfestgoblin01Time;
                                    break;
                                case 1:
                                    me.PlayDirectMusic(SoundIds.EventBrewfestgoblin02);
                                    musicTime = Misc.EventBrewfestgoblin02Time;
                                    break;
                                default:
                                    me.PlayDirectMusic(SoundIds.EventBrewfestgoblin03);
                                    musicTime = Misc.EventBrewfestgoblin03Time;
                                    break;
                            }
                            break;
                        case AreaIds.Ironforge1:
                        case AreaIds.Ironforge2:
                        case AreaIds.Stormwind:
                        case AreaIds.Exodar:
                        case AreaIds.Darnassus:
                            switch (rnd)
                            {
                                case 0:
                                    me.PlayDirectMusic(SoundIds.EventBrewfestdwarf01);
                                    musicTime = Misc.EventBrewfestdwarf01Time;
                                    break;
                                case 1:
                                    me.PlayDirectMusic(SoundIds.EventBrewfestdwarf02);
                                    musicTime = Misc.EventBrewfestdwarf02Time;
                                    break;
                                default:
                                    me.PlayDirectMusic(SoundIds.EventBrewfestdwarf03);
                                    musicTime = Misc.EventBrewfestdwarf03Time;
                                    break;
                            }
                            break;
                        case AreaIds.Shattrath:
                            List<Unit> playersNearby = me.GetPlayerListInGrid(me.GetVisibilityRange());
                            foreach (Player player in playersNearby)
                            {
                                if (player.GetTeamId() == TeamId.Horde)
                                {
                                    switch (rnd)
                                    {
                                        case 0:
                                            me.PlayDirectMusic(SoundIds.EventBrewfestgoblin01);
                                            musicTime = Misc.EventBrewfestgoblin01Time;
                                            break;
                                        case 1:
                                            me.PlayDirectMusic(SoundIds.EventBrewfestgoblin02);
                                            musicTime = Misc.EventBrewfestgoblin02Time;
                                            break;
                                        default:
                                            me.PlayDirectMusic(SoundIds.EventBrewfestgoblin03);
                                            musicTime = Misc.EventBrewfestgoblin03Time;
                                            break;
                                    }
                                }
                                else
                                {
                                    switch (rnd)
                                    {
                                        case 0:
                                            me.PlayDirectMusic(SoundIds.EventBrewfestdwarf01);
                                            musicTime = Misc.EventBrewfestdwarf01Time;
                                            break;
                                        case 1:
                                            me.PlayDirectMusic(SoundIds.EventBrewfestdwarf02);
                                            musicTime = Misc.EventBrewfestdwarf02Time;
                                            break;
                                        default:
                                            me.PlayDirectMusic(SoundIds.EventBrewfestdwarf03);
                                            musicTime = Misc.EventBrewfestdwarf03Time;
                                            break;
                                    }
                                }
                            }
                            break;
                    }
                    task.Repeat(TimeSpan.FromSeconds(5)); // Every 5 second's SMSG_PLAY_MUSIC packet (PlayDirectMusic) is pushed to the client
                }
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class go_midsummer_music : GameObjectAI
    {
        public go_midsummer_music(GameObject go) : base(go)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.MidsummerFireFestival))
                    return;

                var playersNearby = me.GetPlayerListInGrid(me.GetMap().GetVisibilityRange());
                foreach (Player player in playersNearby)
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

    [Script]
    class go_darkmoon_faire_music : GameObjectAI
    {
        public go_darkmoon_faire_music(GameObject go) : base(go)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                if (Global.GameEventMgr.IsHolidayActive(HolidayIds.DarkmoonFaire))
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

    [Script]
    class go_pirate_day_music : GameObjectAI
    {
        public go_pirate_day_music(GameObject go) : base(go)
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

    [Script]
    class go_bells : GameObjectAI
    {   
        uint _soundId;

        public go_bells(GameObject go) : base(go) { }

        public override void InitializeAI()
        {
            switch (me.GetEntry())
            {
                case GameObjectIds.HordeBell:
                    _soundId = me.GetAreaId() == AreaIds.Undercity ? SoundIds.BellTollHorde : SoundIds.BellTollTribal;
                    break;
                case GameObjectIds.AllianceBell:
                    {
                        if (me.GetAreaId() == AreaIds.Ironforge1 || me.GetAreaId() == AreaIds.Ironforge2)
                            _soundId = SoundIds.BellTolldwarfgnome;
                        else if (me.GetAreaId() == AreaIds.Darnassus || me.GetZoneId() == AreaIds.TeldrassilZone)
                            _soundId = SoundIds.BellTollNightelf;
                        else
                            _soundId = SoundIds.BellTollAlliance;

                        break;
                    }
                case GameObjectIds.KharazhanBell:
                    _soundId = SoundIds.BellTollKharazhan;
                    break;
            }
        }

        public override void OnGameEvent(bool start, ushort eventId)
        {
            if (eventId == Misc.GameEventHourlyBells && start)
            {
                var localTm = Time.UnixTimeToDateTime(GameTime.GetGameTime()).ToLocalTime();
                int _rings = (localTm.Hour - 1) % 12 + 1;

                for (var i = 0; i < _rings; ++i)
                    _scheduler.Schedule(TimeSpan.FromSeconds(i * 4 + 1), task => me.PlayDirectSound(_soundId));
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }
}

