using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 로비 세션 하나의 "다음 Run 메인" 선택 원본입니다(로비 메인 선택 설계 LM1). 로비 씬 연결마다 System이 하나 만들고,
    /// 덱 팝업(쓰기)과 출발 흐름(읽기)이 같은 객체를 받습니다. 팝업을 닫았다 열어도 값은 남고, 다음 연결은 새 객체라 기본값부터입니다.
    /// </summary>
    /// <remarks>
    /// 후보 자격·기본 메인은 Core 응답(<see cref="RunStartMainOptions"/>)이 원본이며 여기서 다시 판정하지 않습니다.
    /// 처음 값은 응답의 기본 메인이고, 후보 첫 번째를 기본으로 보지 않습니다. 정리할 자원이 없어 <see cref="IDisposable"/>이 아닙니다.
    /// </remarks>
    internal sealed class LobbyMainSelection : ILobbyMainSelectionWriter
    {
        private ILobbyDepartureHold _hold;
        private ILobbyMainModelDisplay _modelDisplay;

        /// <param name="options">Core가 돌려준 후보·기본 메인입니다.</param>
        /// <param name="expectedLoadoutKey">출발이 쓸 로드아웃 키입니다. 응답이 다른 로드아웃 것이면 구성 오류입니다.</param>
        internal LobbyMainSelection(RunStartMainOptions options, int expectedLoadoutKey)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.LoadoutKey != expectedLoadoutKey)
                throw new InvalidOperationException(
                    $"[Lobby] 메인 후보 응답의 로드아웃({options.LoadoutKey})이 출발 로드아웃({expectedLoadoutKey})과 다릅니다.");
            if (options.CandidateKeys.Count == 0)
                throw new InvalidOperationException($"[Lobby] 로드아웃 {options.LoadoutKey}의 메인 후보가 없습니다.");
            if (!Contains(options.CandidateKeys, options.DefaultMainCharacterKey))
                throw new InvalidOperationException(
                    $"[Lobby] 로드아웃 {options.LoadoutKey}의 기본 메인({options.DefaultMainCharacterKey})이 메인 후보에 없습니다.");
            LoadoutKey = options.LoadoutKey;
            // Core 응답 목록은 이미 불변 사본입니다.
            Candidates = options.CandidateKeys;
            Selected = options.DefaultMainCharacterKey;
        }

        /// <summary>후보를 받은 로드아웃 키입니다. 출발 로드아웃과 같음을 생성 때 확인했습니다.</summary>
        internal int LoadoutKey { get; }

        public IReadOnlyList<EntityKey> Candidates { get; }

        public EntityKey Selected { get; private set; }

        public bool IsLocked => (_hold
            ?? throw new InvalidOperationException("[Lobby] 출발 흐름을 연결하기 전에는 메인 선택의 잠김을 알 수 없습니다.")).IsHolding;

        /// <summary>이 연결의 출발 흐름을 한 번 연결합니다. 연결 Scope가 출발 흐름을 만든 직후 부릅니다.</summary>
        internal void BindDepartureHold(ILobbyDepartureHold hold)
        {
            if (hold == null) throw new ArgumentNullException(nameof(hold));
            if (_hold != null)
                throw new InvalidOperationException("[Lobby] 메인 선택에 출발 흐름이 이미 연결돼 있습니다.");
            _hold = hold;
        }

        /// <summary>같은 로비 연결의 모델 표시를 한 번 연결합니다. 최초 표시는 씬 시작 때 Selected를 읽습니다.</summary>
        internal void BindModelDisplay(ILobbyMainModelDisplay modelDisplay)
        {
            if (modelDisplay == null) throw new ArgumentNullException(nameof(modelDisplay));
            if (_modelDisplay != null)
                throw new InvalidOperationException("[Lobby] 메인 선택에 모델 표시가 이미 연결돼 있습니다.");
            _modelDisplay = modelDisplay;
        }

        public bool Select(EntityKey key)
        {
            if (!Contains(Candidates, key))
                throw new ArgumentException($"[Lobby] 메인 후보가 아닌 키입니다: {key}", nameof(key));
            if (IsLocked)
                throw new InvalidOperationException("[Lobby] 출발 처리 중에는 다음 Run 메인을 바꿀 수 없습니다.");
            if (key == Selected) return false;
            Selected = key;
            // 연결 전에는 씬 Start가 최신 선택을 읽습니다. 표시 실패는 입력 오류 경계로 전파합니다.
            _modelDisplay?.ShowMain(key);
            return true;
        }

        private static bool Contains(IReadOnlyList<EntityKey> keys, EntityKey key)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                if (keys[i] == key) return true;
            }
            return false;
        }
    }
}
