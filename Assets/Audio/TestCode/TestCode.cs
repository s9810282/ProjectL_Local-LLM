using UnityEngine;

using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;

[RequireComponent(typeof(FmodEventPlayer))]
public sealed class TestCode : ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem.AudioBase
{
    [SerializeField] private AudioPlayerId playerId;
    [SerializeField] private string slotName = "Event Slot 0";
    [SerializeField] private bool playOnStart = true;

    protected override void OnAudioServiceReady()
    {
        if (playOnStart)
            PlayConfiguredEvent();
    }

    [ContextMenu("Play Configured Event")]
    public void PlayConfiguredEvent()
    {
        if (AudioService == null)
        {
            Debug.LogError(
                "[Audio TestCode] AudioService가 바인딩되지 않았습니다. " +
                "프레임워크 초기화 후에 호출하세요.",
                this);
            return;
        }

        if (!playerId.IsValid || string.IsNullOrWhiteSpace(slotName))
        {
            Debug.LogError(
                "[Audio TestCode] Player ID와 Slot Name을 모두 설정하세요.",
                this);
            return;
        }

        if (!AudioService.Play(playerId, gameObject, slotName))
        {
            Debug.LogWarning(
                $"[Audio TestCode] 재생 실패: Player ID '{playerId}', Slot '{slotName}'.",
                this);
        }
    }
}
