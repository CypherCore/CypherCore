﻿using System;
using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;
using Game.Spells;

namespace Scripts.EasternKingdoms.Deadmines.NPC
{
    [CreatureScript(48284)]
    public class npc_mining_powder : ScriptedAI
    {
        public npc_mining_powder(Creature creature) : base(creature)
        {

        }

        private bool _damaged = false;

        public override void DamageTaken(Unit attacker, ref uint damage, DamageEffectType damageType, SpellInfo spellInfo = null)
        {
            if (_damaged)
            {
                return;
            }
            _damaged = true;
            me.CastSpell(me, DMSpells.SPELL_EXPLODE);
            me.DespawnOrUnsummon(TimeSpan.FromMilliseconds(100));
        }
    }
}