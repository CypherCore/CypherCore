/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Scripting;

namespace Scripts.Pets
{
    namespace Generic
    {
        struct SpellIds
        {
            public const uint FeelingFroggy = 43906;
            public const uint SeductionVisual = 43919;
        }

        struct TextIds
        {
            public const uint SayMojo = 0;
        }

        [Script]
        class npc_pet_gen_mojo : ScriptedAI
        {
            public npc_pet_gen_mojo(Creature creature) : base(creature) { }

            public override void Reset()
            {
                _victimGUID.Clear();

                Unit owner = me.GetOwner();
                if (owner)
                    me.GetMotionMaster().MoveFollow(owner, 0.0f, 0.0f);
            }

            public override void EnterCombat(Unit who) { }

            public override void UpdateAI(uint diff) { }

            public override void ReceiveEmote(Player player, TextEmotes emote)
            {
                me.HandleEmoteCommand((Emote)emote);
                Unit owner = me.GetOwner();
                if (emote != TextEmotes.Kiss || !owner || !owner.IsTypeId(TypeId.Player) ||
                    owner.ToPlayer().GetTeam() != player.GetTeam())
                {
                    return;
                }

                Talk(TextIds.SayMojo, player);

                if (!_victimGUID.IsEmpty())
                {
                    Player victim = Global.ObjAccessor.GetPlayer(me, _victimGUID);
                    if (victim)
                        victim.RemoveAura(SpellIds.FeelingFroggy);
                }

                _victimGUID = player.GetGUID();

                DoCast(player, SpellIds.FeelingFroggy, true);
                DoCast(me, SpellIds.SeductionVisual, true);
                me.GetMotionMaster().MoveFollow(player, 0.0f, 0.0f);
            }

            ObjectGuid _victimGUID;
        }
    }
}
