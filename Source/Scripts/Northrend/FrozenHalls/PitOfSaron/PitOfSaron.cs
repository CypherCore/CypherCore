using System;
using Game.Entities;
using Game.AI;
using Game.Spells;
using Game.DataStorage;
using Game.Scripting;
using Framework.Constants;
using Game.Maps;

namespace Scripts.Northrend.FrozenHalls.PitOfSaron
{
    struct TextIds
    {
        public const uint SayTyrannusCavernEntrance = 3;
    }

    struct DataTypes
    {
        // Encounter States And Guids
        public const uint Garfrost = 0;
        public const uint Ick = 1;
        public const uint Tyrannus = 2;

        // Guids
        public const uint Rimefang = 3;
        public const uint Krick = 4;
        public const uint JainaSylvanas1 = 5;    // Guid Of Either Jaina Or Sylvanas Part 1; Depending On Team; As It'S The Same Spawn.
        public const uint JainaSylvanas2 = 6;    // Guid Of Either Jaina Or Sylvanas Part 2; Depending On Team; As It'S The Same Spawn.
        public const uint TyrannusEvent = 7;
        public const uint TeamInInstance = 8;
        public const uint IceShardsHit = 9;
        public const uint CavernActive = 10;
    }

    struct CreatureIds
    {
        public const uint Garfrost = 36494;
        public const uint Krick = 36477;
        public const uint Ick = 36476;
        public const uint Tyrannus = 36658;
        public const uint Rimefang = 36661;

        public const uint TyrannusEvents = 36794;
        public const uint SylvanasPart1 = 36990;
        public const uint SylvanasPart2 = 38189;
        public const uint JainaPart1 = 36993;
        public const uint JainaPart2 = 38188;
        public const uint Kilara = 37583;
        public const uint Elandra = 37774;
        public const uint Koralen = 37779;
        public const uint Korlaen = 37582;
        public const uint Champion1Horde = 37584;
        public const uint Champion2Horde = 37587;
        public const uint Champion3Horde = 37588;
        public const uint Champion1Alliance = 37496;
        public const uint Champion2Alliance = 37497;

        public const uint HordeSlave1 = 36770;
        public const uint HordeSlave2 = 36771;
        public const uint HordeSlave3 = 36772;
        public const uint HordeSlave4 = 36773;
        public const uint AllianceSlave1 = 36764;
        public const uint AllianceSlave2 = 36765;
        public const uint AllianceSlave3 = 36766;
        public const uint AllianceSlave4 = 36767;
        public const uint FreedSlave1Alliance = 37575;
        public const uint FreedSlave2Alliance = 37572;
        public const uint FreedSlave3Alliance = 37576;
        public const uint FreedSlave1Horde = 37579;
        public const uint FreedSlave2Horde = 37578;
        public const uint FreedSlave3Horde = 37577;
        public const uint RescuedSlaveAlliance = 36888;
        public const uint RescuedSlaveHorde = 36889;
        public const uint MartinVictus1 = 37591;
        public const uint MartinVictus2 = 37580;
        public const uint GorkunIronskull1 = 37581;
        public const uint GorkunIronskull2 = 37592;

        public const uint ForgemasterStalker = 36495;
        public const uint ExplodingOrb = 36610;
        public const uint YmirjarDeathbringer = 36892;
        public const uint IcyBlast = 36731;
        public const uint CavernEventTrigger = 32780;
    }

    struct GameObjectIds
    {
        public const uint SaroniteRock = 196485;
        public const uint IceWall = 201885;
        public const uint HallsOfReflectionPortcullis = 201848;
    }

    struct SpellIds
    {
        public const uint IcicleSummon = 69424;
        public const uint IcicleFallTrigger = 69426;
        public const uint IcicleFallVisual = 69428;
        public const uint AchievDontLookUpCredit = 72845;

        public const uint Fireball = 69583; //Ymirjar Flamebearer
        public const uint Hellfire = 69586;
        public const uint TacticalBlink = 69584;
        public const uint FrostBreath = 69527; //Iceborn Proto-Drake
        public const uint LeapingFaceMaul = 69504; // Geist Ambusher
    }

    [Script]
    class npc_ymirjar_flamebearer : ScriptedAI
    {
        public npc_ymirjar_flamebearer(Creature creature) : base(creature)
        {
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
        }

        public override void EnterCombat(Unit who)
        {
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));
            _scheduler.Schedule(TimeSpan.FromSeconds(4), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    DoCast(target, SpellIds.Fireball);
                task.Repeat(TimeSpan.FromSeconds(5));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(15), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    DoCast(target, SpellIds.TacticalBlink);
                DoCast(me, SpellIds.Hellfire);
                task.Repeat(TimeSpan.FromSeconds(12));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_iceborn_protodrake : ScriptedAI
    {
        public npc_iceborn_protodrake(Creature creature) : base(creature) { }

        public override void EnterCombat(Unit who)
        {
            Vehicle vehicle = me.GetVehicleKit();
            if (vehicle)
                vehicle.RemoveAllPassengers();

            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                DoCastVictim(SpellIds.FrostBreath);
                task.Repeat(TimeSpan.FromSeconds(10));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_geist_ambusher : ScriptedAI
    {
        public npc_geist_ambusher(Creature creature) : base(creature) { }

        public override void EnterCombat(Unit who)
        {
            if (who.GetTypeId() != TypeId.Player)
                return;

            // the max range is determined by aggro range
            if (me.GetDistance(who) > 5.0f)
                DoCast(who, SpellIds.LeapingFaceMaul);

            _scheduler.Schedule(TimeSpan.FromSeconds(9), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 5.0f, true);
                if (target)
                    DoCast(target, SpellIds.LeapingFaceMaul);

                task.Repeat(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(14));
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class spell_trash_npc_glacial_strike : AuraScript
    {
        void PeriodicTick(AuraEffect aurEff)
        {
            if (GetTarget().IsFullHealth())
            {
                GetTarget().RemoveAura(GetId(), ObjectGuid.Empty, 0, AuraRemoveMode.EnemySpell);
                PreventDefaultAction();
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(PeriodicTick, 2, AuraType.PeriodicDamagePercent));
        }
    }

    [Script]
    class npc_pit_of_saron_icicle : PassiveAI
    {
        public npc_pit_of_saron_icicle(Creature creature) : base(creature)
        {
            me.SetDisplayFromModel(0);
        }

        public override void IsSummonedBy(Unit summoner)
        {
            _summonerGUID = summoner.GetGUID();

            _scheduler.Schedule(TimeSpan.FromMilliseconds(3650), task =>
            {
                DoCastSelf(SpellIds.IcicleFallTrigger, true);
                DoCastSelf(SpellIds.IcicleFallVisual);

                Unit caster = Global.ObjAccessor.GetUnit(me, _summonerGUID);
                if (caster)
                    caster.RemoveDynObject(SpellIds.IcicleSummon);
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }

        ObjectGuid _summonerGUID;
    }

    [Script]
    class spell_pos_ice_shards : SpellScript
    {
        void HandleScriptEffect(uint effIndex)
        {
            if (GetHitPlayer())
                GetCaster().GetInstanceScript().SetData(DataTypes.CavernActive, 1);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script]
    class at_pit_cavern_entrance : AreaTriggerScript
    {
        public at_pit_cavern_entrance() : base("at_pit_cavern_entrance") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            if (!entered)
                return true;

            InstanceScript instance = player.GetInstanceScript();
            if (instance != null)
            {
                if (instance.GetData(DataTypes.CavernActive) != 0)
                    return true;

                instance.SetData(DataTypes.CavernActive, 1);

                Creature tyrannus = ObjectAccessor.GetCreature(player, instance.GetGuidData(DataTypes.TyrannusEvent));
                if (tyrannus)
                    tyrannus.GetAI().Talk(TextIds.SayTyrannusCavernEntrance);
            }
            return true;
        }
    }

    [Script]
    class at_pit_cavern_end : AreaTriggerScript
    {
        public at_pit_cavern_end() : base("at_pit_cavern_end") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            if (!entered)
                return true;

            InstanceScript instance = player.GetInstanceScript();
            if (instance != null)
            {
                instance.SetData(DataTypes.CavernActive, 0);

                if (instance.GetData(DataTypes.IceShardsHit) == 0)
                    instance.DoUpdateCriteria(CriteriaTypes.BeSpellTarget, SpellIds.AchievDontLookUpCredit, 0, player);
            }

            return true;
        }
    }
}
