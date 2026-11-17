using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Tutorial;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.LoadingSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 전투 View 조정자(<see cref="CombatViewSystem"/>)가 서비스를 받을 때(PostBind) 꺼내는 서비스 묶음입니다(조정자 나누기 설계 CS5).
    /// 조회는 <see cref="From"/> 한 곳이 하고, 조정자는 이 값 하나를 받아 검사·파생 객체 생성을 합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 받은 값을 담기만 합니다. 검사는 지금 조회 자리에서 하던 문구 있는 <see cref="InvalidOperationException"/> 넷(Tutorial 상태 읽기·Tutorial 요청 쓰기·로딩 상태·확정 덱 읽기)뿐이고,
    /// 나머지 null 검사와 확인창 계약 검사는 조정자가 지금 순서·인자 이름대로 합니다(설계 P10). 조회 순서도 옮기기 전과 같습니다.
    /// </para>
    /// <para>
    /// 조회는 조정자 안에서 하므로 Factory는 그대로입니다(설계 P2).
    /// </para>
    /// </remarks>
    internal sealed class CombatViewBindings
    {
        private CombatViewBindings(
            ICombatStateBufferReadSink state,
            ICombatResultReadSink results,
            ICharacterBufferReadSink characters,
            ICurrentProgressBufferReadSink current,
            ICurrencyBufferReadSink currency,
            ICombatViewBufferReadSink inputRead,
            ICombatViewBufferWriteSink inputWrite,
            IStageInputReceiverSlot inputSlot,
            IInputLayerService inputLayers,
            ILodingRegisterService loadingRegister,
            ICombatEntryRead entry,
            CombatDisplayServices displayServices,
            IUITextPresentation textPresentation,
            IItemInventoryReadSink items,
            ITutorialStateBufferReadSink tutorialState,
            ITutorialRequestBufferWriteSink tutorialRequests,
            ILodingSystemService loading,
            IRunDeckReadSink runDeck,
            IRunDeckService deck)
        {
            State = state;
            Results = results;
            Characters = characters;
            Current = current;
            Currency = currency;
            InputRead = inputRead;
            InputWrite = inputWrite;
            InputSlot = inputSlot;
            InputLayers = inputLayers;
            LoadingRegister = loadingRegister;
            Entry = entry;
            DisplayServices = displayServices;
            TextPresentation = textPresentation;
            Items = items;
            TutorialState = tutorialState;
            TutorialRequests = tutorialRequests;
            Loading = loading;
            RunDeck = runDeck;
            Deck = deck;
        }

        /// <summary>전투 상태 읽기입니다.</summary>
        internal ICombatStateBufferReadSink State { get; }

        /// <summary>전투 결과 읽기입니다.</summary>
        internal ICombatResultReadSink Results { get; }

        /// <summary>캐릭터 읽기입니다.</summary>
        internal ICharacterBufferReadSink Characters { get; }

        /// <summary>현재 진행(챕터·노드) 읽기입니다. 표시 값 읽기가 씁니다.</summary>
        internal ICurrentProgressBufferReadSink Current { get; }

        /// <summary>재화 읽기입니다. 표시 값 읽기가 씁니다.</summary>
        internal ICurrencyBufferReadSink Currency { get; }

        /// <summary>전투 입력 Buffer 읽기입니다(View가 쓰고 Core 쪽이 읽는 Buffer).</summary>
        internal ICombatViewBufferReadSink InputRead { get; }

        /// <summary>전투 입력 Buffer 쓰기입니다.</summary>
        internal ICombatViewBufferWriteSink InputWrite { get; }

        /// <summary>스테이지 입력 수신 슬롯입니다. 입력 연결이 씁니다.</summary>
        internal IStageInputReceiverSlot InputSlot { get; }

        /// <summary>입력 Layer 서비스입니다. 입력 연결이 씁니다.</summary>
        internal IInputLayerService InputLayers { get; }

        /// <summary>씬 Enter/Exit 처리 등록 서비스입니다.</summary>
        internal ILodingRegisterService LoadingRegister { get; }

        /// <summary>전투 진입 요청 읽기입니다(진입 활성).</summary>
        internal ICombatEntryRead Entry { get; }

        /// <summary>전투 표시가 쓰는 Render·Audio·UI 서비스 묶음입니다(구조 개선 SV5·E16).</summary>
        internal CombatDisplayServices DisplayServices { get; }

        /// <summary>거절 안내·환경 설명·확인창이 쓰는 텍스트 표시입니다. 확인창 계약도 제공해야 합니다(조정자가 검사).</summary>
        internal IUITextPresentation TextPresentation { get; }

        /// <summary>아이템 재고 읽기입니다. 표시 값 읽기가 씁니다.</summary>
        internal IItemInventoryReadSink Items { get; }

        /// <summary>튜토리얼 상태 읽기입니다. 막힘 보고가 씁니다.</summary>
        internal ITutorialStateBufferReadSink TutorialState { get; }

        /// <summary>튜토리얼 요청 쓰기입니다. 막힘 보고가 씁니다.</summary>
        internal ITutorialRequestBufferWriteSink TutorialRequests { get; }

        /// <summary>로딩 완료 상태입니다. 보드 진입 연출·선택 유닛 준비가 씁니다.</summary>
        internal ILodingSystemService Loading { get; }

        /// <summary>확정 RunDeck 읽기입니다. 선택 유닛 준비 대상 읽기가 씁니다.</summary>
        internal IRunDeckReadSink RunDeck { get; }

        /// <summary>전투에서도 보유 개체별 특성 등급이 담긴 덱 스냅샷을 조회합니다.</summary>
        internal IRunDeckService Deck { get; }

        /// <summary>
        /// 서비스 저장소에서 전투 View가 쓰는 서비스를 꺼냅니다. 꺼내는 순서는 옮기기 전 조정자의 인자 평가 순서와 같습니다
        /// (표시 서비스 묶음 → 텍스트 표시 → 재고 → Tutorial 둘 → 로딩 상태 → 확정 덱). 없으면 안 되는 넷은 문구 있는 예외를 던집니다.
        /// </summary>
        /// <param name="resolver">null이 아니어야 합니다. 검사는 부르는 조정자(PostBindService)가 지금처럼 합니다.</param>
        internal static CombatViewBindings From(ISystemServiceStoreReader resolver)
        {
            // IBufferQuery는 모든 싱크를 노출하므로 이 시스템에 필요한 것만 꺼낸다.
            IBufferQuery buffers = resolver.GetService(BufferSystemKey.Buffer);
            var inGame = resolver.GetService(SystemGroupsService.InGame);
            var global = resolver.GetService(SystemGroupsService.Global);
            var lifeCycles = resolver.GetService(SystemGroupsService.LifeCycle);
            // 확인창 기능(IUIConfirmationPresentation)은 조정자가 받을 때 필수로 검사합니다.
            return new CombatViewBindings(
                buffers.CombatStateReadSink,
                buffers.CombatResultReadSink,
                buffers.CharacterReadSink,
                buffers.CurrentProgressReadSink,
                buffers.CurrencyReadSink,
                buffers.CombatViewReadSink,
                buffers.CombatViewWriteSink,
                inGame.ViewGroup.Input.Stage,
                global.InputServiceCommand.InputLayerServices,
                global.LodingServiceCommand.LodingRegisterService,
                lifeCycles.CombatEntry,
                // 표시가 쓰는 Render·Audio·UI 서비스는 묶음이 고른다(구조 개선 SV5·E16).
                CombatDisplayServices.From(global),
                global.UITextPresentation,
                buffers.ItemInventoryReadSink,
                buffers.TutorialStateReadSink ?? throw new InvalidOperationException("[CombatViewSystem] Tutorial 상태 읽기 Sink가 필요합니다."),
                buffers.TutorialRequestWriteSink ?? throw new InvalidOperationException("[CombatViewSystem] Tutorial 요청 쓰기 Sink가 필요합니다."),
                global.LodingServiceCommand.LodingService ?? throw new InvalidOperationException("[CombatViewSystem] 로딩 완료 상태 서비스가 필요합니다."),
                buffers.RunDeckReadSink ?? throw new InvalidOperationException("[CombatViewSystem] 확정 RunDeck 읽기가 필요합니다."),
                inGame.CoreGroup.Deck);
        }
    }
}
