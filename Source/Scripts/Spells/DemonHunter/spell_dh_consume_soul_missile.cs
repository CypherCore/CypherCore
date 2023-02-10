using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(210047)]
public class spell_dh_consume_soul_missile : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleHit(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var caster = GetCaster();

		if (caster == null)
			return;

		var spellToCast = GetSpellValue().EffectBasePoints[0];
		caster.CastSpell(caster, (uint)spellToCast, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHit));
	}
}