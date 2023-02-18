// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(115057)]
public class spell_monk_flying_serpent_kick : SpellScript, ISpellOnCast
{
	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(MonkSpells.FLYING_SERPENT_KICK_NEW, Difficulty.None) != null)
			return false;

		return true;
	}

	public void OnCast()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var _player = caster.ToPlayer();

			if (_player != null)
			{
				if (_player.HasAura(MonkSpells.FLYING_SERPENT_KICK))
					_player.RemoveAura(MonkSpells.FLYING_SERPENT_KICK);

				if (caster.HasAura(MonkSpells.ITEM_PVP_GLOVES_BONUS))
					caster.RemoveAurasByType(AuraType.ModDecreaseSpeed);

				_player.CastSpell(_player, MonkSpells.FLYING_SERPENT_KICK_AOE, true);
			}
		}
	}
}