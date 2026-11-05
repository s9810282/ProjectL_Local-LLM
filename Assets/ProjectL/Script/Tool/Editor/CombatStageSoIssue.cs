using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    // 검사한 작업본의 층/목록 위치입니다. Root·개정 검증 없이 다른 작업본에서 재사용하지 않습니다.
    internal sealed class CombatStageSoIssueTarget
    {
        internal CombatStageSoLayer Layer { get; }
        internal int Index { get; }

        private CombatStageSoIssueTarget(CombatStageSoLayer layer, int index)
        {
            Layer = layer;
            Index = index;
        }

        internal static CombatStageSoIssueTarget Of(CombatStageSoDocument document, object item)
        {
            var layer = document.LayerOf(item);
            return new CombatStageSoIssueTarget(layer,
                layer == CombatStageSoLayer.Headquarters ? 0 : Items(document, layer).IndexOf(item));
        }

        internal object Resolve(CombatStageSoDocument document)
        {
            if (Layer == CombatStageSoLayer.Headquarters)
                return document.HasHeadquarters ? document.enemyHeadquarters : null;
            IList items = Items(document, Layer);
            return Index >= 0 && Index < items.Count ? items[Index] : null;
        }

        private static IList Items(CombatStageSoDocument document, CombatStageSoLayer layer)
        {
            switch (layer)
            {
                case CombatStageSoLayer.Unit: return document.enemyUnits;
                case CombatStageSoLayer.Structure: return document.structures;
                case CombatStageSoLayer.Obstacle: return document.obstacles;
                case CombatStageSoLayer.Terrain: return document.terrains;
                case CombatStageSoLayer.Object: return document.objects;
                default: return document.railCells;
            }
        }
    }

    // 위치 없는 SO 예외도 같은 결과에 담습니다. 메시지에서 위치를 추측하지 않습니다.
    internal sealed class CombatStageSoIssue
    {
        internal string Message { get; }
        internal Vector2Int? ProblemCell { get; }
        internal CombatStageSoIssueTarget Target { get; }

        internal CombatStageSoIssue(string message, Vector2Int? problemCell = null, CombatStageSoIssueTarget target = null)
        {
            Message = message;
            ProblemCell = problemCell;
            Target = target;
        }
    }

    internal enum CombatStageSoCheckPhase { NeedsRecheck, Checking, Complete }

    // 창 전용 검사 결과입니다. Undo/초안/에셋에 직렬화하지 않습니다.
    internal sealed class CombatStageSoCheckResult
    {
        internal string RootGuid { get; }
        internal long Revision { get; }
        internal List<CombatStageSoIssue> Issues { get; }
        internal List<string> Warnings { get; } = new List<string>();
        internal CombatStageSoCheckPhase Phase { get; private set; }

        internal CombatStageSoCheckResult(string rootGuid, long revision, List<CombatStageSoIssue> issues, CombatStageSoCheckPhase phase = CombatStageSoCheckPhase.Checking)
        {
            RootGuid = rootGuid;
            Revision = revision;
            Issues = issues;
            Phase = phase;
        }

        internal void Complete() => Phase = CombatStageSoCheckPhase.Complete;

        internal bool Matches(string rootGuid, long revision) => RootGuid == rootGuid && Revision == revision;
    }
}
