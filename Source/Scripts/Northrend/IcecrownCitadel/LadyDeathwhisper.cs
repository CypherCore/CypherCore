/*
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
using Framework.Dynamic;
using Game;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Scripts.Northrend.IcecrownCitadel
{
    struct TextIds
    {
        // Lady Deathwhisper
        public const uint SayIntro1 = 0;
        public const uint SayIntro2 = 1;
        public const uint SayIntro3 = 2;
        public const uint SayIntro4 = 3;
        public const uint SayIntro5 = 4;
        public const uint SayIntro6 = 5;
        public const uint SayIntro7 = 6;
        public const uint SayAggro = 7;
        public const uint SayPhase2 = 8;
        public const uint EmotePhase2 = 9;
        public const uint SayDominateMind = 10;
        public const uint SayDarkEmpowerment = 11;
        public const uint SayDarkTransformation = 12;
        public const uint SayAnimateDead = 13;
        public const uint SayKill = 14;
        public const uint SayBerserk = 15;
        public const uint SayDeath = 16;

        // Darnavan
        public const uint SayDarnavanAggro = 0;
        public const uint SayDarnavanRescued = 1;
    }

    struct SpellIds
    {
        public const uint Berserk = 26662;

        // Lady Deathwhisper
        public const uint ManaBarrier = 70842;
        public const uint ShadowBolt = 71254;
        public const uint DeathAndDecay = 71001;
        public const uint DominateMind = 71289;
        public const uint DominateMindScale = 71290;
        public const uint Frostbolt = 71420;
        public const uint FrostboltVolley = 72905;
        public const uint TouchOfInsignificance = 71204;
        public const uint SummonShade = 71363;
        public const uint ShadowChanneling = 43897;
        public const uint DarkTransformationT = 70895;
        public const uint DarkEmpowermentT = 70896;
        public const uint DarkMartyrdomT = 70897;
        public const uint SummonSpirits = 72478;

        // Achievement
        public const uint FullHouse = 72827; // Does Not Exist In Dbc But Still Can Be Used For Criteria Check

        // Both Adds
        public const uint TeleportVisual = 41236;
        public const uint ClearAllDebuffs = 34098;
        public const uint FullHeal = 17683;
        public const uint PermanentFeighDeath = 70628;

        // Fanatics
        public const uint DarkTransformation = 70900;
        public const uint NecroticStrike = 70659;
        public const uint ShadowCleave = 70670;
        public const uint VampiricMight = 70674;
        public const uint FanaticSDetermination = 71235;
        public const uint DarkMartyrdomFanatic = 71236;

        //  Adherents
        public const uint DarkEmpowerment = 70901;
        public const uint FrostFever = 67767;
        public const uint DeathchillBolt = 70594;
        public const uint DeathchillBlast = 70906;
        public const uint CurseOfTorpor = 71237;
        public const uint ShorudOfTheOccult = 70768;
        public const uint AdherentSDetermination = 71234;
        public const uint DarkMartyrdomAdherent = 70903;

        // Vengeful Shade
        public const uint VengefulBlast = 71544;
        public const uint VengefulBlastPassive = 71494;
        public const uint VengefulBlast25N = 72010;
        public const uint VengefulBlast10H = 72011;
        public const uint VengefulBlast25H = 72012;

        // Darnavan
        public const uint Bladestorm = 65947;
        public const uint Charge = 65927;
        public const uint IntimidatingShout = 65930;
        public const uint MortalStrike = 65926;
        public const uint ShatteringThrow = 65940;
        public const uint SunderArmor = 65936;
    }

    struct LadyEventTypes
    {
        // Darnavan
        public const uint DARNAVAN_BLADESTORM = 27;
        public const uint DARNAVAN_CHARGE = 28;
        public const uint DARNAVAN_INTIMIDATING_SHOUT = 29;
        public const uint DARNAVAN_MORTAL_STRIKE = 30;
        public const uint DARNAVAN_SHATTERING_THROW = 31;
        public const uint DARNAVAN_SUNDER_ARMOR = 32;
    }

    enum Phases
    {
        All,
        Intro,
        One,
        Two
    }

    struct GroupIds
    {
        public const uint Intro = 0;
        public const uint One = 1;
        public const uint Two = 2;
    }

    struct DeprogrammingData
    {
        public const uint NpcDarnavan10 = 38472;
        public const uint NpcDarnavan25 = 38485;
        public const uint NpcDarnavanCredit10 = 39091;
        public const uint NpcDarnavanCredit25 = 39092;

        public const int ACTION_COMPLETE_QUEST = -384720;
        public const uint POINT_DESPAWN = 384721;
    }

    struct LadyConst
    {
        public const uint GUIDCultist = 1;

        public static uint[] SummonEntries = { CreatureIds.CultFanatic, CreatureIds.CultAdherent };

        public static Position[] SummonPositions =
        {
            new Position(-578.7066f, 2154.167f, 51.01529f, 1.692969f), // 1 Left Door 1 (Cult Fanatic)
            new Position(-598.9028f, 2155.005f, 51.01530f, 1.692969f), // 2 Left Door 2 (Cult Adherent)
            new Position(-619.2864f, 2154.460f, 51.01530f, 1.692969f), // 3 Left Door 3 (Cult Fanatic)
            new Position(-578.6996f, 2269.856f, 51.01529f, 4.590216f), // 4 Right Door 1 (Cult Adherent)
            new Position(-598.9688f, 2269.264f, 51.01529f, 4.590216f), // 5 Right Door 2 (Cult Fanatic)
            new Position(-619.4323f, 2268.523f, 51.01530f, 4.590216f), // 6 Right Door 3 (Cult Adherent)
            new Position(-524.2480f, 2211.920f, 62.90960f, 3.141592f), // 7 Upper (Random Cultist)
        };
    }

    class DaranavanMoveEvent : BasicEvent
    {
        public DaranavanMoveEvent(Creature darnavan)
        {
            _darnavan = darnavan;
        }

        public override bool Execute(ulong time, uint diff)
        {
            _darnavan.GetMotionMaster().MovePoint(DeprogrammingData.POINT_DESPAWN, LadyConst.SummonPositions[6]);
            return true;
        }

        Creature _darnavan;
    }

    [Script]
    public class boss_lady_deathwhisper : BossAI
    {
        public boss_lady_deathwhisper(Creature creature) : base(creature, Bosses.LadyDeathwhisper)
        {
            _dominateMindCount = RaidMode<byte>(0, 1, 1, 3);
            _introDone = false;
            Initialize();
        }

        void Initialize()
        {
            _waveCounter = 0;
            _nextVengefulShadeTargetGUID.Clear();
            _cultistQueue.Clear();
            _darnavanGUID.Clear();
            _phase = Phases.All;
            _scheduler.SetValidator(() =>
            {
                return !(me.HasUnitState(UnitState.Casting) && _phase != Phases.Intro);
            });
        }

        public override void Reset()
        {
            _Reset();
            Initialize();
            _phase = Phases.One;
            DoCast(me, SpellIds.ShadowChanneling);
            me.SetFullPower(PowerType.Mana);
            me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModTaunt, false);
            me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.AttackMe, false);
        }

        public override void DoAction(int action)
        {
            if (action != 0)
                return;

            if (!_introDone)
            {
                _introDone = true;
                Talk(TextIds.SayIntro1);
                _phase = Phases.Intro;
                _scheduler.Schedule(TimeSpan.FromSeconds(10), GroupIds.Intro, task =>
                {
                    switch (task.GetRepeatCounter())
                    {
                        case 0:
                            Talk(TextIds.SayIntro2);
                            task.Repeat(TimeSpan.FromSeconds(21));
                            break;
                        case 1:
                            Talk(TextIds.SayIntro3);
                            task.Repeat(TimeSpan.FromSeconds(11));
                            break;
                        case 2:
                            Talk(TextIds.SayIntro4);
                            task.Repeat(TimeSpan.FromSeconds(9));
                            break;
                        case 3:
                            Talk(TextIds.SayIntro5);
                            task.Repeat(TimeSpan.FromSeconds(21));
                            break;
                        case 4:
                            Talk(TextIds.SayIntro6);
                            task.Repeat(TimeSpan.FromSeconds(10));
                            break;
                        case 5:
                            Talk(TextIds.SayIntro7);
                            return;
                        default:
                            break;
                    }
                });
            }
        }

        public override void AttackStart(Unit victim)
        {
            if (me.HasUnitFlag(UnitFlags.NonAttackable))
                return;

            if (victim && me.Attack(victim, true) && _phase != Phases.One)
                me.GetMotionMaster().MoveChase(victim);
        }

        public override void EnterCombat(Unit who)
        {
            if (!instance.CheckRequiredBosses(Bosses.LadyDeathwhisper, who.ToPlayer()))
            {
                EnterEvadeMode(EvadeReason.SequenceBreak);
                instance.DoCastSpellOnPlayers(TeleporterSpells.LIGHT_S_HAMMER_TELEPORT);
                return;
            }

            me.SetCombatPulseDelay(5);
            me.SetActive(true);
            DoZoneInCombat();
            _phase = Phases.One;
            _scheduler.CancelGroup(GroupIds.Intro);

            // phase-independent events
            _scheduler.Schedule(TimeSpan.FromMinutes(10), context =>
            {
                DoCastSelf(SpellIds.Berserk);
                Talk(TextIds.SayBerserk);
            }).Schedule(TimeSpan.FromSeconds(17), death_and_decay =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random);
                if (target)
                    DoCast(target, SpellIds.DeathAndDecay);
                death_and_decay.Repeat(TimeSpan.FromSeconds(22), TimeSpan.FromSeconds(30));
            });

            if (GetDifficulty() != Difficulty.Raid10N)
            {
                _scheduler.Schedule(TimeSpan.FromSeconds(27), dominate_mind =>
                {
                    Talk(TextIds.SayDominateMind);
                    for (byte i = 0; i < _dominateMindCount; i++)
                    {
                        Unit target = SelectTarget(SelectAggroTarget.Random, 1, 0.0f, true, -(int)SpellIds.DominateMind);
                        if (target != null)
                            DoCast(target, SpellIds.DominateMind);
                    }
                    dominate_mind.Repeat(TimeSpan.FromSeconds(40), TimeSpan.FromSeconds(45));
                });
            }

            // phase one only
            _scheduler.Schedule(TimeSpan.FromSeconds(5), GroupIds.One, wave =>
            {
                SummonWaveP1();
                wave.Repeat(TimeSpan.FromSeconds(IsHeroic() ? 45 : 60));
            }).Schedule(TimeSpan.FromSeconds(2), GroupIds.One, shadow_bolt =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random);
                if (target != null)
                    DoCast(target, SpellIds.ShadowBolt);
                shadow_bolt.Repeat(TimeSpan.FromMilliseconds(2450), TimeSpan.FromMilliseconds(3600));
            }).Schedule(TimeSpan.FromSeconds(15), GroupIds.One, context =>
            {
                DoImproveCultist();
                context.Repeat(TimeSpan.FromSeconds(25));
            });


            Talk(TextIds.SayAggro);
            DoStartNoMovement(who);
            me.RemoveAurasDueToSpell(SpellIds.ShadowChanneling);
            DoCast(me, SpellIds.ManaBarrier, true);
            instance.SetBossState(Bosses.LadyDeathwhisper, EncounterState.InProgress);
        }

        public override void JustDied(Unit killer)
        {
            Talk(TextIds.SayDeath);

            List<uint> livingAddEntries = new List<uint>();
            // Full House achievement
            foreach (var guid in summons)
            {
                Unit unit = Global.ObjAccessor.GetUnit(me, guid);
                if (unit)
                    if (unit.IsAlive() && unit.GetEntry() != CreatureIds.VengefulShade)
                        livingAddEntries.Add(unit.GetEntry());
            }

            if (livingAddEntries.Count >= 5)
                instance.DoUpdateCriteria(CriteriaTypes.BeSpellTarget, SpellIds.FullHouse, 0, me);

            Creature darnavan = ObjectAccessor.GetCreature(me, _darnavanGUID);
            if (darnavan)
            {
                if (darnavan.IsAlive())
                {
                    darnavan.SetFaction(35);
                    darnavan.CombatStop(true);
                    darnavan.GetMotionMaster().MoveIdle();
                    darnavan.SetReactState(ReactStates.Passive);
                    darnavan.m_Events.AddEvent(new DaranavanMoveEvent(darnavan), darnavan.m_Events.CalculateTime(10000));
                    darnavan.GetAI().Talk(TextIds.SayDarnavanRescued);
                    Player owner = killer.GetCharmerOrOwnerPlayerOrPlayerItself();
                    if (owner)
                    {
                        Group group = owner.GetGroup();
                        if (group)
                        {
                            for (GroupReference groupRefe = group.GetFirstMember(); groupRefe != null; groupRefe = groupRefe.Next())
                            {
                                Player member = groupRefe.GetSource();
                                if (member)
                                    member.KilledMonsterCredit(NPC_DARNAVAN_CREDIT, ObjectGuid.Empty);
                            }
                        }
                        else
                            owner.KilledMonsterCredit(NPC_DARNAVAN_CREDIT, ObjectGuid.Empty);
                    }
                }
            }

            _JustDied();
        }

        public override void EnterEvadeMode(EvadeReason why = EvadeReason.Other)
        {
            _scheduler.CancelAll();
            summons.DespawnAll();

            Creature darnavan = ObjectAccessor.GetCreature(me, _darnavanGUID);
            if (darnavan != null)
                darnavan.DespawnOrUnsummon();

            _DespawnAtEvade();
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsTypeId(TypeId.Player))
                Talk(TextIds.SayKill);
        }

        public override void DamageTaken(Unit damageDealer, ref uint damage)
        {
            // phase transition
            if (_phase == Phases.One && damage > (uint)me.GetPower(PowerType.Mana))
            {
                _phase = Phases.Two;
                Talk(TextIds.SayPhase2);
                Talk(TextIds.EmotePhase2);
                DoStartMovement(me.GetVictim());
                DoResetThreat();

                damage -= (uint)me.GetPower(PowerType.Mana);
                me.SetPower(PowerType.Mana, 0);
                me.RemoveAurasDueToSpell(SpellIds.ManaBarrier);
                _scheduler.CancelGroup(GroupIds.One);

                _scheduler.Schedule(TimeSpan.FromSeconds(12), GroupIds.Two, frostbolt =>
                {
                    DoCastVictim(SpellIds.Frostbolt);
                    frostbolt.Repeat();
                }).Schedule(TimeSpan.FromSeconds(20), GroupIds.Two, frostboldVolley =>
                {
                    DoCastAOE(SpellIds.FrostboltVolley);
                    frostboldVolley.Repeat();
                }).Schedule(TimeSpan.FromSeconds(6), TimeSpan.FromSeconds(9), GroupIds.Two, touch =>
                {
                    if (me.GetVictim())
                        me.AddAura(SpellIds.TouchOfInsignificance, me.GetVictim());
                    touch.Repeat();
                }).Schedule(TimeSpan.FromSeconds(12), GroupIds.Two, summonShade =>
                {
                    me.CastCustomSpell(SpellIds.SummonSpirits, SpellValueMod.MaxTargets, Is25ManRaid() ? 2 : 1);
                    summonShade.Repeat();
                });

                // on heroic mode Lady Deathwhisper is immune to taunt effects in phase 2 and continues summoning adds
                if (IsHeroic())
                {
                    me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModTaunt, true);
                    me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.AttackMe, true);
                    _scheduler.Schedule(TimeSpan.FromSeconds(0), GroupIds.Two, context =>
                    {
                        SummonWaveP2();
                        context.Repeat(TimeSpan.FromSeconds(45));
                    });
                }
            }
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            if (spell.Id == SpellIds.SummonSpirits)
                _nextVengefulShadeTargetGUID.Add(target.GetGUID());
        }

        public override void JustSummoned(Creature summon)
        {
            switch (summon.GetEntry())
            {
                case DeprogrammingData.NpcDarnavan10:
                case DeprogrammingData.NpcDarnavan25:
                    _darnavanGUID = summon.GetGUID();
                    summon.GetAI().AttackStart(SelectTarget(SelectAggroTarget.Random));
                    return;
                case CreatureIds.VengefulShade:
                    if (_nextVengefulShadeTargetGUID.Empty())
                        break;
                    summon.GetAI().SetGUID(_nextVengefulShadeTargetGUID.First());
                    _nextVengefulShadeTargetGUID.RemoveAt(0);
                    break;
                case CreatureIds.CultAdherent:
                case CreatureIds.CultFanatic:
                    _cultistQueue.Add(summon.GetGUID());
                    summon.GetAI().AttackStart(SelectTarget(SelectAggroTarget.Random));
                    break;
                default:
                    break;
            }
            summons.Summon(summon);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && _phase != Phases.Intro)
                return;

            _scheduler.Update(diff, () =>
            {
                // We should not melee attack when barrier is up
                if (!me.HasAura(SpellIds.ManaBarrier))
                    DoMeleeAttackIfReady();
            });
        }

        // summoning function for first phase
        void SummonWaveP1()
        {
            byte addIndex = (byte)(_waveCounter & 1);
            byte addIndexOther = (byte)(addIndex ^ 1);

            // Summon first add, replace it with Darnavan if weekly quest is active
            if (_waveCounter != 0 || !Global.PoolMgr.IsSpawnedObject<Quest>(QUEST_DEPROGRAMMING))
                Summon(LadyConst.SummonEntries[addIndex], LadyConst.SummonPositions[addIndex * 3]);
            else
                Summon(NPC_DARNAVAN, LadyConst.SummonPositions[addIndex * 3]);

            Summon(LadyConst.SummonEntries[addIndexOther], LadyConst.SummonPositions[addIndex * 3 + 1]);
            Summon(LadyConst.SummonEntries[addIndex], LadyConst.SummonPositions[addIndex * 3 + 2]);
            if (Is25ManRaid())
            {
                Summon(LadyConst.SummonEntries[addIndexOther], LadyConst.SummonPositions[addIndexOther * 3]);
                Summon(LadyConst.SummonEntries[addIndex], LadyConst.SummonPositions[addIndexOther * 3 + 1]);
                Summon(LadyConst.SummonEntries[addIndexOther], LadyConst.SummonPositions[addIndexOther * 3 + 2]);
                Summon(LadyConst.SummonEntries[RandomHelper.IRand(0, 1)], LadyConst.SummonPositions[6]);
            }

            ++_waveCounter;
        }

        // summoning function for second phase
        void SummonWaveP2()
        {
            if (Is25ManRaid())
            {
                byte addIndex = (byte)(_waveCounter & 1);
                Summon(LadyConst.SummonEntries[addIndex], LadyConst.SummonPositions[addIndex * 3]);
                Summon(LadyConst.SummonEntries[addIndex ^ 1], LadyConst.SummonPositions[addIndex * 3 + 1]);
                Summon(LadyConst.SummonEntries[addIndex], LadyConst.SummonPositions[addIndex * 3 + 2]);
            }
            else
                Summon(LadyConst.SummonEntries[RandomHelper.IRand(0, 1)], LadyConst.SummonPositions[6]);

            ++_waveCounter;
        }

        // helper for summoning wave mobs
        void Summon(uint entry, Position pos)
        {
            TempSummon summon = me.SummonCreature(entry, pos, TempSummonType.CorpseTimedDespawn, 10000);
            if (summon)
                summon.CastSpell(summon, SpellIds.TeleportVisual);
        }

        public override void SummonedCreatureDies(Creature summon, Unit killer)
        {
            if (summon.GetEntry() == CreatureIds.CultAdherent || summon.GetEntry() == CreatureIds.CultFanatic)
                _cultistQueue.Remove(summon.GetGUID());
        }

        void DoImproveCultist()
        {
            if (_cultistQueue.Empty())
                return;

            _cultistGUID = _cultistQueue.SelectRandom();
            _cultistQueue.Remove(_cultistGUID);
            Creature cultist = ObjectAccessor.GetCreature(me, _cultistGUID);
            if (!cultist)
                return;

            if (RandomHelper.RAND(0, 1) != 0)
                me.CastSpell(cultist, SpellIds.DarkMartyrdomT);
            else
            {
                me.CastSpell(cultist, cultist.GetEntry() == CreatureIds.CultFanatic ? SpellIds.DarkTransformationT : SpellIds.DarkEmpowermentT, true);
                Talk(cultist.GetEntry() == CreatureIds.CultFanatic ? TextIds.SayDarkTransformation : TextIds.SayDarkEmpowerment);
            }
        }

        ObjectGuid _darnavanGUID;
        ObjectGuid _cultistGUID;
        List<ObjectGuid> _cultistQueue = new List<ObjectGuid>();
        List<ObjectGuid> _nextVengefulShadeTargetGUID = new List<ObjectGuid>();

        uint _waveCounter;
        byte _dominateMindCount;
        Phases _phase;
        bool _introDone;

        uint NPC_DARNAVAN { get { return RaidMode(DeprogrammingData.NpcDarnavan10, DeprogrammingData.NpcDarnavan25, DeprogrammingData.NpcDarnavan10, DeprogrammingData.NpcDarnavan25); } }
        uint NPC_DARNAVAN_CREDIT { get { return RaidMode(DeprogrammingData.NpcDarnavanCredit10, DeprogrammingData.NpcDarnavanCredit25, DeprogrammingData.NpcDarnavanCredit10, DeprogrammingData.NpcDarnavanCredit25); } }
        uint QUEST_DEPROGRAMMING { get { return RaidMode(WeeklyQuestIds.Deprogramming10, WeeklyQuestIds.Deprogramming25, WeeklyQuestIds.Deprogramming10, WeeklyQuestIds.Deprogramming25); } }
    }

    [Script]
    class npc_cult_fanatic : ScriptedAI
    {
        public npc_cult_fanatic(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.SetValidator(() =>
            {
                return !me.HasUnitState(UnitState.Casting);
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(17), vampiric_might =>
            {
                DoCastSelf(SpellIds.VampiricMight);
                vampiric_might.Repeat(TimeSpan.FromSeconds(25));
            }).Schedule(TimeSpan.FromSeconds(12), shadow_cleave =>
            {
                DoCastVictim(SpellIds.ShadowCleave);
                shadow_cleave.Repeat(TimeSpan.FromSeconds(14));
            }).Schedule(TimeSpan.FromSeconds(10), necrotic_strike =>
            {
                DoCastVictim(SpellIds.NecroticStrike);
                necrotic_strike.Repeat(TimeSpan.FromSeconds(17));
            });
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            switch (spell.Id)
            {
                case SpellIds.DarkTransformationT:
                    me.InterruptNonMeleeSpells(true);
                    DoCastSelf(SpellIds.DarkTransformation);
                    break;
                case SpellIds.DarkTransformation:
                    DoCastSelf(SpellIds.FullHeal);
                    me.UpdateEntry(CreatureIds.DeformedFanatic);
                    break;
                case SpellIds.DarkMartyrdomT:
                    me.SetReactState(ReactStates.Passive);
                    me.InterruptNonMeleeSpells(true);
                    me.AttackStop();
                    DoCastSelf(SpellIds.DarkMartyrdomFanatic);
                    break;
                case SpellIds.DarkMartyrdomFanatic:
                    _scheduler.Schedule(TimeSpan.FromSeconds(2), context =>
                    {
                        me.UpdateEntry(CreatureIds.ReanimatedFanatic);
                        DoCastSelf(SpellIds.PermanentFeighDeath);
                        DoCastSelf(SpellIds.ClearAllDebuffs);
                        DoCastSelf(SpellIds.FullHeal, true);
                        me.AddUnitFlag(UnitFlags.Stunned | UnitFlags.Unk29 | UnitFlags.NotSelectable);
                    }).Schedule(TimeSpan.FromSeconds(6), context =>
                    {
                        me.RemoveAurasDueToSpell(SpellIds.PermanentFeighDeath);
                        me.RemoveUnitFlag(UnitFlags.Stunned | UnitFlags.Unk29 | UnitFlags.NotSelectable);
                        me.SetReactState(ReactStates.Aggressive);
                        DoZoneInCombat(me);

                        Creature ladyDeathwhisper = ObjectAccessor.GetCreature(me, _instance.GetGuidData(Bosses.LadyDeathwhisper));
                        if (ladyDeathwhisper != null)
                            ladyDeathwhisper.GetAI().Talk(TextIds.SayAnimateDead);
                    });
                    break;
                default:
                    break;

            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && !me.HasAura(SpellIds.PermanentFeighDeath))
                return;

            _scheduler.Update(diff, () =>
            {
                DoMeleeAttackIfReady();
            });
        }

        InstanceScript _instance;
    }

    [Script]
    class npc_cult_adherent : ScriptedAI
    {
        public npc_cult_adherent(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            _scheduler.CancelAll();
            _scheduler.SetValidator(() =>
            {
                return !me.HasUnitState(UnitState.Casting);
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(5), deathchill =>
            {
                if (me.GetEntry() == CreatureIds.EmpoweredAdherent)
                    DoCastVictim(SpellIds.DeathchillBlast);
                else
                    DoCastVictim(SpellIds.DeathchillBolt);
                deathchill.Repeat(TimeSpan.FromMilliseconds(2500));
            }).Schedule(TimeSpan.FromSeconds(15), shroud_of_the_occult =>
            {
                DoCastSelf(SpellIds.ShorudOfTheOccult);
                shroud_of_the_occult.Repeat(TimeSpan.FromSeconds(10));
            }).Schedule(TimeSpan.FromSeconds(15), curse_of_torpor =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 1);
                if (target != null)
                    DoCast(target, SpellIds.CurseOfTorpor);
                curse_of_torpor.Repeat(TimeSpan.FromSeconds(18));
            });
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            switch (spell.Id)
            {
                case SpellIds.DarkEmpowermentT:
                    me.UpdateEntry(CreatureIds.EmpoweredAdherent);
                    break;
                case SpellIds.DarkMartyrdomT:
                    me.SetReactState(ReactStates.Passive);
                    me.InterruptNonMeleeSpells(true);
                    me.AttackStop();
                    DoCastSelf(SpellIds.DarkMartyrdomAdherent);
                    break;
                case SpellIds.DarkMartyrdomAdherent:
                    _scheduler.Schedule(TimeSpan.FromSeconds(2), context =>
                    {
                        me.UpdateEntry(CreatureIds.ReanimatedAdherent);
                        DoCastSelf(SpellIds.PermanentFeighDeath);
                        DoCastSelf(SpellIds.ClearAllDebuffs);
                        DoCastSelf(SpellIds.FullHeal, true);
                        me.AddUnitFlag(UnitFlags.Stunned | UnitFlags.Unk29 | UnitFlags.NotSelectable);
                    }).Schedule(TimeSpan.FromSeconds(6), context =>
                    {
                        me.RemoveAurasDueToSpell(SpellIds.PermanentFeighDeath);
                        me.RemoveUnitFlag(UnitFlags.Stunned | UnitFlags.Unk29 | UnitFlags.NotSelectable);
                        me.SetReactState(ReactStates.Aggressive);
                        DoCastSelf(SpellIds.ShorudOfTheOccult);
                        DoZoneInCombat(me);

                        Creature ladyDeathwhisper = ObjectAccessor.GetCreature(me, _instance.GetGuidData(Bosses.LadyDeathwhisper));
                        if (ladyDeathwhisper != null)
                            ladyDeathwhisper.GetAI().Talk(TextIds.SayAnimateDead);
                    });
                    break;
                default:
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && !me.HasAura(SpellIds.PermanentFeighDeath))
                return;

            _scheduler.Update(diff);
        }

        InstanceScript _instance;
    }

    [Script]
    class npc_vengeful_shade : ScriptedAI
    {
        public npc_vengeful_shade(Creature creature) : base(creature) { }

        public override void Reset()
        {
            me.SetReactState(ReactStates.Passive);
            me.AddAura(SpellIds.VengefulBlastPassive, me);

            _scheduler.Schedule(TimeSpan.FromSeconds(2), context =>
            {
                me.SetReactState(ReactStates.Aggressive);
                me.GetAI().AttackStart(Global.ObjAccessor.GetUnit(me, _targetGUID));
            }).Schedule(TimeSpan.FromSeconds(7), context =>
            {
                me.KillSelf();
            });
        }

        public override void SetGUID(ObjectGuid guid, int id = 0)
        {
            _targetGUID = guid;
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            switch (spell.Id)
            {
                case SpellIds.VengefulBlast:
                case SpellIds.VengefulBlast25N:
                case SpellIds.VengefulBlast10H:
                case SpellIds.VengefulBlast25H:
                    me.KillSelf();
                    break;
                default:
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff, () =>
            {
                DoMeleeAttackIfReady();
            });
        }

        ObjectGuid _targetGUID;
    }

    [Script]
    class npc_darnavan : ScriptedAI
    {
        public npc_darnavan(Creature creature) : base(creature)
        {
            Initialize();
        }

        void Initialize()
        {
            _canCharge = true;
            _canShatter = true;
        }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(LadyEventTypes.DARNAVAN_BLADESTORM, 10000);
            _events.ScheduleEvent(LadyEventTypes.DARNAVAN_INTIMIDATING_SHOUT, RandomHelper.URand(20000, 25000));
            _events.ScheduleEvent(LadyEventTypes.DARNAVAN_MORTAL_STRIKE, RandomHelper.URand(25000, 30000));
            _events.ScheduleEvent(LadyEventTypes.DARNAVAN_SUNDER_ARMOR, RandomHelper.URand(5000, 8000));
            Initialize();
        }

        public override void JustDied(Unit killer)
        {
            _events.Reset();
            Player owner = killer.GetCharmerOrOwnerPlayerOrPlayerItself();
            if (owner)
            {
                Group group = owner.GetGroup();
                if (group)
                {
                    for (GroupReference groupRefe = group.GetFirstMember(); groupRefe != null; groupRefe = groupRefe.Next())
                    {
                        Player member = groupRefe.GetSource();
                        if (member)
                            member.FailQuest(QUEST_DEPROGRAMMING);
                    }
                }
                else
                    owner.FailQuest(QUEST_DEPROGRAMMING);
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Point || id != DeprogrammingData.POINT_DESPAWN)
                return;

            me.DespawnOrUnsummon();
        }

        public override void EnterCombat(Unit victim)
        {
            Talk(TextIds.SayDarnavanAggro);
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            if (_canShatter && me.GetVictim() && me.GetVictim().IsImmunedToDamage(SpellSchoolMask.Normal))
            {
                DoCastVictim(SpellIds.ShatteringThrow);
                _canShatter = false;
                _events.ScheduleEvent(LadyEventTypes.DARNAVAN_SHATTERING_THROW, 30000);
                return;
            }

            if (_canCharge && !me.IsWithinMeleeRange(me.GetVictim()))
            {
                DoCastVictim(SpellIds.Charge);
                _canCharge = false;
                _events.ScheduleEvent(LadyEventTypes.DARNAVAN_CHARGE, 20000);
                return;
            }

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case LadyEventTypes.DARNAVAN_BLADESTORM:
                        DoCast(SpellIds.Bladestorm);
                        _events.ScheduleEvent(LadyEventTypes.DARNAVAN_BLADESTORM, RandomHelper.URand(90000, 100000));
                        break;
                    case LadyEventTypes.DARNAVAN_CHARGE:
                        _canCharge = true;
                        break;
                    case LadyEventTypes.DARNAVAN_INTIMIDATING_SHOUT:
                        DoCast(SpellIds.IntimidatingShout);
                        _events.ScheduleEvent(LadyEventTypes.DARNAVAN_INTIMIDATING_SHOUT, RandomHelper.URand(90000, 120000));
                        break;
                    case LadyEventTypes.DARNAVAN_MORTAL_STRIKE:
                        DoCastVictim(SpellIds.MortalStrike);
                        _events.ScheduleEvent(LadyEventTypes.DARNAVAN_MORTAL_STRIKE, RandomHelper.URand(15000, 30000));
                        break;
                    case LadyEventTypes.DARNAVAN_SHATTERING_THROW:
                        _canShatter = true;
                        break;
                    case LadyEventTypes.DARNAVAN_SUNDER_ARMOR:
                        DoCastVictim(SpellIds.SunderArmor);
                        _events.ScheduleEvent(LadyEventTypes.DARNAVAN_SUNDER_ARMOR, RandomHelper.URand(3000, 7000));
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }

        uint QUEST_DEPROGRAMMING { get { return RaidMode<uint>(WeeklyQuestIds.Deprogramming10, WeeklyQuestIds.Deprogramming25, WeeklyQuestIds.Deprogramming10, WeeklyQuestIds.Deprogramming25); } }

        bool _canCharge;
        bool _canShatter;
    }

    [Script]
    class spell_deathwhisper_mana_barrier : AuraScript
    {
        void HandlePeriodicTick(AuraEffect aurEff)
        {
            PreventDefaultAction();
            Unit caster = GetCaster();
            if (caster)
            {
                int missingHealth = (int)(caster.GetMaxHealth() - caster.GetHealth());
                caster.ModifyHealth(missingHealth);
                caster.ModifyPower(PowerType.Mana, -missingHealth);
            }
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(HandlePeriodicTick, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script]
    class at_lady_deathwhisper_entrance : AreaTriggerScript
    {
        public at_lady_deathwhisper_entrance() : base("at_lady_deathwhisper_entrance") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            InstanceScript instance = player.GetInstanceScript();
            if (instance != null)
            {
                if (instance.GetBossState(Bosses.LadyDeathwhisper) != EncounterState.Done)
                {
                    Creature ladyDeathwhisper = ObjectAccessor.GetCreature(player, instance.GetGuidData(Bosses.LadyDeathwhisper));
                    if (ladyDeathwhisper)
                        ladyDeathwhisper.GetAI().DoAction(0);
                }
            }

            return true;
        }
    }

    [Script]
    class spell_deathwhisper_dominated_mind : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DominateMindScale);
        }

        void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Unit target = GetTarget();
            target.CastSpell(target, SpellIds.DominateMindScale, true);
        }

        public override void Register()
        {
            AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.AoeCharm, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_deathwhisper_summon_spirits : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SummonShade);
        }

        void HandleScriptEffect(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), SpellIds.SummonShade, true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.Dummy));
        }
    }
}
