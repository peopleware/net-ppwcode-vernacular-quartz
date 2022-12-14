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

        public IScheduler Scheduler { get; }

        ~QuartzScheduler()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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

        /// <summary>
        ///     Wait for Jobs to finish when shutdown is triggered
        /// </summary>
        [DoNotWire]
        public bool WaitForJobsToCompleteAtShutdown { get; set; }

        /// <inheritdoc />
        /// <summary>
        ///     Returns the name of the <see cref="T:Quartz.IScheduler" />.
        /// </summary>
        public string SchedulerName
            => Scheduler.SchedulerName;

        /// <inheritdoc />
        /// <summary>
        ///     Returns the instance Id of the <see cref="T:Quartz.IScheduler" />.
        /// </summary>
        public string SchedulerInstanceId
            => Scheduler.SchedulerInstanceId;

        /// <inheritdoc />
        /// <summary>
        ///     Returns the <see cref="T:Quartz.SchedulerContext" /> of the <see cref="T:Quartz.IScheduler" />.
        /// </summary>
        public SchedulerContext Context
            => Scheduler.Context;

        /// <summary>
        ///     Reports whether the <see cref="T:Quartz.IScheduler" /> is in stand-by mode.
        /// </summary>
        /// <inheritdoc />
        /// <seealso cref="M:Quartz.IScheduler.Standby" />
        /// <seealso cref="M:Quartz.IScheduler.Start" />
        public bool InStandbyMode
            => Scheduler.InStandbyMode;

        /// <summary>
        ///     Reports whether the <see cref="T:Quartz.IScheduler" /> has been Shutdown.
        /// </summary>
        /// <inheritdoc />
        public bool IsShutdown
            => Scheduler.IsShutdown;

        /// <summary>
        ///     Set the <see cref="P:Quartz.IScheduler.JobFactory" /> that will be responsible for producing
        ///     instances of <see cref="T:Quartz.IJob" /> classes.
        /// </summary>
        /// <remarks>
        ///     JobFactories may be of use to those wishing to have their application
        ///     produce <see cref="T:Quartz.IJob" /> instances via some special mechanism, such as to
        ///     give the opportunity for dependency injection.
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="T:Quartz.Spi.IJobFactory" />
        public IJobFactory JobFactory
        {
            set => Scheduler.JobFactory = value;
        }

        /// <summary>
        ///     Get a reference to the scheduler's <see cref="T:Quartz.IListenerManager" />,
        ///     through which listeners may be registered.
        /// </summary>
        /// <inheritdoc />
        /// <seealso cref="P:Quartz.IScheduler.ListenerManager" />
        /// <seealso cref="T:Quartz.IJobListener" />
        /// <seealso cref="T:Quartz.ITriggerListener" />
        /// <seealso cref="T:Quartz.ISchedulerListener" />
        public IListenerManager ListenerManager
            => Scheduler.ListenerManager;

        /// <inheritdoc />
        /// <summary>
        ///     Whether the scheduler has been started.
        /// </summary>
        /// <remarks>
        ///     Note: This only reflects whether <see cref="M:Quartz.IScheduler.Start" /> has ever
        ///     been called on this Scheduler, so it will return <see langword="true" /> even
        ///     if the <see cref="T:Quartz.IScheduler" /> is currently in standby mode or has been
        ///     since shutdown.
        /// </remarks>
        /// <seealso cref="M:Quartz.IScheduler.Start" />
        /// <seealso cref="P:Quartz.IScheduler.IsShutdown" />
        /// <seealso cref="P:Quartz.IScheduler.InStandbyMode" />
        public bool IsStarted
            => Scheduler.IsStarted;

        /// <inheritdoc />
        /// <summary>
        ///     returns true if the given JobGroup
        ///     is paused
        /// </summary>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="token">The token.</param>
        /// <returns>
        ///     <c>true</c> if [is job group paused] [the specified group name]; otherwise, <c>false</c>.
        /// </returns>
        public Task<bool> IsJobGroupPaused(string groupName, CancellationToken token = default)
            => Scheduler.IsJobGroupPaused(groupName, token);

        /// <summary>
        ///     returns true if the given TriggerGroup
        ///     is paused
        /// </summary>
        /// <param name="groupName">Name of the group.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task<bool> IsTriggerGroupPaused(string groupName, CancellationToken token = default)
            => Scheduler.IsTriggerGroupPaused(groupName, token);

        /// <summary>
        ///     Get a <see cref="T:Quartz.SchedulerMetaData" /> object describing the settings
        ///     and capabilities of the scheduler instance.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     Note that the data returned is an 'instantaneous' snap-shot, and that as
        ///     soon as it's returned, the meta data values may be different.
        /// </remarks>
        /// <inheritdoc />
        public Task<SchedulerMetaData> GetMetaData(CancellationToken token = default)
            => Scheduler.GetMetaData(token);

        /// <summary>
        ///     Return a list of <see cref="T:Quartz.IJobExecutionContext" /> objects that
        ///     represent all currently executing Jobs in this Scheduler instance.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         This method is not cluster aware.  That is, it will only return Jobs
        ///         currently executing in this Scheduler instance, not across the entire
        ///         cluster.
        ///     </para>
        ///     <para>
        ///         Note that the list returned is an 'instantaneous' snap-shot, and that as
        ///         soon as it's returned, the true list of executing jobs may be different.
        ///         Also please read the doc associated with <see cref="T:Quartz.IJobExecutionContext" />-
        ///         especially if you're using remoting.
        ///     </para>
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="T:Quartz.IJobExecutionContext" />
        public Task<IReadOnlyCollection<IJobExecutionContext>> GetCurrentlyExecutingJobs(CancellationToken token = default)
            => Scheduler.GetCurrentlyExecutingJobs(token);

        /// <summary>
        ///     Get the names of all known <see cref="T:Quartz.IJobDetail" /> groups.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> GetJobGroupNames(CancellationToken token = default)
            => Scheduler.GetJobGroupNames(token);

        /// <summary>
        ///     Get the names of all known <see cref="T:Quartz.ITrigger" /> groups.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> GetTriggerGroupNames(CancellationToken token = default)
            => Scheduler.GetTriggerGroupNames(token);

        /// <summary>
        ///     Get the names of all <see cref="T:Quartz.ITrigger" /> groups that are paused.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> GetPausedTriggerGroups(CancellationToken token = default)
            => Scheduler.GetPausedTriggerGroups(token);

        /// <summary>
        ///     Starts the <see cref="T:Quartz.IScheduler" />'s threads that fire <see cref="T:Quartz.ITrigger" />s.
        ///     When a scheduler is first created it is in "stand-by" mode, and will not
        ///     fire triggers.  The scheduler can also be put into stand-by mode by
        ///     calling the <see cref="M:Quartz.IScheduler.Standby" /> method.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     The misfire/recovery process will be started, if it is the initial call
        ///     to this method on this scheduler instance.
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="M:Quartz.IScheduler.StartDelayed(System.TimeSpan)" />
        /// <seealso cref="M:Quartz.IScheduler.Standby" />
        /// <seealso cref="M:Quartz.IScheduler.Shutdown(System.Boolean)" />
        public Task Start(CancellationToken token)
            => Scheduler.Start(token);

        /// <summary>
        ///     Calls <see cref="M:Quartz.IScheduler.Start" /> after the indicated delay.
        ///     (This call does not block). This can be useful within applications that
        ///     have initializers that create the scheduler immediately, before the
        ///     resources needed by the executing jobs have been fully initialized.
        /// </summary>
        /// <param name="delay">The delay.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        /// <seealso cref="M:Quartz.IScheduler.Start" />
        /// <seealso cref="M:Quartz.IScheduler.Standby" />
        /// <seealso cref="M:Quartz.IScheduler.Shutdown(System.Boolean)" />
        public Task StartDelayed(TimeSpan delay, CancellationToken token = default)
            => Scheduler.StartDelayed(delay, token);

        /// <summary>
        ///     Temporarily halts the <see cref="T:Quartz.IScheduler" />'s firing of <see cref="T:Quartz.ITrigger" />s.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         When <see cref="M:Quartz.IScheduler.Start" /> is called (to bring the scheduler out of
        ///         stand-by mode), trigger misfire instructions will NOT be applied
        ///         during the execution of the <see cref="M:Quartz.IScheduler.Start" /> method - any misfires
        ///         will be detected immediately afterward (by the <see cref="T:Quartz.Spi.IJobStore" />'s
        ///         normal process).
        ///     </para>
        ///     <para>
        ///         The scheduler is not destroyed, and can be re-started at any time.
        ///     </para>
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="M:Quartz.IScheduler.Start" />
        /// <seealso cref="M:Quartz.IScheduler.PauseAll" />
        public Task Standby(CancellationToken token = default)
            => Scheduler.Standby(token);

        /// <summary>
        ///     Halts the <see cref="T:Quartz.IScheduler" />'s firing of <see cref="T:Quartz.ITrigger" />s,
        ///     and cleans up all resources associated with the Scheduler. Equivalent to
        ///     <see cref="M:Quartz.IScheduler.Shutdown(System.Boolean)" />.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     The scheduler cannot be re-started.
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="M:Quartz.IScheduler.Shutdown(System.Boolean)" />
        public Task Shutdown(CancellationToken token = default)
            => StopAsync(token);

        /// <summary>
        ///     Halts the <see cref="T:Quartz.IScheduler" />'s firing of <see cref="T:Quartz.ITrigger" />s,
        ///     and cleans up all resources associated with the Scheduler.
        /// </summary>
        /// <param name="waitForJobsToComplete">
        ///     if <see langword="true" /> the scheduler will not allow this method
        ///     to return until all currently executing jobs have completed.
        /// </param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     The scheduler cannot be re-started.
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="M:Quartz.IScheduler.Shutdown" />
        public Task Shutdown(bool waitForJobsToComplete, CancellationToken token = default)
            => Scheduler.Shutdown(waitForJobsToComplete, token);

        /// <summary>
        ///     Add the given <see cref="T:Quartz.IJobDetail" /> to the
        ///     Scheduler, and associate the given <see cref="T:Quartz.ITrigger" /> with
        ///     it.
        /// </summary>
        /// <param name="jobDetail">The job detail.</param>
        /// <param name="trigger">The trigger.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     If the given Trigger does not reference any <see cref="T:Quartz.IJob" />, then it
        ///     will be set to reference the Job passed with it into this method.
        /// </remarks>
        /// <inheritdoc />
        public Task<DateTimeOffset> ScheduleJob(IJobDetail jobDetail, ITrigger trigger, CancellationToken token = default)
            => Scheduler.ScheduleJob(jobDetail, trigger, token);

        /// <summary>
        ///     Schedule the given <see cref="T:Quartz.ITrigger" /> with the
        ///     <see cref="T:Quartz.IJob" /> identified by the <see cref="T:Quartz.ITrigger" />'s settings.
        /// </summary>
        /// <param name="trigger">The trigger.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task<DateTimeOffset> ScheduleJob(ITrigger trigger, CancellationToken token = default)
            => Scheduler.ScheduleJob(trigger, token);

        /// <summary>
        ///     Schedules the job.
        /// </summary>
        /// <param name="jobDetail">The job detail.</param>
        /// <param name="triggersForJob">The triggers for job.</param>
        /// <param name="replace">if set to <c>true</c> [replace].</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task ScheduleJob(
            IJobDetail jobDetail,
            IReadOnlyCollection<ITrigger> triggersForJob,
            bool replace,
            CancellationToken token = default)
            => Scheduler.ScheduleJob(jobDetail, triggersForJob, replace, token);

        /// <summary>
        ///     Schedules the jobs.
        /// </summary>
        /// <param name="triggersAndJobs">The triggers and jobs.</param>
        /// <param name="replace">if set to <c>true</c> [replace].</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task ScheduleJobs(IReadOnlyDictionary<IJobDetail, IReadOnlyCollection<ITrigger>> triggersAndJobs, bool replace, CancellationToken token = default)
            => Scheduler.ScheduleJobs(triggersAndJobs, replace, token);

        /// <summary>
        ///     Remove the indicated <see cref="T:Quartz.ITrigger" /> from the scheduler.
        ///     <para>
        ///         If the related job does not have any other triggers, and the job is
        ///         not durable, then the job will also be deleted.
        ///     </para>
        /// </summary>
        /// <param name="triggerKey">The trigger key.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task<bool> UnscheduleJob(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.UnscheduleJob(triggerKey, token);

        /// <summary>
        ///     Remove all of the indicated <see cref="T:Quartz.ITrigger" />s from the scheduler.
        /// </summary>
        /// <param name="triggerKeys">The trigger keys.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         If the related job does not have any other triggers, and the job is
        ///         not durable, then the job will also be deleted.
        ///     </para>
        ///     Note that while this bulk operation is likely more efficient than
        ///     invoking <see cref="M:Quartz.IScheduler.UnscheduleJob(Quartz.TriggerKey)" /> several
        ///     times, it may have the adverse affect of holding data locks for a
        ///     single long duration of time (rather than lots of small durations
        ///     of time).
        /// </remarks>
        /// <inheritdoc />
        public Task<bool> UnscheduleJobs(IReadOnlyCollection<TriggerKey> triggerKeys, CancellationToken token = default)
            => Scheduler.UnscheduleJobs(triggerKeys, token);

        /// <inheritdoc />
        /// <summary>
        ///     Remove (delete) the <see cref="T:Quartz.ITrigger" /> with the
        ///     given key, and store the new given one - which must be associated
        ///     with the same job (the new trigger must have the job name &amp; group specified)
        ///     - however, the new trigger need not have the same name as the old trigger.
        /// </summary>
        /// <param name="triggerKey">The <see cref="T:Quartz.ITrigger" /> to be replaced.</param>
        /// <param name="newTrigger">The new <see cref="T:Quartz.ITrigger" /> to be stored.</param>
        /// <param name="token">The token.</param>
        /// <returns>
        ///     <see langword="null" /> if a <see cref="T:Quartz.ITrigger" /> with the given
        ///     name and group was not found and removed from the store (and the
        ///     new trigger is therefore not stored),  otherwise
        ///     the first fire time of the newly scheduled trigger.
        /// </returns>
        public Task<DateTimeOffset?> RescheduleJob(TriggerKey triggerKey, ITrigger newTrigger, CancellationToken token = default)
            => Scheduler.RescheduleJob(triggerKey, newTrigger, token);

        /// <inheritdoc />
        /// <summary>
        ///     Add the given <see cref="T:Quartz.IJob" /> to the Scheduler - with no associated
        ///     <see cref="T:Quartz.ITrigger" />. The <see cref="T:Quartz.IJob" /> will be 'dormant' until
        ///     it is scheduled with a <see cref="T:Quartz.ITrigger" />, or
        ///     <see cref="M:Quartz.IScheduler.TriggerJob(Quartz.JobKey)" />
        ///     is called for it.
        /// </summary>
        /// <param name="jobDetail">The job detail.</param>
        /// <param name="replace">if set to <c>true</c> [replace].</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     The <see cref="T:Quartz.IJob" /> must by definition be 'durable', if it is not,
        ///     SchedulerException will be thrown.
        /// </remarks>
        public Task AddJob(IJobDetail jobDetail, bool replace, CancellationToken token = default)
            => Scheduler.AddJob(jobDetail, replace, token);

        /// <inheritdoc />
        /// <summary>
        ///     Adds the job.
        /// </summary>
        /// <param name="jobDetail">The job detail.</param>
        /// <param name="replace">if set to <c>true</c> [replace].</param>
        /// <param name="storeNonDurableWhileAwaitingScheduling">
        ///     if set to <c>true</c> [store non durable while awaiting
        ///     scheduling].
        /// </param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        public Task AddJob(
            IJobDetail jobDetail,
            bool replace,
            bool storeNonDurableWhileAwaitingScheduling,
            CancellationToken token = default)
            => Scheduler.AddJob(jobDetail, replace, storeNonDurableWhileAwaitingScheduling, token);

        /// <inheritdoc />
        /// <summary>
        ///     Delete the identified <see cref="T:Quartz.IJob" /> from the Scheduler - and any
        ///     associated <see cref="T:Quartz.ITrigger" />s.
        /// </summary>
        /// <param name="jobKey">The job key.</param>
        /// <param name="token">The token.</param>
        /// <returns>
        ///     true if the Job was found and deleted.
        /// </returns>
        public Task<bool> DeleteJob(JobKey jobKey, CancellationToken token = default)
            => Scheduler.DeleteJob(jobKey, token);

        /// <summary>
        ///     Delete the identified jobs from the Scheduler - and any
        ///     associated <see cref="T:Quartz.ITrigger" />s.
        /// </summary>
        /// <param name="jobKeys">The job keys.</param>
        /// <param name="token">The token.</param>
        /// <returns>
        ///     true if all of the Jobs were found and deleted, false if
        ///     one or more were not deleted.
        /// </returns>
        /// <remarks>
        ///     Note that while this bulk operation is likely more efficient than
        ///     invoking <see cref="M:Quartz.IScheduler.DeleteJob(Quartz.JobKey)" /> several
        ///     times, it may have the adverse affect of holding data locks for a
        ///     single long duration of time (rather than lots of small durations
        ///     of time).
        /// </remarks>
        /// <inheritdoc />
        public Task<bool> DeleteJobs(IReadOnlyCollection<JobKey> jobKeys, CancellationToken token = default)
            => Scheduler.DeleteJobs(jobKeys, token);

        /// <summary>
        ///     Trigger the identified <see cref="T:Quartz.IJobDetail" />
        ///     (Execute it now).
        /// </summary>
        /// <param name="jobKey">The job key.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task TriggerJob(JobKey jobKey, CancellationToken token = default)
            => Scheduler.TriggerJob(jobKey, token);

        /// <summary>
        ///     Trigger the identified <see cref="T:Quartz.IJobDetail" /> (Execute it now).
        /// </summary>
        /// <param name="jobKey">The <see cref="T:Quartz.JobKey" /> of the <see cref="T:Quartz.IJob" /> to be executed.</param>
        /// <param name="data">
        ///     the (possibly <see langword="null" />) JobDataMap to be
        ///     associated with the trigger that fires the job immediately.
        /// </param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task TriggerJob(JobKey jobKey, JobDataMap data, CancellationToken token = default)
            => Scheduler.TriggerJob(jobKey, data, token);

        /// <summary>
        ///     Pause the <see cref="T:Quartz.IJobDetail" /> with the given
        ///     key - by pausing all of its current <see cref="T:Quartz.ITrigger" />s.
        /// </summary>
        /// <param name="jobKey">The job key.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task PauseJob(JobKey jobKey, CancellationToken token = default)
            => Scheduler.PauseJob(jobKey, token);

        /// <summary>
        ///     Pause all of the <see cref="T:Quartz.IJobDetail" />s in the
        ///     matching groups - by pausing all of their <see cref="T:Quartz.ITrigger" />s.
        /// </summary>
        /// <param name="matcher">The matcher.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         The Scheduler will "remember" that the groups are paused, and impose the
        ///         pause on any new jobs that are added to any of those groups until it is resumed.
        ///     </para>
        ///     <para>
        ///         NOTE: There is a limitation that only exactly matched groups
        ///         can be remembered as paused.  For example, if there are pre-existing
        ///         job in groups "aaa" and "bbb" and a matcher is given to pause
        ///         groups that start with "a" then the group "aaa" will be remembered
        ///         as paused and any subsequently added jobs in group "aaa" will be paused,
        ///         however if a job is added to group "axx" it will not be paused,
        ///         as "axx" wasn't known at the time the "group starts with a" matcher
        ///         was applied.  HOWEVER, if there are pre-existing groups "aaa" and
        ///         "bbb" and a matcher is given to pause the group "axx" (with a
        ///         group equals matcher) then no jobs will be paused, but it will be
        ///         remembered that group "axx" is paused and later when a job is added
        ///         in that group, it will become paused.
        ///     </para>
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="M:Quartz.IScheduler.ResumeJobs(Quartz.Impl.Matchers.GroupMatcher{Quartz.JobKey})" />
        public Task PauseJobs(GroupMatcher<JobKey> matcher, CancellationToken token = default)
            => Scheduler.PauseJobs(matcher, token);

        /// <summary>
        ///     Pause the <see cref="T:Quartz.ITrigger" /> with the given key.
        /// </summary>
        /// <param name="triggerKey">The trigger key.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task PauseTrigger(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.PauseTrigger(triggerKey, token);

        /// <summary>
        ///     Pause all of the <see cref="T:Quartz.ITrigger" />s in the groups matching.
        /// </summary>
        /// <param name="matcher">The matcher.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     <para>
        ///         The Scheduler will "remember" all the groups paused, and impose the
        ///         pause on any new triggers that are added to any of those groups until it is resumed.
        ///     </para>
        ///     <para>
        ///         NOTE: There is a limitation that only exactly matched groups
        ///         can be remembered as paused.  For example, if there are pre-existing
        ///         triggers in groups "aaa" and "bbb" and a matcher is given to pause
        ///         groups that start with "a" then the group "aaa" will be remembered as
        ///         paused and any subsequently added triggers in that group be paused,
        ///         however if a trigger is added to group "axx" it will not be paused,
        ///         as "axx" wasn't known at the time the "group starts with a" matcher
        ///         was applied.  HOWEVER, if there are pre-existing groups "aaa" and
        ///         "bbb" and a matcher is given to pause the group "axx" (with a
        ///         group equals matcher) then no triggers will be paused, but it will be
        ///         remembered that group "axx" is paused and later when a trigger is added
        ///         in that group, it will become paused.
        ///     </para>
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="M:Quartz.IScheduler.ResumeTriggers(Quartz.Impl.Matchers.GroupMatcher{Quartz.TriggerKey})" />
        public Task PauseTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken token = default)
            => Scheduler.PauseTriggers(matcher, token);

        /// <summary>
        ///     Resume (un-pause) the <see cref="T:Quartz.IJobDetail" /> with
        ///     the given key.
        /// </summary>
        /// <param name="jobKey">The job key.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     If any of the <see cref="T:Quartz.IJob" />'s<see cref="T:Quartz.ITrigger" /> s missed one
        ///     or more fire-times, then the <see cref="T:Quartz.ITrigger" />'s misfire
        ///     instruction will be applied.
        /// </remarks>
        /// <inheritdoc />
        public Task ResumeJob(JobKey jobKey, CancellationToken token = default)
            => Scheduler.ResumeJob(jobKey, token);

        /// <summary>
        ///     Resume (un-pause) all of the <see cref="T:Quartz.IJobDetail" />s
        ///     in matching groups.
        /// </summary>
        /// <param name="matcher">The matcher.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     If any of the <see cref="T:Quartz.IJob" /> s had <see cref="T:Quartz.ITrigger" /> s that
        ///     missed one or more fire-times, then the <see cref="T:Quartz.ITrigger" />'s
        ///     misfire instruction will be applied.
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="M:Quartz.IScheduler.PauseJobs(Quartz.Impl.Matchers.GroupMatcher{Quartz.JobKey})" />
        public Task ResumeJobs(GroupMatcher<JobKey> matcher, CancellationToken token = default)
            => Scheduler.ResumeJobs(matcher, token);

        /// <summary>
        ///     Resume (un-pause) the <see cref="T:Quartz.ITrigger" /> with the given
        ///     key.
        /// </summary>
        /// <param name="triggerKey">The trigger key.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     If the <see cref="T:Quartz.ITrigger" /> missed one or more fire-times, then the
        ///     <see cref="T:Quartz.ITrigger" />'s misfire instruction will be applied.
        /// </remarks>
        /// <inheritdoc />
        public Task ResumeTrigger(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.ResumeTrigger(triggerKey, token);

        /// <inheritdoc />
        /// <summary>
        ///     Resume (un-pause) all of the <see cref="T:Quartz.ITrigger" />s in matching groups.
        /// </summary>
        /// <remarks>
        ///     If any <see cref="T:Quartz.ITrigger" /> missed one or more fire-times, then the
        ///     <see cref="T:Quartz.ITrigger" />'s misfire instruction will be applied.
        /// </remarks>
        /// <seealso cref="M:Quartz.IScheduler.PauseTriggers(Quartz.Impl.Matchers.GroupMatcher{Quartz.TriggerKey})" />
        public Task ResumeTriggers(GroupMatcher<TriggerKey> matcher, CancellationToken token = default)
            => Scheduler.ResumeTriggers(matcher, token);

        /// <summary>
        ///     Pause all triggers - similar to calling
        ///     <see cref="M:Quartz.IScheduler.PauseTriggers(Quartz.Impl.Matchers.GroupMatcher{Quartz.TriggerKey})" />
        ///     on every group, however, after using this method <see cref="M:Quartz.IScheduler.ResumeAll" />
        ///     must be called to clear the scheduler's state of 'remembering' that all
        ///     new triggers will be paused as they are added.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     When <see cref="M:Quartz.IScheduler.ResumeAll" /> is called (to un-pause), trigger misfire
        ///     instructions WILL be applied.
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="M:Quartz.IScheduler.ResumeAll" />
        /// <seealso cref="M:Quartz.IScheduler.PauseTriggers(Quartz.Impl.Matchers.GroupMatcher{Quartz.TriggerKey})" />
        /// <seealso cref="M:Quartz.IScheduler.Standby" />
        public Task PauseAll(CancellationToken token = default)
            => Scheduler.PauseAll(token);

        /// <inheritdoc />
        /// <summary>
        ///     Resume (un-pause) all triggers - similar to calling
        ///     <see cref="M:Quartz.IScheduler.ResumeTriggers(Quartz.Impl.Matchers.GroupMatcher{Quartz.TriggerKey})" /> on every
        ///     group.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     If any <see cref="T:Quartz.ITrigger" /> missed one or more fire-times, then the
        ///     <see cref="T:Quartz.ITrigger" />'s misfire instruction will be applied.
        /// </remarks>
        /// <seealso cref="M:Quartz.IScheduler.PauseAll" />
        public Task ResumeAll(CancellationToken token = default)
            => Scheduler.ResumeAll(token);

        /// <summary>
        ///     Get the keys of all the <see cref="T:Quartz.IJobDetail" />s in the matching groups.
        /// </summary>
        /// <param name="matcher">The matcher.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task<IReadOnlyCollection<JobKey>> GetJobKeys(GroupMatcher<JobKey> matcher, CancellationToken token = default)
            => Scheduler.GetJobKeys(matcher, token);

        /// <summary>
        ///     Get all <see cref="T:Quartz.ITrigger" /> s that are associated with the
        ///     identified <see cref="T:Quartz.IJobDetail" />.
        /// </summary>
        /// <param name="jobKey">The job key.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     The returned Trigger objects will be snap-shots of the actual stored
        ///     triggers.  If you wish to modify a trigger, you must re-store the
        ///     trigger afterward (e.g. see <see cref="M:Quartz.IScheduler.RescheduleJob(Quartz.TriggerKey,Quartz.ITrigger)" />).
        /// </remarks>
        /// <inheritdoc />
        public Task<IReadOnlyCollection<ITrigger>> GetTriggersOfJob(JobKey jobKey, CancellationToken token = default)
            => Scheduler.GetTriggersOfJob(jobKey, token);

        /// <summary>
        ///     Get the names of all the <see cref="T:Quartz.ITrigger" />s in the given
        ///     groups.
        /// </summary>
        /// <param name="matcher">The matcher.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task<IReadOnlyCollection<TriggerKey>> GetTriggerKeys(GroupMatcher<TriggerKey> matcher, CancellationToken token = default)
            => Scheduler.GetTriggerKeys(matcher, token);

        /// <summary>
        ///     Get the <see cref="T:Quartz.IJobDetail" /> for the <see cref="T:Quartz.IJob" />
        ///     instance with the given key .
        /// </summary>
        /// <param name="jobKey">The job key.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     The returned JobDetail object will be a snap-shot of the actual stored
        ///     JobDetail.  If you wish to modify the JobDetail, you must re-store the
        ///     JobDetail afterward (e.g. see <see cref="M:Quartz.IScheduler.AddJob(Quartz.IJobDetail,System.Boolean)" />).
        /// </remarks>
        /// <inheritdoc />
        public Task<IJobDetail> GetJobDetail(JobKey jobKey, CancellationToken token = default)
            => Scheduler.GetJobDetail(jobKey, token);

        /// <summary>
        ///     Get the <see cref="T:Quartz.ITrigger" /> instance with the given key.
        /// </summary>
        /// <param name="triggerKey">The trigger key.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <remarks>
        ///     The returned Trigger object will be a snap-shot of the actual stored
        ///     trigger.  If you wish to modify the trigger, you must re-store the
        ///     trigger afterward (e.g. see <see cref="M:Quartz.IScheduler.RescheduleJob(Quartz.TriggerKey,Quartz.ITrigger)" />).
        /// </remarks>
        /// <inheritdoc />
        public Task<ITrigger> GetTrigger(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.GetTrigger(triggerKey, token);

        /// <summary>
        ///     Get the current state of the identified <see cref="T:Quartz.ITrigger" />.
        /// </summary>
        /// <param name="triggerKey">The trigger key.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        /// <seealso cref="F:Quartz.TriggerState.Normal" />
        /// <seealso cref="F:Quartz.TriggerState.Paused" />
        /// <seealso cref="F:Quartz.TriggerState.Complete" />
        /// <seealso cref="F:Quartz.TriggerState.Blocked" />
        /// <seealso cref="F:Quartz.TriggerState.Error" />
        /// <seealso cref="F:Quartz.TriggerState.None" />
        public Task<TriggerState> GetTriggerState(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.GetTriggerState(triggerKey, token);

        /// <summary>
        ///     Add (register) the given <see cref="T:Quartz.ICalendar" /> to the Scheduler.
        /// </summary>
        /// <param name="calName">Name of the calendar.</param>
        /// <param name="calendar">The calendar.</param>
        /// <param name="replace">if set to <c>true</c> [replace].</param>
        /// <param name="updateTriggers">
        ///     whether or not to update existing triggers that
        ///     referenced the already existing calendar so that they are 'correct'
        ///     based on the new trigger.
        /// </param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task AddCalendar(
            string calName,
            ICalendar calendar,
            bool replace,
            bool updateTriggers,
            CancellationToken token = default)
            => Scheduler.AddCalendar(calName, calendar, replace, updateTriggers, token);

        /// <summary>
        ///     Delete the identified <see cref="T:Quartz.ICalendar" /> from the Scheduler.
        /// </summary>
        /// <param name="calName">Name of the calendar.</param>
        /// <param name="token">The token.</param>
        /// <returns>
        ///     true if the Calendar was found and deleted.
        /// </returns>
        /// <remarks>
        ///     If removal of the <c>Calendar</c> would result in
        ///     <see cref="T:Quartz.ITrigger" />s pointing to non-existent calendars, then a
        ///     <see cref="T:Quartz.SchedulerException" /> will be thrown.
        /// </remarks>
        /// <inheritdoc />
        public Task<bool> DeleteCalendar(string calName, CancellationToken token = default)
            => Scheduler.DeleteCalendar(calName, token);

        /// <summary>
        ///     Get the <see cref="T:Quartz.ICalendar" /> instance with the given name.
        /// </summary>
        /// <param name="calName">Name of the cal.</param>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task<ICalendar> GetCalendar(string calName, CancellationToken token = default)
            => Scheduler.GetCalendar(calName, token);

        /// <summary>
        ///     Get the names of all registered <see cref="T:Quartz.ICalendar" />.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task<IReadOnlyCollection<string>> GetCalendarNames(CancellationToken token = default)
            => Scheduler.GetCalendarNames(token);

        /// <summary>
        ///     Request the interruption, within this Scheduler instance, of all
        ///     currently executing instances of the identified <see cref="T:Quartz.IJob" />, which
        ///     must be an implementor of the <see cref="T:Quartz.IInterruptableJob" /> interface.
        /// </summary>
        /// <param name="jobKey">The job key.</param>
        /// <param name="token">The token.</param>
        /// <returns>
        ///     true is at least one instance of the identified job was found and interrupted.
        /// </returns>
        /// <remarks>
        ///     <para>
        ///         If more than one instance of the identified job is currently executing,
        ///         the <see cref="M:Quartz.IInterruptableJob.Interrupt" /> method will be called on
        ///         each instance.  However, there is a limitation that in the case that
        ///         <see cref="M:Quartz.IScheduler.Interrupt(Quartz.JobKey)" /> on one instances throws an exception, all
        ///         remaining  instances (that have not yet been interrupted) will not have
        ///         their <see cref="M:Quartz.IScheduler.Interrupt(Quartz.JobKey)" /> method called.
        ///     </para>
        ///     <para>
        ///         If you wish to interrupt a specific instance of a job (when more than
        ///         one is executing) you can do so by calling
        ///         <see cref="M:Quartz.IScheduler.GetCurrentlyExecutingJobs" /> to obtain a handle
        ///         to the job instance, and then invoke <see cref="M:Quartz.IScheduler.Interrupt(Quartz.JobKey)" /> on it
        ///         yourself.
        ///     </para>
        ///     <para>
        ///         This method is not cluster aware.  That is, it will only interrupt
        ///         instances of the identified InterruptableJob currently executing in this
        ///         Scheduler instance, not across the entire cluster.
        ///     </para>
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="T:Quartz.IInterruptableJob" />
        /// <seealso cref="M:Quartz.IScheduler.GetCurrentlyExecutingJobs" />
        public Task<bool> Interrupt(JobKey jobKey, CancellationToken token = default)
            => Scheduler.Interrupt(jobKey, token);

        /// <summary>
        ///     Request the interruption, within this Scheduler instance, of the
        ///     identified executing job instance, which
        ///     must be an implementor of the <see cref="T:Quartz.IInterruptableJob" /> interface.
        /// </summary>
        /// <param name="fireInstanceId">The fire instance identifier.</param>
        /// <param name="token">The token.</param>
        /// <returns>
        ///     true if the identified job instance was found and interrupted.
        /// </returns>
        /// <remarks>
        ///     This method is not cluster aware.  That is, it will only interrupt
        ///     instances of the identified InterruptableJob currently executing in this
        ///     Scheduler instance, not across the entire cluster.
        /// </remarks>
        /// <inheritdoc />
        /// <seealso cref="M:Quartz.IInterruptableJob.Interrupt" />
        /// <seealso cref="M:Quartz.IScheduler.GetCurrentlyExecutingJobs" />
        /// <seealso cref="P:Quartz.IJobExecutionContext.FireInstanceId" />
        /// <seealso cref="M:Quartz.IScheduler.Interrupt(Quartz.JobKey)" />
        public Task<bool> Interrupt(string fireInstanceId, CancellationToken token = default)
            => Scheduler.Interrupt(fireInstanceId, token);

        /// <summary>
        ///     Determine whether a <see cref="T:Quartz.IJob" /> with the given identifier already
        ///     exists within the scheduler.
        /// </summary>
        /// <param name="jobKey">the identifier to check for</param>
        /// <param name="token">The token.</param>
        /// <returns>
        ///     true if a Job exists with the given identifier
        /// </returns>
        /// <inheritdoc />
        public Task<bool> CheckExists(JobKey jobKey, CancellationToken token = default)
            => Scheduler.CheckExists(jobKey, token);

        /// <summary>
        ///     Determine whether a <see cref="T:Quartz.ITrigger" /> with the given identifier already
        ///     exists within the scheduler.
        /// </summary>
        /// <param name="triggerKey">the identifier to check for</param>
        /// <param name="token">The token.</param>
        /// <returns>
        ///     true if a Trigger exists with the given identifier
        /// </returns>
        /// <inheritdoc />
        public Task<bool> CheckExists(TriggerKey triggerKey, CancellationToken token = default)
            => Scheduler.CheckExists(triggerKey, token);

        /// <summary>
        ///     Clears (deletes!) all scheduling data - all <see cref="T:Quartz.IJob" />s, <see cref="T:Quartz.ITrigger" />s
        ///     <see cref="T:Quartz.ICalendar" />s.
        /// </summary>
        /// <param name="token">The token.</param>
        /// <returns></returns>
        /// <inheritdoc />
        public Task Clear(CancellationToken token = default)
            => Scheduler.Clear(token);

        /// <summary>
        ///     Starts this instance.
        /// </summary>
        public void Start()
        {
            Start(default)
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
            StopAsync(default)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            _log.Info("Scheduler has stopped...");
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
