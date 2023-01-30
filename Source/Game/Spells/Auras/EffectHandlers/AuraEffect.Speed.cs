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
        /***                  MODIFY SPEED                     ***/
        /*********************************************************/
        [AuraEffectHandler(AuraType.ModIncreaseSpeed)]
        [AuraEffectHandler(AuraType.ModSpeedAlways)]
        [AuraEffectHandler(AuraType.ModSpeedNotStack)]
        [AuraEffectHandler(AuraType.ModMinimumSpeed)]
        private void HandleAuraModIncreaseSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
        }

        [AuraEffectHandler(AuraType.ModIncreaseMountedSpeed)]
        [AuraEffectHandler(AuraType.ModMountedSpeedAlways)]
        [AuraEffectHandler(AuraType.ModMountedSpeedNotStack)]
        private void HandleAuraModIncreaseMountedSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            HandleAuraModIncreaseSpeed(aurApp, mode, apply);
        }

        [AuraEffectHandler(AuraType.ModIncreaseVehicleFlightSpeed)]
        [AuraEffectHandler(AuraType.ModIncreaseMountedFlightSpeed)]
        [AuraEffectHandler(AuraType.ModIncreaseFlightSpeed)]
        [AuraEffectHandler(AuraType.ModMountedFlightSpeedAlways)]
        [AuraEffectHandler(AuraType.ModVehicleSpeedAlways)]
        [AuraEffectHandler(AuraType.ModFlightSpeedNotStack)]
        private void HandleAuraModIncreaseFlightSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountSendForClientMask))
                return;

            Unit target = aurApp.GetTarget();

            if (mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                target.UpdateSpeed(UnitMoveType.Flight);

            //! Update ability to fly
            if (GetAuraType() == AuraType.ModIncreaseMountedFlightSpeed)
            {
                // do not remove unit flag if there are more than this auraEffect of that kind on unit on unit
                if (mode.HasAnyFlag(AuraEffectHandleModes.SendForClientMask) &&
                    (apply || !target.HasAuraType(AuraType.ModIncreaseMountedFlightSpeed) && !target.HasAuraType(AuraType.Fly)))
                {
                    target.SetCanTransitionBetweenSwimAndFly(apply);

                    if (target.SetCanFly(apply))
                        if (!apply &&
                            !target.IsGravityDisabled())
                            target.GetMotionMaster().MoveFall();
                }

                //! Someone should clean up these hacks and remove it from this function. It doesn't even belong here.
                if (mode.HasAnyFlag(AuraEffectHandleModes.Real))
                {
                    //Players on flying mounts must be immune to polymorph
                    if (target.IsTypeId(TypeId.Player))
                        target.ApplySpellImmune(GetId(), SpellImmunity.Mechanic, (uint)Mechanics.Polymorph, apply);

                    // Dragonmaw Illusion (overwrite Mount model, mounted aura already applied)
                    if (apply &&
                        target.HasAuraEffect(42016, 0) &&
                        target.GetMountDisplayId() != 0)
                        target.SetMountDisplayId(16314);
                }
            }
        }

        [AuraEffectHandler(AuraType.ModIncreaseSwimSpeed)]
        private void HandleAuraModIncreaseSwimSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Swim);
        }

        [AuraEffectHandler(AuraType.ModDecreaseSpeed)]
        private void HandleAuraModDecreaseSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
            target.UpdateSpeed(UnitMoveType.Swim);
            target.UpdateSpeed(UnitMoveType.Flight);
            target.UpdateSpeed(UnitMoveType.RunBack);
            target.UpdateSpeed(UnitMoveType.SwimBack);
            target.UpdateSpeed(UnitMoveType.FlightBack);
        }

        [AuraEffectHandler(AuraType.UseNormalMovementSpeed)]
        private void HandleAuraModUseNormalSpeed(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
            target.UpdateSpeed(UnitMoveType.Swim);
            target.UpdateSpeed(UnitMoveType.Flight);
        }

        [AuraEffectHandler(AuraType.ModMinimumSpeedRate)]
        private void HandleAuraModMinimumSpeedRate(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.UpdateSpeed(UnitMoveType.Run);
        }

        [AuraEffectHandler(AuraType.ModMovementForceMagnitude)]
        private void HandleModMovementForceMagnitude(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            aurApp.GetTarget().UpdateMovementForcesModMagnitude();
        }

    }
}