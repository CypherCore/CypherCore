using Game.Entities;
using Game.Networking.Packets;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(193537)]
public class spell_rog_weaponmaster_AuraScript : AuraScript, IAuraCheckProc
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		Unit caster = eventInfo.GetActor();
		Unit target = eventInfo.GetActionTarget();
		if (target == null || caster == null)
		{
			return false;
		}

		SpellInfo triggerSpell = eventInfo.GetSpellInfo();
		if (triggerSpell == null)
		{
			return false;
		}

		if (!RandomHelper.randChance(6))
		{
			return false;
		}

		if (eventInfo.GetDamageInfo() != null)
		{
			return false;
		}

		SpellNonMeleeDamage damageLog = new SpellNonMeleeDamage(caster, target, triggerSpell, new SpellCastVisual(triggerSpell.GetSpellXSpellVisualId(), 0), triggerSpell.SchoolMask);
		damageLog.damage      = eventInfo.GetDamageInfo().GetDamage();
		damageLog.cleanDamage = damageLog.damage;
		caster.DealSpellDamage(damageLog, true);
		caster.SendSpellNonMeleeDamageLog(damageLog);
		return true;
	}


}