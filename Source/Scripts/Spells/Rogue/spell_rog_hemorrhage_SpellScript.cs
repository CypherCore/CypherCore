using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(16511)]
public class spell_rog_hemorrhage_SpellScript : SpellScript, ISpellOnHit, ISpellBeforeHit, ISpellAfterHit
{
	private bool _bleeding;

	public void OnHit()
	{
		Player _player = GetCaster().ToPlayer();
		if (_player != null)
		{
			if (GetHitUnit())
			{
				if (_player.HasAura(RogueSpells.SPELL_ROGUE_GLYPH_OF_HEMORRHAGE))
				{
					if (!_bleeding)
					{
						PreventHitAura();
						return;
					}
				}
			}
		}
	}

	public void BeforeHit(SpellMissInfo UnnamedParameter)
	{
		Unit target = GetHitUnit();
		if (target != null)
		{
			_bleeding = target.HasAuraState(AuraStateType.Bleed);
		}
	}

	public void AfterHit()
	{
		Unit caster = GetCaster();
		var  cp     = caster.GetPower(PowerType.ComboPoints);

		if (cp > 0)
		{
			caster.SetPower(PowerType.ComboPoints, cp - 1);
		}
	}
}