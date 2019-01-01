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

namespace Scripts.Northrend.IcecrownCitadel
{
    struct LadyTexts
    {
        // Lady Deathwhisper
        public const uint SAY_INTRO_1 = 0;
        public const uint SAY_INTRO_2 = 1;
        public const uint SAY_INTRO_3 = 2;
        public const uint SAY_INTRO_4 = 3;
        public const uint SAY_INTRO_5 = 4;
        public const uint SAY_INTRO_6 = 5;
        public const uint SAY_INTRO_7 = 6;
        public const uint SAY_AGGRO = 7;
        public const uint SAY_PHASE_2 = 8;
        public const uint EMOTE_PHASE_2 = 9;
        public const uint SAY_DOMINATE_MIND = 10;
        public const uint SAY_DARK_EMPOWERMENT = 11;
        public const uint SAY_DARK_TRANSFORMATION = 12;
        public const uint SAY_ANIMATE_DEAD = 13;
        public const uint SAY_KILL = 14;
        public const uint SAY_BERSERK = 15;
        public const uint SAY_DEATH = 16;

        // Darnavan
        public const uint SAY_DARNAVAN_AGGRO = 0;
        public const uint SAY_DARNAVAN_RESCUED = 1;
    }

    struct LadySpells
    {
        // Lady Deathwhisper
        public const uint MANA_BARRIER = 70842;
        public const uint SHADOW_BOLT = 71254;
        public const uint DEATH_AND_DECAY = 71001;
        public const uint DOMINATE_MIND_H = 71289;
        public const uint FROSTBOLT = 71420;
        public const uint FROSTBOLT_VOLLEY = 72905;
        public const uint TOUCH_OF_INSIGNIFICANCE = 71204;
        public const uint SUMMON_SHADE = 71363;
        public const uint SHADOW_CHANNELING = 43897; // Prefight; during intro
        public const uint DARK_TRANSFORMATION_T = 70895;
        public const uint DARK_EMPOWERMENT_T = 70896;
        public const uint DARK_MARTYRDOM_T = 70897;

        // Achievement
        public const uint FULL_HOUSE = 72827; // does not exist in dbc but still can be used for criteria check

        // Both Adds
        public const uint TELEPORT_VISUAL = 41236;

        // Fanatics
        public const uint DARK_TRANSFORMATION = 70900;
        public const uint NECROTIC_STRIKE = 70659;
        public const uint SHADOW_CLEAVE = 70670;
        public const uint VAMPIRIC_MIGHT = 70674;
        public const uint FANATIC_S_DETERMINATION = 71235;
        public const uint DARK_MARTYRDOM_FANATIC = 71236;

        //  Adherents
        public const uint DARK_EMPOWERMENT = 70901;
        public const uint FROST_FEVER = 67767;
        public const uint DEATHCHILL_BOLT = 70594;
        public const uint DEATHCHILL_BLAST = 70906;
        public const uint CURSE_OF_TORPOR = 71237;
        public const uint SHORUD_OF_THE_OCCULT = 70768;
        public const uint ADHERENT_S_DETERMINATION = 71234;
        public const uint DARK_MARTYRDOM_ADHERENT = 70903;

        // Vengeful Shade
        public const uint VENGEFUL_BLAST = 71544;
        public const uint VENGEFUL_BLAST_PASSIVE = 71494;
        public const uint VENGEFUL_BLAST_25N = 72010;
        public const uint VENGEFUL_BLAST_10H = 72011;
        public const uint VENGEFUL_BLAST_25H = 72012;

        // Darnavan
        public const uint BLADESTORM = 65947;
        public const uint CHARGE = 65927;
        public const uint INTIMIDATING_SHOUT = 65930;
        public const uint MORTAL_STRIKE = 65926;
        public const uint SHATTERING_THROW = 65940;
        public const uint SUNDER_ARMOR = 65936;
    }

    struct LadyEventTypes
    {
        // Lady Deathwhisper
        public const uint INTRO_2 = 1;
        public const uint INTRO_3 = 2;
        public const uint INTRO_4 = 3;
        public const uint INTRO_5 = 4;
        public const uint INTRO_6 = 5;
        public const uint INTRO_7 = 6;
        public const uint BERSERK = 7;
        public const uint DEATH_AND_DECAY = 8;
        public const uint DOMINATE_MIND_H = 9;

        // Phase 1 only
        public const uint P1_SUMMON_WAVE = 10;
        public const uint P1_SHADOW_BOLT = 11;
        public const uint P1_EMPOWER_CULTIST = 12;
        public const uint P1_REANIMATE_CULTIST = 13;

        // Phase 2 only
        public const uint P2_SUMMON_WAVE = 14;
        public const uint P2_FROSTBOLT = 15;
        public const uint P2_FROSTBOLT_VOLLEY = 16;
        public const uint P2_TOUCH_OF_INSIGNIFICANCE = 17;
        public const uint P2_SUMMON_SHADE = 18;

        // Shared adds events
        public const uint CULTIST_DARK_MARTYRDOM = 19;

        // Cult Fanatic
        public const uint FANATIC_NECROTIC_STRIKE = 20;
        public const uint FANATIC_SHADOW_CLEAVE = 21;
        public const uint FANATIC_VAMPIRIC_MIGHT = 22;

        // Cult Adherent
        public const uint ADHERENT_FROST_FEVER = 23;
        public const uint ADHERENT_DEATHCHILL = 24;
        public const uint ADHERENT_CURSE_OF_TORPOR = 25;
        public const uint ADHERENT_SHORUD_OF_THE_OCCULT = 26;

        // Darnavan
        public const uint DARNAVAN_BLADESTORM = 27;
        public const uint DARNAVAN_CHARGE = 28;
        public const uint DARNAVAN_INTIMIDATING_SHOUT = 29;
        public const uint DARNAVAN_MORTAL_STRIKE = 30;
        public const uint DARNAVAN_SHATTERING_THROW = 31;
        public const uint DARNAVAN_SUNDER_ARMOR = 32;
    }

    struct DeprogrammingData
    {
        public const uint NPC_DARNAVAN_10 = 38472;
        public const uint NPC_DARNAVAN_25 = 38485;
        public const uint NPC_DARNAVAN_CREDIT_10 = 39091;
        public const uint NPC_DARNAVAN_CREDIT_25 = 39092;

        public const int ACTION_COMPLETE_QUEST = -384720;
        public const uint POINT_DESPAWN = 384721;
    }

    struct LadyConst
    {
        public const byte PhaseAll = 0;
        public const byte PhaseIntro = 1;
        public const byte PhaseOne = 2;
        public const byte PhaseTwo = 3;

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
        public boss_lady_deathwhisper(Creature creature)
            : base(creature, Bosses.LadyDeathwhisper)
        {
            _dominateMindCount = RaidMode<byte>(0, 1, 1, 3);
            _introDone = false;

        }

        public override void Reset()
        {
            _Reset();
            me.SetFullPower(PowerType.Mana);
            _events.SetPhase(LadyConst.PhaseOne);
            _waveCounter = 0;
            _nextVengefulShadeTargetGUID.Clear();
            _darnavanGUID.Clear();
            DoCast(me, LadySpells.SHADOW_CHANNELING);
            me.RemoveAurasDueToSpell(InstanceSpells.Berserk);
            me.RemoveAurasDueToSpell(LadySpells.MANA_BARRIER);
            me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModTaunt, false);
            me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.AttackMe, false);
        }

        public override void MoveInLineOfSight(Unit who)
        {
            if (!_introDone && me.IsWithinDistInMap(who, 110.0f))
            {
                _introDone = true;
                Talk(LadyTexts.SAY_INTRO_1);
                _events.SetPhase(LadyConst.PhaseIntro);
                _events.ScheduleEvent(LadyEventTypes.INTRO_2, 11000, 0, LadyConst.PhaseIntro);
                _events.ScheduleEvent(LadyEventTypes.INTRO_3, 21000, 0, LadyConst.PhaseIntro);
                _events.ScheduleEvent(LadyEventTypes.INTRO_4, 31500, 0, LadyConst.PhaseIntro);
                _events.ScheduleEvent(LadyEventTypes.INTRO_5, 39500, 0, LadyConst.PhaseIntro);
                _events.ScheduleEvent(LadyEventTypes.INTRO_6, 48500, 0, LadyConst.PhaseIntro);
                _events.ScheduleEvent(LadyEventTypes.INTRO_7, 58000, 0, LadyConst.PhaseIntro);
            }
        }

        public override void AttackStart(Unit victim)
        {
            if (me.HasFlag(UnitFields.Flags, UnitFlags.NonAttackable))
                return;

            if (victim && me.Attack(victim, true) && !_events.IsInPhase(LadyConst.PhaseOne))
                me.GetMotionMaster().MoveChase(victim);
        }

        public override void EnterCombat(Unit who)
        {
            if (!instance.CheckRequiredBosses(Bosses.LadyDeathwhisper, who.ToPlayer()))
            {
                EnterEvadeMode();
                instance.DoCastSpellOnPlayers(TeleporterSpells.LIGHT_S_HAMMER_TELEPORT);
                return;
            }

            me.setActive(true);
            DoZoneInCombat();

            _events.Reset();
            _events.SetPhase(LadyConst.PhaseOne);
            // phase-independent events
            _events.ScheduleEvent(LadyEventTypes.BERSERK, 600000);
            _events.ScheduleEvent(LadyEventTypes.DEATH_AND_DECAY, 10000);
            // phase one only
            _events.ScheduleEvent(LadyEventTypes.P1_SUMMON_WAVE, 5000, 0, LadyConst.PhaseOne);
            _events.ScheduleEvent(LadyEventTypes.P1_SHADOW_BOLT, RandomHelper.URand(5500, 6000), 0, LadyConst.PhaseOne);
            _events.ScheduleEvent(LadyEventTypes.P1_EMPOWER_CULTIST, RandomHelper.URand(20000, 30000), 0, LadyConst.PhaseOne);
            _events.ScheduleEvent(LadyEventTypes.P1_REANIMATE_CULTIST, RandomHelper.URand(10000, 20000), 0, LadyConst.PhaseOne);
            if (GetDifficulty() != Difficulty.Raid10N)
                _events.ScheduleEvent(LadyEventTypes.DOMINATE_MIND_H, 27000);

            Talk(LadyTexts.SAY_AGGRO);
            DoStartNoMovement(who);
            me.RemoveAurasDueToSpell(LadySpells.SHADOW_CHANNELING);
            DoCast(me, LadySpells.MANA_BARRIER, true);

            instance.SetBossState(Bosses.LadyDeathwhisper, EncounterState.InProgress);
        }

        public override void JustDied(Unit killer)
        {
            Talk(LadyTexts.SAY_DEATH);

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
                instance.DoUpdateCriteria(CriteriaTypes.BeSpellTarget, LadySpells.FULL_HOUSE, 0, me);

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
                    darnavan.GetAI().Talk(LadyTexts.SAY_DARNAVAN_RESCUED);
                    Player owner = killer.GetCharmerOrOwnerPlayerOrPlayerItself();
                    if (owner)
                    {
                        Group group = owner.GetGroup();
                        if (group)
                        {
                            for (GroupReference groupRefe = group.GetFirstMember(); groupRefe != null; groupRefe = groupRefe.next())
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

        public override void JustReachedHome()
        {
            _JustReachedHome();
            instance.SetBossState(Bosses.LadyDeathwhisper, EncounterState.Fail);

            summons.DespawnAll();
            Creature darnavan = ObjectAccessor.GetCreature(me, _darnavanGUID);
            if (darnavan)
            {
                darnavan.DespawnOrUnsummon();
                _darnavanGUID.Clear();
            }
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsTypeId(TypeId.Player))
                Talk(LadyTexts.SAY_KILL);
        }

        public override void DamageTaken(Unit damageDealer, ref uint damage)
        {
            // phase transition
            if (_events.IsInPhase(LadyConst.PhaseOne) && damage > (uint)me.GetPower(PowerType.Mana))
            {
                Talk(LadyTexts.SAY_PHASE_2);
                Talk(LadyTexts.EMOTE_PHASE_2);
                DoStartMovement(me.GetVictim());
                damage -= (uint)me.GetPower(PowerType.Mana);
                me.SetPower(PowerType.Mana, 0);
                me.RemoveAurasDueToSpell(LadySpells.MANA_BARRIER);
                _events.SetPhase(LadyConst.PhaseTwo);
                _events.ScheduleEvent(LadyEventTypes.P2_FROSTBOLT, RandomHelper.URand(10000, 12000), 0, LadyConst.PhaseTwo);
                _events.ScheduleEvent(LadyEventTypes.P2_FROSTBOLT_VOLLEY, RandomHelper.URand(19000, 21000), 0, LadyConst.PhaseTwo);
                _events.ScheduleEvent(LadyEventTypes.P2_TOUCH_OF_INSIGNIFICANCE, RandomHelper.URand(6000, 9000), 0, LadyConst.PhaseTwo);
                _events.ScheduleEvent(LadyEventTypes.P2_SUMMON_SHADE, RandomHelper.URand(12000, 15000), 0, LadyConst.PhaseTwo);
                // on heroic mode Lady Deathwhisper is immune to taunt effects in phase 2 and continues summoning adds
                if (IsHeroic())
                {
                    me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModTaunt, true);
                    me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.AttackMe, true);
                    _events.ScheduleEvent(LadyEventTypes.P2_SUMMON_WAVE, 45000, 0, LadyConst.PhaseTwo);
                }
            }
        }

        public override void JustSummoned(Creature summon)
        {
            if (summon.GetEntry() == NPC_DARNAVAN)
                _darnavanGUID = summon.GetGUID();
            else
                summons.Summon(summon);

            Unit target = null;
            if (summon.GetEntry() == CreatureIds.VengefulShade)
            {
                target = Global.ObjAccessor.GetUnit(me, _nextVengefulShadeTargetGUID);   // Vengeful Shade
                _nextVengefulShadeTargetGUID.Clear();
            }
            else
                target = SelectTarget(SelectAggroTarget.Random);                        // Wave adds

            summon.GetAI().AttackStart(target);                                      // CAN be NULL
        }

        public override void UpdateAI(uint diff)
        {
            if ((!UpdateVictim() && !_events.IsInPhase(LadyConst.PhaseIntro)))
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting) && !_events.IsInPhase(LadyConst.PhaseIntro))
                return;

            Unit target;

            _events.ExecuteEvents(eventId =>
            {

                switch (eventId)
                {
                    case LadyEventTypes.INTRO_2:
                        Talk(LadyTexts.SAY_INTRO_2);
                        break;
                    case LadyEventTypes.INTRO_3:
                        Talk(LadyTexts.SAY_INTRO_3);
                        break;
                    case LadyEventTypes.INTRO_4:
                        Talk(LadyTexts.SAY_INTRO_4);
                        break;
                    case LadyEventTypes.INTRO_5:
                        Talk(LadyTexts.SAY_INTRO_5);
                        break;
                    case LadyEventTypes.INTRO_6:
                        Talk(LadyTexts.SAY_INTRO_6);
                        break;
                    case LadyEventTypes.INTRO_7:
                        Talk(LadyTexts.SAY_INTRO_7);
                        break;
                    case LadyEventTypes.DEATH_AND_DECAY:
                        target = SelectTarget(SelectAggroTarget.Random);
                        if (target)
                            DoCast(target, LadySpells.DEATH_AND_DECAY);
                        _events.ScheduleEvent(LadyEventTypes.DEATH_AND_DECAY, RandomHelper.URand(22000, 30000));
                        break;
                    case LadyEventTypes.DOMINATE_MIND_H:
                        Talk(LadyTexts.SAY_DOMINATE_MIND);
                        for (byte i = 0; i < _dominateMindCount; i++)
                        {
                            target = SelectTarget(SelectAggroTarget.Random, 1, 0.0f, true, -(int)LadySpells.DOMINATE_MIND_H);
                            if (target)
                                DoCast(target, LadySpells.DOMINATE_MIND_H);
                        }
                        _events.ScheduleEvent(LadyEventTypes.DOMINATE_MIND_H, RandomHelper.URand(40000, 45000));
                        break;
                    case LadyEventTypes.P1_SUMMON_WAVE:
                        SummonWaveP1();
                        _events.ScheduleEvent(LadyEventTypes.P1_SUMMON_WAVE, (uint)(IsHeroic() ? 45000 : 60000), 0, LadyConst.PhaseOne);
                        break;
                    case LadyEventTypes.P1_SHADOW_BOLT:
                        target = SelectTarget(SelectAggroTarget.Random);
                        if (target)
                            DoCast(target, LadySpells.SHADOW_BOLT);
                        _events.ScheduleEvent(LadyEventTypes.P1_SHADOW_BOLT, RandomHelper.URand(5000, 8000), 0, LadyConst.PhaseOne);
                        break;
                    case LadyEventTypes.P1_REANIMATE_CULTIST:
                        ReanimateCultist();
                        _events.ScheduleEvent(LadyEventTypes.P1_REANIMATE_CULTIST, RandomHelper.URand(6000, 25000), 0, LadyConst.PhaseOne);
                        break;
                    case LadyEventTypes.P1_EMPOWER_CULTIST:
                        EmpowerCultist();
                        _events.ScheduleEvent(LadyEventTypes.P1_EMPOWER_CULTIST, RandomHelper.URand(18000, 25000), 0, LadyConst.PhaseOne);
                        break;
                    case LadyEventTypes.P2_FROSTBOLT:
                        DoCastVictim(LadySpells.FROSTBOLT);
                        _events.ScheduleEvent(LadyEventTypes.P2_FROSTBOLT, RandomHelper.URand(10000, 11000), 0, LadyConst.PhaseTwo);
                        break;
                    case LadyEventTypes.P2_FROSTBOLT_VOLLEY:
                        DoCastAOE(LadySpells.FROSTBOLT_VOLLEY);
                        _events.ScheduleEvent(LadyEventTypes.P2_FROSTBOLT_VOLLEY, RandomHelper.URand(13000, 15000), 0, LadyConst.PhaseTwo);
                        break;
                    case LadyEventTypes.P2_TOUCH_OF_INSIGNIFICANCE:
                        DoCastVictim(LadySpells.TOUCH_OF_INSIGNIFICANCE);
                        _events.ScheduleEvent(LadyEventTypes.P2_TOUCH_OF_INSIGNIFICANCE, RandomHelper.URand(9000, 13000), 0, LadyConst.PhaseTwo);
                        break;
                    case LadyEventTypes.P2_SUMMON_SHADE:
                        Unit shadeTarget = SelectTarget(SelectAggroTarget.Random, 1);
                        if (shadeTarget)
                        {
                            _nextVengefulShadeTargetGUID = shadeTarget.GetGUID();
                            DoCast(shadeTarget, LadySpells.SUMMON_SHADE);
                        }
                        _events.ScheduleEvent(LadyEventTypes.P2_SUMMON_SHADE, RandomHelper.URand(18000, 23000), 0, LadyConst.PhaseTwo);
                        break;
                    case LadyEventTypes.P2_SUMMON_WAVE:
                        SummonWaveP2();
                        _events.ScheduleEvent(LadyEventTypes.P2_SUMMON_WAVE, 45000, 0, LadyConst.PhaseTwo);
                        break;
                    case LadyEventTypes.BERSERK:
                        DoCast(me, InstanceSpells.Berserk);
                        Talk(LadyTexts.SAY_BERSERK);
                        break;
                }
            });

            // We should not melee attack when barrier is up
            if (me.HasAura(LadySpells.MANA_BARRIER))
                return;

            DoMeleeAttackIfReady();
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
                summon.GetAI().DoCast(summon, LadySpells.TELEPORT_VISUAL);
        }

        void ReanimateCultist()
        {
            if (summons.Empty())
                return;

            List<Creature> temp = new List<Creature>();
            foreach (var guid in summons)
            {
                Creature cre = ObjectAccessor.GetCreature(me, guid);
                if (cre)
                    if (cre.IsAlive() && (cre.GetEntry() == CreatureIds.CultFanatic || cre.GetEntry() == CreatureIds.CultAdherent))
                        temp.Add(cre);
            }

            if (temp.Empty())
                return;

            Creature cultist = temp.SelectRandom();
            DoCast(cultist, LadySpells.DARK_MARTYRDOM_T, true);
        }

        void EmpowerCultist()
        {
            if (summons.Empty())
                return;

            List<Creature> temp = new List<Creature>();
            foreach (var guid in summons)
            {
                Creature cre = ObjectAccessor.GetCreature(me, guid);
                if (cre)
                    if (cre.IsAlive() && (cre.GetEntry() == CreatureIds.CultFanatic || cre.GetEntry() == CreatureIds.CultAdherent))
                        temp.Add(cre);
            }

            // noone to empower
            if (temp.Empty())
                return;

            // select random cultist
            Creature cultist = temp.SelectRandom();
            DoCast(cultist, cultist.GetEntry() == CreatureIds.CultFanatic ? LadySpells.DARK_TRANSFORMATION_T : LadySpells.DARK_EMPOWERMENT_T, true);
            Talk(cultist.GetEntry() == CreatureIds.CultFanatic ? LadyTexts.SAY_DARK_TRANSFORMATION : LadyTexts.SAY_DARK_EMPOWERMENT);
        }

        ObjectGuid _nextVengefulShadeTargetGUID;
        ObjectGuid _darnavanGUID;
        uint _waveCounter;
        byte _dominateMindCount;
        bool _introDone;

        uint NPC_DARNAVAN { get { return RaidMode<uint>(DeprogrammingData.NPC_DARNAVAN_10, DeprogrammingData.NPC_DARNAVAN_25, DeprogrammingData.NPC_DARNAVAN_10, DeprogrammingData.NPC_DARNAVAN_25); } }
        uint NPC_DARNAVAN_CREDIT { get { return RaidMode<uint>(DeprogrammingData.NPC_DARNAVAN_CREDIT_10, DeprogrammingData.NPC_DARNAVAN_CREDIT_25, DeprogrammingData.NPC_DARNAVAN_CREDIT_10, DeprogrammingData.NPC_DARNAVAN_CREDIT_25); } }
        uint QUEST_DEPROGRAMMING { get { return RaidMode<uint>(WeeklyQuestIds.Deprogramming10, WeeklyQuestIds.Deprogramming25, WeeklyQuestIds.Deprogramming10, WeeklyQuestIds.Deprogramming25); } }
    }

    [Script]
    class npc_cult_fanatic : ScriptedAI
    {
        public npc_cult_fanatic(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(LadyEventTypes.FANATIC_NECROTIC_STRIKE, RandomHelper.URand(10000, 12000));
            _events.ScheduleEvent(LadyEventTypes.FANATIC_SHADOW_CLEAVE, RandomHelper.URand(14000, 16000));
            _events.ScheduleEvent(LadyEventTypes.FANATIC_VAMPIRIC_MIGHT, RandomHelper.URand(20000, 27000));
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            switch (spell.Id)
            {
                case LadySpells.DARK_TRANSFORMATION:
                    me.UpdateEntry(CreatureIds.DeformedFanatic);
                    break;
                case LadySpells.DARK_TRANSFORMATION_T:
                    if (me.HasFlag(ObjectFields.DynamicFlags, UnitDynFlags.Dead))
                        break;
                    me.InterruptNonMeleeSpells(true);
                    DoCast(me, LadySpells.DARK_TRANSFORMATION);
                    break;
                case LadySpells.DARK_MARTYRDOM_T:
                    me.InterruptNonMeleeSpells(true);
                    DoCast(me, LadySpells.DARK_MARTYRDOM_FANATIC);
                    break;
                case LadySpells.DARK_MARTYRDOM_FANATIC: // 10nm
                case 72495: // 25nm
                case 72496: // 10hc
                case 72497: // 25hc
                    me.SetFlag(UnitFields.Flags, UnitFlags.RemoveClientControl | UnitFlags.Pacified | UnitFlags.NonAttackable | UnitFlags.Unk29);
                    me.SetFlag(ObjectFields.DynamicFlags, UnitDynFlags.Dead);
                    me.SetFlag(UnitFields.Flags2, UnitFlags2.FeignDeath);
                    me.SetReactState(ReactStates.Passive);
                    me.AttackStop();
                    _events.ScheduleEvent(LadyEventTypes.CULTIST_DARK_MARTYRDOM, 4000);
                    break;
                default:
                    break;

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
                    case LadyEventTypes.FANATIC_NECROTIC_STRIKE:
                        DoCastVictim(LadySpells.NECROTIC_STRIKE);
                        _events.ScheduleEvent(LadyEventTypes.FANATIC_NECROTIC_STRIKE, RandomHelper.URand(11000, 13000));
                        break;
                    case LadyEventTypes.FANATIC_SHADOW_CLEAVE:
                        DoCastVictim(LadySpells.SHADOW_CLEAVE);
                        _events.ScheduleEvent(LadyEventTypes.FANATIC_SHADOW_CLEAVE, RandomHelper.URand(9500, 11000));
                        break;
                    case LadyEventTypes.FANATIC_VAMPIRIC_MIGHT:
                        DoCast(me, LadySpells.VAMPIRIC_MIGHT);
                        _events.ScheduleEvent(LadyEventTypes.FANATIC_VAMPIRIC_MIGHT, RandomHelper.URand(20000, 27000));
                        break;
                    case LadyEventTypes.CULTIST_DARK_MARTYRDOM:
                        if (me.IsSummon())
                        {
                            Unit owner = me.ToTempSummon().GetSummoner();
                            if (owner)
                                if (owner.ToCreature())
                                    owner.ToCreature().GetAI().Talk(LadyTexts.SAY_ANIMATE_DEAD);
                        }
                        me.UpdateEntry(CreatureIds.ReanimatedFanatic);
                        me.RemoveFlag(UnitFields.Flags, UnitFlags.RemoveClientControl | UnitFlags.Pacified | UnitFlags.NonAttackable | UnitFlags.Unk29);
                        me.RemoveFlag(ObjectFields.DynamicFlags, UnitDynFlags.Dead);
                        me.RemoveFlag(UnitFields.Flags2, UnitFlags2.FeignDeath);
                        DoCast(me, LadySpells.FANATIC_S_DETERMINATION);
                        me.SetReactState(ReactStates.Aggressive);
                        AttackStart(SelectTarget(SelectAggroTarget.Random));
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_cult_adherent : ScriptedAI
    {
        public npc_cult_adherent(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(LadyEventTypes.ADHERENT_FROST_FEVER, RandomHelper.URand(10000, 12000));
            _events.ScheduleEvent(LadyEventTypes.ADHERENT_DEATHCHILL, RandomHelper.URand(14000, 16000));
            _events.ScheduleEvent(LadyEventTypes.ADHERENT_CURSE_OF_TORPOR, RandomHelper.URand(14000, 16000));
            _events.ScheduleEvent(LadyEventTypes.ADHERENT_SHORUD_OF_THE_OCCULT, RandomHelper.URand(32000, 39000));
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            switch (spell.Id)
            {
                case LadySpells.DARK_EMPOWERMENT:
                    me.UpdateEntry(CreatureIds.EmpoweredAdherent);
                    break;
                case LadySpells.DARK_EMPOWERMENT_T:
                    if (me.HasFlag(ObjectFields.DynamicFlags, UnitDynFlags.Dead))
                        break;
                    me.InterruptNonMeleeSpells(true);
                    DoCast(me, LadySpells.DARK_EMPOWERMENT);
                    break;
                case LadySpells.DARK_MARTYRDOM_T:
                    me.InterruptNonMeleeSpells(true);
                    DoCast(me, LadySpells.DARK_MARTYRDOM_ADHERENT);
                    break;
                case LadySpells.DARK_MARTYRDOM_ADHERENT: // 10nm
                case 72498: // 25nm
                case 72499: // 10hc
                case 72500: // 25hc
                    me.SetFlag(UnitFields.Flags, UnitFlags.RemoveClientControl | UnitFlags.Pacified | UnitFlags.NonAttackable | UnitFlags.Unk29);
                    me.SetFlag(ObjectFields.DynamicFlags, UnitDynFlags.Dead);
                    me.SetFlag(UnitFields.Flags2, UnitFlags2.FeignDeath);
                    me.SetReactState(ReactStates.Passive);
                    me.AttackStop();
                    _events.ScheduleEvent(LadyEventTypes.CULTIST_DARK_MARTYRDOM, 4000);
                    break;
                default:
                    break;
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
                    case LadyEventTypes.ADHERENT_FROST_FEVER:
                        DoCastVictim(LadySpells.FROST_FEVER);
                        _events.ScheduleEvent(LadyEventTypes.ADHERENT_FROST_FEVER, RandomHelper.URand(9000, 13000));
                        break;
                    case LadyEventTypes.ADHERENT_DEATHCHILL:
                        if (me.GetEntry() == CreatureIds.EmpoweredAdherent)
                            DoCastVictim(LadySpells.DEATHCHILL_BLAST);
                        else
                            DoCastVictim(LadySpells.DEATHCHILL_BOLT);
                        _events.ScheduleEvent(LadyEventTypes.ADHERENT_DEATHCHILL, RandomHelper.URand(9000, 13000));
                        break;
                    case LadyEventTypes.ADHERENT_CURSE_OF_TORPOR:
                        Unit target = SelectTarget(SelectAggroTarget.Random, 1);
                        if (target)
                            DoCast(target, LadySpells.CURSE_OF_TORPOR);
                        _events.ScheduleEvent(LadyEventTypes.ADHERENT_CURSE_OF_TORPOR, RandomHelper.URand(9000, 13000));
                        break;
                    case LadyEventTypes.ADHERENT_SHORUD_OF_THE_OCCULT:
                        DoCast(me, LadySpells.SHORUD_OF_THE_OCCULT);
                        _events.ScheduleEvent(LadyEventTypes.ADHERENT_SHORUD_OF_THE_OCCULT, RandomHelper.URand(27000, 32000));
                        break;
                    case LadyEventTypes.CULTIST_DARK_MARTYRDOM:
                        if (me.IsSummon())
                        {
                            Unit owner = me.ToTempSummon().GetSummoner();
                            if (owner)
                                if (owner.ToCreature())
                                    owner.ToCreature().GetAI().Talk(LadyTexts.SAY_ANIMATE_DEAD);
                        }
                        me.UpdateEntry(CreatureIds.ReanimatedAdherent);
                        me.RemoveFlag(UnitFields.Flags, UnitFlags.RemoveClientControl | UnitFlags.Pacified | UnitFlags.NonAttackable | UnitFlags.Unk29);
                        me.RemoveFlag(ObjectFields.DynamicFlags, UnitDynFlags.Dead);
                        me.RemoveFlag(UnitFields.Flags2, UnitFlags2.FeignDeath);
                        DoCast(me, LadySpells.FANATIC_S_DETERMINATION);
                        me.SetReactState(ReactStates.Aggressive);
                        AttackStart(SelectTarget(SelectAggroTarget.Random));
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_vengeful_shade : ScriptedAI
    {
        public npc_vengeful_shade(Creature creature) : base(creature)
        {
            me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
        }

        public override void Reset()
        {
            me.AddAura(LadySpells.VENGEFUL_BLAST_PASSIVE, me);
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            switch (spell.Id)
            {
                case LadySpells.VENGEFUL_BLAST:
                case LadySpells.VENGEFUL_BLAST_25N:
                case LadySpells.VENGEFUL_BLAST_10H:
                case LadySpells.VENGEFUL_BLAST_25H:
                    me.KillSelf();
                    break;
                default:
                    break;
            }
        }
    }

    [Script]
    class npc_darnavan : ScriptedAI
    {
        public npc_darnavan(Creature creature)
            : base(creature)
        {
        }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(LadyEventTypes.DARNAVAN_BLADESTORM, 10000);
            _events.ScheduleEvent(LadyEventTypes.DARNAVAN_INTIMIDATING_SHOUT, RandomHelper.URand(20000, 25000));
            _events.ScheduleEvent(LadyEventTypes.DARNAVAN_MORTAL_STRIKE, RandomHelper.URand(25000, 30000));
            _events.ScheduleEvent(LadyEventTypes.DARNAVAN_SUNDER_ARMOR, RandomHelper.URand(5000, 8000));
            _canCharge = true;
            _canShatter = true;
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
                    for (GroupReference groupRefe = group.GetFirstMember(); groupRefe != null; groupRefe = groupRefe.next())
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
            Talk(LadyTexts.SAY_DARNAVAN_AGGRO);
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
                DoCastVictim(LadySpells.SHATTERING_THROW);
                _canShatter = false;
                _events.ScheduleEvent(LadyEventTypes.DARNAVAN_SHATTERING_THROW, 30000);
                return;
            }

            if (_canCharge && !me.IsWithinMeleeRange(me.GetVictim()))
            {
                DoCastVictim(LadySpells.CHARGE);
                _canCharge = false;
                _events.ScheduleEvent(LadyEventTypes.DARNAVAN_CHARGE, 20000);
                return;
            }

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case LadyEventTypes.DARNAVAN_BLADESTORM:
                        DoCast(LadySpells.BLADESTORM);
                        _events.ScheduleEvent(LadyEventTypes.DARNAVAN_BLADESTORM, RandomHelper.URand(90000, 100000));
                        break;
                    case LadyEventTypes.DARNAVAN_CHARGE:
                        _canCharge = true;
                        break;
                    case LadyEventTypes.DARNAVAN_INTIMIDATING_SHOUT:
                        DoCast(LadySpells.INTIMIDATING_SHOUT);
                        _events.ScheduleEvent(LadyEventTypes.DARNAVAN_INTIMIDATING_SHOUT, RandomHelper.URand(90000, 120000));
                        break;
                    case LadyEventTypes.DARNAVAN_MORTAL_STRIKE:
                        DoCastVictim(LadySpells.MORTAL_STRIKE);
                        _events.ScheduleEvent(LadyEventTypes.DARNAVAN_MORTAL_STRIKE, RandomHelper.URand(15000, 30000));
                        break;
                    case LadyEventTypes.DARNAVAN_SHATTERING_THROW:
                        _canShatter = true;
                        break;
                    case LadyEventTypes.DARNAVAN_SUNDER_ARMOR:
                        DoCastVictim(LadySpells.SUNDER_ARMOR);
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
    class spell_cultist_dark_martyrdom : SpellScript
    {
        void HandleEffect(uint effIndex)
        {
            if (GetCaster().IsSummon())
            {
                Unit owner = GetCaster().ToTempSummon().GetSummoner();
                if (owner)
                    owner.GetAI().SetGUID(GetCaster().GetGUID(), (int)LadyConst.GUIDCultist);
            }

            GetCaster().KillSelf();
            GetCaster().SetDisplayId(GetCaster().GetEntry() == CreatureIds.CultFanatic ? 38009 : 38010u);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleEffect, 2, SpellEffectName.ForceDeselect));
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
}
