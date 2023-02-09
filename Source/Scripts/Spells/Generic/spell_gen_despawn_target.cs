using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_despawn_target : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDespawn, SpellConst.EffectAll, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDespawn(uint effIndex)
	{
		if (GetEffectInfo().IsEffect(SpellEffectName.Dummy) ||
		    GetEffectInfo().IsEffect(SpellEffectName.ScriptEffect))
		{
			var target = GetHitCreature();

			target?.DespawnOrUnsummon();
		}
	}
}