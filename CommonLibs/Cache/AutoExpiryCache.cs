using System;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLibs
{
    public class AutoExpiryCache<TKey, TValue> : Cache<TKey, TValue>
    {
        private Thread _ExpiryThread;
        private readonly object _WaitLock;
        private volatile bool _Quit;
        private volatile bool _RestartWait;

        private TimeSpan _MaxEntryAge;
        public TimeSpan MaxEntryAge
        {
            get { return _MaxEntryAge; }
            set { _MaxEntryAge = value; }
        }

        private TimeSpan _ExpiryInterval;
        public TimeSpan ExpiryInterval
        {
            get { return _ExpiryInterval; }
            set
            {
                _ExpiryInterval = value;
                RestartWaiting();
            }
        }

        private int _NumberOfExpiryRuns = 0;
        public int NumberOfExpiryRuns
        {
            get { return _NumberOfExpiryRuns; }
        }


        public bool ExpiryThreadIsRunning
        {
            get { return _ExpiryThread != null && _ExpiryThread.IsAlive; }
        }

        private void RestartWaiting()
        {
            _RestartWait = true;
            WakeUpThread();
        }

        private void WakeUpThread()
        {
            lock (_WaitLock)
            {
                Monitor.PulseAll(_WaitLock);
            }
        }

        public AutoExpiryCache(string cacheName, int expireMin)
        {
            _Quit = false;
            _RestartWait = false;
            _WaitLock = new object();
            _MaxEntryAge = TimeSpan.FromMinutes(expireMin);
            _ExpiryInterval = TimeSpan.FromMinutes(expireMin / 2);

            _ExpiryThread = new Thread(ThreadMain);
            _ExpiryThread.Name = cacheName + "ExpiryThread";
            _ExpiryThread.IsBackground = true;
            _ExpiryThread.Start();
        }

        private void ThreadMain()
        {
            while (!_Quit)
            {
                lock (_WaitLock)
                {
                    _RestartWait = false;
                    Monitor.Wait(_WaitLock, ExpiryInterval);
                    if (!_Quit && !_RestartWait)
                    {
                        Expire(MaxEntryAge);
                        ++_NumberOfExpiryRuns;
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _Quit = true;
                WakeUpThread();
                if (_ExpiryThread != null)
                {
                    if (!_ExpiryThread.Join(100))
                    {
                        //_ExpiryThread.Abort();
                    }
                    _ExpiryThread = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
