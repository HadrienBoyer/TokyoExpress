#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using TokyoExpress.Grid;
using TokyoExpress.Player;

namespace TokyoExpress.GameEditor
{
    public static class TokyoPrefabFactory
    {
        public static GameObject CreateRoadTilePrefab(string name, CellTileType type, Material roadMat, Material markMat)
        {
            string path = $"{TokyoAssetFactory.PREFAB_FOLDER}/Prefab_{name}.prefab";

            GameObject root = new GameObject($"Tile_{name}");

            // Road slab (2m x 0.12m x 2m)
            GameObject slab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            slab.name = "RoadSlab";
            slab.transform.SetParent(root.transform);
            slab.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            slab.transform.localScale = new Vector3(2f, 0.12f, 2f);
            slab.GetComponent<MeshRenderer>().sharedMaterial = roadMat;

            // Markings
            GameObject mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mark.name = "RoadMark";
            mark.transform.SetParent(root.transform);
            mark.transform.localPosition = new Vector3(0f, 0.005f, 0f);
            mark.transform.localScale = (type == CellTileType.StraightHorizontal) ? new Vector3(1.4f, 0.02f, 0.2f) : new Vector3(0.2f, 0.02f, 1.4f);
            mark.GetComponent<MeshRenderer>().sharedMaterial = markMat;
            Object.DestroyImmediate(mark.GetComponent<Collider>());

            // Add Spline Rail Track for vehicle navigation
            var splineTrack = root.AddComponent<RoadSplineTrack>();
            if (type == CellTileType.Crossroad || type == CellTileType.TIntersection)
            {
                var container = TokyoSplineRailBuilder.CreateCrossroadSplines(root);
                splineTrack.SetSplineContainer(container);
            }
            else
            {
                Vector3 start = (type == CellTileType.StraightHorizontal) ? new Vector3(-1f, 0.01f, 0f) : new Vector3(0f, 0.01f, -1f);
                Vector3 end = (type == CellTileType.StraightHorizontal) ? new Vector3(1f, 0.01f, 0f) : new Vector3(0f, 0.01f, 1f);
                var container = TokyoSplineRailBuilder.CreateStraightSpline(root, start, end);
                splineTrack.SetSplineContainer(container);
            }

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return savedPrefab;
        }

        /// <summary>
        /// Creates a rounded/circular curved road tile matching the screenshot style (smooth pop corners)
        /// </summary>
        public static GameObject CreateRoundedTurnRoadPrefab(string name, CellTileType type, Material roadMat, Material markMat, Material groundMat)
        {
            string path = $"{TokyoAssetFactory.PREFAB_FOLDER}/Prefab_{name}.prefab";

            GameObject root = new GameObject($"Tile_{name}");

            // Background ground block (inner curve grass/water fill)
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "GroundFill";
            ground.transform.SetParent(root.transform);
            ground.transform.localPosition = new Vector3(0f, -0.065f, 0f);
            ground.transform.localScale = new Vector3(2f, 0.11f, 2f);
            if (groundMat != null) ground.GetComponent<MeshRenderer>().sharedMaterial = groundMat;
            else ground.GetComponent<MeshRenderer>().sharedMaterial = roadMat;

            // Circular curved road segments (smooth arc approximation using overlapping cylinders/segments)
            // Central curved arch
            GameObject curvedArch = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            curvedArch.name = "CurvedRoadArc";
            curvedArch.transform.SetParent(root.transform);
            curvedArch.transform.localScale = new Vector3(1.85f, 0.07f, 1.85f);
            curvedArch.transform.localPosition = new Vector3(-0.35f, -0.055f, -0.35f);
            curvedArch.GetComponent<MeshRenderer>().sharedMaterial = roadMat;

            // Inner cutout / curb
            GameObject innerCurb = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            innerCurb.name = "InnerCurb";
            innerCurb.transform.SetParent(root.transform);
            innerCurb.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
            innerCurb.transform.localPosition = new Vector3(-0.35f, -0.05f, -0.35f);
            if (groundMat != null) innerCurb.GetComponent<MeshRenderer>().sharedMaterial = groundMat;

            // Curved marking ring
            GameObject curvedMark = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            curvedMark.name = "CurvedRoadMark";
            curvedMark.transform.SetParent(root.transform);
            curvedMark.transform.localScale = new Vector3(1.4f, 0.075f, 1.4f);
            curvedMark.transform.localPosition = new Vector3(-0.35f, -0.05f, -0.35f);
            curvedMark.GetComponent<MeshRenderer>().sharedMaterial = markMat;
            Object.DestroyImmediate(curvedMark.GetComponent<Collider>());

            // Add Bézier curve Spline for smooth rounded turn
            var splineTrack = root.AddComponent<RoadSplineTrack>();
            // Local curve from bottom (0, 0, -1) to right (1, 0, 0)
            Vector3 startKnot = new Vector3(0f, 0.01f, -1f);
            Vector3 endKnot = new Vector3(1f, 0.01f, 0f);
            Vector3 cornerApex = new Vector3(1f, 0.01f, -1f);
            var container = TokyoSplineRailBuilder.CreateCurvedTurnSpline(root, startKnot, endKnot, cornerApex);
            splineTrack.SetSplineContainer(container);

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return savedPrefab;
        }

        public static GameObject CreateBuildingPrefab(string name, Material buildingMat)
        {
            string path = $"{TokyoAssetFactory.PREFAB_FOLDER}/Prefab_{name}.prefab";

            GameObject root = new GameObject($"Building_{name}");
            GameObject tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.name = "Tower";
            tower.transform.SetParent(root.transform);
            tower.transform.localScale = new Vector3(1.8f, 3.5f, 1.8f);
            tower.transform.localPosition = new Vector3(0f, 1.75f, 0f);
            tower.GetComponent<MeshRenderer>().sharedMaterial = buildingMat;

            // Roof detail
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "RoofAntenna";
            roof.transform.SetParent(root.transform);
            roof.transform.localScale = new Vector3(0.2f, 1.2f, 0.2f);
            roof.transform.localPosition = new Vector3(0f, 4.1f, 0f);
            Material antennaMat = TokyoAssetFactory.GetOrCreateMaterial("Antenna_Red", new Color(1f, 0.2f, 0.3f));
            roof.GetComponent<MeshRenderer>().sharedMaterial = antennaMat;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        /// <summary>
        /// Creates the Start Station Prefab matching the Rail Express / Tokyo Express station style
        /// (Platform + Canopy roof + Station sign + Waiting commuters)
        /// </summary>
        public static GameObject CreateStartStationPrefab(Material platformMat, Material roofMat, Material signMat)
        {
            string path = $"{TokyoAssetFactory.PREFAB_FOLDER}/Prefab_StartStation.prefab";

            GameObject root = new GameObject("StartStation");

            // Platform base
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "StationPlatform";
            platform.transform.SetParent(root.transform);
            platform.transform.localScale = new Vector3(1.8f, 0.2f, 1.8f);
            platform.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            platform.GetComponent<MeshRenderer>().sharedMaterial = platformMat;

            // Canopy Roof Pillars
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    pillar.name = $"Pillar_{x}_{z}";
                    pillar.transform.SetParent(root.transform);
                    pillar.transform.localScale = new Vector3(0.08f, 1.1f, 0.08f);
                    pillar.transform.localPosition = new Vector3(x * 0.7f, 1.1f, z * 0.7f);
                    pillar.GetComponent<MeshRenderer>().sharedMaterial = platformMat;
                    Object.DestroyImmediate(pillar.GetComponent<Collider>());
                }
            }

            // Station Roof / Canopy
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "StationCanopy";
            roof.transform.SetParent(root.transform);
            roof.transform.localScale = new Vector3(1.9f, 0.15f, 1.9f);
            roof.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            roof.GetComponent<MeshRenderer>().sharedMaterial = roofMat;
            Object.DestroyImmediate(roof.GetComponent<Collider>());

            // Station Sign (Signpost)
            GameObject signPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            signPost.name = "SignPost";
            signPost.transform.SetParent(root.transform);
            signPost.transform.localScale = new Vector3(0.06f, 0.8f, 0.06f);
            signPost.transform.localPosition = new Vector3(0.7f, 0.8f, -0.7f);
            signPost.GetComponent<MeshRenderer>().sharedMaterial = platformMat;
            Object.DestroyImmediate(signPost.GetComponent<Collider>());

            GameObject signBoard = GameObject.CreatePrimitive(PrimitiveType.Cube);
            signBoard.name = "SignBoard";
            signBoard.transform.SetParent(root.transform);
            signBoard.transform.localScale = new Vector3(0.5f, 0.4f, 0.08f);
            signBoard.transform.localPosition = new Vector3(0.7f, 1.5f, -0.7f);
            signBoard.GetComponent<MeshRenderer>().sharedMaterial = signMat;
            Object.DestroyImmediate(signBoard.GetComponent<Collider>());

            // Cute low-poly commuters waiting
            Material commuterMat1 = TokyoAssetFactory.GetOrCreateMaterial("Commuter_Blue", new Color(0.2f, 0.4f, 0.9f));
            Material commuterMat2 = TokyoAssetFactory.GetOrCreateMaterial("Commuter_Orange", new Color(1f, 0.5f, 0.1f));
            CreateCommuter("Commuter_1", root.transform, new Vector3(-0.4f, 0.5f, 0.3f), commuterMat1);
            CreateCommuter("Commuter_2", root.transform, new Vector3(0.3f, 0.5f, 0.4f), commuterMat2);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static void CreateCommuter(string name, Transform parent, Vector3 pos, Material mat)
        {
            GameObject person = GameObject.CreatePrimitive(PrimitiveType.Cube);
            person.name = name;
            person.transform.SetParent(parent);
            person.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f);
            person.transform.localPosition = pos;
            person.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(person.GetComponent<Collider>());
        }

        public static GameObject CreatePlayerMotoPrefab(Material bikeMat, Material riderMat)
        {
            string path = $"{TokyoAssetFactory.PREFAB_FOLDER}/Prefab_PlayerTokyoMoto.prefab";

            GameObject root = new GameObject("PlayerTokyoMoto");
            root.tag = "Player";

            // Character Controller
            var cc = root.AddComponent<CharacterController>();
            cc.radius = 0.4f;
            cc.height = 1.6f;
            cc.center = new Vector3(0f, 0.8f, 0f);

            // TokyoMotoController
            root.AddComponent<TokyoMotoController>();

            // Moto Body (Chassis)
            GameObject chassis = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chassis.name = "Chassis";
            chassis.transform.SetParent(root.transform);
            chassis.transform.localScale = new Vector3(0.5f, 0.4f, 1.4f);
            chassis.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            chassis.GetComponent<MeshRenderer>().sharedMaterial = bikeMat;
            Object.DestroyImmediate(chassis.GetComponent<Collider>());

            // Wheels (Cylinders)
            Material wheelMat = TokyoAssetFactory.GetOrCreateMaterial("Wheel_Dark", new Color(0.12f, 0.12f, 0.15f), 0.1f);
            CreateWheel("WheelFront", root.transform, new Vector3(0f, 0.25f, 0.6f), wheelMat);
            CreateWheel("WheelBack", root.transform, new Vector3(0f, 0.25f, -0.6f), wheelMat);

            // Delivery Backpack / Box
            GameObject parcelBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
            parcelBox.name = "DeliveryBox";
            parcelBox.transform.SetParent(root.transform);
            parcelBox.transform.localScale = new Vector3(0.55f, 0.6f, 0.45f);
            parcelBox.transform.localPosition = new Vector3(0f, 0.85f, -0.35f);
            Material boxMat = TokyoAssetFactory.GetOrCreateMaterial("DeliveryBox_Green", new Color(0.1f, 0.85f, 0.45f));
            parcelBox.GetComponent<MeshRenderer>().sharedMaterial = boxMat;
            Object.DestroyImmediate(parcelBox.GetComponent<Collider>());

            // Rider torso + helmet
            GameObject torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.name = "RiderTorso";
            torso.transform.SetParent(root.transform);
            torso.transform.localScale = new Vector3(0.45f, 0.5f, 0.3f);
            torso.transform.localPosition = new Vector3(0f, 0.85f, 0.05f);
            torso.GetComponent<MeshRenderer>().sharedMaterial = riderMat;
            Object.DestroyImmediate(torso.GetComponent<Collider>());

            GameObject helmet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            helmet.name = "Helmet";
            helmet.transform.SetParent(root.transform);
            helmet.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            helmet.transform.localPosition = new Vector3(0f, 1.25f, 0.08f);
            Material helmetMat = TokyoAssetFactory.GetOrCreateMaterial("Helmet_White", Color.white, 0.8f);
            helmet.GetComponent<MeshRenderer>().sharedMaterial = helmetMat;
            Object.DestroyImmediate(helmet.GetComponent<Collider>());

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        public static GameObject CreateTrafficMotoPrefab(Material bodyMat)
        {
            string path = $"{TokyoAssetFactory.PREFAB_FOLDER}/Prefab_TrafficMoto.prefab";

            GameObject root = new GameObject("TrafficMotoObstacle");

            var boxCol = root.AddComponent<BoxCollider>();
            boxCol.size = new Vector3(0.8f, 1.4f, 1.6f);
            boxCol.center = new Vector3(0f, 0.7f, 0f);
            boxCol.isTrigger = true;

            root.AddComponent<TrafficMotoObstacle>();

            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "TrafficBody";
            body.transform.SetParent(root.transform);
            body.transform.localScale = new Vector3(0.5f, 0.5f, 1.3f);
            body.transform.localPosition = new Vector3(0f, 0.4f, 0f);
            body.GetComponent<MeshRenderer>().sharedMaterial = bodyMat;
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        public static GameObject CreateDestinationFlagPrefab(Material flagMat)
        {
            string path = $"{TokyoAssetFactory.PREFAB_FOLDER}/Prefab_DestinationTarget.prefab";

            GameObject root = new GameObject("DestinationTargetMarker");
            
            // Glowing destination cylinder/ring
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.transform.SetParent(root.transform);
            cylinder.transform.localScale = new Vector3(1.6f, 0.1f, 1.6f);
            cylinder.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            cylinder.GetComponent<MeshRenderer>().sharedMaterial = flagMat;
            Object.DestroyImmediate(cylinder.GetComponent<Collider>());

            // Floating indicator diamond
            GameObject diamond = GameObject.CreatePrimitive(PrimitiveType.Cube);
            diamond.transform.SetParent(root.transform);
            diamond.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            diamond.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            diamond.transform.localRotation = Quaternion.Euler(45f, 45f, 45f);
            diamond.GetComponent<MeshRenderer>().sharedMaterial = flagMat;
            Object.DestroyImmediate(diamond.GetComponent<Collider>());

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static void CreateWheel(string name, Transform parent, Vector3 localPos, Material mat)
        {
            GameObject wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            wheel.name = name;
            wheel.transform.SetParent(parent);
            wheel.transform.localScale = new Vector3(0.45f, 0.1f, 0.45f);
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            wheel.transform.localPosition = localPos;
            wheel.GetComponent<MeshRenderer>().sharedMaterial = mat;
            Object.DestroyImmediate(wheel.GetComponent<Collider>());
        }
    }
}
#endif
