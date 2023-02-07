using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Spells.Druid;

[CreatureScript(47649)]
public class npc_dru_efflorescence : ScriptedAI
{
	public npc_dru_efflorescence(Creature creature) : base(creature)
	{
	}

	public override void Reset()
	{
		me.CastSpell(me, EfflorescenceSpells.SPELL_DRUID_EFFLORESCENCE_DUMMY, true);
		me.SetUnitFlag(UnitFlags.NonAttackable);
		me.SetUnitFlag(UnitFlags.Uninteractible);
		me.SetUnitFlag(UnitFlags.RemoveClientControl);
		me.SetReactState(ReactStates.Passive);
	}
}