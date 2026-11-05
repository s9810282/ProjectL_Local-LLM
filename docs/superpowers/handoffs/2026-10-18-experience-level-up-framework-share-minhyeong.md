# 경험치·레벨업 Framework 공유 (민형님)

- 작성: 2026-10-18, Core 작업자(천명). **미전달.**
- 설계: 경험치·레벨업 설계 (Git history: `2026-10-18-experience-level-up-design.md`), 실행 기록: 계획 §5 (Git history: `2026-10-18-experience-level-up-plan.md`)

## 1. 공용 조립 변경

없다. Factory·서비스 키·`IInGameQuery`·Group·asmdef·Buffer 읽기 인터페이스는 바꾸지 않았다. 경험치 수치는 기존 설정형 테이블 `CategoryConfig`에 "Level Progression" 묶음으로 넣었다(설계 Q6, 사용자 결정 X6).

| 파일 | 변경 |
|---|---|
| `DataBaseSystem/Data/InGame/Table/Category/Item/CategoryConfigData.cs` | `LevelProgression`(`LevelProgressionSpec`) 속성·생성자 선택 인자 |
| `…/CategoryConfigDataSO.cs` | `levelProgression` 직렬화 묶음(요구량 단계 메인·서브, 레벨당 처치 경험치, 메인 성장량 3개). 처치 경험치 0 = 꺼짐, 처치만 0이고 다른 값이 있으면 `GetData()` 오류 |
| `DataBaseSO/InGameGroup/Category/Item/CategoryConfig_Default.asset` | 150/100/10/3/1/1 |

`TemporaryCoreDatabase`의 `CategoryConfigData(50)`은 그대로라 개발 DB에서는 경험치가 꺼져 있다.

## 2. 원본 일원화 제기 (E6)

Excel `CharacterMaster`의 `Character_NeedExperiencePoint`·`Character_KillExperiencePoint`와 `UpgradeStatisticMaster`의 `NeedExperiencePoint`·`KillExperiencePoint`·`KillGold`는 런타임에 등록되지 않았고, 이번 DB 설정과 같은 사실을 담을 수 있는 열이다. 지금 원본은 `CategoryConfig_Default.asset` 하나다. Excel 흐름으로 옮길지, Excel 열을 정리할지 기획과 함께 정해 주시면 Core 어댑터를 그에 맞춘다.
