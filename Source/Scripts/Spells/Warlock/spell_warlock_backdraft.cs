using System;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Warlock
{
	// 117828 - Backdraft
	[SpellScript(117828)]
	internal class spell_warlock_backdraft : AuraScript, IAuraCheckProc
	{
		public bool CheckProc(ProcEventInfo UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return false;

			if (caster.VariableStorage.GetValue("Spells.BackdraftCD", DateTime.MinValue) > GameTime.Now())
				return false;

			caster.VariableStorage.Set("Spells.BackdraftCD", GameTime.Now() + TimeSpan.FromMilliseconds(500));

			return true;
		}
	}
}