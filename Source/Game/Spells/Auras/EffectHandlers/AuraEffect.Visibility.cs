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

        /**************************************/
        /***       VISIBILITY & PHASES      ***/
        /**************************************/
        [AuraEffectHandler(AuraType.None)]
        private void HandleUnused(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
        }

        [AuraEffectHandler(AuraType.ModInvisibilityDetect)]
        private void HandleModInvisibilityDetect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();
            InvisibilityType type = (InvisibilityType)GetMiscValue();

            if (apply)
            {
                target.InvisibilityDetect.AddFlag(type);
                target.InvisibilityDetect.AddValue(type, GetAmount());
            }
            else
            {
                if (!target.HasAuraType(AuraType.ModInvisibilityDetect))
                    target.InvisibilityDetect.DelFlag(type);

                target.InvisibilityDetect.AddValue(type, -GetAmount());
            }

            // call functions which may have additional effects after changing State of unit
            if (target.IsInWorld)
                target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.ModInvisibility)]
        private void HandleModInvisibility(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            Unit target = aurApp.GetTarget();
            Player playerTarget = target.ToPlayer();
            InvisibilityType type = (InvisibilityType)GetMiscValue();

            if (apply)
            {
                // apply glow vision
                if (playerTarget != null &&
                    type == InvisibilityType.General)
                    playerTarget.AddAuraVision(PlayerFieldByte2Flags.InvisibilityGlow);

                target.Invisibility.AddFlag(type);
                target.Invisibility.AddValue(type, GetAmount());

                target.SetVisFlag(UnitVisFlags.Invisible);
            }
            else
            {
                if (!target.HasAuraType(AuraType.ModInvisibility))
                {
                    // if not have different invisibility Auras.
                    // always remove glow vision
                    playerTarget?.RemoveAuraVision(PlayerFieldByte2Flags.InvisibilityGlow);

                    target.Invisibility.DelFlag(type);
                }
                else
                {
                    bool found = false;
                    var invisAuras = target.GetAuraEffectsByType(AuraType.ModInvisibility);

                    foreach (var eff in invisAuras)
                        if (GetMiscValue() == eff.GetMiscValue())
                        {
                            found = true;

                            break;
                        }

                    if (!found)
                    {
                        // if not have invisibility Auras of Type INVISIBILITY_GENERAL
                        // remove glow vision
                        if (playerTarget != null &&
                            type == InvisibilityType.General)
                            playerTarget.RemoveAuraVision(PlayerFieldByte2Flags.InvisibilityGlow);

                        target.Invisibility.DelFlag(type);

                        target.RemoveVisFlag(UnitVisFlags.Invisible);
                    }
                }

                target.Invisibility.AddValue(type, -GetAmount());
            }

            // call functions which may have additional effects after changing State of unit
            if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
                // drop flag at invisibiliy in bg
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);

            if (target.IsInWorld)
                target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.ModStealthDetect)]
        private void HandleModStealthDetect(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();
            StealthType type = (StealthType)GetMiscValue();

            if (apply)
            {
                target.StealthDetect.AddFlag(type);
                target.StealthDetect.AddValue(type, GetAmount());
            }
            else
            {
                if (!target.HasAuraType(AuraType.ModStealthDetect))
                    target.StealthDetect.DelFlag(type);

                target.StealthDetect.AddValue(type, -GetAmount());
            }

            // call functions which may have additional effects after changing State of unit
            if (target.IsInWorld)
                target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.ModStealth)]
        private void HandleModStealth(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            Unit target = aurApp.GetTarget();
            StealthType type = (StealthType)GetMiscValue();

            if (apply)
            {
                target.Stealth.AddFlag(type);
                target.Stealth.AddValue(type, GetAmount());
                target.SetVisFlag(UnitVisFlags.Stealthed);
                Player playerTarget = target.ToPlayer();

                playerTarget?.AddAuraVision(PlayerFieldByte2Flags.Stealth);
            }
            else
            {
                target.Stealth.AddValue(type, -GetAmount());

                if (!target.HasAuraType(AuraType.ModStealth)) // if last SPELL_AURA_MOD_STEALTH
                {
                    target.Stealth.DelFlag(type);

                    target.RemoveVisFlag(UnitVisFlags.Stealthed);
                    Player playerTarget = target.ToPlayer();

                    playerTarget?.RemoveAuraVision(PlayerFieldByte2Flags.Stealth);
                }
            }

            // call functions which may have additional effects after changing State of unit
            if (apply && mode.HasAnyFlag(AuraEffectHandleModes.Real))
                // drop flag at stealth in bg
                target.RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.StealthOrInvis);

            if (target.IsInWorld)
                target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.ModStealthLevel)]
        private void HandleModStealthLevel(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();
            StealthType type = (StealthType)GetMiscValue();

            if (apply)
                target.Stealth.AddValue(type, GetAmount());
            else
                target.Stealth.AddValue(type, -GetAmount());

            // call functions which may have additional effects after changing State of unit
            if (target.IsInWorld)
                target.UpdateObjectVisibility();
        }

        [AuraEffectHandler(AuraType.DetectAmore)]
        private void HandleDetectAmore(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (target.IsTypeId(TypeId.Player))
                return;

            if (apply)
            {
                Player playerTarget = target.ToPlayer();

                playerTarget?.AddAuraVision((PlayerFieldByte2Flags)(1 << GetMiscValue() - 1));
            }
            else
            {
                if (target.HasAuraType(AuraType.DetectAmore))
                {
                    var amoreAuras = target.GetAuraEffectsByType(AuraType.DetectAmore);

                    foreach (var auraEffect in amoreAuras)
                        if (GetMiscValue() == auraEffect.GetMiscValue())
                            return;
                }

                Player playerTarget = target.ToPlayer();

                playerTarget?.RemoveAuraVision((PlayerFieldByte2Flags)(1 << GetMiscValue() - 1));
            }
        }

        [AuraEffectHandler(AuraType.SpiritOfRedemption)]
        private void HandleSpiritOfRedemption(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Player))
                return;

            // prepare spirit State
            if (apply)
            {
                if (target.IsTypeId(TypeId.Player))
                    // set stand State (expected in this form)
                    if (!target.IsStandState())
                        target.SetStandState(UnitStandStateType.Stand);
            }
            // die at aura end
            else if (target.IsAlive())
            // call functions which may have additional effects after changing State of unit
            {
                target.SetDeathState(DeathState.JustDied);
            }
        }

        [AuraEffectHandler(AuraType.Ghost)]
        private void HandleAuraGhost(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            if (apply)
            {
                target.SetPlayerFlag(PlayerFlags.Ghost);
                target.ServerSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
                target.ServerSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Ghost);
            }
            else
            {
                if (target.HasAuraType(AuraType.Ghost))
                    return;

                target.RemovePlayerFlag(PlayerFlags.Ghost);
                target.ServerSideVisibility.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);
                target.ServerSideVisibilityDetect.SetValue(ServerSideVisibilityType.Ghost, GhostVisibilityType.Alive);
            }
        }

        [AuraEffectHandler(AuraType.Phase)]
        private void HandlePhase(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                PhasingHandler.AddPhase(target, (uint)GetMiscValueB(), true);
            else
                PhasingHandler.RemovePhase(target, (uint)GetMiscValueB(), true);
        }

        [AuraEffectHandler(AuraType.PhaseGroup)]
        private void HandlePhaseGroup(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
                PhasingHandler.AddPhaseGroup(target, (uint)GetMiscValueB(), true);
            else
                PhasingHandler.RemovePhaseGroup(target, (uint)GetMiscValueB(), true);
        }

        [AuraEffectHandler(AuraType.PhaseAlwaysVisible)]
        private void HandlePhaseAlwaysVisible(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
            {
                PhasingHandler.SetAlwaysVisible(target, true, true);
            }
            else
            {
                if (target.HasAuraType(AuraType.PhaseAlwaysVisible) ||
                    target.IsPlayer() && target.ToPlayer().IsGameMaster())
                    return;

                PhasingHandler.SetAlwaysVisible(target, false, true);
            }
        }

    }
}