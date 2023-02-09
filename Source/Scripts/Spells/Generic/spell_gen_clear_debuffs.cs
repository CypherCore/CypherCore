using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 34098 - ClearAllDebuffs
internal class spell_gen_clear_debuffs : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		Unit target = GetHitUnit();

		if (target)
			target.RemoveOwnedAuras(aura =>
			                        {
				                        SpellInfo spellInfo = aura.GetSpellInfo();

				                        return !spellInfo.IsPositive() && !spellInfo.IsPassive();
			                        });
	}
}