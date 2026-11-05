using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Effects
{
    /// <summary>
    /// 이펙트 하나의 받침입니다(이펙트 따라가기·회수 설계 FE2·FE6, 이펙트 끝 규칙 설계 EL5). 이펙트 창구(<see cref="CombatEffectWindow"/>)가 필드 루트 아래에 두고 빌려 주며,
    /// 창구가 내는 모든 이펙트가 이 받침의 자식으로 놓입니다(그 자리 이펙트도, 계획 TP-E30). 하는 일은 넷입니다: 유닛 몸 위치 따라가기(위치만, 결정 Q-FE2),
    /// 시간 끝(낸 뒤 정한 초에 거둠), 손잡이(<see cref="CombatEffectHandle"/>)로 거두기. 오래 보이는 이펙트의 "언제 거두나"는 유지 표시 부품(<see cref="CombatPersistentEffects"/>)이 정합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 매 프레임 끝(<see cref="LateUpdate"/>, 결과 재생이 유닛을 옮긴 뒤)에 몸 위치를 읽어 옮기고, 시간 끝을 셉니다. 결과 재생이 끝난 뒤에도 남은
    /// 이펙트를 다뤄야 해서 재생 Tick이 아니라 Unity 프레임 콜백을 씁니다(몸 글자 창구와 같은 까닭).
    /// </para>
    /// <para>
    /// 따라가던 유닛 표시가 사라지면(풀 반환·다른 유닛에 붙음) 이펙트를 바로 멈춥니다(결정 Q-FE3): 파티클을 지워 이펙트 시스템의 멈춤 알림으로 풀에 돌아가게 합니다.
    /// 같은 표시가 숨었을 뿐이면(배치 끌기에서 원래 유닛 숨김) 이펙트는 그대로 두고 그리기만 껐다가, 다시 보이면 켭니다(FE11).
    /// 회수는 자연스럽게 합니다(결정 Q-FE5): 방출만 멈추고(<see cref="ParticleStopNotifier.StopEmitting"/>) 남은 입자가 다 사라지면 같은 알림으로 풀에 돌아갑니다.
    /// </para>
    /// <para>
    /// 이펙트가 풀로 돌아가 자식이 없어지면 받침은 비어 다시 빌릴 수 있습니다(<see cref="IsFree"/>). 이펙트를 유닛 아래에 바로 붙이지 않는 까닭은
    /// 옛 결정과 같습니다(이펙트 설계 E-R1): 유닛 표시가 꺼지거나 파괴되면 이펙트가 같이 꺼지거나 파괴됩니다.
    /// </para>
    /// </remarks>
    public sealed class CombatEffectFollower : MonoBehaviour
    {
        private readonly List<Transform> _children = new(2);
        // 숨긴 동안 끈 그리기입니다(FE11). 다시 보이거나 이펙트가 풀로 돌아갈 때 켜 둡니다(숨긴 채 돌아가면 다음에 쓸 때 보이지 않음).
        private readonly List<Renderer> _hiddenRenderers = new(2);
        private bool _hidden;
        private Func<(bool Bound, Vector3? Body)> _body;
        private bool _released;
        // 빌릴 때마다 오르는 사용 번호입니다. 손잡이가 같은 이펙트인지 판단합니다(받침은 다시 쓰임).
        private int _use;
        // 시간 끝까지 남은 초입니다. 0이면 시간 끝이 아닙니다(계획 TP-E29).
        private float _seconds;
        // 이펙트에 링 버퍼 파티클이 있는지입니다. 창구가 종류마다 한 번 판단해 넘깁니다(계획 TP-E25).
        private bool _ringBuffer;

        /// <summary>이펙트가 없어(끝나 풀로 돌아감) 다시 빌릴 수 있는지입니다.</summary>
        internal bool IsFree => transform.childCount == 0;

        /// <summary>지금 이펙트의 손잡이입니다. <see cref="Begin"/> 뒤에 읽습니다.</summary>
        internal CombatEffectHandle Handle => new(this, _use);

        /// <summary>그 사용 번호의 이펙트가 아직 보이는지입니다(거두기·끝·멈춤 전).</summary>
        internal bool IsLive(int use) => use == _use && !_released && !IsFree;

        /// <summary>그 사용 번호의 이펙트를 거둡니다. 이미 끝났거나 받침이 다른 이펙트를 받았으면 아무것도 하지 않습니다.</summary>
        internal void Recall(int use)
        {
            if (IsLive(use))
                Recall();
        }

        /// <summary>같은 사용 번호의 잔여 입자까지 지웁니다. 방출만 멈춘 뒤에도 사용할 수 있습니다.</summary>
        internal void RecallImmediately(int use)
        {
            if (use == _use && !IsFree)
                StopNow();
        }

        /// <summary>
        /// 그 사용 번호의 이펙트가 살아 있으면 받침을 월드 위치로 옮깁니다(하붑 View 계획 HP-23). 지역(Local) 파티클은 입자도 함께 움직이고, 월드(World) 파티클은 입자가 뒤에 남습니다.
        /// 유닛을 따라가는 받침은 다음 <see cref="LateUpdate"/>가 몸 위치로 덮으므로, 따라가지 않는 이펙트에만 부릅니다.
        /// </summary>
        internal void MoveTo(int use, Vector3 position)
        {
            if (IsLive(use))
                transform.position = position;
        }

        /// <summary>
        /// 받침을 켭니다. <paramref name="body"/>가 있으면 그 몸 위치를 따라가고, 없으면 <paramref name="position"/>에 머뭅니다. 회전은 부모 기준 그대로입니다.
        /// </summary>
        /// <param name="body">
        /// 따라갈 몸 읽기입니다(<c>FieldUnitPresenter.TrackBodyPresence</c>). <c>Bound</c>가 거짓이면 유닛 표시가 사라진 것이고, <c>Body</c>가 null이면 숨었을 뿐입니다.
        /// 따라가지 않으면 null입니다.
        /// </param>
        /// <param name="position">받침을 둘 월드 위치입니다(따라가면 지금 몸 위치, 숨어 있으면 낸 자리).</param>
        /// <param name="seconds">0보다 크면 낸 뒤 이 초에 거둡니다(시간 끝). 0이면 시간 끝이 아닙니다.</param>
        internal void Begin(Func<(bool Bound, Vector3? Body)> body, Vector3 position, float seconds)
        {
            ShowRenderers();
            _use++;
            _seconds = seconds > 0f ? seconds : 0f;
            _ringBuffer = false;
            _body = body;
            _released = false;
            transform.localRotation = Quaternion.identity;
            transform.position = position;
            gameObject.SetActive(true);
        }

        /// <summary>낸 이펙트의 회수 모양을 정합니다. 창구가 이펙트를 이 받침에 놓은 바로 뒤에 종류마다 기억한 판단으로 부릅니다(계획 TP-E25).</summary>
        internal void Adopt(bool ringBuffer) => _ringBuffer = ringBuffer;

        private void LateUpdate()
        {
            if (IsFree)
            {
                // 이펙트가 숨은 채 풀로 돌아갔으면 그리기를 되돌립니다(숨긴 그리기는 끝난 이펙트에도 남아 있음).
                ShowRenderers();
                _body = null;
                _seconds = 0f;
                gameObject.SetActive(false);
                return;
            }
            if (_body != null)
            {
                (bool bound, Vector3? body) = _body();
                if (!bound)
                {
                    // 표시가 사라졌습니다(사망 뒤 제거·풀 반환). 바로 멈춥니다(결정 Q-FE3).
                    _body = null;
                    ShowRenderers();
                    StopNow();
                    return;
                }
                if (body.HasValue)
                {
                    transform.position = body.Value;
                    ShowRenderers();
                }
                else
                {
                    // 같은 표시가 숨었을 뿐입니다(배치 끌기 등). 이펙트는 그대로 두고 그리기만 끕니다(FE11).
                    HideRenderers();
                }
            }
            // 시간 끝입니다. 파티클과 같은 시계(크기 조정된 프레임 시간)로 셉니다(계획 TP-E29).
            if (_seconds > 0f && !_released)
            {
                _seconds -= Time.deltaTime;
                if (_seconds <= 0f)
                {
                    _seconds = 0f;
                    Recall();
                }
            }
        }

        // 자연스러운 회수입니다. 방출만 멈추고 남은 입자는 수명대로 사라집니다. 몸 위치 따라가기는 다 사라질 때까지 이어집니다.
        // 링 버퍼(Ring Buffer) 입자는 수명이 끝나도 죽지 않아(대체될 때까지 멈춰 남음) 방출만 멈추면 영원히 남으므로 바로 지웁니다(FE12).
        // 링 버퍼인지는 창구가 종류마다 한 번 판단해 넘긴 값입니다(Adopt, 계획 TP-E25).
        private void Recall()
        {
            _released = true;
            _seconds = 0f;
            foreach (Transform child in Children())
            {
                if (_ringBuffer && child.TryGetComponent(out ParticleSystem kept))
                    kept.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
                else if (child.TryGetComponent(out ParticleStopNotifier notifier))
                    notifier.StopEmitting();
                else if (child.TryGetComponent(out ParticleSystem particles))
                    particles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        // 바로 멈춤입니다. 파티클을 지워 이펙트 시스템의 멈춤 알림으로 풀에 돌아가게 합니다(설계 P3).
        private void StopNow()
        {
            _released = true;
            _seconds = 0f;
            foreach (Transform child in Children())
                if (child.TryGetComponent(out ParticleSystem particles))
                    particles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>숨기며 끈 그리기를 다시 켭니다. 숨긴 적이 없으면 아무것도 하지 않습니다. 창구가 이펙트를 모두 풀로 돌리기 전에도 부릅니다.</summary>
        internal void ShowRenderers()
        {
            _hidden = false;
            for (int i = 0; i < _hiddenRenderers.Count; i++)
                if (_hiddenRenderers[i] != null)
                    _hiddenRenderers[i].enabled = true;
            _hiddenRenderers.Clear();
        }

        // 자식 이펙트의 켜진 그리기를 끕니다(입자는 계속 돎). 이미 숨겼으면 그대로입니다.
        private void HideRenderers()
        {
            if (_hidden)
                return;
            _hidden = true;
            foreach (Transform child in Children())
                foreach (Renderer renderer in child.GetComponentsInChildren<Renderer>())
                    if (renderer.enabled)
                    {
                        renderer.enabled = false;
                        _hiddenRenderers.Add(renderer);
                    }
        }

        // 받침이 지워질 때(창구 해제) 숨긴 그리기를 되돌립니다. 이펙트는 그 전에 풀로 돌아가 있습니다.
        private void OnDestroy() => ShowRenderers();

        // 자식 목록입니다. 멈춤 알림이 이펙트를 풀로 돌리며 부모를 바꿀 수 있어 먼저 모아 둡니다.
        private List<Transform> Children()
        {
            _children.Clear();
            for (int i = 0; i < transform.childCount; i++)
                _children.Add(transform.GetChild(i));
            return _children;
        }
    }
}
