// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class MessageDistDelivererToHostile<T> : Notifier where T : IDoWork<Player>
    {
        private readonly float _distSq;
        private readonly T _packetSender;
        private readonly PhaseShift _phaseShift;
        private readonly Unit _source;

        public MessageDistDelivererToHostile(Unit src, T packetSender, float dist)
        {
            _source = src;
            _packetSender = packetSender;
            _phaseShift = src.GetPhaseShift();
            _distSq = dist * dist;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                if (!player.InSamePhase(_phaseShift))
                    continue;

                if (player.GetExactDist2dSq(_source) > _distSq)
                    continue;

                // Send packet to all who are sharing the player's vision
                if (player.HasSharedVision())
                    foreach (var visionPlayer in player.GetSharedVisionList())
                        if (visionPlayer.SeerView == player)
                            SendPacket(visionPlayer);

                if (player.SeerView == player ||
                    player.GetVehicle())
                    SendPacket(player);
            }
        }

        public override void Visit(IList<Creature> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Creature creature = objs[i];

                if (!creature.InSamePhase(_phaseShift))
                    continue;

                if (creature.GetExactDist2dSq(_source) > _distSq)
                    continue;

                // Send packet to all who are sharing the creature's vision
                if (creature.HasSharedVision())
                    foreach (var player in creature.GetSharedVisionList())
                        if (player.SeerView == creature)
                            SendPacket(player);
            }
        }

        public override void Visit(IList<DynamicObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                DynamicObject dynamicObject = objs[i];

                if (!dynamicObject.InSamePhase(_phaseShift))
                    continue;

                if (dynamicObject.GetExactDist2dSq(_source) > _distSq)
                    continue;

                Unit caster = dynamicObject.GetCaster();

                if (caster != null)
                {
                    // Send packet back to the caster if the caster has vision of dynamic object
                    Player player = caster.ToPlayer();

                    if (player && player.SeerView == dynamicObject)
                        SendPacket(player);
                }
            }
        }

        private void SendPacket(Player player)
        {
            // never send packet to self
            if (player == _source ||
                !player.HaveAtClient(_source) ||
                player.IsFriendlyTo(_source))
                return;

            _packetSender.Invoke(player);
        }
    }
}