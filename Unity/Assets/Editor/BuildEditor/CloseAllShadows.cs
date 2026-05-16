using UnityEngine;
using UnityEditor;

public class CloseAllShadows : Editor
{
    // 在顶部菜单栏生成一个按钮
    [MenuItem("Custom/一键关闭所有物体阴影")]
    public static void CloseAllShadowsInScene()
    {
        // 获取场景里所有物体
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();

        int count = 0;

        foreach (GameObject go in allObjects)
        {
            // 关闭 MeshRenderer（普通模型）
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                count++;
            }

            // 关闭 SkinnedMeshRenderer（角色、怪物、武器）
            SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
            if (smr != null)
            {
                smr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                smr.receiveShadows = false;
                count++;
            }
        }

        EditorUtility.DisplayDialog("完成", $"已关闭场景中 {count} 个物体的阴影！", "OK");
    }
}