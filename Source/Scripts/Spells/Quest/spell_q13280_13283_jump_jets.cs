using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Quest;

[Script] // 4336 - Jump Jets
internal class spell_q13280_13283_jump_jets : SpellScript, ISpellOnCast
{
	public void OnCast()
	{
		var caster = GetCaster();

		if (caster.IsVehicle())
		{
			var rocketBunny = caster.GetVehicleKit().GetPassenger(1);

			rocketBunny?.CastSpell(rocketBunny, QuestSpellIds.JumpRocketBlast, true);
		}
	}
}