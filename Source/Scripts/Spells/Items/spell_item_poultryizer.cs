using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_poultryizer : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(ItemSpellIds.PoultryizerSuccess, ItemSpellIds.PoultryizerBackfire);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		if (GetCastItem() &&
		    GetHitUnit())
			GetCaster().CastSpell(GetHitUnit(), RandomHelper.randChance(80) ? ItemSpellIds.PoultryizerSuccess : ItemSpellIds.PoultryizerBackfire, new CastSpellExtraArgs(GetCastItem()));
	}
}