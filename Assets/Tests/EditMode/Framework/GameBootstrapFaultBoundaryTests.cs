using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.Common;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Boot;
using UnityEngine;
using UnityEngine.TestTools;

namespace ProjectL.Tests.Framework
{
    /// <summary>실제 Bootstrap Update/종료 콜백과 Runtime·Runner의 F1 경계를 검증합니다.</summary>
    public sealed class GameBootstrapFaultBoundaryTests
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo ActiveInstance = typeof(GameBootstrap).GetField(
            "_activeInstance", BindingFlags.Static | BindingFlags.NonPublic);
        private readonly List<Exception> _logs = new();
        private GameObject _object;
        private GameBootstrap _bootstrap;
        private RuntimeFaultBoundary _faults;
        private readonly JobRunnerSystem _runner = new();
        private ProbeModule _probe;

        [SetUp]
        public void SetUp()
        {
            if (ActiveInstance.GetValue(null) != null)
                Assert.Ignore("실행 중인 사용자 Bootstrap이 있어 소유권을 바꾸지 않습니다.");

            _logs.Clear();
            _runner.Clear();
            _faults = new RuntimeFaultBoundary(_logs.Add);
            _probe = new ProbeModule();
            var builder = new SystemBuilderHelper();
            _runner.ExportServices(builder);
            builder.AddModule<ISystemModule>(_probe);
            var runtime = new FrameworkRuntime(builder.BuildSystemReader());
            runtime.Initialize();

            // Boot 씬/DB 시작은 이 테스트 범위가 아니다. Unity 메시지를 자동 실행하지 않는 비활성 소유자를 쓴다.
            _object = new GameObject("Bootstrap F1 test");
            _object.SetActive(false);
            _bootstrap = _object.AddComponent<GameBootstrap>();
            ActiveInstance.SetValue(null, _bootstrap);
            Set("_runtime", runtime);
            Set("_faults", _faults);
            Set("_isRunning", true);
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                if (_bootstrap != null) Invoke("ShutdownOnce");
            }
            finally
            {
                if (ReferenceEquals(ActiveInstance.GetValue(null), _bootstrap))
                    ActiveInstance.SetValue(null, null);
                if (_object != null) UnityEngine.Object.DestroyImmediate(_object);
                _object = null;
                _bootstrap = null;
            }
        }

        [Test]
        public void JobException_StopsActualBootstrapTicks_WithoutCompletingOrRetryingJob()
        {
            var job = new FaultingJob();
            int inputStops = 0;
            _faults.ConnectStopInput(() => inputStops++);
            _runner.Register(job);

            Invoke("Update");
            Invoke("Update");
            Invoke("Update");

            Assert.That(job.Calls, Is.EqualTo(1));
            Assert.That(job.IsCompleted, Is.False);
            Assert.That(_runner.IsIdle, Is.False);
            Assert.That(_probe.Ticks, Is.Zero, "실패 Job 뒤 모듈까지 실행하지 않는다.");
            Assert.That(_faults.FirstError, Is.SameAs(job.Error));
            Assert.That(_logs, Is.EqualTo(new[] { job.Error }));
            Assert.That(inputStops, Is.EqualTo(1));

            Invoke("OnApplicationQuit");
            Invoke("OnDestroy");
            Invoke("Update");

            Assert.That(_runner.IsIdle, Is.True);
            Assert.That(_probe.Shutdowns, Is.EqualTo(1));
            Assert.That(job.Calls, Is.EqualTo(1));
        }

        [Test]
        public void ExternalInputFault_BlocksNextRuntimeTick_AndInitialTransition()
        {
            var error = new InvalidOperationException("Tick 밖 입력 실패");
            _faults.Report(error);

            Invoke("Update");
            // 전환이 시도되면 이 최소 런타임에 없는 LifeCycle 조회에서 실패한다.
            Assert.DoesNotThrow(() => Invoke("RequestInitialLifeCycle"));

            Assert.That(_probe.Ticks, Is.Zero);
            Assert.That(_logs, Is.EqualTo(new[] { error }));
        }

        [Test]
        public void NormalShutdown_StopsInputBeforeRuntime_AndNeverRunsQueuedJob()
        {
            var job = new FaultingJob();
            _runner.Register(job);
            _faults.ConnectStopInput(() => _probe.Events.Add("input"));

            Invoke("OnApplicationQuit");
            Invoke("OnDestroy");
            Invoke("Update");
            Assert.DoesNotThrow(() => Invoke("RequestInitialLifeCycle"));

            Assert.That(_probe.Events, Is.EqualTo(new[] { "input", "runtime" }));
            Assert.That(_probe.Shutdowns, Is.EqualTo(1));
            Assert.That(_runner.IsIdle, Is.True);
            Assert.That(job.Calls, Is.Zero);
            Assert.That(_faults.IsFaulted, Is.False);
            Assert.That(_logs, Is.Empty);
        }

        [Test]
        public void InputCleanupFailure_DoesNotSkipRuntimeShutdown_OrRetryOnDestroy()
        {
            _faults.ConnectStopInput(() => throw new InvalidOperationException("종료 입력 정리 실패"));
            LogAssert.Expect(LogType.Exception, new Regex("Input cleanup failed during shutdown"));

            Invoke("OnApplicationQuit");
            Invoke("OnDestroy");
            Invoke("Update");

            Assert.That(_probe.Shutdowns, Is.EqualTo(1));
            Assert.That(_probe.Ticks, Is.Zero);
        }

        [Test]
        public void RuntimeShutdownFailure_IsReported_WithoutRestartingOrRetryingFailedCleanup()
        {
            var job = new FaultingJob();
            _runner.Register(job);
            _probe.FailShutdown = true;
            LogAssert.Expect(LogType.Exception, new Regex("Runtime shutdown failed"));

            Invoke("OnApplicationQuit");
            Invoke("OnDestroy");
            Invoke("Update");

            Assert.That(_probe.Shutdowns, Is.EqualTo(1));
            Assert.That(job.Calls, Is.Zero);
            Assert.That(_probe.Ticks, Is.Zero);
            // Framework는 하위 Shutdown 실패 시 순회를 멈춘다. 미정리 자원을 정리됐다고 단언하지 않는다.
        }

        private void Set(string field, object value) => typeof(GameBootstrap).GetField(field, InstanceFlags).SetValue(_bootstrap, value);
        private void Invoke(string method) => typeof(GameBootstrap).GetMethod(method, InstanceFlags).Invoke(_bootstrap, null);

        private sealed class FaultingJob : IJobService
        {
            internal readonly Exception Error = new InvalidOperationException("실제 Runner 실행 실패");
            internal int Calls;
            public int Priority => 0;
            public bool IsCompleted => false;
            public void Execute() { Calls++; throw Error; }
        }

        private sealed class ProbeModule : ISystemTick, ISystemShutdown
        {
            internal readonly List<string> Events = new();
            internal int Ticks, Shutdowns;
            internal bool FailShutdown;
            public void Tick(float deltaTime) => Ticks++;
            public void Shutdown()
            {
                Shutdowns++;
                Events.Add("runtime");
                if (FailShutdown) throw new InvalidOperationException("하위 Runtime 정리 실패");
            }
        }
    }
}
