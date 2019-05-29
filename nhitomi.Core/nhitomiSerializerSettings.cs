using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace nhitomi.Core
{
    public class nhitomiSerializerSettings : JsonSerializerSettings
    {
        public static void Apply(JsonSerializerSettings settings)
        {
            settings.DefaultValueHandling = DefaultValueHandling.Ignore;
            settings.NullValueHandling = NullValueHandling.Ignore;
            settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        }

        public nhitomiSerializerSettings()
        {
            Apply(this);
        }
    }
}