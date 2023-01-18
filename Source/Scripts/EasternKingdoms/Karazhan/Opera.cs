// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.Karazhan.EsOpera
{
    struct TextIds
    {
        public const uint SayDorotheeDeath = 0;
        public const uint SayDorotheeSummon = 1;
        public const uint SayDorotheeTitoDeath = 2;
        public const uint SayDorotheeAggro = 3;

        public const uint SayRoarAggro = 0;
        public const uint SayRoarDeath = 1;
        public const uint SayRoarSlay = 2;

        public const uint SayStrawmanAggro = 0;
        public const uint SayStrawmanDeath = 1;
        public const uint SayStrawmanSlay = 2;

        public const uint SayTinheadAggro = 0;
        public const uint SayTinheadDeath = 1;
        public const uint SayTinheadSlay = 2;
        public const uint EmoteRust = 3;

        public const uint SayCroneAggro = 0;
        public const uint SayCroneDeath = 1;
        public const uint SayCroneSlay = 2;

        //RedRidingHood
        public const uint SayWolfAggro = 0;
        public const uint SayWolfSlay = 1;
        public const uint SayWolfHood = 2;
        public const uint OptionWhatPhatLewtsYouHave = 7443;

        //Romulo & Julianne
        public const uint SayJulianneAggro = 0;
        public const uint SayJulianneEnter = 1;
        public const uint SayJulianneDeath01 = 2;
        public const uint SayJulianneDeath02 = 3;
        public const uint SayJulianneResurrect = 4;
        public const uint SayJulianneSlay = 5;

        public const uint SayRomuloAggro = 0;
        public const uint SayRomuloDeath = 1;
        public const uint SayRomuloEnter = 2;
        public const uint SayRomuloResurrect = 3;
        public const uint SayRomuloSlay = 4;
    }

    struct SpellIds
    {
        // Dorothee
        public const uint Waterbolt = 31012;
        public const uint Scream = 31013;
        public const uint Summontito = 31014;

        // Tito
        public const uint Yipping = 31015;

        // Strawman
        public const uint BrainBash = 31046;
        public const uint BrainWipe = 31069;
        public const uint BurningStraw = 31075;

        // Tinhead
        public const uint Cleave = 31043;
        public const uint Rust = 31086;

        // Roar
        public const uint Mangle = 31041;
        public const uint Shred = 31042;
        public const uint FrightenedScream = 31013;

        // Crone
        public const uint ChainLightning = 32337;

        // Cyclone
        public const uint Knockback = 32334;
        public const uint CycloneVisual = 32332;

        //Red Riding Hood
        public const uint LittleRedRidingHood = 30768;
        public const uint TerrifyingHowl = 30752;
        public const uint WideSwipe = 30761;

        //Romulo & Julianne
        public const uint BlindingPassion = 30890;
        public const uint Devotion = 30887;
        public const uint EternalAffection = 30878;
        public const uint PowerfulAttraction = 30889;
        public const uint DrinkPoison = 30907;

        public const uint BackwardLunge = 30815;
        public const uint Daring = 30841;
        public const uint DeadlySwathe = 30817;
        public const uint PoisonThrust = 30822;

        public const uint UndyingLove = 30951;
        public const uint ResVisual = 24171;
    }

    struct CreatureIds
    {
        public const uint Tito = 17548;
        public const uint Cyclone = 18412;
        public const uint Crone = 18168;

        //Red Riding Hood
        public const uint BigBadWolf = 17521;

        //Romulo & Julianne
        public const uint Romulo = 17533;
    }

    struct MiscConst
    {
        //Red Riding Hood
        public const uint SoundWolfDeath = 9275;

        //Romulo & Julianne
        public const int RomuloX = -10900;
        public const int RomuloY = -1758;

        public static void SummonCroneIfReady(InstanceScript instance, Creature creature)
        {
            instance.SetData(DataTypes.OperaOzDeathcount, (uint)EncounterState.Special);  // Increment DeathCount

            if (instance.GetData(DataTypes.OperaOzDeathcount) == 4)
            {
                Creature pCrone = creature.SummonCreature(CreatureIds.Crone, -10891.96f, -1755.95f, creature.GetPositionZ(), 4.64f, TempSummonType.TimedOrDeadDespawn, TimeSpan.FromHours(2));
                if (pCrone)
                {
                    if (creature.GetVictim())
                        pCrone.GetAI().AttackStart(creature.GetVictim());
                }
            }
        }

        public static void PretendToDie(Creature creature)
        {
            creature.InterruptNonMeleeSpells(true);
            creature.RemoveAllAuras();
            creature.SetHealth(0);
            creature.SetUnitFlag(UnitFlags.Uninteractible);
            creature.GetMotionMaster().Clear();
            creature.GetMotionMaster().MoveIdle();
            creature.SetStandState(UnitStandStateType.Dead);
        }

        public static void Resurrect(Creature target)
        {
            target.RemoveUnitFlag(UnitFlags.Uninteractible);
            target.SetFullHealth();
            target.SetStandState(UnitStandStateType.Stand);
            target.CastSpell(target, SpellIds.ResVisual, true);
            if (target.GetVictim())
            {
                target.GetMotionMaster().MoveChase(target.GetVictim());
                target.GetAI().AttackStart(target.GetVictim());
            }
            else
                target.GetMotionMaster().Initialize();
        }
    }

    enum RAJPhase
    {
        Julianne = 0,
        Romulo = 1,
        Both = 2,
    }

    [Script]
    class boss_dorothee : ScriptedAI
    {
        InstanceScript instance;

        uint AggroTimer;

        uint WaterBoltTimer;
        uint FearTimer;
        uint SummonTitoTimer;

        public bool SummonedTito;
        public bool TitoDied;

        public boss_dorothee(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            AggroTimer = 500;

            WaterBoltTimer = 5000;
            FearTimer = 15000;
            SummonTitoTimer = 47500;

            SummonedTito = false;
            TitoDied = false;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayDorotheeAggro);
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDorotheeDeath);

            MiscConst.SummonCroneIfReady(instance, me);
        }

        public override void AttackStart(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.AttackStart(who);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void UpdateAI(uint diff)
        {
            if (AggroTimer != 0)
            {
                if (AggroTimer <= diff)
                {
                    me.RemoveUnitFlag(UnitFlags.NonAttackable);
                    AggroTimer = 0;
                }
                else AggroTimer -= diff;
            }

            if (!UpdateVictim())
                return;

            if (WaterBoltTimer <= diff)
            {
                DoCast(SelectTarget(SelectTargetMethod.Random, 0), SpellIds.Waterbolt);
                WaterBoltTimer = TitoDied ? 1500 : 5000u;
            }
            else WaterBoltTimer -= diff;

            if (FearTimer <= diff)
            {
                DoCastVictim(SpellIds.Scream);
                FearTimer = 30000;
            }
            else FearTimer -= diff;

            if (!SummonedTito)
            {
                if (SummonTitoTimer <= diff)
                    SummonTito();
                else SummonTitoTimer -= diff;
            }

            DoMeleeAttackIfReady();
        }

        void SummonTito()
        {
            Creature pTito = me.SummonCreature(CreatureIds.Tito, 0.0f, 0.0f, 0.0f, 0.0f, TempSummonType.TimedDespawnOutOfCombat, TimeSpan.FromSeconds(30));
            if (pTito)
            {
                Talk(TextIds.SayDorotheeSummon);
                pTito.GetAI<npc_tito>().DorotheeGUID = me.GetGUID();
                pTito.GetAI().AttackStart(me.GetVictim());
                SummonedTito = true;
                TitoDied = false;
            }
        }
    }

    [Script]
    class npc_tito : ScriptedAI
    {
        public ObjectGuid DorotheeGUID;
        uint YipTimer;

        public npc_tito(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            DorotheeGUID.Clear();
            YipTimer = 10000;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void JustEngagedWith(Unit who) { }

        public override void JustDied(Unit killer)
        {
            if (!DorotheeGUID.IsEmpty())
            {
                Creature Dorothee = ObjectAccessor.GetCreature(me, DorotheeGUID);
                if (Dorothee && Dorothee.IsAlive())
                {
                    Dorothee.GetAI<boss_dorothee>().TitoDied = true;
                    Talk(TextIds.SayDorotheeTitoDeath, Dorothee);
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (YipTimer <= diff)
            {
                DoCastVictim(SpellIds.Yipping);
                YipTimer = 10000;
            }
            else YipTimer -= diff;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class boss_strawman : ScriptedAI
    {
        InstanceScript instance;

        uint AggroTimer;
        uint BrainBashTimer;
        uint BrainWipeTimer;

        public boss_strawman(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            AggroTimer = 13000;
            BrainBashTimer = 5000;
            BrainWipeTimer = 7000;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void AttackStart(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.AttackStart(who);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayStrawmanAggro);
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            if ((spellInfo.SchoolMask == SpellSchoolMask.Fire) && ((RandomHelper.Rand32() % 10) == 0))
                DoCast(me, SpellIds.BurningStraw, new CastSpellExtraArgs(true));
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayStrawmanDeath);

            MiscConst.SummonCroneIfReady(instance, me);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayStrawmanSlay);
        }

        public override void UpdateAI(uint diff)
        {
            if (AggroTimer != 0)
            {
                if (AggroTimer <= diff)
                {
                    me.RemoveUnitFlag(UnitFlags.NonAttackable);
                    AggroTimer = 0;
                }
                else AggroTimer -= diff;
            }

            if (!UpdateVictim())
                return;

            if (BrainBashTimer <= diff)
            {
                DoCastVictim(SpellIds.BrainBash);
                BrainBashTimer = 15000;
            }
            else BrainBashTimer -= diff;

            if (BrainWipeTimer <= diff)
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target)
                    DoCast(target, SpellIds.BrainWipe);
                BrainWipeTimer = 20000;
            }
            else BrainWipeTimer -= diff;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class boss_tinhead : ScriptedAI
    {
        InstanceScript instance;

        uint AggroTimer;
        uint CleaveTimer;
        uint RustTimer;

        byte RustCount;

        public boss_tinhead(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            AggroTimer = 15000;
            CleaveTimer = 5000;
            RustTimer = 30000;

            RustCount = 0;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayTinheadAggro);
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void AttackStart(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.AttackStart(who);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayTinheadDeath);

            MiscConst.SummonCroneIfReady(instance, me);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayTinheadSlay);
        }

        public override void UpdateAI(uint diff)
        {
            if (AggroTimer != 0)
            {
                if (AggroTimer <= diff)
                {
                    me.RemoveUnitFlag(UnitFlags.NonAttackable);
                    AggroTimer = 0;
                }
                else AggroTimer -= diff;
            }

            if (!UpdateVictim())
                return;

            if (CleaveTimer <= diff)
            {
                DoCastVictim(SpellIds.Cleave);
                CleaveTimer = 5000;
            }
            else CleaveTimer -= diff;

            if (RustCount < 8)
            {
                if (RustTimer <= diff)
                {
                    ++RustCount;
                    Talk(TextIds.EmoteRust);
                    DoCast(me, SpellIds.Rust);
                    RustTimer = 6000;
                }
                else RustTimer -= diff;
            }

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class boss_roar : ScriptedAI
    {
        InstanceScript instance;

        uint AggroTimer;
        uint MangleTimer;
        uint ShredTimer;
        uint ScreamTimer;

        public boss_roar(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            AggroTimer = 20000;
            MangleTimer = 5000;
            ShredTimer = 10000;
            ScreamTimer = 15000;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void MoveInLineOfSight(Unit who)

        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void AttackStart(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.AttackStart(who);
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayRoarAggro);
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayRoarDeath);

            MiscConst.SummonCroneIfReady(instance, me);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayRoarSlay);
        }

        public override void UpdateAI(uint diff)
        {
            if (AggroTimer != 0)
            {
                if (AggroTimer <= diff)
                {
                    me.RemoveUnitFlag(UnitFlags.NonAttackable);
                    AggroTimer = 0;
                }
                else AggroTimer -= diff;
            }

            if (!UpdateVictim())
                return;

            if (MangleTimer <= diff)
            {
                DoCastVictim(SpellIds.Mangle);
                MangleTimer = RandomHelper.URand(5000, 8000);
            }
            else MangleTimer -= diff;

            if (ShredTimer <= diff)
            {
                DoCastVictim(SpellIds.Shred);
                ShredTimer = RandomHelper.URand(10000, 15000);
            }
            else ShredTimer -= diff;

            if (ScreamTimer <= diff)
            {
                DoCastVictim(SpellIds.FrightenedScream);
                ScreamTimer = RandomHelper.URand(20000, 30000);
            }
            else ScreamTimer -= diff;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class boss_crone : ScriptedAI
    {
        InstanceScript instance;

        uint CycloneTimer;
        uint ChainLightningTimer;

        public boss_crone(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            // Hello, developer from the future! It's me again!
            // This time, you're fixing Karazhan scripts. Awesome. These are a mess of hacks. An amalgamation of hacks, so to speak. Maybe even a Patchwerk thereof.
            // Anyway, I digress.
            // @todo This line below is obviously a hack. Duh. I'm just coming in here to hackfix the encounter to actually be completable.
            // It needs a rewrite. Badly. Please, take good care of it.
            me.RemoveUnitFlag(UnitFlags.NonAttackable);
            me.SetImmuneToPC(false);
            CycloneTimer = 30000;
            ChainLightningTimer = 10000;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayCroneSlay);
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayCroneAggro);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayCroneDeath);
            instance.SetBossState(DataTypes.OperaPerformance, EncounterState.Done);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (CycloneTimer <= diff)
            {
                Creature Cyclone = DoSpawnCreature(CreatureIds.Cyclone, RandomHelper.URand(0, 9), RandomHelper.URand(0, 9), 0, 0, TempSummonType.TimedDespawn, TimeSpan.FromSeconds(15));
                if (Cyclone)
                    Cyclone.CastSpell(Cyclone, SpellIds.CycloneVisual, true);
                CycloneTimer = 30000;
            }
            else CycloneTimer -= diff;

            if (ChainLightningTimer <= diff)
            {
                DoCastVictim(SpellIds.ChainLightning);
                ChainLightningTimer = 15000;
            }
            else ChainLightningTimer -= diff;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_cyclone : ScriptedAI
    {
        uint MoveTimer;

        public npc_cyclone(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            MoveTimer = 1000;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void JustEngagedWith(Unit who) { }

        public override void MoveInLineOfSight(Unit who)

        {
        }

        public override void UpdateAI(uint diff)
        {
            if (!me.HasAura(SpellIds.Knockback))
                DoCast(me, SpellIds.Knockback, new CastSpellExtraArgs(true));

            if (MoveTimer <= diff)
            {
                Position pos = me.GetRandomNearPosition(10);
                me.GetMotionMaster().MovePoint(0, pos);
                MoveTimer = RandomHelper.URand(5000, 8000);
            }
            else MoveTimer -= diff;
        }
    }

    [Script]
    class npc_grandmother : ScriptedAI
    {
        public npc_grandmother(Creature creature) : base(creature) { }

        public override bool OnGossipSelect(Player player, uint menuId, uint gossipListId)
        {
            if (menuId == TextIds.OptionWhatPhatLewtsYouHave && gossipListId == 0)
            {
                player.CloseGossipMenu();

                Creature pBigBadWolf = me.SummonCreature(CreatureIds.BigBadWolf, me.GetPositionX(), me.GetPositionY(), me.GetPositionZ(), me.GetOrientation(), TempSummonType.TimedOrDeadDespawn, TimeSpan.FromHours(2));
                if (pBigBadWolf)
                    pBigBadWolf.GetAI().AttackStart(player);

                me.DespawnOrUnsummon();
            }
            return false;
        }
    }

    [Script]
    class boss_bigbadwolf : ScriptedAI
    {
        InstanceScript instance;

        uint ChaseTimer;
        uint FearTimer;
        uint SwipeTimer;

        ObjectGuid HoodGUID;
        float TempThreat;

        bool IsChasing;

        public boss_bigbadwolf(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            ChaseTimer = 30000;
            FearTimer = RandomHelper.URand(25000, 35000);
            SwipeTimer = 5000;

            HoodGUID.Clear();
            TempThreat = 0;

            IsChasing = false;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayWolfAggro);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayWolfSlay);
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void JustDied(Unit killer)
        {
            DoPlaySoundToSet(me, MiscConst.SoundWolfDeath);
            instance.SetBossState(DataTypes.OperaPerformance, EncounterState.Done);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            DoMeleeAttackIfReady();

            if (ChaseTimer <= diff)
            {
                if (!IsChasing)
                {
                    Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                    if (target)
                    {
                        Talk(TextIds.SayWolfHood);
                        DoCast(target, SpellIds.LittleRedRidingHood, new CastSpellExtraArgs(true));
                        TempThreat = GetThreat(target);
                        if (TempThreat != 0f)
                            ModifyThreatByPercent(target, -100);
                        HoodGUID = target.GetGUID();
                        AddThreat(target, 1000000.0f);
                        ChaseTimer = 20000;
                        IsChasing = true;
                    }
                }
                else
                {
                    IsChasing = false;

                    Unit target = Global.ObjAccessor.GetUnit(me, HoodGUID);
                    if (target)
                    {
                        HoodGUID.Clear();
                        if (GetThreat(target) != 0f)
                            ModifyThreatByPercent(target, -100);
                        AddThreat(target, TempThreat);
                        TempThreat = 0;
                    }

                    ChaseTimer = 40000;
                }
            }
            else ChaseTimer -= diff;

            if (IsChasing)
                return;

            if (FearTimer <= diff)
            {
                DoCastVictim(SpellIds.TerrifyingHowl);
                FearTimer = RandomHelper.URand(25000, 35000);
            }
            else FearTimer -= diff;

            if (SwipeTimer <= diff)
            {
                DoCastVictim(SpellIds.WideSwipe);
                SwipeTimer = RandomHelper.URand(25000, 30000);
            }
            else SwipeTimer -= diff;
        }
    }

    [Script]
    class boss_julianne : ScriptedAI
    {
        InstanceScript instance;

        uint EntryYellTimer;
        uint AggroYellTimer;

        ObjectGuid RomuloGUID;

        RAJPhase Phase;

        uint BlindingPassionTimer;
        uint DevotionTimer;
        uint EternalAffectionTimer;
        uint PowerfulAttractionTimer;
        uint SummonRomuloTimer;
        public uint ResurrectTimer;
        uint DrinkPoisonTimer;
        public uint ResurrectSelfTimer;

        public bool IsFakingDeath;
        bool SummonedRomulo;
        public bool RomuloDead;

        public boss_julianne(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
            EntryYellTimer = 1000;
            AggroYellTimer = 10000;
            IsFakingDeath = false;
            ResurrectTimer = 0;
        }

        void Initialize()
        {
            RomuloGUID.Clear();
            Phase = RAJPhase.Julianne;

            BlindingPassionTimer = 30000;
            DevotionTimer = 15000;
            EternalAffectionTimer = 25000;
            PowerfulAttractionTimer = 5000;
            SummonRomuloTimer = 10000;
            DrinkPoisonTimer = 0;
            ResurrectSelfTimer = 0;

            SummonedRomulo = false;
            RomuloDead = false;
        }

        public override void Reset()
        {
            Initialize();
            if (IsFakingDeath)
            {
                MiscConst.Resurrect(me);
                IsFakingDeath = false;
            }
        }

        public override void JustEngagedWith(Unit who) { }

        public override void AttackStart(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.AttackStart(who);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void SpellHit(WorldObject caster, SpellInfo spellInfo)
        {
            if (spellInfo.Id == SpellIds.DrinkPoison)
            {
                Talk(TextIds.SayJulianneDeath01);
                DrinkPoisonTimer = 2500;
            }
        }

        public override void DamageTaken(Unit done_by, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (damage < me.GetHealth())
                return;

            //anything below only used if incoming damage will kill

            if (Phase == RAJPhase.Julianne)
            {
                damage = 0;

                //this means already drinking, so return
                if (IsFakingDeath)
                    return;

                me.InterruptNonMeleeSpells(true);
                DoCast(me, SpellIds.DrinkPoison);

                IsFakingDeath = true;
                //Is This Usefull? Creature Julianne = (ObjectAccessor.GetCreature((me), JulianneGUID));
                return;
            }

            if (Phase == RAJPhase.Romulo)
            {
                Log.outError(LogFilter.Scripts, "boss_julianneAI: cannot take damage in PhaseRomulo, why was i here?");
                damage = 0;
                return;
            }

            if (Phase == RAJPhase.Both)
            {
                //if this is true then we have to kill romulo too
                if (RomuloDead)
                {
                    Creature Romulo = ObjectAccessor.GetCreature(me, RomuloGUID);
                    if (Romulo)
                    {
                        Romulo.RemoveUnitFlag(UnitFlags.Uninteractible);
                        Romulo.GetMotionMaster().Clear();
                        Romulo.SetDeathState(DeathState.JustDied);
                        Romulo.CombatStop(true);
                        Romulo.ReplaceAllDynamicFlags(UnitDynFlags.Lootable);
                    }

                    return;
                }

                //if not already returned, then romulo is alive and we can pretend die
                Creature Romulo1 = (ObjectAccessor.GetCreature((me), RomuloGUID));
                if (Romulo1)
                {
                    MiscConst.PretendToDie(me);
                    IsFakingDeath = true;
                    Romulo1.GetAI<boss_romulo>().ResurrectTimer = 10000;
                    Romulo1.GetAI<boss_romulo>().JulianneDead = true;
                    damage = 0;
                    return;
                }
            }
            Log.outError(LogFilter.Scripts, "boss_julianneAI: DamageTaken reach end of code, that should not happen.");
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayJulianneDeath02);
            instance.SetBossState(DataTypes.OperaPerformance, EncounterState.Done);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayJulianneSlay);
        }

        public override void UpdateAI(uint diff)
        {
            if (EntryYellTimer != 0)
            {
                if (EntryYellTimer <= diff)
                {
                    Talk(TextIds.SayJulianneEnter);
                    EntryYellTimer = 0;
                }
                else EntryYellTimer -= diff;
            }

            if (AggroYellTimer != 0)
            {
                if (AggroYellTimer <= diff)
                {
                    Talk(TextIds.SayJulianneAggro);
                    me.RemoveUnitFlag(UnitFlags.NonAttackable);
                    me.SetFaction((uint)FactionTemplates.Monster2);
                    AggroYellTimer = 0;
                }
                else AggroYellTimer -= diff;
            }

            if (DrinkPoisonTimer != 0)
            {
                //will do this TimeSpan.FromSeconds(2s)ecs after spell hit. this is time to display visual as expected
                if (DrinkPoisonTimer <= diff)
                {
                    MiscConst.PretendToDie(me);
                    Phase = RAJPhase.Romulo;
                    SummonRomuloTimer = 10000;
                    DrinkPoisonTimer = 0;
                }
                else DrinkPoisonTimer -= diff;
            }

            if (Phase == RAJPhase.Romulo && !SummonedRomulo)
            {
                if (SummonRomuloTimer <= diff)
                {
                    Creature pRomulo = me.SummonCreature(CreatureIds.Romulo, MiscConst.RomuloX, MiscConst.RomuloY, me.GetPositionZ(), 0, TempSummonType.TimedOrDeadDespawn, TimeSpan.FromHours(2));
                    if (pRomulo)
                    {
                        RomuloGUID = pRomulo.GetGUID();
                        pRomulo.GetAI<boss_romulo>().JulianneGUID = me.GetGUID();
                        pRomulo.GetAI<boss_romulo>().Phase = RAJPhase.Romulo;
                        DoZoneInCombat(pRomulo);

                        pRomulo.SetFaction((uint)FactionTemplates.Monster2);
                    }
                    SummonedRomulo = true;
                }
                else SummonRomuloTimer -= diff;
            }

            if (ResurrectSelfTimer != 0)
            {
                if (ResurrectSelfTimer <= diff)
                {
                    MiscConst.Resurrect(me);
                    Phase = RAJPhase.Both;
                    IsFakingDeath = false;

                    if (me.GetVictim())
                        AttackStart(me.GetVictim());

                    ResurrectSelfTimer = 0;
                    ResurrectTimer = 1000;
                }
                else ResurrectSelfTimer -= diff;
            }

            if (!UpdateVictim() || IsFakingDeath)
                return;

            if (RomuloDead)
            {
                if (ResurrectTimer <= diff)
                {
                    Creature Romulo = ObjectAccessor.GetCreature(me, RomuloGUID);
                    if (Romulo && Romulo.GetAI<boss_romulo>().IsFakingDeath)
                    {
                        Talk(TextIds.SayJulianneResurrect);
                        MiscConst.Resurrect(Romulo);
                        Romulo.GetAI<boss_romulo>().IsFakingDeath = false;
                        RomuloDead = false;
                        ResurrectTimer = 10000;
                    }
                }
                else ResurrectTimer -= diff;
            }

            if (BlindingPassionTimer <= diff)
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target)
                    DoCast(target, SpellIds.BlindingPassion);
                BlindingPassionTimer = RandomHelper.URand(30000, 45000);
            }
            else BlindingPassionTimer -= diff;

            if (DevotionTimer <= diff)
            {
                DoCast(me, SpellIds.Devotion);
                DevotionTimer = RandomHelper.URand(15000, 45000);
            }
            else DevotionTimer -= diff;

            if (PowerfulAttractionTimer <= diff)
            {
                DoCast(SelectTarget(SelectTargetMethod.Random, 0), SpellIds.PowerfulAttraction);
                PowerfulAttractionTimer = RandomHelper.URand(5000, 30000);
            }
            else PowerfulAttractionTimer -= diff;

            if (EternalAffectionTimer <= diff)
            {
                if (RandomHelper.URand(0, 1) != 0 && SummonedRomulo)
                {
                    Creature Romulo = (ObjectAccessor.GetCreature((me), RomuloGUID));
                    if (Romulo && Romulo.IsAlive() && !RomuloDead)
                        DoCast(Romulo, SpellIds.EternalAffection);
                }
                else DoCast(me, SpellIds.EternalAffection);

                EternalAffectionTimer = RandomHelper.URand(45000, 60000);
            }
            else EternalAffectionTimer -= diff;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class boss_romulo : ScriptedAI
    {
        InstanceScript instance;

        public ObjectGuid JulianneGUID;
        public RAJPhase Phase;

        uint BackwardLungeTimer;
        uint DaringTimer;
        uint DeadlySwatheTimer;
        uint PoisonThrustTimer;
        public uint ResurrectTimer;

        public bool IsFakingDeath;
        public bool JulianneDead;

        public boss_romulo(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            JulianneGUID.Clear();
            Phase = RAJPhase.Romulo;

            BackwardLungeTimer = 15000;
            DaringTimer = 20000;
            DeadlySwatheTimer = 25000;
            PoisonThrustTimer = 10000;
            ResurrectTimer = 10000;

            IsFakingDeath = false;
            JulianneDead = false;
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void JustReachedHome()
        {
            me.DespawnOrUnsummon();
        }

        public override void DamageTaken(Unit done_by, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (damage < me.GetHealth())
                return;

            //anything below only used if incoming damage will kill

            if (Phase == RAJPhase.Romulo)
            {
                Talk(TextIds.SayRomuloDeath);
                MiscConst.PretendToDie(me);
                IsFakingDeath = true;
                Phase = RAJPhase.Both;

                Creature Julianne = ObjectAccessor.GetCreature(me, JulianneGUID);
                if (Julianne)
                {
                    Julianne.GetAI<boss_julianne>().RomuloDead = true;
                    Julianne.GetAI<boss_julianne>().ResurrectSelfTimer = 10000;
                }

                damage = 0;
                return;
            }

            if (Phase == RAJPhase.Both)
            {
                if (JulianneDead)
                {
                    Creature Julianne = ObjectAccessor.GetCreature(me, JulianneGUID);
                    if (Julianne)
                    {
                        Julianne.RemoveUnitFlag(UnitFlags.Uninteractible);
                        Julianne.GetMotionMaster().Clear();
                        Julianne.SetDeathState(DeathState.JustDied);
                        Julianne.CombatStop(true);
                        Julianne.ReplaceAllDynamicFlags(UnitDynFlags.Lootable);
                    }
                    return;
                }

                Creature Julianne1 = ObjectAccessor.GetCreature(me, JulianneGUID);
                if (Julianne1)
                {
                    MiscConst.PretendToDie(me);
                    IsFakingDeath = true;
                    Julianne1.GetAI<boss_julianne>().ResurrectTimer = 10000;
                    Julianne1.GetAI<boss_julianne>().RomuloDead = true;
                    damage = 0;
                    return;
                }
            }

            Log.outError(LogFilter.Scenario, "boss_romulo: DamageTaken reach end of code, that should not happen.");
        }

        public override void JustEngagedWith(Unit who)
        {
            Talk(TextIds.SayRomuloAggro);
            if (!JulianneGUID.IsEmpty())
            {
                Creature Julianne = ObjectAccessor.GetCreature(me, JulianneGUID);
                if (Julianne && Julianne.GetVictim())
                {
                    AddThreat(Julianne.GetVictim(), 1.0f);
                    AttackStart(Julianne.GetVictim());
                }
            }
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            base.MoveInLineOfSight(who);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayRomuloDeath);
            instance.SetBossState(DataTypes.OperaPerformance, EncounterState.Done);
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayRomuloSlay);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() || IsFakingDeath)
                return;

            if (JulianneDead)
            {
                if (ResurrectTimer <= diff)
                {
                    Creature Julianne = (ObjectAccessor.GetCreature((me), JulianneGUID));
                    if (Julianne && Julianne.GetAI<boss_julianne>().IsFakingDeath)
                    {
                        Talk(TextIds.SayRomuloResurrect);
                        MiscConst.Resurrect(Julianne);
                        Julianne.GetAI<boss_julianne>().IsFakingDeath = false;
                        JulianneDead = false;
                        ResurrectTimer = 10000;
                    }
                }
                else ResurrectTimer -= diff;
            }

            if (BackwardLungeTimer <= diff)
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 1, 100, true);
                if (target && !me.HasInArc(MathF.PI, target))
                {
                    DoCast(target, SpellIds.BackwardLunge);
                    BackwardLungeTimer = RandomHelper.URand(15000, 30000);
                }
            }
            else BackwardLungeTimer -= diff;

            if (DaringTimer <= diff)
            {
                DoCast(me, SpellIds.Daring);
                DaringTimer = RandomHelper.URand(20000, 40000);
            }
            else DaringTimer -= diff;

            if (DeadlySwatheTimer <= diff)
            {
                Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                if (target)
                    DoCast(target, SpellIds.DeadlySwathe);
                DeadlySwatheTimer = RandomHelper.URand(15000, 25000);
            }
            else DeadlySwatheTimer -= diff;

            if (PoisonThrustTimer <= diff)
            {
                DoCastVictim(SpellIds.PoisonThrust);
                PoisonThrustTimer = RandomHelper.URand(10000, 20000);
            }
            else PoisonThrustTimer -= diff;

            DoMeleeAttackIfReady();
        }
    }
}