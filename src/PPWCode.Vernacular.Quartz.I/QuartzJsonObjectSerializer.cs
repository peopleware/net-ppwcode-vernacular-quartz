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

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using Quartz.Simpl;

namespace PPWCode.Vernacular.Quartz.I
{
    /// <inheritdoc />
    /// <remarks>Serialize enums as string. </remarks>
    public class QuartzJsonObjectSerializer : JsonObjectSerializer
    {
        /// <inheritdoc />
        protected override JsonSerializerSettings CreateSerializerSettings()
        {
            JsonSerializerSettings serializerSettings = base.CreateSerializerSettings();
            serializerSettings.Converters.Add(new StringEnumConverter { AllowIntegerValues = false });

            return serializerSettings;
        }
    }
}
