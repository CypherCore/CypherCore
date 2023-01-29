// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
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
        private readonly MultiMap<uint, AuraApplication> _appliedAuras = new();
        private readonly List<AreaTrigger> _areaTrigger = new();
        protected uint[] AttackTimer = new uint[(int)WeaponAttackType.Max];
        protected float[][] AuraFlatModifiersGroup = new float[(int)UnitMods.End][];
        protected float[][] AuraPctModifiersGroup = new float[(int)UnitMods.End][];
        private readonly MultiMap<AuraStateType, AuraApplication> _auraStateAuras = new(); // Used for improve performance of aura State checks on aura apply/remove

        private readonly uint[] _baseAttackSpeed = new uint[(int)WeaponAttackType.Max];
        public bool CanDualWieldWep { get; set; }
        private Unit _charmed; // Unit that is being charmed BY ME
        private Unit _charmer; // Unit that is charming ME
        private CharmInfo _charmInfo;
        private bool _cleanupDone; // lock made to not add stuff after cleanup before delete

        // Threat+combat management
        private readonly CombatManager _combatManager;

        //Charm
        public List<Unit> Controlled { get; set; } = new();
        protected bool ControlledByPlayer { get; set; }

        //Spells 
        protected Dictionary<CurrentSpellTypes, Spell> CurrentSpells { get; set; } = new((int)CurrentSpellTypes.Max);
        protected DeathState DeathState { get; set; }

        private readonly DiminishingReturn[] _diminishing = new DiminishingReturn[(int)DiminishingGroup.Max];
        private bool _duringRemoveFromWorld; // lock made to not add stuff after begining removing from world
        protected List<DynamicObject> DynObj { get; set; } = new();
        private readonly float[] _floatStatNegBuff = new float[(int)Stats.Max];
        private readonly float[] _floatStatPosBuff = new float[(int)Stats.Max];
        private readonly List<AbstractFollower> _followingMe = new();
        protected List<GameObject> GameObj { get; set; } = new();
        private bool _instantCast;
        private readonly List<AuraApplication> _interruptableAuras = new(); // Auras which have interrupt mask applied on unit
        private SpellAuraInterruptFlags _interruptMask;
        private SpellAuraInterruptFlags2 _interruptMask2;
        private bool _isCombatDisallowed;
        private bool _isWalkingBeforeCharm; // Are we walking before we were charmed?
        private ObjectGuid _lastDamagedTargetGuid;

        private uint _lastExtraAttackSpell;
        protected LiquidTypeRecord _lastLiquid;
        private ushort _meleeAnimKitId;
        internal float[] ModAttackSpeedPct { get; set; } = new float[(int)WeaponAttackType.Max];

        //Auras
        private readonly MultiMap<AuraType, AuraEffect> _modAuras = new();
        private ushort _movementAnimKitId;
        public uint MovementCounter { get; set; } //< Incrementing counter used in movement packets
        private MovementForces _movementForces;
        public ObjectGuid[] ObjectSlot = new ObjectGuid[4];

        private uint _oldFactionId; // faction before charm
        private readonly MultiMap<uint, Aura> _ownedAuras = new();
        protected Player PlayerMovingMe { get; set; } // only set for direct client control (possess effects, vehicles and similar)

        private bool _playHoverAnim;
        private PositionUpdateInfo _positionUpdateInfo;
        protected int ProcDeep { get; set; }
        private readonly Dictionary<ReactiveType, uint> _reactiveTimer = new();
        private readonly List<Aura> _removedAuras = new();
        private uint _removedAurasCount;
        private readonly List<Aura> _scAuras = new();
        private readonly List<Player> _sharedVision = new();

        //Movement
        protected float[] SpeedRate { get; set; } = new float[(int)UnitMoveType.Max];
        private SpellHistory _spellHistory;
        private readonly MultiMap<uint, uint>[] _spellImmune = new MultiMap<uint, uint>[(int)SpellImmunity.Max];
        private UnitState _state;
        public ObjectGuid[] SummonSlot { get; set; } = new ObjectGuid[7];
        private readonly ThreatManager _threatManager;
        private uint _transformSpell;

        //General  
        public UnitData UnitData { get; set; }
        protected Unit UnitMovedByMe { get; set; } // only ever set for players, and only for direct client control
        private readonly SortedSet<AuraApplication> _visibleAuras = new(new VisibleAuraSlotCompare());
        private readonly SortedSet<AuraApplication> _visibleAurasToUpdate = new(new VisibleAuraSlotCompare());
        protected float[][] WeaponDamage { get; set; } = new float[(int)WeaponAttackType.Max][];

        //Combat
        protected List<Unit> AttackerList { get; set; } = new();

        protected Unit Attacking { get; set; }
        private bool _canModifyStats;
        protected float[] CreateStats { get; set; } = new float[(int)Stats.Max];
        private readonly Dictionary<ObjectGuid, uint> _extraAttacksTargets = new();

        protected UnitAI IAi { get; set; }

        //AI
        protected Stack<UnitAI> IAIs { get; set; } = new();
        private readonly MotionMaster _iMotionMaster;
        private readonly TimeTracker _splineSyncTimer;
        public MoveSpline MoveSpline { get; set; }

        public float ModMeleeHitChance { get; set; }
        public float ModRangedHitChance { get; set; }
        public float ModSpellHitChance { get; set; }
        public int BaseSpellCritChance { get; set; }
        public uint RegenTimer { get; set; }
        public ObjectGuid LastCharmerGUID { get; set; }
        public UnitTypeMask UnitTypeMask { get; set; }
        public Vehicle Vehicle { get; set; }
        public Vehicle VehicleKit { get; set; }
        public uint LastSanctuaryTime { get; set; }
        public VariableStore VariableStorage { get; } = new();

        private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            private readonly ObjectFieldData _objectMask = new();
            private readonly Unit _owner;
            private readonly UnitData _unitMask = new();

            public ValuesUpdateForPlayerWithMaskSender(Unit owner)
            {
                _owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(_owner.GetMapId());

                _owner.BuildValuesUpdateForPlayerWithMask(udata, _objectMask.GetUpdateMask(), _unitMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }
    }
}