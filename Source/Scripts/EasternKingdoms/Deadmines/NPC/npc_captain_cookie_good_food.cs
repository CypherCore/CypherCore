// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using static Scripts.EasternKingdoms.Deadmines.Bosses.boss_captain_cookie;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(new uint[] { 48006, 48294, 48296, 48297, 48300, 48301 })]
    public class npc_captain_cookie_good_food : ScriptedAI
    {
        public npc_captain_cookie_good_food(Creature pCreature) : base(pCreature)
        {
            _pInstance = pCreature.GetInstanceScript();
        }

        public override void JustDied(Unit killer)
        {
            me.DespawnOrUnsummon();
        }

        public override void UpdateAI(uint diff)
        {
            if (_pInstance == null)
            {
                return;
            }

            if (_pInstance.GetBossState(DMData.DATA_COOKIE) != EncounterState.InProgress)
            {
                me.DespawnOrUnsummon();
            }
        }

        public override bool OnGossipHello(Player player)
        {
            InstanceScript pInstance = me.GetInstanceScript();
            if (pInstance == null)
            {
                return true;
            }
            if (pInstance.GetBossState(DMData.DATA_COOKIE) != EncounterState.InProgress)
            {
                return true;
            }

            player.CastSpell(player, (player.GetMap().IsHeroic() ? eSpell.SETIATED_H : eSpell.SETIATED), true);

            me.DespawnOrUnsummon();
            return true;
        }


        private InstanceScript _pInstance;

    }
}
    

