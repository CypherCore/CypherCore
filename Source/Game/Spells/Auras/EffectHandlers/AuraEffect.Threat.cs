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
        /***        THREAT        ***/
        /****************************/
        [AuraEffectHandler(AuraType.ModThreat)]
        private void HandleModThreat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            aurApp.GetTarget().GetThreatManager().UpdateMySpellSchoolModifiers();
        }

        [AuraEffectHandler(AuraType.ModTotalThreat)]
        private void HandleAuraModTotalThreat(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsAlive() ||
                !target.IsTypeId(TypeId.Player))
                return;

            Unit caster = GetCaster();

            if (caster != null &&
                caster.IsAlive())
                caster.GetThreatManager().UpdateMyTempModifiers();
        }

        [AuraEffectHandler(AuraType.ModTaunt)]
        private void HandleModTaunt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit target = aurApp.GetTarget();

            if (!target.IsAlive() ||
                !target.CanHaveThreatList())
                return;

            target.GetThreatManager().TauntUpdate();
        }

        [AuraEffectHandler(AuraType.ModDetaunt)]
        private void HandleModDetaunt(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Unit caster = GetCaster();
            Unit target = aurApp.GetTarget();

            if (!caster ||
                !caster.IsAlive() ||
                !target.IsAlive() ||
                !caster.CanHaveThreatList())
                return;

            caster.GetThreatManager().TauntUpdate();
        }

    }
}