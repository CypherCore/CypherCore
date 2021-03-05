﻿/*
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
using Game;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Dynamic;
using Game.Movement;

namespace Scripts.World.NpcSpecial
{
    internal enum SpawnType
    {
        TripwireRooftop,                             // no warning, summon Creature at smaller range
        AlarmBot,                                     // cast guards mark and summon npc - if player shows up with that buff duration < 5 seconds attack
    }

    internal class SpawnAssociation
    {
        public SpawnAssociation(uint _thisCreatureEntry, uint _spawnedCreatureEntry, SpawnType _spawnType)
        {
            thisCreatureEntry = _thisCreatureEntry;
            spawnedCreatureEntry = _spawnedCreatureEntry;
            spawnType = _spawnType;
        }

        public uint thisCreatureEntry;
        public uint spawnedCreatureEntry;
        public SpawnType spawnType;
    }

    internal struct CreatureIds
    {
        //Torchtossingtarget
        public const uint TorchTossingTargetBunny = 25535;

        //Garments
        public const uint Shaya = 12429;
        public const uint Roberts = 12423;
        public const uint Dolf = 12427;
        public const uint Korja = 12430;
        public const uint DgKel = 12428;

        //Doctor
        public const uint DoctorAlliance = 12939;
        public const uint DoctorHorde = 12920;

        //Fireworks
        public const uint Omen = 15467;
        public const uint MinionOfOmen = 15466;
        public const uint FireworkBlue = 15879;
        public const uint FireworkGreen = 15880;
        public const uint FireworkPurple = 15881;
        public const uint FireworkRed = 15882;
        public const uint FireworkYellow = 15883;
        public const uint FireworkWhite = 15884;
        public const uint FireworkBigBlue = 15885;
        public const uint FireworkBigGreen = 15886;
        public const uint FireworkBigPurple = 15887;
        public const uint FireworkBigRed = 15888;
        public const uint FireworkBigYellow = 15889;
        public const uint FireworkBigWhite = 15890;

        public const uint ClusterBlue = 15872;
        public const uint ClusterRed = 15873;
        public const uint ClusterGreen = 15874;
        public const uint ClusterPurple = 15875;
        public const uint ClusterWhite = 15876;
        public const uint ClusterYellow = 15877;
        public const uint ClusterBigBlue = 15911;
        public const uint ClusterBigGreen = 15912;
        public const uint ClusterBigPurple = 15913;
        public const uint ClusterBigRed = 15914;
        public const uint ClusterBigWhite = 15915;
        public const uint ClusterBigYellow = 15916;
        public const uint ClusterElune = 15918;

        // Rabbitspells
        public const uint SpringRabbit = 32791;

        // TrainWrecker
        public const uint ExultingWindUpTrainWrecker = 81071;

        // Argent squire/gruntling
        public const uint ArgentSquire = 33238;

        // BountifulTable
        public const uint TheTurkeyChair = 34812;
        public const uint TheCranberryChair = 34823;
        public const uint TheStuffingChair = 34819;
        public const uint TheSweetPotatoChair = 34824;
        public const uint ThePieChair = 34822;
    }

    internal struct GameobjectIds
    {
        //Fireworks
        public const uint FireworkLauncher1 = 180771;
        public const uint FireworkLauncher2 = 180868;
        public const uint FireworkLauncher3 = 180850;
        public const uint ClusterLauncher1 = 180772;
        public const uint ClusterLauncher2 = 180859;
        public const uint ClusterLauncher3 = 180869;
        public const uint ClusterLauncher4 = 180874;

        //TrainWrecker
        public const uint ToyTrain = 193963;

        //RibbonPole
        public const uint RibbonPole = 181605;
    }

    internal struct SpellIds
    {
        public const uint GuardsMark = 38067;

        //Dancingflames
        public const uint Brazier = 45423;
        public const uint Seduction = 47057;
        public const uint FieryAura = 45427;

        //RibbonPole
        public const uint RibbonDanceCosmetic = 29726;
        public const uint RedFireRing = 46836;
        public const uint BlueFireRing = 46842;

        //Torchtossingtarget
        public const uint TargetIndicator = 45723;

        //Garments    
        public const uint LesserHealR2 = 2052;
        public const uint FortitudeR1 = 1243;

        //Guardianspells
        public const uint Deathtouch = 5;

        //Sayge
        public const uint Strength = 23735; // +10% Strength
        public const uint Agility = 23736; // +10% Agility
        public const uint Stamina = 23737; // +10% Stamina
        public const uint Spirit = 23738; // +10% Spirit
        public const uint Intellect = 23766; // +10% Intellect
        public const uint Armor = 23767; // +10% Armor
        public const uint Damage = 23768; // +10% Damage
        public const uint Resistance = 23769; // +25 Magic Resistance (All)
        public const uint Fortune = 23765;  // Darkmoon Faire Fortune

        //Tonkmine
        public const uint TonkMineDetonate = 25099;

        //Brewfestreveler
        public const uint BrewfestToast = 41586;

        //Wormholespells
        public const uint BoreanTundra = 67834;
        public const uint SholazarBasin = 67835;
        public const uint Icecrown = 67836;
        public const uint StormPeaks = 67837;
        public const uint HowlingFjord = 67838;
        public const uint Underground = 68081;

        //Fireworks  
        public const uint RocketBlue = 26344;
        public const uint RocketGreen = 26345;
        public const uint RocketPurple = 26346;
        public const uint RocketRed = 26347;
        public const uint RocketWhite = 26348;
        public const uint RocketYellow = 26349;
        public const uint RocketBigBlue = 26351;
        public const uint RocketBigGreen = 26352;
        public const uint RocketBigPurple = 26353;
        public const uint RocketBigRed = 26354;
        public const uint RocketBigWhite = 26355;
        public const uint RocketBigYellow = 26356;
        public const uint LunarFortune = 26522;

        //Rabbitspells
        public const uint SpringFling = 61875;
        public const uint SpringRabbitJump = 61724;
        public const uint SpringRabbitWander = 61726;
        public const uint SummonBabyBunny = 61727;
        public const uint SpringRabbitInLove = 61728;

        //TrainWrecker
        public const uint ToyTrainPulse = 61551;
        public const uint WreckTrain = 62943;

        //Argent squire/gruntling
        public const uint DarnassusPennant = 63443;
        public const uint ExodarPennant = 63439;
        public const uint GnomereganPennant = 63442;
        public const uint IronforgePennant = 63440;
        public const uint StormwindPennant = 62727;
        public const uint SenjinPennant = 63446;
        public const uint UndercityPennant = 63441;
        public const uint OrgrimmarPennant = 63444;
        public const uint SilvermoonPennant = 63438;
        public const uint ThunderbluffPennant = 63445;
        public const uint AuraPostmanS = 67376;
        public const uint AuraShopS = 67377;
        public const uint AuraBankS = 67368;
        public const uint AuraTiredS = 67401;
        public const uint AuraBankG = 68849;
        public const uint AuraPostmanG = 68850;
        public const uint AuraShopG = 68851;
        public const uint AuraTiredG = 68852;
        public const uint TiredPlayer = 67334;

        //BountifulTable
        public const uint CranberryServer = 61793;
        public const uint PieServer = 61794;
        public const uint StuffingServer = 61795;
        public const uint TurkeyServer = 61796;
        public const uint SweetPotatoesServer = 61797;
        public static Dictionary<uint, uint> ChairSpells = new Dictionary<uint, uint>()
        {
            { CreatureIds.TheCranberryChair, CranberryServer },
            { CreatureIds.ThePieChair, PieServer },
            { CreatureIds.TheStuffingChair, StuffingServer },
            { CreatureIds.TheTurkeyChair, TurkeyServer },
            { CreatureIds.TheSweetPotatoChair, SweetPotatoesServer },
        };
    }

    internal struct QuestConst
    {
        //Lunaclawspirit
        public const uint BodyHeartA = 6001;
        public const uint BodyHeartH = 6002;

        //ChickenCluck
        public const uint Cluck = 3861;

        //Garments
        public const uint Moon = 5621;
        public const uint Light1 = 5624;
        public const uint Light2 = 5625;
        public const uint Spirit = 5648;
        public const uint Darkness = 5650;
    }

    internal struct TextIds
    {
        //Lunaclawspirit
        public const uint TextIdDefault = 4714;
        public const uint TextIdProgress = 4715;

        //Chickencluck
        public const uint EmoteHelloA = 0;
        public const uint EmoteHelloH = 1;
        public const uint EmoteCluck = 2;

        //Doctor
        public const uint SayDoc = 0;

        //    Garments
        // Used By 12429; 12423; 12427; 12430; 12428; But Signed For 12429
        public const uint SayThanks = 0;
        public const uint SayGoodbye = 1;
        public const uint SayHealed = 2;

        //Wormholespells
        public const uint Wormhole = 14785;

        //NpcExperience
        public const uint XpOnOff = 14736;
    }

    internal struct GossipMenus
    {
        //Sayge
        public const int OptionIdAnswer1 = 0;
        public const int OptionIdAnswer2 = 1;
        public const int OptionIdAnswer3 = 2;
        public const int OptionIdAnswer4 = 3;
        public const int IAmReadyToDiscover = 6186;
        public const int OptionSayge = 6185;
        public const int OptionSayge2 = 6187;
        public const int OptionSayge3 = 6208;
        public const int OptionSayge4 = 6209;
        public const int OptionSayge5 = 6210;
        public const int OptionSayge6 = 6211;
        public const int IHaveLongKnown = 7339;
        public const int YouHaveBeenTasked = 7340;
        public const int SwornExecutioner = 7341;
        public const int DiplomaticMission = 7361;
        public const int YourBrotherSeeks = 7362;
        public const int ATerribleBeast = 7363;
        public const int YourFortuneIsCast = 7364;
        public const int HereIsYourFortune = 7365;
        public const int CantGiveYouYour = 7393;

        //Wormhole
        public const int MenuIdWormhole = 10668; // "This tear in the fabric of time and space looks ominous."
        public const int OptionIdWormhole1 = 0;  // "Borean Tundra"
        public const int OptionIdWormhole2 = 1;  // "Howling Fjord"
        public const int OptionIdWormhole3 = 2;   // "Sholazar Basin"
        public const int OptionIdWormhole4 = 3;  // "Icecrown"
        public const int OptionIdWormhole5 = 4;  // "Storm Peaks"
        public const int OptionIdWormhole6 = 5;  // "Underground..."

        //Lunaclawspirit
        public const string ItemGrant = "You Have Thought Well; Spirit. I Ask You To Grant Me The Strength Of Your Body And The Strength Of Your Heart.";

        //Pettrainer
        public const uint MenuIdPetUnlearn = 6520;
        public const uint OptionIdPleaseDo = 0;

        //NpcExperience
        public const uint MenuIdXpOnOff = 10638;
        public const uint OptionIdXpOff = 0;
        public const uint OptionIdXpOn = 1;

        //Argent squire/gruntling
        public const uint OptionIdBank = 0;
        public const uint OptionIdShop = 1;
        public const uint OptionIdMail = 2;
        public const uint OptionIdDarnassusSenjinPennant = 3;
        public const uint OptionIdExodarUndercityPennant = 4;
        public const uint OptionIdGnomereganOrgrimmarPennant = 5;
        public const uint OptionIdIronforgeSilvermoonPennant = 6;
        public const uint OptionIdStormwindThunderbluffPennant = 7;
    }

    internal enum SeatIds
    {
        //BountifulTable
        TurkeyChair = 0,
        CranberryChair = 1,
        StuffingChair = 2,
        SweetPotatoChair = 3,
        PieChair = 4,
        FoodHolder = 5,
        PlateHolder = 6
    }

    internal struct Misc
    {
        public static SpawnAssociation[] spawnAssociations =
        {
            new SpawnAssociation(2614,  15241, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Alliance)
            new SpawnAssociation(2615,  15242, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Horde)
            new SpawnAssociation(21974, 21976, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Area 52)
            new SpawnAssociation(21993, 15242, SpawnType.AlarmBot),                     //Air Force Guard Post (Horde - Bat Rider)
            new SpawnAssociation(21996, 15241, SpawnType.AlarmBot),                     //Air Force Guard Post (Alliance - Gryphon)
            new SpawnAssociation(21997, 21976, SpawnType.AlarmBot),                     //Air Force Guard Post (Goblin - Area 52 - Zeppelin)
            new SpawnAssociation(21999, 15241, SpawnType.TripwireRooftop),             //Air Force Trip Wire - Rooftop (Alliance)
            new SpawnAssociation(22001, 15242, SpawnType.TripwireRooftop),             //Air Force Trip Wire - Rooftop (Horde)
            new SpawnAssociation(22002, 15242, SpawnType.TripwireRooftop),             //Air Force Trip Wire - Ground (Horde)
            new SpawnAssociation(22003, 15241, SpawnType.TripwireRooftop),             //Air Force Trip Wire - Ground (Alliance)
            new SpawnAssociation(22063, 21976, SpawnType.TripwireRooftop),             //Air Force Trip Wire - Rooftop (Goblin - Area 52)
            new SpawnAssociation(22065, 22064, SpawnType.AlarmBot),                     //Air Force Guard Post (Ethereal - Stormspire)
            new SpawnAssociation(22066, 22067, SpawnType.AlarmBot),                     //Air Force Guard Post (Scryer - Dragonhawk)
            new SpawnAssociation(22068, 22064, SpawnType.TripwireRooftop),             //Air Force Trip Wire - Rooftop (Ethereal - Stormspire)
            new SpawnAssociation(22069, 22064, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Stormspire)
            new SpawnAssociation(22070, 22067, SpawnType.TripwireRooftop),             //Air Force Trip Wire - Rooftop (Scryer)
            new SpawnAssociation(22071, 22067, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Scryer)
            new SpawnAssociation(22078, 22077, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Aldor)
            new SpawnAssociation(22079, 22077, SpawnType.AlarmBot),                     //Air Force Guard Post (Aldor - Gryphon)
            new SpawnAssociation(22080, 22077, SpawnType.TripwireRooftop),             //Air Force Trip Wire - Rooftop (Aldor)
            new SpawnAssociation(22086, 22085, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Sporeggar)
            new SpawnAssociation(22087, 22085, SpawnType.AlarmBot),                     //Air Force Guard Post (Sporeggar - Spore Bat)
            new SpawnAssociation(22088, 22085, SpawnType.TripwireRooftop),             //Air Force Trip Wire - Rooftop (Sporeggar)
            new SpawnAssociation(22090, 22089, SpawnType.AlarmBot),                     //Air Force Guard Post (Toshley's Station - Flying Machine)
            new SpawnAssociation(22124, 22122, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Cenarion)
            new SpawnAssociation(22125, 22122, SpawnType.AlarmBot),                     //Air Force Guard Post (Cenarion - Stormcrow)
            new SpawnAssociation(22126, 22122, SpawnType.AlarmBot)                      //Air Force Trip Wire - Rooftop (Cenarion Expedition)
        };

        public const float RangeTripwire = 15.0f;
        public const float RangeGuardsMark = 50.0f;

        //ChickenCluck
        public const uint FactionFriendly = 35;
        public const uint FactionChicken = 31;

        //Doctor
        public static Position[] DoctorAllianceCoords =
        {
            new Position(-3757.38f, -4533.05f, 14.16f, 3.62f),                      // Top-far-right bunk as seen from entrance
            new Position(-3754.36f, -4539.13f, 14.16f, 5.13f),                      // Top-far-left bunk
            new Position(-3749.54f, -4540.25f, 14.28f, 3.34f),                      // Far-right bunk
            new Position(-3742.10f, -4536.85f, 14.28f, 3.64f),                      // Right bunk near entrance
            new Position(-3755.89f, -4529.07f, 14.05f, 0.57f),                      // Far-left bunk
            new Position(-3749.51f, -4527.08f, 14.07f, 5.26f),                      // Mid-left bunk
            new Position(-3746.37f, -4525.35f, 14.16f, 5.22f),                      // Left bunk near entrance
        };

        //alliance run to where
        public static Position DoctorAllianceRunTo = new Position(-3742.96f, -4531.52f, 11.91f);

        public static Position[] DoctorHordeCoords =
        {
            new Position(-1013.75f, -3492.59f, 62.62f, 4.34f),                      // Left, Behind
            new Position(-1017.72f, -3490.92f, 62.62f, 4.34f),                      // Right, Behind
            new Position(-1015.77f, -3497.15f, 62.82f, 4.34f),                      // Left, Mid
            new Position(-1019.51f, -3495.49f, 62.82f, 4.34f),                      // Right, Mid
            new Position(-1017.25f, -3500.85f, 62.98f, 4.34f),                      // Left, front
            new Position(-1020.95f, -3499.21f, 62.98f, 4.34f)                       // Right, Front
        };

        //horde run to where
        public static Position DoctorHordeRunTo = new Position(-1016.44f, -3508.48f, 62.96f);

        public static uint[] AllianceSoldierId =
        {
            12938,                                                  // 12938 Injured Alliance Soldier
            12936,                                                  // 12936 Badly injured Alliance Soldier
            12937                                                   // 12937 Critically injured Alliance Soldier
        };

        public static uint[] HordeSoldierId =
        {
            12923,                                                  //12923 Injured Soldier
            12924,                                                  //12924 Badly injured Soldier
            12925                                                   //12925 Critically injured Soldier
        };

        //    WormholeSpells
        public const uint DataShowUnderground = 1;

        //Fireworks
        public const uint AnimGoLaunchFirework = 3;
        public const uint ZoneMoonglade = 493;

        public static Position omenSummonPos = new Position(7558.993f, -2839.999f, 450.0214f, 4.46f);

        public const uint AuraDurationTimeLeft = 5000;

        //Argent squire/gruntling
        public static Tuple<uint, uint>[] bannerSpells =
        {
            Tuple.Create(SpellIds.DarnassusPennant, SpellIds.SenjinPennant),
            Tuple.Create(SpellIds.ExodarPennant, SpellIds.UndercityPennant),
            Tuple.Create(SpellIds.GnomereganPennant, SpellIds.OrgrimmarPennant),
            Tuple.Create(SpellIds.IronforgePennant, SpellIds.SilvermoonPennant),
            Tuple.Create(SpellIds.StormwindPennant, SpellIds.ThunderbluffPennant)
        };
    }

    [Script]
    internal class npc_air_force_bots : ScriptedAI
    {
        public npc_air_force_bots(Creature creature) : base(creature)
        {
            SpawnAssoc = null;
            SpawnedGUID.Clear();

            // find the correct spawnhandling
            foreach (var association in Misc.spawnAssociations)
            {
                if (association.thisCreatureEntry == creature.GetEntry())
                {
                    SpawnAssoc = association;
                    break;
                }
            }

            if (SpawnAssoc == null)
                Log.outError(LogFilter.Sql, "TCSR: Creature template entry {0} has ScriptName npc_air_force_bots, but it's not handled by that script", creature.GetEntry());
            else
            {
                var spawnedTemplate = Global.ObjectMgr.GetCreatureTemplate(SpawnAssoc.spawnedCreatureEntry);
                if (spawnedTemplate == null)
                {
                    Log.outError(LogFilter.Sql, "TCSR: Creature template entry {0} does not exist in DB, which is required by npc_air_force_bots", SpawnAssoc.spawnedCreatureEntry);
                    SpawnAssoc = null;
                    return;
                }
            }
        }

        private SpawnAssociation SpawnAssoc;
        private ObjectGuid SpawnedGUID;

        public override void Reset() { }

        private Creature SummonGuard()
        {
            Creature summoned = me.SummonCreature(SpawnAssoc.spawnedCreatureEntry, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, 300000);

            if (summoned)
                SpawnedGUID = summoned.GetGUID();
            else
            {
                Log.outError(LogFilter.Sql, "npc_air_force_bots: wasn't able to spawn Creature {0}", SpawnAssoc.spawnedCreatureEntry);
                SpawnAssoc = null;
            }

            return summoned;
        }

        private Creature GetSummonedGuard()
        {
            var creature = ObjectAccessor.GetCreature(me, SpawnedGUID);
            if (creature && creature.IsAlive())
                return creature;

            return null;
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (SpawnAssoc == null)
                return;

            if (me.IsValidAttackTarget(who))
            {
                var playerTarget = who.ToPlayer();

                // airforce guards only spawn for players
                if (!playerTarget)
                    return;

                var lastSpawnedGuard = SpawnedGUID.IsEmpty() ? null : GetSummonedGuard();

                // prevent calling Unit::GetUnit at next MoveInLineOfSight call - speedup
                if (!lastSpawnedGuard)
                    SpawnedGUID.Clear();

                switch (SpawnAssoc.spawnType)
                {
                    case SpawnType.AlarmBot:
                        {
                            if (!who.IsWithinDistInMap(me, Misc.RangeGuardsMark))
                                return;

                            var markAura = who.GetAura(SpellIds.GuardsMark);
                            if (markAura != null)
                            {
                                // the target wasn't able to move out of our range within 25 seconds
                                if (!lastSpawnedGuard)
                                {
                                    lastSpawnedGuard = SummonGuard();

                                    if (!lastSpawnedGuard)
                                        return;
                                }

                                if (markAura.GetDuration() < Misc.AuraDurationTimeLeft)
                                    if (!lastSpawnedGuard.GetVictim())
                                        lastSpawnedGuard.GetAI().AttackStart(who);
                            }
                            else
                            {
                                if (!lastSpawnedGuard)
                                    lastSpawnedGuard = SummonGuard();

                                if (!lastSpawnedGuard)
                                    return;

                                lastSpawnedGuard.CastSpell(who, SpellIds.GuardsMark, true);
                            }
                            break;
                        }
                    case SpawnType.TripwireRooftop:
                        {
                            if (!who.IsWithinDistInMap(me, Misc.RangeTripwire))
                                return;

                            if (!lastSpawnedGuard)
                                lastSpawnedGuard = SummonGuard();

                            if (!lastSpawnedGuard)
                                return;

                            // ROOFTOP only triggers if the player is on the ground
                            if (!playerTarget.IsFlying() && !lastSpawnedGuard.GetVictim())
                                lastSpawnedGuard.GetAI().AttackStart(who);

                            break;
                        }
                }
            }
        }
    }

    [Script]
    internal class npc_chicken_cluck : ScriptedAI
    {
        public npc_chicken_cluck(Creature creature) : base(creature)
        {
            Initialize();
        }

        private void Initialize()
        {
            ResetFlagTimer = 120000;
        }

        private uint ResetFlagTimer;

        public override void Reset()
        {
            Initialize();
            me.SetFaction(Misc.FactionChicken);
            me.RemoveNpcFlag(NPCFlags.QuestGiver);
        }

        public override void EnterCombat(Unit who) { }

        public override void UpdateAI(uint diff)
        {
            // Reset flags after a certain time has passed so that the next player has to start the 'event' again
            if (me.HasNpcFlag(NPCFlags.QuestGiver))
            {
                if (ResetFlagTimer <= diff)
                {
                    EnterEvadeMode();
                    return;
                }
                else
                    ResetFlagTimer -= diff;
            }

            if (UpdateVictim())
                DoMeleeAttackIfReady();
        }

        public override void ReceiveEmote(Player player, TextEmotes emote)
        {
            switch (emote)
            {
                case TextEmotes.Chicken:
                    if (player.GetQuestStatus(QuestConst.Cluck) == QuestStatus.None && RandomHelper.Rand32() % 30 == 1)
                    {
                        me.AddNpcFlag(NPCFlags.QuestGiver);
                        me.SetFaction(Misc.FactionFriendly);
                        Talk(player.GetTeam() == Team.Horde ? TextIds.EmoteHelloH : TextIds.EmoteHelloA);
                    }
                    break;
                case TextEmotes.Cheer:
                    if (player.GetQuestStatus(QuestConst.Cluck) == QuestStatus.Complete)
                    {
                        me.AddNpcFlag(NPCFlags.QuestGiver);
                        me.SetFaction(Misc.FactionFriendly);
                        Talk(TextIds.EmoteCluck);
                    }
                    break;
            }
        }

        public override void QuestAccept(Player player, Quest quest)
        {
            if (quest.Id == QuestConst.Cluck)
                Reset();
        }

        public override void QuestReward(Player player, Quest quest, uint opt)
        {
            if (quest.Id == QuestConst.Cluck)
                Reset();
        }
    }

    [Script]
    internal class npc_dancing_flames : ScriptedAI
    {
        public npc_dancing_flames(Creature creature) : base(creature)
        {
            Initialize();
        }

        private void Initialize()
        {
            Active = true;
            CanIteract = 3500;
        }

        private bool Active;
        private uint CanIteract;

        public override void Reset()
        {
            Initialize();
            DoCast(me, SpellIds.Brazier, true);
            DoCast(me, SpellIds.FieryAura, false);
            float x, y, z;
            me.GetPosition(out x, out y, out z);
            me.Relocate(x, y, z + 0.94f);
            me.SetDisableGravity(true);
            me.HandleEmoteCommand(Emote.OneshotDance);
        }

        public override void UpdateAI(uint diff)
        {
            if (!Active)
            {
                if (CanIteract <= diff)
                {
                    Active = true;
                    CanIteract = 3500;
                    me.HandleEmoteCommand(Emote.OneshotDance);
                }
                else
                    CanIteract -= diff;
            }
        }

        public override void EnterCombat(Unit who) { }

        public override void ReceiveEmote(Player player, TextEmotes emote)
        {
            if (me.IsWithinLOS(player.GetPositionX(), player.GetPositionY(), player.GetPositionZ()) && me.IsWithinDistInMap(player, 30.0f))
            {
                me.SetFacingToObject(player);
                Active = false;

                switch (emote)
                {
                    case TextEmotes.Kiss:
                        me.HandleEmoteCommand(Emote.OneshotShy);
                        break;
                    case TextEmotes.Wave:
                        me.HandleEmoteCommand(Emote.OneshotWave);
                        break;
                    case TextEmotes.Bow:
                        me.HandleEmoteCommand(Emote.OneshotBow);
                        break;
                    case TextEmotes.Joke:
                        me.HandleEmoteCommand(Emote.OneshotLaugh);
                        break;
                    case TextEmotes.Dance:
                        if (!player.HasAura(SpellIds.Seduction))
                            DoCast(player, SpellIds.Seduction, true);
                        break;
                }
            }
        }
    }

    [Script]
    internal class npc_torch_tossing_target_bunny_controller : ScriptedAI
    {
        public npc_torch_tossing_target_bunny_controller(Creature creature) : base(creature)
        {
            _targetTimer = 3000;
        }

        private ObjectGuid DoSearchForTargets(ObjectGuid lastTargetGUID)
        {
            var targets = new List<Creature>();
            me.GetCreatureListWithEntryInGrid(targets, CreatureIds.TorchTossingTargetBunny, 60.0f);
            targets.RemoveAll(creature => creature.GetGUID() == lastTargetGUID);

            if (!targets.Empty())
            {
                _lastTargetGUID = targets.SelectRandom().GetGUID();

                return _lastTargetGUID;
            }
            return ObjectGuid.Empty;
        }

        public override void UpdateAI(uint diff)
        {
            if (_targetTimer < diff)
            {
                var target = Global.ObjAccessor.GetUnit(me, DoSearchForTargets(_lastTargetGUID));
                if (target)
                    target.CastSpell(target, SpellIds.TargetIndicator, true);

                _targetTimer = 3000;
            }
            else
                _targetTimer -= diff;
        }

        private uint _targetTimer;
        private ObjectGuid _lastTargetGUID;
    }

    [Script]
    internal class npc_midsummer_bunny_pole : ScriptedAI
    {
        public npc_midsummer_bunny_pole(Creature creature) : base(creature)
        {
            Initialize();
        }

        private void Initialize()
        {
            _scheduler.CancelAll();
            running = false;
        }

        public override void Reset()
        {
            Initialize();

            _scheduler.SetValidator(() => running);

            _scheduler.Schedule(TimeSpan.FromMilliseconds(1), task =>
            {
                if (checkNearbyPlayers())
                {
                    Reset();
                    return;
                }

                var go = me.FindNearestGameObject(GameobjectIds.RibbonPole, 10.0f);
                if (go)
                    me.CastSpell(go, SpellIds.RedFireRing, true);

                task.Schedule(TimeSpan.FromSeconds(5), task1 =>
                {
                    if (checkNearbyPlayers())
                    {
                        Reset();
                        return;
                    }

                    go = me.FindNearestGameObject(GameobjectIds.RibbonPole, 10.0f);
                    if (go)
                        me.CastSpell(go, SpellIds.BlueFireRing, true);

                    task.Repeat(TimeSpan.FromSeconds(5));
                });
            });
        }

        public override void DoAction(int action)
        {
            // Don't start event if it's already running.
            if (running)
                return;

            running = true;
            //events.ScheduleEvent(EVENT_CAST_RED_FIRE_RING, 1);
        }

        private bool checkNearbyPlayers()
        {
            // Returns true if no nearby player has aura "Test Ribbon Pole Channel".
            var players = new List<Unit>();
            var check = new UnitAuraCheck<Player>(true, SpellIds.RibbonDanceCosmetic);
            var searcher = new PlayerListSearcher(me, players, check);
            Cell.VisitWorldObjects(me, searcher, 10.0f);

            return players.Empty();
        }

        public override void UpdateAI(uint diff)
        {
            if (!running)
                return;

            _scheduler.Update(diff);
        }

        private bool running;
    }

    [Script]
    internal class npc_doctor : ScriptedAI
    {
        public npc_doctor(Creature creature) : base(creature)
        {
            Initialize();
        }

        private void Initialize()
        {
            PlayerGUID.Clear();

            SummonPatientTimer = 10000;
            SummonPatientCount = 0;
            PatientDiedCount = 0;
            PatientSavedCount = 0;

            Patients.Clear();
            Coordinates.Clear();

            Event = false;
        }

        public override void Reset()
        {
            Initialize();
            me.RemoveUnitFlag(UnitFlags.NotSelectable);
        }

        public void BeginEvent(Player player)
        {
            PlayerGUID = player.GetGUID();

            SummonPatientTimer = 10000;
            SummonPatientCount = 0;
            PatientDiedCount = 0;
            PatientSavedCount = 0;

            switch (me.GetEntry())
            {
                case CreatureIds.DoctorAlliance:
                    foreach (var coord in Misc.DoctorAllianceCoords)
                        Coordinates.Add(coord);
                    break;
                case CreatureIds.DoctorHorde:
                    foreach (var coord in Misc.DoctorHordeCoords)
                        Coordinates.Add(coord);
                    break;
            }

            Event = true;
            me.AddUnitFlag(UnitFlags.NotSelectable);
        }

        public void PatientDied(Position point)
        {
            var player = Global.ObjAccessor.GetPlayer(me, PlayerGUID);
            if (player && ((player.GetQuestStatus(6624) == QuestStatus.Incomplete) || (player.GetQuestStatus(6622) == QuestStatus.Incomplete)))
            {
                ++PatientDiedCount;

                if (PatientDiedCount > 5 && Event)
                {
                    if (player.GetQuestStatus(6624) == QuestStatus.Incomplete)
                        player.FailQuest(6624);
                    else if (player.GetQuestStatus(6622) == QuestStatus.Incomplete)
                        player.FailQuest(6622);

                    Reset();
                    return;
                }

                Coordinates.Add(point);
            }
            else
                // If no player or player abandon quest in progress
                Reset();
        }

        public void PatientSaved(Creature soldier, Player player, Position point)
        {
            if (player && PlayerGUID == player.GetGUID())
            {
                if ((player.GetQuestStatus(6624) == QuestStatus.Incomplete) || (player.GetQuestStatus(6622) == QuestStatus.Incomplete))
                {
                    ++PatientSavedCount;

                    if (PatientSavedCount == 15)
                    {
                        if (!Patients.Empty())
                        {
                            foreach (var guid in Patients)
                            {
                                var patient = ObjectAccessor.GetCreature(me, guid);
                                if (patient)
                                    patient.SetDeathState(DeathState.JustDied);
                            }
                        }

                        if (player.GetQuestStatus(6624) == QuestStatus.Incomplete)
                            player.AreaExploredOrEventHappens(6624);
                        else if (player.GetQuestStatus(6622) == QuestStatus.Incomplete)
                            player.AreaExploredOrEventHappens(6622);

                        Reset();
                        return;
                    }

                    Coordinates.Add(point);
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (Event && SummonPatientCount >= 20)
            {
                Reset();
                return;
            }

            if (Event)
            {
                if (SummonPatientTimer <= diff)
                {
                    if (Coordinates.Empty())
                        return;

                    uint patientEntry;
                    switch (me.GetEntry())
                    {
                        case CreatureIds.DoctorAlliance:
                            patientEntry = Misc.AllianceSoldierId[RandomHelper.Rand32() % 3];
                            break;
                        case CreatureIds.DoctorHorde:
                            patientEntry = Misc.HordeSoldierId[RandomHelper.Rand32() % 3];
                            break;
                        default:
                            Log.outError(LogFilter.Scripts, "Invalid entry for Triage doctor. Please check your database");
                            return;
                    }

                    var index = RandomHelper.IRand(0, Coordinates.Count - 1);

                    Creature Patient = me.SummonCreature(patientEntry, Coordinates[index], TempSummonType.TimedDespawnOutOfCombat, 5000);
                    if (Patient)
                    {
                        //303, this flag appear to be required for client side item.spell to work (TARGET_SINGLE_FRIEND)
                        Patient.AddUnitFlag(UnitFlags.PvpAttackable);

                        Patients.Add(Patient.GetGUID());
                        ((npc_injured_patient)Patient.GetAI()).DoctorGUID = me.GetGUID();
                        ((npc_injured_patient)Patient.GetAI()).Coord = Coordinates[index];

                        Coordinates.RemoveAt(index);
                    }

                    SummonPatientTimer = 10000;
                    ++SummonPatientCount;
                }
                else
                    SummonPatientTimer -= diff;
            }
        }

        public override void EnterCombat(Unit who) { }

        public override void QuestAccept(Player player, Quest quest)
        {
            if ((quest.Id == 6624) || (quest.Id == 6622))
                BeginEvent(player);
        }

        private ObjectGuid PlayerGUID;

        private uint SummonPatientTimer;
        private uint SummonPatientCount;
        private uint PatientDiedCount;
        private uint PatientSavedCount;

        private bool Event;

        private List<ObjectGuid> Patients = new List<ObjectGuid>();
        private List<Position> Coordinates = new List<Position>();
    }

    [Script]
    public class npc_injured_patient : ScriptedAI
    {
        public npc_injured_patient(Creature creature) : base(creature)
        {
            Initialize();
        }

        private void Initialize()
        {
            DoctorGUID.Clear();
            Coord = null;
        }

        public ObjectGuid DoctorGUID;
        public Position Coord;

        public override void Reset()
        {
            Initialize();

            //no select
            me.RemoveUnitFlag(UnitFlags.NotSelectable);

            //no regen health
            me.AddUnitFlag(UnitFlags.InCombat);

            //to make them lay with face down
            me.SetStandState(UnitStandStateType.Dead);

            var mobId = me.GetEntry();

            switch (mobId)
            {                                                   //lower max health
                case 12923:
                case 12938:                                     //Injured Soldier
                    me.SetHealth(me.CountPctFromMaxHealth(75));
                    break;
                case 12924:
                case 12936:                                     //Badly injured Soldier
                    me.SetHealth(me.CountPctFromMaxHealth(50));
                    break;
                case 12925:
                case 12937:                                     //Critically injured Soldier
                    me.SetHealth(me.CountPctFromMaxHealth(25));
                    break;
            }
        }

        public override void EnterCombat(Unit who) { }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            var player = caster.ToPlayer();
            if (!player || !me.IsAlive() || spell.Id != 20804)
                return;

            if (player.GetQuestStatus(6624) == QuestStatus.Incomplete || player.GetQuestStatus(6622) == QuestStatus.Incomplete)
            {
                if (!DoctorGUID.IsEmpty())
                {
                    var doctor = ObjectAccessor.GetCreature(me, DoctorGUID);
                    if (doctor)
                        ((npc_doctor)doctor.GetAI()).PatientSaved(me, player, Coord);
                }
            }

            //make not selectable
            me.AddUnitFlag(UnitFlags.NotSelectable);

            //regen health
            me.RemoveUnitFlag(UnitFlags.InCombat);

            //stand up
            me.SetStandState(UnitStandStateType.Stand);

            Talk(TextIds.SayDoc);

            var mobId = me.GetEntry();
            me.SetWalk(false);

            switch (mobId)
            {
                case 12923:
                case 12924:
                case 12925:
                    me.GetMotionMaster().MovePoint(0, Misc.DoctorHordeRunTo);
                    break;
                case 12936:
                case 12937:
                case 12938:
                    me.GetMotionMaster().MovePoint(0, Misc.DoctorAllianceRunTo);
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            //lower HP on every world tick makes it a useful counter, not officlone though
            if (me.IsAlive() && me.GetHealth() > 6)
                me.ModifyHealth(-5);

            if (me.IsAlive() && me.GetHealth() <= 6)
            {
                me.RemoveUnitFlag(UnitFlags.InCombat);
                me.AddUnitFlag(UnitFlags.NotSelectable);
                me.SetDeathState(DeathState.JustDied);
                me.AddDynamicFlag(UnitDynFlags.Dead);

                if (!DoctorGUID.IsEmpty())
                {
                    var doctor = ObjectAccessor.GetCreature((me), DoctorGUID);
                    if (doctor)
                        ((npc_doctor)doctor.GetAI()).PatientDied(Coord);
                }
            }
        }
    }

    [Script]
    internal class npc_garments_of_quests : EscortAI
    {
        public npc_garments_of_quests(Creature creature) : base(creature)
        {
            switch (me.GetEntry())
            {
                case CreatureIds.Shaya:
                    quest = QuestConst.Moon;
                    break;
                case CreatureIds.Roberts:
                    quest = QuestConst.Light1;
                    break;
                case CreatureIds.Dolf:
                    quest = QuestConst.Light2;
                    break;
                case CreatureIds.Korja:
                    quest = QuestConst.Spirit;
                    break;
                case CreatureIds.DgKel:
                    quest = QuestConst.Darkness;
                    break;
                default:
                    quest = 0;
                    break;
            }

            Reset();
        }

        private ObjectGuid CasterGUID;

        private bool IsHealed;
        private bool CanRun;

        private uint RunAwayTimer;
        private uint quest;

        public override void Reset()
        {
            CasterGUID.Clear();

            IsHealed = false;
            CanRun = false;

            RunAwayTimer = 5000;

            me.SetStandState(UnitStandStateType.Kneel);
            // expect database to have RegenHealth=0
            me.SetHealth(me.CountPctFromMaxHealth(70));
        }

        public override void EnterCombat(Unit who) { }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            if (spell.Id == SpellIds.LesserHealR2 || spell.Id == SpellIds.FortitudeR1)
            {
                //not while in combat
                if (me.IsInCombat())
                    return;

                //nothing to be done now
                if (IsHealed && CanRun)
                    return;

                var player = caster.ToPlayer();
                if (player)
                {
                    if (quest != 0 && player.GetQuestStatus(quest) == QuestStatus.Incomplete)
                    {
                        if (IsHealed && !CanRun && spell.Id == SpellIds.FortitudeR1)
                        {
                            Talk(TextIds.SayThanks, caster);
                            CanRun = true;
                        }
                        else if (!IsHealed && spell.Id == SpellIds.LesserHealR2)
                        {
                            CasterGUID = caster.GetGUID();
                            me.SetStandState(UnitStandStateType.Stand);
                            Talk(TextIds.SayHealed, caster);
                            IsHealed = true;
                        }
                    }

                    // give quest credit, not expect any special quest objectives
                    if (CanRun)
                        player.TalkedToCreature(me.GetEntry(), me.GetGUID());
                }
            }
        }

        public override void WaypointReached(uint waypointId, uint pathId)
        {
        }

        public override void UpdateAI(uint diff)
        {
            if (CanRun && !me.IsInCombat())
            {
                if (RunAwayTimer <= diff)
                {
                    var unit = Global.ObjAccessor.GetUnit(me, CasterGUID);
                    if (unit)
                    {
                        switch (me.GetEntry())
                        {
                            case CreatureIds.Shaya:
                            case CreatureIds.Roberts:
                            case CreatureIds.Dolf:
                            case CreatureIds.Korja:
                            case CreatureIds.DgKel:
                                Talk(TextIds.SayGoodbye, unit);
                                break;
                        }

                        Start(false, true);
                    }
                    else
                        EnterEvadeMode();                       //something went wrong

                    RunAwayTimer = 30000;
                }
                else
                    RunAwayTimer -= diff;
            }

            base.UpdateAI(diff);
        }
    }

    [Script]
    internal class npc_guardian : ScriptedAI
    {
        public npc_guardian(Creature creature) : base(creature) { }

        public override void Reset()
        {
            me.AddUnitFlag(UnitFlags.NonAttackable);
        }

        public override void EnterCombat(Unit who)
        {
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (me.IsAttackReady())
            {
                DoCastVictim(SpellIds.Deathtouch, true);
                me.ResetAttackTimer();
            }
        }
    }

    [Script]
    internal class npc_sayge : ScriptedAI
    {
        public npc_sayge(Creature creature) : base(creature) { }

        public override bool GossipHello(Player player)
        {
            if (me.IsQuestGiver())
                player.PrepareQuestMenu(me.GetGUID());

            if (player.GetSpellHistory().HasCooldown(SpellIds.Strength) ||
                player.GetSpellHistory().HasCooldown(SpellIds.Agility) ||
                player.GetSpellHistory().HasCooldown(SpellIds.Stamina) ||
                player.GetSpellHistory().HasCooldown(SpellIds.Spirit) ||
                player.GetSpellHistory().HasCooldown(SpellIds.Intellect) ||
                player.GetSpellHistory().HasCooldown(SpellIds.Armor) ||
                player.GetSpellHistory().HasCooldown(SpellIds.Damage) ||
                player.GetSpellHistory().HasCooldown(SpellIds.Resistance))
                player.SendGossipMenu(GossipMenus.CantGiveYouYour, me.GetGUID());
            else
            {
                player.ADD_GOSSIP_ITEM_DB(GossipMenus.IAmReadyToDiscover, GossipMenus.OptionIdAnswer1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                player.SendGossipMenu(GossipMenus.IHaveLongKnown, me.GetGUID());
            }

            return true;
        }

        private void SendAction(Player player, uint action)
        {
            switch (action)
            {
                case eTradeskill.GossipActionInfoDef + 1:
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge, GossipMenus.OptionIdAnswer1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge, GossipMenus.OptionIdAnswer2, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 3);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge, GossipMenus.OptionIdAnswer3, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 4);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge, GossipMenus.OptionIdAnswer4, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 5);
                    player.SendGossipMenu(GossipMenus.YouHaveBeenTasked, me.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 2:
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge2, GossipMenus.OptionIdAnswer1, eTradeskill.GossipSenderMain + 1, eTradeskill.GossipActionInfoDef);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge2, GossipMenus.OptionIdAnswer2, eTradeskill.GossipSenderMain + 2, eTradeskill.GossipActionInfoDef);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge2, GossipMenus.OptionIdAnswer3, eTradeskill.GossipSenderMain + 3, eTradeskill.GossipActionInfoDef);
                    player.SendGossipMenu(GossipMenus.SwornExecutioner, me.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 3:
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge3, GossipMenus.OptionIdAnswer1, eTradeskill.GossipSenderMain + 4, eTradeskill.GossipActionInfoDef);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge3, GossipMenus.OptionIdAnswer2, eTradeskill.GossipSenderMain + 5, eTradeskill.GossipActionInfoDef);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge3, GossipMenus.OptionIdAnswer3, eTradeskill.GossipSenderMain + 2, eTradeskill.GossipActionInfoDef);
                    player.SendGossipMenu(GossipMenus.DiplomaticMission, me.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 4:
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge4, GossipMenus.OptionIdAnswer1, eTradeskill.GossipSenderMain + 6, eTradeskill.GossipActionInfoDef);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge4, GossipMenus.OptionIdAnswer2, eTradeskill.GossipSenderMain + 7, eTradeskill.GossipActionInfoDef);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge4, GossipMenus.OptionIdAnswer3, eTradeskill.GossipSenderMain + 8, eTradeskill.GossipActionInfoDef);
                    player.SendGossipMenu(GossipMenus.YourBrotherSeeks, me.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 5:
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge5, GossipMenus.OptionIdAnswer1, eTradeskill.GossipSenderMain + 5, eTradeskill.GossipActionInfoDef);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge5, GossipMenus.OptionIdAnswer2, eTradeskill.GossipSenderMain + 4, eTradeskill.GossipActionInfoDef);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge5, GossipMenus.OptionIdAnswer3, eTradeskill.GossipSenderMain + 3, eTradeskill.GossipActionInfoDef);
                    player.SendGossipMenu(GossipMenus.ATerribleBeast, me.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef:
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.OptionSayge6, GossipMenus.OptionIdAnswer1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 6);
                    player.SendGossipMenu(GossipMenus.YourFortuneIsCast, me.GetGUID());
                    break;
                case eTradeskill.GossipActionInfoDef + 6:
                    DoCast(player, SpellIds.Fortune, false);
                    player.SendGossipMenu(GossipMenus.HereIsYourFortune, me.GetGUID());
                    break;
            }
        }

        public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
        {
            var sender = player.PlayerTalkClass.GetGossipOptionSender(gossipListId);
            var action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.PlayerTalkClass.ClearMenus();
            uint spellId = 0;
            switch (sender)
            {
                case eTradeskill.GossipSenderMain:
                    SendAction(player, action);
                    break;
                case eTradeskill.GossipSenderMain + 1:
                    spellId = SpellIds.Damage;
                    break;
                case eTradeskill.GossipSenderMain + 2:
                    spellId = SpellIds.Resistance;
                    break;
                case eTradeskill.GossipSenderMain + 3:
                    spellId = SpellIds.Armor;
                    break;
                case eTradeskill.GossipSenderMain + 4:
                    spellId = SpellIds.Spirit;
                    break;
                case eTradeskill.GossipSenderMain + 5:
                    spellId = SpellIds.Intellect;
                    break;
                case eTradeskill.GossipSenderMain + 6:
                    spellId = SpellIds.Stamina;
                    break;
                case eTradeskill.GossipSenderMain + 7:
                    spellId = SpellIds.Strength;
                    break;
                case eTradeskill.GossipSenderMain + 8:
                    spellId = SpellIds.Agility;
                    break;
            }

            if (spellId != 0)
            {
                DoCast(player, spellId, false);
                player.GetSpellHistory().AddCooldown(spellId, 0, TimeSpan.FromHours(2));
                SendAction(player, action);
            }
            return true;
        }
    }

    [Script]
    internal class npc_steam_tonk : ScriptedAI
    {
        public npc_steam_tonk(Creature creature) : base(creature) { }

        public override void Reset() { }
        public override void EnterCombat(Unit who) { }

        public override void OnPossess(bool apply)
        {
            if (apply)
            {
                // Initialize the action bar without the melee attack command
                me.InitCharmInfo();
                me.GetCharmInfo().InitEmptyActionBar(false);

                me.SetReactState(ReactStates.Passive);
            }
            else
                me.SetReactState(ReactStates.Aggressive);
        }
    }

    [Script]
    internal class npc_tonk_mine : ScriptedAI
    {
        public npc_tonk_mine(Creature creature) : base(creature)
        {
            Initialize();
            me.SetReactState(ReactStates.Passive);
        }

        private void Initialize()
        {
            ExplosionTimer = 3000;
        }

        private uint ExplosionTimer;

        public override void Reset()
        {
            Initialize();
        }

        public override void EnterCombat(Unit who) { }
        public override void AttackStart(Unit who) { }
        public override void MoveInLineOfSight(Unit who) { }

        public override void UpdateAI(uint diff)
        {
            if (ExplosionTimer <= diff)
            {
                DoCast(me, SpellIds.TonkMineDetonate, true);
                me.SetDeathState(DeathState.Dead); // unsummon it
            }
            else
                ExplosionTimer -= diff;
        }
    }

    [Script]
    internal class npc_brewfest_reveler : ScriptedAI
    {
        public npc_brewfest_reveler(Creature creature) : base(creature) { }

        public override void ReceiveEmote(Player player, TextEmotes emote)
        {
            if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest))
                return;

            if (emote == TextEmotes.Dance)
                me.CastSpell(player, SpellIds.BrewfestToast, false);
        }
    }

    [Script]
    internal class npc_training_dummy : ScriptedAI
    {
        public npc_training_dummy(Creature creature) : base(creature)
        {
            SetCombatMovement(false);
        }

        public override void Reset()
        {
            // TODO: solve this in a different way! setting them as stunned prevents dummies from parrying
            me.SetControlled(true, UnitState.Stunned);//disable rotate

            _scheduler.CancelAll();
            _damageTimes.Clear();
            if (me.GetEntry() != AdvancedTargetDummy && me.GetEntry() != TargetDummy)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
                {
                    var now = Time.UnixTime;
                    foreach (var pair in _damageTimes.ToList())
                    {
                        // If unit has not dealt damage to training dummy for 5 seconds, Remove him from combat
                        if (pair.Value < now - 5)
                        {
                            var unit = Global.ObjAccessor.GetUnit(me, pair.Key);
                            if (unit)
                                unit.GetHostileRefManager().DeleteReference(me);

                            _damageTimes.Remove(pair.Key);
                        }
                    }
                    task.Repeat();
                });
            }
            else
                _scheduler.Schedule(TimeSpan.FromSeconds(15), task => me.DespawnOrUnsummon());
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            if (!_EnterEvadeMode(why))
                return;

            Reset();
        }

        public override void DamageTaken(Unit doneBy, ref uint damage)
        {
            AddThreat(doneBy, damage);    // just to create threat reference
            _damageTimes[doneBy.GetGUID()] = Time.UnixTime;
            damage = 0;
        }

        public override void UpdateAI(uint diff)
        {
            if (!me.IsInCombat())
                return;

            if (!me.HasUnitState(UnitState.Stunned))
                me.SetControlled(true, UnitState.Stunned);//disable rotate

            _scheduler.Update(diff);
        }

        public override void MoveInLineOfSight(Unit who) { }

        private Dictionary<ObjectGuid, long> _damageTimes = new Dictionary<ObjectGuid, long>();

        private const uint AdvancedTargetDummy = 2674;
        private const uint TargetDummy = 2673;
    }

    [Script]
    internal class npc_wormhole : PassiveAI
    {
        public npc_wormhole(Creature creature) : base(creature)
        {
            Initialize();
        }

        private void Initialize()
        {
            _showUnderground = RandomHelper.URand(0, 100) == 0; // Guessed value, it is really rare though
        }

        public override void InitializeAI()
        {
            Initialize();
        }

        public override bool GossipHello(Player player)
        {
            if (me.IsSummon())
            {
                if (player == me.ToTempSummon().GetSummoner())
                {
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole2, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole3, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 3);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole4, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 4);
                    player.ADD_GOSSIP_ITEM_DB(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 5);

                    if (_showUnderground)
                        player.ADD_GOSSIP_ITEM_DB(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole6, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 6);

                    player.SendGossipMenu(TextIds.Wormhole, me.GetGUID());
                }
            }

            return true;
        }

        public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
        {
            var action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.PlayerTalkClass.ClearMenus();

            switch (action)
            {
                case eTradeskill.GossipActionInfoDef + 1: // Borean Tundra
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.BoreanTundra, false);
                    break;
                case eTradeskill.GossipActionInfoDef + 2: // Howling Fjord
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.HowlingFjord, false);
                    break;
                case eTradeskill.GossipActionInfoDef + 3: // Sholazar Basin
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.SholazarBasin, false);
                    break;
                case eTradeskill.GossipActionInfoDef + 4: // Icecrown
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.Icecrown, false);
                    break;
                case eTradeskill.GossipActionInfoDef + 5: // Storm peaks
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.StormPeaks, false);
                    break;
                case eTradeskill.GossipActionInfoDef + 6: // Underground
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.Underground, false);
                    break;
            }

            return true;
        }

        private bool _showUnderground;
    }

    [Script]
    internal class npc_experience : ScriptedAI
    {
        public npc_experience(Creature creature) : base(creature) { }

        public override bool GossipHello(Player player)
        {
            if (player.HasPlayerFlag(PlayerFlags.NoXPGain)) // not gaining XP
            {
                player.ADD_GOSSIP_ITEM_DB(GossipMenus.MenuIdXpOnOff, GossipMenus.OptionIdXpOn, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                player.SendGossipMenu(TextIds.XpOnOff, me.GetGUID());
            }
            else // currently gaining XP
            {
                player.ADD_GOSSIP_ITEM_DB(GossipMenus.MenuIdXpOnOff, GossipMenus.OptionIdXpOff, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                player.SendGossipMenu(TextIds.XpOnOff, me.GetGUID());
            }
            return true;
        }

        public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
        {
            var action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.PlayerTalkClass.ClearMenus();

            switch (action)
            {
                case eTradeskill.GossipActionInfoDef + 1:// XP ON selected
                    player.RemovePlayerFlag(PlayerFlags.NoXPGain); // turn on XP gain
                    break;
                case eTradeskill.GossipActionInfoDef + 2:// XP OFF selected
                    player.AddPlayerFlag(PlayerFlags.NoXPGain); // turn off XP gain
                    break;
            }

            player.PlayerTalkClass.SendCloseGossip();
            return true;
        }
    }

    [Script]
    internal class npc_firework : ScriptedAI
    {
        public npc_firework(Creature creature) : base(creature) { }

        private bool isCluster()
        {
            switch (me.GetEntry())
            {
                case CreatureIds.FireworkBlue:
                case CreatureIds.FireworkGreen:
                case CreatureIds.FireworkPurple:
                case CreatureIds.FireworkRed:
                case CreatureIds.FireworkYellow:
                case CreatureIds.FireworkWhite:
                case CreatureIds.FireworkBigBlue:
                case CreatureIds.FireworkBigGreen:
                case CreatureIds.FireworkBigPurple:
                case CreatureIds.FireworkBigRed:
                case CreatureIds.FireworkBigYellow:
                case CreatureIds.FireworkBigWhite:
                    return false;
                case CreatureIds.ClusterBlue:
                case CreatureIds.ClusterGreen:
                case CreatureIds.ClusterPurple:
                case CreatureIds.ClusterRed:
                case CreatureIds.ClusterYellow:
                case CreatureIds.ClusterWhite:
                case CreatureIds.ClusterBigBlue:
                case CreatureIds.ClusterBigGreen:
                case CreatureIds.ClusterBigPurple:
                case CreatureIds.ClusterBigRed:
                case CreatureIds.ClusterBigYellow:
                case CreatureIds.ClusterBigWhite:
                case CreatureIds.ClusterElune:
                default:
                    return true;
            }
        }

        private GameObject FindNearestLauncher()
        {
            GameObject launcher = null;

            if (isCluster())
            {
                var launcher1 = GetClosestGameObjectWithEntry(me, GameobjectIds.ClusterLauncher1, 0.5f);
                var launcher2 = GetClosestGameObjectWithEntry(me, GameobjectIds.ClusterLauncher2, 0.5f);
                var launcher3 = GetClosestGameObjectWithEntry(me, GameobjectIds.ClusterLauncher3, 0.5f);
                var launcher4 = GetClosestGameObjectWithEntry(me, GameobjectIds.ClusterLauncher4, 0.5f);

                if (launcher1)
                    launcher = launcher1;
                else if (launcher2)
                    launcher = launcher2;
                else if (launcher3)
                    launcher = launcher3;
                else if (launcher4)
                    launcher = launcher4;
            }
            else
            {
                var launcher1 = GetClosestGameObjectWithEntry(me, GameobjectIds.FireworkLauncher1, 0.5f);
                var launcher2 = GetClosestGameObjectWithEntry(me, GameobjectIds.FireworkLauncher2, 0.5f);
                var launcher3 = GetClosestGameObjectWithEntry(me, GameobjectIds.FireworkLauncher3, 0.5f);

                if (launcher1)
                    launcher = launcher1;
                else if (launcher2)
                    launcher = launcher2;
                else if (launcher3)
                    launcher = launcher3;
            }

            return launcher;
        }

        private uint GetFireworkSpell(uint entry)
        {
            switch (entry)
            {
                case CreatureIds.FireworkBlue:
                    return SpellIds.RocketBlue;
                case CreatureIds.FireworkGreen:
                    return SpellIds.RocketGreen;
                case CreatureIds.FireworkPurple:
                    return SpellIds.RocketPurple;
                case CreatureIds.FireworkRed:
                    return SpellIds.RocketRed;
                case CreatureIds.FireworkYellow:
                    return SpellIds.RocketYellow;
                case CreatureIds.FireworkWhite:
                    return SpellIds.RocketWhite;
                case CreatureIds.FireworkBigBlue:
                    return SpellIds.RocketBigBlue;
                case CreatureIds.FireworkBigGreen:
                    return SpellIds.RocketBigGreen;
                case CreatureIds.FireworkBigPurple:
                    return SpellIds.RocketBigPurple;
                case CreatureIds.FireworkBigRed:
                    return SpellIds.RocketBigRed;
                case CreatureIds.FireworkBigYellow:
                    return SpellIds.RocketBigYellow;
                case CreatureIds.FireworkBigWhite:
                    return SpellIds.RocketBigWhite;
                default:
                    return 0;
            }
        }

        private uint GetFireworkGameObjectId()
        {
            uint spellId = 0;

            switch (me.GetEntry())
            {
                case CreatureIds.ClusterBlue:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBlue);
                    break;
                case CreatureIds.ClusterGreen:
                    spellId = GetFireworkSpell(CreatureIds.FireworkGreen);
                    break;
                case CreatureIds.ClusterPurple:
                    spellId = GetFireworkSpell(CreatureIds.FireworkPurple);
                    break;
                case CreatureIds.ClusterRed:
                    spellId = GetFireworkSpell(CreatureIds.FireworkRed);
                    break;
                case CreatureIds.ClusterYellow:
                    spellId = GetFireworkSpell(CreatureIds.FireworkYellow);
                    break;
                case CreatureIds.ClusterWhite:
                    spellId = GetFireworkSpell(CreatureIds.FireworkWhite);
                    break;
                case CreatureIds.ClusterBigBlue:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigBlue);
                    break;
                case CreatureIds.ClusterBigGreen:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigGreen);
                    break;
                case CreatureIds.ClusterBigPurple:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigPurple);
                    break;
                case CreatureIds.ClusterBigRed:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigRed);
                    break;
                case CreatureIds.ClusterBigYellow:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigYellow);
                    break;
                case CreatureIds.ClusterBigWhite:
                    spellId = GetFireworkSpell(CreatureIds.FireworkBigWhite);
                    break;
                case CreatureIds.ClusterElune:
                    spellId = GetFireworkSpell(RandomHelper.URand(CreatureIds.FireworkBlue, CreatureIds.FireworkWhite));
                    break;
            }

            var spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            if (spellInfo != null && spellInfo.GetEffect(0).Effect == SpellEffectName.SummonObjectWild)
                return (uint)spellInfo.GetEffect(0).MiscValue;

            return 0;
        }

        public override void Reset()
        {
            var launcher = FindNearestLauncher();
            if (launcher)
            {
                launcher.SendCustomAnim(Misc.AnimGoLaunchFirework);
                me.SetOrientation(launcher.GetOrientation() + MathFunctions.PI / 2);
            }
            else
                return;

            if (isCluster())
            {
                // Check if we are near Elune'ara lake south, if so try to summon Omen or a minion
                if (me.GetZoneId() == Misc.ZoneMoonglade)
                {
                    if (!me.FindNearestCreature(CreatureIds.Omen, 100.0f) && me.GetDistance2d(Misc.omenSummonPos.GetPositionX(), Misc.omenSummonPos.GetPositionY()) <= 100.0f)
                    {
                        switch (RandomHelper.URand(0, 9))
                        {
                            case 0:
                            case 1:
                            case 2:
                            case 3:
                                Creature minion = me.SummonCreature(CreatureIds.MinionOfOmen, me.GetPositionX() + RandomHelper.FRand(-5.0f, 5.0f), me.GetPositionY() + RandomHelper.FRand(-5.0f, 5.0f), me.GetPositionZ(), 0.0f, TempSummonType.CorpseTimedDespawn, 20000);
                                if (minion)
                                    minion.GetAI().AttackStart(me.SelectNearestPlayer(20.0f));
                                break;
                            case 9:
                                me.SummonCreature(CreatureIds.Omen, Misc.omenSummonPos);
                                break;
                        }
                    }
                }
                if (me.GetEntry() == CreatureIds.ClusterElune)
                    DoCast(SpellIds.LunarFortune);

                var displacement = 0.7f;
                for (byte i = 0; i < 4; i++)
                    me.SummonGameObject(GetFireworkGameObjectId(), me.GetPositionX() + (i % 2 == 0 ? displacement : -displacement), me.GetPositionY() + (i > 1 ? displacement : -displacement), me.GetPositionZ() + 4.0f, me.GetOrientation(), Quaternion.fromEulerAnglesZYX(me.GetOrientation(), 0.0f, 0.0f), 1);
            }
            else
                //me.CastSpell(me, GetFireworkSpell(me.GetEntry()), true);
                me.CastSpell(me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), GetFireworkSpell(me.GetEntry()), true);
        }
    }

    [Script]
    internal class npc_spring_rabbit : ScriptedAI
    {
        public npc_spring_rabbit(Creature creature) : base(creature)
        {
            Initialize();
        }

        private void Initialize()
        {
            inLove = false;
            rabbitGUID.Clear();
            jumpTimer = RandomHelper.URand(5000, 10000);
            bunnyTimer = RandomHelper.URand(10000, 20000);
            searchTimer = RandomHelper.URand(5000, 10000);
        }

        private bool inLove;
        private uint jumpTimer;
        private uint bunnyTimer;
        private uint searchTimer;
        private ObjectGuid rabbitGUID;

        public override void Reset()
        {
            Initialize();
            var owner = me.GetOwner();
            if (owner)
                me.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
        }

        public override void EnterCombat(Unit who) { }

        public override void DoAction(int param)
        {
            inLove = true;
            var owner = me.GetOwner();
            if (owner)
                owner.CastSpell(owner, SpellIds.SpringFling, true);
        }

        public override void UpdateAI(uint diff)
        {
            if (inLove)
            {
                if (jumpTimer <= diff)
                {
                    var rabbit = Global.ObjAccessor.GetUnit(me, rabbitGUID);
                    if (rabbit)
                        DoCast(rabbit, SpellIds.SpringRabbitJump);
                    jumpTimer = RandomHelper.URand(5000, 10000);
                }
                else jumpTimer -= diff;

                if (bunnyTimer <= diff)
                {
                    DoCast(SpellIds.SummonBabyBunny);
                    bunnyTimer = RandomHelper.URand(20000, 40000);
                }
                else bunnyTimer -= diff;
            }
            else
            {
                if (searchTimer <= diff)
                {
                    var rabbit = me.FindNearestCreature(CreatureIds.SpringRabbit, 10.0f);
                    if (rabbit)
                    {
                        if (rabbit == me || rabbit.HasAura(SpellIds.SpringRabbitInLove))
                            return;

                        me.AddAura(SpellIds.SpringRabbitInLove, me);
                        DoAction(1);
                        rabbit.AddAura(SpellIds.SpringRabbitInLove, rabbit);
                        rabbit.GetAI().DoAction(1);
                        rabbit.CastSpell(rabbit, SpellIds.SpringRabbitJump, true);
                        rabbitGUID = rabbit.GetGUID();
                    }
                    searchTimer = RandomHelper.URand(5000, 10000);
                }
                else searchTimer -= diff;
            }
        }
    }

    [Script]
    internal class npc_imp_in_a_ball : ScriptedAI
    {
        public npc_imp_in_a_ball(Creature creature) : base(creature)
        {
            summonerGUID.Clear();
        }

        public override void IsSummonedBy(Unit summoner)
        {
            if (summoner.IsTypeId(TypeId.Player))
            {
                summonerGUID = summoner.GetGUID();

                _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
                {
                    var owner = Global.ObjAccessor.GetPlayer(me, summonerGUID);
                    if (owner)
                        Global.CreatureTextMgr.SendChat(me, 0, owner, owner.GetGroup() ? ChatMsg.MonsterParty : ChatMsg.MonsterWhisper, Language.Addon, CreatureTextRange.Normal);
                });
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        private ObjectGuid summonerGUID;
    }

    internal struct TrainWrecker
    {
        public const int ActionWrecked = 1;
        public const int EventDoJump = 1;
        public const int EventDoFacing = 2;
        public const int EventDoWreck = 3;
        public const int EventDoDance = 4;
        public const uint MoveidChase = 1;
        public const uint MoveidJump = 2;
    }

    [Script]
    internal class npc_train_wrecker : NullCreatureAI
    {
        public npc_train_wrecker(Creature creature) : base(creature)
        {
            _isSearching = true;
            _nextAction = 0;
            _timer = 1 * Time.InMilliseconds;
        }

        private GameObject VerifyTarget()
        {
            var target = ObjectAccessor.GetGameObject(me, _target);
            if (target)
                return target;

            me.HandleEmoteCommand(Emote.OneshotRude);
            me.DespawnOrUnsummon(3 * Time.InMilliseconds);
            return null;
        }

        public override void UpdateAI(uint diff)
        {
            if (_isSearching)
            {
                if (diff < _timer)
                    _timer -= diff;
                else
                {
                    var target = me.FindNearestGameObject(GameobjectIds.ToyTrain, 15.0f);
                    if (target)
                    {
                        _isSearching = false;
                        _target = target.GetGUID();
                        me.SetWalk(true);
                        me.GetMotionMaster().MovePoint(TrainWrecker.MoveidChase, target.GetNearPosition(3.0f, target.GetAngle(me)));
                    }
                    else
                        _timer = 3 * Time.InMilliseconds;
                }
            }
            else
            {
                switch (_nextAction)
                {
                    case TrainWrecker.EventDoJump:
                        {
                            var target = VerifyTarget();
                            if (target)
                                me.GetMotionMaster().MoveJump(target, 5.0f, 10.0f, TrainWrecker.MoveidJump);
                            _nextAction = 0;
                        }
                        break;
                    case TrainWrecker.EventDoFacing:
                        {
                            var target = VerifyTarget();
                            if (target)
                            {
                                me.SetFacingTo(target.GetOrientation());
                                me.HandleEmoteCommand(Emote.OneshotAttack1h);
                                _timer = (uint)(1.5 * Time.InMilliseconds);
                                _nextAction = TrainWrecker.EventDoWreck;
                            }
                            else
                                _nextAction = 0;
                        }
                        break;
                    case TrainWrecker.EventDoWreck:
                        {
                            if (diff < _timer)
                            {
                                _timer -= diff;
                                break;
                            }

                            var target = VerifyTarget();
                            if (target)
                            {
                                me.CastSpell(target, SpellIds.WreckTrain, false);
                                target.GetAI().DoAction(TrainWrecker.ActionWrecked);
                                _timer = 2 * Time.InMilliseconds;
                                _nextAction = TrainWrecker.EventDoDance;
                            }
                            else
                                _nextAction = 0;
                        }
                        break;
                    case TrainWrecker.EventDoDance:
                        if (diff < _timer)
                        {
                            _timer -= diff;
                            break;
                        }
                        me.UpdateEntry(CreatureIds.ExultingWindUpTrainWrecker);
                        me.SetEmoteState(Emote.OneshotDance);
                        me.DespawnOrUnsummon(5 * Time.InMilliseconds);
                        _nextAction = 0;
                        break;
                    default:
                        break;
                }
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (id == TrainWrecker.MoveidChase)
                _nextAction = TrainWrecker.EventDoJump;
            else if (id == TrainWrecker.MoveidJump)
                _nextAction = TrainWrecker.EventDoFacing;
        }

        private bool _isSearching;
        private byte _nextAction;
        private uint _timer;
        private ObjectGuid _target;
    }

    [Script]
    internal class npc_argent_squire_gruntling : ScriptedAI
    {
        public npc_argent_squire_gruntling(Creature creature) : base(creature)
        {
            ScheduleTasks();
        }

        public void ScheduleTasks()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                var ownerTired = me.GetOwner().GetAura(SpellIds.TiredPlayer);
                if (ownerTired != null)
                {
                    var squireTired = me.AddAura(IsArgentSquire() ? SpellIds.AuraTiredS : SpellIds.AuraTiredG, me);
                    if (squireTired != null)
                        squireTired.SetDuration(ownerTired.GetDuration());
                }
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                if ((me.HasAura(SpellIds.AuraTiredS) || me.HasAura(SpellIds.AuraTiredG)) && me.HasNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox | NPCFlags.Vendor))
                    me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox | NPCFlags.Vendor);
                task.Repeat();
            });
        }

        public override bool GossipSelect(Player player, uint menuId, uint gossipListId)
        {
            switch (gossipListId)
            {
                case GossipMenus.OptionIdBank:
                    {
                        me.AddNpcFlag(NPCFlags.Banker);
                        var _bankAura = IsArgentSquire() ? SpellIds.AuraBankS : SpellIds.AuraBankG;
                        if (!me.HasAura(_bankAura))
                            DoCastSelf(_bankAura);

                        if (!player.HasAura(SpellIds.TiredPlayer))
                            player.CastSpell(player, SpellIds.TiredPlayer, true);
                        break;
                    }
                case GossipMenus.OptionIdShop:
                    {
                        me.AddNpcFlag(NPCFlags.Vendor);
                        var _shopAura = IsArgentSquire() ? SpellIds.AuraShopS : SpellIds.AuraShopG;
                        if (!me.HasAura(_shopAura))
                            DoCastSelf(_shopAura);

                        if (!player.HasAura(SpellIds.TiredPlayer))
                            player.CastSpell(player, SpellIds.TiredPlayer, true);
                        break;
                    }
                case GossipMenus.OptionIdMail:
                    {
                        me.AddNpcFlag(NPCFlags.Mailbox);
                        player.GetSession().SendShowMailBox(me.GetGUID());

                        var _mailAura = IsArgentSquire() ? SpellIds.AuraPostmanS : SpellIds.AuraPostmanG;
                        if (!me.HasAura(_mailAura))
                            DoCastSelf(_mailAura);

                        if (!player.HasAura(SpellIds.TiredPlayer))
                            player.CastSpell(player, SpellIds.TiredPlayer, true);
                        break;
                    }
                case GossipMenus.OptionIdDarnassusSenjinPennant:
                case GossipMenus.OptionIdExodarUndercityPennant:
                case GossipMenus.OptionIdGnomereganOrgrimmarPennant:
                case GossipMenus.OptionIdIronforgeSilvermoonPennant:
                case GossipMenus.OptionIdStormwindThunderbluffPennant:
                    if (IsArgentSquire())
                        DoCastSelf(Misc.bannerSpells[gossipListId - 3].Item1, true);
                    else
                        DoCastSelf(Misc.bannerSpells[gossipListId - 3].Item2, true);
                    break;
            }
            player.PlayerTalkClass.SendCloseGossip();
            return false;
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        private bool IsArgentSquire() { return me.GetEntry() == CreatureIds.ArgentSquire; }
    }

    [Script]
    internal class npc_bountiful_tableAI : PassiveAI
    {
        public npc_bountiful_tableAI(Creature creature) : base(creature) { }

        public override void PassengerBoarded(Unit who, sbyte seatId, bool apply)
        {
            var x = 0.0f;
            var y = 0.0f;
            var z = 0.0f;
            var o = 0.0f;

            switch ((SeatIds)seatId)
            {
                case SeatIds.TurkeyChair:
                    x = 3.87f;
                    y = 2.07f;
                    o = 3.700098f;
                    break;
                case SeatIds.CranberryChair:
                    x = 3.87f;
                    y = -2.07f;
                    o = 2.460914f;
                    break;
                case SeatIds.StuffingChair:
                    x = -2.52f;
                    break;
                case SeatIds.SweetPotatoChair:
                    x = -0.09f;
                    y = -3.24f;
                    o = 1.186824f;
                    break;
                case SeatIds.PieChair:
                    x = -0.18f;
                    y = 3.24f;
                    o = 5.009095f;
                    break;
                case SeatIds.FoodHolder:
                case SeatIds.PlateHolder:
                    var holders = who.GetVehicleKit();
                    if (holders)
                        holders.InstallAllAccessories(true);
                    return;
                default:
                    break;
            }

            var init = new MoveSplineInit(who);
            init.DisableTransportPathTransformations();
            init.MoveTo(x, y, z, false);
            init.SetFacing(o);
            init.Launch();
            who.m_Events.AddEvent(new CastFoodSpell(who, SpellIds.ChairSpells[who.GetEntry()]), who.m_Events.CalculateTime(1000));
            var creature = who.ToCreature();
            if (creature)
                creature.SetDisplayFromModel(0);
        }
    }

    internal class CastFoodSpell : BasicEvent
    {
        private Unit _owner;
        private uint _spellId;

        public CastFoodSpell(Unit owner, uint spellId)
        {
            _owner = owner;
            _spellId = spellId;
        }

        public override bool Execute(ulong execTime, uint diff)
        {
            _owner.CastSpell(_owner, _spellId, true);
            return true;
        }
    }
}
