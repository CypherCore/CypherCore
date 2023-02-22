// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Deadmines.Bosses
{
    [CreatureScript(DMCreatures.NPC_VANESSA_BOSS)]
    public class boss_vanessa_vancleef : BossAI
    {
        public const string COMBAT_START = "I will not share my father''s fate!  Your tale ends here!";
        public const string FOOLS_BOMB = "Fools! This entire ship is rigged with explosives! Enjoy your fiery deaths!";
        public const string HUGH_BOMB = "Vanessa pulls out a final barrel of mining powder and ignites it! RUN!";
        public const string VANESSA_DETONATE = "Vannesa has detonated charges on the ship! Get to the ropes at the side of the boat!";
        public const string TEXT_INFO = "A shadowy figure appears in the ship's cabine!";
        public const string TEXT_INFO_2 = "Vanessa injects you with the Nightmare Elixir!";
        public const string TEXT_INFO_3 = "Active the steam valves to free yourself!";
        public const string TEXT_INFO_4 = "The Nightmare Elixir takes hold!";
        public const string TEXT_INFO_5 = "You have entered Glubtok's Nightmare!";
        public const string VANESSA_SAY_1 = "I've been waiting a long time for this you know.";
        public const string VANESSA_SAY_2 = "Biding my time , building my forces, studing the minds of my enemies.";
        public const string VANESSA_SAY_3 = "I was never very good at hand-to-hand combat, you know. Not like my father.";
        public const string VANESSA_SAY_4 = "But I always excelled at poison.";
        public const string VANESSA_SAY_5 = "Especially venoms that affect the mind.";
        public const string INTRUDER_SAY = "Continue reading... <Note: This will alert Vanessa to your presence!>";
        public const string VANESSA_GLUB_1 = "Poor Glubtok. When his powers manifested his own ogre mound was the first to burn.";
        public const string VANESSA_GLUB_2 = "Deep within his soul the thing he feared most of all was.. himself!";
        public const string VANESSA_HELIX_1 = "Most rogues prefer to cloak themselves in the shadows, but not Helix.";
        public const string VANESSA_HELIX_2 = "You never know what skitters in the darkness.";
        public const string VANESSA_MECHANICAL_1 = "Can you imagine the life of a machine?";
        public const string VANESSA_MECHANICAL_2 = "A simple spark can mean the difference between life... and death.";
        public const string VANESSA_RIPSNARL_1 = "Ripsnarl wasn't always a bloodthirsty savage. Once, he even had a family.";
        public const string VANESSA_RIPSNARL_2 = "He was called James Harringtion. A tragedy in three parts.";
        public const string VANESSA_NIGHTMARE_1 = "You hear a voice from above the cabin door.";
        public const string VANESSA_NIGHTMARE_2 = "Vanessa injects you with the Nightmare Elixir!";
        public const string VANESSA_NIGHTMARE_3 = "You have entered Glubtok's Nightmare!";
        public const string VANESSA_NIGHTMARE_4 = "Get back to the ship!";
        public const string VANESSA_NIGHTMARE_5 = "You have entered Helix's nightmare!";
        public const string VANESSA_NIGHTMARE_6 = "The Nightmare Elixir takes hold!";
        public const string VANESSA_NIGHTMARE_7 = "Nightmare spiders appear in the darkness! Kill Helix before his nightmare overhelms you!";
        public const string VANESSA_NIGHTMARE_8 = "You have entered the mechanical nightmare!";
        public const string VANESSA_NIGHTMARE_9 = "You have entered Ripsnarl's nightmare!";
        public const string VANESSA_NIGHTMARE_10 = "Save Emme Harrington!";
        public const string VANESSA_NIGHTMARE_11 = "Save Erik Harrington!";
        public const string VANESSA_NIGHTMARE_12 = "Save Calissa Harrington!";
        public const string VANESSA_NIGHTMARE_13 = "The Nightmare Elixir wears off!";
        public const string VANESSA_NIGHTMARE_14 = "The nightmare shifts!";

        public static readonly Position[] RopeSpawn =
        {
            new Position(-64.01f, -839.84f, 41.22f, 0),
            new Position(-66.82f, -839.92f, 40.97f, 0),
            new Position(-69.75f, -839.87f, 40.71f, 0),
            new Position(-72.32f, -839.71f, 40.48f, 0),
            new Position(-75.76f, -839.33f, 40.18f, 0)
        };

        public static readonly Position[] Shadowspawn =
        {
            new Position(-74.61f, -822.91f, 40.22f, 6.24f),
            new Position(-74.98f, -816.88f, 40.18f, 6.24f),
            new Position(-76.12f, -819.95f, 40.08f, 6.24f)
        };


        public boss_vanessa_vancleef(Creature creature) : base(creature, DMData.DATA_VANESSA)
        {
            
        }

        public struct Spells
        {
            public const uint BACKSLASH = 92619;
            public const uint DEFLECTION = 92614;
            public const uint DEADLY_BLADES = 92622;
            public const uint VENGEANCE = 95542;
            public const uint POWDER_EXP = 96283;
            public const uint FIERY_BLAZE = 93484;
            public const uint FIERY_BLAZE_DMG = 93485;
            public const uint CLICK_ME = 95527;
            // misc
            public const uint EVASION = 90958;
            public const uint SHADOWSTEP = 90956;
            public const uint SINISTER = 90951;
            public const uint BLADES = 90960;
            public const uint CHARGE = 90928;
            public const uint RECKLESSNESS = 90929;
            public const uint BLOODBATH = 90925;
            public const uint MOTIVATE = 91036;
            public const uint THREATENING = 91034;
            public const uint UPPERCUT = 91045;
            public const uint AXE_HEAD = 90098;
            public const uint ENRAGE = 8599;
            public const uint BLOODBOLT = 90938;
            public const uint BLOODWASH = 90946;
            public const uint RAGEZONE = 90932;
            //envocer
            public const uint HOLY_FIRE = 91004;
            public const uint RENEGADE = 90047;
            public const uint SHIELD = 92001;
            // Vanessa event
            public const uint SITTING = 89279;
            public const uint NOXIOUS_CONCOCTION = 92100;
            public const uint BLACKOUT = 92120;
            public const uint RIDE_VEHICLE = 46598;

            //1 Nightmare
            public const uint EFFECT_1 = 92563;
            public const uint ICYCLE = 92189;
            public const uint SPIRIT_STRIKE = 59304;
            public const uint SPRINT = 92604;
        }

        public struct eAchievementMisc
        {
            public const uint ACHIEVEMENT_VIGOROUS_VANCLEEF_VINDICATOR = 5371;
            public const uint NPC_ACHIEVEMENT_CONTROLLER = 51624;
            public const uint START_TIMER_ACHIEVEMENT = 1;
            public const uint ACHIEVEMENT_READY_GET = 2;
        }

        public struct BossEvents
        {
            public const uint EVENT_BACKSLASH = 1;
            public const uint EVENT_VENGEANCE = 2;
            public const uint EVENT_DEFLECTION = 3;
            public const uint EVENT_DEADLY_BLADES = 4;
            public const uint EVENT_POWDER_EXP = 5;
            public const uint EVENT_FIERY_BLAZE = 6;
            public const uint EVENT_SHADOWGUARD = 7;
            public const uint EVENT_SUMMON_ROPE = 8;
            public const uint EVENT_ROPE_READY = 9;
            public const uint EVENT_DISSAPEAR = 10;
            public const uint EVENT_FIRE_BOOM = 11;
            public const uint EVENT_CLEAR_SHIP = 12;
            public const uint EVENT_SUMMON_ADD_1 = 13;
            public const uint EVENT_SUMMON_ADD_2 = 14;
            public const uint EVENT_SUMMON_ADD_3 = 15;
            public const uint EVENT_FINAL_TIMER = 16;
            public const uint EVENT_ICYCLE_AOE = 1;
            public const uint EVENT_SPIRIT_STRIKE = 17;
            public const uint EVENT_SPIDERS = 1;
        }

        public struct Actions
        {
            public const uint ACTION_EJECT_ALL = 1;
        }


        public bool Under;
        public bool Under2;
        public bool Killed;

        public uint RemoveAurasTimer;

        public Player PlayerGUID;

        public override void Reset()
        {
            Under = false;
            Under2 = false;
            Killed = false;
            _Reset();
            RemoveAurasTimer = 0;
            me.SetReactState(ReactStates.Passive);
        }

        public override void JustEnteredCombat(Unit who)
        {
            Creature controller_achi = me.FindNearestCreature(eAchievementMisc.NPC_ACHIEVEMENT_CONTROLLER, 300.0f);
            if (controller_achi != null)
            {
                controller_achi.GetAI().SetData(0, eAchievementMisc.ACHIEVEMENT_READY_GET);
            }

            _events.ScheduleEvent(BossEvents.EVENT_DEADLY_BLADES, TimeSpan.FromMilliseconds(12000));
            _events.ScheduleEvent(BossEvents.EVENT_DEFLECTION, TimeSpan.FromMilliseconds(10000));
            _events.ScheduleEvent(BossEvents.EVENT_SUMMON_ADD_1, TimeSpan.FromMilliseconds(9000));
            _events.ScheduleEvent(BossEvents.EVENT_BACKSLASH, TimeSpan.FromMilliseconds(15000));

            me.GetMotionMaster().MoveJump(new Position(-65.585f, -820.742f, 41.022f), 10.0f, 5.0f);
            me.SetReactState(ReactStates.Aggressive);
            me.Yell(COMBAT_START, Language.Universal);

            DoZoneInCombat();
            base.JustEnteredCombat(who);
            instance.SendEncounterUnit(EncounterFrameType.Engage, me);
        }

        public override void JustDied(Unit killer)
        {
            base.JustDied(killer);
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
            base.summons.DespawnAll();
            summons.DespawnAll();
        }

        public override void JustSummoned(Creature summon)
        {
            switch (summon.GetEntry())
            {
                case DMCreatures.NPC_ROPE:
                    summons.Summon(summon);
                    summon.SummonCreature(DMCreatures.NPC_ROPE_ANCHOR, summon.GetPositionX(), summon.GetPositionY(), summon.GetPositionZ() + 40.0f, 0, TempSummonType.TimedDespawn, TimeSpan.FromMilliseconds(10000));
                    break;
            }
            base.summons.Summon(summon);
        }

        public override void SummonedCreatureDespawn(Creature summon)
        {
            switch (summon.GetEntry())
            {
                case DMCreatures.NPC_ROPE:
                    summons.Despawn(summon);
                    break;
            }
            base.summons.Despawn(summon);
        }

        public override void MovementInform(MovementGeneratorType UnnamedParameter, uint id)
        {
            if (id == 0)
            {
                DoCast(me, 18373);
            }
        }

        public void FieryBoom()
        {
            //var fiery = me.GetCreatureListWithEntryInGrid(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF2, 150.0f);
            //fiery.Sort(new ObjectDistanceOrderPred(me));
            //foreach (var item in fiery)
            //{
            //    if (item.IsAlive() && item.GetTypeId() == TypeId.Unit)
            //    {
            //        item.CastSpell(item, Spells.FIERY_BLAZE, true);
            //    }
            //}
        }

        public void RemoveFiresFromShip()
        {
            //var fiery = me.GetCreatureListWithEntryInGrid(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF2, 150.0f);
            //fiery.Sort(new ObjectDistanceOrderPred(me));
            //foreach (var item in fiery)
            //{
            //    if (item.IsAlive() && item.GetTypeId() == TypeId.Unit)
            //    {
            //        item.RemoveAurasDueToSpell(Spells.FIERY_BLAZE);
            //    }
            //}
        }

        public override void DamageTaken(Unit done_by, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            Player player = done_by.ToPlayer();
            if (player != null)
            {
                PlayerGUID = player;
            }

            if (me.GetHealth() <= damage)
            {
                damage = (uint)me.GetHealth() - 1;

                if (!Killed)
                {
                    _events.ScheduleEvent(BossEvents.EVENT_FINAL_TIMER, TimeSpan.FromMilliseconds(5000));
                    me.TextEmote(HUGH_BOMB, PlayerGUID, true);
                    me.RemoveAllAuras();
                    me.AttackStop();
                    me.ClearAllReactives();
                    me.CastSpell(me, 18373, true);

                }
                Killed = true;
            }
            else if (me.HealthBelowPctDamaged(25, damage))
            {
                _events.ScheduleEvent(BossEvents.EVENT_VENGEANCE, TimeSpan.FromMilliseconds(4000));
            }
            if (!Under2 && me.HealthBelowPctDamaged(26, damage))
            {
                _events.ScheduleEvent(BossEvents.EVENT_DISSAPEAR, TimeSpan.FromMilliseconds(1000));
                Under2 = true;
            }
            else if (!Under && me.HealthBelowPctDamaged(51, damage))
            {
                _events.ScheduleEvent(BossEvents.EVENT_DISSAPEAR, TimeSpan.FromMilliseconds(1000));
                Under = true;
            }
        }

        public void SummonThreatController()
        {
            //Creature bunny = me.SummonCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, -52.31f, -820.18f, 51.91f, 3.32963f);
            //if (bunny != null)
            //{
            //    bunny.SetUnitFlag(UnitFlags.Stunned);
            //    bunny.SetUnitFlag(UnitFlags.ImmuneToPc);
            //    bunny.SetReactState(ReactStates.Aggressive);
            //    bunny.SetFaction(18);
            //    bunny.Attack(me, true);
            //    me.GetThreatManager().AddThreat(bunny, 200000.0f);
            //    me.SetInCombatWith(bunny);
            //}
            //me.SetInCombatWithZone();
        }

        public void SummonRopes()
        {
            for (byte i = 0; i < 5; ++i)
            {
                me.SummonCreature(DMCreatures.NPC_ROPE, RopeSpawn[i], TempSummonType.ManualDespawn);
            }
        }

        public void RopeReady()
        {
            me.Whisper(VANESSA_DETONATE, Language.Universal, PlayerGUID, true);

            foreach (var guid in summons)
            {
                Creature rope = ObjectAccessor.GetCreature(me, guid);
                if (rope != null)
                {
                    if (rope.IsAlive())
                    {
                        rope.AddAura(Spells.CLICK_ME, rope);
                        SummonThreatController();
                    }
                }
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
            {
                return;
            }

            _events.Update(diff);

            uint eventId;
            while ((eventId = _events.ExecuteEvent()) != 0)
            {
                switch (eventId)
                {
                    case BossEvents.EVENT_DEFLECTION:
                        if (HealthAbovePct(25))
                        {
                            DoCast(me, Spells.DEFLECTION);
                            _events.ScheduleEvent(BossEvents.EVENT_DEFLECTION, TimeSpan.FromMilliseconds(50000));
                        }
                        break;
                    case BossEvents.EVENT_SUMMON_ADD_1:
                        if ((me.GetHealth() * 100) / me.GetMaxHealth() > 50)
                        {
                            me.SummonCreature(DMCreatures.NPC_DEFIAS_ENFORCER, Shadowspawn[1], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
                            _events.ScheduleEvent(BossEvents.EVENT_SUMMON_ADD_2, TimeSpan.FromMilliseconds(15000));
                        }
                        break;
                    case BossEvents.EVENT_SUMMON_ADD_2:
                        if ((me.GetHealth() * 100) / me.GetMaxHealth() > 50)
                        {
                            me.SummonCreature(DMCreatures.NPC_DEFIAS_SHADOWGUARD, Shadowspawn[0], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
                            _events.ScheduleEvent(BossEvents.EVENT_SUMMON_ADD_3, TimeSpan.FromMilliseconds(15000));
                        }
                        break;
                    case BossEvents.EVENT_SUMMON_ADD_3:
                        if ((me.GetHealth() * 100) / me.GetMaxHealth() > 50)
                        {
                            me.SummonCreature(DMCreatures.NPC_DEFIAS_BLOODWIZARD, Shadowspawn[2], TempSummonType.CorpseTimedDespawn, TimeSpan.FromMilliseconds(10000));
                            _events.ScheduleEvent(BossEvents.EVENT_SUMMON_ADD_1, TimeSpan.FromMilliseconds(15000));
                        }
                        break;
                    case BossEvents.EVENT_DEADLY_BLADES:
                        DoCast(me, Spells.DEADLY_BLADES);
                        _events.ScheduleEvent(BossEvents.EVENT_DEADLY_BLADES, TimeSpan.FromMilliseconds(35000));
                        break;
                    case BossEvents.EVENT_BACKSLASH:
                        Unit target = SelectTarget(SelectTargetMethod.Random, 0, 100, true);
                        if (target != null)
                        {
                            DoCast(target, Spells.BACKSLASH);
                        }
                        _events.ScheduleEvent(BossEvents.EVENT_BACKSLASH, TimeSpan.FromMilliseconds(17000));
                        break;
                    case BossEvents.EVENT_VENGEANCE:
                        me.AddAura(Spells.VENGEANCE, me);
                        break;

                    case BossEvents.EVENT_DISSAPEAR:
                        me.Yell(FOOLS_BOMB);
                        me.RemoveAllAuras();
                        me.GetMotionMaster().MovePoint(0, -52.31f, -820.18f, 51.91f);
                        me.SetVisible(false);
                        base.summons.DespawnAll();
                        _events.ScheduleEvent(BossEvents.EVENT_SUMMON_ROPE, TimeSpan.FromMilliseconds(2000));
                        break;
                    case BossEvents.EVENT_SUMMON_ROPE:
                        SummonRopes();
                        _events.ScheduleEvent(BossEvents.EVENT_ROPE_READY, TimeSpan.FromMilliseconds(1000));
                        break;
                    case BossEvents.EVENT_ROPE_READY:
                        RopeReady();
                        _events.CancelEvent(BossEvents.EVENT_SHADOWGUARD);
                        _events.ScheduleEvent(BossEvents.EVENT_FIRE_BOOM, TimeSpan.FromMilliseconds(3000));
                        break;
                    case BossEvents.EVENT_FIRE_BOOM:
                        FieryBoom();
                        _events.ScheduleEvent(BossEvents.EVENT_CLEAR_SHIP, TimeSpan.FromMilliseconds(2500));
                        break;
                    case BossEvents.EVENT_CLEAR_SHIP:
                        RemoveFiresFromShip();
                        me.SetVisible(true);
                        me.GetMotionMaster().MoveJump(-65.93f, -820.33f, 40.98f, 10.0f, 8.0f);
                        me.RemoveAllAuras();
                        _events.ScheduleEvent(BossEvents.EVENT_SHADOWGUARD, TimeSpan.FromMilliseconds(27000));

                        //Creature bunny = me.FindNearestCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, 150.0f, true);
                        //if (bunny != null)
                        //{
                        //    bunny.DespawnOrUnsummon(TimeSpan.FromMilliseconds(2000));
                        //}

                        break;
                    case BossEvents.EVENT_FINAL_TIMER:
                        me.CastSpell(me, Spells.POWDER_EXP, true);
                        me.AttackStop();
                        me.ClearAllReactives();
                        Unit.Kill(me, me, false);
                        return;
                }
            }
            DoMeleeAttackIfReady();
        }
    }
}
