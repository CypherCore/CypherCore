using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_captain_cookie;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(47754)]
    public class npc_captain_cookie_cauldron : ScriptedAI
    {
        public npc_captain_cookie_cauldron(Creature pCreature) : base(pCreature)
        {
            me.SetReactState(ReactStates.Passive);
            me.SetUnitFlag(UnitFlags.Uninteractible);
        }

        public override void Reset()
        {
            DoCast(me, eSpell.SPELL_CAULDRON_VISUAL, new Game.Spells.CastSpellExtraArgs(true));
            DoCast(me, eSpell.SPELL_CAULDRON_FIRE);
            me.SetUnitFlag(UnitFlags.Stunned);
        }
    }
}
