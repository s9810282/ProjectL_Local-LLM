using System;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap
{
    /// <summary>
    /// 스테이지 맵 연결 한 번의 팝업 항목입니다. 팝업 목록(<see cref="StageMapViewPopups"/>)이 시작·Tick·정리를 같은 꼴로 돌고,
    /// 맵 한가함 판정이 열림·결과 대기를 목록으로 읽습니다(스테이지 선택 리팩토링 설계 §2.6 C-01).
    /// </summary>
    internal interface IStageMapViewPopup : IDisposable
    {
        /// <summary>팝업 화면이 열려 있습니다.</summary>
        bool IsOpen { get; }

        /// <summary>제출한 요청의 결과를 기다립니다. 결과 대기가 없는 팝업은 늘 거짓입니다.</summary>
        bool IsWaiting { get; }

        /// <summary>부모가 기본 Layer를 연결한 뒤 이미 열린 화면을 인수합니다.</summary>
        void Start();

        void Tick();
    }
}
