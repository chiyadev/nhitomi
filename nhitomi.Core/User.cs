using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace nhitomi.Core
{
    public class User
    {
        [Key] public ulong Id { get; set; }

        public ICollection<Collection> Collections { get; set; }

        public string Language { get; set; }

        public static void Describe(ModelBuilder model)
        {
        }
    }
}