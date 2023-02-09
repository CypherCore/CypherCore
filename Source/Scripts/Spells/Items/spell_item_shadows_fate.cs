using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script]
internal class spell_item_shadows_fate : AuraScript, IAuraOnProc
{
	public void OnProc(ProcEventInfo procInfo)
	{
		PreventDefaultAction();

		var caster = procInfo.GetActor();
		var target = GetCaster();

		if (!caster ||
		    !target)
			return;

		caster.CastSpell(target, ItemSpellIds.SoulFeast, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}
}