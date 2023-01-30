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
        /***  SKILLS & TALENTS    ***/
        /****************************/
        [AuraEffectHandler(AuraType.ModSkill)]
        [AuraEffectHandler(AuraType.ModSkill2)]
        private void HandleAuraModSkill(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.ChangeAmountMask | AuraEffectHandleModes.Skill))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            SkillType prot = (SkillType)GetMiscValue();
            int points = GetAmount();

            if (prot == SkillType.Defense)
                return;

            target.ModifySkillBonus(prot, apply ? points : -points, GetAuraType() == AuraType.ModSkillTalent);
        }

        [AuraEffectHandler(AuraType.AllowTalentSwapping)]
        private void HandleAuraAllowTalentSwapping(AuraApplication aurApp, AuraEffectHandleModes mode, bool apply)
        {
            if (!mode.HasAnyFlag(AuraEffectHandleModes.Real))
                return;

            Player target = aurApp.GetTarget().ToPlayer();

            if (target == null)
                return;

            if (apply)
                target.SetUnitFlag2(UnitFlags2.AllowChangingTalents);
            else if (!target.HasAuraType(GetAuraType()))
                target.RemoveUnitFlag2(UnitFlags2.AllowChangingTalents);
        }

    }
}