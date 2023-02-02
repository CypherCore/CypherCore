using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.AI;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;


using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.Bosses
{
    [CreatureScript(47626)]
    public class boss_admiral_ripsnarl : BossAI
    {
        public static readonly Position CookieSpawn = new Position(-88.1319f, -819.33f, 39.23453f, 0.0f);

        public static readonly Position[] VaporFinalSpawn =
        {
            new Position(-70.59f, -820.57f, 40.56f, 6.28f),
            new Position(-55.73f, -815.84f, 41.97f, 3.85f),
            new Position(-55.73f, -825.54f, 41.99f, 2.60f)
        };

        public struct eSpells
        {
            public const uint SPELL_GO_FOR_THE_THROAT = 88836;
            public const uint SPELL_GO_FOR_THE_THROAT_H = 91863;
            public const uint SPELL_SWIPE = 88839;
            public const uint SPELL_SWIPE_H = 91859;
            public const uint SPELL_THIRST_FOR_BLOOD = 88736;
            public const uint SPELL_STEAM_AURA = 95503;
            public const uint SPELL_FOG_AURA = 89247;
            public const uint SPELL_BUNNY_AURA = 88755;
            public const uint SPELL_FOG = 88768;
            public const uint SPELL_SUMMON_VAPOR = 88831;
            public const uint SPELL_CONDENSE = 92016;
            public const uint SPELL_CONDENSE_2 = 92020;
            public const uint SPELL_CONDENSE_3 = 92029;
            public const uint SPELL_CONDENSATION = 92013;
            public const uint SPELL_FREEZING_VAPOR = 92011;
            public const uint SPELL_COALESCE = 92042;
            public const uint SPELL_SWIRLING_VAPOR = 92007;
            public const uint SPELL_CONDENSING_VAPOR = 92008;
        }

        public struct eAchievementMisc
        {
            public const uint ACHIEVEMENT_ITS_FROST_DAMAGE = 5369;
            public const uint VAPOR_CASTED_COALESCE = 1;
        }

        public struct AdmiralPhases
        {
            public const uint PHASE_NORMAL = 1;
            public const uint PHASE_FOG = 2;
        }

        public struct BossEvents
        {
            public const uint EVENT_NULL = 0;
            public const uint EVENT_SWIPE = 1;
            public const uint EVENT_FLEE_TO_FROG = 2;
            public const uint EVENT_SUMMON_VAPOR = 3;
            public const uint EVENT_PHASE_TWO = 4;
            public const uint EVENT_UPDATE_FOG = 5;
            public const uint EVENT_GO_FOR_THROAT = 6;
            public const uint EVENT_THIRST_FOR_BLOOD = 7;
            public const uint EVENT_SHOW_UP = 8;
        }

        public struct Says
        {
            public const uint SAY_DEATH = 0;
            public const uint SAY_KILL = 1;
            public const uint SAY_FOG_1 = 2;
            public const uint SAY_FOG_2 = 3;
            public const uint SAY_SPELL_1 = 4;
            public const uint SAY_SPELL_2 = 5;
            public const uint SAY_AUUUU = 6;
            public const uint SAY_AGGRO = 7;
        }

        public boss_admiral_ripsnarl(Creature creature) : base(creature, DMData.DATA_RIPSNARL)
        {
        }

        private uint _eventId;
        private byte _vaporCount;
        private uint _phase;
        private uint _numberCastCoalesce;

        private bool _below_10;
        private bool _below_25;
        private bool _below_50;
        private bool _below_75;

        public override void Reset()
        {
            if (!me)
            {
                return;
            }

            _Reset();
            summons.DespawnAll();
            _events.Reset();
            _vaporCount = 0;
            me.SetFullHealth();
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
            RemoveAuraFromMap();
            SetFog(false);

            _below_10 = false;
            _below_25 = false;
            _below_50 = false;
            _below_75 = false;
            _numberCastCoalesce = 0;
            _phase = AdmiralPhases.PHASE_NORMAL;
        }

        public override void JustEnteredCombat(Unit who)
        {
            if (!me)
            {
                return;
            }

            base.JustEnteredCombat(who);
            Talk(Says.SAY_AGGRO);
            instance.SendEncounterUnit(EncounterFrameType.Engage, me);

            _events.ScheduleEvent(BossEvents.EVENT_THIRST_FOR_BLOOD, TimeSpan.FromMilliseconds(0));
            _events.ScheduleEvent(BossEvents.EVENT_SWIPE, TimeSpan.FromMilliseconds(10000));

            if (IsHeroic())
            {
                _events.ScheduleEvent(BossEvents.EVENT_GO_FOR_THROAT, TimeSpan.FromMilliseconds(10000));
            }
        }

        public override void JustSummoned(Creature summoned)
        {
            if (summoned.GetAI() != null)
            {
                summoned.GetAI().AttackStart(SelectTarget(SelectTargetMethod.Random));
            }

            summons.Summon(summoned);
        }

        public override void JustReachedHome()
        {
            if (!me)
            {
                return;
            }

            base.JustReachedHome();
            Talk(Says.SAY_KILL);
            RemoveAuraFromMap();
        }

        public override void SummonedCreatureDespawn(Creature summon)
        {
            summons.Despawn(summon);
        }

        public override void JustDied(Unit killer)
        {
            if (!me)
            {
                return;
            }

            base.JustDied(killer);
            summons.DespawnAll();
            Talk(Says.SAY_DEATH);
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
            RemoveAuraFromMap();
            RemoveFog();
            me.SummonCreature(DMCreatures.NPC_CAPTAIN_COOKIE, CookieSpawn);
        }

        public override void SetData(uint uiI, uint uiValue)
        {
            if (uiValue == eAchievementMisc.VAPOR_CASTED_COALESCE && _numberCastCoalesce < 3)
            {
                _numberCastCoalesce++;

                if (_numberCastCoalesce >= 3)
                {
                    Map map = me.GetMap();
                    AchievementRecord its_frost_damage = Global.AchievementMgr.GetAchievementByReferencedId(eAchievementMisc.ACHIEVEMENT_ITS_FROST_DAMAGE).FirstOrDefault();
                    
                    if (map != null && map.IsDungeon() && map.GetDifficultyID() == Difficulty.Heroic)
                    {
                        var players = map.GetPlayers();
                        if (!players.Empty())
                        {
                            foreach (var player in map.GetPlayers())
                            {
                                if (player != null)
                                {
                                    if (player.GetDistance(me) < 300.0f)
                                    {
                                        player.CompletedAchievement(its_frost_damage);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public void VaporsKilled()
        {
            _vaporCount++;

            if (_vaporCount == 4)
            {
                _events.ScheduleEvent(BossEvents.EVENT_SHOW_UP, TimeSpan.FromMilliseconds(1000));
            }
        }

        public void SetFog(bool apply)
        {
            if (!me)
            {
                return;
            }

            _phase = AdmiralPhases.PHASE_FOG;
            var creature_list = me.GetCreatureListWithEntryInGrid(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF2, 100.0f);

            creature_list.Sort(new ObjectDistanceOrderPred(me));
            foreach (var item in creature_list)
            {
                if (item != null && item.IsAlive() && item.GetTypeId() == TypeId.Unit)
                {
                    Creature bunny = item.ToCreature();
                    if (bunny != null)
                    {
                        if (apply)
                        {
                            bunny.AddAura(eSpells.SPELL_FOG, bunny);
                        }
                        else
                        {
                            bunny.RemoveAura(eSpells.SPELL_FOG);
                        }
                    }
                }
            }
            return;
        }

        public void RemoveFog()
        {
            _phase = AdmiralPhases.PHASE_NORMAL;
            List<Unit> players = new List<Unit>();

            AnyPlayerInObjectRangeCheck checker = new AnyPlayerInObjectRangeCheck(me, 150.0f);
            PlayerListSearcher searcher = new PlayerListSearcher(me, players, checker);
            Cell.VisitWorldObjects(me, searcher, 150f);

            foreach (var item in players)
            {
                item.RemoveAurasDueToSpell(eSpells.SPELL_FOG_AURA);
            }

        }

        public void RemoveAuraFromMap()
        {
            if (!me)
            {
                return;
            }

            SetFog(false);

            Creature bunny = me.FindNearestCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, 20.0f);
            if (bunny != null)
            {
                bunny.DespawnOrUnsummon();
            }
        }

        public void SummonFinalVapors()
        {
            for (byte i = 0; i < 3; ++i)
            {
                me.SummonCreature(DMCreatures.NPC_VAPOR, VaporFinalSpawn[i], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
            }
        }

        public override void UpdateAI(uint uiDiff)
        {
            if (!me || instance != null)
            {
                return;
            }

            if (!UpdateVictim())
            {
                return;
            }

            DoMeleeAttackIfReady();

            _events.Update(uiDiff);

            if (me.GetHealthPct() < 75 && !_below_75)
            {
                Talk(Says.SAY_FOG_1);
                Creature bunny = me.SummonCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, me.GetPositionX(), me.GetPositionY(), me.GetPositionZ());
                if (bunny != null)
                {
                    bunny.AddAura(eSpells.SPELL_BUNNY_AURA, bunny);
                    bunny.AddAura(eSpells.SPELL_FOG_AURA, bunny);
                }
                SetFog(true);
                _events.ScheduleEvent(BossEvents.EVENT_PHASE_TWO, TimeSpan.FromMilliseconds(1000));
                _events.ScheduleEvent(BossEvents.EVENT_UPDATE_FOG, TimeSpan.FromMilliseconds(100));
                _below_75 = true;
            }
            else if (me.GetHealthPct() < 50 && !_below_50)
            {
                Talk(Says.SAY_FOG_1);
                _events.ScheduleEvent(BossEvents.EVENT_PHASE_TWO, TimeSpan.FromMilliseconds(500));
                _below_50 = true;
            }
            else if (me.GetHealthPct() < 25 && !_below_25)
            {
                Talk(Says.SAY_FOG_1);
                _events.ScheduleEvent(BossEvents.EVENT_PHASE_TWO, TimeSpan.FromMilliseconds(500));
                _below_25 = true;
            }
            else if (me.GetHealthPct() < 10 && !_below_10)
            {
                if (IsHeroic())
                {
                    SummonFinalVapors();
                    _below_10 = true;
                }
            }

            uint eventId;
            while ((eventId = _events.ExecuteEvent()) != 0)
            {
                switch (eventId)
                {
                    case BossEvents.EVENT_SWIPE:
                        Unit victim = me.GetVictim();
                        if (victim != null)
                        {
                            me.CastSpell(victim, IsHeroic() ? eSpells.SPELL_SWIPE_H : eSpells.SPELL_SWIPE);
                        }
                        _events.ScheduleEvent(BossEvents.EVENT_SWIPE, TimeSpan.FromMilliseconds(3000));
                        break;

                    case BossEvents.EVENT_UPDATE_FOG:
                        instance.DoCastSpellOnPlayers(eSpells.SPELL_FOG_AURA);
                        break;

                    case BossEvents.EVENT_GO_FOR_THROAT:
                        Unit target = SelectTarget(SelectTargetMethod.Random, 1, 100, true);
                        if (target != null)
                        {
                            DoCast(target, eSpells.SPELL_GO_FOR_THE_THROAT);
                        }
                        _events.ScheduleEvent(BossEvents.EVENT_GO_FOR_THROAT, TimeSpan.FromMilliseconds(10000));
                        break;

                    case BossEvents.EVENT_THIRST_FOR_BLOOD:
                        DoCast(me, eSpells.SPELL_THIRST_FOR_BLOOD);
                        break;

                    case BossEvents.EVENT_PHASE_TWO:
                        _events.CancelEvent(BossEvents.EVENT_GO_FOR_THROAT);
                        _events.CancelEvent(BossEvents.EVENT_SWIPE);
                        me.RemoveAurasDueToSpell(eSpells.SPELL_THIRST_FOR_BLOOD);
                        me.SetVisible(false);
                        _events.ScheduleEvent(BossEvents.EVENT_FLEE_TO_FROG, TimeSpan.FromMilliseconds(100));

                        if (_vaporCount > 0)
                        {
                            Talk(Says.SAY_FOG_2);
                        }
                        else
                        {
                            Unit victim2 = me.GetVictim();
                            if (victim2 != null)
                            {
                                Talk(Says.SAY_SPELL_1);
                                me.CastSpell(victim2, eSpells.SPELL_GO_FOR_THE_THROAT);
                            }
                        }
                        break;

                    case BossEvents.EVENT_FLEE_TO_FROG:
                        me.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible | UnitFlags.Pacified);
                        me.DoFleeToGetAssistance();
                        Talk(Says.SAY_AUUUU);
                        _events.RescheduleEvent(BossEvents.EVENT_SUMMON_VAPOR, TimeSpan.FromMilliseconds(1000));
                        _events.ScheduleEvent(BossEvents.EVENT_SHOW_UP, TimeSpan.FromMilliseconds(25000));
                        break;

                    case BossEvents.EVENT_SHOW_UP:
                        me.SetVisible(true);
                        _vaporCount = 0;
                        me.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible | UnitFlags.Pacified);
                        _events.ScheduleEvent(BossEvents.EVENT_SWIPE, TimeSpan.FromMilliseconds(1000));
                        _events.ScheduleEvent(BossEvents.EVENT_GO_FOR_THROAT, TimeSpan.FromMilliseconds(3000));
                        _events.ScheduleEvent(BossEvents.EVENT_THIRST_FOR_BLOOD, TimeSpan.FromMilliseconds(0));
                        break;

                    case BossEvents.EVENT_SUMMON_VAPOR:
                        if (_phase == AdmiralPhases.PHASE_FOG)
                        {
                            Unit target1 = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                            if (target1 != null)
                            {
                                me.CastSpell(target1, eSpells.SPELL_SUMMON_VAPOR);
                            }
                        }
                        _events.RescheduleEvent(BossEvents.EVENT_SUMMON_VAPOR, TimeSpan.FromMilliseconds(3500));
                        break;
                }
                eventId = _events.ExecuteEvent();
            }
        }
    }
}