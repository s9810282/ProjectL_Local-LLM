# Project_L
청강대 졸업작품 ANTI-LIKE팀 프로젝트 입니다.

## 개발 문서

- [현재 폴더·네임스페이스 구조](Assets/ProjectL/Script/README.md)
- [Framework / Systems 구조와 실행 흐름](Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md)
- [개발 가이드와 API 계약](Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)
- [시스템 제작·등록·조회 예제](docs/superpowers/framework/03_System_Creation_Registration_Query.md)
- [개발 지침](docs/superpowers/framework/Framework%20Development%20Guidelines.md)

구조와 API의 기준은 현재 코드와 위 개발 문서다. 날짜가 붙은 계획·설계 문서는 당시 결정과 검증 기록이므로 현재 구현과 대조해서 읽는다.

2026-10-06에 `docs/superpowers/plans/Old`와 Framework/Buffer 전환 전의 옛 설계 문서를 정리했다. 삭제된 원문은 Git 커밋 `606fdf4c`에서 확인할 수 있다. 남은 문서의 `Git 이력` 표시는 과거 근거를 뜻하며 현재 구현 지시가 아니다.

## Core design and plan retirement - 2026-10-24

Core designs and plans dated before 2026-10-23, together with their companion execution records, were removed after confirming that their original contents are preserved in Git commit `d73fb5c6192cb2ea7e0bb9bd15b0b9509f84658c`. View, Flow and shared Framework documents are retained. References marked `Git history` identify historical evidence, not current implementation instructions.

Recover a removed document with `git show d73fb5c6192cb2ea7e0bb9bd15b0b9509f84658c:docs/superpowers/<specs-or-plans>/<filename>`.

The replacement combat documents remain in the working tree: `docs/superpowers/specs/2026-10-23-player-turn-shared-actioncost-design.md` and `docs/superpowers/plans/2026-10-24-player-turn-shared-actioncost-implementation-plan.md`. This cleanup does not implement the migration; new untracked design, plan and code files are excluded from the cleanup commit.
