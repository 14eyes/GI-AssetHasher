using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace GI_AssetHasher
{
    public class Hashing
    {
        public static ulong GetPathHash(string path)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(path);
            byte[] array = new byte[(path.Length >> 8) + 1 << 8];
            Array.Copy(bytes, 0, array, 0, bytes.Length);
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(array);
                ulong num = 0UL;
                for (int i = 4; i >= 0; i--)
                {
                    num <<= 8;
                    num |= (ulong)hash[i];
                }
                return num;
            }
        }
        public static ulong PreLastToHash(byte Pre, uint Last) => ((ulong)Last << 8) | Pre;
    }
}
