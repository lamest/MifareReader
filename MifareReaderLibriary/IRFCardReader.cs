using System;

namespace MifareReaderLibriary
{
    public interface IRFCardReader
    {
        void StartScan();
        void StopScan();
        event EventHandler CardRegistered;
    }
}