using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 29830 - Mirren's Drinking Hat
internal class spell_item_mirrens_drinking_hat : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScriptEffect, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScriptEffect(uint effIndex)
	{
		uint spellId = 0;

		switch (RandomHelper.URand(1, 6))
		{
			case 1:
			case 2:
			case 3:
				spellId = ItemSpellIds.LochModanLager;

				break;
			case 4:
			case 5:
				spellId = ItemSpellIds.StouthammerLite;

				break;
			case 6:
				spellId = ItemSpellIds.AeriePeakPaleAle;

				break;
			default:
				return;
		}

		var caster = GetCaster();
		caster.CastSpell(caster, spellId, new CastSpellExtraArgs(GetSpell()));
	}
}