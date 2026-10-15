#if UNITY_EDITOR
using UnityEditor;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot
{
    /// <summary>
    /// 에디터에서만 쓰는 GameBootstrap 설정 덮어쓰기입니다. 이 PC의 EditorPrefs에만 저장됩니다.
    /// 메뉴 <c>ProjectL &gt; Play From Boot 설정</c> 창이 쓰고, GameBootstrap이 재생 시작 때 읽습니다.
    /// </summary>
    /// <remarks>
    /// Boot 씬에 저장된 값은 공용 기본값으로 그대로 두고, 켜져 있을 때만 이번 재생의 인스턴스 값을 바꿉니다.
    /// 빌드에는 들어가지 않습니다. 열거형은 에디터 어셈블리에서 쓸 수 있도록 정수로 저장합니다
    /// (시작 구간 0 = Combat, 1 = Stage / 실행 모드 0 = Live, 1 = RecordInputOnly / 전투 정의 0 = Fixture, 1 = Database).
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

        /// <summary>0 = Combat, 1 = Stage.</summary>
        public static int StartLifeCycle
        {
            get => EditorPrefs.GetInt(Prefix + nameof(StartLifeCycle), 0);
            set => EditorPrefs.SetInt(Prefix + nameof(StartLifeCycle), value);
        }

        /// <summary>0 = Live, 1 = RecordInputOnly.</summary>
        public static int ExecutionMode
        {
            get => EditorPrefs.GetInt(Prefix + nameof(ExecutionMode), 0);
            set => EditorPrefs.SetInt(Prefix + nameof(ExecutionMode), value);
        }

        /// <summary>Combat 직접 시작의 새 Run·챕터 준비와 노드 진입을 쓸지입니다. 이전 키("UseDevBattle")를 그대로 써서 저장값을 잇습니다.</summary>
        public static bool UseDevProgress
        {
            get => EditorPrefs.GetBool(Prefix + "UseDevBattle", true);
            set => EditorPrefs.SetBool(Prefix + "UseDevBattle", value);
        }

        /// <summary>0 = Fixture(개발 고정 정의), 1 = Database(운영과 같은 DB 정의).</summary>
        public static int DevBattleDefinition
        {
            get => EditorPrefs.GetInt(Prefix + nameof(DevBattleDefinition), 0);
            set => EditorPrefs.SetInt(Prefix + nameof(DevBattleDefinition), value);
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

        public static int DevMapWidth
        {
            get => EditorPrefs.GetInt(Prefix + nameof(DevMapWidth), DevBattleInitializationSource.DefaultMapWidth);
            set => EditorPrefs.SetInt(Prefix + nameof(DevMapWidth), value);
        }

        public static int DevMapHeight
        {
            get => EditorPrefs.GetInt(Prefix + nameof(DevMapHeight), DevBattleInitializationSource.DefaultMapHeight);
            set => EditorPrefs.SetInt(Prefix + nameof(DevMapHeight), value);
        }

        public static int DevPlayerDeploymentDepth
        {
            get => EditorPrefs.GetInt(Prefix + nameof(DevPlayerDeploymentDepth), DevBattleInitializationSource.DefaultPlayerDeploymentDepth);
            set => EditorPrefs.SetInt(Prefix + nameof(DevPlayerDeploymentDepth), value);
        }

        public static int DevEnemyDeploymentDepth
        {
            get => EditorPrefs.GetInt(Prefix + nameof(DevEnemyDeploymentDepth), DevBattleInitializationSource.DefaultEnemyDeploymentDepth);
            set => EditorPrefs.SetInt(Prefix + nameof(DevEnemyDeploymentDepth), value);
        }
    }
}
#endif
