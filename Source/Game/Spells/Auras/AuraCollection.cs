using System;
using System.Collections.Generic;
using System.Linq;
using Game.Entities;

namespace Game.Spells.Auras
{
    public class AuraCollection
    {
        protected Dictionary<Guid, Aura> _auras = new();
        protected MultiMapHashSet<uint, Guid> _aurasBySpellId = new();
        protected MultiMapHashSet<ObjectGuid, Guid> _byCasterGuid  = new();
        protected HashSet<Guid> _isSingleTarget  = new();
        protected MultiMapHashSet<uint, Guid> _labelMap  = new();
        protected HashSet<Guid> _canBeSaved  = new();
        protected HashSet<Guid> _isgroupBuff  = new();
        protected HashSet<Guid> _isPassive  = new();
        protected HashSet<Guid> _isDeathPersistant  = new();
        protected HashSet<Guid> _isRequiringDeadTarget  = new();
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

                if (aura.IsSingleTarget())
                    _isSingleTarget.Add(aura.Guid);

                foreach (var label in aura.GetSpellInfo().Labels)
                    _labelMap.Add(label, aura.Guid);

                if (aura.CanBeSaved())
                    _canBeSaved.Add(aura.Guid);

                if (aura.GetSpellInfo().IsGroupBuff())
                    _isgroupBuff.Add(aura.Guid);

                if (aura.IsPassive())
                    _isPassive.Add(aura.Guid);

                if (aura.IsDeathPersistent())
                    _isDeathPersistant.Add(aura.Guid);

                if (aura.GetSpellInfo().IsRequiringDeadTarget())
                    _isRequiringDeadTarget.Add(aura.Guid);

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

                _isSingleTarget.Remove(aura.Guid);

                foreach (var label in aura.GetSpellInfo().Labels)
                    _labelMap.Remove(label, aura.Guid);

                if (aura.CanBeSaved())
                    _canBeSaved.Remove(aura.Guid);

                if (aura.GetSpellInfo().IsGroupBuff())
                    _isgroupBuff.Remove(aura.Guid);

                if (aura.IsPassive())
                    _isPassive.Remove(aura.Guid);

                if (aura.IsDeathPersistent())
                    _isDeathPersistant.Remove(aura.Guid);

                if (aura.GetSpellInfo().IsRequiringDeadTarget())
                    _isRequiringDeadTarget.Remove(aura.Guid);


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

            public HashSet<Guid> Results { get; } = new();

            internal AuraQuery(AuraCollection auraCollection)
            {
                _collection = auraCollection;
            }

            public AuraQuery HasSpellId(uint spellId)
            {
                lock (_collection._auras)
                    if (_collection._aurasBySpellId.TryGetValue(spellId, out var result))
                        Sync(result);

                return this;
            }

            public AuraQuery HasCasterGuid(ObjectGuid caster)
            {
                lock (_collection._auras)
                    if (!caster.IsEmpty() && _collection._byCasterGuid.TryGetValue(caster, out var result))
                        Sync(result);

                return this;
            }

            public AuraQuery HasLabel(uint label)
            {
                lock (_collection._auras)
                    if (_collection._labelMap.TryGetValue(label, out var result))
                        Sync(result);

                return this;
            }

            public AuraQuery HasAuraType(AuraObjectType label)
            {
                lock (_collection._auras)
                    if (_collection._typeMap.TryGetValue(label, out var result))
                        Sync(result);

                return this;
            }

            public AuraQuery IsSingleTarget(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isSingleTarget, t);

                return this;
            }

            public AuraQuery CanBeSaved(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._canBeSaved, t);

                return this;
            }

            public AuraQuery IsGroupBuff(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isgroupBuff, t);

                return this;
            }

            public AuraQuery IsPassive(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isPassive, t);

                return this;
            }

            public AuraQuery IsDeathPersistant(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isDeathPersistant, t);

                return this;
            }

            public AuraQuery IsRequiringDeadTarget(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isRequiringDeadTarget, t);

                return this;
            }

            public AuraQuery Execute(Action<uint, Aura, AuraRemoveMode> action, AuraRemoveMode auraRemoveMode = AuraRemoveMode.Default)
            {
                foreach (var aura in Results)
                {
                    var obj = _collection._auras[aura];

                    action(obj.GetSpellInfo().Id, obj, auraRemoveMode);
                }

                return this;
            }

            public IEnumerable<Aura> GetResults()
            {
                foreach (var aura in Results)
                    yield return _collection._auras[aura];
            }

            public AuraQuery ForEachResult(Action<Aura> action)
            {
                foreach (var ar in Results)
                    action(_collection._auras[ar]);

                return this;
            }

            public AuraQuery AlsoMatches(Func<Aura, bool> predicate)
            {
                Results.RemoveWhere(g => !predicate(_collection._auras[g]));
                return this;
            }

            private void Sync(HashSet<Guid> collection, bool contains = true)
            {
                if (_hasLoaded)
                {
                    if (collection != null && collection.Count != 0 && contains)
                        foreach (var a in collection)
                            Results.Add(a);

                    _hasLoaded = true;
                }
                else if (Results.Count != 0)
                    Results.RemoveWhere(r => collection.Contains(r) != contains);
            }
        }
    }

    
}
