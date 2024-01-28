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
using static Global;

namespace Scripts.World.NpcsSpecial
{
    enum AirForceBots
    {
        Tripwire, // do not attack flying players, smaller range
        Alarmbot, // attack flying players, casts guard's mark
    }

    class AirForceSpawn
    {
        public uint MyEntry;
        public uint OtherEntry;
        public AirForceBots Type;

        public AirForceSpawn(uint myEntry, uint otherEntry, AirForceBots type)
        {
            MyEntry = myEntry;
            OtherEntry = otherEntry;
            Type = type;
        }
    }

    [Script]
    class npc_air_force_bots : NullCreatureAI
    {
        AirForceSpawn _spawn;
        ObjectGuid _myGuard;
        List<ObjectGuid> _toAttack = new();

        AirForceSpawn[] airforceSpawns =
        {
            new AirForceSpawn(2614, 15241, AirForceBots.Alarmbot), // Air Force Alarm Bot (Alliance)
            new AirForceSpawn(2615, 15242, AirForceBots.Alarmbot), // Air Force Alarm Bot (Horde)
            new AirForceSpawn(21974, 21976, AirForceBots.Alarmbot), // Air Force Alarm Bot (Area 52)
            new AirForceSpawn(21993, 15242, AirForceBots.Alarmbot), // Air Force Guard Post (Horde - Bat Rider)
            new AirForceSpawn(21996, 15241, AirForceBots.Alarmbot), // Air Force Guard Post (Alliance - Gryphon)
            new AirForceSpawn(21997, 21976, AirForceBots.Alarmbot), // Air Force Guard Post (Goblin - Area 52 - Zeppelin)
            new AirForceSpawn(21999, 15241, AirForceBots.Tripwire), // Air Force Trip Wire - Rooftop (Alliance)
            new AirForceSpawn(22001, 15242, AirForceBots.Tripwire), // Air Force Trip Wire - Rooftop (Horde)
            new AirForceSpawn(22002, 15242, AirForceBots.Tripwire), // Air Force Trip Wire - Ground (Horde)
            new AirForceSpawn(22003, 15241, AirForceBots.Tripwire), // Air Force Trip Wire - Ground (Alliance)
            new AirForceSpawn(22063, 21976, AirForceBots.Tripwire), // Air Force Trip Wire - Rooftop (Goblin - Area 52)
            new AirForceSpawn(22065, 22064, AirForceBots.Alarmbot), // Air Force Guard Post (Ethereal - Stormspire)
            new AirForceSpawn(22066, 22067, AirForceBots.Alarmbot), // Air Force Guard Post (Scryer - Dragonhawk)
            new AirForceSpawn(22068, 22064, AirForceBots.Tripwire), // Air Force Trip Wire - Rooftop (Ethereal - Stormspire)
            new AirForceSpawn(22069, 22064, AirForceBots.Alarmbot), // Air Force Alarm Bot (Stormspire)
            new AirForceSpawn(22070, 22067, AirForceBots.Tripwire), // Air Force Trip Wire - Rooftop (Scryer)
            new AirForceSpawn(22071, 22067, AirForceBots.Alarmbot), // Air Force Alarm Bot (Scryer)
            new AirForceSpawn(22078, 22077, AirForceBots.Alarmbot), // Air Force Alarm Bot (Aldor)
            new AirForceSpawn(22079, 22077, AirForceBots.Alarmbot), // Air Force Guard Post (Aldor - Gryphon)
            new AirForceSpawn(22080, 22077, AirForceBots.Tripwire), // Air Force Trip Wire - Rooftop (Aldor)
            new AirForceSpawn(22086, 22085, AirForceBots.Alarmbot), // Air Force Alarm Bot (Sporeggar)
            new AirForceSpawn(22087, 22085, AirForceBots.Alarmbot), // Air Force Guard Post (Sporeggar - Spore Bat)
            new AirForceSpawn(22088, 22085, AirForceBots.Tripwire), // Air Force Trip Wire - Rooftop (Sporeggar)
            new AirForceSpawn(22090, 22089, AirForceBots.Alarmbot), // Air Force Guard Post (Toshley's Station - Flying Machine)
            new AirForceSpawn(22124, 22122, AirForceBots.Alarmbot), // Air Force Alarm Bot (Cenarion)
            new AirForceSpawn(22125, 22122, AirForceBots.Alarmbot), // Air Force Guard Post (Cenarion - Stormcrow)
            new AirForceSpawn(22126, 22122, AirForceBots.Alarmbot)  // Air Force Trip Wire - Rooftop (Cenarion Expedition)
        };

        float RangeTripwire = 15.0f;
        float RangeAlarmbot = 100.0f;
        const uint SpellGuardsMark = 38067;

        public npc_air_force_bots(Creature creature) : base(creature)
        {
            _spawn = FindSpawnFor(creature.GetEntry());
        }

        Creature GetOrSummonGuard()
        {
            Creature guard = ObjectAccessor.GetCreature(me, _myGuard);

            if (guard == null && (guard = me.SummonCreature(_spawn.OtherEntry, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromMinutes(5))) != null)
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

            foreach (ObjectGuid guid in _toAttack)
            {
                Unit target = ObjAccessor.GetUnit(me, guid);
                if (target == null)
                    continue;

                if (guard.IsEngagedBy(target))
                    continue;

                guard.EngageWithTarget(target);
                if (_spawn.Type == AirForceBots.Alarmbot)
                    guard.CastSpell(target, SpellGuardsMark, true);
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
            if (!who.IsWithinDistInMap(me, (_spawn.Type == AirForceBots.Alarmbot) ? RangeAlarmbot : RangeTripwire))
                return;

            // check if they're hostile
            if (!(me.IsHostileTo(who) || who.IsHostileTo(me)))
                return;

            // check if they're a valid attack target
            if (!me.IsValidAttackTarget(who))
                return;

            if ((_spawn.Type == AirForceBots.Tripwire) && who.IsFlying())
                return;

            _toAttack.Add(who.GetGUID());
        }

        AirForceSpawn FindSpawnFor(uint entry)
        {
            foreach (AirForceSpawn spawn in airforceSpawns)
            {
                if (spawn.MyEntry == entry)
                {
                    Cypher.Assert(ObjectMgr.GetCreatureTemplate(spawn.OtherEntry) != null, $"Invalid creature entry {spawn.OtherEntry} in 'npc_air_force_bots' script");
                    return spawn;
                }
            }
            Cypher.Assert(false, $"Unhandled creature with entry {entry} is assigned 'npc_air_force_bots' script");
            return null;
        }
    }

    [Script]
    class npc_chicken_cluck : ScriptedAI
    {
        const uint QuestCluck = 3861;

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
            me.SetFaction(FactionTemplates.Prey);
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
                    if (player.GetQuestStatus(QuestCluck) == QuestStatus.None && RandomHelper.Rand32() % 30 == 1)
                    {
                        me.SetNpcFlag(NPCFlags.QuestGiver);
                        me.SetFaction(FactionTemplates.Friendly);
                        Talk(player.GetTeam() == Team.Horde ? 1 : 0u);
                    }
                    break;
                case TextEmotes.Cheer:
                    if (player.GetQuestStatus(QuestCluck) == QuestStatus.Complete)
                    {
                        me.SetNpcFlag(NPCFlags.QuestGiver);
                        me.SetFaction(FactionTemplates.Friendly);
                        Talk(2);
                    }
                    break;
            }
        }

        public override void OnQuestAccept(Player player, Quest quest)
        {
            if (quest.Id == QuestCluck)
                Reset();
        }

        public override void OnQuestReward(Player player, Quest quest, LootItemType type, uint opt)
        {
            if (quest.Id == QuestCluck)
                Reset();
        }
    }

    [Script]
    class npc_dancing_flames : ScriptedAI
    {
        const uint SpellSummonBrazier = 45423;
        const uint SpellBrazierDance = 45427;
        const uint SpellFierySeduction = 47057;

        public npc_dancing_flames(Creature creature) : base(creature) { }

        public override void Reset()
        {
            DoCastSelf(SpellSummonBrazier, true);
            DoCastSelf(SpellBrazierDance, false);
            me.SetEmoteState(Emote.StateDance);
            me.GetPosition(out float x, out float y, out float z);
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
                // She responds to emotes not instantly but ~TimeSpan.FromMilliseconds(1500) later
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
                        if (!player.HasAura(SpellFierySeduction))
                        {
                            DoCast(player, SpellFierySeduction, true);
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
        const uint SpellTorchTargetPicker = 45907;

        public npc_torch_tossing_target_bunny_controller(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(2), context =>
            {
                me.CastSpell(null, SpellTorchTargetPicker);
                _scheduler.Schedule(TimeSpan.FromSeconds(3), context => me.CastSpell(null, SpellTorchTargetPicker));
                context.Repeat(TimeSpan.FromSeconds(5));
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_midsummer_bunny_pole : ScriptedAI
    {
        const uint GoRibbonPole = 181605;
        const uint SpellRibbonDanceCosmetic = 29726;
        const uint SpellRedFireRing = 46836;
        const uint SpellBlueFireRing = 46842;

        bool running;

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
        }

        public override void DoAction(int action)
        {
            // Don't start event if it's already running.
            if (running)
                return;

            running = true;
            _scheduler.Schedule(TimeSpan.FromMilliseconds(1), context =>
            {
                if (checkNearbyPlayers())
                {
                    Reset();
                    return;
                }

                GameObject go = me.FindNearestGameObject(GoRibbonPole, 10.0f);
                if (go != null)
                    me.CastSpell(go, SpellRedFireRing, true);

                context.Schedule(TimeSpan.FromSeconds(5), _ =>
                {
                    if (checkNearbyPlayers())
                    {
                        Reset();
                        return;
                    }

                    GameObject go = me.FindNearestGameObject(GoRibbonPole, 10.0f);
                    if (go != null)
                        me.CastSpell(go, SpellBlueFireRing, true);

                    context.Repeat(TimeSpan.FromSeconds(5));
                });
            });
        }

        bool checkNearbyPlayers()
        {
            // Returns true if no nearby player has aura "Test Ribbon Pole Channel".
            List<Unit> players = new();
            UnitAuraCheck check = new(true, SpellRibbonDanceCosmetic);
            PlayerListSearcher searcher = new(me, players, check);
            Cell.VisitWorldObjects(me, searcher, 10.0f);

            return players.Empty();
        }

        public override void UpdateAI(uint diff)
        {
            if (!running)
                return;

            _scheduler.Update(diff);
        }
    }

    struct DoctorConst
    {
        public const uint SayDoc = 0;

        public const uint DoctorAlliance = 12939;
        public const uint DoctorHorde = 12920;

        public static Position[] AllianceCoords =
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
        public static Position ARunTo = new Position(-3742.96f, -4531.52f, 11.91f);

        public static Position[] HordeCoords =
        {
            new Position(-1013.75f, -3492.59f, 62.62f, 4.34f),                      // Left, Behind
            new Position(-1017.72f, -3490.92f, 62.62f, 4.34f),                      // Right, Behind
            new Position(-1015.77f, -3497.15f, 62.82f, 4.34f),                      // Left, Mid
            new Position(-1019.51f, -3495.49f, 62.82f, 4.34f),                      // Right, Mid
            new Position(-1017.25f, -3500.85f, 62.98f, 4.34f),                      // Left, front
            new Position(-1020.95f, -3499.21f, 62.98f, 4.34f)                       // Right, Front
        };

        //horde run to where
        public static Position HRunTo = new Position(-1016.44f, -3508.48f, 62.96f);

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
    }

    [Script]
    class npc_doctor : ScriptedAI
    {
        ObjectGuid PlayerGUID;

        uint SummonPatientTimer;
        uint SummonPatientCount;
        uint PatientDiedCount;
        uint PatientSavedCount;

        bool Event;

        List<ObjectGuid> Patients = new();
        List<Position> Coordinates = new();

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
            me.SetUninteractible(false);
        }

        void BeginEvent(Player player)
        {
            PlayerGUID = player.GetGUID();

            SummonPatientTimer = 10000;
            SummonPatientCount = 0;
            PatientDiedCount = 0;
            PatientSavedCount = 0;

            switch (me.GetEntry())
            {
                case DoctorConst.DoctorAlliance:
                    for (byte i = 0; i < DoctorConst.AllianceCoords.Length; ++i)
                        Coordinates.Add(DoctorConst.AllianceCoords[i]);
                    break;
                case DoctorConst.DoctorHorde:
                    for (byte i = 0; i < DoctorConst.HordeCoords.Length; ++i)
                        Coordinates.Add(DoctorConst.HordeCoords[i]);
                    break;
            }

            Event = true;
            me.SetUninteractible(true);
        }

        public void PatientDied(Position point)
        {
            Player player = ObjAccessor.GetPlayer(me, PlayerGUID);
            if (player != null && ((player.GetQuestStatus(6624) == QuestStatus.Incomplete) || (player.GetQuestStatus(6622) == QuestStatus.Incomplete)))
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
            if (player != null && PlayerGUID == player.GetGUID())
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
                                if (patient != null)
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
                        case DoctorConst.DoctorAlliance:
                            patientEntry = DoctorConst.AllianceSoldierId[RandomHelper.Rand32() % 3];
                            break;
                        case DoctorConst.DoctorHorde:
                            patientEntry = DoctorConst.HordeSoldierId[RandomHelper.Rand32() % 3];
                            break;
                        default:
                            Log.outError(LogFilter.Scripts, "Invalid entry for Triage doctor. Please check your database");
                            return;
                    }

                    var point = Coordinates[RandomHelper.IRand(0, Coordinates.Count - 1)];

                    Creature patient = me.SummonCreature(patientEntry, point, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(5));
                    if (patient != null)
                    {
                        //303, this flag appear to be required for client side item.spell to work (TargetSingleFriend)
                        patient.SetUnitFlag(UnitFlags.PlayerControlled);

                        Patients.Add(patient.GetGUID());

                        var patientAI = patient.GetAI<npc_injured_patient>();
                        if (patientAI != null)
                        {
                            patientAI.DoctorGUID = me.GetGUID();
                            patientAI.Coord = point;
                        }

                        Coordinates.Remove(point);
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
    }

    [Script]
    class npc_injured_patient : ScriptedAI
    {
        public ObjectGuid DoctorGUID;
        public Position Coord;

        public npc_injured_patient(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            DoctorGUID.Clear();
            Coord = null;
        }

        public override void Reset()
        {
            Initialize();

            //no select
            me.SetUninteractible(false);

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
            if (player == null || !me.IsAlive() || spellInfo.Id != 20804)
                return;

            if (player.GetQuestStatus(6624) == QuestStatus.Incomplete || player.GetQuestStatus(6622) == QuestStatus.Incomplete)
                if (!DoctorGUID.IsEmpty())
                {
                    Creature doctor = ObjectAccessor.GetCreature(me, DoctorGUID);
                    if (doctor != null)
                        doctor.GetAI<npc_doctor>().PatientSaved(me, player, Coord);
                }

            //make uninteractible
            me.SetUninteractible(true);

            //regen health
            me.RemoveUnitFlag(UnitFlags.InCombat);

            //stand up
            me.SetStandState(UnitStandStateType.Stand);

            Talk(DoctorConst.SayDoc);

            uint mobId = me.GetEntry();
            me.SetWalk(false);

            switch (mobId)
            {
                case 12923:
                case 12924:
                case 12925:
                    me.GetMotionMaster().MovePoint(0, DoctorConst.HRunTo);
                    break;
                case 12936:
                case 12937:
                case 12938:
                    me.GetMotionMaster().MovePoint(0, DoctorConst.ARunTo);
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            //lower Hp on every world tick makes it a useful counter, not officlone though
            if (me.IsAlive() && me.GetHealth() > 6)
                me.ModifyHealth(-5);

            if (me.IsAlive() && me.GetHealth() <= 6)
            {
                me.RemoveUnitFlag(UnitFlags.InCombat);
                me.SetUninteractible(true);
                me.SetDeathState(DeathState.JustDied);
                me.SetUnitFlag3(UnitFlags3.FakeDead);

                if (!DoctorGUID.IsEmpty())
                {
                    Creature doctor = ObjectAccessor.GetCreature((me), DoctorGUID);
                    if (doctor != null)
                        doctor.GetAI<npc_doctor>().PatientDied(Coord);
                }
            }
        }
    }

    struct GarmentIds
    {
        public const uint SpellLesserHealR2 = 2052;
        public const uint SpellFortitudeR1 = 1243;

        public const uint QuestMoon = 5621;
        public const uint QuestLight1 = 5624;
        public const uint QuestLight2 = 5625;
        public const uint QuestSpirit = 5648;
        public const uint QuestDarkness = 5650;

        public const uint EntryShaya = 12429;
        public const uint EntryRoberts = 12423;
        public const uint EntryDolf = 12427;
        public const uint EntryKorja = 12430;
        public const uint EntryDgKel = 12428;

        // used by 12429, 12423, 12427, 12430, 12428, but signed for 12429
        public const uint SayThanks = 0;
        public const uint SayGoodbye = 1;
        public const uint SayHealed = 2;
    }

    [Script]
    class npc_garments_of_quests : EscortAI
    {
        ObjectGuid CasterGUID;

        bool IsHealed;
        bool CanRun;

        uint questId;

        public npc_garments_of_quests(Creature creature) : base(creature)
        {
            switch (me.GetEntry())
            {
                case GarmentIds.EntryShaya:
                    questId = GarmentIds.QuestMoon;
                    break;
                case GarmentIds.EntryRoberts:
                    questId = GarmentIds.QuestLight1;
                    break;
                case GarmentIds.EntryDolf:
                    questId = GarmentIds.QuestLight2;
                    break;
                case GarmentIds.EntryKorja:
                    questId = GarmentIds.QuestSpirit;
                    break;
                case GarmentIds.EntryDgKel:
                    questId = GarmentIds.QuestDarkness;
                    break;
                default:
                    questId = 0;
                    break;
            }

            Initialize();
        }

        void Initialize()
        {
            IsHealed = false;
            CanRun = false;

            _scheduler.SetValidator(() => CanRun && !me.IsInCombat());
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                Unit unit = ObjAccessor.GetUnit(me, CasterGUID);
                if (unit != null)
                {
                    switch (me.GetEntry())
                    {
                        case GarmentIds.EntryShaya:
                        case GarmentIds.EntryRoberts:
                        case GarmentIds.EntryDolf:
                        case GarmentIds.EntryKorja:
                        case GarmentIds.EntryDgKel:
                            Talk(GarmentIds.SayGoodbye, unit);
                            break;
                    }

                    LoadPath((me.GetEntry() << 3) | 2);
                    Start(false);
                }
                else
                    EnterEvadeMode(EvadeReason.Other);                       //something went wrong

                task.Repeat(TimeSpan.FromSeconds(30));
            });
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
            if (spellInfo.Id == GarmentIds.SpellLesserHealR2 || spellInfo.Id == GarmentIds.SpellFortitudeR1)
            {
                //not while in combat
                if (me.IsInCombat())
                    return;

                //nothing to be done now
                if (IsHealed && CanRun)
                    return;

                Player player = caster.ToPlayer();
                if (player != null)
                {
                    if (questId != 0 && player.GetQuestStatus(questId) == QuestStatus.Incomplete)
                    {
                        if (IsHealed && !CanRun && spellInfo.Id == GarmentIds.SpellFortitudeR1)
                        {
                            Talk(GarmentIds.SayThanks, player);
                            CanRun = true;
                        }
                        else if (!IsHealed && spellInfo.Id == GarmentIds.SpellLesserHealR2)
                        {
                            CasterGUID = player.GetGUID();
                            me.SetStandState(UnitStandStateType.Stand);
                            Talk(GarmentIds.SayHealed, player);
                            IsHealed = true;
                        }
                    }

                    // give quest credit, not expect any special quest objives
                    if (CanRun)
                        player.TalkedToCreature(me.GetEntry(), me.GetGUID());
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
            base.UpdateAI(diff);
        }
    }

    [Script]
    class npc_guardian : ScriptedAI
    {
        const uint SpellDeathtouch = 5;

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
                DoCastVictim(SpellDeathtouch, true);
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

        void OnPossess(bool apply)
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

    struct TournamentPennantIds
    {
        public const uint SpellStormwindAspirant = 62595;
        public const uint SpellStormwindValiant = 62596;
        public const uint SpellStormwindChapion = 62594;
        public const uint SpellGnomereganAspirant = 63394;
        public const uint SpellGnomereganValiant = 63395;
        public const uint SpellGnomereganChapion = 63396;
        public const uint SpellSenJinAspirant = 63397;
        public const uint SpellSenJinValiant = 63398;
        public const uint SpellSenJinChapion = 63399;
        public const uint SpellSilvermoonAspirant = 63401;
        public const uint SpellSilvermoonValiant = 63402;
        public const uint SpellSilvermoonChapion = 63403;
        public const uint SpellDarnassusAspirant = 63404;
        public const uint SpellDarnassusValiant = 63405;
        public const uint SpellDarnassusChapion = 63406;
        public const uint SpellExodarAspirant = 63421;
        public const uint SpellExodarValiant = 63422;
        public const uint SpellExodarChapion = 63423;
        public const uint SpellIronforgeAspirant = 63425;
        public const uint SpellIronforgeValiant = 63426;
        public const uint SpellIronforgeChapion = 63427;
        public const uint SpellUndercityAspirant = 63428;
        public const uint SpellUndercityValiant = 63429;
        public const uint SpellUndercityChapion = 63430;
        public const uint SpellOrgrimmarAspirant = 63431;
        public const uint SpellOrgrimmarValiant = 63432;
        public const uint SpellOrgrimmarChapion = 63433;
        public const uint SpellThunderBluffAspirant = 63434;
        public const uint SpellThunderBluffValiant = 63435;
        public const uint SpellThunderBluffChapion = 63436;
        public const uint SpellArgentCrusadeAspirant = 63606;
        public const uint SpellArgentCrusadeValiant = 63500;
        public const uint SpellArgentCrusadeChapion = 63501;
        public const uint SpellEbonBladeAspirant = 63607;
        public const uint SpellEbonBladeValiant = 63608;
        public const uint SpellEbonBladeChapion = 63609;

        public const uint NpcStormwindSteed = 33217;
        public const uint NpcIronforgeRam = 33316;
        public const uint NpcGnomereganMechanostrider = 33317;
        public const uint NpcExodarElekk = 33318;
        public const uint NpcDarnassianNightsaber = 33319;
        public const uint NpcOrgrimmarWolf = 33320;
        public const uint NpcDarkSpearRaptor = 33321;
        public const uint NpcThunderBluffKodo = 33322;
        public const uint NpcSilvermoonHawkstrider = 33323;
        public const uint NpcForsakenWarhorse = 33324;
        public const uint NpcArgentWarhorse = 33782;
        public const uint NpcArgentSteedAspirant = 33845;
        public const uint NpcArgentHawkstriderAspirant = 33844;

        public const uint AchievementChapionStormwind = 2781;
        public const uint AchievementChapionDarnassus = 2777;
        public const uint AchievementChapionIronforge = 2780;
        public const uint AchievementChapionGnomeregan = 2779;
        public const uint AchievementChapionTheExodar = 2778;
        public const uint AchievementChapionOrgrimmar = 2783;
        public const uint AchievementChapionSenJin = 2784;
        public const uint AchievementChapionThunderBluff = 2786;
        public const uint AchievementChapionUndercity = 2787;
        public const uint AchievementChapionSilvermoon = 2785;
        public const uint AchievementArgentValor = 2758;
        public const uint AchievementChapionAlliance = 2782;
        public const uint AchievementChapionHorde = 2788;

        public const uint QuestValiantOfStormwind = 13593;
        public const uint QuestAValiantOfStormwind = 13684;
        public const uint QuestValiantOfDarnassus = 13706;
        public const uint QuestAValiantOfDarnassus = 13689;
        public const uint QuestValiantOfIronforge = 13703;
        public const uint QuestAValiantOfIronforge = 13685;
        public const uint QuestValiantOfGnomeregan = 13704;
        public const uint QuestAValiantOfGnomeregan = 13688;
        public const uint QuestValiantOfTheExodar = 13705;
        public const uint QuestAValiantOfTheExodar = 13690;
        public const uint QuestValiantOfOrgrimmar = 13707;
        public const uint QuestAValiantOfOrgrimmar = 13691;
        public const uint QuestValiantOfSenJin = 13708;
        public const uint QuestAValiantOfSenJin = 13693;
        public const uint QuestValiantOfThunderBluff = 13709;
        public const uint QuestAValiantOfThunderBluff = 13694;
        public const uint QuestValiantOfUndercity = 13710;
        public const uint QuestAValiantOfUndercity = 13695;
        public const uint QuestValiantOfSilvermoon = 13711;
        public const uint QuestAValiantOfSilvermoon = 13696;
    }

    [Script]
    class npc_tournament_mount : VehicleAI
    {
        public npc_tournament_mount(Creature creature) : base(creature)
        {
            _pennantSpellId = 0;
        }

        public override void PassengerBoarded(Unit passenger, sbyte seatId, bool apply)
        {
            Player player = passenger.ToPlayer();
            if (player == null)
                return;

            if (apply)
            {
                _pennantSpellId = GetPennantSpellId(player);
                player.CastSpell(null, _pennantSpellId, true);
            }
            else
                player.RemoveAurasDueToSpell(_pennantSpellId);
        }


        uint _pennantSpellId;

        uint GetPennantSpellId(Player player)
        {
            switch (me.GetEntry())
            {
                case TournamentPennantIds.NpcArgentSteedAspirant:
                case TournamentPennantIds.NpcStormwindSteed:
                {
                    if (player.HasAchieved(TournamentPennantIds.AchievementChapionStormwind))
                        return TournamentPennantIds.SpellStormwindChapion;
                    else if (player.GetQuestRewardStatus(TournamentPennantIds.QuestValiantOfStormwind) || player.GetQuestRewardStatus(TournamentPennantIds.QuestAValiantOfStormwind))
                        return TournamentPennantIds.SpellStormwindValiant;
                    else
                        return TournamentPennantIds.SpellStormwindAspirant;
                }
                case TournamentPennantIds.NpcGnomereganMechanostrider:
                {
                    if (player.HasAchieved(TournamentPennantIds.AchievementChapionGnomeregan))
                        return TournamentPennantIds.SpellGnomereganChapion;
                    else if (player.GetQuestRewardStatus(TournamentPennantIds.QuestValiantOfGnomeregan) || player.GetQuestRewardStatus(TournamentPennantIds.QuestAValiantOfGnomeregan))
                        return TournamentPennantIds.SpellGnomereganValiant;
                    else
                        return TournamentPennantIds.SpellGnomereganAspirant;
                }
                case TournamentPennantIds.NpcDarkSpearRaptor:
                {
                    if (player.HasAchieved(TournamentPennantIds.AchievementChapionSenJin))
                        return TournamentPennantIds.SpellSenJinChapion;
                    else if (player.GetQuestRewardStatus(TournamentPennantIds.QuestValiantOfSenJin) || player.GetQuestRewardStatus(TournamentPennantIds.QuestAValiantOfSenJin))
                        return TournamentPennantIds.SpellSenJinValiant;
                    else
                        return TournamentPennantIds.SpellSenJinAspirant;
                }
                case TournamentPennantIds.NpcArgentHawkstriderAspirant:
                case TournamentPennantIds.NpcSilvermoonHawkstrider:
                {
                    if (player.HasAchieved(TournamentPennantIds.AchievementChapionSilvermoon))
                        return TournamentPennantIds.SpellSilvermoonChapion;
                    else if (player.GetQuestRewardStatus(TournamentPennantIds.QuestValiantOfSilvermoon) || player.GetQuestRewardStatus(TournamentPennantIds.QuestAValiantOfSilvermoon))
                        return TournamentPennantIds.SpellSilvermoonValiant;
                    else
                        return TournamentPennantIds.SpellSilvermoonAspirant;
                }
                case TournamentPennantIds.NpcDarnassianNightsaber:
                {
                    if (player.HasAchieved(TournamentPennantIds.AchievementChapionDarnassus))
                        return TournamentPennantIds.SpellDarnassusChapion;
                    else if (player.GetQuestRewardStatus(TournamentPennantIds.QuestValiantOfDarnassus) || player.GetQuestRewardStatus(TournamentPennantIds.QuestAValiantOfDarnassus))
                        return TournamentPennantIds.SpellDarnassusValiant;
                    else
                        return TournamentPennantIds.SpellDarnassusAspirant;
                }
                case TournamentPennantIds.NpcExodarElekk:
                {
                    if (player.HasAchieved(TournamentPennantIds.AchievementChapionTheExodar))
                        return TournamentPennantIds.SpellExodarChapion;
                    else if (player.GetQuestRewardStatus(TournamentPennantIds.QuestValiantOfTheExodar) || player.GetQuestRewardStatus(TournamentPennantIds.QuestAValiantOfTheExodar))
                        return TournamentPennantIds.SpellExodarValiant;
                    else
                        return TournamentPennantIds.SpellExodarAspirant;
                }
                case TournamentPennantIds.NpcIronforgeRam:
                {
                    if (player.HasAchieved(TournamentPennantIds.AchievementChapionIronforge))
                        return TournamentPennantIds.SpellIronforgeChapion;
                    else if (player.GetQuestRewardStatus(TournamentPennantIds.QuestValiantOfIronforge) || player.GetQuestRewardStatus(TournamentPennantIds.QuestAValiantOfIronforge))
                        return TournamentPennantIds.SpellIronforgeValiant;
                    else
                        return TournamentPennantIds.SpellIronforgeAspirant;
                }
                case TournamentPennantIds.NpcForsakenWarhorse:
                {
                    if (player.HasAchieved(TournamentPennantIds.AchievementChapionUndercity))
                        return TournamentPennantIds.SpellUndercityChapion;
                    else if (player.GetQuestRewardStatus(TournamentPennantIds.QuestValiantOfUndercity) || player.GetQuestRewardStatus(TournamentPennantIds.QuestAValiantOfUndercity))
                        return TournamentPennantIds.SpellUndercityValiant;
                    else
                        return TournamentPennantIds.SpellUndercityAspirant;
                }
                case TournamentPennantIds.NpcOrgrimmarWolf:
                {
                    if (player.HasAchieved(TournamentPennantIds.AchievementChapionOrgrimmar))
                        return TournamentPennantIds.SpellOrgrimmarChapion;
                    else if (player.GetQuestRewardStatus(TournamentPennantIds.QuestValiantOfOrgrimmar) || player.GetQuestRewardStatus(TournamentPennantIds.QuestAValiantOfOrgrimmar))
                        return TournamentPennantIds.SpellOrgrimmarValiant;
                    else
                        return TournamentPennantIds.SpellOrgrimmarAspirant;
                }
                case TournamentPennantIds.NpcThunderBluffKodo:
                {
                    if (player.HasAchieved(TournamentPennantIds.AchievementChapionThunderBluff))
                        return TournamentPennantIds.SpellThunderBluffChapion;
                    else if (player.GetQuestRewardStatus(TournamentPennantIds.QuestValiantOfThunderBluff) || player.GetQuestRewardStatus(TournamentPennantIds.QuestAValiantOfThunderBluff))
                        return TournamentPennantIds.SpellThunderBluffValiant;
                    else
                        return TournamentPennantIds.SpellThunderBluffAspirant;
                }
                case TournamentPennantIds.NpcArgentWarhorse:
                {
                    if (player.HasAchieved(TournamentPennantIds.AchievementChapionAlliance) || player.HasAchieved(TournamentPennantIds.AchievementChapionHorde))
                        return player.GetClass() == Class.Deathknight ? TournamentPennantIds.SpellEbonBladeChapion : TournamentPennantIds.SpellArgentCrusadeChapion;
                    else if (player.HasAchieved(TournamentPennantIds.AchievementArgentValor))
                        return player.GetClass() == Class.Deathknight ? TournamentPennantIds.SpellEbonBladeValiant : TournamentPennantIds.SpellArgentCrusadeValiant;
                    else
                        return player.GetClass() == Class.Deathknight ? TournamentPennantIds.SpellEbonBladeAspirant : TournamentPennantIds.SpellArgentCrusadeAspirant;
                }
                default:
                    return 0;
            }
        }
    }

    [Script]
    class npc_brewfest_reveler : ScriptedAI
    {
        const uint SpellBrewfestToast = 41586;

        public npc_brewfest_reveler(Creature creature) : base(creature) { }

        public override void ReceiveEmote(Player player, TextEmotes emote)
        {
            if (!GameEventMgr.IsHolidayActive(HolidayIds.Brewfest))
                return;

            if (emote == TextEmotes.Dance)
                me.CastSpell(player, SpellBrewfestToast, false);
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

        const uint SpellBrewfestToast = 41586;
        const uint NpcBrewfestReveler = 24484;

        List<ObjectGuid> _revelerGuids = new();

        npc_brewfest_reveler_2(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), task =>
            {
                List<Creature> creatureList = me.GetCreatureListWithEntryInGrid(NpcBrewfestReveler, 5.0f);
                foreach (Creature creature in creatureList)
                    if (creature != me)
                        _revelerGuids.Add(creature.GetGUID());

                _scheduler.Schedule(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), faceToTask =>
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
                        // Play random emote or dance

                        Action<TaskContext> taskContext = task =>
                        {
                            // If dancing stop before next random state
                            if (me.GetEmoteState() == Emote.StateDance)
                                me.SetEmoteState(Emote.OneshotNone);

                            // Random EventEmote or EventFaceto
                            if (RandomHelper.randChance(50))
                                faceToTask.Repeat(TimeSpan.FromSeconds(1));
                            else
                                emoteTask.Repeat(TimeSpan.FromSeconds(1));
                        };

                        if (RandomHelper.randChance(50))
                        {
                            me.HandleEmoteCommand(BrewfestRandomEmote.SelectRandom());
                            _scheduler.Schedule(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(6), taskContext);
                        }
                        else
                        {
                            me.SetEmoteState(Emote.StateDance);
                            _scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12), taskContext);
                        }
                    });
                });
            });
        }

        // Copied from old script. I don't know if this is 100% correct.
        public override void ReceiveEmote(Player player, TextEmotes emote)
        {
            if (!GameEventMgr.IsHolidayActive(HolidayIds.Brewfest))
                return;

            if (emote == TextEmotes.Dance)
                me.CastSpell(player, SpellBrewfestToast, false);
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

            if (attacker == null || damageType == DamageEffectType.DOT)
                return;

            _combatTimer[attacker.GetGUID()] = TimeSpan.FromSeconds(5);
        }

        public override void UpdateAI(uint diff)
        {
            foreach (var key in _combatTimer.Keys.ToList())
            {
                _combatTimer[key] -= TimeSpan.FromMilliseconds(diff);
                if (_combatTimer[key] <= TimeSpan.FromSeconds(0))
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

    struct WormholeIds
    {
        public const uint MenuIdWormhole = 10668; // "This tear in the fabric of time and space looks ominous."
        public const uint NpcTextWormhole = 14785; // (not 907 "What brings you to this part of the world, $n?")
        public const uint GossipOption1 = 0;     // "Borean Tundra"
        public const uint GossipOption2 = 1;     // "Howling Fjord"
        public const uint GossipOption3 = 2;     // "Sholazar BaMath.Sin"
        public const uint GossipOption4 = 3;     // "Icecrown"
        public const uint GossipOption5 = 4;     // "Storm Peaks"
        public const uint GossipOption6 = 5;     // "Underground..."

        public const uint SpellBoreanTundra = 67834; // 0
        public const uint SpellHowlingFjord = 67838; // 1
        public const uint SpellSholazarBasin = 67835; // 2
        public const uint SpellIcecrown = 67836; // 3
        public const uint SpellStormPeaks = 67837; // 4
        public const uint SpellUnderground = 68081;  // 5
    }

    [Script]
    class npc_wormhole : PassiveAI
    {
        bool _showUnderground;

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
            player.InitGossipMenu(WormholeIds.MenuIdWormhole);
            if (me.IsSummon())
            {
                if (player == me.ToTempSummon().GetSummoner())
                {
                    player.AddGossipItem(WormholeIds.MenuIdWormhole, WormholeIds.GossipOption1, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 1);
                    player.AddGossipItem(WormholeIds.MenuIdWormhole, WormholeIds.GossipOption2, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 2);
                    player.AddGossipItem(WormholeIds.MenuIdWormhole, WormholeIds.GossipOption3, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 3);
                    player.AddGossipItem(WormholeIds.MenuIdWormhole, WormholeIds.GossipOption4, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 4);
                    player.AddGossipItem(WormholeIds.MenuIdWormhole, WormholeIds.GossipOption5, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 5);

                    if (_showUnderground)
                        player.AddGossipItem(WormholeIds.MenuIdWormhole, WormholeIds.GossipOption6, eTradeskill.GossipSenderMain, eTradeskill.GossipActionInfoDef + 6);

                    player.SendGossipMenu(WormholeIds.NpcTextWormhole, me.GetGUID());
                }
            }

            return true;
        }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            uint action = player.PlayerTalkClass.GetGossipOptionAction(gossipListId);
            player.ClearGossipMenu();

            switch (action)
            {
                case eTradeskill.GossipActionInfoDef + 1: // Borean Tundra
                    player.CloseGossipMenu();
                    DoCast(player, WormholeIds.SpellBoreanTundra, false);
                    break;
                case eTradeskill.GossipActionInfoDef + 2: // Howling Fjord
                    player.CloseGossipMenu();
                    DoCast(player, WormholeIds.SpellHowlingFjord, false);
                    break;
                case eTradeskill.GossipActionInfoDef + 3: // Sholazar BaMath.Sin
                    player.CloseGossipMenu();
                    DoCast(player, WormholeIds.SpellSholazarBasin, false);
                    break;
                case eTradeskill.GossipActionInfoDef + 4: // Icecrown
                    player.CloseGossipMenu();
                    DoCast(player, WormholeIds.SpellIcecrown, false);
                    break;
                case eTradeskill.GossipActionInfoDef + 5: // Storm peaks
                    player.CloseGossipMenu();
                    DoCast(player, WormholeIds.SpellStormPeaks, false);
                    break;
                case eTradeskill.GossipActionInfoDef + 6: // Underground
                    player.CloseGossipMenu();
                    DoCast(player, WormholeIds.SpellUnderground, false);
                    break;
            }

            return true;
        }
    }

    [Script]
    class npc_spring_rabbit : ScriptedAI
    {
        const uint SpellSpringFling = 61875;
        const uint SpellSpringRabbitJump = 61724;
        const uint SpellSpringRabbitWander = 61726;
        const uint SpellSummonBabyBunny = 61727;
        const uint SpellSpringRabbitInLove = 61728;
        const uint NpcSpringRabbit = 32791;

        ObjectGuid rabbitGUID;

        public npc_spring_rabbit(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            rabbitGUID.Clear();

            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), 1, task =>
            {
                Creature rabbit = me.FindNearestCreature(NpcSpringRabbit, 10.0f);
                if (rabbit != null)
                {
                    if (rabbit == me || rabbit.HasAura(SpellSpringRabbitInLove))
                        return;

                    me.AddAura(SpellSpringRabbitInLove, me);
                    DoAction(1);
                    rabbit.AddAura(SpellSpringRabbitInLove, rabbit);
                    rabbit.GetAI().DoAction(1);
                    rabbit.CastSpell(rabbit, SpellSpringRabbitJump, true);
                    rabbitGUID = rabbit.GetGUID();
                    task.CancelGroup(1);
                }
                task.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10), task =>
            {
                Unit rabbit = ObjAccessor.GetUnit(me, rabbitGUID);
                if (rabbit != null)
                    DoCast(rabbit, SpellSpringRabbitJump);
                task.Repeat();
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20), task =>
            {
                DoCast(SpellSummonBabyBunny);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(40));
            });
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
            Initialize();
            Unit owner = me.GetOwner();
            if (owner != null)
                me.GetMotionMaster().MoveFollow(owner, SharedConst.PetFollowDist, SharedConst.PetFollowAngle);
        }

        public override void JustEngagedWith(Unit who) { }

        public override void DoAction(int param)
        {
            Unit owner = me.GetOwner();
            if (owner != null)
                owner.CastSpell(owner, SpellSpringFling, true);
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_imp_in_a_ball : ScriptedAI
    {
        const uint SayRandom = 0;
        const uint EventTalk = 1;

        ObjectGuid summonerGUID;

        public npc_imp_in_a_ball(Creature creature) : base(creature)
        {
            summonerGUID.Clear();
        }

        public override void IsSummonedBy(WorldObject summoner)
        {
            if (summoner.IsPlayer())
            {
                summonerGUID = summoner.GetGUID();
                _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
                {
                    Player owner = ObjAccessor.GetPlayer(me, summonerGUID);
                    if (owner != null)
                    {
                        CreatureTextMgr.SendChat(me, (byte)SayRandom, owner, owner.GetGroup() != null ? ChatMsg.MonsterParty : ChatMsg.MonsterWhisper, Language.Addon, CreatureTextRange.Normal);
                    }
                });
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_train_wrecker : NullCreatureAI
    {
        const uint GoToyTrain = 193963;
        const uint SpellToyTrainPulse = 61551;
        const uint SpellWreckTrain = 62943;
        const uint MoveidChase = 1;
        const uint MoveidJump = 2;

        const uint NpcExultingWindUpTrainWrecker = 81071;

        ObjectGuid _target;

        public npc_train_wrecker(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                GameObject target = me.FindNearestGameObject(GoToyTrain, 15.0f);
                if (target != null)
                {
                    _target = target.GetGUID();
                    me.SetWalk(true);
                    me.GetMotionMaster().MovePoint(MoveidChase, target.GetNearPosition(3.0f, target.GetAbsoluteAngle(me)));
                    return;
                }

                task.Repeat(TimeSpan.FromSeconds(3));
            });
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
            Initialize();
        }

        GameObject VerifyTarget()
        {
            GameObject target = ObjectAccessor.GetGameObject(me, _target);
            if (target != null)
                return target;

            me.HandleEmoteCommand(Emote.OneshotRude);
            me.DespawnOrUnsummon(TimeSpan.FromSeconds(3));
            return null;
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        void MovementInform(uint type, uint id)
        {
            _scheduler.CancelAll();
            if (id == MoveidChase)
            {
                _scheduler.Async(() =>
                {
                    GameObject target = VerifyTarget();
                    if (target != null)
                        me.GetMotionMaster().MoveJump(target, 5.0f, 10.0f, MoveidJump);
                });
            }
            else if (id == MoveidJump)
            {
                _scheduler.Async(() =>
                {
                    GameObject target = VerifyTarget();
                    if (target != null)
                    {
                        me.SetFacingTo(target.GetOrientation());
                        me.HandleEmoteCommand(Emote.OneshotAttack1h);
                        _scheduler.Schedule(TimeSpan.FromSeconds(1.5), task =>
                        {
                            GameObject target = VerifyTarget();
                            if (target != null)
                            {
                                me.CastSpell(target, SpellWreckTrain, false);
                                _scheduler.Schedule(TimeSpan.FromSeconds(2), danceTask =>
                                {
                                    me.UpdateEntry(NpcExultingWindUpTrainWrecker);
                                    me.SetEmoteState(Emote.OneshotDance);
                                    me.DespawnOrUnsummon(TimeSpan.FromSeconds(5));
                                });
                            }
                        });
                    }
                });
            }
        }
    }

    struct ArgentSquireIds
    {
        public const uint SpellDarnassusPennant = 63443;
        public const uint SpellExodarPennant = 63439;
        public const uint SpellGnomereganPennant = 63442;
        public const uint SpellIronforgePennant = 63440;
        public const uint SpellStormwindPennant = 62727;
        public const uint SpellSenjinPennant = 63446;
        public const uint SpellUndercityPennant = 63441;
        public const uint SpellOrgrimmarPennant = 63444;
        public const uint SpellSilvermoonPennant = 63438;
        public const uint SpellThunderbluffPennant = 63445;
        public const uint AuraPostmanS = 67376;
        public const uint AuraShopS = 67377;
        public const uint AuraBankS = 67368;
        public const uint AuraTiredS = 67401;
        public const uint AuraBankG = 68849;
        public const uint AuraPostmanG = 68850;
        public const uint AuraShopG = 68851;
        public const uint AuraTiredG = 68852;
        public const uint SpellTiredPlayer = 67334;

        public const uint GossipOptionBank = 0;
        public const uint GossipOptionShop = 1;
        public const uint GossipOptionMail = 2;
        public const uint GossipOptionDarnassusSenjinPennant = 3;
        public const uint GossipOptionExodarUndercityPennant = 4;
        public const uint GossipOptionGnomereganOrgrimmarPennant = 5;
        public const uint GossipOptionIronforgeSilvermoonPennant = 6;
        public const uint GossipOptionStormwindThunderbluffPennant = 7;

        public const uint NpcArgentSquire = 33238;
        public const uint AchievementPonyUp = 3736;

        public static (uint, uint)[] bannerSpells =
        {
            (SpellDarnassusPennant, SpellSenjinPennant),
            (SpellExodarPennant, SpellUndercityPennant),
            (SpellGnomereganPennant, SpellOrgrimmarPennant),
            (SpellIronforgePennant, SpellSilvermoonPennant),
            (SpellStormwindPennant, SpellThunderbluffPennant)
        };
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
                Aura ownerTired = owner.GetAura(ArgentSquireIds.SpellTiredPlayer);
                if (ownerTired != null)
                {
                    Aura squireTired = me.AddAura(IsArgentSquire() ? ArgentSquireIds.AuraTiredS : ArgentSquireIds.AuraTiredG, me);
                    if (squireTired != null)
                        squireTired.SetDuration(ownerTired.GetDuration());
                }

                if (owner.HasAchieved(ArgentSquireIds.AchievementPonyUp) && !me.HasAura(ArgentSquireIds.AuraTiredS) && !me.HasAura(ArgentSquireIds.AuraTiredG))
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
                case ArgentSquireIds.GossipOptionBank:
                {
                    me.RemoveNpcFlag(NPCFlags.Mailbox | NPCFlags.Vendor);
                    uint _bankAura = IsArgentSquire() ? ArgentSquireIds.AuraBankS : ArgentSquireIds.AuraBankG;
                    if (!me.HasAura(_bankAura))
                        DoCastSelf(_bankAura);

                    if (!player.HasAura(ArgentSquireIds.SpellTiredPlayer))
                        player.CastSpell(player, ArgentSquireIds.SpellTiredPlayer, true);
                    break;
                }
                case ArgentSquireIds.GossipOptionShop:
                {
                    me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Mailbox);
                    uint _shopAura = IsArgentSquire() ? ArgentSquireIds.AuraShopS : ArgentSquireIds.AuraShopG;
                    if (!me.HasAura(_shopAura))
                        DoCastSelf(_shopAura);

                    if (!player.HasAura(ArgentSquireIds.SpellTiredPlayer))
                        player.CastSpell(player, ArgentSquireIds.SpellTiredPlayer, true);
                    break;
                }
                case ArgentSquireIds.GossipOptionMail:
                {
                    me.RemoveNpcFlag(NPCFlags.Banker | NPCFlags.Vendor);
                    uint _mailAura = IsArgentSquire() ? ArgentSquireIds.AuraPostmanS : ArgentSquireIds.AuraPostmanG;
                    if (!me.HasAura(_mailAura))
                        DoCastSelf(_mailAura);

                    if (!player.HasAura(ArgentSquireIds.SpellTiredPlayer))
                        player.CastSpell(player, ArgentSquireIds.SpellTiredPlayer, true);
                    break;
                }
                case ArgentSquireIds.GossipOptionDarnassusSenjinPennant:
                case ArgentSquireIds.GossipOptionExodarUndercityPennant:
                case ArgentSquireIds.GossipOptionGnomereganOrgrimmarPennant:
                case ArgentSquireIds.GossipOptionIronforgeSilvermoonPennant:
                case ArgentSquireIds.GossipOptionStormwindThunderbluffPennant:
                    if (IsArgentSquire())
                        DoCastSelf(ArgentSquireIds.bannerSpells[gossipListId - 3].Item1, true);
                    else
                        DoCastSelf(ArgentSquireIds.bannerSpells[gossipListId - 3].Item2, true);

                    player.PlayerTalkClass.SendCloseGossip();
                    break;
                default:
                    break;
            }

            return false;
        }

        bool IsArgentSquire() { return me.GetEntry() == ArgentSquireIds.NpcArgentSquire; }
    }

    struct BountifulTableIds
    {
        public const sbyte SeatTurkeyChair = 0;
        public const sbyte SeatCranberryChair = 1;
        public const sbyte SeatStuffingChair = 2;
        public const sbyte SeatSweetPotatoChair = 3;
        public const sbyte SeatPieChair = 4;
        public const sbyte SeatFoodHolder = 5;
        public const sbyte SeatPlateHolder = 6;
        public const uint NpcTheTurkeyChair = 34812;
        public const uint NpcTheCranberryChair = 34823;
        public const uint NpcTheStuffingChair = 34819;
        public const uint NpcTheSweetPotatoChair = 34824;
        public const uint NpcThePieChair = 34822;
        public const uint SpellCranberryServer = 61793;
        public const uint SpellPieServer = 61794;
        public const uint SpellStuffingServer = 61795;
        public const uint SpellTurkeyServer = 61796;
        public const uint SpellSweetPotatoesServer = 61797;

        public static Dictionary<uint, uint> ChairSpells = new()
        {
            { NpcTheCranberryChair, SpellCranberryServer },
            { NpcThePieChair, SpellPieServer },
            { NpcTheStuffingChair, SpellStuffingServer },
            { NpcTheTurkeyChair, SpellTurkeyServer },
            { NpcTheSweetPotatoChair, SpellSweetPotatoesServer },
        };
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

    [Script]
    class npc_bountiful_table : PassiveAI
    {
        public npc_bountiful_table(Creature creature) : base(creature) { }

        public override void PassengerBoarded(Unit who, sbyte seatId, bool apply)
        {
            float x = 0.0f;
            float y = 0.0f;
            float z = 0.0f;
            float o = 0.0f;

            switch (seatId)
            {
                case BountifulTableIds.SeatTurkeyChair:
                    x = 3.87f;
                    y = 2.07f;
                    o = 3.700098f;
                    break;
                case BountifulTableIds.SeatCranberryChair:
                    x = 3.87f;
                    y = -2.07f;
                    o = 2.460914f;
                    break;
                case BountifulTableIds.SeatStuffingChair:
                    x = -2.52f;
                    break;
                case BountifulTableIds.SeatSweetPotatoChair:
                    x = -0.09f;
                    y = -3.24f;
                    o = 1.186824f;
                    break;
                case BountifulTableIds.SeatPieChair:
                    x = -0.18f;
                    y = 3.24f;
                    o = 5.009095f;
                    break;
                case BountifulTableIds.SeatFoodHolder:
                case BountifulTableIds.SeatPlateHolder:
                    Vehicle holders = who.GetVehicleKit();
                    if (holders != null)
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
            who.m_Events.AddEvent(new CastFoodSpell(who, BountifulTableIds.ChairSpells.LookupByKey(who.GetEntry())), who.m_Events.CalculateTime(TimeSpan.FromSeconds(1)));
            Creature creature = who.ToCreature();
            if (creature != null)
                creature.SetDisplayFromModel(0);
        }
    }

    [Script]
    class npc_gen_void_zone : ScriptedAI
    {
        const uint SpellConsumption = 28874;

        public npc_gen_void_zone(Creature creature) : base(creature) { }

        public override void InitializeAI()
        {
            me.SetReactState(ReactStates.Passive);
        }

        public override void JustAppeared()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(2), task => DoCastSelf(SpellConsumption));
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }
}

