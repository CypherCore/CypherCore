using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;

namespace Game.Spells.Auras
{
    public class AuraApplicationCollection
    {
        protected Dictionary<Guid, AuraApplication> _auras = new();
        protected MultiMapHashSet<uint, Guid> _aurasBySpellId = new();
        protected MultiMapHashSet<ObjectGuid, Guid> _byCasterGuid  = new();
        protected MultiMapHashSet<ObjectGuid, Guid> _byCastItemGuid = new();
        protected MultiMapHashSet<ObjectGuid, Guid> _byCastId = new();
        protected MultiMapHashSet<WorldObject, Guid> _byOwner = new();
        protected HashSet<Guid> _isSingleTarget  = new();
        protected MultiMapHashSet<uint, Guid> _labelMap  = new();
        protected HashSet<Guid> _canBeSaved  = new();
        protected HashSet<Guid> _isgroupBuff  = new();
        protected HashSet<Guid> _isPassive  = new();
        protected HashSet<Guid> _isDeathPersistant  = new();
        protected HashSet<Guid> _isRequiringDeadTarget  = new();
        protected HashSet<Guid> _isNotPlayer = new();
        protected HashSet<Guid> _isPerm = new();
        protected MultiMapHashSet<AuraObjectType, Guid> _typeMap  = new();
        protected MultiMapHashSet<int, Guid> _effectIndex = new();
        protected MultiMapHashSet<DiminishingGroup, Guid> _deminishGroup = new();
        protected MultiMapHashSet<AuraStateType, Guid> _casterAuraState = new();
        protected MultiMapHashSet<DispelType, Guid> _dispelType = new();
        protected HashSet<Guid> _hasNegitiveFlag = new();

        public void Add(AuraApplication auraApp)
        {
            lock (_auras)
            {
                if (_auras.ContainsKey(auraApp.Guid))
                    return;

                var aura = auraApp.GetBase();
                var si = aura.GetSpellInfo();
                _auras[auraApp.Guid] = auraApp;
                _aurasBySpellId.Add(aura.GetId(), auraApp.Guid);
              
                var casterGuid = aura.GetCasterGUID();

                if (!casterGuid.IsEmpty())
                    _byCasterGuid.Add(casterGuid, auraApp.Guid);

                if (!aura.GetCastItemGUID().IsEmpty())
                    _byCastItemGuid.Add(aura.GetCastItemGUID(), auraApp.Guid);

                if (aura.IsSingleTarget())
                    _isSingleTarget.Add(auraApp.Guid);

                foreach (var label in aura.GetSpellInfo().Labels)
                    _labelMap.Add(label, auraApp.Guid);

                if (aura.CanBeSaved())
                    _canBeSaved.Add(auraApp.Guid);

                if (aura.GetSpellInfo().IsGroupBuff())
                    _isgroupBuff.Add(auraApp.Guid);

                if (aura.IsPassive())
                    _isPassive.Add(auraApp.Guid);

                if (aura.IsDeathPersistent())
                    _isDeathPersistant.Add(auraApp.Guid);

                if (aura.GetSpellInfo().IsRequiringDeadTarget())
                    _isRequiringDeadTarget.Add(auraApp.Guid);

                var owner = aura.GetOwner();

                if (owner != null)
                    _byOwner.Add(owner, auraApp.Guid);

                var castId = aura.GetCastId();

                if (!castId.IsEmpty())
                    _byCastId.Add(castId, auraApp.Guid);

                if (!aura.GetCaster().IsPlayer())
                    _isNotPlayer.Add(auraApp.Guid);

                _deminishGroup.Add(si.GetDiminishingReturnsGroupForSpell(), auraApp.Guid);
                _casterAuraState.Add(si.CasterAuraState, auraApp.Guid);
                _typeMap.Add(aura.GetAuraType(), auraApp.Guid);
                _dispelType.Add(si.Dispel, auraApp.Guid);

                var flags = auraApp.GetFlags();

                if (flags.HasFlag(AuraFlags.Negative))
                    _hasNegitiveFlag.Add(auraApp.Guid);

                if (aura.IsPermanent())
                    _isPerm.Add(auraApp.Guid);
            }
        }

        public void Remove(AuraApplication auraApp)
        {
            lock (_auras)
            {
                var aura = auraApp.GetBase();
                var si = aura.GetSpellInfo();
                _auras.Remove(auraApp.Guid);
                _aurasBySpellId.Remove(aura.GetId(), aura.Guid);

                if (!aura.GetCaster().IsPlayer())
                    _isNotPlayer.Remove(auraApp.Guid);

                _deminishGroup.Remove(si.GetDiminishingReturnsGroupForSpell(), auraApp.Guid);
                _casterAuraState.Remove(si.CasterAuraState, auraApp.Guid);
                _typeMap.Remove(aura.GetAuraType(), auraApp.Guid);
                _dispelType.Remove(si.Dispel, auraApp.Guid);

                if (aura.CanBeSaved())
                    _canBeSaved.Remove(auraApp.Guid);

                if (aura.GetSpellInfo().IsGroupBuff())
                    _isgroupBuff.Remove(auraApp.Guid);

                if (aura.IsPassive())
                    _isPassive.Remove(auraApp.Guid);

                if (aura.IsDeathPersistent())
                    _isDeathPersistant.Remove(auraApp.Guid);

                if (aura.GetSpellInfo().IsRequiringDeadTarget())
                    _isRequiringDeadTarget.Remove(auraApp.Guid);

                if (!aura.GetCastItemGUID().IsEmpty())
                    _byCastItemGuid.Remove(aura.GetCastItemGUID(), auraApp.Guid);

                if (aura.IsSingleTarget())
                    _isSingleTarget.Remove(auraApp.Guid);

                foreach (var label in aura.GetSpellInfo().Labels)
                    _labelMap.Remove(label, auraApp.Guid);

                var casterGuid = aura.GetCasterGUID();

                if (!casterGuid.IsEmpty())
                    _byCasterGuid.Remove(casterGuid);

                var owner = aura.GetOwner();

                if (owner != null)
                    _byOwner.Remove(owner, auraApp.Guid);

                var castId = aura.GetCastId();

                if (!castId.IsEmpty())
                    _byCastId.Remove(castId, auraApp.Guid);

                var flags = auraApp.GetFlags();

                if (flags.HasFlag(AuraFlags.Negative))
                    _hasNegitiveFlag.Remove(auraApp.Guid);

                if (aura.IsPermanent())
                    _isPerm.Remove(auraApp.Guid);
            }
        }

        public AuraApplication GetByGuid(Guid guid)
        {
            lock(_auras)
            if (_auras.TryGetValue(guid, out var ret))
                return ret;

            return null;
        }

        public bool TryGetAuraByGuid(Guid guid, out AuraApplication aura)
        {
            lock (_auras)
                return _auras.TryGetValue(guid, out aura);
        }

        public bool Contains(AuraApplication aura)
        {
            lock(_auras)
                return _auras.ContainsKey(aura.Guid);
        }

        public HashSet<AuraApplication> AuraApplications
        {
            get
            {
                lock (_auras)
                    return _auras.Values.ToHashSet();
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

        public AuraApplicationQuery Query()
        {
            return new AuraApplicationQuery(this);
        }

        public class AuraApplicationQuery
        {
            AuraApplicationCollection _collection;
            bool _hasLoaded = false;

            public HashSet<Guid> Results { get; } = new();

            internal AuraApplicationQuery(AuraApplicationCollection auraCollection)
            {
                _collection = auraCollection;
            }

            public AuraApplicationQuery HasSpellId(uint spellId)
            {
                lock (_collection._auras)
                    if (_collection._aurasBySpellId.TryGetValue(spellId, out var result))
                        Sync(result);

                return this;
            }

            public AuraApplicationQuery HasCasterGuid(ObjectGuid caster)
            {
                lock (_collection._auras)
                    if (!caster.IsEmpty() && _collection._byCasterGuid.TryGetValue(caster, out var result))
                        Sync(result);

                return this;
            }

            public AuraApplicationQuery HasCastItemGuid(ObjectGuid item)
            {
                lock (_collection._auras)
                    if (!item.IsEmpty() && _collection._byCastItemGuid.TryGetValue(item, out var result))
                        Sync(result);

                return this;
            }

            public AuraApplicationQuery HasCastId(ObjectGuid Id)
            {
                lock (_collection._auras)
                    if (!Id.IsEmpty() && _collection._byCastId.TryGetValue(Id, out var result))
                        Sync(result);

                return this;
            }

            public AuraApplicationQuery HasOwner(WorldObject owner)
            {
                lock (_collection._auras)
                    if (owner != null && _collection._byOwner.TryGetValue(owner, out var result))
                        Sync(result);

                return this;
            }

            public AuraApplicationQuery HasLabel(uint label)
            {
                lock (_collection._auras)
                    if (_collection._labelMap.TryGetValue(label, out var result))
                        Sync(result);

                return this;
            }

            public AuraApplicationQuery HasDeminishGroup(DiminishingGroup group)
            {
                lock (_collection._auras)
                    if (_collection._deminishGroup.TryGetValue(group, out var result))
                        Sync(result);

                return this;
            }


            public AuraApplicationQuery HasAuraType(AuraObjectType auraType)
            {
                lock (_collection._auras)
                    if (_collection._typeMap.TryGetValue(auraType, out var result))
                        Sync(result);

                return this;
            }

            public AuraApplicationQuery HasCasterAuraState(AuraStateType state)
            {
                lock (_collection._auras)
                    if (_collection._casterAuraState.TryGetValue(state, out var result))
                        Sync(result);

                return this;
            }

            public AuraApplicationQuery HasDispelType(DispelType dispellType)
            {
                lock (_collection._auras)
                    if (_collection._dispelType.TryGetValue(dispellType, out var result))
                        Sync(result);

                return this;
            }

            public AuraApplicationQuery IsSingleTarget(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isSingleTarget, t);

                return this;
            }

            public AuraApplicationQuery CanBeSaved(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._canBeSaved, t);

                return this;
            }

            public AuraApplicationQuery IsGroupBuff(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isgroupBuff, t);

                return this;
            }

            public AuraApplicationQuery IsPassive(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isPassive, t);

                return this;
            }

            public AuraApplicationQuery IsDeathPersistant(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isDeathPersistant, t);

                return this;
            }

            public AuraApplicationQuery IsRequiringDeadTarget(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isRequiringDeadTarget, t);

                return this;
            }

            public AuraApplicationQuery IsNotPlayer(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isNotPlayer, t);

                return this;
            }

            public AuraApplicationQuery HasNegitiveFlag(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._hasNegitiveFlag, t);

                return this;
            }

            public AuraApplicationQuery IsPermanent(bool t = true)
            {
                lock (_collection._auras)
                    Sync(_collection._isPerm, t);

                return this;
            }

            public AuraApplicationQuery Execute(Action<uint, AuraApplication, AuraRemoveMode> action, AuraRemoveMode auraRemoveMode = AuraRemoveMode.Default)
            {
                foreach (var aura in Results)
                {
                    var obj = _collection._auras[aura];

                    action(obj.GetBase().GetSpellInfo().Id, obj, auraRemoveMode);
                }

                return this;
            }

            public IEnumerable<AuraApplication> GetResults()
            {
                foreach (var aura in Results)
                    yield return _collection._auras[aura];
            }

            public AuraApplicationQuery ForEachResult(Action<AuraApplication> action)
            {
                foreach (var ar in Results)
                    action(_collection._auras[ar]);

                return this;
            }

            public AuraApplicationQuery AlsoMatches(Func<AuraApplication, bool> predicate)
            {
                Results.RemoveWhere(g => !predicate(_collection._auras[g]));
                return this;
            }

            private void Sync(HashSet<Guid> collection, bool contains = true)
            {
                if (!_hasLoaded)
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
