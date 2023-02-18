// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(14769)]
public class spell_pri_improved_power_word_shield : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void HandleEffectCalcSpellMod(AuraEffect aurEff, ref SpellModifier spellMod)
	{
		if (spellMod == null)
		{
			var mod = new SpellModifierByClassMask(GetAura());
			spellMod.op      = (SpellModOp)aurEff.GetMiscValue();
			spellMod.type    = SpellModType.Pct;
			spellMod.spellId = GetId();
			mod.mask         = aurEff.GetSpellEffectInfo().SpellClassMask;
		}

		((SpellModifierByClassMask)spellMod).value = aurEff.GetAmount();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcSpellModHandler(HandleEffectCalcSpellMod, 0, AuraType.Dummy));
	}
}