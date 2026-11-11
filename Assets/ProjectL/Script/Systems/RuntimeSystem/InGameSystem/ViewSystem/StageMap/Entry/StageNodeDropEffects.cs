using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Entry
{
    /// <summary>시스템 수명의 효과 Scope 하나를 재사용하고, 현재 세대의 고정 착지 받침만 소유합니다.</summary>
    internal sealed class StageNodeDropEffects
    {
        private readonly IEffectSystemServices _effects;
        private readonly List<Transform> _anchors = new();
        private EffectScope _scope;
        private Transform _parent;
        private long _nextGeneration;
        private long _generation;
        private bool _playing;
        private bool _ending;
        private bool _endRequested;

        internal StageNodeDropEffects(IEffectSystemServices effects)
        {
            _effects = effects ?? throw new ArgumentNullException(nameof(effects));
        }

        internal long Begin(Transform parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            if (!parent.gameObject.scene.IsValid() || !parent.gameObject.scene.isLoaded)
                throw new InvalidOperationException("[StageNodeDropEffects] 로드된 씬의 소유자가 필요합니다.");
            if (_generation != 0)
                throw new InvalidOperationException("[StageNodeDropEffects] 이전 효과 소유를 먼저 반환해야 합니다.");
            _scope ??= _effects.GetEffectHandle() ??
                throw new InvalidOperationException("[StageNodeDropEffects] 효과 Scope를 받지 못했습니다.");
            _generation = checked(++_nextGeneration);
            _parent = parent;
            return _generation;
        }

        internal void Play(long generation, Pose groundPose)
        {
            if (generation <= 0 || generation != _generation || _endRequested || _ending) return;
            if (_playing || _parent == null)
                throw new InvalidOperationException("[StageNodeDropEffects] 유효한 부모와 단일 효과 발행이 필요합니다.");
            Transform anchor = new GameObject("StageNodeDropImpact").transform;
            _anchors.Add(anchor);
            // 맵의 비균일 스케일/회전을 상속하면 풀 반환 뒤에도 파티클 localScale 보정이 남습니다.
            // 소유 씬에 독립 루트로 두어 고정 월드 자세와 단위 스케일을 함께 보장합니다.
            SceneManager.MoveGameObjectToScene(anchor.gameObject, _parent.gameObject.scene);
            anchor.SetPositionAndRotation(groundPose.position, groundPose.rotation);
            anchor.localScale = Vector3.one;
            _playing = true;
            try
            {
                _scope.Play(new EffectScopeRequest(EffectType.FallingRocks, anchor,
                    anchor.InverseTransformPoint(groundPose.position),
                    Quaternion.Inverse(anchor.rotation) * groundPose.rotation));
            }
            finally
            {
                _playing = false;
                // Play 안의 활성화 콜백에서 취소돼도 Scope가 등록을 마친 뒤 반환합니다.
                if (_endRequested) End(generation);
            }
        }

        internal bool IsComplete(long generation)
        {
            if (generation <= 0 || generation != _generation) return true;
            if (_playing || _ending || _endRequested) return false;
            foreach (Transform anchor in _anchors)
            {
                if (anchor == null)
                    throw new InvalidOperationException("[StageNodeDropEffects] 반환 전에 착지 받침이 사라졌습니다.");
                if (anchor.childCount != 0) return false;
            }
            return true;
        }

        internal void End(long generation)
        {
            if (generation <= 0 || generation != _generation || _ending) return;
            _endRequested = true;
            if (_playing) return;
            _ending = true;
            try
            {
                _scope.StopAll();
                foreach (Transform anchor in _anchors)
                {
                    if (anchor != null && anchor.childCount != 0)
                        throw new InvalidOperationException("[StageNodeDropEffects] 풀 반환 전에 받침을 파괴할 수 없습니다.");
                }
                Transform[] owned = _anchors.ToArray();
                _anchors.Clear();
                _parent = null;
                _generation = 0;
                _endRequested = false;
                foreach (Transform anchor in owned)
                    if (anchor != null) Object.Destroy(anchor.gameObject);
            }
            finally
            {
                _ending = false;
            }
        }
    }
}
