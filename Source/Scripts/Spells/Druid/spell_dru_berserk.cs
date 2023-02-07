// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid
{
    [Script] // 77758 - Berserk
    internal class spell_dru_berserk : SpellScript, ISpellBeforeCast
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(DruidSpellIds.BearForm);
        }

        public void BeforeCast()
        {
            // Change into cat form
            if (GetCaster().GetShapeshiftForm() != ShapeShiftForm.BearForm)
                GetCaster().CastSpell(GetCaster(), DruidSpellIds.BearForm, true);
        }
    }
}