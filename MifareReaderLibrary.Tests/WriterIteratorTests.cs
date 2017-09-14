using System;
using System.Collections.Generic;
using System.Linq;
using MifareReaderLibriary;
using NUnit.Framework;

namespace MifareReaderLibrary.Tests
{
    [TestFixture]
    public class WriterIteratorTests
    {
        private static byte[] StandardData = {
            0x00, 0x00, 0x00,
            0x11, 0x11, 0x11,
            0x22, 0x22, 0x22,
            0x33, 0x33, 0x33,
            0x44, 0x44, 0x44,
            0x55, 0x55, 0x55,
            0x66, 0x66, 0x66,
            0x77, 0x77, 0x77,
            0x88, 0x88, 0x88,
            0x99, 0x99, 0x99,
            0x00, 0x00, 0x00
        };

        private static byte[] EmptyData = { };

        private static byte[][] TestCaseSource1 = {StandardData, EmptyData};

        [Test, TestCaseSource(nameof(TestCaseSource1))]
        public void Iterator_InputBytesEquivalentToOutput(byte[] testData)
        {
            var writer = new MifareCardWriter();
            var resultBytes = new List<byte>();
            foreach (var bytes in writer.GenerateChunk(testData))
            {
                Console.WriteLine(BitConverter.ToString(bytes));
                resultBytes.AddRange(bytes);
            }

            Assert.That(testData, Is.EquivalentTo(resultBytes));
        }

        [Test, TestCaseSource(nameof(TestCaseSource1))]
        public void Iterator_InputBytesBreaksIntoGroupsOfBlockSize(byte[] testData)
        {
            var writer = new MifareCardWriter();
            var bytesPerBlock = MifareCardWriter.BytesPerBlock;
            var resultBytes = new List<byte[]>();
            foreach (var bytes in writer.GenerateChunk(testData))
            {
                Console.WriteLine(BitConverter.ToString(bytes));
                resultBytes.Add(bytes);
            }

            foreach (var bytes in resultBytes.Take(resultBytes.Count - 1))
            {
                Assert.AreEqual(bytesPerBlock, bytes.Length);
            }

            if (resultBytes.Count != 0)
            {
                var lastChunk = resultBytes.Last().ToArray();
                Assert.That(lastChunk.Length, Is.InRange(0, bytesPerBlock));
            }
        }

        [Test]
        public void Iterator_EmptyInput_NotGenerateOutput()
        {
            var writer = new MifareCardWriter();
            foreach (var bytes in writer.GenerateChunk(EmptyData))
            {
                Assert.Fail();
            }
            Assert.Pass();
        }

        [Test]
        public void Iterator_NullInput_Throws_ArgumentNullException()
        {
            var writer = new MifareCardWriter();
            Assert.Throws<ArgumentNullException>(() =>
            {
                writer.GenerateChunk(null).GetEnumerator().MoveNext();
            });
        }
    }
}

