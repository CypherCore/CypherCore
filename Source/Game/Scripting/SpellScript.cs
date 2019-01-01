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
using Game.Entities;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Scripting
{
    // helper class from which SpellScript and SpellAura derive, use these classes instead
    public class BaseSpellScript
    {
        // internal use classes & functions
        // DO NOT OVERRIDE THESE IN SCRIPTS
        public BaseSpellScript()
        {
            m_currentScriptState = (byte)SpellScriptState.None;
        }

        public virtual bool _Validate(SpellInfo entry)
        {
            if (!Validate(entry))
            {
                Log.outError(LogFilter.Scripts, "Spell `{0}` did not pass Validate() function of script `{1}` - script will be not added to the spell", entry.Id, m_scriptName);
                return false;
            }
            return true;
        }

        public bool ValidateSpellInfo(params uint[] spellIds)
        {
            bool allValid = true;
            foreach (uint spellId in spellIds)
            {
                if (!Global.SpellMgr.HasSpellInfo(spellId))
                {
                    Log.outError(LogFilter.Scripts, "BaseSpellScript.ValidateSpellInfo: Spell {0} does not exist.", spellId);
                    allValid = false;
                }
            }

            return allValid;
        }

        public void _Register()
        {
            m_currentScriptState = (byte)SpellScriptState.Registration;
            Register();
            m_currentScriptState = (byte)SpellScriptState.None;
        }
        public void _Unload()
        {
            m_currentScriptState = (byte)SpellScriptState.Unloading;
            Unload();
            m_currentScriptState = (byte)SpellScriptState.None;
        }

        public void _Init(string scriptname, uint spellId)
        {
            m_currentScriptState = (byte)SpellScriptState.None;
            m_scriptName = scriptname;
            m_scriptSpellId = spellId;
        }
        public string _GetScriptName()
        {
            return m_scriptName;
        }

        public abstract class EffectHook
        {
            protected EffectHook(uint effIndex)
            {
                // effect index must be in range <0;2>, allow use of special effindexes
                Cypher.Assert(_effIndex == SpellConst.EffectAll || _effIndex == SpellConst.EffectFirstFound || _effIndex < SpellConst.MaxEffects);
                _effIndex = effIndex;
            }

            public uint GetAffectedEffectsMask(SpellInfo spellEntry)
            {
                uint mask = 0;
                if ((_effIndex == SpellConst.EffectAll) || (_effIndex == SpellConst.EffectFirstFound))
                {
                    for (byte i = 0; i < SpellConst.MaxEffects; ++i)
                    {
                        if ((_effIndex == SpellConst.EffectFirstFound) && mask != 0)
                            return mask;
                        if (CheckEffect(spellEntry, i))
                            mask |= (1u << i);
                    }
                }
                else
                {
                    if (CheckEffect(spellEntry, _effIndex))
                        mask |= (1u << (int)_effIndex);
                }
                return mask;
            }

            public bool IsEffectAffected(SpellInfo spellEntry, uint effIndex)
            {
                return Convert.ToBoolean(GetAffectedEffectsMask(spellEntry) & (1 << (int)effIndex));
            }

            public abstract bool CheckEffect(SpellInfo spellEntry, uint effIndex);

            uint _effIndex;
        }

        public byte m_currentScriptState { get; set; }
        public string m_scriptName { get; set; }
        public uint m_scriptSpellId { get; set; }

        //
        // SpellScript/AuraScript interface base
        // these functions are safe to override, see notes below for usage instructions
        //
        // Function in which handler functions are registered, must be implemented in script
        public virtual void Register() { }
        // Function called on server startup, if returns false script won't be used in core
        // use for: dbc/template data presence/correctness checks
        public virtual bool Validate(SpellInfo spellEntry) { return true; }
        // Function called when script is created, if returns false script will be unloaded afterwards
        // use for: initializing local script variables (DO NOT USE CONSTRUCTOR FOR THIS PURPOSE!)
        public virtual bool Load() { return true; }
        // Function called when script is destroyed
        // use for: deallocating memory allocated by script
        public virtual void Unload() { }
    }

    public class SpellScript : BaseSpellScript
    {
        // internal use classes & functions
        // DO NOT OVERRIDE THESE IN SCRIPTS
        public delegate SpellCastResult SpellCheckCastFnType();
        public delegate void SpellEffectFnType(uint index);
        public delegate void SpellBeforeHitFnType(SpellMissInfo missInfo);
        public delegate void SpellHitFnType();
        public delegate void SpellCastFnType();
        public delegate void SpellObjectAreaTargetSelectFnType(List<WorldObject> targets);
        public delegate void SpellObjectTargetSelectFnType(ref WorldObject targets);
        public delegate void SpellDestinationTargetSelectFnType(ref SpellDestination dest);

        public class CastHandler
        {
            public CastHandler(SpellCastFnType _pCastHandlerScript) { pCastHandlerScript = _pCastHandlerScript; }

            public void Call()
            {
                pCastHandlerScript();
            }

            SpellCastFnType pCastHandlerScript;
        }

        public class CheckCastHandler
        {
            public CheckCastHandler(SpellCheckCastFnType checkCastHandlerScript)
            {
                _checkCastHandlerScript = checkCastHandlerScript;
            }

            public SpellCastResult Call()
            {
                return _checkCastHandlerScript();
            }

            SpellCheckCastFnType _checkCastHandlerScript;
        }

        public class EffectHandler : EffectHook
        {
            public EffectHandler(SpellEffectFnType pEffectHandlerScript, uint effIndex, SpellEffectName effName) : base(effIndex)
            {
                _pEffectHandlerScript = pEffectHandlerScript;
                _effName = effName;
            }

            public override bool CheckEffect(SpellInfo spellEntry, uint effIndex)
            {
                SpellEffectInfo effect = spellEntry.GetEffect(effIndex);
                if (effect == null)
                    return false;

                if (effect.Effect == 0 && _effName == 0)
                    return true;
                if (effect.Effect == 0)
                    return false;
                return (_effName == SpellEffectName.Any) || (effect.Effect == _effName);
            }

            public void Call(uint effIndex)
            {
                _pEffectHandlerScript(effIndex);
            }

            SpellEffectName _effName;
            SpellEffectFnType _pEffectHandlerScript;
        }

        public class BeforeHitHandler
        {
            public BeforeHitHandler(SpellBeforeHitFnType pBeforeHitHandlerScript)
            {
                _pBeforeHitHandlerScript = pBeforeHitHandlerScript;
            }

            public void Call(SpellMissInfo missInfo)
            {
                _pBeforeHitHandlerScript(missInfo);
            }

            SpellBeforeHitFnType _pBeforeHitHandlerScript;
        }

        public class HitHandler
        {
            public HitHandler(SpellHitFnType pHitHandlerScript)
            {
                _pHitHandlerScript = pHitHandlerScript;
            }

            public void Call()
            {
                _pHitHandlerScript();
            }

            SpellHitFnType _pHitHandlerScript;
        }

        public class TargetHook : EffectHook
        {
            public TargetHook(uint effectIndex, Targets targetType, bool area, bool dest = false) : base(effectIndex)
            {
                _targetType = targetType;
                _area = area;
                _dest = dest;
            }

            public override bool CheckEffect(SpellInfo spellEntry, uint effIndex)
            {
                if (_targetType == 0)
                    return false;

                SpellEffectInfo effect = spellEntry.GetEffect(effIndex);
                if (effect == null)
                    return false;

                if (effect.TargetA.GetTarget() != _targetType && effect.TargetB.GetTarget() != _targetType)
                    return false;

                SpellImplicitTargetInfo targetInfo = new SpellImplicitTargetInfo(_targetType);
                switch (targetInfo.GetSelectionCategory())
                {
                    case SpellTargetSelectionCategories.Channel: // SINGLE
                        return !_area;
                    case SpellTargetSelectionCategories.Nearby: // BOTH
                        return true;
                    case SpellTargetSelectionCategories.Cone: // AREA
                    case SpellTargetSelectionCategories.Area: // AREA
                        return _area;
                    case SpellTargetSelectionCategories.Default:
                        switch (targetInfo.GetObjectType())
                        {
                            case SpellTargetObjectTypes.Src: // EMPTY
                                return false;
                            case SpellTargetObjectTypes.Dest: // Dest
                                return _dest;
                            default:
                                switch (targetInfo.GetReferenceType())
                                {
                                    case SpellTargetReferenceTypes.Caster: // SINGLE
                                        return !_area;
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

            public Targets GetTarget() { return _targetType; }

            Targets _targetType;
            bool _area;
            bool _dest;
        }

        public class ObjectAreaTargetSelectHandler : TargetHook
        {
            public ObjectAreaTargetSelectHandler(SpellObjectAreaTargetSelectFnType pObjectAreaTargetSelectHandlerScript, uint effIndex, Targets targetType)
                : base(effIndex, targetType, true)
            {
                _pObjectAreaTargetSelectHandlerScript = pObjectAreaTargetSelectHandlerScript;
            }

            public void Call(List<WorldObject> targets)
            {
                _pObjectAreaTargetSelectHandlerScript(targets);
            }

            SpellObjectAreaTargetSelectFnType _pObjectAreaTargetSelectHandlerScript;
        }

        public class ObjectTargetSelectHandler : TargetHook
        {
            public ObjectTargetSelectHandler(SpellObjectTargetSelectFnType _pObjectTargetSelectHandlerScript, uint _effIndex, Targets _targetType)
                : base(_effIndex, _targetType, false)
            {
                pObjectTargetSelectHandlerScript = _pObjectTargetSelectHandlerScript;
            }

            public void Call(ref WorldObject target)
            {
                pObjectTargetSelectHandlerScript(ref target);
            }

            SpellObjectTargetSelectFnType pObjectTargetSelectHandlerScript;
        }

        public class DestinationTargetSelectHandler : TargetHook
        {
            public DestinationTargetSelectHandler(SpellDestinationTargetSelectFnType _DestinationTargetSelectHandlerScript, uint _effIndex, Targets _targetType)
                : base(_effIndex, _targetType, false, true)
            {
                DestinationTargetSelectHandlerScript = _DestinationTargetSelectHandlerScript;
            }

            public void Call(ref SpellDestination target)
            {
                DestinationTargetSelectHandlerScript(ref target);
            }

            SpellDestinationTargetSelectFnType DestinationTargetSelectHandlerScript;
        }

        public override bool _Validate(SpellInfo entry)
        {
            foreach (var eff in OnEffectLaunch)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectLaunch` of SpellScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in OnEffectLaunchTarget)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectLaunchTarget` of SpellScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in OnEffectHit)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectHit` of SpellScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in OnEffectHitTarget)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectHitTarget` of SpellScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in OnEffectSuccessfulDispel)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnEffectSuccessfulDispel` of SpellScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in OnObjectAreaTargetSelect)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnObjectAreaTargetSelect` of SpellScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in OnObjectTargetSelect)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `OnObjectTargetSelect` of SpellScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            return base._Validate(entry);
        }
        public bool _Load(Spell spell)
        {
            m_spell = spell;
            _PrepareScriptCall((SpellScriptHookType)SpellScriptState.Loading);
            bool load = Load();
            _FinishScriptCall();
            return load;
        }
        public void _InitHit()
        {
            m_hitPreventEffectMask = 0;
            m_hitPreventDefaultEffectMask = 0;
        }
        public bool _IsEffectPrevented(uint effIndex) { return Convert.ToBoolean(m_hitPreventEffectMask & (1 << (int)effIndex)); }
        public bool _IsDefaultEffectPrevented(uint effIndex) { return Convert.ToBoolean(m_hitPreventDefaultEffectMask & (1 << (int)effIndex)); }
        public void _PrepareScriptCall(SpellScriptHookType hookType)
        {
            m_currentScriptState = (byte)hookType;
        }
        public void _FinishScriptCall()
        {
            m_currentScriptState = (byte)SpellScriptState.None;
        }
        public bool IsInCheckCastHook()
        {
            return m_currentScriptState == (byte)SpellScriptHookType.CheckCast;
        }
        public bool IsInTargetHook()
        {
            switch ((SpellScriptHookType)m_currentScriptState)
            {
                case SpellScriptHookType.LaunchTarget:
                case SpellScriptHookType.EffectHitTarget:
                case SpellScriptHookType.EffectSuccessfulDispel:
                case SpellScriptHookType.BeforeHit:
                case SpellScriptHookType.OnHit:
                case SpellScriptHookType.AfterHit:
                    return true;
            }
            return false;
        }
        public bool IsInHitPhase()
        {
            return (m_currentScriptState >= (byte)SpellScriptHookType.EffectHit && m_currentScriptState < (byte)SpellScriptHookType.AfterHit + 1);
        }
        public bool IsInEffectHook()
        {
            return (m_currentScriptState >= (byte)SpellScriptHookType.Launch && m_currentScriptState <= (byte)SpellScriptHookType.EffectHitTarget)
                || m_currentScriptState == (byte)SpellScriptHookType.EffectSuccessfulDispel;
        }

        Spell m_spell;
        uint m_hitPreventEffectMask;
        uint m_hitPreventDefaultEffectMask;

        // SpellScript interface
        // hooks to which you can attach your functions
        public List<CastHandler> BeforeCast = new List<CastHandler>();
        public List<CastHandler> OnCast = new List<CastHandler>();
        public List<CastHandler> AfterCast = new List<CastHandler>();

        // where function is SpellCastResult function()
        public List<CheckCastHandler> OnCheckCast = new List<CheckCastHandler>();

        // where function is void function(uint effIndex)
        public List<EffectHandler> OnEffectLaunch = new List<EffectHandler>();
        public List<EffectHandler> OnEffectLaunchTarget = new List<EffectHandler>();
        public List<EffectHandler> OnEffectHit = new List<EffectHandler>();
        public List<EffectHandler> OnEffectHitTarget = new List<EffectHandler>();
        public List<EffectHandler> OnEffectSuccessfulDispel = new List<EffectHandler>();

        public List<BeforeHitHandler> BeforeHit = new List<BeforeHitHandler>();
        public List<HitHandler> OnHit = new List<HitHandler>();
        public List<HitHandler> AfterHit = new List<HitHandler>();

        // where function is void function(List<WorldObject> targets)
        public List<ObjectAreaTargetSelectHandler> OnObjectAreaTargetSelect = new List<ObjectAreaTargetSelectHandler>();

        // where function is void function(ref WorldObject target)
        public List<ObjectTargetSelectHandler> OnObjectTargetSelect = new List<ObjectTargetSelectHandler>();

        // where function is void function(SpellDestination target)
        public List<DestinationTargetSelectHandler> OnDestinationTargetSelect = new List<DestinationTargetSelectHandler>();

        // hooks are executed in following order, at specified event of spell:
        // 1. BeforeCast - executed when spell preparation is finished (when cast bar becomes full) before cast is handled
        // 2. OnCheckCast - allows to override result of CheckCast function
        // 3a. OnObjectAreaTargetSelect - executed just before adding selected targets to final target list (for area targets)
        // 3b. OnObjectTargetSelect - executed just before adding selected target to final target list (for single unit targets)
        // 4. OnCast - executed just before spell is launched (creates missile) or executed
        // 5. AfterCast - executed after spell missile is launched and immediate spell actions are done
        // 6. OnEffectLaunch - executed just before specified effect handler call - when spell missile is launched
        // 7. OnEffectLaunchTarget - executed just before specified effect handler call - when spell missile is launched - called for each target from spell target map
        // 8. OnEffectHit - executed just before specified effect handler call - when spell missile hits dest
        // 9. BeforeHit - executed just before spell hits a target - called for each target from spell target map
        // 10. OnEffectHitTarget - executed just before specified effect handler call - called for each target from spell target map
        // 11. OnHit - executed just before spell deals damage and procs auras - when spell hits target - called for each target from spell target map
        // 12. AfterHit - executed just after spell finishes all it's jobs for target - called for each target from spell target map

        //
        // methods allowing interaction with Spell object
        //
        // methods useable during all spell handling phases
        public Unit GetCaster() { return m_spell.GetCaster(); }
        public Unit GetOriginalCaster() { return m_spell.GetOriginalCaster(); }
        public SpellInfo GetSpellInfo() { return m_spell.GetSpellInfo(); }
        public SpellValue GetSpellValue() { return m_spell.m_spellValue; }

        public SpellEffectInfo GetEffectInfo(uint effIndex)
        {
            return m_spell.GetEffect(effIndex);
        }

        // methods useable after spell is prepared
        // accessors to the explicit targets of the spell
        // explicit target - target selected by caster (player, game client, or script - DoCast(explicitTarget, ...), required for spell to be cast
        // examples:
        // -shadowstep - explicit target is the unit you want to go behind of
        // -chain heal - explicit target is the unit to be healed first
        // -holy nova/arcane explosion - explicit target = null because target you are selecting doesn't affect how spell targets are selected
        // you can determine if spell requires explicit targets by dbc columns:
        // - Targets - mask of explicit target types
        // - ImplicitTargetXX set to TARGET_XXX_TARGET_YYY, _TARGET_ here means that explicit target is used by the effect, so spell needs one too

        // returns: WorldLocation which was selected as a spell destination or null
        public WorldLocation GetExplTargetDest()
        {
            if (m_spell.m_targets.HasDst())
                return m_spell.m_targets.GetDstPos();
            return null;
        }

        public void SetExplTargetDest(WorldLocation loc)
        {
            m_spell.m_targets.SetDst(loc);
        }

        // returns: WorldObject which was selected as an explicit spell target or null if there's no target
        public WorldObject GetExplTargetWorldObject() { return m_spell.m_targets.GetObjectTarget(); }

        // returns: Unit which was selected as an explicit spell target or null if there's no target
        public Unit GetExplTargetUnit() { return m_spell.m_targets.GetUnitTarget(); }

        // returns: GameObject which was selected as an explicit spell target or null if there's no target
        GameObject GetExplTargetGObj() { return m_spell.m_targets.GetGOTarget(); }

        // returns: Item which was selected as an explicit spell target or null if there's no target
        Item GetExplTargetItem() { return m_spell.m_targets.GetItemTarget(); }

        // methods useable only during spell hit on target, or during spell launch on target:
        // returns: target of current effect if it was Unit otherwise null
        public Unit GetHitUnit()
        {
            if (!IsInTargetHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.GetHitUnit was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return null;
            }
            return m_spell.unitTarget;
        }
        // returns: target of current effect if it was Creature otherwise null
        public Creature GetHitCreature()
        {
            if (!IsInTargetHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.GetHitCreature was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return null;
            }
            if (m_spell.unitTarget != null)
                return m_spell.unitTarget.ToCreature();
            else
                return null;
        }
        // returns: target of current effect if it was Player otherwise null
        public Player GetHitPlayer()
        {
            if (!IsInTargetHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.GetHitPlayer was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return null;
            }
            if (m_spell.unitTarget != null)
                return m_spell.unitTarget.ToPlayer();
            else
                return null;
        }
        // returns: target of current effect if it was Item otherwise null
        Item GetHitItem()
        {
            if (!IsInTargetHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.GetHitItem was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return null;
            }
            return m_spell.itemTarget;
        }
        // returns: target of current effect if it was GameObject otherwise null
        public GameObject GetHitGObj()
        {
            if (!IsInTargetHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.GetHitGObj was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return null;
            }
            return m_spell.gameObjTarget;
        }
        // returns: destination of current effect
        public WorldLocation GetHitDest()
        {
            if (!IsInEffectHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.GetHitDest was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return null;
            }
            return m_spell.destTarget;
        }
        // setter/getter for for damage done by spell to target of spell hit
        // returns damage calculated before hit, and real dmg done after hit
        public int GetHitDamage()
        {
            if (!IsInTargetHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.GetHitDamage was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return 0;
            }
            return m_spell.m_damage;
        }
        public void SetHitDamage(int damage)
        {
            if (!IsInTargetHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.SetHitDamage was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return;
            }
            m_spell.m_damage = damage;
        }
        public void PreventHitDamage() { SetHitDamage(0); }
        // setter/getter for for heal done by spell to target of spell hit
        // returns healing calculated before hit, and real dmg done after hit
        public int GetHitHeal()
        {
            if (!IsInTargetHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.GetHitHeal was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return 0;
            }
            return m_spell.m_healing;
        }
        public void SetHitHeal(int heal)
        {
            if (!IsInTargetHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.SetHitHeal was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return;
            }
            m_spell.m_healing = heal;
        }
        void PreventHitHeal() { SetHitHeal(0); }
        public Spell GetSpell() { return m_spell; }
        // returns current spell hit target aura
        public Aura GetHitAura()
        {
            if (!IsInTargetHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.GetHitAura was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return null;
            }
            if (m_spell.m_spellAura == null)
                return null;
            if (m_spell.m_spellAura.IsRemoved())
                return null;
            return m_spell.m_spellAura;
        }
        // prevents applying aura on current spell hit target
        public void PreventHitAura()
        {
            if (!IsInTargetHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.PreventHitAura was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return;
            }
            if (m_spell.m_spellAura != null)
                m_spell.m_spellAura.Remove();
        }

        // prevents effect execution on current spell hit target
        // including other effect/hit scripts
        // will not work on aura/damage/heal
        // will not work if effects were already handled
        public void PreventHitEffect(uint effIndex)
        {
            if (!IsInHitPhase() && !IsInEffectHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.PreventHitEffect was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return;
            }
            m_hitPreventEffectMask |= (1u << (int)effIndex);
            PreventHitDefaultEffect(effIndex);
        }

        // prevents default effect execution on current spell hit target
        // will not work on aura/damage/heal effects
        // will not work if effects were already handled
        public void PreventHitDefaultEffect(uint effIndex)
        {
            if (!IsInHitPhase() && !IsInEffectHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.PreventHitDefaultEffect was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return;
            }
            m_hitPreventDefaultEffectMask |= (1u << (int)effIndex);
        }

        public SpellEffectInfo GetEffectInfo()
        {
            Cypher.Assert(IsInEffectHook(), $"Script: `{m_scriptName}` Spell: `{m_scriptSpellId}`: function SpellScript::GetEffectInfo was called, but function has no effect in current hook!");

            return m_spell.effectInfo;
        }

        // method avalible only in EffectHandler method
        public int GetEffectValue()
        {
            if (!IsInEffectHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.PreventHitDefaultEffect was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return 0;
            }
            return m_spell.damage;
        }

        public void SetEffectValue(int value)
        {
            if (!IsInEffectHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.SetEffectValue was called, but function has no effect in current hook!", m_scriptName, m_scriptSpellId);
                return;
            }

            m_spell.damage = value;
        }

        // returns: cast item if present.
        public Item GetCastItem() { return m_spell.m_CastItem; }

        // Creates item. Calls Spell.DoCreateItem method.
        public void CreateItem(uint effIndex, uint itemId) { m_spell.DoCreateItem(effIndex, itemId); }

        // Returns SpellInfo from the spell that triggered the current one
        public SpellInfo GetTriggeringSpell() { return m_spell.m_triggeredByAuraSpell; }

        // finishes spellcast prematurely with selected error message
        public void FinishCast(SpellCastResult result, uint? param1 = null, uint? param2 = null)
        {
            m_spell.SendCastResult(result, param1, param2);
            m_spell.finish(result == SpellCastResult.SpellCastOk);
        }

        public void SetCustomCastResultMessage(SpellCustomErrors result)
        {
            if (!IsInCheckCastHook())
            {
                Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}`: function SpellScript.SetCustomCastResultMessage was called while spell not in check cast phase!", m_scriptName, m_scriptSpellId);
                return;
            }

            m_spell.m_customError = result;
        }
    }

    public class AuraScript : BaseSpellScript
    {
        // internal use classes & functions
        // DO NOT OVERRIDE THESE IN SCRIPTS
        public delegate bool AuraCheckAreaTargetDelegate(Unit target);
        public delegate void AuraDispelDelegate(DispelInfo dispelInfo);
        public delegate void AuraEffectApplicationModeDelegate(AuraEffect aura, AuraEffectHandleModes auraMode);
        public delegate void AuraEffectPeriodicDelegate(AuraEffect aura);
        public delegate void AuraEffectUpdatePeriodicDelegate(AuraEffect aura);
        public delegate void AuraEffectCalcAmountDelegate(AuraEffect aura, ref int amount, ref bool canBeRecalculated);
        public delegate void AuraEffectCalcPeriodicDelegate(AuraEffect aura, bool isPeriodic, int amplitude);
        public delegate void AuraEffectCalcSpellModDelegate(AuraEffect aura, ref SpellModifier spellMod);
        public delegate void AuraEffectAbsorbDelegate(AuraEffect aura, DamageInfo damageInfo, ref uint absorbAmount);
        public delegate void AuraEffectSplitDelegate(AuraEffect aura, DamageInfo damageInfo, uint splitAmount);
        public delegate bool AuraCheckProcDelegate(ProcEventInfo info);
        public delegate bool AuraCheckEffectProcDelegate(AuraEffect aura , ProcEventInfo info);
        public delegate void AuraProcDelegate(ProcEventInfo info);
        public delegate void AuraEffectProcDelegate(AuraEffect aura, ProcEventInfo info);

        public class CheckAreaTargetHandler
        {
            public CheckAreaTargetHandler(AuraCheckAreaTargetDelegate _pHandlerScript) { pHandlerScript = _pHandlerScript; }
            public bool Call(Unit target)
            {
                return pHandlerScript(target);
            }

            AuraCheckAreaTargetDelegate pHandlerScript;
        }
        public class AuraDispelHandler
        {
            public AuraDispelHandler(AuraDispelDelegate _pHandlerScript) { pHandlerScript = _pHandlerScript; }
            public void Call(DispelInfo dispelInfo)
            {
                pHandlerScript(dispelInfo);
            }

            AuraDispelDelegate pHandlerScript;
        }
        public class EffectBase : EffectHook
        {
            public EffectBase(uint _effIndex, AuraType _effName)
                : base(_effIndex)
            {
                effAurName = _effName;
            }

            public override bool CheckEffect(SpellInfo spellEntry, uint effIndex)
            {
                SpellEffectInfo effect = spellEntry.GetEffect(effIndex);
                if (effect == null)
                    return false;

                if (effect.ApplyAuraName == 0 && effAurName == 0)
                    return true;
                if (effect.ApplyAuraName == 0)
                    return false;
                return (effAurName == AuraType.Any) || (effect.ApplyAuraName == effAurName);
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
                pEffectHandlerScript(aurEff, isPeriodic, periodicTimer);
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
            public EffectAbsorbHandler(AuraEffectAbsorbDelegate _pEffectHandlerScript, byte _effIndex)
                : base(_effIndex, AuraType.SchoolAbsorb)
            {
                pEffectHandlerScript = _pEffectHandlerScript;
            }

            public void Call(AuraEffect aurEff, DamageInfo dmgInfo, ref uint absorbAmount)
            {
                pEffectHandlerScript(aurEff, dmgInfo, ref absorbAmount);
            }

            AuraEffectAbsorbDelegate pEffectHandlerScript;
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

        public AuraScript()
        {
            m_aura = null;
            m_auraApplication = null;
            m_defaultActionPrevented = false;
        }

        public override bool _Validate(SpellInfo entry)
        {
            foreach (var eff in DoCheckAreaTarget)
                if (!entry.HasAreaAuraEffect() && !entry.HasEffect(SpellEffectName.PersistentAreaAura) && !entry.HasEffect(SpellEffectName.ApplyAura))
                    Log.outError(LogFilter.Scripts, "Spell `{0}` of script `{1}` does not have apply aura effect - handler bound to hook `DoCheckAreaTarget` of AuraScript won't be executed", entry.Id, m_scriptName);

            foreach (var eff in OnDispel)
                if (!entry.HasEffect(SpellEffectName.ApplyAura) && !entry.HasAreaAuraEffect())
                    Log.outError(LogFilter.Scripts, "Spell `{0}` of script `{1}` does not have apply aura effect - handler bound to hook `OnDispel` of AuraScript won't be executed", entry.Id, m_scriptName);

            foreach (var eff in AfterDispel)
                if (!entry.HasEffect(SpellEffectName.ApplyAura) && !entry.HasAreaAuraEffect())
                    Log.outError(LogFilter.Scripts, "Spell `{0}` of script `{1}` does not have apply aura effect - handler bound to hook `AfterDispel` of AuraScript won't be executed", entry.Id, m_scriptName);

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

            foreach (var eff in DoCheckProc)
                if (!entry.HasEffect(SpellEffectName.ApplyAura) && !entry.HasAreaAuraEffect())
                    Log.outError(LogFilter.Scripts, "Spell `{0}` of script `{1}` does not have apply aura effect - handler bound to hook `DoCheckProc` of AuraScript won't be executed", entry.Id, m_scriptName);

            foreach (var eff in DoCheckEffectProc)
                if (eff.GetAffectedEffectsMask(entry) == 0)
                    Log.outError(LogFilter.Scripts, "Spell `{0}` Effect `{1}` of script `{2}` did not match dbc effect data - handler bound to hook `DoCheckEffectProc` of AuraScript won't be executed", entry.Id, eff.ToString(), m_scriptName);

            foreach (var eff in DoPrepareProc)
                if (!entry.HasEffect(SpellEffectName.ApplyAura) && !entry.HasAreaAuraEffect())
                    Log.outError(LogFilter.Scripts, "Spell `{0}` of script `{1}` does not have apply aura effect - handler bound to hook `DoPrepareProc` of AuraScript won't be executed", entry.Id, m_scriptName);

            foreach (var eff in OnProc)
                if (!entry.HasEffect(SpellEffectName.ApplyAura) && !entry.HasAreaAuraEffect())
                    Log.outError(LogFilter.Scripts, "Spell `{0}` of script `{1}` does not have apply aura effect - handler bound to hook `OnProc` of AuraScript won't be executed", entry.Id, m_scriptName);

            foreach (var eff in AfterProc)
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
        Stack<ScriptStateStore> m_scriptStates = new Stack<ScriptStateStore>();

        // AuraScript interface
        // hooks to which you can attach your functions
        //
        // executed when area aura checks if it can be applied on target
        // example: OnEffectApply += AuraEffectApplyFn(class.function);
        // where function is: bool function (Unit target);
        public List<CheckAreaTargetHandler> DoCheckAreaTarget = new List<CheckAreaTargetHandler>();

        // executed when aura is dispelled by a unit
        // example: OnDispel += AuraDispelFn(class.function);
        // where function is: void function (DispelInfo dispelInfo);
        public List<AuraDispelHandler> OnDispel = new List<AuraDispelHandler>();

        // executed after aura is dispelled by a unit
        // example: AfterDispel += AuraDispelFn(class.function);
        // where function is: void function (DispelInfo dispelInfo);
        public List<AuraDispelHandler> AfterDispel = new List<AuraDispelHandler>();

        // executed when aura effect is applied with specified mode to target
        // should be used when when effect handler preventing/replacing is needed, do not use this hook for triggering spellcasts/removing auras etc - may be unsafe
        // example: OnEffectApply += AuraEffectApplyFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier, AuraEffectHandleModes);
        // where function is: void function (AuraEffect aurEff, AuraEffectHandleModes mode);
        public List<EffectApplyHandler> OnEffectApply = new List<EffectApplyHandler>();

        // executed after aura effect is applied with specified mode to target
        // example: AfterEffectApply += AuraEffectApplyFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier, AuraEffectHandleModes);
        // where function is: void function (AuraEffect aurEff, AuraEffectHandleModes mode);
        public List<EffectApplyHandler> AfterEffectApply = new List<EffectApplyHandler>();

        // executed after aura effect is removed with specified mode from target
        // should be used when when effect handler preventing/replacing is needed, do not use this hook for triggering spellcasts/removing auras etc - may be unsafe
        // example: OnEffectRemove += AuraEffectRemoveFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier, AuraEffectHandleModes);
        // where function is: void function (AuraEffect aurEff, AuraEffectHandleModes mode);
        public List<EffectApplyHandler> OnEffectRemove = new List<EffectApplyHandler>();

        // executed when aura effect is removed with specified mode from target
        // example: AfterEffectRemove += AuraEffectRemoveFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier, AuraEffectHandleModes);
        // where function is: void function (AuraEffect aurEff, AuraEffectHandleModes mode);
        public List<EffectApplyHandler> AfterEffectRemove = new List<EffectApplyHandler>();

        // executed when periodic aura effect ticks on target
        // example: OnEffectPeriodic += AuraEffectPeriodicFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff);
        public List<EffectPeriodicHandler> OnEffectPeriodic = new List<EffectPeriodicHandler>();

        // executed when periodic aura effect is updated
        // example: OnEffectUpdatePeriodic += AuraEffectUpdatePeriodicFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff);
        public List<EffectUpdatePeriodicHandler> OnEffectUpdatePeriodic = new List<EffectUpdatePeriodicHandler>();

        // executed when aura effect calculates amount
        // example: DoEffectCalcAmount += AuraEffectCalcAmounFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff, int& amount, bool& canBeRecalculated);
        public List<EffectCalcAmountHandler> DoEffectCalcAmount = new List<EffectCalcAmountHandler>();

        // executed when aura effect calculates periodic data
        // example: DoEffectCalcPeriodic += AuraEffectCalcPeriodicFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff, bool& isPeriodic, int& amplitude);
        public List<EffectCalcPeriodicHandler> DoEffectCalcPeriodic = new List<EffectCalcPeriodicHandler>();

        // executed when aura effect calculates spellmod
        // example: DoEffectCalcSpellMod += AuraEffectCalcSpellModFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff, SpellModifier& spellMod);
        public List<EffectCalcSpellModHandler> DoEffectCalcSpellMod = new List<EffectCalcSpellModHandler>();

        // executed when absorb aura effect is going to reduce damage
        // example: OnEffectAbsorb += AuraEffectAbsorbFn(class.function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect aurEff, DamageInfo& dmgInfo, uint& absorbAmount);
        public List<EffectAbsorbHandler> OnEffectAbsorb = new List<EffectAbsorbHandler>();

        // executed after absorb aura effect reduced damage to target - absorbAmount is real amount absorbed by aura
        // example: AfterEffectAbsorb += AuraEffectAbsorbFn(class.function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect aurEff, DamageInfo& dmgInfo, uint& absorbAmount);
        public List<EffectAbsorbHandler> AfterEffectAbsorb = new List<EffectAbsorbHandler>();

        // executed when mana shield aura effect is going to reduce damage
        // example: OnEffectManaShield += AuraEffectAbsorbFn(class.function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect aurEff, DamageInfo& dmgInfo, uint& absorbAmount);
        public List<EffectManaShieldHandler> OnEffectManaShield = new List<EffectManaShieldHandler>();

        // executed after mana shield aura effect reduced damage to target - absorbAmount is real amount absorbed by aura
        // example: AfterEffectManaShield += AuraEffectAbsorbFn(class.function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect aurEff, DamageInfo& dmgInfo, uint& absorbAmount);
        public List<EffectManaShieldHandler> AfterEffectManaShield = new List<EffectManaShieldHandler>();

        // executed when the caster of some spell with split dmg aura gets damaged through it
        // example: OnEffectSplit += AuraEffectSplitFn(class.function, EffectIndexSpecifier);
        // where function is: void function (AuraEffect aurEff, DamageInfo& dmgInfo, uint& splitAmount);
        public List<EffectSplitHandler> OnEffectSplit = new List<EffectSplitHandler>();

        // executed when aura checks if it can proc
        // example: DoCheckProc += AuraCheckProcFn(class.function);
        // where function is: bool function (ProcEventInfo& eventInfo);
        public List<CheckProcHandler> DoCheckProc = new List<CheckProcHandler>();

        // executed when aura effect checks if it can proc the aura
        // example: DoCheckEffectProc += AuraCheckEffectProcFn(class::function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is bool function (AuraEffect const* aurEff, ProcEventInfo& eventInfo);
        public List<CheckEffectProcHandler> DoCheckEffectProc = new List<CheckEffectProcHandler>();

        // executed before aura procs (possibility to prevent charge drop/cooldown)
        // example: DoPrepareProc += AuraProcFn(class.function);
        // where function is: void function (ProcEventInfo& eventInfo);
        public List<AuraProcHandler> DoPrepareProc = new List<AuraProcHandler>();

        // executed when aura procs
        // example: OnProc += AuraProcFn(class.function);
        // where function is: void function (ProcEventInfo& eventInfo);
        public List<AuraProcHandler> OnProc = new List<AuraProcHandler>();

        // executed after aura proced
        // example: AfterProc += AuraProcFn(class.function);
        // where function is: void function (ProcEventInfo& eventInfo);
        public List<AuraProcHandler> AfterProc = new List<AuraProcHandler>();

        // executed when aura effect procs
        // example: OnEffectProc += AuraEffectProcFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff, ProcEventInfo& procInfo);
        public List<EffectProcHandler> OnEffectProc = new List<EffectProcHandler>();

        // executed after aura effect proced
        // example: AfterEffectProc += AuraEffectProcFn(class.function, EffectIndexSpecifier, EffectAuraNameSpecifier);
        // where function is: void function (AuraEffect aurEff, ProcEventInfo& procInfo);
        public List<EffectProcHandler> AfterEffectProc = new List<EffectProcHandler>();

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
        // returns spellid of the spell
        public uint GetId() { return m_aura.GetId(); }

        // returns guid of object which casted the aura (m_originalCaster of the Spell class)
        public ObjectGuid GetCasterGUID() { return m_aura.GetCasterGUID(); }
        // returns unit which casted the aura or null if not avalible (caster logged out for example)
        public Unit GetCaster() { return m_aura.GetCaster(); }
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
        int GetDuration() { return m_aura.GetDuration(); }
        public void SetDuration(int duration, bool withMods = false) { m_aura.SetDuration(duration, withMods); }
        // sets duration to maxduration
        void RefreshDuration() { m_aura.RefreshDuration(); }
        long GetApplyTime() { return m_aura.GetApplyTime(); }
        public int GetMaxDuration() { return m_aura.GetMaxDuration(); }
        void SetMaxDuration(int duration) { m_aura.SetMaxDuration(duration); }
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
                    return m_auraApplication.GetTarget();
                default:
                    Log.outError(LogFilter.Scripts, "Script: `{0}` Spell: `{1}` AuraScript.GetTarget called in a hook in which the call won't have effect!", m_scriptName, m_scriptSpellId);
                    break;
            }

            return null;
        }
        // returns AuraApplication object of currently processed target
        public AuraApplication GetTargetApplication() { return m_auraApplication; }
    }
}
