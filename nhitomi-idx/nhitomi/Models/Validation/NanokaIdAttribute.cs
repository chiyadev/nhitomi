using System.ComponentModel.DataAnnotations;
using ChiyaFlake;

namespace nhitomi.Models.Validation
{
    public class NanokaIdAttribute : RegularExpressionAttribute
    {
        public NanokaIdAttribute() : base($@"^[\w\-]{{3,{Snowflake.MaxLength}}}$") { }
    }
}