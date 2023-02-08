using Framework.Constants;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.DeathKnight;

[SpellScript(47568)]
public class spell_dk_empower_rune_weapon : SpellScript
{
	public void OnHit()
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			Player player = caster.ToPlayer();
			if (player != null)
			{
				for (byte i = 0; i < PlayerConst.MaxRunes; ++i)
				{
					player.SetRuneCooldown(i, 0);
				}
				player.ResyncRunes();
			}
		}
	}
}