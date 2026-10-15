using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.Render.Table.Effect.Enum;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.RenderSystem.Core.EffectSystem
{
    public readonly struct EffectScopeRequest
    {
        public EffectType EffectType { get; }
        public Transform TargetParent { get; }
        public Vector3 LocalPosition { get; }
        public Quaternion LocalRotation { get; }

        public EffectScopeRequest(
            EffectType effectType,
            Transform targetParent,
            Vector3 localPosition,
            Quaternion localRotation)
        {
            EffectType = effectType;
            TargetParent = targetParent;
            LocalPosition = localPosition;
            LocalRotation = localRotation;
        }
    }

    internal interface IDisposaHandle 
    {
        void DisposHandle();
    }

    public class EffectScope : IDisposaHandle
    {
        private List<ElementNode> elements = new List<ElementNode>();
        private IEffectSystemScopeServices EffectSystemHandleServices;

        internal EffectScope(IEffectSystemScopeServices effectSystemHandleServices) 
        {
            EffectSystemHandleServices = effectSystemHandleServices;
        }

        public void Play(EffectScopeRequest handleRequset) 
        {
            ElementNode Node = EffectSystemHandleServices.GetElementNode(handleRequset.EffectType);
            Node.particleStopNotifier.transform.SetParent(handleRequset.TargetParent);
            Node.particleStopNotifier.transform.localPosition = handleRequset.LocalPosition;
            Node.particleStopNotifier.transform.localRotation = handleRequset.LocalRotation;
            Node.particleStopNotifier.gameObject.SetActive(true);

            Node.particleStopNotifier.Play(() => Stop(Node));

            elements.Add(Node);
        }

        public void StopAll()
        {
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                elements[i].particleStopNotifier.ClearCallback();
                EffectSystemHandleServices.ReturnElementNode(elements[i]);
            }

            elements.Clear();
        }

        private void Stop(ElementNode node)
        {
            if (elements.Remove(node) == false)
                return;

            EffectSystemHandleServices.ReturnElementNode(node);
        }

        void IDisposaHandle.DisposHandle()
        {
            for (int i = elements.Count - 1; i >= 0; i--)
            {
                elements[i].particleStopNotifier.ClearCallback();
                elements[i].particleStopNotifier.StopImmediately();
                EffectSystemHandleServices.ReturnElementNode(elements[i]);
            }

            elements.Clear();
        }
    }
}
