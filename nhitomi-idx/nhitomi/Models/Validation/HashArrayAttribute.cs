using System;
using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Validation
{
    /// <summary>
    /// byte[][]
    /// </summary>
    public class HashArrayAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return null;

            if (!(value is byte[][] hashes))
                throw new ArgumentException("Valid is not byte[][].");

            for (var i = 0; i < hashes.Length; i++)
            {
                var hash = hashes[i];

                if (hash == null)
                    return new ValidationResult($"Null hash at index {i}.");

                if (hash.Length != Piece.HashSize)
                    return new ValidationResult($"Invalid hash size {hash.Length} at index {i}.");
            }

            return ValidationResult.Success;
        }
    }
}