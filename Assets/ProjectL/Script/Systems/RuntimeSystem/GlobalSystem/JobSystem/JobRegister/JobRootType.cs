namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRegister
{
    public enum JobRootType
    {
        Lobby = 0,
        Stage = 1,
        // 2는 옛 Artifact였다. 아티팩트 편집·프리뷰는 로비 Plan이 Next 안에서 판단해 만들어 따로 거는 열쇠가 필요 없어 지웠다(검사 후속 정리 R-f J7).
        Combat = 3,
        // 4·5는 옛 Shop·Recovery, 7은 옛 TraitGrowth였다. Stage Plan이 Next 안에서 상점·회복·증강을 판단해 만들므로
        // 따로 거는 열쇠가 필요 없어 지웠다. 다른 값의 번호는 그대로 둔다(검사 후속 정리 계획 Q-Rd3).
        /// <summary>튜토리얼의 다음 한 걸음입니다. <see cref="JobType.Next"/>만 받습니다(튜토리얼 설계 §9.1).</summary>
        Tutorial = 6,
    }

    public enum JobType
    {
        None = 0,
        Start = 1,
        // 2는 옛 NewRun(Stage 새 Run 진입)이었다. Stage Plan이 Start 안에서 로스터를 보고 새 Run을 붙이므로 지웠다(검사 후속 정리 계획 TP-R8).
        // 3·4는 옛 ProcessCommand·ProcessPreview(아티팩트 Plan 전용)였다. 로비 Plan이 Next 안에서 만들므로 지웠다(검사 후속 정리 R-f J7).
        // 5는 옛 AdvanceCombat(전투 자동 진행)이었다. 전투 Plan이 Next 안에서 판단해 만들므로 등록 값이 필요 없어 지웠다.
        // 다른 값의 번호는 그대로 둔다(전투 개선 설계 §7.2 E3).
        /// <summary>Plan이 지금 사실을 읽어 다음 작업을 정한다. 할 일이 없으면 Job을 만들지 않는다.</summary>
        Next = 6,
        /// <summary>구간을 나간 뒤의 사후 확인이다.</summary>
        ExitCheck = 7,
        /// <summary>전투 전용: 착지를 기다리던 현재 진입 요청의 전환을 다시 시도한다(전투 개선 설계 §6.1).</summary>
        EntryTransition = 8,
        /// <summary>전투 전용: 진입 서비스가 막 받은 현재 요청으로 준비·전환을 등록한다(골격 점검 설계 K-2).</summary>
        Entry = 9,
    }
}
