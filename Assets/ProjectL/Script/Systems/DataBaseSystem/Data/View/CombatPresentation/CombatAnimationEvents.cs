using System;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 전투 클립의 애니메이션 이벤트 이름표이자, 전투 재생이 맡을 Animator의 자리 표시입니다(클립 설계 AC2, 계획 P1 A).
    /// 클립마다 시작·첫 이벤트·끝을 하나씩 찍고, 필요할 때만 <see cref="ClipEvent2"/>·<see cref="ClipEvent3"/>를 더합니다.
    /// 시각은 DB가 클립에서 읽고(<see cref="CombatClipEvents.ReadTimings"/>) 전투 재생이 그 시각에 연출을 시작합니다.
    /// </summary>
    /// <remarks>
    /// <para>이벤트 함수 이름의 원본은 이 클래스 하나입니다. 다른 곳은 <c>nameof</c>로만 씁니다.</para>
    /// <para>
    /// DataBaseSystem의 첫 MonoBehaviour입니다(평가 G5). 프리팹에 붙는 작업 입력(이름표·자리 표시 — 이벤트 함수, 전투 재생이 맡을 Animator,
    /// 이벤트 지점 목록)과 그 규칙 함수(<see cref="FindIn"/>)만 둡니다.
    /// 런타임 상태·<c>Update</c>·서비스 참조는 두지 않습니다(잘못 불렸을 때 한 번만 알리는 표시는 예외).
    /// </para>
    /// </remarks>
    [DisallowMultipleComponent, RequireComponent(typeof(Animator))]
    internal sealed class CombatAnimationEvents : MonoBehaviour
    {
        [Tooltip("이벤트 지점 목록입니다(클립 칸 다시 짜기 설계 CS4). 번호 = 목록 칸입니다. 원거리 클립의 이벤트 줄이 발사 지점으로, 공격 이펙트 큐의 ActorEventPoint가 이펙트 자리로 번호를 고릅니다. "
            + "한 타이밍에 지점 둘(발사 하나, 이펙트 하나)을 각각 쓸 수 있습니다. 쓰는 번호의 칸은 비면 안 되고 이 프리팹 안 Transform이어야 합니다(외형 표 검사).")]
        [SerializeField] private Transform[] eventPoints = Array.Empty<Transform>();

        private bool _reportedCall;

        /// <summary>시작 이벤트의 이름표입니다. 애니메이션 창에서 고르는 이름표이며 실행 중에는 불리지 않습니다(Animator 이벤트를 끔, 설계 AC2).</summary>
        public void ClipStart() => ReportCall();

        /// <summary>이벤트 타이밍(연출 기준 시점)의 이름표입니다. 애니메이션 창에서 고르는 이름표이며 실행 중에는 불리지 않습니다(Animator 이벤트를 끔, 설계 AC2).</summary>
        public void ClipEvent() => ReportCall();

        /// <summary>선택 두 번째 연출 시점의 이름표입니다. 실제 동작은 전투 재생의 조립이 정합니다.</summary>
        public void ClipEvent2() => ReportCall();

        /// <summary>선택 세 번째 연출 시점의 이름표입니다. 두 번째 시점 없이 사용할 수도 있습니다.</summary>
        public void ClipEvent3() => ReportCall();

        /// <summary>끝 이벤트의 이름표입니다. 이 시각이 재생 길이입니다. 애니메이션 창에서 고르는 이름표이며 실행 중에는 불리지 않습니다(Animator 이벤트를 끔, 설계 AC2).</summary>
        public void ClipEnd() => ReportCall();

        /// <summary>같은 GameObject의 Animator입니다. 전투 재생은 이 Animator만 맡습니다.</summary>
        internal Animator Animator => GetComponent<Animator>();

        /// <summary>입력 목록을 복사해 연결 동안 사용할 이벤트 지점을 만듭니다. 배열/번호 해석은 이 값에 감춥니다.</summary>
        internal CombatEventPoints CaptureEventPoints() => new(eventPoints);

        /// <summary>
        /// <paramref name="root"/> 아래(비활성 포함)에서 이 컴포넌트를 찾습니다. 없으면 null입니다.
        /// "하나만, Animator와 같은 GameObject에" 규칙을 실패시키는 검사는 이 함수에만 있습니다. DB 검사와 표시 준비가 함께 부릅니다(평가 G2).
        /// 클래스에 붙은 <see cref="DisallowMultipleComponent"/>·<see cref="RequireComponent"/>는 편집을 돕는 장치일 뿐입니다(다른 자식에 둘째를 붙이는 것은 막지 못함).
        /// </summary>
        /// <exception cref="InvalidOperationException">둘 이상이거나 같은 GameObject에 Animator가 없을 때입니다.</exception>
        internal static CombatAnimationEvents FindIn(GameObject root)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            CombatAnimationEvents[] found = root.GetComponentsInChildren<CombatAnimationEvents>(true);
            if (found.Length == 0)
                return null;
            if (found.Length > 1)
                throw new InvalidOperationException(
                    $"[CombatAnimationEvents] 전투 클립 이벤트 컴포넌트는 하나만 둡니다({found.Length}개): {root.name}");
            if (found[0].GetComponent<Animator>() == null)
                throw new InvalidOperationException(
                    $"[CombatAnimationEvents] 전투 클립 이벤트 컴포넌트와 같은 GameObject에 Animator가 없습니다: {root.name}");
            return found[0];
        }

        private void ReportCall()
        {
            if (_reportedCall)
                return;
            _reportedCall = true;
            Debug.LogError($"[CombatAnimationEvents] 전투 클립 이벤트는 전투 재생기가 부릅니다. Animator 이벤트가 켜져 있습니다: {gameObject.name}");
        }
    }
}
