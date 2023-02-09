using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(209795)]
public class spell_dh_fracture : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleHit(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		//  for (uint8 i = 0; i < 2; ++i)
		//caster->CastCustomSpell(SPELL_DH_SHATTERED_SOULS_MISSILE, SpellValueMod.BasePoint0, SPELL_DH_LESSER_SOUL_SHARD, caster, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}