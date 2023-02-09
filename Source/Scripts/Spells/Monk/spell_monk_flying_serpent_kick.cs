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
		if (Global.SpellMgr.GetSpellInfo(MonkSpells.SPELL_MONK_FLYING_SERPENT_KICK_NEW, Difficulty.None) != null)
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
				if (_player.HasAura(MonkSpells.SPELL_MONK_FLYING_SERPENT_KICK))
					_player.RemoveAura(MonkSpells.SPELL_MONK_FLYING_SERPENT_KICK);

				if (caster.HasAura(MonkSpells.SPELL_MONK_ITEM_PVP_GLOVES_BONUS))
					caster.RemoveAurasByType(AuraType.ModDecreaseSpeed);

				_player.CastSpell(_player, MonkSpells.SPELL_MONK_FLYING_SERPENT_KICK_AOE, true);
			}
		}
	}
}