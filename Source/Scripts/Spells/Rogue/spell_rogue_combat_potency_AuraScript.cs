using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Rogue;

[SpellScript(35551)]
public class spell_rogue_combat_potency_AuraScript : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		bool  offHand        = (eventInfo.GetDamageInfo().GetAttackType() == WeaponAttackType.OffAttack && RandomHelper.randChance(20));
		float mainRollChance = 20.0f * GetCaster().GetAttackTimer(WeaponAttackType.BaseAttack) / 1.4f / 600.0f;
		bool  mainHand       = (eventInfo.GetDamageInfo().GetAttackType() == WeaponAttackType.BaseAttack && RandomHelper.randChance(mainRollChance));
		return offHand || mainHand;
	}
}