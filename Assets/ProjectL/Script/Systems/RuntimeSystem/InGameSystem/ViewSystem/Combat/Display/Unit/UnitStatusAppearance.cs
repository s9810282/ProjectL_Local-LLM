using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 걸린 상태가 정하는 유닛 모습(원래 모델 대신 보일 모델)의 주인입니다(재구성 연출 설계 RE2·계획 TP-4~TP-6).
    /// 유닛 표시(<see cref="FieldUnitView"/>) 하나가 하나를 갖고, 무엇을 보일지는 상태 줄 표(<see cref="CombatUnitStatuses.TryGetAppearance"/>)가 정합니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 마지막으로 본 사실 객체를 기억해, 같은 객체면 판단 함수를 부르지 않습니다. 사실은 불변이라 같은 객체면 모습도 같습니다(HUD 사실 기억 GH3와 같은 꼴).
    /// 그래서 사건마다 모든 유닛을 다시 그려도 상태 읽기는 사실이 바뀐 유닛만 합니다.
    /// </para>
    /// <para>
    /// 바꿀 때 원래 렌더러는 켜짐만 끄고(뼈대·Animator·클립 채널은 그대로), 잔해 인스턴스를 켭니다. 렌더러 켜짐은 이 부품만 바꿉니다
    /// (보이기는 GameObject, 래그돌은 Animator·Collider를 다룸). 잔해는 프리팹마다 하나를 유닛 뿌리의 자식으로 만들어 두고 끄고 켜기만 하며,
    /// 유닛 표시가 지워질 때 함께 지워집니다. 외형 층(투명도·플래시·윤곽)을 놓고 다시 잡는 순서는 그 층의 주인인 <see cref="FieldUnitView"/>가 맡습니다.
    /// </para>
    /// </remarks>
    internal sealed class UnitStatusAppearance
    {
        private readonly Transform _root;
        private readonly Dictionary<GameObject, GameObject> _instances = new();
        // 처음 바꿀 때 잡은 원래 모델 렌더러와, 숨기기 직전 켜짐 값입니다(되돌릴 때 원래대로).
        private Renderer[] _originals;
        private bool[] _originalEnabled;
        private GameObject _shownPrefab;
        private CharacterCombatFacts _seenFacts;

        internal UnitStatusAppearance(Transform root)
        {
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
        }

        /// <summary>원래 모델 대신 다른 모델을 보이고 있는지입니다.</summary>
        internal bool IsReplaced => _shownPrefab != null;

        /// <summary>
        /// 이 사실에서 보일 모습이 지금과 다른지 판단합니다. 같은 사실 객체면 판단하지 않고 false입니다.
        /// 다르면 true와 보일 프리팹(원래 모습이면 null)을 돌려주고, 바꾸는 일은 부르는 쪽이 <see cref="Show"/>로 합니다.
        /// </summary>
        internal bool TryResolveChange(CharacterCombatFacts facts, Func<CharacterCombatFacts, GameObject> resolve, out GameObject next)
        {
            if (facts == null) throw new ArgumentNullException(nameof(facts));
            if (resolve == null) throw new ArgumentNullException(nameof(resolve));
            next = _shownPrefab;
            if (ReferenceEquals(facts, _seenFacts)) return false;
            _seenFacts = facts;
            next = resolve(facts);
            return next != _shownPrefab;
        }

        /// <summary>그 프리팹의 모습을 보입니다. null이면 원래 모습으로 되돌립니다. 지금과 같으면 아무것도 하지 않습니다.</summary>
        internal void Show(GameObject prefab)
        {
            if (prefab == _shownPrefab) return;
            // 실패할 수 있는 일(원래 렌더러 잡기·잔해 만들기)을 상태를 바꾸기 전에 합니다. 만들다 실패하면 원래 모습 그대로입니다(묶음 끝 검토 L4).
            // 원래 렌더러는 잔해를 만들기 전에 잡아야 잔해 렌더러가 섞이지 않습니다.
            GameObject next = null;
            if (prefab != null)
            {
                _originals ??= CaptureOriginals();
                next = InstanceOf(prefab);
            }
            if (_shownPrefab != null && _instances.TryGetValue(_shownPrefab, out GameObject shown) && shown != null)
                shown.SetActive(false);
            if (next == null)
                RestoreOriginals();
            else
            {
                if (_shownPrefab == null) HideOriginals();
                next.SetActive(true);
            }
            _shownPrefab = prefab;
        }

        /// <summary>본 사실 기억을 비웁니다. 다음 판단이 상태를 다시 읽습니다(다른 개체·풀 반환·표가 바뀔 수 있는 Configure).</summary>
        internal void Forget() => _seenFacts = null;

        private void HideOriginals()
        {
            _originalEnabled ??= new bool[_originals.Length];
            for (int i = 0; i < _originals.Length; i++)
            {
                Renderer renderer = _originals[i];
                if (renderer == null) continue;
                _originalEnabled[i] = renderer.enabled;
                renderer.enabled = false;
            }
        }

        private void RestoreOriginals()
        {
            if (_originals == null) return;
            for (int i = 0; i < _originals.Length; i++)
                if (_originals[i] != null) _originals[i].enabled = _originalEnabled[i];
        }

        // 원래 모델 렌더러는 처음 숨기기 전, 잔해를 만들기 전에 잡습니다. 모델 부분 규칙(UnitModelParts.Is) 밖인 체력 글자·HUD·커서는 뺍니다.
        private Renderer[] CaptureOriginals()
        {
            var models = new List<Renderer>();
            foreach (Renderer renderer in _root.GetComponentsInChildren<Renderer>(true))
                if (UnitModelParts.Is(renderer))
                    models.Add(renderer);
            return models.ToArray();
        }

        // 잔해는 유닛 뿌리 레이어를 물려받습니다(프리팹은 Default일 수 있고 유닛은 Entity라 카메라·윤곽이 같은 층을 보게).
        private GameObject InstanceOf(GameObject prefab)
        {
            if (_instances.TryGetValue(prefab, out GameObject instance) && instance != null)
                return instance;
            instance = Object.Instantiate(prefab, _root, false);
            instance.name = prefab.name;
            int layer = _root.gameObject.layer;
            foreach (Transform child in instance.GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = layer;
            instance.SetActive(false);
            _instances[prefab] = instance;
            return instance;
        }
    }
}
