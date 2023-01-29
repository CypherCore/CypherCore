// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class ActionButton
    {
        public ActionButtonUpdateState UState;

        public ActionButton()
        {
            PackedData = 0;
            UState = ActionButtonUpdateState.New;
        }

        public ulong PackedData { get; set; }

        public ActionButtonType GetButtonType()
        {
            return (ActionButtonType)((PackedData & 0xFF00000000000000) >> 56);
        }

        public ulong GetAction()
        {
            return (PackedData & 0x00FFFFFFFFFFFFFF);
        }

        public void SetActionAndType(ulong action, ActionButtonType type)
        {
            ulong newData = action | ((ulong)type << 56);

            if (newData != PackedData ||
                UState == ActionButtonUpdateState.Deleted)
            {
                PackedData = newData;

                if (UState != ActionButtonUpdateState.New)
                    UState = ActionButtonUpdateState.Changed;
            }
        }
    }
}