// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
namespace Scripts.Spells.Warrior
{
    // 70844 - Item - Warrior T10 Protection 4P Bonus
    [Script] // 7.1.5
    internal class spell_warr_item_t10_prot_4p_bonus : AuraScript, IAuraOnProc
    {
        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(SpellIds.STOICISM) && spellInfo.GetEffects().Count > 1;
        }

        public void OnProc(ProcEventInfo eventInfo)
        {
            PreventDefaultAction();

            Unit target = eventInfo.GetActionTarget();
            int bp0 = (int)MathFunctions.CalculatePct(target.GetMaxHealth(), GetEffectInfo(1).CalcValue());
            CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
            args.AddSpellMod(SpellValueMod.BasePoint0, bp0);
            target.CastSpell((Unit)null, SpellIds.STOICISM, args);
        }
    }
}