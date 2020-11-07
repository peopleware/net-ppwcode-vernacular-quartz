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
using System.Threading;
using System.Threading.Tasks;

using Castle.Core.Logging;
using Castle.MicroKernel;

using JetBrains.Annotations;

using Quartz;

namespace PPWCode.Vernacular.Quartz.I
{
    /// <inheritdoc cref="ISchedulerManager" />
    [UsedImplicitly]
    public class SchedulerManager
        : ISchedulerManager
    {
        private ILogger _logger = NullLogger.Instance;

        public SchedulerManager(
            [NotNull] IKernel kernel,
            Type[] schedulerTypes)
        {
            Kernel = kernel;
            SchedulerTypes = schedulerTypes;
        }

        [UsedImplicitly]
        public ILogger Logger
        {
            get => _logger;
            set
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (value != null)
                {
                    _logger = value;
                }
            }
        }

        [NotNull]
        public IKernel Kernel { get; }

        public Type[] SchedulerTypes { get; }

        /// <inheritdoc />
        public async Task StartSchedulersAsync(CancellationToken cancellationToken = default)
        {
            foreach (Type schedulerType in SchedulerTypes)
            {
                await StartQuartzSchedulerAsync(schedulerType, cancellationToken);
            }
        }

        private Task StartQuartzSchedulerAsync(
            [NotNull] Type schedulerType,
            CancellationToken cancellationToken = default)
        {
            bool serviceExists = Kernel.HasComponent(schedulerType);
            if (serviceExists)
            {
                IScheduler scheduler = (IScheduler)Kernel.Resolve(schedulerType);
                Logger.Info($"Starting scheduler type: {schedulerType.FullName}, named {scheduler.SchedulerName}.");
                return scheduler.Start(cancellationToken);
            }

            Logger.Warn($"Requested to start scheduler type: {schedulerType.FullName}, but type was not registered.");
            return Task.CompletedTask;
        }
    }
}
