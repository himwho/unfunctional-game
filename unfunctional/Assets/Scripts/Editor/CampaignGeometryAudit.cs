using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CampaignGeometryAudit
{
    [MenuItem("Unfunctional/Scene Art/Audit Door and Keypad Geometry")]
    public static void Run()
    {
        if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var setup=EditorSceneManager.GetSceneManagerSetup();var report=new StringBuilder();
        var player=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
        var cc=player.GetComponent<CharacterController>();
        report.AppendLine($"PLAYER scale={player.transform.localScale} height={cc.height} radius={cc.radius} skin={cc.skinWidth}");
        try
        {
            foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
            {
                var scene=EditorSceneManager.OpenScene(entry.path);
                report.AppendLine("\n"+scene.name);
                foreach(var door in Object.FindObjectsByType<DoorController>(FindObjectsSortMode.None))
                {
                    report.AppendLine($"DOOR {door.name} at {door.transform.position} scale {door.transform.lossyScale} openDistance {door.doorOpenDistance}");
                    foreach(var c in door.GetComponentsInChildren<Collider>(true))
                        if(!c.isTrigger)report.AppendLine($"  SOLID {c.name} active={c.gameObject.activeInHierarchy} bounds={c.bounds}");
                    foreach(var b in door.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="bttn5"))
                        report.AppendLine($"  KEY {b.name} world={b.position} scale={b.lossyScale} box={b.GetComponent<Collider>()?.bounds}");
                }
                foreach(var c in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
                    if(c.name.Contains("Wall")||c.name.Contains("wall"))report.AppendLine($"WALL {c.name} type={c.GetType().Name} bounds={c.bounds} (bounds alone do not establish a blocked mesh opening)");
            }
        }
        finally{EditorSceneManager.RestoreSceneManagerSetup(setup);}
        Directory.CreateDirectory("TestResults");File.WriteAllText("TestResults/geometry-audit.txt",report.ToString());
        Debug.Log("[GeometryAudit] Wrote TestResults/geometry-audit.txt");
    }
}
