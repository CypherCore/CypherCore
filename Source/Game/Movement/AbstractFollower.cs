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

using Game.Entities;

namespace Game.Movement
{
    public class AbstractFollower
    {
        Unit _target;

        public AbstractFollower(Unit target = null) { SetTarget(target); }

        public void SetTarget(Unit unit)
        {
            if (unit == _target)
                return;

            if (_target)
                _target.FollowerRemoved(this);

            _target = unit;

            if (_target)
                _target.FollowerAdded(this);
        }

        public Unit GetTarget() { return _target; }
    }
}
