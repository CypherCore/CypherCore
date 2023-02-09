using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.DeathKnight;

[Script] // 70656 - Advantage (T10 4P Melee Bonus)
internal class spell_dk_advantage_t10_4p : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var caster = eventInfo.GetActor();

		if (caster)
		{
			var player = caster.ToPlayer();

			if (!player ||
			    caster.GetClass() != Class.Deathknight)
				return false;

			for (byte i = 0; i < player.GetMaxPower(PowerType.Runes); ++i)
				if (player.GetRuneCooldown(i) == 0)
					return false;

			return true;
		}

		return false;
	}
}