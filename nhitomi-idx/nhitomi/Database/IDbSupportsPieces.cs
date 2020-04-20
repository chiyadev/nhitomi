using System;
using Microsoft.AspNetCore.WebUtilities;
using nhitomi.Models;

namespace nhitomi.Database
{
    public interface IDbSupportsPieces
    {
        DbPiece[] Pieces { get; set; }

        /// <summary>
        /// Calculates a hash that represents all piece hashes combined.
        /// </summary>
        string GetCombinedPieceHash()
        {
            var count = Pieces.Length;

            byte[] buffer;

            if (count == 1)
            {
                buffer = Pieces[0].Hash;
            }
            else
            {
                // make a contiguous array of hashes
                buffer = new byte[count * Piece.HashSize];

                for (var i = 0; i < count; i++)
                    Buffer.BlockCopy(Pieces[i].Hash, 0, buffer, i * Piece.HashSize, Piece.HashSize);
            }

            return WebEncoders.Base64UrlEncode(HashUtility.Sha256(buffer), 0, Piece.HashSize);
        }
    }
}