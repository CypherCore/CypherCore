// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Combat
{
    public class ThreatManager
    {
        public static uint THREAT_UPDATE_INTERVAL = 1000u;

        public Unit _owner;
        bool _ownerCanHaveThreatList;

        public bool NeedClientUpdate;
        uint _updateTimer;
        List<ThreatReference> _sortedThreatList = new();
        Dictionary<ObjectGuid, ThreatReference> _myThreatListEntries = new();
        List<ThreatReference> _needsAIUpdate = new();
        ThreatReference _currentVictimRef;
        ThreatReference _fixateRef;

        Dictionary<ObjectGuid, ThreatReference> _threatenedByMe = new(); // these refs are entries for myself on other units' threat lists
        public float[] _singleSchoolModifiers = new float[(int)SpellSchools.Max]; // most spells are single school - we pre-calculate these and store them
        public volatile Dictionary<SpellSchoolMask, float> _multiSchoolModifiers = new(); // these are calculated on demand

        public List<Tuple<ObjectGuid, uint>> _redirectInfo = new(); // current redirection targets and percentages (updated from registry in ThreatManager::UpdateRedirectInfo)
        public Dictionary<uint, Dictionary<ObjectGuid, uint>> _redirectRegistry = new(); // spellid . (victim . pct); all redirection effects on us (removal individually managed by spell scripts because blizzard is dumb)

        public static bool CanHaveThreatList(Unit who)
        {
            Creature cWho = who.ToCreature();
            // only creatures can have threat list
            if (!cWho)
                return false;

            // pets, totems and triggers cannot have threat list
            if (cWho.IsPet() || cWho.IsTotem() || cWho.IsTrigger())
                return false;

            // summons cannot have a threat list if they were summoned by a player
            if (cWho.HasUnitTypeMask(UnitTypeMask.Minion | UnitTypeMask.Guardian))
            {
                TempSummon tWho = cWho.ToTempSummon();
                if (tWho != null)
                    if (tWho.GetSummonerGUID().IsPlayer())
                        return false;
            }

            return true;
        }

        public ThreatManager(Unit owner)
        {
            _owner = owner;
            _updateTimer = THREAT_UPDATE_INTERVAL;

            for (var i = 0; i < (int)SpellSchools.Max; ++i)
                _singleSchoolModifiers[i] = 1.0f;
        }

        public void Initialize()
        {
            _ownerCanHaveThreatList = CanHaveThreatList(_owner);
        }

        public void Update(uint tdiff)
        {
            if (!CanHaveThreatList() || IsThreatListEmpty(true))
                return;

            if (_updateTimer <= tdiff)
            {
                UpdateVictim();
                _updateTimer = THREAT_UPDATE_INTERVAL;
            }
            else
                _updateTimer -= tdiff;
        }

        public Unit GetCurrentVictim()
        {
            if (_currentVictimRef == null || _currentVictimRef.ShouldBeOffline())
                UpdateVictim();

            Cypher.Assert(_currentVictimRef == null || _currentVictimRef.IsAvailable());
            return _currentVictimRef?.GetVictim();
        }

        public Unit GetLastVictim()
        {
            if (_currentVictimRef != null && !_currentVictimRef.ShouldBeOffline())
                return _currentVictimRef.GetVictim();

            return null;
        }

        public Unit GetAnyTarget()
        {
            foreach (ThreatReference refe in _sortedThreatList)
                if (!refe.IsOffline())
                    return refe.GetVictim();

            return null;
        }

        public bool IsThreatListEmpty(bool includeOffline = false)
        {
            if (includeOffline)
                return _sortedThreatList.Empty();

            foreach (ThreatReference refe in _sortedThreatList)
                if (refe.IsAvailable())
                    return false;

            return true;
        }

        public bool IsThreatenedBy(ObjectGuid who, bool includeOffline = false)
        {
            var refe = _myThreatListEntries.LookupByKey(who);
            if (refe == null)
                return false;

            return (includeOffline || refe.IsAvailable());
        }

        public bool IsThreatenedBy(Unit who, bool includeOffline = false) { return IsThreatenedBy(who.GetGUID(), includeOffline); }

        public float GetThreat(Unit who, bool includeOffline = false)
        {
            var refe = _myThreatListEntries.LookupByKey(who.GetGUID());
            if (refe == null)
                return 0.0f;

            return (includeOffline || refe.IsAvailable()) ? refe.GetThreat() : 0.0f;
        }

        public List<ThreatReference> GetModifiableThreatList()
        {
            return new(_sortedThreatList);
        }

        public bool IsThreateningAnyone(bool includeOffline = false)
        {
            if (includeOffline)
                return !_threatenedByMe.Empty();

            foreach (var pair in _threatenedByMe)
                if (pair.Value.IsAvailable())
                    return true;

            return false;
        }

        public bool IsThreateningTo(ObjectGuid who, bool includeOffline = false)
        {
            var refe = _threatenedByMe.LookupByKey(who);
            if (refe == null)
                return false;

            return (includeOffline || refe.IsAvailable());
        }

        public bool IsThreateningTo(Unit who, bool includeOffline = false) { return IsThreateningTo(who.GetGUID(), includeOffline); }

        public void EvaluateSuppressed(bool canExpire = false)
        {
            foreach (var pair in _threatenedByMe)
            {
                bool shouldBeSuppressed = pair.Value.ShouldBeSuppressed();
                if (pair.Value.IsOnline() && shouldBeSuppressed)
                {
                    pair.Value.Online = OnlineState.Suppressed;
                    pair.Value.ListNotifyChanged();
                }
                else if (canExpire && pair.Value.IsSuppressed() && !shouldBeSuppressed)
                {
                    pair.Value.Online = OnlineState.Online;
                    pair.Value.ListNotifyChanged();
                }
            }
        }

        public void AddThreat(Unit target, float amount, SpellInfo spell = null, bool ignoreModifiers = false, bool ignoreRedirects = false)
        {
            // step 1: we can shortcut if the spell has one of the NO_THREAT attrs set - nothing will happen
            if (spell != null)
            {
                if (spell.HasAttribute(SpellAttr1.NoThreat))
                    return;
                if (!_owner.IsEngaged() && spell.HasAttribute(SpellAttr2.NoInitialThreat))
                    return;
            }

            // while riding a vehicle, all threat goes to the vehicle, not the pilot
            Unit vehicle = target.GetVehicleBase();
            if (vehicle != null)
            {
                AddThreat(vehicle, amount, spell, ignoreModifiers, ignoreRedirects);
                if (target.HasUnitTypeMask(UnitTypeMask.Accessory)) // accessories are fully treated as components of the parent and cannot have threat
                    return;
                amount = 0.0f;
            }

            // If victim is personal spawn, redirect all aggro to summoner
            if (target.IsPrivateObject() && (!GetOwner().IsPrivateObject() || !GetOwner().CheckPrivateObjectOwnerVisibility(target)))
            {
                Unit privateObjectOwner = Global.ObjAccessor.GetUnit(GetOwner(), target.GetPrivateObjectOwner());
                if (privateObjectOwner != null)
                {
                    AddThreat(privateObjectOwner, amount, spell, ignoreModifiers, ignoreRedirects);
                    amount = 0.0f;
                }
            }

            // if we cannot actually have a threat list, we instead just set combat state and avoid creating threat refs altogether
            if (!CanHaveThreatList())
            {
                CombatManager combatMgr = _owner.GetCombatManager();
                if (!combatMgr.SetInCombatWith(target))
                    return;
                // traverse redirects and put them in combat, too
                foreach (var pair in target.GetThreatManager()._redirectInfo)
                {
                    if (!combatMgr.IsInCombatWith(pair.Item1))
                    {
                        Unit redirTarget = Global.ObjAccessor.GetUnit(_owner, pair.Item1);
                        if (redirTarget != null)
                            combatMgr.SetInCombatWith(redirTarget);
                    }
                }
                return;
            }

            // apply threat modifiers to the amount
            if (!ignoreModifiers)
                amount = CalculateModifiedThreat(amount, target, spell);

            // if we're increasing threat, send some/all of it to redirection targets instead if applicable
            if (!ignoreRedirects && amount > 0.0f)
            {
                var redirInfo = target.GetThreatManager()._redirectInfo;
                if (!redirInfo.Empty())
                {
                    float origAmount = amount;
                    // intentional iteration by index - there's a nested AddThreat call further down that might cause AI calls which might modify redirect info through spells
                    for (var i = 0; i < redirInfo.Count; ++i)
                    {
                        var pair = redirInfo[i]; // (victim,pct)
                        Unit redirTarget;

                        var refe = _myThreatListEntries.LookupByKey(pair.Item1); // try to look it up in our threat list first (faster)
                        if (refe != null)
                            redirTarget = refe.GetVictim();
                        else
                            redirTarget = Global.ObjAccessor.GetUnit(_owner, pair.Item1);

                        if (redirTarget)
                        {
                            float amountRedirected = MathFunctions.CalculatePct(origAmount, pair.Item2);
                            AddThreat(redirTarget, amountRedirected, spell, true, true);
                            amount -= amountRedirected;
                        }
                    }
                }
            }

            // ensure we're in combat (threat implies combat!)
            if (!_owner.GetCombatManager().SetInCombatWith(target)) // if this returns false, we're not actually in combat, and thus cannot have threat!
                return;                                              // typical causes: bad scripts trying to add threat to GMs, dead targets etc

            // ok, now we actually apply threat
            // check if we already have an entry - if we do, just increase threat for that entry and we're done
            var targetRefe = _myThreatListEntries.LookupByKey(target.GetGUID());
            if (targetRefe != null)
            {
                // SUPPRESSED threat states don't go back to ONLINE until threat is caused by them (retail behavior)
                if (targetRefe.GetOnlineState() == OnlineState.Suppressed)
                {
                    if (!targetRefe.ShouldBeSuppressed())
                    {
                        targetRefe.Online = OnlineState.Online;
                        targetRefe.ListNotifyChanged();
                    }
                }

                if (targetRefe.IsOnline())
                    targetRefe.AddThreat(amount);
                return;
            }

            // ok, we're now in combat - create the threat list reference and push it to the respective managers
            ThreatReference newRefe = new(this, target);
            PutThreatListRef(target.GetGUID(), newRefe);
            target.GetThreatManager().PutThreatenedByMeRef(_owner.GetGUID(), newRefe);

            // afterwards, we evaluate whether this is an online reference (it might not be an acceptable target, but we need to add it to our threat list before we check!)
            newRefe.UpdateOffline();
            if (newRefe.IsOnline()) // we only add the threat if the ref is currently available
                newRefe.AddThreat(amount);

            if (_currentVictimRef == null)
                UpdateVictim();
            else
                ProcessAIUpdates();
        }

        void ScaleThreat(Unit target, float factor)
        {
            var refe = _myThreatListEntries.LookupByKey(target.GetGUID());
            if (refe != null)
                refe.ScaleThreat(Math.Max(factor, 0.0f));
        }

        public void MatchUnitThreatToHighestThreat(Unit target)
        {
            if (_sortedThreatList.Empty())
                return;

            int index = 0;

            ThreatReference highest = _sortedThreatList[index];
            if (!highest.IsAvailable())
                return;

            if (highest.IsTaunting() && (++index) != _sortedThreatList.Count - 1) // might need to skip this - max threat could be the preceding element (there is only one taunt element)
            {
                ThreatReference a = _sortedThreatList[index];
                if (a.IsAvailable() && a.GetThreat() > highest.GetThreat())
                    highest = a;
            }

            AddThreat(target, highest.GetThreat() - GetThreat(target, true), null, true, true);
        }

        public void TauntUpdate()
        {
            var tauntEffects = _owner.GetAuraEffectsByType(AuraType.ModTaunt);
            TauntState state = TauntState.Taunt;
            Dictionary<ObjectGuid, TauntState> tauntStates = new();

            // Only the last taunt effect applied by something still on our threat list is considered
            foreach (var auraEffect in tauntEffects)
                tauntStates[auraEffect.GetCasterGUID()] = state++;

            foreach (var pair in _myThreatListEntries)
            {
                if (tauntStates.TryGetValue(pair.Key, out TauntState tauntState))
                    pair.Value.UpdateTauntState(tauntState);
                else
                    pair.Value.UpdateTauntState();
            }

            // taunt aura update also re-evaluates all suppressed states (retail behavior)
            EvaluateSuppressed(true);
        }

        public void ResetAllThreat()
        {
            foreach (var pair in _myThreatListEntries)
                pair.Value.ScaleThreat(0.0f);
        }

        public void ClearThreat(Unit target)
        {
            var refe = _myThreatListEntries.LookupByKey(target.GetGUID());
            if (refe != null)
                ClearThreat(refe);
        }

        public void ClearThreat(ThreatReference refe)
        {
            SendRemoveToClients(refe.GetVictim());
            refe.UnregisterAndFree();
            if (_currentVictimRef == null)
                UpdateVictim();
        }

        public void ClearAllThreat()
        {
            if (!_myThreatListEntries.Empty())
            {
                SendClearAllThreatToClients();
                do
                    _myThreatListEntries.FirstOrDefault().Value.UnregisterAndFree();
                while (!_myThreatListEntries.Empty());
            }
        }

        public void FixateTarget(Unit target)
        {
            if (target)
            {
                var it = _myThreatListEntries.LookupByKey(target.GetGUID());
                if (it != null)
                {
                    _fixateRef = it;
                    return;
                }
            }
            _fixateRef = null;
        }

        public Unit GetFixateTarget()
        {
            if (_fixateRef != null)
                return _fixateRef.GetVictim();
            else
                return null;
        }

        public void ClearFixate() { FixateTarget(null); }

        void UpdateVictim()
        {
            ThreatReference newVictim = ReselectVictim();
            bool newHighest = newVictim != null && (newVictim != _currentVictimRef);

            _currentVictimRef = newVictim;
            if (newHighest || NeedClientUpdate)
            {
                SendThreatListToClients(newHighest);
                NeedClientUpdate = false;
            }

            ProcessAIUpdates();
        }

        ThreatReference ReselectVictim()
        {
            if (_sortedThreatList.Empty())
                return null;

            foreach (var pair in _myThreatListEntries)
                pair.Value.UpdateOffline(); // AI notifies are processed in ::UpdateVictim caller

            // fixated target is always preferred
            if (_fixateRef != null && _fixateRef.IsAvailable())
                return _fixateRef;

            ThreatReference oldVictimRef = _currentVictimRef;
            if (oldVictimRef != null && oldVictimRef.IsOffline())
                oldVictimRef = null;

            // in 99% of cases - we won't need to actually look at anything beyond the first element
            ThreatReference highest = _sortedThreatList.First();
            // if the highest reference is offline, the entire list is offline, and we indicate this
            if (!highest.IsAvailable())
                return null;

            // if we have no old victim, or old victim is still highest, then highest is our target and we're done
            if (oldVictimRef == null || highest == oldVictimRef)
                return highest;

            // if highest threat doesn't break 110% of old victim, nothing below it is going to do so either; new victim = old victim and done
            if (!CompareReferencesLT(oldVictimRef, highest, 1.1f))
                return oldVictimRef;

            // if highest threat breaks 130%, it's our new target regardless of range (and we're done)
            if (CompareReferencesLT(oldVictimRef, highest, 1.3f))
                return highest;

            // if it doesn't break 130%, we need to check if it's melee - if yes, it breaks 110% (we checked earlier) and is our new target
            if (_owner.IsWithinMeleeRange(highest.GetVictim()))
                return highest;

            // If we get here, highest threat is ranged, but below 130% of current - there might be a melee that breaks 110% below us somewhere, so now we need to actually look at the next highest element
            // luckily, this is a heap, so getting the next highest element is O(log n), and we're just gonna do that repeatedly until we've seen enough targets (or find a target)
            foreach (var next in _sortedThreatList)
            {
                // if we've found current victim, we're done (nothing above is higher, and nothing below can be higher)
                if (next == oldVictimRef)
                    return next;

                // if next isn't above 110% threat, then nothing below it can be either - we're done, old victim stays
                if (!CompareReferencesLT(oldVictimRef, next, 1.1f))
                    return oldVictimRef;

                // if next is melee, he's above 110% and our new victim
                if (_owner.IsWithinMeleeRange(next.GetVictim()))
                    return next;

                // otherwise the next highest target may still be a melee above 110% and we need to look further
            }
            // we should have found the old victim at some point in the loop above, so execution should never get to this point
            Cypher.Assert(false, "Current victim not found in sorted threat list even though it has a reference - manager desync!");
            return null;
        }

        void ProcessAIUpdates()
        {
            CreatureAI ai = _owner.ToCreature().GetAI();
            List<ThreatReference> v = new(_needsAIUpdate); // _needClientUpdate is now empty in case this triggers a recursive call
            if (ai == null)
                return;
            foreach (ThreatReference refe in v)
                ai.JustStartedThreateningMe(refe.GetVictim());
        }

        // returns true if a is LOWER on the threat list than b
        public static bool CompareReferencesLT(ThreatReference a, ThreatReference b, float aWeight)
        {
            if (a.GetOnlineState() != b.GetOnlineState()) // online state precedence (ONLINE > SUPPRESSED > OFFLINE)
                return a.GetOnlineState() < b.GetOnlineState();

            if (a.GetTauntState() != b.GetTauntState()) // taunt state precedence (TAUNT > NONE > DETAUNT)
                return a.GetTauntState() < b.GetTauntState();

            return (a.GetThreat() * aWeight < b.GetThreat());
        }

        public static float CalculateModifiedThreat(float threat, Unit victim, SpellInfo spell)
        {
            // modifiers by spell
            if (spell != null)
            {
                SpellThreatEntry threatEntry = Global.SpellMgr.GetSpellThreatEntry(spell.Id);
                if (threatEntry != null)
                    if (threatEntry.pctMod != 1.0f) // flat/AP modifiers handled in Spell::HandleThreatSpells
                        threat *= threatEntry.pctMod;

                Player modOwner = victim.GetSpellModOwner();
                if (modOwner != null)
                    modOwner.ApplySpellMod(spell, SpellModOp.Hate, ref threat);
            }

            // modifiers by effect school
            ThreatManager victimMgr = victim.GetThreatManager();
            SpellSchoolMask mask = spell != null ? spell.GetSchoolMask() : SpellSchoolMask.Normal;
            switch (mask)
            {
                case SpellSchoolMask.Normal:
                    threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Normal];
                    break;
                case SpellSchoolMask.Holy:
                    threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Holy];
                    break;
                case SpellSchoolMask.Fire:
                    threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Fire];
                    break;
                case SpellSchoolMask.Nature:
                    threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Nature];
                    break;
                case SpellSchoolMask.Frost:
                    threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Frost];
                    break;
                case SpellSchoolMask.Shadow:
                    threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Shadow];
                    break;
                case SpellSchoolMask.Arcane:
                    threat *= victimMgr._singleSchoolModifiers[(int)SpellSchools.Arcane];
                    break;
                default:
                {
                    if (victimMgr._multiSchoolModifiers.TryGetValue(mask, out float value))
                    {
                        threat *= value;
                        break;
                    }
                    float mod = victim.GetTotalAuraMultiplierByMiscMask(AuraType.ModThreat, (uint)mask);
                    victimMgr._multiSchoolModifiers[mask] = mod;
                    threat *= mod;
                    break;
                }
            }
            return threat;
        }

        public void ForwardThreatForAssistingMe(Unit assistant, float baseAmount, SpellInfo spell = null, bool ignoreModifiers = false)
        {
            if (spell != null && (spell.HasAttribute(SpellAttr1.NoThreat) || spell.HasAttribute(SpellAttr4.NoHelpfulThreat))) // shortcut, none of the calls would do anything
                return;

            if (_threatenedByMe.Empty())
                return;

            List<Creature> canBeThreatened = new();
            List<Creature> cannotBeThreatened = new();
            foreach (var pair in _threatenedByMe)
            {
                Creature owner = pair.Value.GetOwner();
                if (!owner.HasUnitState(UnitState.Controlled))
                    canBeThreatened.Add(owner);
                else
                    cannotBeThreatened.Add(owner);
            }

            if (!canBeThreatened.Empty()) // targets under CC cannot gain assist threat - split evenly among the rest
            {
                float perTarget = baseAmount / canBeThreatened.Count;
                foreach (Creature threatened in canBeThreatened)
                    threatened.GetThreatManager().AddThreat(assistant, perTarget, spell, ignoreModifiers);
            }

            foreach (Creature threatened in cannotBeThreatened)
                threatened.GetThreatManager().AddThreat(assistant, 0.0f, spell, true);
        }

        public void RemoveMeFromThreatLists()
        {
            while (!_threatenedByMe.Empty())
            {
                var refe = _threatenedByMe.FirstOrDefault().Value;
                refe._mgr.ClearThreat(_owner);
            }
        }

        public void UpdateMyTempModifiers()
        {
            int mod = 0;
            foreach (AuraEffect eff in _owner.GetAuraEffectsByType(AuraType.ModTotalThreat))
                mod += eff.GetAmount();

            if (_threatenedByMe.Empty())
                return;

            foreach (var pair in _threatenedByMe)
            {
                pair.Value.TempModifier = mod;
                pair.Value.ListNotifyChanged();
            }
        }

        public void UpdateMySpellSchoolModifiers()
        {
            for (byte i = 0; i < (int)SpellSchools.Max; ++i)
                _singleSchoolModifiers[i] = _owner.GetTotalAuraMultiplierByMiscMask(AuraType.ModThreat, 1u << i);
            _multiSchoolModifiers.Clear();
        }

        public void RegisterRedirectThreat(uint spellId, ObjectGuid victim, uint pct)
        {
            if (!_redirectRegistry.ContainsKey(spellId))
                _redirectRegistry[spellId] = new();

            _redirectRegistry[spellId][victim] = pct;
            UpdateRedirectInfo();
        }

        public void UnregisterRedirectThreat(uint spellId)
        {
            if (_redirectRegistry.Remove(spellId))
                UpdateRedirectInfo();
        }

        void UnregisterRedirectThreat(uint spellId, ObjectGuid victim)
        {
            var victimMap = _redirectRegistry.LookupByKey(spellId);
            if (victimMap == null)
                return;

            if (victimMap.Remove(victim))
                UpdateRedirectInfo();
        }

        void SendClearAllThreatToClients()
        {
            ThreatClear threatClear = new();
            threatClear.UnitGUID = _owner.GetGUID();
            _owner.SendMessageToSet(threatClear, false);
        }

        public void SendRemoveToClients(Unit victim)
        {
            ThreatRemove threatRemove = new();
            threatRemove.UnitGUID = _owner.GetGUID();
            threatRemove.AboutGUID = victim.GetGUID();
            _owner.SendMessageToSet(threatRemove, false);
        }

        void SendThreatListToClients(bool newHighest)
        {
            void fillSharedPacketDataAndSend(dynamic packet)
            {
                packet.UnitGUID = _owner.GetGUID();

                foreach (ThreatReference refe in _sortedThreatList)
                {
                    if (!refe.IsAvailable())
                        continue;

                    ThreatInfo threatInfo = new();
                    threatInfo.UnitGUID = refe.GetVictim().GetGUID();
                    threatInfo.Threat = (long)(refe.GetThreat() * 100);
                    packet.ThreatList.Add(threatInfo);
                }
                _owner.SendMessageToSet(packet, false);
            }

            if (newHighest)
            {
                HighestThreatUpdate highestThreatUpdate = new();
                highestThreatUpdate.HighestThreatGUID = _currentVictimRef.GetVictim().GetGUID();
                fillSharedPacketDataAndSend(highestThreatUpdate);
            }
            else
            {
                ThreatUpdate threatUpdate = new();
                fillSharedPacketDataAndSend(threatUpdate);
            }
        }

        void PutThreatListRef(ObjectGuid guid, ThreatReference refe)
        {
            NeedClientUpdate = true;
            Cypher.Assert(!_myThreatListEntries.ContainsKey(guid), $"Duplicate threat reference being inserted on {_owner.GetGUID()} for {guid}!");
            _myThreatListEntries[guid] = refe;
            _sortedThreatList.Add(refe);
            _sortedThreatList.Sort();
        }

        public void PurgeThreatListRef(ObjectGuid guid)
        {
            var refe = _myThreatListEntries.LookupByKey(guid);
            if (refe == null)
                return;

            _myThreatListEntries.Remove(guid);
            _sortedThreatList.Remove(refe);

            if (_fixateRef == refe)
                _fixateRef = null;

            if (_currentVictimRef == refe)
                _currentVictimRef = null;
        }

        void PutThreatenedByMeRef(ObjectGuid guid, ThreatReference refe)
        {
            Cypher.Assert(!_threatenedByMe.ContainsKey(guid), $"Duplicate threatened-by-me reference being inserted on {_owner.GetGUID()} for {guid}!");
            _threatenedByMe[guid] = refe;
        }

        public void PurgeThreatenedByMeRef(ObjectGuid guid)
        {
            _threatenedByMe.Remove(guid);
        }

        void UpdateRedirectInfo()
        {
            _redirectInfo.Clear();
            uint totalPct = 0;
            foreach (var pair in _redirectRegistry) // (spellid, victim . pct)
            {
                foreach (var victimPair in pair.Value) // (victim,pct)
                {
                    uint thisPct = Math.Min(100 - totalPct, victimPair.Value);
                    if (thisPct > 0)
                    {
                        _redirectInfo.Add(Tuple.Create(victimPair.Key, thisPct));
                        totalPct += thisPct;
                        Cypher.Assert(totalPct <= 100);
                        if (totalPct == 100)
                            return;
                    }
                }
            }
        }

        // never nullptr
        Unit GetOwner() { return _owner; }

        // can our owner have a threat list?
        // identical to ThreatManager::CanHaveThreatList(GetOwner())
        public bool CanHaveThreatList() { return _ownerCanHaveThreatList; }

        public int GetThreatListSize() { return _sortedThreatList.Count; }

        // fastest of the three threat list getters - gets the threat list in "arbitrary" order
        public List<ThreatReference> GetSortedThreatList() { return _sortedThreatList; }

        public void ListNotifyChanged()
        {
            _sortedThreatList.Sort();
        }

        public Dictionary<ObjectGuid, ThreatReference> GetThreatenedByMeList() { return _threatenedByMe; }

        // Modify target's threat by +percent%
        public void ModifyThreatByPercent(Unit target, int percent)
        {
            if (percent != 0)
                ScaleThreat(target, 0.01f * (100f + percent));
        }

        // Resets the specified unit's threat to zero
        public void ResetThreat(Unit target) { ScaleThreat(target, 0.0f); }

        public void RegisterForAIUpdate(ThreatReference refe) { _needsAIUpdate.Add(refe); }
    }

    public enum TauntState
    {
        Detaunt = 0,
        None = 1,
        Taunt = 2
    }
    public enum OnlineState
    {
        Online = 2,
        Suppressed = 1,
        Offline = 0
    }

    public class ThreatReference : IComparable<ThreatReference>
    {
        Creature _owner;
        public ThreatManager _mgr;
        Unit _victim;
        public OnlineState Online;
        float _baseAmount;
        public int TempModifier; // Temporary effects (auras with SPELL_AURA_MOD_TOTAL_THREAT) - set from victim's threatmanager in ThreatManager::UpdateMyTempModifiers
        TauntState _taunted;

        public ThreatReference(ThreatManager mgr, Unit victim)
        {
            _owner = mgr._owner as Creature;
            _mgr = mgr;
            _victim = victim;
            Online = OnlineState.Offline;
        }

        public void AddThreat(float amount)
        {
            if (amount == 0.0f)
                return;

            _baseAmount = Math.Max(_baseAmount + amount, 0.0f);
            ListNotifyChanged();
            _mgr.NeedClientUpdate = true;
        }

        public void ScaleThreat(float factor)
        {
            if (factor == 1.0f)
                return;

            _baseAmount *= factor;
            ListNotifyChanged();
            _mgr.NeedClientUpdate = true;
        }

        public void UpdateOffline()
        {
            bool shouldBeOffline = ShouldBeOffline();
            if (shouldBeOffline == IsOffline())
                return;

            if (shouldBeOffline)
            {
                Online = OnlineState.Offline;
                ListNotifyChanged();
                _mgr.SendRemoveToClients(_victim);
            }
            else
            {
                Online = ShouldBeSuppressed() ? OnlineState.Suppressed : OnlineState.Online;
                ListNotifyChanged();
                _mgr.RegisterForAIUpdate(this);
            }
        }

        public static bool FlagsAllowFighting(Unit a, Unit b)
        {
            if (a.IsCreature() && a.ToCreature().IsTrigger())
                return false;

            if (a.HasUnitFlag(UnitFlags.PlayerControlled))
            {
                if (b.HasUnitFlag(UnitFlags.ImmuneToPc))
                    return false;
            }
            else
            {
                if (b.HasUnitFlag(UnitFlags.ImmuneToNpc))
                    return false;
            }
            return true;
        }

        public bool ShouldBeOffline()
        {
            if (!_owner.CanSeeOrDetect(_victim))
                return true;

            if (!_owner._IsTargetAcceptable(_victim) || !_owner.CanCreatureAttack(_victim))
                return true;

            if (!FlagsAllowFighting(_owner, _victim) || !FlagsAllowFighting(_victim, _owner))
                return true;

            return false;
        }

        public bool ShouldBeSuppressed()
        {
            if (IsTaunting()) // a taunting victim can never be suppressed
                return false;

            if (_victim.IsImmunedToDamage(_owner.GetMeleeDamageSchoolMask()))
                return true;

            if (_victim.HasAuraType(AuraType.ModConfuse))
                return true;

            if (_victim.HasBreakableByDamageAuraType(AuraType.ModStun))
                return true;

            return false;
        }

        public void UpdateTauntState(TauntState state = TauntState.None)
        {
            // Check for SPELL_AURA_MOD_DETAUNT (applied from owner to victim)
            if (state < TauntState.Taunt && _victim.HasAuraTypeWithCaster(AuraType.ModDetaunt, _owner.GetGUID()))
                state = TauntState.Detaunt;

            if (state == _taunted)
                return;

            Extensions.Swap(ref state, ref _taunted);

            ListNotifyChanged();
            _mgr.NeedClientUpdate = true;
        }

        public void ClearThreat()
        {
            _mgr.ClearThreat(this);
        }

        public void UnregisterAndFree()
        {
            _owner.GetThreatManager().PurgeThreatListRef(_victim.GetGUID());
            _victim.GetThreatManager().PurgeThreatenedByMeRef(_owner.GetGUID());
        }

        public Creature GetOwner() { return _owner; }
        public Unit GetVictim() { return _victim; }
        public float GetThreat() { return Math.Max(_baseAmount + (float)TempModifier, 0.0f); }
        public OnlineState GetOnlineState() { return Online; }
        public bool IsOnline() { return Online >= OnlineState.Online; }
        public bool IsAvailable() { return Online > OnlineState.Offline; }
        public bool IsSuppressed() { return Online == OnlineState.Suppressed; }
        public bool IsOffline() { return Online <= OnlineState.Offline; }
        public TauntState GetTauntState() { return IsTaunting() ? TauntState.Taunt : _taunted; }
        public bool IsTaunting() { return _taunted >= TauntState.Taunt; }
        public bool IsDetaunted() { return _taunted == TauntState.Detaunt; }

        public void SetThreat(float amount) 
        { 
            _baseAmount = amount;
            ListNotifyChanged(); 
        }

        public void ModifyThreatByPercent(int percent)
        {
            if (percent != 0)
                ScaleThreat(0.01f * (100f + percent)); }

        public void ListNotifyChanged() { _mgr.ListNotifyChanged(); }

        public int CompareTo(ThreatReference other)
        {
            return ThreatManager.CompareReferencesLT(this, other, 1.0f) ? 1 : -1;
        }
    }
}
