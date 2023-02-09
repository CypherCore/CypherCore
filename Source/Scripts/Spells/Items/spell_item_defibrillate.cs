using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_goblin_jumper_cables", 33u, ItemSpellIds.GoblinJumperCablesFail)]
[Script("spell_item_goblin_jumper_cables_xl", 50u, ItemSpellIds.GoblinJumperCablesXlFail)]
[Script("spell_item_gnomish_army_knife", 67u, 0u)]
internal class spell_item_defibrillate : SpellScript, IHasSpellEffects
{
	private readonly uint _chance;
	private readonly uint _failSpell;

	public spell_item_defibrillate(uint chance, uint failSpell)
	{
		_chance    = chance;
		_failSpell = failSpell;
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		if (_failSpell != 0 &&
		    !ValidateSpellInfo(_failSpell))
			return false;

		return true;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Resurrect, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleScript(uint effIndex)
	{
		if (RandomHelper.randChance(_chance))
		{
			PreventHitDefaultEffect(effIndex);

			if (_failSpell != 0)
				GetCaster().CastSpell(GetCaster(), _failSpell, new CastSpellExtraArgs(GetCastItem()));
		}
	}
}