using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(208244)]
public class spell_rog_roll_the_bones_visual_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


	private void Prevent(uint effIndex)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		if (caster.ToPlayer())
		{
			PreventHitAura();
			PreventHitDamage();
			PreventHitDefaultEffect(effIndex);
			PreventHitEffect(effIndex);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(Prevent, (byte)255, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
	}
}