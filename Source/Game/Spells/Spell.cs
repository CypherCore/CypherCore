// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Loots;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;
using Game.Movement;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IPlayer;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells.Auras.EffectHandlers;

namespace Game.Spells
{
    public partial class Spell : IDisposable
    {

        #region Fields

        private readonly Dictionary<SpellEffectName, SpellLogEffect> _executeLogEffects = new();
        private PathGenerator _preGeneratedPath;

        public SpellInfo SpellInfo { get; set; }
        public Item CastItem { get; set; }
        public ObjectGuid CastItemGUID;
        public uint CastItemEntry { get; set; }
        public int CastItemLevel { get; set; }
        public ObjectGuid CastId;
        public ObjectGuid OriginalCastId;
        public bool FromClient { get; set; }
        public SpellCastFlagsEx CastFlagsEx { get; set; }
        public SpellMisc Misc;
        public object CustomArg { get; set; }
        public SpellCastVisual SpellVisual;
        public SpellCastTargets Targets { get; set; }
        public sbyte ComboPointGain { get; set; }
        public SpellCustomErrors CustomError { get; set; }

        public List<Aura> AppliedMods { get; set; }

        private readonly WorldObject _caster;
        public SpellValue SpellValue { get; set; }
        private ObjectGuid _originalCasterGUID;
        private Unit _originalCaster;
        public Spell SelfContainer { get; set; }

        //Spell _data
        internal SpellSchoolMask SpellSchoolMask; // Spell school (can be overwrite for some spells (wand shoot for example)
        internal WeaponAttackType AttackType;     // For weapon based attack

        private List<SpellPowerCost> _powerCost = new();
        private int _casttime;             // Calculated spell cast Time initialized only in Spell.prepare
        private int _channeledDuration;    // Calculated channeled spell duration in order to calculate correct pushback.
        private readonly bool _canReflect; // can reflect this spell?
        private bool _autoRepeat;
        private byte _runesState;
        private byte _delayAtDamageCount;

        // Delayed spells system
        private ulong _delayStart;      // Time of spell delay start, filled by event handler, zero = just started
        private ulong _delayMoment;     // moment of next delay call, used internally
        private bool _launchHandled;    // were launch actions handled
        private bool _immediateHandled; // were immediate actions handled? (used by delayed spells only)

        // These vars are used in both delayed spell system and modified immediate spell system
        private bool _referencedFromCurrentSpell;
        private bool _executedCurrently;
        internal bool NeedComboPoints { get; set; }
        private uint _applyMultiplierMask;
        private readonly float[] _damageMultipliers = new float[SpellConst.MaxEffects];

        // Current targets, to be used in SpellEffects (MUST BE USED ONLY IN SPELL EFFECTS)
        public Unit UnitTarget { get; set; }
        public Item ItemTarget { get; set; }
        public GameObject GameObjTarget { get; set; }
        public Corpse CorpseTarget { get; set; }
        public WorldLocation DestTarget { get; set; }
        public int Damage { get; set; }
        public SpellMissInfo TargetMissInfo { get; set; }
        public float Variance;
        private SpellEffectHandleMode _effectHandleMode;

        public SpellEffectInfo EffectInfo { get; set; }

        // used in effects handlers
        internal UnitAura SpellAura { get; set; }
        internal DynObjAura DynObjAura { get; set; }

        // -------------------------------------------
        private GameObject _focusObject;

        // Damage and healing in effects need just calculate
        public int EffectDamage { get; set; }  // Damge   in effects Count here
        public int EffectHealing { get; set; } // Healing in effects Count here

        // ******************************************
        // Spell trigger system
        // ******************************************
        internal ProcFlagsInit ProcAttacker { get; set; } // Attacker trigger Flags
        internal ProcFlagsInit ProcVictim { get; set; }   // Victim   trigger Flags
        internal ProcFlagsHit HitMask { get; set; }

        // *****************************************
        // Spell Target subsystem
        // *****************************************
        // Targets store structures and _data
        public List<TargetInfo> UniqueTargetInfo { get; set; } = new();
        private uint _channelTargetEffectMask; // Mask req. alive targets

        private readonly List<GOTargetInfo> _uniqueGOTargetInfo = new();
        private readonly List<ItemTargetInfo> _uniqueItemInfo = new();
        private readonly List<CorpseTargetInfo> _uniqueCorpseTargetInfo = new();

        private readonly SpellDestination[] _destTargets = new SpellDestination[SpellConst.MaxEffects];

        private readonly List<HitTriggerSpell> _hitTriggerSpells = new();

        private SpellState _spellState;
        private int _timer;

        private SpellEvent _spellEvent;
        private readonly TriggerCastFlags _triggeredCastFlags;

        // if need this can be replaced by Aura copy
        // we can't store original aura link to prevent access to deleted Auras
        // and in same Time need aura _data and after aura deleting.
        public SpellInfo TriggeredByAuraSpell;

        #endregion

        private static readonly List<ISpellScript> _dummy = new();
        private static readonly List<(ISpellScript, ISpellEffect)> _dummySpellEffects = new();
        private readonly Dictionary<Type, List<ISpellScript>> _spellScriptsByType = new();
        private readonly Dictionary<uint, Dictionary<SpellScriptHookType, List<(ISpellScript, ISpellEffect)>>> _effectHandlers = new();

        private List<SpellScript> _loadedScripts = new();

        public Spell(WorldObject caster, SpellInfo info, TriggerCastFlags triggerFlags, ObjectGuid originalCasterGUID = default, ObjectGuid originalCastId = default)
        {
            SpellInfo = info;
            _caster = (info.HasAttribute(SpellAttr6.OriginateFromController) && caster.GetCharmerOrOwner() != null ? caster.GetCharmerOrOwner() : caster);
            SpellValue = new SpellValue(SpellInfo, caster);
            CastItemLevel = -1;

            if (IsIgnoringCooldowns())
                CastFlagsEx |= SpellCastFlagsEx.IgnoreCooldown;

            CastId = ObjectGuid.Create(HighGuid.Cast, SpellCastSource.Normal, _caster.GetMapId(), SpellInfo.Id, _caster.GetMap().GenerateLowGuid(HighGuid.Cast));
            OriginalCastId = originalCastId;
            SpellVisual.SpellXSpellVisualID = caster.GetCastSpellXSpellVisualId(SpellInfo);

            CustomError = SpellCustomErrors.None;
            FromClient = false;
            NeedComboPoints = SpellInfo.NeedsComboPoints();

            // Get _data for Type of attack
            AttackType = info.GetAttackType();

            SpellSchoolMask = SpellInfo.GetSchoolMask(); // Can be override for some spell (wand shoot for example)

            Player playerCaster = _caster.ToPlayer();

            if (playerCaster != null)
                // wand case
                if (AttackType == WeaponAttackType.RangedAttack)
                    if ((playerCaster.GetClassMask() & (uint)Class.ClassMaskWandUsers) != 0)
                    {
                        Item pItem = playerCaster.GetWeaponForAttack(WeaponAttackType.RangedAttack);

                        if (pItem != null)
                            SpellSchoolMask = (SpellSchoolMask)(1 << (int)pItem.GetTemplate().GetDamageType());
                    }

            Player modOwner = caster.GetSpellModOwner();

            modOwner?.ApplySpellMod(info, SpellModOp.Doses, ref SpellValue.AuraStackAmount, this);

            if (!originalCasterGUID.IsEmpty())
                _originalCasterGUID = originalCasterGUID;
            else
                _originalCasterGUID = _caster.GetGUID();

            if (_originalCasterGUID == _caster.GetGUID())
            {
                _originalCaster = _caster.ToUnit();
            }
            else
            {
                _originalCaster = Global.ObjAccessor.GetUnit(_caster, _originalCasterGUID);

                if (_originalCaster != null &&
                    !_originalCaster.IsInWorld)
                    _originalCaster = null;
            }

            _spellState = SpellState.None;
            _triggeredCastFlags = triggerFlags;

            if (info.HasAttribute(SpellAttr2.DoNotReportSpellFailure))
                _triggeredCastFlags = _triggeredCastFlags | TriggerCastFlags.DontReportCastError;

            if (SpellInfo.HasAttribute(SpellAttr4.AllowCastWhileCasting))
                _triggeredCastFlags = _triggeredCastFlags | TriggerCastFlags.IgnoreCastInProgress;

            _effectHandleMode = SpellEffectHandleMode.Launch;

            //Auto Shot & Shoot (wand)
            _autoRepeat = SpellInfo.IsAutoRepeatRangedSpell();

            // Determine if spell can be reflected back to the caster
            // Patch 1.2 notes: Spell Reflection no longer reflects abilities
            _canReflect = caster.IsUnit() && SpellInfo.DmgClass == SpellDmgClass.Magic && !SpellInfo.HasAttribute(SpellAttr0.IsAbility) && !SpellInfo.HasAttribute(SpellAttr1.NoReflection) && !SpellInfo.HasAttribute(SpellAttr0.NoImmunities) && !SpellInfo.IsPassive();

            CleanupTargetList();

            for (var i = 0; i < SpellConst.MaxEffects; ++i)
                _destTargets[i] = new SpellDestination(_caster);

            //not sure needed.
            Targets = new SpellCastTargets();
            AppliedMods = new List<Aura>();
        }

        public virtual void Dispose()
        {
            // unload scripts
            for (var i = 0; i < _loadedScripts.Count; ++i)
                _loadedScripts[i]._Unload();

            if (_referencedFromCurrentSpell &&
                SelfContainer &&
                SelfContainer == this)
            {
                // Clean the reference to avoid later crash.
                // If this error is repeating, we may have to add an ASSERT to better track down how we get into this case.
                Log.outError(LogFilter.Spells, "SPELL: deleting spell for spell ID {0}. However, spell still referenced.", SpellInfo.Id);
                SelfContainer = null;
            }

            if (_caster && _caster.GetTypeId() == TypeId.Player)
                Cypher.Assert(_caster.ToPlayer().SpellModTakingSpell != this);
        }

        public void SelectSpellTargets()
        {
            // select targets for cast phase
            SelectExplicitTargets();

            uint processedAreaEffectsMask = 0;

            foreach (var spellEffectInfo in SpellInfo.GetEffects())
            {
                // not call for empty effect.
                // Also some spells use not used effect targets for store targets for dummy effect in triggered spells
                if (!spellEffectInfo.IsEffect())
                    continue;

                // set expected Type of implicit targets to be sent to client
                SpellCastTargetFlags implicitTargetMask = SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetA.GetObjectType()) | SpellInfo.GetTargetFlagMask(spellEffectInfo.TargetB.GetObjectType());

                if (Convert.ToBoolean(implicitTargetMask & SpellCastTargetFlags.Unit))
                    Targets.SetTargetFlag(SpellCastTargetFlags.Unit);

                if (Convert.ToBoolean(implicitTargetMask & (SpellCastTargetFlags.Gameobject | SpellCastTargetFlags.GameobjectItem)))
                    Targets.SetTargetFlag(SpellCastTargetFlags.Gameobject);

                SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetA, ref processedAreaEffectsMask);
                SelectEffectImplicitTargets(spellEffectInfo, spellEffectInfo.TargetB, ref processedAreaEffectsMask);

                // Select targets of effect based on effect Type
                // those are used when no valid Target could be added for spell effect based on spell Target Type
                // some spell effects use explicit Target as a default Target added to Target map (like SPELL_EFFECT_LEARN_SPELL)
                // some spell effects add Target to Target map only when Target Type specified (like SPELL_EFFECT_WEAPON)
                // some spell effects don't add anything to Target map (confirmed with sniffs) (like SPELL_EFFECT_DESTROY_ALL_TOTEMS)
                SelectEffectTypeImplicitTargets(spellEffectInfo);

                if (Targets.HasDst())
                    AddDestTarget(Targets.GetDst(), spellEffectInfo.EffectIndex);

                if (spellEffectInfo.TargetA.GetObjectType() == SpellTargetObjectTypes.Unit ||
                    spellEffectInfo.TargetA.GetObjectType() == SpellTargetObjectTypes.UnitAndDest ||
                    spellEffectInfo.TargetB.GetObjectType() == SpellTargetObjectTypes.Unit ||
                    spellEffectInfo.TargetB.GetObjectType() == SpellTargetObjectTypes.UnitAndDest)
                {
                    if (SpellInfo.HasAttribute(SpellAttr1.RequireAllTargets))
                    {
                        bool noTargetFound = !UniqueTargetInfo.Any(target => (target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0);

                        if (noTargetFound)
                        {
                            SendCastResult(SpellCastResult.BadImplicitTargets);
                            Finish(false);

                            return;
                        }
                    }

                    if (SpellInfo.HasAttribute(SpellAttr2.FailOnAllTargetsImmune))
                    {
                        bool anyNonImmuneTargetFound = UniqueTargetInfo.Any(target => (target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 && target.MissCondition != SpellMissInfo.Immune && target.MissCondition != SpellMissInfo.Immune2);

                        if (!anyNonImmuneTargetFound)
                        {
                            SendCastResult(SpellCastResult.Immune);
                            Finish(false);

                            return;
                        }
                    }
                }

                if (SpellInfo.IsChanneled())
                {
                    // maybe do this for all spells?
                    if (_focusObject == null &&
                        UniqueTargetInfo.Empty() &&
                        _uniqueGOTargetInfo.Empty() &&
                        _uniqueItemInfo.Empty() &&
                        !Targets.HasDst())
                    {
                        SendCastResult(SpellCastResult.BadImplicitTargets);
                        Finish(false);

                        return;
                    }

                    uint mask = (1u << (int)spellEffectInfo.EffectIndex);

                    foreach (var ihit in UniqueTargetInfo)
                        if (Convert.ToBoolean(ihit.EffectMask & mask))
                        {
                            _channelTargetEffectMask |= mask;

                            break;
                        }
                }
            }

            ulong dstDelay = CalculateDelayMomentForDst(SpellInfo.LaunchDelay);

            if (dstDelay != 0)
                _delayMoment = dstDelay;
        }

        public void RecalculateDelayMomentForDst()
        {
            _delayMoment = CalculateDelayMomentForDst(0.0f);
            _caster.Events.ModifyEventTime(_spellEvent, TimeSpan.FromMilliseconds(GetDelayStart() + _delayMoment));
        }

        public GridMapTypeMask GetSearcherTypeMask(SpellTargetObjectTypes objType, List<Condition> condList)
        {
            // this function selects which containers need to be searched for spell Target
            GridMapTypeMask retMask = GridMapTypeMask.All;

            // filter searchers based on searched object Type
            switch (objType)
            {
                case SpellTargetObjectTypes.Unit:
                case SpellTargetObjectTypes.UnitAndDest:
                    retMask &= GridMapTypeMask.Player | GridMapTypeMask.Creature;

                    break;
                case SpellTargetObjectTypes.Corpse:
                case SpellTargetObjectTypes.CorpseEnemy:
                case SpellTargetObjectTypes.CorpseAlly:
                    retMask &= GridMapTypeMask.Player | GridMapTypeMask.Corpse | GridMapTypeMask.Creature;

                    break;
                case SpellTargetObjectTypes.Gobj:
                case SpellTargetObjectTypes.GobjItem:
                    retMask &= GridMapTypeMask.GameObject;

                    break;
                default:
                    break;
            }

            if (SpellInfo.HasAttribute(SpellAttr3.OnlyOnPlayer))
                retMask &= GridMapTypeMask.Corpse | GridMapTypeMask.Player;

            if (SpellInfo.HasAttribute(SpellAttr3.OnlyOnGhosts))
                retMask &= GridMapTypeMask.Player;

            if (SpellInfo.HasAttribute(SpellAttr5.NotOnPlayer))
                retMask &= ~GridMapTypeMask.Player;

            if (condList != null)
                retMask &= Global.ConditionMgr.GetSearcherTypeMaskForConditionList(condList);

            return retMask;
        }

        public void CleanupTargetList()
        {
            UniqueTargetInfo.Clear();
            _uniqueGOTargetInfo.Clear();
            _uniqueItemInfo.Clear();
            _delayMoment = 0;
        }

        public long GetUnitTargetCountForEffect(uint effect)
        {
            return UniqueTargetInfo.Count(targetInfo => targetInfo.MissCondition == SpellMissInfo.None && (targetInfo.EffectMask & (1 << (int)effect)) != 0);
        }

        public long GetGameObjectTargetCountForEffect(uint effect)
        {
            return _uniqueGOTargetInfo.Count(targetInfo => (targetInfo.EffectMask & (1 << (int)effect)) != 0);
        }

        public long GetItemTargetCountForEffect(uint effect)
        {
            return _uniqueItemInfo.Count(targetInfo => (targetInfo.EffectMask & (1 << (int)effect)) != 0);
        }

        public long GetCorpseTargetCountForEffect(uint effect)
        {
            return _uniqueCorpseTargetInfo.Count(targetInfo => (targetInfo.EffectMask & (1u << (int)effect)) != 0);
        }

        public SpellMissInfo PreprocessSpellHit(Unit unit, TargetInfo hitInfo)
        {
            if (unit == null)
                return SpellMissInfo.Evade;

            // Target may have begun evading between launch and hit phases - re-check now
            Creature creatureTarget = unit.ToCreature();

            if (creatureTarget != null &&
                creatureTarget.IsEvadingAttacks())
                return SpellMissInfo.Evade;

            // For delayed spells immunity may be applied between missile launch and hit - check immunity for that case
            if (SpellInfo.HasHitDelay() &&
                unit.IsImmunedToSpell(SpellInfo, _caster))
                return SpellMissInfo.Immune;

            CallScriptBeforeHitHandlers(hitInfo.MissCondition);

            Player player = unit.ToPlayer();

            if (player != null)
            {
                player.StartCriteriaTimer(CriteriaStartEvent.BeSpellTarget, SpellInfo.Id);
                player.UpdateCriteria(CriteriaType.BeSpellTarget, SpellInfo.Id, 0, 0, _caster);
                player.UpdateCriteria(CriteriaType.GainAura, SpellInfo.Id);
            }

            Player casterPlayer = _caster.ToPlayer();

            if (casterPlayer)
            {
                casterPlayer.StartCriteriaTimer(CriteriaStartEvent.CastSpell, SpellInfo.Id);
                casterPlayer.UpdateCriteria(CriteriaType.LandTargetedSpellOnTarget, SpellInfo.Id, 0, 0, unit);
            }

            if (_caster != unit)
            {
                // Recheck  UNIT_FLAG_NON_ATTACKABLE for delayed spells
                if (SpellInfo.HasHitDelay() &&
                    unit.HasUnitFlag(UnitFlags.NonAttackable) &&
                    unit.GetCharmerOrOwnerGUID() != _caster.GetGUID())
                    return SpellMissInfo.Evade;

                if (_caster.IsValidAttackTarget(unit, SpellInfo))
                {
                    unit.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.HostileActionReceived);
                }
                else if (_caster.IsFriendlyTo(unit))
                {
                    // for delayed spells ignore negative spells (after Duel end) for friendly targets
                    if (SpellInfo.HasHitDelay() &&
                        unit.IsPlayer() &&
                        !IsPositive() &&
                        !_caster.IsValidAssistTarget(unit, SpellInfo))
                        return SpellMissInfo.Evade;

                    // assisting case, healing and resurrection
                    if (unit.HasUnitState(UnitState.AttackPlayer))
                    {
                        Player playerOwner = _caster.GetCharmerOrOwnerPlayerOrPlayerItself();

                        if (playerOwner != null)
                        {
                            playerOwner.SetContestedPvP();
                            playerOwner.UpdatePvP(true);
                        }
                    }

                    if (_originalCaster &&
                        unit.IsInCombat() &&
                        SpellInfo.HasInitialAggro())
                    {
                        if (_originalCaster.HasUnitFlag(UnitFlags.PlayerControlled))          // only do explicit combat forwarding for PvP enabled units
                            _originalCaster.GetCombatManager().InheritCombatStatesFrom(unit); // for creature v creature combat, the threat forward does it for us

                        unit.GetThreatManager().ForwardThreatForAssistingMe(_originalCaster, 0.0f, null, true);
                    }
                }
            }

            // original caster for Auras
            WorldObject origCaster = _caster;

            if (_originalCaster)
                origCaster = _originalCaster;

            // check immunity due to diminishing returns
            if (Aura.BuildEffectMaskForOwner(SpellInfo, SpellConst.MaxEffectMask, unit) != 0)
            {
                foreach (var spellEffectInfo in SpellInfo.GetEffects())
                    hitInfo.AuraBasePoints[spellEffectInfo.EffectIndex] = (SpellValue.CustomBasePointsMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 ? SpellValue.EffectBasePoints[spellEffectInfo.EffectIndex] : spellEffectInfo.CalcBaseValue(_originalCaster, unit, CastItemEntry, CastItemLevel);

                // Get Data Needed for Diminishing Returns, some effects may have multiple Auras, so this must be done on spell hit, not aura add
                hitInfo.DRGroup = SpellInfo.GetDiminishingReturnsGroupForSpell();

                DiminishingLevels diminishLevel = DiminishingLevels.Level1;

                if (hitInfo.DRGroup != 0)
                {
                    diminishLevel = unit.GetDiminishing(hitInfo.DRGroup);
                    DiminishingReturnsType type = SpellInfo.GetDiminishingReturnsGroupType();

                    // Increase Diminishing on unit, current informations for actually casts will use values above
                    if (type == DiminishingReturnsType.All ||
                        (type == DiminishingReturnsType.Player && unit.IsAffectedByDiminishingReturns()))
                        unit.IncrDiminishing(SpellInfo);
                }

                // Now Reduce spell duration using _data received at spell hit
                // check whatever effects we're going to apply, diminishing returns only apply to negative aura effects
                hitInfo.Positive = true;

                if (origCaster == unit ||
                    !origCaster.IsFriendlyTo(unit))
                    foreach (var spellEffectInfo in SpellInfo.GetEffects())
                        // mod duration only for effects applying aura!
                        if ((hitInfo.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 &&
                            spellEffectInfo.IsUnitOwnedAuraEffect() &&
                            !SpellInfo.IsPositiveEffect(spellEffectInfo.EffectIndex))
                        {
                            hitInfo.Positive = false;

                            break;
                        }

                hitInfo.AuraDuration = Aura.CalcMaxDuration(SpellInfo, origCaster);

                // unit is immune to aura if it was diminished to 0 duration
                if (!hitInfo.Positive &&
                    !unit.ApplyDiminishingToDuration(SpellInfo, ref hitInfo.AuraDuration, origCaster, diminishLevel))
                    if (SpellInfo.GetEffects().All(effInfo => !effInfo.IsEffect() || effInfo.IsEffect(SpellEffectName.ApplyAura)))
                        return SpellMissInfo.Immune;
            }

            return SpellMissInfo.None;
        }

        public void DoSpellEffectHit(Unit unit, SpellEffectInfo spellEffectInfo, TargetInfo hitInfo)
        {
            uint aura_effmask = Aura.BuildEffectMaskForOwner(SpellInfo, 1u << (int)spellEffectInfo.EffectIndex, unit);

            if (aura_effmask != 0)
            {
                WorldObject caster = _caster;

                if (_originalCaster)
                    caster = _originalCaster;

                if (caster != null)
                {
                    // delayed spells with multiple targets need to create a new aura object, otherwise we'll access a deleted aura
                    if (hitInfo.HitAura == null)
                    {
                        bool resetPeriodicTimer = (SpellInfo.StackAmount < 2) && !_triggeredCastFlags.HasFlag(TriggerCastFlags.DontResetPeriodicTimer);
                        uint allAuraEffectMask = Aura.BuildEffectMaskForOwner(SpellInfo, SpellConst.MaxEffectMask, unit);

                        AuraCreateInfo createInfo = new(CastId, SpellInfo, GetCastDifficulty(), allAuraEffectMask, unit);
                        createInfo.SetCasterGUID(caster.GetGUID());
                        createInfo.SetBaseAmount(hitInfo.AuraBasePoints);
                        createInfo.SetCastItem(CastItemGUID, CastItemEntry, CastItemLevel);
                        createInfo.SetPeriodicReset(resetPeriodicTimer);
                        createInfo.SetOwnerEffectMask(aura_effmask);

                        Aura aura = Aura.TryRefreshStackOrCreate(createInfo, false);

                        if (aura != null)
                        {
                            hitInfo.HitAura = aura.ToUnitAura();

                            // Set aura stack amount to desired value
                            if (SpellValue.AuraStackAmount > 1)
                            {
                                if (!createInfo.IsRefresh)
                                    hitInfo.HitAura.SetStackAmount((byte)SpellValue.AuraStackAmount);
                                else
                                    hitInfo.HitAura.ModStackAmount(SpellValue.AuraStackAmount);
                            }

                            hitInfo.HitAura.SetDiminishGroup(hitInfo.DRGroup);

                            if (!SpellValue.Duration.HasValue)
                            {
                                hitInfo.AuraDuration = caster.ModSpellDuration(SpellInfo, unit, hitInfo.AuraDuration, hitInfo.Positive, hitInfo.HitAura.GetEffectMask());

                                if (hitInfo.AuraDuration > 0)
                                {
                                    hitInfo.AuraDuration *= (int)SpellValue.DurationMul;

                                    // Haste modifies duration of channeled spells
                                    if (SpellInfo.IsChanneled())
                                    {
                                        caster.ModSpellDurationTime(SpellInfo, ref hitInfo.AuraDuration, this);
                                    }
                                    else if (SpellInfo.HasAttribute(SpellAttr8.HasteAffectsDuration))
                                    {
                                        int origDuration = hitInfo.AuraDuration;
                                        hitInfo.AuraDuration = 0;

                                        foreach (AuraEffect auraEff in hitInfo.HitAura.GetAuraEffects())
                                            if (auraEff != null)
                                            {
                                                int period = auraEff.GetPeriod();

                                                if (period != 0) // period is hastened by UNIT_MOD_CAST_SPEED
                                                    hitInfo.AuraDuration = Math.Max(Math.Max(origDuration / period, 1) * period, hitInfo.AuraDuration);
                                            }

                                        // if there is no periodic effect
                                        if (hitInfo.AuraDuration == 0)
                                            hitInfo.AuraDuration = (int)(origDuration * _originalCaster.UnitData.ModCastingSpeed);
                                    }
                                }
                            }
                            else
                            {
                                hitInfo.AuraDuration = SpellValue.Duration.Value;
                            }

                            if (hitInfo.AuraDuration != hitInfo.HitAura.GetMaxDuration())
                            {
                                hitInfo.HitAura.SetMaxDuration(hitInfo.AuraDuration);
                                hitInfo.HitAura.SetDuration(hitInfo.AuraDuration);
                            }

                            if (createInfo.IsRefresh)
                                hitInfo.HitAura.AddStaticApplication(unit, aura_effmask);
                        }
                    }
                    else
                    {
                        hitInfo.HitAura.AddStaticApplication(unit, aura_effmask);
                    }
                }
            }

            SpellAura = hitInfo.HitAura;
            HandleEffects(unit, null, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
            SpellAura = null;
        }

        public void DoTriggersOnSpellHit(Unit unit)
        {
            // handle SPELL_AURA_ADD_TARGET_TRIGGER Auras
            // this is executed after spell proc spells on Target hit
            // spells are triggered for each hit spell Target
            // info confirmed with retail sniffs of permafrost and shadow weaving
            if (!_hitTriggerSpells.Empty())
            {
                int _duration = 0;

                foreach (var hit in _hitTriggerSpells)
                    if (CanExecuteTriggersOnHit(unit, hit.TriggeredByAura) &&
                        RandomHelper.randChance(hit.Chance))
                    {
                        _caster.CastSpell(unit,
                                          hit.TriggeredSpell.Id,
                                          new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                              .SetTriggeringSpell(this)
                                              .SetCastDifficulty(hit.TriggeredSpell.Difficulty));

                        Log.outDebug(LogFilter.Spells, "Spell {0} triggered spell {1} by SPELL_AURA_ADD_TARGET_TRIGGER aura", SpellInfo.Id, hit.TriggeredSpell.Id);

                        // SPELL_AURA_ADD_TARGET_TRIGGER Auras shouldn't trigger Auras without duration
                        // set duration of current aura to the triggered spell
                        if (hit.TriggeredSpell.GetDuration() == -1)
                        {
                            Aura triggeredAur = unit.GetAura(hit.TriggeredSpell.Id, _caster.GetGUID());

                            if (triggeredAur != null)
                            {
                                // get duration from aura-only once
                                if (_duration == 0)
                                {
                                    Aura aur = unit.GetAura(SpellInfo.Id, _caster.GetGUID());
                                    _duration = aur != null ? aur.GetDuration() : -1;
                                }

                                triggeredAur.SetDuration(_duration);
                            }
                        }
                    }
            }

            // trigger linked Auras remove/apply
            // @todo remove/cleanup this, as this table is not documented and people are doing stupid things with it
            var spellTriggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Hit, SpellInfo.Id);

            if (spellTriggered != null)
                foreach (var id in spellTriggered)
                    if (id < 0)
                        unit.RemoveAurasDueToSpell((uint)-id);
                    else
                        unit.CastSpell(unit, (uint)id, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetOriginalCaster(_caster.GetGUID()).SetTriggeringSpell(this));
        }

        public SpellCastResult Prepare(SpellCastTargets targets, AuraEffect triggeredByAura = null)
        {
            if (CastItem != null)
            {
                CastItemGUID = CastItem.GetGUID();
                CastItemEntry = CastItem.GetEntry();

                Player owner = CastItem.GetOwner();

                if (owner)
                {
                    CastItemLevel = (int)CastItem.GetItemLevel(owner);
                }
                else if (CastItem.GetOwnerGUID() == _caster.GetGUID())
                {
                    CastItemLevel = (int)CastItem.GetItemLevel(_caster.ToPlayer());
                }
                else
                {
                    SendCastResult(SpellCastResult.EquippedItem);
                    Finish(false);

                    return SpellCastResult.EquippedItem;
                }
            }

            InitExplicitTargets(targets);

            _spellState = SpellState.Preparing;

            if (triggeredByAura != null)
            {
                TriggeredByAuraSpell = triggeredByAura.GetSpellInfo();
                CastItemLevel = triggeredByAura.GetBase().GetCastItemLevel();
            }

            // create and add update event for this spell
            _spellEvent = new SpellEvent(this);
            _caster.Events.AddEvent(_spellEvent, _caster.Events.CalculateTime(TimeSpan.FromMilliseconds(1)));

            // check disables
            if (Global.DisableMgr.IsDisabledFor(DisableType.Spell, SpellInfo.Id, _caster))
            {
                SendCastResult(SpellCastResult.SpellUnavailable);
                Finish(false);

                return SpellCastResult.SpellUnavailable;
            }

            // Prevent casting at cast another spell (ServerSide check)
            if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress) &&
                _caster.ToUnit() != null &&
                _caster.ToUnit().IsNonMeleeSpellCast(false, true, true, SpellInfo.Id == 75) &&
                !CastId.IsEmpty())
            {
                SendCastResult(SpellCastResult.SpellInProgress);
                Finish(false);

                return SpellCastResult.SpellInProgress;
            }

            LoadScripts();

            // Fill cost _data (not use power for Item casts
            if (CastItem == null)
                _powerCost = SpellInfo.CalcPowerCost(_caster, SpellSchoolMask, this);

            // Set combo point requirement
            if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreComboPoints) ||
                CastItem != null)
                NeedComboPoints = false;

            int param1 = 0, param2 = 0;
            SpellCastResult result = CheckCast(true, ref param1, ref param2);

            // Target is checked in too many locations and with different results to handle each of them
            // handle just the general SPELL_FAILED_BAD_TARGETS result which is the default result for most DBC Target checks
            if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreTargetCheck) &&
                result == SpellCastResult.BadTargets)
                result = SpellCastResult.SpellCastOk;

            if (result != SpellCastResult.SpellCastOk)
            {
                // Periodic Auras should be interrupted when aura triggers a spell which can't be cast
                // for example bladestorm aura should be removed on disarm as of patch 3.3.5
                // channeled periodic spells should be affected by this (arcane missiles, penance, etc)
                // a possible alternative sollution for those would be validating aura Target on unit State change
                if (triggeredByAura != null &&
                    triggeredByAura.IsPeriodic() &&
                    !triggeredByAura.GetBase().IsPassive())
                {
                    SendChannelUpdate(0);
                    triggeredByAura.GetBase().SetDuration(0);
                }

                if (param1 != 0 ||
                    param2 != 0)
                    SendCastResult(result, param1, param2);
                else
                    SendCastResult(result);

                // queue autorepeat spells for future repeating
                if (GetCurrentContainer() == CurrentSpellTypes.AutoRepeat &&
                    _caster.IsUnit())
                    _caster.ToUnit().SetCurrentCastSpell(this);

                Finish(false);

                return result;
            }

            // Prepare _data for triggers
            PrepareDataForTriggerSystem();

            _casttime = CallScriptCalcCastTimeHandlers(SpellInfo.CalcCastTime(this));

            if (_caster.IsUnit() &&
                _caster.ToUnit().IsMoving())
            {
                result = CheckMovement();

                if (result != SpellCastResult.SpellCastOk)
                {
                    SendCastResult(result);
                    Finish(false);

                    return result;
                }
            }

            // Creatures focus their Target when possible
            if (_casttime != 0 &&
                _caster.IsCreature() &&
                !SpellInfo.IsNextMeleeSwingSpell() &&
                !IsAutoRepeat() &&
                !_caster.ToUnit().HasUnitFlag(UnitFlags.Possessed))
            {
                // Channeled spells and some triggered spells do not focus a cast Target. They face their Target later on via channel object Guid and via spell attribute or not at all
                bool focusTarget = !SpellInfo.IsChanneled() && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreSetFacing);

                if (focusTarget &&
                    Targets.GetObjectTarget() &&
                    _caster != Targets.GetObjectTarget())
                    _caster.ToCreature().SetSpellFocus(this, Targets.GetObjectTarget());
                else
                    _caster.ToCreature().SetSpellFocus(this, null);
            }

            CallScriptOnPrecastHandler();

            // set timer base at cast Time
            ReSetTimer();

            Log.outDebug(LogFilter.Spells, "Spell.prepare: spell Id {0} source {1} caster {2} customCastFlags {3} mask {4}", SpellInfo.Id, _caster.GetEntry(), _originalCaster != null ? (int)_originalCaster.GetEntry() : -1, _triggeredCastFlags, Targets.GetTargetMask());

            if (SpellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart))
                SendSpellCooldown();

            //Containers for channeled spells have to be set
            // @todoApply this to all casted spells if needed
            // Why check duration? 29350: channelled triggers channelled
            if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.CastDirectly) &&
                (!SpellInfo.IsChanneled() || SpellInfo.GetMaxDuration() == 0))
            {
                Cast(true);
            }
            else
            {
                // commented out !_spellInfo->StartRecoveryTime, it forces instant spells with global cooldown to be processed in spell::update
                // as a result a spell that passed CheckCast and should be processed instantly may suffer from this delayed process
                // the easiest bug to observe is LoS check in AddUnitTarget, even if spell passed the CheckCast LoS check the situation can change in spell::update
                // because Target could be relocated in the meantime, making the spell fly to the air (no targets can be registered, so no effects processed, nothing in combat log)
                bool willCastDirectly = _casttime == 0 && /*!_spellInfo->StartRecoveryTime && */ GetCurrentContainer() == CurrentSpellTypes.Generic;

                Unit unitCaster = _caster.ToUnit();

                if (unitCaster != null)
                {
                    // stealth must be removed at cast starting (at show channel bar)
                    // skip triggered spell (Item equip spell casting and other not explicit character casts/Item uses)
                    if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreAuraInterruptFlags) &&
                        !SpellInfo.HasAttribute(SpellAttr2.NotAnAction))
                        unitCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Action, SpellInfo);

                    // Do not register as current spell when requested to ignore cast in progress
                    // We don't want to interrupt that other spell with cast Time
                    if (!willCastDirectly ||
                        !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress))
                        unitCaster.SetCurrentCastSpell(this);
                }

                SendSpellStart();

                if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreGCD))
                    TriggerGlobalCooldown();

                // Call CreatureAI hook OnSpellStart
                Creature caster = _caster.ToCreature();

                if (caster != null)
                    if (caster.IsAIEnabled())
                        caster.GetAI().OnSpellStart(GetSpellInfo());

                if (willCastDirectly)
                    Cast(true);
            }

            return SpellCastResult.SpellCastOk;
        }

        public void Cancel()
        {
            if (_spellState == SpellState.Finished)
                return;

            SpellState oldState = _spellState;
            _spellState = SpellState.Finished;

            _autoRepeat = false;

            switch (oldState)
            {
                case SpellState.Preparing:
                    CancelGlobalCooldown();
                    goto case SpellState.Delayed;
                case SpellState.Delayed:
                    SendInterrupted(0);
                    SendCastResult(SpellCastResult.Interrupted);

                    break;

                case SpellState.Casting:
                    foreach (var ihit in UniqueTargetInfo)
                        if (ihit.MissCondition == SpellMissInfo.None)
                        {
                            Unit unit = _caster.GetGUID() == ihit.TargetGUID ? _caster.ToUnit() : Global.ObjAccessor.GetUnit(_caster, ihit.TargetGUID);

                            unit?.RemoveOwnedAura(SpellInfo.Id, _originalCasterGUID, 0, AuraRemoveMode.Cancel);
                        }

                    SendChannelUpdate(0);
                    SendInterrupted(0);
                    SendCastResult(SpellCastResult.Interrupted);

                    AppliedMods.Clear();

                    break;

                default:
                    break;
            }

            SetReferencedFromCurrent(false);

            if (SelfContainer != null &&
                SelfContainer == this)
                SelfContainer = null;

            // originalcaster handles gameobjects/dynobjects for gob caster
            if (_originalCaster != null)
            {
                _originalCaster.RemoveDynObject(SpellInfo.Id);

                if (SpellInfo.IsChanneled()) // if not channeled then the object for the current cast wasn't summoned yet
                    _originalCaster.RemoveGameObject(SpellInfo.Id, true);
            }

            //set State back so finish will be processed
            _spellState = oldState;

            Finish(false);
        }

        public void Cast(bool skipCheck = false)
        {
            Player modOwner = _caster.GetSpellModOwner();
            Spell lastSpellMod = null;

            if (modOwner)
            {
                lastSpellMod = modOwner.SpellModTakingSpell;

                if (lastSpellMod)
                    modOwner.SetSpellModTakingSpell(lastSpellMod, false);
            }

            _cast(skipCheck);

            if (lastSpellMod)
                modOwner.SetSpellModTakingSpell(lastSpellMod, true);
        }

        public ulong HandleDelayed(ulong offset)
        {
            if (!UpdatePointers())
            {
                // finish the spell if UpdatePointers() returned false, something wrong happened there
                Finish(false);

                return 0;
            }

            bool single_missile = Targets.HasDst();
            ulong next_time = 0;

            if (!_launchHandled)
            {
                ulong launchMoment = (ulong)Math.Floor(SpellInfo.LaunchDelay * 1000.0f);

                if (launchMoment > offset)
                    return launchMoment;

                HandleLaunchPhase();
                _launchHandled = true;

                if (_delayMoment > offset)
                {
                    if (single_missile)
                        return _delayMoment;

                    next_time = _delayMoment;

                    if ((UniqueTargetInfo.Count > 2 || (UniqueTargetInfo.Count == 1 && UniqueTargetInfo[0].TargetGUID == _caster.GetGUID())) ||
                        !_uniqueGOTargetInfo.Empty())
                        offset = 0; // if LaunchDelay was present then the only Target that has timeDelay = 0 is _caster - and that is the only Target we want to process now
                }
            }

            if (single_missile && offset == 0)
                return _delayMoment;

            Player modOwner = _caster.GetSpellModOwner();

            modOwner?.SetSpellModTakingSpell(this, true);

            PrepareTargetProcessing();

            if (!_immediateHandled &&
                offset != 0)
            {
                _handle_immediate_phase();
                _immediateHandled = true;
            }

            // now recheck units targeting correctness (need before any effects apply to prevent adding immunity at first effect not allow apply second spell effect and similar cases)
            {
                List<TargetInfo> delayedTargets = new();

                UniqueTargetInfo.RemoveAll(target =>
                                            {
                                                if (single_missile || target.TimeDelay <= offset)
                                                {
                                                    target.TimeDelay = offset;
                                                    delayedTargets.Add(target);

                                                    return true;
                                                }
                                                else if (next_time == 0 ||
                                                         target.TimeDelay < next_time)
                                                {
                                                    next_time = target.TimeDelay;
                                                }

                                                return false;
                                            });

                DoProcessTargetContainer(delayedTargets);
            }

            // now recheck gameobject targeting correctness
            {
                List<GOTargetInfo> delayedGOTargets = new();

                _uniqueGOTargetInfo.RemoveAll(goTarget =>
                                              {
                                                  if (single_missile || goTarget.TimeDelay <= offset)
                                                  {
                                                      goTarget.TimeDelay = offset;
                                                      delayedGOTargets.Add(goTarget);

                                                      return true;
                                                  }
                                                  else if (next_time == 0 ||
                                                           goTarget.TimeDelay < next_time)
                                                  {
                                                      next_time = goTarget.TimeDelay;
                                                  }

                                                  return false;
                                              });

                DoProcessTargetContainer(delayedGOTargets);
            }

            FinishTargetProcessing();

            if (modOwner)
                modOwner.SetSpellModTakingSpell(this, false);

            // All targets passed - need finish phase
            if (next_time == 0)
            {
                // spell is finished, perform some last features of the spell here
                _handle_finish_phase();

                Finish(true); // successfully finish spell cast

                // return zero, spell is finished now
                return 0;
            }
            else
            {
                // spell is unfinished, return next execution Time
                return next_time;
            }
        }

        public void Update(uint difftime)
        {
            if (!UpdatePointers())
            {
                // cancel the spell if UpdatePointers() returned false, something wrong happened there
                Cancel();

                return;
            }

            if (!Targets.GetUnitTargetGUID().IsEmpty() &&
                Targets.GetUnitTarget() == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell {0} is cancelled due to removal of Target.", SpellInfo.Id);
                Cancel();

                return;
            }

            // check if the player caster has moved before the spell finished
            // with the exception of spells affected with SPELL_AURA_CAST_WHILE_WALKING effect
            if (_timer != 0 &&
                _caster.IsUnit() &&
                _caster.ToUnit().IsMoving() &&
                CheckMovement() != SpellCastResult.SpellCastOk)
                // if charmed by creature, trust the AI not to cheat and allow the cast to proceed
                // @todo this is a hack, "creature" movesplines don't differentiate turning/moving right now
                // however, checking what Type of movement the spline is for every single spline would be really expensive
                if (!_caster.ToUnit().GetCharmerGUID().IsCreature())
                    Cancel();

            switch (_spellState)
            {
                case SpellState.Preparing:
                    {
                        if (_timer > 0)
                        {
                            if (difftime >= _timer)
                                _timer = 0;
                            else
                                _timer -= (int)difftime;
                        }

                        if (_timer == 0 &&
                            !SpellInfo.IsNextMeleeSwingSpell())
                            // don't CheckCast for instant spells - done in spell.prepare, skip duplicate checks, needed for range checks for example
                            Cast(_casttime == 0);

                        break;
                    }
                case SpellState.Casting:
                    {
                        if (_timer != 0)
                        {
                            // check if there are alive targets left
                            if (!UpdateChanneledTargetList())
                            {
                                Log.outDebug(LogFilter.Spells, "Channeled spell {0} is removed due to lack of targets", SpellInfo.Id);
                                _timer = 0;

                                // Also remove applied Auras
                                foreach (TargetInfo target in UniqueTargetInfo)
                                {
                                    Unit unit = _caster.GetGUID() == target.TargetGUID ? _caster.ToUnit() : Global.ObjAccessor.GetUnit(_caster, target.TargetGUID);

                                    if (unit)
                                        unit.RemoveOwnedAura(SpellInfo.Id, _originalCasterGUID, 0, AuraRemoveMode.Cancel);
                                }
                            }

                            if (_timer > 0)
                            {
                                if (difftime >= _timer)
                                    _timer = 0;
                                else
                                    _timer -= (int)difftime;
                            }
                        }

                        if (_timer == 0)
                        {
                            SendChannelUpdate(0);
                            Finish();

                            // We call the hook here instead of in Spell::finish because we only want to call it for completed channeling. Everything else is handled by interrupts
                            Creature creatureCaster = _caster.ToCreature();

                            if (creatureCaster != null)
                                if (creatureCaster.IsAIEnabled())
                                    creatureCaster.GetAI().OnChannelFinished(SpellInfo);
                        }

                        break;
                    }
                default:
                    break;
            }
        }

        public void Finish(bool ok = true)
        {
            if (_spellState == SpellState.Finished)
                return;

            _spellState = SpellState.Finished;

            if (!_caster)
                return;

            Unit unitCaster = _caster.ToUnit();

            if (unitCaster == null)
                return;

            // successful cast of the initial autorepeat spell is moved to idle State so that it is not deleted as long as autorepeat is active
            if (IsAutoRepeat() &&
                unitCaster.GetCurrentSpell(CurrentSpellTypes.AutoRepeat) == this)
                _spellState = SpellState.Idle;

            if (SpellInfo.IsChanneled())
                unitCaster.UpdateInterruptMask();

            if (unitCaster.HasUnitState(UnitState.Casting) &&
                !unitCaster.IsNonMeleeSpellCast(false, false, true))
                unitCaster.ClearUnitState(UnitState.Casting);

            // Unsummon summon as possessed creatures on spell cancel
            if (SpellInfo.IsChanneled() &&
                unitCaster.IsTypeId(TypeId.Player))
            {
                Unit charm = unitCaster.GetCharmed();

                if (charm != null)
                    if (charm.IsTypeId(TypeId.Unit) &&
                        charm.ToCreature().HasUnitTypeMask(UnitTypeMask.Puppet) &&
                        charm.UnitData.CreatedBySpell == SpellInfo.Id)
                        ((Puppet)charm).UnSummon();
            }

            Creature creatureCaster = unitCaster.ToCreature();

            creatureCaster?.ReleaseSpellFocus(this);

            if (!SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
                Unit.ProcSkillsAndAuras(unitCaster, null, new ProcFlagsInit(ProcFlags.CastEnded), new ProcFlagsInit(), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, this, null, null);

            if (!ok)
            {
                // on failure (or manual cancel) send TraitConfigCommitFailed to revert talent UI saved config selection
                if (_caster.IsPlayer() &&
                    SpellInfo.HasEffect(SpellEffectName.ChangeActiveCombatTraitConfig))
                    if (CustomArg is TraitConfig)
                        _caster.ToPlayer().SendPacket(new TraitConfigCommitFailed((CustomArg as TraitConfig).ID));

                return;
            }

            if (unitCaster.IsTypeId(TypeId.Unit) &&
                unitCaster.ToCreature().IsSummon())
            {
                // Unsummon statue
                uint spell = unitCaster.UnitData.CreatedBySpell;
                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell, GetCastDifficulty());

                if (spellInfo != null &&
                    spellInfo.IconFileDataId == 134230)
                {
                    Log.outDebug(LogFilter.Spells, "Statue {0} is unsummoned in spell {1} finish", unitCaster.GetGUID().ToString(), SpellInfo.Id);

                    // Avoid infinite loops with setDeathState(JUST_DIED) being called over and over
                    // It might make sense to do this check in Unit::setDeathState() and all overloaded functions
                    if (unitCaster.GetDeathState() != DeathState.JustDied)
                        unitCaster.SetDeathState(DeathState.JustDied);

                    return;
                }
            }

            if (IsAutoActionResetSpell())
                if (!SpellInfo.HasAttribute(SpellAttr2.DoNotResetCombatTimers))
                {
                    unitCaster.ResetAttackTimer(WeaponAttackType.BaseAttack);

                    if (unitCaster.HaveOffhandWeapon())
                        unitCaster.ResetAttackTimer(WeaponAttackType.OffAttack);

                    unitCaster.ResetAttackTimer(WeaponAttackType.RangedAttack);
                }

            // potions disabled by client, send event "not in combat" if need
            if (unitCaster.IsTypeId(TypeId.Player))
                if (TriggeredByAuraSpell == null)
                    unitCaster.ToPlayer().UpdatePotionCooldown(this);

            // Stop Attack for some spells
            if (SpellInfo.HasAttribute(SpellAttr0.CancelsAutoAttackCombat))
                unitCaster.AttackStop();
        }

        public void SendCastResult(SpellCastResult result, int? param1 = null, int? param2 = null)
        {
            if (result == SpellCastResult.SpellCastOk)
                return;

            if (!_caster.IsTypeId(TypeId.Player))
                return;

            if (_caster.ToPlayer().IsLoading()) // don't send cast results at loading Time
                return;

            if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
                result = SpellCastResult.DontReport;

            CastFailed castFailed = new();
            castFailed.Visual = SpellVisual;
            FillSpellCastFailedArgs(castFailed, CastId, SpellInfo, result, CustomError, param1, param2, _caster.ToPlayer());
            _caster.ToPlayer().SendPacket(castFailed);
        }

        public void SendPetCastResult(SpellCastResult result, int? param1 = null, int? param2 = null)
        {
            if (result == SpellCastResult.SpellCastOk)
                return;

            Unit owner = _caster.GetCharmerOrOwner();

            if (!owner ||
                !owner.IsTypeId(TypeId.Player))
                return;

            if (_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DontReportCastError))
                result = SpellCastResult.DontReport;

            PetCastFailed petCastFailed = new();
            FillSpellCastFailedArgs(petCastFailed, CastId, SpellInfo, result, SpellCustomErrors.None, param1, param2, owner.ToPlayer());
            owner.ToPlayer().SendPacket(petCastFailed);
        }

        public static void SendCastResult(Player caster, SpellInfo spellInfo, SpellCastVisual spellVisual, ObjectGuid castCount, SpellCastResult result, SpellCustomErrors customError = SpellCustomErrors.None, int? param1 = null, int? param2 = null)
        {
            if (result == SpellCastResult.SpellCastOk)
                return;

            CastFailed packet = new();
            packet.Visual = spellVisual;
            FillSpellCastFailedArgs(packet, castCount, spellInfo, result, customError, param1, param2, caster);
            caster.SendPacket(packet);
        }

        public SpellLogEffect GetExecuteLogEffect(SpellEffectName effect)
        {
            var spellLogEffect = _executeLogEffects.LookupByKey(effect);

            if (spellLogEffect != null)
                return spellLogEffect;

            SpellLogEffect executeLogEffect = new();
            executeLogEffect.Effect = (int)effect;
            _executeLogEffects.Add(effect, executeLogEffect);

            return executeLogEffect;
        }

        public void SendChannelUpdate(uint time)
        {
            // GameObjects don't channel
            Unit unitCaster = _caster.ToUnit();

            if (unitCaster == null)
                return;

            if (time == 0)
            {
                unitCaster.ClearChannelObjects();
                unitCaster.SetChannelSpellId(0);
                unitCaster.SetChannelVisual(new SpellCastVisualField());
            }

            SpellChannelUpdate spellChannelUpdate = new();
            spellChannelUpdate.CasterGUID = unitCaster.GetGUID();
            spellChannelUpdate.TimeRemaining = (int)time;
            unitCaster.SendMessageToSet(spellChannelUpdate, true);
        }

        public void HandleEffects(Unit pUnitTarget, Item pItemTarget, GameObject pGoTarget, Corpse pCorpseTarget, SpellEffectInfo spellEffectInfo, SpellEffectHandleMode mode)
        {
            _effectHandleMode = mode;
            UnitTarget = pUnitTarget;
            ItemTarget = pItemTarget;
            GameObjTarget = pGoTarget;
            CorpseTarget = pCorpseTarget;
            DestTarget = _destTargets[spellEffectInfo.EffectIndex].Position;
            EffectInfo = spellEffectInfo;

            Damage = CalculateDamage(spellEffectInfo, UnitTarget, out Variance);

            bool preventDefault = CallScriptEffectHandlers(spellEffectInfo.EffectIndex, mode);

            if (!preventDefault)
                Global.SpellMgr.GetSpellEffectHandler(spellEffectInfo.Effect).Invoke(this);
        }

        public static Spell ExtractSpellFromEvent(BasicEvent basicEvent)
        {
            SpellEvent spellEvent = (SpellEvent)basicEvent;

            if (spellEvent != null)
                return spellEvent.GetSpell();

            return null;
        }

        public SpellCastResult CheckCast(bool strict)
        {
            int param1 = 0, param2 = 0;

            return CheckCast(strict, ref param1, ref param2);
        }

        public SpellCastResult CheckCast(bool strict, ref int param1, ref int param2)
        {
            SpellCastResult castResult;

            // check death State
            if (_caster.ToUnit() &&
                !_caster.ToUnit().IsAlive() &&
                !SpellInfo.IsPassive() &&
                !(SpellInfo.HasAttribute(SpellAttr0.AllowCastWhileDead) || (IsTriggered() && TriggeredByAuraSpell == null)))
                return SpellCastResult.CasterDead;

            // Prevent cheating in case the player has an immunity effect and tries to interact with a non-allowed gameobject. The error message is handled by the client so we don't report anything here
            if (_caster.IsPlayer() &&
                Targets.GetGOTarget() != null)
                if (Targets.GetGOTarget().GetGoInfo().GetNoDamageImmune() != 0 &&
                    _caster.ToUnit().HasUnitFlag(UnitFlags.Immune))
                    return SpellCastResult.DontReport;

            // check cooldowns to prevent cheating
            if (!SpellInfo.IsPassive())
            {
                Player playerCaster = _caster.ToPlayer();

                if (playerCaster != null)
                {
                    //can cast triggered (by aura only?) spells while have this flag
                    if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAurastate))
                    {
                        // These two Auras check SpellFamilyName defined by db2 class _data instead of current spell SpellFamilyName
                        if (playerCaster.HasAuraType(AuraType.DisableCastingExceptAbilities) &&
                            !SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) &&
                            !SpellInfo.HasEffect(SpellEffectName.Attack) &&
                            !SpellInfo.HasAttribute(SpellAttr12.IgnoreCastingDisabled) &&
                            !playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableCastingExceptAbilities, CliDB.ChrClassesStorage.LookupByKey(playerCaster.GetClass()).SpellClassSet, SpellInfo.SpellFamilyFlags))
                            return SpellCastResult.CantDoThatRightNow;

                        if (playerCaster.HasAuraType(AuraType.DisableAttackingExceptAbilities))
                            if (!playerCaster.HasAuraTypeWithFamilyFlags(AuraType.DisableAttackingExceptAbilities, CliDB.ChrClassesStorage.LookupByKey(playerCaster.GetClass()).SpellClassSet, SpellInfo.SpellFamilyFlags))
                                if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) ||
                                    SpellInfo.IsNextMeleeSwingSpell() ||
                                    SpellInfo.HasAttribute(SpellAttr1.InitiatesCombatEnablesAutoAttack) ||
                                    SpellInfo.HasAttribute(SpellAttr2.InitiateCombatPostCastEnablesAutoAttack) ||
                                    SpellInfo.HasEffect(SpellEffectName.Attack) ||
                                    SpellInfo.HasEffect(SpellEffectName.NormalizedWeaponDmg) ||
                                    SpellInfo.HasEffect(SpellEffectName.WeaponDamageNoSchool) ||
                                    SpellInfo.HasEffect(SpellEffectName.WeaponPercentDamage) ||
                                    SpellInfo.HasEffect(SpellEffectName.WeaponDamage))
                                    return SpellCastResult.CantDoThatRightNow;
                    }

                    // check if we are using a potion in combat for the 2nd+ Time. Cooldown is added only after caster gets out of combat
                    if (!IsIgnoringCooldowns() &&
                        playerCaster.GetLastPotionId() != 0 &&
                        CastItem &&
                        (CastItem.IsPotion() || SpellInfo.IsCooldownStartedOnEvent()))
                        return SpellCastResult.NotReady;
                }

                if (!IsIgnoringCooldowns() &&
                    _caster.ToUnit() != null)
                {
                    if (!_caster.ToUnit().GetSpellHistory().IsReady(SpellInfo, CastItemEntry))
                    {
                        if (TriggeredByAuraSpell != null)
                            return SpellCastResult.DontReport;
                        else
                            return SpellCastResult.NotReady;
                    }

                    if ((IsAutoRepeat() || SpellInfo.CategoryId == 76) &&
                        !_caster.ToUnit().IsAttackReady(WeaponAttackType.RangedAttack))
                        return SpellCastResult.DontReport;
                }
            }

            if (SpellInfo.HasAttribute(SpellAttr7.IsCheatSpell) &&
                _caster.IsUnit() &&
                !_caster.ToUnit().HasUnitFlag2(UnitFlags2.AllowCheatSpells))
            {
                CustomError = SpellCustomErrors.GmOnly;

                return SpellCastResult.CustomError;
            }

            // Check global cooldown
            if (strict &&
                !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreGCD) &&
                HasGlobalCooldown())
                return !SpellInfo.HasAttribute(SpellAttr0.CooldownOnEvent) ? SpellCastResult.NotReady : SpellCastResult.DontReport;

            // only triggered spells can be processed an ended Battleground
            if (!IsTriggered() &&
                _caster.IsTypeId(TypeId.Player))
            {
                Battleground bg = _caster.ToPlayer().GetBattleground();

                if (bg)
                    if (bg.GetStatus() == BattlegroundStatus.WaitLeave)
                        return SpellCastResult.DontReport;
            }

            if (_caster.IsTypeId(TypeId.Player) &&
                Global.VMapMgr.IsLineOfSightCalcEnabled())
            {
                if (SpellInfo.HasAttribute(SpellAttr0.OnlyOutdoors) &&
                    !_caster.IsOutdoors())
                    return SpellCastResult.OnlyOutdoors;

                if (SpellInfo.HasAttribute(SpellAttr0.OnlyIndoors) &&
                    _caster.IsOutdoors())
                    return SpellCastResult.OnlyIndoors;
            }

            Unit unitCaster = _caster.ToUnit();

            if (unitCaster != null)
            {
                if (SpellInfo.HasAttribute(SpellAttr5.NotAvailableWhileCharmed) &&
                    unitCaster.IsCharmed())
                    return SpellCastResult.Charmed;

                // only check at first call, Stealth Auras are already removed at second call
                // for now, ignore triggered spells
                if (strict && !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreShapeshift))
                {
                    bool checkForm = true;
                    // Ignore form req aura
                    var ignore = unitCaster.GetAuraEffectsByType(AuraType.ModIgnoreShapeshift);

                    foreach (var aurEff in ignore)
                    {
                        if (!aurEff.IsAffectingSpell(SpellInfo))
                            continue;

                        checkForm = false;

                        break;
                    }

                    if (checkForm)
                    {
                        // Cannot be used in this stance/form
                        SpellCastResult shapeError = SpellInfo.CheckShapeshift(unitCaster.GetShapeshiftForm());

                        if (shapeError != SpellCastResult.SpellCastOk)
                            return shapeError;

                        if (SpellInfo.HasAttribute(SpellAttr0.OnlyStealthed) &&
                            !unitCaster.HasStealthAura())
                            return SpellCastResult.OnlyStealthed;
                    }
                }

                bool reqCombat = true;
                var stateAuras = unitCaster.GetAuraEffectsByType(AuraType.AbilityIgnoreAurastate);

                foreach (var aura in stateAuras)
                    if (aura.IsAffectingSpell(SpellInfo))
                    {
                        NeedComboPoints = false;

                        if (aura.GetMiscValue() == 1)
                        {
                            reqCombat = false;

                            break;
                        }
                    }

                // caster State requirements
                // not for triggered spells (needed by execute)
                if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCasterAurastate))
                {
                    if (SpellInfo.CasterAuraState != 0 &&
                        !unitCaster.HasAuraState(SpellInfo.CasterAuraState, SpellInfo, unitCaster))
                        return SpellCastResult.CasterAurastate;

                    if (SpellInfo.ExcludeCasterAuraState != 0 &&
                        unitCaster.HasAuraState(SpellInfo.ExcludeCasterAuraState, SpellInfo, unitCaster))
                        return SpellCastResult.CasterAurastate;

                    // Note: spell 62473 requres casterAuraSpell = triggering spell
                    if (SpellInfo.CasterAuraSpell != 0 &&
                        !unitCaster.HasAura(SpellInfo.CasterAuraSpell))
                        return SpellCastResult.CasterAurastate;

                    if (SpellInfo.ExcludeCasterAuraSpell != 0 &&
                        unitCaster.HasAura(SpellInfo.ExcludeCasterAuraSpell))
                        return SpellCastResult.CasterAurastate;

                    if (SpellInfo.CasterAuraType != 0 &&
                        !unitCaster.HasAuraType(SpellInfo.CasterAuraType))
                        return SpellCastResult.CasterAurastate;

                    if (SpellInfo.ExcludeCasterAuraType != 0 &&
                        unitCaster.HasAuraType(SpellInfo.ExcludeCasterAuraType))
                        return SpellCastResult.CasterAurastate;

                    if (reqCombat &&
                        unitCaster.IsInCombat() &&
                        !SpellInfo.CanBeUsedInCombat())
                        return SpellCastResult.AffectingCombat;
                }

                // Check vehicle Flags
                if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterMountedOrOnVehicle))
                {
                    SpellCastResult vehicleCheck = SpellInfo.CheckVehicle(unitCaster);

                    if (vehicleCheck != SpellCastResult.SpellCastOk)
                        return vehicleCheck;
                }
            }

            // check spell cast conditions from database
            {
                ConditionSourceInfo condInfo = new(_caster, Targets.GetObjectTarget());

                if (!Global.ConditionMgr.IsObjectMeetingNotGroupedConditions(ConditionSourceType.Spell, SpellInfo.Id, condInfo))
                {
                    // mLastFailedCondition can be NULL if there was an error processing the condition in Condition.Meets (i.e. wrong _data for ConditionTarget or others)
                    if (condInfo.LastFailedCondition != null &&
                        condInfo.LastFailedCondition.ErrorType != 0)
                    {
                        if (condInfo.LastFailedCondition.ErrorType == (uint)SpellCastResult.CustomError)
                            CustomError = (SpellCustomErrors)condInfo.LastFailedCondition.ErrorTextId;

                        return (SpellCastResult)condInfo.LastFailedCondition.ErrorType;
                    }

                    if (condInfo.LastFailedCondition == null ||
                        condInfo.LastFailedCondition.ConditionTarget == 0)
                        return SpellCastResult.CasterAurastate;

                    return SpellCastResult.BadTargets;
                }
            }

            // Don't check explicit Target for passive spells (workaround) (check should be skipped only for learn case)
            // those spells may have incorrect Target entries or not filled at all (for example 15332)
            // such spells when learned are not targeting anyone using targeting system, they should apply directly to caster instead
            // also, such casts shouldn't be sent to client
            if (!(SpellInfo.IsPassive() && (Targets.GetUnitTarget() == null || Targets.GetUnitTarget() == _caster)))
            {
                // Check explicit Target for _originalCaster - todo: get rid of such workarounds
                WorldObject caster = _caster;

                // in case of gameobjects like traps, we need the gameobject itself to check Target validity
                // otherwise, if originalCaster is far away and cannot detect the Target, the trap would not hit the Target
                if (_originalCaster != null &&
                    !caster.IsGameObject())
                    caster = _originalCaster;

                castResult = SpellInfo.CheckExplicitTarget(caster, Targets.GetObjectTarget(), Targets.GetItemTarget());

                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;
            }

            Unit unitTarget = Targets.GetUnitTarget();

            if (unitTarget != null)
            {
                castResult = SpellInfo.CheckTarget(_caster, unitTarget, _caster.IsGameObject()); // skip stealth checks for GO casts

                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;

                // If it's not a melee spell, check if vision is obscured by SPELL_AURA_INTERFERE_TARGETTING
                if (SpellInfo.DmgClass != SpellDmgClass.Melee)
                {
                    Unit unitCaster1 = _caster.ToUnit();

                    if (unitCaster1 != null)
                    {
                        foreach (var auraEffect in unitCaster1.GetAuraEffectsByType(AuraType.InterfereTargetting))
                            if (!unitCaster1.IsFriendlyTo(auraEffect.GetCaster()) &&
                                !unitTarget.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID()))
                                return SpellCastResult.VisionObscured;

                        foreach (var auraEffect in unitTarget.GetAuraEffectsByType(AuraType.InterfereTargetting))
                            if (!unitCaster1.IsFriendlyTo(auraEffect.GetCaster()) &&
                                (!unitTarget.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID()) || !unitCaster1.HasAura(auraEffect.GetId(), auraEffect.GetCasterGUID())))
                                return SpellCastResult.VisionObscured;
                    }
                }

                if (unitTarget != _caster)
                {
                    // Must be behind the Target
                    if (SpellInfo.HasAttribute(SpellCustomAttributes.ReqCasterBehindTarget) &&
                        unitTarget.HasInArc(MathFunctions.PI, _caster))
                        return SpellCastResult.NotBehind;

                    // Target must be facing you
                    if (SpellInfo.HasAttribute(SpellCustomAttributes.ReqTargetFacingCaster) &&
                        !unitTarget.HasInArc(MathFunctions.PI, _caster))
                        return SpellCastResult.NotInfront;

                    // Ignore LOS for gameobjects casts
                    if (!_caster.IsGameObject())
                    {
                        WorldObject losTarget = _caster;

                        if (IsTriggered() &&
                            TriggeredByAuraSpell != null)
                        {
                            DynamicObject dynObj = _caster.ToUnit().GetDynObject(TriggeredByAuraSpell.Id);

                            if (dynObj)
                                losTarget = dynObj;
                        }

                        if (!SpellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) &&
                            !Global.DisableMgr.IsDisabledFor(DisableType.Spell, SpellInfo.Id, null, (byte)DisableFlags.SpellLOS) &&
                            !unitTarget.IsWithinLOSInMap(losTarget, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                            return SpellCastResult.LineOfSight;
                    }
                }
            }

            // Check for line of sight for spells with dest
            if (Targets.HasDst())
            {
                float x, y, z;
                Targets.GetDstPos().GetPosition(out x, out y, out z);

                if (!SpellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) &&
                    !Global.DisableMgr.IsDisabledFor(DisableType.Spell, SpellInfo.Id, null, (byte)DisableFlags.SpellLOS) &&
                    !_caster.IsWithinLOS(x, y, z, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                    return SpellCastResult.LineOfSight;
            }

            // check pet presence
            if (unitCaster != null)
            {
                if (SpellInfo.HasAttribute(SpellAttr2.NoActivePets))
                    if (!unitCaster.GetPetGUID().IsEmpty())
                        return SpellCastResult.AlreadyHavePet;

                foreach (var spellEffectInfo in SpellInfo.GetEffects())
                    if (spellEffectInfo.TargetA.GetTarget() == Framework.Constants.Targets.UnitPet)
                    {
                        if (unitCaster.GetGuardianPet() == null)
                        {
                            if (TriggeredByAuraSpell != null) // not report pet not existence for triggered spells
                                return SpellCastResult.DontReport;
                            else
                                return SpellCastResult.NoPet;
                        }

                        break;
                    }
            }

            // Spell casted only on Battleground
            if (SpellInfo.HasAttribute(SpellAttr3.OnlyBattlegrounds))
                if (!_caster.GetMap().IsBattleground())
                    return SpellCastResult.OnlyBattlegrounds;

            // do not allow spells to be cast in arenas or rated Battlegrounds
            Player player = _caster.ToPlayer();

            if (player != null)
                if (player.InArena() /* || player.InRatedBattleground() NYI*/)
                {
                    castResult = CheckArenaAndRatedBattlegroundCastRules();

                    if (castResult != SpellCastResult.SpellCastOk)
                        return castResult;
                }

            // zone check
            if (!_caster.IsPlayer() ||
                !_caster.ToPlayer().IsGameMaster())
            {
                uint zone, area;
                _caster.GetZoneAndAreaId(out zone, out area);

                SpellCastResult locRes = SpellInfo.CheckLocation(_caster.GetMapId(), zone, area, _caster.ToPlayer());

                if (locRes != SpellCastResult.SpellCastOk)
                    return locRes;
            }

            // not let players cast spells at Mount (and let do it to creatures)
            if (!_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCasterMountedOrOnVehicle))
                if (_caster.IsPlayer() &&
                    _caster.ToPlayer().IsMounted() &&
                    !SpellInfo.IsPassive() &&
                    !SpellInfo.HasAttribute(SpellAttr0.AllowWhileMounted))
                {
                    if (_caster.ToPlayer().IsInFlight())
                        return SpellCastResult.NotOnTaxi;
                    else
                        return SpellCastResult.NotMounted;
                }

            // check spell focus object
            if (SpellInfo.RequiresSpellFocus != 0)
                if (!_caster.IsUnit() ||
                    !_caster.ToUnit().HasAuraTypeWithMiscvalue(AuraType.ProvideSpellFocus, (int)SpellInfo.RequiresSpellFocus))
                {
                    _focusObject = SearchSpellFocus();

                    if (!_focusObject)
                        return SpellCastResult.RequiresSpellFocus;
                }

            // always (except passive spells) check items (focus object can be required for any Type casts)
            if (!SpellInfo.IsPassive())
            {
                castResult = CheckItems(ref param1, ref param2);

                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;
            }

            // Triggered spells also have range check
            // @todo determine if there is some flag to enable/disable the check
            castResult = CheckRange(strict);

            if (castResult != SpellCastResult.SpellCastOk)
                return castResult;

            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost))
            {
                castResult = CheckPower();

                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;
            }

            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCasterAuras))
            {
                castResult = CheckCasterAuras(ref param1);

                if (castResult != SpellCastResult.SpellCastOk)
                    return castResult;
            }

            // script hook
            castResult = CallScriptCheckCastHandlers();

            if (castResult != SpellCastResult.SpellCastOk)
                return castResult;

            uint approximateAuraEffectMask = 0;
            uint nonAuraEffectMask = 0;

            foreach (var spellEffectInfo in SpellInfo.GetEffects())
            {
                // for effects of spells that have only one Target
                switch (spellEffectInfo.Effect)
                {
                    case SpellEffectName.Dummy:
                        {
                            if (SpellInfo.Id == 19938) // Awaken Peon
                            {
                                Unit unit = Targets.GetUnitTarget();

                                if (unit == null ||
                                    !unit.HasAura(17743))
                                    return SpellCastResult.BadTargets;
                            }
                            else if (SpellInfo.Id == 31789) // Righteous Defense
                            {
                                if (!_caster.IsTypeId(TypeId.Player))
                                    return SpellCastResult.DontReport;

                                Unit target = Targets.GetUnitTarget();

                                if (target == null ||
                                    !target.IsFriendlyTo(_caster) ||
                                    target.GetAttackers().Empty())
                                    return SpellCastResult.BadTargets;
                            }

                            break;
                        }
                    case SpellEffectName.LearnSpell:
                        {
                            if (spellEffectInfo.TargetA.GetTarget() != Framework.Constants.Targets.UnitPet)
                                break;

                            Pet pet = _caster.ToPlayer().GetPet();

                            if (pet == null)
                                return SpellCastResult.NoPet;

                            SpellInfo learn_spellproto = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, Difficulty.None);

                            if (learn_spellproto == null)
                                return SpellCastResult.NotKnown;

                            if (SpellInfo.SpellLevel > pet.GetLevel())
                                return SpellCastResult.Lowlevel;

                            break;
                        }
                    case SpellEffectName.UnlockGuildVaultTab:
                        {
                            if (!_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.BadTargets;

                            var guild = _caster.ToPlayer().GetGuild();

                            if (guild != null)
                                if (guild.GetLeaderGUID() != _caster.ToPlayer().GetGUID())
                                    return SpellCastResult.CantDoThatRightNow;

                            break;
                        }
                    case SpellEffectName.LearnPetSpell:
                        {
                            // check Target only for unit Target case
                            Unit target = Targets.GetUnitTarget();

                            if (target != null)
                            {
                                if (!_caster.IsTypeId(TypeId.Player))
                                    return SpellCastResult.BadTargets;

                                Pet pet = target.ToPet();

                                if (pet == null ||
                                    pet.GetOwner() != _caster)
                                    return SpellCastResult.BadTargets;

                                SpellInfo learn_spellproto = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, Difficulty.None);

                                if (learn_spellproto == null)
                                    return SpellCastResult.NotKnown;

                                if (SpellInfo.SpellLevel > pet.GetLevel())
                                    return SpellCastResult.Lowlevel;
                            }

                            break;
                        }
                    case SpellEffectName.ApplyGlyph:
                        {
                            if (!_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.GlyphNoSpec;

                            Player caster = _caster.ToPlayer();

                            if (!caster.HasSpell(Misc.SpellId))
                                return SpellCastResult.NotKnown;

                            uint glyphId = (uint)spellEffectInfo.MiscValue;

                            if (glyphId != 0)
                            {
                                GlyphPropertiesRecord glyphProperties = CliDB.GlyphPropertiesStorage.LookupByKey(glyphId);

                                if (glyphProperties == null)
                                    return SpellCastResult.InvalidGlyph;

                                List<uint> glyphBindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(glyphId);

                                if (glyphBindableSpells.Empty())
                                    return SpellCastResult.InvalidGlyph;

                                if (!glyphBindableSpells.Contains(Misc.SpellId))
                                    return SpellCastResult.InvalidGlyph;

                                List<uint> glyphRequiredSpecs = Global.DB2Mgr.GetGlyphRequiredSpecs(glyphId);

                                if (!glyphRequiredSpecs.Empty())
                                {
                                    if (caster.GetPrimarySpecialization() == 0)
                                        return SpellCastResult.GlyphNoSpec;

                                    if (!glyphRequiredSpecs.Contains(caster.GetPrimarySpecialization()))
                                        return SpellCastResult.GlyphInvalidSpec;
                                }

                                uint replacedGlyph = 0;

                                foreach (uint activeGlyphId in caster.GetGlyphs(caster.GetActiveTalentGroup()))
                                {
                                    List<uint> activeGlyphBindableSpells = Global.DB2Mgr.GetGlyphBindableSpells(activeGlyphId);

                                    if (!activeGlyphBindableSpells.Empty())
                                        if (activeGlyphBindableSpells.Contains(Misc.SpellId))
                                        {
                                            replacedGlyph = activeGlyphId;

                                            break;
                                        }
                                }

                                foreach (uint activeGlyphId in caster.GetGlyphs(caster.GetActiveTalentGroup()))
                                {
                                    if (activeGlyphId == replacedGlyph)
                                        continue;

                                    if (activeGlyphId == glyphId)
                                        return SpellCastResult.UniqueGlyph;

                                    if (CliDB.GlyphPropertiesStorage.LookupByKey(activeGlyphId).GlyphExclusiveCategoryID == glyphProperties.GlyphExclusiveCategoryID)
                                        return SpellCastResult.GlyphExclusiveCategory;
                                }
                            }

                            break;
                        }
                    case SpellEffectName.FeedPet:
                        {
                            if (!_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.BadTargets;

                            Item foodItem = Targets.GetItemTarget();

                            if (!foodItem)
                                return SpellCastResult.BadTargets;

                            Pet pet = _caster.ToPlayer().GetPet();

                            if (!pet)
                                return SpellCastResult.NoPet;

                            if (!pet.HaveInDiet(foodItem.GetTemplate()))
                                return SpellCastResult.WrongPetFood;

                            if (foodItem.GetTemplate().GetBaseItemLevel() + 30 <= pet.GetLevel())
                                return SpellCastResult.FoodLowlevel;

                            if (_caster.ToPlayer().IsInCombat() ||
                                pet.IsInCombat())
                                return SpellCastResult.AffectingCombat;

                            break;
                        }
                    case SpellEffectName.Charge:
                        {
                            if (unitCaster == null)
                                return SpellCastResult.BadTargets;

                            if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreCasterAuras) &&
                                unitCaster.HasUnitState(UnitState.Root))
                                return SpellCastResult.Rooted;

                            if (GetSpellInfo().NeedsExplicitUnitTarget())
                            {
                                Unit target = Targets.GetUnitTarget();

                                if (target == null)
                                    return SpellCastResult.DontReport;

                                // first we must check to see if the Target is in LoS. A path can usually be built but LoS matters for charge spells
                                if (!target.IsWithinLOSInMap(unitCaster)) //Do full LoS/Path check. Don't exclude m2
                                    return SpellCastResult.LineOfSight;

                                float objSize = target.GetCombatReach();
                                float range = SpellInfo.GetMaxRange(true, unitCaster, this) * 1.5f + objSize; // can't be overly strict

                                _preGeneratedPath = new PathGenerator(unitCaster);
                                _preGeneratedPath.SetPathLengthLimit(range);

                                // first try with raycast, if it fails fall back to normal path
                                bool result = _preGeneratedPath.CalculatePath(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ(), false);

                                if (_preGeneratedPath.GetPathType().HasAnyFlag(PathType.Short))
                                    return SpellCastResult.NoPath;
                                else if (!result ||
                                         _preGeneratedPath.GetPathType().HasAnyFlag(PathType.NoPath | PathType.Incomplete))
                                    return SpellCastResult.NoPath;
                                else if (_preGeneratedPath.IsInvalidDestinationZ(target)) // Check position z, if not in a straight line
                                    return SpellCastResult.NoPath;

                                _preGeneratedPath.ShortenPathUntilDist(target, objSize); //move back
                            }

                            break;
                        }
                    case SpellEffectName.Skinning:
                        {
                            if (!_caster.IsTypeId(TypeId.Player) ||
                                Targets.GetUnitTarget() == null ||
                                !Targets.GetUnitTarget().IsTypeId(TypeId.Unit))
                                return SpellCastResult.BadTargets;

                            if (!Targets.GetUnitTarget().HasUnitFlag(UnitFlags.Skinnable))
                                return SpellCastResult.TargetUnskinnable;

                            Creature creature = Targets.GetUnitTarget().ToCreature();
                            Loot loot = creature.GetLootForPlayer(_caster.ToPlayer());

                            if (loot != null &&
                                (!loot.IsLooted() || loot.Loot_type == LootType.Skinning))
                                return SpellCastResult.TargetNotLooted;

                            SkillType skill = creature.GetCreatureTemplate().GetRequiredLootSkill();

                            ushort skillValue = _caster.ToPlayer().GetSkillValue(skill);
                            uint TargetLevel = Targets.GetUnitTarget().GetLevelForTarget(_caster);
                            int ReqValue = (int)(skillValue < 100 ? (TargetLevel - 10) * 10 : TargetLevel * 5);

                            if (ReqValue > skillValue)
                                return SpellCastResult.LowCastlevel;

                            break;
                        }
                    case SpellEffectName.OpenLock:
                        {
                            if (spellEffectInfo.TargetA.GetTarget() != Framework.Constants.Targets.GameobjectTarget &&
                                spellEffectInfo.TargetA.GetTarget() != Framework.Constants.Targets.GameobjectItemTarget)
                                break;

                            if (!_caster.IsTypeId(TypeId.Player) // only players can open locks, gather etc.
                                                                 // we need a go Target in case of TARGET_GAMEOBJECT_TARGET
                                ||
                                (spellEffectInfo.TargetA.GetTarget() == Framework.Constants.Targets.GameobjectTarget && Targets.GetGOTarget() == null))
                                return SpellCastResult.BadTargets;

                            Item pTempItem = null;

                            if (Convert.ToBoolean(Targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
                            {
                                TradeData pTrade = _caster.ToPlayer().GetTradeData();

                                if (pTrade != null)
                                    pTempItem = pTrade.GetTraderData().GetItem(TradeSlots.NonTraded);
                            }
                            else if (Convert.ToBoolean(Targets.GetTargetMask() & SpellCastTargetFlags.Item))
                            {
                                pTempItem = _caster.ToPlayer().GetItemByGuid(Targets.GetItemTargetGUID());
                            }

                            // we need a go Target, or an openable Item Target in case of TARGET_GAMEOBJECT_ITEM_TARGET
                            if (spellEffectInfo.TargetA.GetTarget() == Framework.Constants.Targets.GameobjectItemTarget &&
                                Targets.GetGOTarget() == null &&
                                (pTempItem == null || pTempItem.GetTemplate().GetLockID() == 0 || !pTempItem.IsLocked()))
                                return SpellCastResult.BadTargets;

                            if (SpellInfo.Id != 1842 ||
                                (Targets.GetGOTarget() != null &&
                                 Targets.GetGOTarget().GetGoInfo().type != GameObjectTypes.Trap))
                                if (_caster.ToPlayer().InBattleground() && // In Battlegroundplayers can use only Flags and banners
                                    !_caster.ToPlayer().CanUseBattlegroundObject(Targets.GetGOTarget()))
                                    return SpellCastResult.TryAgain;

                            // get the lock entry
                            uint lockId = 0;
                            GameObject go = Targets.GetGOTarget();
                            Item itm = Targets.GetItemTarget();

                            if (go != null)
                            {
                                lockId = go.GetGoInfo().GetLockId();

                                if (lockId == 0)
                                    return SpellCastResult.BadTargets;

                                if (go.GetGoInfo().GetNotInCombat() != 0 &&
                                    _caster.ToUnit().IsInCombat())
                                    return SpellCastResult.AffectingCombat;
                            }
                            else if (itm != null)
                            {
                                lockId = itm.GetTemplate().GetLockID();
                            }

                            SkillType skillId = SkillType.None;
                            int reqSkillValue = 0;
                            int skillValue = 0;

                            // check lock compatibility
                            SpellCastResult res = CanOpenLock(spellEffectInfo, lockId, ref skillId, ref reqSkillValue, ref skillValue);

                            if (res != SpellCastResult.SpellCastOk)
                                return res;

                            break;
                        }
                    case SpellEffectName.ResurrectPet:
                        {
                            Player playerCaster = _caster.ToPlayer();

                            if (playerCaster == null ||
                                playerCaster.GetPetStable() == null)
                                return SpellCastResult.BadTargets;

                            Pet pet = playerCaster.GetPet();

                            if (pet != null &&
                                pet.IsAlive())
                                return SpellCastResult.AlreadyHaveSummon;

                            PetStable petStable = playerCaster.GetPetStable();
                            var deadPetInfo = petStable.ActivePets.FirstOrDefault(petInfo => petInfo?.Health == 0);

                            if (deadPetInfo == null)
                                return SpellCastResult.BadTargets;

                            break;
                        }
                    // This is generic summon effect
                    case SpellEffectName.Summon:
                        {
                            if (unitCaster == null)
                                break;

                            var SummonProperties = CliDB.SummonPropertiesStorage.LookupByKey(spellEffectInfo.MiscValueB);

                            if (SummonProperties == null)
                                break;

                            switch (SummonProperties.Control)
                            {
                                case SummonCategory.Pet:
                                    if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst) &&
                                        !unitCaster.GetPetGUID().IsEmpty())
                                        return SpellCastResult.AlreadyHaveSummon;

                                    goto case SummonCategory.Puppet;
                                case SummonCategory.Puppet:
                                    if (!unitCaster.GetCharmedGUID().IsEmpty())
                                        return SpellCastResult.AlreadyHaveCharm;

                                    break;
                            }

                            break;
                        }
                    case SpellEffectName.CreateTamedPet:
                        {
                            if (Targets.GetUnitTarget() != null)
                            {
                                if (!Targets.GetUnitTarget().IsTypeId(TypeId.Player))
                                    return SpellCastResult.BadTargets;

                                if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst) &&
                                    !Targets.GetUnitTarget().GetPetGUID().IsEmpty())
                                    return SpellCastResult.AlreadyHaveSummon;
                            }

                            break;
                        }
                    case SpellEffectName.SummonPet:
                        {
                            if (unitCaster == null)
                                return SpellCastResult.BadTargets;

                            if (!unitCaster.GetPetGUID().IsEmpty()) //let warlock do a replacement summon
                            {
                                if (unitCaster.IsTypeId(TypeId.Player))
                                {
                                    if (strict) //starting cast, trigger pet stun (cast by pet so it doesn't attack player)
                                    {
                                        Pet pet = unitCaster.ToPlayer().GetPet();

                                        pet?.CastSpell(pet,
                                                          32752,
                                                          new CastSpellExtraArgs(TriggerCastFlags.FullMask)
                                                              .SetOriginalCaster(pet.GetGUID())
                                                              .SetTriggeringSpell(this));
                                    }
                                }
                                else if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst))
                                {
                                    return SpellCastResult.AlreadyHaveSummon;
                                }
                            }

                            if (!unitCaster.GetCharmedGUID().IsEmpty())
                                return SpellCastResult.AlreadyHaveCharm;

                            Player playerCaster = unitCaster.ToPlayer();

                            if (playerCaster != null &&
                                playerCaster.GetPetStable() != null)
                            {
                                PetSaveMode? petSlot = null;

                                if (spellEffectInfo.MiscValue == 0)
                                {
                                    petSlot = (PetSaveMode)spellEffectInfo.CalcValue();

                                    // No pet can be summoned if any pet is dead
                                    foreach (var activePet in playerCaster.GetPetStable().ActivePets)
                                        if (activePet?.Health == 0)
                                        {
                                            playerCaster.SendTameFailure(PetTameResult.Dead);

                                            return SpellCastResult.DontReport;
                                        }
                                }

                                var info = Pet.GetLoadPetInfo(playerCaster.GetPetStable(), (uint)spellEffectInfo.MiscValue, 0, petSlot);

                                if (info.Item1 != null)
                                {
                                    if (info.Item1.Type == PetType.Hunter)
                                    {
                                        CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(info.Item1.CreatureId);

                                        if (creatureInfo == null ||
                                            !creatureInfo.IsTameable(playerCaster.CanTameExoticPets()))
                                        {
                                            // if problem in exotic pet
                                            if (creatureInfo != null &&
                                                creatureInfo.IsTameable(true))
                                                playerCaster.SendTameFailure(PetTameResult.CantControlExotic);
                                            else
                                                playerCaster.SendTameFailure(PetTameResult.NoPetAvailable);

                                            return SpellCastResult.DontReport;
                                        }
                                    }
                                }
                                else if (spellEffectInfo.MiscValue == 0) // when miscvalue is present it is allowed to create new pets
                                {
                                    playerCaster.SendTameFailure(PetTameResult.NoPetAvailable);

                                    return SpellCastResult.DontReport;
                                }
                            }

                            break;
                        }
                    case SpellEffectName.DismissPet:
                        {
                            Player playerCaster = _caster.ToPlayer();

                            if (playerCaster == null)
                                return SpellCastResult.BadTargets;

                            Pet pet = playerCaster.GetPet();

                            if (pet == null)
                                return SpellCastResult.NoPet;

                            if (!pet.IsAlive())
                                return SpellCastResult.TargetsDead;

                            break;
                        }
                    case SpellEffectName.SummonPlayer:
                        {
                            if (!_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.BadTargets;

                            if (_caster.ToPlayer().GetTarget().IsEmpty())
                                return SpellCastResult.BadTargets;

                            Player target = Global.ObjAccessor.FindPlayer(_caster.ToPlayer().GetTarget());

                            if (target == null ||
                                _caster.ToPlayer() == target ||
                                (!target.IsInSameRaidWith(_caster.ToPlayer()) && SpellInfo.Id != 48955)) // refer-a-friend spell
                                return SpellCastResult.BadTargets;

                            if (target.HasSummonPending())
                                return SpellCastResult.SummonPending;

                            // check if our map is dungeon
                            InstanceMap map = _caster.GetMap().ToInstanceMap();

                            if (map != null)
                            {
                                uint mapId = map.GetId();
                                Difficulty difficulty = map.GetDifficultyID();
                                InstanceLock mapLock = map.GetInstanceLock();

                                if (mapLock != null)
                                    if (Global.InstanceLockMgr.CanJoinInstanceLock(target.GetGUID(), new MapDb2Entries(mapId, difficulty), mapLock) != TransferAbortReason.None)
                                        return SpellCastResult.TargetLockedToRaidInstance;

                                if (!target.Satisfy(Global.ObjectMgr.GetAccessRequirement(mapId, difficulty), mapId))
                                    return SpellCastResult.BadTargets;
                            }

                            break;
                        }
                    // RETURN HERE
                    case SpellEffectName.SummonRafFriend:
                        {
                            if (!_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.BadTargets;

                            Player playerCaster = _caster.ToPlayer();

                            //
                            if (playerCaster.GetTarget().IsEmpty())
                                return SpellCastResult.BadTargets;

                            Player target = Global.ObjAccessor.FindPlayer(playerCaster.GetTarget());

                            if (target == null ||
                                !(target.GetSession().GetRecruiterId() == playerCaster.GetSession().GetAccountId() || target.GetSession().GetAccountId() == playerCaster.GetSession().GetRecruiterId()))
                                return SpellCastResult.BadTargets;

                            break;
                        }
                    case SpellEffectName.Leap:
                    case SpellEffectName.TeleportUnitsFaceCaster:
                        {
                            //Do not allow to cast it before BG starts.
                            if (_caster.IsTypeId(TypeId.Player))
                            {
                                Battleground bg = _caster.ToPlayer().GetBattleground();

                                if (bg)
                                    if (bg.GetStatus() != BattlegroundStatus.InProgress)
                                        return SpellCastResult.TryAgain;
                            }

                            break;
                        }
                    case SpellEffectName.StealBeneficialBuff:
                        {
                            if (Targets.GetUnitTarget() == null ||
                                Targets.GetUnitTarget() == _caster)
                                return SpellCastResult.BadTargets;

                            break;
                        }
                    case SpellEffectName.LeapBack:
                        {
                            if (unitCaster == null)
                                return SpellCastResult.BadTargets;

                            if (unitCaster.HasUnitState(UnitState.Root))
                            {
                                if (unitCaster.IsTypeId(TypeId.Player))
                                    return SpellCastResult.Rooted;
                                else
                                    return SpellCastResult.DontReport;
                            }

                            break;
                        }
                    case SpellEffectName.Jump:
                    case SpellEffectName.JumpDest:
                        {
                            if (unitCaster == null)
                                return SpellCastResult.BadTargets;

                            if (unitCaster.HasUnitState(UnitState.Root))
                                return SpellCastResult.Rooted;

                            break;
                        }
                    case SpellEffectName.TalentSpecSelect:
                        {
                            ChrSpecializationRecord spec = CliDB.ChrSpecializationStorage.LookupByKey(Misc.SpecializationId);
                            Player playerCaster = _caster.ToPlayer();

                            if (!playerCaster)
                                return SpellCastResult.TargetNotPlayer;

                            if (spec == null ||
                                (spec.ClassID != (uint)player.GetClass() && !spec.IsPetSpecialization()))
                                return SpellCastResult.NoSpec;

                            if (spec.IsPetSpecialization())
                            {
                                Pet pet = player.GetPet();

                                if (!pet ||
                                    pet.GetPetType() != PetType.Hunter ||
                                    pet.GetCharmInfo() == null)
                                    return SpellCastResult.NoPet;
                            }

                            // can't change during already started arena/Battleground
                            Battleground bg = player.GetBattleground();

                            if (bg)
                                if (bg.GetStatus() == BattlegroundStatus.InProgress)
                                    return SpellCastResult.NotInBattleground;

                            break;
                        }
                    case SpellEffectName.RemoveTalent:
                        {
                            Player playerCaster = _caster.ToPlayer();

                            if (playerCaster == null)
                                return SpellCastResult.BadTargets;

                            TalentRecord talent = CliDB.TalentStorage.LookupByKey(Misc.TalentId);

                            if (talent == null)
                                return SpellCastResult.DontReport;

                            if (playerCaster.GetSpellHistory().HasCooldown(talent.SpellID))
                            {
                                param1 = (int)talent.SpellID;

                                return SpellCastResult.CantUntalent;
                            }

                            break;
                        }
                    case SpellEffectName.GiveArtifactPower:
                    case SpellEffectName.GiveArtifactPowerNoBonus:
                        {
                            Player playerCaster = _caster.ToPlayer();

                            if (playerCaster == null)
                                return SpellCastResult.BadTargets;

                            Aura artifactAura = playerCaster.GetAura(PlayerConst.ArtifactsAllWeaponsGeneralWeaponEquippedPassive);

                            if (artifactAura == null)
                                return SpellCastResult.NoArtifactEquipped;

                            Item artifact = playerCaster.ToPlayer().GetItemByGuid(artifactAura.GetCastItemGUID());

                            if (artifact == null)
                                return SpellCastResult.NoArtifactEquipped;

                            if (spellEffectInfo.Effect == SpellEffectName.GiveArtifactPower)
                            {
                                ArtifactRecord artifactEntry = CliDB.ArtifactStorage.LookupByKey(artifact.GetTemplate().GetArtifactID());

                                if (artifactEntry == null ||
                                    artifactEntry.ArtifactCategoryID != spellEffectInfo.MiscValue)
                                    return SpellCastResult.WrongArtifactEquipped;
                            }

                            break;
                        }
                    case SpellEffectName.ChangeBattlepetQuality:
                    case SpellEffectName.GrantBattlepetLevel:
                    case SpellEffectName.GrantBattlepetExperience:
                        {
                            Player playerCaster = _caster.ToPlayer();

                            if (playerCaster == null ||
                                !Targets.GetUnitTarget() ||
                                !Targets.GetUnitTarget().IsCreature())
                                return SpellCastResult.BadTargets;

                            var battlePetMgr = playerCaster.GetSession().GetBattlePetMgr();

                            if (!battlePetMgr.HasJournalLock())
                                return SpellCastResult.CantDoThatRightNow;

                            Creature creature = Targets.GetUnitTarget().ToCreature();

                            if (creature != null)
                            {
                                if (playerCaster.GetSummonedBattlePetGUID().IsEmpty() ||
                                    creature.GetBattlePetCompanionGUID().IsEmpty())
                                    return SpellCastResult.NoPet;

                                if (playerCaster.GetSummonedBattlePetGUID() != creature.GetBattlePetCompanionGUID())
                                    return SpellCastResult.BadTargets;

                                var battlePet = battlePetMgr.GetPet(creature.GetBattlePetCompanionGUID());

                                if (battlePet != null)
                                {
                                    var battlePetSpecies = CliDB.BattlePetSpeciesStorage.LookupByKey(battlePet.PacketInfo.Species);

                                    if (battlePetSpecies != null)
                                    {
                                        uint battlePetType = (uint)spellEffectInfo.MiscValue;

                                        if (battlePetType != 0)
                                            if ((battlePetType & (1 << battlePetSpecies.PetTypeEnum)) == 0)
                                                return SpellCastResult.WrongBattlePetType;

                                        if (spellEffectInfo.Effect == SpellEffectName.ChangeBattlepetQuality)
                                        {
                                            var qualityRecord = CliDB.BattlePetBreedQualityStorage.Values.FirstOrDefault(a1 => a1.MaxQualityRoll < spellEffectInfo.BasePoints);

                                            BattlePetBreedQuality quality = BattlePetBreedQuality.Poor;

                                            if (qualityRecord != null)
                                                quality = (BattlePetBreedQuality)qualityRecord.QualityEnum;

                                            if (battlePet.PacketInfo.Quality >= (byte)quality)
                                                return SpellCastResult.CantUpgradeBattlePet;
                                        }

                                        if (spellEffectInfo.Effect == SpellEffectName.GrantBattlepetLevel ||
                                            spellEffectInfo.Effect == SpellEffectName.GrantBattlepetExperience)
                                            if (battlePet.PacketInfo.Level >= SharedConst.MaxBattlePetLevel)
                                                return SpellCastResult.GrantPetLevelFail;

                                        if (battlePetSpecies.GetFlags().HasFlag(BattlePetSpeciesFlags.CantBattle))
                                            return SpellCastResult.BadTargets;
                                    }
                                }
                            }

                            break;
                        }
                    default:
                        break;
                }

                if (spellEffectInfo.IsAura())
                    approximateAuraEffectMask |= 1u << (int)spellEffectInfo.EffectIndex;
                else if (spellEffectInfo.IsEffect())
                    nonAuraEffectMask |= 1u << (int)spellEffectInfo.EffectIndex;
            }

            foreach (var spellEffectInfo in SpellInfo.GetEffects())
            {
                switch (spellEffectInfo.ApplyAuraName)
                {
                    case AuraType.ModPossessPet:
                        {
                            if (!_caster.IsTypeId(TypeId.Player))
                                return SpellCastResult.NoPet;

                            Pet pet = _caster.ToPlayer().GetPet();

                            if (pet == null)
                                return SpellCastResult.NoPet;

                            if (!pet.GetCharmerGUID().IsEmpty())
                                return SpellCastResult.AlreadyHaveCharm;

                            break;
                        }
                    case AuraType.ModPossess:
                    case AuraType.ModCharm:
                    case AuraType.AoeCharm:
                        {
                            Unit unitCaster1 = (_originalCaster ? _originalCaster : _caster.ToUnit());

                            if (unitCaster1 == null)
                                return SpellCastResult.BadTargets;

                            if (!unitCaster1.GetCharmerGUID().IsEmpty())
                                return SpellCastResult.AlreadyHaveCharm;

                            if (spellEffectInfo.ApplyAuraName == AuraType.ModCharm ||
                                spellEffectInfo.ApplyAuraName == AuraType.ModPossess)
                            {
                                if (!SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst) &&
                                    !unitCaster1.GetPetGUID().IsEmpty())
                                    return SpellCastResult.AlreadyHaveSummon;

                                if (!unitCaster1.GetCharmedGUID().IsEmpty())
                                    return SpellCastResult.AlreadyHaveCharm;
                            }

                            Unit target = Targets.GetUnitTarget();

                            if (target != null)
                            {
                                if (target.IsTypeId(TypeId.Unit) &&
                                    target.ToCreature().IsVehicle())
                                    return SpellCastResult.BadImplicitTargets;

                                if (target.IsMounted())
                                    return SpellCastResult.CantBeCharmed;

                                if (!target.GetCharmerGUID().IsEmpty())
                                    return SpellCastResult.Charmed;

                                if (target.GetOwner() != null &&
                                    target.GetOwner().IsTypeId(TypeId.Player))
                                    return SpellCastResult.TargetIsPlayerControlled;

                                int damage = CalculateDamage(spellEffectInfo, target);

                                if (damage != 0 &&
                                    target.GetLevelForTarget(_caster) > damage)
                                    return SpellCastResult.Highlevel;
                            }

                            break;
                        }
                    case AuraType.Mounted:
                        {
                            if (unitCaster == null)
                                return SpellCastResult.BadTargets;

                            if (unitCaster.IsInWater() &&
                                SpellInfo.HasAura(AuraType.ModIncreaseMountedFlightSpeed))
                                return SpellCastResult.OnlyAbovewater;

                            if (unitCaster.IsInDisallowedMountForm())
                            {
                                SendMountResult(MountResult.Shapeshifted); // Mount result gets sent before the cast result

                                return SpellCastResult.DontReport;
                            }

                            break;
                        }
                    case AuraType.RangedAttackPowerAttackerBonus:
                        {
                            if (Targets.GetUnitTarget() == null)
                                return SpellCastResult.BadImplicitTargets;

                            // can be casted at non-friendly unit or own pet/charm
                            if (_caster.IsFriendlyTo(Targets.GetUnitTarget()))
                                return SpellCastResult.TargetFriendly;

                            break;
                        }
                    case AuraType.Fly:
                    case AuraType.ModIncreaseFlightSpeed:
                        {
                            // not allow cast fly spells if not have req. Skills  (all spells is self Target)
                            // allow always ghost flight spells
                            if (_originalCaster != null &&
                                _originalCaster.IsTypeId(TypeId.Player) &&
                                _originalCaster.IsAlive())
                            {
                                BattleField Bf = Global.BattleFieldMgr.GetBattlefieldToZoneId(_originalCaster.GetMap(), _originalCaster.GetZoneId());
                                var area = CliDB.AreaTableStorage.LookupByKey(_originalCaster.GetAreaId());

                                if (area != null)
                                    if (area.HasFlag(AreaFlags.NoFlyZone) ||
                                        (Bf != null && !Bf.CanFlyIn()))
                                        return SpellCastResult.NotHere;
                            }

                            break;
                        }
                    case AuraType.PeriodicManaLeech:
                        {
                            if (spellEffectInfo.IsTargetingArea())
                                break;

                            if (Targets.GetUnitTarget() == null)
                                return SpellCastResult.BadImplicitTargets;

                            if (!_caster.IsTypeId(TypeId.Player) ||
                                CastItem != null)
                                break;

                            if (Targets.GetUnitTarget().GetPowerType() != PowerType.Mana)
                                return SpellCastResult.BadTargets;

                            break;
                        }
                    default:
                        break;
                }

                // check if Target already has the same Type, but more powerful aura
                if (!SpellInfo.HasAttribute(SpellAttr4.AuraNeverBounces) &&
                    (nonAuraEffectMask == 0 || SpellInfo.HasAttribute(SpellAttr4.AuraBounceFailsSpell)) &&
                    (approximateAuraEffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0 &&
                    !SpellInfo.IsTargetingArea())
                {
                    Unit target = Targets.GetUnitTarget();

                    if (target != null)
                        if (!target.IsHighestExclusiveAuraEffect(SpellInfo, spellEffectInfo.ApplyAuraName, spellEffectInfo.CalcValue(_caster, SpellValue.EffectBasePoints[spellEffectInfo.EffectIndex], null, CastItemEntry, CastItemLevel), approximateAuraEffectMask, false))
                            return SpellCastResult.AuraBounced;
                }
            }

            // check trade Slot case (last, for allow catch any another cast problems)
            if (Convert.ToBoolean(Targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
            {
                if (CastItem != null)
                    return SpellCastResult.ItemEnchantTradeWindow;

                if (SpellInfo.HasAttribute(SpellAttr2.EnchantOwnItemOnly))
                    return SpellCastResult.ItemEnchantTradeWindow;

                if (!_caster.IsTypeId(TypeId.Player))
                    return SpellCastResult.NotTrading;

                TradeData my_trade = _caster.ToPlayer().GetTradeData();

                if (my_trade == null)
                    return SpellCastResult.NotTrading;

                TradeSlots slot = (TradeSlots)Targets.GetItemTargetGUID().GetLowValue();

                if (slot != TradeSlots.NonTraded)
                    return SpellCastResult.BadTargets;

                if (!IsTriggered())
                    if (my_trade.GetSpell() != 0)
                        return SpellCastResult.ItemAlreadyEnchanted;
            }

            // check if caster has at least 1 combo point for spells that require combo points
            if (NeedComboPoints)
            {
                Player plrCaster = _caster.ToPlayer();

                if (plrCaster != null)
                    if (plrCaster.GetComboPoints() == 0)
                        return SpellCastResult.NoComboPoints;
            }

            // all ok
            return SpellCastResult.SpellCastOk;
        }

        public SpellCastResult CheckPetCast(Unit target)
        {
            Unit unitCaster = _caster.ToUnit();

            if (unitCaster != null &&
                unitCaster.HasUnitState(UnitState.Casting) &&
                !_triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreCastInProgress)) //prevent spellcast interruption by another spellcast
                return SpellCastResult.SpellInProgress;

            // dead owner (pets still alive when owners ressed?)
            Unit owner = _caster.GetCharmerOrOwner();

            if (owner != null)
                if (!owner.IsAlive())
                    return SpellCastResult.CasterDead;

            if (target == null &&
                Targets.GetUnitTarget() != null)
                target = Targets.GetUnitTarget();

            if (SpellInfo.NeedsExplicitUnitTarget())
            {
                if (target == null)
                    return SpellCastResult.BadImplicitTargets;

                Targets.SetUnitTarget(target);
            }

            // cooldown
            Creature creatureCaster = _caster.ToCreature();

            if (creatureCaster)
                if (creatureCaster.GetSpellHistory().HasCooldown(SpellInfo.Id))
                    return SpellCastResult.NotReady;

            // Check if spell is affected by GCD
            if (SpellInfo.StartRecoveryCategory > 0)
                if (unitCaster.GetCharmInfo() != null &&
                    unitCaster.GetSpellHistory().HasGlobalCooldown(SpellInfo))
                    return SpellCastResult.NotReady;

            return CheckCast(true);
        }

        public bool CanAutoCast(Unit target)
        {
            if (!target)
                return (CheckPetCast(target) == SpellCastResult.SpellCastOk);

            ObjectGuid targetguid = target.GetGUID();

            // check if Target already has the same or a more powerful aura
            foreach (var spellEffectInfo in SpellInfo.GetEffects())
            {
                if (!spellEffectInfo.IsAura())
                    continue;

                AuraType auraType = spellEffectInfo.ApplyAuraName;
                var auras = target.GetAuraEffectsByType(auraType);

                foreach (var eff in auras)
                {
                    if (GetSpellInfo().Id == eff.GetSpellInfo().Id)
                        return false;

                    switch (Global.SpellMgr.CheckSpellGroupStackRules(GetSpellInfo(), eff.GetSpellInfo()))
                    {
                        case SpellGroupStackRule.Exclusive:
                            return false;
                        case SpellGroupStackRule.ExclusiveFromSameCaster:
                            if (GetCaster() == eff.GetCaster())
                                return false;

                            break;
                        case SpellGroupStackRule.ExclusiveSameEffect: // this one has further checks, but i don't think they're necessary for autocast logic
                        case SpellGroupStackRule.ExclusiveHighest:
                            if (Math.Abs(spellEffectInfo.BasePoints) <= Math.Abs(eff.GetAmount()))
                                return false;

                            break;
                        case SpellGroupStackRule.Default:
                        default:
                            break;
                    }
                }
            }

            SpellCastResult result = CheckPetCast(target);

            if (result == SpellCastResult.SpellCastOk ||
                result == SpellCastResult.UnitNotInfront)
            {
                // do not check targets for ground-targeted spells (we Target them on top of the intended Target anyway)
                if (GetSpellInfo().ExplicitTargetMask.HasAnyFlag((uint)SpellCastTargetFlags.DestLocation))
                    return true;

                SelectSpellTargets();

                //check if among Target units, our WANTED Target is as well (.only self cast spells return false)
                foreach (var ihit in UniqueTargetInfo)
                    if (ihit.TargetGUID == targetguid)
                        return true;
            }

            // either the cast failed or the intended Target wouldn't be hit
            return false;
        }

        public void Delayed() // only called in DealDamage()
        {
            Unit unitCaster = _caster.ToUnit();

            if (unitCaster == null)
                return;

            if (IsDelayableNoMore()) // Spells may only be delayed twice
                return;

            //check pushback reduce
            int delaytime = 500; // spellcasting delay is normally 500ms
            int delayReduce = 100; // must be initialized to 100 for percent modifiers

            Player player = unitCaster.GetSpellModOwner();

            player?.ApplySpellMod(SpellInfo, SpellModOp.ResistPushback, ref delayReduce, this);

            delayReduce += unitCaster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;

            if (delayReduce >= 100)
                return;

            delaytime = MathFunctions.AddPct(delaytime, -delayReduce);

            if (_timer + delaytime > _casttime)
            {
                delaytime = _casttime - _timer;
                _timer = _casttime;
            }
            else
            {
                _timer += delaytime;
            }

            SpellDelayed spellDelayed = new();
            spellDelayed.Caster = unitCaster.GetGUID();
            spellDelayed.ActualDelay = delaytime;

            unitCaster.SendMessageToSet(spellDelayed, true);
        }

        public void DelayedChannel()
        {
            Unit unitCaster = _caster.ToUnit();

            if (unitCaster == null)
                return;

            if (_spellState != SpellState.Casting)
                return;

            if (IsDelayableNoMore()) // Spells may only be delayed twice
                return;

            //check pushback reduce
            // should be affected by modifiers, not take the dbc duration.
            int duration = ((_channeledDuration > 0) ? _channeledDuration : SpellInfo.GetDuration());

            int delaytime = MathFunctions.CalculatePct(duration, 25); // channeling delay is normally 25% of its Time per hit
            int delayReduce = 100;                                      // must be initialized to 100 for percent modifiers

            Player player = unitCaster.GetSpellModOwner();

            player?.ApplySpellMod(SpellInfo, SpellModOp.ResistPushback, ref delayReduce, this);

            delayReduce += unitCaster.GetTotalAuraModifier(AuraType.ReducePushback) - 100;

            if (delayReduce >= 100)
                return;

            delaytime = MathFunctions.AddPct(delaytime, -delayReduce);

            if (_timer <= delaytime)
            {
                delaytime = _timer;
                _timer = 0;
            }
            else
            {
                _timer -= delaytime;
            }

            foreach (var ihit in UniqueTargetInfo)
                if (ihit.MissCondition == SpellMissInfo.None)
                {
                    Unit unit = (unitCaster.GetGUID() == ihit.TargetGUID) ? unitCaster : Global.ObjAccessor.GetUnit(unitCaster, ihit.TargetGUID);

                    unit?.DelayOwnedAuras(SpellInfo.Id, _originalCasterGUID, delaytime);
                }

            // partially interrupt persistent area Auras
            DynamicObject dynObj = unitCaster.GetDynObject(SpellInfo.Id);

            dynObj?.Delay(delaytime);

            SendChannelUpdate((uint)_timer);
        }

        public bool HasPowerTypeCost(PowerType power)
        {
            return GetPowerTypeCostAmount(power).HasValue;
        }

        public int? GetPowerTypeCostAmount(PowerType power)
        {
            var powerCost = _powerCost.Find(cost => cost.Power == power);

            if (powerCost == null)
                return null;

            return powerCost.Amount;
        }

        public CurrentSpellTypes GetCurrentContainer()
        {
            if (SpellInfo.IsNextMeleeSwingSpell())
                return CurrentSpellTypes.Melee;
            else if (IsAutoRepeat())
                return CurrentSpellTypes.AutoRepeat;
            else if (SpellInfo.IsChanneled())
                return CurrentSpellTypes.Channeled;

            return CurrentSpellTypes.Generic;
        }

        public Difficulty GetCastDifficulty()
        {
            return _caster.GetMap().GetDifficultyID();
        }

        public bool IsPositive()
        {
            return SpellInfo.IsPositive() && (TriggeredByAuraSpell == null || TriggeredByAuraSpell.IsPositive());
        }

        public Unit GetUnitCasterForEffectHandlers()
        {
            return _originalCaster != null ? _originalCaster : _caster.ToUnit();
        }

        public void SetSpellValue(SpellValueMod mod, int value)
        {
            if (mod < SpellValueMod.End)
            {
                SpellValue.EffectBasePoints[(int)mod] = value;
                SpellValue.CustomBasePointsMask |= 1u << (int)mod;

                return;
            }

            switch (mod)
            {
                case SpellValueMod.RadiusMod:
                    SpellValue.RadiusMod = (float)value / 10000;

                    break;
                case SpellValueMod.MaxTargets:
                    SpellValue.MaxAffectedTargets = (uint)value;

                    break;
                case SpellValueMod.AuraStack:
                    SpellValue.AuraStackAmount = value;

                    break;
                case SpellValueMod.CritChance:
                    SpellValue.CriticalChance = value / 100.0f; // @todo ugly /100 remove when basepoints are double

                    break;
                case SpellValueMod.DurationPct:
                    SpellValue.DurationMul = (float)value / 100.0f;

                    break;
                case SpellValueMod.Duration:
                    SpellValue.Duration = value;

                    break;
            }
        }

        public bool CheckTargetHookEffect(ITargetHookHandler th, uint effIndexToCheck)
        {
            if (th.TargetType == 0)
                return false;

            if (SpellInfo.GetEffects().Count <= effIndexToCheck)
                return false;

            SpellEffectInfo spellEffectInfo = SpellInfo.GetEffect(effIndexToCheck);

            if (spellEffectInfo.TargetA.GetTarget() != th.TargetType &&
                spellEffectInfo.TargetB.GetTarget() != th.TargetType)
                return false;

            SpellImplicitTargetInfo targetInfo = new(th.TargetType);

            switch (targetInfo.GetSelectionCategory())
            {
                case SpellTargetSelectionCategories.Channel: // SINGLE
                    return !th.Area;
                case SpellTargetSelectionCategories.Nearby: // BOTH
                    return true;
                case SpellTargetSelectionCategories.Cone: // AREA
                case SpellTargetSelectionCategories.Line: // AREA
                    return th.Area;
                case SpellTargetSelectionCategories.Area: // AREA
                    if (targetInfo.GetObjectType() == SpellTargetObjectTypes.UnitAndDest)
                        return th.Area || th.Dest;

                    return th.Area;
                case SpellTargetSelectionCategories.Default:
                    switch (targetInfo.GetObjectType())
                    {
                        case SpellTargetObjectTypes.Src: // EMPTY
                            return false;
                        case SpellTargetObjectTypes.Dest: // Dest
                            return th.Dest;
                        default:
                            switch (targetInfo.GetReferenceType())
                            {
                                case SpellTargetReferenceTypes.Caster: // SINGLE
                                    return !th.Area;
                                case SpellTargetReferenceTypes.Target: // BOTH
                                    return true;
                                default:
                                    break;
                            }

                            break;
                    }

                    break;
                default:
                    break;
            }

            return false;
        }

        public void CallScriptBeforeHitHandlers(SpellMissInfo missInfo)
        {
            foreach (ISpellScript script in GetSpellScripts<IBeforeHit>())
            {
                script._InitHit();
                script._PrepareScriptCall(SpellScriptHookType.BeforeHit);
                ((IBeforeHit)script).BeforeHit(missInfo);
                script._FinishScriptCall();
            }
        }

        public void CallScriptOnHitHandlers()
        {
            foreach (ISpellScript script in GetSpellScripts<IOnHit>())
            {
                script._PrepareScriptCall(SpellScriptHookType.Hit);
                ((IOnHit)script).OnHit();
                script._FinishScriptCall();
            }
        }

        public void CallScriptAfterHitHandlers()
        {
            foreach (ISpellScript script in GetSpellScripts<IAfterHit>())
            {
                script._PrepareScriptCall(SpellScriptHookType.AfterHit);
                ((IAfterHit)script).AfterHit();
                script._FinishScriptCall();
            }
        }

        public void CallScriptCalcCritChanceHandlers(Unit victim, ref float critChance)
        {
            foreach (ISpellScript loadedScript in GetSpellScripts<ICalcCritChance>())
            {
                loadedScript._PrepareScriptCall(SpellScriptHookType.CalcCritChance);

                ((ICalcCritChance)loadedScript).CalcCritChance(victim, ref critChance);

                loadedScript._FinishScriptCall();
            }
        }

        public void CallScriptOnResistAbsorbCalculateHandlers(DamageInfo damageInfo, ref uint resistAmount, ref int absorbAmount)
        {
            foreach (ISpellScript script in GetSpellScripts<ICheckCastHander>())
            {
                script._PrepareScriptCall(SpellScriptHookType.OnResistAbsorbCalculation);

                ((ICalculateResistAbsorb)script).CalculateResistAbsorb(damageInfo, ref resistAmount, ref absorbAmount);

                script._FinishScriptCall();
            }
        }

        public bool CanExecuteTriggersOnHit(Unit unit, SpellInfo triggeredByAura = null)
        {
            bool onlyOnTarget = triggeredByAura != null && triggeredByAura.HasAttribute(SpellAttr4.ClassTriggerOnlyOnTarget);

            if (!onlyOnTarget)
                return true;

            // If triggeredByAura has SPELL_ATTR4_CLASS_TRIGGER_ONLY_ON_TARGET then it can only proc on either noncaster units...
            if (unit != _caster)
                return true;

            // ... or caster if it is the only Target
            if (UniqueTargetInfo.Count == 1)
                return true;

            return false;
        }

        public List<ISpellScript> GetSpellScripts<T>() where T : ISpellScript
        {
            if (_spellScriptsByType.TryGetValue(typeof(T), out List<ISpellScript> scripts))
                return scripts;

            return _dummy;
        }

        public List<(ISpellScript, ISpellEffect)> GetEffectScripts(SpellScriptHookType h, uint index)
        {
            if (_effectHandlers.TryGetValue(index, out var effDict) &&
                effDict.TryGetValue(h, out List<(ISpellScript, ISpellEffect)> scripts))
                return scripts;

            return _dummySpellEffects;
        }


        public SpellCastResult CheckMovement()
        {
            if (IsTriggered())
                return SpellCastResult.SpellCastOk;

            Unit unitCaster = _caster.ToUnit();

            if (unitCaster != null)
                if (!unitCaster.CanCastSpellWhileMoving(SpellInfo))
                {
                    if (GetState() == SpellState.Preparing)
                    {
                        if (_casttime > 0 &&
                            SpellInfo.InterruptFlags.HasFlag(SpellInterruptFlags.Movement))
                            return SpellCastResult.Moving;
                    }
                    else if (GetState() == SpellState.Casting &&
                             !SpellInfo.IsMoveAllowedChannel())
                    {
                        return SpellCastResult.Moving;
                    }
                }

            return SpellCastResult.SpellCastOk;
        }

        public SpellState GetState()
        {
            return _spellState;
        }

        public void SetState(SpellState state)
        {
            _spellState = state;
        }

        public int GetCastTime()
        {
            return _casttime;
        }

        public bool IsTriggered()
        {
            return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.FullMask);
        }

        public bool IsTriggeredByAura(SpellInfo auraSpellInfo)
        {
            return (auraSpellInfo == TriggeredByAuraSpell);
        }

        public bool IsIgnoringCooldowns()
        {
            return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreSpellAndCategoryCD);
        }

        public bool IsFocusDisabled()
        {
            return _triggeredCastFlags.HasFlag(TriggerCastFlags.IgnoreSetFacing) || (SpellInfo.IsChanneled() && !SpellInfo.HasAttribute(SpellAttr1.TrackTargetInChannel));
        }

        public bool IsProcDisabled()
        {
            return _triggeredCastFlags.HasAnyFlag(TriggerCastFlags.DisallowProcEvents);
        }

        public bool IsChannelActive()
        {
            return _caster.IsUnit() && _caster.ToUnit().GetChannelSpellId() != 0;
        }

        public bool IsDeletable()
        {
            return !_referencedFromCurrentSpell && !_executedCurrently;
        }

        public void SetReferencedFromCurrent(bool yes)
        {
            _referencedFromCurrentSpell = yes;
        }

        public bool IsInterruptable()
        {
            return !_executedCurrently;
        }

        public ulong GetDelayStart()
        {
            return _delayStart;
        }

        public void SetDelayStart(ulong _time)
        {
            _delayStart = _time;
        }

        public ulong GetDelayMoment()
        {
            return _delayMoment;
        }

        public WorldObject GetCaster()
        {
            return _caster;
        }

        public ObjectGuid GetOriginalCasterGUID()
        {
            return _originalCasterGUID;
        }

        public Unit GetOriginalCaster()
        {
            return _originalCaster;
        }

        public SpellInfo GetSpellInfo()
        {
            return SpellInfo;
        }

        public List<SpellPowerCost> GetPowerCost()
        {
            return _powerCost;
        }

        public SpellInfo GetTriggeredByAuraSpell()
        {
            return TriggeredByAuraSpell;
        }

        public static implicit operator bool(Spell spell)
        {
            return spell != null;
        }

        private void InitExplicitTargets(SpellCastTargets targets)
        {
            Targets = targets;

            // this function tries to correct spell explicit targets for spell
            // client doesn't send explicit targets correctly sometimes - we need to fix such spells serverside
            // this also makes sure that we correctly send explicit targets to client (removes redundant _data)
            SpellCastTargetFlags neededTargets = SpellInfo.GetExplicitTargetMask();

            WorldObject target = Targets.GetObjectTarget();

            if (target != null)
            {
                // check if object Target is valid with needed Target Flags
                // for unit case allow corpse Target mask because player with not released corpse is a unit Target
                if ((target.ToUnit() && !neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask)) ||
                    (target.IsTypeId(TypeId.GameObject) && !neededTargets.HasFlag(SpellCastTargetFlags.GameobjectMask)) ||
                    (target.IsTypeId(TypeId.Corpse) && !neededTargets.HasFlag(SpellCastTargetFlags.CorpseMask)))
                    Targets.RemoveObjectTarget();
            }
            else
            {
                // try to select correct unit Target if not provided by client or by serverside cast
                if (neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitMask))
                {
                    Unit unit = null;
                    // try to use player selection as a Target
                    Player playerCaster = _caster.ToPlayer();

                    if (playerCaster != null)
                    {
                        // selection has to be found and to be valid Target for the spell
                        Unit selectedUnit = Global.ObjAccessor.GetUnit(_caster, playerCaster.GetTarget());

                        if (selectedUnit != null)
                            if (SpellInfo.CheckExplicitTarget(_caster, selectedUnit) == SpellCastResult.SpellCastOk)
                                unit = selectedUnit;
                    }
                    // try to use attacked unit as a Target
                    else if (_caster.IsTypeId(TypeId.Unit) &&
                             neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitEnemy | SpellCastTargetFlags.Unit))
                    {
                        unit = _caster.ToUnit().GetVictim();
                    }

                    // didn't find anything - let's use self as Target
                    if (unit == null &&
                        neededTargets.HasAnyFlag(SpellCastTargetFlags.UnitRaid | SpellCastTargetFlags.UnitParty | SpellCastTargetFlags.UnitAlly))
                        unit = _caster.ToUnit();

                    Targets.SetUnitTarget(unit);
                }
            }

            // check if spell needs dst Target
            if (neededTargets.HasFlag(SpellCastTargetFlags.DestLocation))
            {
                // and Target isn't set
                if (!Targets.HasDst())
                {
                    // try to use unit Target if provided
                    WorldObject targett = targets.GetObjectTarget();

                    if (targett != null)
                        Targets.SetDst(targett);
                    // or use self if not available
                    else
                        Targets.SetDst(_caster);
                }
            }
            else
            {
                Targets.RemoveDst();
            }

            if (neededTargets.HasFlag(SpellCastTargetFlags.SourceLocation))
            {
                if (!targets.HasSrc())
                    Targets.SetSrc(_caster);
            }
            else
            {
                Targets.RemoveSrc();
            }
        }

        private void SelectExplicitTargets()
        {
            // here go all explicit Target changes made to explicit targets after spell prepare phase is finished
            Unit target = Targets.GetUnitTarget();

            if (target != null)
                // check for explicit Target redirection, for Grounding Totem for example
                if (SpellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.UnitEnemy) ||
                    (SpellInfo.GetExplicitTargetMask().HasAnyFlag(SpellCastTargetFlags.Unit) && !_caster.IsFriendlyTo(target)))
                {
                    Unit redirect = null;

                    switch (SpellInfo.DmgClass)
                    {
                        case SpellDmgClass.Magic:
                            redirect = _caster.GetMagicHitRedirectTarget(target, SpellInfo);

                            break;
                        case SpellDmgClass.Melee:
                        case SpellDmgClass.Ranged:
                            // should gameobjects cast damagetype melee/ranged spells this needs to be changed
                            redirect = _caster.ToUnit().GetMeleeHitRedirectTarget(target, SpellInfo);

                            break;
                        default:
                            break;
                    }

                    if (redirect != null &&
                        (redirect != target))
                        Targets.SetUnitTarget(redirect);
                }
        }

        private ulong CalculateDelayMomentForDst(float launchDelay)
        {
            if (Targets.HasDst())
            {
                if (Targets.HasTraj())
                {
                    float speed = Targets.GetSpeedXY();

                    if (speed > 0.0f)
                        return (ulong)(Math.Floor((Targets.GetDist2d() / speed + launchDelay) * 1000.0f));
                }
                else if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                {
                    return (ulong)(Math.Floor((SpellInfo.Speed + launchDelay) * 1000.0f));
                }
                else if (SpellInfo.Speed > 0.0f)
                {
                    // We should not subtract caster size from dist calculation (fixes execution Time desync with animation on client, eg. Malleable Goo cast by PP)
                    float dist = _caster.GetExactDist(Targets.GetDstPos());

                    return (ulong)(Math.Floor((dist / SpellInfo.Speed + launchDelay) * 1000.0f));
                }

                return (ulong)Math.Floor(launchDelay * 1000.0f);
            }

            return 0;
        }

        private void SelectEffectImplicitTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, ref uint processedEffectMask)
        {
            if (targetType.GetTarget() == 0)
                return;

            uint effectMask = (1u << (int)spellEffectInfo.EffectIndex);

            // set the same Target list for all effects
            // some spells appear to need this, however this requires more research
            switch (targetType.GetSelectionCategory())
            {
                case SpellTargetSelectionCategories.Nearby:
                case SpellTargetSelectionCategories.Cone:
                case SpellTargetSelectionCategories.Area:
                case SpellTargetSelectionCategories.Line:
                    {
                        // targets for effect already selected
                        if (Convert.ToBoolean(effectMask & processedEffectMask))
                            return;

                        var effects = GetSpellInfo().GetEffects();

                        // choose which targets we can select at once
                        for (int j = (int)spellEffectInfo.EffectIndex + 1; j < effects.Count; ++j)
                            if (effects[j].IsEffect() &&
                                spellEffectInfo.TargetA.GetTarget() == effects[j].TargetA.GetTarget() &&
                                spellEffectInfo.TargetB.GetTarget() == effects[j].TargetB.GetTarget() &&
                                spellEffectInfo.ImplicitTargetConditions == effects[j].ImplicitTargetConditions &&
                                spellEffectInfo.CalcRadius(_caster) == effects[j].CalcRadius(_caster) &&
                                CheckScriptEffectImplicitTargets(spellEffectInfo.EffectIndex, (uint)j))
                                effectMask |= 1u << j;

                        processedEffectMask |= effectMask;

                        break;
                    }
                default:
                    break;
            }

            switch (targetType.GetSelectionCategory())
            {
                case SpellTargetSelectionCategories.Channel:
                    SelectImplicitChannelTargets(spellEffectInfo, targetType);

                    break;
                case SpellTargetSelectionCategories.Nearby:
                    SelectImplicitNearbyTargets(spellEffectInfo, targetType, effectMask);

                    break;
                case SpellTargetSelectionCategories.Cone:
                    SelectImplicitConeTargets(spellEffectInfo, targetType, effectMask);

                    break;
                case SpellTargetSelectionCategories.Area:
                    SelectImplicitAreaTargets(spellEffectInfo, targetType, effectMask);

                    break;
                case SpellTargetSelectionCategories.Traj:
                    // just in case there is no dest, explanation in SelectImplicitDestDestTargets
                    CheckDst();

                    SelectImplicitTrajTargets(spellEffectInfo, targetType);

                    break;
                case SpellTargetSelectionCategories.Line:
                    SelectImplicitLineTargets(spellEffectInfo, targetType, effectMask);

                    break;
                case SpellTargetSelectionCategories.Default:
                    switch (targetType.GetObjectType())
                    {
                        case SpellTargetObjectTypes.Src:
                            switch (targetType.GetReferenceType())
                            {
                                case SpellTargetReferenceTypes.Caster:
                                    Targets.SetSrc(_caster);

                                    break;
                                default:
                                    Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select Target reference Type for TARGET_TYPE_OBJECT_SRC");

                                    break;
                            }

                            break;
                        case SpellTargetObjectTypes.Dest:
                            switch (targetType.GetReferenceType())
                            {
                                case SpellTargetReferenceTypes.Caster:
                                    SelectImplicitCasterDestTargets(spellEffectInfo, targetType);

                                    break;
                                case SpellTargetReferenceTypes.Target:
                                    SelectImplicitTargetDestTargets(spellEffectInfo, targetType);

                                    break;
                                case SpellTargetReferenceTypes.Dest:
                                    SelectImplicitDestDestTargets(spellEffectInfo, targetType);

                                    break;
                                default:
                                    Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select Target reference Type for TARGET_TYPE_OBJECT_DEST");

                                    break;
                            }

                            break;
                        default:
                            switch (targetType.GetReferenceType())
                            {
                                case SpellTargetReferenceTypes.Caster:
                                    SelectImplicitCasterObjectTargets(spellEffectInfo, targetType);

                                    break;
                                case SpellTargetReferenceTypes.Target:
                                    SelectImplicitTargetObjectTargets(spellEffectInfo, targetType);

                                    break;
                                default:
                                    Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select Target reference Type for TARGET_TYPE_OBJECT");

                                    break;
                            }

                            break;
                    }

                    break;
                case SpellTargetSelectionCategories.Nyi:
                    Log.outDebug(LogFilter.Spells, "SPELL: Target Type {0}, found in spellID {1}, effect {2} is not implemented yet!", SpellInfo.Id, spellEffectInfo.EffectIndex, targetType.GetTarget());

                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectEffectImplicitTargets: received not implemented select Target category");

                    break;
            }
        }

        private void SelectImplicitChannelTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            if (targetType.GetReferenceType() != SpellTargetReferenceTypes.Caster)
            {
                Cypher.Assert(false, "Spell.SelectImplicitChannelTargets: received not implemented Target reference Type");

                return;
            }

            Spell channeledSpell = _originalCaster.GetCurrentSpell(CurrentSpellTypes.Channeled);

            if (channeledSpell == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitChannelTargets: cannot find channel spell for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);

                return;
            }

            switch (targetType.GetTarget())
            {
                case Framework.Constants.Targets.UnitChannelTarget:
                    {
                        foreach (ObjectGuid channelTarget in _originalCaster.UnitData.ChannelObjects)
                        {
                            WorldObject target = Global.ObjAccessor.GetUnit(_caster, channelTarget);
                            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);
                            // unit Target may be no longer avalible - teleported out of map for example
                            Unit unitTarget = target ? target.ToUnit() : null;

                            if (unitTarget)
                                AddUnitTarget(unitTarget, 1u << (int)spellEffectInfo.EffectIndex);
                            else
                                Log.outDebug(LogFilter.Spells, "SPELL: cannot find channel spell Target for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);
                        }

                        break;
                    }
                case Framework.Constants.Targets.DestChannelTarget:
                    {
                        if (channeledSpell.Targets.HasDst())
                        {
                            Targets.SetDst(channeledSpell.Targets);
                        }
                        else
                        {
                            List<ObjectGuid> channelObjects = _originalCaster.UnitData.ChannelObjects;
                            WorldObject target = !channelObjects.Empty() ? Global.ObjAccessor.GetWorldObject(_caster, channelObjects[0]) : null;

                            if (target != null)
                            {
                                CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

                                if (target)
                                {
                                    SpellDestination dest = new(target);

                                    if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                                        dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                                    CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                                    Targets.SetDst(dest);
                                }
                            }
                            else
                            {
                                Log.outDebug(LogFilter.Spells, "SPELL: cannot find channel spell destination for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);
                            }
                        }

                        break;
                    }
                case Framework.Constants.Targets.DestChannelCaster:
                    {
                        SpellDestination dest = new(channeledSpell.GetCaster());

                        if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                            dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                        CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                        Targets.SetDst(dest);

                        break;
                    }
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitChannelTargets: received not implemented Target Type");

                    break;
            }
        }

        private void SelectImplicitNearbyTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
        {
            if (targetType.GetReferenceType() != SpellTargetReferenceTypes.Caster)
            {
                Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented Target reference Type");

                return;
            }

            float range = 0.0f;

            switch (targetType.GetCheckType())
            {
                case SpellTargetCheckTypes.Enemy:
                    range = SpellInfo.GetMaxRange(false, _caster, this);

                    break;
                case SpellTargetCheckTypes.Ally:
                case SpellTargetCheckTypes.Party:
                case SpellTargetCheckTypes.Raid:
                case SpellTargetCheckTypes.RaidClass:
                    range = SpellInfo.GetMaxRange(true, _caster, this);

                    break;
                case SpellTargetCheckTypes.Entry:
                case SpellTargetCheckTypes.Default:
                    range = SpellInfo.GetMaxRange(IsPositive(), _caster, this);

                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented selection check Type");

                    break;
            }

            List<Condition> condList = spellEffectInfo.ImplicitTargetConditions;

            // handle emergency case - try to use other provided targets if no conditions provided
            if (targetType.GetCheckType() == SpellTargetCheckTypes.Entry &&
                (condList == null || condList.Empty()))
            {
                Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitNearbyTargets: no conditions entry for Target with TARGET_CHECK_ENTRY of spell ID {0}, effect {1} - selecting default targets", SpellInfo.Id, spellEffectInfo.EffectIndex);

                switch (targetType.GetObjectType())
                {
                    case SpellTargetObjectTypes.Gobj:
                        if (SpellInfo.RequiresSpellFocus != 0)
                        {
                            if (_focusObject != null)
                            {
                                AddGOTarget(_focusObject, effMask);
                            }
                            else
                            {
                                SendCastResult(SpellCastResult.BadImplicitTargets);
                                Finish(false);
                            }

                            return;
                        }

                        break;
                    case SpellTargetObjectTypes.Dest:
                        if (SpellInfo.RequiresSpellFocus != 0)
                        {
                            if (_focusObject != null)
                            {
                                SpellDestination dest = new(_focusObject);

                                if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                                    dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                                Targets.SetDst(dest);
                            }
                            else
                            {
                                SendCastResult(SpellCastResult.BadImplicitTargets);
                                Finish(false);
                            }

                            return;
                        }

                        break;
                    default:
                        break;
                }
            }

            WorldObject target = SearchNearbyTarget(range, targetType.GetObjectType(), targetType.GetCheckType(), condList);

            if (target == null)
            {
                Log.outDebug(LogFilter.Spells, "Spell.SelectImplicitNearbyTargets: cannot find nearby Target for spell ID {0}, effect {1}", SpellInfo.Id, spellEffectInfo.EffectIndex);
                SendCastResult(SpellCastResult.BadImplicitTargets);
                Finish(false);

                return;
            }

            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

            if (!target)
            {
                Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set NULL Target, effect {spellEffectInfo.EffectIndex}");
                SendCastResult(SpellCastResult.BadImplicitTargets);
                Finish(false);

                return;
            }

            switch (targetType.GetObjectType())
            {
                case SpellTargetObjectTypes.Unit:
                    Unit unitTarget = target.ToUnit();

                    if (unitTarget != null)
                    {
                        AddUnitTarget(unitTarget, effMask, true, false);
                    }
                    else
                    {
                        Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set object of wrong Type, expected unit, got {target.GetGUID().GetHigh()}, effect {effMask}");
                        SendCastResult(SpellCastResult.BadImplicitTargets);
                        Finish(false);

                        return;
                    }

                    break;
                case SpellTargetObjectTypes.Gobj:
                    GameObject gobjTarget = target.ToGameObject();

                    if (gobjTarget != null)
                    {
                        AddGOTarget(gobjTarget, effMask);
                    }
                    else
                    {
                        Log.outDebug(LogFilter.Spells, $"Spell.SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set object of wrong Type, expected gameobject, got {target.GetGUID().GetHigh()}, effect {effMask}");
                        SendCastResult(SpellCastResult.BadImplicitTargets);
                        Finish(false);

                        return;
                    }

                    break;
                case SpellTargetObjectTypes.Corpse:
                    Corpse corpseTarget = target.ToCorpse();

                    if (corpseTarget != null)
                    {
                        AddCorpseTarget(corpseTarget, effMask);
                    }
                    else
                    {
                        Log.outDebug(LogFilter.Spells, $"Spell::SelectImplicitNearbyTargets: OnObjectTargetSelect script hook for spell Id {SpellInfo.Id} set object of wrong Type, expected corpse, got {target.GetGUID().GetTypeId()}, effect {effMask}");
                        SendCastResult(SpellCastResult.BadImplicitTargets);
                        Finish(false);

                        return;
                    }

                    break;
                case SpellTargetObjectTypes.Dest:
                    SpellDestination dest = new(target);

                    if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                        dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                    CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                    Targets.SetDst(dest);

                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitNearbyTargets: received not implemented Target object Type");

                    break;
            }

            SelectImplicitChainTargets(spellEffectInfo, targetType, target, effMask);
        }

        private void SelectImplicitConeTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
        {
            Position coneSrc = new(_caster);
            float coneAngle = SpellInfo.ConeAngle;

            switch (targetType.GetReferenceType())
            {
                case SpellTargetReferenceTypes.Caster:
                    break;
                case SpellTargetReferenceTypes.Dest:
                    if (_caster.GetExactDist2d(Targets.GetDstPos()) > 0.1f)
                        coneSrc.SetOrientation(_caster.GetAbsoluteAngle(Targets.GetDstPos()));

                    break;
                default:
                    break;
            }

            switch (targetType.GetTarget())
            {
                case Framework.Constants.Targets.UnitCone180DegEnemy:
                    if (coneAngle == 0.0f)
                        coneAngle = 180.0f;

                    break;
                default:
                    break;
            }

            List<WorldObject> targets = new();
            SpellTargetObjectTypes objectType = targetType.GetObjectType();
            SpellTargetCheckTypes selectionType = targetType.GetCheckType();

            var condList = spellEffectInfo.ImplicitTargetConditions;
            float radius = spellEffectInfo.CalcRadius(_caster) * SpellValue.RadiusMod;

            GridMapTypeMask containerTypeMask = GetSearcherTypeMask(objectType, condList);

            if (containerTypeMask != 0)
            {
                float extraSearchRadius = radius > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
                var spellCone = new WorldObjectSpellConeTargetCheck(coneSrc, MathFunctions.DegToRad(coneAngle), SpellInfo.Width != 0 ? SpellInfo.Width : _caster.GetCombatReach(), radius, _caster, SpellInfo, selectionType, condList, objectType);
                var searcher = new WorldObjectListSearcher(_caster, targets, spellCone, containerTypeMask);
                SearchTargets(searcher, containerTypeMask, _caster, _caster.GetPosition(), radius + extraSearchRadius);

                CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

                if (!targets.Empty())
                {
                    // Other special Target selection goes here
                    uint maxTargets = SpellValue.MaxAffectedTargets;

                    if (maxTargets != 0)
                        targets.RandomResize(maxTargets);

                    foreach (var obj in targets)
                        if (obj.IsUnit())
                            AddUnitTarget(obj.ToUnit(), effMask, false);
                        else if (obj.IsGameObject())
                            AddGOTarget(obj.ToGameObject(), effMask);
                        else if (obj.IsCorpse())
                            AddCorpseTarget(obj.ToCorpse(), effMask);
                }
            }
        }

        private void SelectImplicitAreaTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
        {
            WorldObject referer;

            switch (targetType.GetReferenceType())
            {
                case SpellTargetReferenceTypes.Src:
                case SpellTargetReferenceTypes.Dest:
                case SpellTargetReferenceTypes.Caster:
                    referer = _caster;

                    break;
                case SpellTargetReferenceTypes.Target:
                    referer = Targets.GetUnitTarget();

                    break;
                case SpellTargetReferenceTypes.Last:
                    {
                        referer = _caster;

                        // find last added Target for this effect
                        foreach (var target in UniqueTargetInfo)
                            if (Convert.ToBoolean(target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)))
                            {
                                referer = Global.ObjAccessor.GetUnit(_caster, target.TargetGUID);

                                break;
                            }

                        break;
                    }
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitAreaTargets: received not implemented Target reference Type");

                    return;
            }

            if (referer == null)
                return;

            Position center;

            switch (targetType.GetReferenceType())
            {
                case SpellTargetReferenceTypes.Src:
                    center = Targets.GetSrcPos();

                    break;
                case SpellTargetReferenceTypes.Dest:
                    center = Targets.GetDstPos();

                    break;
                case SpellTargetReferenceTypes.Caster:
                case SpellTargetReferenceTypes.Target:
                case SpellTargetReferenceTypes.Last:
                    center = referer.GetPosition();

                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitAreaTargets: received not implemented Target reference Type");

                    return;
            }

            float radius = spellEffectInfo.CalcRadius(_caster) * SpellValue.RadiusMod;
            List<WorldObject> targets = new();

            switch (targetType.GetTarget())
            {
                case Framework.Constants.Targets.UnitCasterAndPassengers:
                    targets.Add(_caster);
                    Unit unit = _caster.ToUnit();

                    if (unit != null)
                    {
                        Vehicle vehicleKit = unit.GetVehicleKit();

                        if (vehicleKit != null)
                            for (sbyte seat = 0; seat < SharedConst.MaxVehicleSeats; ++seat)
                            {
                                Unit passenger = vehicleKit.GetPassenger(seat);

                                if (passenger != null)
                                    targets.Add(passenger);
                            }
                    }

                    break;
                case Framework.Constants.Targets.UnitTargetAllyOrRaid:
                    Unit targetedUnit = Targets.GetUnitTarget();

                    if (targetedUnit != null)
                    {
                        if (!_caster.IsUnit() ||
                            !_caster.ToUnit().IsInRaidWith(targetedUnit))
                            targets.Add(Targets.GetUnitTarget());
                        else
                            SearchAreaTargets(targets, radius, targetedUnit, referer, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions);
                    }

                    break;
                case Framework.Constants.Targets.UnitCasterAndSummons:
                    targets.Add(_caster);
                    SearchAreaTargets(targets, radius, center, referer, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions);

                    break;
                default:
                    SearchAreaTargets(targets, radius, center, referer, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions);

                    break;
            }

            if (targetType.GetObjectType() == SpellTargetObjectTypes.UnitAndDest)
            {
                SpellDestination dest = new(referer);

                if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                    dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);

                Targets.ModDst(dest);
            }

            CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

            if (targetType.GetTarget() == Framework.Constants.Targets.UnitSrcAreaFurthestEnemy)
                targets.Sort(new ObjectDistanceOrderPred(referer, false));

            if (!targets.Empty())
            {
                // Other special Target selection goes here
                uint maxTargets = SpellValue.MaxAffectedTargets;

                if (maxTargets != 0)
                {
                    if (targetType.GetTarget() != Framework.Constants.Targets.UnitSrcAreaFurthestEnemy)
                        targets.RandomResize(maxTargets);
                    else if (targets.Count > maxTargets)
                        targets.Resize(maxTargets);
                }

                foreach (var obj in targets)
                    if (obj.IsUnit())
                        AddUnitTarget(obj.ToUnit(), effMask, false, true, center);
                    else if (obj.IsGameObject())
                        AddGOTarget(obj.ToGameObject(), effMask);
                    else if (obj.IsCorpse())
                        AddCorpseTarget(obj.ToCorpse(), effMask);
            }
        }

        private void SelectImplicitCasterDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            SpellDestination dest = new(_caster);

            switch (targetType.GetTarget())
            {
                case Framework.Constants.Targets.DestCaster:
                    break;
                case Framework.Constants.Targets.DestHome:
                    Player playerCaster = _caster.ToPlayer();

                    if (playerCaster != null)
                        dest = new SpellDestination(playerCaster.GetHomebind());

                    break;
                case Framework.Constants.Targets.DestDb:
                    SpellTargetPosition st = Global.SpellMgr.GetSpellTargetPosition(SpellInfo.Id, spellEffectInfo.EffectIndex);

                    if (st != null)
                    {
                        // @todo fix this check
                        if (SpellInfo.HasEffect(SpellEffectName.TeleportUnits) ||
                            SpellInfo.HasEffect(SpellEffectName.TeleportWithSpellVisualKitLoadingScreen) ||
                            SpellInfo.HasEffect(SpellEffectName.Bind))
                            dest = new SpellDestination(st.Target_X, st.Target_Y, st.Target_Z, st.Target_Orientation, st.Target_mapId);
                        else if (st.Target_mapId == _caster.GetMapId())
                            dest = new SpellDestination(st.Target_X, st.Target_Y, st.Target_Z, st.Target_Orientation);
                    }
                    else
                    {
                        Log.outDebug(LogFilter.Spells, "SPELL: unknown Target coordinates for spell ID {0}", SpellInfo.Id);
                        WorldObject target = Targets.GetObjectTarget();

                        if (target)
                            dest = new SpellDestination(target);
                    }

                    break;
                case Framework.Constants.Targets.DestCasterFishing:
                    {
                        float minDist = SpellInfo.GetMinRange(true);
                        float maxDist = SpellInfo.GetMaxRange(true);
                        float dis = (float)RandomHelper.NextDouble() * (maxDist - minDist) + minDist;
                        float x, y, z;
                        float angle = (float)RandomHelper.NextDouble() * (MathFunctions.PI * 35.0f / 180.0f) - (float)(Math.PI * 17.5f / 180.0f);
                        _caster.GetClosePoint(out x, out y, out z, SharedConst.DefaultPlayerBoundingRadius, dis, angle);

                        float ground = _caster.GetMapHeight(x, y, z);
                        float liquidLevel = MapConst.VMAPInvalidHeightValue;
                        LiquidData liquidData = new();

                        if (_caster.GetMap().GetLiquidStatus(_caster.GetPhaseShift(), x, y, z, LiquidHeaderTypeFlags.AllLiquids, liquidData, _caster.GetCollisionHeight()) != 0)
                            liquidLevel = liquidData.Level;

                        if (liquidLevel <= ground) // When there is no liquid Map.GetWaterOrGroundLevel returns ground level
                        {
                            SendCastResult(SpellCastResult.NotHere);
                            SendChannelUpdate(0);
                            Finish(false);

                            return;
                        }

                        if (ground + 0.75 > liquidLevel)
                        {
                            SendCastResult(SpellCastResult.TooShallow);
                            SendChannelUpdate(0);
                            Finish(false);

                            return;
                        }

                        dest = new SpellDestination(x, y, liquidLevel, _caster.GetOrientation());

                        break;
                    }
                case Framework.Constants.Targets.DestCasterFrontLeap:
                case Framework.Constants.Targets.DestCasterMovementDirection:
                    {
                        Unit unitCaster = _caster.ToUnit();

                        if (unitCaster == null)
                            break;

                        float dist = spellEffectInfo.CalcRadius(unitCaster);
                        float angle = targetType.CalcDirectionAngle();

                        if (targetType.GetTarget() == Framework.Constants.Targets.DestCasterMovementDirection)
                            switch (_caster.MovementInfo.GetMovementFlags() & (MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight))
                            {
                                case MovementFlag.None:
                                case MovementFlag.Forward:
                                case MovementFlag.Forward | MovementFlag.Backward:
                                case MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
                                case MovementFlag.Forward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
                                case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
                                    angle = 0.0f;

                                    break;
                                case MovementFlag.Backward:
                                case MovementFlag.Backward | MovementFlag.StrafeLeft | MovementFlag.StrafeRight:
                                    angle = MathF.PI;

                                    break;
                                case MovementFlag.StrafeLeft:
                                case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeLeft:
                                    angle = (MathF.PI / 2);

                                    break;
                                case MovementFlag.Forward | MovementFlag.StrafeLeft:
                                    angle = (MathF.PI / 4);

                                    break;
                                case MovementFlag.Backward | MovementFlag.StrafeLeft:
                                    angle = (3 * MathF.PI / 4);

                                    break;
                                case MovementFlag.StrafeRight:
                                case MovementFlag.Forward | MovementFlag.Backward | MovementFlag.StrafeRight:
                                    angle = (-MathF.PI / 2);

                                    break;
                                case MovementFlag.Forward | MovementFlag.StrafeRight:
                                    angle = (-MathF.PI / 4);

                                    break;
                                case MovementFlag.Backward | MovementFlag.StrafeRight:
                                    angle = (-3 * MathF.PI / 4);

                                    break;
                                default:
                                    angle = 0.0f;

                                    break;
                            }

                        Position pos = new(dest.Position);

                        unitCaster.MovePositionToFirstCollision(pos, dist, angle);
                        dest.Relocate(pos);

                        break;
                    }
                case Framework.Constants.Targets.DestCasterGround:
                case Framework.Constants.Targets.DestCasterGround2:
                    dest.Position.Z = _caster.GetMapWaterOrGroundLevel(dest.Position.GetPositionX(), dest.Position.GetPositionY(), dest.Position.GetPositionZ());

                    break;
                case Framework.Constants.Targets.DestSummoner:
                    {
                        Unit unitCaster = _caster.ToUnit();

                        if (unitCaster != null)
                        {
                            TempSummon casterSummon = unitCaster.ToTempSummon();

                            if (casterSummon != null)
                            {
                                WorldObject summoner = casterSummon.GetSummoner();

                                if (summoner != null)
                                    dest = new SpellDestination(summoner);
                            }
                        }

                        break;
                    }
                default:
                    {
                        float dist = spellEffectInfo.CalcRadius(_caster);
                        float angl = targetType.CalcDirectionAngle();
                        float objSize = _caster.GetCombatReach();

                        switch (targetType.GetTarget())
                        {
                            case Framework.Constants.Targets.DestCasterSummon:
                                dist = SharedConst.PetFollowDist;

                                break;
                            case Framework.Constants.Targets.DestCasterRandom:
                                if (dist > objSize)
                                    dist = objSize + (dist - objSize) * (float)RandomHelper.NextDouble();

                                break;
                            case Framework.Constants.Targets.DestCasterFrontLeft:
                            case Framework.Constants.Targets.DestCasterBackLeft:
                            case Framework.Constants.Targets.DestCasterFrontRight:
                            case Framework.Constants.Targets.DestCasterBackRight:
                                {
                                    float DefaultTotemDistance = 3.0f;

                                    if (!spellEffectInfo.HasRadius() &&
                                        !spellEffectInfo.HasMaxRadius())
                                        dist = DefaultTotemDistance;

                                    break;
                                }
                            default:
                                break;
                        }

                        if (dist < objSize)
                            dist = objSize;

                        Position pos = new(dest.Position);
                        _caster.MovePositionToFirstCollision(pos, dist, angl);

                        dest.Relocate(pos);

                        break;
                    }
            }

            if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

            CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
            Targets.SetDst(dest);
        }

        private void SelectImplicitTargetDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            WorldObject target = Targets.GetObjectTarget();

            SpellDestination dest = new(target);

            switch (targetType.GetTarget())
            {
                case Framework.Constants.Targets.DestTargetEnemy:
                case Framework.Constants.Targets.DestAny:
                case Framework.Constants.Targets.DestTargetAlly:
                    break;
                default:
                    {
                        float angle = targetType.CalcDirectionAngle();
                        float dist = spellEffectInfo.CalcRadius(null);

                        if (targetType.GetTarget() == Framework.Constants.Targets.DestRandom)
                            dist *= (float)RandomHelper.NextDouble();

                        Position pos = new(dest.Position);
                        target.MovePositionToFirstCollision(pos, dist, angle);

                        dest.Relocate(pos);
                    }

                    break;
            }

            if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

            CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
            Targets.SetDst(dest);
        }

        private void SelectImplicitDestDestTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            // set destination to caster if no dest provided
            // can only happen if previous destination Target could not be set for some reason
            // (not found nearby Target, or channel Target for example
            // maybe we should abort the spell in such case?
            CheckDst();

            SpellDestination dest = Targets.GetDst();

            switch (targetType.GetTarget())
            {
                case Framework.Constants.Targets.DestDynobjEnemy:
                case Framework.Constants.Targets.DestDynobjAlly:
                case Framework.Constants.Targets.DestDynobjNone:
                case Framework.Constants.Targets.DestDest:
                    break;
                case Framework.Constants.Targets.DestDestGround:
                    dest.Position.Z = _caster.GetMapHeight(dest.Position.GetPositionX(), dest.Position.GetPositionY(), dest.Position.GetPositionZ());

                    break;
                default:
                    {
                        float angle = targetType.CalcDirectionAngle();
                        float dist = spellEffectInfo.CalcRadius(_caster);

                        if (targetType.GetTarget() == Framework.Constants.Targets.DestRandom)
                            dist *= (float)RandomHelper.NextDouble();

                        Position pos = new(Targets.GetDstPos());
                        _caster.MovePositionToFirstCollision(pos, dist, angle);

                        dest.Relocate(pos);
                    }

                    break;
            }

            if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

            CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
            Targets.ModDst(dest);
        }

        private void SelectImplicitCasterObjectTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            WorldObject target = null;
            bool checkIfValid = true;

            switch (targetType.GetTarget())
            {
                case Framework.Constants.Targets.UnitCaster:
                    target = _caster;
                    checkIfValid = false;

                    break;
                case Framework.Constants.Targets.UnitMaster:
                    target = _caster.GetCharmerOrOwner();

                    break;
                case Framework.Constants.Targets.UnitPet:
                    {
                        Unit unitCaster = _caster.ToUnit();

                        if (unitCaster != null)
                            target = unitCaster.GetGuardianPet();

                        break;
                    }
                case Framework.Constants.Targets.UnitSummoner:
                    {
                        Unit unitCaster = _caster.ToUnit();

                        if (unitCaster != null)
                            if (unitCaster.IsSummon())
                                target = unitCaster.ToTempSummon().GetSummonerUnit();

                        break;
                    }
                case Framework.Constants.Targets.UnitVehicle:
                    {
                        Unit unitCaster = _caster.ToUnit();

                        if (unitCaster != null)
                            target = unitCaster.GetVehicleBase();

                        break;
                    }
                case Framework.Constants.Targets.UnitPassenger0:
                case Framework.Constants.Targets.UnitPassenger1:
                case Framework.Constants.Targets.UnitPassenger2:
                case Framework.Constants.Targets.UnitPassenger3:
                case Framework.Constants.Targets.UnitPassenger4:
                case Framework.Constants.Targets.UnitPassenger5:
                case Framework.Constants.Targets.UnitPassenger6:
                case Framework.Constants.Targets.UnitPassenger7:
                    Creature vehicleBase = _caster.ToCreature();

                    if (vehicleBase != null &&
                        vehicleBase.IsVehicle())
                        target = vehicleBase.GetVehicleKit().GetPassenger((sbyte)(targetType.GetTarget() - Framework.Constants.Targets.UnitPassenger0));

                    break;
                case Framework.Constants.Targets.UnitTargetTapList:
                    Creature creatureCaster = _caster.ToCreature();

                    if (creatureCaster != null &&
                        !creatureCaster.GetTapList().Empty())
                        target = Global.ObjAccessor.GetWorldObject(creatureCaster, creatureCaster.GetTapList().SelectRandom());

                    break;
                case Framework.Constants.Targets.UnitOwnCritter:
                    {
                        Unit unitCaster = _caster.ToUnit();

                        if (unitCaster != null)
                            target = ObjectAccessor.GetCreatureOrPetOrVehicle(_caster, unitCaster.GetCritterGUID());

                        break;
                    }
                default:
                    break;
            }

            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

            if (target)
            {
                if (target.IsUnit())
                    AddUnitTarget(target.ToUnit(), 1u << (int)spellEffectInfo.EffectIndex, checkIfValid);
                else if (target.IsGameObject())
                    AddGOTarget(target.ToGameObject(), 1u << (int)spellEffectInfo.EffectIndex);
                else if (target.IsCorpse())
                    AddCorpseTarget(target.ToCorpse(), 1u << (int)spellEffectInfo.EffectIndex);
            }
        }

        private void SelectImplicitTargetObjectTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            WorldObject target = Targets.GetObjectTarget();

            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, targetType);

            Item item = Targets.GetItemTarget();

            if (target != null)
            {
                if (target.IsUnit())
                    AddUnitTarget(target.ToUnit(), 1u << (int)spellEffectInfo.EffectIndex, true, false);
                else if (target.IsGameObject())
                    AddGOTarget(target.ToGameObject(), 1u << (int)spellEffectInfo.EffectIndex);
                else if (target.IsCorpse())
                    AddCorpseTarget(target.ToCorpse(), 1u << (int)spellEffectInfo.EffectIndex);

                SelectImplicitChainTargets(spellEffectInfo, targetType, target, 1u << (int)spellEffectInfo.EffectIndex);
            }
            // Script hook can remove object Target and we would wrongly land here
            else if (item != null)
            {
                AddItemTarget(item, 1u << (int)spellEffectInfo.EffectIndex);
            }
        }

        private void SelectImplicitChainTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, WorldObject target, uint effMask)
        {
            int maxTargets = spellEffectInfo.ChainTargets;
            Player modOwner = _caster.GetSpellModOwner();

            if (modOwner)
                modOwner.ApplySpellMod(SpellInfo, SpellModOp.ChainTargets, ref maxTargets, this);

            if (maxTargets > 1)
            {
                // mark Damage multipliers as used
                for (int k = (int)spellEffectInfo.EffectIndex; k < SpellInfo.GetEffects().Count; ++k)
                    if (Convert.ToBoolean(effMask & (1 << (int)k)))
                        _damageMultipliers[spellEffectInfo.EffectIndex] = 1.0f;

                _applyMultiplierMask |= effMask;

                List<WorldObject> targets = new();
                SearchChainTargets(targets, (uint)maxTargets - 1, target, targetType.GetObjectType(), targetType.GetCheckType(), spellEffectInfo, targetType.GetTarget() == Framework.Constants.Targets.UnitChainhealAlly);

                // Chain primary Target is added earlier
                CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

                Position losPosition = SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) ? _caster : target;

                foreach (var obj in targets)
                {
                    Unit unitTarget = obj.ToUnit();

                    if (unitTarget)
                        AddUnitTarget(unitTarget, effMask, false, true, losPosition);

                    if (!SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) &&
                        !spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
                        losPosition = obj;
                }
            }
        }

        private float Tangent(float x)
        {
            x = (float)Math.Tan(x);

            if (x < 100000.0f &&
                x > -100000.0f) return x;

            if (x >= 100000.0f) return 100000.0f;
            if (x <= 100000.0f) return -100000.0f;

            return 0.0f;
        }

        private void SelectImplicitTrajTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType)
        {
            if (!Targets.HasTraj())
                return;

            float dist2d = Targets.GetDist2d();

            if (dist2d == 0)
                return;

            Position srcPos = Targets.GetSrcPos();
            srcPos.SetOrientation(_caster.GetOrientation());
            float srcToDestDelta = Targets.GetDstPos().Z - srcPos.Z;

            List<WorldObject> targets = new();
            var spellTraj = new WorldObjectSpellTrajTargetCheck(dist2d, srcPos, _caster, SpellInfo, targetType.GetCheckType(), spellEffectInfo.ImplicitTargetConditions, SpellTargetObjectTypes.None);
            var searcher = new WorldObjectListSearcher(_caster, targets, spellTraj);
            SearchTargets(searcher, GridMapTypeMask.All, _caster, srcPos, dist2d);

            if (targets.Empty())
                return;

            targets.Sort(new ObjectDistanceOrderPred(_caster));

            float b = Tangent(Targets.GetPitch());
            float a = (srcToDestDelta - dist2d * b) / (dist2d * dist2d);

            if (a > -0.0001f)
                a = 0f;

            // We should check if triggered spell has greater range (which is true in many cases, and initial spell has too short max range)
            // limit max range to 300 yards, sometimes triggered spells can have 50000yds
            float bestDist = SpellInfo.GetMaxRange(false);
            SpellInfo triggerSpellInfo = Global.SpellMgr.GetSpellInfo(spellEffectInfo.TriggerSpell, GetCastDifficulty());

            if (triggerSpellInfo != null)
                bestDist = Math.Min(Math.Max(bestDist, triggerSpellInfo.GetMaxRange(false)), Math.Min(dist2d, 300.0f));

            // GameObjects don't cast traj
            Unit unitCaster = _caster.ToUnit();

            foreach (var obj in targets)
            {
                if (SpellInfo.CheckTarget(unitCaster, obj, true) != SpellCastResult.SpellCastOk)
                    continue;

                Unit unitTarget = obj.ToUnit();

                if (unitTarget)
                {
                    if (unitCaster == obj ||
                        unitCaster.IsOnVehicle(unitTarget) ||
                        unitTarget.GetVehicle())
                        continue;

                    Creature creatureTarget = unitTarget.ToCreature();

                    if (creatureTarget)
                        if (!creatureTarget.GetCreatureTemplate().TypeFlags.HasAnyFlag(CreatureTypeFlags.CollideWithMissiles))
                            continue;
                }

                float size = Math.Max(obj.GetCombatReach(), 1.0f);
                float objDist2d = srcPos.GetExactDist2d(obj);
                float dz = obj.GetPositionZ() - srcPos.Z;

                float horizontalDistToTraj = (float)Math.Abs(objDist2d * Math.Sin(srcPos.GetRelativeAngle(obj)));
                float sizeFactor = (float)Math.Cos((horizontalDistToTraj / size) * (Math.PI / 2.0f));
                float distToHitPoint = (float)Math.Max(objDist2d * Math.Cos(srcPos.GetRelativeAngle(obj)) - size * sizeFactor, 0.0f);
                float height = distToHitPoint * (a * distToHitPoint + b);

                if (Math.Abs(dz - height) > size + b / 2.0f + SpellConst.TrajectoryMissileSize)
                    continue;

                if (distToHitPoint < bestDist)
                {
                    bestDist = distToHitPoint;

                    break;
                }
            }

            if (dist2d > bestDist)
            {
                float x = (float)(Targets.GetSrcPos().X + Math.Cos(unitCaster.GetOrientation()) * bestDist);
                float y = (float)(Targets.GetSrcPos().Y + Math.Sin(unitCaster.GetOrientation()) * bestDist);
                float z = Targets.GetSrcPos().Z + bestDist * (a * bestDist + b);

                SpellDestination dest = new(x, y, z, unitCaster.GetOrientation());

                if (SpellInfo.HasAttribute(SpellAttr4.UseFacingFromSpell))
                    dest.Position.SetOrientation(spellEffectInfo.PositionFacing);

                CallScriptDestinationTargetSelectHandlers(ref dest, spellEffectInfo.EffectIndex, targetType);
                Targets.ModDst(dest);
            }
        }

        private void SelectImplicitLineTargets(SpellEffectInfo spellEffectInfo, SpellImplicitTargetInfo targetType, uint effMask)
        {
            List<WorldObject> targets = new();
            SpellTargetObjectTypes objectType = targetType.GetObjectType();
            SpellTargetCheckTypes selectionType = targetType.GetCheckType();

            Position dst;

            switch (targetType.GetReferenceType())
            {
                case SpellTargetReferenceTypes.Src:
                    dst = Targets.GetSrcPos();

                    break;
                case SpellTargetReferenceTypes.Dest:
                    dst = Targets.GetDstPos();

                    break;
                case SpellTargetReferenceTypes.Caster:
                    dst = _caster;

                    break;
                case SpellTargetReferenceTypes.Target:
                    dst = Targets.GetUnitTarget();

                    break;
                default:
                    Cypher.Assert(false, "Spell.SelectImplicitLineTargets: received not implemented Target reference Type");

                    return;
            }

            var condList = spellEffectInfo.ImplicitTargetConditions;
            float radius = spellEffectInfo.CalcRadius(_caster) * SpellValue.RadiusMod;

            GridMapTypeMask containerTypeMask = GetSearcherTypeMask(objectType, condList);

            if (containerTypeMask != 0)
            {
                WorldObjectSpellLineTargetCheck check = new(_caster, dst, SpellInfo.Width != 0 ? SpellInfo.Width : _caster.GetCombatReach(), radius, _caster, SpellInfo, selectionType, condList, objectType);
                WorldObjectListSearcher searcher = new(_caster, targets, check, containerTypeMask);
                SearchTargets(searcher, containerTypeMask, _caster, _caster, radius);

                CallScriptObjectAreaTargetSelectHandlers(targets, spellEffectInfo.EffectIndex, targetType);

                if (!targets.Empty())
                {
                    // Other special Target selection goes here
                    uint maxTargets = SpellValue.MaxAffectedTargets;

                    if (maxTargets != 0)
                        if (maxTargets < targets.Count)
                        {
                            targets.Sort(new ObjectDistanceOrderPred(_caster));
                            targets.Resize(maxTargets);
                        }

                    foreach (var obj in targets)
                        if (obj.IsUnit())
                            AddUnitTarget(obj.ToUnit(), effMask, false);
                        else if (obj.IsGameObject())
                            AddGOTarget(obj.ToGameObject(), effMask);
                        else if (obj.IsCorpse())
                            AddCorpseTarget(obj.ToCorpse(), effMask);
                }
            }
        }

        private void SelectEffectTypeImplicitTargets(SpellEffectInfo spellEffectInfo)
        {
            // special case for SPELL_EFFECT_SUMMON_RAF_FRIEND and SPELL_EFFECT_SUMMON_PLAYER, queue them on map for later execution
            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.SummonRafFriend:
                case SpellEffectName.SummonPlayer:
                    if (_caster.IsTypeId(TypeId.Player) &&
                        !_caster.ToPlayer().GetTarget().IsEmpty())
                    {
                        WorldObject rafTarget = Global.ObjAccessor.FindPlayer(_caster.ToPlayer().GetTarget());

                        CallScriptObjectTargetSelectHandlers(ref rafTarget, spellEffectInfo.EffectIndex, new SpellImplicitTargetInfo());

                        // scripts may modify the Target - recheck
                        if (rafTarget != null &&
                            rafTarget.IsPlayer())
                        {
                            // Target is not stored in Target map for those spells
                            // since we're completely skipping AddUnitTarget logic, we need to check immunity manually
                            // eg. aura 21546 makes Target immune to summons
                            Player player = rafTarget.ToPlayer();

                            if (player.IsImmunedToSpellEffect(SpellInfo, spellEffectInfo, null))
                                return;

                            var spell = this;
                            var targetGuid = rafTarget.GetGUID();

                            rafTarget.GetMap()
                                     .AddFarSpellCallback(map =>
                                                          {
                                                              Player player = Global.ObjAccessor.GetPlayer(map, targetGuid);

                                                              if (player == null)
                                                                  return;

                                                              // check immunity again in case it changed during update
                                                              if (player.IsImmunedToSpellEffect(spell.GetSpellInfo(), spellEffectInfo, null))
                                                                  return;

                                                              spell.HandleEffects(player, null, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);
                                                          });
                        }
                    }

                    return;
                default:
                    break;
            }

            // select spell implicit targets based on effect Type
            if (spellEffectInfo.GetImplicitTargetType() == 0)
                return;

            SpellCastTargetFlags targetMask = spellEffectInfo.GetMissingTargetMask();

            if (targetMask == 0)
                return;

            WorldObject target = null;

            switch (spellEffectInfo.GetImplicitTargetType())
            {
                // add explicit object Target or self to the Target map
                case SpellEffectImplicitTargetTypes.Explicit:
                    // player which not released his spirit is Unit, but Target flag for it is TARGET_FLAG_CORPSE_MASK
                    if (Convert.ToBoolean(targetMask & (SpellCastTargetFlags.UnitMask | SpellCastTargetFlags.CorpseMask)))
                    {
                        Unit unitTarget = Targets.GetUnitTarget();

                        if (unitTarget != null)
                        {
                            target = unitTarget;
                        }
                        else if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.CorpseMask))
                        {
                            Corpse corpseTarget = Targets.GetCorpseTarget();

                            if (corpseTarget != null)
                                target = corpseTarget;
                        }
                        else //if (targetMask & TARGET_FLAG_UNIT_MASK)
                        {
                            target = _caster;
                        }
                    }

                    if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.ItemMask))
                    {
                        Item itemTarget = Targets.GetItemTarget();

                        if (itemTarget != null)
                            AddItemTarget(itemTarget, (uint)(1 << (int)spellEffectInfo.EffectIndex));

                        return;
                    }

                    if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.GameobjectMask))
                        target = Targets.GetGOTarget();

                    break;
                // add self to the Target map
                case SpellEffectImplicitTargetTypes.Caster:
                    if (Convert.ToBoolean(targetMask & SpellCastTargetFlags.UnitMask))
                        target = _caster;

                    break;
                default:
                    break;
            }

            CallScriptObjectTargetSelectHandlers(ref target, spellEffectInfo.EffectIndex, new SpellImplicitTargetInfo());

            if (target != null)
            {
                if (target.IsUnit())
                    AddUnitTarget(target.ToUnit(), 1u << (int)spellEffectInfo.EffectIndex, false);
                else if (target.IsGameObject())
                    AddGOTarget(target.ToGameObject(), 1u << (int)spellEffectInfo.EffectIndex);
                else if (target.IsCorpse())
                    AddCorpseTarget(target.ToCorpse(), 1u << (int)spellEffectInfo.EffectIndex);
            }
        }

        private void SearchTargets(Notifier notifier, GridMapTypeMask containerMask, WorldObject referer, Position pos, float radius)
        {
            if (containerMask == 0)
                return;

            bool searchInGrid = containerMask.HasAnyFlag(GridMapTypeMask.Creature | GridMapTypeMask.GameObject);
            bool searchInWorld = containerMask.HasAnyFlag(GridMapTypeMask.Creature | GridMapTypeMask.Player | GridMapTypeMask.Corpse);

            if (searchInGrid || searchInWorld)
            {
                float x = pos.GetPositionX();
                float y = pos.GetPositionY();

                CellCoord p = GridDefines.ComputeCellCoord(x, y);
                Cell cell = new(p);
                cell.SetNoCreate();

                Map map = referer.GetMap();

                if (searchInWorld)
                    Cell.VisitWorldObjects(x, y, map, notifier, radius);

                if (searchInGrid)
                    Cell.VisitGridObjects(x, y, map, notifier, radius);
            }
        }

        private WorldObject SearchNearbyTarget(float range, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
        {
            GridMapTypeMask containerTypeMask = GetSearcherTypeMask(objectType, condList);

            if (containerTypeMask == 0)
                return null;

            var check = new WorldObjectSpellNearbyTargetCheck(range, _caster, SpellInfo, selectionType, condList, objectType);
            var searcher = new WorldObjectLastSearcher(_caster, check, containerTypeMask);
            SearchTargets(searcher, containerTypeMask, _caster, _caster.GetPosition(), range);

            return searcher.GetTarget();
        }

        private void SearchAreaTargets(List<WorldObject> targets, float range, Position position, WorldObject referer, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectionType, List<Condition> condList)
        {
            var containerTypeMask = GetSearcherTypeMask(objectType, condList);

            if (containerTypeMask == 0)
                return;

            float extraSearchRadius = range > 0.0f ? SharedConst.ExtraCellSearchRadius : 0.0f;
            var check = new WorldObjectSpellAreaTargetCheck(range, position, _caster, referer, SpellInfo, selectionType, condList, objectType);
            var searcher = new WorldObjectListSearcher(_caster, targets, check, containerTypeMask);
            SearchTargets(searcher, containerTypeMask, _caster, position, range + extraSearchRadius);
        }

        private void SearchChainTargets(List<WorldObject> targets, uint chainTargets, WorldObject target, SpellTargetObjectTypes objectType, SpellTargetCheckTypes selectType, SpellEffectInfo spellEffectInfo, bool isChainHeal)
        {
            // max dist for Jump Target selection
            float jumpRadius = 0.0f;

            switch (SpellInfo.DmgClass)
            {
                case SpellDmgClass.Ranged:
                    // 7.5y for multi shot
                    jumpRadius = 7.5f;

                    break;
                case SpellDmgClass.Melee:
                    // 5y for swipe, cleave and similar
                    jumpRadius = 5.0f;

                    break;
                case SpellDmgClass.None:
                case SpellDmgClass.Magic:
                    // 12.5y for chain heal spell since 3.2 patch
                    if (isChainHeal)
                        jumpRadius = 12.5f;
                    // 10y as default for magic chain spells
                    else
                        jumpRadius = 10.0f;

                    break;
            }

            Player modOwner = _caster.GetSpellModOwner();

            if (modOwner)
                modOwner.ApplySpellMod(SpellInfo, SpellModOp.ChainJumpDistance, ref jumpRadius, this);

            float searchRadius;

            if (SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster))
                searchRadius = GetMinMaxRange(false).maxRange;
            else if (spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
                searchRadius = jumpRadius;
            else
                searchRadius = jumpRadius * chainTargets;

            WorldObject chainSource = SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) ? _caster : target;
            List<WorldObject> tempTargets = new();
            SearchAreaTargets(tempTargets, searchRadius, chainSource, _caster, objectType, selectType, spellEffectInfo.ImplicitTargetConditions);
            tempTargets.Remove(target);

            // remove targets which are always invalid for chain spells
            // for some spells allow only chain targets in front of caster (swipe for example)
            if (SpellInfo.HasAttribute(SpellAttr5.MeleeChainTargeting))
                tempTargets.RemoveAll(obj => !_caster.HasInArc(MathF.PI, obj));

            while (chainTargets != 0)
            {
                // try to get unit for next chain Jump
                WorldObject found = null;

                // get unit with highest hp deficit in dist
                if (isChainHeal)
                {
                    uint maxHPDeficit = 0;

                    foreach (var obj in tempTargets)
                    {
                        Unit unitTarget = obj.ToUnit();

                        if (unitTarget != null)
                        {
                            uint deficit = (uint)(unitTarget.GetMaxHealth() - unitTarget.GetHealth());

                            if ((deficit > maxHPDeficit || found == null) &&
                                chainSource.IsWithinDist(unitTarget, jumpRadius) &&
                                chainSource.IsWithinLOSInMap(unitTarget, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                            {
                                found = obj;
                                maxHPDeficit = deficit;
                            }
                        }
                    }
                }
                // get closest object
                else
                {
                    foreach (var obj in tempTargets)
                        if (found == null)
                        {
                            if (chainSource.IsWithinDist(obj, jumpRadius) &&
                                chainSource.IsWithinLOSInMap(obj, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                                found = obj;
                        }
                        else if (chainSource.GetDistanceOrder(obj, found) &&
                                 chainSource.IsWithinLOSInMap(obj, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                        {
                            found = obj;
                        }
                }

                // not found any valid Target - chain ends
                if (found == null)
                    break;

                if (!SpellInfo.HasAttribute(SpellAttr2.ChainFromCaster) &&
                    !spellEffectInfo.EffectAttributes.HasFlag(SpellEffectAttributes.ChainFromInitialTarget))
                    chainSource = found;

                targets.Add(found);
                tempTargets.Remove(found);
                --chainTargets;
            }
        }

        private GameObject SearchSpellFocus()
        {
            var check = new GameObjectFocusCheck(_caster, SpellInfo.RequiresSpellFocus);
            var searcher = new GameObjectSearcher(_caster, check);
            SearchTargets(searcher, GridMapTypeMask.GameObject, _caster, _caster, _caster.GetVisibilityRange());

            return searcher.GetTarget();
        }

        private void PrepareDataForTriggerSystem()
        {
            //==========================================================================================
            // Now fill _data for trigger system, need know:
            // Create base triggers Flags for Attacker and Victim (_procAttacker, _procVictim and _hitMask)
            //==========================================================================================

            ProcVictim = ProcAttacker = new ProcFlagsInit();

            // Get _data for Type of attack and fill base info for trigger
            switch (SpellInfo.DmgClass)
            {
                case SpellDmgClass.Melee:
                    ProcAttacker = new ProcFlagsInit(ProcFlags.DealMeleeAbility);

                    if (AttackType == WeaponAttackType.OffAttack)
                        ProcAttacker.Or(ProcFlags.OffHandWeaponSwing);
                    else
                        ProcAttacker.Or(ProcFlags.MainHandWeaponSwing);

                    ProcVictim = new ProcFlagsInit(ProcFlags.TakeMeleeAbility);

                    break;
                case SpellDmgClass.Ranged:
                    // Auto attack
                    if (SpellInfo.HasAttribute(SpellAttr2.AutoRepeat))
                    {
                        ProcAttacker = new ProcFlagsInit(ProcFlags.DealRangedAttack);
                        ProcVictim = new ProcFlagsInit(ProcFlags.TakeRangedAttack);
                    }
                    else // Ranged spell attack
                    {
                        ProcAttacker = new ProcFlagsInit(ProcFlags.DealRangedAbility);
                        ProcVictim = new ProcFlagsInit(ProcFlags.TakeRangedAbility);
                    }

                    break;
                default:
                    if (SpellInfo.EquippedItemClass == ItemClass.Weapon &&
                        Convert.ToBoolean(SpellInfo.EquippedItemSubClassMask & (1 << (int)ItemSubClassWeapon.Wand)) &&
                        SpellInfo.HasAttribute(SpellAttr2.AutoRepeat)) // Wands auto attack
                    {
                        ProcAttacker = new ProcFlagsInit(ProcFlags.DealRangedAttack);
                        ProcVictim = new ProcFlagsInit(ProcFlags.TakeRangedAttack);
                    }

                    break;
                    // For other spells trigger procflags are set in Spell::TargetInfo::DoDamageAndTriggers
                    // Because spell positivity is dependant on Target
            }
        }

        private void AddUnitTarget(Unit target, uint effectMask, bool checkIfValid = true, bool Implicit = true, Position losPosition = null)
        {
            foreach (var spellEffectInfo in SpellInfo.GetEffects())
                if (!spellEffectInfo.IsEffect() ||
                    !CheckEffectTarget(target, spellEffectInfo, losPosition))
                    effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

            // no effects left
            if (effectMask == 0)
                return;

            if (checkIfValid)
                if (SpellInfo.CheckTarget(_caster, target, Implicit) != SpellCastResult.SpellCastOk) // skip stealth checks for AOE
                    return;

            // Check for effect immune skip if immuned
            foreach (var spellEffectInfo in SpellInfo.GetEffects())
                if (target.IsImmunedToSpellEffect(SpellInfo, spellEffectInfo, _caster))
                    effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

            ObjectGuid targetGUID = target.GetGUID();

            // Lookup Target in already in list
            var index = UniqueTargetInfo.FindIndex(target => target.TargetGUID == targetGUID);

            if (index != -1) // Found in list
            {
                // Immune effects removed from mask
                UniqueTargetInfo[index].EffectMask |= effectMask;

                return;
            }

            // This is new Target calculate _data for him

            // Get spell hit result on Target
            TargetInfo targetInfo = new();
            targetInfo.TargetGUID = targetGUID; // Store Target GUID
            targetInfo.EffectMask = effectMask; // Store all effects not immune
            targetInfo.IsAlive = target.IsAlive();

            // Calculate hit result
            WorldObject caster = _originalCaster ? _originalCaster : _caster;
            targetInfo.MissCondition = caster.SpellHitResult(target, SpellInfo, _canReflect && !(IsPositive() && _caster.IsFriendlyTo(target)));

            // Spell have speed - need calculate incoming Time
            // Incoming Time is zero for self casts. At least I think so.
            if (_caster != target)
            {
                float hitDelay = SpellInfo.LaunchDelay;
                WorldObject missileSource = _caster;

                if (SpellInfo.HasAttribute(SpellAttr4.BouncyChainMissiles))
                {
                    var previousTargetInfo = UniqueTargetInfo.FindLast(target => (target.EffectMask & effectMask) != 0);

                    if (previousTargetInfo != null)
                    {
                        hitDelay = 0.0f; // this is not the first Target in chain, LaunchDelay was already included

                        WorldObject previousTarget = Global.ObjAccessor.GetWorldObject(_caster, previousTargetInfo.TargetGUID);

                        if (previousTarget != null)
                            missileSource = previousTarget;

                        targetInfo.TimeDelay += previousTargetInfo.TimeDelay;
                    }
                }

                if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                {
                    hitDelay += SpellInfo.Speed;
                }
                else if (SpellInfo.Speed > 0.0f)
                {
                    // calculate spell incoming interval
                    /// @todo this is a hack
                    float dist = Math.Max(missileSource.GetDistance(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ()), 5.0f);
                    hitDelay += dist / SpellInfo.Speed;
                }

                targetInfo.TimeDelay += (ulong)Math.Floor(hitDelay * 1000.0f);
            }
            else
            {
                targetInfo.TimeDelay = 0L;
            }

            // If Target reflect spell back to caster
            if (targetInfo.MissCondition == SpellMissInfo.Reflect)
            {
                // Calculate reflected spell result on caster (shouldn't be able to reflect gameobject spells)
                Unit unitCaster = _caster.ToUnit();
                targetInfo.ReflectResult = unitCaster.SpellHitResult(unitCaster, SpellInfo, false); // can't reflect twice

                // Proc spell reflect aura when missile hits the original Target
                target.Events.AddEvent(new ProcReflectDelayed(target, _originalCasterGUID), target.Events.CalculateTime(TimeSpan.FromMilliseconds(targetInfo.TimeDelay)));

                // Increase Time interval for reflected spells by 1.5
                targetInfo.TimeDelay += targetInfo.TimeDelay >> 1;
            }
            else
            {
                targetInfo.ReflectResult = SpellMissInfo.None;
            }

            // Calculate minimum incoming Time
            if (targetInfo.TimeDelay != 0 &&
                (_delayMoment == 0 || _delayMoment > targetInfo.TimeDelay))
                _delayMoment = targetInfo.TimeDelay;

            // Add Target to list
            UniqueTargetInfo.Add(targetInfo);
        }

        private void AddGOTarget(GameObject go, uint effectMask)
        {
            foreach (var spellEffectInfo in SpellInfo.GetEffects())
                if (!spellEffectInfo.IsEffect() ||
                    !CheckEffectTarget(go, spellEffectInfo))
                    effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

            // no effects left
            if (effectMask == 0)
                return;

            ObjectGuid targetGUID = go.GetGUID();

            // Lookup Target in already in list
            var index = _uniqueGOTargetInfo.FindIndex(target => target.TargetGUID == targetGUID);

            if (index != -1) // Found in list
            {
                // Add only effect mask
                _uniqueGOTargetInfo[index].EffectMask |= effectMask;

                return;
            }

            // This is new Target calculate _data for him
            GOTargetInfo target = new();
            target.TargetGUID = targetGUID;
            target.EffectMask = effectMask;

            // Spell have speed - need calculate incoming Time
            if (_caster != go)
            {
                float hitDelay = SpellInfo.LaunchDelay;

                if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                {
                    hitDelay += SpellInfo.Speed;
                }
                else if (SpellInfo.Speed > 0.0f)
                {
                    // calculate spell incoming interval
                    float dist = Math.Max(_caster.GetDistance(go.GetPositionX(), go.GetPositionY(), go.GetPositionZ()), 5.0f);
                    hitDelay += dist / SpellInfo.Speed;
                }

                target.TimeDelay = (ulong)Math.Floor(hitDelay * 1000.0f);
            }
            else
            {
                target.TimeDelay = 0UL;
            }

            // Calculate minimum incoming Time
            if (target.TimeDelay != 0 &&
                (_delayMoment == 0 || _delayMoment > target.TimeDelay))
                _delayMoment = target.TimeDelay;

            // Add Target to list
            _uniqueGOTargetInfo.Add(target);
        }

        private void AddItemTarget(Item item, uint effectMask)
        {
            foreach (var spellEffectInfo in SpellInfo.GetEffects())
                if (!spellEffectInfo.IsEffect() ||
                    !CheckEffectTarget(item, spellEffectInfo))
                    effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

            // no effects left
            if (effectMask == 0)
                return;

            // Lookup Target in already in list
            var index = _uniqueItemInfo.FindIndex(target => target.TargetItem == item);

            if (index != -1) // Found in list
            {
                // Add only effect mask
                _uniqueItemInfo[index].EffectMask |= effectMask;

                return;
            }

            // This is new Target add _data

            ItemTargetInfo target = new();
            target.TargetItem = item;
            target.EffectMask = effectMask;

            _uniqueItemInfo.Add(target);
        }

        private void AddCorpseTarget(Corpse corpse, uint effectMask)
        {
            foreach (SpellEffectInfo spellEffectInfo in SpellInfo.GetEffects())
                if (!spellEffectInfo.IsEffect())
                    effectMask &= ~(1u << (int)spellEffectInfo.EffectIndex);

            // no effects left
            if (effectMask == 0)
                return;

            ObjectGuid targetGUID = corpse.GetGUID();

            // Lookup Target in already in list
            var corpseTargetInfo = _uniqueCorpseTargetInfo.Find(target => { return target.TargetGUID == targetGUID; });

            if (corpseTargetInfo != null) // Found in list
            {
                // Add only effect mask
                corpseTargetInfo.EffectMask |= effectMask;

                return;
            }

            // This is new Target calculate _data for him
            CorpseTargetInfo target = new();
            target.TargetGUID = targetGUID;
            target.EffectMask = effectMask;

            // Spell have speed - need calculate incoming Time
            if (_caster != corpse)
            {
                float hitDelay = SpellInfo.LaunchDelay;

                if (SpellInfo.HasAttribute(SpellAttr9.SpecialDelayCalculation))
                {
                    hitDelay += SpellInfo.Speed;
                }
                else if (SpellInfo.Speed > 0.0f)
                {
                    // calculate spell incoming interval
                    float dist = Math.Max(_caster.GetDistance(corpse.GetPositionX(), corpse.GetPositionY(), corpse.GetPositionZ()), 5.0f);
                    hitDelay += dist / SpellInfo.Speed;
                }

                target.TimeDelay = (ulong)Math.Floor(hitDelay * 1000.0f);
            }
            else
            {
                target.TimeDelay = 0;
            }

            // Calculate minimum incoming Time
            if (target.TimeDelay != 0 &&
                (_delayMoment == 0 || _delayMoment > target.TimeDelay))
                _delayMoment = target.TimeDelay;

            // Add Target to list
            _uniqueCorpseTargetInfo.Add(target);
        }

        private void AddDestTarget(SpellDestination dest, uint effIndex)
        {
            _destTargets[effIndex] = dest;
        }

        private bool UpdateChanneledTargetList()
        {
            // Not need check return true
            if (_channelTargetEffectMask == 0)
                return true;

            uint channelTargetEffectMask = _channelTargetEffectMask;
            uint channelAuraMask = 0;

            foreach (var spellEffectInfo in SpellInfo.GetEffects())
                if (spellEffectInfo.IsEffect(SpellEffectName.ApplyAura))
                    channelAuraMask |= 1u << (int)spellEffectInfo.EffectIndex;

            channelAuraMask &= channelTargetEffectMask;

            float range = 0;

            if (channelAuraMask != 0)
            {
                range = SpellInfo.GetMaxRange(IsPositive());
                Player modOwner = _caster.GetSpellModOwner();

                modOwner?.ApplySpellMod(SpellInfo, SpellModOp.Range, ref range, this);

                // add little tolerance level
                range += Math.Min(3.0f, range * 0.1f); // 10% but no more than 3.0f
            }

            foreach (var targetInfo in UniqueTargetInfo)
                if (targetInfo.MissCondition == SpellMissInfo.None &&
                    Convert.ToBoolean(channelTargetEffectMask & targetInfo.EffectMask))
                {
                    Unit unit = _caster.GetGUID() == targetInfo.TargetGUID ? _caster.ToUnit() : Global.ObjAccessor.GetUnit(_caster, targetInfo.TargetGUID);

                    if (unit == null)
                    {
                        Unit unitCaster = _caster.ToUnit();

                        unitCaster?.RemoveChannelObject(targetInfo.TargetGUID);

                        continue;
                    }

                    if (IsValidDeadOrAliveTarget(unit))
                    {
                        if (Convert.ToBoolean(channelAuraMask & targetInfo.EffectMask))
                        {
                            AuraApplication aurApp = unit.GetAuraApplication(SpellInfo.Id, _originalCasterGUID);

                            if (aurApp != null)
                            {
                                if (_caster != unit &&
                                    !_caster.IsWithinDistInMap(unit, range))
                                {
                                    targetInfo.EffectMask &= ~aurApp.GetEffectMask();
                                    unit.RemoveAura(aurApp);
                                    Unit unitCaster = _caster.ToUnit();

                                    unitCaster?.RemoveChannelObject(targetInfo.TargetGUID);

                                    continue;
                                }
                            }
                            else // aura is dispelled
                            {
                                Unit unitCaster = _caster.ToUnit();

                                unitCaster?.RemoveChannelObject(targetInfo.TargetGUID);

                                continue;
                            }
                        }

                        channelTargetEffectMask &= ~targetInfo.EffectMask; // remove from need alive mask effect that have alive Target
                    }
                }

            // is all effects from _needAliveTargetMask have alive targets
            return channelTargetEffectMask == 0;
        }

        private void _cast(bool skipCheck = false)
        {
            if (!UpdatePointers())
            {
                // cancel the spell if UpdatePointers() returned false, something wrong happened there
                Cancel();

                return;
            }

            // cancel at lost explicit Target during cast
            if (!Targets.GetObjectTargetGUID().IsEmpty() &&
                Targets.GetObjectTarget() == null)
            {
                Cancel();

                return;
            }

            Player playerCaster = _caster.ToPlayer();

            if (playerCaster != null)
            {
                // now that we've done the basic check, now run the scripts
                // should be done before the spell is actually executed
                Global.ScriptMgr.ForEach<IPlayerOnSpellCast>(p => p.OnSpellCast(playerCaster, this, skipCheck));

                // As of 3.0.2 pets begin attacking their owner's Target immediately
                // Let any pets know we've attacked something. Check DmgClass for harmful spells only
                // This prevents spells such as Hunter's Mark from triggering pet attack
                if (SpellInfo.DmgClass != SpellDmgClass.None)
                {
                    Unit target = Targets.GetUnitTarget();

                    if (target != null)
                        foreach (Unit controlled in playerCaster.Controlled)
                        {
                            Creature cControlled = controlled.ToCreature();

                            if (cControlled != null)
                            {
                                CreatureAI controlledAI = cControlled.GetAI();

                                controlledAI?.OwnerAttacked(target);
                            }
                        }
                }
            }

            SetExecutedCurrently(true);

            // Should this be done for original caster?
            Player modOwner = _caster.GetSpellModOwner();

            // Set spell which will drop charges for triggered cast spells
            // if not successfully casted, will be remove in finish(false)
            modOwner?.SetSpellModTakingSpell(this, true);

            CallScriptBeforeCastHandlers();

            // skip check if done already (for instant cast spells for example)
            if (!skipCheck)
            {
                void cleanupSpell(SpellCastResult result, int? param1 = null, int? param2 = null)
                {
                    SendCastResult(result, param1, param2);
                    SendInterrupted(0);

                    if (modOwner)
                        modOwner.SetSpellModTakingSpell(this, false);

                    Finish(false);
                    SetExecutedCurrently(false);
                }

                int param1 = 0, param2 = 0;
                SpellCastResult castResult = CheckCast(false, ref param1, ref param2);

                if (castResult != SpellCastResult.SpellCastOk)
                {
                    cleanupSpell(castResult, param1, param2);

                    return;
                }

                // additional check after cast bar completes (must not be in CheckCast)
                // if trade not complete then remember it in trade _data
                if (Convert.ToBoolean(Targets.GetTargetMask() & SpellCastTargetFlags.TradeItem))
                    if (modOwner)
                    {
                        TradeData my_trade = modOwner.GetTradeData();

                        if (my_trade != null)
                            if (!my_trade.IsInAcceptProcess())
                            {
                                // Spell will be casted at completing the trade. Silently ignore at this place
                                my_trade.SetSpell(SpellInfo.Id, CastItem);
                                cleanupSpell(SpellCastResult.DontReport);

                                return;
                            }
                    }

                // check diminishing returns (again, only after finish cast bar, tested on retail)
                Unit target = Targets.GetUnitTarget();

                if (target != null)
                {
                    uint aura_effmask = 0;

                    foreach (var spellEffectInfo in SpellInfo.GetEffects())
                        if (spellEffectInfo.IsUnitOwnedAuraEffect())
                            aura_effmask |= 1u << (int)spellEffectInfo.EffectIndex;

                    if (aura_effmask != 0)
                        if (SpellInfo.GetDiminishingReturnsGroupForSpell() != 0)
                        {
                            DiminishingReturnsType type = SpellInfo.GetDiminishingReturnsGroupType();

                            if (type == DiminishingReturnsType.All ||
                                (type == DiminishingReturnsType.Player && target.IsAffectedByDiminishingReturns()))
                            {
                                Unit caster1 = _originalCaster ? _originalCaster : _caster.ToUnit();

                                if (caster1 != null)
                                    if (target.HasStrongerAuraWithDR(SpellInfo, caster1))
                                    {
                                        cleanupSpell(SpellCastResult.AuraBounced);

                                        return;
                                    }
                            }
                        }
                }
            }

            // The spell focusing is making sure that we have a valid cast Target Guid when we need it so only check for a Guid value here.
            Creature creatureCaster = _caster.ToCreature();

            if (creatureCaster != null)
                if (!creatureCaster.GetTarget().IsEmpty() &&
                    !creatureCaster.HasUnitFlag(UnitFlags.Possessed))
                {
                    WorldObject target = Global.ObjAccessor.GetUnit(creatureCaster, creatureCaster.GetTarget());

                    if (target != null)
                        creatureCaster.SetInFront(target);
                }

            SelectSpellTargets();

            // Spell may be finished after Target map check
            if (_spellState == SpellState.Finished)
            {
                SendInterrupted(0);

                if (_caster.IsTypeId(TypeId.Player))
                    _caster.ToPlayer().SetSpellModTakingSpell(this, false);

                Finish(false);
                SetExecutedCurrently(false);

                return;
            }

            Unit unitCaster = _caster.ToUnit();

            if (unitCaster != null)
                if (SpellInfo.HasAttribute(SpellAttr1.DismissPetFirst))
                {
                    Creature pet = ObjectAccessor.GetCreature(_caster, unitCaster.GetPetGUID());

                    pet?.DespawnOrUnsummon();
                }

            PrepareTriggersExecutedOnHit();

            CallScriptOnCastHandlers();

            // traded items have trade Slot instead of Guid in _itemTargetGUID
            // set to real Guid to be sent later to the client
            Targets.UpdateTradeSlotItem();

            Player player = _caster.ToPlayer();

            if (player != null)
            {
                if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastItem) &&
                    CastItem != null)
                {
                    player.StartCriteriaTimer(CriteriaStartEvent.UseItem, CastItem.GetEntry());
                    player.UpdateCriteria(CriteriaType.UseItem, CastItem.GetEntry());
                }

                player.UpdateCriteria(CriteriaType.CastSpell, SpellInfo.Id);
            }

            Item targetItem = Targets.GetItemTarget();

            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost))
            {
                // Powers have to be taken before SendSpellGo
                TakePower();
                TakeReagents(); // we must remove reagents before HandleEffects to allow place crafted Item in same Slot
            }
            else if (targetItem != null)
            {
                // Not own traded Item (in trader trade Slot) req. reagents including triggered spell case
                if (targetItem.GetOwnerGUID() != _caster.GetGUID())
                    TakeReagents();
            }

            // CAST SPELL
            if (!SpellInfo.HasAttribute(SpellAttr12.StartCooldownOnCastStart))
                SendSpellCooldown();

            if (SpellInfo.LaunchDelay == 0)
            {
                HandleLaunchPhase();
                _launchHandled = true;
            }

            // we must send smsg_spell_go packet before _castItem delete in TakeCastItem()...
            SendSpellGo();

            if (!SpellInfo.IsChanneled())
                creatureCaster?.ReleaseSpellFocus(this);

            // Okay, everything is prepared. Now we need to distinguish between immediate and evented delayed spells
            if ((SpellInfo.HasHitDelay() && !SpellInfo.IsChanneled()) ||
                SpellInfo.HasAttribute(SpellAttr4.NoHarmfulThreat))
            {
                // Remove used for cast Item if need (it can be already NULL after TakeReagents call
                // in case delayed spell remove Item at cast delay start
                TakeCastItem();

                // Okay, maps created, now prepare Flags
                _immediateHandled = false;
                _spellState = SpellState.Delayed;
                SetDelayStart(0);

                unitCaster = _caster.ToUnit();

                if (unitCaster != null)
                    if (unitCaster.HasUnitState(UnitState.Casting) &&
                        !unitCaster.IsNonMeleeSpellCast(false, false, true))
                        unitCaster.ClearUnitState(UnitState.Casting);
            }
            else
            {
                // Immediate spell, no big deal
                HandleImmediate();
            }

            CallScriptAfterCastHandlers();

            var spell_triggered = Global.SpellMgr.GetSpellLinked(SpellLinkedType.Cast, SpellInfo.Id);

            if (spell_triggered != null)
                foreach (var spellId in spell_triggered)
                    if (spellId < 0)
                    {
                        unitCaster = _caster.ToUnit();

                        unitCaster?.RemoveAurasDueToSpell((uint)-spellId);
                    }
                    else
                    {
                        _caster.CastSpell(Targets.GetUnitTarget() ?? _caster, (uint)spellId, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetTriggeringSpell(this));
                    }

            if (modOwner != null)
            {
                modOwner.SetSpellModTakingSpell(this, false);

                //Clear spell cooldowns after every spell is cast if .cheat cooldown is enabled.
                if (_originalCaster != null &&
                    modOwner.GetCommandStatus(PlayerCommandStates.Cooldown))
                {
                    _originalCaster.GetSpellHistory().ResetCooldown(SpellInfo.Id, true);
                    _originalCaster.GetSpellHistory().RestoreCharge(SpellInfo.ChargeCategoryId);
                }
            }

            SetExecutedCurrently(false);

            if (!_originalCaster)
                return;

            // Handle procs on cast
            ProcFlagsInit procAttacker = ProcAttacker;

            if (!procAttacker)
            {
                if (SpellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
                }
                else if (SpellInfo.HasAttribute(SpellAttr0.IsAbility))
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulAbility);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulSpell);
                }
                else
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulSpell);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulSpell);
                }
            }

            procAttacker.Or(ProcFlags2.CastSuccessful);

            ProcFlagsHit hitMask = HitMask;

            if (!hitMask.HasAnyFlag(ProcFlagsHit.Critical))
                hitMask |= ProcFlagsHit.Normal;

            if (!_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnoreAuraInterruptFlags) &&
                !SpellInfo.HasAttribute(SpellAttr2.NotAnAction))
                _originalCaster.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.ActionDelayed, SpellInfo);

            if (!SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
                Unit.ProcSkillsAndAuras(_originalCaster, null, procAttacker, new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Cast, hitMask, this, null, null);

            // Call CreatureAI hook OnSpellCast
            Creature caster = _originalCaster.ToCreature();

            if (caster)
                if (caster.IsAIEnabled())
                    caster.GetAI().OnSpellCast(GetSpellInfo());
        }

        private void DoProcessTargetContainer<T>(List<T> targetContainer) where T : TargetInfoBase
        {
            foreach (TargetInfoBase target in targetContainer)
                target.PreprocessTarget(this);

            foreach (var spellEffectInfo in SpellInfo.GetEffects())
            {
                foreach (TargetInfoBase target in targetContainer)
                    if ((target.EffectMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0)
                        target.DoTargetSpellHit(this, spellEffectInfo);
            }

            foreach (TargetInfoBase target in targetContainer)
                target.DoDamageAndTriggers(this);
        }

        private void HandleImmediate()
        {
            // start channeling if applicable
            if (SpellInfo.IsChanneled())
            {
                int duration = SpellInfo.GetDuration();

                if (duration > 0 ||
                    SpellValue.Duration.HasValue)
                {
                    if (!SpellValue.Duration.HasValue)
                    {
                        // First mod_duration then haste - see Missile Barrage
                        // Apply duration mod
                        Player modOwner = _caster.GetSpellModOwner();

                        modOwner?.ApplySpellMod(SpellInfo, SpellModOp.Duration, ref duration);

                        duration = (int)(duration * SpellValue.DurationMul);

                        // Apply haste mods
                        _caster.ModSpellDurationTime(SpellInfo, ref duration, this);
                    }
                    else
                    {
                        duration = SpellValue.Duration.Value;
                    }

                    _channeledDuration = duration;
                    SendChannelStart((uint)duration);
                }
                else if (duration == -1)
                {
                    SendChannelStart(unchecked((uint)duration));
                }

                if (duration != 0)
                {
                    _spellState = SpellState.Casting;
                    // GameObjects shouldn't cast channeled spells
                    _caster.ToUnit()?.AddInterruptMask(SpellInfo.ChannelInterruptFlags, SpellInfo.ChannelInterruptFlags2);
                }
            }

            PrepareTargetProcessing();

            // process immediate effects (items, ground, etc.) also initialize some variables
            _handle_immediate_phase();

            // consider spell hit for some spells without Target, so they may proc on finish phase correctly
            if (UniqueTargetInfo.Empty())
                HitMask = ProcFlagsHit.Normal;
            else
                DoProcessTargetContainer(UniqueTargetInfo);

            DoProcessTargetContainer(_uniqueGOTargetInfo);

            DoProcessTargetContainer(_uniqueCorpseTargetInfo);

            FinishTargetProcessing();

            // spell is finished, perform some last features of the spell here
            _handle_finish_phase();

            // Remove used for cast Item if need (it can be already NULL after TakeReagents call
            TakeCastItem();

            if (_spellState != SpellState.Casting)
                Finish(true); // successfully finish spell cast (not last in case autorepeat or channel spell)
        }

        private void _handle_immediate_phase()
        {
            // handle some immediate features of the spell here
            HandleThreatSpells();

            // handle effects with SPELL_EFFECT_HANDLE_HIT mode
            foreach (var spellEffectInfo in SpellInfo.GetEffects())
            {
                // don't do anything for empty effect
                if (!spellEffectInfo.IsEffect())
                    continue;

                // call effect handlers to handle destination hit
                HandleEffects(null, null, null, null, spellEffectInfo, SpellEffectHandleMode.Hit);
            }

            // process items
            DoProcessTargetContainer(_uniqueItemInfo);
        }

        private void _handle_finish_phase()
        {
            Unit unitCaster = _caster.ToUnit();

            if (unitCaster != null)
            {
                // Take for real after all targets are processed
                if (NeedComboPoints)
                    unitCaster.ClearComboPoints();

                // Real add combo points from effects
                if (ComboPointGain != 0)
                    unitCaster.AddComboPoints(ComboPointGain);

                if (SpellInfo.HasEffect(SpellEffectName.AddExtraAttacks))
                    unitCaster.SetLastExtraAttackSpell(SpellInfo.Id);
            }

            // Handle procs on finish
            if (!_originalCaster)
                return;

            ProcFlagsInit procAttacker = ProcAttacker;

            if (!procAttacker)
            {
                if (SpellInfo.HasAttribute(SpellAttr3.TreatAsPeriodic))
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulPeriodic);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulPeriodic);
                }
                else if (SpellInfo.HasAttribute(SpellAttr0.IsAbility))
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulAbility);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulAbility);
                }
                else
                {
                    if (IsPositive())
                        procAttacker.Or(ProcFlags.DealHelpfulSpell);
                    else
                        procAttacker.Or(ProcFlags.DealHarmfulSpell);
                }
            }

            if (!SpellInfo.HasAttribute(SpellAttr3.SuppressCasterProcs))
                Unit.ProcSkillsAndAuras(_originalCaster, null, procAttacker, new ProcFlagsInit(ProcFlags.None), ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.Finish, HitMask, this, null, null);
        }

        private void SendSpellCooldown()
        {
            if (!_caster.IsUnit())
                return;

            if (CastItem)
                _caster.ToUnit().GetSpellHistory().HandleCooldowns(SpellInfo, CastItem, this);
            else
                _caster.ToUnit().GetSpellHistory().HandleCooldowns(SpellInfo, CastItemEntry, this);

            if (IsAutoRepeat())
                _caster.ToUnit().ResetAttackTimer(WeaponAttackType.RangedAttack);
        }

        private static void FillSpellCastFailedArgs<T>(T packet, ObjectGuid castId, SpellInfo spellInfo, SpellCastResult result, SpellCustomErrors customError, int? param1, int? param2, Player caster) where T : CastFailedBase
        {
            packet.CastID = castId;
            packet.SpellID = (int)spellInfo.Id;
            packet.Reason = result;

            switch (result)
            {
                case SpellCastResult.NotReady:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = 0; // unknown (value 1 update cooldowns on client flag)

                    break;
                case SpellCastResult.RequiresSpellFocus:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = (int)spellInfo.RequiresSpellFocus; // SpellFocusObject.dbc Id

                    break;
                case SpellCastResult.RequiresArea: // AreaTable.dbc Id
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        // hardcode areas limitation case
                        switch (spellInfo.Id)
                        {
                            case 41617: // Cenarion Mana Salve
                            case 41619: // Cenarion Healing Salve
                                packet.FailedArg1 = 3905;

                                break;
                            case 41618: // Bottled Nethergon Energy
                            case 41620: // Bottled Nethergon Vapor
                                packet.FailedArg1 = 3842;

                                break;
                            case 45373: // Bloodberry Elixir
                                packet.FailedArg1 = 4075;

                                break;
                            default: // default case (don't must be)
                                packet.FailedArg1 = 0;

                                break;
                        }

                    break;
                case SpellCastResult.Totems:
                    if (param1.HasValue)
                    {
                        packet.FailedArg1 = (int)param1;

                        if (param2.HasValue)
                            packet.FailedArg2 = (int)param2;
                    }
                    else
                    {
                        if (spellInfo.Totem[0] != 0)
                            packet.FailedArg1 = (int)spellInfo.Totem[0];

                        if (spellInfo.Totem[1] != 0)
                            packet.FailedArg2 = (int)spellInfo.Totem[1];
                    }

                    break;
                case SpellCastResult.TotemCategory:
                    if (param1.HasValue)
                    {
                        packet.FailedArg1 = (int)param1;

                        if (param2.HasValue)
                            packet.FailedArg2 = (int)param2;
                    }
                    else
                    {
                        if (spellInfo.TotemCategory[0] != 0)
                            packet.FailedArg1 = (int)spellInfo.TotemCategory[0];

                        if (spellInfo.TotemCategory[1] != 0)
                            packet.FailedArg2 = (int)spellInfo.TotemCategory[1];
                    }

                    break;
                case SpellCastResult.EquippedItemClass:
                case SpellCastResult.EquippedItemClassMainhand:
                case SpellCastResult.EquippedItemClassOffhand:
                    if (param1.HasValue &&
                        param2.HasValue)
                    {
                        packet.FailedArg1 = (int)param1;
                        packet.FailedArg2 = (int)param2;
                    }
                    else
                    {
                        packet.FailedArg1 = (int)spellInfo.EquippedItemClass;
                        packet.FailedArg2 = spellInfo.EquippedItemSubClassMask;
                    }

                    break;
                case SpellCastResult.TooManyOfItem:
                    {
                        if (param1.HasValue)
                        {
                            packet.FailedArg1 = (int)param1;
                        }
                        else
                        {
                            uint item = 0;

                            foreach (var spellEffectInfo in spellInfo.GetEffects())
                                if (spellEffectInfo.ItemType != 0)
                                    item = spellEffectInfo.ItemType;

                            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(item);

                            if (proto != null &&
                                proto.GetItemLimitCategory() != 0)
                                packet.FailedArg1 = (int)proto.GetItemLimitCategory();
                        }

                        break;
                    }
                case SpellCastResult.PreventedByMechanic:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = (int)spellInfo.GetAllEffectsMechanicMask(); // SpellMechanic.dbc Id

                    break;
                case SpellCastResult.NeedExoticAmmo:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = spellInfo.EquippedItemSubClassMask; // seems correct...

                    break;
                case SpellCastResult.NeedMoreItems:
                    if (param1.HasValue &&
                        param2.HasValue)
                    {
                        packet.FailedArg1 = (int)param1;
                        packet.FailedArg2 = (int)param2;
                    }
                    else
                    {
                        packet.FailedArg1 = 0; // Item Id
                        packet.FailedArg2 = 0; // Item Count?
                    }

                    break;
                case SpellCastResult.MinSkill:
                    if (param1.HasValue &&
                        param2.HasValue)
                    {
                        packet.FailedArg1 = (int)param1;
                        packet.FailedArg2 = (int)param2;
                    }
                    else
                    {
                        packet.FailedArg1 = 0; // SkillLine.dbc Id
                        packet.FailedArg2 = 0; // required skill value
                    }

                    break;
                case SpellCastResult.FishingTooLow:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = 0; // required fishing skill

                    break;
                case SpellCastResult.CustomError:
                    packet.FailedArg1 = (int)customError;

                    break;
                case SpellCastResult.Silenced:
                    if (param1.HasValue)
                        packet.FailedArg1 = (int)param1;
                    else
                        packet.FailedArg1 = 0; // Unknown

                    break;
                case SpellCastResult.Reagents:
                    {
                        if (param1.HasValue)
                            packet.FailedArg1 = (int)param1;
                        else
                            for (uint i = 0; i < SpellConst.MaxReagents; i++)
                            {
                                if (spellInfo.Reagent[i] <= 0)
                                    continue;

                                uint itemid = (uint)spellInfo.Reagent[i];
                                uint itemcount = spellInfo.ReagentCount[i];

                                if (!caster.HasItemCount(itemid, itemcount))
                                {
                                    packet.FailedArg1 = (int)itemid; // first missing Item

                                    break;
                                }
                            }

                        if (param2.HasValue)
                            packet.FailedArg2 = (int)param2;
                        else if (!param1.HasValue)
                            foreach (var reagentsCurrency in spellInfo.ReagentsCurrency)
                                if (!caster.HasCurrency(reagentsCurrency.CurrencyTypesID, reagentsCurrency.CurrencyCount))
                                {
                                    packet.FailedArg1 = -1;
                                    packet.FailedArg2 = reagentsCurrency.CurrencyTypesID;

                                    break;
                                }

                        break;
                    }
                case SpellCastResult.CantUntalent:
                    {
                        Cypher.Assert(param1.HasValue);
                        packet.FailedArg1 = (int)param1;

                        break;
                    }
                // TODO: SPELL_FAILED_NOT_STANDING
                default:
                    break;
            }
        }

        private void SendMountResult(MountResult result)
        {
            if (result == MountResult.Ok)
                return;

            if (!_caster.IsPlayer())
                return;

            Player caster = _caster.ToPlayer();

            if (caster.IsLoading()) // don't send Mount results at loading Time
                return;

            MountResultPacket packet = new();
            packet.Result = (uint)result;
            caster.SendPacket(packet);
        }

        private void SendSpellStart()
        {
            if (!IsNeedSendToClient())
                return;

            SpellCastFlags castFlags = SpellCastFlags.HasTrajectory;
            uint schoolImmunityMask = 0;
            ulong mechanicImmunityMask = 0;
            Unit unitCaster = _caster.ToUnit();

            if (unitCaster != null)
            {
                schoolImmunityMask = _timer != 0 ? unitCaster.GetSchoolImmunityMask() : 0;
                mechanicImmunityMask = _timer != 0 ? SpellInfo.GetMechanicImmunityMask(unitCaster) : 0;
            }

            if (schoolImmunityMask != 0 ||
                mechanicImmunityMask != 0)
                castFlags |= SpellCastFlags.Immunity;

            if (((IsTriggered() && !SpellInfo.IsAutoRepeatRangedSpell()) || TriggeredByAuraSpell != null) &&
                !FromClient)
                castFlags |= SpellCastFlags.Pending;

            if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) ||
                SpellInfo.HasAttribute(SpellAttr10.UsesRangedSlotCosmeticOnly) ||
                SpellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
                castFlags |= SpellCastFlags.Projectile;

            if ((_caster.IsTypeId(TypeId.Player) || (_caster.IsTypeId(TypeId.Unit) && _caster.ToCreature().IsPet())) &&
                _powerCost.Any(cost => cost.Power != PowerType.Health))
                castFlags |= SpellCastFlags.PowerLeftSelf;

            if (HasPowerTypeCost(PowerType.Runes))
                castFlags |= SpellCastFlags.NoGCD; // not needed, but Blizzard sends it

            SpellStart packet = new();
            SpellCastData castData = packet.Cast;

            if (CastItem)
                castData.CasterGUID = CastItem.GetGUID();
            else
                castData.CasterGUID = _caster.GetGUID();

            castData.CasterUnit = _caster.GetGUID();
            castData.CastID = CastId;
            castData.OriginalCastID = OriginalCastId;
            castData.SpellID = (int)SpellInfo.Id;
            castData.Visual = SpellVisual;
            castData.CastFlags = castFlags;
            castData.CastFlagsEx = CastFlagsEx;
            castData.CastTime = (uint)_casttime;

            Targets.Write(castData.Target);

            if (castFlags.HasAnyFlag(SpellCastFlags.PowerLeftSelf))
                foreach (SpellPowerCost cost in _powerCost)
                {
                    SpellPowerData powerData;
                    powerData.Type = cost.Power;
                    powerData.Cost = _caster.ToUnit().GetPower(cost.Power);
                    castData.RemainingPower.Add(powerData);
                }

            if (castFlags.HasAnyFlag(SpellCastFlags.RuneList)) // rune cooldowns list
            {
                castData.RemainingRunes = new RuneData();

                RuneData runeData = castData.RemainingRunes;
                //TODO: There is a crash caused by a spell with CAST_FLAG_RUNE_LIST casted by a creature
                //The creature is the mover of a player, so HandleCastSpellOpcode uses it as the caster

                Player player = _caster.ToPlayer();

                if (player)
                {
                    runeData.Start = _runesState;            // runes State before
                    runeData.Count = player.GetRunesState(); // runes State after

                    for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                    {
                        // float casts ensure the division is performed on floats as we need float result
                        float baseCd = player.GetRuneBaseCooldown();
                        runeData.Cooldowns.Add((byte)((baseCd - player.GetRuneCooldown(i)) / baseCd * 255)); // rune cooldown passed
                    }
                }
                else
                {
                    runeData.Start = 0;
                    runeData.Count = 0;

                    for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                        runeData.Cooldowns.Add(0);
                }
            }

            UpdateSpellCastDataAmmo(castData.Ammo);

            if (castFlags.HasAnyFlag(SpellCastFlags.Immunity))
            {
                castData.Immunities.School = schoolImmunityMask;
                castData.Immunities.Value = (uint)mechanicImmunityMask;
            }

            /** @todo implement heal prediction packet _data
			if (castFlags & CAST_FLAG_HEAL_PREDICTION)
			{
			    castData.Predict.BeconGUID = ??
			    castData.Predict.Points = 0;
			    castData.Predict.Type = 0;
			}**/

            _caster.SendMessageToSet(packet, true);
        }

        private void SendSpellGo()
        {
            // not send invisible spell casting
            if (!IsNeedSendToClient())
                return;

            Log.outDebug(LogFilter.Spells, "Sending SMSG_SPELL_GO Id={0}", SpellInfo.Id);

            SpellCastFlags castFlags = SpellCastFlags.Unk9;

            // triggered spells with spell visual != 0
            if (((IsTriggered() && !SpellInfo.IsAutoRepeatRangedSpell()) || TriggeredByAuraSpell != null) &&
                !FromClient)
                castFlags |= SpellCastFlags.Pending;

            if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) ||
                SpellInfo.HasAttribute(SpellAttr10.UsesRangedSlotCosmeticOnly) ||
                SpellInfo.HasAttribute(SpellCustomAttributes.NeedsAmmoData))
                castFlags |= SpellCastFlags.Projectile; // arrows/bullets visual

            if ((_caster.IsTypeId(TypeId.Player) || (_caster.IsTypeId(TypeId.Unit) && _caster.ToCreature().IsPet())) &&
                _powerCost.Any(cost => cost.Power != PowerType.Health))
                castFlags |= SpellCastFlags.PowerLeftSelf;

            if (_caster.IsTypeId(TypeId.Player) &&
                _caster.ToPlayer().GetClass() == Class.Deathknight &&
                HasPowerTypeCost(PowerType.Runes) &&
                !_triggeredCastFlags.HasAnyFlag(TriggerCastFlags.IgnorePowerAndReagentCost))
            {
                castFlags |= SpellCastFlags.NoGCD;    // same as in SMSG_SPELL_START
                castFlags |= SpellCastFlags.RuneList; // rune cooldowns list
            }

            if (Targets.HasTraj())
                castFlags |= SpellCastFlags.AdjustMissile;

            if (SpellInfo.StartRecoveryTime == 0)
                castFlags |= SpellCastFlags.NoGCD;

            SpellGo packet = new();
            SpellCastData castData = packet.Cast;

            if (CastItem != null)
                castData.CasterGUID = CastItem.GetGUID();
            else
                castData.CasterGUID = _caster.GetGUID();

            castData.CasterUnit = _caster.GetGUID();
            castData.CastID = CastId;
            castData.OriginalCastID = OriginalCastId;
            castData.SpellID = (int)SpellInfo.Id;
            castData.Visual = SpellVisual;
            castData.CastFlags = castFlags;
            castData.CastFlagsEx = CastFlagsEx;
            castData.CastTime = Time.GetMSTime();

            castData.HitTargets = new List<ObjectGuid>();
            UpdateSpellCastDataTargets(castData);

            Targets.Write(castData.Target);

            if (Convert.ToBoolean(castFlags & SpellCastFlags.PowerLeftSelf))
            {
                castData.RemainingPower = new List<SpellPowerData>();

                foreach (SpellPowerCost cost in _powerCost)
                {
                    SpellPowerData powerData;
                    powerData.Type = cost.Power;
                    powerData.Cost = _caster.ToUnit().GetPower(cost.Power);
                    castData.RemainingPower.Add(powerData);
                }
            }

            if (Convert.ToBoolean(castFlags & SpellCastFlags.RuneList)) // rune cooldowns list
            {
                castData.RemainingRunes = new RuneData();
                RuneData runeData = castData.RemainingRunes;

                Player player = _caster.ToPlayer();
                runeData.Start = _runesState;            // runes State before
                runeData.Count = player.GetRunesState(); // runes State after

                for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                {
                    // float casts ensure the division is performed on floats as we need float result
                    float baseCd = (float)player.GetRuneBaseCooldown();
                    runeData.Cooldowns.Add((byte)((baseCd - (float)player.GetRuneCooldown(i)) / baseCd * 255)); // rune cooldown passed
                }
            }

            if (castFlags.HasFlag(SpellCastFlags.AdjustMissile))
            {
                castData.MissileTrajectory.TravelTime = (uint)_delayMoment;
                castData.MissileTrajectory.Pitch = Targets.GetPitch();
            }

            packet.LogData.Initialize(this);

            _caster.SendCombatLogMessage(packet);
        }

        // Writes miss and hit targets for a SMSG_SPELL_GO packet
        private void UpdateSpellCastDataTargets(SpellCastData data)
        {
            // This function also fill _data for channeled spells:
            // _needAliveTargetMask req for stop channelig if one Target die
            foreach (var targetInfo in UniqueTargetInfo)
            {
                if (targetInfo.EffectMask == 0) // No effect apply - all immuned add State
                                                // possibly SPELL_MISS_IMMUNE2 for this??
                    targetInfo.MissCondition = SpellMissInfo.Immune2;

                if (targetInfo.MissCondition == SpellMissInfo.None ||
                    (targetInfo.MissCondition == SpellMissInfo.Block && !SpellInfo.HasAttribute(SpellAttr3.CompletelyBlocked))) // Add only hits and partial Blocked
                {
                    data.HitTargets.Add(targetInfo.TargetGUID);
                    data.HitStatus.Add(new SpellHitStatus(SpellMissInfo.None));

                    _channelTargetEffectMask |= targetInfo.EffectMask;
                }
                else // misses
                {
                    data.MissTargets.Add(targetInfo.TargetGUID);

                    data.MissStatus.Add(new SpellMissStatus(targetInfo.MissCondition, targetInfo.ReflectResult));
                }
            }

            foreach (GOTargetInfo targetInfo in _uniqueGOTargetInfo)
                data.HitTargets.Add(targetInfo.TargetGUID); // Always hits

            foreach (CorpseTargetInfo targetInfo in _uniqueCorpseTargetInfo)
                data.HitTargets.Add(targetInfo.TargetGUID); // Always hits

            // Reset _needAliveTargetMask for non channeled spell
            if (!SpellInfo.IsChanneled())
                _channelTargetEffectMask = 0;
        }

        private void UpdateSpellCastDataAmmo(SpellAmmo ammo)
        {
            InventoryType ammoInventoryType = 0;
            uint ammoDisplayID = 0;

            Player playerCaster = _caster.ToPlayer();

            if (playerCaster != null)
            {
                Item pItem = playerCaster.GetWeaponForAttack(WeaponAttackType.RangedAttack);

                if (pItem)
                {
                    ammoInventoryType = pItem.GetTemplate().GetInventoryType();

                    if (ammoInventoryType == InventoryType.Thrown)
                    {
                        ammoDisplayID = pItem.GetDisplayId(playerCaster);
                    }
                    else if (playerCaster.HasAura(46699)) // Requires No Ammo
                    {
                        ammoDisplayID = 5996; // normal arrow
                        ammoInventoryType = InventoryType.Ammo;
                    }
                }
            }
            else
            {
                Unit unitCaster = _caster.ToUnit();

                if (unitCaster != null)
                {
                    uint nonRangedAmmoDisplayID = 0;
                    InventoryType nonRangedAmmoInventoryType = 0;

                    for (byte i = (int)WeaponAttackType.BaseAttack; i < (int)WeaponAttackType.Max; ++i)
                    {
                        uint itemId = unitCaster.GetVirtualItemId(i);

                        if (itemId != 0)
                        {
                            ItemRecord itemEntry = CliDB.ItemStorage.LookupByKey(itemId);

                            if (itemEntry != null)
                                if (itemEntry.ClassID == ItemClass.Weapon)
                                {
                                    switch ((ItemSubClassWeapon)itemEntry.SubclassID)
                                    {
                                        case ItemSubClassWeapon.Thrown:
                                            ammoDisplayID = Global.DB2Mgr.GetItemDisplayId(itemId, unitCaster.GetVirtualItemAppearanceMod(i));
                                            ammoInventoryType = (InventoryType)itemEntry.inventoryType;

                                            break;
                                        case ItemSubClassWeapon.Bow:
                                        case ItemSubClassWeapon.Crossbow:
                                            ammoDisplayID = 5996; // is this need fixing?
                                            ammoInventoryType = InventoryType.Ammo;

                                            break;
                                        case ItemSubClassWeapon.Gun:
                                            ammoDisplayID = 5998; // is this need fixing?
                                            ammoInventoryType = InventoryType.Ammo;

                                            break;
                                        default:
                                            nonRangedAmmoDisplayID = Global.DB2Mgr.GetItemDisplayId(itemId, unitCaster.GetVirtualItemAppearanceMod(i));
                                            nonRangedAmmoInventoryType = itemEntry.inventoryType;

                                            break;
                                    }

                                    if (ammoDisplayID != 0)
                                        break;
                                }
                        }
                    }

                    if (ammoDisplayID == 0 &&
                        ammoInventoryType == 0)
                    {
                        ammoDisplayID = nonRangedAmmoDisplayID;
                        ammoInventoryType = nonRangedAmmoInventoryType;
                    }
                }
            }

            ammo.DisplayID = (int)ammoDisplayID;
            ammo.InventoryType = (sbyte)ammoInventoryType;
        }

        private void SendSpellExecuteLog()
        {
            if (_executeLogEffects.Empty())
                return;

            SpellExecuteLog spellExecuteLog = new();

            spellExecuteLog.Caster = _caster.GetGUID();
            spellExecuteLog.SpellID = SpellInfo.Id;
            spellExecuteLog.Effects = _executeLogEffects.Values.ToList();
            spellExecuteLog.LogData.Initialize(this);

            _caster.SendCombatLogMessage(spellExecuteLog);
        }

        private void ExecuteLogEffectTakeTargetPower(SpellEffectName effect, Unit target, PowerType powerType, uint points, float amplitude)
        {
            SpellLogEffectPowerDrainParams spellLogEffectPowerDrainParams;

            spellLogEffectPowerDrainParams.Victim = target.GetGUID();
            spellLogEffectPowerDrainParams.Points = points;
            spellLogEffectPowerDrainParams.PowerType = (uint)powerType;
            spellLogEffectPowerDrainParams.Amplitude = amplitude;

            GetExecuteLogEffect(effect).PowerDrainTargets.Add(spellLogEffectPowerDrainParams);
        }

        private void ExecuteLogEffectExtraAttacks(SpellEffectName effect, Unit victim, uint numAttacks)
        {
            SpellLogEffectExtraAttacksParams spellLogEffectExtraAttacksParams;
            spellLogEffectExtraAttacksParams.Victim = victim.GetGUID();
            spellLogEffectExtraAttacksParams.NumAttacks = numAttacks;

            GetExecuteLogEffect(effect).ExtraAttacksTargets.Add(spellLogEffectExtraAttacksParams);
        }

        private void SendSpellInterruptLog(Unit victim, uint spellId)
        {
            SpellInterruptLog data = new();
            data.Caster = _caster.GetGUID();
            data.Victim = victim.GetGUID();
            data.InterruptedSpellID = SpellInfo.Id;
            data.SpellID = spellId;

            _caster.SendMessageToSet(data, true);
        }

        private void ExecuteLogEffectDurabilityDamage(SpellEffectName effect, Unit victim, int itemId, int amount)
        {
            SpellLogEffectDurabilityDamageParams spellLogEffectDurabilityDamageParams;
            spellLogEffectDurabilityDamageParams.Victim = victim.GetGUID();
            spellLogEffectDurabilityDamageParams.ItemID = itemId;
            spellLogEffectDurabilityDamageParams.Amount = amount;

            GetExecuteLogEffect(effect).DurabilityDamageTargets.Add(spellLogEffectDurabilityDamageParams);
        }

        private void ExecuteLogEffectOpenLock(SpellEffectName effect, WorldObject obj)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

            GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
        }

        private void ExecuteLogEffectCreateItem(SpellEffectName effect, uint entry)
        {
            SpellLogEffectTradeSkillItemParams spellLogEffectTradeSkillItemParams;
            spellLogEffectTradeSkillItemParams.ItemID = (int)entry;

            GetExecuteLogEffect(effect).TradeSkillTargets.Add(spellLogEffectTradeSkillItemParams);
        }

        private void ExecuteLogEffectDestroyItem(SpellEffectName effect, uint entry)
        {
            SpellLogEffectFeedPetParams spellLogEffectFeedPetParams;
            spellLogEffectFeedPetParams.ItemID = (int)entry;

            GetExecuteLogEffect(effect).FeedPetTargets.Add(spellLogEffectFeedPetParams);
        }

        private void ExecuteLogEffectSummonObject(SpellEffectName effect, WorldObject obj)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

            GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
        }

        private void ExecuteLogEffectUnsummonObject(SpellEffectName effect, WorldObject obj)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = obj.GetGUID();

            GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
        }

        private void ExecuteLogEffectResurrect(SpellEffectName effect, Unit target)
        {
            SpellLogEffectGenericVictimParams spellLogEffectGenericVictimParams;
            spellLogEffectGenericVictimParams.Victim = target.GetGUID();

            GetExecuteLogEffect(effect).GenericVictimTargets.Add(spellLogEffectGenericVictimParams);
        }

        private void SendInterrupted(byte result)
        {
            SpellFailure failurePacket = new();
            failurePacket.CasterUnit = _caster.GetGUID();
            failurePacket.CastID = CastId;
            failurePacket.SpellID = SpellInfo.Id;
            failurePacket.Visual = SpellVisual;
            failurePacket.Reason = result;
            _caster.SendMessageToSet(failurePacket, true);

            SpellFailedOther failedPacket = new();
            failedPacket.CasterUnit = _caster.GetGUID();
            failedPacket.CastID = CastId;
            failedPacket.SpellID = SpellInfo.Id;
            failedPacket.Visual = SpellVisual;
            failedPacket.Reason = result;
            _caster.SendMessageToSet(failedPacket, true);
        }

        private void SendChannelStart(uint duration)
        {
            // GameObjects don't channel
            Unit unitCaster = _caster.ToUnit();

            if (unitCaster == null)
                return;

            SpellChannelStart spellChannelStart = new();
            spellChannelStart.CasterGUID = unitCaster.GetGUID();
            spellChannelStart.SpellID = (int)SpellInfo.Id;
            spellChannelStart.Visual = SpellVisual;
            spellChannelStart.ChannelDuration = duration;

            uint schoolImmunityMask = unitCaster.GetSchoolImmunityMask();
            ulong mechanicImmunityMask = unitCaster.GetMechanicImmunityMask();

            if (schoolImmunityMask != 0 ||
                mechanicImmunityMask != 0)
            {
                SpellChannelStartInterruptImmunities interruptImmunities = new();
                interruptImmunities.SchoolImmunities = (int)schoolImmunityMask;
                interruptImmunities.Immunities = (int)mechanicImmunityMask;

                spellChannelStart.InterruptImmunities = interruptImmunities;
            }

            unitCaster.SendMessageToSet(spellChannelStart, true);

            _timer = (int)duration;

            if (!Targets.HasDst())
            {
                uint channelAuraMask = 0;
                uint explicitTargetEffectMask = 0xFFFFFFFF;

                // if there is an explicit Target, only add channel objects from effects that also hit ut
                if (!Targets.GetUnitTargetGUID().IsEmpty())
                {
                    var explicitTarget = UniqueTargetInfo.Find(target => target.TargetGUID == Targets.GetUnitTargetGUID());

                    if (explicitTarget != null)
                        explicitTargetEffectMask = explicitTarget.EffectMask;
                }

                foreach (var spellEffectInfo in SpellInfo.GetEffects())
                    if (spellEffectInfo.Effect == SpellEffectName.ApplyAura &&
                        (explicitTargetEffectMask & (1u << (int)spellEffectInfo.EffectIndex)) != 0)
                        channelAuraMask |= 1u << (int)spellEffectInfo.EffectIndex;

                foreach (TargetInfo target in UniqueTargetInfo)
                {
                    if ((target.EffectMask & channelAuraMask) == 0)
                        continue;

                    SpellAttr1 requiredAttribute = target.TargetGUID != unitCaster.GetGUID() ? SpellAttr1.IsChannelled : SpellAttr1.IsSelfChannelled;

                    if (!SpellInfo.HasAttribute(requiredAttribute))
                        continue;

                    unitCaster.AddChannelObject(target.TargetGUID);
                }

                foreach (GOTargetInfo target in _uniqueGOTargetInfo)
                    if ((target.EffectMask & channelAuraMask) != 0)
                        unitCaster.AddChannelObject(target.TargetGUID);
            }
            else if (SpellInfo.HasAttribute(SpellAttr1.IsSelfChannelled))
            {
                unitCaster.AddChannelObject(unitCaster.GetGUID());
            }

            Creature creatureCaster = unitCaster.ToCreature();

            if (creatureCaster != null)
                if (unitCaster.UnitData.ChannelObjects.Size() == 1 &&
                    unitCaster.UnitData.ChannelObjects[0].IsUnit())
                    if (!creatureCaster.HasSpellFocus(this))
                        creatureCaster.SetSpellFocus(this, Global.ObjAccessor.GetWorldObject(creatureCaster, unitCaster.UnitData.ChannelObjects[0]));

            unitCaster.SetChannelSpellId(SpellInfo.Id);
            unitCaster.SetChannelVisual(SpellVisual);
        }

        private void SendResurrectRequest(Player target)
        {
            // get resurrector Name for creature resurrections, otherwise packet will be not accepted
            // for player resurrections the Name is looked up by Guid
            string sentName = "";

            if (!_caster.IsPlayer())
                sentName = _caster.GetName(target.GetSession().GetSessionDbLocaleIndex());

            ResurrectRequest resurrectRequest = new();
            resurrectRequest.ResurrectOffererGUID = _caster.GetGUID();
            resurrectRequest.ResurrectOffererVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            resurrectRequest.Name = sentName;
            resurrectRequest.Sickness = _caster.IsUnit() && !_caster.IsTypeId(TypeId.Player); // "you'll be afflicted with resurrection sickness"
            resurrectRequest.UseTimer = !SpellInfo.HasAttribute(SpellAttr3.NoResTimer);

            Pet pet = target.GetPet();

            if (pet)
            {
                CharmInfo charmInfo = pet.GetCharmInfo();

                if (charmInfo != null)
                    resurrectRequest.PetNumber = charmInfo.GetPetNumber();
            }

            resurrectRequest.SpellID = SpellInfo.Id;

            target.SendPacket(resurrectRequest);
        }

        private void TakeCastItem()
        {
            if (CastItem == null ||
                !_caster.IsTypeId(TypeId.Player))
                return;

            // not remove cast Item at triggered spell (equipping, weapon Damage, etc)
            if (Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreCastItem))
                return;

            ItemTemplate proto = CastItem.GetTemplate();

            if (proto == null)
            {
                // This code is to avoid a crash
                // I'm not sure, if this is really an error, but I guess every Item needs a prototype
                Log.outError(LogFilter.Spells, "Cast Item has no Item prototype {0}", CastItem.GetGUID().ToString());

                return;
            }

            bool expendable = false;
            bool withoutCharges = false;

            foreach (ItemEffectRecord itemEffect in CastItem.GetEffects())
            {
                if (itemEffect.LegacySlotIndex >= CastItem._itemData.SpellCharges.GetSize())
                    continue;

                // Item has limited charges
                if (itemEffect.Charges != 0)
                {
                    if (itemEffect.Charges < 0)
                        expendable = true;

                    int charges = CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

                    // Item has charges left
                    if (charges != 0)
                    {
                        if (charges > 0)
                            --charges;
                        else
                            ++charges;

                        if (proto.GetMaxStackSize() == 1)
                            CastItem.SetSpellCharges(itemEffect.LegacySlotIndex, charges);

                        CastItem.SetState(ItemUpdateState.Changed, _caster.ToPlayer());
                    }

                    // all charges used
                    withoutCharges = (charges == 0);
                }
            }

            if (expendable && withoutCharges)
            {
                uint count = 1;
                _caster.ToPlayer().DestroyItemCount(CastItem, ref count, true);

                // prevent crash at access to deleted _targets.GetItemTarget
                if (CastItem == Targets.GetItemTarget())
                    Targets.SetItemTarget(null);

                CastItem = null;
                CastItemGUID.Clear();
                CastItemEntry = 0;
            }
        }

        private void TakePower()
        {
            // GameObjects don't use power
            Unit unitCaster = _caster.ToUnit();

            if (!unitCaster)
                return;

            if (CastItem != null ||
                TriggeredByAuraSpell != null)
                return;

            //Don't take power if the spell is cast while .cheat power is enabled.
            if (unitCaster.IsTypeId(TypeId.Player))
                if (unitCaster.ToPlayer().GetCommandStatus(PlayerCommandStates.Power))
                    return;

            foreach (SpellPowerCost cost in _powerCost)
            {
                bool hit = true;

                if (unitCaster.IsTypeId(TypeId.Player))
                    if (SpellInfo.HasAttribute(SpellAttr1.DiscountPowerOnMiss))
                    {
                        ObjectGuid targetGUID = Targets.GetUnitTargetGUID();

                        if (!targetGUID.IsEmpty())
                        {
                            var ihit = UniqueTargetInfo.FirstOrDefault(targetInfo => targetInfo.TargetGUID == targetGUID && targetInfo.MissCondition != SpellMissInfo.None);

                            if (ihit != null)
                            {
                                hit = false;
                                //lower spell cost on fail (by talent aura)
                                Player modOwner = unitCaster.GetSpellModOwner();

                                modOwner?.ApplySpellMod(SpellInfo, SpellModOp.PowerCostOnMiss, ref cost.Amount);
                            }
                        }
                    }

                if (cost.Power == PowerType.Runes)
                {
                    TakeRunePower(hit);

                    continue;
                }

                if (cost.Amount == 0)
                    continue;

                // health as power used
                if (cost.Power == PowerType.Health)
                {
                    unitCaster.ModifyHealth(-cost.Amount);

                    continue;
                }

                if (cost.Power >= PowerType.Max)
                {
                    Log.outError(LogFilter.Spells, "Spell.TakePower: Unknown power Type '{0}'", cost.Power);

                    continue;
                }

                unitCaster.ModifyPower(cost.Power, -cost.Amount);
            }
        }

        private SpellCastResult CheckRuneCost()
        {
            int runeCost = _powerCost.Sum(cost => cost.Power == PowerType.Runes ? cost.Amount : 0);

            if (runeCost == 0)
                return SpellCastResult.SpellCastOk;

            Player player = _caster.ToPlayer();

            if (!player)
                return SpellCastResult.SpellCastOk;

            if (player.GetClass() != Class.Deathknight)
                return SpellCastResult.SpellCastOk;

            int readyRunes = 0;

            for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                if (player.GetRuneCooldown(i) == 0)
                    ++readyRunes;

            if (readyRunes < runeCost)
                return SpellCastResult.NoPower; // not sure if result code is correct

            return SpellCastResult.SpellCastOk;
        }

        private void TakeRunePower(bool didHit)
        {
            if (!_caster.IsTypeId(TypeId.Player) ||
                _caster.ToPlayer().GetClass() != Class.Deathknight)
                return;

            Player player = _caster.ToPlayer();
            _runesState = player.GetRunesState(); // store previous State

            int runeCost = _powerCost.Sum(cost => cost.Power == PowerType.Runes ? cost.Amount : 0);

            for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
                if (player.GetRuneCooldown(i) == 0 &&
                    runeCost > 0)
                {
                    player.SetRuneCooldown(i, didHit ? player.GetRuneBaseCooldown() : RuneCooldowns.Miss);
                    --runeCost;
                }
        }

        private void TakeReagents()
        {
            if (!_caster.IsTypeId(TypeId.Player))
                return;

            // do not take reagents for these Item casts
            if (CastItem != null &&
                CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost))
                return;

            Player p_caster = _caster.ToPlayer();

            if (p_caster.CanNoReagentCast(SpellInfo))
                return;

            for (int x = 0; x < SpellConst.MaxReagents; ++x)
            {
                if (SpellInfo.Reagent[x] <= 0)
                    continue;

                uint itemid = (uint)SpellInfo.Reagent[x];
                uint itemcount = SpellInfo.ReagentCount[x];

                // if CastItem is also spell reagent
                if (CastItem != null &&
                    CastItem.GetEntry() == itemid)
                {
                    foreach (ItemEffectRecord itemEffect in CastItem.GetEffects())
                    {
                        if (itemEffect.LegacySlotIndex >= CastItem._itemData.SpellCharges.GetSize())
                            continue;

                        // CastItem will be used up and does not Count as reagent
                        int charges = CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

                        if (itemEffect.Charges < 0 &&
                            Math.Abs(charges) < 2)
                        {
                            ++itemcount;

                            break;
                        }
                    }

                    CastItem = null;
                    CastItemGUID.Clear();
                    CastItemEntry = 0;
                }

                // if GetItemTarget is also spell reagent
                if (Targets.GetItemTargetEntry() == itemid)
                    Targets.SetItemTarget(null);

                p_caster.DestroyItemCount(itemid, itemcount, true);
            }

            foreach (var reagentsCurrency in SpellInfo.ReagentsCurrency)
                p_caster.ModifyCurrency(reagentsCurrency.CurrencyTypesID, -reagentsCurrency.CurrencyCount, false, true);
        }

        private void HandleThreatSpells()
        {
            // wild GameObject spells don't cause threat
            Unit unitCaster = (_originalCaster ? _originalCaster : _caster.ToUnit());

            if (unitCaster == null)
                return;

            if (UniqueTargetInfo.Empty())
                return;

            if (!SpellInfo.HasInitialAggro())
                return;

            float threat = 0.0f;
            SpellThreatEntry threatEntry = Global.SpellMgr.GetSpellThreatEntry(SpellInfo.Id);

            if (threatEntry != null)
            {
                if (threatEntry.ApPctMod != 0.0f)
                    threat += threatEntry.ApPctMod * unitCaster.GetTotalAttackPowerValue(WeaponAttackType.BaseAttack);

                threat += threatEntry.FlatMod;
            }
            else if (!SpellInfo.HasAttribute(SpellCustomAttributes.NoInitialThreat))
            {
                threat += SpellInfo.SpellLevel;
            }

            // past this point only multiplicative effects occur
            if (threat == 0.0f)
                return;

            // since 2.0.1 threat from positive effects also is distributed among all targets, so the overall caused threat is at most the defined bonus
            threat /= UniqueTargetInfo.Count;

            foreach (var ihit in UniqueTargetInfo)
            {
                float threatToAdd = threat;

                if (ihit.MissCondition != SpellMissInfo.None)
                    threatToAdd = 0.0f;

                Unit target = Global.ObjAccessor.GetUnit(unitCaster, ihit.TargetGUID);

                if (target == null)
                    continue;

                // positive spells distribute threat among all units that are in combat with Target, like healing
                if (IsPositive())
                {
                    target.GetThreatManager().ForwardThreatForAssistingMe(unitCaster, threatToAdd, SpellInfo);
                }
                // for negative spells threat gets distributed among affected targets
                else
                {
                    if (!target.CanHaveThreatList())
                        continue;

                    target.GetThreatManager().AddThreat(unitCaster, threatToAdd, SpellInfo, true);
                }
            }

            Log.outDebug(LogFilter.Spells, "Spell {0}, added an additional {1} threat for {2} {3} Target(s)", SpellInfo.Id, threat, IsPositive() ? "assisting" : "harming", UniqueTargetInfo.Count);
        }

        private SpellCastResult CheckCasterAuras(ref int param1)
        {
            Unit unitCaster = (_originalCaster ? _originalCaster : _caster.ToUnit());

            if (unitCaster == null)
                return SpellCastResult.SpellCastOk;

            // these attributes only show the spell as usable on the client when it has related aura applied
            // still they need to be checked against certain mechanics

            // SPELL_ATTR5_USABLE_WHILE_STUNNED by default only MECHANIC_STUN (ie no sleep, knockout, freeze, etc.)
            bool usableWhileStunned = SpellInfo.HasAttribute(SpellAttr5.AllowWhileStunned);

            // SPELL_ATTR5_USABLE_WHILE_FEARED by default only fear (ie no horror)
            bool usableWhileFeared = SpellInfo.HasAttribute(SpellAttr5.AllowWhileFleeing);

            // SPELL_ATTR5_USABLE_WHILE_CONFUSED by default only disorient (ie no polymorph)
            bool usableWhileConfused = SpellInfo.HasAttribute(SpellAttr5.AllowWhileConfused);

            // Check whether the cast should be prevented by any State you might have.
            SpellCastResult result = SpellCastResult.SpellCastOk;
            // Get unit State
            UnitFlags unitflag = (UnitFlags)(uint)unitCaster.UnitData.Flags;

            // this check should only be done when player does cast directly
            // (ie not when it's called from a script) Breaks for example PlayerAI when charmed
            /*if (!unitCaster.GetCharmerGUID().IsEmpty())
			{
			    Unit charmer = unitCaster.GetCharmer();
			    if (charmer)
			        if (charmer.GetUnitBeingMoved() != unitCaster && !CheckSpellCancelsCharm(ref param1))
			            result = SpellCastResult.Charmed;
			}*/

            // spell has attribute usable while having a cc State, check if caster has allowed mechanic Auras, another mechanic types must prevent cast spell
            SpellCastResult mechanicCheck(AuraType auraType, ref int _param1)
            {
                bool foundNotMechanic = false;
                var auras = unitCaster.GetAuraEffectsByType(auraType);

                foreach (AuraEffect aurEff in auras)
                {
                    ulong mechanicMask = aurEff.GetSpellInfo().GetAllEffectsMechanicMask();

                    if (mechanicMask != 0 &&
                        !Convert.ToBoolean(mechanicMask & GetSpellInfo().GetAllowedMechanicMask()))
                    {
                        foundNotMechanic = true;

                        // fill up aura mechanic info to send client proper error message
                        _param1 = (int)aurEff.GetSpellEffectInfo().Mechanic;

                        if (_param1 == 0)
                            _param1 = (int)aurEff.GetSpellInfo().Mechanic;

                        break;
                    }
                }

                if (foundNotMechanic)
                    switch (auraType)
                    {
                        case AuraType.ModStun:
                        case AuraType.ModStunDisableGravity:
                            return SpellCastResult.Stunned;
                        case AuraType.ModFear:
                            return SpellCastResult.Fleeing;
                        case AuraType.ModConfuse:
                            return SpellCastResult.Confused;
                        default:
                            //ABORT();
                            return SpellCastResult.NotKnown;
                    }

                return SpellCastResult.SpellCastOk;
            }

            if (unitflag.HasAnyFlag(UnitFlags.Stunned))
            {
                if (usableWhileStunned)
                {
                    SpellCastResult mechanicResult = mechanicCheck(AuraType.ModStun, ref param1);

                    if (mechanicResult != SpellCastResult.SpellCastOk)
                        result = mechanicResult;
                }
                else if (!CheckSpellCancelsStun(ref param1))
                {
                    result = SpellCastResult.Stunned;
                }
                else if ((SpellInfo.Mechanic & Mechanics.ImmuneShield) != 0 &&
                         _caster.IsUnit() &&
                         _caster.ToUnit().HasAuraWithMechanic(1 << (int)Mechanics.Banish))
                {
                    result = SpellCastResult.Stunned;
                }
            }
            else if (unitflag.HasAnyFlag(UnitFlags.Silenced) &&
                     SpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Silence) &&
                     !CheckSpellCancelsSilence(ref param1))
            {
                result = SpellCastResult.Silenced;
            }
            else if (unitflag.HasAnyFlag(UnitFlags.Pacified) &&
                     SpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.Pacify) &&
                     !CheckSpellCancelsPacify(ref param1))
            {
                result = SpellCastResult.Pacified;
            }
            else if (unitflag.HasAnyFlag(UnitFlags.Fleeing))
            {
                if (usableWhileFeared)
                {
                    SpellCastResult mechanicResult = mechanicCheck(AuraType.ModFear, ref param1);

                    if (mechanicResult != SpellCastResult.SpellCastOk)
                    {
                        result = mechanicResult;
                    }
                    else
                    {
                        mechanicResult = mechanicCheck(AuraType.ModStunDisableGravity, ref param1);

                        if (mechanicResult != SpellCastResult.SpellCastOk)
                            result = mechanicResult;
                    }
                }
                else if (!CheckSpellCancelsFear(ref param1))
                {
                    result = SpellCastResult.Fleeing;
                }
            }
            else if (unitflag.HasAnyFlag(UnitFlags.Confused))
            {
                if (usableWhileConfused)
                {
                    SpellCastResult mechanicResult = mechanicCheck(AuraType.ModConfuse, ref param1);

                    if (mechanicResult != SpellCastResult.SpellCastOk)
                        result = mechanicResult;
                }
                else if (!CheckSpellCancelsConfuse(ref param1))
                {
                    result = SpellCastResult.Confused;
                }
            }
            else if (unitCaster.HasUnitFlag2(UnitFlags2.NoActions) &&
                     SpellInfo.PreventionType.HasAnyFlag(SpellPreventionType.NoActions) &&
                     !CheckSpellCancelsNoActions(ref param1))
            {
                result = SpellCastResult.NoActions;
            }

            // Attr must make flag drop spell totally immune from all effects
            if (result != SpellCastResult.SpellCastOk)
                return (param1 != 0) ? SpellCastResult.PreventedByMechanic : result;

            return SpellCastResult.SpellCastOk;
        }

        private bool CheckSpellCancelsAuraEffect(AuraType auraType, ref int param1)
        {
            Unit unitCaster = (_originalCaster ? _originalCaster : _caster.ToUnit());

            if (unitCaster == null)
                return false;

            // Checking Auras is needed now, because you are prevented by some State but the spell grants immunity.
            var auraEffects = unitCaster.GetAuraEffectsByType(auraType);

            if (auraEffects.Empty())
                return true;

            foreach (AuraEffect aurEff in auraEffects)
            {
                if (SpellInfo.SpellCancelsAuraEffect(aurEff))
                    continue;

                param1 = (int)aurEff.GetSpellEffectInfo().Mechanic;

                if (param1 == 0)
                    param1 = (int)aurEff.GetSpellInfo().Mechanic;

                return false;
            }

            return true;
        }

        private bool CheckSpellCancelsCharm(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModCharm, ref param1) ||
                   CheckSpellCancelsAuraEffect(AuraType.AoeCharm, ref param1) ||
                   CheckSpellCancelsAuraEffect(AuraType.ModPossess, ref param1);
        }

        private bool CheckSpellCancelsStun(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModStun, ref param1) &&
                   CheckSpellCancelsAuraEffect(AuraType.ModStunDisableGravity, ref param1);
        }

        private bool CheckSpellCancelsSilence(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModSilence, ref param1) ||
                   CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
        }

        private bool CheckSpellCancelsPacify(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModPacify, ref param1) ||
                   CheckSpellCancelsAuraEffect(AuraType.ModPacifySilence, ref param1);
        }

        private bool CheckSpellCancelsFear(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModFear, ref param1);
        }

        private bool CheckSpellCancelsConfuse(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModConfuse, ref param1);
        }

        private bool CheckSpellCancelsNoActions(ref int param1)
        {
            return CheckSpellCancelsAuraEffect(AuraType.ModNoActions, ref param1);
        }

        private SpellCastResult CheckArenaAndRatedBattlegroundCastRules()
        {
            bool isRatedBattleground = false; // NYI
            bool isArena = !isRatedBattleground;

            // check USABLE attributes
            // USABLE takes precedence over NOT_USABLE
            if (isRatedBattleground && SpellInfo.HasAttribute(SpellAttr9.UsableInRatedBattlegrounds))
                return SpellCastResult.SpellCastOk;

            if (isArena && SpellInfo.HasAttribute(SpellAttr4.IgnoreDefaultArenaRestrictions))
                return SpellCastResult.SpellCastOk;

            // check NOT_USABLE attributes
            if (SpellInfo.HasAttribute(SpellAttr4.NotInArenaOrRatedBattleground))
                return isArena ? SpellCastResult.NotInArena : SpellCastResult.NotInBattleground;

            if (isArena && SpellInfo.HasAttribute(SpellAttr9.NotUsableInArena))
                return SpellCastResult.NotInArena;

            // check cooldowns
            uint spellCooldown = SpellInfo.GetRecoveryTime();

            if (isArena && spellCooldown > 10 * Time.Minute * Time.InMilliseconds) // not sure if still needed
                return SpellCastResult.NotInArena;

            if (isRatedBattleground && spellCooldown > 15 * Time.Minute * Time.InMilliseconds)
                return SpellCastResult.NotInBattleground;

            return SpellCastResult.SpellCastOk;
        }

        private SpellCastResult CheckRange(bool strict)
        {
            // Don't check for instant cast spells
            if (!strict &&
                _casttime == 0)
                return SpellCastResult.SpellCastOk;

            (float minRange, float maxRange) = GetMinMaxRange(strict);

            // dont check max_range to strictly after cast
            if (SpellInfo.RangeEntry != null &&
                SpellInfo.RangeEntry.Flags != SpellRangeFlag.Melee &&
                !strict)
                maxRange += Math.Min(3.0f, maxRange * 0.1f); // 10% but no more than 3.0f

            // get square values for sqr distance checks
            minRange *= minRange;
            maxRange *= maxRange;

            Unit target = Targets.GetUnitTarget();

            if (target && target != _caster)
            {
                if (_caster.GetExactDistSq(target) > maxRange)
                    return SpellCastResult.OutOfRange;

                if (minRange > 0.0f &&
                    _caster.GetExactDistSq(target) < minRange)
                    return SpellCastResult.OutOfRange;

                if (_caster.IsTypeId(TypeId.Player) &&
                    ((SpellInfo.FacingCasterFlags.HasAnyFlag(1u) && !_caster.HasInArc((float)Math.PI, target)) && !_caster.ToPlayer().IsWithinBoundaryRadius(target)))
                    return SpellCastResult.UnitNotInfront;
            }

            GameObject goTarget = Targets.GetGOTarget();

            if (goTarget != null)
                if (!goTarget.IsAtInteractDistance(_caster.ToPlayer(), SpellInfo))
                    return SpellCastResult.OutOfRange;

            if (Targets.HasDst() &&
                !Targets.HasTraj())
            {
                if (_caster.GetExactDistSq(Targets.GetDstPos()) > maxRange)
                    return SpellCastResult.OutOfRange;

                if (minRange > 0.0f &&
                    _caster.GetExactDistSq(Targets.GetDstPos()) < minRange)
                    return SpellCastResult.OutOfRange;
            }

            return SpellCastResult.SpellCastOk;
        }

        private (float minRange, float maxRange) GetMinMaxRange(bool strict)
        {
            float rangeMod = 0.0f;
            float minRange = 0.0f;
            float maxRange = 0.0f;

            if (strict && SpellInfo.IsNextMeleeSwingSpell())
                return (0.0f, 100.0f);

            Unit unitCaster = _caster.ToUnit();

            if (SpellInfo.RangeEntry != null)
            {
                Unit target = Targets.GetUnitTarget();

                if (SpellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Melee))
                {
                    // when the Target is not a unit, take the caster's combat reach as the Target's combat reach.
                    if (unitCaster)
                        rangeMod = unitCaster.GetMeleeRange(target ? target : unitCaster);
                }
                else
                {
                    float meleeRange = 0.0f;

                    if (SpellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                        // when the Target is not a unit, take the caster's combat reach as the Target's combat reach.
                        if (unitCaster != null)
                            meleeRange = unitCaster.GetMeleeRange(target ? target : unitCaster);

                    minRange = _caster.GetSpellMinRangeForTarget(target, SpellInfo) + meleeRange;
                    maxRange = _caster.GetSpellMaxRangeForTarget(target, SpellInfo);

                    if (target || Targets.GetCorpseTarget())
                    {
                        rangeMod = _caster.GetCombatReach() + (target ? target.GetCombatReach() : _caster.GetCombatReach());

                        if (minRange > 0.0f &&
                            !SpellInfo.RangeEntry.Flags.HasAnyFlag(SpellRangeFlag.Ranged))
                            minRange += rangeMod;
                    }
                }

                if (target != null &&
                    unitCaster != null &&
                    unitCaster.IsMoving() &&
                    target.IsMoving() &&
                    !unitCaster.IsWalking() &&
                    !target.IsWalking() &&
                    (SpellInfo.RangeEntry.Flags.HasFlag(SpellRangeFlag.Melee) || target.IsPlayer()))
                    rangeMod += 8.0f / 3.0f;
            }

            if (SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) &&
                _caster.IsTypeId(TypeId.Player))
            {
                Item ranged = _caster.ToPlayer().GetWeaponForAttack(WeaponAttackType.RangedAttack, true);

                if (ranged)
                    maxRange *= ranged.GetTemplate().GetRangedModRange() * 0.01f;
            }

            Player modOwner = _caster.GetSpellModOwner();

            if (modOwner)
                modOwner.ApplySpellMod(SpellInfo, SpellModOp.Range, ref maxRange, this);

            maxRange += rangeMod;

            return (minRange, maxRange);
        }

        private SpellCastResult CheckPower()
        {
            Unit unitCaster = _caster.ToUnit();

            if (unitCaster == null)
                return SpellCastResult.SpellCastOk;

            // Item cast not used power
            if (CastItem != null)
                return SpellCastResult.SpellCastOk;

            foreach (SpellPowerCost cost in _powerCost)
            {
                // health as power used - need check health amount
                if (cost.Power == PowerType.Health)
                {
                    if (unitCaster.GetHealth() <= (ulong)cost.Amount)
                        return SpellCastResult.CasterAurastate;

                    continue;
                }

                // Check valid power Type
                if (cost.Power >= PowerType.Max)
                {
                    Log.outError(LogFilter.Spells, "Spell.CheckPower: Unknown power Type '{0}'", cost.Power);

                    return SpellCastResult.Unknown;
                }

                //check rune cost only if a spell has PowerType == POWER_RUNES
                if (cost.Power == PowerType.Runes)
                {
                    SpellCastResult failReason = CheckRuneCost();

                    if (failReason != SpellCastResult.SpellCastOk)
                        return failReason;
                }

                // Check power amount
                if (unitCaster.GetPower(cost.Power) < cost.Amount)
                    return SpellCastResult.NoPower;
            }

            return SpellCastResult.SpellCastOk;
        }

        private SpellCastResult CheckItems(ref int param1, ref int param2)
        {
            Player player = _caster.ToPlayer();

            if (!player)
                return SpellCastResult.SpellCastOk;

            if (CastItem == null)
            {
                if (!CastItemGUID.IsEmpty())
                    return SpellCastResult.ItemNotReady;
            }
            else
            {
                uint itemid = CastItem.GetEntry();

                if (!player.HasItemCount(itemid))
                    return SpellCastResult.ItemNotReady;

                ItemTemplate proto = CastItem.GetTemplate();

                if (proto == null)
                    return SpellCastResult.ItemNotReady;

                foreach (ItemEffectRecord itemEffect in CastItem.GetEffects())
                    if (itemEffect.LegacySlotIndex < CastItem._itemData.SpellCharges.GetSize() &&
                        itemEffect.Charges != 0)
                        if (CastItem.GetSpellCharges(itemEffect.LegacySlotIndex) == 0)
                            return SpellCastResult.NoChargesRemain;

                // consumable cast Item checks
                if (proto.GetClass() == ItemClass.Consumable &&
                    Targets.GetUnitTarget() != null)
                {
                    // such items should only fail if there is no suitable effect at all - see Rejuvenation Potions for example
                    SpellCastResult failReason = SpellCastResult.SpellCastOk;

                    foreach (var spellEffectInfo in SpellInfo.GetEffects())
                    {
                        // skip check, pet not required like checks, and for TARGET_UNIT_PET _targets.GetUnitTarget() is not the real Target but the caster
                        if (spellEffectInfo.TargetA.GetTarget() == Framework.Constants.Targets.UnitPet)
                            continue;

                        if (spellEffectInfo.Effect == SpellEffectName.Heal)
                        {
                            if (Targets.GetUnitTarget().IsFullHealth())
                            {
                                failReason = SpellCastResult.AlreadyAtFullHealth;

                                continue;
                            }
                            else
                            {
                                failReason = SpellCastResult.SpellCastOk;

                                break;
                            }
                        }

                        // Mana Potion, Rage Potion, Thistle Tea(Rogue), ...
                        if (spellEffectInfo.Effect == SpellEffectName.Energize)
                        {
                            if (spellEffectInfo.MiscValue < 0 ||
                                spellEffectInfo.MiscValue >= (int)PowerType.Max)
                            {
                                failReason = SpellCastResult.AlreadyAtFullPower;

                                continue;
                            }

                            PowerType power = (PowerType)spellEffectInfo.MiscValue;

                            if (Targets.GetUnitTarget().GetPower(power) == Targets.GetUnitTarget().GetMaxPower(power))
                            {
                                failReason = SpellCastResult.AlreadyAtFullPower;

                                continue;
                            }
                            else
                            {
                                failReason = SpellCastResult.SpellCastOk;

                                break;
                            }
                        }
                    }

                    if (failReason != SpellCastResult.SpellCastOk)
                        return failReason;
                }
            }

            // check Target Item
            if (!Targets.GetItemTargetGUID().IsEmpty())
            {
                Item item = Targets.GetItemTarget();

                if (item == null)
                    return SpellCastResult.ItemGone;

                if (!item.IsFitToSpellRequirements(SpellInfo))
                    return SpellCastResult.EquippedItemClass;
            }
            // if not Item Target then required Item must be equipped
            else
            {
                if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreEquippedItemRequirement))
                    if (_caster.IsTypeId(TypeId.Player) &&
                        !_caster.ToPlayer().HasItemFitToSpellRequirements(SpellInfo))
                        return SpellCastResult.EquippedItemClass;
            }

            // do not take reagents for these Item casts
            if (!(CastItem != null && CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost)))
            {
                bool checkReagents = !Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnorePowerAndReagentCost) && !player.CanNoReagentCast(SpellInfo);

                // Not own traded Item (in trader trade Slot) requires reagents even if triggered spell
                if (!checkReagents)
                {
                    Item targetItem = Targets.GetItemTarget();

                    if (targetItem != null)
                        if (targetItem.GetOwnerGUID() != player.GetGUID())
                            checkReagents = true;
                }

                // check reagents (ignore triggered spells with reagents processed by original spell) and special reagent ignore case.
                if (checkReagents)
                {
                    for (byte i = 0; i < SpellConst.MaxReagents; i++)
                    {
                        if (SpellInfo.Reagent[i] <= 0)
                            continue;

                        uint itemid = (uint)SpellInfo.Reagent[i];
                        uint itemcount = SpellInfo.ReagentCount[i];

                        // if CastItem is also spell reagent
                        if (CastItem != null &&
                            CastItem.GetEntry() == itemid)
                        {
                            ItemTemplate proto = CastItem.GetTemplate();

                            if (proto == null)
                                return SpellCastResult.ItemNotReady;

                            foreach (ItemEffectRecord itemEffect in CastItem.GetEffects())
                            {
                                if (itemEffect.LegacySlotIndex >= CastItem._itemData.SpellCharges.GetSize())
                                    continue;

                                // CastItem will be used up and does not Count as reagent
                                int charges = CastItem.GetSpellCharges(itemEffect.LegacySlotIndex);

                                if (itemEffect.Charges < 0 &&
                                    Math.Abs(charges) < 2)
                                {
                                    ++itemcount;

                                    break;
                                }
                            }
                        }

                        if (!player.HasItemCount(itemid, itemcount))
                        {
                            param1 = (int)itemid;

                            return SpellCastResult.Reagents;
                        }
                    }

                    foreach (var reagentsCurrency in SpellInfo.ReagentsCurrency)
                        if (!player.HasCurrency(reagentsCurrency.CurrencyTypesID, reagentsCurrency.CurrencyCount))
                        {
                            param1 = -1;
                            param2 = reagentsCurrency.CurrencyTypesID;

                            return SpellCastResult.Reagents;
                        }
                }

                // check totem-Item requirements (items presence in inventory)
                uint totems = 2;

                for (int i = 0; i < 2; ++i)
                    if (SpellInfo.Totem[i] != 0)
                    {
                        if (player.HasItemCount(SpellInfo.Totem[i]))
                        {
                            totems -= 1;

                            continue;
                        }
                    }
                    else
                    {
                        totems -= 1;
                    }

                if (totems != 0)
                    return SpellCastResult.Totems;

                // Check items for TotemCategory (items presence in inventory)
                uint totemCategory = 2;

                for (byte i = 0; i < 2; ++i)
                    if (SpellInfo.TotemCategory[i] != 0)
                    {
                        if (player.HasItemTotemCategory(SpellInfo.TotemCategory[i]))
                        {
                            totemCategory -= 1;

                            continue;
                        }
                    }
                    else
                    {
                        totemCategory -= 1;
                    }

                if (totemCategory != 0)
                    return SpellCastResult.TotemCategory;
            }

            // special checks for spell effects
            foreach (var spellEffectInfo in SpellInfo.GetEffects())
                switch (spellEffectInfo.Effect)
                {
                    case SpellEffectName.CreateItem:
                    case SpellEffectName.CreateLoot:
                        {
                            // _targets.GetUnitTarget() means explicit cast, otherwise we dont check for possible equip error
                            Unit target = Targets.GetUnitTarget() ?? player;

                            if (target.IsPlayer() &&
                                !IsTriggered())
                            {
                                // SPELL_EFFECT_CREATE_ITEM_2 differs from SPELL_EFFECT_CREATE_ITEM in that it picks the random Item to create from a pool of potential items,
                                // so we need to make sure there is at least one free space in the player's inventory
                                if (spellEffectInfo.Effect == SpellEffectName.CreateLoot)
                                    if (target.ToPlayer().GetFreeInventorySpace() == 0)
                                    {
                                        player.SendEquipError(InventoryResult.InvFull, null, null, spellEffectInfo.ItemType);

                                        return SpellCastResult.DontReport;
                                    }

                                if (spellEffectInfo.ItemType != 0)
                                {
                                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(spellEffectInfo.ItemType);

                                    if (itemTemplate == null)
                                        return SpellCastResult.ItemNotFound;

                                    uint createCount = (uint)Math.Clamp(spellEffectInfo.CalcValue(), 1u, itemTemplate.GetMaxStackSize());

                                    List<ItemPosCount> dest = new();
                                    InventoryResult msg = target.ToPlayer().CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, spellEffectInfo.ItemType, createCount);

                                    if (msg != InventoryResult.Ok)
                                    {
                                        /// @todo Needs review
                                        if (itemTemplate.GetItemLimitCategory() == 0)
                                        {
                                            player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

                                            return SpellCastResult.DontReport;
                                        }
                                        else
                                        {
                                            // Conjure Food/Water/Refreshment spells
                                            if (SpellInfo.SpellFamilyName != SpellFamilyNames.Mage ||
                                                (!SpellInfo.SpellFamilyFlags[0].HasAnyFlag(0x40000000u)))
                                            {
                                                return SpellCastResult.TooManyOfItem;
                                            }
                                            else if (!target.ToPlayer().HasItemCount(spellEffectInfo.ItemType))
                                            {
                                                player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

                                                return SpellCastResult.DontReport;
                                            }
                                            else if (SpellInfo.GetEffects().Count > 1)
                                            {
                                                player.CastSpell(player,
                                                                 (uint)SpellInfo.GetEffect(1).CalcValue(),
                                                                 new CastSpellExtraArgs()
                                                                     .SetTriggeringSpell(this)); // move this to anywhere
                                            }

                                            return SpellCastResult.DontReport;
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    case SpellEffectName.EnchantItem:
                        if (spellEffectInfo.ItemType != 0 &&
                            Targets.GetItemTarget() != null &&
                            Targets.GetItemTarget().IsVellum())
                        {
                            // cannot enchant vellum for other player
                            if (Targets.GetItemTarget().GetOwner() != player)
                                return SpellCastResult.NotTradeable;

                            // do not allow to enchant vellum from scroll made by vellum-prevent exploit
                            if (CastItem != null &&
                                CastItem.GetTemplate().HasFlag(ItemFlags.NoReagentCost))
                                return SpellCastResult.TotemCategory;

                            List<ItemPosCount> dest = new();
                            InventoryResult msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, spellEffectInfo.ItemType, 1);

                            if (msg != InventoryResult.Ok)
                            {
                                player.SendEquipError(msg, null, null, spellEffectInfo.ItemType);

                                return SpellCastResult.DontReport;
                            }
                        }

                        goto case SpellEffectName.EnchantItemPrismatic;
                    case SpellEffectName.EnchantItemPrismatic:
                        {
                            Item targetItem = Targets.GetItemTarget();

                            if (targetItem == null)
                                return SpellCastResult.ItemNotFound;

                            // required level has to be checked also! Exploit fix
                            if (targetItem.GetItemLevel(targetItem.GetOwner()) < SpellInfo.BaseLevel ||
                                (targetItem.GetRequiredLevel() != 0 && targetItem.GetRequiredLevel() < SpellInfo.BaseLevel))
                                return SpellCastResult.Lowlevel;

                            bool isItemUsable = false;

                            foreach (ItemEffectRecord itemEffect in targetItem.GetEffects())
                                if (itemEffect.SpellID != 0 &&
                                    itemEffect.TriggerType == ItemSpelltriggerType.OnUse)
                                {
                                    isItemUsable = true;

                                    break;
                                }

                            var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(spellEffectInfo.MiscValue);

                            // do not allow adding usable enchantments to items that have use effect already
                            if (enchantEntry != null)
                                for (var s = 0; s < ItemConst.MaxItemEnchantmentEffects; ++s)
                                    switch (enchantEntry.Effect[s])
                                    {
                                        case ItemEnchantmentType.UseSpell:
                                            if (isItemUsable)
                                                return SpellCastResult.OnUseEnchant;

                                            break;
                                        case ItemEnchantmentType.PrismaticSocket:
                                            {
                                                uint numSockets = 0;

                                                for (uint socket = 0; socket < ItemConst.MaxGemSockets; ++socket)
                                                    if (targetItem.GetSocketColor(socket) != 0)
                                                        ++numSockets;

                                                if (numSockets == ItemConst.MaxGemSockets ||
                                                    targetItem.GetEnchantmentId(EnchantmentSlot.Prismatic) != 0)
                                                    return SpellCastResult.MaxSockets;

                                                break;
                                            }
                                    }

                            // Not allow enchant in trade Slot for some enchant Type
                            if (targetItem.GetOwner() != player)
                            {
                                if (enchantEntry == null)
                                    return SpellCastResult.Error;

                                if (enchantEntry.GetFlags().HasFlag(SpellItemEnchantmentFlags.Soulbound))
                                    return SpellCastResult.NotTradeable;
                            }

                            break;
                        }
                    case SpellEffectName.EnchantItemTemporary:
                        {
                            Item item = Targets.GetItemTarget();

                            if (item == null)
                                return SpellCastResult.ItemNotFound;

                            // Not allow enchant in trade Slot for some enchant Type
                            if (item.GetOwner() != player)
                            {
                                int enchant_id = spellEffectInfo.MiscValue;
                                var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);

                                if (enchantEntry == null)
                                    return SpellCastResult.Error;

                                if (enchantEntry.GetFlags().HasFlag(SpellItemEnchantmentFlags.Soulbound))
                                    return SpellCastResult.NotTradeable;
                            }

                            // Apply Item level restriction if the enchanting spell has max level restrition set
                            if (CastItem != null &&
                                SpellInfo.MaxLevel > 0)
                            {
                                if (item.GetTemplate().GetBaseItemLevel() < CastItem.GetTemplate().GetBaseRequiredLevel())
                                    return SpellCastResult.Lowlevel;

                                if (item.GetTemplate().GetBaseItemLevel() > SpellInfo.MaxLevel)
                                    return SpellCastResult.Highlevel;
                            }

                            break;
                        }
                    case SpellEffectName.EnchantHeldItem:
                        // check Item existence in effect code (not output errors at offhand hold Item effect to main hand for example
                        break;
                    case SpellEffectName.Disenchant:
                        {
                            Item item = Targets.GetItemTarget();

                            if (!item)
                                return SpellCastResult.CantBeSalvaged;

                            // prevent disenchanting in trade Slot
                            if (item.GetOwnerGUID() != player.GetGUID())
                                return SpellCastResult.CantBeSalvaged;

                            ItemTemplate itemProto = item.GetTemplate();

                            if (itemProto == null)
                                return SpellCastResult.CantBeSalvaged;

                            ItemDisenchantLootRecord itemDisenchantLoot = item.GetDisenchantLoot(_caster.ToPlayer());

                            if (itemDisenchantLoot == null)
                                return SpellCastResult.CantBeSalvaged;

                            if (itemDisenchantLoot.SkillRequired > player.GetSkillValue(SkillType.Enchanting))
                                return SpellCastResult.CantBeSalvagedSkill;

                            break;
                        }
                    case SpellEffectName.Prospecting:
                        {
                            Item item = Targets.GetItemTarget();

                            if (!item)
                                return SpellCastResult.CantBeProspected;

                            //ensure Item is a prospectable ore
                            if (!item.GetTemplate().HasFlag(ItemFlags.IsProspectable))
                                return SpellCastResult.CantBeProspected;

                            //prevent prospecting in trade Slot
                            if (item.GetOwnerGUID() != player.GetGUID())
                                return SpellCastResult.CantBeProspected;

                            //Check for enough skill in jewelcrafting
                            uint item_prospectingskilllevel = item.GetTemplate().GetRequiredSkillRank();

                            if (item_prospectingskilllevel > player.GetSkillValue(SkillType.Jewelcrafting))
                                return SpellCastResult.LowCastlevel;

                            //make sure the player has the required ores in inventory
                            if (item.GetCount() < 5)
                            {
                                param1 = (int)item.GetEntry();
                                param2 = 5;

                                return SpellCastResult.NeedMoreItems;
                            }

                            if (!LootStorage.Prospecting.HaveLootFor(Targets.GetItemTargetEntry()))
                                return SpellCastResult.CantBeProspected;

                            break;
                        }
                    case SpellEffectName.Milling:
                        {
                            Item item = Targets.GetItemTarget();

                            if (!item)
                                return SpellCastResult.CantBeMilled;

                            //ensure Item is a millable herb
                            if (!item.GetTemplate().HasFlag(ItemFlags.IsMillable))
                                return SpellCastResult.CantBeMilled;

                            //prevent milling in trade Slot
                            if (item.GetOwnerGUID() != player.GetGUID())
                                return SpellCastResult.CantBeMilled;

                            //Check for enough skill in inscription
                            uint item_millingskilllevel = item.GetTemplate().GetRequiredSkillRank();

                            if (item_millingskilllevel > player.GetSkillValue(SkillType.Inscription))
                                return SpellCastResult.LowCastlevel;

                            //make sure the player has the required herbs in inventory
                            if (item.GetCount() < 5)
                            {
                                param1 = (int)item.GetEntry();
                                param2 = 5;

                                return SpellCastResult.NeedMoreItems;
                            }

                            if (!LootStorage.Milling.HaveLootFor(Targets.GetItemTargetEntry()))
                                return SpellCastResult.CantBeMilled;

                            break;
                        }
                    case SpellEffectName.WeaponDamage:
                    case SpellEffectName.WeaponDamageNoSchool:
                        {
                            if (AttackType != WeaponAttackType.RangedAttack)
                                break;

                            Item item = player.GetWeaponForAttack(AttackType);

                            if (item == null ||
                                item.IsBroken())
                                return SpellCastResult.EquippedItem;

                            switch ((ItemSubClassWeapon)item.GetTemplate().GetSubClass())
                            {
                                case ItemSubClassWeapon.Thrown:
                                    {
                                        uint ammo = item.GetEntry();

                                        if (!player.HasItemCount(ammo))
                                            return SpellCastResult.NoAmmo;

                                        break;
                                    }
                                case ItemSubClassWeapon.Gun:
                                case ItemSubClassWeapon.Bow:
                                case ItemSubClassWeapon.Crossbow:
                                case ItemSubClassWeapon.Wand:
                                    break;
                                default:
                                    break;
                            }

                            break;
                        }
                    case SpellEffectName.RechargeItem:
                        {
                            uint itemId = spellEffectInfo.ItemType;

                            ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemId);

                            if (proto == null)
                                return SpellCastResult.ItemAtMaxCharges;

                            Item item = player.GetItemByEntry(itemId);

                            if (item != null)
                                foreach (ItemEffectRecord itemEffect in item.GetEffects())
                                    if (itemEffect.LegacySlotIndex <= item._itemData.SpellCharges.GetSize() &&
                                        itemEffect.Charges != 0 &&
                                        item.GetSpellCharges(itemEffect.LegacySlotIndex) == itemEffect.Charges)
                                        return SpellCastResult.ItemAtMaxCharges;

                            break;
                        }
                    case SpellEffectName.RespecAzeriteEmpoweredItem:
                        {
                            Item item = Targets.GetItemTarget();

                            if (item == null)
                                return SpellCastResult.AzeriteEmpoweredOnly;

                            if (item.GetOwnerGUID() != _caster.GetGUID())
                                return SpellCastResult.DontReport;

                            AzeriteEmpoweredItem azeriteEmpoweredItem = item.ToAzeriteEmpoweredItem();

                            if (azeriteEmpoweredItem == null)
                                return SpellCastResult.AzeriteEmpoweredOnly;

                            bool hasSelections = false;

                            for (int tier = 0; tier < SharedConst.MaxAzeriteEmpoweredTier; ++tier)
                                if (azeriteEmpoweredItem.GetSelectedAzeritePower(tier) != 0)
                                {
                                    hasSelections = true;

                                    break;
                                }

                            if (!hasSelections)
                                return SpellCastResult.AzeriteEmpoweredNoChoicesToUndo;

                            if (!_caster.ToPlayer().HasEnoughMoney(azeriteEmpoweredItem.GetRespecCost()))
                                return SpellCastResult.DontReport;

                            break;
                        }
                    default:
                        break;
                }

            // check weapon presence in slots for main/offhand weapons
            if (!Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.IgnoreEquippedItemRequirement) &&
                SpellInfo.EquippedItemClass >= 0)
            {
                var weaponCheck = new Func<WeaponAttackType, SpellCastResult>(attackType =>
                                                                              {
                                                                                  Item item = player.ToPlayer().GetWeaponForAttack(attackType);

                                                                                  // skip spell if no weapon in Slot or broken
                                                                                  if (!item ||
                                                                                      item.IsBroken())
                                                                                      return SpellCastResult.EquippedItemClass;

                                                                                  // skip spell if weapon not fit to triggered spell
                                                                                  if (!item.IsFitToSpellRequirements(SpellInfo))
                                                                                      return SpellCastResult.EquippedItemClass;

                                                                                  return SpellCastResult.SpellCastOk;
                                                                              });

                // main hand weapon required
                if (SpellInfo.HasAttribute(SpellAttr3.RequiresMainHandWeapon))
                {
                    SpellCastResult mainHandResult = weaponCheck(WeaponAttackType.BaseAttack);

                    if (mainHandResult != SpellCastResult.SpellCastOk)
                        return mainHandResult;
                }

                // offhand hand weapon required
                if (SpellInfo.HasAttribute(SpellAttr3.RequiresOffHandWeapon))
                {
                    SpellCastResult offHandResult = weaponCheck(WeaponAttackType.OffAttack);

                    if (offHandResult != SpellCastResult.SpellCastOk)
                        return offHandResult;
                }
            }

            return SpellCastResult.SpellCastOk;
        }

        private bool UpdatePointers()
        {
            if (_originalCasterGUID == _caster.GetGUID())
            {
                _originalCaster = _caster.ToUnit();
            }
            else
            {
                _originalCaster = Global.ObjAccessor.GetUnit(_caster, _originalCasterGUID);

                if (_originalCaster != null &&
                    !_originalCaster.IsInWorld)
                    _originalCaster = null;
            }

            if (!CastItemGUID.IsEmpty() &&
                _caster.IsTypeId(TypeId.Player))
            {
                CastItem = _caster.ToPlayer().GetItemByGuid(CastItemGUID);
                CastItemLevel = -1;

                // cast Item not found, somehow the Item is no longer where we expected
                if (!CastItem)
                    return false;

                // check if the Item is really the same, in case it has been wrapped for example
                if (CastItemEntry != CastItem.GetEntry())
                    return false;

                CastItemLevel = (int)CastItem.GetItemLevel(_caster.ToPlayer());
            }

            Targets.Update(_caster);

            // further actions done only for dest targets
            if (!Targets.HasDst())
                return true;

            // cache last Transport
            WorldObject transport = null;

            // update effect destinations (in case of moved Transport dest Target)
            foreach (var spellEffectInfo in SpellInfo.GetEffects())
            {
                SpellDestination dest = _destTargets[spellEffectInfo.EffectIndex];

                if (dest.TransportGUID.IsEmpty())
                    continue;

                if (transport == null ||
                    transport.GetGUID() != dest.TransportGUID)
                    transport = Global.ObjAccessor.GetWorldObject(_caster, dest.TransportGUID);

                if (transport != null)
                {
                    dest.Position.Relocate(transport.GetPosition());
                    dest.Position.RelocateOffset(dest.TransportOffset);
                }
            }

            return true;
        }

        private bool CheckEffectTarget(Unit target, SpellEffectInfo spellEffectInfo, Position losPosition)
        {
            if (spellEffectInfo == null ||
                !spellEffectInfo.IsEffect())
                return false;

            switch (spellEffectInfo.ApplyAuraName)
            {
                case AuraType.ModPossess:
                case AuraType.ModCharm:
                case AuraType.ModPossessPet:
                case AuraType.AoeCharm:
                    if (target.GetVehicleKit() &&
                        target.GetVehicleKit().IsControllableVehicle())
                        return false;

                    if (target.IsMounted())
                        return false;

                    if (!target.GetCharmerGUID().IsEmpty())
                        return false;

                    int damage = CalculateDamage(spellEffectInfo, target);

                    if (damage != 0)
                        if (target.GetLevelForTarget(_caster) > damage)
                            return false;

                    break;
                default:
                    break;
            }

            // check for ignore LOS on the effect itself
            if (SpellInfo.HasAttribute(SpellAttr2.IgnoreLineOfSight) ||
                Global.DisableMgr.IsDisabledFor(DisableType.Spell, SpellInfo.Id, null, (byte)DisableFlags.SpellLOS))
                return true;

            // check if gameobject ignores LOS
            GameObject gobCaster = _caster.ToGameObject();

            if (gobCaster != null)
                if (gobCaster.GetGoInfo().GetRequireLOS() == 0)
                    return true;

            // if spell is triggered, need to check for LOS disable on the aura triggering it and inherit that behaviour
            if (!SpellInfo.HasAttribute(SpellAttr5.AlwaysLineOfSight) &&
                IsTriggered() &&
                TriggeredByAuraSpell != null &&
                (TriggeredByAuraSpell.HasAttribute(SpellAttr2.IgnoreLineOfSight) || Global.DisableMgr.IsDisabledFor(DisableType.Spell, TriggeredByAuraSpell.Id, null, (byte)DisableFlags.SpellLOS)))
                return true;

            // @todo shit below shouldn't be here, but it's temporary
            //Check targets for LOS visibility
            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.SkinPlayerCorpse:
                    {
                        if (Targets.GetCorpseTargetGUID().IsEmpty())
                        {
                            if (target.IsWithinLOSInMap(_caster, LineOfSightChecks.All, ModelIgnoreFlags.M2) &&
                                target.HasUnitFlag(UnitFlags.Skinnable))
                                return true;

                            return false;
                        }

                        Corpse corpse = ObjectAccessor.GetCorpse(_caster, Targets.GetCorpseTargetGUID());

                        if (!corpse)
                            return false;

                        if (target.GetGUID() != corpse.GetOwnerGUID())
                            return false;

                        if (!corpse.HasCorpseDynamicFlag(CorpseDynFlags.Lootable))
                            return false;

                        if (!corpse.IsWithinLOSInMap(_caster, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                            return false;

                        break;
                    }
                default:
                    {
                        if (losPosition == null ||
                            SpellInfo.HasAttribute(SpellAttr5.AlwaysAoeLineOfSight))
                        {
                            // Get GO cast coordinates if original caster . GO
                            WorldObject caster = null;

                            if (_originalCasterGUID.IsGameObject())
                                caster = _caster.GetMap().GetGameObject(_originalCasterGUID);

                            if (!caster)
                                caster = _caster;

                            if (target != _caster &&
                                !target.IsWithinLOSInMap(caster, LineOfSightChecks.All, ModelIgnoreFlags.M2))
                                return false;
                        }

                        if (losPosition != null)
                            if (!target.IsWithinLOS(losPosition.GetPositionX(), losPosition.GetPositionY(), losPosition.GetPositionZ(), LineOfSightChecks.All, ModelIgnoreFlags.M2))
                                return false;

                        break;
                    }
            }

            return true;
        }

        private bool CheckEffectTarget(GameObject target, SpellEffectInfo spellEffectInfo)
        {
            if (spellEffectInfo == null ||
                !spellEffectInfo.IsEffect())
                return false;

            switch (spellEffectInfo.Effect)
            {
                case SpellEffectName.GameObjectDamage:
                case SpellEffectName.GameobjectRepair:
                case SpellEffectName.GameobjectSetDestructionState:
                    if (target.GetGoType() != GameObjectTypes.DestructibleBuilding)
                        return false;

                    break;
                default:
                    break;
            }

            return true;
        }

        private bool CheckEffectTarget(Item target, SpellEffectInfo spellEffectInfo)
        {
            if (spellEffectInfo == null ||
                !spellEffectInfo.IsEffect())
                return false;

            return true;
        }

        private bool IsAutoActionResetSpell()
        {
            if (IsTriggered())
                return false;

            if (_casttime == 0 &&
                SpellInfo.HasAttribute(SpellAttr6.DoesntResetSwingTimerIfInstant))
                return false;

            return true;
        }

        private bool IsNeedSendToClient()
        {
            return SpellVisual.SpellXSpellVisualID != 0 ||
                   SpellVisual.ScriptVisualID != 0 ||
                   SpellInfo.IsChanneled() ||
                   SpellInfo.HasAttribute(SpellAttr8.AuraSendAmount) ||
                   SpellInfo.HasHitDelay() ||
                   (TriggeredByAuraSpell == null && !IsTriggered());
        }

        private bool IsValidDeadOrAliveTarget(Unit target)
        {
            if (target.IsAlive())
                return !SpellInfo.IsRequiringDeadTarget();

            if (SpellInfo.IsAllowingDeadTarget())
                return true;

            return false;
        }

        private void HandleLaunchPhase()
        {
            // handle effects with SPELL_EFFECT_HANDLE_LAUNCH mode
            foreach (var spellEffectInfo in SpellInfo.GetEffects())
            {
                // don't do anything for empty effect
                if (!spellEffectInfo.IsEffect())
                    continue;

                HandleEffects(null, null, null, null, spellEffectInfo, SpellEffectHandleMode.Launch);
            }

            PrepareTargetProcessing();

            foreach (TargetInfo target in UniqueTargetInfo)
                PreprocessSpellLaunch(target);

            foreach (var spellEffectInfo in SpellInfo.GetEffects())
            {
                float multiplier = 1.0f;

                if ((_applyMultiplierMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0)
                    multiplier = spellEffectInfo.CalcDamageMultiplier(_originalCaster, this);

                foreach (TargetInfo target in UniqueTargetInfo)
                {
                    uint mask = target.EffectMask;

                    if ((mask & (1 << (int)spellEffectInfo.EffectIndex)) == 0)
                        continue;

                    DoEffectOnLaunchTarget(target, multiplier, spellEffectInfo);
                }
            }

            FinishTargetProcessing();
        }

        private void PreprocessSpellLaunch(TargetInfo targetInfo)
        {
            Unit targetUnit = _caster.GetGUID() == targetInfo.TargetGUID ? _caster.ToUnit() : Global.ObjAccessor.GetUnit(_caster, targetInfo.TargetGUID);

            if (targetUnit == null)
                return;

            // This will only cause combat - the Target will engage once the projectile hits (in Spell::TargetInfo::PreprocessTarget)
            if (_originalCaster &&
                targetInfo.MissCondition != SpellMissInfo.Evade &&
                !_originalCaster.IsFriendlyTo(targetUnit) &&
                (!SpellInfo.IsPositive() || SpellInfo.HasEffect(SpellEffectName.Dispel)) &&
                (SpellInfo.HasInitialAggro() || targetUnit.IsEngaged()))
                _originalCaster.SetInCombatWith(targetUnit, true);

            Unit unit = null;

            // In case spell hit Target, do all effect on that Target
            if (targetInfo.MissCondition == SpellMissInfo.None)
                unit = targetUnit;
            // In case spell reflect from Target, do all effect on caster (if hit)
            else if (targetInfo.MissCondition == SpellMissInfo.Reflect &&
                     targetInfo.ReflectResult == SpellMissInfo.None)
                unit = _caster.ToUnit();

            if (unit == null)
                return;

            float critChance = SpellValue.CriticalChance;

            if (_originalCaster)
            {
                if (critChance == 0)
                    critChance = _originalCaster.SpellCritChanceDone(this, null, SpellSchoolMask, AttackType);

                critChance = unit.SpellCritChanceTaken(_originalCaster, this, null, SpellSchoolMask, critChance, AttackType);
            }

            targetInfo.IsCrit = RandomHelper.randChance(critChance);
        }

        private void DoEffectOnLaunchTarget(TargetInfo targetInfo, float multiplier, SpellEffectInfo spellEffectInfo)
        {
            Unit unit = null;

            // In case spell hit Target, do all effect on that Target
            if (targetInfo.MissCondition == SpellMissInfo.None ||
                (targetInfo.MissCondition == SpellMissInfo.Block && !SpellInfo.HasAttribute(SpellAttr3.CompletelyBlocked)))
                unit = _caster.GetGUID() == targetInfo.TargetGUID ? _caster.ToUnit() : Global.ObjAccessor.GetUnit(_caster, targetInfo.TargetGUID);
            // In case spell reflect from Target, do all effect on caster (if hit)
            else if (targetInfo.MissCondition == SpellMissInfo.Reflect &&
                     targetInfo.ReflectResult == SpellMissInfo.None)
                unit = _caster.ToUnit();

            if (!unit)
                return;

            EffectDamage = 0;
            EffectHealing = 0;

            HandleEffects(unit, null, null, null, spellEffectInfo, SpellEffectHandleMode.LaunchTarget);

            if (_originalCaster != null &&
                EffectDamage > 0)
                if (spellEffectInfo.IsTargetingArea() ||
                    spellEffectInfo.IsAreaAuraEffect() ||
                    spellEffectInfo.IsEffect(SpellEffectName.PersistentAreaAura) ||
                    SpellInfo.HasAttribute(SpellAttr5.TreatAsAreaEffect))
                {
                    EffectDamage = unit.CalculateAOEAvoidance(EffectDamage, (uint)SpellInfo.SchoolMask, _originalCaster.GetGUID());

                    if (_originalCaster.IsPlayer())
                    {
                        // cap Damage of player AOE
                        long targetAmount = GetUnitTargetCountForEffect(spellEffectInfo.EffectIndex);

                        if (targetAmount > 20)
                            EffectDamage = (int)(EffectDamage * 20 / targetAmount);
                    }
                }

            if ((_applyMultiplierMask & (1 << (int)spellEffectInfo.EffectIndex)) != 0)
            {
                EffectDamage = (int)(EffectDamage * _damageMultipliers[spellEffectInfo.EffectIndex]);
                EffectHealing = (int)(EffectHealing * _damageMultipliers[spellEffectInfo.EffectIndex]);

                _damageMultipliers[spellEffectInfo.EffectIndex] *= multiplier;
            }

            targetInfo.Damage += EffectDamage;
            targetInfo.Healing += EffectHealing;
        }

        private SpellCastResult CanOpenLock(SpellEffectInfo effect, uint lockId, ref SkillType skillId, ref int reqSkillValue, ref int skillValue)
        {
            if (lockId == 0) // possible case for GO and maybe for items.
                return SpellCastResult.SpellCastOk;

            Unit unitCaster = _caster.ToUnit();

            if (unitCaster == null)
                return SpellCastResult.BadTargets;

            // Get LockInfo
            var lockInfo = CliDB.LockStorage.LookupByKey(lockId);

            if (lockInfo == null)
                return SpellCastResult.BadTargets;

            bool reqKey = false; // some locks not have reqs

            for (int j = 0; j < SharedConst.MaxLockCase; ++j)
                switch ((LockKeyType)lockInfo.LockType[j])
                {
                    // check key Item (many fit cases can be)
                    case LockKeyType.Item:
                        if (lockInfo.Index[j] != 0 &&
                            CastItem &&
                            CastItem.GetEntry() == lockInfo.Index[j])
                            return SpellCastResult.SpellCastOk;

                        reqKey = true;

                        break;
                    // check key skill (only single first fit case can be)
                    case LockKeyType.Skill:
                        {
                            reqKey = true;

                            // wrong locktype, skip
                            if (effect.MiscValue != lockInfo.Index[j])
                                continue;

                            skillId = SharedConst.SkillByLockType((LockType)lockInfo.Index[j]);

                            if (skillId != SkillType.None ||
                                lockInfo.Index[j] == (uint)LockType.Lockpicking)
                            {
                                reqSkillValue = lockInfo.Skill[j];

                                // castitem check: rogue using skeleton keys. the skill values should not be added in this case.
                                skillValue = 0;

                                if (!CastItem &&
                                    unitCaster.IsTypeId(TypeId.Player))
                                    skillValue = unitCaster.ToPlayer().GetSkillValue(skillId);
                                else if (lockInfo.Index[j] == (uint)LockType.Lockpicking)
                                    skillValue = (int)unitCaster.GetLevel() * 5;

                                // skill bonus provided by casting spell (mostly Item spells)
                                // add the effect base points modifier from the spell cast (cheat lock / skeleton key etc.)
                                if (effect.TargetA.GetTarget() == Framework.Constants.Targets.GameobjectItemTarget ||
                                    effect.TargetB.GetTarget() == Framework.Constants.Targets.GameobjectItemTarget)
                                    skillValue += effect.CalcValue();

                                if (skillValue < reqSkillValue)
                                    return SpellCastResult.LowCastlevel;
                            }

                            return SpellCastResult.SpellCastOk;
                        }
                    case LockKeyType.Spell:
                        if (SpellInfo.Id == lockInfo.Index[j])
                            return SpellCastResult.SpellCastOk;

                        reqKey = true;

                        break;
                }

            if (reqKey)
                return SpellCastResult.BadTargets;

            return SpellCastResult.SpellCastOk;
        }

        private void PrepareTargetProcessing()
        {
        }

        private void FinishTargetProcessing()
        {
            SendSpellExecuteLog();
        }

        private void LoadScripts()
        {
            _loadedScripts = Global.ScriptMgr.CreateSpellScripts(SpellInfo.Id, this);

            foreach (var script in _loadedScripts)
            {
                Log.outDebug(LogFilter.Spells, "Spell.LoadScripts: Script `{0}` for spell `{1}` is loaded now", script._GetScriptName(), SpellInfo.Id);
                script.Register();

                if (script is ISpellScript)
                    foreach (var iFace in script.GetType().GetInterfaces())
                    {
                        if (iFace.Name == nameof(ISpellScript) ||
                            iFace.Name == nameof(ISpellScript))
                            continue;

                        if (!_spellScriptsByType.TryGetValue(iFace, out var spellScripts))
                        {
                            spellScripts = new List<ISpellScript>();
                            _spellScriptsByType[iFace] = spellScripts;
                        }

                        spellScripts.Add((ISpellScript)script);
                        RegisterSpellEffectHandler(script);
                    }
            }
        }

        private void RegisterSpellEffectHandler(SpellScript script)
        {
            if (script is IHasSpellEffects hse)
                foreach (var effect in hse.SpellEffects)
                    if (effect is ISpellEffectHandler se)
                    {
                        uint mask = 0;

                        if (se.EffectIndex == SpellConst.EffectAll ||
                            se.EffectIndex == SpellConst.EffectFirstFound)
                        {
                            for (byte i = 0; i < SpellConst.MaxEffects; ++i)
                            {
                                if (se.EffectIndex == SpellConst.EffectFirstFound &&
                                    mask != 0)
                                    break;

                                if (CheckSpellEffectHandler(se, i))
                                    AddSpellEffect(i, script, se);
                            }
                        }
                        else
                        {
                            if (CheckSpellEffectHandler(se, se.EffectIndex))
                                AddSpellEffect(se.EffectIndex, script, se);
                        }
                    }
                    else if (effect is ITargetHookHandler th)
                    {
                        uint mask = 0;

                        if (th.EffectIndex == SpellConst.EffectAll ||
                            th.EffectIndex == SpellConst.EffectFirstFound)
                        {
                            for (byte i = 0; i < SpellConst.MaxEffects; ++i)
                            {
                                if (th.EffectIndex == SpellConst.EffectFirstFound &&
                                    mask != 0)
                                    break;

                                if (CheckTargetHookEffect(th, i))
                                    AddSpellEffect(i, script, th);
                            }
                        }
                        else
                        {
                            if (CheckTargetHookEffect(th, th.EffectIndex))
                                AddSpellEffect(th.EffectIndex, script, th);
                        }
                    }
        }

        private bool CheckSpellEffectHandler(ISpellEffectHandler se, uint effIndex)
        {
            if (SpellInfo.GetEffects().Count <= effIndex)
                return false;

            SpellEffectInfo spellEffectInfo = SpellInfo.GetEffect(effIndex);

            if (spellEffectInfo.Effect == 0 &&
                se.EffectName == 0)
                return true;

            if (spellEffectInfo.Effect == 0)
                return false;

            return se.EffectName == SpellEffectName.Any || spellEffectInfo.Effect == se.EffectName;
        }


        private void CallScriptOnPrecastHandler()
        {
            foreach (ISpellScript script in GetSpellScripts<IOnPrecast>())
            {
                script._PrepareScriptCall(SpellScriptHookType.OnPrecast);
                ((IOnPrecast)script).OnPrecast();
                script._FinishScriptCall();
            }
        }

        private void CallScriptBeforeCastHandlers()
        {
            foreach (ISpellScript script in GetSpellScripts<IBeforeCast>())
            {
                script._PrepareScriptCall(SpellScriptHookType.BeforeCast);

                ((IBeforeCast)script).BeforeCast();

                script._FinishScriptCall();
            }
        }

        private void CallScriptOnCastHandlers()
        {
            foreach (ISpellScript script in GetSpellScripts<IOnCast>())
            {
                script._PrepareScriptCall(SpellScriptHookType.OnCast);

                ((IOnCast)script).OnCast();

                script._FinishScriptCall();
            }
        }

        private void CallScriptAfterCastHandlers()
        {
            foreach (ISpellScript script in GetSpellScripts<IAfterCast>())
            {
                script._PrepareScriptCall(SpellScriptHookType.AfterCast);

                ((IAfterCast)script).AfterCast();

                script._FinishScriptCall();
            }
        }

        private SpellCastResult CallScriptCheckCastHandlers()
        {
            SpellCastResult retVal = SpellCastResult.SpellCastOk;

            foreach (ISpellScript script in GetSpellScripts<ICheckCastHander>())
            {
                script._PrepareScriptCall(SpellScriptHookType.CheckCast);

                var tempResult = ((ICheckCastHander)script).CheckCast();

                if (tempResult != SpellCastResult.SpellCastOk)
                    retVal = tempResult;

                script._FinishScriptCall();
            }

            return retVal;
        }

        private int CallScriptCalcCastTimeHandlers(int castTime)
        {
            foreach (ISpellScript script in GetSpellScripts<ICalculateCastTime>())
            {
                script._PrepareScriptCall(SpellScriptHookType.CalcCastTime);
                castTime = ((ICalculateCastTime)script).CalcCastTime(castTime);
                script._FinishScriptCall();
            }

            return castTime;
        }

        private bool CallScriptEffectHandlers(uint effIndex, SpellEffectHandleMode mode)
        {
            // execute script effect handler hooks and check if effects was prevented
            bool preventDefault = false;

            switch (mode)
            {
                case SpellEffectHandleMode.Launch:

                    foreach (var script in GetEffectScripts(SpellScriptHookType.Launch, effIndex))
                        preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.Launch);

                    break;
                case SpellEffectHandleMode.LaunchTarget:

                    foreach (var script in GetEffectScripts(SpellScriptHookType.LaunchTarget, effIndex))
                        preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.LaunchTarget);

                    break;
                case SpellEffectHandleMode.Hit:

                    foreach (var script in GetEffectScripts(SpellScriptHookType.Hit, effIndex))
                        preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.Hit);

                    break;
                case SpellEffectHandleMode.HitTarget:

                    foreach (var script in GetEffectScripts(SpellScriptHookType.EffectHitTarget, effIndex))
                        preventDefault = ProcessScript(effIndex, preventDefault, script.Item1, script.Item2, SpellScriptHookType.EffectHitTarget);

                    break;
                default:
                    Cypher.Assert(false);

                    return false;
            }

            return preventDefault;
        }

        private static bool ProcessScript(uint effIndex, bool preventDefault, ISpellScript script, ISpellEffect effect, SpellScriptHookType hookType)
        {
            script._InitHit();

            script._PrepareScriptCall(hookType);

            if (!script._IsEffectPrevented(effIndex))
                if (effect is ISpellEffectHandler seh)
                    seh.CallEffect(effIndex);

            if (!preventDefault)
                preventDefault = script._IsDefaultEffectPrevented(effIndex);

            script._FinishScriptCall();

            return preventDefault;
        }

        private void CallScriptSuccessfulDispel(uint effIndex)
        {
            foreach (var script in GetEffectScripts(SpellScriptHookType.EffectSuccessfulDispel, effIndex))
            {
                script.Item1._PrepareScriptCall(SpellScriptHookType.EffectSuccessfulDispel);

                if (script.Item2 is ISpellEffectHandler seh)
                    seh.CallEffect(effIndex);

                script.Item1._FinishScriptCall();
            }
        }

        private void CallScriptObjectAreaTargetSelectHandlers(List<WorldObject> targets, uint effIndex, SpellImplicitTargetInfo targetType)
        {
            foreach (var script in GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndex))
            {
                script.Item1._PrepareScriptCall(SpellScriptHookType.ObjectAreaTargetSelect);

                if (script.Item2 is IObjectAreaTargetSelect oas)
                    if (targetType.GetTarget() == oas.TargetType)
                        oas.FilterTargets(targets);

                script.Item1._FinishScriptCall();
            }
        }

        private void CallScriptObjectTargetSelectHandlers(ref WorldObject target, uint effIndex, SpellImplicitTargetInfo targetType)
        {
            foreach (var script in GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndex))
            {
                script.Item1._PrepareScriptCall(SpellScriptHookType.ObjectTargetSelect);

                if (script.Item2 is IObjectTargetSelectHandler ots)
                    if (targetType.GetTarget() == ots.TargetType)
                        ots.TargetSelect(ref target);

                script.Item1._FinishScriptCall();
            }
        }

        private void CallScriptDestinationTargetSelectHandlers(ref SpellDestination target, uint effIndex, SpellImplicitTargetInfo targetType)
        {
            foreach (var script in GetEffectScripts(SpellScriptHookType.DestinationTargetSelect, effIndex))
            {
                script.Item1._PrepareScriptCall(SpellScriptHookType.DestinationTargetSelect);

                if (script.Item2 is IDestinationTargetSelectHandler dts)
                    if (targetType.GetTarget() == dts.TargetType)
                        dts.SetDest(ref target);

                script.Item1._FinishScriptCall();
            }
        }

        private bool CheckScriptEffectImplicitTargets(uint effIndex, uint effIndexToCheck)
        {
            // Skip if there are not any script
            if (_loadedScripts.Empty())
                return true;

            var otsTargetEffIndex = GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndex).Count > 0;
            var otsEffIndexCheck = GetEffectScripts(SpellScriptHookType.ObjectTargetSelect, effIndexToCheck).Count > 0;

            var oatsTargetEffIndex = GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndex).Count > 0;
            var oatsEffIndexCheck = GetEffectScripts(SpellScriptHookType.ObjectAreaTargetSelect, effIndexToCheck).Count > 0;

            if ((otsTargetEffIndex && !otsEffIndexCheck) ||
                (!otsTargetEffIndex && otsEffIndexCheck))
                return false;

            if ((oatsTargetEffIndex && !oatsEffIndexCheck) ||
                (!oatsTargetEffIndex && oatsEffIndexCheck))
                return false;

            return true;
        }

        private void PrepareTriggersExecutedOnHit()
        {
            Unit unitCaster = _caster.ToUnit();

            if (unitCaster == null)
                return;

            // handle SPELL_AURA_ADD_TARGET_TRIGGER Auras:
            // save Auras which were present on spell caster on cast, to prevent triggered Auras from affecting caster
            // and to correctly calculate proc chance when combopoints are present
            var targetTriggers = unitCaster.GetAuraEffectsByType(AuraType.AddTargetTrigger);

            foreach (var aurEff in targetTriggers)
            {
                if (!aurEff.IsAffectingSpell(SpellInfo))
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(aurEff.GetSpellEffectInfo().TriggerSpell, GetCastDifficulty());

                if (spellInfo != null)
                {
                    // calculate the chance using spell base amount, because aura amount is not updated on combo-points change
                    // this possibly needs fixing
                    int auraBaseAmount = aurEff.GetBaseAmount();
                    // proc chance is stored in effect amount
                    int chance = unitCaster.CalculateSpellDamage(null, aurEff.GetSpellEffectInfo(), auraBaseAmount);
                    chance *= aurEff.GetBase().GetStackAmount();

                    // build trigger and add to the list
                    _hitTriggerSpells.Add(new HitTriggerSpell(spellInfo, aurEff.GetSpellInfo(), chance));
                }
            }
        }

        private bool CanHaveGlobalCooldown(WorldObject caster)
        {
            // Only players or controlled units have global cooldown
            if (!caster.IsPlayer() &&
                (!caster.IsCreature() || caster.ToCreature().GetCharmInfo() == null))
                return false;

            return true;
        }

        private bool HasGlobalCooldown()
        {
            if (!CanHaveGlobalCooldown(_caster))
                return false;

            return _caster.ToUnit().GetSpellHistory().HasGlobalCooldown(SpellInfo);
        }

        private void TriggerGlobalCooldown()
        {
            if (!CanHaveGlobalCooldown(_caster))
                return;

            TimeSpan gcd = TimeSpan.FromMilliseconds(SpellInfo.StartRecoveryTime);

            if (gcd == TimeSpan.Zero ||
                SpellInfo.StartRecoveryCategory == 0)
                return;

            if (_caster.IsTypeId(TypeId.Player))
                if (_caster.ToPlayer().GetCommandStatus(PlayerCommandStates.Cooldown))
                    return;

            TimeSpan MinGCD = TimeSpan.FromMilliseconds(750);
            TimeSpan MaxGCD = TimeSpan.FromMilliseconds(1500);

            // Global cooldown can't leave range 1..1.5 secs
            // There are some spells (mostly not casted directly by player) that have < 1 sec and > 1.5 sec global cooldowns
            // but as tests show are not affected by any spell mods.
            if (gcd >= MinGCD &&
                gcd <= MaxGCD)
            {
                // gcd modifier Auras are applied only to own spells and only players have such mods
                Player modOwner = _caster.GetSpellModOwner();

                if (modOwner)
                {
                    int intGcd = (int)gcd.TotalMilliseconds;
                    modOwner.ApplySpellMod(SpellInfo, SpellModOp.StartCooldown, ref intGcd, this);
                    gcd = TimeSpan.FromMilliseconds(intGcd);
                }

                bool isMeleeOrRangedSpell = SpellInfo.DmgClass == SpellDmgClass.Melee ||
                                            SpellInfo.DmgClass == SpellDmgClass.Ranged ||
                                            SpellInfo.HasAttribute(SpellAttr0.UsesRangedSlot) ||
                                            SpellInfo.HasAttribute(SpellAttr0.IsAbility);

                // Apply haste rating
                if (gcd > MinGCD &&
                    (SpellInfo.StartRecoveryCategory == 133 && !isMeleeOrRangedSpell))
                {
                    gcd = TimeSpan.FromMilliseconds(gcd.TotalMilliseconds * _caster.ToUnit().UnitData.ModSpellHaste);
                    int intGcd = (int)gcd.TotalMilliseconds;
                    MathFunctions.RoundToInterval(ref intGcd, 750, 1500);
                    gcd = TimeSpan.FromMilliseconds(intGcd);
                }

                if (gcd > MinGCD &&
                    _caster.ToUnit().HasAuraTypeWithAffectMask(AuraType.ModGlobalCooldownByHasteRegen, SpellInfo))
                {
                    gcd = TimeSpan.FromMilliseconds(gcd.TotalMilliseconds * _caster.ToUnit().UnitData.ModHasteRegen);
                    int intGcd = (int)gcd.TotalMilliseconds;
                    MathFunctions.RoundToInterval(ref intGcd, 750, 1500);
                    gcd = TimeSpan.FromMilliseconds(intGcd);
                }
            }

            _caster.ToUnit().GetSpellHistory().AddGlobalCooldown(SpellInfo, gcd);
        }

        private void CancelGlobalCooldown()
        {
            if (!CanHaveGlobalCooldown(_caster))
                return;

            if (SpellInfo.StartRecoveryTime == 0)
                return;

            // Cancel global cooldown when interrupting current cast
            if (_caster.ToUnit().GetCurrentSpell(CurrentSpellTypes.Generic) != this)
                return;

            _caster.ToUnit().GetSpellHistory().CancelGlobalCooldown(SpellInfo);
        }

        private string GetDebugInfo()
        {
            return $"Id: {GetSpellInfo().Id} Name: '{GetSpellInfo().SpellName[Global.WorldMgr.GetDefaultDbcLocale()]}' OriginalCaster: {_originalCasterGUID} State: {GetState()}";
        }


        private void AddSpellEffect(uint index, ISpellScript script, ISpellEffect effect)
        {
            if (!_effectHandlers.TryGetValue(index, out var effecTypes))
            {
                effecTypes = new Dictionary<SpellScriptHookType, List<(ISpellScript, ISpellEffect)>>();
                _effectHandlers.Add(index, effecTypes);
            }

            if (!effecTypes.TryGetValue(effect.HookType, out var effects))
            {
                effects = new List<(ISpellScript, ISpellEffect)>();
                effecTypes.Add(effect.HookType, effects);
            }

            effects.Add((script, effect));
        }

        private int CalculateDamage(SpellEffectInfo spellEffectInfo, Unit target)
        {
            return CalculateDamage(spellEffectInfo, target, out _);
        }

        private int CalculateDamage(SpellEffectInfo spellEffectInfo, Unit target, out float variance)
        {
            bool needRecalculateBasePoints = (SpellValue.CustomBasePointsMask & (1 << (int)spellEffectInfo.EffectIndex)) == 0;

            return _caster.CalculateSpellDamage(out variance, target, spellEffectInfo, needRecalculateBasePoints ? null : SpellValue.EffectBasePoints[spellEffectInfo.EffectIndex], CastItemEntry, CastItemLevel);
        }

        private void CheckSrc()
        {
            if (!Targets.HasSrc()) Targets.SetSrc(_caster);
        }

        private void CheckDst()
        {
            if (!Targets.HasDst()) Targets.SetDst(_caster);
        }

        private bool IsAutoRepeat()
        {
            return _autoRepeat;
        }

        private void SetAutoRepeat(bool rep)
        {
            _autoRepeat = rep;
        }

        private void ReSetTimer()
        {
            _timer = _casttime > 0 ? _casttime : 0;
        }

        private void SetExecutedCurrently(bool yes)
        {
            _executedCurrently = yes;
        }

        private bool IsDelayableNoMore()
        {
            if (_delayAtDamageCount >= 2)
                return true;

            ++_delayAtDamageCount;

            return false;
        }

        private bool DontReport()
        {
            return Convert.ToBoolean(_triggeredCastFlags & TriggerCastFlags.DontReportCastError);
        }

    }
}