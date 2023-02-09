using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_pygmy_oil : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(ItemSpellIds.PygmyOilPygmyAura, ItemSpellIds.PygmyOilSmallerAura);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		Unit caster = GetCaster();
		Aura aura   = caster.GetAura(ItemSpellIds.PygmyOilPygmyAura);

		if (aura != null)
		{
			aura.RefreshDuration();
		}
		else
		{
			aura = caster.GetAura(ItemSpellIds.PygmyOilSmallerAura);

			if (aura == null ||
			    aura.GetStackAmount() < 5 ||
			    !RandomHelper.randChance(50))
			{
				caster.CastSpell(caster, ItemSpellIds.PygmyOilSmallerAura, true);
			}
			else
			{
				aura.Remove();
				caster.CastSpell(caster, ItemSpellIds.PygmyOilPygmyAura, true);
			}
		}
	}
}