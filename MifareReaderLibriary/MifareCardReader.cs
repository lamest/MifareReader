using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PCSC;
using PCSC.Iso7816;

namespace MifareReaderLibriary
{
    public class MifareCardReader : IRFCardReader, IMifareCardWriter
    {
        public MifareCardReader(MifareConfiguration config)
        {
            _config = config;
        }

        private ISCardContext _context;
        private string _readerName;
        private MifareConfiguration _config;

        public void StartScan()
        {
            var contextFactory = ContextFactory.Instance;
            _context = contextFactory.Establish(SCardScope.System);
            var readerNames = _context.GetReaders();
            if (NoReaderAvailable(readerNames))
            {
                throw new Exception("No readers available");
            }

            _readerName = ChooseReader(readerNames);
            var monitor = new SCardMonitor(contextFactory, SCardScope.System);
                monitor.CardInserted += OnCardInserted;
                monitor.Start(new[] {_readerName});
        }

        public void StopScan()
        {
            throw new NotImplementedException();
        }

        public event EventHandler CardRegistered;

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
                return;
            }
            using (var isoReader = new IsoReader(_context, _readerName, SCardShareMode.Shared, SCardProtocol.Any, false))
            {
                var card = new MifareCard(isoReader);

                var loadKeySuccessful = card.LoadKey(
                    KeyStructure.NonVolatileMemory,
                    0x00, // first key slot
                    _config.AuthKeys[0].Key // key
                );

                if (!loadKeySuccessful)
                {
                    throw new Exception("LOAD KEY failed.");
                }

                var authSuccessful = card.Authenticate(_config.MSB, _config.LSB, KeyType.KeyA, 0x00);
                if (!authSuccessful)
                {
                    throw new Exception("AUTHENTICATE failed.");
                }

                var result = card.ReadBinary(_config.MSB, _config.LSB, 16);
                var resultString = result != null
                    ? BitConverter.ToString(result)
                    : null;

                CardRegistered?.Invoke(this, new CardRegisteredEventArgs(resultString));

                //var updateSuccessful = card.UpdateBinary(MSB, LSB, DATA_TO_WRITE);

                //if (!updateSuccessful)
                //{
                //    throw new Exception("UPDATE BINARY failed.");
                //}

                //result = card.ReadBinary(MSB, LSB, 16);
                //Console.WriteLine("Result (after BINARY UPDATE): {0}",
                //    (result != null)
                //        ? BitConverter.ToString(result)
                //        : null);
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

        public async Task<bool> WriteDataAsync(ICollection<MifareKey> keys, string data, CancellationToken ct)
        {
            var contextFactory = ContextFactory.Instance;
            var context = contextFactory.Establish(SCardScope.System);
            var readerNames = context.GetReaders();
            if (NoReaderAvailable(readerNames))
            {
                throw new Exception("No readers available");
            }

            _readerName = ChooseReader(readerNames);
            var monitor = new SCardMonitor(contextFactory, SCardScope.System);
            var tcs = new TaskCompletionSource<CardStatusEventArgs>();
            ct.Register(() =>
            {
                tcs.TrySetCanceled();
            });
            CardInsertedEvent onCardInserted = (sender, args) =>
            {
                tcs.TrySetResult(args);
            };
            monitor.CardInserted += onCardInserted;
            monitor.Start(new[] { _readerName });
            var cardStatus = await tcs.Task;
            if (IsValidATR(cardStatus.Atr))
            {
                throw new Exception("Invalid card type");
            }

            using (var isoReader = new IsoReader(_context, _readerName, SCardShareMode.Exclusive, SCardProtocol.Any, false))
            {
                var card = new MifareCard(isoReader);

                var loadKeySuccessful = card.LoadKey(
                    KeyStructure.NonVolatileMemory,
                    0x00, // first key slot
                    _config.AuthKeys[0].Key // key
                );

                if (!loadKeySuccessful)
                {
                    throw new Exception("LOAD KEY failed.");
                }

                var authSuccessful = card.Authenticate(_config.MSB, _config.LSB, KeyType.KeyA, 0x00);
                if (!authSuccessful)
                {
                    throw new Exception("AUTHENTICATE failed.");
                }

                var result = card.ReadBinary(_config.MSB, _config.LSB, 16);
                var resultString = result != null
                    ? BitConverter.ToString(result)
                    : null;

                CardRegistered?.Invoke(this, new CardRegisteredEventArgs(resultString));

                //var updateSuccessful = card.UpdateBinary(MSB, LSB, DATA_TO_WRITE);

                //if (!updateSuccessful)
                //{
                //    throw new Exception("UPDATE BINARY failed.");
                //}

                //result = card.ReadBinary(MSB, LSB, 16);
                //Console.WriteLine("Result (after BINARY UPDATE): {0}",
                //    (result != null)
                //        ? BitConverter.ToString(result)
                //        : null);
            }

            return true;
        }

        private bool IsValidATR(byte[] cardStatusAtr)
        {
            if (cardStatusAtr[cardStatusAtr.Length - 6] == 1)//1 for mifare 1k
            {
                return true;
            }
            return false;
        }

        public Task<bool> ChangeTrailersAsync(IDictionary<MifareKey, SectorTrailer> data, CancellationToken ct)
        {
            throw new NotImplementedException();
        }
    }
}