// Copyright 2022 by PeopleWare n.v..
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Castle.Core.Logging;
using Castle.MicroKernel.Lifestyle.Scoped;

using JetBrains.Annotations;

using PPWCode.Vernacular.Exceptions.III;

using Quartz;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace PPWCode.Vernacular.Quartz.I
{
    /// <inheritdoc cref="IQuartzScopedJob" />
    /// <inheritdoc cref="IDisposable" />
    public abstract class QuartzScopedJob
        : IQuartzScopedJob,
          IDisposable
    {
        private static readonly MethodInfo _setCurrentScope;
        private readonly ISet<IDisposable> _disposables = new HashSet<IDisposable>();
        private readonly object _locker = new object();
        private CancellationToken? _cancellationToken;
        private bool _disposed;
        private ILogger _logger = NullLogger.Instance;

        static QuartzScopedJob()
        {
            MethodInfo method = typeof(CallContextLifetimeScope).GetMethod("SetCurrentScope", BindingFlags.Static | BindingFlags.NonPublic);
            if (method != null)
            {
                ParameterInfo[] parameters = method.GetParameters();
                if (parameters.Length == 1)
                {
                    ParameterInfo parameter = parameters[0];
                    if (parameter.ParameterType == typeof(CallContextLifetimeScope))
                    {
                        _setCurrentScope = method;
                    }
                }
            }
        }

        protected virtual CancellationToken CancellationToken
            => _cancellationToken ?? CancellationToken.None;

        [CanBeNull]
        [UsedImplicitly]
        public CallContextLifetimeScope Scope { get; set; }

        [UsedImplicitly]
        public ILogger Logger
        {
            get => _logger;
            set
            {
                if (value != null)
                {
                    _logger = value;
                }
            }
        }

        /// <summary>
        ///     Gets or sets a value indicating whether to unschedule firing trigger.
        /// </summary>
        /// <value>
        ///     <c>true</c> if firing trigger should be unscheduled; otherwise, <c>false</c>.
        /// </value>
        protected virtual bool UnscheduleFiringTrigger { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to unschedule all triggers.
        /// </summary>
        /// <value>
        ///     <c>true</c> if all triggers should be unscheduled; otherwise, <c>false</c>.
        /// </value>
        protected virtual bool UnscheduleAllTriggers { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether to refire immediately.
        /// </summary>
        /// <value><c>true</c> if to refire immediately; otherwise, <c>false</c>.</value>
        public virtual bool RefireImmediately { get; set; }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void RegisterForDispose(IDisposable disposable)
        {
            CheckDisposed();

            if (disposable != null)
            {
                lock (_locker)
                {
                    _disposables.Add(disposable);
                }
            }
        }

        /// <inheritdoc />
        public virtual Task Execute(IJobExecutionContext context)
        {
            CheckDisposed();
            TrySetResolvingScope();
            _cancellationToken = context.CancellationToken;

            QuartzJobExecutionContext quartzJobExecutionContext = new QuartzJobExecutionContext(context);

            Task ExecuteBody(QuartzJobExecutionContext executionContext)
                => ExecuteBodyAsync(executionContext.JobExecutionContext);

            return TimeAsync(quartzJobExecutionContext, ExecuteBody);
        }

        [NotNull]
        protected virtual async Task ExecuteBodyAsync([NotNull] IJobExecutionContext context)
        {
            try
            {
                context.CancellationToken.ThrowIfCancellationRequested();
                await OnPreExecuteAsync(context).ConfigureAwait(false);
                context.CancellationToken.ThrowIfCancellationRequested();
                await OnExecuteAsync(context).ConfigureAwait(false);
                context.CancellationToken.ThrowIfCancellationRequested();
                await OnPostExecuteAsync(context).ConfigureAwait(false);
                context.CancellationToken.ThrowIfCancellationRequested();
            }
            catch (Exception e)
            {
                bool rethrow = await OnExceptionAsync(context, e).ConfigureAwait(false);
                if (rethrow)
                {
                    throw;
                }
            }
        }

        protected void TrySetResolvingScope()
        {
            if ((Scope != null) && (_setCurrentScope != null))
            {
                try
                {
                    CallContextLifetimeScope currentScope = CallContextLifetimeScope.ObtainCurrentScope();
                    if (currentScope == null)
                    {
                        _setCurrentScope.Invoke(null, new object[] { Scope });
                    }
                }
                catch (Exception e)
                {
                    Logger.Error("Failing while adjusting our scope", e);
                }
            }
        }

        [NotNull]
        protected virtual async Task TimeAsync<TQuartzJobExecutionContext>(
            [NotNull] TQuartzJobExecutionContext quartzJobExecutionContext,
            [NotNull] Func<TQuartzJobExecutionContext, Task> lambda)
            where TQuartzJobExecutionContext : QuartzJobExecutionContext
        {
            if (Logger.IsInfoEnabled)
            {
                IJobExecutionContext context = quartzJobExecutionContext.JobExecutionContext;
                Stopwatch sw = Stopwatch.StartNew();
                try
                {
                    if (Logger.IsInfoEnabled)
                    {
                        Logger.Info(GatherJobInformation(context).ToString());
                    }

                    await lambda(quartzJobExecutionContext).ConfigureAwait(false);
                }
                finally
                {
                    sw.Stop();
                    Logger.Info(
                        context.CancellationToken.IsCancellationRequested
                            ? $"Job {context.JobDetail.Key} was cancelled after {sw.ElapsedMilliseconds} milliseconds."
                            : $"Job {context.JobDetail.Key} was executed in {sw.ElapsedMilliseconds} milliseconds.");
                }
            }
            else
            {
                await lambda(quartzJobExecutionContext).ConfigureAwait(false);
            }
        }

        [NotNull]
        protected abstract Task OnExecuteAsync([NotNull] IJobExecutionContext context);

        [NotNull]
        protected abstract Task OnPreExecuteAsync([NotNull] IJobExecutionContext context);

        [NotNull]
        protected abstract Task OnPostExecuteAsync([NotNull] IJobExecutionContext context);

        [NotNull]
        protected virtual Task<bool> OnExceptionAsync(
            [NotNull] IJobExecutionContext context,
            [NotNull] Exception exception)
        {
            if (exception is JobExecutionException)
            {
                return Task.FromResult(true);
            }

            string msg = $"Re-wrapping {exception.GetType().FullName} to a JobExecutionException.";
            Logger.Error(msg);
            JobExecutionException jobExecutionException =
                new JobExecutionException(msg, exception, RefireImmediately)
                {
                    UnscheduleAllTriggers = UnscheduleAllTriggers,
                    UnscheduleFiringTrigger = UnscheduleFiringTrigger,
                };
            throw jobExecutionException;
        }

        [NotNull]
        protected virtual StringBuilder GatherJobInformation([NotNull] IJobExecutionContext context)
        {
            StringBuilder sb = new StringBuilder();
            try
            {
                sb
                    .AppendLine()
                    .AppendLine($"Executing job {context.JobInstance.GetType().FullName}")
                    .AppendLine($"  Scheduler                    : {context.Scheduler.SchedulerName}")
                    .AppendLine($"  Key                          : {context.JobDetail.Key.Name}.{context.JobDetail.Key.Group}")
                    .AppendLine($"  JobType                      : {context.JobDetail.JobType.FullName}")
                    .AppendLine($"  Description                  : {context.JobDetail.Description}")
                    .AppendLine($"  Durable                      : {context.JobDetail.Durable}")
                    .AppendLine($"  RequestsRecovery             : {context.JobDetail.RequestsRecovery}")
                    .AppendLine($"  ConcurrentExecutionDisallowed: {context.JobDetail.ConcurrentExecutionDisallowed}")
                    .AppendLine($"  PersistJobDataAfterExecution : {context.JobDetail.PersistJobDataAfterExecution}")
                    .AppendLine("  Parameters");
                foreach (KeyValuePair<string, object> pair in context.MergedJobDataMap)
                {
                    string s;
                    try
                    {
                        s = Convert.ToString(pair.Value);
                    }
                    catch
                    {
                        s = "<object>";
                    }

                    sb.AppendLine($"    {pair.Key}: [{s}]");
                }
            }
            catch (Exception e)
            {
                sb.Append($"Exception occured while gathering job information {e}");
            }

            return sb;
        }

        protected virtual void OnReleaseUnmanagedResources()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            OnReleaseUnmanagedResources();
            if (disposing)
            {
                lock (_locker)
                {
                    if (!_disposed)
                    {
                        IDisposable[] disposables = _disposables.ToArray();
                        foreach (IDisposable disposable in disposables)
                        {
                            try
                            {
                                disposable.Dispose();
                                _disposables.Remove(disposable);
                            }
                            catch (Exception e)
                            {
                                Logger.Error("Unexpected exception occurred while disposing", e);
                            }
                        }

                        _disposed = true;
                    }
                }
            }
        }

        protected void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectAlreadyDisposedError();
            }
        }

        ~QuartzScopedJob()
        {
            Dispose(false);
        }
    }
}
