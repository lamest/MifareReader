using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using PCSC;
using PCSC.Iso7816;

namespace MifareReaderLibriary
{
    public class MifareCardReader : IRFCardReader
    {
        private const int MaxDataSize = 15 * 16 * 3;
        private const byte MaxSector = 16;
        private const byte BlocksPerSector = 4;
        private const byte BytesPerBlock = 16;
        private MifareConfiguration _config;
        private SCardMonitor _monitor;

        public MifareCardReader(MifareConfiguration config)
        {
            _config = config;
        }

        public void StartScan(string deviceName)
        {
            _monitor = new SCardMonitor(ContextFactory.Instance, SCardScope.System);
            _monitor.CardInserted += OnCardInserted;
            _monitor.Start(deviceName);
        }

        public void StopScan(string deviceName)
        {
            _monitor.Cancel();
            _monitor.CardInserted -= OnCardInserted;
        }

        public event EventHandler<CardRegisteredEventArgs> CardRegistered;

        public string[] GetDevices()
        {
            var contextFactory = ContextFactory.Instance;
            using (var context = contextFactory.Establish(SCardScope.System))
            {
                return context.GetReaders();
            }
        }

        private string ATRToHexStr(byte[] atr)
        {
            var hexStrBuilder = new StringBuilder();
            foreach (var b in atr)
            {
                hexStrBuilder.AppendLine(Convert.ToString(b, 16).PadLeft(2, '0'));
            }
            return hexStrBuilder.ToString();
        }

        private void OnCardInserted(object sender, CardStatusEventArgs e)
        {
            if (!IsValidATR(e.Atr))
            {
                Debug.WriteLine("Card with invalid ATR inserted");
                return;
            }
            Debug.WriteLine("Card with valid ATR inserted");
            var currentContext = ContextFactory.Instance.Establish(SCardScope.System);
            using (var isoReader = new IsoReader(currentContext, e.ReaderName, SCardShareMode.Shared, SCardProtocol.Any))
            {
                var card = new MifareCard(isoReader);
                var loadKeySuccessful = card.LoadKey(
                    KeyStructure.NonVolatileMemory,
                    0x00, // first key slot
                    _config.AuthKeys[0].Key // key
                );

                if (!loadKeySuccessful)
                {
                    //key load failed
                    Debug.WriteLine("Auth key load failed");
                    return;
                }
                Debug.WriteLine("Auth key load success");

                var cardDump = new List<byte>();

                //read all blocks except 0 sector and trailers
                for (byte sectorNumber = 1; sectorNumber < MaxSector; sectorNumber++)
                {
                    for (byte blockNumber = 0; blockNumber < 3; blockNumber++)
                    {
                        var currentBlock = (byte) (sectorNumber * BlocksPerSector + blockNumber);
                        //auth in every block
                        var authSuccessful = card.Authenticate(0, currentBlock, KeyType.KeyA, 0x00);
                        if (!authSuccessful)
                        {
                            //autentication failed
                            Debug.WriteLine($"Authentification to block {currentBlock} with key number {0} failed");
                            return;
                        }
                        Debug.WriteLine($"Authentification to block {currentBlock} succeded");

                        var blockResult = card.ReadBinary(0, currentBlock, BytesPerBlock);
                        if (blockResult == null)
                        {
                            //fail to get block
                            Debug.WriteLine($"Fail to get block {currentBlock}");
                            return;
                        }
                        cardDump.AddRange(blockResult);
                    }
                }

                var result = cardDump.ToArray();
                Debug.WriteLine("Reading complete.");
                if (result.Length != 0)
                {
                    CardRegistered?.Invoke(this, new CardRegisteredEventArgs(result));
                }
            }
        }

        private static bool NoReaderAvailable(ICollection<string> readerNames)
        {
            return readerNames == null || readerNames.Count < 1;
        }

        private string ChooseReader(string[] readerNames)
        {
            return readerNames[1];
        }

        private bool IsValidATR(byte[] cardStatusAtr)
        {
            if (cardStatusAtr[cardStatusAtr.Length - 6] == 1) //1 for mifare 1k
            {
                return true;
            }
            return false;
        }
    }
}