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
        /***************************/
        /***        CHARM        ***/
        /***************************/
        [AuraEffectHandler(AuraType.ModPossess)]
        private void HandleModPossess(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            Unit caster = GetCaster();

            // no support for posession AI yet
            if (caster != null &&
                caster.IsTypeId(TypeId.Unit))
            {
                HandleModCharm(aurApp, mode, apply);

                return;
            }

            if (apply)
                target.SetCharmedBy(caster, CharmType.Possess, aurApp);
            else
                target.RemoveCharmedBy(caster);
        }

        [AuraEffectHandler(AuraType.ModPossessPet)]
        private void HandleModPossessPet(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit caster = GetCaster();

            if (caster == null ||
                !caster.IsTypeId(TypeId.Player))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsTypeId(TypeId.Unit) ||
                !target.IsPet())
                return;

            Pet pet = target.ToPet();

            if (apply)
            {
                if (caster.ToPlayer().GetPet() != pet)
                    return;

                pet.SetCharmedBy(caster, CharmType.Possess, aurApp);
            }
            else
            {
                pet.RemoveCharmedBy(caster);

                if (!pet.IsWithinDistInMap(caster, pet.GetMap().GetVisibilityRange()))
                {
                    pet.Remove(PetSaveMode.NotInSlot, true);
                }
                else
                {
                    // Reinitialize the pet bar or it will appear greyed out
                    caster.ToPlayer().PetSpellInitialize();

                    // TODO: remove this
                    if (pet.GetVictim() == null &&
                        !pet.GetCharmInfo().HasCommandState(CommandStates.Stay))
                        pet.GetMotionMaster().MoveFollow(caster, SharedConst.PetFollowDist, pet.GetFollowAngle());
                }
            }
        }

        [AuraEffectHandler(AuraType.ModCharm)]
        private void HandleModCharm(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            Unit caster = GetCaster();

            if (apply)
                target.SetCharmedBy(caster, CharmType.Charm, aurApp);
            else
                target.RemoveCharmedBy(caster);
        }

        [AuraEffectHandler(AuraType.AoeCharm)]
        private void HandleCharmConvert(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            Unit caster = GetCaster();

            if (apply)
                target.SetCharmedBy(caster, CharmType.Convert, aurApp);
            else
                target.RemoveCharmedBy(caster);
        }

        /**
		 * Such Auras are applied from a caster(=player) to a vehicle.
		 * This has been verified using spell #49256
		 */
        [AuraEffectHandler(AuraType.ControlVehicle)]
        private void HandleAuraControlVehicle(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsVehicle())
                return;

            Unit caster = GetCaster();

            if (caster == null ||
                caster == target)
                return;

            if (apply)
            {
                // Currently spells that have base points  0 and DieSides 0 = "0/0" exception are pushed to -1,
                // however the idea of 0/0 is to ingore flag VEHICLE_SEAT_FLAG_CAN_ENTER_OR_EXIT and -1 checks for it,
                // so this break such spells or most of them.
                // Current formula about _amount: effect base points + dieside - 1
                // TO DO: Reasearch more about 0/0 and fix it.
                caster._EnterVehicle(target.GetVehicleKit(), (sbyte)(GetAmount() - 1), aurApp);
            }
            else
            {
                // Remove pending passengers before exiting vehicle - might cause an Uninstall
                target.GetVehicleKit().RemovePendingEventsForPassenger(caster);

                if (GetId() == 53111) // Devour Humanoid
                {
                    Unit.Kill(target, caster);

                    if (caster.IsTypeId(TypeId.Unit))
                        caster.ToCreature().DespawnOrUnsummon();
                }

                bool seatChange = mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmount) // Seat change on the same direct vehicle
                                  ||
                                  target.HasAuraTypeWithCaster(AuraType.ControlVehicle, caster.GetGUID()); // Seat change to a proxy vehicle (for example turret mounted on a siege engine)

                if (!seatChange)
                    caster._ExitVehicle();
                else
                    target.GetVehicleKit().RemovePassenger(caster); // Only remove passenger from vehicle without launching exit movement or despawning the vehicle

                // some SPELL_AURA_CONTROL_VEHICLE Auras have a dummy effect on the player - remove them
                caster.RemoveAurasDueToSpell(GetId());
            }
        }

    }
}