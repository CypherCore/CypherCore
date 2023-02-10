using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	//204730 - Fear (effect)
	[SpellScript(204730)]
	public class spell_warl_fear_buff : SpellScript, ISpellAfterHit
	{
		public override bool Validate(SpellInfo UnnamedParameter)
		{
			if (Global.SpellMgr.GetSpellInfo(WarlockSpells.FEAR_BUFF, Difficulty.None) != null)
				return false;

			return true;
		}

		public void AfterHit()
		{
			var aura = GetHitAura();

			if (aura != null)
			{
				aura.SetMaxDuration(20000);
				aura.SetDuration(20000);
				aura.RefreshDuration();
			}
		}
	}
}