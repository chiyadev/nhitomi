using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.WebUtilities;

namespace nhitomi.Models.Validation
{
    /// <summary>
    /// Validates a <see cref="WebEncoders.Base64UrlEncode(byte[])"/>'ed piece hash string.
    /// </summary>
    public class HashStringAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return null;

            if (!(value is string str))
                return new ValidationResult("Value must be a string.");

            try
            {
                var bytes = WebEncoders.Base64UrlDecode(str);

                if (bytes.Length != Piece.HashSize)
                    return new ValidationResult("Invalid piece hash length.");
            }
            catch (FormatException)
            {
                return new ValidationResult("Invalid piece hash format.");
            }

            return ValidationResult.Success;
        }
    }
}