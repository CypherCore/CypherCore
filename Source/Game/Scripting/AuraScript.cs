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
        // internal use classes & functions
        // DO NOT OVERRIDE THESE IN SCRIPTS
        public delegate void AuraEffectApplicationModeDelegate(AuraEffect aura, AuraEffectHandleModes auraMode);
        public delegate void AuraEffectPeriodicDelegate(AuraEffect aura);
        public delegate void AuraEffectUpdatePeriodicDelegate(AuraEffect aura);
        public delegate void AuraEffectCalcAmountDelegate(AuraEffect aura, ref int amount, ref bool canBeRecalculated);
        public delegate void AuraEffectCalcPeriodicDelegate(AuraEffect aura, ref bool isPeriodic, ref int amplitude);
        public delegate void AuraEffectCalcSpellModDelegate(AuraEffect aura, ref SpellModifier spellMod);
        public delegate void AuraEffectCalcCritChanceFnType(AuraEffect aura, Unit victim, ref float critChance);
        public delegate void AuraEffectAbsorbDelegate(AuraEffect aura, DamageInfo damageInfo, ref uint absorbAmount);
        public delegate void AuraEffectAbsorbHealDelegate(AuraEffect aura, HealInfo healInfo, ref uint absorbAmount);
        public delegate void AuraEffectSplitDelegate(AuraEffect aura, DamageInfo damageInfo, uint splitAmount);
        public delegate bool AuraCheckProcDelegate(ProcEventInfo info);
        public delegate bool AuraCheckEffectProcDelegate(AuraEffect aura, ProcEventInfo info);
        public delegate void AuraProcDelegate(ProcEventInfo info);
        public delegate void AuraEffectProcDelegate(AuraEffect aura, ProcEventInfo info);
        public delegate void AuraEnterLeaveCombatFnType(bool isNowInCombat);


        public class EffectBase : EffectHook
        {
            public EffectBase(uint _effIndex, AuraType _effName)
                : base(_effIndex)
            {
                effAurName = _effName;
            }

            public override bool CheckEffect(SpellInfo spellEntry, uint effIndex)
            {
                if (spellEntry.GetEffects().Count <= effIndex)
                    return false;

                SpellEffectInfo spellEffectInfo = spellEntry.GetEffect(effIndex);
                if (spellEffectInfo.ApplyAuraName == 0 && effAurName == 0)
                    return true;

                if (spellEffectInfo.ApplyAuraName == 0)
                    return false;

                return effAurName == AuraType.Any || spellEffectInfo.ApplyAuraName == effAurName;
            }

            AuraType effAurName;
        }

        public class EffectPeriodicHandler : EffectBase
        {
            public EffectPeriodicHandler(AuraEffectPeriodicDelegate _pEffectHandlerScript, byte _effIndex, AuraType _effName)
                : base(_effIndex, _effName)
            {
                pEffectHandlerScript = _pEffectHandlerScript;
            }
            public void Call(AuraEffect _aurEff)
            {
                pEffectHandlerScript(_aurEff);
            }
            AuraEffectPeriodicDelegate pEffectHandlerScript;
        }
        public class EffectUpdatePeriodicHandler : EffectBase
        {
            public EffectUpdatePeriodicHandler(AuraEffectUpdatePeriodicDelegate _pEffectHandlerScript, byte _effIndex, AuraType _effName)
                : base(_effIndex, _effName)
            {
                pEffectHandlerScript = _pEffectHandlerScript;
            }
            public void Call(AuraEffect aurEff) { pEffectHandlerScript(aurEff); }

            AuraEffectUpdatePeriodicDelegate pEffectHandlerScript;
        }
        public class EffectCalcAmountHandler : EffectBase
        {
            public EffectCalcAmountHandler(AuraEffectCalcAmountDelegate _pEffectHandlerScript, uint _effIndex, AuraType _effName)
                : base(_effIndex, _effName)
            {
                pEffectHandlerScript = _pEffectHandlerScript;
            }
            public void Call(AuraEffect aurEff, ref int amount, ref bool canBeRecalculated)
            {
                pEffectHandlerScript(aurEff, ref amount, ref canBeRecalculated);
            }

            public AuraEffectCalcAmountDelegate pEffectHandlerScript;
        }
        public class EffectCalcPeriodicHandler : EffectBase
        {
            public EffectCalcPeriodicHandler(AuraEffectCalcPeriodicDelegate _pEffectHandlerScript, byte _effIndex, AuraType _effName)
                : base(_effIndex, _effName)
            {
                pEffectHandlerScript = _pEffectHandlerScript;
            }
            public void Call(AuraEffect aurEff, ref bool isPeriodic, ref int periodicTimer)
            {
                pEffectHandlerScript(aurEff, ref isPeriodic, ref periodicTimer);
            }

            AuraEffectCalcPeriodicDelegate pEffectHandlerScript;
        }
        public class EffectCalcSpellModHandler : EffectBase
        {
            public EffectCalcSpellModHandler(AuraEffectCalcSpellModDelegate _pEffectHandlerScript, byte _effIndex, AuraType _effName)
                : base(_effIndex, _effName)
            {
                pEffectHandlerScript = _pEffectHandlerScript;
            }
            public void Call(AuraEffect aurEff, ref SpellModifier spellMod)
            {
                pEffectHandlerScript(aurEff, ref spellMod);
            }

            AuraEffectCalcSpellModDelegate pEffectHandlerScript;
        }
        public class EffectCalcCritChanceHandler : EffectBase
        {
            public EffectCalcCritChanceHandler(AuraEffectCalcCritChanceFnType effectHandlerScript, byte effIndex, AuraType effName) : base(effIndex, effName)
            {
                _effectHandlerScript = effectHandlerScript;
            }
            public void Call(AuraEffect aurEff, Unit victim, ref float critChance)
            {
                _effectHandlerScript(aurEff, victim, ref critChance);
            }

            AuraEffectCalcCritChanceFnType _effectHandlerScript;
        }
        public class EffectApplyHandler : EffectBase
        {
            public EffectApplyHandler(AuraEffectApplicationModeDelegate _pEffectHandlerScript, byte _effIndex, AuraType _effName, AuraEffectHandleModes _mode)
                : base(_effIndex, _effName)
            {
                pEffectHandlerScript = _pEffectHandlerScript;
                mode = _mode;
            }
            public void Call(AuraEffect _aurEff, AuraEffectHandleModes _mode)
            {
                if (Convert.ToBoolean(_mode & mode))
                    pEffectHandlerScript(_aurEff, _mode);
            }

            AuraEffectApplicationModeDelegate pEffectHandlerScript;
            AuraEffectHandleModes mode;
        }
        public class EffectAbsorbHandler : EffectBase
        {
            public EffectAbsorbHandler(AuraEffectAbsorbDelegate _pEffectHandlerScript, byte _effIndex, bool overKill = false)
                : base(_effIndex, overKill ? AuraType.SchoolAbsorbOverkill : AuraType.SchoolAbsorb)
            {
                pEffectHandlerScript = _pEffectHandlerScript;
            }

            public void Call(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
            {
                pEffectHandlerScript(aurEff, dmgInfo, ref absorbAmount);
            }

            AuraEffectAbsorbDelegate pEffectHandlerScript;
        }
        public class EffectAbsorbHealHandler : EffectBase
        {
            public EffectAbsorbHealHandler(AuraEffectAbsorbHealDelegate _pEffectHandlerScript, byte _effIndex)
                : base(_effIndex, AuraType.SchoolHealAbsorb)
            {
                pEffectHandlerScript = _pEffectHandlerScript;
            }

            public void Call(AuraEffect aurEff, HealInfo healInfo, ref uint absorbAmount)
            {
                pEffectHandlerScript(aurEff, healInfo, ref absorbAmount);
            }

            AuraEffectAbsorbHealDelegate pEffectHandlerScript;
        }
        public class EffectManaShieldHandler : EffectBase
        {
            public EffectManaShieldHandler(AuraEffectAbsorbDelegate _pEffectHandlerScript, byte _effIndex)
                : base(_effIndex, AuraType.ManaShield)
            {
                pEffectHandlerScript = _pEffectHandlerScript;
            }
            public void Call(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
            {
                pEffectHandlerScript(aurEff, dmgInfo, ref absorbAmount);
            }

            AuraEffectAbsorbDelegate pEffectHandlerScript;
        }
        public class EffectSplitHandler : EffectBase
        {
            public EffectSplitHandler(AuraEffectSplitDelegate _pEffectHandlerScript, byte _effIndex)
                : base(_effIndex, AuraType.SplitDamagePct)
            {
                pEffectHandlerScript = _pEffectHandlerScript;
            }
            public void Call(AuraEffect aurEff, DamageInfo dmgInfo, uint splitAmount)
            {
                pEffectHandlerScript(aurEff, dmgInfo, splitAmount);
            }

            AuraEffectSplitDelegate pEffectHandlerScript;
        }
        public class CheckProcHandler
        {
            public CheckProcHandler(AuraCheckProcDelegate handlerScript)
            {
                _HandlerScript = handlerScript;
            }
            public bool Call(ProcEventInfo eventInfo)
            {
                return _HandlerScript(eventInfo);
            }

            AuraCheckProcDelegate _HandlerScript;
        }
        public class CheckEffectProcHandler : EffectBase
        {
            public CheckEffectProcHandler(AuraCheckEffectProcDelegate handlerScript, uint effIndex, AuraType effName) : base(effIndex, effName)
            {
                _HandlerScript = handlerScript;
            }

            public bool Call(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                return _HandlerScript(aurEff, eventInfo);
            }

            AuraCheckEffectProcDelegate _HandlerScript;
        }
        public class AuraProcHandler
        {
            public AuraProcHandler(AuraProcDelegate handlerScript)
            {
                _HandlerScript = handlerScript;
            }

            public void Call(ProcEventInfo eventInfo)
            {
                _HandlerScript(eventInfo);
            }

            AuraProcDelegate _HandlerScript;
        }
        public class EffectProcHandler : EffectBase
        {
            public EffectProcHandler(AuraEffectProcDelegate effectHandlerScript, byte effIndex, AuraType effName) : base(effIndex, effName)
            {
                _EffectHandlerScript = effectHandlerScript;
            }
            public void Call(AuraEffect aurEff, ProcEventInfo eventInfo)
            {
                _EffectHandlerScript(aurEff, eventInfo);
            }

            AuraEffectProcDelegate _EffectHandlerScript;
        }
        public class EnterLeaveCombatHandler
        {
            public EnterLeaveCombatHandler(AuraEnterLeaveCombatFnType handlerScript)
            {
                _handlerScript = handlerScript;
            }
            public void Call(bool isNowInCombat)
            {
                _handlerScript(isNowInCombat);
            }

            AuraEnterLeaveCombatFnType _handlerScript;
        }

        public AuraScript()
        {
            m_aura = null;
            m_auraApplication = null;
            m_defaultActionPrevented = false;
        }

        public override bool _Validate(SpellInfo entry)
        {
            foreach (var eff in OnEffectApply)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectApply` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in OnEffectRemove)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectRemove` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in AfterEffectApply)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `AfterEffectApply` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in AfterEffectRemove)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `AfterEffectRemove` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in OnEffectPeriodic)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectPeriodic` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in OnEffectUpdatePeriodic)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectUpdatePeriodic` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in DoEffectCalcAmount)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `DoEffectCalcAmount` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in DoEffectCalcPeriodic)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `DoEffectCalcPeriodic` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in DoEffectCalcSpellMod)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `DoEffectCalcSpellMod` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in DoEffectCalcCritChance)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, $"Spell `{entry.Id}` Effect `{eff}` of script `{m_scriptName}` did not match dbc effect data - handler bound to hook `DoEffectCalcCritChance` of AuraScript won't be executed");

            foreach (var eff in OnEffectAbsorb)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectAbsorb` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in AfterEffectAbsorb)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `AfterEffectAbsorb` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in OnEffectManaShield)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectManaShield` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in AfterEffectManaShield)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `AfterEffectManaShield` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in OnEffectSplit)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectSplit` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var _ in DoCheckProc)
                if (!entry.HasEffect(SpellEffectName.ApplyAura) && !entry.HasAreaAuraEffect())
                    Log.outError(LogFilter.Scripts, "Spell `{0}` of script `{1}` does not have apply aura effect - handler bound to hook `DoCheckProc` of AuraScript won't be executed", entry.Id, m_scriptName);

            foreach (var eff in DoCheckEffectProc)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `DoCheckEffectProc` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var _ in DoPrepareProc)
                if (!entry.HasEffect(SpellEffectName.ApplyAura) && !entry.HasAreaAuraEffect())
                    Log.outError(LogFilter.Scripts, "Spell `{0}` of script `{1}` does not have apply aura effect - handler bound to hook `DoPrepareProc` of AuraScript won't be executed", entry.Id, m_scriptName);

            foreach (var _ in OnProc)
                if (!entry.HasEffect(SpellEffectName.ApplyAura) && !entry.HasAreaAuraEffect())
                    Log.outError(LogFilter.Scripts, "Spell `{0}` of script `{1}` does not have apply aura effect - handler bound to hook `OnProc` of AuraScript won't be executed", entry.Id, m_scriptName);

            foreach (var _ in AfterProc)
                if (!entry.HasEffect(SpellEffectName.ApplyAura) && !entry.HasAreaAuraEffect())
                    Log.outError(LogFilter.Scripts, "Spell `{0}` of script `{1}` does not have apply aura effect - handler bound to hook `AfterProc` of AuraScript won't be executed", entry.Id, m_scriptName);

            foreach (var eff in OnEffectProc)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectProc` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in AfterEffectProc)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `AfterEffectProc` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            return base._Validate(entry);
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

        // executed when aura effect is applied with specified mode to target
        // should be used when effect handler preventing/replacing is needed, do not use this hook for triggering spellcasts/removing auras etc - may be unsafe
        // example: OnEffectApply += AuraEffectApplyFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier, AuraEffectHandleModes);
        // where function is: void function (AuraEffect aurEff, AuraEffectHandleModes mode);
        public List<EffectApplyHandler> OnEffectApply = new();

        // executed after aura effect is applied with specified mode to target
        // example: AfterEffectApply += AuraEffectApplyFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier, AuraEffectHandleModes);
        // where function is: void function (AuraEffect aurEff, AuraEffectHandleModes mode);
        public List<EffectApplyHandler> AfterEffectApply = new();

        // executed after aura effect is removed with specified mode from target
        // should be used when effect handler preventing/replacing is needed, do not use this hook for triggering spellcasts/removing auras etc - may be unsafe
        // example: OnEffectRemove += AuraEffectRemoveFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier, AuraEffectHandleModes);
        // where function is: void function (AuraEffect aurEff, AuraEffectHandleModes mode);
        public List<EffectApplyHandler> OnEffectRemove = new();

        // executed when aura effect is removed with specified mode from target
        // example: AfterEffectRemove += AuraEffectRemoveFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier, AuraEffectHandleModes);
        // where function is: void function (AuraEffect aurEff, AuraEffectHandleModes mode);
        public List<EffectApplyHandler> AfterEffectRemove = new();

        // executed when periodic aura effect ticks on target
        // example: OnEffectPeriodic += AuraEffectPeriodicFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff);
        public List<EffectPeriodicHandler> OnEffectPeriodic = new();

        // executed when periodic aura effect is updated
        // example: OnEffectUpdatePeriodic += AuraEffectUpdatePeriodicFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff);
        public List<EffectUpdatePeriodicHandler> OnEffectUpdatePeriodic = new();

        // executed when aura effect calculates amount
        // example: DoEffectCalcAmount += AuraEffectCalcAmounFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff, int& amount, bool& canBeRecalculated);
        public List<EffectCalcAmountHandler> DoEffectCalcAmount = new();

        // executed when aura effect calculates periodic data
        // example: DoEffectCalcPeriodic += AuraEffectCalcPeriodicFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff, bool& isPeriodic, int& amplitude);
        public List<EffectCalcPeriodicHandler> DoEffectCalcPeriodic = new();

        // executed when aura effect calculates spellmod
        // example: DoEffectCalcSpellMod += AuraEffectCalcSpellModFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff, SpellModifier& spellMod);
        public List<EffectCalcSpellModHandler> DoEffectCalcSpellMod = new();

        // executed when aura effect calculates crit chance for dots and hots
        // example: DoEffectCalcCritChance += AuraEffectCalcCritChanceFn(class::function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect const* aurEff, Unit* victim, float& critChance);
        public List<EffectCalcCritChanceHandler> DoEffectCalcCritChance = new();

        // executed when absorb aura effect is going to reduce damage
        // example: OnEffectAbsorb += AuraEffectAbsorbFn(class.function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect aurEff, DamageInfo& dmgInfo, uint& absorbAmount);
        public List<EffectAbsorbHandler> OnEffectAbsorb = new();

        // executed after absorb aura effect reduced damage to target - absorbAmount is real amount absorbed by aura
        // example: AfterEffectAbsorb += AuraEffectAbsorbFn(class.function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect aurEff, DamageInfo& dmgInfo, uint& absorbAmount);
        public List<EffectAbsorbHandler> AfterEffectAbsorb = new();

        // executed when absorb aura effect is going to reduce damage
        // example: OnEffectAbsorbHeal += AuraEffectAbsorbHealFn(class::function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect const* aurEff, HealInfo& healInfo, uint32& absorbAmount);
        public List<EffectAbsorbHealHandler> OnEffectAbsorbHeal = new();

        // executed after absorb aura effect reduced heal to target - absorbAmount is real amount absorbed by aura
        // example: AfterEffectAbsorbHeal += AuraEffectAbsorbHealFn(class::function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect* aurEff, HealInfo& healInfo, uint32& absorbAmount);
        public List<EffectAbsorbHealHandler> AfterEffectAbsorbHeal = new();

        // executed when mana shield aura effect is going to reduce damage
        // example: OnEffectManaShield += AuraEffectAbsorbFn(class.function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect aurEff, DamageInfo& dmgInfo, uint& absorbAmount);
        public List<EffectManaShieldHandler> OnEffectManaShield = new();

        // executed after mana shield aura effect reduced damage to target - absorbAmount is real amount absorbed by aura
        // example: AfterEffectManaShield += AuraEffectAbsorbFn(class.function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect aurEff, DamageInfo& dmgInfo, uint& absorbAmount);
        public List<EffectManaShieldHandler> AfterEffectManaShield = new();

        // executed when the caster of some spell with split dmg aura gets damaged through it
        // example: OnEffectSplit += AuraEffectSplitFn(class.function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect aurEff, DamageInfo& dmgInfo, uint& splitAmount);
        public List<EffectSplitHandler> OnEffectSplit = new();

        // executed when aura checks if it can proc
        // example: DoCheckProc += AuraCheckProcFn(class.function);
        // where function is: bool function (ProcEventInfo& eventInfo);
        public List<CheckProcHandler> DoCheckProc = new();

        // executed when aura effect checks if it can proc the aura
        // example: DoCheckEffectProc += AuraCheckEffectProcFn(class::function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is bool function (AuraEffect const* aurEff, ProcEventInfo& eventInfo);
        public List<CheckEffectProcHandler> DoCheckEffectProc = new();

        // executed before aura procs (possibility to prevent charge drop/cooldown)
        // example: DoPrepareProc += AuraProcFn(class.function);
        // where function is: void function (ProcEventInfo& eventInfo);
        public List<AuraProcHandler> DoPrepareProc = new();

        // executed when aura procs
        // example: OnProc += AuraProcFn(class.function);
        // where function is: void function (ProcEventInfo& eventInfo);
        public List<AuraProcHandler> OnProc = new();

        // executed after aura proced
        // example: AfterProc += AuraProcFn(class.function);
        // where function is: void function (ProcEventInfo& eventInfo);
        public List<AuraProcHandler> AfterProc = new();

        // executed when aura effect procs
        // example: OnEffectProc += AuraEffectProcFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff, ProcEventInfo& procInfo);
        public List<EffectProcHandler> OnEffectProc = new();

        // executed after aura effect proced
        // example: AfterEffectProc += AuraEffectProcFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff, ProcEventInfo& procInfo);
        public List<EffectProcHandler> AfterEffectProc = new();

        // executed when target enters or leaves combat
        // example: OnEnterLeaveCombat += AuraEnterLeaveCombatFn(class::function)
        // where function is: void function (bool isNowInCombat);
        public List<EnterLeaveCombatHandler> OnEnterLeaveCombat = new();

        // AuraScript interface - hook/effect execution manipulators

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
