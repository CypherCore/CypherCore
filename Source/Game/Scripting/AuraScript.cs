using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Scripting
{
    public class AuraScript : BaseSpellScript, IAuraScript
    {
        public AuraScript()
        {
            m_aura = null;
            m_auraApplication = null;
            m_defaultActionPrevented = false;
        }

        public bool _Load(Aura aura)
        {
            m_aura = aura;
            _PrepareScriptCall((AuraScriptHookType)SpellScriptState.Loading, null);
            bool load = Load();
            _FinishScriptCall();
            return load;
        }
        public void _PrepareScriptCall(AuraScriptHookType hookType, AuraApplication aurApp = null)
        {
            m_scriptStates.Push(new ScriptStateStore(m_currentScriptState, m_auraApplication, m_defaultActionPrevented));
            m_currentScriptState = (byte)hookType;
            m_defaultActionPrevented = false;
            m_auraApplication = aurApp;
        }
        public void _FinishScriptCall()
        {
            ScriptStateStore stateStore = m_scriptStates.Peek();
            m_currentScriptState = stateStore._currentScriptState;
            m_auraApplication = stateStore._auraApplication;
            m_defaultActionPrevented = stateStore._defaultActionPrevented;
            m_scriptStates.Pop();
        }
        public bool _IsDefaultActionPrevented()
        {
            switch ((AuraScriptHookType)m_currentScriptState)
            {
                case AuraScriptHookType.EffectApply:
                case AuraScriptHookType.EffectRemove:
                case AuraScriptHookType.EffectPeriodic:
                case AuraScriptHookType.EffectAbsorb:
                case AuraScriptHookType.EffectSplit:
                case AuraScriptHookType.PrepareProc:
                case AuraScriptHookType.Proc:
                case AuraScriptHookType.EffectProc:
                    return m_defaultActionPrevented;
                default:
                    throw new Exception("AuraScript._IsDefaultActionPrevented is called in a wrong place");
            }
        }

        Aura m_aura;
        AuraApplication m_auraApplication;
        bool m_defaultActionPrevented;

        class ScriptStateStore
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
        Stack<ScriptStateStore> m_scriptStates = new();



        // prevents default action of a hook from being executed (works only while called in a hook which default action can be prevented)
        public void PreventDefaultAction()
        {
            switch ((AuraScriptHookType)m_currentScriptState)
            {
                case AuraScriptHookType.EffectApply:
                case AuraScriptHookType.EffectRemove:
                case AuraScriptHookType.EffectPeriodic:
                case AuraScriptHookType.EffectAbsorb:
                case AuraScriptHookType.EffectSplit:
                case AuraScriptHookType.PrepareProc:
                case AuraScriptHookType.EffectProc:
                    m_defaultActionPrevented = true;
                    break;
                default:
                    Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}` AuraScript.PreventDefaultAction called in a hook in which the call won't have effect!", m_scriptName, m_scriptSpellId);
                    break;
            }
        }

        // AuraScript interface - functions which are redirecting to Aura class

        // returns proto of the spell
        public SpellInfo GetSpellInfo() { return m_aura.GetSpellInfo(); }

        public SpellEffectInfo GetEffectInfo(uint effIndex)
        {
            return m_aura.GetSpellInfo().GetEffect(effIndex);
        }

        // returns spellid of the spell
        public uint GetId() { return m_aura.GetId(); }

        // returns guid of object which casted the aura (m_originalCaster of the Spell class)
        public ObjectGuid GetCasterGUID() { return m_aura.GetCasterGUID(); }
        // returns unit which casted the aura or null if not avalible (caster logged out for example)
        public Unit GetCaster()
        {
            WorldObject caster = m_aura.GetCaster();
            if (caster != null)
                return caster.ToUnit();

            return null;
        }
        // returns gameobject which cast the aura or NULL if not available
        public GameObject GetGObjCaster()
        {
            WorldObject caster = m_aura.GetCaster();
            if (caster != null)
                return caster.ToGameObject();

            return null;
        }
        // returns object on which aura was casted, target for non-area auras, area aura source for area auras
        public WorldObject GetOwner() { return m_aura.GetOwner(); }
        // returns owner if it's unit or unit derived object, null otherwise (only for persistent area auras null is returned)
        public Unit GetUnitOwner() { return m_aura.GetUnitOwner(); }
        // returns owner if it's dynobj, null otherwise
        DynamicObject GetDynobjOwner() { return m_aura.GetDynobjOwner(); }

        // removes aura with remove mode (see AuraRemoveMode enum)
        public void Remove(AuraRemoveMode removeMode = 0) { m_aura.Remove(removeMode); }
        // returns aura object of script
        public Aura GetAura() { return m_aura; }

        // returns type of the aura, may be dynobj owned aura or unit owned aura
        AuraObjectType GetAuraType() { return m_aura.GetAuraType(); }

        // aura duration manipulation - when duration goes to 0 aura is removed
        public int GetDuration() { return m_aura.GetDuration(); }
        public void SetDuration(int duration, bool withMods = false) { m_aura.SetDuration(duration, withMods); }
        // sets duration to maxduration
        void RefreshDuration() { m_aura.RefreshDuration(); }
        long GetApplyTime() { return m_aura.GetApplyTime(); }
        public int GetMaxDuration() { return m_aura.GetMaxDuration(); }
        public void SetMaxDuration(int duration) { m_aura.SetMaxDuration(duration); }
        int CalcMaxDuration() { return m_aura.CalcMaxDuration(); }
        // expired - duration just went to 0
        public bool IsExpired() { return m_aura.IsExpired(); }
        // permament - has infinite duration
        bool IsPermanent() { return m_aura.IsPermanent(); }

        // charges manipulation - 0 - not charged aura
        byte GetCharges() { return m_aura.GetCharges(); }
        void SetCharges(byte charges) { m_aura.SetCharges(charges); }
        byte CalcMaxCharges() { return m_aura.CalcMaxCharges(); }
        bool ModCharges(sbyte num, AuraRemoveMode removeMode = AuraRemoveMode.Default) { return m_aura.ModCharges(num, removeMode); }
        // returns true if last charge dropped
        bool DropCharge(AuraRemoveMode removeMode = AuraRemoveMode.Default) { return m_aura.DropCharge(removeMode); }

        // stack amount manipulation
        public byte GetStackAmount() { return m_aura.GetStackAmount(); }
        void SetStackAmount(byte num) { m_aura.SetStackAmount(num); }
        public bool ModStackAmount(int num, AuraRemoveMode removeMode = AuraRemoveMode.Default) { return m_aura.ModStackAmount(num, removeMode); }

        // passive - "working in background", not saved, not removed by immunities, not seen by player
        bool IsPassive() { return m_aura.IsPassive(); }
        // death persistent - not removed on death
        bool IsDeathPersistent() { return m_aura.IsDeathPersistent(); }

        // check if aura has effect of given effindex
        public bool HasEffect(byte effIndex) { return m_aura.HasEffect(effIndex); }
        // returns aura effect of given effect index or null
        public AuraEffect GetEffect(byte effIndex) { return m_aura.GetEffect(effIndex); }

        // check if aura has effect of given aura type
        bool HasEffectType(AuraType type)
        {
            return m_aura.HasEffectType(type);
        }

        // AuraScript interface - functions which are redirecting to AuraApplication class
        // Do not call these in hooks in which AuraApplication is not avalible, otherwise result will differ from expected (the functions will return null)

        // returns currently processed target of an aura
        // Return value does not need to be null-checked, the only situation this will (always)
        // return null is when the call happens in an unsupported hook, in other cases, it is always valid
        public Unit GetTarget()
        {
            switch ((AuraScriptHookType)m_currentScriptState)
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
                    return m_auraApplication.GetTarget();
                default:
                    Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}` AuraScript.GetTarget called in a hook in which the call won't have effect!", m_scriptName, m_scriptSpellId);
                    break;
            }

            return null;
        }
        // returns AuraApplication object of currently processed target
        public AuraApplication GetTargetApplication() { return m_auraApplication; }

        public Difficulty GetCastDifficulty()
        {
            return GetAura().GetCastDifficulty();
        }
    }
}
