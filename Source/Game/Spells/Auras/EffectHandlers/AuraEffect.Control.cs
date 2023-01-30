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
        /*****************************/
        /***        CONTROL        ***/
        /*****************************/
        [AuraEffectHandler(AuraType.ModConfuse)]
        private void HandleModConfuse(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Confused);

            if (apply)
                target.GetThreatManager().EvaluateSuppressed();
        }

        [AuraEffectHandler(AuraType.ModFear)]
        private void HandleModFear(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Fleeing);
        }

        [AuraEffectHandler(AuraType.ModStun)]
        private void HandleAuraModStun(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Stunned);

            if (apply)
                target.GetThreatManager().EvaluateSuppressed();
        }

        [AuraEffectHandler(AuraType.ModRoot)]
        [AuraEffectHandler(AuraType.ModRoot2)]
        private void HandleAuraModRoot(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Root);
        }

        [AuraEffectHandler(AuraType.PreventsFleeing)]
        private void HandlePreventFleeing(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            // Since patch 3.0.2 this mechanic no longer affects fear effects. It will ONLY prevent humanoids from fleeing due to low health.
            if (!apply ||
                target.HasAuraType(AuraType.ModFear))
                return;

            // TODO: find a way to cancel fleeing for assistance.
            // Currently this will only stop creatures fleeing due to low health that could not find nearby allies to flee towards.
            target.SetControlled(false, UnitState.Fleeing);
        }

        [AuraEffectHandler(AuraType.ModRootDisableGravity)]
        private void HandleAuraModRootAndDisableGravity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Root);

            // Do not remove DisableGravity if there are more than this auraEffect of that kind on the unit or if it's a creature with DisableGravity on its movement template.
            if (!apply &&
                (target.HasAuraType(GetAuraType()) || target.HasAuraType(AuraType.ModStunDisableGravity) || target.IsCreature() && target.ToCreature().GetMovementTemplate().Flight == CreatureFlightMovementType.DisableGravity))
                return;

            if (target.SetDisableGravity(apply))
                if (!apply &&
                    !target.IsFlying())
                    target.GetMotionMaster().MoveFall();
        }

        [AuraEffectHandler(AuraType.ModStunDisableGravity)]
        private void HandleAuraModStunAndDisableGravity(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            target.SetControlled(apply, UnitState.Stunned);

            if (apply)
                target.GetThreatManager().EvaluateSuppressed();

            // Do not remove DisableGravity if there are more than this auraEffect of that kind on the unit or if it's a creature with DisableGravity on its movement template.
            if (!apply &&
                (target.HasAuraType(GetAuraType()) || target.HasAuraType(AuraType.ModStunDisableGravity) || target.IsCreature() && target.ToCreature().GetMovementTemplate().Flight == CreatureFlightMovementType.DisableGravity))
                return;

            if (target.SetDisableGravity(apply))
                if (!apply &&
                    !target.IsFlying())
                    target.GetMotionMaster().MoveFall();
        }

    }
}