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
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;

using Castle.Core;
using Castle.Core.Internal;
using Castle.MicroKernel.Facilities;
using Castle.MicroKernel.Registration;

using JetBrains.Annotations;

using PPWCode.Vernacular.Exceptions.III;

using Quartz;
using Quartz.Impl;
using Quartz.Spi;

namespace PPWCode.Vernacular.Quartz.I
{
    /// <inheritdoc />
    public class QuartzFacility : AbstractFacility
    {
        private readonly IList<AdditionalScheduler> _additionalSchedulers =
            new List<AdditionalScheduler>();

        private readonly IDictionary<Type, LifestyleType?> _jobs =
            new Dictionary<Type, LifestyleType?>();

        private Type _jobFactory;

        private LifestyleType? _lifestyleType;
        private IDictionary<string, string> _properties;
        private Type _scheduler;
        private Type _schedulerFactory;

        private string _sectionName;
        private bool _waitForJobsToCompleteAtShutdown = true;

        /// <inheritdoc />
        protected override void Init()
        {
            foreach (KeyValuePair<Type, LifestyleType?> job in _jobs)
            {
                AddComponent<IJob>(job.Key, true, job.Value);
            }

            AddComponent<IJobFactory>(
                _jobFactory ?? typeof(QuartzJobFactory),
                false,
                LifestyleType.Singleton);

            AddComponent<IQuartzSchedulerFactory>(
                _schedulerFactory ?? typeof(QuartzSchedulerFactory),
                false,
                LifestyleType.Singleton,
                r =>
                {
                    r.Forward<ISchedulerFactory>();
                    r.IsDefault();
                    r.OnCreate(
                        s =>
                        {
                            NameValueCollection properties = FromApplicationConfig(_sectionName, _properties);
                            CheckProperties(
                                properties,
                                typeof(IQuartzSchedulerFactory),
                                _schedulerFactory ?? typeof(QuartzSchedulerFactory));
                            s.Initialize(properties);
                        });
                });
            AddComponent<IQuartzScheduler>(
                _scheduler ?? typeof(QuartzScheduler),
                false,
                LifestyleType.Singleton,
                r =>
                {
                    r.Forward<IScheduler>();
                    r.IsDefault();
                    r.OnCreate(s => s.WaitForJobsToCompleteAtShutdown = _waitForJobsToCompleteAtShutdown);
                });

            List<Type> schedulers =
                new List<Type>
                {
                    typeof(IQuartzScheduler)
                };
            foreach (AdditionalScheduler additionalScheduler in _additionalSchedulers)
            {
                AddComponent(
                    additionalScheduler.SchedulerFactoryServiceType,
                    additionalScheduler.SchedulerFactoryComponentType,
                    LifestyleType.Singleton,
                    r =>
                    {
                        r.Forward<ISchedulerFactory>();
                        r.OnCreate(
                            s =>
                            {
                                NameValueCollection properties =
                                    FromApplicationConfig(
                                        additionalScheduler.SectionName,
                                        additionalScheduler.Properties);
                                CheckProperties(
                                    properties,
                                    additionalScheduler.SchedulerFactoryServiceType,
                                    additionalScheduler.SchedulerFactoryComponentType);
                                ((IQuartzSchedulerFactory)s).Initialize(properties);
                            });
                    });
                AddComponent(
                    additionalScheduler.SchedulerServiceType,
                    additionalScheduler.SchedulerComponentType,
                    LifestyleType.Singleton,
                    r =>
                    {
                        r.Forward<IQuartzScheduler, IScheduler>();
                        r.OnCreate(s => ((IQuartzScheduler)s).WaitForJobsToCompleteAtShutdown = additionalScheduler.WaitForJobsToCompleteAtShutdown);
                    });
                if (additionalScheduler.StartScheduler)
                {
                    schedulers.Add(additionalScheduler.SchedulerServiceType);
                }
            }

            AddComponent<ISchedulerManager>(
                typeof(SchedulerManager),
                false,
                LifestyleType.Singleton,
                r => r.DependsOn(Dependency.OnValue(typeof(Type[]), schedulers.ToArray())));
        }

        [NotNull]
        public QuartzFacility UseAppConfigSection([NotNull] string sectionName)
        {
            _sectionName = sectionName;
            return this;
        }

        [NotNull]
        public QuartzFacility UseProperties([CanBeNull] IDictionary<string, string> properties)
        {
            _properties = properties;
            return this;
        }

        [NotNull]
        public QuartzFacility UseWaitForJobsToCompleteAtShutdown(bool waitForJobsToCompleteAtShutdown)
        {
            _waitForJobsToCompleteAtShutdown = waitForJobsToCompleteAtShutdown;
            return this;
        }

        [NotNull]
        public QuartzFacility UseLifestyleTypeForJobs(LifestyleType lifestyleType)
        {
            _lifestyleType = lifestyleType;
            return this;
        }

        public QuartzFacility UseAdditionalScheduler(AdditionalScheduler additionalScheduler)
        {
            _additionalSchedulers.Add(additionalScheduler);
            return this;
        }

        [NotNull]
        public QuartzFacility AddJob<T>([CanBeNull] LifestyleType? lifestyleType = null)
            where T : IJob
        {
            _jobs.Add(typeof(T), lifestyleType);
            return this;
        }

        [NotNull]
        public QuartzFacility AddJobs([CanBeNull] IDictionary<Type, LifestyleType?> jobs)
        {
            if (jobs != null)
            {
                if (jobs.Keys.Any(j => !j.Is<IJob>()))
                {
                    StringBuilder sb =
                        new StringBuilder()
                            .AppendLine($"When adding jobs, each job should implement {typeof(IJob).FullName}.")
                            .AppendLine("Following jobs doesn't fulfill this contract:");
                    foreach (Type job in jobs.Keys.Where(j => !j.Is<IJob>()))
                    {
                        sb.AppendLine($"  - {job.FullName}");
                    }

                    throw new FacilityException(sb.ToString());
                }

                foreach (KeyValuePair<Type, LifestyleType?> job in jobs)
                {
                    _jobs.Add(job);
                }
            }

            return this;
        }

        [NotNull]
        public QuartzFacility UseJobFactory<T>()
            where T : IJobFactory
        {
            _jobFactory = typeof(T);
            return this;
        }

        [NotNull]
        public QuartzFacility UseSchedulerFactory<T>()
            where T : IQuartzSchedulerFactory
        {
            _schedulerFactory = typeof(T);
            return this;
        }

        [NotNull]
        public QuartzFacility UseScheduler<T>()
            where T : IQuartzScheduler
        {
            _scheduler = typeof(T);
            return this;
        }

        /// <summary>
        ///     Adds a component, of type <paramref name="componentType" />, with lifestyle <paramref name="lifestyleType" />.
        /// </summary>
        /// <param name="componentType">Component type</param>
        /// <param name="forwardComponentType">Should the component-type also be resolvable?</param>
        /// <param name="lifestyleType">Optional <see cref="LifestyleType" /> for this component</param>
        /// <param name="onRegistration">Optional lambda for customizing our component registration</param>
        /// <typeparam name="TService">Type of component's interface</typeparam>
        /// <returns>
        ///     key of added component
        /// </returns>
        [CanBeNull]
        protected virtual string AddComponent<TService>(
            [NotNull] Type componentType,
            bool forwardComponentType,
            LifestyleType? lifestyleType = null,
            [CanBeNull] Action<ComponentRegistration<TService>> onRegistration = null)
            where TService : class
        {
            if (Kernel.HasComponent(componentType))
            {
                return null;
            }

            lifestyleType ??=
                typeof(TService).Is<IQuartzScopedJob>()
                    ? LifestyleType.Scoped
                    : _lifestyleType ?? LifestyleType.Transient;

            string key = componentType.AssemblyQualifiedName;
            ComponentRegistration<TService> component =
                Component
                    .For<TService>()
                    .ImplementedBy(componentType)
                    .LifeStyle.Is(lifestyleType.Value);
            if (forwardComponentType)
            {
                component.Forward(componentType);
            }

            onRegistration?.Invoke(component);
            Kernel.Register(component);

            return key;
        }

        /// <summary>
        ///     Adds a component, of type <paramref name="componentType" />, with lifestyle <paramref name="lifestyleType" />.
        /// </summary>
        /// <param name="serviceType">Type of component's interface</param>
        /// <param name="componentType">Component type</param>
        /// <param name="lifestyleType">Optional <see cref="LifestyleType" /> for this component</param>
        /// <param name="onRegistration">Optional lambda for customizing our component registration</param>
        /// <returns>
        ///     key of added component
        /// </returns>
        [CanBeNull]
        protected virtual string AddComponent(
            [NotNull] Type serviceType,
            [NotNull] Type componentType,
            LifestyleType? lifestyleType = null,
            [CanBeNull] Action<ComponentRegistration<object>> onRegistration = null)
        {
            if (Kernel.HasComponent(componentType))
            {
                return null;
            }

            lifestyleType ??=
                serviceType.Is<IQuartzScopedJob>()
                    ? LifestyleType.Scoped
                    : _lifestyleType ?? LifestyleType.Transient;

            string key = componentType.AssemblyQualifiedName;
            ComponentRegistration<object> component =
                Component
                    .For(serviceType)
                    .ImplementedBy(componentType)
                    .LifeStyle.Is(lifestyleType.Value);
            onRegistration?.Invoke(component);
            Kernel.Register(component);

            return key;
        }

        [NotNull]
        protected virtual NameValueCollection ConvertToNameValueCollection(
            [CanBeNull] IDictionary<string, string> properties)
        {
            NameValueCollection result = new NameValueCollection();
            if (properties != null)
            {
                foreach (KeyValuePair<string, string> keyValuePair in properties)
                {
                    result.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            return result;
        }

        [NotNull]
        protected virtual IDictionary<string, string> ConvertToDictionary(
            [CanBeNull] NameValueCollection nameValueCollection)
        {
            IDictionary<string, string> result = new Dictionary<string, string>();
            if (nameValueCollection != null)
            {
                foreach (string key in nameValueCollection.AllKeys)
                {
                    result[key] = nameValueCollection[key];
                }
            }

            return result;
        }

        [NotNull]
        protected virtual IDictionary<string, string> Merge(
            [CanBeNull] IDictionary<string, string> properties,
            [CanBeNull] IDictionary<string, string> additionalProperties)
        {
            IDictionary<string, string> result = properties ?? new Dictionary<string, string>();
            if (additionalProperties != null)
            {
                foreach (string key in additionalProperties.Keys)
                {
                    result[key] = additionalProperties[key];
                }
            }

            return result;
        }

        [NotNull]
        protected virtual NameValueCollection FromApplicationConfig(
            [CanBeNull] string sectionName,
            [CanBeNull] IDictionary<string, string> additionalProperties)
            => ConvertToNameValueCollection(
                (sectionName != null) && ConfigurationManager.GetSection(sectionName) is NameValueCollection nameValueCollection
                    ? Merge(ConvertToDictionary(nameValueCollection), additionalProperties)
                    : additionalProperties);

        protected virtual void CheckProperties(
            [CanBeNull] NameValueCollection properties,
            [NotNull] Type serviceType,
            [NotNull] Type componentType)
        {
            if ((properties == null) || (properties.Count == 0))
            {
                throw new ProgrammingError(
                    "No properties found to configure a SchedulerFactory, identified by" +
                    $"(service:{serviceType.Name}, component{componentType.Name})!");
            }

            if (!properties.AllKeys.Contains(StdSchedulerFactory.PropertySchedulerInstanceName))
            {
                throw new ProgrammingError(
                    $"Property, identified by {StdSchedulerFactory.PropertySchedulerInstanceName}, " +
                    "not found to configure a SchedulerFactory, identified by" +
                    $"(service:{serviceType.Name}, component{componentType.Name})!");
            }

            string schedulerName = properties[StdSchedulerFactory.PropertySchedulerInstanceName];
            IScheduler scheduler =
                SchedulerRepository
                    .Instance
                    .Lookup(schedulerName)
                    .ConfigureAwait(false)
                    .GetAwaiter()
                    .GetResult();
            if (scheduler != null)
            {
                throw new ProgrammingError(
                    $"A scheduler, identified by {schedulerName}, " +
                    "is already registered in the scheduler-repository." +
                    "While registering a SchedulerFactory, identified by" +
                    $"(service:{serviceType.Name}, component{componentType.Name})!");
            }
        }

        [UsedImplicitly]
        public class AdditionalScheduler
        {
            public AdditionalScheduler(
                [CanBeNull] string sectionName,
                [CanBeNull] IDictionary<string, string> properties,
                [NotNull] Type schedulerFactoryServiceType,
                [NotNull] Type schedulerFactoryComponentType,
                [NotNull] Type schedulerServiceType,
                [NotNull] Type schedulerComponentType,
                bool waitForJobsToCompleteAtShutdown,
                bool startScheduler)
            {
                SectionName = sectionName;
                Properties = properties;
                SchedulerFactoryServiceType = schedulerFactoryServiceType;
                if (!schedulerFactoryServiceType.Is<IQuartzSchedulerFactory>())
                {
                    throw new ProgrammingError($"{nameof(schedulerFactoryServiceType)} should implement {typeof(IQuartzSchedulerFactory).FullName}");
                }

                SchedulerFactoryComponentType = schedulerFactoryComponentType;
                if (!schedulerFactoryComponentType.Is<IQuartzSchedulerFactory>())
                {
                    throw new ProgrammingError($"{nameof(schedulerFactoryComponentType)} should implement {typeof(IQuartzSchedulerFactory).FullName}");
                }

                SchedulerServiceType = schedulerServiceType;
                if (!schedulerServiceType.Is<IQuartzScheduler>())
                {
                    throw new ProgrammingError($"{nameof(schedulerServiceType)} should implement {typeof(IQuartzSchedulerFactory).FullName}");
                }

                SchedulerComponentType = schedulerComponentType;
                if (!schedulerComponentType.Is<IQuartzScheduler>())
                {
                    throw new ProgrammingError($"{nameof(schedulerServiceType)} should implement {typeof(IQuartzSchedulerFactory).FullName}");
                }

                WaitForJobsToCompleteAtShutdown = waitForJobsToCompleteAtShutdown;
                StartScheduler = startScheduler;
            }

            [CanBeNull]
            public string SectionName { get; }

            [CanBeNull]
            public IDictionary<string, string> Properties { get; }

            [NotNull]
            public Type SchedulerFactoryServiceType { get; }

            [NotNull]
            public Type SchedulerFactoryComponentType { get; }

            [NotNull]
            public Type SchedulerServiceType { get; }

            [NotNull]
            public Type SchedulerComponentType { get; }

            public bool WaitForJobsToCompleteAtShutdown { get; }

            public bool StartScheduler { get; }
        }
    }
}
