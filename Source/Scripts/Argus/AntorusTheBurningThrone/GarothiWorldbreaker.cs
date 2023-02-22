// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IAura;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;
using Game.Spells;

namespace Scripts.Argus.AntorusTheBurningThrone.GarothiWorldbreaker
{
    internal struct TextIds
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

    internal struct SpellIds
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

    internal struct EventIds
    {
        // Garothi Worldbreaker
        public const uint ReengagePlayers = 1;
        public const uint FelBombardment = 2;
        public const uint SearingBarrage = 3;
        public const uint CannonChooser = 4;
        public const uint SurgingFel = 5;
    }

    internal struct MiscConst
    {
        public const uint MinTargetsSize = 2;
        public const uint MaxTargetsSize = 6;

        public const byte SummonGroupIdSurgingFel = 0;
        public const ushort AnimKitIdCannonDestroyed = 13264;
        public const uint DataLastFiredCannon = 0;

        public const uint MaxApocalypseDriveCount = 2;
        public static Position AnnihilationCenterReferencePos = new(-3296.72f, 9767.78f, -60.0f);

        public static void PreferNonTankTargetsAndResizeTargets(List<WorldObject> targets, Unit caster)
        {
            if (targets.Empty())
                return;

            List<WorldObject> targetsCopy = targets;
            byte size = (byte)targetsCopy.Count;
            // Selecting our prefered Target size based on total targets (min 10 player: 2, max 30 player: 6)
            byte preferedSize = (byte)(Math.Min(Math.Max(MathF.Ceiling(size / 5), MinTargetsSize), MaxTargetsSize));

            // Now we get rid of the tank as these abilities prefer non-tanks above tanks as long as there are alternatives
            targetsCopy.RemoveAll(new VictimCheck(caster, false));

            // We have less available nontank targets than we want, include tanks
            if (targetsCopy.Count < preferedSize)
            {
                targets.RandomResize(preferedSize);
            }
            else
            {
                // Our Target list has enough alternative targets, resize
                targetsCopy.RandomResize(preferedSize);
                targets.Clear();
                targets.AddRange(targetsCopy);
            }
        }
    }

    [Script]
    internal class boss_garothi_worldbreaker : BossAI
    {
        private readonly byte[] _apocalypseDriveHealthLimit = new byte[MiscConst.MaxApocalypseDriveCount];
        private readonly List<ObjectGuid> _surgingFelDummyGuids = new();
        private byte _apocalypseDriveCount;
        private bool _castEradication;
        private uint _lastCanonEntry;
        private ObjectGuid _lastSurgingFelDummyGuid;
        private uint _searingBarrageSpellId;

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
            _DespawnAtEvade(TimeSpan.FromSeconds(30));
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
                    me.RemoveUnitFlag(UnitFlags.Uninteractible);

                    break;
                default:
                    break;
            }
        }

        public override void DamageTaken(Unit attacker, ref double damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (me.HealthBelowPctDamaged(_apocalypseDriveHealthLimit[_apocalypseDriveCount], damage))
            {
                me.AttackStop();
                me.SetReactState(ReactStates.Passive);
                me.InterruptNonMeleeSpells(true);
                me.SetFacingTo(me.GetHomePosition().GetOrientation());
                _events.Reset();

                if (GetDifficulty() == Difficulty.MythicRaid ||
                    GetDifficulty() == Difficulty.HeroicRaid)
                    _events.ScheduleEvent(EventIds.SurgingFel, TimeSpan.FromSeconds(8));

                DoCastSelf(SpellIds.ApocalypseDrive);
                DoCastSelf(SpellIds.ApocalypseDriveFinalDamage);
                Talk(TextIds.SayAnnounceApocalypseDrive);
                Talk(TextIds.SayApocalypseDrive);
                me.SetUnitFlag(UnitFlags.Uninteractible);

                Creature decimator = instance.GetCreature(DataTypes.Decimator);

                if (decimator)
                {
                    instance.SendEncounterUnit(EncounterFrameType.Engage, decimator, 2);
                    decimator.SetUnitFlag(UnitFlags.InCombat);
                    decimator.RemoveUnitFlag(UnitFlags.Uninteractible);
                }

                Creature annihilator = instance.GetCreature(DataTypes.Annihilator);

                if (annihilator)
                {
                    instance.SendEncounterUnit(EncounterFrameType.Engage, annihilator, 2);
                    annihilator.SetUnitFlag(UnitFlags.InCombat);
                    annihilator.RemoveUnitFlag(UnitFlags.Uninteractible);
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
                    me.RemoveAura(SpellIds.ApocalypseDrive);
                    me.RemoveUnitFlag(UnitFlags.Uninteractible);

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

            if (me.HasUnitState(UnitState.Casting) &&
                !me.HasAura(SpellIds.ApocalypseDrive))
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
                                                  _surgingFelDummyGuids.Remove(_lastSurgingFelDummyGuid);
                                                  _lastSurgingFelDummyGuid = _surgingFelDummyGuids.SelectRandom();
                                                  Creature dummy = ObjectAccessor.GetCreature(me, _lastSurgingFelDummyGuid);

                                                  if (dummy)
                                                      dummy.CastSpell(dummy, SpellIds.SurgingFelAreaTrigger);

                                                  _events.Repeat(TimeSpan.FromSeconds(8));

                                                  break;
                                              }
                                          default:
                                              break;
                                      }
                                  });

            if (me.GetVictim() &&
                me.GetVictim().IsWithinMeleeRange(me))
                DoMeleeAttackIfReady();
            else
                DoSpellAttackIfReady(SpellIds.Carnage);
        }

        private void CleanupEncounter()
        {
            Creature decimator = instance.GetCreature(DataTypes.Decimator);

            if (decimator)
                instance.SendEncounterUnit(EncounterFrameType.Disengage, decimator);

            Creature annihilator = instance.GetCreature(DataTypes.Annihilator);

            if (annihilator)
                instance.SendEncounterUnit(EncounterFrameType.Disengage, annihilator);

            instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.DecimationWarning);
            instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.FelBombardmentWarning);
            instance.DoRemoveAurasDueToSpellOnPlayers(SpellIds.FelBombardmentPeriodic);
            summons.DespawnAll();
        }

        private void HideCannons()
        {
            Creature decimator = instance.GetCreature(DataTypes.Decimator);

            if (decimator)
            {
                instance.SendEncounterUnit(EncounterFrameType.Disengage, decimator);
                decimator.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.Immune);
            }

            Creature annihilator = instance.GetCreature(DataTypes.Annihilator);

            if (annihilator)
            {
                instance.SendEncounterUnit(EncounterFrameType.Disengage, annihilator);
                annihilator.SetUnitFlag(UnitFlags.Uninteractible | UnitFlags.Immune);
            }
        }
    }

    [Script]
    internal class at_garothi_annihilation : AreaTriggerAI
    {
        private byte _playerCount;

        public at_garothi_annihilation(AreaTrigger areatrigger) : base(areatrigger)
        {
            Initialize();
        }

        public override void OnUnitEnter(Unit unit)
        {
            if (!unit.IsPlayer())
                return;

            _playerCount++;

            Unit annihilation = at.GetCaster();

            if (annihilation)
                annihilation.RemoveAura(SpellIds.AnnihilationWarning);
        }

        public override void OnUnitExit(Unit unit)
        {
            if (!unit.IsPlayer())
                return;

            _playerCount--;

            if (_playerCount == 0 &&
                !at.IsRemoved())
            {
                Unit annihilation = at.GetCaster();

                annihilation?.CastSpell(annihilation, SpellIds.AnnihilationWarning);
            }
        }

        private void Initialize()
        {
            _playerCount = 0;
        }
    }

    [Script]
    internal class spell_garothi_apocalypse_drive : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.ApocalypseDrivePeriodicDamage);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 1, AuraType.PeriodicDummy));
        }

        private void HandlePeriodic(AuraEffect aurEff)
        {
            GetTarget().CastSpell(GetTarget(), SpellIds.ApocalypseDrivePeriodicDamage, new CastSpellExtraArgs(aurEff));
        }
    }

    [Script]
    internal class spell_garothi_fel_bombardment_selector : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FelBombardmentWarning, SpellIds.FelBombardmentDummy);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy, SpellScriptHookType.ObjectAreaTargetSelect));
            SpellEffects.Add(new EffectHandler(HandleWarningEffect, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            if (targets.Empty())
                return;

            Unit caster = GetCaster();

            if (caster)
                targets.RemoveAll(new VictimCheck(caster, true));
        }

        private void HandleWarningEffect(int effIndex)
        {
            Creature caster = GetCaster() ? GetCaster().ToCreature() : null;

            if (!caster ||
                !caster.IsAIEnabled())
                return;

            Unit target = GetHitUnit();
            caster.GetAI().Talk(TextIds.SayAnnounceFelBombardment, target);
            caster.CastSpell(target, SpellIds.FelBombardmentWarning, true);
            caster.CastSpell(target, SpellIds.FelBombardmentDummy, true);
        }
    }

    [Script]
    internal class spell_garothi_fel_bombardment_warning : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.FelBombardmentPeriodic);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
            {
                Unit caster = GetCaster();

                if (caster)
                    caster.CastSpell(GetTarget(), SpellIds.FelBombardmentPeriodic, true);
            }
        }
    }

    [Script]
    internal class spell_garothi_fel_bombardment_periodic : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return !spellInfo.GetEffects().Empty() && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicTriggerSpell));
        }

        private void HandlePeriodic(AuraEffect aurEff)
        {
            Unit caster = GetCaster();

            if (caster)
                caster.CastSpell(GetTarget(), (uint)aurEff.GetSpellEffectInfo().CalcValue(caster), true);
        }
    }

    [Script]
    internal class spell_garothi_searing_barrage_dummy : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SearingBarrageSelector);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleHit(int effIndex)
        {
            GetHitUnit().CastSpell(GetHitUnit(), SpellIds.SearingBarrageSelector, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)GetSpellInfo().Id));
        }
    }

    [Script]
    internal class spell_garothi_searing_barrage_selector : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SearingBarrageDamageAnnihilator, SpellIds.SearingBarrageDamageDecimator, SpellIds.SearingBarrageDummyAnnihilator, SpellIds.SearingBarrageDummyDecimator);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEntry, SpellScriptHookType.ObjectAreaTargetSelect));
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            MiscConst.PreferNonTankTargetsAndResizeTargets(targets, GetCaster());
        }

        private void HandleHit(int effIndex)
        {
            uint spellId = GetEffectValue() == SpellIds.SearingBarrageDummyAnnihilator ? SpellIds.SearingBarrageDamageAnnihilator : SpellIds.SearingBarrageDamageDecimator;
            Unit caster = GetCaster();

            if (caster)
                caster.CastSpell(GetHitUnit(), spellId, true);
        }
    }

    [Script]
    internal class spell_garothi_decimation_selector : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DecimationWarning);
        }

        public override void Register()
        {
            SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitSrcAreaEnemy));
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void FilterTargets(List<WorldObject> targets)
        {
            MiscConst.PreferNonTankTargetsAndResizeTargets(targets, GetCaster());
        }

        private void HandleHit(int effIndex)
        {
            Unit caster = GetCaster();

            if (caster)
            {
                caster.CastSpell(GetHitUnit(), SpellIds.DecimationWarning, true);
                Creature decimator = caster.ToCreature();

                if (decimator)
                    if (decimator.IsAIEnabled())
                        decimator.GetAI().Talk(TextIds.SayAnnounceDecimation, GetHitUnit());
            }
        }
    }

    [Script]
    internal class spell_garothi_decimation_warning : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.DecimationMissile);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
            {
                Unit caster = GetCaster();

                if (caster)
                {
                    caster.CastSpell(GetTarget(), SpellIds.DecimationMissile, true);

                    if (!caster.HasUnitState(UnitState.Casting))
                        caster.CastSpell(caster, SpellIds.DecimationCastVisual);
                }
            }
        }
    }

    [Script]
    internal class spell_garothi_carnage : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
        }

        private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
        {
            // Usually we could just handle this via spell_proc but since we want
            // to silence the console message because it's not a spell trigger proc, we need a script here.
            PreventDefaultAction();
            Remove();
        }
    }

    [Script]
    internal class spell_garothi_annihilation_selector : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return !spellInfo.GetEffects().Empty() && ValidateSpellInfo((uint)spellInfo.GetEffect(0).CalcValue());
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleHit(int effIndex)
        {
            Unit caster = GetCaster();

            if (caster)
                caster.CastSpell(GetHitUnit(), (uint)GetEffectInfo().CalcValue(caster), true);
        }
    }

    [Script]
    internal class spell_garothi_annihilation_triggered : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.AnnihilationDamageUnsplitted);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleHit(int effIndex)
        {
            Unit target = GetHitUnit();

            if (target.HasAura(SpellIds.AnnihilationWarning))
                target.CastSpell(target, SpellIds.AnnihilationDamageUnsplitted, true);

            target.RemoveAllAuras();
        }
    }

    [Script]
    internal class spell_garothi_eradication : SpellScript, ISpellOnHit
    {
        public void OnHit()
        {
            Unit caster = GetCaster();

            if (caster)
            {
                uint damageReduction = (uint)MathFunctions.CalculatePct(GetHitDamage(), GetHitUnit().GetDistance(caster));
                SetHitDamage((int)(GetHitDamage() - damageReduction));
            }
        }
    }

    [Script]
    internal class spell_garothi_surging_fel : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.SurgingFelDamage);
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.AreaTrigger, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
        }

        private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
                GetTarget().CastSpell(GetTarget(), SpellIds.SurgingFelDamage, true);
        }
    }

    [Script]
    internal class spell_garothi_cannon_chooser : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummyEffect, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummyEffect(int effIndex)
        {
            Creature caster = GetHitCreature();

            if (!caster ||
                !caster.IsAIEnabled())
                return;

            InstanceScript instance = caster.GetInstanceScript();

            if (instance == null)
                return;

            Creature decimator = instance.GetCreature(DataTypes.Decimator);
            Creature annihilator = instance.GetCreature(DataTypes.Annihilator);
            uint lastCannonEntry = caster.GetAI().GetData(MiscConst.DataLastFiredCannon);

            if ((lastCannonEntry == CreatureIds.Annihilator && decimator) ||
                (decimator && !annihilator))
            {
                decimator.CastSpell(decimator, SpellIds.DecimationSelector, true);
                caster.GetAI().Talk(TextIds.SayDecimation, decimator);
                lastCannonEntry = CreatureIds.Decimator;
            }
            else if ((lastCannonEntry == CreatureIds.Decimator && annihilator) ||
                     (annihilator && !decimator))
            {
                byte count = (byte)(caster.GetMap().GetDifficultyID() == Difficulty.MythicRaid ? MiscConst.MaxTargetsSize : Math.Max(MiscConst.MinTargetsSize, Math.Ceiling((double)caster.GetMap().GetPlayersCountExceptGMs() / 5)));

                for (byte i = 0; i < count; i++)
                {
                    float x = MiscConst.AnnihilationCenterReferencePos.GetPositionX() + MathF.Cos(RandomHelper.FRand(0.0f, MathF.PI * 2)) * RandomHelper.FRand(15.0f, 30.0f);
                    float y = MiscConst.AnnihilationCenterReferencePos.GetPositionY() + MathF.Sin(RandomHelper.FRand(0.0f, MathF.PI * 2)) * RandomHelper.FRand(15.0f, 30.0f);
                    float z = caster.GetMap().GetHeight(caster.GetPhaseShift(), x, y, MiscConst.AnnihilationCenterReferencePos.GetPositionZ());
                    annihilator.CastSpell(new Position(x, y, z), SpellIds.AnnihilationSummon, new CastSpellExtraArgs(true));
                }

                annihilator.CastSpell(annihilator, SpellIds.AnnihilationDummy);
                annihilator.CastSpell(annihilator, SpellIds.AnnihilationSelector);
                caster.GetAI().Talk(TextIds.SayAnnihilation);
                lastCannonEntry = CreatureIds.Annihilator;
            }

            caster.GetAI().SetData(MiscConst.DataLastFiredCannon, lastCannonEntry);
        }
    }

    internal class VictimCheck : ICheck<WorldObject>
    {
        private readonly Unit _caster;
        private readonly bool _keepTank; // true = remove all nontank targets | false = remove current tank

        public VictimCheck(Unit caster, bool keepTank)
        {
            _caster = caster;
            _keepTank = keepTank;
        }

        public bool Invoke(WorldObject obj)
        {
            Unit unit = obj.ToUnit();

            if (!unit)
                return true;

            if (_caster.GetVictim() &&
                _caster.GetVictim() != unit)
                return _keepTank;

            return false;
        }
    }
}