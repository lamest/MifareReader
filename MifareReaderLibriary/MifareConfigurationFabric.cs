using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MifareReaderLibriary
{
    public static class MifareConfigurationFabric
    {
        public static MifareConfiguration GetDefaultConfig()
        {
            return new MifareConfiguration()
            {
                MSB = 0x00,
                LSB = 0x08,
                AuthKeys = new[]
                {
                    new MifareKey
                    {
                        Type = MifareKeyType.KeyA,
                        SectorNumber = 0,
                        Key = new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}
                    }
                }
            };
        }

        public static MifareConfiguration GetWriteConfig()
        {
            return new MifareConfiguration()
            {
                MSB = 0x00,
                LSB = 0x08,
                AuthKeys = new[]
                {
                    new MifareKey
                    {
                        Type = MifareKeyType.KeyA,
                        SectorNumber = 0,
                        Key = new byte[] {0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF}
                    }
                }
            };
        }
    }

    public class MifareConfiguration
    {
        public byte MSB { get; set; } 
        public byte LSB { get; set; }
        public IList<MifareKey> AuthKeys { get; set; }
    }

    public class MifareKey
    {
        public MifareKeyType Type { get; set; }
        public byte SectorNumber { get; set; }
        public byte[] Key { get; set; }
    }

    public class SectorTrailer
    {
        public byte[] KeyA { get; set; }
        public byte[] AccessBits { get; set; }
        public byte GenerapPurposeByte { get; set; }
        public byte[] KeyB { get; set; }



        public bool Verify()
        {
            return VerifyKeyA() &&
                   VerifyKeyB() &&
                   VerifyAccessBits() &&
                   VerifyGPB();
        }

        private bool VerifyGPB()
        {
            return true;
        }

        private bool VerifyAccessBits()
        {
            return AccessBits.Length == 3;
        }

        private bool VerifyKeyA()
        {
            return KeyA.Length == 6;
        }
        private bool VerifyKeyB()
        {
            return KeyB.Length == 6;
        }
    }

    public enum MifareKeyType
    {
        KeyA,
        KeyB
    }
}
