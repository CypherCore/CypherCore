// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;

namespace Game.Spells
{
    internal class DelayedSpellTeleportEvent : BasicEvent
    {
        private readonly TeleportToOptions _options;
        private readonly uint _spellId;
        private readonly Unit _target;
        private readonly WorldLocation _targetDest;

        public DelayedSpellTeleportEvent(Unit target, WorldLocation targetDest, TeleportToOptions options, uint spellId)
        {
            _target = target;
            _targetDest = targetDest;
            _options = options;
            _spellId = spellId;
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            if (_targetDest.GetMapId() == _target.GetMapId())
            {
                _target.NearTeleportTo(_targetDest, (_options & TeleportToOptions.Spell) != 0);
            }
            else
            {
                Player player = _target.ToPlayer();

                if (player != null)
                    player.TeleportTo(_targetDest, _options);
                else
                    Log.outError(LogFilter.Spells, $"Spell::EffectTeleportUnitsWithVisualLoadingScreen - spellId {_spellId} attempted to teleport creature to a different map.");
            }

            return true;
        }
    }
}