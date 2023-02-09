using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman
{
	// 215864 Rainfall
	[SpellScript(215864)]
	public class spell_sha_rainfall_SpellScript : SpellScript, ISpellOnHit
	{
		public void OnHit()
		{
			var pos = GetHitDest();

			if (pos != null)
				GetCaster().SummonCreature(ShamanNpcs.NPC_RAINFALL, pos);
		}
	}
}