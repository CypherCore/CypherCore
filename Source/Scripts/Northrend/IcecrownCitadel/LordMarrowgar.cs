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
using Game.Entities;
using Game.Maps;
using Game.Movement;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Northrend.IcecrownCitadel
{
    namespace Marrorgar
    {
        struct Texts
        {
            public const int SayEnterZone = 0;
            public const int SayAggro = 1;
            public const int SayBoneStorm = 2;
            public const int SayBonespike = 3;
            public const int SayKill = 4;
            public const int SayDeath = 5;
            public const int SayBerserk = 6;
            public const int EmoteBoneStorm = 7;
        }

        struct Spells
        {
            // Lord Marrowgar
            public const uint BoneSlice = 69055;
            public const uint BoneStorm = 69076;
            public const uint BoneSpikeGraveyard = 69057;
            public const uint ColdflameNormal = 69140;
            public const uint ColdflameBoneStorm = 72705;

            // Bone Spike
            public const uint Impaled = 69065;
            public const uint RideVehicle = 46598;

            // Coldflame
            public const uint ColdflamePassive = 69145;
            public const uint ColdflameSummon = 69147;
        }

        struct Misc
        {
            public static uint[] BoneSpikeSummonId = { 69062, 72669, 72670 };

            public const uint EventBoneSpikeGraveyard = 1;
            public const uint EventColdflame = 2;
            public const uint EventBoneStormBegin = 3;
            public const uint EventBoneStormMove = 4;
            public const uint EventBoneStormEnd = 5;
            public const uint EventEnableBoneSlice = 6;
            public const uint EventEnrage = 7;
            public const uint EventWarnBoneStorm = 8;

            public const uint EventColdflameTrigger = 9;
            public const uint EventFailBoned = 10;

            public const uint EventGroupSpecial = 1;

            public const uint PointTargetBonestormPlayer = 36612631;
            public const uint PointTargetColdflame = 36672631;

            public const int DataColdflameGuid = 0;

            // Manual Marking For Targets Hit By Bone Slice As No Aura Exists For This Purpose
            // These Units Are The Tanks In This Encounter
            // And Should Be Immune To Bone Spike Graveyard
            public const int DataSpikeImmune = 1;
            //DataSpikeImmune1;          = 2; // Reserved & Used
            //DataSpikeImmune2;          = 3; // Reserved & Used

            public const int ActionClearSpikeImmunities = 1;

            public const uint MaxBoneSpikeImmune = 3;
        }

        class BoneSpikeTargetSelector : ISelector
        {
            public BoneSpikeTargetSelector(UnitAI ai)
            {
                _ai = ai;
            }

            public bool Check(Unit unit)
            {
                if (!unit.IsTypeId(TypeId.Player))
                    return false;

                if (unit.HasAura(Spells.Impaled))
                    return false;

                // Check if it is one of the tanks soaking Bone Slice
                for (int i = 0; i < Misc.MaxBoneSpikeImmune; ++i)
                    if (unit.GetGUID() == _ai.GetGUID(Misc.DataSpikeImmune + i))
                        return false;

                return true;
            }

            UnitAI _ai;
        }

        [Script]
        public class boss_lord_marrowgar : BossAI
        {
            public boss_lord_marrowgar(Creature creature) : base(creature, Bosses.LordMarrowgar)
            {
                _boneStormDuration = RaidMode<uint>(20000, 30000, 20000, 30000);
                _baseSpeed = creature.GetSpeedRate(UnitMoveType.Run);
                _coldflameLastPos.Relocate(creature);
                _introDone = false;
                _boneSlice = false;
            }

            public override void Reset()
            {
                _Reset();
                me.SetSpeedRate(UnitMoveType.Run, _baseSpeed);
                me.RemoveAurasDueToSpell(Spells.BoneStorm);
                me.RemoveAurasDueToSpell(InstanceSpells.Berserk);

                _events.ScheduleEvent(Misc.EventEnableBoneSlice, 10000);
                _events.ScheduleEvent(Misc.EventBoneSpikeGraveyard, 15000, Misc.EventGroupSpecial);
                _events.ScheduleEvent(Misc.EventColdflame, 5000, Misc.EventGroupSpecial);
                _events.ScheduleEvent(Misc.EventWarnBoneStorm, RandomHelper.URand(45000, 50000));
                _events.ScheduleEvent(Misc.EventEnrage, 600000);
                _boneSlice = false;
                _boneSpikeImmune.Clear();
            }

            public override void EnterCombat(Unit who)
            {
                Talk(Texts.SayAggro);

                me.setActive(true);
                DoZoneInCombat();
                instance.SetBossState(Bosses.LordMarrowgar, EncounterState.InProgress);
            }

            public override void JustDied(Unit killer)
            {
                Talk(Texts.SayDeath);

                _JustDied();
            }

            public override void JustReachedHome()
            {
                _JustReachedHome();
                instance.SetBossState(Bosses.LordMarrowgar, EncounterState.Fail);
                instance.SetData(DataTypes.BonedAchievement, 1);    // reset
            }

            public override void KilledUnit(Unit victim)
            {
                if (victim.IsTypeId(TypeId.Player))
                    Talk(Texts.SayKill);
            }

            public override void MoveInLineOfSight(Unit who)
            {
                if (!_introDone && me.IsWithinDistInMap(who, 70.0f))
                {
                    Talk(Texts.SayEnterZone);
                    _introDone = true;
                }
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
                        case Misc.EventBoneSpikeGraveyard:
                            if (IsHeroic() || !me.HasAura(Spells.BoneStorm))
                                DoCast(me, Spells.BoneSpikeGraveyard);
                            _events.ScheduleEvent(Misc.EventBoneSpikeGraveyard, RandomHelper.URand(15000, 20000), Misc.EventGroupSpecial);
                            break;
                        case Misc.EventColdflame:
                            _coldflameLastPos.Relocate(me);
                            _coldflameTarget.Clear();
                            if (!me.HasAura(Spells.BoneStorm))
                                DoCastAOE(Spells.ColdflameNormal);
                            else
                                DoCast(me, Spells.ColdflameBoneStorm);
                            _events.ScheduleEvent(Misc.EventColdflame, 5000, Misc.EventGroupSpecial);
                            break;
                        case Misc.EventWarnBoneStorm:
                            _boneSlice = false;
                            Talk(Texts.SayBoneStorm);
                            me.FinishSpell(CurrentSpellTypes.Melee, false);
                            DoCast(me, Spells.BoneStorm);
                            _events.DelayEvents(3000, Misc.EventGroupSpecial);
                            _events.ScheduleEvent(Misc.EventBoneStormBegin, 3050);
                            _events.ScheduleEvent(Misc.EventWarnBoneStorm, RandomHelper.URand(90000, 95000));
                            break;
                        case Misc.EventBoneStormBegin:
                            Aura pStorm = me.GetAura(Spells.BoneStorm);
                            if (pStorm != null)
                                pStorm.SetDuration((int)_boneStormDuration);
                            me.SetSpeedRate(UnitMoveType.Run, _baseSpeed * 3.0f);
                            Talk(Texts.SayBoneStorm);
                            _events.ScheduleEvent(Misc.EventBoneStormEnd, _boneStormDuration + 1);
                            goto case Misc.EventBoneStormMove;
                            // no break here
                            case Misc.EventBoneStormMove:
                            {
                                _events.ScheduleEvent(Misc.EventBoneStormMove, _boneStormDuration / 3);
                                Unit unit = SelectTarget(SelectAggroTarget.Random, 0, new NonTankTargetSelector(me));
                                if (!unit)
                                    unit = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                                if (unit)
                                    me.GetMotionMaster().MovePoint(Misc.PointTargetBonestormPlayer, unit);
                                break;
                            }
                        case Misc.EventBoneStormEnd:
                            if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Point)
                                me.GetMotionMaster().MovementExpired();
                            me.GetMotionMaster().MoveChase(me.GetVictim());
                            me.SetSpeedRate(UnitMoveType.Run, _baseSpeed);
                            _events.CancelEvent(Misc.EventBoneStormMove);
                            _events.ScheduleEvent(Misc.EventEnableBoneSlice, 10000);
                            if (!IsHeroic())
                                _events.RescheduleEvent(Misc.EventBoneSpikeGraveyard, 15000, Misc.EventGroupSpecial);
                            break;
                        case Misc.EventEnableBoneSlice:
                            _boneSlice = true;
                            break;
                        case Misc.EventEnrage:
                            DoCast(me, Texts.SayBerserk, true);
                            Talk(Texts.SayBerserk);
                            break;
                    }
                });

                // We should not melee attack when storming
                if (me.HasAura(Spells.BoneStorm))
                    return;

                // 10 seconds since encounter start Bone Slice replaces melee attacks
                if (_boneSlice && !me.GetCurrentSpell(CurrentSpellTypes.Melee))
                    DoCastVictim(Spells.BoneSlice);

                DoMeleeAttackIfReady();
            }

            public override void MovementInform(MovementGeneratorType type, uint id)
            {
                if (type != MovementGeneratorType.Point || id != Misc.PointTargetBonestormPlayer)
                    return;

                // lock movement
                me.GetMotionMaster().MoveIdle();
            }

            public Position GetLastColdflamePosition()
            {
                return _coldflameLastPos;
            }

            public override ObjectGuid GetGUID(int type = 0)
            {
                switch (type)
                {
                    case Misc.DataColdflameGuid:
                        return _coldflameTarget;
                    case Misc.DataSpikeImmune + 0:
                    case Misc.DataSpikeImmune + 1:
                    case Misc.DataSpikeImmune + 2:
                        {
                            int index = type - Misc.DataSpikeImmune;
                            if (index < _boneSpikeImmune.Count)
                                return _boneSpikeImmune[index];

                            break;
                        }
                }

                return ObjectGuid.Empty;
            }

            public override void SetGUID(ObjectGuid guid, int type = 0)
            {
                switch (type)
                {
                    case Misc.DataColdflameGuid:
                        _coldflameTarget = guid;
                        break;
                    case Misc.DataSpikeImmune:
                        _boneSpikeImmune.Add(guid);
                        break;
                }
            }

            public override void DoAction(int action)
            {
                if (action != Misc.ActionClearSpikeImmunities)
                    return;

                _boneSpikeImmune.Clear();
            }

            Position _coldflameLastPos = new Position();
            List<ObjectGuid> _boneSpikeImmune = new List<ObjectGuid>();
            ObjectGuid _coldflameTarget;
            uint _boneStormDuration;
            float _baseSpeed;
            bool _introDone;
            bool _boneSlice;
        }

        [Script]
        class npc_coldflame : ScriptedAI
        {
            public npc_coldflame(Creature creature) : base(creature) { }

            public override void IsSummonedBy(Unit owner)
            {
                if (!owner.IsTypeId(TypeId.Player))
                    return;

                Position pos = new Position();
                var marrowgarAI = (boss_lord_marrowgar)owner.GetAI();
                if (marrowgarAI != null)
                    pos.Relocate(marrowgarAI.GetLastColdflamePosition());
                else
                    pos.Relocate(owner);

                if (owner.HasAura(Spells.BoneStorm))
                {
                    float ang = Position.NormalizeOrientation(pos.GetAngle(me));
                    me.SetOrientation(ang);
                    owner.GetNearPoint2D(out pos.posX, out pos.posY, 5.0f - owner.GetObjectSize(), ang);
                }
                else
                {
                    Player target = Global.ObjAccessor.GetPlayer(owner, owner.GetAI().GetGUID(Misc.DataColdflameGuid));
                    if (!target)
                    {
                        me.DespawnOrUnsummon();
                        return;
                    }

                    float ang = Position.NormalizeOrientation(pos.GetAngle(target));
                    me.SetOrientation(ang);
                    owner.GetNearPoint2D(out pos.posX, out pos.posY, 15.0f - owner.GetObjectSize(), ang);
                }

                me.NearTeleportTo(pos.GetPositionX(), pos.GetPositionY(), me.GetPositionZ(), me.GetOrientation());
                DoCast(Spells.ColdflameSummon);
                _events.ScheduleEvent(Misc.EventColdflameTrigger, 500);
            }

            public override void UpdateAI(uint diff)
            {
                _events.Update(diff);

                if (_events.ExecuteEvent() == Misc.EventColdflameTrigger)
                {
                    Position newPos = me.GetNearPosition(5.0f, 0.0f);
                    me.NearTeleportTo(newPos.GetPositionX(), newPos.GetPositionY(), me.GetPositionZ(), me.GetOrientation());
                    DoCast(Spells.ColdflameSummon);
                    _events.ScheduleEvent(Misc.EventColdflameTrigger, 500);
                }
            }
        }

        [Script]
        class npc_bone_spike : ScriptedAI
        {
            public npc_bone_spike(Creature creature) : base(creature)
            {
                _hasTrappedUnit = false;
                Cypher.Assert(creature.GetVehicleKit());

                SetCombatMovement(false);
            }

            public override void JustDied(Unit killer)
            {
                TempSummon summ = me.ToTempSummon();
                if (summ)
                {
                    Unit trapped = summ.GetSummoner();
                    if (trapped)
                        trapped.RemoveAurasDueToSpell(Spells.Impaled);
                }

                me.DespawnOrUnsummon();
            }

            public override void KilledUnit(Unit victim)
            {
                me.DespawnOrUnsummon();
                victim.RemoveAurasDueToSpell(Spells.Impaled);
            }

            public override void IsSummonedBy(Unit summoner)
            {
                DoCast(summoner, Spells.Impaled);
                summoner.CastSpell(me, Spells.RideVehicle, true);
                _events.ScheduleEvent(Misc.EventFailBoned, 8000);
                _hasTrappedUnit = true;
            }

            public override void PassengerBoarded(Unit passenger, sbyte seat, bool apply)
            {
                if (!apply)
                    return;

                // @HACK - Change passenger offset to the one taken directly from sniffs
                // Remove this when proper calculations are implemented.
                // This fixes healing spiked people
                MoveSplineInit init = new MoveSplineInit(passenger);
                init.DisableTransportPathTransformations();
                init.MoveTo(-0.02206125f, -0.02132235f, 5.514783f, false);
                init.Launch();
            }

            public override void UpdateAI(uint diff)
            {
                if (!_hasTrappedUnit)
                    return;

                _events.Update(diff);

                if (_events.ExecuteEvent() == Misc.EventFailBoned)
                {
                    InstanceScript instance = me.GetInstanceScript();
                    if (instance != null)
                        instance.SetData(DataTypes.BonedAchievement, 0);
                }
            }

            bool _hasTrappedUnit;
        }

        [Script]
        class spell_marrowgar_coldflame : SpellScript
        {
            void SelectTarget(List<WorldObject> targets)
            {
                targets.Clear();
                // select any unit but not the tank (by owners threatlist)
                Unit target = GetCaster().GetAI().SelectTarget(SelectAggroTarget.Random, 1, -GetCaster().GetObjectSize(), true, -(int)Spells.Impaled);
                if (!target)
                    target = GetCaster().GetAI().SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true); // or the tank if its solo
                if (!target)
                    return;

                GetCaster().GetAI().SetGUID(target.GetGUID(), Misc.DataColdflameGuid);

                targets.Add(target);
            }

            void HandleScriptEffect(uint effIndex)
            {
                PreventHitDefaultEffect(effIndex);
                GetCaster().CastSpell(GetHitUnit(), (uint)GetEffectValue(), true);
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(SelectTarget, 0, Targets.UnitDestAreaEnemy));
                OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
            }
        }

        [Script]
        class spell_marrowgar_coldflame_bonestorm : SpellScript
        {
            void HandleScriptEffect(uint effIndex)
            {
                PreventHitDefaultEffect(effIndex);
                for (byte i = 0; i < 4; ++i)
                    GetCaster().CastSpell(GetHitUnit(), (uint)(GetEffectValue() + i), true);
            }

            public override void Register()
            {
                OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect));
            }
        }

        [Script]
        class spell_marrowgar_coldflame_damage : AuraScript
        {
            bool CanBeAppliedOn(Unit target)
            {
                if (target.HasAura(Spells.Impaled))
                    return false;

                SpellEffectInfo effect = GetSpellInfo().GetEffect(target.GetMap().GetDifficultyID(), 0);
                if (effect != null)
                    if (target.GetExactDist2d(GetOwner()) > effect.CalcRadius())
                        return false;

                Aura aur = target.GetAura(GetId());
                if (aur != null)
                    if (aur.GetOwner() != GetOwner())
                        return false;

                return true;
            }

            public override void Register()
            {
                DoCheckAreaTarget.Add(new CheckAreaTargetHandler(CanBeAppliedOn));
            }
        }

        [Script]
        class spell_marrowgar_bone_spike_graveyard : SpellScript
        {
            public override bool Validate(SpellInfo spell)
            {
                return ValidateSpellInfo(Misc.BoneSpikeSummonId);
            }

            public override bool Load()
            {
                return GetCaster().IsTypeId(TypeId.Unit) && GetCaster().IsAIEnabled;
            }

            SpellCastResult CheckCast()
            {
                return GetCaster().GetAI().SelectTarget(SelectAggroTarget.Random, 0, new BoneSpikeTargetSelector(GetCaster().GetAI())) ? SpellCastResult.SpellCastOk : SpellCastResult.NoValidTargets;
            }

            void HandleSpikes(uint effIndex)
            {
                PreventHitDefaultEffect(effIndex);
                Creature marrowgar = GetCaster().ToCreature();
                if (marrowgar)
                {
                    CreatureAI marrowgarAI = marrowgar.GetAI();
                    byte boneSpikeCount = (byte)(Convert.ToBoolean((int)GetCaster().GetMap().GetDifficultyID() & 1) ? 3 : 1);

                    List<Unit> targets = marrowgarAI.SelectTargetList(new BoneSpikeTargetSelector(marrowgarAI), boneSpikeCount, SelectAggroTarget.Random);
                    if (targets.Empty())
                        return;

                    uint i = 0;
                    foreach (var target in targets)
                    {
                        target.CastSpell(target, Misc.BoneSpikeSummonId[i], true);
                        i++;
                    }

                    marrowgarAI.Talk(Texts.SayBonespike);
                }
            }

            public override void Register()
            {
                OnCheckCast.Add(new CheckCastHandler(CheckCast));
                OnEffectHitTarget.Add(new EffectHandler(HandleSpikes, 1, SpellEffectName.ApplyAura));
            }
        }

        [Script]
        class spell_marrowgar_bone_storm : SpellScript
        {
            void RecalculateDamage()
            {
                SetHitDamage((int)(GetHitDamage() / Math.Max(Math.Sqrt(GetHitUnit().GetExactDist2d(GetCaster())), 1.0f)));
            }

            public override void Register()
            {
                OnHit.Add(new HitHandler(RecalculateDamage));
            }
        }

        [Script]
        class spell_marrowgar_bone_slice : SpellScript
        {
            public override bool Load()
            {
                _targetCount = 0;
                return true;
            }

            void ClearSpikeImmunities()
            {
                GetCaster().GetAI().DoAction(Misc.ActionClearSpikeImmunities);
            }

            void CountTargets(List<WorldObject> targets)
            {
                _targetCount = (uint)Math.Min(targets.Count, GetSpellInfo().MaxAffectedTargets);
            }

            void SplitDamage()
            {
                // Mark the unit as hit, even if the spell missed or was dodged/parried
                GetCaster().GetAI().SetGUID(GetHitUnit().GetGUID(), Misc.DataSpikeImmune);

                if (_targetCount == 0)
                    return; // This spell can miss all targets

                SetHitDamage((int)(GetHitDamage() / _targetCount));
            }

            public override void Register()
            {
                BeforeCast.Add(new CastHandler(ClearSpikeImmunities));
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitDestAreaEnemy));
                OnHit.Add(new HitHandler(SplitDamage));
            }

            uint _targetCount;
        }

    }
}
