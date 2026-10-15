using System;
using System.Collections.Generic;
using System.Reflection;
using ProjectL.Framework.DataBase;
using UnityEngine;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// DB 테스트용으로 SO와 GameObject를 메모리에서 만들고, 끝나면 한 번에 지웁니다.
    /// SO 입력 필드는 private 직렬화 필드이므로 리플렉션으로 채웁니다.
    /// </summary>
    internal sealed class DataBaseTestAssets
    {
        private readonly List<UnityEngine.Object> _created = new();

        internal T Create<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            _created.Add(asset);
            return asset;
        }

        internal GameObject CreateGameObject(string name)
        {
            var gameObject = new GameObject(name);
            _created.Add(gameObject);
            return gameObject;
        }

        internal Material CreateMaterial(string name)
        {
            // 어느 렌더 파이프라인에서도 항상 있는 내장 셰이더입니다.
            var material = new Material(Shader.Find("Hidden/InternalErrorShader")) { name = name };
            _created.Add(material);
            return material;
        }

        internal T Table<T>(params DatabaseItem[] items) where T : DatabaseTable
        {
            var table = Create<T>();
            Set(table, "databaseItems", new List<DatabaseItem>(items));
            return table;
        }

        internal T Group<T>(params DatabaseTable[] tables) where T : DatabaseGroup
        {
            var group = Create<T>();
            Set(group, "databaseTables", new List<DatabaseTable>(tables));
            return group;
        }

        internal void DestroyAll()
        {
            foreach (var created in _created)
            {
                if (created != null)
                    UnityEngine.Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        /// <summary>상속 계층까지 찾아 인스턴스 필드 값을 넣습니다.</summary>
        internal static void Set(object target, string field, object value)
        {
            FindField(target.GetType(), field).SetValue(target, value);
        }

        internal static object Get(object target, string field)
        {
            return FindField(target.GetType(), field).GetValue(target);
        }

        /// <summary>SO 안의 private 직렬화 구조체(예: Loadout의 Entry) 배열을 만듭니다.</summary>
        internal static Array Entries(Type owner, string nestedType, params (string Field, object Value)[][] rows)
        {
            Type entryType = owner.GetNestedType(nestedType, BindingFlags.NonPublic)
                ?? throw new MissingMemberException(owner.Name, nestedType);
            Array entries = Array.CreateInstance(entryType, rows.Length);
            for (int i = 0; i < rows.Length; i++)
            {
                object boxed = Activator.CreateInstance(entryType, true);
                foreach (var (field, value) in rows[i])
                    FindField(entryType, field).SetValue(boxed, value);
                entries.SetValue(boxed, i);
            }

            return entries;
        }

        private static FieldInfo FindField(Type type, string field)
        {
            for (Type current = type; current != null; current = current.BaseType)
            {
                FieldInfo info = current.GetField(field,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (info != null)
                    return info;
            }

            throw new MissingFieldException(type.Name, field);
        }
    }
}
