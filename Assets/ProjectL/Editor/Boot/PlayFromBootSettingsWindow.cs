using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>
    /// Boot 씬을 열지 않고 재생 설정을 바꾸는 창입니다. Play From Boot 토글과 GameBootstrap 값 덮어쓰기를 한곳에서 다룹니다.
    /// </summary>
    /// <remarks>
    /// 값은 이 PC의 EditorPrefs에만 저장됩니다(<see cref="GameBootstrapEditorOverrides"/>). Boot 씬의 값은 공용 기본값으로 그대로이며
    /// 다른 작업자의 재생에는 영향이 없습니다. 덮어쓰기를 켜면 Boot 씬에서 직접 Play해도 적용됩니다. 빌드에는 영향이 없습니다.
    /// </remarks>
    internal sealed class PlayFromBootSettingsWindow : EditorWindow
    {
        private static readonly string[] StartLabels = { "Combat", "Stage" };
        private static readonly string[] ModeLabels = { "Live", "RecordInputOnly" };
        private static readonly string[] DefinitionLabels = { "Fixture (개발 고정 정의)", "Database (운영 DB 정의)" };
        private Vector2 _scrollPosition;

        [MenuItem("ProjectL/Play From Boot 설정...", false, 101)]
        private static void Open()
        {
            PlayFromBootSettingsWindow window = GetWindow<PlayFromBootSettingsWindow>("Play From Boot");
            window.minSize = new Vector2(360f, 460f);
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
            if (playing)
                EditorGUILayout.HelpBox("재생 중에 바꾼 값은 다음 재생부터 적용됩니다.", MessageType.Info);

            DrawPlayFromBoot(playing);
            EditorGUILayout.Space();
            DrawOverrides();
            EditorGUILayout.EndScrollView();
        }

        private static void DrawPlayFromBoot(bool playing)
        {
            using (new EditorGUI.DisabledScope(playing))
            {
                EditorGUI.BeginChangeCheck();
                bool playFromBoot = EditorGUILayout.Toggle("Play From Boot", PlayFromBootToggle.IsEnabled);
                if (EditorGUI.EndChangeCheck())
                {
                    PlayFromBootToggle.IsEnabled = playFromBoot;
                    PlayFromBootToggle.Apply();
                }
            }
        }

        private static void DrawOverrides()
        {
            // 값이 바뀐 때만 EditorPrefs에 쓴다(다시 그릴 때마다 쓰지 않는다).
            bool enabled = GameBootstrapEditorOverrides.Enabled;
            int start = Clamp(GameBootstrapEditorOverrides.StartLifeCycle, StartLabels.Length);
            int mode = Clamp(GameBootstrapEditorOverrides.ExecutionMode, ModeLabels.Length);
            bool useDevProgress = GameBootstrapEditorOverrides.UseDevProgress;
            int definition = Clamp(GameBootstrapEditorOverrides.DevBattleDefinition, DefinitionLabels.Length);
            int loadout = GameBootstrapEditorOverrides.DevLoadoutKey;
            int chapter = GameBootstrapEditorOverrides.DevChapterKey;
            int node = GameBootstrapEditorOverrides.DevNodeId;
            int mapWidth = GameBootstrapEditorOverrides.DevMapWidth;
            int mapHeight = GameBootstrapEditorOverrides.DevMapHeight;
            int playerDepth = GameBootstrapEditorOverrides.DevPlayerDeploymentDepth;
            int enemyDepth = GameBootstrapEditorOverrides.DevEnemyDeploymentDepth;

            EditorGUI.BeginChangeCheck();
            enabled = EditorGUILayout.ToggleLeft("GameBootstrap 값 덮어쓰기 (이 PC만)", enabled);
            using (new EditorGUI.DisabledScope(!enabled))
            {
                EditorGUI.indentLevel++;
                start = EditorGUILayout.Popup("Start Life Cycle", start, StartLabels);
                mode = EditorGUILayout.Popup("Execution Mode", mode, ModeLabels);

                EditorGUILayout.Space();
                useDevProgress = EditorGUILayout.Toggle("Use Dev Progress", useDevProgress);
                using (new EditorGUI.DisabledScope(!useDevProgress))
                {
                    definition = EditorGUILayout.Popup("Battle Definition", definition, DefinitionLabels);
                    loadout = EditorGUILayout.IntField("Dev Loadout Key", loadout);
                    chapter = EditorGUILayout.IntField("Dev Chapter Key", chapter);
                    node = EditorGUILayout.IntField("Dev Node Id", node);
                    EditorGUILayout.Space();
                    // 필드 크기는 Fixture 정의에서만 쓴다. Database는 DB의 필드·배치 깊이를 쓴다.
                    using (new EditorGUI.DisabledScope(start != 0 || definition != 0))
                    {
                        EditorGUILayout.LabelField("개발 전투 필드", EditorStyles.boldLabel);
                        mapWidth = EditorGUILayout.IntField("필드 가로 (칸)", mapWidth);
                        mapHeight = EditorGUILayout.IntField("필드 세로 (칸)", mapHeight);
                        playerDepth = EditorGUILayout.IntField("아군 배치 깊이", playerDepth);
                        enemyDepth = EditorGUILayout.IntField("적군 배치 깊이", enemyDepth);
                    }
                }

                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                GameBootstrapEditorOverrides.Enabled = enabled;
                GameBootstrapEditorOverrides.StartLifeCycle = start;
                GameBootstrapEditorOverrides.ExecutionMode = mode;
                GameBootstrapEditorOverrides.UseDevProgress = useDevProgress;
                GameBootstrapEditorOverrides.DevBattleDefinition = definition;
                GameBootstrapEditorOverrides.DevLoadoutKey = loadout;
                GameBootstrapEditorOverrides.DevChapterKey = chapter;
                GameBootstrapEditorOverrides.DevNodeId = node;
                GameBootstrapEditorOverrides.DevMapWidth = mapWidth;
                GameBootstrapEditorOverrides.DevMapHeight = mapHeight;
                GameBootstrapEditorOverrides.DevPlayerDeploymentDepth = playerDepth;
                GameBootstrapEditorOverrides.DevEnemyDeploymentDepth = enemyDepth;
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                enabled
                    ? "재생 시작 때 Boot 씬의 GameBootstrap 값 대신 위 값을 씁니다. 콘솔에 '에디터 덮어쓰기 사용' 로그가 한 번 남습니다."
                    : "꺼져 있으면 Boot 씬의 GameBootstrap 인스펙터 값을 그대로 씁니다.",
                MessageType.None);
            if (enabled && start == 0)
            {
                EditorGUILayout.HelpBox(
                    "Combat 직접 시작은 Use Dev Progress를 켜고 유효한 로드아웃·챕터·노드를 지정해야 합니다. Live와 RecordInputOnly에 동일하게 적용됩니다.",
                    MessageType.Info);
                if (useDevProgress && definition == 1)
                {
                    EditorGUILayout.HelpBox(
                        "Database 정의는 운영과 같은 DB 전투 정의를 씁니다. 노드 그래프도 DB 맵 설정으로 만들어지므로 Dev Node Id가 전투 노드가 아니면 진입이 거절됩니다(콘솔에 사유).",
                        MessageType.Info);
                }
                else if (useDevProgress)
                {
                    bool invalid = mapWidth <= 0 || mapHeight <= 0 || playerDepth <= 0 || enemyDepth <= 0
                        || (long)playerDepth + enemyDepth >= mapHeight || (long)mapWidth * enemyDepth < 2;
                    EditorGUILayout.HelpBox(
                        "필드 값은 다음 재생부터 적용됩니다. 모든 값은 1 이상, 배치 깊이 합은 세로보다 작아야 하며 적군 구역은 최소 2칸이어야 합니다. 적·본부 위치는 필드 크기에 맞춰 조정됩니다.",
                        invalid ? MessageType.Error : MessageType.Info);
                }
            }
        }

        private static int Clamp(int value, int count)
        {
            return value < 0 || value >= count ? 0 : value;
        }
    }
}
