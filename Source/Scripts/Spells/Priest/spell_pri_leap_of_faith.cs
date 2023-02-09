using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(73325)]
public class spell_pri_leap_of_faith : SpellScript, IHasSpellEffects, ISpellOnHit
{
	public List<ISpellEffect> SpellEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return Global.SpellMgr.GetSpellInfo(PriestSpells.SPELL_PRIEST_LEAP_OF_FAITH_GLYPH, Difficulty.None) != null && Global.SpellMgr.GetSpellInfo(PriestSpells.SPELL_PRIEST_LEAP_OF_FAITH_EFFECT, Difficulty.None) != null;
	}

	private void HandleScript(int UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (caster.HasAura(PriestSpells.SPELL_PRIEST_LEAP_OF_FAITH_GLYPH))
			GetHitUnit().RemoveMovementImpairingAuras(false);

		GetHitUnit().CastSpell(caster, PriestSpells.SPELL_PRIEST_LEAP_OF_FAITH_EFFECT, true);
	}

	public void OnHit()
	{
		var _player = GetCaster().ToPlayer();

		if (_player != null)
		{
			var target = GetHitUnit();

			if (target != null)
			{
				target.CastSpell(_player, PriestSpells.SPELL_PRIEST_LEAP_OF_FAITH_JUMP, true);

				if (_player.HasAura(PriestSpells.SPELL_PRIEST_BODY_AND_SOUL_AURA))
					_player.CastSpell(target, PriestSpells.SPELL_PRIEST_BODY_AND_SOUL_SPEED, true);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}