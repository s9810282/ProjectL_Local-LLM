using System;
using ProjectL.Tool.Runtime;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>일반 씬과 분리된 가상 씬의 조명 컨텍스트에 라이트맵을 연결합니다.</summary>
    public static class FbxPlacementPreviewLighting
    {
        public static void Apply(GameObject root)
        {
            if (!FbxPlacementWorkspace.IsWorkspaceRoot(root))
                throw new ArgumentException("가상 씬의 맵 루트가 필요합니다.", nameof(root));
            MapBakedLighting binding = root.GetComponent<MapBakedLighting>();
            if (binding == null || binding.AtlasCount == 0) return;
            MapBakedLightingDataSO lighting = binding.ExportData();
            try
            {
                MapBakedLightingDataSO.Resolved data = lighting.Resolve(root.transform);
                var maps = new LightmapData[data.atlases.Length];
                for (int i = 0; i < maps.Length; i++) maps[i] = data.atlases[i].ToLightmap();
                // 런타임 공통 배열에 등록돼 있으면 이 가상 씬이 소유한 항목만 해제합니다.
                binding.Release();
                bool overridden = Unsupported.SetOverrideLightingSettings(root.scene);
                if (!overridden) throw new InvalidOperationException("가상 씬의 조명 설정에 접근하지 못했습니다.");
                try
                {
                    LightmapSettings.lightmapsMode = data.mode;
                    LightmapSettings.lightmaps = maps;
                }
                finally { Unsupported.RestoreOverrideLightingSettings(); }
                // 가상 씬 카메라는 이 씬의 라이트맵 배열을 사용하므로 로컬 인덱스를 지정합니다.
                foreach (MapBakedLighting.Surface surface in data.surfaces)
                {
                    surface.renderer.lightmapIndex = surface.atlasIndex;
                    surface.renderer.lightmapScaleOffset = surface.scaleOffset;
                }
                foreach (MapBakedLighting.BakedLight light in data.lights) light.Apply();
            }
            finally
            {
                if (!EditorUtility.IsPersistent(lighting)) UnityEngine.Object.DestroyImmediate(lighting);
            }
        }
    }
}
