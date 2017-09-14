using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PCSC;
using PCSC.Iso7816;

namespace MifareReaderLibriary
{
    public class MifareCardWriter:IMifareCardWriter
    {
        private const int MaxDataSize = 15 * 16 * 3;
        private const byte MaxSector = 16;
        private const byte BlocksPerSector = 4;
        public const byte BytesPerBlock = 16;

        public string[] FindReaders()
        {
            var contextFactory = ContextFactory.Instance;
            using (var context = contextFactory.Establish(SCardScope.System))
            {
                return context.GetReaders();
            }
        }

        public IEnumerable<byte[]> GenerateChunk(IEnumerable<byte> input)
        {
            if (input == null)
            {
                throw new ArgumentNullException();
            }
            int currentChunk = 0;
            byte[] returnArray = input.Skip(currentChunk * BytesPerBlock).Take(BytesPerBlock).ToArray();
            while (returnArray.Length != 0)
            {
                currentChunk++;
                yield return returnArray;
                returnArray = input.Skip(currentChunk * BytesPerBlock).Take(BytesPerBlock).ToArray();
            }
        }

        public async Task<bool> WriteDataAsync(string readerName, ICollection<MifareKey> keys, byte[] bytesToWrite, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(readerName))
            {
                throw new Exception("readerName is null");
            }
            var contextFactory = ContextFactory.Instance;
            var cardStatus = await WaitForCardInsert(readerName, contextFactory, ct);
            if (!IsValidATR(cardStatus.Atr))
            {
                throw new Exception("Invalid card type");
            }
            var context = contextFactory.Establish(SCardScope.System);
            using (var isoReader = new IsoReader(context, readerName, SCardShareMode.Shared, SCardProtocol.Any, false))
            {
                var card = new MifareCard(isoReader);
                var loadKeySuccessful = card.LoadKey(
                    KeyStructure.NonVolatileMemory,
                    0x00, // first key slot
                    keys.First().Key // key
                );

                if (!loadKeySuccessful)
                {
                    throw new Exception("LOAD KEY failed.");
                }

                if (bytesToWrite.Length > MaxDataSize)
                {
                    throw new Exception($"Data length is more than {MaxDataSize}");
                }

                var generator = GenerateChunk(bytesToWrite).GetEnumerator();
                //write to every block except 0 sector and trailers
                for (byte sectorNumber = 1; sectorNumber < MaxSector; sectorNumber++)
                {
                    for (byte blockNumber = 0; blockNumber < BlocksPerSector - 1; blockNumber++)
                    {
                        var currentBlock = (byte) (sectorNumber * BlocksPerSector + blockNumber);
                        if (CheckIfTrailerBlock(currentBlock))
                        {
                            throw new Exception("Trying to write to trailer block");
                        }

                        byte[] blockToWrite = GetNextBlock(generator);
                        if (blockToWrite == null)
                        {
                            return true;
                        }

                        //auth in every block
                        var authSuccessful = card.Authenticate(0, currentBlock, KeyType.KeyA, 0x00);
                        if (!authSuccessful)
                        {
                            throw new Exception("AUTHENTICATE failed.");
                        }
                        Debug.WriteLine($"Authentification to block {currentBlock} succeded");


                        Debug.WriteLine($"In block {currentBlock} writing {BitConverter.ToString(blockToWrite)}");
                        var updateResult = card.UpdateBinary(0, currentBlock, blockToWrite);
                        if (updateResult == false)
                        {
                            //fail to write block
                            Debug.WriteLine($"Fail to write block {currentBlock}");
                            return false;
                        }
                    }
                }
                return false;
            }
        }

        private byte[] GetNextBlock(IEnumerator<byte[]> enumerator)
        {
            byte[] dataToWrite = null;
            if (enumerator.MoveNext() && enumerator.Current != null)
            {
                dataToWrite = enumerator.Current;
            }
            else
            {
                return null;
            }

            byte[] blockToWrite;
            if (dataToWrite.Length < BytesPerBlock)
            {
                blockToWrite = new byte[BytesPerBlock];
                Array.Copy(dataToWrite, blockToWrite, dataToWrite.Length);
            }
            else
            {
                blockToWrite = dataToWrite;
            }
            return blockToWrite;
        }

        public Task<bool> ChangeTrailersAsync(string readerName, IDictionary<MifareKey, SectorTrailer> data, CancellationToken ct)
        {
            throw new NotImplementedException();
        }

        private static async Task<CardStatusEventArgs> WaitForCardInsert(string readerName, IContextFactory contextFactory, CancellationToken ct)
        {
            var monitor = new SCardMonitor(contextFactory, SCardScope.System);
            var tcs = new TaskCompletionSource<CardStatusEventArgs>();
            ct.Register(() => { tcs.TrySetCanceled(); });
            CardInsertedEvent onCardInserted = (sender, args) => { tcs.TrySetResult(args); };
            monitor.CardInserted += onCardInserted;
            monitor.Start(new[] { readerName });
            var cardStatus = await tcs.Task.ConfigureAwait(false);
            monitor.CardInserted -= onCardInserted;
            monitor.Cancel();
            return cardStatus;
        }

        private bool CheckIfTrailerBlock(byte currentBlock)
        {
            return (currentBlock + 1) % 4 == 0;
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
