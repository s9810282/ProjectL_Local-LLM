using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>곡사 투사체(포탄) 부품입니다(범용 연출 구조 설계 G4). 이 부품을 참조한 공격 묶음은 곡사(<see cref="CombatAttackPresentationKind.Mortar"/>)입니다.</summary>
    [CreateAssetMenu(fileName = "CombatProjectile", menuName = "ProjectL/Database/View/Combat Projectile (Arc)")]
    internal sealed class CombatArcProjectileSO : CombatProjectileSO
    {
        [SerializeField, Min(0.01f)] private float flightDuration = 0.8f;
        [Tooltip("필드 셀 폭을 기준으로 한 곡사 높이입니다.")]
        [SerializeField, Min(0f)] private float arcHeightInCells = 2f;
        [Tooltip("Spin은 기존 XYZ 회전, Follow Path는 화살촉이 곡선의 진행 방향을 향합니다. 프리팹 Pivot은 경로 위에 유지합니다.")]
        [SerializeField] private CombatArcRotationMode rotationMode = CombatArcRotationMode.Spin;
        [Tooltip("Spin 모드에서만 사용하는 초당 회전입니다. Follow Path에서는 적용하지 않습니다.")]
        [SerializeField] private Vector3 spinDegreesPerSecond = new(60f, 30f, 15f);

        internal override CombatAttackPresentationKind Kind => CombatAttackPresentationKind.Mortar;
        internal float FlightDuration => flightDuration;
        internal float ArcHeightInCells => arcHeightInCells;
        internal CombatArcRotationMode RotationMode => rotationMode;
        internal Vector3 SpinDegreesPerSecond => spinDegreesPerSecond;
    }
}
