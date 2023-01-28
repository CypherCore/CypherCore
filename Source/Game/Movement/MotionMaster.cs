// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Game.AI;
using Game.DataStorage;
using Game.Entities;

namespace Game.Movement
{
	internal class MovementGeneratorComparator : IComparer<MovementGenerator>
	{
		public int Compare(MovementGenerator a, MovementGenerator b)
		{
			if (a.Equals(b))
				return 0;

			if (a.Mode > b.Mode)
				return 1;
			else if (a.Mode == b.Mode)
				return a.Priority.CompareTo(b.Priority);

			return -1;
		}
	}

	public struct MovementGeneratorInformation
	{
		public MovementGeneratorType Type;
		public ObjectGuid TargetGUID;
		public string TargetName;

		public MovementGeneratorInformation(MovementGeneratorType type, ObjectGuid targetGUID, string targetName = "")
		{
			Type       = type;
			TargetGUID = targetGUID;
			TargetName = targetName;
		}
	}

	internal class DelayedAction
	{
		private Action Action;
		private MotionMasterDelayedActionType Type;
		private Func<bool> Validator;

		public DelayedAction(Action action, Func<bool> validator, MotionMasterDelayedActionType type)
		{
			Action    = action;
			Validator = validator;
			Type      = type;
		}

		public DelayedAction(Action action, MotionMasterDelayedActionType type)
		{
			Action    = action;
			Validator = () => true;
			Type      = type;
		}

		public void Resolve()
		{
			if (Validator())
				Action();
		}
	}

	public class MotionMaster
	{
		public const double gravity = 19.29110527038574;
		public const float SPEED_CHARGE = 42.0f;
		private static IdleMovementGenerator staticIdleMovement = new();
		private static uint splineId;

		public MotionMaster(Unit unit)
		{
			_owner = unit;
			_flags = MotionMasterFlags.InitializationPending;
		}

		private Unit _owner { get; }
		private MovementGenerator _defaultGenerator { get; set; }
		private SortedSet<MovementGenerator> _generators { get; } = new(new MovementGeneratorComparator());

		private MultiMap<uint, MovementGenerator> _baseUnitStatesMap { get; } = new();
		private Queue<DelayedAction> _delayedActions { get; } = new();
		private MotionMasterFlags _flags { get; set; }

		public static uint SplineId => splineId++;

		public void Initialize()
		{
			if (HasFlag(MotionMasterFlags.InitializationPending))
				return;

			if (HasFlag(MotionMasterFlags.Update))
			{
				_delayedActions.Enqueue(new DelayedAction(() => Initialize(), MotionMasterDelayedActionType.Initialize));

				return;
			}

			DirectInitialize();
		}

		public void InitializeDefault()
		{
			Add(AISelector.SelectMovementGenerator(_owner), MovementSlot.Default);
		}

		public void AddToWorld()
		{
			if (!HasFlag(MotionMasterFlags.InitializationPending))
				return;

			AddFlag(MotionMasterFlags.Initializing);
			RemoveFlag(MotionMasterFlags.InitializationPending);

			DirectInitialize();
			ResolveDelayedActions();

			RemoveFlag(MotionMasterFlags.Initializing);
		}

		public bool Empty()
		{
			return _defaultGenerator == null && _generators.Empty();
		}

		public int Size()
		{
			return (_defaultGenerator != null ? 1 : 0) + _generators.Count;
		}

		public List<MovementGeneratorInformation> GetMovementGeneratorsInformation()
		{
			List<MovementGeneratorInformation> list = new();

			if (_defaultGenerator != null)
				list.Add(new MovementGeneratorInformation(_defaultGenerator.GetMovementGeneratorType(), ObjectGuid.Empty, ""));

			foreach (var movement in _generators)
			{
				MovementGeneratorType type = movement.GetMovementGeneratorType();

				switch (type)
				{
					case MovementGeneratorType.Chase:
					case MovementGeneratorType.Follow:
						var followInformation = movement as FollowMovementGenerator;

						if (followInformation != null)
						{
							Unit target = followInformation.GetTarget();

							if (target != null)
								list.Add(new MovementGeneratorInformation(type, target.GetGUID(), target.GetName()));
							else
								list.Add(new MovementGeneratorInformation(type, ObjectGuid.Empty));
						}
						else
						{
							list.Add(new MovementGeneratorInformation(type, ObjectGuid.Empty));
						}

						break;
					default:
						list.Add(new MovementGeneratorInformation(type, ObjectGuid.Empty));

						break;
				}
			}

			return list;
		}

		public MovementSlot GetCurrentSlot()
		{
			if (!_generators.Empty())
				return MovementSlot.Active;

			if (_defaultGenerator != null)
				return MovementSlot.Default;

			return MovementSlot.Max;
		}

		public MovementGenerator GetCurrentMovementGenerator()
		{
			if (!_generators.Empty())
				return _generators.FirstOrDefault();

			if (_defaultGenerator != null)
				return _defaultGenerator;

			return null;
		}

		public MovementGeneratorType GetCurrentMovementGeneratorType()
		{
			if (Empty())
				return MovementGeneratorType.Max;

			MovementGenerator movement = GetCurrentMovementGenerator();

			if (movement == null)
				return MovementGeneratorType.Max;

			return movement.GetMovementGeneratorType();
		}

		public MovementGeneratorType GetCurrentMovementGeneratorType(MovementSlot slot)
		{
			if (Empty() ||
			    IsInvalidMovementSlot(slot))
				return MovementGeneratorType.Max;

			if (slot == MovementSlot.Active &&
			    !_generators.Empty())
				return _generators.FirstOrDefault().GetMovementGeneratorType();

			if (slot == MovementSlot.Default &&
			    _defaultGenerator != null)
				return _defaultGenerator.GetMovementGeneratorType();

			return MovementGeneratorType.Max;
		}

		public MovementGenerator GetCurrentMovementGenerator(MovementSlot slot)
		{
			if (Empty() ||
			    IsInvalidMovementSlot(slot))
				return null;

			if (slot == MovementSlot.Active &&
			    !_generators.Empty())
				return _generators.FirstOrDefault();

			if (slot == MovementSlot.Default &&
			    _defaultGenerator != null)
				return _defaultGenerator;

			return null;
		}

		public MovementGenerator GetMovementGenerator(Func<MovementGenerator, bool> filter, MovementSlot slot = MovementSlot.Active)
		{
			if (Empty() ||
			    IsInvalidMovementSlot(slot))
				return null;

			MovementGenerator movement = null;

			switch (slot)
			{
				case MovementSlot.Default:
					if (_defaultGenerator != null &&
					    filter(_defaultGenerator))
						movement = _defaultGenerator;

					break;
				case MovementSlot.Active:
					if (!_generators.Empty())
					{
						var itr = _generators.FirstOrDefault(filter);

						if (itr != null)
							movement = itr;
					}

					break;
				default:
					break;
			}

			return movement;
		}

		public bool HasMovementGenerator(Func<MovementGenerator, bool> filter, MovementSlot slot = MovementSlot.Active)
		{
			if (Empty() ||
			    IsInvalidMovementSlot(slot))
				return false;

			bool value = false;

			switch (slot)
			{
				case MovementSlot.Default:
					if (_defaultGenerator != null &&
					    filter(_defaultGenerator))
						value = true;

					break;
				case MovementSlot.Active:
					if (!_generators.Empty())
					{
						var itr = _generators.FirstOrDefault(filter);
						value = itr != null;
					}

					break;
				default:
					break;
			}

			return value;
		}

		public void Update(uint diff)
		{
			if (!_owner)
				return;

			if (HasFlag(MotionMasterFlags.InitializationPending | MotionMasterFlags.Initializing))
				return;

			Cypher.Assert(!Empty(), $"MotionMaster:Update: update called without Initializing! ({_owner.GetGUID()})");

			AddFlag(MotionMasterFlags.Update);

			MovementGenerator top = GetCurrentMovementGenerator();

			if (HasFlag(MotionMasterFlags.StaticInitializationPending) &&
			    IsStatic(top))
			{
				RemoveFlag(MotionMasterFlags.StaticInitializationPending);
				top.Initialize(_owner);
			}

			if (top.HasFlag(MovementGeneratorFlags.InitializationPending))
				top.Initialize(_owner);

			if (top.HasFlag(MovementGeneratorFlags.Deactivated))
				top.Reset(_owner);

			Cypher.Assert(!top.HasFlag(MovementGeneratorFlags.InitializationPending | MovementGeneratorFlags.Deactivated), $"MotionMaster:Update: update called on an uninitialized top! ({_owner.GetGUID()}) (Type: {top.GetMovementGeneratorType()}, flags: {top.Flags})");

			if (!top.Update(_owner, diff))
			{
				Cypher.Assert(top == GetCurrentMovementGenerator(), $"MotionMaster::Update: top was modified while updating! ({_owner.GetGUID()})");

				// Since all the actions that modify any Slot are delayed, this movement is guaranteed to be top
				Pop(true, true); // Natural, and only, call to MovementInform
			}

			RemoveFlag(MotionMasterFlags.Update);

			ResolveDelayedActions();
		}

		private void Add(MovementGenerator movement, MovementSlot slot = MovementSlot.Active)
		{
			if (movement == null)
				return;

			if (IsInvalidMovementSlot(slot))
				return;

			if (HasFlag(MotionMasterFlags.Delayed))
				_delayedActions.Enqueue(new DelayedAction(() => Add(movement, slot), MotionMasterDelayedActionType.Add));
			else
				DirectAdd(movement, slot);
		}

		public void Remove(MovementGenerator movement, MovementSlot slot = MovementSlot.Active)
		{
			if (movement == null ||
			    IsInvalidMovementSlot(slot))
				return;

			if (HasFlag(MotionMasterFlags.Delayed))
			{
				_delayedActions.Enqueue(new DelayedAction(() => Remove(movement, slot), MotionMasterDelayedActionType.Remove));

				return;
			}

			if (Empty())
				return;

			switch (slot)
			{
				case MovementSlot.Default:
					if (_defaultGenerator != null &&
					    _defaultGenerator == movement)
						DirectClearDefault();

					break;
				case MovementSlot.Active:
					if (!_generators.Empty())
						if (_generators.Contains(movement))
							Remove(movement, GetCurrentMovementGenerator() == movement, false);

					break;
				default:
					break;
			}
		}

		public void Remove(MovementGeneratorType type, MovementSlot slot = MovementSlot.Active)
		{
			if (IsInvalidMovementGeneratorType(type) ||
			    IsInvalidMovementSlot(slot))
				return;

			if (HasFlag(MotionMasterFlags.Delayed))
			{
				_delayedActions.Enqueue(new DelayedAction(() => Remove(type, slot), MotionMasterDelayedActionType.RemoveType));

				return;
			}

			if (Empty())
				return;

			switch (slot)
			{
				case MovementSlot.Default:
					if (_defaultGenerator != null &&
					    _defaultGenerator.GetMovementGeneratorType() == type)
						DirectClearDefault();

					break;
				case MovementSlot.Active:
					if (!_generators.Empty())
					{
						var itr = _generators.FirstOrDefault(a => a.GetMovementGeneratorType() == type);

						if (itr != null)
							Remove(itr, GetCurrentMovementGenerator() == itr, false);
					}

					break;
				default:
					break;
			}
		}

		public void Clear()
		{
			if (HasFlag(MotionMasterFlags.Delayed))
			{
				_delayedActions.Enqueue(new DelayedAction(() => Clear(), MotionMasterDelayedActionType.Clear));

				return;
			}

			if (!Empty())
				DirectClear();
		}

		public void Clear(MovementSlot slot)
		{
			if (IsInvalidMovementSlot(slot))
				return;

			if (HasFlag(MotionMasterFlags.Delayed))
			{
				_delayedActions.Enqueue(new DelayedAction(() => Clear(slot), MotionMasterDelayedActionType.ClearSlot));

				return;
			}

			if (Empty())
				return;

			switch (slot)
			{
				case MovementSlot.Default:
					DirectClearDefault();

					break;
				case MovementSlot.Active:
					DirectClear();

					break;
				default:
					break;
			}
		}

		public void Clear(MovementGeneratorMode mode)
		{
			if (HasFlag(MotionMasterFlags.Delayed))
			{
				_delayedActions.Enqueue(new DelayedAction(() => Clear(mode), MotionMasterDelayedActionType.ClearMode));

				return;
			}

			if (Empty())
				return;

			DirectClear(a => a.Mode == mode);
		}

		public void Clear(MovementGeneratorPriority priority)
		{
			if (HasFlag(MotionMasterFlags.Delayed))
			{
				_delayedActions.Enqueue(new DelayedAction(() => Clear(priority), MotionMasterDelayedActionType.ClearPriority));

				return;
			}

			if (Empty())
				return;

			DirectClear(a => a.Priority == priority);
		}

		public void PropagateSpeedChange()
		{
			if (Empty())
				return;

			MovementGenerator movement = GetCurrentMovementGenerator();

			if (movement == null)
				return;

			movement.UnitSpeedChanged();
		}

		public bool GetDestination(out float x, out float y, out float z)
		{
			x = 0f;
			y = 0f;
			z = 0f;

			if (_owner.MoveSpline.Finalized())
				return false;

			Vector3 dest = _owner.MoveSpline.FinalDestination();
			x = dest.X;
			y = dest.Y;
			z = dest.Z;

			return true;
		}

		public bool StopOnDeath()
		{
			MovementGenerator movementGenerator = GetCurrentMovementGenerator();

			if (movementGenerator != null)
				if (movementGenerator.HasFlag(MovementGeneratorFlags.PersistOnDeath))
					return false;

			if (_owner.IsInWorld)
			{
				// Only clear MotionMaster for entities that exists in world
				// Avoids crashes in the following conditions :
				//  * Using 'call pet' on dead pets
				//  * Using 'call stabled pet'
				//  * Logging in with dead pets
				Clear();
				MoveIdle();
			}

			_owner.StopMoving();

			return true;
		}

		public void MoveIdle()
		{
			Add(GetIdleMovementGenerator(), MovementSlot.Default);
		}

		public void MoveTargetedHome()
		{
			Creature owner = _owner.ToCreature();

			if (owner == null)
			{
				Log.outError(LogFilter.Movement, $"MotionMaster::MoveTargetedHome: '{_owner.GetGUID()}', attempted to move towards target home.");

				return;
			}

			Clear();

			Unit target = owner.GetCharmerOrOwner();

			if (target == null)
				Add(new HomeMovementGenerator<Creature>());
			else
				Add(new FollowMovementGenerator(target, SharedConst.PetFollowDist, new ChaseAngle(SharedConst.PetFollowAngle)));
		}

		public void MoveRandom(float wanderDistance = 0.0f)
		{
			if (_owner.IsTypeId(TypeId.Unit))
				Add(new RandomMovementGenerator(wanderDistance), MovementSlot.Default);
		}

		public void MoveFollow(Unit target, float dist, float angle = 0.0f, MovementSlot slot = MovementSlot.Active)
		{
			MoveFollow(target, dist, new ChaseAngle(angle), slot);
		}

		public void MoveFollow(Unit target, float dist, ChaseAngle angle, MovementSlot slot = MovementSlot.Active)
		{
			// Ignore movement request if target not exist
			if (!target ||
			    target == _owner)
				return;

			Add(new FollowMovementGenerator(target, dist, angle), slot);
		}

		public void MoveChase(Unit target, float dist, float angle = 0.0f)
		{
			MoveChase(target, new ChaseRange(dist), new ChaseAngle(angle));
		}

		public void MoveChase(Unit target, float dist)
		{
			MoveChase(target, new ChaseRange(dist));
		}

		public void MoveChase(Unit target, ChaseRange? dist = null, ChaseAngle? angle = null)
		{
			// Ignore movement request if target not exist
			if (!target ||
			    target == _owner)
				return;

			Add(new ChaseMovementGenerator(target, dist, angle));
		}

		public void MoveConfused()
		{
			if (_owner.IsTypeId(TypeId.Player))
				Add(new ConfusedMovementGenerator<Player>());
			else
				Add(new ConfusedMovementGenerator<Creature>());
		}

		public void MoveFleeing(Unit enemy, uint time)
		{
			if (!enemy)
				return;

			if (_owner.IsCreature())
			{
				if (time != 0)
					Add(new TimedFleeingMovementGenerator(enemy.GetGUID(), time));
				else
					Add(new FleeingMovementGenerator<Creature>(enemy.GetGUID()));
			}
			else
			{
				Add(new FleeingMovementGenerator<Player>(enemy.GetGUID()));
			}
		}

		public void MovePoint(uint id, Position pos, bool generatePath = true, float? finalOrient = null)
		{
			MovePoint(id, pos.posX, pos.posY, pos.posZ, generatePath, finalOrient);
		}

		public void MovePoint(uint id, float x, float y, float z, bool generatePath = true, float? finalOrient = null)
		{
			if (_owner.IsTypeId(TypeId.Player))
				Add(new PointMovementGenerator<Player>(id, x, y, z, generatePath, 0.0f, finalOrient));
			else
				Add(new PointMovementGenerator<Creature>(id, x, y, z, generatePath, 0.0f, finalOrient));
		}

		public void MoveCloserAndStop(uint id, Unit target, float distance)
		{
			float distanceToTravel = _owner.GetExactDist2d(target) - distance;

			if (distanceToTravel > 0.0f)
			{
				float angle = _owner.GetAbsoluteAngle(target);
				float destx = _owner.GetPositionX() + distanceToTravel * (float)Math.Cos(angle);
				float desty = _owner.GetPositionY() + distanceToTravel * (float)Math.Sin(angle);
				MovePoint(id, destx, desty, target.GetPositionZ());
			}
			else
			{
				// We are already close enough. We just need to turn toward the target without changing position.
				var initializer = (MoveSplineInit init) =>
				                  {
					                  init.MoveTo(_owner.GetPositionX(), _owner.GetPositionY(), _owner.GetPositionZ());
					                  Unit refreshedTarget = Global.ObjAccessor.GetUnit(_owner, target.GetGUID());

					                  if (refreshedTarget != null)
						                  init.SetFacing(refreshedTarget);
				                  };

				Add(new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, id));
			}
		}

		public void MoveLand(uint id, Position pos, float? velocity = null)
		{
			var initializer = (MoveSplineInit init) =>
			                  {
				                  init.MoveTo(pos, false);
				                  init.SetAnimation(AnimTier.Ground);

				                  if (velocity.HasValue)
					                  init.SetVelocity(velocity.Value);
			                  };

			Add(new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, id));
		}

		public void MoveTakeoff(uint id, Position pos, float? velocity = null)
		{
			var initializer = (MoveSplineInit init) =>
			                  {
				                  init.MoveTo(pos, false);
				                  init.SetAnimation(AnimTier.Hover);

				                  if (velocity.HasValue)
					                  init.SetVelocity(velocity.Value);
			                  };

			Add(new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, id));
		}

		public void MoveCharge(float x, float y, float z, float speed = SPEED_CHARGE, uint id = EventId.Charge, bool generatePath = false, Unit target = null, SpellEffectExtraData spellEffectExtraData = null)
		{
			/*
			if (_slot[(int)MovementSlot.Controlled] != null && _slot[(int)MovementSlot.Controlled].GetMovementGeneratorType() != MovementGeneratorType.Distract)
			    return;
			*/

			if (_owner.IsTypeId(TypeId.Player))
			{
				PointMovementGenerator<Player> movement = new(id, x, y, z, generatePath, speed, null, target, spellEffectExtraData);
				movement.Priority      = MovementGeneratorPriority.Highest;
				movement.BaseUnitState = UnitState.Charging;
				Add(movement);
			}
			else
			{
				PointMovementGenerator<Creature> movement = new(id, x, y, z, generatePath, speed, null, target, spellEffectExtraData);
				movement.Priority      = MovementGeneratorPriority.Highest;
				movement.BaseUnitState = UnitState.Charging;
				Add(movement);
			}
		}

		public void MoveCharge(PathGenerator path, float speed = SPEED_CHARGE, Unit target = null, SpellEffectExtraData spellEffectExtraData = null)
		{
			Vector3 dest = path.GetActualEndPosition();

			MoveCharge(dest.X, dest.Y, dest.Z, SPEED_CHARGE, EventId.ChargePrepath);

			// Charge movement is not started when using EVENT_CHARGE_PREPATH
			MoveSplineInit init = new(_owner);
			init.MovebyPath(path.GetPath());
			init.SetVelocity(speed);

			if (target != null)
				init.SetFacing(target);

			if (spellEffectExtraData != null)
				init.SetSpellEffectExtraData(spellEffectExtraData);

			init.Launch();
		}

		public void MoveKnockbackFrom(Position origin, float speedXY, float speedZ, SpellEffectExtraData spellEffectExtraData = null)
		{
			//This function may make players fall below map
			if (_owner.IsTypeId(TypeId.Player))
				return;

			if (speedXY < 0.01f)
				return;

			Position dest         = _owner.GetPosition();
			float    moveTimeHalf = (float)(speedZ / gravity);
			float    dist         = 2 * moveTimeHalf * speedXY;
			float    max_height   = -MoveSpline.ComputeFallElevation(moveTimeHalf, false, -speedZ);

			// Use a mmap raycast to get a valid destination.
			_owner.MovePositionToFirstCollision(dest, dist, _owner.GetRelativeAngle(origin) + MathF.PI);

			var initializer = (MoveSplineInit init) =>
			                  {
				                  init.MoveTo(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ(), false);
				                  init.SetParabolic(max_height, 0);
				                  init.SetOrientationFixed(true);
				                  init.SetVelocity(speedXY);

				                  if (spellEffectExtraData != null)
					                  init.SetSpellEffectExtraData(spellEffectExtraData);
			                  };

			GenericMovementGenerator movement = new(initializer, MovementGeneratorType.Effect, 0);
			movement.Priority = MovementGeneratorPriority.Highest;
			movement.AddFlag(MovementGeneratorFlags.PersistOnDeath);
			Add(movement);
		}

		public void MoveJumpTo(float angle, float speedXY, float speedZ)
		{
			//This function may make players fall below map
			if (_owner.IsTypeId(TypeId.Player))
				return;

			float moveTimeHalf = (float)(speedZ / gravity);
			float dist         = 2 * moveTimeHalf * speedXY;
			_owner.GetNearPoint2D(null, out float x, out float y, dist, _owner.GetOrientation() + angle);
			float z = _owner.GetPositionZ();
			_owner.UpdateAllowedPositionZ(x, y, ref z);
			MoveJump(x, y, z, 0.0f, speedXY, speedZ);
		}

		public void MoveJump(Position pos, float speedXY, float speedZ, uint id = EventId.Jump, bool hasOrientation = false, JumpArrivalCastArgs arrivalCast = null, SpellEffectExtraData spellEffectExtraData = null)
		{
			MoveJump(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), pos.GetOrientation(), speedXY, speedZ, id, hasOrientation, arrivalCast, spellEffectExtraData);
		}

		public void MoveJump(float x, float y, float z, float o, float speedXY, float speedZ, uint id = EventId.Jump, bool hasOrientation = false, JumpArrivalCastArgs arrivalCast = null, SpellEffectExtraData spellEffectExtraData = null)
		{
			Log.outDebug(LogFilter.Server, "Unit ({0}) jump to point (X: {1} Y: {2} Z: {3})", _owner.GetGUID().ToString(), x, y, z);

			if (speedXY < 0.01f)
				return;

			float moveTimeHalf = (float)(speedZ / gravity);
			float max_height   = -MoveSpline.ComputeFallElevation(moveTimeHalf, false, -speedZ);

			var initializer = (MoveSplineInit init) =>
			                  {
				                  init.MoveTo(x, y, z, false);
				                  init.SetParabolic(max_height, 0);
				                  init.SetVelocity(speedXY);

				                  if (hasOrientation)
					                  init.SetFacing(o);

				                  if (spellEffectExtraData != null)
					                  init.SetSpellEffectExtraData(spellEffectExtraData);
			                  };

			uint       arrivalSpellId         = 0;
			ObjectGuid arrivalSpellTargetGuid = ObjectGuid.Empty;

			if (arrivalCast != null)
			{
				arrivalSpellId         = arrivalCast.SpellId;
				arrivalSpellTargetGuid = arrivalCast.Target;
			}

			GenericMovementGenerator movement = new(initializer, MovementGeneratorType.Effect, id, arrivalSpellId, arrivalSpellTargetGuid);
			movement.Priority      = MovementGeneratorPriority.Highest;
			movement.BaseUnitState = UnitState.Jumping;
			Add(movement);
		}

		public void MoveJumpWithGravity(Position pos, float speedXY, float gravity, uint id = EventId.Jump, bool hasOrientation = false, JumpArrivalCastArgs arrivalCast = null, SpellEffectExtraData spellEffectExtraData = null)
		{
			Log.outDebug(LogFilter.Movement, $"MotionMaster.MoveJumpWithGravity: '{_owner.GetGUID()}', jumps to point Id: {id} ({pos})");

			if (speedXY < 0.01f)
				return;

			var initializer = (MoveSplineInit init) =>
			                  {
				                  init.MoveTo(pos.GetPositionX(), pos.GetPositionY(), pos.GetPositionZ(), false);
				                  init.SetParabolicVerticalAcceleration(gravity, 0);
				                  init.SetUncompressed();
				                  init.SetVelocity(speedXY);
				                  init.SetUnlimitedSpeed();

				                  if (hasOrientation)
					                  init.SetFacing(pos.GetOrientation());

				                  if (spellEffectExtraData != null)
					                  init.SetSpellEffectExtraData(spellEffectExtraData);
			                  };

			uint       arrivalSpellId         = 0;
			ObjectGuid arrivalSpellTargetGuid = default;

			if (arrivalCast != null)
			{
				arrivalSpellId         = arrivalCast.SpellId;
				arrivalSpellTargetGuid = arrivalCast.Target;
			}

			GenericMovementGenerator movement = new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, id, arrivalSpellId, arrivalSpellTargetGuid);
			movement.Priority      = MovementGeneratorPriority.Highest;
			movement.BaseUnitState = UnitState.Jumping;
			movement.AddFlag(MovementGeneratorFlags.PersistOnDeath);
			Add(movement);
		}

		public void MoveCirclePath(float x, float y, float z, float radius, bool clockwise, byte stepCount)
		{
			var initializer = (MoveSplineInit init) =>
			                  {
				                  float    step  = 2 * MathFunctions.PI / stepCount * (clockwise ? -1.0f : 1.0f);
				                  Position pos   = new(x, y, z, 0.0f);
				                  float    angle = pos.GetAbsoluteAngle(_owner.GetPositionX(), _owner.GetPositionY());

				                  // add the owner's current position as starting point as it gets removed after entering the cycle
				                  init.Path().Add(new Vector3(_owner.GetPositionX(), _owner.GetPositionY(), _owner.GetPositionZ()));

				                  for (byte i = 0; i < stepCount; angle += step, ++i)
				                  {
					                  Vector3 point = new();
					                  point.X = (float)(x + radius * Math.Cos(angle));
					                  point.Y = (float)(y + radius * Math.Sin(angle));

					                  if (_owner.IsFlying())
						                  point.Z = z;
					                  else
						                  point.Z = _owner.GetMapHeight(point.X, point.Y, z) + _owner.GetHoverOffset();

					                  init.Path().Add(point);
				                  }

				                  if (_owner.IsFlying())
				                  {
					                  init.SetFly();
					                  init.SetCyclic();
					                  init.SetAnimation(AnimTier.Hover);
				                  }
				                  else
				                  {
					                  init.SetWalk(true);
					                  init.SetCyclic();
				                  }
			                  };

			Add(new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, 0));
		}

		public void MoveSmoothPath(uint pointId, Vector3[] pathPoints, int pathSize, bool walk = false, bool fly = false)
		{
			var initializer = (MoveSplineInit init) =>
			                  {
				                  init.MovebyPath(pathPoints);
				                  init.SetWalk(walk);

				                  if (fly)
				                  {
					                  init.SetFly();
					                  init.SetUncompressed();
					                  init.SetSmooth();
				                  }
			                  };

			// This code is not correct
			// GenericMovementGenerator does not affect UNIT_STATE_ROAMING_MOVE
			// need to call PointMovementGenerator with various pointIds
			Add(new GenericMovementGenerator(initializer, MovementGeneratorType.Effect, pointId));
		}

		public void MoveAlongSplineChain(uint pointId, uint dbChainId, bool walk)
		{
			Creature owner = _owner.ToCreature();

			if (!owner)
			{
				Log.outError(LogFilter.Misc, "MotionMaster.MoveAlongSplineChain: non-creature {0} tried to walk along DB spline chain. Ignoring.", _owner.GetGUID().ToString());

				return;
			}

			List<SplineChainLink> chain = Global.ScriptMgr.GetSplineChain(owner, (byte)dbChainId);

			if (chain.Empty())
			{
				Log.outError(LogFilter.Misc, "MotionMaster.MoveAlongSplineChain: creature with entry {0} tried to walk along non-existing spline chain with DB id {1}.", owner.GetEntry(), dbChainId);

				return;
			}

			MoveAlongSplineChain(pointId, chain, walk);
		}

		private void MoveAlongSplineChain(uint pointId, List<SplineChainLink> chain, bool walk)
		{
			Add(new SplineChainMovementGenerator(pointId, chain, walk));
		}

		private void ResumeSplineChain(SplineChainResumeInfo info)
		{
			if (info.Empty())
			{
				Log.outError(LogFilter.Movement, "MotionMaster.ResumeSplineChain: unit with entry {0} tried to resume a spline chain from empty info.", _owner.GetEntry());

				return;
			}

			Add(new SplineChainMovementGenerator(info));
		}

		public void MoveFall(uint id = 0)
		{
			// Use larger distance for vmap height search than in most other cases
			float tz = _owner.GetMapHeight(_owner.GetPositionX(), _owner.GetPositionY(), _owner.GetPositionZ(), true, MapConst.MaxFallDistance);

			if (tz <= MapConst.InvalidHeight)
				return;

			// Abort too if the ground is very near
			if (Math.Abs(_owner.GetPositionZ() - tz) < 0.1f)
				return;

			// rooted units don't move (also setting falling+root flag causes client freezes)
			if (_owner.HasUnitState(UnitState.Root | UnitState.Stunned))
				return;

			_owner.SetFall(true);

			// Don't run spline movement for players
			if (_owner.IsTypeId(TypeId.Player))
			{
				_owner.ToPlayer().SetFallInformation(0, _owner.GetPositionZ());

				return;
			}

			var initializer = (MoveSplineInit init) =>
			                  {
				                  init.MoveTo(_owner.GetPositionX(), _owner.GetPositionY(), tz + _owner.GetHoverOffset(), false);
				                  init.SetFall();
			                  };

			GenericMovementGenerator movement = new(initializer, MovementGeneratorType.Effect, id);
			movement.Priority = MovementGeneratorPriority.Highest;
			Add(movement);
		}

		public void MoveSeekAssistance(float x, float y, float z)
		{
			Creature creature = _owner.ToCreature();

			if (creature != null)
			{
				Log.outDebug(LogFilter.Movement, $"MotionMaster::MoveSeekAssistance: '{creature.GetGUID()}', seeks assistance (X: {x}, Y: {y}, Z: {z})");
				creature.AttackStop();
				creature.CastStop();
				creature.DoNotReacquireSpellFocusTarget();
				creature.SetReactState(ReactStates.Passive);
				Add(new AssistanceMovementGenerator(EventId.AssistMove, x, y, z));
			}
			else
			{
				Log.outError(LogFilter.Server, $"MotionMaster::MoveSeekAssistance: {_owner.GetGUID()}, attempted to seek assistance");
			}
		}

		public void MoveSeekAssistanceDistract(uint time)
		{
			if (_owner.IsCreature())
				Add(new AssistanceDistractMovementGenerator(time, _owner.GetOrientation()));
			else
				Log.outError(LogFilter.Server, $"MotionMaster::MoveSeekAssistanceDistract: {_owner.GetGUID()} attempted to call distract after assistance");
		}

		public void MoveTaxiFlight(uint path, uint pathnode)
		{
			if (_owner.IsTypeId(TypeId.Player))
			{
				if (path < CliDB.TaxiPathNodesByPath.Count)
				{
					Log.outDebug(LogFilter.Server, $"MotionMaster::MoveTaxiFlight: {_owner.GetGUID()} taxi to Path Id: {path} (node {pathnode})");

					// Only one FLIGHT_MOTION_TYPE is allowed
					bool hasExisting = HasMovementGenerator(gen => gen.GetMovementGeneratorType() == MovementGeneratorType.Flight);
					Cypher.Assert(!hasExisting, "Duplicate flight path movement generator");

					FlightPathMovementGenerator movement = new();
					movement.LoadPath(_owner.ToPlayer());
					Add(movement);
				}
				else
				{
					Log.outError(LogFilter.Movement, $"MotionMaster::MoveTaxiFlight: '{_owner.GetGUID()}', attempted taxi to non-existing path Id: {path} (node: {pathnode})");
				}
			}
			else
			{
				Log.outError(LogFilter.Movement, $"MotionMaster::MoveTaxiFlight: '{_owner.GetGUID()}', attempted taxi to path Id: {path} (node: {pathnode})");
			}
		}

		public void MoveDistract(uint timer, float orientation)
		{
			/*
			if (_slot[(int)MovementSlot.Controlled] != null)
			    return;
			*/

			Add(new DistractMovementGenerator(timer, orientation));
		}

		public void MovePath(uint pathId, bool repeatable)
		{
			if (pathId == 0)
				return;

			Add(new WaypointMovementGenerator(pathId, repeatable), MovementSlot.Default);
		}

		public void MovePath(WaypointPath path, bool repeatable)
		{
			Add(new WaypointMovementGenerator(path, repeatable), MovementSlot.Default);
		}

		public void MoveRotate(uint id, uint time, RotateDirection direction)
		{
			if (time == 0)
				return;

			Add(new RotateMovementGenerator(id, time, direction));
		}

		public void MoveFormation(Unit leader, float range, float angle, uint point1, uint point2)
		{
			if (_owner.GetTypeId() == TypeId.Unit &&
			    leader != null)
				Add(new FormationMovementGenerator(leader, range, angle, point1, point2), MovementSlot.Default);
		}

		public void LaunchMoveSpline(Action<MoveSplineInit> initializer, uint id = 0, MovementGeneratorPriority priority = MovementGeneratorPriority.Normal, MovementGeneratorType type = MovementGeneratorType.Effect)
		{
			if (IsInvalidMovementGeneratorType(type))
			{
				Log.outDebug(LogFilter.Movement, $"MotionMaster::LaunchMoveSpline: '{_owner.GetGUID()}', tried to launch a spline with an invalid MovementGeneratorType: {type} (Id: {id}, Priority: {priority})");

				return;
			}

			GenericMovementGenerator movement = new(initializer, type, id);
			movement.Priority = priority;
			Add(movement);
		}

		private void ResolveDelayedActions()
		{
			while (_delayedActions.Count != 0)
			{
				_delayedActions.Peek().Resolve();
				_delayedActions.Dequeue();
			}
		}

		private void Remove(MovementGenerator movement, bool active, bool movementInform)
		{
			_generators.Remove(movement);
			Delete(movement, active, movementInform);
		}

		private void Pop(bool active, bool movementInform)
		{
			if (!_generators.Empty())
				Remove(_generators.FirstOrDefault(), active, movementInform);
		}

		private void DirectInitialize()
		{
			// Clear ALL movement generators (including default)
			DirectClearDefault();
			DirectClear();
			InitializeDefault();
		}

		private void DirectClear()
		{
			// First delete Top
			if (!_generators.Empty())
				Pop(true, false);

			// Then the rest
			while (!_generators.Empty())
				Pop(false, false);

			// Make sure the storage is empty
			ClearBaseUnitStates();
		}

		private void DirectClearDefault()
		{
			if (_defaultGenerator != null)
				DeleteDefault(_generators.Empty(), false);
		}

		private void DirectClear(Func<MovementGenerator, bool> filter)
		{
			if (_generators.Empty())
				return;

			MovementGenerator top = GetCurrentMovementGenerator();

			foreach (var movement in _generators.ToList())
				if (filter(movement))
				{
					_generators.Remove(movement);
					Delete(movement, movement == top, false);
				}
		}

		private void DirectAdd(MovementGenerator movement, MovementSlot slot = MovementSlot.Active)
		{
			/*
			IMovementGenerator curr = _slot[(int)Slot];
			if (curr != null)
			{
			    _slot[(int)Slot] = null; // in case a new one is generated in this Slot during directdelete
			    if (_top == (int)Slot && Convert.ToBoolean(_cleanFlag & MotionMasterCleanFlag.Update))
			        DelayedDelete(curr);
			    else
			        DirectDelete(curr);
			}
			else if (_top < (int)Slot)
			{
			    _top = (int)Slot;
			}

			_slot[(int)Slot] = m;
			if (_top > (int)Slot)
			    _initialize[(int)Slot] = true;
			else
			{
			    _initialize[(int)Slot] = false;
			    m.Initialize(_owner);
			}
			*/

			/*
 * NOTE: This mimics old behaviour: only one MOTION_SLOT_IDLE, MOTION_SLOT_ACTIVE, MOTION_SLOT_CONTROLLED
 * On future changes support for multiple will be added
 */
			switch (slot)
			{
				case MovementSlot.Default:
					if (_defaultGenerator != null)
						_defaultGenerator.Finalize(_owner, _generators.Empty(), false);

					_defaultGenerator = movement;

					if (IsStatic(movement))
						AddFlag(MotionMasterFlags.StaticInitializationPending);

					break;
				case MovementSlot.Active:
					if (!_generators.Empty())
					{
						if (movement.Priority >= _generators.FirstOrDefault().Priority)
						{
							var itr = _generators.FirstOrDefault();

							if (movement.Priority == itr.Priority)
								Remove(itr, true, false);
							else
								itr.Deactivate(_owner);
						}
						else
						{
							var pointer = _generators.FirstOrDefault(a => a.Priority == movement.Priority);

							if (pointer != null)
								Remove(pointer, false, false);
						}
					}
					else
					{
						_defaultGenerator.Deactivate(_owner);
					}

					_generators.Add(movement);
					AddBaseUnitState(movement);

					break;
				default:
					break;
			}
		}

		private void Delete(MovementGenerator movement, bool active, bool movementInform)
		{
			movement.Finalize(_owner, active, movementInform);
			ClearBaseUnitState(movement);
		}

		private void DeleteDefault(bool active, bool movementInform)
		{
			_defaultGenerator.Finalize(_owner, active, movementInform);
			_defaultGenerator = GetIdleMovementGenerator();
			AddFlag(MotionMasterFlags.StaticInitializationPending);
		}

		private void AddBaseUnitState(MovementGenerator movement)
		{
			if (movement == null ||
			    movement.BaseUnitState == 0)
				return;

			_baseUnitStatesMap.Add((uint)movement.BaseUnitState, movement);
			_owner.AddUnitState(movement.BaseUnitState);
		}

		private void ClearBaseUnitState(MovementGenerator movement)
		{
			if (movement == null ||
			    movement.BaseUnitState == 0)
				return;

			_baseUnitStatesMap.Remove((uint)movement.BaseUnitState, movement);

			if (!_baseUnitStatesMap.ContainsKey((uint)movement.BaseUnitState))
				_owner.ClearUnitState(movement.BaseUnitState);
		}

		private void ClearBaseUnitStates()
		{
			uint unitState = 0;

			foreach (var itr in _baseUnitStatesMap)
				unitState |= itr.Key;

			_owner.ClearUnitState((UnitState)unitState);
			_baseUnitStatesMap.Clear();
		}

		private void AddFlag(MotionMasterFlags flag)
		{
			_flags |= flag;
		}

		private bool HasFlag(MotionMasterFlags flag)
		{
			return (_flags & flag) != 0;
		}

		private void RemoveFlag(MotionMasterFlags flag)
		{
			_flags &= ~flag;
		}

		public static MovementGenerator GetIdleMovementGenerator()
		{
			return staticIdleMovement;
		}

		public static bool IsStatic(MovementGenerator movement)
		{
			return (movement == GetIdleMovementGenerator());
		}

		public static bool IsInvalidMovementGeneratorType(MovementGeneratorType type)
		{
			return type == MovementGeneratorType.MaxDB || type >= MovementGeneratorType.Max;
		}

		public static bool IsInvalidMovementSlot(MovementSlot slot)
		{
			return slot >= MovementSlot.Max;
		}
	}

	public class JumpArrivalCastArgs
	{
		public uint SpellId;
		public ObjectGuid Target;
	}

	public class JumpChargeParams
	{
		public float JumpGravity;
		public uint? ParabolicCurveId;
		public uint? ProgressCurveId;
		public float Speed;

		public uint? SpellVisualId;

		public bool TreatSpeedAsMoveTimeSeconds;
	}

	public struct ChaseRange
	{
		// this contains info that informs how we should path!
		public float MinRange;     // we have to move if we are within this range...    (min. attack range)
		public float MinTolerance; // ...and if we are, we will move this far away
		public float MaxRange;     // we have to move if we are outside this range...   (max. attack range)
		public float MaxTolerance; // ...and if we are, we will move into this range

		public ChaseRange(float range)
		{
			MinRange     = range > SharedConst.ContactDistance ? 0 : range - SharedConst.ContactDistance;
			MinTolerance = range;
			MaxRange     = range + SharedConst.ContactDistance;
			MaxTolerance = range;
		}

		public ChaseRange(float min, float max)
		{
			MinRange     = min;
			MinTolerance = Math.Min(min + SharedConst.ContactDistance, (min + max) / 2);
			MaxRange     = max;
			MaxTolerance = Math.Max(max - SharedConst.ContactDistance, MinTolerance);
		}

		public ChaseRange(float min, float tMin, float tMax, float max)
		{
			MinRange     = min;
			MinTolerance = tMin;
			MaxRange     = max;
			MaxTolerance = tMax;
		}
	}

	public struct ChaseAngle
	{
		public float RelativeAngle; // we want to be at this angle relative to the target (0 = front, _PI = back)
		public float Tolerance;     // but we'll tolerate anything within +- this much

		public ChaseAngle(float angle, float tol = MathFunctions.PiOver4)
		{
			RelativeAngle = Position.NormalizeOrientation(angle);
			Tolerance     = tol;
		}

		public float UpperBound()
		{
			return Position.NormalizeOrientation(RelativeAngle + Tolerance);
		}

		public float LowerBound()
		{
			return Position.NormalizeOrientation(RelativeAngle - Tolerance);
		}

		public bool IsAngleOkay(float relAngle)
		{
			float diff = Math.Abs(relAngle - RelativeAngle);

			return (Math.Min(diff, (2 * MathF.PI) - diff) <= Tolerance);
		}
	}
}