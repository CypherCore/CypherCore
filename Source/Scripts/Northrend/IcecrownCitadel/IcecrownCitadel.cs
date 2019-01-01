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
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;
using System.Linq;

namespace Scripts.Northrend.IcecrownCitadel
{
    class FrostwingVrykulSearcher<T> : ICheck<T> where T : Unit
    {
        public FrostwingVrykulSearcher(Creature source, float range)
        {
            _source = source;
            _range = range;
        }

        public bool Invoke(T u)
        {
            if (!u.IsAlive())
                return false;

            switch (u.GetEntry())
            {
                case CreatureIds.YmirjarBattleMaiden:
                case CreatureIds.YmirjarDeathbringer:
                case CreatureIds.YmirjarFrostbinder:
                case CreatureIds.YmirjarHuntress:
                case CreatureIds.YmirjarWarlord:
                    break;
                default:
                    return false;
            }

            if (!u.IsWithinDist(_source, _range, false))
                return false;

            return true;
        }

        Creature _source;
        float _range;
    }

    class FrostwingGauntletRespawner : IDoWork<Creature>
    {
        public void Invoke(Creature creature)
        {
            switch (creature.GetOriginalEntry())
            {
                case CreatureIds.YmirjarBattleMaiden:
                case CreatureIds.YmirjarDeathbringer:
                case CreatureIds.YmirjarFrostbinder:
                case CreatureIds.YmirjarHuntress:
                case CreatureIds.YmirjarWarlord:
                    break;
                case CreatureIds.CrokScourgebane:
                case CreatureIds.CaptainArnath:
                case CreatureIds.CaptainBrandon:
                case CreatureIds.CaptainGrondel:
                case CreatureIds.CaptainRupert:
                    creature.GetAI().DoAction(Actions.ResetEvent);
                    break;
                case CreatureIds.SisterSvalna:
                    creature.GetAI().DoAction(Actions.ResetEvent);
                    // return, this creature is never dead if event is reset
                    return;
                default:
                    return;
            }

            uint corpseDelay = creature.GetCorpseDelay();
            uint respawnDelay = creature.GetRespawnDelay();
            creature.SetCorpseDelay(1);
            creature.SetRespawnDelay(2);

            CreatureData data = creature.GetCreatureData();
            if (data != null)
                creature.SetPosition(data.posX, data.posY, data.posZ, data.orientation);
            creature.DespawnOrUnsummon();

            creature.SetCorpseDelay(corpseDelay);
            creature.SetRespawnDelay(respawnDelay);
        }
    }

    class CaptainSurviveTalk : BasicEvent
    {
        public CaptainSurviveTalk(Creature owner)
        {
            _owner = owner;
        }

        public override bool Execute(ulong currTime, uint diff)
        {
            _owner.GetAI().Talk(Texts.SayCaptainSurviveTalk);
            return true;
        }

        Creature _owner;
    }

    // at Light's Hammer
    [Script]
    class npc_highlord_tirion_fordring_lh : ScriptedAI
    {
        public npc_highlord_tirion_fordring_lh(Creature creature)
            : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            _events.Reset();
            _theLichKing.Clear();
            _bolvarFordragon.Clear();
            _factionNPC.Clear();
            _damnedKills = 0;
        }

        // IMPORTANT NOTE: This is triggered from per-GUID scripts
        // of The Damned SAI
        public override void SetData(uint type, uint data)
        {
            if (type == 1 && data == 1)
            {
                if (++_damnedKills == 2)
                {
                    Creature theLichKing = me.FindNearestCreature(CreatureIds.TheLichKingLh, 150.0f);
                    if (theLichKing)
                    {
                        Creature bolvarFordragon = me.FindNearestCreature(CreatureIds.HighlordBolvarFordragonLh, 150.0f);
                        if (bolvarFordragon)
                        {
                            Creature factionNPC = me.FindNearestCreature(_instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde ? CreatureIds.SeHighOverlordSaurfang : CreatureIds.SeMuradinBronzebeard, 50.0f);
                            if (factionNPC)
                            {
                                me.setActive(true);
                                _theLichKing = theLichKing.GetGUID();
                                theLichKing.setActive(true);
                                _bolvarFordragon = bolvarFordragon.GetGUID();
                                bolvarFordragon.setActive(true);
                                _factionNPC = factionNPC.GetGUID();
                                factionNPC.setActive(true);
                            }
                        }
                    }

                    if (_bolvarFordragon.IsEmpty() || _theLichKing.IsEmpty() || _factionNPC.IsEmpty())
                        return;

                    Talk(Texts.SayTirionIntro1);
                    _events.ScheduleEvent(EventTypes.TirionIntro2, 4000);
                    _events.ScheduleEvent(EventTypes.TirionIntro3, 14000);
                    _events.ScheduleEvent(EventTypes.TirionIntro4, 18000);
                    _events.ScheduleEvent(EventTypes.TirionIntro5, 31000);
                    _events.ScheduleEvent(EventTypes.LkIntro1, 35000);
                    _events.ScheduleEvent(EventTypes.TirionIntro6, 51000);
                    _events.ScheduleEvent(EventTypes.LkIntro2, 58000);
                    _events.ScheduleEvent(EventTypes.LkIntro3, 74000);
                    _events.ScheduleEvent(EventTypes.LkIntro4, 86000);
                    _events.ScheduleEvent(EventTypes.BolvarIntro1, 100000);
                    _events.ScheduleEvent(EventTypes.LkIntro5, 108000);

                    if (_instance.GetData(DataTypes.TeamInInstance) == (uint)Team.Horde)
                    {
                        _events.ScheduleEvent(EventTypes.SaurfangIntro1, 120000);
                        _events.ScheduleEvent(EventTypes.TirionIntroH7, 129000);
                        _events.ScheduleEvent(EventTypes.SaurfangIntro2, 139000);
                        _events.ScheduleEvent(EventTypes.SaurfangIntro3, 150000);
                        _events.ScheduleEvent(EventTypes.SaurfangIntro4, 162000);
                        _events.ScheduleEvent(EventTypes.SaurfangRun, 170000);
                    }
                    else
                    {
                        _events.ScheduleEvent(EventTypes.MuradinIntro1, 120000);
                        _events.ScheduleEvent(EventTypes.MuradinIntro2, 124000);
                        _events.ScheduleEvent(EventTypes.MuradinIntro3, 127000);
                        _events.ScheduleEvent(EventTypes.TirionIntroA7, 136000);
                        _events.ScheduleEvent(EventTypes.MuradinIntro4, 144000);
                        _events.ScheduleEvent(EventTypes.MuradinIntro5, 151000);
                        _events.ScheduleEvent(EventTypes.MuradinRun, 157000);
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (_damnedKills != 2)
                return;

            _events.Update(diff);

            _events.ExecuteEvents(eventId =>
            {
                Creature temp;

                switch (eventId)
                {
                    case EventTypes.TirionIntro2:
                        me.HandleEmoteCommand(Emote.OneshotExclamation);
                        break;
                    case EventTypes.TirionIntro3:
                        Talk(Texts.SayTirionIntro2);
                        break;
                    case EventTypes.TirionIntro4:
                        me.HandleEmoteCommand(Emote.OneshotExclamation);
                        break;
                    case EventTypes.TirionIntro5:
                        Talk(Texts.SayTirionIntro3);
                        break;
                    case EventTypes.LkIntro1:
                        me.HandleEmoteCommand(Emote.StateDanceNosheathe);
                        temp = ObjectAccessor.GetCreature(me, _theLichKing);
                        if (temp)
                            temp.GetAI().Talk(Texts.SayLkIntro1);
                        break;
                    case EventTypes.TirionIntro6:
                        Talk(Texts.SayTirionIntro4);
                        break;
                    case EventTypes.LkIntro2:
                        temp = ObjectAccessor.GetCreature(me, _theLichKing);
                        if (temp)
                            temp.GetAI().Talk(Texts.SayLkIntro2);
                        break;
                    case EventTypes.LkIntro3:
                        temp = ObjectAccessor.GetCreature(me, _theLichKing);
                        if (temp)
                            temp.GetAI().Talk(Texts.SayLkIntro3);
                        break;
                    case EventTypes.LkIntro4:
                        temp = ObjectAccessor.GetCreature(me, _theLichKing);
                        if (temp)
                            temp.GetAI().Talk(Texts.SayLkIntro4);
                        break;
                    case EventTypes.BolvarIntro1:
                        temp = ObjectAccessor.GetCreature(me, _bolvarFordragon);
                        if (temp)
                        {
                            temp.GetAI().Talk(Texts.SayBolvarIntro1);
                            temp.setActive(false);
                        }
                        break;
                    case EventTypes.LkIntro5:
                        temp = ObjectAccessor.GetCreature(me, _theLichKing);
                        if (temp)
                        {
                            temp.GetAI().Talk(Texts.SayLkIntro5);
                            temp.setActive(false);
                        }
                        break;
                    case EventTypes.SaurfangIntro1:
                        temp = ObjectAccessor.GetCreature(me, _factionNPC);
                        if (temp)
                            temp.GetAI().Talk(Texts.SaySaurfangIntro1);
                        break;
                    case EventTypes.TirionIntroH7:
                        Talk(Texts.SayTirionIntroH5);
                        break;
                    case EventTypes.SaurfangIntro2:
                        temp = ObjectAccessor.GetCreature(me, _factionNPC);
                        if (temp)
                            temp.GetAI().Talk(Texts.SaySaurfangIntro2);
                        break;
                    case EventTypes.SaurfangIntro3:
                        temp = ObjectAccessor.GetCreature(me, _factionNPC);
                        if (temp)
                            temp.GetAI().Talk(Texts.SaySaurfangIntro3);
                        break;
                    case EventTypes.SaurfangIntro4:
                        temp = ObjectAccessor.GetCreature(me, _factionNPC);
                        if (temp)
                            temp.GetAI().Talk(Texts.SaySaurfangIntro4);
                        break;
                    case EventTypes.MuradinRun:
                    case EventTypes.SaurfangRun:
                        Creature factionNPC = ObjectAccessor.GetCreature(me, _factionNPC);
                        if (factionNPC)
                            factionNPC.GetMotionMaster().MovePath((uint)(factionNPC.GetSpawnId() * 10), false);
                        me.setActive(false);
                        _damnedKills = 3;
                        break;
                    case EventTypes.MuradinIntro1:
                        temp = ObjectAccessor.GetCreature(me, _factionNPC);
                        if (temp)
                            temp.GetAI().Talk(Texts.SayMuradinIntro1);
                        break;
                    case EventTypes.MuradinIntro2:
                        temp = ObjectAccessor.GetCreature(me, _factionNPC);
                        if (temp)
                            temp.HandleEmoteCommand(Emote.OneshotTalk);
                        break;
                    case EventTypes.MuradinIntro3:
                        temp = ObjectAccessor.GetCreature(me, _factionNPC);
                        if (temp)
                            temp.HandleEmoteCommand(Emote.OneshotExclamation);
                        break;
                    case EventTypes.TirionIntroA7:
                        Talk(Texts.SayTirionIntroA5);
                        break;
                    case EventTypes.MuradinIntro4:
                        temp = ObjectAccessor.GetCreature(me, _factionNPC);
                        if (temp)
                            temp.GetAI().Talk(Texts.SayMuradinIntro2);
                        break;
                    case EventTypes.MuradinIntro5:
                        temp = ObjectAccessor.GetCreature(me, _factionNPC);
                        if (temp)
                            temp.GetAI().Talk(Texts.SayMuradinIntro3);
                        break;
                    default:
                        break;
                }
            });
        }

        InstanceScript _instance;
        ObjectGuid _theLichKing;
        ObjectGuid _bolvarFordragon;
        ObjectGuid _factionNPC;
        ushort _damnedKills;
    }

    [Script]
    class npc_rotting_frost_giant : ScriptedAI
    {
        public npc_rotting_frost_giant(Creature creature)
            : base(creature) { }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(EventTypes.DeathPlague, 15000);
            _events.ScheduleEvent(EventTypes.Stomp, RandomHelper.URand(5000, 8000));
            _events.ScheduleEvent(EventTypes.ArcticBreath, RandomHelper.URand(10000, 15000));
        }

        public override void JustDied(Unit killer)
        {
            _events.Reset();
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
                    case EventTypes.DeathPlague:
                        Unit target = SelectTarget(SelectAggroTarget.Random, 1, 0.0f, true);
                        if (target)
                        {
                            Talk(Texts.EmoteDeathPlagueWarning, target);
                            DoCast(target, InstanceSpells.DeathPlague);
                        }
                        _events.ScheduleEvent(EventTypes.DeathPlague, 15000);
                        break;
                    case EventTypes.Stomp:
                        DoCastVictim(InstanceSpells.Stomp);
                        _events.ScheduleEvent(EventTypes.Stomp, RandomHelper.URand(15000, 18000));
                        break;
                    case EventTypes.ArcticBreath:
                        DoCastVictim(InstanceSpells.ArcticBreath);
                        _events.ScheduleEvent(EventTypes.ArcticBreath, RandomHelper.URand(26000, 33000));
                        break;
                    default:
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_frost_freeze_trap : ScriptedAI
    {
        public npc_frost_freeze_trap(Creature creature)
            : base(creature)
        {
            SetCombatMovement(false);
        }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case 1000:
                case 11000:
                    _events.ScheduleEvent(EventTypes.ActivateTrap, (uint)action);
                    break;
                default:
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            _events.Update(diff);

            if (_events.ExecuteEvent() == EventTypes.ActivateTrap)
            {
                DoCast(me, InstanceSpells.ColdflameJets);
                _events.ScheduleEvent(EventTypes.ActivateTrap, 22000);
            }
        }
    }

    [Script]
    class npc_alchemist_adrianna : CreatureScript
    {
        public npc_alchemist_adrianna() : base("npc_alchemist_adrianna") { }

        public override bool OnGossipHello(Player player, Creature creature)
        {
            if (!creature.FindCurrentSpellBySpellId(InstanceSpells.HarvestBlightSpecimen) && !creature.FindCurrentSpellBySpellId(InstanceSpells.HarvestBlightSpecimen25))
                if (player.HasAura(InstanceSpells.OrangeBlightResidue) && player.HasAura(InstanceSpells.GreenBlightResidue))
                    creature.CastSpell(creature, InstanceSpells.HarvestBlightSpecimen, false);
            return false;
        }
    }

    [Script]
    class boss_sister_svalna : BossAI
    {
        public boss_sister_svalna(Creature creature)
            : base(creature, Bosses.SisterSvalna)
        {
            _isEventInProgress = false;

        }

        public override void InitializeAI()
        {
            if (!me.IsDead())
                Reset();

            me.SetReactState(ReactStates.Passive);
        }

        public override void Reset()
        {
            _Reset();
            me.SetReactState(ReactStates.Defensive);
            _isEventInProgress = false;
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(Texts.SaySvalnaDeath);

            ulong delay = 1;
            for (uint i = 0; i < 4; ++i)
            {
                Creature crusader = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.CaptainArnath + i));
                if (crusader)
                {
                    if (crusader.IsAlive() && crusader.GetEntry() == crusader.GetCreatureData().id)
                    {
                        crusader.m_Events.AddEvent(new CaptainSurviveTalk(crusader), crusader.m_Events.CalculateTime(delay));
                        delay += 6000;
                    }
                }
            }
        }

        public override void EnterCombat(Unit attacker)
        {
            _EnterCombat();
            Creature crok = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.CrokScourgebane));
            if (crok)
                crok.GetAI().Talk(Texts.SayCrokCombatSvalna);
            _events.ScheduleEvent(EventTypes.SvalnaCombat, 9000);
            _events.ScheduleEvent(EventTypes.ImpalingSpear, RandomHelper.URand(40000, 50000));
            _events.ScheduleEvent(EventTypes.AetherShield, RandomHelper.URand(100000, 110000));
        }

        public override void KilledUnit(Unit victim)
        {
            switch (victim.GetTypeId())
            {
                case TypeId.Player:
                    Talk(Texts.SaySvalnaKill);
                    break;
                case TypeId.Unit:
                    switch (victim.GetEntry())
                    {
                        case CreatureIds.CaptainArnath:
                        case CreatureIds.CaptainBrandon:
                        case CreatureIds.CaptainGrondel:
                        case CreatureIds.CaptainRupert:
                            Talk(Texts.SaySvalnaKillCaptain);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        public override void JustReachedHome()
        {
            _JustReachedHome();
            me.SetReactState(ReactStates.Passive);
            me.SetDisableGravity(false);
            me.SetHover(false);
        }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case Actions.KillCaptain:
                    me.CastCustomSpell(InstanceSpells.CaressOfDeath, SpellValueMod.MaxTargets, 1, me, true);
                    break;
                case Actions.StartGauntlet:
                    me.setActive(true);
                    _isEventInProgress = true;
                    me.SetFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
                    _events.ScheduleEvent(EventTypes.SvalnaStart, 25000);
                    break;
                case Actions.ResurrectCaptains:
                    _events.ScheduleEvent(EventTypes.SvalnaResurrect, 7000);
                    break;
                case Actions.CaptainDies:
                    Talk(Texts.SaySvalnaCaptainDeath);
                    break;
                case Actions.ResetEvent:
                    me.setActive(false);
                    Reset();
                    break;
                default:
                    break;
            }
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            if (spell.Id == InstanceSpells.HurlSpear && me.HasAura(InstanceSpells.AetherShield))
            {
                me.RemoveAurasDueToSpell(InstanceSpells.AetherShield);
                Talk(Texts.EmoteSvalnaBrokenShield, caster);
            }
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            if (type != MovementGeneratorType.Effect || id != 1)
                return;

            _isEventInProgress = false;
            me.setActive(false);
            me.RemoveFlag(UnitFields.Flags, UnitFlags.ImmuneToPc | UnitFlags.ImmuneToNpc);
            me.SetDisableGravity(false);
            me.SetHover(false);
        }

        public override void SpellHitTarget(Unit target, SpellInfo spell)
        {
            switch (spell.Id)
            {
                case InstanceSpells.ImpalingSpearKill:
                    me.Kill(target);
                    break;
                case InstanceSpells.ImpalingSpear:
                    TempSummon summon = target.SummonCreature(CreatureIds.ImpalingSpear, target);
                    if (summon)
                    {
                        Talk(Texts.EmoteSvalnaImpale, target);
                        summon.CastCustomSpell(SharedConst.VehicleSpellRideHardcoded, SpellValueMod.BasePoint0, 1, target, false);
                        summon.SetFlag(UnitFields.Flags2, UnitFlags2.Unk1 | UnitFlags2.AllowEnemyInteract);
                    }
                    break;
                default:
                    break;
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim() && !_isEventInProgress)
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventTypes.SvalnaStart:
                        Talk(Texts.SaySvalnaEventStart);
                        break;
                    case EventTypes.SvalnaResurrect:
                        Talk(Texts.SaySvalnaResurrectCaptains);
                        me.CastSpell(me, InstanceSpells.ReviveChampion, false);
                        break;
                    case EventTypes.SvalnaCombat:
                        me.SetReactState(ReactStates.Defensive);
                        Talk(Texts.SaySvalnaAggro);
                        break;
                    case EventTypes.ImpalingSpear:
                        Unit target = SelectTarget(SelectAggroTarget.Random, 1, 0.0f, true, -(int)InstanceSpells.ImpalingSpear);
                        if (target)
                        {
                            DoCast(me, InstanceSpells.AetherShield);
                            DoCast(target, InstanceSpells.ImpalingSpear);
                        }
                        _events.ScheduleEvent(EventTypes.ImpalingSpear, RandomHelper.URand(20000, 25000));
                        break;
                    default:
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }

        bool _isEventInProgress;
    }

    [Script]
    class npc_crok_scourgebane : npc_escortAI
    {
        public npc_crok_scourgebane(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
            _respawnTime = creature.GetRespawnDelay();
            _corpseDelay = creature.GetCorpseDelay();

            SetDespawnAtEnd(false);
            SetDespawnAtFar(false);
            _isEventActive = false;
            _isEventDone = _instance.GetBossState(Bosses.SisterSvalna) == EncounterState.Done;
            _didUnderTenPercentText = false;
        }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(EventTypes.ScourgeStrike, RandomHelper.URand(7500, 12500));
            _events.ScheduleEvent(EventTypes.DeathStrike, RandomHelper.URand(25000, 30000));
            me.SetReactState(ReactStates.Defensive);
            _didUnderTenPercentText = false;
            _wipeCheckTimer = 1000;
        }

        public override void DoAction(int action)
        {
            if (action == Actions.StartGauntlet)
            {
                if (_isEventDone || !me.IsAlive())
                    return;

                _isEventActive = true;
                _isEventDone = true;
                // Load Grid with Sister Svalna
                me.GetMap().LoadGrid(4356.71f, 2484.33f);
                Creature svalna = ObjectAccessor.GetCreature(me, _instance.GetGuidData(Bosses.SisterSvalna));
                if (svalna)
                    svalna.GetAI().DoAction(Actions.StartGauntlet);
                Talk(Texts.SayCrokIntro1);
                _events.ScheduleEvent(EventTypes.ArnathIntro2, 7000);
                _events.ScheduleEvent(EventTypes.CrokIntro3, 14000);
                _events.ScheduleEvent(EventTypes.StartPathing, 37000);
                me.setActive(true);
                for (uint i = 0; i < 4; ++i)
                {
                    Creature crusader = ObjectAccessor.GetCreature(me, _instance.GetGuidData(DataTypes.CaptainArnath + i));
                    if (crusader)
                        crusader.GetAI().DoAction(Actions.StartGauntlet);
                }
            }
            else if (action == Actions.ResetEvent)
            {
                _isEventActive = false;
                _isEventDone = _instance.GetBossState(Bosses.SisterSvalna) == EncounterState.Done;
                me.setActive(false);
                _aliveTrash.Clear();
                _currentWPid = 0;
            }
        }

        public override void SetGUID(ObjectGuid guid, int type = 0)
        {
            if (type == Actions.VrykulDeath)
            {
                _aliveTrash.Remove(guid);
                if (_aliveTrash.Empty())
                {
                    SetEscortPaused(false);
                    if (_currentWPid == 4 && _isEventActive)
                    {
                        _isEventActive = false;
                        me.setActive(false);
                        Talk(Texts.SayCrokFinalWp);
                        Creature svalna = ObjectAccessor.GetCreature(me, _instance.GetGuidData(Bosses.SisterSvalna));
                        if (svalna)
                            svalna.GetAI().DoAction(Actions.ResurrectCaptains);
                    }
                }
            }
        }

        public override void WaypointReached(uint waypointId)
        {
            switch (waypointId)
            {
                // pause pathing until trash pack is cleared
                case 0:
                    Talk(Texts.SayCrokCombatWp0);
                    if (!_aliveTrash.Empty())
                        SetEscortPaused(true);
                    break;
                case 1:
                    Talk(Texts.SayCrokCombatWp1);
                    if (!_aliveTrash.Empty())
                        SetEscortPaused(true);
                    break;
                case 4:
                    if (_aliveTrash.Empty() && _isEventActive)
                    {
                        _isEventActive = false;
                        me.setActive(false);
                        Talk(Texts.SayCrokFinalWp);
                        Creature svalna = ObjectAccessor.GetCreature(me, _instance.GetGuidData(Bosses.SisterSvalna));
                        if (svalna)
                            svalna.GetAI().DoAction(Actions.ResurrectCaptains);
                    }
                    break;
                default:
                    break;
            }
        }

        public override void WaypointStart(uint waypointId)
        {
            _currentWPid = waypointId;
            switch (waypointId)
            {
                case 0:
                case 1:
                case 4:
                    {
                        // get spawns by home position
                        float minY = 2600.0f;
                        float maxY = 2650.0f;
                        if (waypointId == 1)
                        {
                            minY -= 50.0f;
                            maxY -= 50.0f;
                            // at waypoints 1 and 2 she kills one captain
                            Creature svalna = ObjectAccessor.GetCreature(me, _instance.GetGuidData(Bosses.SisterSvalna));
                            if (svalna)
                                svalna.GetAI().DoAction(Actions.KillCaptain);
                        }
                        else if (waypointId == 4)
                        {
                            minY -= 100.0f;
                            maxY -= 100.0f;
                        }

                        // get all nearby vrykul
                        List<Creature> temp = new List<Creature>();
                        var check = new FrostwingVrykulSearcher<Creature>(me, 80.0f);
                        var searcher = new CreatureListSearcher(me, temp, check);
                        Cell.VisitGridObjects(me, searcher, 80.0f);

                        _aliveTrash.Clear();
                        foreach (var creature in temp)
                            if (creature.GetHomePosition().GetPositionY() < maxY && creature.GetHomePosition().GetPositionY() > minY)
                                _aliveTrash.Add(creature.GetGUID());
                        break;
                    }
                // at waypoints 1 and 2 she kills one captain
                case 2:
                    Creature svalna1 = ObjectAccessor.GetCreature(me, _instance.GetGuidData(Bosses.SisterSvalna));
                    if (svalna1)
                        svalna1.GetAI().DoAction(Actions.KillCaptain);
                    break;
                default:
                    break;
            }
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            // check wipe
            if (_wipeCheckTimer == 0)
            {
                _wipeCheckTimer = 1000;
                var check = new AnyPlayerInObjectRangeCheck(me, 60.0f);
                var searcher = new PlayerSearcher(me, check);
                Cell.VisitWorldObjects(me, searcher, 60.0f);
                // wipe
                if (!searcher.GetTarget())
                {
                    damage *= 100;
                    if (damage >= me.GetHealth())
                    {
                        FrostwingGauntletRespawner respawner = new FrostwingGauntletRespawner();
                        var worker = new CreatureWorker(me, respawner);
                        Cell.VisitGridObjects(me, worker, 333.0f);
                        Talk(Texts.SayCrokDeath);
                    }
                    return;
                }
            }

            if (HealthBelowPct(10))
            {
                if (!_didUnderTenPercentText)
                {
                    _didUnderTenPercentText = true;
                    if (_isEventActive)
                        Talk(Texts.SayCrokWeakeningGauntlet);
                    else
                        Talk(Texts.SayCrokWeakeningSvalna);
                }

                damage = 0;
                DoCast(me, InstanceSpells.IceboundArmor);
                _events.ScheduleEvent(EventTypes.HealthCheck, 1000);
            }
        }

        void UpdateEscortAI(uint diff)
        {
            if (_wipeCheckTimer <= diff)
                _wipeCheckTimer = 0;
            else
                _wipeCheckTimer -= diff;

            if (!UpdateVictim() && !_isEventActive)
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventTypes.ArnathIntro2:
                        Creature arnath = ObjectAccessor.GetCreature(me, _instance.GetGuidData(DataTypes.CaptainArnath));
                        if (arnath)
                            arnath.GetAI().Talk(Texts.SayArnathIntro2);
                        break;
                    case EventTypes.CrokIntro3:
                        Talk(Texts.SayCrokIntro3);
                        break;
                    case EventTypes.StartPathing:
                        Start(true, true);
                        break;
                    case EventTypes.ScourgeStrike:
                        DoCastVictim(InstanceSpells.ScourgeStrike);
                        _events.ScheduleEvent(EventTypes.ScourgeStrike, RandomHelper.URand(10000, 14000));
                        break;
                    case EventTypes.DeathStrike:
                        if (HealthBelowPct(20))
                            DoCastVictim(InstanceSpells.DeathStrike);
                        _events.ScheduleEvent(EventTypes.DeathStrike, RandomHelper.URand(5000, 10000));
                        break;
                    case EventTypes.HealthCheck:
                        if (HealthAbovePct(15))
                        {
                            me.RemoveAurasDueToSpell(InstanceSpells.IceboundArmor);
                            _didUnderTenPercentText = false;
                        }
                        else
                        {
                            // looks totally hacky to me
                            me.ModifyHealth((long)me.CountPctFromMaxHealth(5));
                            _events.ScheduleEvent(EventTypes.HealthCheck, 1000);
                        }
                        break;
                    default:
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }

        public override bool CanAIAttack(Unit target)
        {
            // do not see targets inside Frostwing Halls when we are not there
            return (me.GetPositionY() > 2660.0f) == (target.GetPositionY() > 2660.0f);
        }

        List<ObjectGuid> _aliveTrash = new List<ObjectGuid>();
        InstanceScript _instance;
        uint _currentWPid;
        uint _wipeCheckTimer;
        uint _respawnTime;
        uint _corpseDelay;
        bool _isEventActive;
        bool _isEventDone;
        bool _didUnderTenPercentText;
    }

    class npc_argent_captainAI : ScriptedAI
    {
        public npc_argent_captainAI(Creature creature) : base(creature)
        {
            instance = creature.GetInstanceScript();
            _firstDeath = true;
            FollowAngle = SharedConst.PetFollowAngle;
            FollowDist = SharedConst.PetFollowDist;
            IsUndead = false;
        }

        public override void JustDied(Unit killer)
        {
            if (_firstDeath)
            {
                _firstDeath = false;
                Talk(Texts.SayCaptainDeath);
            }
            else
                Talk(Texts.SayCaptainSecondDeath);
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsTypeId(TypeId.Player))
                Talk(Texts.SayCaptainKill);
        }

        public override void DoAction(int action)
        {
            if (action == Actions.StartGauntlet)
            {
                Creature crok = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.CrokScourgebane));
                if (crok)
                {
                    me.SetReactState(ReactStates.Defensive);
                    FollowAngle = me.GetAngle(crok) + me.GetOrientation();
                    FollowDist = me.GetDistance2d(crok);
                    me.GetMotionMaster().MoveFollow(crok, FollowDist, FollowAngle, MovementSlot.Idle);
                }

                me.setActive(true);
            }
            else if (action == Actions.ResetEvent)
            {
                _firstDeath = true;
            }
        }

        public override void EnterCombat(Unit target)
        {
            me.SetHomePosition(me);
            if (IsUndead)
                DoZoneInCombat();
        }

        public override bool CanAIAttack(Unit target)
        {
            // do not see targets inside Frostwing Halls when we are not there
            return (me.GetPositionY() > 2660.0f) == (target.GetPositionY() > 2660.0f);
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            // not yet following
            if (me.GetMotionMaster().GetMotionSlotType((int)MovementSlot.Idle) != MovementGeneratorType.Chase || IsUndead)
            {
                base.EnterEvadeMode(why);
                return;
            }

            if (!_EnterEvadeMode(why))
                return;

            if (!me.GetVehicle())
            {
                me.GetMotionMaster().Clear(false);
                Creature crok = ObjectAccessor.GetCreature(me, instance.GetGuidData(DataTypes.CrokScourgebane));
                if (crok)
                    me.GetMotionMaster().MoveFollow(crok, FollowDist, FollowAngle, MovementSlot.Idle);
            }

            Reset();
        }

        public override void SpellHit(Unit caster, SpellInfo spell)
        {
            if (spell.Id == InstanceSpells.ReviveChampion && !IsUndead)
            {
                IsUndead = true;
                me.setDeathState(DeathState.JustRespawned);
                uint newEntry = 0;
                switch (me.GetEntry())
                {
                    case CreatureIds.CaptainArnath:
                        newEntry = CreatureIds.CaptainArnathUndead;
                        break;
                    case CreatureIds.CaptainBrandon:
                        newEntry = CreatureIds.CaptainBrandonUndead;
                        break;
                    case CreatureIds.CaptainGrondel:
                        newEntry = CreatureIds.CaptainGrondelUndead;
                        break;
                    case CreatureIds.CaptainRupert:
                        newEntry = CreatureIds.CaptainRupertUndead;
                        break;
                    default:
                        return;
                }

                Talk(Texts.SayCaptainResurrected);
                me.UpdateEntry(newEntry, me.GetCreatureData());
                DoCast(me, InstanceSpells.Undeath, true);
            }
        }

        InstanceScript instance;
        float FollowAngle;
        float FollowDist;
        public bool IsUndead;
        bool _firstDeath;
    }

    [Script]
    class npc_captain_arnath : npc_argent_captainAI
    {
        public npc_captain_arnath(Creature creature)
            : base(creature) { }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(EventTypes.ArnathFlashHeal, RandomHelper.URand(4000, 7000));
            _events.ScheduleEvent(EventTypes.ArnathPwShield, RandomHelper.URand(8000, 14000));
            _events.ScheduleEvent(EventTypes.ArnathSmite, RandomHelper.URand(3000, 6000));
            if (Is25ManRaid() && IsUndead)
                _events.ScheduleEvent(EventTypes.ArnathDominateMind, RandomHelper.URand(22000, 27000));
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
                    case EventTypes.ArnathFlashHeal:
                        {
                            Creature target = FindFriendlyCreature();
                            if (target)
                                DoCast(target, InstanceSpells.SpellFlashHeal(IsUndead));
                            _events.ScheduleEvent(EventTypes.ArnathFlashHeal, RandomHelper.URand(6000, 9000));
                        }
                        break;
                    case EventTypes.ArnathPwShield:
                        {
                            List<Creature> targets = DoFindFriendlyMissingBuff(40.0f, InstanceSpells.SpellPowerWordShield(IsUndead));
                            DoCast(targets.SelectRandom(), InstanceSpells.SpellPowerWordShield(IsUndead));
                            _events.ScheduleEvent(EventTypes.ArnathPwShield, RandomHelper.URand(15000, 20000));
                            break;
                        }
                    case EventTypes.ArnathSmite:
                        DoCastVictim(InstanceSpells.SpellSmite(IsUndead));
                        _events.ScheduleEvent(EventTypes.ArnathSmite, RandomHelper.URand(4000, 7000));
                        break;
                    case EventTypes.ArnathDominateMind:
                        {
                            Unit target = SelectTarget(SelectAggroTarget.Random, 1, 0.0f, true);
                            if (target)
                                DoCast(target, InstanceSpells.DominateMind);
                            _events.ScheduleEvent(EventTypes.ArnathDominateMind, RandomHelper.URand(28000, 37000));
                        }
                        break;
                    default:
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }

        Creature FindFriendlyCreature()
        {
            var u_check = new MostHPMissingInRange<Creature>(me, 60.0f, 0);
            var searcher = new CreatureLastSearcher(me, u_check);
            Cell.VisitGridObjects(me, searcher, 60.0f);
            return searcher.GetTarget();
        }
    }

    [Script]
    class npc_captain_brandon : npc_argent_captainAI
    {
        public npc_captain_brandon(Creature creature)
            : base(creature) { }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(EventTypes.BrandonCrusaderStrike, RandomHelper.URand(6000, 10000));
            _events.ScheduleEvent(EventTypes.BrandonDivineShield, 500);
            _events.ScheduleEvent(EventTypes.BrandonJudgementOfCommand, RandomHelper.URand(8000, 13000));
            if (IsUndead)
                _events.ScheduleEvent(EventTypes.BrandonHammerOfBetrayal, RandomHelper.URand(25000, 30000));
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
                    case EventTypes.BrandonCrusaderStrike:
                        DoCastVictim(InstanceSpells.CrusaderStrike);
                        _events.ScheduleEvent(EventTypes.BrandonCrusaderStrike, RandomHelper.URand(6000, 12000));
                        break;
                    case EventTypes.BrandonDivineShield:
                        if (HealthBelowPct(20))
                            DoCast(me, InstanceSpells.DivineShield);
                        _events.ScheduleEvent(EventTypes.BrandonDivineShield, 500);
                        break;
                    case EventTypes.BrandonJudgementOfCommand:
                        DoCastVictim(InstanceSpells.JudgementOfCommand);
                        _events.ScheduleEvent(EventTypes.BrandonJudgementOfCommand, RandomHelper.URand(8000, 13000));
                        break;
                    case EventTypes.BrandonHammerOfBetrayal:
                        Unit target = SelectTarget(SelectAggroTarget.Random, 1, 0.0f, true);
                        if (target)
                            DoCast(target, InstanceSpells.HammerOfBetrayal);
                        _events.ScheduleEvent(EventTypes.BrandonHammerOfBetrayal, RandomHelper.URand(45000, 60000));
                        break;
                    default:
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_captain_grondel : npc_argent_captainAI
    {
        public npc_captain_grondel(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(EventTypes.GrondelChargeCheck, 500);
            _events.ScheduleEvent(EventTypes.GrondelMortalStrike, RandomHelper.URand(8000, 14000));
            _events.ScheduleEvent(EventTypes.GrondelSunderArmor, RandomHelper.URand(3000, 12000));
            if (IsUndead)
                _events.ScheduleEvent(EventTypes.GrondelConflagration, RandomHelper.URand(12000, 17000));
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
                    case EventTypes.GrondelChargeCheck:
                        DoCastVictim(InstanceSpells.Charge);
                        _events.ScheduleEvent(EventTypes.GrondelChargeCheck, 500);
                        break;
                    case EventTypes.GrondelMortalStrike:
                        DoCastVictim(InstanceSpells.MortalStrike);
                        _events.ScheduleEvent(EventTypes.GrondelMortalStrike, RandomHelper.URand(10000, 15000));
                        break;
                    case EventTypes.GrondelSunderArmor:
                        DoCastVictim(InstanceSpells.SunderArmor);
                        _events.ScheduleEvent(EventTypes.GrondelSunderArmor, RandomHelper.URand(5000, 17000));
                        break;
                    case EventTypes.GrondelConflagration:
                        Unit target = SelectTarget(SelectAggroTarget.Random, 0, 0.0f, true);
                        if (target)
                            DoCast(target, InstanceSpells.Conflagration);
                        _events.ScheduleEvent(EventTypes.GrondelConflagration, RandomHelper.URand(10000, 15000));
                        break;
                    default:
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_captain_rupert : npc_argent_captainAI
    {
        public npc_captain_rupert(Creature creature)
            : base(creature)
        {
        }

        public override void Reset()
        {
            _events.Reset();
            _events.ScheduleEvent(EventTypes.RupertFelIronBomb, RandomHelper.URand(15000, 20000));
            _events.ScheduleEvent(EventTypes.RupertMachineGun, RandomHelper.URand(25000, 30000));
            _events.ScheduleEvent(EventTypes.RupertRocketLaunch, RandomHelper.URand(10000, 15000));
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
                Unit target;
                switch (eventId)
                {
                    case EventTypes.RupertFelIronBomb:
                        target = SelectTarget(SelectAggroTarget.Random, 0);
                        if (target)
                            DoCast(target, InstanceSpells.SpellFelIronBomb(IsUndead));
                        _events.ScheduleEvent(EventTypes.RupertFelIronBomb, RandomHelper.URand(15000, 20000));
                        break;
                    case EventTypes.RupertMachineGun:
                        target = SelectTarget(SelectAggroTarget.Random, 1);
                        if (target)
                            DoCast(target, InstanceSpells.SpellMachineGun(IsUndead));
                        _events.ScheduleEvent(EventTypes.RupertMachineGun, RandomHelper.URand(25000, 30000));
                        break;
                    case EventTypes.RupertRocketLaunch:
                        target = SelectTarget(SelectAggroTarget.Random, 1);
                        if (target)
                            DoCast(target, InstanceSpells.SpellRocketLaunch(IsUndead));
                        _events.ScheduleEvent(EventTypes.RupertRocketLaunch, RandomHelper.URand(10000, 15000));
                        break;
                    default:
                        break;
                }
            });

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    class npc_frostwing_vrykul : SmartAI
    {
        public npc_frostwing_vrykul(Creature creature)
            : base(creature) { }

        public override bool CanAIAttack(Unit target)
        {
            // do not see targets inside Frostwing Halls when we are not there
            return (me.GetPositionY() > 2660.0f) == (target.GetPositionY() > 2660.0f) && base.CanAIAttack(target);
        }
    }

    [Script]
    class npc_impaling_spear : CreatureAI
    {
        public npc_impaling_spear(Creature creature)
            : base(creature)
        {
        }

        public override void Reset()
        {
            me.SetReactState(ReactStates.Passive);
            _vehicleCheckTimer = 500;
        }

        public override void UpdateAI(uint diff)
        {
            if (_vehicleCheckTimer <= diff)
            {
                _vehicleCheckTimer = 500;
                if (!me.GetVehicle())
                    me.DespawnOrUnsummon(100);
            }
            else
                _vehicleCheckTimer -= diff;
        }

        uint _vehicleCheckTimer;
    }

    [Script]
    class npc_arthas_teleport_visual : NullCreatureAI
    {
        public npc_arthas_teleport_visual(Creature creature)
            : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void Reset()
        {
            _events.Reset();
            if (_instance.GetBossState(Bosses.ProfessorPutricide) == EncounterState.Done &&
                _instance.GetBossState(Bosses.BloodQueenLanaThel) == EncounterState.Done &&
                _instance.GetBossState(Bosses.Sindragosa) == EncounterState.Done)
                _events.ScheduleEvent(EventTypes.SoulMissile, RandomHelper.URand(1000, 6000));
        }

        void Update(uint diff)
        {
            if (_events.Empty())
                return;

            _events.Update(diff);

            if (_events.ExecuteEvent() == EventTypes.SoulMissile)
            {
                DoCastAOE(InstanceSpells.SoulMissile);
                _events.ScheduleEvent(EventTypes.SoulMissile, RandomHelper.URand(5000, 7000));
            }
        }

        InstanceScript _instance;
    }

    [Script]
    class spell_icc_stoneform : AuraScript
    {
        void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Creature target = GetTarget().ToCreature();
            if (target)
            {
                target.SetReactState(ReactStates.Passive);
                target.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.ImmuneToPc);
                target.SetUInt32Value(UnitFields.NpcEmotestate, (uint)Emote.OneshotCustomSpell02);
            }
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Creature target = GetTarget().ToCreature();
            if (target)
            {
                target.SetReactState(ReactStates.Aggressive);
                target.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable | UnitFlags.ImmuneToPc);
                target.SetUInt32Value(UnitFields.NpcEmotestate, 0);
            }
        }

        public override void Register()
        {
            OnEffectApply.Add(new EffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
            OnEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_icc_sprit_alarm : SpellScript
    {
        public const int AwakenWard1 = 22900;
        public const int AwakenWard2 = 22907;
        public const int AwakenWard3 = 22908;
        public const int AwakenWard4 = 22909;

        void HandleEvent(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            uint trapId = 0;
            switch (GetSpellInfo().GetEffect(effIndex).MiscValue)
            {
                case AwakenWard1:
                    trapId = GameObjectIds.SpiritAlarm1;
                    break;
                case AwakenWard2:
                    trapId = GameObjectIds.SpiritAlarm2;
                    break;
                case AwakenWard3:
                    trapId = GameObjectIds.SpiritAlarm3;
                    break;
                case AwakenWard4:
                    trapId = GameObjectIds.SpiritAlarm4;
                    break;
                default:
                    return;
            }

            GameObject trap = GetCaster().FindNearestGameObject(trapId, 5.0f);
            if (trap)
                trap.SetRespawnTime((int)trap.GetGoInfo().GetAutoCloseTime());

            List<Creature> wards = new List<Creature>();
            GetCaster().GetCreatureListWithEntryInGrid(wards, CreatureIds.DeathboundWard, 150.0f);
            wards.Sort(new ObjectDistanceOrderPred(GetCaster()));
            foreach (var creature in wards)
            {
                if (creature.IsAlive() && creature.HasAura(InstanceSpells.StoneForm))
                {
                    creature.GetAI().Talk(Texts.SayTrapActivate);
                    creature.RemoveAurasDueToSpell(InstanceSpells.StoneForm);
                    Unit target = creature.SelectNearestTarget(150.0f);
                    if (target)
                        creature.GetAI().AttackStart(target);

                    break;
                }
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleEvent, 2, SpellEffectName.SendEvent));
        }
    }

    [Script]
    class spell_frost_giant_death_plague : SpellScript
    {
        public override bool Load()
        {
            _failed = false;
            return true;
        }

        // First effect
        void CountTargets(List<WorldObject> targets)
        {
            targets.Remove(GetCaster());
            _failed = targets.Empty();
        }

        // Second effect
        void FilterTargets(List<WorldObject> targets)
        {
            // Select valid targets for jump
            targets.RemoveAll(obj =>
            {
                if (obj == GetCaster())
                    return true;

                if (!obj.IsTypeId(TypeId.Player))
                    return true;

                if (obj.ToUnit().HasAura(InstanceSpells.RecentlyInfected) || obj.ToUnit().HasAura(InstanceSpells.DeathPlagueAura))
                    return true;

                return false;
            });

            if (!targets.Empty())
            {
                WorldObject target = targets.SelectRandom();
                targets.Clear();
                targets.Add(target);
            }

            targets.Add(GetCaster());
        }

        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            if (GetHitUnit() != GetCaster())
                GetCaster().CastSpell(GetHitUnit(), InstanceSpells.DeathPlagueAura, true);
            else if (_failed)
                GetCaster().CastSpell(GetCaster(), InstanceSpells.DeathPlagueKill, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(CountTargets, 0, Targets.UnitSrcAreaAlly));
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitSrcAreaAlly));
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect));
        }

        bool _failed;
    }

    [Script]
    class spell_icc_harvest_blight_specimen : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            GetHitUnit().RemoveAurasDueToSpell((uint)GetEffectValue());
        }

        void HandleQuestComplete(uint effIndex)
        {
            GetHitUnit().RemoveAurasDueToSpell((uint)GetEffectValue());
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
            OnEffectHitTarget.Add(new EffectHandler(HandleQuestComplete, 1, SpellEffectName.QuestComplete));
        }
    }

    [Script]
    class spell_svalna_revive_champion : SpellScript
    {
        void RemoveAliveTarget(List<WorldObject> targets)
        {
            targets.RemoveAll(obj =>
            {
                Unit unit = obj.ToUnit();
                if (unit)
                    return unit.IsAlive();

                return true;
            });

            var newTargets = targets.SelectRandom(2);
            targets.Clear();

            targets.AddRange(newTargets);
        }

        void Land(uint effIndex)
        {
            Creature caster = GetCaster().ToCreature();
            if (!caster)
                return;

            Position pos = caster.GetNearPosition(5.0f, 0.0f);
            caster.SetHomePosition(pos);
            caster.GetMotionMaster().MoveLand(1, pos);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(RemoveAliveTarget, 0, Targets.UnitDestAreaEntry));
            OnEffectHit.Add(new EffectHandler(Land, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_svalna_remove_spear : SpellScript
    {
        void HandleScript(uint effIndex)
        {
            PreventHitDefaultEffect(effIndex);
            Creature target = GetHitCreature();
            if (target)
            {
                Unit vehicle = target.GetVehicleBase();
                if (vehicle)
                    vehicle.RemoveAurasDueToSpell(InstanceSpells.ImpalingSpear);
                target.DespawnOrUnsummon(1);
            }
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    // 72585 - Soul Missile
    [Script]
    class spell_icc_soul_missile : SpellScript
    {
        void RelocateDest(ref SpellDestination dest)
        {
            Position offset = new Position(0.0f, 0.0f, 200.0f, 0.0f);
            dest.RelocateOffset(offset);
        }

        public override void Register()
        {
            OnDestinationTargetSelect.Add(new DestinationTargetSelectHandler(RelocateDest, 0, Targets.DestCaster));
        }
    }

    [Script]
    class at_icc_saurfang_portal : AreaTriggerScript
    {
        public at_icc_saurfang_portal() : base("at_icc_saurfang_portal") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            InstanceScript instance = player.GetInstanceScript();
            if (instance == null || instance.GetBossState(Bosses.DeathbringerSaurfang) != EncounterState.Done)
                return true;

            player.TeleportTo(631, 4126.35f, 2769.23f, 350.963f, 0.0f);

            if (instance.GetData(DataTypes.ColdflameJets) == (uint)EncounterState.NotStarted)
            {
                // Process relocation now, to preload the grid and initialize traps
                player.GetMap().PlayerRelocation(player, 4126.35f, 2769.23f, 350.963f, 0.0f);

                instance.SetData(DataTypes.ColdflameJets, (uint)EncounterState.InProgress);
                List<Creature> traps = new List<Creature>();
                player.GetCreatureListWithEntryInGrid(traps, CreatureIds.FrostFreezeTrap, 120.0f);
                traps.Sort(new ObjectDistanceOrderPred(player));
                bool instant = false;
                foreach (var creature in traps)
                {
                    creature.GetAI().DoAction(instant ? 1000 : 11000);
                    instant = !instant;
                }
            }

            return true;
        }
    }

    [Script]
    class at_icc_shutdown_traps : AreaTriggerScript
    {
        public at_icc_shutdown_traps() : base("at_icc_shutdown_traps") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            InstanceScript instance = player.GetInstanceScript();
            if (instance != null)
                instance.SetData(DataTypes.UpperSpireTeleAct, (uint)EncounterState.Done);

            return true;
        }
    }

    [Script]
    class at_icc_start_blood_quickening : AreaTriggerScript
    {
        public at_icc_start_blood_quickening() : base("at_icc_start_blood_quickening") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            InstanceScript instance = player.GetInstanceScript();
            if (instance != null)
                if (instance.GetData(DataTypes.BloodQuickeningState) == (uint)EncounterState.NotStarted)
                    instance.SetData(DataTypes.BloodQuickeningState, (uint)EncounterState.InProgress);
            return true;
        }
    }

    [Script]
    class at_icc_start_frostwing_gauntlet : AreaTriggerScript
    {
        public at_icc_start_frostwing_gauntlet() : base("at_icc_start_frostwing_gauntlet") { }

        public override bool OnTrigger(Player player, AreaTriggerRecord areaTrigger, bool entered)
        {
            InstanceScript instance = player.GetInstanceScript();
            if (instance != null)
            {
                Creature crok = ObjectAccessor.GetCreature(player, instance.GetGuidData(DataTypes.CrokScourgebane));
                if (crok)
                    crok.GetAI().DoAction(Actions.StartGauntlet);
            }
            return true;
        }
    }

    [Script("spell_svalna_caress_of_death", 70196u)]
    class spell_trigger_spell_from_caster : SpellScript
    {
        public spell_trigger_spell_from_caster(uint triggerId)
        {
            _triggerId = triggerId;
        }

        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(_triggerId);
        }

        void HandleTrigger()
        {
            GetCaster().CastSpell(GetHitUnit(), _triggerId, true);
        }

        public override void Register()
        {
            AfterHit.Add(new HitHandler(HandleTrigger));
        }

        uint _triggerId;
    }
}
