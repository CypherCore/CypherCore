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
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.EasternKingdoms.Karazhan.Midnight
{
    struct Misc
    {
        public const uint MountedDisplayid = 16040;

        //Attumen (@Todo Use The Summoning Spell Instead Of Creature Id. It Works; But Is Not Convenient For Us)
        public const uint SummonAttumen = 15550;
    }

    struct TextIds
    {
        public const uint SayKill = 0;
        public const uint SayRandom = 1;
        public const uint SayDisarmed = 2;
        public const uint SayMidnightKill = 3;
        public const uint SayAppear = 4;
        public const uint SayMount = 5;

        public const uint SayDeath = 3;

        // Midnight
        public const uint EmoteCallAttumen = 0;
        public const uint EmoteMountUp = 1;
    }

    struct SpellIds
    {
        public const uint Shadowcleave = 29832;
        public const uint IntangiblePresence = 29833;
        public const uint SpawnSmoke = 10389;
        public const uint Charge = 29847;

        // Midnight
        public const uint Knockdown = 29711;
        public const uint SummonAttumen = 29714;
        public const uint Mount = 29770;
        public const uint SummonAttumenMounted = 29799;
    }

    enum Phases
    {
        None,
        AttumenEngages,
        Mounted
    }

    [Script]
    public class boss_attumen : BossAI
    {
        public boss_attumen(Creature creature) : base(creature, DataTypes.Attumen)
        {
            Initialize();
        }

        void Initialize()
        {
            _midnightGUID.Clear();
            _phase = Phases.None;
        }

        public override void Reset()
        {
            Initialize();
            base.Reset();
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            Creature midnight = ObjectAccessor.GetCreature(me, _midnightGUID);
            if (midnight != null)
                _DespawnAtEvade(10, midnight);

            me.DespawnOrUnsummon();
        }

        public override void ScheduleTasks()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25), task =>
            {
                DoCastVictim(SpellIds.Shadowcleave);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(45), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target != null)
                    DoCast(target, SpellIds.IntangiblePresence);

                task.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(45));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60), task =>
            {
                Talk(TextIds.SayRandom);
                task.Repeat(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));
            });
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            // Attumen does not die until he mounts Midnight, let health fall to 1 and prevent further damage.
            if (damage >= me.GetHealth() && _phase != Phases.Mounted)
                damage = (uint)(me.GetHealth() - 1);

            if (_phase == Phases.AttumenEngages && me.HealthBelowPctDamaged(25, damage))
            {
                _phase = Phases.None;

                Creature midnight = ObjectAccessor.GetCreature(me, _midnightGUID);
                if (midnight != null)
                    midnight.GetAI().DoCastAOE(SpellIds.Mount, true);
            }
        }

        public override void KilledUnit(Unit victim)
        {
            Talk(TextIds.SayKill);
        }

        public override void JustSummoned(Creature summon)
        {
            if (summon.GetEntry() == CreatureIds.AttumenMounted)
            {
                Creature midnight = ObjectAccessor.GetCreature(me, _midnightGUID);
                if (midnight != null)
                {
                    if (midnight.GetHealth() > me.GetHealth())
                        summon.SetHealth(midnight.GetHealth());
                    else
                        summon.SetHealth(me.GetHealth());

                    summon.GetAI().DoZoneInCombat();
                    summon.GetAI().SetGUID(_midnightGUID, (int)CreatureIds.Midnight);
                }
            }

            base.JustSummoned(summon);
        }

        public override void IsSummonedBy(Unit summoner)
        {
            if (summoner.GetEntry() == CreatureIds.Midnight)
                _phase = Phases.AttumenEngages;

            if (summoner.GetEntry() == CreatureIds.AttumenUnmounted)
            {
                _phase = Phases.Mounted;
                DoCastSelf(SpellIds.SpawnSmoke);

                _scheduler.Schedule(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25), task =>
                {
                    Unit target = null;
                    var t_list = me.GetThreatManager().GetThreatList();
                    List<Unit> target_list = new List<Unit>();

                    foreach (var itr in t_list)
                    {
                        target = Global.ObjAccessor.GetUnit(me, itr.GetUnitGuid());
                        if (target && !target.IsWithinDist(me, 8.00f, false) && target.IsWithinDist(me, 25.0f, false))
                            target_list.Add(target);

                        target = null;
                    }

                    if (!target_list.Empty())
                        target = target_list.SelectRandom();

                    DoCast(target, SpellIds.Charge);
                    task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(25));
                });

                _scheduler.Schedule(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35), task =>
                {
                    DoCastVictim(SpellIds.Knockdown);
                    task.Repeat(TimeSpan.FromSeconds(25), TimeSpan.FromSeconds(35));
                });
            }
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);
            Unit midnight = Global.ObjAccessor.GetUnit(me, _midnightGUID);
            if (midnight)
                midnight.KillSelf();

            base.JustDied(killer);
        }

        public override void SetGUID(ObjectGuid guid, int id = 0)
        {
            if (id == CreatureIds.Midnight)
                _midnightGUID = guid;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && _phase != Phases.None)
                return;

            _scheduler.Update(diff, DoMeleeAttackIfReady);
        }

        public override void SpellHit(Unit source, SpellInfo spell)
        {
            if (spell.Mechanic == Mechanics.Disarm)
                Talk(TextIds.SayDisarmed);

            if (spell.Id == SpellIds.Mount)
            {
                Creature midnight = ObjectAccessor.GetCreature(me, _midnightGUID);
                if (midnight != null)
                {
                    _phase = Phases.None;
                    _scheduler.CancelAll();

                    midnight.AttackStop();
                    midnight.RemoveAllAttackers();
                    midnight.SetReactState(ReactStates.Passive);
                    midnight.GetMotionMaster().MoveChase(me);
                    midnight.GetAI().Talk(TextIds.EmoteMountUp);

                    me.AttackStop();
                    me.RemoveAllAttackers();
                    me.SetReactState(ReactStates.Passive);
                    me.GetMotionMaster().MoveChase(midnight);
                    Talk(TextIds.SayMount);

                    _scheduler.Schedule(TimeSpan.FromSeconds(3), task =>
                    {
                        Creature midnight1 = ObjectAccessor.GetCreature(me, _midnightGUID);
                        if (midnight1 != null)
                        {
                            if (me.IsWithinMeleeRange(midnight1))
                            {
                                DoCastAOE(SpellIds.SummonAttumenMounted);
                                me.SetVisible(false);
                                midnight1.SetVisible(false);
                            }
                            else
                            {
                                midnight1.GetMotionMaster().MoveChase(me);
                                me.GetMotionMaster().MoveChase(midnight1);
                                task.Repeat(TimeSpan.FromSeconds(3));
                            }
                        }
                    });
                }
            }
        }

        ObjectGuid _midnightGUID;
        Phases _phase;
    }

    [Script]
    public class boss_midnight : BossAI
    {
        public boss_midnight(Creature creature) : base(creature, DataTypes.Attumen)
        {
            Initialize();
        }

        void Initialize()
        {
            _phase = Phases.None;
        }

        public override void Reset()
        {
            Initialize();
            base.Reset();
            me.SetVisible(true);
            me.SetReactState(ReactStates.Defensive);
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            // Midnight never dies, let health fall to 1 and prevent further damage.
            if (damage >= me.GetHealth())
                damage = (uint)(me.GetHealth() - 1);

            if (_phase == Phases.None && me.HealthBelowPctDamaged(95, damage))
            {
                _phase = Phases.AttumenEngages;
                Talk(TextIds.EmoteCallAttumen);
                DoCastAOE(SpellIds.SummonAttumen);
            }
            else if (_phase == Phases.AttumenEngages && me.HealthBelowPctDamaged(25, damage))
            {
                _phase = Phases.Mounted;
                DoCastAOE(SpellIds.Mount, true);
            }
        }

        public override void JustSummoned(Creature summon)
        {
            if (summon.GetEntry() == CreatureIds.AttumenUnmounted)
            {
                _attumenGUID = summon.GetGUID();
                summon.GetAI().SetGUID(me.GetGUID(), (int)CreatureIds.Midnight);
                summon.GetAI().AttackStart(me.GetVictim());
                summon.GetAI().Talk(TextIds.SayAppear);
            }

            base.JustSummoned(summon);
        }

        public override void EnterCombat(Unit who)
        {
            base.EnterCombat(who);

            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25), task =>
            {
                DoCastVictim(SpellIds.Knockdown);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(25));
            });
        }

        public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
        {
            base._DespawnAtEvade(10);
        }

        public override void KilledUnit(Unit victim)
        {
            if (_phase == Phases.AttumenEngages)
            {
                Unit unit = Global.ObjAccessor.GetUnit(me, _attumenGUID);
                if (unit != null)
                    Talk(TextIds.SayMidnightKill, unit);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() || _phase == Phases.Mounted)
                return;

            _scheduler.Update(diff, DoMeleeAttackIfReady);
        }

        ObjectGuid _attumenGUID;
        Phases _phase;
    }
}
