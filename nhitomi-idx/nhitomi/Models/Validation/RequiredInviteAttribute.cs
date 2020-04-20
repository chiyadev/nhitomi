using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace nhitomi.Models.Validation
{
    public class RequiredInviteAttribute : ValidationAttribute
    {
        public override bool RequiresValidationContext => true;

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var options = validationContext.GetService<IOptionsSnapshot<UserServiceOptions>>().Value;

            if (options.OpenRegistration || value != null)
                return ValidationResult.Success;

            return new ValidationResult("Invite is required.");
        }
    }
}