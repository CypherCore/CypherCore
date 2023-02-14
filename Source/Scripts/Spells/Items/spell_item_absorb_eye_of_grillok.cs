// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 38554 - Absorb Eye of Grillok (31463: Zezzak's Shard)
internal class spell_item_absorb_eye_of_grillok : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.EyeOfGrillok);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicTriggerSpell));
	}

	private void PeriodicTick(AuraEffect aurEff)
	{
		PreventDefaultAction();

		if (!GetCaster() ||
		    !GetTarget().IsTypeId(TypeId.Unit))
			return;

		GetCaster().CastSpell(GetCaster(), ItemSpellIds.EyeOfGrillok, new CastSpellExtraArgs(aurEff));
		GetTarget().ToCreature().DespawnOrUnsummon();
	}
}