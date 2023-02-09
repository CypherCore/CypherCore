using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(271232)]
public class spell_monk_touch_of_death_passive : AuraScript, IAuraCheckProc
{

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.SPELL_MONK_TOUCH_OF_DEATH);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id != MonkSpells.SPELL_MONK_TOUCH_OF_DEATH)
		{
			return false;
		}
		return true;
	}
}