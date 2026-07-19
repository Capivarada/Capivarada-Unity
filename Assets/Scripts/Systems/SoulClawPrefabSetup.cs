#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using ArenadasAlmas.Player;

namespace ArenadasAlmas.Systems
{
    public class SoulClawPrefabSetup
    {
        [MenuItem("ArenadasAlmas/Create Prefabs/Soul Claw")]
        public static void CreateSoulClawPrefab()
        {
            GameObject soulClawObj = new GameObject("SoulClaw");
            
            Transform cylinderBody = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            cylinderBody.name = "Cylinder";
            cylinderBody.SetParent(soulClawObj.transform);
            cylinderBody.localPosition = Vector3.zero;
            cylinderBody.localScale = new Vector3(0.1f, 0.5f, 0.1f);
            Object.DestroyImmediate(cylinderBody.GetComponent<Collider>());

            MeshRenderer cylinderRenderer = cylinderBody.GetComponent<MeshRenderer>();
            cylinderRenderer.material = new Material(Shader.Find("Standard"));
            cylinderRenderer.material.color = Color.yellow;

            Transform clawTip = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            clawTip.name = "ClawTip";
            clawTip.SetParent(soulClawObj.transform);
            clawTip.localPosition = new Vector3(0, 0.5f, 0);
            clawTip.localScale = Vector3.one * 0.2f;
            Object.DestroyImmediate(clawTip.GetComponent<Collider>());

            MeshRenderer tipRenderer = clawTip.GetComponent<MeshRenderer>();
            tipRenderer.material = new Material(Shader.Find("Standard"));
            tipRenderer.material.color = Color.yellow;

            SoulClawVisual visual = soulClawObj.AddComponent<SoulClawVisual>();
            visual.GetType().GetField("cylinderBody", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(visual, cylinderBody);
            visual.GetType().GetField("clawTip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(visual, clawTip);

            SoulClawController controller = soulClawObj.AddComponent<SoulClawController>();
            controller.GetType().GetField("clawVisual", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(controller, visual);

            soulClawObj.SetActive(false);

            string path = "Assets/Prefabs/SoulClaw.prefab";
            PrefabUtility.SaveAsPrefabAsset(soulClawObj, path);
            Object.DestroyImmediate(soulClawObj);
            
            EditorUtility.DisplayDialog("Success", "SoulClaw prefab created at " + path, "OK");
        }
    }
}
#endif
