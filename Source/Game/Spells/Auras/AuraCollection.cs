// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;
using static Game.AI.SmartEvent;

namespace Game.Spells.Auras
{
    public class AuraCollection
    {
        protected Dictionary<Guid, Aura> _auras = new(); // To keep this thread safe we have the guid as the key to all auras, The aura may be removed while preforming a query.
        protected MultiMapHashSet<uint, Guid> _aurasBySpellId = new();
        protected MultiMapHashSet<ObjectGuid, Guid> _byCasterGuid  = new();
        protected MultiMapHashSet<bool, Guid> _isSingleTarget  = new();
        protected MultiMapHashSet<uint, Guid> _labelMap  = new();
        protected MultiMapHashSet<bool, Guid> _canBeSaved  = new();
        protected MultiMapHashSet<bool, Guid> _isgroupBuff  = new();
        protected MultiMapHashSet<bool, Guid> _isPassive  = new();
        protected MultiMapHashSet<bool, Guid> _isDeathPersistant  = new();
        protected MultiMapHashSet<bool, Guid> _isRequiringDeadTarget  = new();
        protected MultiMapHashSet<ObjectGuid, Guid> _byCastId = new();
        protected MultiMapHashSet<AuraObjectType, Guid> _typeMap  = new();

        public void Add(Aura aura)
        {
            lock (_auras)
            {
                if (_auras.ContainsKey(aura.Guid))
                    return;

                _auras[aura.Guid] = aura;
                _aurasBySpellId.Add(aura.GetId(), aura.Guid);

                var casterGuid = aura.GetCasterGUID();

                if (!casterGuid.IsEmpty())
                    _byCasterGuid.Add(casterGuid, aura.Guid);

                _isSingleTarget.Add(aura.IsSingleTarget(), aura.Guid);

                foreach (var label in aura.GetSpellInfo().Labels)
                    _labelMap.Add(label, aura.Guid);

                var castId = aura.GetCastId();

                if (!castId.IsEmpty())
                    _byCastId.Add(castId, aura.Guid);

                _canBeSaved.Add(aura.CanBeSaved(), aura.Guid);
                _isgroupBuff.Add(aura.GetSpellInfo().IsGroupBuff(), aura.Guid);
                _isPassive.Add(aura.IsPassive(), aura.Guid);
                _isDeathPersistant.Add(aura.IsDeathPersistent(), aura.Guid);
                _isRequiringDeadTarget.Add(aura.GetSpellInfo().IsRequiringDeadTarget(), aura.Guid);
                _typeMap.Add(aura.GetAuraType(), aura.Guid);
            }
        }

        public void Remove(Aura aura)
        {
            lock (_auras)
            {
                _auras.Remove(aura.Guid);
                _aurasBySpellId.Remove(aura.GetId(), aura.Guid);

                var casterGuid = aura.GetCasterGUID();

                if (!casterGuid.IsEmpty())
                    _byCasterGuid.Remove(casterGuid, aura.Guid);

                _isSingleTarget.Remove(aura.IsSingleTarget(), aura.Guid);

                foreach (var label in aura.GetSpellInfo().Labels)
                    _labelMap.Remove(label, aura.Guid);

                var castId = aura.GetCastId();

                if (!castId.IsEmpty())
                    _byCastId.Remove(castId, aura.Guid);

                _canBeSaved.Remove(aura.CanBeSaved(), aura.Guid);
                _isgroupBuff.Remove(aura.GetSpellInfo().IsGroupBuff(), aura.Guid);
                _isPassive.Remove(aura.IsPassive(), aura.Guid);
                _isDeathPersistant.Remove(aura.IsDeathPersistent(), aura.Guid);
                _isRequiringDeadTarget.Remove(aura.GetSpellInfo().IsRequiringDeadTarget(), aura.Guid);
                _typeMap.Remove(aura.GetAuraType(), aura.Guid);
            }
        }

        public Aura GetByGuid(Guid guid)
        {
            lock(_auras)
            if (_auras.TryGetValue(guid, out var ret))
                return ret;

            return null;
        }

        public bool TryGetAuraByGuid(Guid guid, out Aura aura)
        {
            lock (_auras)
                return _auras.TryGetValue(guid, out aura);
        }

        public bool Contains(Aura aura)
        {
            lock(_auras)
                return _auras.ContainsKey(aura.Guid);
        }

        public List<Aura> Auras
        {
            get
            {
                lock (_auras)
                    return _auras.Values.ToList();
            }
        }

        public bool Empty()
        {
            lock(_auras)
                return _auras.Count == 0;
        }

        public int Count
        {
            get 
            {
                lock (_auras)
                    return _auras.Count;
            }
        }

        public AuraQuery Query()
        {
            return new AuraQuery(this);
        }

        public class AuraQuery
        {
            AuraCollection _collection;
            bool _hasLoaded = false;

            public HashSet<Guid> Results { get; private set; } = new();

            internal AuraQuery(AuraCollection auraCollection)
            {
                _collection = auraCollection;
            }

            public AuraQuery HasSpellId(uint spellId)
            {
                lock (_collection._auras)
                    if (_collection._aurasBySpellId.TryGetValue(spellId, out var result))
                        Sync(result);

                _hasLoaded = true;
                return this;
            }

            public AuraQuery HasCasterGuid(ObjectGuid caster)
            {
                lock (_collection._auras)
                    if (!caster.IsEmpty() && _collection._byCasterGuid.TryGetValue(caster, out var result))
                        Sync(result);

                _hasLoaded = true;
                return this;
            }

            public AuraQuery HasLabel(uint label)
            {
                lock (_collection._auras)
                    if (_collection._labelMap.TryGetValue(label, out var result))
                        Sync(result);

                _hasLoaded = true;
                return this;
            }

            public AuraQuery HasAuraType(AuraObjectType label)
            {
                lock (_collection._auras)
                    if (_collection._typeMap.TryGetValue(label, out var result))
                        Sync(result);

                _hasLoaded = true;
                return this;
            }

            public AuraQuery IsSingleTarget(bool t = true)
            {
                lock (_collection._auras)
                    if (_collection._isSingleTarget.TryGetValue(t, out var result))
                        Sync(result);

                _hasLoaded = true;
                return this;
            }

            public AuraQuery CanBeSaved(bool t = true)
            {
                lock (_collection._auras)
                    if (_collection._canBeSaved.TryGetValue(t, out var result))
                        Sync(result);

                _hasLoaded = true;
                return this;
            }

            public AuraQuery IsGroupBuff(bool t = true)
            {
                lock (_collection._auras)
                    if (_collection._isgroupBuff.TryGetValue(t, out var result))
                        Sync(result);

                _hasLoaded = true;
                return this;
            }

            public AuraQuery IsPassive(bool t = true)
            {
                lock (_collection._auras)
                    if (_collection._isPassive.TryGetValue(t, out var result))
                        Sync(result);

                _hasLoaded = true;
                return this;
            }

            public AuraQuery IsDeathPersistant(bool t = true)
            {
                lock (_collection._auras)
                    if (_collection._isDeathPersistant.TryGetValue(t, out var result))
                        Sync(result);

                _hasLoaded = true;
                return this;
            }

            public AuraQuery HasCastId(ObjectGuid Id)
            {
                lock (_collection._auras)
                    if (!Id.IsEmpty() && _collection._byCastId.TryGetValue(Id, out var result))
                        Sync(result);

                _hasLoaded = true;
                return this;
            }

            public AuraQuery IsRequiringDeadTarget(bool t = true)
            {
                lock (_collection._auras)
                    if (_collection._isRequiringDeadTarget.TryGetValue(t, out var result))
                        Sync(result);

                _hasLoaded = true;
                return this;
            }

            public AuraQuery Execute(Action<uint, Aura, AuraRemoveMode> action, AuraRemoveMode auraRemoveMode = AuraRemoveMode.Default)
            {
                foreach (var aura in Results)
                    if (_collection._auras.TryGetValue(aura, out var result))
                        action(result.GetSpellInfo().Id, result, auraRemoveMode);

                _hasLoaded = true;
                return this;
            }

            public IEnumerable<Aura> GetResults()
            {
                foreach (var aura in Results)
                    if (_collection._auras.TryGetValue(aura, out var result))
                        yield return result;
            }

            public AuraQuery ForEachResult(Action<Aura> action)
            {
                foreach (var aura in Results)
                    if (_collection._auras.TryGetValue(aura, out var result))
                        action(result);

                return this;
            }

            public AuraQuery AlsoMatches(Func<Aura, bool> predicate)
            {
                if (!_hasLoaded)
                {
                    lock (_collection._auras)
                        Results = _collection._auras.Keys.ToHashSet();
                }

                Results.RemoveWhere(g =>
                {
                    if (_collection._auras.TryGetValue(g, out var result))
                        return !predicate(result);

                    return true;
                });

                _hasLoaded = true;
                return this;
            }

            private void Sync(HashSet<Guid> collection)
            {
                if (!_hasLoaded)
                {
                    if (collection != null && collection.Count != 0)
                        foreach (var a in collection)
                            Results.Add(a);
                }
                else if (Results.Count != 0)
                    Results.RemoveWhere(r => !collection.Contains(r));
            }
        }
    }

    
}
