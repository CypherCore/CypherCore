// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System.Collections.Generic;

namespace Game.Entities
{
    public class Account : BaseEntity
    {
        HousingStorageData m_housingStorageData = new();

        WorldSession m_session;
        string m_name;

        public Account(WorldSession session, ObjectGuid guid, string name)
        {
            m_session = session;
            m_name = name;

            _Create(guid);

            EntityFragments.Add(EntityFragment.FHousingStorage_C, false, m_housingStorageData);

            // Default value
            SetUpdateFieldValue(m_values.ModifyValue(m_housingStorageData).ModifyValue(m_housingStorageData.DecorMaxOwnedCount), 5000u);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_housingStorageData);
            base.ClearUpdateMask(remove);
        }

        public string GetNameForLocaleIdx(uint locale)
        {
            return m_name;
        }

        public override void BuildUpdate(Dictionary<Player, UpdateData> data_map)
        {
            BuildUpdateChangesMask();

            Player owner = m_session.GetPlayer();
            if (owner != null)
                BuildFieldsUpdate(owner, data_map);

            ClearUpdateMask(false);
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nName: {m_name}";
        }

        public override UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
        {
            if (target.m_playerData.BnetAccount == GetGUID())
                return UpdateFieldFlag.Owner;

            return UpdateFieldFlag.None;
        }

        public override bool AddToObjectUpdate()
        {
            Player owner = m_session.GetPlayer();
            if (owner != null && owner.IsInWorld)
            {
                owner.GetMap().AddUpdateObject(this);
                return true;
            }

            return false;
        }

        public override void RemoveFromObjectUpdate()
        {
            Player owner = m_session.GetPlayer();
            if (owner != null && owner.IsInWorld)
                owner.GetMap().RemoveUpdateObject(this);
        }
    }
}
