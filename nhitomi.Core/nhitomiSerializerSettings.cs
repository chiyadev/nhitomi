using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace nhitomi.Core
{
    public class nhitomiSerializerSettings : JsonSerializerSettings
    {
        public static void Apply(JsonSerializerSettings settings)
        {
            settings.Formatting            = Formatting.None;
            settings.TypeNameHandling      = TypeNameHandling.None;
            settings.ContractResolver      = new CamelCasePropertyNamesContractResolver();
            settings.NullValueHandling     = NullValueHandling.Ignore;
            settings.DefaultValueHandling  = DefaultValueHandling.IgnoreAndPopulate;
            settings.StringEscapeHandling  = StringEscapeHandling.EscapeNonAscii;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        }

        public nhitomiSerializerSettings()
        {
            Apply(this);
        }
    }
}