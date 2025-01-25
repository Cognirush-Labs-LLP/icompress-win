using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace miCompressor.core
{
    public static class ReaderWriterLockSlimExtensions
    {
        public static IDisposable ReadLock(this ReaderWriterLockSlim rwLock)
        {
            rwLock.EnterReadLock();
            return new LockReleaser(() => rwLock.ExitReadLock());
        }

        public static IDisposable WriteLock(this ReaderWriterLockSlim rwLock)
        {
            rwLock.EnterWriteLock();
            return new LockReleaser(() => rwLock.ExitWriteLock());
        }

        private class LockReleaser : IDisposable
        {
            private readonly Action _release;
            public LockReleaser(Action release) => _release = release;
            public void Dispose() => _release(); // Ensures lock is released when disposed
        }
    }
}
