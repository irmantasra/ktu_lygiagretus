
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lygiagretus_lab1
{
    public class MailBox
    {
        private int _letter;
        private readonly int _readers;
        private bool _canWrite;
        private bool[] _canRead;
        private readonly object _locker;

        public MailBox(int readers)
        {
            _readers = readers;
            _canRead = Enumerable.Repeat(false, readers).ToArray();
            _letter = 0;
            _canWrite = true;
            _locker = new object();
        }

        public void Put(int newLetter)
        {
            lock (_locker)
            {
                while (!_canWrite)
                {
                    Monitor.Wait(_locker);
                }
                _letter = newLetter;
                _canWrite = false;
                _canRead = Enumerable.Repeat(true, _readers).ToArray();
                Monitor.PulseAll(_locker);
            }
        }
        public int Get(int k)
        {
            int newLetter;
            lock (_locker)
            {
                while (!_canRead[k])
                {
                    Monitor.Wait(_locker);
                }
                newLetter = _letter;
                _canRead[k] = false;
                _canWrite = _canRead.All(c => !c);
                Monitor.PulseAll(_locker);
            }
            return newLetter;
        }
    }

    public class Reader
    {
        private readonly int _itemsToRead;
        public List<int> Letters { get; }
        private readonly MailBox _mailbox;
        private readonly int _id;

        public Reader(int itemsToRead, MailBox mailbox, int id)
        {
            _itemsToRead = itemsToRead;
            _mailbox = mailbox;
            _id = id;
            Letters = new List<int>();
        }
        public void Read()
        {
            for (var i = 0; i < _itemsToRead; i++)
            { 
                Letters.Add(_mailbox.Get(_id));
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            const int readerCount = 5;
            const int itemsProcessed = 6;
            var mailbox = new MailBox(readerCount);
            var readers = Enumerable.Range(0, readerCount)
                .Select(i => new Reader(itemsProcessed, mailbox, i)).ToList();
            var threads = readers
                .Select(reader => new Thread(reader.Read)).ToList();
            threads.Add(new Thread(() =>
            {
                for (var i = 0; i < itemsProcessed; i++) { mailbox.Put(i * i); }
            }));
            foreach (var thread in threads) { thread.Start(); }
            foreach (var thread in threads) { thread.Join(); }
            var lines = readers.Select(r => r.Letters)
                .Select(letters => string.Join(", ", letters));
            foreach (var line in lines) { Console.WriteLine(line); }
            Console.ReadKey();
        }
    }
}
