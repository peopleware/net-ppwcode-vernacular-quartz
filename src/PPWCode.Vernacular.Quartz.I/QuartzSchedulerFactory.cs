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
using System.Collections.Specialized;
using System.Linq;

using Castle.MicroKernel;
using Castle.MicroKernel.Context;

using JetBrains.Annotations;

using Quartz.Impl;

namespace PPWCode.Vernacular.Quartz.I
{
    /// <inheritdoc cref="StdSchedulerFactory" />
    [UsedImplicitly]
    public class QuartzSchedulerFactory
        : StdSchedulerFactory,
          IQuartzSchedulerFactory
    {
        private bool _isInitialized;

        public QuartzSchedulerFactory([NotNull] IKernel kernel)
        {
            Kernel = kernel;
        }

        public IKernel Kernel { get; }

        /// <inheritdoc cref="IQuartzSchedulerFactory.Initialize" />
        public override void Initialize(NameValueCollection props)
        {
            if (!_isInitialized)
            {
                base.Initialize(props);
                _isInitialized = true;
            }
        }

        /// <inheritdoc />
        [NotNull]
        protected override TService InstantiateType<TService>(Type componentType)
        {
            TService instance;

            IHandler handler = FindComponentHandler<TService>(componentType);
            if ((handler == null) || (componentType == null))
            {
                instance = base.InstantiateType<TService>(componentType);
            }
            else
            {
                CreationContext creationContext =
                    new CreationContext(
                        handler,
                        Kernel.ReleasePolicy,
                        typeof(TService),
                        null,
                        null,
                        null);
                instance = (TService)handler.Resolve(creationContext);
            }

            return instance;
        }

        [CanBeNull]
        protected virtual IHandler FindComponentHandler<TService>(Type componentType)
            => Kernel
                .GetHandlers(typeof(TService))
                .SingleOrDefault(h => h.ComponentModel.Implementation == componentType);
    }
}
