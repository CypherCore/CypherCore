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
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Northrend.Ulduar.Xt002
{
    struct SpellIds
    {
        public const uint TympanicTantrum = 62776;
        public const uint SearingLight = 63018;

        public const uint SummonLifeSpark = 64210;
        public const uint SummonVoidZone = 64203;

        public const uint GravityBomb = 63024;

        public const uint Heartbreak = 65737;

        // Cast By 33337 At Heartbreak:
        public const uint RechargePummeler = 62831;    // Summons 33344
        public const uint RechargeScrapbot = 62828;    // Summons 33343
        public const uint RechargeBoombot = 62835;    // Summons 33346

        // Cast By 33329 On 33337 (Visual?)
        public const uint EnergyOrb = 62790;    // Triggers 62826 - Needs Spellscript For Periodic Tick To Cast One Of The Random Spells Above

        public const uint HeartHealToFull = 17683;
        public const uint HeartOverload = 62789;

        public const uint HeartLightningTether = 64799;    // Cast On Self?
        public const uint Enrage = 26662;
        public const uint Stand = 37752;
        public const uint Submerge = 37751;

        //------------------Void Zone--------------------
        public const uint VoidZone = 64203;
        public const uint Consumption = 64208;

        // Life Spark
        public const uint ArcanePowerState = 49411;
        public const uint StaticCharged = 64227;
        public const uint Shock = 64230;

        //----------------Xt-002 Heart-------------------
        public const uint ExposedHeart = 63849;
        public const uint HeartRideVehicle = 63852;
        public const uint RideVehicleExposed = 63313; //Heart Exposed

        //---------------Xm-024 Pummeller----------------
        public const uint ArcingSmash = 8374;
        public const uint Trample = 5568;
        public const uint Uppercut = 10966;

        // Scrabot:
        public const uint ScrapbotRideVehicle = 47020;
        public const uint ScrapRepair = 62832;
        public const uint Suicide = 7;

        //------------------Boombot-----------------------
        public const uint AuraBoombot = 65032;
        public const uint Boom = 62834;

        // Achievement-Related Spells
        public const uint AchievementCreditNerfScrapbots = 65037;
    }

    struct XT002Data
    {
        public const uint TransferedHealth = 0;
        public const uint HardMode = 1;
        public const uint HealthRecovered = 2;
        public const uint GravityBombCasualty = 3;
    }

    struct Texts
    {
        public const uint Aggro = 0;
        public const uint HeartOpened = 1;
        public const uint HeartClosed = 2;
        public const uint TympanicTantrum = 3;
        public const uint Slay = 4;
        public const uint Berserk = 5;
        public const uint Death = 6;
        public const uint Summon = 7;
        public const uint EmoteHeartOpened = 8;
        public const uint EmoteHeartClosed = 9;
        public const uint EmoteTympanicTantrum = 10;
        public const uint EmoteScrapbot = 11;
    }

    struct Misc
    {
        public const uint PhaseOneGroup = 1;
        public const int ActionHardMode = 1;
        public const uint AchievMustDeconstructFaster = 21027;

        public const sbyte SeatHeartNormal = 0;
        public const sbyte SeatHeartExposed = 1;
    }

    [Script]
    class boss_xt002 : BossAI
    {
        public boss_xt002(Creature creature) : base(creature, BossIds.Xt002)
        {
            Initialize();
            _transferHealth = 0;
        }

        void Initialize()
        {
            _healthRecovered = false;
            _gravityBombCasualty = false;
            _hardMode = false;

            _heartExposed = 0;
        }

        public override void Reset()
        {
            _Reset();

            me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
            me.SetReactState(ReactStates.Aggressive);
            DoCastSelf(SpellIds.Stand);

            Initialize();

            instance.DoStopCriteriaTimer(CriteriaTimedTypes.Event, Misc.AchievMustDeconstructFaster);
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            summons.DespawnAll();
            _DespawnAtEvade();
        }

        public override void EnterCombat(Unit who)
        {
            Talk(Texts.Aggro);
            _EnterCombat();

            //Enrage
            _scheduler.Schedule(TimeSpan.FromMinutes(10), task =>
            {
                Talk(Texts.Berserk);
                DoCastSelf(SpellIds.Enrage);
            });

            //Gavity Bomb
            _scheduler.Schedule(TimeSpan.FromSeconds(20), Misc.PhaseOneGroup, task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    DoCast(target, SpellIds.GravityBomb);

                task.Repeat(TimeSpan.FromSeconds(20));
            });

            //Searing Light
            _scheduler.Schedule(TimeSpan.FromSeconds(20), Misc.PhaseOneGroup, task =>
            {
                Unit target = SelectTarget(SelectAggroTarget.Random, 0);
                if (target)
                    DoCast(target, SpellIds.SearingLight);

                task.Repeat(TimeSpan.FromSeconds(20));
            });

            //Tympanic Tantrum
            _scheduler.Schedule(TimeSpan.FromSeconds(30), Misc.PhaseOneGroup, task =>
            {
                Talk(Texts.TympanicTantrum);
                Talk(Texts.EmoteTympanicTantrum);
                DoCast(SpellIds.TympanicTantrum);
                task.Repeat(TimeSpan.FromSeconds(30));
            });

            instance.DoStartCriteriaTimer(CriteriaTimedTypes.Event, Misc.AchievMustDeconstructFaster);
        }

        public override void DoAction(int action)
        {
            switch (action)
            {
                case Misc.ActionHardMode:
                    _scheduler.Schedule(TimeSpan.FromMilliseconds(1), task =>
                    {
                        SetPhaseOne(true);
                    });
                    break;
            }
        }

        public override void KilledUnit(Unit who)
        {
            if (who.GetTypeId() == TypeId.Player)
                Talk(Texts.Slay);
        }

        public override void JustDied(Unit killer)
        {
            Talk(Texts.Death);
            _JustDied();
            me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
        }

        public override void DamageTaken(Unit attacker, ref uint damage)
        {
            if (!_hardMode && !me.HasReactState(ReactStates.Passive) && !HealthAbovePct(100 - 25 * (_heartExposed + 1)))
                ExposeHeart();
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            if (!me.HasReactState(ReactStates.Passive))
                DoMeleeAttackIfReady();
        }

        public override void PassengerBoarded(Unit who, sbyte seatId, bool apply)
        {
            if (apply && who.GetEntry() == InstanceCreatureIds.XS013Scrapbot)
            {
                // Need this so we can properly determine when to expose heart again in damagetaken hook
                if (me.GetHealthPct() > (25 * (4 - _heartExposed)))
                    ++_heartExposed;

                Talk(Texts.EmoteScrapbot);
                DoCast(who, SpellIds.ScrapRepair, true);
                _healthRecovered = true;
            }

            if (apply && seatId == Misc.SeatHeartExposed)
                who.CastSpell(who, SpellIds.ExposedHeart);   // Channeled
        }

        public override uint GetData(uint type)
        {
            switch (type)
            {
                case XT002Data.HardMode:
                    return _hardMode ? 1 : 0u;
                case XT002Data.HealthRecovered:
                    return _healthRecovered ? 1 : 0u;
                case XT002Data.GravityBombCasualty:
                    return _gravityBombCasualty ? 1 : 0u;
            }

            return 0;
        }

        public override void SetData(uint type, uint data)
        {
            switch (type)
            {
                case XT002Data.TransferedHealth:
                    _transferHealth = data;
                    break;
                case XT002Data.GravityBombCasualty:
                    _gravityBombCasualty = (data > 0) ? true : false;
                    break;
            }
        }

        void ExposeHeart()
        {
            Talk(Texts.HeartOpened);
            Talk(Texts.EmoteHeartOpened);

            DoCastSelf(SpellIds.Submerge);  // WIll make creature untargetable
            me.AttackStop();
            me.SetReactState(ReactStates.Passive);

            Unit heart = me.GetVehicleKit() ? me.GetVehicleKit().GetPassenger(Misc.SeatHeartNormal) : null;
            if (heart)
            {
                heart.CastSpell(heart, SpellIds.HeartOverload);
                heart.CastSpell(me, SpellIds.HeartLightningTether);
                heart.CastSpell(heart, SpellIds.HeartHealToFull, true);
                heart.CastSpell(me, SpellIds.RideVehicleExposed, true);
                heart.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
                heart.SetFlag(UnitFields.Flags, UnitFlags.Unk29);
            }
            _scheduler.DelayGroup(Misc.PhaseOneGroup, TimeSpan.FromSeconds(30));

            // Start "end of phase 2 timer"
            _scheduler.Schedule(TimeSpan.FromSeconds(30), task => { SetPhaseOne(false); });

            _heartExposed++;
        }

        void SetPhaseOne(bool isHardMode)
        {
            if (isHardMode)
            {
                me.SetFullHealth();
                DoCastSelf(SpellIds.Heartbreak, true);
                me.AddLootMode(LootModes.HardMode1);
                _hardMode = true;
            }

            Talk(Texts.HeartClosed);
            Talk(Texts.EmoteHeartClosed);

            me.RemoveFlag(UnitFields.Flags, UnitFlags.NotSelectable);
            me.SetReactState(ReactStates.Aggressive);
            DoCastSelf(SpellIds.Stand);

            //_events.RescheduleEvent(EVENT_SEARING_LIGHT, TIMER_SEARING_LIGHT / 2);
            //_events.RescheduleEvent(EVENT_GRAVITY_BOMB, TIMER_GRAVITY_BOMB);
            //_events.RescheduleEvent(EVENT_TYMPANIC_TANTRUM, RandomHelper.URand(TIMER_TYMPANIC_TANTRUM_MIN, TIMER_TYMPANIC_TANTRUM_MAX));

            Unit heart = me.GetVehicleKit() ? me.GetVehicleKit().GetPassenger(Misc.SeatHeartExposed) : null;
            if (!heart)
                return;

            heart.CastSpell(me, SpellIds.HeartRideVehicle, true);
            heart.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
            heart.RemoveFlag(UnitFields.Flags, UnitFlags.Unk29);
            heart.RemoveAurasDueToSpell(SpellIds.ExposedHeart);

            if (!_hardMode)
            {
                if (_transferHealth == 0)
                    _transferHealth = (uint)(heart.GetMaxHealth() - heart.GetHealth());

                if (_transferHealth >= me.GetHealth())
                    _transferHealth = (uint)me.GetHealth() - 1;

                me.ModifyHealth(-(int)_transferHealth);
                me.LowerPlayerDamageReq(_transferHealth);
            }
        }

        // Achievement related
        bool _healthRecovered;       // Did a scrapbot recover XT-002's health during the encounter?
        bool _hardMode;              // Are we in hard mode? Or: was the heart killed during phase 2?
        bool _gravityBombCasualty;   // Did someone die because of Gravity Bomb damage?

        byte _heartExposed;
        uint _transferHealth;
    }

    [Script]
    class npc_xt002_heart : NullCreatureAI
    {
        public npc_xt002_heart(Creature creature) : base(creature)
        {
            _instance = creature.GetInstanceScript();
        }

        public override void JustDied(Unit killer)
        {
            Creature xt002 = ObjectAccessor.GetCreature(me, _instance.GetGuidData(BossIds.Xt002));
            if (xt002)
            {
                xt002.GetAI().SetData(XT002Data.TransferedHealth, (uint)me.GetHealth());
                xt002.GetAI().DoAction(Misc.ActionHardMode);
            }
        }

        InstanceScript _instance;
    }

    [Script]
    class npc_scrapbot : ScriptedAI
    {
        public npc_scrapbot(Creature creature) : base(creature)
        {
            _instance = me.GetInstanceScript();
        }

        public override void Reset()
        {
            me.SetReactState(ReactStates.Passive);
            Creature pXT002 = ObjectAccessor.GetCreature(me, _instance.GetGuidData(BossIds.Xt002));
            if (pXT002)
                me.GetMotionMaster().MoveFollow(pXT002, 0.0f, 0.0f);
        }

        public override void MovementInform(MovementGeneratorType type, uint id)
        {
            ObjectGuid guid = _instance.GetGuidData(BossIds.Xt002);
            if (type == MovementGeneratorType.Follow && id == guid.GetCounter())
            {
                Creature xt002 = ObjectAccessor.GetCreature(me, guid);
                if (xt002)
                {
                    if (me.IsWithinMeleeRange(xt002))
                    {
                        DoCast(xt002, SpellIds.ScrapbotRideVehicle);
                        // Unapply vehicle aura again
                        xt002.RemoveAurasDueToSpell(SpellIds.ScrapbotRideVehicle);
                        me.DespawnOrUnsummon();
                    }
                }
            }
        }

        InstanceScript _instance;
    }

    [Script]
    class npc_pummeller : ScriptedAI
    {
        public npc_pummeller(Creature creature) : base(creature)
        {
            Initialize();
            _instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            _scheduler.SetValidator(() => me.IsWithinMeleeRange(me.GetVictim()));

            //Arcing Smash
            _scheduler.Schedule(TimeSpan.FromSeconds(27), task =>
            {
                DoCastVictim(SpellIds.ArcingSmash);
                task.Repeat(TimeSpan.FromSeconds(27));
            });

            //Trample
            _scheduler.Schedule(TimeSpan.FromSeconds(22), task =>
            {
                DoCastVictim(SpellIds.Trample);
                task.Repeat(TimeSpan.FromSeconds(22));
            });

            //Uppercut
            _scheduler.Schedule(TimeSpan.FromSeconds(17), task =>
            {
                DoCastVictim(SpellIds.Uppercut);
                task.Repeat(TimeSpan.FromSeconds(17));
            });
        }

        public override void Reset()
        {
            Initialize();
            Creature xt002 = ObjectAccessor.GetCreature(me, _instance.GetGuidData(BossIds.Xt002));
            if (xt002)
            {
                Position pos = xt002.GetPosition();
                me.GetMotionMaster().MovePoint(0, pos);
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _scheduler.Update(diff);

            DoMeleeAttackIfReady();
        }

        InstanceScript _instance;
    }

    class BoomEvent : BasicEvent
    {
        public BoomEvent(Creature me)
        {
            _me = me;
        }

        public override bool Execute(ulong time, uint diff)
        {
            // This hack is here because we suspect our implementation of spell effect execution on targets
            // is done in the wrong order. We suspect that 0 needs to be applied on all targets,
            // then 1, etc - instead of applying each effect on target1, then target2, etc.
            // The above situation causes the visual for this spell to be bugged, so we remove the instakill
            // effect and implement a script hack for that.

            _me.CastSpell(_me, SpellIds.Boom, false);
            return true;
        }

        Creature _me;
    }

    [Script]
    class npc_boombot : ScriptedAI
    {
        public npc_boombot(Creature creature) : base(creature)
        {
            Initialize();
            _instance = creature.GetInstanceScript();
        }

        void Initialize()
        {
            _boomed = false;
        }

        public override void Reset()
        {
            Initialize();

            DoCast(SpellIds.AuraBoombot); // For achievement

            // HACK/workaround:
            // these values aren't confirmed - lack of data - and the values in DB are incorrect
            // these values are needed for correct damage of Boom spell
            me.SetFloatValue(UnitFields.MinDamage, 15000.0f);
            me.SetFloatValue(UnitFields.MaxDamage, 18000.0f);

            // @todo proper waypoints?
            Creature pXT002 = ObjectAccessor.GetCreature(me, _instance.GetGuidData(BossIds.Xt002));
            if (pXT002)
                me.GetMotionMaster().MoveFollow(pXT002, 0.0f, 0.0f);
        }

        public override void DamageTaken(Unit who, ref uint damage)
        {
            if (damage >= (me.GetHealth() - me.GetMaxHealth() * 0.5f) && !_boomed)
            {
                _boomed = true; // Prevent recursive calls

                //me.SendSpellInstakillLog(Spells.Boom, me);

                //me.DealDamage(me, me.GetHealth(), null, DamageEffectType.NoDamage, SpellSchoolMask.Normal, null, false);

                damage = 0;

                me.CastSpell(me, SpellIds.Boom, false);

                // Visual only seems to work if the instant kill event is delayed or the spell itself is delayed
                // Casting done from player and caster source has the same targetinfo flags,
                // so that can't be the issue
                // See BoomEvent class
                // Schedule 1s delayed
                //me.m_Events.AddEvent(new BoomEvent(me), me.m_Events.CalculateTime(1 * Time.InMilliseconds));
            }
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            // No melee attack
        }

        InstanceScript _instance;
        bool _boomed;
    }

    [Script]
    class npc_life_spark : ScriptedAI
    {
        public npc_life_spark(Creature creature) : base(creature) { }

        void Initialize()
        {
            _scheduler.Schedule(TimeSpan.FromMilliseconds(1), task =>
            {
                DoCastVictim(SpellIds.Shock);
                task.Repeat(TimeSpan.FromSeconds(12));
            });
        }

        public override void Reset()
        {
            DoCastSelf(SpellIds.ArcanePowerState);
            _scheduler.CancelAll();
        }

        public override void EnterCombat(Unit victim)
        {
            DoCastSelf(SpellIds.StaticCharged);
            _scheduler.Schedule(TimeSpan.FromSeconds(12), task =>
            {
                DoCastVictim(SpellIds.Shock);
                task.Repeat();
            });
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            if (me.HasUnitState(UnitState.Casting))
                return;

            _scheduler.Update(diff, DoMeleeAttackIfReady);
        }
    }

    [Script]
    class npc_xt_void_zone : PassiveAI
    {
        public npc_xt_void_zone(Creature creature) : base(creature) { }

        public override void Reset()
        {
            _scheduler.Schedule(TimeSpan.FromSeconds(1), consumption =>
            {
                DoCastSelf(SpellIds.Consumption);
                consumption.Repeat();
            });
        }

        public override void UpdateAI(uint diff)
        {
            _scheduler.Update(diff);
        }
    }

    [Script]
    class spell_xt002_searing_light_spawn_life_spark : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.SummonLifeSpark);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player player = GetOwner().ToPlayer();
            if (player)
            {
                Unit xt002 = GetCaster();
                if (xt002)
                    if (xt002.HasAura((uint)aurEff.GetAmount()))   // Heartbreak aura indicating hard mode
                        xt002.CastSpell(player, SpellIds.SummonLifeSpark, true);
            }
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_xt002_gravity_bomb_aura : AuraScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.SummonVoidZone);
        }

        void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            Player player = GetOwner().ToPlayer();
            if (player)
            {
                Unit xt002 = GetCaster();
                if (xt002)
                    if (xt002.HasAura((uint)aurEff.GetAmount()))   // Heartbreak aura indicating hard mode
                        xt002.CastSpell(player, SpellIds.SummonVoidZone, true);
            }
        }

        void OnPeriodic(AuraEffect aurEff)
        {
            Unit xt002 = GetCaster();
            if (!xt002)
                return;

            Unit owner = GetOwner().ToUnit();
            if (!owner)
                return;

            if ((uint)aurEff.GetAmount() >= owner.GetHealth())
                if (xt002.GetAI() != null)
                    xt002.GetAI().SetData(XT002Data.GravityBombCasualty, 1);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new EffectPeriodicHandler(OnPeriodic, 2, AuraType.PeriodicDamage));
            AfterEffectRemove.Add(new EffectApplyHandler(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_xt002_gravity_bomb_damage : SpellScript
    {
        void HandleScript(uint eff)
        {
            Unit caster = GetCaster();
            if (!caster)
                return;

            if ((uint)GetHitDamage() >= GetHitUnit().GetHealth())
                if (caster.GetAI() != null)
                    caster.GetAI().SetData(XT002Data.GravityBombCasualty, 1);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.SchoolDamage));
        }
    }

    [Script]
    class spell_xt002_heart_overload_periodic : SpellScript
    {
        public override bool Validate(SpellInfo spell)
        {
            return ValidateSpellInfo(SpellIds.EnergyOrb, SpellIds.RechargeBoombot, SpellIds.RechargePummeler, SpellIds.RechargeScrapbot);
        }

        void HandleScript(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster)
            {
                InstanceScript instance = caster.GetInstanceScript();
                if (instance != null)
                {
                    Unit toyPile = Global.ObjAccessor.GetUnit(caster, instance.GetGuidData(InstanceData.ToyPile0 + RandomHelper.URand(0, 3)));
                    if (toyPile)
                    {
                        caster.CastSpell(toyPile, SpellIds.EnergyOrb, true);

                        // This should probably be incorporated in a dummy effect handler, but I've had trouble getting the correct target
                        // Weighed randomization (approximation)
                        uint[] spells = { SpellIds.RechargeScrapbot, SpellIds.RechargeScrapbot, SpellIds.RechargeScrapbot, SpellIds.RechargePummeler, SpellIds.RechargeBoombot };

                        for (byte i = 0; i < 5; ++i)
                        {
                            uint spellId = spells[RandomHelper.IRand(0, 4)];
                            toyPile.CastSpell(toyPile, spellId, true, null, null, instance.GetGuidData(BossIds.Xt002));
                        }
                    }
                }

                Creature creatureBase = caster.GetVehicleCreatureBase();
                if (creatureBase)
                    creatureBase.GetAI().Talk(Texts.Summon);
            }
        }

        public override void Register()
        {
            OnEffectHit.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_xt002_tympanic_tantrum : SpellScript
    {
        void FilterTargets(List<WorldObject> targets)
        {
            targets.RemoveAll(new PlayerOrPetCheck());
        }

        void RecalculateDamage()
        {
            SetHitDamage((int)GetHitUnit().CountPctFromMaxHealth(GetHitDamage()));
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitSrcAreaEnemy));
            OnHit.Add(new HitHandler(RecalculateDamage));
        }
    }

    [Script]
    class spell_xt002_submerged : SpellScript
    {
        void HandleScript(uint eff)
        {
            Creature target = GetHitCreature();
            if (!target)
                return;

            target.SetFlag(UnitFields.Flags, UnitFlags.NotSelectable);
            target.SetStandState(UnitStandStateType.Submerged);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect));
        }
    }

    [Script]
    class spell_xt002_321_boombot_aura : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AchievementCreditNerfScrapbots);
        }

        bool CheckProc(ProcEventInfo eventInfo)
        {
            if (eventInfo.GetActionTarget().GetEntry() != InstanceCreatureIds.XS013Scrapbot)
                return false;
            return true;
        }

        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            InstanceScript instance = eventInfo.GetActor().GetInstanceScript();
            if (instance == null)
                return;

            instance.DoCastSpellOnPlayers(SpellIds.AchievementCreditNerfScrapbots);
        }

        public override void Register()
        {
            DoCheckProc.Add(new CheckProcHandler(CheckProc));
            OnEffectProc.Add(new EffectProcHandler(HandleProc, 0, AuraType.Dummy));
        }
    }

    [Script]
    class achievement_nerf_engineering : AchievementCriteriaScript
    {
        public achievement_nerf_engineering() : base("achievement_nerf_engineering") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (!target || target.GetAI() == null)
                return false;

            return target.GetAI().GetData(XT002Data.HealthRecovered) == 0;
        }
    }

    [Script]
    class achievement_heartbreaker : AchievementCriteriaScript
    {
        public achievement_heartbreaker() : base("achievement_heartbreaker") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (!target || target.GetAI() == null)
                return false;

            return target.GetAI().GetData(XT002Data.HardMode) != 0;
        }
    }

    [Script]
    class achievement_nerf_gravity_bombs : AchievementCriteriaScript
    {
        public achievement_nerf_gravity_bombs() : base("achievement_nerf_gravity_bombs") { }

        public override bool OnCheck(Player source, Unit target)
        {
            if (!target || target.GetAI() == null)
                return false;

            return target.GetAI().GetData(XT002Data.GravityBombCasualty) == 0;
        }
    }
}
