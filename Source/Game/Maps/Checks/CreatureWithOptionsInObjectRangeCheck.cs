// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved._hp
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps.Checks
{
    public class CreatureWithOptionsInObjectRangeCheck<T> : ICheck<Creature> where T : NoopCheckCustomizer
    {
        private readonly T _customizer;
        private readonly WorldObject _obj;
        private FindCreatureOptions _args;

        public CreatureWithOptionsInObjectRangeCheck(WorldObject obj, T customizer, FindCreatureOptions args)
        {
            _obj = obj;
            _args = args;
            _customizer = customizer;
        }

        public bool Invoke(Creature u)
        {
            if (u.GetDeathState() == DeathState.Dead) // Despawned
                return false;

            if (u.GetGUID() == _obj.GetGUID())
                return false;

            if (!_customizer.Test(u))
                return false;

            if (_args.CreatureId.HasValue &&
                u.GetEntry() != _args.CreatureId)
                return false;

            if (_args.StringId != null &&
                !u.HasStringId(_args.StringId))
                return false;

            if (_args.IsAlive.HasValue &&
                u.IsAlive() != _args.IsAlive)
                return false;

            if (_args.IsSummon.HasValue &&
                u.IsSummon() != _args.IsSummon)
                return false;

            if (_args.IsInCombat.HasValue &&
                u.IsInCombat() != _args.IsInCombat)
                return false;

            if (_args.OwnerGuid.HasValue && u.GetOwnerGUID() != _args.OwnerGuid ||
                _args.CharmerGuid.HasValue && u.GetCharmerGUID() != _args.CharmerGuid ||
                _args.CreatorGuid.HasValue && u.GetCreatorGUID() != _args.CreatorGuid ||
                _args.DemonCreatorGuid.HasValue && u.GetDemonCreatorGUID() != _args.DemonCreatorGuid ||
                _args.PrivateObjectOwnerGuid.HasValue && u.GetPrivateObjectOwner() != _args.PrivateObjectOwnerGuid)
                return false;

            if (_args.IgnorePrivateObjects &&
                u.IsPrivateObject())
                return false;

            if (_args.IgnoreNotOwnedPrivateObjects &&
                !u.CheckPrivateObjectOwnerVisibility(_obj))
                return false;

            if (_args.AuraSpellId.HasValue &&
                !u.HasAura((uint)_args.AuraSpellId))
                return false;

            _customizer.Update(u);

            return true;
        }
    }
}