using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 33060 Make a Wish
internal class spell_item_make_a_wish : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().GetTypeId() == TypeId.Player;
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.MrPinchysBlessing, ItemSpellIds.SummonMightyMrPinchy, ItemSpellIds.SummonFuriousMrPinchy, ItemSpellIds.TinyMagicalCrawdad, ItemSpellIds.MrPinchysGift);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(int effIndex)
	{
		var caster  = GetCaster();
		var spellId = ItemSpellIds.MrPinchysGift;

		switch (RandomHelper.URand(1, 5))
		{
			case 1:
				spellId = ItemSpellIds.MrPinchysBlessing;

				break;
			case 2:
				spellId = ItemSpellIds.SummonMightyMrPinchy;

				break;
			case 3:
				spellId = ItemSpellIds.SummonFuriousMrPinchy;

				break;
			case 4:
				spellId = ItemSpellIds.TinyMagicalCrawdad;

				break;
		}

		caster.CastSpell(caster, spellId, true);
	}
}