using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public class Guild
    {
        [Key] public ulong Id { get; set; }

        public string Language { get; set; }

        public bool? SearchQualityFilter { get; set; }

        public static void Describe(ModelBuilder model)
        {
        }
    }
}