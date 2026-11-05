using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 불러온 Combat 씬의 루트와 배경 맵입니다(View 3라운드 W11-3). 씬 로드(<see cref="SceneNodeOrder.CombatBackdrop"/>)에서 루트를 찾고 배경을 세우며,
    /// 씬 이탈·런타임 종료에서 배경을 놓습니다(배경 설계 BD1·BD2·BD4).
    /// </summary>
    /// <remarks>
    /// 씬 오브젝트를 찾는 유일한 자리입니다. Tick·입력 경로에서는 찾지 않습니다.
    /// 수명은 씬 수명이라, 전투 중간의 표시 분리(<c>Detach</c>)는 배경을 남깁니다(배경 설계 Q-B2 A). 조정자 한 곳만 씁니다.
    /// </remarks>
    internal sealed class CombatLoadedScene
    {
        private readonly ICombatSceneFactory _factory;
        private StageViewPropPresenter _backdrop;

        internal CombatLoadedScene(ICombatSceneFactory factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        /// <summary>씬 로드 때 찾은 루트입니다. 로드 전·해제 뒤에는 null입니다.</summary>
        internal CombatSceneRoot Root { get; private set; }

        /// <summary>
        /// 앞 씬 수명에서 놓지 못한 배경을 먼저 놓은 뒤, 씬 루트 하나를 찾아 배경을 세웁니다. 루트가 하나가 아니면 예외입니다.
        /// 선택이 없거나 키에 맞는 배경이 없으면 배경을 두지 않습니다(배경 표시의 규칙).
        /// </summary>
        /// <param name="values">배경을 고를 스테이지 키와 바닥 타일을 맞출 보드 크기를 읽는 곳입니다. 배경을 만든 뒤에 읽습니다(옛 조정자 순서).</param>
        internal void Load(CombatDisplayTables tables, CombatDisplayValueReader values)
        {
            if (tables == null)
                throw new ArgumentNullException(nameof(tables));
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            Release();

            CombatSceneRoot[] roots = UnityEngine.Object.FindObjectsByType<CombatSceneRoot>(UnityEngine.FindObjectsSortMode.None);
            if (roots.Length != 1)
                throw new InvalidOperationException(
                    $"[CombatViewSystem] Combat 씬에는 CombatSceneRoot가 하나 있어야 합니다. 찾은 수: {roots.Length}");

            Root = roots[0];
            _backdrop = _factory.CreateBackdrop(Root, tables);
            // 보드 크기는 바닥 타일을 필드 칸 경계에 맞출 때 씁니다. 전투 준비가 씬 전환보다 먼저라 이 시점에 읽힙니다(타일 맞춤 계획 P9).
            _backdrop.Show(values.CurrentStageKey, values.TryReadBoardSize(out UnityEngine.Vector2Int board) ? (UnityEngine.Vector2Int?)board : null);
        }

        /// <summary>
        /// 그 루트에 세운 배경입니다. 씬 로드 때 찾은 루트가 아니면(다른 루트로 직접 붙이는 시험 조립) 남의 배경 카메라를 쓰지 않게 null입니다.
        /// </summary>
        internal StageViewPropPresenter BackdropFor(CombatSceneRoot root) => ReferenceEquals(root, Root) ? _backdrop : null;

        /// <summary>배경을 없애고 루트를 놓습니다. 씬과 함께 이미 사라졌으면 아무것도 하지 않습니다. 여러 번 불려도 안전합니다.</summary>
        internal void Release()
        {
            StageViewPropPresenter backdrop = _backdrop;
            _backdrop = null;
            Root = null;
            backdrop?.Release();
        }
    }
}
