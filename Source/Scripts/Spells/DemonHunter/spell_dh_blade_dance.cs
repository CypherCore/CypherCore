// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[Script] // 210152 - Death Sweep
internal class spell_dh_blade_dance : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DemonHunterSpells.FirstBlood);
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(DecideFirstTarget, 0, Targets.UnitSrcAreaEnemy));
	}

	private void DecideFirstTarget(List<WorldObject> targetList)
	{
		if (targetList.Empty())
			return;

		var aura = GetCaster().GetAura(DemonHunterSpells.FirstBlood);

		if (aura == null)
			return;

		var firstTargetGUID = ObjectGuid.Empty;
		var selectedTarget  = GetCaster().GetTarget();

		// Prefer the selected Target if he is one of the enemies
		if (targetList.Count > 1 &&
		    !selectedTarget.IsEmpty())
		{
			var foundObj = targetList.Find(obj => obj.GetGUID() == selectedTarget);

			if (foundObj != null)
				firstTargetGUID = foundObj.GetGUID();
		}

		if (firstTargetGUID.IsEmpty())
			firstTargetGUID = targetList[0].GetGUID();

		var script = aura.GetScript<spell_dh_first_blood>();

		script?.SetFirstTarget(firstTargetGUID);
	}
}