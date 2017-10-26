using System;
using System.Collections.Generic;
using Game.AI;
using Game.Entities;
using Framework.Constants;
using Game.Spells;
using Game.Scripting;

namespace Scripts.Northrend.FrozenHalls.PitOfSaron.BossForgemasterGarfrost
{
    struct TextIds
    {
        public const uint SayAggro = 0;
        public const uint SayPhase2 = 1;
        public const uint SayPhase3 = 2;
        public const uint SayDeath = 3;
        public const uint SaySlay = 4;
        public const uint SayThrowSaronite = 5;
        public const uint SayCastDeepFreeze = 6;

        public const uint SayTyrannusDeath = 0;
    }

    struct SpellIds
    {
        public const uint Permafrost = 70326;
        public const uint ThrowSaronite = 68788;
        public const uint ThunderingStomp = 68771;
        public const uint ChillingWave = 68778;
        public const uint DeepFreeze = 70381;
        public const uint ForgeMace = 68785;
        public const uint ForgeBlade = 68774;

        public const uint PermafrostAura = 68786;
    }

    struct Misc
    {
        public const byte PhaseOne = 1;
        public const byte PhaseTwo = 2;
        public const byte PhaseThree = 3;

        public const int EquipIdSword = 49345;
        public const int EquipIdMace = 49344;
        public const uint AchievDoesntGoToEleven = 0;
        public const uint PointForge = 0;

        public static Position northForgePos = new Position(722.5643f, -234.1615f, 527.182f, 2.16421f);
        public static Position southForgePos = new Position(639.257f, -210.1198f, 529.015f, 0.523599f);
    }

    struct Events
    {
        public const uint ThrowSaronite = 1;
        public const uint ChillingWave = 2;
        public const uint DeepFreeze = 3;
        public const uint ForgeJump = 4;
        public const uint ResumeAttack = 5;
    }

    [Script]
    class boss_garfrost : BossAI
    {
        public boss_garfrost(Creature creature) : base(creature, DataTypes.Garfrost) { }

        public override void Reset()
        {
            _Reset();
            _events.SetPhase(Misc.PhaseOne);
            SetEquipmentSlots(true);
            _permafrostStack = 0;
        }

        public override void EnterCombat(Unit who)
        {
            _EnterCombat();
            Talk(TextIds.SayAggro);
            DoCast(me, SpellIds.Permafrost);
            me.CallForHelp(70.0f);
            _events.ScheduleEvent(Events.ThrowSaronite, 7000);
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsPlayer())
                Talk(TextIds.SaySlay);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.SayDeath);
            me.RemoveAllGameObjects();

            Creature tyrannus = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.Tyrannus));
            if (tyrannus)
                tyrannus.GetAI().Talk(TextIds.SayTyrannusDeath);
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (_events.IsInPhase(Misc.PhaseOne) && !HealthAbovePct(66))
            {
                _events.SetPhase(Misc.PhaseTwo);
                Talk(TextIds.SayPhase2);
                _events.DelayEvents(8000);
                DoCast(me, SpellIds.ThunderingStomp);
                _events.ScheduleEvent(Events.ForgeJump, 1500);
                return;
            }

            if (_events.IsInPhase(Misc.PhaseTwo) && !HealthAbovePct(33))
            {
                _events.SetPhase(Misc.PhaseThree);
                Talk(TextIds.SayPhase3);
                _events.DelayEvents(8000);
                DoCast(me, SpellIds.ThunderingStomp);
                _events.ScheduleEvent(Events.ForgeJump, 1500);
                return;
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Effect || id != Misc.PointForge)
                return;

            if (_events.IsInPhase(Misc.PhaseTwo))
            {
                DoCast(me, SpellIds.ForgeBlade);
                SetEquipmentSlots(false, Misc.EquipIdSword);
            }
            if (_events.IsInPhase(Misc.PhaseThree))
            {
                me.RemoveAurasDueToSpell(SpellIds.ForgeBlade);
                DoCast(me, SpellIds.ForgeMace);
                SetEquipmentSlots(false, Misc.EquipIdMace);
            }
            _events.ScheduleEvent(Events.ResumeAttack, 5000);
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            if (spell.Id == SpellIds.PermafrostAura)
            {
                Aura aura = target.GetAura(SpellIds.PermafrostAura);
                if (aura != null)
                    _permafrostStack = Math.Max(_permafrostStack, aura.GetStackAmount());
            }
        }

        public override uint GetData(uint type)
        {
            return _permafrostStack;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case Events.ThrowSaronite:
                        {
                            Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                            if (target)
                            {
                                Talk(TextIds.SayThrowSaronite, target);
                                DoCast(target, SpellIds.ThrowSaronite);
                            }
                            _events.ScheduleEvent(Events.ThrowSaronite, TimeSpan.FromSeconds(12.5), TimeSpan.FromSeconds(20));
                        }
                        break;
                    case Events.ChillingWave:
                        DoCast(me, SpellIds.ChillingWave);
                        _events.ScheduleEvent(Events.ChillingWave, 40000, 0, Misc.PhaseTwo);
                        break;
                    case Events.DeepFreeze:
                        {
                            Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                            if (target)
                            {
                                Talk(TextIds.SayCastDeepFreeze, target);
                                DoCast(target, SpellIds.DeepFreeze);
                            }
                            _events.ScheduleEvent(Events.DeepFreeze, 35000, 0, Misc.PhaseThree);
                        }
                        break;
                    case Events.ForgeJump:
                        me.AttackStop();
                        if (_events.IsInPhase(Misc.PhaseTwo))
                            me.GetMotionMaster().MoveJump(Misc.northForgePos, 25.0f, 15.0f, Misc.PointForge);
                        else if (_events.IsInPhase(Misc.PhaseThree))
                            me.GetMotionMaster().MoveJump(Misc.southForgePos, 25.0f, 15.0f, Misc.PointForge);
                        break;
                    case Events.ResumeAttack:
                        if (_events.IsInPhase(Misc.PhaseTwo))
                            _events.ScheduleEvent(Events.ChillingWave, 5000, 0, Misc.PhaseTwo);
                        else if (_events.IsInPhase(Misc.PhaseThree))
                            _events.ScheduleEvent(Events.DeepFreeze, 10000, 0, Misc.PhaseThree);
                        AttackStart(me.GetVictim());
                        break;
                    default:
                        break;
                }

                if (me.HasUnitState(UnitState.Casting))
                    return;
            });

            DoMeleeAttackIfReady();
        }

        uint _permafrostStack;
    }

    [Script]
    class spell_garfrost_permafrost : SpellScript
    {
        public spell_garfrost_permafrost()
        {
            prevented = false;
        }

        void PreventHitByLoS(SpellMissInfo missInfo)
        {
            if (missInfo != SpellMissInfo.None)
                return;

            Unit target = GetHitUnit();
            if (target)
            {
                Unit caster = GetCaster();
                //Temporary Line of Sight Check
                List<GameObject> blockList = new List<GameObject>();
                caster.GetGameObjectListWithEntryInGrid(blockList, GameObjectIds.SaroniteRock, 100.0f);
                if (!blockList.Empty())
                {
                    foreach (var obj in blockList)
                    {
                        if (!obj.IsInvisibleDueToDespawn())
                        {
                            if (obj.IsInBetween(caster, target, 4.0f))
                            {
                                prevented = true;
                                target.ApplySpellImmune(GetSpellInfo().Id, SpellImmunity.Id, GetSpellInfo().Id, true);
                                PreventHitDefaultEffect(0);
                                PreventHitDefaultEffect(1);
                                PreventHitDefaultEffect(2);
                                PreventHitDamage();
                                break;
                            }
                        }
                    }
                }
            }
        }

        void RestoreImmunity()
        {
            Unit target = GetHitUnit();
            if (target)
            {
                target.ApplySpellImmune(GetSpellInfo().Id, SpellImmunity.Id, GetSpellInfo().Id, false);
                if (prevented)
                    PreventHitAura();
            }
        }

        public override void Register()
        {
            BeforeHit.Add(new BeforeHitHandler(PreventHitByLoS));
            AfterHit.Add(new HitHandler(RestoreImmunity));
        }

        bool prevented;
    }

    [Script]
    class achievement_doesnt_go_to_eleven : AchievementCriteriaScript
    {
        public achievement_doesnt_go_to_eleven() : base("achievement_doesnt_go_to_eleven") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (target)
            {
                Creature garfrost = target.ToCreature();
                if (garfrost)
                    if (garfrost.GetAI().GetData(Misc.AchievDoesntGoToEleven) <= 10)
                        return true;
            }

            return false;
        }
    }
}
