using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_bonked : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		Player target = GetHitPlayer();

		if (target)
		{
			Aura aura = GetHitAura();

			if (!(aura != null && aura.GetStackAmount() == 3))
				return;

			target.CastSpell(target, GenericSpellIds.FormSwordDefeat, true);
			target.RemoveAurasDueToSpell(GenericSpellIds.Bonked);

			aura = target.GetAura(GenericSpellIds.Onguard);

			if (aura != null)
			{
				Item item = target.GetItemByGuid(aura.GetCastItemGUID());

				if (item)
					target.DestroyItemCount(item.GetEntry(), 1, true);
			}
		}
	}
}