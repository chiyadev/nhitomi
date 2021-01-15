using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models
{
    public class DonationInfo : DonationInfoBase, IHasId
    {
        /// <summary>
        /// Donation month in the format "{year}-{month}".
        /// </summary>
        public string Id { get; set; }
    }

    public class DonationInfoBase
    {
        /// <summary>
        /// Donation progress in USD.
        /// </summary>
        [Required]
        public double Progress { get; set; }
    }
}