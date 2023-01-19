// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Movement;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Scripts.World.NpcSpecial
{
    enum SpawnType
    {
        Tripwire,                             // no warning, summon Creature at smaller range
        AlarmBot,                                     // cast guards mark and summon npc - if player shows up with that buff duration < 5 seconds attack
    }

    class AirForceSpawn
    {
        public AirForceSpawn(uint _myEntry, uint _otherEntry, SpawnType _spawnType)
        {
            myEntry = _myEntry;
            otherEntry = _otherEntry;
            spawnType = _spawnType;
        }

        public uint myEntry;
        public uint otherEntry;
        public SpawnType spawnType;
    }

    struct CreatureIds
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

        // TravelerTundraMammothNPCs
        public const uint HakmudOfArgus = 32638;
        public const uint Gnimo = 32639;
        public const uint DrixBlackwrench = 32641;
        public const uint Mojodishu = 32642;

        // BrewfestReveler2
        public const uint BrewfestReveler = 24484;
    }

    struct GameobjectIds
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

    struct SpellIds
    {
        public const uint GuardsMark = 38067;

        //Dancingflames
        public const uint SummonBrazier = 45423;
        public const uint BrazierDance = 45427;
        public const uint FierySeduction = 47057;

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

        //Brewfestreveler
        public const uint BrewfestToast = 41586;

        //Wormholespells
        public const uint BoreanTundra = 67834;
        public const uint SholazarBasin = 67835;
        public const uint Icecrown = 67836;
        public const uint StormPeaks = 67837;
        public const uint HowlingFjord = 67838;
        public const uint Underground = 68081;

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

        //VoidZone
        public const uint Consumption = 28874;
    }

    struct QuestConst
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

    struct TextIds
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

    struct GossipMenus
    {
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

    enum SeatIds
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

    struct Misc
    {
        public static AirForceSpawn[] AirforceSpawns =
        {
            new AirForceSpawn(2614,  15241, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Alliance)
            new AirForceSpawn(2615,  15242, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Horde)
            new AirForceSpawn(21974, 21976, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Area 52)
            new AirForceSpawn(21993, 15242, SpawnType.AlarmBot),                     //Air Force Guard Post (Horde - Bat Rider)
            new AirForceSpawn(21996, 15241, SpawnType.AlarmBot),                     //Air Force Guard Post (Alliance - Gryphon)
            new AirForceSpawn(21997, 21976, SpawnType.AlarmBot),                     //Air Force Guard Post (Goblin - Area 52 - Zeppelin)
            new AirForceSpawn(21999, 15241, SpawnType.Tripwire),             //Air Force Trip Wire - Rooftop (Alliance)
            new AirForceSpawn(22001, 15242, SpawnType.Tripwire),             //Air Force Trip Wire - Rooftop (Horde)
            new AirForceSpawn(22002, 15242, SpawnType.Tripwire),             //Air Force Trip Wire - Ground (Horde)
            new AirForceSpawn(22003, 15241, SpawnType.Tripwire),             //Air Force Trip Wire - Ground (Alliance)
            new AirForceSpawn(22063, 21976, SpawnType.Tripwire),             //Air Force Trip Wire - Rooftop (Goblin - Area 52)
            new AirForceSpawn(22065, 22064, SpawnType.AlarmBot),                     //Air Force Guard Post (Ethereal - Stormspire)
            new AirForceSpawn(22066, 22067, SpawnType.AlarmBot),                     //Air Force Guard Post (Scryer - Dragonhawk)
            new AirForceSpawn(22068, 22064, SpawnType.Tripwire),             //Air Force Trip Wire - Rooftop (Ethereal - Stormspire)
            new AirForceSpawn(22069, 22064, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Stormspire)
            new AirForceSpawn(22070, 22067, SpawnType.Tripwire),             //Air Force Trip Wire - Rooftop (Scryer)
            new AirForceSpawn(22071, 22067, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Scryer)
            new AirForceSpawn(22078, 22077, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Aldor)
            new AirForceSpawn(22079, 22077, SpawnType.AlarmBot),                     //Air Force Guard Post (Aldor - Gryphon)
            new AirForceSpawn(22080, 22077, SpawnType.Tripwire),             //Air Force Trip Wire - Rooftop (Aldor)
            new AirForceSpawn(22086, 22085, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Sporeggar)
            new AirForceSpawn(22087, 22085, SpawnType.AlarmBot),                     //Air Force Guard Post (Sporeggar - Spore Bat)
            new AirForceSpawn(22088, 22085, SpawnType.Tripwire),             //Air Force Trip Wire - Rooftop (Sporeggar)
            new AirForceSpawn(22090, 22089, SpawnType.AlarmBot),                     //Air Force Guard Post (Toshley's Station - Flying Machine)
            new AirForceSpawn(22124, 22122, SpawnType.AlarmBot),                     //Air Force Alarm Bot (Cenarion)
            new AirForceSpawn(22125, 22122, SpawnType.AlarmBot),                     //Air Force Guard Post (Cenarion - Stormcrow)
            new AirForceSpawn(22126, 22122, SpawnType.AlarmBot)                      //Air Force Trip Wire - Rooftop (Cenarion Expedition)
        };

        public const float RangeTripwire = 15.0f;
        public const float RangeAlarmbot = 100.0f;

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
        public static Position DoctorAllianceRunTo = new(-3742.96f, -4531.52f, 11.91f);

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
        public static Position DoctorHordeRunTo = new(-1016.44f, -3508.48f, 62.96f);

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

        public static Position omenSummonPos = new(7558.993f, -2839.999f, 450.0214f, 4.46f);

        public const uint AuraDurationTimeLeft = 30000;

        //Argent squire/gruntling
        public const uint AchievementPonyUp = 3736;
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
    class npc_air_force_bots : NullCreatureAI
    {
        AirForceSpawn _spawn;
        ObjectGuid _myGuard;
        List<ObjectGuid> _toAttack = new();

        static AirForceSpawn FindSpawnFor(uint entry)
        {
            foreach (AirForceSpawn spawn in Misc.AirforceSpawns)
            {
                if (spawn.myEntry == entry)
                {
                    Cypher.Assert(Global.ObjectMgr.GetCreatureTemplate(spawn.otherEntry) != null, $"Invalid creature entry {spawn.otherEntry} in 'npc_air_force_bots' script");
                    return spawn;
                }
            }
            Cypher.Assert(false, $"Unhandled creature with entry {entry} is assigned 'npc_air_force_bots' script");

            return null;
        }

        public npc_air_force_bots(Creature creature) : base(creature)
        {
            _spawn = FindSpawnFor(creature.GetEntry());
        }

        Creature GetOrSummonGuard()
        {
            Creature guard = ObjectAccessor.GetCreature(me, _myGuard);

            if (guard == null && (guard = me.SummonCreature(_spawn.otherEntry, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromMinutes(5))))
                _myGuard = guard.GetGUID();

            return guard;
        }

        public override void UpdateAI(uint diff)
        {
            if (_toAttack.Empty())
                return;

            Creature guard = GetOrSummonGuard();
            if (guard == null)
                return;

            // Keep the list of targets for later on when the guards will be alive
            if (!guard.IsAlive())
                return;

            for (var i = 0; i < _toAttack.Count; ++i)
            {
                ObjectGuid guid = _toAttack[i];

                Unit target = Global.ObjAccessor.GetUnit(me, guid);
                if (!target)
                    continue;

                if (guard.IsEngagedBy(target))
                    continue;

                guard.EngageWithTarget(target);
                if (_spawn.spawnType == SpawnType.AlarmBot)
                    guard.CastSpell(target, SpellIds.GuardsMark, true);
            }

            _toAttack.Clear();
        }

        public override void MoveInLineOfSight(Unit who)
        {
            // guards are only spawned against players
            if (!who.IsPlayer())
                return;

            // we're already scheduled to attack this player on our next tick, don't bother checking
            if (_toAttack.Contains(who.GetGUID()))
                return;

            // check if they're in range
            if (!who.IsWithinDistInMap(me, (_spawn.spawnType == SpawnType.AlarmBot) ? Misc.RangeAlarmbot : Misc.RangeTripwire))
                return;

            // check if they're hostile
            if (!(me.IsHostileTo(who) || who.IsHostileTo(me)))
                return;

            // check if they're a valid attack target
            if (!me.IsValidAttackTarget(who))
                return;

            if ((_spawn.spawnType == SpawnType.Tripwire) && who.IsFlying())
                return;

            _toAttack.Add(who.GetGUID());
        }
    }

    [Script]
    class npc_chicken_cluck : ScriptedAI
    {
        public npc_chicken_cluck(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            ResetFlagTimer = 120000;
        }

        uint ResetFlagTimer;

        public override void Reset()
        {
            Initialize();
            me.SetFaction(Misc.FactionChicken);
            me.RemoveNpcFlag(NPCFlags.QuestGiver);
        }

        public override void JustEngagedWith(Unit who) { }

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
                        me.SetNpcFlag(NPCFlags.QuestGiver);
                        me.SetFaction(Misc.FactionFriendly);
                        Talk(player.GetTeam() == Team.Horde ? TextIds.EmoteHelloH : TextIds.EmoteHelloA);
                    }
                    break;
                case TextEmotes.Cheer:
                    if (player.GetQuestStatus(QuestConst.Cluck) == QuestStatus.Complete)
                    {
                        me.SetNpcFlag(NPCFlags.QuestGiver);
                        me.SetFaction(Misc.FactionFriendly);
                        Talk(TextIds.EmoteCluck);
                    }
                    break;
            }
        }

        public override void OnQuestAccept(Player player, Quest quest)
        {
            if (quest.Id == QuestConst.Cluck)
                Reset();
        }

        public override void OnQuestReward(Player player, Quest quest, LootItemType type, uint opt)
        {
            if (quest.Id == QuestConst.Cluck)
                Reset();
        }
    }

    [Script]
    class npc_dancing_flames : ScriptedAI
    {
        public npc_dancing_flames(Creature creature) : base(creature) { }

        public override void Reset()
        {
            DoCastSelf(SpellIds.SummonBrazier, new CastSpellExtraArgs(true));
            DoCastSelf(SpellIds.BrazierDance, new CastSpellExtraArgs(false));
            me.SetEmoteState(Emote.StateDance);
            float x, y, z;
            me.GetPosition(out x, out y, out z);
            me.Relocate(x, y, z + 1.05f);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        public override void ReceiveEmote(Player player, TextEmotes emote)
        {
            if (me.IsWithinLOS(player.GetPositionX(), player.GetPositionY(), player.GetPositionZ()) && me.IsWithinDistInMap(player, 30.0f))
            {
                // She responds to emotes not instantly but ~1500ms later
                // If you first /bow, then /wave before dancing flames bow back, it doesnt bow at all and only does wave
                // If you're performing emotes too fast, she will not respond to them
                // Means she just replaces currently scheduled event with new after receiving new emote
                _scheduler.CancelAll();

                switch (emote)
                {
                    case TextEmotes.Kiss:
                        _scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => me.HandleEmoteCommand(Emote.OneshotShy));
                        break;
                    case TextEmotes.Wave:
                        _scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => me.HandleEmoteCommand(Emote.OneshotWave));
                        break;
                    case TextEmotes.Bow:
                        _scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => me.HandleEmoteCommand(Emote.OneshotBow));
                        break;
                    case TextEmotes.Joke:
                        _scheduler.Schedule(TimeSpan.FromMilliseconds(1500), context => me.HandleEmoteCommand(Emote.OneshotLaugh));
                        break;
                    case TextEmotes.Dance:
                        if (!player.HasAura(SpellIds.FierySeduction))
                        {
                            DoCast(player, SpellIds.FierySeduction, new CastSpellExtraArgs(true));
                            me.SetFacingTo(me.GetAbsoluteAngle(player));
                        }
                        break;
                }
            }
        }
    }

    [Script]
    class npc_torch_tossing_target_bunny_controller : ScriptedAI
    {
        public npc_torch_tossing_target_bunny_controller(Creature creature) : base(creature)
        {
            _targetTimer = 3000;
        }

        ObjectGuid DoSearchForTargets(ObjectGuid lastTargetGUID)
        {
            List<Creature> targets = me.GetCreatureListWithEntryInGrid(CreatureIds.TorchTossingTargetBunny, 60.0f);
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
                Unit target = Global.ObjAccessor.GetUnit(me, DoSearchForTargets(_lastTargetGUID));
                if (target)
                    target.CastSpell(target, SpellIds.TargetIndicator, true);

                _targetTimer = 3000;
            }
            else
                _targetTimer -= diff;
        }

        uint _targetTimer;
        ObjectGuid _lastTargetGUID;
    }

    [Script]
    class npc_midsummer_bunny_pole : ScriptedAI
    {
        public npc_midsummer_bunny_pole(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
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

                GameObject go = me.FindNearestGameObject(GameobjectIds.RibbonPole, 10.0f);
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

        bool checkNearbyPlayers()
        {
            // Returns true if no nearby player has aura "Test Ribbon Pole Channel".
            List<Unit> players = new();
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

        bool running;
    }

    [Script]
    class npc_doctor : ScriptedAI
    {
        public npc_doctor(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
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
            me.RemoveUnitFlag(UnitFlags.Uninteractible);
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
            me.SetUnitFlag(UnitFlags.Uninteractible);
        }

        public void PatientDied(Position point)
        {
            Player player = Global.ObjAccessor.GetPlayer(me, PlayerGUID);
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
                                Creature patient = ObjectAccessor.GetCreature(me, guid);
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

                    Creature Patient = me.SummonCreature(patientEntry, Coordinates[index], TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(5));
                    if (Patient)
                    {
                        //303, this flag appear to be required for client side item.spell to work (TARGET_SINGLE_FRIEND)
                        Patient.SetUnitFlag(UnitFlags.PlayerControlled);

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

        public override void JustEngagedWith(Unit who) { }

        public override void OnQuestAccept(Player player, Quest quest)
        {
            if ((quest.Id == 6624) || (quest.Id == 6622))
                BeginEvent(player);
        }

        ObjectGuid PlayerGUID;

        uint SummonPatientTimer;
        uint SummonPatientCount;
        uint PatientDiedCount;
        uint PatientSavedCount;

        bool Event;

        List<ObjectGuid> Patients = new();
        List<Position> Coordinates = new();
    }

    [Script]
    public class npc_injured_patient : ScriptedAI
    {
        public npc_injured_patient(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
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
            me.RemoveUnitFlag(UnitFlags.Uninteractible);

            //no regen health
            me.SetUnitFlag(UnitFlags.InCombat);

            //to make them lay with face down
            me.SetStandState(UnitStandStateType.Dead);

            uint mobId = me.GetEntry();

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

        public override void JustEngagedWith(Unit who) { }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            Player player = caster.ToPlayer();
            if (!player || !me.IsAlive() || spellInfo.Id != 20804)
                return;

            if (player.GetQuestStatus(6624) == QuestStatus.Incomplete || player.GetQuestStatus(6622) == QuestStatus.Incomplete)
            {
                if (!DoctorGUID.IsEmpty())
                {
                    Creature doctor = ObjectAccessor.GetCreature(me, DoctorGUID);
                    if (doctor)
                        ((npc_doctor)doctor.GetAI()).PatientSaved(me, player, Coord);
                }
            }

            //make not selectable
            me.SetUnitFlag(UnitFlags.Uninteractible);

            //regen health
            me.RemoveUnitFlag(UnitFlags.InCombat);

            //stand up
            me.SetStandState(UnitStandStateType.Stand);

            Talk(TextIds.SayDoc);

            uint mobId = me.GetEntry();
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
                me.SetUnitFlag(UnitFlags.Uninteractible);
                me.SetDeathState(DeathState.JustDied);
                me.SetUnitFlag3(UnitFlags3.FakeDead);

                if (!DoctorGUID.IsEmpty())
                {
                    Creature doctor = ObjectAccessor.GetCreature((me), DoctorGUID);
                    if (doctor)
                        ((npc_doctor)doctor.GetAI()).PatientDied(Coord);
                }
            }
        }
    }

    [Script]
    class npc_garments_of_quests : EscortAI
    {
        ObjectGuid CasterGUID;

        bool IsHealed;
        bool CanRun;

        uint RunAwayTimer;
        uint quest;

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

            Initialize();
        }

        void Initialize()
        {
            IsHealed = false;
            CanRun = false;

            RunAwayTimer = 5000;
        }

        public override void Reset()
        {
            CasterGUID.Clear();

            Initialize();

            me.SetStandState(UnitStandStateType.Kneel);
            // expect database to have RegenHealth=0
            me.SetHealth(me.CountPctFromMaxHealth(70));
        }

        public override void JustEngagedWith(Unit who) { }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            if (spellInfo.Id == SpellIds.LesserHealR2 || spellInfo.Id == SpellIds.FortitudeR1)
            {
                //not while in combat
                if (me.IsInCombat())
                    return;

                //nothing to be done now
                if (IsHealed && CanRun)
                    return;

                Player player = caster.ToPlayer();
                if (player)
                {
                    if (quest != 0 && player.GetQuestStatus(quest) == QuestStatus.Incomplete)
                    {
                        if (IsHealed && !CanRun && spellInfo.Id == SpellIds.FortitudeR1)
                        {
                            Talk(TextIds.SayThanks, player);
                            CanRun = true;
                        }
                        else if (!IsHealed && spellInfo.Id == SpellIds.LesserHealR2)
                        {
                            CasterGUID = player.GetGUID();
                            me.SetStandState(UnitStandStateType.Stand);
                            Talk(TextIds.SayHealed, player);
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
                    Unit unit = Global.ObjAccessor.GetUnit(me, CasterGUID);
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
    class npc_guardian : ScriptedAI
    {
        public npc_guardian(Creature creature) : base(creature) { }

        public override void Reset()
        {
            me.SetUnitFlag(UnitFlags.NonAttackable);
        }

        public override void JustEngagedWith(Unit who) { }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (me.IsAttackReady())
            {
                DoCastVictim(SpellIds.Deathtouch, new CastSpellExtraArgs(true));
                me.ResetAttackTimer();
            }
        }
    }

    [Script]
    class npc_steam_tonk : ScriptedAI
    {
        public npc_steam_tonk(Creature creature) : base(creature) { }

        public override void Reset() { }
        public override void JustEngagedWith(Unit who) { }

        public void OnPossess(bool apply)
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
    class npc_brewfest_reveler : ScriptedAI
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
    class npc_brewfest_reveler_2 : ScriptedAI
    {
        Emote[] BrewfestRandomEmote =
        {
            Emote.OneshotQuestion,
            Emote.OneshotApplaud,
            Emote.OneshotShout,
            Emote.OneshotEatNoSheathe,
            Emote.OneshotLaughNoSheathe
        };

        List<ObjectGuid> _revelerGuids = new();

        public npc_brewfest_reveler_2(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), fillListTask =>
            {
                List<Creature> creatureList = me.GetCreatureListWithEntryInGrid(CreatureIds.BrewfestReveler, 5.0f);
                foreach (Creature creature in creatureList)
                    if (creature != me)
                        _revelerGuids.Add(creature.GetGUID());

                fillListTask.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), faceToTask =>
                {
                    // Turn to random brewfest reveler within set range
                    if (!_revelerGuids.Empty())
                    {
                        Creature creature = ObjectAccessor.GetCreature(me, _revelerGuids.SelectRandom());
                        if (creature != null)
                            me.SetFacingToObject(creature);
                    }

                    _scheduler.Schedule(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(6), emoteTask =>
                    {
                        var nextTask = (TaskContext task) =>
                        {
                            // If dancing stop before next random state
                            if (me.GetEmoteState() == Emote.StateDance)
                                me.SetEmoteState(Emote.OneshotNone);

                            // Random EVENT_EMOTE or EVENT_FACETO
                            if (RandomHelper.randChance(50))
                                faceToTask.Repeat(TimeSpan.FromSeconds(1));
                            else
                                emoteTask.Repeat(TimeSpan.FromSeconds(1));
                        };

                        // Play random emote or dance
                        if (RandomHelper.randChance(50))
                        {
                            me.HandleEmoteCommand(BrewfestRandomEmote.SelectRandom());
                            _scheduler.Schedule(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6), nextTask);
                        }
                        else
                        {
                            me.SetEmoteState(Emote.StateDance);
                            _scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12), nextTask);
                        }
                    });
                });
            });
        }

        // Copied from old script. I don't know if this is 100% correct.
        public override void ReceiveEmote(Player player, TextEmotes emote)
        {
            if (!Global.GameEventMgr.IsHolidayActive(HolidayIds.Brewfest))
                return;

            if (emote == TextEmotes.Dance)
                me.CastSpell(player, SpellIds.BrewfestToast, false);
        }

        public override void UpdateAI(uint diff)
        {
            UpdateVictim();

            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_training_dummy : NullCreatureAI
    {
        Dictionary<ObjectGuid, TimeSpan> _combatTimer = new();

        public npc_training_dummy(Creature creature) : base(creature) { }

        public override void JustEnteredCombat(Unit who)
        {
            _combatTimer[who.GetGUID()] = TimeSpan.FromSeconds(5);
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            damage = 0;

            if (!attacker || damageType == DamageEffectType.DOT)
                return;

            _combatTimer[attacker.GetGUID()] = TimeSpan.FromSeconds(5);
        }

        public override void UpdateAI(uint diff)
        {
            foreach (var key in _combatTimer.Keys.ToList())
            {
                _combatTimer[key] -= TimeSpan.FromMilliseconds(diff);
                if (_combatTimer[key] <= TimeSpan.Zero)
                {
                    // The attacker has not dealt any damage to the dummy for over 5 seconds. End combat.
                    var pveRefs = me.GetCombatManager().GetPvECombatRefs();
                    var it = pveRefs.LookupByKey(key);
                    if (it != null)
                        it.EndCombat();

                    _combatTimer.Remove(key);
                }
            }
        }
    }

    [Script]
    class npc_wormhole : PassiveAI
    {
        public npc_wormhole(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _showUnderground = RandomHelper.URand(0, 100) == 0; // Guessed value, it is really rare though
        }

        public override void InitializeAI()
        {
            Initialize();
        }

        public override bool OnGossipHello(Player player)
        {
            player.InitGossipMenu(GossipMenus.MenuIdWormhole);
            if (me.IsSummon())
            {
                if (player == me.ToTempSummon().GetSummoner())
                {
                    player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                    player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole2, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole3, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 3);
                    player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole4, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 4);
                    player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 5);

                    if (_showUnderground)
                        player.AddGossipItem(GossipMenus.MenuIdWormhole, GossipMenus.OptionIdWormhole6, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 6);

                    player.SendGossipMenu(TextIds.Wormhole, me.GetGUID());
                }
            }

            return true;
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.PlayerTalkClass.ClearMenus();

            switch (action)
            {
                case eTradeskill.GossipActionInfoDef + 1: // Borean Tundra
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.BoreanTundra, new CastSpellExtraArgs(false));
                    break;
                case eTradeskill.GossipActionInfoDef + 2: // Howling Fjord
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.HowlingFjord, new CastSpellExtraArgs(false));
                    break;
                case eTradeskill.GossipActionInfoDef + 3: // Sholazar Basin
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.SholazarBasin, new CastSpellExtraArgs(false));
                    break;
                case eTradeskill.GossipActionInfoDef + 4: // Icecrown
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.Icecrown, new CastSpellExtraArgs(false));
                    break;
                case eTradeskill.GossipActionInfoDef + 5: // Storm peaks
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.StormPeaks, new CastSpellExtraArgs(false));
                    break;
                case eTradeskill.GossipActionInfoDef + 6: // Underground
                    player.CloseGossipMenu();
                    DoCast(player, SpellIds.Underground, new CastSpellExtraArgs(false));
                    break;
            }

            return true;
        }

        bool _showUnderground;
    }

    [Script]
    class npc_spring_rabbit : ScriptedAI
    {
        public npc_spring_rabbit(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            inLove = false;
            rabbitGUID.Clear();
            jumpTimer = RandomHelper.URand(5000, 10000);
            bunnyTimer = RandomHelper.URand(10000, 20000);
            searchTimer = RandomHelper.URand(5000, 10000);
        }

        bool inLove;
        uint jumpTimer;
        uint bunnyTimer;
        uint searchTimer;
        ObjectGuid rabbitGUID;

        public override void Reset()
        {
            Initialize();
            Unit owner = me.GetOwner();
            if (owner)
                me.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
        }

        public override void JustEngagedWith(Unit who) { }

        public override void DoAction(int param)
        {
            inLove = true;
            Unit owner = me.GetOwner();
            if (owner)
                owner.CastSpell(owner, SpellIds.SpringFling, true);
        }

        public override void UpdateAI(uint diff)
        {
            if (inLove)
            {
                if (jumpTimer <= diff)
                {
                    Unit rabbit = Global.ObjAccessor.GetUnit(me, rabbitGUID);
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
                    Creature rabbit = me.FindNearestCreature(CreatureIds.SpringRabbit, 10.0f);
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
    class npc_imp_in_a_ball : ScriptedAI
    {
        public npc_imp_in_a_ball(Creature creature) : base(creature)
        {
            summonerGUID.Clear();
        }

        public override void IsSummonedBy(WorldObject summoner)
        {
            if (summoner.IsTypeId(TypeId.Player))
            {
                summonerGUID = summoner.GetGUID();

                _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
                {
                    Player owner = Global.ObjAccessor.GetPlayer(me, summonerGUID);
                    if (owner)
                        Global.CreatureTextMgr.SendChat(me, 0, owner, owner.GetGroup() ? ChatMsg.MonsterParty : ChatMsg.MonsterWhisper, Language.Addon, CreatureTextRange.Normal);
                });
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        ObjectGuid summonerGUID;
    }

    struct TrainWrecker
    {
        public const int EventDoJump = 1;
        public const int EventDoFacing = 2;
        public const int EventDoWreck = 3;
        public const int EventDoDance = 4;
        public const uint MoveidChase = 1;
        public const uint MoveidJump = 2;
    }

    [Script]
    class npc_train_wrecker : NullCreatureAI
    {
        public npc_train_wrecker(Creature creature) : base(creature)
        {
            _isSearching = true;
            _nextAction = 0;
            _timer = 1 * Time.InMilliseconds;
        }

        GameObject VerifyTarget()
        {
            GameObject target = ObjectAccessor.GetGameObject(me, _target);
            if (target)
                return target;

            me.HandleEmoteCommand(Emote.OneshotRude);
            me.DespawnOrUnsummon(TimeSpan.FromSeconds(3));
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
                    GameObject target = me.FindNearestGameObject(GameobjectIds.ToyTrain, 15.0f);
                    if (target)
                    {
                        _isSearching = false;
                        _target = target.GetGUID();
                        me.SetWalk(true);
                        me.GetMotionMaster().MovePoint(TrainWrecker.MoveidChase, target.GetNearPosition(3.0f, target.GetAbsoluteAngle(me)));
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
                        GameObject target = VerifyTarget();
                        if (target)
                            me.GetMotionMaster().MoveJump(target, 5.0f, 10.0f, TrainWrecker.MoveidJump);
                        _nextAction = 0;
                    }
                    break;
                    case TrainWrecker.EventDoFacing:
                    {
                        GameObject target = VerifyTarget();
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

                        GameObject target = VerifyTarget();
                        if (target)
                        {
                            me.CastSpell(target, SpellIds.WreckTrain, false);
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
                        me.DespawnOrUnsummon(TimeSpan.FromSeconds(5));
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

        bool _isSearching;
        byte _nextAction;
        uint _timer;
        ObjectGuid _target;
    }

    [Script]
    class npc_argent_squire_gruntling : ScriptedAI
    {
        public npc_argent_squire_gruntling(Creature creature) : base(creature) { }

        public override void Reset()
        {
            Player owner = me.GetOwner()?.ToPlayer();
            if (owner != null)
            {
                Aura ownerTired = owner.GetAura(SpellIds.TiredPlayer);
                if (ownerTired != null)
                {
                    Aura squireTired = me.AddAura(IsArgentSquire() ? SpellIds.AuraTiredS : SpellIds.AuraTiredG, me);
                    if (squireTired != null)
                        squireTired.SetDuration(ownerTired.GetDuration());
                }

                if (owner.HasAchieved(Misc.AchievementPonyUp) && !me.HasAura(SpellIds.AuraTiredS) && !me.HasAura(SpellIds.AuraTiredG))
                {
                    me.SetNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox | NPCFlags.Vendor);
                    return;
                }
            }

            me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox | NPCFlags.Vendor);
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            switch (gossipListId)
            {
                case GossipMenus.OptionIdBank:
                {
                    me.RemoveNpcFlag(NPCFlags.Mailbox | NPCFlags.Vendor);
                    uint _bankAura = IsArgentSquire() ? SpellIds.AuraBankS : SpellIds.AuraBankG;
                    if (!me.HasAura(_bankAura))
                        DoCastSelf(_bankAura);

                    if (!player.HasAura(SpellIds.TiredPlayer))
                        player.CastSpell(player, SpellIds.TiredPlayer, true);
                    break;
                }
                case GossipMenus.OptionIdShop:
                {
                    me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox);
                    uint _shopAura = IsArgentSquire() ? SpellIds.AuraShopS : SpellIds.AuraShopG;
                    if (!me.HasAura(_shopAura))
                        DoCastSelf(_shopAura);

                    if (!player.HasAura(SpellIds.TiredPlayer))
                        player.CastSpell(player, SpellIds.TiredPlayer, true);
                    break;
                }
                case GossipMenus.OptionIdMail:
                {
                    me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Vendor);

                    uint _mailAura = IsArgentSquire() ? SpellIds.AuraPostmanS : SpellIds.AuraPostmanG;
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
                        DoCastSelf(Misc.bannerSpells[gossipListId - 3].Item1, new CastSpellExtraArgs(true));
                    else
                        DoCastSelf(Misc.bannerSpells[gossipListId - 3].Item2, new CastSpellExtraArgs(true));

                    player.PlayerTalkClass.SendCloseGossip();
                    break;
                default:
                    break;
            }

            return false;
        }

        bool IsArgentSquire() { return me.GetEntry() == CreatureIds.ArgentSquire; }
    }

    [Script]
    class npc_bountiful_table : PassiveAI
    {
        Dictionary<uint, uint> ChairSpells = new()
        {
            { CreatureIds.TheCranberryChair, SpellIds.CranberryServer },
            { CreatureIds.ThePieChair, SpellIds.PieServer },
            { CreatureIds.TheStuffingChair, SpellIds.StuffingServer },
            { CreatureIds.TheTurkeyChair, SpellIds.TurkeyServer },
            { CreatureIds.TheSweetPotatoChair, SpellIds.SweetPotatoesServer },
        };

        public npc_bountiful_table(Creature creature) : base(creature) { }

        public override void PassengerBoarded(Unit who, sbyte seatId, bool apply)
        {
            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;
            float o = 0.0f;

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
                    Vehicle holders = who.GetVehicleKit();
                    if (holders)
                        holders.InstallAllAccessories(true);
                    return;
                default:
                    break;
            }

            var initializer = (MoveSplineInit init) =>
            {
                init.DisableTransportPathTransformations();
                init.MoveTo(x, y, z, false);
                init.SetFacing(o);
            };

            who.GetMotionMaster().LaunchMoveSpline(initializer, EventId.VehicleBoard, MovementGeneratorPriority.Highest);
            who.m_Events.AddEvent(new CastFoodSpell(who, ChairSpells[who.GetEntry()]), who.m_Events.CalculateTime(TimeSpan.FromSeconds(1)));
            Creature creature = who.ToCreature();
            if (creature)
                creature.SetDisplayFromModel(0);
        }
    }

    [Script]
    class npc_gen_void_zone : ScriptedAI
    {
        public npc_gen_void_zone(Creature creature) : base(creature) { }

        public override void InitializeAI()
        {
            me.SetReactState(ReactStates.Passive);
        }

        public override void JustAppeared()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task =>
            {
                DoCastSelf(SpellIds.Consumption);
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    class CastFoodSpell : BasicEvent
    {
        Unit _owner;
        uint _spellId;

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
