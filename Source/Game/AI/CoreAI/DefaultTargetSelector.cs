// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.AI
{
	// default predicate function to select Target based on distance, player and/or aura criteria
	public class DefaultTargetSelector : ICheck<Unit>
	{
		private readonly int _aura;
		private readonly float _dist;
		private readonly Unit _exception;
		private readonly Unit _me;
		private readonly bool _playerOnly;

        /// <param Name="unit">the reference unit</param>
        /// <param Name="dist">if 0: ignored, if > 0: maximum distance to the reference unit, if < 0: minimum distance to the reference unit</param>
        /// <param Name="playerOnly">self explaining</param>
        /// <param Name="withTank">allow current tank to be selected</param>
        /// <param Name="aura">if 0: ignored, if > 0: the Target shall have the aura, if < 0, the Target shall NOT have the aura</param>
        public DefaultTargetSelector(Unit unit, float dist, bool playerOnly, bool withTank, int aura)
		{
			_me         = unit;
			_dist       = dist;
			_playerOnly = playerOnly;
			_exception  = !withTank ? unit.GetThreatManager().GetLastVictim() : null;
			_aura       = aura;
		}

		public bool Invoke(Unit target)
		{
			if (_me == null)
				return false;

			if (target == null)
				return false;

			if (_exception != null &&
			    target == _exception)
				return false;

			if (_playerOnly && !target.IsTypeId(TypeId.Player))
				return false;

			if (_dist > 0.0f &&
			    !_me.IsWithinCombatRange(target, _dist))
				return false;

			if (_dist < 0.0f &&
			    _me.IsWithinCombatRange(target, -_dist))
				return false;

			if (_aura != 0)
			{
				if (_aura > 0)
				{
					if (!target.HasAura((uint)_aura))
						return false;
				}
				else
				{
					if (target.HasAura((uint)-_aura))
						return false;
				}
			}

			return false;
		}
	}


}