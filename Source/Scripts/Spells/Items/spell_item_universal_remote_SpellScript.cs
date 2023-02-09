using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 8344 - Universal Remote (Gnomish Universal Remote)
internal class spell_item_universal_remote_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		if (!GetCastItem())
			return false;

		return true;
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.ControlMachine, ItemSpellIds.MobilityMalfunction, ItemSpellIds.TargetLock);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		Unit target = GetHitUnit();

		if (target)
		{
			uint chance = RandomHelper.URand(0, 99);

			if (chance < 15)
				GetCaster().CastSpell(target, ItemSpellIds.TargetLock, new CastSpellExtraArgs(GetCastItem()));
			else if (chance < 25)
				GetCaster().CastSpell(target, ItemSpellIds.MobilityMalfunction, new CastSpellExtraArgs(GetCastItem()));
			else
				GetCaster().CastSpell(target, ItemSpellIds.ControlMachine, new CastSpellExtraArgs(GetCastItem()));
		}
	}
}