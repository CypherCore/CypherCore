// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Scripts.Argus.AntorusTheBurningThrone.Argus
{
    struct TextIds
    {
        // Garothi Worldbreaker
        public const uint SayAggro = 0;
        public const uint SayDisengage = 1;
        public const uint SayAnnounceApocalypseDrive = 2;
        public const uint SayApocalypseDrive = 3;
        public const uint SayAnnounceEradication = 4;
        public const uint SayFinishApocalypseDrive = 5;
        public const uint SayDecimation = 6;
        public const uint SayAnnihilation = 7;
        public const uint SayAnnounceFelBombardment = 8;
        public const uint SaySlay = 9;
        public const uint SayDeath = 10;

        // Decimator
        public const uint SayAnnounceDecimation = 0;
    }

    struct SpellIds
    {
        // Garothi Worldbreaker
        public const uint Melee = 248229;
        public const uint ApocalypseDrive = 244152;
        public const uint ApocalypseDrivePeriodicDamage = 253300;
        public const uint ApocalypseDriveFinalDamage = 240277;
        public const uint Eradication = 244969;
        public const uint Empowered = 245237;
        public const uint RestoreHealth = 246012;
        public const uint AnnihilatorCannonEject = 245527;
        public const uint DecimatorCannonEject = 245515;
        public const uint FelBombardmentSelector = 244150;
        public const uint FelBombardmentWarning = 246220;
        public const uint FelBombardmentDummy = 245219;
        public const uint FelBombardmentPeriodic = 244536;
        public const uint CannonChooser = 245124;
        public const uint SearingBarrageAnnihilator = 246368;
        public const uint SearingBarrageDecimator = 244395;
        public const uint SearingBarrageDummyAnnihilator = 244398;
        public const uint SearingBarrageDummyDecimator = 246369;
        public const uint SearingBarrageSelector = 246360;
        public const uint SearingBarrageDamageAnnihilator = 244400;
        public const uint SearingBarrageDamageDecimator = 246373;
        public const uint Carnage = 244106;

        // Decimator
        public const uint DecimationSelector = 244399;
        public const uint DecimationWarning = 244410;
        public const uint DecimationCastVisual = 245338;
        public const uint DecimationMissile = 244448;

        // Annihilator
        public const uint AnnihilationSummon = 244790;
        public const uint AnnihilationSelector = 247572;
        public const uint AnnihilationDummy = 244294;
        public const uint AnnihilationDamageUnsplitted = 244762;

        // Annihilation
        public const uint AnnihilationAreaTrigger = 244795;
        public const uint AnnihilationWarning = 244799;

        // Garothi Worldbreaker (Surging Fel)
        public const uint SurgingFelAreaTrigger = 246655;
        public const uint SurgingFelDamage = 246663;
    }

    struct EventIds
    {
        // Garothi Worldbreaker
        public const uint ReengagePlayers = 1;
        public const uint FelBombardment = 2;
        public const uint SearingBarrage = 3;
        public const uint CannonChooser = 4;
        public const uint SurgingFel = 5;
    }

    struct MiscConst
    {
        public const uint MinTargetsSize = 2;
        public const uint MaxTargetsSize = 6;

        public const uint DataLastFiredCannon = 0;
        public const byte SummonGroupIdSurgingFel = 0;
        public const ushort AnimKitIdCannonDestroyed = 13264;

        public const uint MaxApocalypseDriveCount = 2;
        public static Position AnnihilationCenterReferencePos = new(-3296.72f, 9767.78f, -60.0f);

        public static void PreferNonTankTargetsAndResizeTargets(List<WorldObject> targets, Unit caster)
        {
            if (targets.Empty())
                return;

            List<WorldObject> targetsCopy = new(targets);
            int size = targetsCopy.Count;
            // Selecting our prefered target size based on total targets (min 10 player: 2, max 30 player: 6)
            uint preferedSize = (uint)Math.Min(Math.Max(MathF.Ceiling(size / 5), MinTargetsSize), MaxTargetsSize);

            // Now we get rid of the tank as these abilities prefer non-tanks above tanks as long as there are alternatives
            targetsCopy.RemoveAll(new VictimCheck(caster, false));

            // We have less available nontank targets than we want, include tanks
            if (targetsCopy.Count < preferedSize)
                targets.RandomResize(preferedSize);
            else
            {
                // Our target list has enough alternative targets, resize
                targetsCopy.RandomResize(preferedSize);
                targets.Clear();
                targets.AddRange(targetsCopy);
            }
        }
    }

    class VictimCheck : ICheck<WorldObject>
    {
        Unit _caster;
        bool _keepTank; // true = Remove all nontank targets | false = Remove current tank

        public VictimCheck(Unit caster, bool keepTank)
        {
            _caster = caster;
            _keepTank = keepTank;
        }

        public bool Invoke(WorldObject obj)
        {
            Unit unit = obj.ToUnit();
            if (unit == null)
                return true;

            if (_caster.GetVictim() != null && _caster.GetVictim() != unit)
                return _keepTank;

            return false;
        }
    }

    [Script]
    class boss_garothi_worldbreaker : BossAI
    {
        byte[] _apocalypseDriveHealthLimit = new byte[MiscConst.MaxApocalypseDriveCount];
        byte _apocalypseDriveCount;
        uint _searingBarrageSpellId;
        uint _lastCanonEntry;
        bool _castEradication;
        ObjectGuid _lastSurgingFelDummyGuid;
        List<ObjectGuid> _surgingFelDummyGuids = new();

        public boss_garothi_worldbreaker(Creature creature) : base(creature, DataTypes.GarothiWorldbreaker)
        {
            _lastCanonEntry = CreatureIds.Decimator;
            SetCombatMovement(false);
            me.SetReactState(ReactStates.Passive);
        }

        public override void InitializeAI()
        {
            switch (GetDifficulty())
            {
                case Difficulty.MythicRaid:
                case Difficulty.HeroicRaid:
                    _apocalypseDriveHealthLimit[0] = 65;
                    _apocalypseDriveHealthLimit[1] = 35;
                    break;
                case Difficulty.NormalRaid:
                case Difficulty.LFRNew:
                    _apocalypseDriveHealthLimit[0] = 60;
                    _apocalypseDriveHealthLimit[1] = 20;
                    break;
                default:
                    break;
            }
        }

        public override void JustAppeared()
        {
            me.SummonCreatureGroup(MiscConst.SummonGroupIdSurgingFel);
        }

        public override void JustEngagedWith(Unit who)
        {
            me.SetReactState(ReactStates.Aggressive);
            base.JustEngagedWith(who);
            Talk(TextIds.SayAggro);
            DoCastSelf(SpellIds.Melee);
            instance.SendEncounterUnit(EncounterFrameType.Engage, me);
            _events.ScheduleEvent(EventIds.FelBombardment, TimeSpan.FromSeconds(9));
            _events.ScheduleEvent(EventIds.CannonChooser, TimeSpan.FromSeconds(8));
        }

        public override void EnterEvadeMode(EvadeReason why)
        {
            Talk(TextIds.SayDisengage);
            _EnterEvadeMode();
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
            _events.Reset();
            CleanupEncounter();
            _DespawnAtEvade();
        }

        public override void KilledUnit(Unit victim)
        {
            if (victim.IsPlayer())
                Talk(TextIds.SaySlay, victim);
        }

        public override void JustDied(Unit killer)
        {
            _JustDied();
            Talk(TextIds.SayDeath);
            CleanupEncounter();
            instance.SendEncounterUnit(EncounterFrameType.Disengage, me);
        }

        public override void OnSpellCast(SpellInfo spell)
        {
            switch (spell.Id)
            {
                case SpellIds.ApocalypseDriveFinalDamage:
                    if (_apocalypseDriveCount < MiscConst.MaxApocalypseDriveCount)
                        _events.Reset();
                    _events.ScheduleEvent(EventIds.ReengagePlayers, TimeSpan.FromSeconds(3.5));
                    HideCannons();
                    me.SetUninteractible(false);
                    break;
                default:
                    break;
            }
        }

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.HealthBelowPctDamaged(_apocalypseDriveHealthLimit[_apocalypseDriveCount], damage))
            {
                me.AttackStop();
                me.SetReactState(ReactStates.Passive);
                me.InterruptNonMeleeSpells(true);
                me.SetFacingTo(me.GetHomePosition().GetOrientation());
                _events.Reset();

                if (GetDifficulty() == Difficulty.MythicRaid || GetDifficulty() == Difficulty.HeroicRaid)
                    _events.ScheduleEvent(EventIds.SurgingFel, TimeSpan.FromSeconds(8));

                DoCastSelf(SpellIds.ApocalypseDrive);
                DoCastSelf(SpellIds.ApocalypseDriveFinalDamage);
                Talk(TextIds.SayAnnounceApocalypseDrive);
                Talk(TextIds.SayApocalypseDrive);
                me.SetUninteractible(true);

                Creature decimator = instance.GetCreature(DataTypes.Decimator);
                if (decimator != null)
                {
                    instance.SendEncounterUnit(EncounterFrameType.Engage, decimator, 2);
                    decimator.SetUnitFlag(UnitFlags.InCombat);
                    decimator.SetUninteractible(false);
                }

                Creature annihilator = instance.GetCreature(DataTypes.Annihilator);
                if (annihilator != null)
                {
                    instance.SendEncounterUnit(EncounterFrameType.Engage, annihilator, 2);
                    annihilator.SetUnitFlag(UnitFlags.InCombat);
                    annihilator.SetUninteractible(false);
                }
                ++_apocalypseDriveCount;
            }
        }

        public override void JustSummoned(Creature summon)
        {
            summons.Summon(summon);
            switch (summon.GetEntry())
            {
                case CreatureIds.Annihilation:
                    summon.CastSpell(summon, SpellIds.AnnihilationWarning);
                    summon.CastSpell(summon, SpellIds.AnnihilationAreaTrigger);
                    break;
                case CreatureIds.Annihilator:
                case CreatureIds.Decimator:
                    summon.SetReactState(ReactStates.Passive);
                    break;
                case CreatureIds.GarothiWorldbreaker:
                    _surgingFelDummyGuids.Add(summon.GetGUID());
                    break;
                default:
                    break;
            }
        }

        public override void SummonedCreatureDies(Creature summon, Unit killer)
        {
            switch (summon.GetEntry())
            {
                case CreatureIds.Decimator:
                case CreatureIds.Annihilator:
                    me.InterruptNonMeleeSpells(true);
                    me.RemoveAurasDueToSpell(SpellIds.ApocalypseDrive);
                    me.SetUninteractible(false);

                    if (summon.GetEntry() == CreatureIds.Annihilator)
                        _searingBarrageSpellId = SpellIds.SearingBarrageAnnihilator;
                    else
                        _searingBarrageSpellId = SpellIds.SearingBarrageDecimator;

                    if (_apocalypseDriveCount < MiscConst.MaxApocalypseDriveCount)
                        _events.Reset();

                    _events.ScheduleEvent(EventIds.SearingBarrage, TimeSpan.FromSeconds(3.5));
                    _events.ScheduleEvent(EventIds.ReengagePlayers, TimeSpan.FromSeconds(3.5));
                    _castEradication = true;

                    if (summon.GetEntry() == CreatureIds.Decimator)
                        DoCastSelf(SpellIds.DecimatorCannonEject);
                    else
                        DoCastSelf(SpellIds.AnnihilatorCannonEject);

                    me.PlayOneShotAnimKitId(MiscConst.AnimKitIdCannonDestroyed);
                    HideCannons();
                    break;
                default:
                    break;
            }
        }

        public override uint GetData(uint type)
        {
            if (type == MiscConst.DataLastFiredCannon)
                return _lastCanonEntry;

            return 0;
        }

        public override void SetData(uint type, uint value)
        {
            if (type == MiscConst.DataLastFiredCannon)
                _lastCanonEntry = value;
        }

        public override void UpdateAI(uint diff)
        {
            if (!UpdateVictim())
                return;

            _events.Update(diff);

            if (me.HasUnitState(UnitState.Casting) && !me.HasAura(SpellIds.ApocalypseDrive))
                return;

            _events.ExecuteEvents(eventId =>
            {
                switch (eventId)
                {
                    case EventIds.ReengagePlayers:
                        DoCastSelf(SpellIds.Empowered);
                        DoCastSelf(SpellIds.RestoreHealth);
                        if (_castEradication)
                        {
                            DoCastSelf(SpellIds.Eradication);
                            Talk(TextIds.SayAnnounceEradication);
                            Talk(TextIds.SayFinishApocalypseDrive);
                            _castEradication = false;
                        }
                        me.SetReactState(ReactStates.Aggressive);
                        _events.ScheduleEvent(EventIds.FelBombardment, TimeSpan.FromSeconds(20));
                        _events.ScheduleEvent(EventIds.CannonChooser, TimeSpan.FromSeconds(18));
                        break;
                    case EventIds.FelBombardment:
                        DoCastAOE(SpellIds.FelBombardmentSelector);
                        _events.Repeat(TimeSpan.FromSeconds(20));
                        break;
                    case EventIds.SearingBarrage:
                        DoCastSelf(_searingBarrageSpellId);
                        break;
                    case EventIds.CannonChooser:
                        DoCastSelf(SpellIds.CannonChooser);
                        _events.Repeat(TimeSpan.FromSeconds(16));
                        break;
                    case EventIds.SurgingFel:
                    {
                        List<ObjectGuid> guids = new(_surgingFelDummyGuids);
                        guids.Remove(_lastSurgingFelDummyGuid);
                        _lastSurgingFelDummyGuid = guids.SelectRandom();
                        Creature dummy = ObjectAccessor.GetCreature(me, _lastSurgingFelDummyGuid);
                        if (dummy != null)
                            dummy.CastSpell(dummy, SpellIds.SurgingFelAreaTrigger);

                        _events.Repeat(TimeSpan.FromSeconds(8));
                        break;
                    }
                    default:
                        break;
                }
            });


            if (me.GetVictim() != null && me.GetVictim().IsWithinMeleeRange(me))
                DoMeleeAttackIfReady();
            else
                DoSpellAttackIfReady(SpellIds.Carnage);
        }

        void CleanupEncounter()
        {
            Creature decimator = instance.GetCreature(DataTypes.Decimator);
            if (decimator != null)
                instance.SendEncounterUnit(EncounterFrameType.Disengage, decimator);

            Creature annihilator = instance.GetCreature(DataTypes.Annihilator);
            if (annihilator != null)
                instance.SendEncounterUnit(EncounterFrameType.Disengage, annihilator);

            instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.DecimationWarning);
            instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.FelBombardmentWarning);
            instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.FelBombardmentPeriodic);
            summons.DespawnAll();
        }

        void HideCannons()
        {
            Creature decimator = instance.GetCreature(DataTypes.Decimator);
            if (decimator != null)
            {
                instance.SendEncounterUnit(EncounterFrameType.Disengage, decimator);
                decimator.SetUninteractible(true);
                decimator.SetUnitFlag(UnitFlags.Immune);
            }

            Creature annihilator = instance.GetCreature(DataTypes.Annihilator);
            if (annihilator != null)
            {
                instance.SendEncounterUnit(EncounterFrameType.Disengage, annihilator);
                annihilator.SetUninteractible(true);
                annihilator.SetUnitFlag(UnitFlags.Immune);
            }
        }
    }

    [Script]
    class at_garothi_annihilation : AreaTriggerAI
    {
        byte _playerCount;

        public at_garothi_annihilation(AreaTrigger areatrigger) : base(areatrigger)
        {
            Initialize();
        }

        void Initialize()
        {
            _playerCount = 0;
        }

        public override void OnUnitEnter(Unit unit)
        {
            if (!unit.IsPlayer())
                return;

            _playerCount++;

            Unit annihilation = at.GetCaster();
            if (annihilation != null)
                annihilation.RemoveAurasDueToSpell(SpellIds.AnnihilationWarning);
        }

        public override void OnUnitExit(Unit unit)
        {
            if (!unit.IsPlayer())
                return;

            _playerCount--;

            if (_playerCount == 0 && !at.IsRemoved())
            {
                Unit annihilation = at.GetCaster();
                if (annihilation != null)
                    annihilation.CastSpell(annihilation, SpellIds.AnnihilationWarning);
            }
        }
    }

    [Script]
    class spell_garothi_apocalypse_drive : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ApocalypseDrivePeriodicDamage);
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.ApocalypseDrivePeriodicDamage, aurEff);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandlePeriodic, 1, AuraType.PeriodicDummy));
        }
    }

    [Script]
    class spell_garothi_fel_bombardment_selector : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FelBombardmentWarning, SpellIds.FelBombardmentDummy);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            if (targets.Empty())
                return;

            Unit caster = GetCaster();
            if (caster != null)
                targets.RemoveAll(new VictimCheck(caster, true));
        }

        void HandleWarningEffect(uint effIndex)
        {
            Creature caster = GetCaster()?.ToCreature();
            if (caster == null || !caster.IsAIEnabled())
                return;

            Unit target = GetHitUnit();
            caster.GetAI().Talk(TextIds.SayAnnounceFelBombardment, target);
            caster.CastSpell(target, SpellIds.FelBombardmentWarning, true);
            caster.CastSpell(target, SpellIds.FelBombardmentDummy, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            OnEffectHitTarget.Add(new(HandleWarningEffect, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_garothi_fel_bombardment_warning : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FelBombardmentPeriodic);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
            {
                Unit caster = GetCaster();
                if (caster != null)
                    caster.CastSpell(GetTarget(), SpellIds.FelBombardmentPeriodic, true);
            }
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_garothi_fel_bombardment_periodic : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 0)) && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        void HandlePeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(GetTarget(), (uint)aurEff.GetSpellEffectInfo().CalcValue(caster), true);
        }

        public override void Register()
        {
            OnEffectPeriodic.Add(new(HandlePeriodic, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script]
    class spell_garothi_searing_barrage_dummy : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SearingBarrageSelector);
        }

        void HandleHit(uint effIndex)
        {
            GetHitUnit().CastSpell(GetHitUnit(), SpellIds.SearingBarrageSelector, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)GetSpellInfo().Id));
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHit, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_garothi_searing_barrage_selector : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SearingBarrageDamageAnnihilator, SpellIds.SearingBarrageDamageDecimator, SpellIds.SearingBarrageDummyAnnihilator, SpellIds.SearingBarrageDummyDecimator);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            MiscConst.PreferNonTankTargetsAndResizeTargets(targets, GetCaster());
        }

        void HandleHit(uint effIndex)
        {
            uint spellId = GetEffectValue() == SpellIds.SearingBarrageDummyAnnihilator ? SpellIds.SearingBarrageDamageAnnihilator : SpellIds.SearingBarrageDamageDecimator;
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(GetHitUnit(), spellId, true);
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitSrcAreaEntry));
            OnEffectHitTarget.Add(new(HandleHit, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_garothi_decimation_selector : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DecimationWarning);
        }

        void FilterTargets(List<WorldObject> targets)
        {
            MiscConst.PreferNonTankTargetsAndResizeTargets(targets, GetCaster());
        }

        void HandleHit(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                caster.CastSpell(GetHitUnit(), SpellIds.DecimationWarning, true);
                Creature decimator = caster.ToCreature();
                if (decimator != null)
                    if (decimator.IsAIEnabled())
                        decimator.GetAI().Talk(TextIds.SayAnnounceDecimation, GetHitUnit());
            }
        }

        public override void Register()
        {
            OnObjectAreaTargetSelect.Add(new(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            OnEffectHitTarget.Add(new(HandleHit, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_garothi_decimation_warning : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DecimationMissile);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
            {
                Unit caster = GetCaster();
                if (caster != null)
                {
                    caster.CastSpell(GetTarget(), SpellIds.DecimationMissile, true);
                    if (!caster.HasUnitState(UnitState.Casting))
                        caster.CastSpell(caster, SpellIds.DecimationCastVisual);
                }
            }
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_garothi_carnage : AuraScript
    {
        void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            // Usually we could just handle this via spell_proc but Math.Since we want
            // to silence the console message because it's not a spell trigger proc, we need a script here.
            PreventDefaultAction();
            Remove();
        }

        public override void Register()
        {
            OnEffectProc.Add(new(HandleProc, 0, AuraType.PeriodicTriggerSpell));
        }
    }

    [Script]
    class spell_garothi_annihilation_selector : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellEffect((spellInfo.Id, 0)) && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        void HandleHit(uint effIndex)
        {
            Unit caster = GetCaster();
            if (caster != null)
                caster.CastSpell(GetHitUnit(), (uint)GetEffectInfo().CalcValue(caster), true);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHit, 0, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_garothi_annihilation_triggered : SpellScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AnnihilationDamageUnsplitted);
        }

        void HandleHit(uint effIndex)
        {
            Unit target = GetHitUnit();
            if (target.HasAura(SpellIds.AnnihilationWarning))
                target.CastSpell(target, SpellIds.AnnihilationDamageUnsplitted, true);

            target.RemoveAllAuras();
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleHit, 1, SpellEffectName.Dummy));
        }
    }

    [Script]
    class spell_garothi_eradication : SpellScript
    {
        void ChangeDamage()
        {
            Unit caster = GetCaster();
            if (caster != null)
            {
                int damageReduction = MathFunctions.CalculatePct(GetHitDamage(), GetHitUnit().GetDistance(caster));
                SetHitDamage(GetHitDamage() - damageReduction);
            }
        }

        public override void Register()
        {
            OnHit.Add(new(ChangeDamage));
        }
    }

    [Script]
    class spell_garothi_surging_fel : AuraScript
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SurgingFelDamage);
        }

        void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
                GetTarget().CastSpell(GetTarget(), SpellIds.SurgingFelDamage, true);
        }

        public override void Register()
        {
            AfterEffectRemove.Add(new(AfterRemove, 0, AuraType.AreaTrigger, AuraEffectHandleModes.Real));
        }
    }

    [Script]
    class spell_garothi_cannon_chooser : SpellScript
    {
        void HandleDummyEffect(uint effIndex)
        {
            Creature caster = GetHitCreature();
            if (caster == null || !caster.IsAIEnabled())
                return;

            InstanceScript instance = caster.GetInstanceScript();
            if (instance == null)
                return;

            Creature decimator = instance.GetCreature(DataTypes.Decimator);
            Creature annihilator = instance.GetCreature(DataTypes.Annihilator);
            uint lastCannonEntry = caster.GetAI().GetData(MiscConst.DataLastFiredCannon);

            if ((lastCannonEntry == CreatureIds.Annihilator && decimator != null) || (decimator != null && annihilator == null))
            {
                decimator.CastSpell(decimator, SpellIds.DecimationSelector, true);
                caster.GetAI().Talk(TextIds.SayDecimation, decimator);
                lastCannonEntry = CreatureIds.Decimator;
            }
            else if ((lastCannonEntry == CreatureIds.Decimator && annihilator != null) || (annihilator != null && decimator == null))
            {
                int count = (int)(caster.GetMap().GetDifficultyID() == Difficulty.MythicRaid ? MiscConst.MaxTargetsSize :
                    Math.Max(MiscConst.MinTargetsSize, MathF.Ceiling((float)caster.GetMap().GetPlayersCountExceptGMs() / 5)));

                for (byte i = 0; i < count; i++)
                {
                    float x = MiscConst.AnnihilationCenterReferencePos.GetPositionX() + MathF.Cos(RandomHelper.FRand(0.0f, (float)(MathF.PI * 2))) * RandomHelper.FRand(15.0f, 30.0f);
                    float y = MiscConst.AnnihilationCenterReferencePos.GetPositionY() + MathF.Sin(RandomHelper.FRand(0.0f, (float)(MathF.PI * 2))) * RandomHelper.FRand(15.0f, 30.0f);
                    float z = caster.GetMap().GetHeight(caster.GetPhaseShift(), x, y, MiscConst.AnnihilationCenterReferencePos.GetPositionZ());
                    annihilator.CastSpell(new Position(x, y, z), SpellIds.AnnihilationSummon, true);
                }

                annihilator.CastSpell(annihilator, SpellIds.AnnihilationDummy);
                annihilator.CastSpell(annihilator, SpellIds.AnnihilationSelector);
                caster.GetAI().Talk(TextIds.SayAnnihilation);
                lastCannonEntry = CreatureIds.Annihilator;
            }

            caster.GetAI().SetData(MiscConst.DataLastFiredCannon, lastCannonEntry);
        }

        public override void Register()
        {
            OnEffectHitTarget.Add(new(HandleDummyEffect, 0, SpellEffectName.Dummy));
        }
    }
}