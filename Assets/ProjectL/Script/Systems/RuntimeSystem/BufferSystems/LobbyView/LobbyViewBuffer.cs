using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView
{
    /// <summary>실행 계층(LifeCycle)이 출발 요청의 단계를 바꾸려고 구체 요청을 읽는 싱크입니다. View는 받지 않습니다.</summary>
    public interface ILobbyViewBufferReadSink : IServiceModule
    {
        LobbyDepartureRequest Current { get; }
    }

    /// <summary>
    /// View의 출발 창구입니다. 제출·취소와 현재 요청 읽기를 모두 읽기 계약(<see cref="ILobbyDepartureState"/>)으로만 주고받아,
    /// View가 단계 변경자를 부를 수 없습니다(로비 리팩토링 2차 설계 N-75).
    /// </summary>
    public interface ILobbyViewBufferWriteSink : IServiceModule
    {
        /// <summary>현재 요청입니다. 없으면 null입니다.</summary>
        ILobbyDepartureState Current { get; }
        /// <summary>로드아웃 기본 메인으로 출발 의도를 제출합니다. 진행 중인 요청이 있으면 그 요청을 반환합니다.</summary>
        ILobbyDepartureState Submit();
        /// <summary>
        /// 고른 메인 키를 실어 출발 의도를 제출합니다. 키는 <see cref="EntityKey.None"/>이라도 그대로 싣습니다.
        /// 진행 중인 요청이 있으면 그 요청을 반환하며, 그 요청에 실린 키를 바꾸지 않습니다.
        /// </summary>
        ILobbyDepartureState Submit(EntityKey mainCharacterKey);
        /// <summary>현재 요청의 실행 전 취소만 허용합니다. 시작한 Run을 되돌리지 않습니다.</summary>
        bool Cancel(ILobbyDepartureState request);
    }

    /// <summary>로비 출발 입력 번호를 발급하고 현재 실행 요청 하나를 보관합니다.</summary>
    internal sealed class LobbyViewBuffer : ILobbyViewBufferReadSink,
        ILobbyViewBufferWriteSink, IExportSystemServices
    {
        private int _lastIssuedNumber;
        public LobbyDepartureRequest Current { get; private set; }

        ILobbyDepartureState ILobbyViewBufferWriteSink.Current => Current;

        ILobbyDepartureState ILobbyViewBufferWriteSink.Submit() => Submit(null);

        ILobbyDepartureState ILobbyViewBufferWriteSink.Submit(EntityKey mainCharacterKey) => Submit(mainCharacterKey);

        /// <summary>두 제출 입구의 공통 본문입니다. null은 "메인을 지정하지 않음"이고, 진행 중인 요청은 키째 그대로 돌려줍니다.</summary>
        private LobbyDepartureRequest Submit(EntityKey? mainCharacterKey)
        {
            if (Current != null && Current.IsPending)
                return Current;
            if (_lastIssuedNumber == int.MaxValue)
                throw new InvalidOperationException("로비 출발 요청 번호를 더 발급할 수 없습니다.");
            Current = new LobbyDepartureRequest(++_lastIssuedNumber, mainCharacterKey);
            return Current;
        }

        bool ILobbyViewBufferWriteSink.Cancel(ILobbyDepartureState request) =>
            request is LobbyDepartureRequest owned && ReferenceEquals(Current, owned) && owned.TryCancel();

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null)
                throw new ArgumentNullException(nameof(sink));
            sink.AddService<ILobbyViewBufferReadSink>(this);
            sink.AddService<ILobbyViewBufferWriteSink>(this);
        }
    }
}
