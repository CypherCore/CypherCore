using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(195452)]
public class spell_rog_nightblade_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleLaunch(int UnnamedParameter)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		if (caster == null || target == null)
			return;

		target.RemoveAurasDueToSpell(RogueSpells.SPELL_ROGUE_NIGHTBLADE, caster.GetGUID());
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleLaunch, 0, SpellEffectName.ApplyAura, SpellScriptHookType.LaunchTarget));
	}
}