using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Mage;

[Script]
public class at_mage_frozen_orb : AreaTriggerAI
{
	public at_mage_frozen_orb(AreaTrigger areatrigger) : base(areatrigger)
	{
		damageInterval = 500;
	}

	public uint damageInterval;
	public bool procDone = false;

	public override void OnInitialize()
	{
		Unit caster = at.GetCaster();
		if (caster == null)
		{
			return;
		}

		Position pos = caster.GetPosition();

		at.MovePositionToFirstCollision(pos, 40.0f, 0.0f);
		at.SetDestination(pos, 4000);
	}

	public override void OnUpdate(uint diff)
	{
		Unit caster = at.GetCaster();
		if (caster == null || !caster.IsPlayer())
		{
			return;
		}

		if (damageInterval <= diff)
		{
			if (!procDone)
			{
				foreach (ObjectGuid guid in at.GetInsideUnits())
				{
					Unit unit = ObjectAccessor.Instance.GetUnit(caster, guid);
					if (unit != null)
					{
						if (caster.IsValidAttackTarget(unit))
						{
							if (caster.HasAura(MageSpells.SPELL_MAGE_FINGERS_OF_FROST_AURA))
							{
								caster.CastSpell(caster, MageSpells.SPELL_MAGE_FINGERS_OF_FROST_VISUAL_UI, true);
							}

							caster.CastSpell(caster, MageSpells.SPELL_MAGE_FINGERS_OF_FROST_AURA, true);

							// at->UpdateTimeToTarget(8000); TODO
							procDone = true;
							break;
						}
					}
				}
			}

			caster.CastSpell(at.GetPosition(), MageSpells.SPELL_MAGE_FROZEN_ORB_DAMAGE, true);
			damageInterval = 500;
		}
		else
		{
			damageInterval -= diff;
		}
	}
}