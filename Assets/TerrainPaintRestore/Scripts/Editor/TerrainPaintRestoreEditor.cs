using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TerrainPaintRestore))]
public class TerrainPaintRestoreEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        TerrainPaintRestore script = (TerrainPaintRestore)target;

        if (script.terrains.Length > 0 && !script.terrains.Any(x => x == null))
        {
            EditorGUI.BeginDisabledGroup(script.activeTerrain != null);
            if (GUILayout.Button("Backup Terrain", GUILayout.Height(40)))
            {
                script.BackupTerrain();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(20);

            if (script.backups.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(script.activeTerrain != null);
                GUIContent arrayLabel = new GUIContent("Backups");
                script.selectedBackup = EditorGUILayout.Popup(arrayLabel, script.selectedBackup, script.backups?.ToArray() ?? new string[0]);
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Really delete terrain backup?", $"Are you sure you want to delete {script.backups[script.selectedBackup]}?", "Yes", "No"))
                    {
                        script.DeleteBackup(script.backups[script.selectedBackup]);
                    }
                }
                EditorGUI.EndDisabledGroup();

                EditorGUILayout.EndHorizontal();
                GUILayout.BeginVertical("GroupBox");
                script.SetPainting(GUILayout.Toggle(script.activeTerrain != null, "Paint Restore", "Button", GUILayout.Height(40)));
                script.brushSize = EditorGUILayout.Slider("Brush Size", script.brushSize, 0, 128);
                script.restoreHeight = GUILayout.Toggle(script.restoreHeight, "Restore Height");
                script.restoreTexture = GUILayout.Toggle(script.restoreTexture, "Restore Texture");
                script.restoreDetails = GUILayout.Toggle(script.restoreDetails, "Restore Details");
                script.restoreTrees = GUILayout.Toggle(script.restoreTrees, "Restore Trees");
                GUILayout.EndVertical();
            }
        }
    }

    private void OnSceneGUI()
    {
        TerrainPaintRestore script = (TerrainPaintRestore)target;

        if (script.CanRestorePaint())
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
    }
}