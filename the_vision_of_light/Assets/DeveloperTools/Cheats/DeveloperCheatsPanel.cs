using UnityEngine;

namespace VisionOfLight.DeveloperTools
{
    /// <summary>
    /// Small IMGUI overlay showing the live state of every developer cheat.
    /// Added automatically by <see cref="DeveloperCheatsManager"/>.
    /// Uses OnGUI so it never touches the game's Canvas, EventSystem, or time scale.
    /// Compiled out of release builds entirely.
    /// </summary>
    public class DeveloperCheatsPanel : MonoBehaviour
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private DeveloperCheatsManager manager;
        private GUIStyle boxStyle;
        private GUIStyle lineStyle;

        private void Awake()
        {
            manager = GetComponent<DeveloperCheatsManager>();
        }

        private void OnGUI()
        {
            if (manager == null || !manager.CheatsAllowed || !manager.PanelVisible)
                return;

            EnsureStyles();

            const float width = 320f;
            const float height = 220f;
            GUILayout.BeginArea(new Rect(12f, 12f, width, height), GUIContent.none, boxStyle);

            GUILayout.Label("<b>DEVELOPER CHEATS</b>  (F1 hide)", lineStyle);
            GUILayout.Space(4f);
            DrawLine("F2", "High Damage x" + manager.Config.damageMultiplier, manager.HighDamageOn);
            DrawLine("F3", "Full Heal (tap)", true, stateless: true);
            DrawLine("Shift+F3", "Auto Heal " + manager.Config.autoHealPerSecond + "/s", manager.AutoHealOn);
            DrawLine("F4", "Unlimited Stamina", manager.UnlimitedStaminaOn);
            DrawLine("F5", "Invincibility", manager.InvincibleOn);
            DrawLine("F6", "Fast Move x" + manager.Config.moveSpeedMultiplier, manager.FastMoveOn);
            DrawLine("F7", "Fly Mode (WASD / Space / LCtrl / Shift)", manager.FlyOn);

            GUILayout.EndArea();
        }

        private void DrawLine(string key, string label, bool on, bool stateless = false)
        {
            string state = stateless ? "" : (on ? "  <color=#7CFC00><b>ON</b></color>" : "  <color=#999999>OFF</color>");
            GUILayout.Label("<b>[" + key + "]</b> " + label + state, lineStyle);
        }

        private void EnsureStyles()
        {
            if (boxStyle != null)
                return;

            Texture2D bg = new Texture2D(1, 1);
            bg.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
            bg.Apply();

            boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = bg;
            boxStyle.padding = new RectOffset(10, 10, 8, 8);

            lineStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 14
            };
            lineStyle.normal.textColor = Color.white;
        }
#endif
    }
}
