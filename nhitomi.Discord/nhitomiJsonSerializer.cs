// Copyright (c) 2018 phosphene47
// 
// This software is released under the MIT License.
// https://opensource.org/licenses/MIT

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace nhitomi
{
    public class nhitomiJsonSerializer : JsonSerializer
    {
        public nhitomiJsonSerializer()
        {
            DefaultValueHandling = DefaultValueHandling.Ignore;
            NullValueHandling = NullValueHandling.Ignore;
            ContractResolver = new CamelCasePropertyNamesContractResolver();
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        }
    }
}