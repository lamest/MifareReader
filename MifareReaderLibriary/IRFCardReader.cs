using System;

namespace MifareReaderLibriary
{
    public interface IRFCardReader
    {
        string[] GetDevices();
        void StartScan(string deviceName);
        void StopScan(string deviceName);
        event EventHandler CardRegistered;
    }
}