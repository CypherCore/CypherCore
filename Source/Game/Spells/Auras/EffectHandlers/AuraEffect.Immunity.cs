// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Dynamic;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IUnit;

namespace Game.Spells.Auras.EffectHandlers
{
    public partial class AuraEffect
    {
        /*********************************************************/
        /***                     IMMUNITY                      ***/
        /*********************************************************/
        [AuraEffectHandler(AuraType.MechanicImmunityMask)]
        private void HandleModMechanicImmunityMask(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        [AuraEffectHandler(AuraType.MechanicImmunity)]
        private void HandleModMechanicImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        [AuraEffectHandler(AuraType.EffectImmunity)]
        private void HandleAuraModEffectImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);

            // when removing flag aura, handle flag drop
            // TODO: this should be handled in aura script for flag spells using AfterEffectRemove hook
            Player player = target.ToPlayer();

            if (!apply &&
                player != null &&
                GetSpellInfo().HasAuraInterruptFlag(SpellAuraInterruptFlags.StealthOrInvis))
            {
                if (player.InBattleground())
                {
                    Battleground bg = player.GetBattleground();

                    if (bg)
                        bg.EventPlayerDroppedFlag(player);
                }
                else
                {
                    Global.OutdoorPvPMgr.HandleDropFlag(player, GetSpellInfo().Id);
                }
            }
        }

        [AuraEffectHandler(AuraType.StateImmunity)]
        private void HandleAuraModStateImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

        [AuraEffectHandler(AuraType.SchoolImmunity)]
        private void HandleAuraModSchoolImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);

            if (GetSpellInfo().Mechanic == Mechanics.Banish)
            {
                if (apply)
                {
                    target.AddUnitState(UnitState.Isolated);
                }
                else
                {
                    bool banishFound = false;
                    var banishAuras = target.GetAuraEffectsByType(GetAuraType());

                    foreach (var aurEff in banishAuras)
                        if (aurEff.GetSpellInfo().Mechanic == Mechanics.Banish)
                        {
                            banishFound = true;

                            break;
                        }

                    if (!banishFound)
                        target.ClearUnitState(UnitState.Isolated);
                }
            }

            // TODO: should be changed to a proc script on flag spell (they have "Taken positive" proc Flags in db2)
            {
                if (apply && GetMiscValue() == (int)SpellSchoolMask.Normal)
                    target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);

                // remove all flag Auras (they are positive, but they must be removed when you are immune)
                if (GetSpellInfo().HasAttribute(SpellAttr1.ImmunityPurgesEffect) &&
                    GetSpellInfo().HasAttribute(SpellAttr2.FailOnAllTargetsImmune))
                    target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);
            }

            if (apply)
            {
                target.SetUnitFlag(UnitFlags.Immune);
                target.GetThreatManager().EvaluateSuppressed();
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit
                if (target.HasAuraType(GetAuraType()) ||
                    target.HasAuraType(AuraType.DamageImmunity))
                    return;

                target.RemoveUnitFlag(UnitFlags.Immune);
            }
        }

        [AuraEffectHandler(AuraType.DamageImmunity)]
        private void HandleAuraModDmgImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);

            if (apply)
            {
                target.SetUnitFlag(UnitFlags.Immune);
                target.GetThreatManager().EvaluateSuppressed();
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit
                if (target.HasAuraType(GetAuraType()) ||
                    target.HasAuraType(AuraType.SchoolImmunity))
                    return;

                target.RemoveUnitFlag(UnitFlags.Immune);
            }
        }

        [AuraEffectHandler(AuraType.DispelImmunity)]
        private void HandleAuraModDispelImmunity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();
            _spellInfo.ApplyAllSpellImmunitiesTo(target, GetSpellEffectInfo(), apply);
        }

    }
}