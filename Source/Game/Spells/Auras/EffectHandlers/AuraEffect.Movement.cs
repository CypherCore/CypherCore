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
        /****************************/
        /***       MOVEMENT       ***/
        /****************************/
        [AuraEffectHandler(AuraType.Mounted)]
        private void HandleAuraMounted(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                if (mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                {
                    uint creatureEntry = (uint)GetMiscValue();
                    uint displayId = 0;
                    uint vehicleId = 0;

                    var mountEntry = Global.DB2Mgr.GetMount(GetId());

                    if (mountEntry != null)
                    {
                        var mountDisplays = Global.DB2Mgr.GetMountDisplays(mountEntry.Id);

                        if (mountDisplays != null)
                        {
                            if (mountEntry.IsSelfMount())
                            {
                                displayId = SharedConst.DisplayIdHiddenMount;
                            }
                            else
                            {
                                var usableDisplays = mountDisplays.Where(mountDisplay =>
                                                                         {
                                                                             Player playerTarget = target.ToPlayer();

                                                                             if (playerTarget != null)
                                                                             {
                                                                                 var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(mountDisplay.PlayerConditionID);

                                                                                 if (playerCondition != null)
                                                                                     return ConditionManager.IsPlayerMeetingCondition(playerTarget, playerCondition);
                                                                             }

                                                                             return true;
                                                                         })
                                                                  .ToList();

                                if (!usableDisplays.Empty())
                                    displayId = usableDisplays.SelectRandom().CreatureDisplayInfoID;
                            }
                        }
                        // TODO: CREATE TABLE mount_vehicle (mountId, vehicleCreatureId) for future mounts that are vehicles (new mounts no longer have proper _data in MiscValue)
                        //if (MountVehicle const* mountVehicle = sObjectMgr->GetMountVehicle(mountEntry->Id))
                        //    creatureEntry = mountVehicle->VehicleCreatureId;
                    }

                    CreatureTemplate creatureInfo = Global.ObjectMgr.GetCreatureTemplate(creatureEntry);

                    if (creatureInfo != null)
                    {
                        vehicleId = creatureInfo.VehicleId;

                        if (displayId == 0)
                        {
                            CreatureModel model = ObjectManager.ChooseDisplayId(creatureInfo);
                            Global.ObjectMgr.GetCreatureModelRandomGender(ref model, creatureInfo);
                            displayId = model.CreatureDisplayID;
                        }

                        //some spell has one aura of Mount and one of vehicle
                        foreach (SpellEffectInfo effect in GetSpellInfo().GetEffects())
                            if (effect.IsEffect(SpellEffectName.Summon) &&
                                effect.MiscValue == GetMiscValue())
                                displayId = 0;
                    }

                    target.Mount(displayId, vehicleId, creatureEntry);
                }

                // cast speed aura
                if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                {
                    var mountCapability = CliDB.MountCapabilityStorage.LookupByKey(GetAmount());

                    if (mountCapability != null)
                        target.CastSpell(target, mountCapability.ModSpellAuraID, new CastSpellExtraArgs(this));
                }
            }
            else
            {
                if (mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                    target.Dismount();

                //some mounts like Headless Horseman's Mount or broom stick are skill based spell
                // need to remove ALL arura related to mounts, this will stop client crash with broom stick
                // and never endless flying after using Headless Horseman's Mount
                if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
                    target.RemoveAurasByType(AuraType.Mounted);

                if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                {
                    // remove speed aura
                    var mountCapability = CliDB.MountCapabilityStorage.LookupByKey(GetAmount());

                    if (mountCapability != null)
                        target.RemoveAurasDueToSpell(mountCapability.ModSpellAuraID, target.GetGUID());
                }
            }
        }

        [AuraEffectHandler(AuraType.Fly)]
        private void HandleAuraAllowFlight(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()) ||
                    target.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed))
                    return;

            target.SetCanTransitionBetweenSwimAndFly(apply);

            if (target.SetCanFly(apply))
                if (!apply &&
                    !target.IsGravityDisabled())
                    target.GetMotionMaster().MoveFall();
        }

        [AuraEffectHandler(AuraType.WaterWalk)]
        private void HandleAuraWaterWalk(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;

            target.SetWaterWalking(apply);
        }

        [AuraEffectHandler(AuraType.FeatherFall)]
        private void HandleAuraFeatherFall(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;

            target.SetFeatherFall(apply);

            // start fall from current height
            if (!apply &&
                target.IsTypeId(TypeId.Player))
                target.ToPlayer().SetFallInformation(0, target.GetPositionZ());
        }

        [AuraEffectHandler(AuraType.Hover)]
        private void HandleAuraHover(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;

            target.SetHover(apply); //! Sets movementflags
        }

        [AuraEffectHandler(AuraType.WaterBreathing)]
        private void HandleWaterBreathing(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            // update timers in client
            if (target.IsTypeId(TypeId.Player))
                target.ToPlayer().UpdateMirrorTimers();
        }

        [AuraEffectHandler(AuraType.ForceMoveForward)]
        private void HandleForceMoveForward(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (apply)
            {
                target.SetUnitFlag2(UnitFlags2.ForceMovement);
            }
            else
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;

                target.RemoveUnitFlag2(UnitFlags2.ForceMovement);
            }
        }

        [AuraEffectHandler(AuraType.CanTurnWhileFalling)]
        private void HandleAuraCanTurnWhileFalling(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;

            target.SetCanTurnWhileFalling(apply);
        }

        [AuraEffectHandler(AuraType.IgnoreMovementForces)]
        private void HandleIgnoreMovementForces(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;

            target.SetIgnoreMovementForces(apply);
        }

        [AuraEffectHandler(AuraType.DisableInertia)]
        private void HandleDisableInertia(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasFlag(AuraEffectHandleModes.SendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!apply)
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (target.HasAuraType(GetAuraType()))
                    return;

            target.SetDisableInertia(apply);
        }

    }
}