using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_purify_helboar_meat : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().GetTypeId() == TypeId.Player;
	}

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(ItemSpellIds.SummonPurifiedHelboarMeat, ItemSpellIds.SummonToxicHelboarMeat);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster = GetCaster();
		caster.CastSpell(caster, RandomHelper.randChance(50) ? ItemSpellIds.SummonPurifiedHelboarMeat : ItemSpellIds.SummonToxicHelboarMeat, true);
	}
}