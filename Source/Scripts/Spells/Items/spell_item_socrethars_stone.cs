using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_socrethars_stone : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return (GetCaster().GetAreaId() == 3900 || GetCaster().GetAreaId() == 3742);
	}

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(ItemSpellIds.SocretharToSeat, ItemSpellIds.SocretharFromSeat);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = GetCaster();

		switch (caster.GetAreaId())
		{
			case 3900:
				caster.CastSpell(caster, ItemSpellIds.SocretharToSeat, true);

				break;
			case 3742:
				caster.CastSpell(caster, ItemSpellIds.SocretharFromSeat, true);

				break;
			default:
				return;
		}
	}
}