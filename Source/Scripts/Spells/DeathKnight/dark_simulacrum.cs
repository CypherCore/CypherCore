using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DeathKnight;

[SpellScript(77606)]
public class dark_simulacrum : AuraScript, IAuraOnProc
{
	public void OnProc(ProcEventInfo info)
	{
		var spellInfo = info.GetSpellInfo();
		var player    = GetCaster().ToPlayer();
		var target    = GetTarget();

		if (spellInfo != null && player != null && target != null && target.IsValidAttackTarget(player, spellInfo))
		{
			player.CastSpell(target, spellInfo.Id, true);
		}
	}
}