using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 53034 - Set Health Random
internal class spell_q28813_set_health_random : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		Unit caster = GetCaster();
		caster.SetHealth(caster.CountPctFromMaxHealth(RandomHelper.IRand(3, 5) * 10));
	}
}