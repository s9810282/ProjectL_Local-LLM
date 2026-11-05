#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot
{
    /// <summary>
    /// 에디터에서만 쓰는 GameBootstrap 설정 덮어쓰기입니다. 이 PC의 EditorPrefs에만 저장됩니다.
    /// 메뉴 <c>ProjectL &gt; Play From Boot 설정</c> 창이 쓰고, GameBootstrap이 재생 시작 때 읽습니다.
    /// </summary>
    /// <remarks>
    /// Boot 씬에 저장된 값은 공용 기본값으로 그대로 두고, 켜져 있을 때만 이번 재생의 인스턴스 값을 바꿉니다.
    /// 빌드에는 들어가지 않습니다. 열거형은 에디터 어셈블리에서 쓸 수 있도록 정수로 저장합니다
    /// (시작 구간 0 = Lobby, 1 = Stage, 2 = Combat). 전투 정의 선택(Fixture/Database)은 Fixture 모드를 지우며 걷었다(2026-10-28).
    /// 그때 쓰던 EditorPrefs 키(DevBattleDefinition·DevMapWidth·DevMapHeight·DevPlayerDeploymentDepth·DevEnemyDeploymentDepth)는 읽지 않고 남는다.
    /// </remarks>
    public static class GameBootstrapEditorOverrides
    {
        private const string Prefix = "ProjectL.BootOverride.";

        /// <summary>덮어쓰기를 쓸지입니다. 기본값은 꺼짐(Boot 씬 값 사용)입니다.</summary>
        public static bool Enabled
        {
            get => EditorPrefs.GetBool(Prefix + nameof(Enabled), false);
            set => EditorPrefs.SetBool(Prefix + nameof(Enabled), value);
        }

        /// <summary>0 = Lobby, 1 = Stage, 2 = Combat(Build Settings 순서). 기본값은 Lobby입니다.</summary>
        public static int StartLifeCycle
        {
            get => EditorPrefs.GetInt(Prefix + nameof(StartLifeCycle), 0);
            set => EditorPrefs.SetInt(Prefix + nameof(StartLifeCycle), value);
        }

        /// <summary>Stage 시작에서 바로 열 챕터입니다. 기존 Stage 저장값은 기본 챕터 1로 이어지고, Combat의 DevChapterKey와 별도로 보관합니다.</summary>
        public static int StageChapterKey
        {
            get => EditorPrefs.GetInt(Prefix + nameof(StageChapterKey), 1);
            set => EditorPrefs.SetInt(Prefix + nameof(StageChapterKey), value);
        }

        // 2026-10-15에 시작 구간 번호를 옛 순서(0 = Combat, 1 = Stage, 2 = Lobby)에서 Build Settings 순서로 바꿨다.
        // 이 PC에 옛 순서로 저장된 값이 있으면 에디터를 열 때 한 번 바꿔 저장한다. 키 이름은 그대로 둔다.
        private const string StartOrderMigrated = Prefix + "StartLifeCycleBuildOrder";

        [InitializeOnLoadMethod]
        private static void MigrateStartLifeCycleOrder()
        {
            if (EditorPrefs.GetBool(StartOrderMigrated, false))
                return;
            string key = Prefix + nameof(StartLifeCycle);
            if (EditorPrefs.HasKey(key))
            {
                int old = EditorPrefs.GetInt(key);
                if (old == 0 || old == 2)
                    EditorPrefs.SetInt(key, 2 - old);
            }
            EditorPrefs.SetBool(StartOrderMigrated, true);
        }

        /// <summary>Combat 직접 시작의 새 Run·챕터 준비와 노드 진입을 쓸지입니다. 이전 키("UseDevBattle")를 그대로 써서 저장값을 잇습니다.</summary>
        public static bool UseDevProgress
        {
            get => EditorPrefs.GetBool(Prefix + "UseDevBattle", true);
            set => EditorPrefs.SetBool(Prefix + "UseDevBattle", value);
        }

        public static int DevLoadoutKey
        {
            get => EditorPrefs.GetInt(Prefix + nameof(DevLoadoutKey), 1);
            set => EditorPrefs.SetInt(Prefix + nameof(DevLoadoutKey), value);
        }

        public static int DevChapterKey
        {
            get => EditorPrefs.GetInt(Prefix + nameof(DevChapterKey), 1);
            set => EditorPrefs.SetInt(Prefix + nameof(DevChapterKey), value);
        }

        public static int DevNodeId
        {
            get => EditorPrefs.GetInt(Prefix + nameof(DevNodeId), 2);
            set => EditorPrefs.SetInt(Prefix + nameof(DevNodeId), value);
        }

        /// <summary>
        /// 0 = 진입 노드의 스테이지, 1 = 현재 맵의 보스 전투 스테이지 정의, 2 = <see cref="DevStageKey"/>의 전투 스테이지 정의,
        /// 3 = <see cref="DevStageData"/>를 Dev Node Id 노드에 놓음(1·2·3은 Database 정의 전용).
        /// </summary>
        public static int DevBattleStage
        {
            get => EditorPrefs.GetInt(Prefix + nameof(DevBattleStage), 0);
            set => EditorPrefs.SetInt(Prefix + nameof(DevBattleStage), value);
        }

        /// <summary>전투 스테이지가 지정 스테이지(2)일 때 읽을 전투 스테이지 키입니다. 기본값 0은 미지정이며, 그대로 재생하면 시작에서 멈춥니다.</summary>
        public static int DevStageKey
        {
            get => EditorPrefs.GetInt(Prefix + nameof(DevStageKey), 0);
            set => EditorPrefs.SetInt(Prefix + nameof(DevStageKey), value);
        }

        private const string DevStageDataGuidKey = Prefix + "DevStageDataGuid";

        /// <summary>
        /// 전투 스테이지가 지정 스테이지 배치(3)일 때 노드에 놓을 전투 스테이지입니다. 에셋 GUID로 저장하며, null이면 키를 지웁니다.
        /// 읽기는 저장된 에셋을 찾지 못하면 null입니다(설정 창 표시용). 재생 시작은 <see cref="TryReadDevStageData"/>로 실패를 구분합니다.
        /// </summary>
        public static CombatStageData DevStageData
        {
            get => TryReadDevStageData(out CombatStageData stage) ? stage : null;
            set
            {
                string path = value != null ? AssetDatabase.GetAssetPath(value) : string.Empty;
                if (string.IsNullOrEmpty(path))
                {
                    EditorPrefs.DeleteKey(DevStageDataGuidKey);
                    return;
                }
                EditorPrefs.SetString(DevStageDataGuidKey, AssetDatabase.AssetPathToGUID(path));
            }
        }

        /// <summary>저장값이 없으면 null로 true, 저장된 GUID의 에셋을 찾지 못하면 null로 false입니다.</summary>
        public static bool TryReadDevStageData(out CombatStageData stage)
        {
            string guid = EditorPrefs.GetString(DevStageDataGuidKey, string.Empty);
            if (string.IsNullOrEmpty(guid))
            {
                stage = null;
                return true;
            }
            string path = AssetDatabase.GUIDToAssetPath(guid);
            stage = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<CombatStageData>(path);
            return stage != null;
        }

        /// <summary>
        /// 새 Run 직후 지급할 개발 시작 아이템입니다(시작 아이템 설계 DI5). 키 하나에 JSON으로 담고, 키가 없으면 빈 목록입니다.
        /// 빈 목록을 쓰면 키를 지웁니다. 저장값을 읽지 못하면 재생 시작이 멈추도록 예외를 던집니다.
        /// </summary>
        public static DevStartingItem[] DevStartingItems
        {
            get
            {
                if (TryReadDevStartingItems(out DevStartingItem[] items))
                    return items;
                throw new InvalidOperationException(
                    "[Play From Boot] 시작 아이템 설정을 읽지 못했습니다. 설정 창에서 비우고 다시 넣으세요.");
            }
            set
            {
                if (value == null || value.Length == 0)
                {
                    EditorPrefs.DeleteKey(Prefix + nameof(DevStartingItems));
                    return;
                }
                EditorPrefs.SetString(Prefix + nameof(DevStartingItems),
                    JsonUtility.ToJson(new StoredItems { items = (DevStartingItem[])value.Clone() }));
            }
        }

        /// <summary>설정 창용 읽기입니다. 저장값을 읽지 못하면 false이고 목록은 비웁니다.</summary>
        public static bool TryReadDevStartingItems(out DevStartingItem[] items)
        {
            string json = EditorPrefs.GetString(Prefix + nameof(DevStartingItems), string.Empty);
            if (string.IsNullOrEmpty(json))
            {
                items = Array.Empty<DevStartingItem>();
                return true;
            }
            try
            {
                items = JsonUtility.FromJson<StoredItems>(json)?.items;
            }
            catch (ArgumentException)
            {
                items = null;
            }
            if (items != null)
                return true;
            items = Array.Empty<DevStartingItem>();
            return false;
        }

        // JsonUtility는 배열을 바로 읽지 못하므로 감싸서 저장한다.
        [Serializable]
        private sealed class StoredItems
        {
            public DevStartingItem[] items;
        }
    }
}
#endif
