using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
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
        // 시작 구간 저장값은 유지한다. Chapter 2는 창에서만 쓰는 선택값이며 Stage + StageChapterKey로 저장한다.
        private const int LobbyStart = 0;
        private const int StageStart = 1;
        private const int CombatStart = 2;
        private const int Chapter2Start = 3;
        private static readonly string[] StartLabels = { "Lobby", "Chapter 1", "Chapter 2", "Combat" };
        private static readonly int[] StartValues = { LobbyStart, StageStart, Chapter2Start, CombatStart };
        private static readonly string[] StageLabels =
        {
            "진입 노드의 스테이지", "보스 스테이지 (맵 역할 Boss)", "지정 스테이지 (Dev Stage Key)",
            "지정 스테이지 데이터를 Dev Node Id 노드에 (Dev Stage Data)"
        };
        // GameBootstrapEditorOverrides.DevBattleStage 값. 라벨 순서와 같다.
        private const int BossStage = 1;
        private const int SelectedStage = 2;
        private const int PlacedStage = 3;
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
            int start = Clamp(GameBootstrapEditorOverrides.StartLifeCycle, CombatStart + 1);
            if (start == StageStart && GameBootstrapEditorOverrides.StageChapterKey == 2)
                start = Chapter2Start;
            bool useDevProgress = GameBootstrapEditorOverrides.UseDevProgress;
            bool useTutorial = GameBootstrapEditorOverrides.UseTutorial;
            int loadout = GameBootstrapEditorOverrides.DevLoadoutKey;
            int chapter = GameBootstrapEditorOverrides.DevChapterKey;
            int node = GameBootstrapEditorOverrides.DevNodeId;
            int battleStage = Clamp(GameBootstrapEditorOverrides.DevBattleStage, StageLabels.Length);
            int stageKey = GameBootstrapEditorOverrides.DevStageKey;
            bool stageDataRead = GameBootstrapEditorOverrides.TryReadDevStageData(out CombatStageData stageData);

            EditorGUI.BeginChangeCheck();
            enabled = EditorGUILayout.ToggleLeft("GameBootstrap 값 덮어쓰기 (이 PC만)", enabled);
            using (new EditorGUI.DisabledScope(!enabled))
            {
                EditorGUI.indentLevel++;
                start = EditorGUILayout.IntPopup("Start Life Cycle", start, StartLabels, StartValues);
                bool chapterStart = start == StageStart || start == Chapter2Start;
                useTutorial = EditorGUILayout.Toggle("튜토리얼 사용", useTutorial);
                EditorGUILayout.HelpBox(
                    "켜면 기존 첫 전투 조건에 따라 튜토리얼을 시작하고, 끄면 자동 시작을 건너뜁니다. 다음 재생부터 적용됩니다.",
                    MessageType.None);

                EditorGUILayout.Space();
                using (new EditorGUI.DisabledScope(start != CombatStart))
                {
                    useDevProgress = EditorGUILayout.Toggle("Use Dev Progress", useDevProgress);
                }
                using (new EditorGUI.DisabledScope(!chapterStart && (!useDevProgress || start != CombatStart)))
                {
                    loadout = EditorGUILayout.IntField("Dev Loadout Key", loadout);
                    using (new EditorGUI.DisabledScope(chapterStart))
                    {
                        // 개발 진입의 전투 정의는 Database 하나다(Fixture 모드는 2026-10-28에 지움). 고를 것이 없어 읽기 줄로만 보인다.
                        EditorGUILayout.LabelField("Battle Definition", "Database (운영 DB 정의)");
                        // 지정 스테이지 배치는 챕터를 그 스테이지의 맵으로 정하므로 Dev Chapter Key를 쓰지 않는다.
                        using (new EditorGUI.DisabledScope(battleStage == PlacedStage))
                        {
                            chapter = EditorGUILayout.IntField("Dev Chapter Key", chapter);
                        }
                        node = EditorGUILayout.IntField("Dev Node Id", node);
                        battleStage = EditorGUILayout.Popup("Dev Battle Stage", battleStage, StageLabels);
                        using (new EditorGUI.DisabledScope(battleStage != SelectedStage))
                        {
                            stageKey = EditorGUILayout.IntField("Dev Stage Key", stageKey);
                        }
                        using (new EditorGUI.DisabledScope(battleStage != PlacedStage))
                        {
                            stageData = (CombatStageData)EditorGUILayout.ObjectField(
                                "Dev Stage Data", stageData, typeof(CombatStageData), false);
                        }
                    }
                    DrawStartingItems();
                }

                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                GameBootstrapEditorOverrides.Enabled = enabled;
                bool chapterStart = start == StageStart || start == Chapter2Start;
                GameBootstrapEditorOverrides.StartLifeCycle = chapterStart ? StageStart : start;
                if (chapterStart)
                    GameBootstrapEditorOverrides.StageChapterKey = start == Chapter2Start ? 2 : 1;
                GameBootstrapEditorOverrides.UseDevProgress = useDevProgress;
                GameBootstrapEditorOverrides.UseTutorial = useTutorial;
                GameBootstrapEditorOverrides.DevLoadoutKey = loadout;
                GameBootstrapEditorOverrides.DevChapterKey = chapter;
                GameBootstrapEditorOverrides.DevNodeId = node;
                GameBootstrapEditorOverrides.DevBattleStage = battleStage;
                GameBootstrapEditorOverrides.DevStageKey = stageKey;
                // 저장된 에셋을 찾지 못한 채(null) 다른 칸만 바꾼 경우에는 저장값을 지우지 않는다(경고를 남겨 둠).
                if (stageDataRead || stageData != null)
                    GameBootstrapEditorOverrides.DevStageData = stageData;
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                enabled
                    ? "재생 시작 때 Boot 씬의 GameBootstrap 값 대신 위 값을 씁니다. 콘솔에 '에디터 덮어쓰기 사용' 로그가 한 번 남습니다."
                    : "꺼져 있으면 Boot 씬의 GameBootstrap 인스펙터 값을 그대로 씁니다.",
                MessageType.None);
            if (enabled && start == CombatStart)
            {
                EditorGUILayout.HelpBox(
                    "Combat 직접 시작은 Use Dev Progress를 켜고 유효한 로드아웃·챕터·노드를 지정해야 합니다.",
                    MessageType.Info);
                if (useDevProgress && battleStage == BossStage)
                {
                    EditorGUILayout.HelpBox(
                        "보스 스테이지: Dev Node Id 노드로 진입하되(시작 노드와 이어진 노드여야 함) 전투 정의는 현재 맵의 보스 전투 스테이지에서 읽습니다. 맵 진행은 진입한 노드로 처리되므로 보스 승리 뒤 챕터 완료·Run 종료 흐름은 돌지 않습니다.",
                        MessageType.Info);
                }
                if (useDevProgress && battleStage == SelectedStage)
                {
                    EditorGUILayout.HelpBox(
                        "지정 스테이지: Dev Node Id 노드로 진입하되(시작 노드와 이어진 노드여야 함) 전투 정의는 Dev Stage Key의 전투 스테이지에서 읽습니다. 키는 현재 챕터 맵의 전투 스테이지여야 하며, 1 미만이면 재생 시작에서 멈춥니다. 맵 진행은 진입한 노드로 처리됩니다.",
                        stageKey >= 1 ? MessageType.Info : MessageType.Warning);
                }
                if (useDevProgress && battleStage == PlacedStage)
                {
                    EditorGUILayout.HelpBox(
                        "지정 스테이지 데이터: 이 전투 스테이지를 Dev Node Id 노드(시작 노드와 이어진 노드여야 함)에 실제로 놓고 진입합니다. 챕터는 그 스테이지의 맵이라 Dev Chapter Key는 쓰지 않습니다. DB 스테이지 루트 표에 등록된 일반 전투 스테이지만 됩니다(보스·미등록은 재생 시작에서 멈춤).",
                        stageData != null ? MessageType.Info : MessageType.Warning);
                    if (!stageDataRead)
                    {
                        EditorGUILayout.HelpBox(
                            "저장된 Dev Stage Data 에셋을 찾지 못했습니다. 다시 고르세요.", MessageType.Warning);
                    }
                }
                if (useDevProgress)
                {
                    EditorGUILayout.HelpBox(
                        "개발 진입은 운영과 같은 DB 전투 정의를 씁니다. 노드 그래프도 DB 맵 설정으로 만들어지며, Dev Node Id가 전투 노드가 아니면 시작 노드와 이어진 첫 전투 노드로 바꿔 진입합니다(콘솔에 한 줄).",
                        MessageType.Info);
                }
            }
            else if (enabled && (start == StageStart || start == Chapter2Start))
            {
                int chapterKey = start == Chapter2Start ? 2 : 1;
                EditorGUILayout.HelpBox(
                    $"Starts a new run at the beginning of Chapter {chapterKey} and opens stage selection. " +
                    "Dev Loadout Key and starting items apply. Combat entry settings are not used.",
                    MessageType.Info);
            }
            else if (enabled && start == LobbyStart)
            {
                EditorGUILayout.HelpBox(
                    "로비는 화면 표시로 시작합니다. 개발 전투 설정은 적용하지 않으며 새 Run·챕터를 자동으로 시작하지 않습니다.",
                    MessageType.Info);
            }
        }

        /// <summary>
        /// 새 Run 직후 지급할 시작 아이템 목록입니다(시작 아이템 설계 DI8). 값 입력은 바뀐 때만, 추가·삭제는 누른 즉시 저장합니다.
        /// 키가 DB에 있는지는 검사하지 않습니다. 없는 키는 재생 시작 때 Core 사유로 멈춥니다.
        /// </summary>
        private static void DrawStartingItems()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("시작 아이템 (새 Run 직후 지급)", EditorStyles.boldLabel);
            if (!GameBootstrapEditorOverrides.TryReadDevStartingItems(out DevStartingItem[] items))
            {
                EditorGUILayout.HelpBox(
                    "저장된 시작 아이템 값을 읽지 못했습니다. 이대로 재생하면 시작에서 멈춥니다. 비운 뒤 다시 넣으세요.",
                    MessageType.Error);
                if (GUILayout.Button("비우기"))
                    GameBootstrapEditorOverrides.DevStartingItems = Array.Empty<DevStartingItem>();
                return;
            }

            var list = new List<DevStartingItem>(items);
            bool changed = false;
            int removed = -1;
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 36f;
            try
            {
                for (int i = 0; i < list.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUI.BeginChangeCheck();
                        int key = EditorGUILayout.IntField("키", list[i].ItemKey);
                        int quantity = EditorGUILayout.IntField("수량", list[i].Quantity);
                        if (EditorGUI.EndChangeCheck())
                        {
                            list[i] = new DevStartingItem(key, quantity);
                            changed = true;
                        }
                        if (GUILayout.Button("삭제", GUILayout.Width(48f)))
                            removed = i;
                    }
                }
            }
            finally
            {
                EditorGUIUtility.labelWidth = labelWidth;
            }

            if (removed >= 0)
            {
                list.RemoveAt(removed);
                changed = true;
            }
            if (GUILayout.Button("추가"))
            {
                list.Add(new DevStartingItem(1, 1));
                changed = true;
            }
            if (changed)
                GameBootstrapEditorOverrides.DevStartingItems = list.ToArray();

            foreach (DevStartingItem item in list)
            {
                if (item.ItemKey >= 1 && item.Quantity >= 1)
                    continue;
                EditorGUILayout.HelpBox(
                    "키와 수량은 1 이상이어야 합니다. 없는 키는 재생 시작 때 Core 사유(UnknownItem)로 멈춥니다.",
                    MessageType.Error);
                break;
            }
        }

        private static int Clamp(int value, int count)
        {
            return value < 0 || value >= count ? 0 : value;
        }
    }
}
