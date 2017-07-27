using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MifareReaderLibriary
{
    internal interface IMifareCardWriter
    {
        Task<bool> WriteDataAsync(string readerName, ICollection<MifareKey> keys, string data, CancellationToken ct);
        Task<bool> ChangeTrailersAsync(string readerName, IDictionary<MifareKey, SectorTrailer> data, CancellationToken ct);
    }
}
