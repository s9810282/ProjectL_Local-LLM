using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// AudioManager에 등록된 플레이어 그룹을 식별하는 직렬화 가능한 ID.
/// 표시 이름과 별개의 고유 키를 사용하므로 ID 이름을 바꿔도 기존 참조가 유지된다.
/// </summary>
[Serializable]
public struct AudioPlayerId : IEquatable<AudioPlayerId>
{
    [SerializeField] private string key;

    public bool IsValid => !string.IsNullOrEmpty(key);

    internal string Key => key ?? string.Empty;

    internal AudioPlayerId(string key)
    {
        this.key = key ?? string.Empty;
    }

    public bool Equals(AudioPlayerId other)
    {
        return string.Equals(key, other.key, StringComparison.Ordinal);
    }

    public override bool Equals(object obj)
    {
        return obj is AudioPlayerId other && Equals(other);
    }

    public override int GetHashCode()
    {
        return key == null ? 0 : StringComparer.Ordinal.GetHashCode(key);
    }

    public override string ToString()
    {
        return IsValid ? key : "None";
    }

    public static bool operator ==(AudioPlayerId left, AudioPlayerId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(AudioPlayerId left, AudioPlayerId right)
    {
        return !left.Equals(right);
    }
}

/// <summary>AudioManager Inspector에서 편집하는 enum형 ID 항목.</summary>
[Serializable]
public sealed class AudioPlayerIdDefinition
{
    [SerializeField, HideInInspector] private string key;
    [SerializeField] private string displayName;

    public AudioPlayerId Id => new(key);
    public string DisplayName => displayName ?? string.Empty;

    internal AudioPlayerIdDefinition(string key, string displayName)
    {
        this.key = key;
        this.displayName = displayName;
    }

    internal bool EnsureValid(HashSet<string> usedKeys)
    {
        bool changed = false;

        if (string.IsNullOrEmpty(key) || usedKeys.Contains(key))
        {
            key = Guid.NewGuid().ToString("N");
            changed = true;
        }

        usedKeys.Add(key);

        string normalizedName = string.IsNullOrWhiteSpace(displayName)
            ? "Unnamed Player ID"
            : displayName.Trim();

        if (!string.Equals(displayName, normalizedName, StringComparison.Ordinal))
        {
            displayName = normalizedName;
            changed = true;
        }

        return changed;
    }
}
