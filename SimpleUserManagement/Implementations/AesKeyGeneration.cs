using System;
using System.Linq;
using System.Text;
using System.Security.Cryptography;

namespace SimpleUserManagement.Implementations
{
    public class AesKeyGeneration
    {
        private readonly static Random random = new Random();

        public static string Generate(int length = 16)
        {
            const string lower = "abcdefghijklmnopqrstuvwxyz";
            const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string number = "1234567890";
            const string special = "!@#$%^&*_-=+";

            var bytes = new byte[length];
            new RNGCryptoServiceProvider().GetBytes(bytes);

            var stringBuilder = new StringBuilder();
            foreach (var b in bytes)
            {
                switch (random.Next(4))
                {
                    case 0:
                        stringBuilder.Append(lower[b % lower.Count()]);
                        break;
                    case 1:
                        stringBuilder.Append(upper[b % upper.Count()]);
                        break;
                    case 2:
                        stringBuilder.Append(number[b % number.Count()]);
                        break;
                    case 3:
                        stringBuilder.Append(special[b % special.Count()]);
                        break;
                }
            }
            return stringBuilder.ToString();
        }
    }
}
