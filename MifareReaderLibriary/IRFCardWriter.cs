using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MifareReaderLibriary
{
    public interface IMifareCardWriter
    {
        Task<bool> WriteDataAsync(string readerName, ICollection<MifareKey> keys, byte[] data, CancellationToken ct);
        Task<bool> ChangeTrailersAsync(string readerName, IDictionary<MifareKey, SectorTrailer> data, CancellationToken ct);
    }
}