// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.AI;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Movement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Entities
{
    public class Vehicle : ITransport, IDisposable
    {
        public Vehicle(Unit unit, VehicleRecord vehInfo, uint creatureEntry)
        {
            _me = unit;
            _vehicleInfo = vehInfo;
            _creatureEntry = creatureEntry;
            _status = Status.None;

            for (uint i = 0; i < SharedConst.MaxVehicleSeats; ++i)
            {
                uint seatId = _vehicleInfo.SeatID[i];
                if (seatId != 0)
                {
                    VehicleSeatRecord veSeat = CliDB.VehicleSeatStorage.LookupByKey(seatId);
                    if (veSeat != null)
                    {
                        VehicleSeatAddon addon = Global.ObjectMgr.GetVehicleSeatAddon(seatId);
                        Seats.Add((sbyte)i, new VehicleSeat(veSeat, addon));
                        if (veSeat.CanEnterOrExit())
                            ++UsableSeatNum;
                    }
                }
            }

            // Set or remove correct flags based on available seats. Will overwrite db data (if wrong).
            if (UsableSeatNum != 0)
                _me.SetNpcFlag(_me.IsTypeId(TypeId.Player) ? NPCFlags.PlayerVehicle : NPCFlags.SpellClick);
            else
                _me.RemoveNpcFlag(_me.IsTypeId(TypeId.Player) ? NPCFlags.PlayerVehicle : NPCFlags.SpellClick);

            InitMovementInfoForBase();
        }

        public void Dispose()
        {
            // @Uninstall must be called before this.
            Cypher.Assert(_status == Status.UnInstalling);
            foreach (var pair in Seats)
                Cypher.Assert(pair.Value.IsEmpty());
        }

        public void Install()
        {
            _status = Status.Installed;
            if (GetBase().IsTypeId(TypeId.Unit))
                Global.ScriptMgr.OnInstall(this);
        }

        public void InstallAllAccessories(bool evading)
        {
            if (GetBase().IsTypeId(TypeId.Player) || !evading)
                RemoveAllPassengers();   // We might have aura's saved in the DB with now invalid casters - remove

            List<VehicleAccessory> accessories = Global.ObjectMgr.GetVehicleAccessoryList(this);
            if (accessories == null)
                return;

            foreach (var acc in accessories)
                if (!evading || acc.IsMinion)  // only install minions on evade mode
                    InstallAccessory(acc.AccessoryEntry, acc.SeatId, acc.IsMinion, acc.SummonedType, acc.SummonTime);
        }

        public void Uninstall()
        {
            // @Prevent recursive uninstall call. (Bad script in OnUninstall/OnRemovePassenger/PassengerBoarded hook.)
            if (_status == Status.UnInstalling && !GetBase().HasUnitTypeMask(UnitTypeMask.Minion))
            {
                Log.outError(LogFilter.Vehicle, "Vehicle GuidLow: {0}, Entry: {1} attempts to uninstall, but already has STATUS_UNINSTALLING! " +
                    "Check Uninstall/PassengerBoarded script hooks for errors.", _me.GetGUID().ToString(), _me.GetEntry());
                return;
            }

            _status = Status.UnInstalling;
            Log.outDebug(LogFilter.Vehicle, "Vehicle.Uninstall Entry: {0}, GuidLow: {1}", _creatureEntry, _me.GetGUID().ToString());
            RemoveAllPassengers();

            if (GetBase().IsTypeId(TypeId.Unit))
                Global.ScriptMgr.OnUninstall(this);
        }

        public void Reset(bool evading = false)
        {
            if (!GetBase().IsTypeId(TypeId.Unit))
                return;

            Log.outDebug(LogFilter.Vehicle, "Vehicle.Reset (Entry: {0}, GuidLow: {1}, DBGuid: {2})", GetCreatureEntry(), _me.GetGUID().ToString(), _me.ToCreature().GetSpawnId());

            ApplyAllImmunities();
            if (GetBase().IsAlive())
                InstallAllAccessories(evading);

            Global.ScriptMgr.OnReset(this);
        }

        void ApplyAllImmunities()
        {
            // This couldn't be done in DB, because some spells have MECHANIC_NONE

            // Vehicles should be immune on Knockback ...
            _me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBack, true);
            _me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.KnockBackDest, true);

            // Mechanical units & vehicles ( which are not Bosses, they have own immunities in DB ) should be also immune on healing ( exceptions in switch below )
            if (_me.IsTypeId(TypeId.Unit) && _me.ToCreature().GetCreatureTemplate().CreatureType == CreatureType.Mechanical && !_me.ToCreature().IsWorldBoss())
            {
                // Heal & dispel ...
                _me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.Heal, true);
                _me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.HealPct, true);
                _me.ApplySpellImmune(0, SpellImmunity.Effect, SpellEffectName.Dispel, true);
                _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.PeriodicHeal, true);

                // ... Shield & Immunity grant spells ...
                _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.SchoolImmunity, true);
                _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModUnattackable, true);
                _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.SchoolAbsorb, true);
                _me.ApplySpellImmune(0, SpellImmunity.Mechanic, (uint)Mechanics.Banish, true);
                _me.ApplySpellImmune(0, SpellImmunity.Mechanic, (uint)Mechanics.Shield, true);
                _me.ApplySpellImmune(0, SpellImmunity.Mechanic, (uint)Mechanics.ImmuneShield, true);

                // ... Resistance, Split damage, Change stats ...
                _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.DamageShield, true);
                _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.SplitDamagePct, true);
                _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModResistance, true);
                _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModStat, true);
                _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModDamagePercentTaken, true);
            }

            // Different immunities for vehicles goes below
            switch (GetVehicleInfo().Id)
            {
                // code below prevents a bug with movable cannons
                case 160: // Strand of the Ancients
                case 244: // Wintergrasp
                case 452: // Isle of Conquest
                case 510: // Isle of Conquest
                case 543: // Isle of Conquest
                    _me.SetControlled(true, UnitState.Root);
                    // why we need to apply this? we can simple add immunities to slow mechanic in DB
                    _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModDecreaseSpeed, true);
                    break;
                case 335: // Salvaged Chopper
                case 336: // Salvaged Siege Engine
                case 338: // Salvaged Demolisher
                    _me.ApplySpellImmune(0, SpellImmunity.State, AuraType.ModDamagePercentTaken, false); // Battering Ram
                    break;
                default:
                    break;
            }
        }

        public void RemoveAllPassengers()
        {
            Log.outDebug(LogFilter.Vehicle, "Vehicle.RemoveAllPassengers. Entry: {0}, GuidLow: {1}", _creatureEntry, _me.GetGUID().ToString());

            // Setting to_Abort to true will cause @VehicleJoinEvent.Abort to be executed on next @Unit.UpdateEvents call
            // This will properly "reset" the pending join process for the passenger.
            {
                // Update vehicle in every pending join event - Abort may be called after vehicle is deleted
                Vehicle eventVehicle = _status != Status.UnInstalling ? this : null;

                while (!_pendingJoinEvents.Empty())
                {
                    VehicleJoinEvent e = _pendingJoinEvents.First();
                    e.ScheduleAbort();
                    e.Target = eventVehicle;
                    _pendingJoinEvents.Remove(_pendingJoinEvents.First());
                }
            }

            // Passengers always cast an aura with SPELL_AURA_CONTROL_VEHICLE on the vehicle
            // We just remove the aura and the unapply handler will make the target leave the vehicle.
            // We don't need to iterate over Seats
            _me.RemoveAurasByType(AuraType.ControlVehicle);

            // Aura script might cause the vehicle to be despawned in the middle of handling SPELL_AURA_CONTROL_VEHICLE removal
            // In that case, aura effect has already been unregistered but passenger may still be found in Seats
            foreach (var (_, seat) in Seats)
            {
                Unit passenger = Global.ObjAccessor.GetUnit(_me, seat.Passenger.Guid);
                if (passenger != null)
                    passenger._ExitVehicle();
            }
        }

        public bool HasEmptySeat(sbyte seatId)
        {
            var seat = Seats.LookupByKey(seatId);
            if (seat == null)
                return false;
            return seat.IsEmpty();
        }

        public Unit GetPassenger(sbyte seatId)
        {
            var seat = Seats.LookupByKey(seatId);
            if (seat == null)
                return null;

            return Global.ObjAccessor.GetUnit(GetBase(), seat.Passenger.Guid);
        }

        public VehicleSeat GetNextEmptySeat(sbyte seatId, bool next)
        {
            var seat = Seats.LookupByKey(seatId);
            if (seat == null)
                return null;

            var newSeatId = seatId;
            while (!seat.IsEmpty() || HasPendingEventForSeat(newSeatId) || (!seat.SeatInfo.CanEnterOrExit() && !seat.SeatInfo.IsUsableByOverride()))
            {
                if (next)
                {
                    if (!Seats.ContainsKey(++newSeatId))
                        newSeatId = 0;
                }
                else
                {
                    if (!Seats.ContainsKey(newSeatId))
                        newSeatId = SharedConst.MaxVehicleSeats;
                    --newSeatId;
                }

                // Make sure we don't loop indefinetly
                if (newSeatId == seatId)
                    return null;

                seat = Seats[newSeatId];
            }

            return seat;
        }

        /// <summary>
        /// Gets the vehicle seat addon data for the seat of a passenger
        /// </summary>
        /// <param name="passenger">Identifier for the current seat user</param>
        /// <returns>The seat addon data for the currently used seat of a passenger</returns>
        public VehicleSeatAddon GetSeatAddonForSeatOfPassenger(Unit passenger)
        {
            foreach (var pair in Seats)
                if (!pair.Value.IsEmpty() && pair.Value.Passenger.Guid == passenger.GetGUID())
                    return pair.Value.SeatAddon;

            return null;
        }
        
        void InstallAccessory(uint entry, sbyte seatId, bool minion, byte type, uint summonTime)
        {
            // @Prevent adding accessories when vehicle is uninstalling. (Bad script in OnUninstall/OnRemovePassenger/PassengerBoarded hook.)
            
            if (_status == Status.UnInstalling)
            {
                Log.outError(LogFilter.Vehicle, "Vehicle ({0}, Entry: {1}) attempts to install accessory (Entry: {2}) on seat {3} with STATUS_UNINSTALLING! " +
            "Check Uninstall/PassengerBoarded script hooks for errors.", _me.GetGUID().ToString(), GetCreatureEntry(), entry, seatId);
                return;
            }

            Log.outDebug(LogFilter.Vehicle, "Vehicle ({0}, Entry {1}): installing accessory (Entry: {2}) on seat: {3}", _me.GetGUID().ToString(), GetCreatureEntry(), entry, seatId);

            TempSummon accessory = _me.SummonCreature(entry, _me, (TempSummonType)type, TimeSpan.FromMilliseconds(summonTime));
            Cypher.Assert(accessory != null);

            if (minion)
                accessory.AddUnitTypeMask(UnitTypeMask.Accessory);

            _me.HandleSpellClick(accessory, seatId);

            // If for some reason adding accessory to vehicle fails it will unsummon in
            // @VehicleJoinEvent.Abort
        }

        public bool AddVehiclePassenger(Unit unit, sbyte seatId)
        {
            // @Prevent adding passengers when vehicle is uninstalling. (Bad script in OnUninstall/OnRemovePassenger/PassengerBoarded hook.)
            if (_status == Status.UnInstalling)
            {
                Log.outError(LogFilter.Vehicle, "Passenger GuidLow: {0}, Entry: {1}, attempting to board vehicle GuidLow: {2}, Entry: {3} during uninstall! SeatId: {4}",
                    unit.GetGUID().ToString(), unit.GetEntry(), _me.GetGUID().ToString(), _me.GetEntry(), seatId);
                return false;
            }

            Log.outDebug(LogFilter.Vehicle, "Unit {0} scheduling enter vehicle (entry: {1}, vehicleId: {2}, guid: {3} (dbguid: {4}) on seat {5}",
                unit.GetName(), _me.GetEntry(), _vehicleInfo.Id, _me.GetGUID().ToString(),
                (_me.IsTypeId(TypeId.Unit) ? _me.ToCreature().GetSpawnId() : 0), seatId);

            // The seat selection code may kick other passengers off the vehicle.
            // While the validity of the following may be arguable, it is possible that when such a passenger
            // exits the vehicle will dismiss. That's why the actual adding the passenger to the vehicle is scheduled
            // asynchronously, so it can be cancelled easily in case the vehicle is uninstalled meanwhile.
            VehicleJoinEvent e = new(this, unit);
            unit.m_Events.AddEvent(e, unit.m_Events.CalculateTime(TimeSpan.Zero));

            KeyValuePair<sbyte, VehicleSeat> seat = new();
            if (seatId < 0) // no specific seat requirement
            {
                foreach (var _seat in Seats)
                {
                    seat = _seat;
                    if (seat.Value.IsEmpty() && !HasPendingEventForSeat(seat.Key) && (_seat.Value.SeatInfo.CanEnterOrExit() || _seat.Value.SeatInfo.IsUsableByOverride()))
                        break;
                }

                if (seat.Value == null) // no available seat
                {
                    e.ScheduleAbort();
                    return false;
                }

                e.Seat = seat;
                _pendingJoinEvents.Add(e);
            }
            else
            {
                seat = new KeyValuePair<sbyte, VehicleSeat>(seatId, Seats.LookupByKey(seatId));
                if (seat.Value == null)
                {
                    e.ScheduleAbort();
                    return false;
                }

                e.Seat = seat;
                _pendingJoinEvents.Add(e);
                if (!seat.Value.IsEmpty())
                {
                    Unit passenger = Global.ObjAccessor.GetUnit(GetBase(), seat.Value.Passenger.Guid);
                    Cypher.Assert(passenger != null);
                    passenger.ExitVehicle();
                }

                Cypher.Assert(seat.Value.IsEmpty());
            }

            return true;
        }

        public ITransport RemovePassenger(WorldObject passenger)
        {
            Unit unit = passenger.ToUnit();
            if (unit == null)
                return null;

            if (unit.GetVehicle() != this)
                return null;

            var seat = GetSeatKeyValuePairForPassenger(unit);
            Cypher.Assert(seat.Value != null);

            Log.outDebug( LogFilter.Vehicle, "Unit {0} exit vehicle entry {1} id {2} dbguid {3} seat {4}",
                unit.GetName(), _me.GetEntry(), _vehicleInfo.Id, _me.GetGUID().ToString(), seat.Key);

            if (seat.Value.SeatInfo.CanEnterOrExit() && ++UsableSeatNum != 0)
                _me.SetNpcFlag(_me.IsTypeId(TypeId.Player) ? NPCFlags.PlayerVehicle : NPCFlags.SpellClick);

            // Enable gravity for passenger when he did not have it active before entering the vehicle
            if (seat.Value.SeatInfo.HasFlag(VehicleSeatFlags.DisableGravity) && !seat.Value.Passenger.IsGravityDisabled)
                unit.SetDisableGravity(false);

            // Remove UNIT_FLAG_NOT_SELECTABLE if passenger did not have it before entering vehicle
            if (seat.Value.SeatInfo.HasFlag(VehicleSeatFlags.PassengerNotSelectable) && !seat.Value.Passenger.IsUninteractible)
                unit.SetUninteractible(false);

            seat.Value.Passenger.Reset();

            if (_me.IsTypeId(TypeId.Unit) && unit.IsTypeId(TypeId.Player) && seat.Value.SeatInfo.HasFlag(VehicleSeatFlags.CanControl))
                _me.RemoveCharmedBy(unit);

            if (_me.IsInWorld)
                unit.m_movementInfo.ResetTransport();

            // only for flyable vehicles
            if (unit.IsFlying())
                _me.CastSpell(unit, SharedConst.VehicleSpellParachute, true);

            if (_me.IsTypeId(TypeId.Unit) && _me.ToCreature().IsAIEnabled())
                _me.ToCreature().GetAI().PassengerBoarded(unit, seat.Key, false);

            if (GetBase().IsTypeId(TypeId.Unit))
               Global.ScriptMgr.OnRemovePassenger(this, unit);

            unit.SetVehicle(null);
            return this;
        }

        public void RelocatePassengers()
        {
            Cypher.Assert(_me.GetMap() != null);

            List<Tuple<Unit, Position>> seatRelocation = new();

            // not sure that absolute position calculation is correct, it must depend on vehicle pitch angle
            foreach (var pair in Seats)
            {
                Unit passenger = Global.ObjAccessor.GetUnit(GetBase(), pair.Value.Passenger.Guid);
                if (passenger != null)
                {
                    Cypher.Assert(passenger.IsInWorld);

                    float px, py, pz, po;
                    passenger.m_movementInfo.transport.pos.GetPosition(out px, out py, out pz, out po);
                    CalculatePassengerPosition(ref px, ref py, ref pz, ref po);

                    seatRelocation.Add(Tuple.Create(passenger, new Position(px, py, pz, po)));
                }
            }

            foreach (var (passenger, position) in seatRelocation)
                ITransport.UpdatePassengerPosition(this, _me.GetMap(), passenger, position.GetPositionX(), position.GetPositionY(), position.GetPositionZ(), position.GetOrientation(), false);
        }

        public bool IsVehicleInUse()
        {
            foreach (var pair in Seats)
                if (!pair.Value.IsEmpty())
                    return true;

            return false;
        }

        public bool IsControllableVehicle()
        {
            foreach (var itr in Seats)
                if (itr.Value.SeatInfo.HasFlag(VehicleSeatFlags.CanControl))
                    return true;

            return false;
        }
        
        void InitMovementInfoForBase()
        {
            VehicleFlags vehicleFlags = (VehicleFlags)GetVehicleInfo().Flags;

            if (vehicleFlags.HasAnyFlag(VehicleFlags.NoStrafe))
                _me.AddUnitMovementFlag2(MovementFlag2.NoStrafe);
            if (vehicleFlags.HasAnyFlag(VehicleFlags.NoJumping))
                _me.AddUnitMovementFlag2(MovementFlag2.NoJumping);
            if (vehicleFlags.HasAnyFlag(VehicleFlags.Fullspeedturning))
                _me.AddUnitMovementFlag2(MovementFlag2.FullSpeedTurning);
            if (vehicleFlags.HasAnyFlag(VehicleFlags.AllowPitching))
                _me.AddUnitMovementFlag2(MovementFlag2.AlwaysAllowPitching);
            if (vehicleFlags.HasAnyFlag(VehicleFlags.Fullspeedpitching))
                _me.AddUnitMovementFlag2(MovementFlag2.FullSpeedPitching);
        }

        public VehicleSeatRecord GetSeatForPassenger(Unit passenger)
        {
            foreach (var pair in Seats)
                if (pair.Value.Passenger.Guid == passenger.GetGUID())
                    return pair.Value.SeatInfo;

            return null;
        }

        KeyValuePair<sbyte, VehicleSeat> GetSeatKeyValuePairForPassenger(Unit passenger)
        {
            foreach (var pair in Seats)
                if (pair.Value.Passenger.Guid == passenger.GetGUID())
                    return pair;

            return Seats.Last();
        }

        public byte GetAvailableSeatCount()
        {
            byte ret = 0;
            foreach (var pair in Seats)
                if (pair.Value.IsEmpty() && !HasPendingEventForSeat(pair.Key) && (pair.Value.SeatInfo.CanEnterOrExit() || pair.Value.SeatInfo.IsUsableByOverride()))
                    ++ret;

            return ret;
        }

        public ObjectGuid GetTransportGUID() { return GetBase().GetGUID(); }

        public float GetTransportOrientation() { return GetBase().GetOrientation(); }

        public void AddPassenger(WorldObject passenger) { Log.outFatal(LogFilter.Vehicle, "Vehicle cannot directly gain passengers without auras"); }
        
        public void CalculatePassengerPosition(ref float x, ref float y, ref float z, ref float o)
        {
            ITransport.CalculatePassengerPosition(ref x, ref y, ref z, ref o, 
                GetBase().GetPositionX(), GetBase().GetPositionY(), 
                GetBase().GetPositionZ(), GetBase().GetOrientation());
        }

        public void CalculatePassengerOffset(ref float x, ref float y, ref float z, ref float o)
        {
            ITransport.CalculatePassengerOffset(ref x, ref y, ref z, ref o,
                GetBase().GetPositionX(), GetBase().GetPositionY(),
                GetBase().GetPositionZ(), GetBase().GetOrientation());
        }

        public int GetMapIdForSpawning() { return (int)GetBase().GetMapId(); }
        
        public void RemovePendingEvent(VehicleJoinEvent e)
        {
            foreach (var Event in _pendingJoinEvents)
            {
                if (Event == e)
                {
                    _pendingJoinEvents.Remove(Event);
                    break;
                }
            }
        }

        public void RemovePendingEventsForSeat(sbyte seatId)
        {
            for (var i = 0; i < _pendingJoinEvents.Count; ++i)
            {
                var joinEvent = _pendingJoinEvents[i];
                if (joinEvent.Seat.Key == seatId)
                {
                    joinEvent.ScheduleAbort();
                    _pendingJoinEvents.Remove(joinEvent);
                }
            }
        }

        public void RemovePendingEventsForPassenger(Unit passenger)
        {
            for (var i = 0; i< _pendingJoinEvents.Count; ++i)
            {
                var joinEvent = _pendingJoinEvents[i];
                if (joinEvent.Passenger == passenger)
                {
                    joinEvent.ScheduleAbort();
                    _pendingJoinEvents.Remove(joinEvent);
                }
            }
        }

        bool HasPendingEventForSeat(sbyte seatId)
        {
            for (var i = 0; i < _pendingJoinEvents.Count; ++i)
            {
                var joinEvent = _pendingJoinEvents[i];
                if (joinEvent.Seat.Key == seatId)
                    return true;
            }
            return false;
        }
        
        public TimeSpan GetDespawnDelay()
        {
            VehicleTemplate vehicleTemplate = Global.ObjectMgr.GetVehicleTemplate(this);
            if (vehicleTemplate != null)
                return vehicleTemplate.DespawnDelay;

            return TimeSpan.FromMilliseconds(1);
        }

        public string GetDebugInfo()
        {
            StringBuilder str = new StringBuilder("Vehicle seats:\n");
            foreach (var (id, seat) in Seats)
                str.Append($"seat {id}: {(seat.IsEmpty() ? "empty" : seat.Passenger.Guid)}\n");

            str.Append("Vehicle pending events:");

            if (_pendingJoinEvents.Empty())
                str.Append(" none");
            else
            {
                str.Append("\n");
                foreach (var joinEvent in _pendingJoinEvents)
                    str.Append($"seat {joinEvent.Seat.Key}: {joinEvent.Passenger.GetGUID()}\n");
            }

            return str.ToString();
        }

        public Unit GetBase() { return _me; }
        public VehicleRecord GetVehicleInfo() { return _vehicleInfo; }
        public uint GetCreatureEntry() { return _creatureEntry; }

        Unit _me;
        VehicleRecord _vehicleInfo;                   //< DBC data for vehicle

        uint _creatureEntry;                              //< Can be different than the entry of _me in case of players
        Status _status;                                     //< Internal variable for sanity checks

        List<VehicleJoinEvent> _pendingJoinEvents = new();
        public Dictionary<sbyte, VehicleSeat> Seats = new();
        public uint UsableSeatNum;    //< Number of seats that match VehicleSeatEntry.UsableByPlayer, used for proper display flags

        public enum Status
        {
            None,
            Installed,
            UnInstalling,
        }
    }

    public class VehicleJoinEvent : BasicEvent
    {
        public VehicleJoinEvent(Vehicle v, Unit u)
        {
            Target = v;
            Passenger = u;
            Seat = Target.Seats.Last();
        }

        public override bool Execute(ulong e_time, uint p_time)
        {
            Cypher.Assert(Passenger.IsInWorld);
            Cypher.Assert(Target != null && Target.GetBase().IsInWorld);

            var vehicleAuras = Target.GetBase().GetAuraEffectsByType(AuraType.ControlVehicle);
            var aurEffect = vehicleAuras.Find(aurEff => aurEff.GetCasterGUID() == Passenger.GetGUID());
            Cypher.Assert(aurEffect != null);

            var aurApp = aurEffect.GetBase().GetApplicationOfTarget(Target.GetBase().GetGUID());
            Cypher.Assert(aurApp != null && !aurApp.HasRemoveMode());

            Target.RemovePendingEventsForSeat(Seat.Key);
            Target.RemovePendingEventsForPassenger(Passenger);

            // Passenger might've died in the meantime - abort if this is the case
            if (!Passenger.IsAlive())
            {
                Abort(0);
                return true;
            }

            //It's possible that multiple vehicle join
            //events are executed in the same update
            if (Passenger.GetVehicle() != null)
                Passenger.ExitVehicle();

            Passenger.SetVehicle(Target);
            Seat.Value.Passenger.Guid = Passenger.GetGUID();
            Seat.Value.Passenger.IsUninteractible = Passenger.IsUninteractible();
            Seat.Value.Passenger.IsGravityDisabled = Passenger.HasUnitMovementFlag(MovementFlag.DisableGravity);
            if (Seat.Value.SeatInfo.CanEnterOrExit())
            {
                Cypher.Assert(Target.UsableSeatNum != 0);
                --Target.UsableSeatNum;
                if (Target.UsableSeatNum == 0)
                {
                    if (Target.GetBase().IsTypeId(TypeId.Player))
                        Target.GetBase().RemoveNpcFlag(NPCFlags.PlayerVehicle);
                    else
                        Target.GetBase().RemoveNpcFlag(NPCFlags.SpellClick);
                }
            }

            Passenger.InterruptNonMeleeSpells(false);
            Passenger.RemoveAurasByType(AuraType.Mounted);

            VehicleSeatRecord veSeat = Seat.Value.SeatInfo;
            VehicleSeatAddon veSeatAddon = Seat.Value.SeatAddon;

            Player player = Passenger.ToPlayer();
            if (player != null)
            {
                // drop flag
                Battleground bg = player.GetBattleground();
                if (bg != null)
                    bg.EventPlayerDroppedFlag(player);

                player.StopCastingCharm();
                player.StopCastingBindSight();
                player.SendOnCancelExpectedVehicleRideAura();
                if (!veSeat.HasFlag(VehicleSeatFlagsB.KeepPet))
                    player.UnsummonPetTemporaryIfAny();
            }

            if (veSeat.HasFlag(VehicleSeatFlags.DisableGravity))
                Passenger.SetDisableGravity(true);

            float o = veSeatAddon != null ? veSeatAddon.SeatOrientationOffset : 0.0f;
            float x = veSeat.AttachmentOffset.X;
            float y = veSeat.AttachmentOffset.Y;
            float z = veSeat.AttachmentOffset.Z;

            Passenger.m_movementInfo.transport.pos.Relocate(x, y, z, o);
            Passenger.m_movementInfo.transport.time = 0;
            Passenger.m_movementInfo.transport.seat = Seat.Key;
            Passenger.m_movementInfo.transport.guid = Target.GetBase().GetGUID();

            if (Target.GetBase().IsTypeId(TypeId.Unit) && Passenger.IsTypeId(TypeId.Player) && Seat.Value.SeatInfo.HasFlag(VehicleSeatFlags.CanControl))
            {
                // handles SMSG_CLIENT_CONTROL
                if (!Target.GetBase().SetCharmedBy(Passenger, CharmType.Vehicle, aurApp))
                {
                    // charming failed, probably aura was removed by relocation/scripts/whatever
                    Abort(0);
                    return true;
                }
            }

            Passenger.SendClearTarget();                            // SMSG_BREAK_TARGET
            Passenger.SetControlled(true, UnitState.Root);         // SMSG_FORCE_ROOT - In some cases we send SMSG_SPLINE_MOVE_ROOT here (for creatures)
                                                                   // also adds MOVEMENTFLAG_ROOT

            var initializer = (MoveSplineInit init) =>
            {
                init.DisableTransportPathTransformations();
                init.MoveTo(x, y, z, false, true);
                init.SetFacing(o);
                init.SetTransportEnter();
            };
            Passenger.GetMotionMaster().LaunchMoveSpline(initializer, EventId.VehicleBoard, MovementGeneratorPriority.Highest);

            foreach (var (_, threatRef) in Passenger.GetThreatManager().GetThreatenedByMeList())
                threatRef.GetOwner().GetThreatManager().AddThreat(Target.GetBase(), threatRef.GetThreat(), null, true, true);

            Creature creature = Target.GetBase().ToCreature();
            if (creature != null)
            {
                CreatureAI ai = creature.GetAI();
                if (ai != null)
                    ai.PassengerBoarded(Passenger, Seat.Key, true);

                Global.ScriptMgr.OnAddPassenger(Target, Passenger, Seat.Key);

                // Actually quite a redundant hook. Could just use OnAddPassenger and check for unit typemask inside script.
                if (Passenger.HasUnitTypeMask(UnitTypeMask.Accessory))
                    Global.ScriptMgr.OnInstallAccessory(Target, Passenger.ToCreature());
            }

            return true;
        }

        public override void Abort(ulong e_time)
        {
            // Check if the Vehicle was already uninstalled, in which case all auras were removed already
            if (Target != null)
            {
                Log.outDebug(LogFilter.Vehicle, "Passenger GuidLow: {0}, Entry: {1}, board on vehicle GuidLow: {2}, Entry: {3} SeatId: {4} cancelled",
                    Passenger.GetGUID().ToString(), Passenger.GetEntry(), Target.GetBase().GetGUID().ToString(), Target.GetBase().GetEntry(), Seat.Key);

                // Remove the pending event when Abort was called on the event directly
                Target.RemovePendingEvent(this);

                // @SPELL_AURA_CONTROL_VEHICLE auras can be applied even when the passenger is not (yet) on the vehicle.
                // When this code is triggered it means that something went wrong in @Vehicle.AddPassenger, and we should remove
                // the aura manually.
                Target.GetBase().RemoveAurasByType(AuraType.ControlVehicle, Passenger.GetGUID());
            }
            else
                Log.outDebug(LogFilter.Vehicle, "Passenger GuidLow: {0}, Entry: {1}, board on uninstalled vehicle SeatId: {2} cancelled",
                    Passenger.GetGUID().ToString(), Passenger.GetEntry(), Seat.Key);

            if (Passenger.IsInWorld && Passenger.HasUnitTypeMask(UnitTypeMask.Accessory))
                Passenger.ToCreature().DespawnOrUnsummon();
        }

        public Vehicle Target;
        public Unit Passenger;
        public KeyValuePair<sbyte, VehicleSeat> Seat;
    }

    public struct PassengerInfo
    {
        public ObjectGuid Guid;
        public bool IsUninteractible;
        public bool IsGravityDisabled;

        public void Reset()
        {
            Guid = ObjectGuid.Empty;
            IsUninteractible = false;
            IsGravityDisabled = false;
        }
    }

    public class VehicleSeat
    {
        public VehicleSeat(VehicleSeatRecord seatInfo, VehicleSeatAddon seatAddon)
        {
            SeatInfo = seatInfo;
            SeatAddon = seatAddon;
            Passenger.Reset();
        }

        public bool IsEmpty() { return Passenger.Guid.IsEmpty(); }

        public VehicleSeatRecord SeatInfo;
        public VehicleSeatAddon SeatAddon;
        public PassengerInfo Passenger;
    }

    public struct VehicleAccessory
    {
        public VehicleAccessory(uint entry, sbyte seatId, bool isMinion, byte summonType, uint summonTime)
        {
            AccessoryEntry = entry;
            IsMinion = isMinion;
            SummonTime = summonTime;
            SeatId = seatId;
            SummonedType = summonType;
        }
        public uint AccessoryEntry;
        public bool IsMinion;
        public uint SummonTime;
        public sbyte SeatId;
        public byte SummonedType;
    }

    public class VehicleTemplate
    {
        public TimeSpan DespawnDelay;
    }

    public class VehicleSeatAddon
    {
        public float SeatOrientationOffset;
        public float ExitParameterX;
        public float ExitParameterY;
        public float ExitParameterZ;
        public float ExitParameterO;
        public VehicleExitParameters ExitParameter;

        public VehicleSeatAddon(float orientatonOffset, float exitX, float exitY, float exitZ, float exitO, byte param)
        {
            SeatOrientationOffset = orientatonOffset;
            ExitParameterX = exitX;
            ExitParameterY = exitY;
            ExitParameterZ = exitZ;
            ExitParameterO = exitO;
            ExitParameter = (VehicleExitParameters)param;
        }
    }
}
