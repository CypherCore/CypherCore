// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System;

namespace Scripts.EasternKingdoms.BlackrockMountain.BlackrockCaverns
{
    enum SpellIds
    {
        // Firecyclone
        FireCycloneAura = 74851,

        // Twilightflamecaller
        FireChanneling1 = 74911,
        FireChanneling2 = 74912,
        BlastWave = 76473,
        CallFlames = 76325,

        // Twilighttorturer
        InflictPain = 75590,
        RedHotPoker = 76478,
        Shackles = 76484,
        WildBeatdown = 76487,

        // Twilightsadist
        InflictPain1 = 76497,
        HeatSeekerBlade = 76502,
        ShortThrow = 76572,
        SinisterStrike = 76500,

        // Madprisoner
        HeadCrack = 77568,
        InfectedWound = 76512,
        Enrage = 8599,

        // Razthecrazed
        AggroNearbyTargets = 80196,
        ShadowPrison = 79725,
        LeapFromCage = 79720,
        FuriousSwipe = 80206,
        LeapFromBridge = 80273,

        // Chainsofwoe
        ChainsOfWoe1 = 75437,
        ChainsOfWoe2 = 75441,
        ChainsOfWoe3 = 75464,
        ChainsOfWoe4 = 82189,
        ChainsOfWoe5 = 82192,

        // Netherdragonessence
        NetherDragonEssence1 = 75649,
        NetherDragonEssence2 = 75650,
        NetherDragonEssence3 = 75653,
        NetherDragonEssence4 = 75654
    }

    [Script]
    class npc_fire_cyclone : ScriptedAI
    {
        public npc_fire_cyclone(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromMilliseconds(100), task =>
            {
                DoCast(me, (uint)SpellIds.FireCycloneAura, true);
                task.Repeat(TimeSpan.FromSeconds(4));
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_twilight_flame_caller : ScriptedAI
    {
        static Position[] SummonPos =
        {
            new Position(162.5990f, 1085.321f, 201.1190f, 0.0f),
            new Position(170.5469f, 1063.403f, 201.1409f, 0.0f),
            new Position(191.2326f, 1100.160f, 201.1071f, 0.0f),
            new Position(228.0816f, 1106.000f, 201.1292f, 0.0f),
            new Position(252.8351f, 1095.127f, 201.1436f, 0.0f),
            new Position(253.6476f, 1070.226f, 201.1344f, 0.0f)
        };

        const uint NPC_FIRE_CYCLONE = 40164;

        ObjectGuid _flamecaller1GUID;
        ObjectGuid _flamecaller2GUID;
        SummonList _summons;

        public npc_twilight_flame_caller(Creature creature) : base(creature)
        {
            _summons = new(me);
        }

        public override void Reset()
        {
            _flamecaller1GUID.Clear();
            _flamecaller2GUID.Clear();

            if (me.GetPositionX() > 172 && me.GetPositionX() < 173 && me.GetPositionY() > 1086 && me.GetPositionY() < 1087)
            {
                _flamecaller1GUID = me.GetGUID();
                me.SummonCreature(NPC_FIRE_CYCLONE, SummonPos[0], TempSummonType.CorpseDespawn, TimeSpan.FromSeconds(0));
                me.SummonCreature(NPC_FIRE_CYCLONE, SummonPos[1], TempSummonType.CorpseDespawn, TimeSpan.FromSeconds(0));
                me.SummonCreature(NPC_FIRE_CYCLONE, SummonPos[2], TempSummonType.CorpseDespawn, TimeSpan.FromSeconds(0));
            }
            if (me.GetPositionX() > 247 && me.GetPositionX() < 248 && me.GetPositionY() > 1081 && me.GetPositionY() < 1082)
            {
                _flamecaller2GUID = me.GetGUID();
                me.SummonCreature(NPC_FIRE_CYCLONE, SummonPos[3], TempSummonType.CorpseDespawn, TimeSpan.FromSeconds(0));
                me.SummonCreature(NPC_FIRE_CYCLONE, SummonPos[4], TempSummonType.CorpseDespawn, TimeSpan.FromSeconds(0));
                me.SummonCreature(NPC_FIRE_CYCLONE, SummonPos[5], TempSummonType.CorpseDespawn, TimeSpan.FromSeconds(0));
            }

            _scheduler.Schedule(TimeSpan.FromMilliseconds(100), task =>
            {
                if (me.GetGUID() == _flamecaller1GUID)
                    DoCast(me, (uint)SpellIds.FireChanneling1);
                if (me.GetGUID() == _flamecaller2GUID)
                    DoCast(me, (uint)SpellIds.FireChanneling2);
                task.Repeat(TimeSpan.FromSeconds(12));
            });
        }

        public override void JustSummoned(Creature summoned)
        {
            _summons.Summon(summoned);
        }

        public override void JustDied(Unit killer)
        {
            _summons.DespawnAll();
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10), task =>
            {
                DoCast(me, (uint)SpellIds.BlastWave);
                task.Repeat(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(14), task =>
            {
                DoCast(me, (uint)SpellIds.CallFlames);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_twilight_torturer : ScriptedAI
    {
        public npc_twilight_torturer(Creature creature) : base(creature) { }

        public override void Reset()
        {
            if (me.GetWaypointPathId() == 0)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(18), task =>
                {
                    DoCast(me, (uint)SpellIds.InflictPain);
                    task.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(32));
                });
            }
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(9), task =>
            {
                DoCast(me, (uint)SpellIds.RedHotPoker);
                task.Repeat(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(13), task =>
            {
                DoCast(me, (uint)SpellIds.Shackles);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(17), task =>
            {
                DoCast(me, (uint)SpellIds.WildBeatdown);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_twilight_sadist : ScriptedAI
    {
        public npc_twilight_sadist(Creature creature) : base(creature) { }

        public override void Reset()
        {
            if (me.GetWaypointPathId() == 0)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(18), task =>
                {
                    DoCast(me, (uint)SpellIds.InflictPain);
                    task.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(32));
                });
            }
        }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(13), task =>
            {
                DoCast(me, (uint)SpellIds.InflictPain1);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(13), task =>
            {
                DoCast(me, (uint)SpellIds.HeatSeekerBlade);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(17), task =>
            {
                DoCast(me, (uint)SpellIds.ShortThrow);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(17), task =>
            {
                DoCast(me, (uint)SpellIds.SinisterStrike);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_mad_prisoner : ScriptedAI
    {
        public npc_mad_prisoner(Creature creature) : base(creature) { }

        public override void Reset() { }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(9), task =>
            {
                DoCast(me, (uint)SpellIds.HeadCrack);
                task.Repeat(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(13), task =>
            {
                DoCast(me, (uint)SpellIds.InfectedWound);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(17), task =>
            {
                DoCast(me, (uint)SpellIds.Enrage);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_crazed_mage : ScriptedAI
    {
        public npc_crazed_mage(Creature creature) : base(creature) { }

        public override void Reset() { }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromSeconds(9), task =>
            {
                DoCast(me, (uint)SpellIds.HeadCrack);
                task.Repeat(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(20));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(13), task =>
            {
                DoCast(me, (uint)SpellIds.InfectedWound);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
            _scheduler.Schedule(TimeSpan.FromSeconds(17), task =>
            {
                DoCast(me, (uint)SpellIds.Enrage);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(15));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_raz_the_crazed : ScriptedAI
    {
        const uint SaySmash = 0;
        const uint TypeRaz = 1;
        const uint DataRomoggDead = 1;

        public npc_raz_the_crazed(Creature creature) : base(creature) { }

        public override void Reset() { }

        public override void JustEngagedWith(Unit who)
        {
            _scheduler.CancelAll();
            _scheduler.Schedule(TimeSpan.FromMilliseconds(500), task =>
            {
                DoCastVictim((uint)SpellIds.FuriousSwipe, true);
                task.Repeat(TimeSpan.FromMilliseconds(500));
            });
        }

        public override void IsSummonedBy(WorldObject summoner)
        {
            if (summoner.GetEntry() == CreatureIds.RomoggBonecrusher)
            {
                me.SetDisableGravity(true);
                DoCast(me, (uint)SpellIds.ShadowPrison);
                _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
                {
                    DoCast(me, (uint)SpellIds.AggroNearbyTargets);
                    task.Repeat(TimeSpan.FromSeconds(1.5));
                });
            }
        }

        public override void SetData(uint id, uint data)
        {
            if (id == TypeRaz && data == DataRomoggDead)
            {
                me.RemoveAura((uint)SpellIds.ShadowPrison);
                me.SetDisableGravity(false);
                DoCast(me, (uint)SpellIds.LeapFromCage);
                _scheduler.Schedule(TimeSpan.FromSeconds(3), _ => Talk(SaySmash));
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class npc_chains_of_woe : ScriptedAI
    {
        const uint ModelInvisible = 38330;

        public npc_chains_of_woe(Creature creature) : base(creature) { }

        public override void IsSummonedBy(WorldObject summoner)
        {
            me.SetDisplayId(ModelInvisible);
            DoCast(me, (uint)SpellIds.ChainsOfWoe1, true);
            DoCast(me, (uint)SpellIds.ChainsOfWoe2, true);
        }
    }

    [Script]
    class spell_chains_of_woe_1 : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)SpellIds.ChainsOfWoe1);
        }

        void HandleScriptEffect(uint effIndex)
        {
            if (GetHitUnit().IsPlayer())
                GetHitUnit().CastSpell(GetCaster(), (uint)SpellIds.ChainsOfWoe3, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_chains_of_woe_4 : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)SpellIds.ChainsOfWoe4);
        }

        void HandleScriptEffect(uint effIndex)
        {
            if (GetHitUnit().IsPlayer())
                GetHitUnit().CastSpell(GetHitUnit(), (uint)SpellIds.ChainsOfWoe5, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_nether_dragon_essence_1 : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo((uint)SpellIds.NetherDragonEssence2, (uint)SpellIds.NetherDragonEssence3, (uint)SpellIds.NetherDragonEssence4);
        }

        void HandleTriggerSpell(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(caster, RandomHelper.RAND((uint)SpellIds.NetherDragonEssence2, (uint)SpellIds.NetherDragonEssence3, (uint)SpellIds.NetherDragonEssence4));
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandleTriggerSpell, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script]
    class spell_nether_dragon_essence_2 : SpellScript
    {
        void ModDestHeight(ref SpellDestination dest)
        {
            Position offset = new(RandomHelper.FRand(-35.0f, 35.0f), RandomHelper.FRand(-25.0f, 25.0f), 0.0f, 0.0f);

            switch (GetSpellInfo().Id)
            {
                case (uint)SpellIds.NetherDragonEssence2:
                    offset.posZ = 25.0f;
                    break;
                case (uint)SpellIds.NetherDragonEssence3:
                    offset.posZ = 17.0f;
                    break;
                case (uint)SpellIds.NetherDragonEssence4:
                    offset.posZ = 33.0f;
                    break;
            }

            dest.RelocateOffset(offset);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new(ModDestHeight, 0, Targets.DestCasterRandom));
        }
    }
}