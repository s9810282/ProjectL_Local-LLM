using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // An isolated preview camera and built-in meshes; no objects are added to the user's scene.
    internal sealed class CombatLayoutPreview : IDisposable
    {
        private readonly PreviewRenderUtility preview = new PreviewRenderUtility();
        private readonly Dictionary<Color, Material> materials = new Dictionary<Color, Material>();
        private readonly Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        private readonly Mesh capsule = Resources.GetBuiltinResource<Mesh>("Capsule.fbx");
        private float yaw = 35, pitch = 48, distance = 16;
        private Vector3 pan;
        private static readonly Color Floor = new Color(.25f, .32f, .38f);
        private static readonly Color Blocked = new Color(.45f, .47f, .51f);
        private static readonly Color Rail = new Color(.60f, .48f, .26f);
        private static readonly Color Player = new Color(.27f, .64f, .80f);
        private static readonly Color Enemy = new Color(.87f, .42f, .43f);

        public CombatLayoutPreview()
        {
            preview.camera.nearClipPlane = .1f;
            preview.camera.farClipPlane = 300;
            preview.camera.fieldOfView = 40;
            preview.camera.clearFlags = CameraClearFlags.SolidColor;
            preview.camera.backgroundColor = new Color(.09f, .11f, .14f);
            preview.lights[0].intensity = 1.1f;
            preview.lights[0].transform.rotation = Quaternion.Euler(50, -30, 0);
            preview.lights[1].intensity = .6f;
            preview.ambientColor = new Color(.35f, .35f, .35f);
        }

        public void Reset(CombatLayoutStage stage)
        {
            yaw = 35; pitch = 48; pan = Vector3.zero;
            distance = Mathf.Max(8, Mathf.Max(stage.width, stage.height) * 1.6f);
        }

        public bool Navigate(Event e, Rect rect)
        {
            if (!rect.Contains(e.mousePosition)) return false;
            if (e.type == EventType.ScrollWheel)
            {
                distance = Mathf.Clamp(distance * Mathf.Exp(e.delta.y * .07f), 3, 140);
                e.Use(); return true;
            }
            if (e.type == EventType.MouseDrag && (e.button == 1 || e.button == 2 || (e.button == 0 && e.alt)))
            {
                if (e.shift || e.button == 2)
                    pan += (preview.camera.transform.right * -e.delta.x + preview.camera.transform.up * e.delta.y) * distance * .0015f;
                else { yaw += e.delta.x * .4f; pitch = Mathf.Clamp(pitch + e.delta.y * .4f, 15, 85); }
                e.Use(); return true;
            }
            return false;
        }

        private void Camera(CombatLayoutDocument document, Rect rect)
        {
            var target = new Vector3((document.stage.width - 1) * .5f, .5f, -(document.stage.height - 1) * .5f) + pan;
            var rotation = Quaternion.Euler(pitch, yaw, 0);
            preview.camera.transform.SetPositionAndRotation(target - rotation * Vector3.forward * distance, rotation);
            preview.camera.aspect = rect.width / Mathf.Max(1, rect.height);
        }

        public Vector2Int Pick(CombatLayoutDocument document, Rect rect, Vector2 mouse)
        {
            Camera(document, rect);
            Ray ray = preview.camera.ViewportPointToRay(new Vector3((mouse.x - rect.x) / rect.width, 1 - (mouse.y - rect.y) / rect.height, 0));
            float nearest = float.PositiveInfinity;
            Vector2Int selected = new Vector2Int(-1, -1);
            foreach (var cell in document.cells)
            {
                float top = cell.elevation * .45f;
                float height = .22f + (cell.type == "Blocked" ? .6f : 0);
                var bounds = new Bounds(new Vector3(cell.x, top + height * .5f - .22f, -cell.y), new Vector3(.96f, height, .96f));
                if (document.Unit(cell.x, cell.y) != null) bounds.Encapsulate(new Bounds(new Vector3(cell.x, top + .65f, -cell.y), new Vector3(.55f, 1.3f, .55f)));
                if (bounds.IntersectRay(ray, out float hit) && hit < nearest) { nearest = hit; selected = new Vector2Int(cell.x, cell.y); }
            }
            return selected;
        }

        public void Draw(Rect rect, CombatLayoutDocument document, Vector2Int selected, int selectedUnitKey, bool labels,
            HashSet<Vector2Int> move, HashSet<Vector2Int> attack)
        {
            if (Event.current.type != EventType.Repaint) return;
            Camera(document, rect);
            preview.BeginPreview(rect, GUIStyle.none);
            foreach (var cell in document.cells)
            {
                var position = new Vector2Int(cell.x, cell.y);
                Color color = cell.type == "Blocked" ? Blocked : cell.type == "Rail" ? Rail : Floor;
                if (cell.type == "Floor")
                {
                    if (cell.y < document.stage.playerDepth) color = Color.Lerp(color, Player, .25f);
                    else if (cell.y >= document.stage.height - document.stage.enemyDepth) color = Color.Lerp(color, Enemy, .25f);
                }
                if (move.Contains(position)) color = Color.Lerp(color, new Color(.25f, .85f, .5f), .7f);
                if (attack.Contains(position)) color = Color.Lerp(color, new Color(1, .65f, .2f), .7f);
                if (move.Contains(position) && attack.Contains(position)) color = new Color(.82f, .78f, .3f);
                if (position == selected) color = Color.Lerp(color, Color.white, .6f);
                float top = cell.elevation * .45f;
                float height = .22f + (cell.type == "Blocked" ? .6f : 0);
                preview.DrawMesh(cube, Matrix4x4.TRS(new Vector3(cell.x, top + height * .5f - .22f, -cell.y), Quaternion.identity, new Vector3(.96f, height, .96f)), Material(color), 0);
            }
            foreach (var unit in document.units)
            {
                float top = document.Cell(unit.x, unit.y).elevation * .45f;
                Color color = unit.owner == "Player" ? Player : Enemy;
                var reference = document.characters.Find("Character_Key", unit.characterKey);
                if (reference != null && ColorUtility.TryParseHtmlString(document.characters.Value(reference, "EditorCharacter_Color"), out var custom)) color = custom;
                if (unit.key == selectedUnitKey) color = Color.Lerp(color, Color.white, .4f);
                preview.DrawMesh(capsule, Matrix4x4.TRS(new Vector3(unit.x, top + .65f, -unit.y), Quaternion.identity, new Vector3(.5f, .65f, .5f)), Material(color), 0);
            }
            preview.Render(true);
            GUI.DrawTexture(rect, preview.EndPreview(), ScaleMode.StretchToFill, false);
            if (!labels) return;
            GUI.BeginGroup(rect);
            foreach (var unit in document.units)
            {
                var viewport = preview.camera.WorldToViewportPoint(new Vector3(unit.x, document.Cell(unit.x, unit.y).elevation * .45f + 1.5f, -unit.y));
                if (viewport.z <= 0 || viewport.x < 0 || viewport.x > 1 || viewport.y < 0 || viewport.y > 1) continue;
                string text = CombatLayoutWindow.CharacterName(document.characters, unit.characterKey);
                GUI.Label(new Rect(viewport.x * rect.width - 70, (1 - viewport.y) * rect.height - 20, 140, 22), text, EditorStyles.whiteMiniLabel);
            }
            GUI.EndGroup();
        }

        private Material Material(Color color)
        {
            if (materials.TryGetValue(color, out var material)) return material;
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Hidden/Internal-Colored");
            material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave, color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            materials.Add(color, material);
            return material;
        }

        public void Dispose()
        {
            preview.Cleanup();
            foreach (var material in materials.Values) UnityEngine.Object.DestroyImmediate(material);
            materials.Clear();
        }
    }
}
