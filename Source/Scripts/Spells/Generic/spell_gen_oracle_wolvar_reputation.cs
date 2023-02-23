// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_oracle_wolvar_reputation : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 1;
	}

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleDummy(int effIndex)
	{
		var player    = GetCaster().ToPlayer();
		var factionId = (uint)GetEffectInfo().CalcValue();
		double repChange = GetEffectInfo(1).CalcValue();

		var factionEntry = CliDB.FactionStorage.LookupByKey(factionId);

		if (factionEntry == null)
			return;

		// Set rep to baserep + basepoints (expecting spillover for oposite faction . become hated)
		// Not when player already has equal or higher rep with this faction
		if (player.GetReputationMgr().GetBaseReputation(factionEntry) < repChange)
			player.GetReputationMgr().SetReputation(factionEntry, repChange);

		// EFFECT_INDEX_2 most likely update at war State, we already handle this in SetReputation
	}
}