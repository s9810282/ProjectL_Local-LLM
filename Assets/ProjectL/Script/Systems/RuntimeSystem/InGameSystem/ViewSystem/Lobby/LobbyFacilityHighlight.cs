using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.LobbyVisual.Item;
using TMPro;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Lobby
{
    /// <summary>
    /// 호버한 시설의 안내 글자를 밝게 빛나게 합니다. 호버하는 동안 글자 색과 빛 번짐을 그대로 유지하고, 벗어나면 원래 글자로 돌립니다.
    /// 빛 번짐은 글자 머티리얼의 복사본에 TMP Underlay를 켜서 그리며, 복사본은 처음 호버할 때 만들고 정리 때 지웁니다(로비 설계 §16).
    /// 바꿀 때만 일하고 프레임마다 하는 일은 없습니다.
    /// </summary>
    internal sealed class LobbyFacilityHighlight : IDisposable
    {
        private sealed class Entry
        {
            internal LobbyFacilityView Facility;
            internal TextMeshProUGUI Label;
            internal Material Original;
            internal Color OriginalColor;
            internal Material Glow;
        }

        private readonly LobbyFacilityHoverData _data;
        private List<Entry> _entries;
        private Entry _current;

        internal LobbyFacilityHighlight(IReadOnlyList<LobbyFacilityView> facilities, LobbyFacilityHoverData data)
        {
            if (facilities == null) throw new ArgumentNullException(nameof(facilities));
            _data = data ?? throw new ArgumentNullException(nameof(data));
            // 시설·글자 연결은 DB 생성 때 원본 프리팹에서 검사했습니다(LobbyVisualPrefabRules.ValidateRoom).
            _entries = new List<Entry>(facilities.Count);
            for (int i = 0; i < facilities.Count; i++)
                _entries.Add(new Entry { Facility = facilities[i], Label = facilities[i].Label });
        }

        /// <summary>빛날 시설을 바꿉니다. null이면 모두 끕니다. 같은 시설이면 아무것도 하지 않습니다.</summary>
        internal void Show(LobbyFacilityView facility)
        {
            if (_entries == null || facility == _current?.Facility) return;
            Restore(_current);
            _current = null;
            if (facility == null) return;

            Entry entry = Find(facility);
            if (entry == null || entry.Label == null) return;
            entry.Original = entry.Label.fontSharedMaterial;
            entry.OriginalColor = entry.Label.color;
            entry.Glow ??= CreateGlow(entry.Original);
            // 크기·부드러움을 넣은 뒤 붙여야 TMP가 빛 번짐까지 담는 여백을 계산합니다.
            entry.Label.fontSharedMaterial = entry.Glow;
            entry.Label.color = _data.TextColor;
            _current = entry;
        }

        public void Dispose()
        {
            List<Entry> entries = _entries;
            if (entries == null) return;
            _entries = null;
            Entry current = _current;
            _current = null;
            try
            {
                Restore(current);
            }
            finally
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    Material glow = entries[i].Glow;
                    entries[i].Glow = null;
                    if (glow == null) continue;
                    if (Application.isPlaying) UnityEngine.Object.Destroy(glow);
                    else UnityEngine.Object.DestroyImmediate(glow);
                }
            }
        }

        private Material CreateGlow(Material original)
        {
            if (original == null)
                throw new InvalidOperationException("[Lobby] 시설 안내 글자에 머티리얼이 없습니다.");
            var glow = new Material(original) { name = original.name + " (Lobby Hover)" };
            glow.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            glow.SetFloat(ShaderUtilities.ID_UnderlayDilate, _data.GlowSize);
            glow.SetFloat(ShaderUtilities.ID_UnderlaySoftness, _data.GlowSoftness);
            glow.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            glow.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, 0f);
            glow.SetColor(ShaderUtilities.ID_UnderlayColor, _data.GlowColor);
            return glow;
        }

        private static void Restore(Entry entry)
        {
            if (entry?.Label == null) return;
            entry.Label.fontSharedMaterial = entry.Original;
            entry.Label.color = entry.OriginalColor;
        }

        private Entry Find(LobbyFacilityView facility)
        {
            for (int i = 0; i < _entries.Count; i++)
                if (_entries[i].Facility == facility) return _entries[i];
            return null;
        }
    }
}
