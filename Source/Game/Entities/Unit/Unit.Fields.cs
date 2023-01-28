// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Collections;
using Framework.Constants;
using Framework.Util;
using Game.AI;
using Game.Combat;
using Game.DataStorage;
using Game.Movement;
using Game.Networking.Packets;
using Game.Spells;

namespace Game.Entities
{
	public partial class Unit
	{
		private ushort _aiAnimKitId;
		private bool _aiLocked;
		private MultiMap<uint, AuraApplication> _appliedAuras = new();
		private List<AreaTrigger> _areaTrigger = new();
		protected uint[] _attackTimer = new uint[(int)WeaponAttackType.Max];
		protected float[][] _auraFlatModifiersGroup = new float[(int)UnitMods.End][];
		protected float[][] _auraPctModifiersGroup = new float[(int)UnitMods.End][];
		private MultiMap<AuraStateType, AuraApplication> _auraStateAuras = new(); // Used for improve performance of aura State checks on aura apply/remove

		private uint[] _baseAttackSpeed = new uint[(int)WeaponAttackType.Max];
		public bool _canDualWield;
		private Unit _charmed; // Unit that is being charmed BY ME
		private Unit _charmer; // Unit that is charming ME
		private CharmInfo _charmInfo;
		private bool _cleanupDone; // lock made to not add stuff after cleanup before delete

		// Threat+combat management
		private CombatManager _combatManager;

		//Charm
		public List<Unit> _Controlled = new();
		protected bool _ControlledByPlayer;

		//Spells 
		protected Dictionary<CurrentSpellTypes, Spell> _currentSpells = new((int)CurrentSpellTypes.Max);
		protected DeathState _deathState;

		private DiminishingReturn[] _Diminishing = new DiminishingReturn[(int)DiminishingGroup.Max];
		private bool _duringRemoveFromWorld; // lock made to not add stuff after begining removing from world
		protected List<DynamicObject> _dynObj = new();
		private float[] _floatStatNegBuff = new float[(int)Stats.Max];
		private float[] _floatStatPosBuff = new float[(int)Stats.Max];
		private List<AbstractFollower> _followingMe = new();
		protected List<GameObject> _gameObj = new();
		private bool _instantCast;
		private List<AuraApplication> _interruptableAuras = new(); // auras which have interrupt mask applied on unit
		private SpellAuraInterruptFlags _interruptMask;
		private SpellAuraInterruptFlags2 _interruptMask2;
		private bool _isCombatDisallowed;
		private bool _isWalkingBeforeCharm; // Are we walking before we were charmed?
		private ObjectGuid _lastDamagedTargetGuid;

		private uint _lastExtraAttackSpell;
		protected LiquidTypeRecord _lastLiquid;
		private ushort _meleeAnimKitId;
		internal float[] _modAttackSpeedPct = new float[(int)WeaponAttackType.Max];

		//Auras
		private MultiMap<AuraType, AuraEffect> _modAuras = new();
		private ushort _movementAnimKitId;
		public uint _movementCounter; //< Incrementing counter used in movement packets
		private MovementForces _movementForces;
		public ObjectGuid[] _ObjectSlot = new ObjectGuid[4];

		private uint _oldFactionId; // faction before charm
		private MultiMap<uint, Aura> _ownedAuras = new();
		protected Player _playerMovingMe; // only set for direct client control (possess effects, vehicles and similar)

		private bool _playHoverAnim;
		private PositionUpdateInfo _positionUpdateInfo;
		protected int _procDeep;
		private Dictionary<ReactiveType, uint> _reactiveTimer = new();
		private List<Aura> _removedAuras = new();
		private uint _removedAurasCount;
		private List<Aura> _scAuras = new();
		private List<Player> _sharedVision = new();

		//Movement
		protected float[] _speed_rate = new float[(int)UnitMoveType.Max];
		private SpellHistory _spellHistory;
		private MultiMap<uint, uint>[] _spellImmune = new MultiMap<uint, uint>[(int)SpellImmunity.Max];
		private UnitState _state;
		public ObjectGuid[] _SummonSlot = new ObjectGuid[7];
		private ThreatManager _threatManager;
		private uint _transformSpell;

		//General  
		public UnitData _unitData;
		protected Unit _unitMovedByMe; // only ever set for players, and only for direct client control
		private SortedSet<AuraApplication> _visibleAuras = new(new VisibleAuraSlotCompare());
		private SortedSet<AuraApplication> _visibleAurasToUpdate = new(new VisibleAuraSlotCompare());
		protected float[][] _weaponDamage = new float[(int)WeaponAttackType.Max][];

		//Combat
		protected List<Unit> attackerList = new();

		protected Unit attacking;
		private bool canModifyStats;
		protected float[] CreateStats = new float[(int)Stats.Max];
		private Dictionary<ObjectGuid, uint> extraAttacksTargets = new();

		protected UnitAI i_AI;

		//AI
		protected Stack<UnitAI> i_AIs = new();
		private MotionMaster i_motionMaster;
		private TimeTracker splineSyncTimer;
		public MoveSpline MoveSpline { get; set; }

		public float ModMeleeHitChance { get; set; }
		public float ModRangedHitChance { get; set; }
		public float ModSpellHitChance { get; set; }
		public int BaseSpellCritChance { get; set; }
		public uint RegenTimer { get; set; }
		public ObjectGuid LastCharmerGUID { get; set; }
		public UnitTypeMask UnitTypeMask { get; set; }
		public Vehicle _vehicle { get; set; }
		public Vehicle VehicleKit { get; set; }
		public uint LastSanctuaryTime { get; set; }
		public VariableStore VariableStorage { get; } = new();

		private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
		{
			private ObjectFieldData ObjectMask = new();
			private Unit Owner;
			private UnitData UnitMask = new();

			public ValuesUpdateForPlayerWithMaskSender(Unit owner)
			{
				Owner = owner;
			}

			public void Invoke(Player player)
			{
				UpdateData udata = new(Owner.GetMapId());

				Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), UnitMask.GetUpdateMask(), player);

				udata.BuildPacket(out UpdateObject packet);
				player.SendPacket(packet);
			}
		}
	}

	public struct DiminishingReturn
	{
		public DiminishingReturn(uint hitTime, DiminishingLevels hitCount)
		{
			Stack    = 0;
			HitTime  = hitTime;
			HitCount = hitCount;
		}

		public void Clear()
		{
			Stack    = 0;
			HitTime  = 0;
			HitCount = DiminishingLevels.Level1;
		}

		public uint Stack;
		public uint HitTime;
		public DiminishingLevels HitCount;
	}

	public class ProcEventInfo
	{
		private Unit _actionTarget;

		private Unit _actor;
		private DamageInfo _damageInfo;
		private HealInfo _healInfo;
		private ProcFlagsHit _hitMask;
		private Unit _procTarget;
		private Spell _spell;
		private ProcFlagsSpellPhase _spellPhaseMask;
		private ProcFlagsSpellType _spellTypeMask;
		private ProcFlagsInit _typeMask;

		public ProcEventInfo(Unit actor, Unit actionTarget, Unit procTarget, ProcFlagsInit typeMask, ProcFlagsSpellType spellTypeMask,
		                     ProcFlagsSpellPhase spellPhaseMask, ProcFlagsHit hitMask, Spell spell, DamageInfo damageInfo, HealInfo healInfo)
		{
			_actor          = actor;
			_actionTarget   = actionTarget;
			_procTarget     = procTarget;
			_typeMask       = typeMask;
			_spellTypeMask  = spellTypeMask;
			_spellPhaseMask = spellPhaseMask;
			_hitMask        = hitMask;
			_spell          = spell;
			_damageInfo     = damageInfo;
			_healInfo       = healInfo;
		}

		public Unit GetActor()
		{
			return _actor;
		}

		public Unit GetActionTarget()
		{
			return _actionTarget;
		}

		public Unit GetProcTarget()
		{
			return _procTarget;
		}

		public ProcFlagsInit GetTypeMask()
		{
			return _typeMask;
		}

		public ProcFlagsSpellType GetSpellTypeMask()
		{
			return _spellTypeMask;
		}

		public ProcFlagsSpellPhase GetSpellPhaseMask()
		{
			return _spellPhaseMask;
		}

		public ProcFlagsHit GetHitMask()
		{
			return _hitMask;
		}

		public SpellInfo GetSpellInfo()
		{
			if (_spell)
				return _spell.GetSpellInfo();

			if (_damageInfo != null)
				return _damageInfo.GetSpellInfo();

			if (_healInfo != null)
				return _healInfo.GetSpellInfo();

			return null;
		}

		public SpellSchoolMask GetSchoolMask()
		{
			if (_spell)
				return _spell.GetSpellInfo().GetSchoolMask();

			if (_damageInfo != null)
				return _damageInfo.GetSchoolMask();

			if (_healInfo != null)
				return _healInfo.GetSchoolMask();

			return SpellSchoolMask.None;
		}

		public DamageInfo GetDamageInfo()
		{
			return _damageInfo;
		}

		public HealInfo GetHealInfo()
		{
			return _healInfo;
		}

		public Spell GetProcSpell()
		{
			return _spell;
		}
	}

	public class DamageInfo
	{
		private uint _absorb;

		private Unit _attacker;
		private WeaponAttackType _attackType;
		private uint _block;
		private uint _damage;
		private DamageEffectType _damageType;
		private ProcFlagsHit _hitMask;
		private uint _originalDamage;
		private uint _resist;
		private SpellSchoolMask _schoolMask;
		private SpellInfo _spellInfo;
		private Unit _victim;

		public DamageInfo(Unit attacker, Unit victim, uint damage, SpellInfo spellInfo, SpellSchoolMask schoolMask, DamageEffectType damageType, WeaponAttackType attackType)
		{
			_attacker       = attacker;
			_victim         = victim;
			_damage         = damage;
			_originalDamage = damage;
			_spellInfo      = spellInfo;
			_schoolMask     = schoolMask;
			_damageType     = damageType;
			_attackType     = attackType;
		}

		public DamageInfo(CalcDamageInfo dmgInfo)
		{
			_attacker       = dmgInfo.Attacker;
			_victim         = dmgInfo.Target;
			_damage         = dmgInfo.Damage;
			_originalDamage = dmgInfo.Damage;
			_spellInfo      = null;
			_schoolMask     = (SpellSchoolMask)dmgInfo.DamageSchoolMask;
			_damageType     = DamageEffectType.Direct;
			_attackType     = dmgInfo.AttackType;
			_absorb         = dmgInfo.Absorb;
			_resist         = dmgInfo.Resist;
			_block          = dmgInfo.Blocked;

			switch (dmgInfo.TargetState)
			{
				case VictimState.Immune:
					_hitMask |= ProcFlagsHit.Immune;

					break;
				case VictimState.Blocks:
					_hitMask |= ProcFlagsHit.FullBlock;

					break;
			}

			if (dmgInfo.HitInfo.HasAnyFlag(HitInfo.PartialAbsorb | HitInfo.FullAbsorb))
				_hitMask |= ProcFlagsHit.Absorb;

			if (dmgInfo.HitInfo.HasAnyFlag(HitInfo.FullResist))
				_hitMask |= ProcFlagsHit.FullResist;

			if (_block != 0)
				_hitMask |= ProcFlagsHit.Block;

			bool damageNullified = dmgInfo.HitInfo.HasAnyFlag(HitInfo.FullAbsorb | HitInfo.FullResist) || _hitMask.HasAnyFlag(ProcFlagsHit.Immune | ProcFlagsHit.FullBlock);

			switch (dmgInfo.HitOutCome)
			{
				case MeleeHitOutcome.Miss:
					_hitMask |= ProcFlagsHit.Miss;

					break;
				case MeleeHitOutcome.Dodge:
					_hitMask |= ProcFlagsHit.Dodge;

					break;
				case MeleeHitOutcome.Parry:
					_hitMask |= ProcFlagsHit.Parry;

					break;
				case MeleeHitOutcome.Evade:
					_hitMask |= ProcFlagsHit.Evade;

					break;
				case MeleeHitOutcome.Block:
				case MeleeHitOutcome.Crushing:
				case MeleeHitOutcome.Glancing:
				case MeleeHitOutcome.Normal:
					if (!damageNullified)
						_hitMask |= ProcFlagsHit.Normal;

					break;
				case MeleeHitOutcome.Crit:
					if (!damageNullified)
						_hitMask |= ProcFlagsHit.Critical;

					break;
			}
		}

		public DamageInfo(SpellNonMeleeDamage spellNonMeleeDamage, DamageEffectType damageType, WeaponAttackType attackType, ProcFlagsHit hitMask)
		{
			_attacker   = spellNonMeleeDamage.attacker;
			_victim     = spellNonMeleeDamage.target;
			_damage     = spellNonMeleeDamage.damage;
			_spellInfo  = spellNonMeleeDamage.Spell;
			_schoolMask = spellNonMeleeDamage.schoolMask;
			_damageType = damageType;
			_attackType = attackType;
			_absorb     = spellNonMeleeDamage.absorb;
			_resist     = spellNonMeleeDamage.resist;
			_block      = spellNonMeleeDamage.blocked;
			_hitMask    = hitMask;

			if (spellNonMeleeDamage.blocked != 0)
				_hitMask |= ProcFlagsHit.Block;

			if (spellNonMeleeDamage.absorb != 0)
				_hitMask |= ProcFlagsHit.Absorb;
		}

		public void ModifyDamage(int amount)
		{
			amount  =  Math.Max(amount, -((int)GetDamage()));
			_damage += (uint)amount;
		}

		public void AbsorbDamage(uint amount)
		{
			amount   =  Math.Min(amount, GetDamage());
			_absorb  += amount;
			_damage  -= amount;
			_hitMask |= ProcFlagsHit.Absorb;
		}

		public void ResistDamage(uint amount)
		{
			amount  =  Math.Min(amount, GetDamage());
			_resist += amount;
			_damage -= amount;

			if (_damage == 0)
			{
				_hitMask |= ProcFlagsHit.FullResist;
				_hitMask &= ~(ProcFlagsHit.Normal | ProcFlagsHit.Critical);
			}
		}

		private void BlockDamage(uint amount)
		{
			amount   =  Math.Min(amount, GetDamage());
			_block   += amount;
			_damage  -= amount;
			_hitMask |= ProcFlagsHit.Block;

			if (_damage == 0)
			{
				_hitMask |= ProcFlagsHit.FullBlock;
				_hitMask &= ~(ProcFlagsHit.Normal | ProcFlagsHit.Critical);
			}
		}

		public Unit GetAttacker()
		{
			return _attacker;
		}

		public Unit GetVictim()
		{
			return _victim;
		}

		public SpellInfo GetSpellInfo()
		{
			return _spellInfo;
		}

		public SpellSchoolMask GetSchoolMask()
		{
			return _schoolMask;
		}

		private DamageEffectType GetDamageType()
		{
			return _damageType;
		}

		public WeaponAttackType GetAttackType()
		{
			return _attackType;
		}

		public uint GetDamage()
		{
			return _damage;
		}

		public uint GetOriginalDamage()
		{
			return _originalDamage;
		}

		public uint GetAbsorb()
		{
			return _absorb;
		}

		public uint GetResist()
		{
			return _resist;
		}

		private uint GetBlock()
		{
			return _block;
		}

		public ProcFlagsHit GetHitMask()
		{
			return _hitMask;
		}
	}

	public class HealInfo
	{
		private uint _absorb;
		private uint _effectiveHeal;
		private uint _heal;

		private Unit _healer;
		private ProcFlagsHit _hitMask;
		private uint _originalHeal;
		private SpellSchoolMask _schoolMask;
		private SpellInfo _spellInfo;
		private Unit _target;

		public HealInfo(Unit healer, Unit target, uint heal, SpellInfo spellInfo, SpellSchoolMask schoolMask)
		{
			_healer       = healer;
			_target       = target;
			_heal         = heal;
			_originalHeal = heal;
			_spellInfo    = spellInfo;
			_schoolMask   = schoolMask;
		}

		public void AbsorbHeal(uint amount)
		{
			amount         =  Math.Min(amount, GetHeal());
			_absorb        += amount;
			_heal          -= amount;
			amount         =  Math.Min(amount, GetEffectiveHeal());
			_effectiveHeal -= amount;
			_hitMask       |= ProcFlagsHit.Absorb;
		}

		public void SetEffectiveHeal(uint amount)
		{
			_effectiveHeal = amount;
		}

		public Unit GetHealer()
		{
			return _healer;
		}

		public Unit GetTarget()
		{
			return _target;
		}

		public uint GetHeal()
		{
			return _heal;
		}

		public uint GetOriginalHeal()
		{
			return _originalHeal;
		}

		public uint GetEffectiveHeal()
		{
			return _effectiveHeal;
		}

		public uint GetAbsorb()
		{
			return _absorb;
		}

		public SpellInfo GetSpellInfo()
		{
			return _spellInfo;
		}

		public SpellSchoolMask GetSchoolMask()
		{
			return _schoolMask;
		}

		private ProcFlagsHit GetHitMask()
		{
			return _hitMask;
		}
	}

	public class CalcDamageInfo
	{
		public uint Absorb;
		public uint Damage;
		public Unit Attacker { get; set; } // Attacker
		public Unit Target { get; set; }   // Target for damage
		public uint DamageSchoolMask { get; set; }
		public uint OriginalDamage { get; set; }
		public uint Resist { get; set; }
		public uint Blocked { get; set; }
		public HitInfo HitInfo { get; set; }
		public VictimState TargetState { get; set; }

		// Helper
		public WeaponAttackType AttackType { get; set; }
		public ProcFlagsInit ProcAttacker { get; set; }
		public ProcFlagsInit ProcVictim { get; set; }
		public uint CleanDamage { get; set; }           // Used only for rage calculation
		public MeleeHitOutcome HitOutCome { get; set; } // TODO: remove this field (need use TargetState)
	}

	public class SpellNonMeleeDamage
	{
		public uint absorb;
		public Unit attacker;
		public uint blocked;

		public ObjectGuid castId;

		// Used for help
		public uint cleanDamage;
		public uint damage;
		public bool fullBlock;
		public HitInfo HitInfo;
		public uint originalDamage;
		public bool periodicLog;
		public uint preHitHealth;
		public uint resist;
		public SpellSchoolMask schoolMask;
		public SpellInfo Spell;
		public SpellCastVisual SpellVisual;

		public Unit target;

		public SpellNonMeleeDamage(Unit _attacker, Unit _target, SpellInfo _spellInfo, SpellCastVisual spellVisual, SpellSchoolMask _schoolMask, ObjectGuid _castId = default)
		{
			target       = _target;
			attacker     = _attacker;
			Spell        = _spellInfo;
			SpellVisual  = spellVisual;
			schoolMask   = _schoolMask;
			castId       = _castId;
			preHitHealth = (uint)_target.GetHealth();
		}
	}

	public class CleanDamage
	{
		public CleanDamage(uint mitigated, uint absorbed, WeaponAttackType _attackType, MeleeHitOutcome _hitOutCome)
		{
			absorbed_damage  = absorbed;
			mitigated_damage = mitigated;
			attackType       = _attackType;
			hitOutCome       = _hitOutCome;
		}

		public uint absorbed_damage { get; }
		public uint mitigated_damage { get; set; }

		public WeaponAttackType attackType { get; }
		public MeleeHitOutcome hitOutCome { get; }
	}

	public class DispelInfo
	{
		private byte _chargesRemoved;

		private WorldObject _dispeller;
		private uint _dispellerSpell;

		public DispelInfo(WorldObject dispeller, uint dispellerSpellId, byte chargesRemoved)
		{
			_dispeller      = dispeller;
			_dispellerSpell = dispellerSpellId;
			_chargesRemoved = chargesRemoved;
		}

		public WorldObject GetDispeller()
		{
			return _dispeller;
		}

		private uint GetDispellerSpellId()
		{
			return _dispellerSpell;
		}

		public byte GetRemovedCharges()
		{
			return _chargesRemoved;
		}

		public void SetRemovedCharges(byte amount)
		{
			_chargesRemoved = amount;
		}
	}

	public class SpellPeriodicAuraLogInfo
	{
		public uint absorb;

		public AuraEffect auraEff;
		public bool critical;
		public uint damage;
		public float multiplier;
		public uint originalDamage;
		public uint overDamage; // overkill/overheal
		public uint resist;

		public SpellPeriodicAuraLogInfo(AuraEffect _auraEff, uint _damage, uint _originalDamage, uint _overDamage, uint _absorb, uint _resist, float _multiplier, bool _critical)
		{
			auraEff        = _auraEff;
			damage         = _damage;
			originalDamage = _originalDamage;
			overDamage     = _overDamage;
			absorb         = _absorb;
			resist         = _resist;
			multiplier     = _multiplier;
			critical       = _critical;
		}
	}

	internal class VisibleAuraSlotCompare : IComparer<AuraApplication>
	{
		public int Compare(AuraApplication x, AuraApplication y)
		{
			return x.GetSlot().CompareTo(y.GetSlot());
		}
	}

	public class DeclinedName
	{
		public StringArray name = new(SharedConst.MaxDeclinedNameCases);
	}

	internal struct PositionUpdateInfo
	{
		public bool Relocated;
		public bool Turned;

		public void Reset()
		{
			Relocated = false;
			Turned    = false;
		}
	}
}