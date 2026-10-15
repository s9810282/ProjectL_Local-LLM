using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using UnityEngine.InputSystem;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.UnityBridge
{
    /// <summary>
    /// 등록된 Layer에 따라 복제 액션 에셋의 ActionMap을 켜고 끕니다.
    /// Layer가 없는 상태(CurrentLayer == default, 모든 맵 꺼짐)로 시작하며, ClearLayer로 다시 이 상태가 됩니다.
    /// 맵을 끌 때 생기는 Canceled 콜백은 이전 Layer가 유지된 상태에서 전달됩니다.
    /// </summary>
    internal sealed class UnityInputLayerRuntime : IInputLayerRuntime, IDisposable
    {
        private static readonly InputActionMap[] EmptyMaps = Array.Empty<InputActionMap>();

        private readonly Dictionary<string, LayerEntry> _entries = new(StringComparer.Ordinal);

        private bool _isResumed;
        private bool _isTransitioning;
        private bool _hasPendingRequest;
        private bool _isDisposed;

        /// <summary>전환 중에 들어온 마지막 요청입니다. default는 ClearLayer 요청입니다.</summary>
        private InputLayer _pendingLayer;

        public InputLayer CurrentLayer { get; private set; }

        internal UnityInputLayerRuntime(InputActionAsset actions, IReadOnlyList<InputLayer> layers)
        {
            if (actions == null)
                throw new ArgumentNullException(nameof(actions));
            if (layers == null)
                throw new ArgumentNullException(nameof(layers));
            if (layers.Count == 0)
                throw new ArgumentException("At least one Input Layer is required.", nameof(layers));

            for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
            {
                InputLayer layer = layers[layerIndex];

                if (layer.IsValid == false)
                    throw new ArgumentException($"Input Layer is invalid. Index={layerIndex}", nameof(layers));
                if (_entries.ContainsKey(layer.Name))
                    throw new InvalidOperationException($"Input Layer name is duplicated: {layer.Name}");

                IReadOnlyList<string> mapNames = layer.ActionMaps;
                var maps = new InputActionMap[mapNames.Count];

                for (int mapIndex = 0; mapIndex < mapNames.Count; mapIndex++)
                {
                    maps[mapIndex] = actions.FindActionMap(mapNames[mapIndex], false)
                        ?? throw new InvalidOperationException(
                            $"Action Map was not found: {layer.Name}.{mapNames[mapIndex]}");
                }

                _entries.Add(layer.Name, new LayerEntry(layer, maps));
            }
        }

        #region Layer Control

        public void ChangeLayer(InputLayer nextLayer)
        {
            ThrowIfDisposed();
            Request(ResolveEntry(nextLayer).Layer);
        }

        public void ClearLayer()
        {
            ThrowIfDisposed();
            Request(default);
        }

        /// <summary>현재 Layer의 맵을 켭니다. Layer가 없으면 켤 맵도 없습니다.</summary>
        internal void Resume()
        {
            ThrowIfDisposed();

            if (_isResumed)
                return;

            InputActionMap[] currentMaps = GetMaps(CurrentLayer);

            _isResumed = true;
            _isTransitioning = true;

            try
            {
                for (int i = 0; i < currentMaps.Length; i++)
                    currentMaps[i].Enable();
            }
            finally
            {
                _isTransitioning = false;
            }

            ApplyPendingRequest();
        }

        /// <summary>현재 Layer의 맵을 끕니다. CurrentLayer는 유지하므로 Resume하면 같은 Layer로 돌아옵니다.</summary>
        internal void Suspend()
        {
            if (_isDisposed || _isResumed == false)
                return;

            InputActionMap[] currentMaps = GetMaps(CurrentLayer);

            // 맵을 끄는 동안 이전 Layer를 유지해야 Canceled 콜백이 올바른 Layer로 전달됩니다.
            _isResumed = false;
            _isTransitioning = true;

            try
            {
                for (int i = 0; i < currentMaps.Length; i++)
                    currentMaps[i].Disable();
            }
            finally
            {
                _isTransitioning = false;
            }

            // Suspend 중에는 보류 요청을 반영해도 맵을 켜지 않습니다.
            ApplyPendingRequest();
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            try
            {
                Suspend();
            }
            finally
            {
                _isDisposed = true;
                _hasPendingRequest = false;
                _pendingLayer = default;
                CurrentLayer = default;
            }
        }

        #endregion

        #region Transitions

        private void Request(InputLayer nextLayer)
        {
            if (_isTransitioning)
            {
                // 전환 중 콜백에서 들어온 요청은 전환이 끝난 뒤 마지막 것만 적용합니다.
                _pendingLayer = nextLayer;
                _hasPendingRequest = true;
                return;
            }

            ApplyLayer(nextLayer);
            ApplyPendingRequest();
        }

        private void ApplyLayer(InputLayer nextLayer)
        {
            // InputLayer는 이름으로 비교하므로 default끼리도 같다고 판단합니다.
            if (CurrentLayer.Equals(nextLayer))
                return;

            InputActionMap[] previousMaps = GetMaps(CurrentLayer);
            InputActionMap[] nextMaps = GetMaps(nextLayer);

            if (_isResumed == false)
            {
                CurrentLayer = nextLayer;
                return;
            }

            _isTransitioning = true;

            try
            {
                // 빠지는 맵을 먼저 끕니다. 이때 생기는 Canceled는 아직 이전 Layer로 전달됩니다.
                for (int i = 0; i < previousMaps.Length; i++)
                {
                    if (ContainsReference(nextMaps, previousMaps[i]) == false)
                        previousMaps[i].Disable();
                }

                CurrentLayer = nextLayer;

                // 유지되는 맵(예: Common)은 다시 켜지 않습니다.
                for (int i = 0; i < nextMaps.Length; i++)
                {
                    if (ContainsReference(previousMaps, nextMaps[i]) == false)
                        nextMaps[i].Enable();
                }
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        private void ApplyPendingRequest()
        {
            while (_hasPendingRequest)
            {
                InputLayer pendingLayer = _pendingLayer;
                _pendingLayer = default;
                _hasPendingRequest = false;

                ApplyLayer(pendingLayer);
            }
        }

        private InputActionMap[] GetMaps(InputLayer layer)
        {
            return layer.IsValid ? ResolveEntry(layer).Maps : EmptyMaps;
        }

        private LayerEntry ResolveEntry(InputLayer requestedLayer)
        {
            if (requestedLayer.IsValid == false)
                throw new ArgumentException("Input Layer is invalid.", nameof(requestedLayer));

            if (_entries.TryGetValue(requestedLayer.Name, out LayerEntry entry) == false)
                throw new InvalidOperationException($"Input Layer is not registered: {requestedLayer.Name}");

            if (MatchesDefinition(requestedLayer, entry.Layer) == false)
                throw new InvalidOperationException(
                    $"Input Layer does not match the registered definition: {requestedLayer.Name}");

            return entry;
        }

        /// <summary>이름과 ActionMap 목록(순서 포함)이 모두 같은지 확인합니다.</summary>
        internal static bool MatchesDefinition(InputLayer left, InputLayer right)
        {
            if (string.Equals(left.Name, right.Name, StringComparison.Ordinal) == false)
                return false;

            IReadOnlyList<string> leftMaps = left.ActionMaps;
            IReadOnlyList<string> rightMaps = right.ActionMaps;

            if (leftMaps.Count != rightMaps.Count)
                return false;

            for (int i = 0; i < leftMaps.Count; i++)
            {
                if (string.Equals(leftMaps[i], rightMaps[i], StringComparison.Ordinal) == false)
                    return false;
            }

            return true;
        }

        private static bool ContainsReference(InputActionMap[] maps, InputActionMap target)
        {
            for (int i = 0; i < maps.Length; i++)
            {
                if (ReferenceEquals(maps[i], target))
                    return true;
            }

            return false;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(UnityInputLayerRuntime));
        }

        #endregion

        private readonly struct LayerEntry
        {
            internal InputLayer Layer { get; }
            internal InputActionMap[] Maps { get; }

            internal LayerEntry(InputLayer layer, InputActionMap[] maps)
            {
                Layer = layer;
                Maps = maps ?? throw new ArgumentNullException(nameof(maps));
            }
        }
    }
}
