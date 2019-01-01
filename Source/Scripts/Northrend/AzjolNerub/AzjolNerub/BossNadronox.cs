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
using Game.Combat;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Northrend.AzjolNerub.AzjolNerub.Nadronox
{
    struct SpellIds
    {
        // Hadronox
        public const uint WebFrontDoors = 53177;
        public const uint WebSideDoors = 53185;
        public const uint LeechPoison = 53030;
        public const uint LeechPoisonHeal = 53800;
        public const uint AcidCloud = 53400;
        public const uint WebGrab = 57731;
        public const uint PierceArmor = 53418;

        // Anub'Ar Opponent Summoning Spells
        public const uint SummonChampionPeriodic = 53035;
        public const uint SummonCryptFiendPeriodic = 53037;
        public const uint SummonNecromancerPeriodic = 53036;
        public const uint SummonChampionTop = 53064;
        public const uint SummonCryptFiendTop = 53065;
        public const uint SummonNecromancerTop = 53066;
        public const uint SummonChampionBottom = 53090;
        public const uint SummonCryptFiendBottom = 53091;
        public const uint SummonNecromancerBottom = 53092;

        // Anub'Ar Crusher
        public const uint Smash = 53318;
        public const uint Frenzy = 53801;

        // Anub'Ar Foes - Shared
        public const uint Taunt = 53798;

        // Anub'Ar Champion
        public const uint Rend = 59343;
        public const uint Pummel = 59344;

        // Anub'Ar Crypt Guard
        public const uint CrushingWebs = 59347;
        public const uint InfectedWound = 59348;

        // Anub'Ar Necromancer
        public const uint ShadowBolt = 53333;
        public const uint AnimateBones1 = 53334;
        public const uint AnimateBones2 = 53336;
    }

    enum SummonGroups
    {
        Crusher1 = 1,
        Crusher2 = 2,
        Crusher3 = 3
    }

    struct ActionIds
    {
        public const int HadronoxMove = 1;
        public const int CrusherEngaged = 2;
        public const int PackWalk = 3;
    }

    struct Data
    {
        public const uint CrusherPackId = 1;
        public const uint HadronoxEnteredCombat = 2;
        public const uint HadronoxWebbedDoors = 3;
    }

    struct CreatureIds
    {
        public const uint Crusher = 28922;
        public const uint WorldtriggerLarge = 23472;
    }

    struct TextIds
    {
        public const uint SayCrusherAggro = 1;
        public const uint EmoteCrusherFrenzy = 2;
        public const uint EmoteHadronoxMove = 1;
    }

    // Movement IDs used by the permanently spawning Anub'ar opponents - they are done in sequence, as one finishes, the next one starts
    enum MovementIds
    {
        None = 0,
        Outside,
        Downstairs,
        Downstairs2,
        Hadronox,       // this one might have us take a detour to avoid pathfinding "through" the floor...
        HadronoxReal   // while this one will always make us movechase
    }

    struct Misc
    {
        public static Position[] hadronoxStep =
        {
            new Position(515.5848f, 544.2007f, 673.6272f),
            new Position(562.191f , 514.068f , 696.4448f),
            new Position(610.3828f, 518.6407f, 695.9385f),
            new Position(530.42f  , 560.003f,  733.0308f)
        };

        public static Position[] crusherWaypoints =
        {
            new Position(529.6913f, 547.1257f, 731.9155f, 4.799650f),
            new Position(517.51f  , 561.439f , 734.0306f, 4.520403f),
            new Position(543.414f , 551.728f , 732.0522f, 3.996804f)
        };

        public static Position[] championWaypoints =
        {
            new Position(539.2076f, 549.7539f, 732.8668f, 4.55531f),
            new Position(527.3098f, 559.5197f, 732.9407f, 4.742493f),
            new Position()
        };

        public static Position[] cryptFiendWaypoints =
        {
            new Position(520.3911f, 548.7895f, 732.0118f, 5.0091f),
            new Position(),
            new Position(550.9611f, 545.1674f, 731.9031f, 3.996804f)
        };

        public static Position[] necromancerWaypoints =
        {
            new Position(),
            new Position(507.6937f, 563.3471f, 734.8986f, 4.520403f),
            new Position(535.1049f, 552.8961f, 732.8441f, 3.996804f),
        };

        public static Position[] initialMoves =
        {
            new Position(485.314606f, 611.418640f, 771.428406f),
            new Position(575.760437f, 611.516418f, 771.427368f),
            new Position(588.930725f, 598.233276f, 739.142151f)
        };

        public static Position[] downstairsMoves =
        {
            new Position(513.574341f, 587.022156f, 736.229065f),
            new Position(537.920410f, 580.436157f, 732.796692f),
            new Position(601.289246f, 583.259644f, 725.443054f),
        };

        public static Position[] downstairsMoves2 =
        {
            new Position(571.498718f, 576.978333f, 727.582947f),
            new Position(571.498718f, 576.978333f, 727.582947f),
            new Position()
        };
    }

    [Script]
    class boss_hadronox : BossAI
    {
        public boss_hadronox(Creature creature) : base(creature, ANDataTypes.Hadronox) { }

        bool IsInCombatWithPlayer()
        {
            List<HostileReference> refs = me.GetThreatManager().getThreatList();
            foreach (HostileReference hostileRef in refs)
            {
                Unit target = hostileRef.getTarget();
                if (target)
                    if (target.IsControlledByPlayer())
                        return true;
            }
            return false;
        }

        void SetStep(byte step)
        {
            if (_lastPlayerCombatState)
                return;

            _step = step;
            me.SetHomePosition(Misc.hadronoxStep[step]);
            me.GetMotionMaster().Clear();
            me.AttackStop();
            SetCombatMovement(false);
            me.GetMotionMaster().MovePoint(0, Misc.hadronoxStep[step]);
        }

        void SummonCrusherPack(SummonGroups group)
        {
            List<TempSummon> summoned;
            me.SummonCreatureGroup((byte)group, out summoned);
            foreach (TempSummon summon in summoned)
            {
                summon.GetAI().SetData(Data.CrusherPackId, (uint)group);
                summon.GetAI().DoAction(ActionIds.PackWalk);
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point)
                return;
            SetCombatMovement(true);
            AttackStart(me.GetVictim());
            if (_step < Misc.hadronoxStep.Length - 1)
                return;
            DoCastAOE(SpellIds.WebFrontDoors);
            DoCastAOE(SpellIds.WebSideDoors);
            _doorsWebbed = true;
            DoZoneInCombat();
        }

        public override uint GetData(uint data)
        {
            if (data == Data.HadronoxEnteredCombat)
                return _enteredCombat ? 1 : 0u;
            if (data == Data.HadronoxWebbedDoors)
                return _doorsWebbed ? 1 : 0u;
            return 0;
        }

        public override bool CanAIAttack(Unit target)
        {
            // Prevent Hadronox from going too far from her current home position
            if (!target.IsControlledByPlayer() && target.GetDistance(me.GetHomePosition()) > 20.0f)
                return false;
            return base.CanAIAttack(target);
        }

        public override void EnterCombat(Unit who)
        {
            _scheduler.CancelAll();
            _scheduler.SetValidator(() => !me.HasUnitState(UnitState.Casting));

            _scheduler.Schedule(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(7), task =>
            {
                DoCastAOE(SpellIds.LeechPoison);
                task.Repeat(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(9));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(13), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0, 100.0f);
                if (target)
                    DoCast(target, SpellIds.AcidCloud);
                task.Repeat(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(23));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(19), task =>
            {
                DoCastAOE(SpellIds.WebGrab);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(7), task =>
            {
                DoCastVictim(SpellIds.PierceArmor);
                task.Repeat(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(1), task =>
            {
                if (IsInCombatWithPlayer() != _lastPlayerCombatState)
                {
                    _lastPlayerCombatState = !_lastPlayerCombatState;
                    if (_lastPlayerCombatState) // we are now in combat with players
                    {
                        if (!instance.CheckRequiredBosses(ANDataTypes.Hadronox))
                        {
                            EnterEvadeMode(EvadeReason.SequenceBreak);
                            return;
                        }
                        // cancel current point movement if engaged by players
                        if (me.GetMotionMaster().GetCurrentMovementGeneratorType() == MovementGeneratorType.Point)
                        {
                            me.GetMotionMaster().Clear();
                            SetCombatMovement(true);
                            AttackStart(me.GetVictim());
                        }
                    }
                    else // we are no longer in combat with players - reset the encounter
                        EnterEvadeMode(EvadeReason.NoHostiles);
                }
                task.Repeat(TimeSpan.FromSeconds(1));
            });

            me.setActive(true);
        }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case ActionIds.CrusherEngaged:
                    if (_enteredCombat)
                        break;
                    instance.SetBossState(ANDataTypes.Hadronox, EncounterState.InProgress);
                    _enteredCombat = true;
                    SummonCrusherPack(SummonGroups.Crusher2);
                    SummonCrusherPack(SummonGroups.Crusher3);
                    break;
                case ActionIds.HadronoxMove:
                    if (_step < Misc.hadronoxStep.Length - 1)
                    {
                        SetStep((byte)(_step + 1));
                        Talk(TextIds.EmoteHadronoxMove);
                    }
                    break;
            }
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            List<Creature> triggers = new List<Creature>();
            me.GetCreatureListWithEntryInGrid(triggers, CreatureIds.WorldtriggerLarge);
            foreach (Creature trigger in triggers)
            {
                if (trigger.HasAura(SpellIds.SummonChampionPeriodic) || trigger.HasAura(SpellIds.WebFrontDoors) || trigger.HasAura(SpellIds.WebSideDoors))
                    _DespawnAtEvade(25, trigger);
            }
            _DespawnAtEvade(25);
            summons.DespawnAll();
            foreach (ObjectGuid gNerubian in _anubar)
            {
                Creature nerubian = ObjectAccessor.GetCreature(me, gNerubian);
                if (nerubian)
                    nerubian.DespawnOrUnsummon();
            }
        }

        public override void SetGUID(ObjectGuid guid, int what)
        {
            _anubar.Add(guid);
        }

        public void Initialize()
        {
            me.SetFloatValue(UnitFields.BoundingRadius, 9.0f);
            me.SetFloatValue(UnitFields.CombatReach, 9.0f);
            _enteredCombat = false;
            _doorsWebbed = false;
            _lastPlayerCombatState = false;
            SetStep(0);
            SetCombatMovement(true);
            SummonCrusherPack(SummonGroups.Crusher1);
        }

        public override void InitializeAI()
        {
            base.InitializeAI();
            if (me.IsAlive())
                Initialize();
        }

        public override void JustRespawned()
        {
            base.JustRespawned();
            Initialize();
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

        // Safeguard to prevent Hadronox dying to NPCs
        public override void DamageTaken(Unit who, ref uint damage)
        {
            if (!who.IsControlledByPlayer() && me.HealthBelowPct(70))
            {
                if (me.HealthBelowPctDamaged(5, damage))
                    damage = 0;
                else
                    damage *= (uint)((me.GetHealthPct() - 5.0f) / 65.0f);
            }
        }

        public override void JustSummoned(Creature summon)
        {
            summons.Summon(summon);
            // Do not enter combat with zone
        }

        bool _enteredCombat; // has a player entered combat with the first crusher pack? (talk and spawn two more packs)
        bool _doorsWebbed;   // obvious - have we reached the top and webbed the doors shut? (trigger for hadronox denied achievement)
        bool _lastPlayerCombatState; // was there a player in our threat list the last time we checked (we check every second)
        byte _step;
        List<ObjectGuid> _anubar = new List<ObjectGuid>();
    }

    class npc_hadronox_crusherPackAI : ScriptedAI
    {
        public npc_hadronox_crusherPackAI(Creature creature, Position[] positions) : base(creature)
        {
            _instance = creature.GetInstanceScript();
            _positions = positions;
            _myPack = 0;
            _doFacing = false;
        }

        public override void DoAction(int action)
        {
            if (action == ActionIds.PackWalk)
            {
                switch (_myPack)
                {
                    case SummonGroups.Crusher1:
                    case SummonGroups.Crusher2:
                    case SummonGroups.Crusher3:
                        me.GetMotionMaster().MovePoint(ActionIds.PackWalk, _positions[_myPack - SummonGroups.Crusher1]);
                        break;
                    default:
                        break;
                }
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type == MovementGeneratorType.Point && id == ActionIds.PackWalk)
                _doFacing = true;
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            Creature hadronox = _instance.GetCreature(ANDataTypes.Hadronox);
            if (hadronox)
                hadronox.GetAI().EnterEvadeMode(EvadeReason.Other);
        }

        public override uint GetData(uint data)
        {
            if (data == Data.CrusherPackId)
                return (uint)_myPack;
            return 0;
        }

        public override void SetData(uint data, uint value)
        {
            if (data == Data.CrusherPackId)
            {
                _myPack = (SummonGroups)value;
                me.SetReactState(_myPack != 0 ? ReactStates.Passive : ReactStates.Aggressive);
            }
        }

        public override void EnterCombat(Unit who)
        {
            if (me.HasReactState(ReactStates.Passive))
            {
                List<Creature> others = new List<Creature>();
                me.GetCreatureListWithEntryInGrid(others, 0, 40.0f);
                foreach (Creature other in others)
                {
                    if (other.GetAI().GetData(Data.CrusherPackId) == (uint)_myPack)
                    {
                        other.SetReactState(ReactStates.Aggressive);
                        other.GetAI().AttackStart(who);
                    }
                }
            }
            _EnterCombat();
            base.EnterCombat(who);
        }

        public virtual void _EnterCombat() { }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!me.HasReactState(ReactStates.Passive))
            {
                base.MoveInLineOfSight(who);
                return;
            }

            if (me.CanStartAttack(who, false) && me.IsWithinDistInMap(who, me.GetAttackDistance(who) + me.m_CombatDistance))
                EnterCombat(who);
        }

        public override void UpdateAI(uint diff)
        {
            if (_doFacing)
            {
                _doFacing = false;
                me.SetFacingTo(_positions[_myPack - SummonGroups.Crusher1].GetOrientation());
            }

            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }

        protected InstanceScript _instance;
        Position[] _positions;
        protected SummonGroups _myPack;
        bool _doFacing;
    }

    [Script]
    class npc_anub_ar_crusher : npc_hadronox_crusherPackAI
    {
        public npc_anub_ar_crusher(Creature creature) : base(creature, Misc.crusherWaypoints) { }

        public override void _EnterCombat()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Smash);
                task.Repeat(TimeSpan.FromSeconds(13), TimeSpan.FromSeconds(21));
            });

            if (_myPack != SummonGroups.Crusher1)
                return;

            Creature hadronox = _instance.GetCreature(ANDataTypes.Hadronox);
            if (hadronox)
            {
                if (hadronox.GetAI().GetData(Data.HadronoxEnteredCombat) != 0)
                    return;
                hadronox.GetAI().DoAction(ActionIds.CrusherEngaged);
            }

            Talk(TextIds.SayCrusherAggro);
        }

        public override void DamageTaken(Unit source, ref uint damage)
        {
            if (_hadFrenzy || !me.HealthBelowPctDamaged(25, damage))
                return;
            _hadFrenzy = true;
            Talk(TextIds.EmoteCrusherFrenzy);
            DoCastSelf(SpellIds.Frenzy);
        }

        public override void JustDied(Unit killer)
        {
            Creature hadronox = _instance.GetCreature(ANDataTypes.Hadronox);
            if (hadronox)
                hadronox.GetAI().DoAction(ActionIds.HadronoxMove);
            base.JustDied(killer);
        }

        bool _hadFrenzy;
    }

    [Script]
    class npc_anub_ar_crusher_champion : npc_hadronox_crusherPackAI
    {
        public npc_anub_ar_crusher_champion(Creature creature) : base(creature, Misc.championWaypoints) { }

        public override void _EnterCombat()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.Rend);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(16));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(19), task =>
            {
                DoCastVictim(SpellIds.Pummel);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(17));
            });
        }
    }

    [Script]
    class npc_anub_ar_crusher_crypt_fiend : npc_hadronox_crusherPackAI
    {
        public npc_anub_ar_crusher_crypt_fiend(Creature creature) : base(creature, Misc.cryptFiendWaypoints) { }

        public override void _EnterCombat()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.CrushingWebs);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(16));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(19), task =>
            {
                DoCastVictim(SpellIds.InfectedWound);
                task.Repeat(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(25));
            });
        }
    }

    [Script]
    class npc_anub_ar_crusher_necromancer : npc_hadronox_crusherPackAI
    {
        public npc_anub_ar_crusher_necromancer(Creature creature) : base(creature, Misc.necromancerWaypoints) { }

        public override void _EnterCombat()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), task =>
            {
                DoCastVictim(SpellIds.ShadowBolt);
                task.Repeat(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(37), TimeSpan.FromSeconds(45), task =>
            {
                DoCastVictim(RandomHelper.URand(0, 1) != 0 ? SpellIds.AnimateBones2 : SpellIds.AnimateBones1);
                task.Repeat(TimeSpan.FromSeconds(35), TimeSpan.FromSeconds(50));
            });
        }
    }

    class npc_hadronox_foeAI : ScriptedAI
    {
        public npc_hadronox_foeAI(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
            _nextMovement = MovementIds.Outside;
            _mySpawn = 0;
        }

        public override void InitializeAI()
        {
            base.InitializeAI();
            Creature hadronox = _instance.GetCreature(ANDataTypes.Hadronox);
            if (hadronox)
                hadronox.GetAI().SetGUID(me.GetGUID());
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type == MovementGeneratorType.Point)
                _nextMovement = (MovementIds)(id + 1);
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            me.DespawnOrUnsummon();
        }

        public override void UpdateAI(uint diff)
        {
            if (_nextMovement != 0)
            {
                switch (_nextMovement)
                {
                    case MovementIds.Outside:
                        {
                            float dist = float.PositiveInfinity;
                            for (byte spawn = 0; spawn < Misc.initialMoves.Length; ++spawn)
                            {
                                float thisDist = Misc.initialMoves[spawn].GetExactDistSq(me);
                                if (thisDist < dist)
                                {
                                    _mySpawn = spawn;
                                    dist = thisDist;
                                }
                            }
                            me.GetMotionMaster().MovePoint((uint)MovementIds.Outside, Misc.initialMoves[_mySpawn], false); // do not pathfind here, we have to pass through a "wall" of webbing
                            break;
                        }
                    case MovementIds.Downstairs:
                        me.GetMotionMaster().MovePoint((uint)MovementIds.Downstairs, Misc.downstairsMoves[_mySpawn]);
                        break;
                    case MovementIds.Downstairs2:
                        if (Misc.downstairsMoves2[_mySpawn].GetPositionX() > 0.0f) // might be unset for this spawn - if yes, skip
                        {
                            me.GetMotionMaster().MovePoint((uint)MovementIds.Downstairs2, Misc.downstairsMoves2[_mySpawn]);
                            break;
                        }
                        goto case MovementIds.Hadronox;
                    // intentional missing break
                    case MovementIds.Hadronox:
                    case MovementIds.HadronoxReal:
                        {
                            float zCutoff = 702.0f;
                            Creature hadronox = _instance.GetCreature(ANDataTypes.Hadronox);
                            if (hadronox && hadronox.IsAlive())
                            {
                                if (_nextMovement != MovementIds.HadronoxReal)
                                {
                                    if (hadronox.GetPositionZ() < zCutoff)
                                    {
                                        me.GetMotionMaster().MovePoint((uint)MovementIds.Hadronox, Misc.hadronoxStep[2]);
                                        break;
                                    }
                                }
                                AttackStart(hadronox);
                            }
                            break;
                        }
                    default:
                        break;
                }
                _nextMovement = MovementIds.None;
            }

            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }

        InstanceScript _instance;

        MovementIds _nextMovement;
        byte _mySpawn;
    }

    [Script]
    class npc_anub_ar_champion : npc_hadronox_foeAI
    {
        public npc_anub_ar_champion(Creature creature) : base(creature) { }

        public override void EnterCombat(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.Rend);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(16));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(19), task =>
            {
                DoCastVictim(SpellIds.Pummel);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(17));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(50), task =>
            {
                DoCastVictim(SpellIds.Taunt);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(50));
            });
        }
    }

    [Script]
    class npc_anub_ar_crypt_fiend : npc_hadronox_foeAI
    {
        public npc_anub_ar_crypt_fiend(Creature creature) : base(creature) { }

        public override void EnterCombat(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(SpellIds.CrushingWebs);
                task.Repeat(TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(16));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(19), task =>
            {
                DoCastVictim(SpellIds.InfectedWound);
                task.Repeat(TimeSpan.FromSeconds(16), TimeSpan.FromSeconds(25));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(50), task =>
            {
                DoCastVictim(SpellIds.Taunt);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(50));
            });
        }
    }

    [Script]
    class npc_anub_ar_necromancer : npc_hadronox_foeAI
    {
        public npc_anub_ar_necromancer(Creature creature) : base(creature) { }

        public override void EnterCombat(Unit who)
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4), task =>
            {
                DoCastVictim(SpellIds.ShadowBolt);
                task.Repeat(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(37), TimeSpan.FromSeconds(45), task =>
            {
                DoCastVictim(RandomHelper.URand(0, 1) != 0 ? SpellIds.AnimateBones2 : SpellIds.AnimateBones1);
                task.Repeat(TimeSpan.FromSeconds(35), TimeSpan.FromSeconds(50));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(50), task =>
            {
                DoCastVictim(SpellIds.Taunt);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(50));
            });
        }
    }

    [Script("spell_hadronox_periodic_summon_champion", SpellIds.SummonChampionTop, SpellIds.SummonChampionBottom)]
    [Script("spell_hadronox_periodic_summon_crypt_fiend", SpellIds.SummonCryptFiendTop, SpellIds.SummonCryptFiendBottom)]
    [Script("spell_hadronox_periodic_summon_necromancer", SpellIds.SummonNecromancerTop, SpellIds.SummonNecromancerBottom)]
    class spell_hadronox_periodic_summon_template : AuraScript
    {
        public spell_hadronox_periodic_summon_template(uint topSpellId, uint bottomSpellId)
        {
            _topSpellId = topSpellId;
            _bottomSpellId = bottomSpellId;
        }

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(_topSpellId, _bottomSpellId);
        }

        void HandleApply(AuraEffect eff, AuraEffectHandleModes mode)
        {
            AuraEffect effect = GetAura().GetEffect(0);
            if (effect != null)
                effect.SetPeriodicTimer(RandomHelper.IRand(2, 17) * Time.InMilliseconds);
        }

        void HandlePeriodic(AuraEffect eff)
        {
            Unit caster = GetCaster();
            if (!caster)
                return;
            InstanceScript instance = caster.GetInstanceScript();
            if (instance == null)
                return;
            if (instance.GetBossState(ANDataTypes.Hadronox) == EncounterState.Done)
                GetAura().Remove();
            else
            {
                if (caster.GetPositionZ() >= 750.0f)
                    caster.CastSpell(caster, _topSpellId, true);
                else
                    caster.CastSpell(caster, _bottomSpellId, true);
            }
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
        }

        uint _topSpellId;
        uint _bottomSpellId;
    }

    [Script]
    class spell_hadronox_leeching_poison : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.LeechPoisonHeal);
        }

        void HandleEffectRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Death)
                return;

            if (GetTarget().IsGuardian())
                return;

            Unit caster = GetCaster();
            if (caster)
                caster.CastSpell(caster, SpellIds.LeechPoisonHeal, true);
        }

        public override void Register()
        {
            OnEffectRemove.Add(new EffectApplyHandler(HandleEffectRemove, 0, AuraType.PeriodicLeech, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_hadronox_web_doors : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.SummonChampionPeriodic, SpellIds.SummonCryptFiendPeriodic, SpellIds.SummonNecromancerPeriodic);
        }

        void HandleDummy(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target)
            {
                target.RemoveAurasDueToSpell(SpellIds.SummonChampionPeriodic);
                target.RemoveAurasDueToSpell(SpellIds.SummonCryptFiendPeriodic);
                target.RemoveAurasDueToSpell(SpellIds.SummonNecromancerPeriodic);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.ApplyAura));
        }
    }

    [Script]
    class achievement_hadronox_denied : AchievementCriteriaScript
    {
        public achievement_hadronox_denied() : base("achievement_hadronox_denied") { }

        public override bool OnCheck(Player player, Unit target)
        {
            if (!target)
                return false;

            Creature cTarget = target.ToCreature();
            if (cTarget)
                if (cTarget.GetAI().GetData(Data.HadronoxWebbedDoors) == 0)
                    return true;

            return false;
        }
    }
}