using System;
using System.Collections.Generic;

namespace ProjectL.Framework.Utility
{
    public readonly struct PoolContext<T>
    {
        public readonly Func<T> CreateFunc;
        public readonly int initialSize;
        public readonly IPoolReturnRule<T> ReturnRule;
        public readonly IPoolLifecycle<T> Lifecycle;

        public PoolContext(
            Func<T> createFunc,
            int initialSize,
            IPoolReturnRule<T> returnRule = null,
            IPoolLifecycle<T> lifecycle = null)
        {
            CreateFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            this.initialSize = initialSize;

            ReturnRule = returnRule;
            Lifecycle = lifecycle;
        }
    }

    public interface IPoolLifecycle<T>
    {
        void OnGet(T item);
        void OnBeforeReturn(T item);
        void OnReturn(T item);
    }

    public interface IPoolReturnRule<T>
    {
        bool CanReturn(T item);
    }

    public interface IPoolService<T> where T : class
    {
        T Get();
        void Return(T item);
    }

    public class CustomPool<T> : IPoolService<T> where T : class
    {
        protected readonly Queue<T> _pool;
        protected readonly Func<T> _createFunc;

        protected readonly IPoolReturnRule<T> _returnRule;
        protected readonly IPoolLifecycle<T> _lifecycle;

        public CustomPool(PoolContext<T> context)
        {
            Validate(context);

            _createFunc = context.CreateFunc;
            _returnRule = context.ReturnRule;
            _lifecycle = context.Lifecycle;

            _pool = new Queue<T>(context.initialSize);

            Prewarm(context.initialSize);
        }

        private void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                T item = CreateInstance();

                if (item == null)
                    throw new InvalidOperationException("CreateFunc returned null during prewarm.");

                _pool.Enqueue(item);
            }
        }

        protected virtual T CreateInstance()
        {
            return _createFunc();
        }

        public virtual T Get()
        {
            T item = _pool.Count > 0
                ? _pool.Dequeue()
                : CreateInstance();

            _lifecycle?.OnGet(item);

            return item;
        }

        public virtual void Return(T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            _lifecycle?.OnBeforeReturn(item);

            if (_returnRule != null && !_returnRule.CanReturn(item))
            {
                // ��å������ ���� (silent return)
                // �ʿ��ϸ� ���⼭ log/exception���� ���� ����
                return;
            }

            _pool.Enqueue(item);

            _lifecycle?.OnReturn(item);
        }

        private static void Validate(PoolContext<T> context)
        {
            if (context.CreateFunc == null)
                throw new ArgumentNullException(nameof(context.CreateFunc));

            if (context.initialSize < 0)
                throw new ArgumentNullException(nameof(context.initialSize));
        }
    }
}