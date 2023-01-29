// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class UpdateFieldHolder
    {
        private readonly UpdateMask _changesMask = new((int)TypeId.Max);

        public UpdateFieldHolder(WorldObject owner)
        {
        }

        public BaseUpdateData<T> ModifyValue<T>(BaseUpdateData<T> updateData)
        {
            _changesMask.Set(updateData.Bit);

            return updateData;
        }

        public void ClearChangesMask<T>(BaseUpdateData<T> updateData)
        {
            _changesMask.Reset(updateData.Bit);
            updateData.ClearChangesMask();
        }

        public void ClearChangesMask<T, U>(BaseUpdateData<T> updateData, ref UpdateField<U> updateField) where T : new() where U : new()
        {
            _changesMask.Reset(updateData.Bit);

            IHasChangesMask hasChangesMask = (IHasChangesMask)updateField.Value;

            hasChangesMask?.ClearChangesMask();
        }

        public uint GetChangedObjectTypeMask()
        {
            return _changesMask.GetBlock(0);
        }

        public bool HasChanged(TypeId index)
        {
            return _changesMask[(int)index];
        }
    }
}