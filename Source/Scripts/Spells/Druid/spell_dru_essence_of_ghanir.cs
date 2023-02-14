// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(208253)]
public class spell_dru_essence_of_ghanir : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void HandleEffectCalcSpellMod(AuraEffect aurEff, ref SpellModifier spellMod)
	{
		if (spellMod == null)
		{
			var mod = new SpellModifierByClassMask(GetAura());
			mod.op      = SpellModOp.PeriodicHealingAndDamage;
			mod.type    = SpellModType.Flat;
			mod.spellId = GetId();
			mod.mask    = aurEff.GetSpellEffectInfo().SpellClassMask;
			spellMod    = mod;
		}

		((SpellModifierByClassMask)spellMod).value = aurEff.GetAmount() / 7;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcSpellModHandler(HandleEffectCalcSpellMod, 0, AuraType.AddPctModifier));
		AuraEffects.Add(new AuraEffectCalcSpellModHandler(HandleEffectCalcSpellMod, 0, AuraType.AddPctModifier));
	}
}