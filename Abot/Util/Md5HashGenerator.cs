using System;
using System.Security.Cryptography;

namespace Abot.Util
{
    public interface IHashGenerator
    {
        byte[] GenerateHash(byte[] input);
        byte[] GenerateHash(string input);
    }

    [Obsolete("Nothing uses this")]
    public class Md5HashGenerator : IHashGenerator
    {
        static MD5 _md5Impl = System.Security.Cryptography.MD5.Create();

        public byte[] GenerateHash(string input)
        {
            return GenerateHash(System.Text.Encoding.ASCII.GetBytes(input));
        }

        public byte[] GenerateHash(byte[] input)
        {
            return _md5Impl.ComputeHash(input);
        }
    }
}
