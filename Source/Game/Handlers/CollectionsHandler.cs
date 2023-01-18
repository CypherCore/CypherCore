// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.CollectionItemSetFavorite)]
        void HandleCollectionItemSetFavorite(CollectionItemSetFavorite collectionItemSetFavorite)
        {
            switch (collectionItemSetFavorite.Type)
            {
                case CollectionType.Toybox:
                    GetCollectionMgr().ToySetFavorite(collectionItemSetFavorite.Id, collectionItemSetFavorite.IsFavorite);
                    break;
                case CollectionType.Appearance:
                    {
                        var pair = GetCollectionMgr().HasItemAppearance(collectionItemSetFavorite.Id);
                        if (!pair.Item1 || pair.Item2)
                            return;

                        GetCollectionMgr().SetAppearanceIsFavorite(collectionItemSetFavorite.Id, collectionItemSetFavorite.IsFavorite);
                        break;
                    }
                case CollectionType.TransmogSet:
                    break;
                default:
                    break;
            }
        }
    }
}
