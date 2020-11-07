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

using System.Threading;
using System.Threading.Tasks;

namespace PPWCode.Vernacular.Quartz.I
{
    /// <summary>
    ///     <para>This manager is responsible to start all registered schedulers.</para>
    ///     <para>The <see cref="QuartzFacility" /> is responsible for registering a scheduler.</para>
    ///     A registered scheduler will be started if
    ///     <list type="bullet">
    ///         <item>it is the default scheduler</item>
    ///         <item>
    ///             an additional scheduler that is explicit marked as startable,
    ///             <see cref="QuartzFacility.AdditionalScheduler.StartScheduler" />, using the
    ///             <see cref="QuartzFacility.UseAdditionalScheduler" />.
    ///         </item>
    ///     </list>
    /// </summary>
    public interface ISchedulerManager
    {
        /// <summary>
        ///     Start all registered schedulers.
        /// </summary>
        /// <param name="cancellationToken">Propagates notification that operations should be canceled.</param>
        /// <returns>
        ///     An asynchronous operation.
        /// </returns>
        Task StartSchedulersAsync(CancellationToken cancellationToken = default);
    }
}
