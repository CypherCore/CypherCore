// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_nitro_boosts : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		if (!GetCastItem())
			return false;

		return true;
	}

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(ItemSpellIds.NitroBoostsSuccess, ItemSpellIds.NitroBoostsBackfire);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(int effIndex)
	{
		var caster    = GetCaster();
		var areaEntry = CliDB.AreaTableStorage.LookupByKey(caster.GetAreaId());
		var success   = true;

		if (areaEntry != null &&
		    areaEntry.IsFlyable() &&
		    !caster.GetMap().IsDungeon())
			success = RandomHelper.randChance(95);

		caster.CastSpell(caster, success ? ItemSpellIds.NitroBoostsSuccess : ItemSpellIds.NitroBoostsBackfire, new CastSpellExtraArgs(GetCastItem()));
	}
}