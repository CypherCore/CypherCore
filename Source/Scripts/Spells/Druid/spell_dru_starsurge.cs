using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(78674)]
public class spell_dru_starsurge : SpellScript, ISpellOnHit
{


	public void OnHit()
	{
		if (GetCaster())
		{
			if (GetCaster().GetAuraCount(DruidSpells.SPELL_DRU_STARLORD_BUFF) < 3)
			{
				GetCaster().CastSpell(null, DruidSpells.SPELL_DRU_STARLORD_BUFF, true);
			}
		}
	}


}