// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Entities
{
    public class MovementForces
	{
		private List<MovementForce> _forces = new();
		private float _modMagnitude = 1.0f;

		public List<MovementForce> GetForces()
		{
			return _forces;
		}

		public bool Add(MovementForce newForce)
		{
			var movementForce = FindMovementForce(newForce.ID);

			if (movementForce == null)
			{
				_forces.Add(newForce);

				return true;
			}

			return false;
		}

		public bool Remove(ObjectGuid id)
		{
			var movementForce = FindMovementForce(id);

			if (movementForce != null)
			{
				_forces.Remove(movementForce);

				return true;
			}

			return false;
		}

		public float GetModMagnitude()
		{
			return _modMagnitude;
		}

		public void SetModMagnitude(float modMagnitude)
		{
			_modMagnitude = modMagnitude;
		}

		public bool IsEmpty()
		{
			return _forces.Empty() && _modMagnitude == 1.0f;
		}

		private MovementForce FindMovementForce(ObjectGuid id)
		{
			return _forces.Find(force => force.ID == id);
		}
	}
}