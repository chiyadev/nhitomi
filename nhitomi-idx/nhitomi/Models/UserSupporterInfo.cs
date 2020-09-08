using System;
using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models
{
    public class UserSupporterInfo
    {
        /// <summary>
        /// Time when the supporter period started for this user for the first time.
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Time when the supporter period ended for this user for the last time.
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Total number of months this user has been a supporter for.
        /// </summary>
        [Required]
        public int TotalMonths { get; set; }

        /// <summary>
        /// Total amount of money in USD spent by this user for supporter.
        /// </summary>
        [Required]
        public double TotalSpending { get; set; }
    }
}