using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace nhitomi.Models.Validation
{
    /// <summary>
    /// Validates an enumerable <see cref="Piece"/> property that all pieces are tightly packed i.e. every piece has size equal to <see cref="Piece.MaxSize"/> except the last piece.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PieceListAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            switch (value)
            {
                case null:
                    break;

                case IEnumerable<Piece> pieces:
                    var size  = null as int?;
                    var index = 0;

                    foreach (var piece in pieces)
                    {
                        // check that the previous piece has maximum length
                        if (size != null && size != Piece.MaxSize)
                            return new ValidationResult($"Piece array is not tightly packed at index {index - 1} (size: {size}).");

                        size = piece.Size;
                        ++index;
                    }

                    // last piece in the array can have variable length
                    break;

                default:
                    throw new InvalidCastException($"Property does not implement {nameof(IEnumerable<Piece>)}.");
            }

            return ValidationResult.Success;
        }
    }
}