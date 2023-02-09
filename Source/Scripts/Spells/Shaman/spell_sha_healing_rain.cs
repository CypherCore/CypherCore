using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 73920 - Healing Rain
internal class spell_sha_healing_rain : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		Aura aura = GetHitAura();

		if (aura != null)
		{
			WorldLocation dest = GetExplTargetDest();

			if (dest != null)
			{
				int        duration = GetSpellInfo().CalcDuration(GetOriginalCaster());
				TempSummon summon   = GetCaster().GetMap().SummonCreature(CreatureIds.HealingRainInvisibleStalker, dest, null, (uint)duration, GetOriginalCaster());

				if (summon == null)
					return;

				summon.CastSpell(summon, ShamanSpells.HealingRainVisual, true);

				var script = aura.GetScript<spell_sha_healing_rain_AuraScript>();

				script?.SetVisualDummy(summon);
			}
		}
	}
}