// Copyright 2020 by PeopleWare n.v..
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
using System.Collections.Concurrent;

using Castle.Core.Internal;
using Castle.Core.Logging;
using Castle.MicroKernel;
using Castle.MicroKernel.Lifestyle;
using Castle.MicroKernel.Lifestyle.Scoped;

using JetBrains.Annotations;

using Quartz;
using Quartz.Spi;

namespace PPWCode.Vernacular.Quartz.I
{
    /// <inheritdoc cref="IJobFactory" />
    [UsedImplicitly]
    public class QuartzJobFactory : IJobFactory
    {
        private static readonly ConcurrentDictionary<IQuartzScopedJob, IDisposable> _scopes
            = new ConcurrentDictionary<IQuartzScopedJob, IDisposable>();

        public QuartzJobFactory([NotNull] IKernel kernel)
        {
            Kernel = kernel;
        }

        [NotNull]
        public IKernel Kernel { get; }

        [NotNull]
        [UsedImplicitly]
        public ILogger Logger { get; set; } = NullLogger.Instance;

        /// <summary>
        ///     Called by the scheduler at the time of the trigger firing, in order to
        ///     produce a <see cref="T:Quartz.IJob" /> instance on which to call Execute.
        /// </summary>
        /// <remarks>
        ///     It should be extremely rare for this method to throw an exception -
        ///     basically only the case where there is no way at all to instantiate
        ///     and prepare the Job for execution.  When the exception is thrown, the
        ///     Scheduler will move all triggers associated with the Job into the
        ///     <see cref="F:Quartz.TriggerState.Error" /> state, which will require human
        ///     intervention (e.g. an application restart after fixing whatever
        ///     configuration problem led to the issue with instantiating the Job.
        /// </remarks>
        /// <param name="bundle">
        ///     The TriggerFiredBundle from which the <see cref="T:Quartz.IJobDetail" />
        ///     and other info relating to the trigger firing can be obtained.
        /// </param>
        /// <param name="scheduler">a handle to the scheduler that is about to execute the job</param>
        /// <throws>SchedulerException if there is a problem instantiating the Job. </throws>
        /// <returns>
        ///     the newly instantiated Job
        /// </returns>
        [NotNull]
        public virtual IJob NewJob([NotNull] TriggerFiredBundle bundle, [NotNull] IScheduler scheduler)
        {
            try
            {
                OnBeforeNewJob(bundle, scheduler);

                IJob job;
                if (bundle.JobDetail.JobType.Is<IQuartzScopedJob>())
                {
                    // create scope linked to the Quartz job
                    IDisposable scope = Kernel.BeginScope();
                    try
                    {
                        IQuartzScopedJob quartzScopedJob = ResolveJob<IQuartzScopedJob>(bundle, scheduler, scope as CallContextLifetimeScope);
                        job = quartzScopedJob;
                        if (!_scopes.TryAdd(quartzScopedJob, scope))
                        {
                            throw new Exception($"Failed to add scope to job '{bundle.JobDetail.JobType.FullName}'");
                        }
                    }
                    catch
                    {
                        scope?.Dispose();
                        throw;
                    }
                }
                else
                {
                    job = ResolveJob<IJob>(bundle, scheduler, null);
                }

                OnAfterNewJob(bundle, scheduler, job);

                return job;
            }
            catch (SchedulerException)
            {
                throw;
            }
            catch (Exception e)
            {
                string msg = $"Problem instantiating class '{bundle.JobDetail.JobType.FullName}'";
                Logger.Error(msg, e);
                throw new SchedulerException(msg, e);
            }
        }

        public virtual void ReturnJob([NotNull] IJob job)
        {
            if (job is IQuartzScopedJob scopedJob)
            {
                // release scope
                if (_scopes.TryRemove(scopedJob, out IDisposable disposable))
                {
                    disposable.Dispose();
                }
                else
                {
                    Logger.Error($"Failed to find associated scope to Job: {job}");
                }
            }
            else
            {
                Kernel.ReleaseComponent(job);
            }
        }

        protected virtual void OnBeforeNewJob([NotNull] TriggerFiredBundle bundle, [NotNull] IScheduler scheduler)
        {
        }

        protected virtual void OnAfterNewJob([NotNull] TriggerFiredBundle bundle, [NotNull] IScheduler scheduler, [NotNull] IJob job)
        {
        }

        [NotNull]
        protected virtual T ResolveJob<T>([NotNull] TriggerFiredBundle bundle, [NotNull] IScheduler scheduler, [CanBeNull] CallContextLifetimeScope scope)
            where T : IJob
        {
            Arguments arguments =
                new Arguments()
                    .AddNamed(nameof(scope), scope);
            return (T)Kernel.Resolve(bundle.JobDetail.JobType, arguments);
        }
    }
}
