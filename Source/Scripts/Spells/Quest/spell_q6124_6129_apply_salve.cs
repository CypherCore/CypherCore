// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 19512 Apply Salve
internal class spell_q6124_6129_apply_salve : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		return GetCaster().IsTypeId(TypeId.Player);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		var caster = GetCaster().ToPlayer();

		if (GetCastItem())
		{
			var creatureTarget = GetHitCreature();

			if (creatureTarget)
			{
				uint newEntry = 0;

				switch (caster.GetTeam())
				{
					case Team.Horde:
						if (creatureTarget.GetEntry() == CreatureIds.SicklyGazelle)
							newEntry = CreatureIds.CuredGazelle;

						break;
					case Team.Alliance:
						if (creatureTarget.GetEntry() == CreatureIds.SicklyDeer)
							newEntry = CreatureIds.CuredDeer;

						break;
				}

				if (newEntry != 0)
				{
					creatureTarget.UpdateEntry(newEntry);
					creatureTarget.DespawnOrUnsummon(Misc.DespawnTime);
					caster.KilledMonsterCredit(newEntry);
				}
			}
		}
	}
}