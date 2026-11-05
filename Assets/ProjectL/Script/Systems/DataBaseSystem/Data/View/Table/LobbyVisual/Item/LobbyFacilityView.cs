using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item
{
    /// <summary>
    /// 로비 방 프리팹에서 시설 종류와 같은 객체의 클릭 영역(BoxCollider)을 표시합니다(검사 후속 정리 TP-R13).
    /// 구성 검사는 DB 생성 때 원본 프리팹에 한 번 합니다(<see cref="LobbyVisualPrefabRules.ValidateRoom"/>).
    /// 복제본은 같은 구성이라 View는 다시 검사하지 않습니다. 프리팹에 붙는 작업 입력만 둡니다. 클릭 영역 캐시는 View의 시설 고르기가 갖습니다(로비 리팩토링 2차 설계 N-59).
    /// </summary>
    [DisallowMultipleComponent]
    [MovedFrom(true, sourceNamespace: "ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby", sourceAssembly: "ProjectL.Systems", sourceClassName: "LobbyFacilityView")]
    public sealed class LobbyFacilityView : MonoBehaviour
    {
        [SerializeField] private LobbyFacilityKind kind;
        [Tooltip("호버 때 반짝일 이 시설의 안내 글자입니다. 같은 방 안의 TextMeshProUGUI를 연결합니다.")]
        [SerializeField] private TextMeshProUGUI label;

        public LobbyFacilityKind Kind => kind;

        /// <summary>이 시설의 안내 글자입니다. 호버 반짝임이 씁니다(로비 설계 §16).</summary>
        public TextMeshProUGUI Label => label;
    }
}
