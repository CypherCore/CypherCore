using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DeathKnight;

[SpellScript(194909)]
public class spell_dk_frozen_pulse : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo UnnamedParameter)
	{
		Unit caster = GetCaster();

		if (caster == null)
		{
			return false;
		}

		if (caster.GetPower(PowerType.Runes) > GetSpellInfo().GetEffect(1).BasePoints)
		{
			return false;
		}

		return true;
	}
}