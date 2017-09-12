using System;

namespace MifareReaderLibriary
{
    public class CardRegisteredEventArgs : EventArgs
    {
        public CardRegisteredEventArgs(byte[] cardData)
        {
            CardData = cardData;
        }

        public byte[] CardData { get; private set; }
    }
}