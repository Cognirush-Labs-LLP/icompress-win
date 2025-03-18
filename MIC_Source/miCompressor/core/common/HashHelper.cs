using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace miCompressor.core.common
{
    public static class HashHelper
    {
        public static string ComputeStableHash(params object[] values)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                StringBuilder sb = new StringBuilder();
                foreach (var value in values)
                {
                    if (value == null)
                    {
                        sb.Append("null|");
                    }
                    else if (value is string || value.GetType().IsPrimitive)
                    {
                        sb.Append(value.ToString());
                        sb.Append("|");
                    }
                    else
                    {
                        // Serialize complex objects to JSON
                        string json = JsonSerializer.Serialize(value);
                        sb.Append(json);
                        sb.Append("|");
                    }
                }

                byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}
