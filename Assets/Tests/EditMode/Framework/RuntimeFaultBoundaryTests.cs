using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.Common;

namespace ProjectL.Tests.Framework
{
    /// <summary>
    /// 런타임 공통 오류 경계(F1, 설계 W1·W17)의 최초 오류 고정, 입력 정리, 1회 보고를 검증합니다.
    /// </summary>
    public sealed class RuntimeFaultBoundaryTests
    {
        private readonly List<Exception> _logs = new();

        // NUnit은 한 Fixture 인스턴스를 모든 테스트가 공유하므로 로그를 테스트마다 비운다.
        [SetUp]
        public void SetUp()
        {
            _logs.Clear();
        }

        [Test]
        public void Constructor_RequiresLogCallback()
        {
            Assert.Throws<ArgumentNullException>(() => new RuntimeFaultBoundary(null));
        }

        [Test]
        public void Report_FixesFirstError_StopsInputOnce_AndLogsOnce()
        {
            RuntimeFaultBoundary faults = Create();
            int stopCount = 0;
            faults.ConnectStopInput(() => stopCount++);
            var first = new InvalidOperationException("first");

            faults.Report(first);
            faults.Report(new InvalidOperationException("second"));

            Assert.That(faults.IsFaulted, Is.True);
            Assert.That(faults.FirstError, Is.SameAs(first));
            Assert.That(stopCount, Is.EqualTo(1));
            Assert.That(_logs, Is.EqualTo(new Exception[] { first }));
        }

        [Test]
        public void Report_IsFaultedBeforeInputCleanup_AndIgnoresReentrantReports()
        {
            RuntimeFaultBoundary faults = Create();
            bool faultedDuringCleanup = false;
            var first = new InvalidOperationException("first");
            faults.ConnectStopInput(() =>
            {
                // 정리 중 Unbind·ClearLayer가 부른 콜백이 다시 보고하는 경우를 흉내 낸다.
                faultedDuringCleanup = faults.IsFaulted;
                faults.Report(new InvalidOperationException("reentrant"));
            });

            faults.Report(first);

            Assert.That(faultedDuringCleanup, Is.True);
            Assert.That(faults.FirstError, Is.SameAs(first));
            Assert.That(_logs, Is.EqualTo(new Exception[] { first }));
        }

        [Test]
        public void Report_CollectsCleanupFailureWithOriginalCause()
        {
            RuntimeFaultBoundary faults = Create();
            var first = new InvalidOperationException("first");
            var cleanup = new InvalidOperationException("cleanup");
            faults.ConnectStopInput(() => throw cleanup);

            faults.Report(first);

            Assert.That(faults.IsFaulted, Is.True);
            Assert.That(faults.FirstError, Is.SameAs(first));
            Assert.That(_logs, Has.Count.EqualTo(1));
            var aggregate = (AggregateException)_logs[0];
            Assert.That(aggregate.InnerExceptions, Is.EqualTo(new Exception[] { first, cleanup }));
        }

        [Test]
        public void Report_WorksWithoutConnectedInputCleanup()
        {
            RuntimeFaultBoundary faults = Create();
            var first = new InvalidOperationException("first");

            faults.Report(first);

            Assert.That(faults.IsFaulted, Is.True);
            Assert.That(_logs, Is.EqualTo(new Exception[] { first }));
        }

        [Test]
        public void Report_RejectsNull()
        {
            RuntimeFaultBoundary faults = Create();

            Assert.Throws<ArgumentNullException>(() => faults.Report(null));
            Assert.That(faults.IsFaulted, Is.False);
        }

        [Test]
        public void ConnectStopInput_AllowsOnlyOneCleanupCallback()
        {
            RuntimeFaultBoundary faults = Create();
            Action cleanup = () => { };
            faults.ConnectStopInput(cleanup);

            Assert.DoesNotThrow(() => faults.ConnectStopInput(cleanup));
            Assert.Throws<InvalidOperationException>(() => faults.ConnectStopInput(() => { }));
            Assert.Throws<ArgumentNullException>(() => faults.ConnectStopInput(null));
        }

        [Test]
        public void StopInputForShutdown_RunsCleanupWithoutFaulting_AndReturnsFailure()
        {
            RuntimeFaultBoundary faults = Create();
            int stopCount = 0;
            var cleanup = new InvalidOperationException("cleanup");
            faults.ConnectStopInput(() =>
            {
                stopCount++;
                throw cleanup;
            });

            Exception failure = faults.StopInputForShutdown();

            Assert.That(failure, Is.SameAs(cleanup));
            Assert.That(stopCount, Is.EqualTo(1));
            Assert.That(faults.IsFaulted, Is.False);
            Assert.That(_logs, Is.Empty);
        }

        [Test]
        public void DisconnectStopInput_ReleasesOnlyTheConnectedCallback()
        {
            RuntimeFaultBoundary faults = Create();
            int stopCount = 0;
            Action stop = () => stopCount++;
            faults.ConnectStopInput(stop);

            faults.DisconnectStopInput(() => { });
            faults.StopInputForShutdown();
            Assert.That(stopCount, Is.EqualTo(1), "다른 콜백으로는 해제하지 않는다.");

            faults.DisconnectStopInput(stop);
            faults.Report(new InvalidOperationException("after disconnect"));

            Assert.That(stopCount, Is.EqualTo(1));
            Assert.That(_logs.Count, Is.EqualTo(1));
            Assert.DoesNotThrow(() => faults.ConnectStopInput(() => { }));
            Assert.Throws<ArgumentNullException>(() => faults.DisconnectStopInput(null));
        }

        private RuntimeFaultBoundary Create()
        {
            return new RuntimeFaultBoundary(_logs.Add);
        }
    }
}
