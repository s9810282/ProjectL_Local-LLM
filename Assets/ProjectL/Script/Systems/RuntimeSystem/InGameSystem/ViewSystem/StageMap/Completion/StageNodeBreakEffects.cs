using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Completion
{
    /// <summary>System 수명의 전용 EffectScope를 재사용하고 재생별 단위 스케일 받침과 세대를 소유합니다.</summary>
    internal sealed class StageNodeBreakEffects
    {
        private readonly IEffectSystemServices _effects;
        private EffectScope _scope;
        private Transform _anchor;
        private Transform _sceneOwner;
        private long _nextGeneration;
        private long _generation;
        private bool _playing;
        private bool _ending;
        private bool _endRequested;

        internal StageNodeBreakEffects(IEffectSystemServices effects)
        {
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        internal long Begin(EffectType type, Transform sceneOwner, Pose pose)
        {
            if (sceneOwner == null) throw new ArgumentNullException(nameof(sceneOwner));
            if (!sceneOwner.gameObject.scene.IsValid() || !sceneOwner.gameObject.scene.isLoaded)
                throw new InvalidOperationException("[StageNodeBreakEffects] 로드된 씬의 소유자가 필요합니다.");
            if (type == EffectType.Empty || !System.Enum.IsDefined(typeof(EffectType), type))
                throw new ArgumentOutOfRangeException(nameof(type), "정의된 효과 키가 필요합니다.");
            float rotationLength = pose.rotation.x * pose.rotation.x + pose.rotation.y * pose.rotation.y +
                pose.rotation.z * pose.rotation.z + pose.rotation.w * pose.rotation.w;
            if (!Finite(pose.position) || !float.IsFinite(rotationLength) || Mathf.Abs(rotationLength - 1f) > 0.001f)
                throw new InvalidOperationException("[StageNodeBreakEffects] 유한한 월드 위치와 단위 회전이 필요합니다.");
            if (_generation != 0 || _playing || _ending)
                throw new InvalidOperationException("[StageNodeBreakEffects] 이전 효과를 End로 반환한 뒤 시작해야 합니다.");

            _scope ??= _effects.GetEffectHandle() ??
                throw new InvalidOperationException("[StageNodeBreakEffects] 효과 Scope를 받지 못했습니다.");
            long generation = checked(++_nextGeneration);
            _generation = generation;
            _sceneOwner = sceneOwner;
            try
            {
                _anchor = new GameObject("StageNodeBreakAnchor").transform;
                // 건물 숨김이나 맵 스케일을 상속하지 않도록 같은 소유 씬의 독립 루트에 둡니다.
                SceneManager.MoveGameObjectToScene(_anchor.gameObject, sceneOwner.gameObject.scene);
                _anchor.SetPositionAndRotation(pose.position, pose.rotation);
                _anchor.localScale = Vector3.one;
                _playing = true;
                try
                {
                    _scope.Play(new EffectScopeRequest(type, _anchor, Vector3.zero, Quaternion.identity));
                }
                finally
                {
                    _playing = false;
                }
                // 활성화 콜백 안에서 취소됐어도 Play의 내부 등록이 끝난 뒤 반환합니다.
                // Play 실패는 아래 catch에서 원래 오류와 정리 오류를 함께 보존합니다.
                if (_endRequested) End(generation);
                return generation;
            }
            catch (Exception error)
            {
                List<Exception> errors = new() { error };
                CleanupErrors.Try(() => End(generation), ref errors);
                CleanupErrors.ThrowIfAny(errors, "[StageNodeBreakEffects] 파괴 효과 시작/정리에 실패했습니다.");
                throw;
            }
        }

        internal StageNodeBreakStatus GetStatus(long generation)
        {
            if (generation <= 0 || generation != _generation || _endRequested || _ending)
                return StageNodeBreakStatus.Cancelled;
            if (_playing) return StageNodeBreakStatus.Playing;
            if (_sceneOwner == null || _anchor == null)
                throw new InvalidOperationException("[StageNodeBreakEffects] 반환 전에 씬 소유자 또는 파괴 받침이 사라졌습니다.");
            // 루트 반환이 모든 파편의 종료 뒤라는 Variant 계약을 사용하며 시간을 복제하지 않습니다.
            return _anchor.childCount == 0 ? StageNodeBreakStatus.Completed : StageNodeBreakStatus.Playing;
        }

        /// <summary>자연 종료에도 호출합니다. 재진입 취소는 유예하고, 풀 반환 확인 후 받침을 정리합니다.</summary>
        internal void End(long generation)
        {
            if (generation <= 0 || generation != _generation || _ending) return;
            _endRequested = true;
            if (_playing) return;
            _ending = true;
            try
            {
                _scope.StopAll();
                if (_anchor == null)
                    throw new InvalidOperationException("[StageNodeBreakEffects] 반환 전에 파괴 받침이 사라졌습니다.");
                if (_anchor.childCount != 0)
                    throw new InvalidOperationException("[StageNodeBreakEffects] 풀 반환 전에 파괴 받침을 해제할 수 없습니다.");
                Transform owned = _anchor;
                _anchor = null;
                _sceneOwner = null;
                _generation = 0;
                _endRequested = false;
                Object.Destroy(owned.gameObject);
            }
            finally
            {
                _ending = false;
            }
        }

        private static bool Finite(Vector3 value) =>
            float.IsFinite(value.x) && float.IsFinite(value.y) && float.IsFinite(value.z);
    }
}
