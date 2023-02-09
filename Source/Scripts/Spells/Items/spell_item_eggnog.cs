using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 21149 - Egg Nog
internal class spell_item_eggnog : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.EggNogReindeer, ItemSpellIds.EggNogSnowman);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 2, SpellEffectName.Inebriate, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		if (RandomHelper.randChance(40))
			GetCaster().CastSpell(GetHitUnit(), RandomHelper.randChance(50) ? ItemSpellIds.EggNogReindeer : ItemSpellIds.EggNogSnowman, GetCastItem());
	}
}