using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Warrior
{
    //262231
    [Script]
    public class war_machine : ScriptObjectAutoAdd, IPlayerOnPVPKill, IPlayerOnCreatureKill
    {
        public war_machine() : base("war_machine")
        {
        }

        public void OnPVPKill(Player killer, Player killed)
        {
            if (killer.GetClass() != Class.Warrior)
            {
                return;
            }

            if (!killer.HasAura(WarriorSpells.SPELL_WARRRIOR_WAR_MACHINE_BUFF) && killer.HasAura(WarriorSpells.WAR_MACHINE))
            {
                killer.CastSpell(null, WarriorSpells.SPELL_WARRRIOR_WAR_MACHINE_BUFF, true);
            }
        }

        public void OnCreatureKill(Player killer, Creature killed)
        {
            if (killer.GetClass() != Class.Warrior)
            {
                return;
            }

            if (!killer.HasAura(WarriorSpells.SPELL_WARRRIOR_WAR_MACHINE_BUFF) && killer.HasAura(WarriorSpells.WAR_MACHINE))
            {
                killer.CastSpell(null, WarriorSpells.SPELL_WARRRIOR_WAR_MACHINE_BUFF, true);
            }
        }
    }
}
