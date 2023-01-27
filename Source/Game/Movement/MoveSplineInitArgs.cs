// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.DataStorage;
using Game.Entities;

namespace Game.Movement
{
	public class MoveSplineInitArgs
	{
		public AnimTierTransition animTier;
		public FacingInfo facing = new();
		public MoveSplineFlag flags = new();
		public bool HasVelocity;
		public float initialOrientation;
		public float parabolic_amplitude;

		public List<Vector3> path = new();
		public int path_Idx_offset;
		public SpellEffectExtraData spellEffectExtra;
		public uint splineId;
		public float time_perc;
		public bool TransformForTransport;
		public float velocity;
		public float vertical_acceleration;
		public bool walk;

		public MoveSplineInitArgs(int path_capacity = 16)
		{
			path_Idx_offset       = 0;
			velocity              = 0.0f;
			parabolic_amplitude   = 0.0f;
			time_perc             = 0.0f;
			splineId              = 0;
			initialOrientation    = 0.0f;
			HasVelocity           = false;
			TransformForTransport = true;
		}

		// Returns true to show that the arguments were configured correctly and MoveSpline initialization will succeed.
		public bool Validate(Unit unit)
		{
			bool CHECK(bool exp, bool verbose)
			{
				if (!exp)
				{
					if (unit)
						Log.outError(LogFilter.Movement, $"MoveSplineInitArgs::Validate: expression '{exp}' failed for {(verbose ? unit.GetDebugInfo() : unit.GetGUID().ToString())}");
					else
						Log.outError(LogFilter.Movement, $"MoveSplineInitArgs::Validate: expression '{exp}' failed for cyclic spline continuation");

					return false;
				}

				return true;
			}

			if (!CHECK(path.Count > 1, true))
				return false;

			if (!CHECK(velocity >= 0.01f, true))
				return false;

			if (!CHECK(time_perc >= 0.0f && time_perc <= 1.0f, true))
				return false;

			if (!CHECK(_checkPathLengths(), false))
				return false;

			if (spellEffectExtra != null)
			{
				if (!CHECK(spellEffectExtra.ProgressCurveId == 0 || CliDB.CurveStorage.ContainsKey(spellEffectExtra.ProgressCurveId), false))
					return false;

				if (!CHECK(spellEffectExtra.ParabolicCurveId == 0 || CliDB.CurveStorage.ContainsKey(spellEffectExtra.ParabolicCurveId), false))
					return false;

				if (!CHECK(spellEffectExtra.ProgressCurveId == 0 || CliDB.CurveStorage.ContainsKey(spellEffectExtra.ProgressCurveId), true))
					return false;

				if (!CHECK(spellEffectExtra.ParabolicCurveId == 0 || CliDB.CurveStorage.ContainsKey(spellEffectExtra.ParabolicCurveId), true))
					return false;
			}

			return true;
		}

		private bool _checkPathLengths()
		{
			if (path.Count > 2 ||
			    facing.type == MonsterMoveType.Normal)
				for (int i = 0; i < path.Count - 1; ++i)
					if ((path[i + 1] - path[i]).Length() < 0.1f)
						return false;

			return true;
		}
	}

	public class SpellEffectExtraData
	{
		public uint ParabolicCurveId;
		public uint ProgressCurveId;
		public uint SpellVisualId;
		public ObjectGuid Target;
	}

	public class AnimTierTransition
	{
		public byte AnimTier;
		public uint TierTransitionId;
	}
}