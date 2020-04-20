using System.ComponentModel.DataAnnotations;
using ChiyaFlake;

namespace nhitomi.Models.Validation
{
    public class nhitomiIdAttribute : RegularExpressionAttribute
    {
        public nhitomiIdAttribute() : base($@"^[\w\-]{{3,{Snowflake.MaxLength}}}$") { }
    }
}