using System;
using System.Collections.Generic;

namespace ProjectL.Systems.RuntimeSystem.ErrorHandling
{
    /// <summary>
    /// 정리 작업의 실패를 모으며 끝까지 진행하고, 마지막에 한 번만 던지는 규칙의 공용 구현입니다
    /// (SOLID·SSOT 개선 설계 D5, 구조 리팩토링 설계 D3).
    /// </summary>
    /// <remarks>
    /// 실패 목록은 처음 실패할 때만 만듭니다. 정리 하나가 실패해도 나머지 정리는 계속 시도합니다.
    /// 실패 원인을 보고하거나 기록하지 않습니다. 던질지, 오류 경계에 보고할지는 호출자가 정합니다.
    /// </remarks>
    internal static class CleanupErrors
    {
        /// <summary>정리 하나를 실행하고, 실패하면 예외를 목록에 더합니다. null 정리도 실패로 모읍니다.</summary>
        internal static void Try(Action cleanup, ref List<Exception> errors)
        {
            try
            {
                if (cleanup == null)
                    throw new ArgumentNullException(nameof(cleanup));
                cleanup();
            }
            catch (Exception exception)
            {
                (errors ??= new List<Exception>()).Add(exception);
            }
        }

        /// <summary>모아 둔 실패가 있으면 <see cref="AggregateException"/>으로 한 번 던집니다. 빈 목록은 실패가 없는 것으로 봅니다.</summary>
        internal static void ThrowIfAny(List<Exception> errors, string message)
        {
            if (errors != null && errors.Count > 0)
                throw new AggregateException(message, errors);
        }

        /// <summary>모든 정리를 순서대로 시도하고, 실패가 있으면 모아서 한 번 던집니다.</summary>
        internal static void RunAll(string message, params Action[] cleanups)
        {
            if (cleanups == null)
                throw new ArgumentNullException(nameof(cleanups));

            List<Exception> errors = null;
            for (int i = 0; i < cleanups.Length; i++)
                Try(cleanups[i], ref errors);
            ThrowIfAny(errors, message);
        }
    }
}
