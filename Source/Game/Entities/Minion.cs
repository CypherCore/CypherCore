// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;

namespace Game.Entities
{
    public class Minion : TempSummon
    {
        private float _followAngle;

        protected Unit Owner { get; set; }

        public Minion(SummonPropertiesRecord properties, Unit owner, bool isWorldObject)
            : base(properties, owner, isWorldObject)
        {
            Owner = owner;
            Cypher.Assert(Owner);
            UnitTypeMask |= UnitTypeMask.Minion;
            _followAngle = SharedConst.PetFollowAngle;
            /// @todo: Find correct way
            InitCharmInfo();
        }

        public override void InitStats(uint duration)
        {
            base.InitStats(duration);

            SetReactState(ReactStates.Passive);

            SetCreatorGUID(GetOwner().GetGUID());
            SetFaction(GetOwner().GetFaction()); // TODO: Is this correct? Overwrite the use of SummonPropertiesFlags::UseSummonerFaction

            GetOwner().SetMinion(this, true);
        }

        public override void RemoveFromWorld()
        {
            if (!IsInWorld)
                return;

            GetOwner().SetMinion(this, false);
            base.RemoveFromWorld();
        }

        public override void SetDeathState(DeathState s)
        {
            base.SetDeathState(s);

            if (s != DeathState.JustDied ||
                !IsGuardianPet())
                return;

            Unit owner = GetOwner();

            if (owner == null ||
                !owner.IsPlayer() ||
                owner.GetMinionGUID() != GetGUID())
                return;

            foreach (Unit controlled in owner.Controlled)
                if (controlled.GetEntry() == GetEntry() &&
                    controlled.IsAlive())
                {
                    owner.SetMinionGUID(controlled.GetGUID());
                    owner.SetPetGUID(controlled.GetGUID());
                    owner.ToPlayer().CharmSpellInitialize();

                    break;
                }
        }

        public bool IsGuardianPet()
        {
            return IsPet() || (Properties != null && Properties.Control == SummonCategory.Pet);
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nOwner: {(GetOwner() ? GetOwner().GetGUID() : "")}";
        }

        public override Unit GetOwner()
        {
            return Owner;
        }

        public override float GetFollowAngle()
        {
            return _followAngle;
        }

        public void SetFollowAngle(float angle)
        {
            _followAngle = angle;
        }

        // Warlock pets
        public bool IsPetImp()
        {
            return GetEntry() == (uint)PetEntry.Imp;
        }

        public bool IsPetFelhunter()
        {
            return GetEntry() == (uint)PetEntry.FelHunter;
        }

        public bool IsPetVoidwalker()
        {
            return GetEntry() == (uint)PetEntry.VoidWalker;
        }

        public bool IsPetSuccubus()
        {
            return GetEntry() == (uint)PetEntry.Succubus;
        }

        public bool IsPetDoomguard()
        {
            return GetEntry() == (uint)PetEntry.Doomguard;
        }

        public bool IsPetFelguard()
        {
            return GetEntry() == (uint)PetEntry.Felguard;
        }

        // Death Knight pets
        public bool IsPetGhoul()
        {
            return GetEntry() == (uint)PetEntry.Ghoul;
        } // Ghoul may be guardian or pet

        public bool IsPetAbomination()
        {
            return GetEntry() == (uint)PetEntry.Abomination;
        } // Sludge Belcher dk talent

        // Shaman pet
        public bool IsSpiritWolf()
        {
            return GetEntry() == (uint)PetEntry.SpiritWolf;
        } // Spirit wolf from feral spirits
    }
}