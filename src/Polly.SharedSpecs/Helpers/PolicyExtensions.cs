﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Polly.Specs.Helpers
{
    public static class PolicyExtensions
    {
        public static void RaiseException<TException>(this Policy policy, int numberOfTimesToRaiseException, Action<TException, int> configureException = null) where TException : Exception, new()
        {
            int counter = 0;

            policy.Execute(() =>
            {
                if (counter < numberOfTimesToRaiseException)
                {
                    counter++;

                    var exception = new TException();
                    
                    if (configureException != null)
                    {
                        configureException(exception, counter);
                    }

                    throw exception;
                }
            });
        }

        public static void RaiseException<TException>(this Policy policy, Action<TException, int> configureException = null) where TException : Exception, new()
        {
            policy.RaiseException(1, configureException);
        }

        public static Task RaiseExceptionAsync<TException>(this Policy policy, int numberOfTimesToRaiseException, Action<TException, int> configureException = null, CancellationToken cancellationToken = default(CancellationToken)) where TException : Exception, new()
        {
            int counter = 0;

            return policy.ExecuteAsync(ct =>
            {
                if (counter < numberOfTimesToRaiseException)
                {
                    counter++;

                    var exception = new TException();

                    if (configureException != null)
                    {
                        configureException(exception, counter);
                    }

                    throw exception;
                }
                return Task.FromResult(true) as Task;
            }, cancellationToken);
        }

        public static Task RaiseExceptionAsync<TException>(this Policy policy, Action<TException, int> configureException = null) where TException : Exception, new()
        {
            return policy.RaiseExceptionAsync(1, configureException);
        }

        public class ExceptionAndOrCancellationScenario
        {
            public int NumberOfTimesToRaiseException = 0;

            public int? AttemptDuringWhichToCancel = null;

            public bool ActionObservesCancellation = true;
        }

        public static Task RaiseExceptionAndOrCancellationAsync<TException>(this Policy policy, ExceptionAndOrCancellationScenario scenario, CancellationTokenSource cancellationTokenSource, Action onExecute) where TException : Exception, new()
        {
            return policy.RaiseExceptionAndOrCancellationAsync<TException, int>(scenario, cancellationTokenSource, onExecute, 0);
        }

        public static Task<TResult> RaiseExceptionAndOrCancellationAsync<TException, TResult>(this Policy policy, ExceptionAndOrCancellationScenario scenario, CancellationTokenSource cancellationTokenSource, Action onExecute, TResult successResult) where TException : Exception, new()
        {
            int counter = 0;

            CancellationToken cancellationToken = cancellationTokenSource.Token;

            return policy.ExecuteAsync(ct =>
            {
                onExecute();

                counter++;

                if (scenario.AttemptDuringWhichToCancel.HasValue && counter >= scenario.AttemptDuringWhichToCancel.Value)
                {
                    cancellationTokenSource.Cancel();
                }

                if (scenario.ActionObservesCancellation)
                {
                    ct.ThrowIfCancellationRequested();
                }

                if (counter <= scenario.NumberOfTimesToRaiseException)
                {
                    throw new TException();
                }

                return Task.FromResult(successResult);
            }, cancellationToken);
        }

#if PORTABLE
        public static Func<Task> Awaiting(this Policy policy, Func<Policy, Task> action)
        {
            return (Func<Task>)(() => action(policy));
        }
#endif
    }
}