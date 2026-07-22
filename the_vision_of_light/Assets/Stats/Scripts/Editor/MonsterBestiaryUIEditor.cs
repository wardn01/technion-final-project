#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MonsterBestiaryUI))]
public class MonsterBestiaryUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        MonsterBestiaryUI ui = (MonsterBestiaryUI)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Debug / Test", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Reveal All Monsters", GUILayout.Height(28)))
            {
                ui.RevealAllForTest();
                EditorUtility.SetDirty(ui);
            }

            if (GUILayout.Button("Lock Unknown", GUILayout.Height(28)))
            {
                ui.LockUnknownForTest();
                EditorUtility.SetDirty(ui);
            }
        }

        EditorGUILayout.HelpBox(
            "Reveal All = show every monster name/icon/loot for layout testing. Does not change the save.\n" +
            "Turn Reveal All OFF before building / pushing to GitHub.",
            MessageType.Warning);
    }
}
#endif
