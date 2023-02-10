using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_proc_charge_drop_only : AuraScript, IAuraOnProc
{
	public void OnProc(ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
	}
}