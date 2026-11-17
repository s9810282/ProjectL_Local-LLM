using System;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>로비 선택 또는 확정 Run 메인의 InfoIcon만 표시합니다. HP·시너지와 독립된 표시 캐시입니다.</summary>
    internal sealed class MainCharacterPortrait
    {
        private readonly Image _image;
        private IUnitVisualTableQuery _visuals;
        private IRunDeckReadSink _deck;
        private ICharacterBufferReadSink _characters;
        private LobbyBinding _lobby;
        private bool _hasMode;
        private bool _lobbyMode;
        private bool _hasKey;
        private EntityKey _lastKey;
        private RunDeckSnapshot _observedDeck;
        private long _observedRosterRevision;

        internal MainCharacterPortrait(Image image)
        {
            _image = image != null ? image
                : throw new InvalidOperationException("[MainCharactorUI] MainCharactor의 portrait Image가 필요합니다.");
        }

        internal void BindVisuals(IUnitVisualTableQuery visuals)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _hasKey = false;
            _observedDeck = null;
        }

        internal void BindRun(IRunDeckReadSink deck, ICharacterBufferReadSink characters)
        {
            if (deck == null) throw new ArgumentNullException(nameof(deck));
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            _deck = deck;
            _characters = characters;
            _observedDeck = null;
        }

        internal IDisposable BindLobby(Func<EntityKey> readMain)
        {
            if (readMain == null) throw new ArgumentNullException(nameof(readMain));
            var binding = new LobbyBinding(this, readMain);
            _lobby = binding;
            return binding;
        }

        internal void SetLobbyMode(bool lobby)
        {
            _hasMode = true;
            _lobbyMode = lobby;
            _observedDeck = null;
        }

        internal void Refresh()
        {
            // 최초 화면 정책 지정 전과 로비 Scope 연결 전·해제 후에는 이전 Run 얼굴을 보여 주지 않습니다.
            if (!_hasMode || (_lobbyMode && _lobby == null))
            {
                Clear();
                return;
            }
            if (_lobbyMode)
            {
                Show(_lobby.ReadMain());
                return;
            }
            if (_deck == null || _characters == null)
                throw new InvalidOperationException("[MainCharactorUI] Run 초상화에는 RunDeck·Character ReadSink가 필요합니다.");
            // 새 Run 게시 전·리셋 중에는 표시할 확정 메인이 없습니다.
            if (!_deck.TryGetCurrent(out RunDeckSnapshot deck) || deck == null)
            {
                Clear();
                return;
            }
            long revision = _characters.RosterRevision;
            if (ReferenceEquals(_observedDeck, deck) && _observedRosterRevision == revision) return;
            if (!_characters.TryGetOwned(deck.MainId, out OwnedCharacterRecord main) || main == null)
                throw new InvalidOperationException($"[MainCharactorUI] Run 메인 {deck.MainId}의 보유 기록이 없습니다.");
            if (main.Role != CharacterOwnershipRole.Main)
                throw new InvalidOperationException($"[MainCharactorUI] Run 메인 {deck.MainId}의 역할이 Main이 아닙니다.");
            Show(main.CharacterKey);
            _observedDeck = deck;
            _observedRosterRevision = revision;
        }

        private void Show(EntityKey key)
        {
            if (_hasKey && _lastKey == key) return;
            if (_visuals == null)
                throw new InvalidOperationException("[MainCharactorUI] 초상화에는 UnitVisual View DB가 필요합니다.");
            if (!_visuals.TryGetUnitVisual(key, out var visual) || visual == null || visual.InfoIcon == null)
                throw new InvalidOperationException($"[MainCharactorUI] 캐릭터 {key}의 UnitVisual.InfoIcon이 필요합니다.");
            var sprite = visual.InfoIcon;
            if (_image.sprite != sprite) _image.sprite = sprite;
            if (!_image.enabled) _image.enabled = true;
            _lastKey = key;
            _hasKey = true;
        }

        internal void Clear()
        {
            _hasKey = false;
            _observedDeck = null;
            if (_image.sprite != null) _image.sprite = null;
            if (_image.enabled) _image.enabled = false;
        }

        /// <summary>이전 Scope의 늦은 반환이 새 연결을 지우지 않게 연결 객체 자체로 소유권을 확인합니다.</summary>
        private sealed class LobbyBinding : IDisposable
        {
            private MainCharacterPortrait _owner;
            internal Func<EntityKey> ReadMain { get; private set; }

            internal LobbyBinding(MainCharacterPortrait owner, Func<EntityKey> readMain)
            {
                _owner = owner;
                ReadMain = readMain;
            }

            public void Dispose()
            {
                if (_owner == null) return;
                if (ReferenceEquals(_owner._lobby, this)) _owner._lobby = null;
                _owner = null;
                ReadMain = null;
            }
        }
    }
}
