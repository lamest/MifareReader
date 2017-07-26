using System;

namespace MifareReaderLibriary
{
    public class CardRegisteredEventArgs : EventArgs
    {
        public CardRegisteredEventArgs(string cardData)
        {
            CardData = cardData;
        }

        public string CardData { get; private set; }
    }
}