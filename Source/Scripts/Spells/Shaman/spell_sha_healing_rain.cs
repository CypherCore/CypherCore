using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

// 73920 - Healing Rain
[SpellScript(73920)]
internal class spell_sha_healing_rain : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		var aura = GetHitAura();

		if (aura != null)
		{
			var dest = GetExplTargetDest();

			if (dest != null)
			{
				var duration = GetSpellInfo().CalcDuration(GetOriginalCaster());
				var summon   = GetCaster().GetMap().SummonCreature(CreatureIds.HealingRainInvisibleStalker, dest, null, (uint)duration, GetOriginalCaster());

				if (summon == null)
					return;

				summon.CastSpell(summon, ShamanSpells.HealingRainVisual, true);

				var script = aura.GetScript<spell_sha_healing_rain_AuraScript>();

				script?.SetVisualDummy(summon);
			}
		}
	}
}