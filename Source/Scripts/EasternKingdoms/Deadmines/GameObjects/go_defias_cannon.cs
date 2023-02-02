using Framework.Constants;
using Game.AI;
using Game.Entities;
using Game.Maps;
using Game.Scripting;

namespace Scripts.EasternKingdoms.Deadmines.GameObjects
{

    /**
     * explode door and say mobs after Door to attack tank...
     */
    [GameObjectScript(DMGameObjects.GO_DEFIAS_CANNON)]
    public class go_defias_cannon : GameObjectAI
    {
        public go_defias_cannon(GameObject go) : base(go)
        {
        }

        public override bool OnGossipHello(Player player)
        {
            if (me == null || player == null)
            {
                return false;
            }

            InstanceScript instance = me.GetInstanceScript();
            GameObject ironCladDoor = me.FindNearestGameObject(DMGameObjects.GO_IRONCLAD_DOOR, 30.0f);

            if (ironCladDoor != null)
            {
                me.SetGoState(GameObjectState.Active);
                me.PlayDistanceSound(DMSound.SOUND_CANNONFIRE, player);
                ironCladDoor.SetGoState(GameObjectState.Active);
                ironCladDoor.PlayDistanceSound(DMSound.SOUND_DESTROYDOOR, player);

                MoveCreatureInside(me, DMCreatures.NPC_DEFIAS_SHADOWGUARD);
                MoveCreatureInside(me, DMCreatures.NPC_DEFIAS_ENFORCER);
                MoveCreatureInside(me, DMCreatures.NPC_DEFIAS_BLOODWIZARD);
                Creature bunny = me.SummonCreature(DMCreatures.NPC_GENERAL_PURPOSE_BUNNY_JMF, me.GetPositionX(), me.GetPositionY(), me.GetPositionZ());

                if (bunny != null)
                    bunny.GetAI().Talk(0);
            }
            return true;
        }

        public void MoveCreatureInside(GameObject go, uint entry)
        {
            if (go == null || entry <= 0)
                return;

            Creature defias = go.FindNearestCreature(entry, 20.0f);

            if (defias != null)
            {
                defias.SetWalk(false);
                defias.GetMotionMaster().MovePoint(0, -102.7f, -655.9f, defias.GetPositionZ());
            }
        }
    }
}
