using System.Collections;
using UnityEngine;

/// <summary>
/// 동일 Player ID를 가진 FmodEventPlayer 프리팹의 중복 등록을 확인하기 위한 테스트입니다.
/// </summary>
public sealed class TestScript : ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem.AudioBase
{
    [Header("Duplicate Player ID Test")]
    [SerializeField] private FmodEventPlayer eventPlayerPrefab;
    [SerializeField] private int slotIndex;
    [SerializeField] private int secondPresetIndex = 1;
    [SerializeField] private Vector3 firstInstancePosition = new(-1f, 0f, 0f);
    [SerializeField] private Vector3 secondInstancePosition = new(1f, 0f, 0f);

    private FmodEventPlayer firstInstance;
    private FmodEventPlayer secondInstance;

    protected override void OnAudioServiceReady()
    {
        SpawnTwoPlayersAndPlayFirst();
    }

    [ContextMenu("Spawn Two Players And Play First")]
    public void SpawnTwoPlayersAndPlayFirst()
    {
        if (eventPlayerPrefab == null)
        {
            Debug.LogError(
                "[Duplicate Player ID Test] FmodEventPlayer 프리팹을 지정하세요.",
                this);
            return;
        }

        if (firstInstance != null || secondInstance != null)
        {
            Debug.LogWarning(
                "[Duplicate Player ID Test] 테스트 인스턴스가 이미 생성되어 있습니다.",
                this);
            return;
        }

        firstInstance = Instantiate(
            eventPlayerPrefab,
            firstInstancePosition,
            Quaternion.identity);
        firstInstance.name = "Duplicate Player ID Test - First";

        secondInstance = Instantiate(
            eventPlayerPrefab,
            secondInstancePosition,
            Quaternion.identity);
        secondInstance.name = "Duplicate Player ID Test - Second";

        if (!secondInstance.SetCurrentPresetIndex(slotIndex, secondPresetIndex))
        {
            Debug.LogWarning(
                $"[Duplicate Player ID Test] 두 번째 Player의 슬롯 {slotIndex} " +
                $"프리셋 {secondPresetIndex} 선택에 실패했습니다.",
                secondInstance);
            return;
        }

        if (AudioService == null)
        {
            Debug.LogError(
                "[Duplicate Player ID Test] AudioService가 바인딩되지 않았습니다.",
                this);
            return;
        }

        if (!AudioService.Play(
                firstInstance.PlayerId,
                firstInstance.gameObject,
                slotIndex))
        {
            Debug.LogWarning(
                $"[Duplicate Player ID Test] 첫 번째 Player의 슬롯 {slotIndex} 재생에 실패했습니다.",
                firstInstance);
        }

        StartCoroutine(PlaySecondAfterDelay());
    }

    private IEnumerator PlaySecondAfterDelay()
    {
        yield return new WaitForSeconds(1f);

        if (secondInstance == null || AudioService == null)
        {
            Debug.LogWarning(
                "[Duplicate Player ID Test] 두 번째 Player를 재생할 수 없습니다.",
                this);
            yield break;
        }

        if (!AudioService.Play(
                secondInstance.PlayerId,
                secondInstance.gameObject,
                slotIndex))
        {
            Debug.LogWarning(
                $"[Duplicate Player ID Test] 두 번째 Player의 슬롯 {slotIndex} 재생에 실패했습니다.",
                secondInstance);
        }
    }
}
