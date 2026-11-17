using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 연결 한 번의 씬 표시 부품입니다. 시설 방·배경 출력 맞춤·판·캐릭터 표시를 갖습니다.
    /// 입력에는 필요한 값만 <see cref="InputTargets"/>로 넘깁니다(로비 리팩토링 2차 설계 N-73).
    /// </summary>
    internal sealed class LobbyViewScene : ILobbyMainModelDisplay
    {
        private readonly LobbySceneServices _services;
        private readonly ILobbyMainSelectionReader _selection;
        private IDisposable _backgroundBinding;
        private LobbyVisualSet _visuals;
        private LobbyCharacterVisual _character;

        internal LobbyViewScene(LobbySceneRoot root, Camera output, LobbySceneServices services,
            ILobbyMainSelectionReader selection)
        {
            Root = root != null ? root : throw new ArgumentNullException(nameof(root));
            Output = output != null ? output : throw new ArgumentNullException(nameof(output));
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        }

        internal LobbySceneRoot Root { get; }
        internal Camera Output { get; }

        /// <summary>로비 입력이 쓰는 값(말·판·시설·출력 카메라)입니다. <see cref="Start"/>가 끝까지 간 뒤에만 있습니다.</summary>
        internal LobbyInputTargets InputTargets { get; private set; }

        /// <summary>
        /// 시설 방 → 배경 출력 맞춤 → 판·캐릭터 순으로 세웁니다. 도중에 연결이 끝나면 false입니다.
        /// 만든 것은 연결이 아직 살아 있을 때만 이 부품의 정리 대상(<see cref="RestoreOutput"/>·<see cref="Dispose"/>)에 넣고,
        /// 이미 끝났으면(정리가 먼저 돌았을 수 있으므로) 그 자리에서 바로 정리합니다. 모든 단계가 이 한 꼴입니다(로비 리팩토링 2차 설계 N-13).
        /// </summary>
        internal bool Start(Action requireRunning, Func<bool> canWork, Func<bool> isCurrent)
        {
            LobbyVisualSet visuals = LobbyVisualSet.Create(Root.WorldRoot, _services.Models);
            try
            {
                requireRunning();
            }
            catch
            {
                visuals.Dispose();
                throw;
            }
            if (!canWork())
            {
                visuals.Dispose();
                return false;
            }
            _visuals = visuals;

            IDisposable backgroundBinding = Root.UI.Background.ConnectOutput(Output,
                Root.LobbyCamera.m_Lens.NearClipPlane, Root.LobbyCamera.m_Lens.FarClipPlane, isCurrent);
            if (!canWork())
            {
                backgroundBinding.Dispose();
                return false;
            }
            _backgroundBinding = backgroundBinding;

            var board = new LobbyBoard(_services.Board, Root.WorldRoot);
            LobbyCharacterVisual character = LobbyCharacterVisual.Create(Root.WorldRoot, _services.CharacterPresentation);
            try
            {
                character.ShowModel(MainModelPrefab.Require(_selection.Selected, _services.UnitVisuals));
                requireRunning();
            }
            catch
            {
                // 아직 씬에 인계하지 않은 최초 모델의 소유권은 이 호출이 정리합니다.
                character.Dispose();
                throw;
            }
            if (!canWork())
            {
                character.Dispose();
                return false;
            }
            _character = character;
            character.MovementRoot.SetPositionAndRotation(board.Start.position, board.Start.rotation);
            character.Show();
            requireRunning();
            if (!canWork()) return false;
            InputTargets = new LobbyInputTargets(character.MovementRoot, board, visuals.Facilities, Output);
            return true;
        }

        /// <summary>시작 전에는 Start가 선택을 읽고, 정리 후에는 표시할 캐릭터가 없으므로 알림을 무시합니다.</summary>
        public void ShowMain(EntityKey key)
        {
            LobbyCharacterVisual character = _character;
            if (character == null) return;
            character.ShowModel(MainModelPrefab.Require(key, _services.UnitVisuals));
        }

        /// <summary>배경 출력 카메라를 원래대로 돌리고 캐릭터를 숨깁니다. 다른 정리 콜백이 새 연결을 게시하기 전에 부릅니다.</summary>
        internal void RestoreOutput()
        {
            List<Exception> errors = null;
            IDisposable binding = _backgroundBinding;
            _backgroundBinding = null;
            LobbyCharacterVisual character = _character;
            CleanupErrors.Try(() => binding?.Dispose(), ref errors);
            CleanupErrors.Try(() => character?.Hide(), ref errors);
            CleanupErrors.ThrowIfAny(errors, "[Lobby] 배경 출력 복원 중 실패했습니다.");
        }

        /// <summary>캐릭터 → 시설 방 순으로 정리합니다(옛 Scope 정리 순서).</summary>
        internal void Dispose()
        {
            List<Exception> errors = null;
            LobbyCharacterVisual character = _character;
            LobbyVisualSet visuals = _visuals;
            _character = null;
            _visuals = null;
            InputTargets = null;
            CleanupErrors.Try(() => character?.Dispose(), ref errors);
            CleanupErrors.Try(() => visuals?.Dispose(), ref errors);
            CleanupErrors.ThrowIfAny(errors, "[Lobby] 씬 표시 정리 중 실패했습니다.");
        }
    }

    /// <summary>로비 입력이 씬 표시에서 받는 값입니다. 씬 부품의 내부 구성을 입력이 알지 않게 합니다(로비 리팩토링 2차 설계 N-73).</summary>
    internal sealed class LobbyInputTargets
    {
        internal LobbyInputTargets(Transform movementRoot, LobbyBoard board, IReadOnlyList<LobbyFacilityView> facilities,
            Camera output)
        {
            MovementRoot = movementRoot != null ? movementRoot : throw new ArgumentNullException(nameof(movementRoot));
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Facilities = facilities ?? throw new ArgumentNullException(nameof(facilities));
            Output = output != null ? output : throw new ArgumentNullException(nameof(output));
        }

        internal Transform MovementRoot { get; }
        internal LobbyBoard Board { get; }
        internal IReadOnlyList<LobbyFacilityView> Facilities { get; }
        internal Camera Output { get; }
    }
}
