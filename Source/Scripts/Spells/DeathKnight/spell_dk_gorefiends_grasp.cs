// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[SpellScript(108199)]
public class spell_dk_gorefiends_grasp : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private void HandleScript(uint UnnamedParameter)
	{
		var _player = GetCaster().ToPlayer();

		if (_player != null)
		{
			var target = GetHitUnit();

			if (target != null)
			{
				var tempList = new List<Unit>();
				var gripList = new List<Unit>();

				_player.GetAttackableUnitListInRange(tempList, 20.0f);

				foreach (var itr in tempList)
				{
					if (itr.GetGUID() == _player.GetGUID())
						continue;

					if (!_player.IsValidAttackTarget(itr))
						continue;

					if (itr.IsImmunedToSpell(GetSpellInfo(), GetCaster()))
						continue;

					if (!itr.IsWithinLOSInMap(_player))
						continue;

					gripList.Add(itr);
				}

				foreach (var itr in gripList)
				{
					itr.CastSpell(target, DeathKnightSpells.SPELL_DK_DEATH_GRIP_ONLY_JUMP, true);
					itr.CastSpell(target, DeathKnightSpells.SPELL_DK_GOREFIENDS_GRASP_GRIP_VISUAL, true);
				}
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}
}