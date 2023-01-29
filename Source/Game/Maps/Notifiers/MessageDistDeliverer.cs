// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Notifiers
{
    public class MessageDistDeliverer<T> : Notifier where T : IDoWork<Player>
    {
        private readonly float _distSq;
        private readonly T _packetSender;
        private readonly PhaseShift _phaseShift;
        private readonly WorldObject _source;
        private readonly bool _required3dDist;
        private readonly Player _skipped_receiver;
        private readonly Team _team;

        public MessageDistDeliverer(WorldObject src, T packetSender, float dist, bool own_team_only = false, Player skipped = null, bool req3dDist = false)
        {
            _source = src;
            _packetSender = packetSender;
            _phaseShift = src.GetPhaseShift();
            _distSq = dist * dist;

            if (own_team_only && src.IsPlayer())
                _team = src.ToPlayer().GetEffectiveTeam();

            _skipped_receiver = skipped;
            _required3dDist = req3dDist;
        }

        public override void Visit(IList<Player> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                Player player = objs[i];

                if (!player.InSamePhase(_phaseShift))
                    continue;

                if ((!_required3dDist ? player.GetExactDist2dSq(_source) : player.GetExactDistSq(_source)) > _distSq)
                    continue;

                // Send packet to all who are sharing the player's vision
                if (player.HasSharedVision())
                    foreach (var visionPlayer in player.GetSharedVisionList())
                        if (visionPlayer.SeerView == player)
                            SendPacket(visionPlayer);

                if (player.SeerView == player ||
                    player.GetVehicle() != null)
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

                if ((!_required3dDist ? creature.GetExactDist2dSq(_source) : creature.GetExactDistSq(_source)) > _distSq)
                    continue;

                // Send packet to all who are sharing the creature's vision
                if (creature.HasSharedVision())
                    foreach (var visionPlayer in creature.GetSharedVisionList())
                        if (visionPlayer.SeerView == creature)
                            SendPacket(visionPlayer);
            }
        }

        public override void Visit(IList<DynamicObject> objs)
        {
            for (var i = 0; i < objs.Count; ++i)
            {
                DynamicObject dynamicObject = objs[i];

                if (!dynamicObject.InSamePhase(_phaseShift))
                    continue;

                if ((!_required3dDist ? dynamicObject.GetExactDist2dSq(_source) : dynamicObject.GetExactDistSq(_source)) > _distSq)
                    continue;

                // Send packet back to the caster if the caster has vision of dynamic object
                Unit caster = dynamicObject.GetCaster();

                if (caster)
                {
                    Player player = caster.ToPlayer();

                    if (player && player.SeerView == dynamicObject)
                        SendPacket(player);
                }
            }
        }

        private void SendPacket(Player player)
        {
            // never send packet to self
            if (_source == player ||
                _team != 0 && player.GetEffectiveTeam() != _team ||
                _skipped_receiver == player)
                return;

            if (!player.HaveAtClient(_source))
                return;

            _packetSender.Invoke(player);
        }
    }

}