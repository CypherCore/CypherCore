using Framework.Dynamic;
using Game.Entities;

namespace Scripts.Spells.DemonHunter;

public class event_dh_infernal_strike : BasicEvent
{
	public event_dh_infernal_strike(Unit caster)
	{
		this._caster = caster;
	}

	public override bool Execute(ulong UnnamedParameter, uint UnnamedParameter2)
	{
		if (_caster != null)
		{
			_caster.CastSpell(_caster, DemonHunterSpells.SPELL_DH_INFERNAL_STRIKE_DAMAGE, true);

			if (_caster.HasAura(DemonHunterSpells.SPELL_DH_RAIN_OF_CHAOS))
				_caster.CastSpell(_caster, DemonHunterSpells.SPELL_DH_RAIN_OF_CHAOS_SLOW, true);

			if (_caster.HasAura(DemonHunterSpells.SPELL_DH_ABYSSAL_STRIKE))
				_caster.CastSpell(_caster, DemonHunterSpells.SPELL_DH_SIGIL_OF_FLAME_NO_DEST, true);
		}

		return true;
	}

	private readonly Unit _caster;
}