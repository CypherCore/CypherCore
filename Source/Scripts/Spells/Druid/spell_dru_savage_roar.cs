// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid
{
    [Script] // 52610 - Savage Roar
    internal class spell_dru_savage_roar : SpellScript, ISpellCheckCast
    {
        public SpellCastResult CheckCast()
        {
            Unit caster = GetCaster();

            if (caster.GetShapeshiftForm() != ShapeShiftForm.CatForm)
                return SpellCastResult.OnlyShapeshift;

            return SpellCastResult.SpellCastOk;
        }
    }
}