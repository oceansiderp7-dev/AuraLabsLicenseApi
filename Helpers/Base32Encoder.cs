using System.Text;

namespace AuraLabsLicenseApi.Helpers;

public static class Base32Encoder
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string Encode(byte[] data)
    {
        if (data.Length == 0)
        {
            return string.Empty;
        }

        var result = new StringBuilder((data.Length + 4) / 5 * 8);
        var buffer = data[0];
        var next = 1;
        var bitsLeft = 8;

        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= (byte)(data[next++] & 0xFF);
                    bitsLeft += 8;
                }
                else
                {
                    var pad = 5 - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }

            var index = 0x1F & (buffer >> (bitsLeft - 5));
            bitsLeft -= 5;
            result.Append(Alphabet[index]);
        }

        return result.ToString();
    }
}
