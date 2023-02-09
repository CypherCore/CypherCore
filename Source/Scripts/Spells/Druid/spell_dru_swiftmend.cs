using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(18562)]
public class spell_dru_swiftmend : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();


	private struct Spells
	{
		public static readonly uint SPELL_DRUID_SOUL_OF_THE_FOREST = 158478;
		public static readonly uint SPELL_DRUID_SOUL_OF_THE_FOREST_TRIGGERED = 114108;
	}


	private void HandleHit(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
			if (caster.HasAura(Spells.SPELL_DRUID_SOUL_OF_THE_FOREST))
				caster.AddAura(Spells.SPELL_DRUID_SOUL_OF_THE_FOREST_TRIGGERED, caster);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}
}