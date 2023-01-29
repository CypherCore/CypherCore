// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;

namespace Game.BattleGrounds.Zones.StrandofAncients
{
    internal struct SAMiscConst
    {
        public static uint[] NpcEntries =
        {
            SACreatureIds.ANTI_PERSONNAL_CANNON, SACreatureIds.ANTI_PERSONNAL_CANNON, SACreatureIds.ANTI_PERSONNAL_CANNON, SACreatureIds.ANTI_PERSONNAL_CANNON, SACreatureIds.ANTI_PERSONNAL_CANNON, SACreatureIds.ANTI_PERSONNAL_CANNON, SACreatureIds.ANTI_PERSONNAL_CANNON, SACreatureIds.ANTI_PERSONNAL_CANNON, SACreatureIds.ANTI_PERSONNAL_CANNON, SACreatureIds.ANTI_PERSONNAL_CANNON,
			// 4 beach demolishers
			SACreatureIds.DEMOLISHER, SACreatureIds.DEMOLISHER, SACreatureIds.DEMOLISHER, SACreatureIds.DEMOLISHER,
			// 4 factory demolishers
			SACreatureIds.DEMOLISHER, SACreatureIds.DEMOLISHER, SACreatureIds.DEMOLISHER, SACreatureIds.DEMOLISHER,
			// Used Demolisher Salesman
			SACreatureIds.RIGGER_SPARKLIGHT, SACreatureIds.GORGRIL_RIGSPARK,
			// Kanrethad
			SACreatureIds.KANRETHAD
        };

        public static Position[] NpcSpawnlocs =
        {
			// Cannons
			new(1436.429f, 110.05f, 41.407f, 5.4f), new(1404.9023f, 84.758f, 41.183f, 5.46f), new(1068.693f, -86.951f, 93.81f, 0.02f), new(1068.83f, -127.56f, 96.45f, 0.0912f), new(1422.115f, -196.433f, 42.1825f, 1.0222f), new(1454.887f, -220.454f, 41.956f, 0.9627f), new(1232.345f, -187.517f, 66.945f, 0.45f), new(1249.634f, -224.189f, 66.72f, 0.635f), new(1236.213f, 92.287f, 64.965f, 5.751f), new(1215.11f, 57.772f, 64.739f, 5.78f),
			// Demolishers
			new(1611.597656f, -117.270073f, 8.719355f, 2.513274f), new(1575.562500f, -158.421875f, 5.024450f, 2.129302f), new(1618.047729f, 61.424641f, 7.248210f, 3.979351f), new(1575.103149f, 98.873344f, 2.830360f, 3.752458f),
			// Demolishers 2
			new(1371.055786f, -317.071136f, 35.007359f, 1.947460f), new(1424.034912f, -260.195190f, 31.084425f, 2.820013f), new(1353.139893f, 223.745438f, 35.265411f, 4.343684f), new(1404.809570f, 197.027237f, 32.046032f, 3.605401f),
			// Npcs
			new(1348.644165f, -298.786469f, 31.080130f, 1.710423f), new(1358.191040f, 195.527786f, 31.018187f, 4.171337f), new(841.921f, -134.194f, 196.838f, 6.23082f)
        };

        public static Position[] ObjSpawnlocs =
        {
            new(1411.57f, 108.163f, 28.692f, 5.441f), new(1055.452f, -108.1f, 82.134f, 0.034f), new(1431.3413f, -219.437f, 30.893f, 0.9736f), new(1227.667f, -212.555f, 55.372f, 0.5023f), new(1214.681f, 81.21f, 53.413f, 5.745f), new(878.555f, -108.2f, 117.845f, 0.0f), new(836.5f, -108.8f, 120.219f, 0.0f),
			// Portal
			new(1468.380005f, -225.798996f, 30.896200f, 0.0f), //blue
			new(1394.270020f, 72.551399f, 31.054300f, 0.0f),   //green
			new(1065.260010f, -89.79501f, 81.073402f, 0.0f),   //yellow
			new(1216.069946f, 47.904301f, 54.278198f, 0.0f),   //purple
			new(1255.569946f, -233.548996f, 56.43699f, 0.0f),  //red
			// Ships
			new(2679.696777f, -826.891235f, 3.712860f, 5.78367f), //rot2 1 rot3 0.0002f
			new(2574.003662f, 981.261475f, 2.603424f, 0.807696f),
			// Sigils
			new(1414.054f, 106.72f, 41.442f, 5.441f), new(1060.63f, -107.8f, 94.7f, 0.034f), new(1433.383f, -216.4f, 43.642f, 0.9736f), new(1230.75f, -210.724f, 67.611f, 0.5023f), new(1217.8f, 79.532f, 66.58f, 5.745f),
			// Flagpoles
			new(1215.114258f, -65.711861f, 70.084267f, -3.124123f), new(1338.863892f, -153.336533f, 30.895121f, -2.530723f), new(1309.124268f, 9.410645f, 30.893402f, -1.623156f),
			// Flags
			new(1215.108032f, -65.715767f, 70.084267f, -3.124123f), new(1338.859253f, -153.327316f, 30.895077f, -2.530723f), new(1309.192017f, 9.416233f, 30.893402f, 1.518436f),
			// Bombs
			new(1333.45f, 211.354f, 31.0538f, 5.03666f), new(1334.29f, 209.582f, 31.0532f, 1.28088f), new(1332.72f, 210.049f, 31.0532f, 1.28088f), new(1334.28f, 210.78f, 31.0538f, 3.85856f), new(1332.64f, 211.39f, 31.0532f, 1.29266f), new(1371.41f, 194.028f, 31.5107f, 0.753095f), new(1372.39f, 194.951f, 31.4679f, 0.753095f), new(1371.58f, 196.942f, 30.9349f, 1.01777f), new(1370.43f, 196.614f, 30.9349f, 0.957299f), new(1369.46f, 196.877f, 30.9351f, 2.45348f), new(1370.35f, 197.361f, 30.9349f, 1.08689f), new(1369.47f, 197.941f, 30.9349f, 0.984787f), new(1592.49f, 47.5969f, 7.52271f, 4.63218f), new(1593.91f, 47.8036f, 7.65856f, 4.63218f), new(1593.13f, 46.8106f, 7.54073f, 4.63218f), new(1589.22f, 36.3616f, 7.45975f, 4.64396f), new(1588.24f, 35.5842f, 7.55613f, 4.79564f), new(1588.14f, 36.7611f, 7.49675f, 4.79564f), new(1595.74f, 35.5278f, 7.46602f, 4.90246f), new(1596, 36.6475f, 7.47991f, 4.90246f), new(1597.03f, 36.2356f, 7.48631f, 4.90246f), new(1597.93f, 37.1214f, 7.51725f, 4.90246f), new(1598.16f, 35.888f, 7.50018f, 4.90246f), new(1579.6f, -98.0917f, 8.48478f, 1.37996f), new(1581.2f, -98.401f, 8.47483f, 1.37996f), new(1580.38f, -98.9556f, 8.4772f, 1.38781f), new(1585.68f, -104.966f, 8.88551f, 0.493246f), new(1586.15f, -106.033f, 9.10616f, 0.493246f), new(1584.88f, -105.394f, 8.82985f, 0.493246f), new(1581.87f, -100.899f, 8.46164f, 0.929142f), new(1581.48f, -99.4657f, 8.46926f, 0.929142f), new(1583.2f, -91.2291f, 8.49227f, 1.40038f), new(1581.94f, -91.0119f, 8.49977f, 1.40038f), new(1582.33f, -91.951f, 8.49353f, 1.1844f), new(1342.06f, -304.049f, 30.9532f, 5.59507f), new(1340.96f, -304.536f, 30.9458f, 1.28323f), new(1341.22f, -303.316f, 30.9413f, 0.486051f), new(1342.22f, -302.939f, 30.986f, 4.87643f), new(1382.16f, -287.466f, 32.3063f, 4.80968f), new(1381, -287.58f, 32.2805f, 4.80968f), new(1381.55f, -286.536f, 32.3929f, 2.84225f), new(1382.75f, -286.354f, 32.4099f, 1.00442f), new(1379.92f, -287.34f, 32.2872f, 3.81615f), new(1100.52f, -2.41391f, 70.2984f, 0.131054f), new(1099.35f, -2.13851f, 70.3375f, 4.4586f), new(1099.59f, -1.00329f, 70.238f, 2.49903f), new(1097.79f, 0.571316f, 70.159f, 4.00307f), new(1098.74f, -7.23252f, 70.7972f, 4.1523f), new(1098.46f, -5.91443f, 70.6715f, 4.1523f), new(1097.53f, -7.39704f, 70.7959f, 4.1523f), new(1097.32f, -6.64233f, 70.7424f, 4.1523f), new(1096.45f, -5.96664f, 70.7242f, 4.1523f), new(971.725f, 0.496763f, 86.8467f, 2.09233f), new(973.589f, 0.119518f, 86.7985f, 3.17225f), new(972.524f, 1.25333f, 86.8351f, 5.28497f), new(971.993f, 2.05668f, 86.8584f, 5.28497f), new(973.635f, 2.11805f, 86.8197f, 2.36722f), new(974.791f, 1.74679f, 86.7942f, 1.5936f), new(974.771f, 3.0445f, 86.8125f, 0.647199f), new(979.554f, 3.6037f, 86.7923f, 1.69178f), new(979.758f, 2.57519f, 86.7748f, 1.76639f), new(980.769f, 3.48904f, 86.7939f, 1.76639f), new(979.122f, 2.87109f, 86.7794f, 1.76639f), new(986.167f, 4.85363f, 86.8439f, 1.5779f), new(986.176f, 3.50367f, 86.8217f, 1.5779f), new(987.33f, 4.67389f, 86.8486f, 1.5779f), new(985.23f, 4.65898f, 86.8368f, 1.5779f), new(984.556f, 3.54097f, 86.8137f, 1.5779f)
        };

        public static uint[] ObjEntries =
        {
            190722, 190727, 190724, 190726, 190723, 192549, 192834, 192819, 192819, 192819, 192819, 192819, 0, // Boat
			0,                                                                                                 // Boat
			192687, 192685, 192689, 192690, 192691, 191311, 191311, 191311, 191310, 191306, 191308, 190753
        };

        public static uint[] Factions =
        {
            1732, 1735
        };

        public static uint[] GYEntries =
        {
            1350, 1349, 1347, 1346, 1348
        };

        public static float[] GYOrientation =
        {
            6.202f, 1.926f, // right capturable GY
			3.917f,         // left capturable GY
			3.104f,         // center, capturable
			6.148f          // defender last GY
		};

        public static SAGateInfo[] Gates =
        {
            new(SAObjectTypes.GREEN_GATE, SAGameObjectIds.GATE_OF_THE_GREEN_EMERALD, SAWorldStateIds.GREEN_GATE, SATextIds.GREEN_GATE_UNDER_ATTACK, SATextIds.GREEN_GATE_DESTROYED), new(SAObjectTypes.YELLOW_GATE, SAGameObjectIds.GATE_OF_THE_YELLOW_MOON, SAWorldStateIds.YELLOW_GATE, SATextIds.YELLOW_GATE_UNDER_ATTACK, SATextIds.YELLOW_GATE_DESTROYED), new(SAObjectTypes.BLUE_GATE, SAGameObjectIds.GATE_OF_THE_BLUE_SAPPHIRE, SAWorldStateIds.BLUE_GATE, SATextIds.BLUE_GATE_UNDER_ATTACK, SATextIds.BLUE_GATE_DESTROYED), new(SAObjectTypes.RED_GATE, SAGameObjectIds.GATE_OF_THE_RED_SUN, SAWorldStateIds.RED_GATE, SATextIds.RED_GATE_UNDER_ATTACK, SATextIds.RED_GATE_DESTROYED), new(SAObjectTypes.PURPLE_GATE, SAGameObjectIds.GATE_OF_THE_PURPLE_AMETHYST, SAWorldStateIds.PURPLE_GATE, SATextIds.PURPLE_GATE_UNDER_ATTACK, SATextIds.PURPLE_GATE_DESTROYED), new(SAObjectTypes.ANCIENT_GATE, SAGameObjectIds.CHAMBER_OF_ANCIENT_RELICS, SAWorldStateIds.ANCIENT_GATE, SATextIds.ANCIENT_GATE_UNDER_ATTACK, SATextIds.ANCIENT_GATE_DESTROYED)
        };
    }

}