using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warrior
{
	// Improved Whirlwind - 12950

	public class spell_warr_meat_cleaver : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo UnnamedParameter)
		{
			return false;
		}
	}
}