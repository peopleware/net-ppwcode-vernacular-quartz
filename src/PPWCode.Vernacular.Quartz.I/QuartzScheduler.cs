// Copyright 2024 by PeopleWare n.v..
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
using System.Threading;
using System.Threading.Tasks;

using Castle.Core;

using Common.Logging;

using JetBrains.Annotations;

using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Spi;

namespace PPWCode.Vernacular.Quartz.I
{
    /// <inheritdoc cref="StdScheduler" />
    /// <seealso cref="IScheduler" />
    /// <seealso cref="Castle.Core.IStartable" />
    /// <seealso cref="System.IDisposable" />
    [UsedImplicitly]
    public class QuartzScheduler
        : IQuartzScheduler,
          IStartable,
          IDisposable
    {
        private readonly ILog _log = LogManager.GetLogger(typeof(QuartzScheduler));

        /// <summary>
        ///     Constructs a Scheduler that uses Castle Windsor
        /// </summary>
        /// <param name="waitForJobsToCompleteAtShutdown">
        ///     Specifies what the scheduler should do with running jobs when shutting
        ///     down
        /// </param>
        /// <param name="schedulerFactory">Factory to create our schedulers</param>
        /// <param name="jobFactory">JobFactory</param>
        /// <param name="releasingJobListener">Job listener responsible for signaling Castle Windsor that this job is end-of-life</param>
        public QuartzScheduler(
            ISchedulerFactory schedulerFactory,
            IJobFactory jobFactory)
        {
            Scheduler = CreateScheduler(schedulerFactory);
            JobFactory = jobFactory;
        }

        /// <inheritdoc cref="IScheduler"/>
        public IScheduler Scheduler { get; }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc cref="IQuartzScheduler.WaitForJobsToCompleteAtShutdown"/>
        [DoNotWire]
        public bool WaitForJobsToCompleteAtShutdown { get; set; }

        /// <inheritdoc cref="IScheduler.SchedulerName"/>
        public string SchedulerName
            => Scheduler.SchedulerName;

        /// <inheritdoc cref="IScheduler.SchedulerInstanceId"/>
        public string SchedulerInstanceId
            => Scheduler.SchedulerInstanceId;

        /// <inheritdoc cref="IScheduler.Context"/>
        public SchedulerContext Context
            => Scheduler.Context;

        /// <inheritdoc cref="IScheduler.InStandbyMode"/>
        public bool InStandbyMode
            => Scheduler.InStandbyMode;

        /// <inheritdoc cref="IScheduler.IsShutdown"/>
        public bool IsShutdown
            => Scheduler.IsShutdown;

        /// <inheritdoc cref="IScheduler.JobFactory"/>
        public IJobFactory JobFactory
        {
            set => Scheduler.JobFactory = value;
        }

        /// <inheritdoc cref="IScheduler.ListenerManager"/>
        public IListenerManager ListenerManager
            => Scheduler.ListenerManager;

        /// <inheritdoc cref="IScheduler.IsStarted"/>
        public bool IsStarted
            => Scheduler.IsStarted;

        /// <inheritdoc cref="IScheduler.IsJobGroupPaused(string, CancellationToken)"/>
        public virtual Task<bool> IsJobGroupPaused(string groupName, CancellationToken token = default)
            => Scheduler.IsJobGroupPaused(groupName, token);

        /// <inheritdoc cref="IScheduler.IsTriggerGroupPaused(string, CancellationToken)"/>
        public virtual Task<bool> IsTriggerGroupPaused(string groupName, CancellationToken token = default)
            => Scheduler.IsTriggerGroupPaused(groupName, token);

        /// <inheritdoc cref="IScheduler.GetMetaData(CancellationToken)"/>
        public virtual Task<SchedulerMetaData> GetMetaData(CancellationToken token = default)
            => Scheduler.GetMetaData(token);

        /// <inheritdoc cref="IScheduler.GetCurrentlyExecutingJobs(CancellationToken)"/>
        public virtual Task<IReadOnlyCollection<IJobExecutionContext>> GetCurrentlyExecutingJobs(CancellationToken token = default)
            => Scheduler.GetCurrentlyExecutingJobs(token);

        /// <inheritdoc cref="IScheduler.GetJobGroupNames(CancellationToken)"/>
        public virtual Task<IReadOnlyCollection<string>> GetJobGroupNames(CancellationToken token = default)
            => Scheduler.GetJobGroupNames(token);

        /// <inheritdoc cref="IScheduler.GetTriggerGroupNames(CancellationToken)"/>
        public virtual Task<IReadOnlyCollection<string>> GetTriggerGroupNames(CancellationToken token = default)
            => Scheduler.GetTriggerGroupNames(token);

        /// <inheritdoc cref="IScheduler.GetPausedTriggerGroups(CancellationToken)"/>
        public virtual Task<IReadOnlyCollection<string>> GetPausedTriggerGroups(CancellationToken token = default)
            => Scheduler.GetPausedTriggerGroups(token);

        /// <inheritdoc cref="IScheduler.Start(CancellationToken)"/>
        public virtual Task Start(CancellationToken token)
            => Scheduler.Start(token);

        /// <inheritdoc cref="IScheduler.StartDelayed(TimeSpan, CancellationToken)"/>
        public virtual Task StartDelayed(TimeSpan delay, CancellationToken token = default)
            => Scheduler.StartDelayed(delay, token);

        /// <inheritdoc cref="IScheduler.Standby(CancellationToken)"/>
        public virtual Task Standby(CancellationToken token = default)
            => Scheduler.Standby(token);

        /// <inheritdoc cref="IScheduler.Shutdown(CancellationToken)"/>
        public virtual Task Shutdown(CancellationToken token = default)
            => StopAsync(token);

        /// <inheritdoc cref="IScheduler.Shutdown(bool, CancellationToken)"/>
        public virtual Task Shutdown(bool waitForJobsToComplete, CancellationToken token = default)
            => Scheduler.Shutdown(waitForJobsToComplete, token);

        /// <inheritdoc cref="IScheduler.ScheduleJob(IJobDetail, ITrigger, CancellationToken)"/>
        public virtual Task<DateTimeOffset> ScheduleJob(IJobDetail jobDetail, ITrigger trigger, CancellationToken token = default)
            => Scheduler.ScheduleJob(jobDetail, trigger, token);

        /// <inheritdoc cref="IScheduler.ScheduleJob(ITrigger, CancellationToken)"/>
        public virtual Task<DateTimeOffset> ScheduleJob(ITrigger trigger, CancellationToken token = default)
            => Scheduler.ScheduleJob(trigger, token);

        /// <inheritdoc cref="IScheduler.ScheduleJob(IJobDetail, IReadOnlyCollection{ITrigger}, bool, CancellationToken)"/>
        public virtual Task ScheduleJob(
            IJobDetail jobDetail,
            IReadOnlyCollection<ITrigger> triggersForJob,
            bool replace,
            CancellationToken token = default)
            => Scheduler.ScheduleJob(jobDetail, triggersForJob, replace, token);

        /// <inheritdoc cref="IScheduler.ScheduleJobs(IReadOnlyDictionary{IJobDetail, IReadOnlyCollection{ITrigger}}, bool, CancellationToken)"/>
        public virtual Task ScheduleJobs(IReadOnlyDictionary<IJobDetail, IReadOnlyCollection<ITrigger>> triggersAndJobs, bool replace, CancellationToken token = default)
            => Scheduler.ScheduleJobs(triggersAndJobs, replace, token);

        /// <inheritdoc cref="IScheduler.UnscheduleJob(TriggerKey, CancellationToken)"/>
        public virtual Task<bool> UnscheduleJob(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.UnscheduleJob(triggerKey, token);

        /// <inheritdoc cref="IScheduler.UnscheduleJobs(IReadOnlyCollection{TriggerKey}, CancellationToken)"/>
        public virtual Task<bool> UnscheduleJobs(IReadOnlyCollection<TriggerKey> triggerKeys, CancellationToken token = default)
            => Scheduler.UnscheduleJobs(triggerKeys, token);

        /// <inheritdoc cref="IScheduler.RescheduleJob(TriggerKey, ITrigger, CancellationToken)"/>
        public virtual Task<DateTimeOffset?> RescheduleJob(TriggerKey triggerKey, ITrigger newTrigger, CancellationToken token = default)
            => Scheduler.RescheduleJob(triggerKey, newTrigger, token);

        /// <inheritdoc cref="IScheduler.AddJob(IJobDetail, bool, CancellationToken)"/>
        public virtual Task AddJob(IJobDetail jobDetail, bool replace, CancellationToken token = default)
            => Scheduler.AddJob(jobDetail, replace, token);

        /// <inheritdoc cref="IScheduler.AddJob(IJobDetail, bool, bool, CancellationToken)"/>
        public virtual Task AddJob(
            IJobDetail jobDetail,
            bool replace,
            bool storeNonDurableWhileAwaitingScheduling,
            CancellationToken token = default)
            => Scheduler.AddJob(jobDetail, replace, storeNonDurableWhileAwaitingScheduling, token);

        /// <inheritdoc cref="IScheduler.DeleteJob(JobKey, CancellationToken)"/>
        public virtual Task<bool> DeleteJob(JobKey jobKey, CancellationToken token = default)
            => Scheduler.DeleteJob(jobKey, token);

        /// <inheritdoc cref="IScheduler.DeleteJobs(IReadOnlyCollection{JobKey}, CancellationToken)"/>
        public virtual Task<bool> DeleteJobs(IReadOnlyCollection<JobKey> jobKeys, CancellationToken token = default)
            => Scheduler.DeleteJobs(jobKeys, token);

        /// <inheritdoc cref="IScheduler.TriggerJob(JobKey, CancellationToken)"/>
        public virtual Task TriggerJob(JobKey jobKey, CancellationToken token = default)
            => Scheduler.TriggerJob(jobKey, token);

        /// <inheritdoc cref="IScheduler.TriggerJob(JobKey, JobDataMap, CancellationToken)"/>
        public virtual Task TriggerJob(JobKey jobKey, JobDataMap data, CancellationToken token = default)
            => Scheduler.TriggerJob(jobKey, data, token);

        /// <inheritdoc cref="IScheduler.PauseJob(JobKey, CancellationToken)"/>
        public virtual Task PauseJob(JobKey jobKey, CancellationToken token = default)
            => Scheduler.PauseJob(jobKey, token);

        /// <inheritdoc cref="IScheduler.PauseJobs(GroupMatcher{JobKey}, CancellationToken)"/>
        public virtual Task PauseJobs(GroupMatcher<JobKey> matcher, CancellationToken token = default)
            => Scheduler.PauseJobs(matcher, token);

        /// <inheritdoc cref="IScheduler.PauseTrigger(TriggerKey, CancellationToken)"/>
        public virtual Task PauseTrigger(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.PauseTrigger(triggerKey, token);

        /// <inheritdoc cref="IScheduler.PauseTriggers(GroupMatcher{TriggerKey}, CancellationToken)"/>
        public virtual Task PauseTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken token = default)
            => Scheduler.PauseTriggers(matcher, token);

        /// <inheritdoc cref="IScheduler.ResumeJob(JobKey, CancellationToken)"/>
        public virtual Task ResumeJob(JobKey jobKey, CancellationToken token = default)
            => Scheduler.ResumeJob(jobKey, token);

        /// <inheritdoc cref="IScheduler.ResumeJobs(GroupMatcher{JobKey}, CancellationToken)"/>
        public virtual Task ResumeJobs(GroupMatcher<JobKey> matcher, CancellationToken token = default)
            => Scheduler.ResumeJobs(matcher, token);

        /// <inheritdoc cref="IScheduler.ResumeTrigger(TriggerKey, CancellationToken)"/>
        public virtual Task ResumeTrigger(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.ResumeTrigger(triggerKey, token);

        /// <inheritdoc cref="IScheduler.ResumeTriggers(GroupMatcher{TriggerKey}, CancellationToken)"/>
        public virtual Task ResumeTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken token = default)
            => Scheduler.ResumeTriggers(matcher, token);

        /// <inheritdoc cref="IScheduler.PauseAll(CancellationToken)"/>
        public virtual Task PauseAll(CancellationToken token = default)
            => Scheduler.PauseAll(token);

        /// <inheritdoc cref="IScheduler.ResumeAll(CancellationToken)"/>
        public virtual Task ResumeAll(CancellationToken token = default)
            => Scheduler.ResumeAll(token);

        /// <inheritdoc cref="IScheduler.GetJobKeys(GroupMatcher{JobKey}, CancellationToken)"/>
        public virtual Task<IReadOnlyCollection<JobKey>> GetJobKeys(GroupMatcher<JobKey> matcher, CancellationToken token = default)
            => Scheduler.GetJobKeys(matcher, token);

        /// <inheritdoc cref="IScheduler.GetTriggersOfJob(JobKey, CancellationToken)"/>
        public virtual Task<IReadOnlyCollection<ITrigger>> GetTriggersOfJob(JobKey jobKey, CancellationToken token = default)
            => Scheduler.GetTriggersOfJob(jobKey, token);

        /// <inheritdoc cref="IScheduler.GetTriggerKeys(GroupMatcher{TriggerKey}, CancellationToken)"/>
        public virtual Task<IReadOnlyCollection<TriggerKey>> GetTriggerKeys(GroupMatcher<TriggerKey> matcher, CancellationToken token = default)
            => Scheduler.GetTriggerKeys(matcher, token);

        /// <inheritdoc cref="IScheduler.GetJobDetail(JobKey, CancellationToken)"/>
        public virtual Task<IJobDetail> GetJobDetail(JobKey jobKey, CancellationToken token = default)
            => Scheduler.GetJobDetail(jobKey, token);

        /// <inheritdoc cref="IScheduler.GetTrigger(TriggerKey, CancellationToken)"/>
        public virtual Task<ITrigger> GetTrigger(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.GetTrigger(triggerKey, token);

        /// <inheritdoc cref="IScheduler.GetTriggerState(TriggerKey, CancellationToken)"/>
        public virtual Task<TriggerState> GetTriggerState(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.GetTriggerState(triggerKey, token);

        /// <inheritdoc cref="IScheduler.ResetTriggerFromErrorState(TriggerKey, CancellationToken)"/>
        public Task ResetTriggerFromErrorState(TriggerKey triggerKey, CancellationToken cancellationToken = default)
            => Scheduler.ResetTriggerFromErrorState(triggerKey, cancellationToken);

        /// <inheritdoc cref="IScheduler.AddCalendar(string, ICalendar, bool, bool, CancellationToken)"/>
        public virtual Task AddCalendar(
            string calName,
            ICalendar calendar,
            bool replace,
            bool updateTriggers,
            CancellationToken token = default)
            => Scheduler.AddCalendar(calName, calendar, replace, updateTriggers, token);

        /// <inheritdoc cref="IScheduler.DeleteCalendar(string, CancellationToken)"/>
        public virtual Task<bool> DeleteCalendar(string calName, CancellationToken token = default)
            => Scheduler.DeleteCalendar(calName, token);

        /// <inheritdoc cref="IScheduler.GetCalendar(string, CancellationToken)"/>
        public virtual Task<ICalendar> GetCalendar(string calName, CancellationToken token = default)
            => Scheduler.GetCalendar(calName, token);

        /// <inheritdoc cref="IScheduler.GetCalendarNames(CancellationToken)"/>
        public virtual Task<IReadOnlyCollection<string>> GetCalendarNames(CancellationToken token = default)
            => Scheduler.GetCalendarNames(token);

        /// <inheritdoc cref="IScheduler.Interrupt(JobKey, CancellationToken)"/>
        public virtual Task<bool> Interrupt(JobKey jobKey, CancellationToken token = default)
            => Scheduler.Interrupt(jobKey, token);

        /// <inheritdoc cref="IScheduler.Interrupt(string, CancellationToken)"/>
        public virtual Task<bool> Interrupt(string fireInstanceId, CancellationToken token = default)
            => Scheduler.Interrupt(fireInstanceId, token);

        /// <inheritdoc cref="IScheduler.CheckExists(JobKey, CancellationToken)"/>
        public virtual Task<bool> CheckExists(JobKey jobKey, CancellationToken token = default)
            => Scheduler.CheckExists(jobKey, token);

        /// <inheritdoc cref="IScheduler.CheckExists(TriggerKey, CancellationToken)"/>
        public virtual Task<bool> CheckExists(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.CheckExists(triggerKey, token);

        /// <inheritdoc cref="IScheduler.Clear(CancellationToken)"/>
        public virtual Task Clear(CancellationToken token = default)
            => Scheduler.Clear(token);

        /// <summary>
        ///     Starts this instance.
        /// </summary>
        public void Start()
        {
            Start(CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            _log.Info("Scheduler is started...");
        }

        /// <summary>
        ///     Stops this instance.
        /// </summary>
        public void Stop()
        {
            StopAsync(CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            _log.Info("Scheduler has stopped...");
        }

        ~QuartzScheduler()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _log.Info("Dispose is started...");
                Stop();
                _log.Info("Successfully disposed.");
            }
        }

        private IScheduler CreateScheduler(ISchedulerFactory schedulerFactory)
            => schedulerFactory
                .GetScheduler()
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

        /// <summary>
        ///     Stops this instance.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>Task</returns>
        public Task StopAsync(CancellationToken token)
            => Scheduler.Shutdown(WaitForJobsToCompleteAtShutdown, token);

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns>Task</returns>
        public Task DisposeAsync(CancellationToken token)
            => StopAsync(token);
    }
}
