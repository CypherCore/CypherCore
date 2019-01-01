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
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.Northrend.CrusadersColiseum.TrialOfTheChampion
{
    struct TrialOfChampionSpells
    {
        //Vehicle
        public const uint CHARGE = 63010;
        public const uint SHIELD_BREAKER = 68504;
        public const uint SHIELD = 66482;

        // Marshal Jacob Alerius && Mokra the Skullcrusher || Warrior
        public const uint MORTAL_STRIKE = 68783;
        public const uint MORTAL_STRIKE_H = 68784;
        public const uint BLADESTORM = 63784;
        public const uint INTERCEPT = 67540;
        public const uint ROLLING_THROW = 47115; //not implemented in the AI yet...

        // Ambrose Boltspark && Eressea Dawnsinger || Mage
        public const uint FIREBALL = 66042;
        public const uint FIREBALL_H = 68310;
        public const uint BLAST_WAVE = 66044;
        public const uint BLAST_WAVE_H = 68312;
        public const uint HASTE = 66045;
        public const uint POLYMORPH = 66043;
        public const uint POLYMORPH_H = 68311;

        // Colosos && Runok Wildmane || Shaman
        public const uint CHAIN_LIGHTNING = 67529;
        public const uint CHAIN_LIGHTNING_H = 68319;
        public const uint EARTH_SHIELD = 67530;
        public const uint HEALING_WAVE = 67528;
        public const uint HEALING_WAVE_H = 68318;
        public const uint HEX_OF_MENDING = 67534;

        // Jaelyne Evensong && Zul'tore || Hunter
        public const uint DISENGAGE = 68340; //not implemented in the AI yet...
        public const uint LIGHTNING_ARROWS = 66083;
        public const uint MULTI_SHOT = 66081;
        public const uint SHOOT = 65868;
        public const uint SHOOT_H = 67988;

        // Lana Stouthammer Evensong && Deathstalker Visceri || Rouge
        public const uint EVISCERATE = 67709;
        public const uint EVISCERATE_H = 68317;
        public const uint FAN_OF_KNIVES = 67706;
        public const uint POISON_BOTTLE = 67701;
    }

    [Script]
    class generic_vehicleAI_toc5 : npc_escortAI
    {
        public generic_vehicleAI_toc5(Creature creature) : base(creature)
        {
            Initialize();
            SetDespawnAtEnd(false);
            uiWaypointPath = 0;

            instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            uiChargeTimer = 5000;
            uiShieldBreakerTimer = 8000;
            uiBuffTimer = RandomHelper.URand(30000, 60000);
        }

        public override void Reset()
        {
            Initialize();
        }

        public override void SetData(uint uiType, uint uiData)
        {
            switch (uiType)
            {
                case 1:
                    AddWaypoint(0, 747.36f, 634.07f, 411.572f);
                    AddWaypoint(1, 780.43f, 607.15f, 411.82f);
                    AddWaypoint(2, 785.99f, 599.41f, 411.92f);
                    AddWaypoint(3, 778.44f, 601.64f, 411.79f);
                    uiWaypointPath = 1;
                    break;
                case 2:
                    AddWaypoint(0, 747.35f, 634.07f, 411.57f);
                    AddWaypoint(1, 768.72f, 581.01f, 411.92f);
                    AddWaypoint(2, 763.55f, 590.52f, 411.71f);
                    uiWaypointPath = 2;
                    break;
                case 3:
                    AddWaypoint(0, 747.35f, 634.07f, 411.57f);
                    AddWaypoint(1, 784.02f, 645.33f, 412.39f);
                    AddWaypoint(2, 775.67f, 641.91f, 411.91f);
                    uiWaypointPath = 3;
                    break;
            }

            if (uiType <= 3)
                Start(false, true);
        }

        public override void WaypointReached(uint waypointId)
        {
            switch (waypointId)
            {
                case 2:
                    if (uiWaypointPath == 3 || uiWaypointPath == 2)
                        instance.SetData((uint)Data.DATA_MOVEMENT_DONE, instance.GetData((uint)Data.DATA_MOVEMENT_DONE) + 1);
                    break;
                case 3:
                    instance.SetData((uint)Data.DATA_MOVEMENT_DONE, instance.GetData((uint)Data.DATA_MOVEMENT_DONE) + 1);
                    break;
            }
        }

        public override void EnterCombat(Unit who)
        {
            DoCastSpellShield();
        }

        void DoCastSpellShield()
        {
            for (byte i = 0; i < 3; ++i)
                DoCast(me, TrialOfChampionSpells.SHIELD, true);
        }

        public override void UpdateAI(uint uiDiff)
        {
            base.UpdateAI(uiDiff);

            if (!UpdateVictim())
                return;

            if (uiBuffTimer <= uiDiff)
            {
                if (!me.HasAura(TrialOfChampionSpells.SHIELD))
                    DoCastSpellShield();

                uiBuffTimer = RandomHelper.URand(30000, 45000);
            }
            else
                uiBuffTimer -= uiDiff;

            if (uiChargeTimer <= uiDiff)
            {
                var players = me.GetMap().GetPlayers();
                if (!players.Empty())
                {
                    foreach (var player in players)
                    {
                        if (player && !player.IsGameMaster() && me.IsInRange(player, 8.0f, 25.0f, false))
                        {
                            DoResetThreat();
                            me.AddThreat(player, 1.0f);
                            DoCast(player, TrialOfChampionSpells.CHARGE);
                            break;
                        }
                    }
                }
                uiChargeTimer = 5000;
            }
            else
                uiChargeTimer -= uiDiff;

            //dosen't work at all
            if (uiShieldBreakerTimer <= uiDiff)
            {
                Vehicle pVehicle = me.GetVehicleKit();
                if (!pVehicle)
                    return;

                Unit pPassenger = pVehicle.GetPassenger(0);
                if (pPassenger)
                {
                    var players = me.GetMap().GetPlayers();
                    if (!players.Empty())
                    {
                        foreach (var player in players)
                        {
                            if (player && !player.IsGameMaster() && me.IsInRange(player, 10.0f, 30.0f, false))
                            {
                                pPassenger.CastSpell(player, TrialOfChampionSpells.SHIELD_BREAKER, true);
                                break;
                            }
                        }
                    }
                }
                uiShieldBreakerTimer = 7000;
            }
            else
                uiShieldBreakerTimer -= uiDiff;

            DoMeleeAttackIfReady();
        }

        InstanceScript instance;

        uint uiChargeTimer;
        uint uiShieldBreakerTimer;
        uint uiBuffTimer;

        uint uiWaypointPath;
    }

    abstract class boss_basic_toc5AI : ScriptedAI
    {
        protected boss_basic_toc5AI(Creature creature) : base(creature)
        {
            Initialize();
            instance = creature.GetInstanceScript();

            bDone = false;
            bHome = false;

            uiPhase = 0;
            uiPhaseTimer = 0;

            me.SetReactState(ReactStates.Passive);
            // THIS IS A HACK, SHOULD BE REMOVED WHEN THE EVENT IS FULL SCRIPTED
            me.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.ImmuneToPc);
        }

        public abstract void Initialize();

        public override void Reset()
        {
            Initialize();
        }

        public override void JustReachedHome()
        {
            base.JustReachedHome();

            if (!bHome)
                return;

            uiPhaseTimer = 15000;
            uiPhase = 1;

            bHome = false;
        }

        public override void JustDied(Unit killer)
        {
            instance.SetData((uint)Data.BOSS_GRAND_CHAMPIONS, (uint)EncounterState.Done);
        }

        public override void UpdateAI(uint diff)
        {
            if (!bDone && GrandChampionsOutVehicle(me))
            {
                bDone = true;

                if (me.GetGUID() == instance.GetGuidData((uint)Data64.DATA_GRAND_CHAMPION_1))
                    me.SetHomePosition(739.678f, 662.541f, 412.393f, 4.49f);
                else if (me.GetGUID() == instance.GetGuidData((uint)Data64.DATA_GRAND_CHAMPION_2))
                    me.SetHomePosition(746.71f, 661.02f, 411.69f, 4.6f);
                else if (me.GetGUID() == instance.GetGuidData((uint)Data64.DATA_GRAND_CHAMPION_3))
                    me.SetHomePosition(754.34f, 660.70f, 412.39f, 4.79f);

                EnterEvadeMode();
                bHome = true;
            }

            if (uiPhaseTimer <= diff)
            {
                if (uiPhase == 1)
                {
                    AggroAllPlayers(me);
                    uiPhase = 0;
                }
            }
            else
                uiPhaseTimer -= diff;
        }

        public bool InVehicle()
        {
            return !me.m_movementInfo.transport.guid.IsEmpty();
        }

        void AggroAllPlayers(Creature temp)
        {
            var PlList = temp.GetMap().GetPlayers();

            if (PlList.Empty())
                return;

            foreach (var player in PlList)
            {
                if (player)
                {
                    if (player.IsGameMaster())
                        continue;

                    if (player.IsAlive())
                    {
                        temp.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.ImmuneToPc);
                        temp.SetReactState(ReactStates.Aggressive);
                        temp.SetInCombatWith(player);
                        player.SetInCombatWith(temp);
                        temp.AddThreat(player, 0.0f);
                    }
                }
            }
        }

        bool GrandChampionsOutVehicle(Creature creature)
        {
            InstanceScript instance = creature.GetInstanceScript();

            if (instance == null)
                return false;

            Creature pGrandChampion1 = ObjectAccessor.GetCreature(creature, instance.GetGuidData((uint)Data64.DATA_GRAND_CHAMPION_1));
            Creature pGrandChampion2 = ObjectAccessor.GetCreature(creature, instance.GetGuidData((uint)Data64.DATA_GRAND_CHAMPION_2));
            Creature pGrandChampion3 = ObjectAccessor.GetCreature(creature, instance.GetGuidData((uint)Data64.DATA_GRAND_CHAMPION_3));

            if (pGrandChampion1 && pGrandChampion2 && pGrandChampion3)
            {
                if (pGrandChampion1.m_movementInfo.transport.guid.IsEmpty() &&
                    pGrandChampion2.m_movementInfo.transport.guid.IsEmpty() &&
                    pGrandChampion3.m_movementInfo.transport.guid.IsEmpty())
                    return true;
            }

            return false;
        }

        public TaskScheduler NonCombatEvents = new TaskScheduler();

        public InstanceScript instance;

        public byte uiPhase;
        public uint uiPhaseTimer;

        bool bDone;
        public bool bHome;
    }

    [Script]
    // Marshal Jacob Alerius && Mokra the Skullcrusher || Warrior
    class boss_warrior_toc5 : boss_basic_toc5AI
    {
        public boss_warrior_toc5(Creature creature) : base(creature) { }

        public override void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20), task =>
            {
                DoCastVictim(TrialOfChampionSpells.BLADESTORM);
                task.Repeat(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(20));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(7), task =>
            {
                var players = me.GetMap().GetPlayers();
                if (!players.Empty())
                {
                    foreach (var player in players)
                    {
                        if (player && !player.IsGameMaster() && me.IsInRange(player, 8.0f, 25.0f, false))
                        {
                            DoResetThreat();
                            me.AddThreat(player, 5.0f);
                            DoCast(player, TrialOfChampionSpells.INTERCEPT);
                            break;
                        }
                    }
                }
                task.Repeat(TimeSpan.FromSeconds(7));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(TrialOfChampionSpells.MORTAL_STRIKE);
                task.Repeat(TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(12));
            });
        }

        public override void UpdateAI(uint diff)
        {
            base.UpdateAI(diff);

            if (!UpdateVictim() || InVehicle())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }

    }

    [Script]
    // Ambrose Boltspark && Eressea Dawnsinger || Mage
    class boss_mage_toc5 : boss_basic_toc5AI
    {
        public boss_mage_toc5(Creature creature) : base(creature) { }

        public override void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(5), task =>
            {
                DoCastVictim(TrialOfChampionSpells.FIREBALL);
                task.Repeat(TimeSpan.FromSeconds(5));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    DoCast(target, TrialOfChampionSpells.POLYMORPH);
                task.Repeat(TimeSpan.FromSeconds(8));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastAOE(TrialOfChampionSpells.BLAST_WAVE, false);
                task.Repeat(TimeSpan.FromSeconds(13));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(22), task =>
            {
                me.InterruptNonMeleeSpells(true);

                DoCast(me, TrialOfChampionSpells.HASTE);
                task.Repeat(TimeSpan.FromSeconds(22));
            });
        }

        public override void UpdateAI(uint diff)
        {
            base.UpdateAI(diff);

            if (!UpdateVictim() || InVehicle())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    // Colosos && Runok Wildmane || Shaman
    class boss_shaman_toc5 : boss_basic_toc5AI
    {
        public boss_shaman_toc5(Creature creature) : base(creature) { }

        public override void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(16), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    DoCast(target, TrialOfChampionSpells.CHAIN_LIGHTNING);

                task.Repeat(TimeSpan.FromSeconds(16));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                bool bChance = RandomHelper.randChance(50);

                if (!bChance)
                {
                    Unit pFriend = DoSelectLowestHpFriendly(40);
                    if (pFriend)
                        DoCast(pFriend, TrialOfChampionSpells.HEALING_WAVE);
                }
                else
                    DoCast(me, TrialOfChampionSpells.HEALING_WAVE);

                task.Repeat(TimeSpan.FromSeconds(12));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35), task =>
            {
                DoCast(me, TrialOfChampionSpells.EARTH_SHIELD);
                task.Repeat(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(35));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25), task =>
            {
                DoCastVictim(TrialOfChampionSpells.HEX_OF_MENDING, true);
                task.Repeat(TimeSpan.FromSeconds(20), TimeSpan.FromSeconds(25));
            });
        }

        public override void EnterCombat(Unit who)
        {
            DoCast(me, TrialOfChampionSpells.EARTH_SHIELD);
            DoCast(who, TrialOfChampionSpells.HEX_OF_MENDING);
        }

        public override void UpdateAI(uint diff)
        {
            base.UpdateAI(diff);

            if (!UpdateVictim() || InVehicle())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    // Jaelyne Evensong && Zul'tore || Hunter
    class boss_hunter_toc5 : boss_basic_toc5AI
    {
        public boss_hunter_toc5(Creature creature) : base(creature) { }

        public override void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(7), task =>
            {
                DoCastAOE(TrialOfChampionSpells.LIGHTNING_ARROWS, false);
                task.Repeat(TimeSpan.FromSeconds(7));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                ObjectGuid uiTargetGUID = ObjectGuid.Empty;
                Unit target = SelectTarget(SelectAggroTarget.Farthest, 0, 30.0f);
                if (target)
                {
                    uiTargetGUID = target.GetGUID();
                    DoCast(target, TrialOfChampionSpells.SHOOT);
                }

                bool bShoot = true;
                task.Repeat(TimeSpan.FromSeconds(12));
                task.Schedule(TimeSpan.FromSeconds(3), task1 =>
                {
                    if (bShoot)
                    {
                        me.InterruptNonMeleeSpells(true);

                        Unit uiTarget = Global.ObjAccessor.GetUnit(me, uiTargetGUID);
                        if (uiTarget && me.IsInRange(uiTarget, 5.0f, 30.0f, false))
                        {
                            DoCast(uiTarget, TrialOfChampionSpells.MULTI_SHOT);
                        }
                        else
                        {
                            var players = me.GetMap().GetPlayers();
                            if (!players.Empty())
                            {
                                foreach (var player in players)
                                {
                                    if (player && !player.IsGameMaster() && me.IsInRange(player, 5.0f, 30.0f, false))
                                    {
                                        DoCast(player, TrialOfChampionSpells.MULTI_SHOT);
                                        break;
                                    }
                                }
                            }
                        }
                        bShoot = false;
                    }
                });
            });
        }

        public override void UpdateAI(uint diff)
        {
            base.UpdateAI(diff);

            if (!UpdateVictim() || InVehicle())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }
    }

    [Script]
    // Lana Stouthammer Evensong && Deathstalker Visceri || Rouge
    class boss_rouge_toc5 : boss_basic_toc5AI
    {
        public boss_rouge_toc5(Creature creature) : base(creature) { }

        public override void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(8), task =>
            {
                DoCastVictim(TrialOfChampionSpells.EVISCERATE);

                task.Repeat(TimeSpan.FromSeconds(8));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(14), task =>
            {
                DoCastAOE(TrialOfChampionSpells.FAN_OF_KNIVES, false);

                task.Repeat(TimeSpan.FromSeconds(14));
            });

            _scheduler.Schedule(TimeSpan.FromSeconds(19), task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    DoCast(target, TrialOfChampionSpells.POISON_BOTTLE);

                task.Repeat(TimeSpan.FromSeconds(19));
            });
        }

        public override void UpdateAI(uint diff)
        {
            base.UpdateAI(diff);

            if (!UpdateVictim() || !me.m_movementInfo.transport.guid.IsEmpty())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }
    }
}