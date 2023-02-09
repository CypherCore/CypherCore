using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 57934 - Tricks of the Trade
internal class spell_rog_tricks_of_the_trade : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		Aura aura = GetHitAura();

		if (aura != null)
		{
			spell_rog_tricks_of_the_trade_aura script = aura.GetScript<spell_rog_tricks_of_the_trade_aura>();

			if (script != null)
			{
				Unit explTarget = GetExplTargetUnit();

				if (explTarget != null)
					script.SetRedirectTarget(explTarget.GetGUID());
				else
					script.SetRedirectTarget(ObjectGuid.Empty);
			}
		}
	}
}