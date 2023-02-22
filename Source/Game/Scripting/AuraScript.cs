// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces;
using Game.Spells;
using Game.Spells;

namespace Game.Scripting
{
    public class AuraScript : BaseSpellScript, IAuraScript
    {
        private class ScriptStateStore
        {
            public AuraApplication _auraApplication;
            public byte _currentScriptState;
            public bool _defaultActionPrevented;

            public ScriptStateStore(byte currentScriptState, AuraApplication auraApplication, bool defaultActionPrevented)
            {
                _auraApplication = auraApplication;
                _currentScriptState = currentScriptState;
                _defaultActionPrevented = defaultActionPrevented;
            }
        }

        private readonly Stack<ScriptStateStore> _scriptStates = new();
        private Aura _aura;
        private AuraApplication _auraApplication;
        private bool _defaultActionPrevented;

        public AuraScript()
        {
            _aura = null;
            _auraApplication = null;
            _defaultActionPrevented = false;
        }

        public bool _Load(Aura aura)
        {
            _aura = aura;
            _PrepareScriptCall((AuraScriptHookType)SpellScriptState.Loading, null);
            bool load = Load();
            _FinishScriptCall();

            return load;
        }

        public void _PrepareScriptCall(AuraScriptHookType hookType, AuraApplication aurApp = null)
        {
            _scriptStates.Push(new ScriptStateStore(CurrentScriptState, _auraApplication, _defaultActionPrevented));
            CurrentScriptState = (byte)hookType;
            _defaultActionPrevented = false;
            _auraApplication = aurApp;
        }

        public void _FinishScriptCall()
        {
            ScriptStateStore stateStore = _scriptStates.Peek();
            CurrentScriptState = stateStore._currentScriptState;
            _auraApplication = stateStore._auraApplication;
            _defaultActionPrevented = stateStore._defaultActionPrevented;
            _scriptStates.Pop();
        }

        public bool _IsDefaultActionPrevented()
        {
            switch ((AuraScriptHookType)CurrentScriptState)
            {
                case AuraScriptHookType.EffectApply:
                case AuraScriptHookType.EffectRemove:
                case AuraScriptHookType.EffectPeriodic:
                case AuraScriptHookType.EffectAbsorb:
                case AuraScriptHookType.EffectSplit:
                case AuraScriptHookType.PrepareProc:
                case AuraScriptHookType.Proc:
                case AuraScriptHookType.EffectProc:
                    return _defaultActionPrevented;
                default:
                    throw new Exception("AuraScript._IsDefaultActionPrevented is called in a wrong place");
            }
        }


        // prevents default Action of a hook from being executed (works only while called in a hook which default Action can be prevented)
        public void PreventDefaultAction()
        {
            switch ((AuraScriptHookType)CurrentScriptState)
            {
                case AuraScriptHookType.EffectApply:
                case AuraScriptHookType.EffectRemove:
                case AuraScriptHookType.EffectPeriodic:
                case AuraScriptHookType.EffectAbsorb:
                case AuraScriptHookType.EffectSplit:
                case AuraScriptHookType.PrepareProc:
                case AuraScriptHookType.EffectProc:
                    _defaultActionPrevented = true;

                    break;
                default:
                    Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}` AuraScript.PreventDefaultAction called in a hook in which the call won't have effect!", ScriptName, ScriptSpellId);

                    break;
            }
        }

        // AuraScript interface - functions which are redirecting to Aura class

        // returns proto of the spell
        public SpellInfo GetSpellInfo()
        {
            return _aura.GetSpellInfo();
        }

        public SpellEffectInfo GetEffectInfo(int effIndex)
        {
            return _aura.GetSpellInfo().GetEffect(effIndex);
        }

        // returns spellid of the spell
        public uint GetId()
        {
            return _aura.GetId();
        }

        // returns Guid of object which casted the aura (_originalCaster of the Spell class)
        public ObjectGuid GetCasterGUID()
        {
            return _aura.GetCasterGUID();
        }

        // returns unit which casted the aura or null if not avalible (caster logged out for example)
        public Unit GetCaster()
        {
            WorldObject caster = _aura.GetCaster();

            if (caster != null)
                return caster.ToUnit();

            return null;
        }

        // returns gameobject which cast the aura or NULL if not available
        public GameObject GetGObjCaster()
        {
            WorldObject caster = _aura.GetCaster();

            if (caster != null)
                return caster.ToGameObject();

            return null;
        }

        // returns object on which aura was casted, Target for non-area Auras, area aura source for area Auras
        public WorldObject GetOwner()
        {
            return _aura.GetOwner();
        }

        // returns owner if it's unit or unit derived object, null otherwise (only for persistent area Auras null is returned)
        public Unit GetUnitOwner()
        {
            return _aura.GetUnitOwner();
        }

        // removes aura with remove mode (see AuraRemoveMode enum)
        public void Remove(AuraRemoveMode removeMode = 0)
        {
            _aura.Remove(removeMode);
        }

        // returns aura object of script
        public Aura GetAura()
        {
            return _aura;
        }

        // aura duration manipulation - when duration goes to 0 aura is removed
        public int GetDuration()
        {
            return _aura.GetDuration();
        }

        public void SetDuration(double duration, bool withMods = false)
        {
            _aura.SetDuration(duration, withMods);
        }

        public void SetDuration(int duration, bool withMods = false)
        {
            _aura.SetDuration(duration, withMods);
        }

        public int GetMaxDuration()
        {
            return _aura.GetMaxDuration();
        }

        public void SetMaxDuration(int duration)
        {
            _aura.SetMaxDuration(duration);
        }

        // expired - duration just went to 0
        public bool IsExpired()
        {
            return _aura.IsExpired();
        }

        // stack amount manipulation
        public byte GetStackAmount()
        {
            return _aura.GetStackAmount();
        }

        public bool ModStackAmount(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            return _aura.ModStackAmount(num, removeMode);
        }

        // check if aura has effect of given effindex
        public bool HasEffect(byte effIndex)
        {
            return _aura.HasEffect(effIndex);
        }

        // returns aura effect of given effect index or null
        public AuraEffect GetEffect(byte effIndex)
        {
            return _aura.GetEffect(effIndex);
        }

        // AuraScript interface - functions which are redirecting to AuraApplication class
        // Do not call these in hooks in which AuraApplication is not avalible, otherwise result will differ from expected (the functions will return null)

        // returns currently processed Target of an aura
        // Return value does not need to be null-checked, the only situation this will (always)
        // return null is when the call happens in an unsupported hook, in other cases, it is always valid
        public Unit GetTarget()
        {
            switch ((AuraScriptHookType)CurrentScriptState)
            {
                case AuraScriptHookType.EffectApply:
                case AuraScriptHookType.EffectRemove:
                case AuraScriptHookType.EffectAfterApply:
                case AuraScriptHookType.EffectAfterRemove:
                case AuraScriptHookType.EffectPeriodic:
                case AuraScriptHookType.EffectAbsorb:
                case AuraScriptHookType.EffectAfterAbsorb:
                case AuraScriptHookType.EffectManaShield:
                case AuraScriptHookType.EffectAfterManaShield:
                case AuraScriptHookType.EffectSplit:
                case AuraScriptHookType.CheckProc:
                case AuraScriptHookType.CheckEffectProc:
                case AuraScriptHookType.PrepareProc:
                case AuraScriptHookType.Proc:
                case AuraScriptHookType.AfterProc:
                case AuraScriptHookType.EffectProc:
                case AuraScriptHookType.EffectAfterProc:
                case AuraScriptHookType.EnterLeaveCombat:
                    return _auraApplication.GetTarget();
                default:
                    Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}` AuraScript.GetTarget called in a hook in which the call won't have effect!", ScriptName, ScriptSpellId);

                    break;
            }

            return null;
        }

        // returns AuraApplication object of currently processed Target
        public AuraApplication GetTargetApplication()
        {
            return _auraApplication;
        }

        public Difficulty GetCastDifficulty()
        {
            return GetAura().GetCastDifficulty();
        }

        // returns owner if it's dynobj, null otherwise
        public DynamicObject GetDynobjOwner()
        {
            return _aura.GetDynobjOwner();
        }

        // returns Type of the aura, may be dynobj owned aura or unit owned aura
        public AuraObjectType GetAuraType()
        {
            return _aura.GetAuraType();
        }

        // sets duration to maxduration
        public void RefreshDuration()
        {
            _aura.RefreshDuration();
        }

        public long GetApplyTime()
        {
            return _aura.GetApplyTime();
        }

        public int CalcMaxDuration()
        {
            return _aura.CalcMaxDuration();
        }

        // permament - has infinite duration
        public bool IsPermanent()
        {
            return _aura.IsPermanent();
        }

        // charges manipulation - 0 - not charged aura
        public byte GetCharges()
        {
            return _aura.GetCharges();
        }

        public void SetCharges(byte charges)
        {
            _aura.SetCharges(charges);
        }

        public byte CalcMaxCharges()
        {
            return _aura.CalcMaxCharges();
        }

        public bool ModCharges(sbyte num, AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            return _aura.ModCharges(num, removeMode);
        }

        // returns true if last charge dropped
        public bool DropCharge(AuraRemoveMode removeMode = AuraRemoveMode.Default)
        {
            return _aura.DropCharge(removeMode);
        }

        public void SetStackAmount(byte num)
        {
            _aura.SetStackAmount(num);
        }

        // passive - "working in background", not saved, not removed by immunities, not seen by player
        public bool IsPassive()
        {
            return _aura.IsPassive();
        }

        // death persistent - not removed on death
        public bool IsDeathPersistent()
        {
            return _aura.IsDeathPersistent();
        }

        // check if aura has effect of given aura Type
        public bool HasEffectType(AuraType type)
        {
            return _aura.HasEffectType(type);
        }
    }
}