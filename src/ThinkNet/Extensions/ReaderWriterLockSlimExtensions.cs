using System;
using System.Threading;

namespace ThinkNet
{
    /// <summary>
    /// <see cref="ReaderWriterLockSlim"/> 的扩展类
    /// </summary>
    public static class ReaderWriterLockSlimExtensions
    {
        static class Locks
        {
            public static void GetReadLock(ReaderWriterLockSlim locks)
            {
                bool lockAcquired = false;
                while (!lockAcquired)
                    lockAcquired = locks.TryEnterUpgradeableReadLock(1);
            }

            public static void GetReadOnlyLock(ReaderWriterLockSlim locks)
            {
                bool lockAcquired = false;
                while (!lockAcquired)
                    lockAcquired = locks.TryEnterReadLock(1);
            }

            public static void GetWriteLock(ReaderWriterLockSlim locks)
            {
                bool lockAcquired = false;
                while (!lockAcquired)
                    lockAcquired = locks.TryEnterWriteLock(1);
            }

            public static void ReleaseReadOnlyLock(ReaderWriterLockSlim locks)
            {
                if (locks.IsReadLockHeld)
                    locks.ExitReadLock();
            }

            public static void ReleaseReadLock(ReaderWriterLockSlim locks)
            {
                if (locks.IsUpgradeableReadLockHeld)
                    locks.ExitUpgradeableReadLock();
            }

            public static void ReleaseWriteLock(ReaderWriterLockSlim locks)
            {
                if (locks.IsWriteLockHeld)
                    locks.ExitWriteLock();
            }

            public static void ReleaseLock(ReaderWriterLockSlim locks)
            {
                ReleaseWriteLock(locks);
                ReleaseReadLock(locks);
                ReleaseReadOnlyLock(locks);
            }

            public static ReaderWriterLockSlim GetLockInstance()
            {
                return GetLockInstance(LockRecursionPolicy.SupportsRecursion);
            }

            public static ReaderWriterLockSlim GetLockInstance(LockRecursionPolicy recursionPolicy)
            {
                return new ReaderWriterLockSlim(recursionPolicy);
            }
        }

        abstract class BaseLock : IDisposable
        {
            protected ReaderWriterLockSlim _Locks;

            public BaseLock(ReaderWriterLockSlim locks)
            {
                _Locks = locks;
            }

            public abstract void Dispose();
        }

        class ReadLock : BaseLock
        {
            public ReadLock(ReaderWriterLockSlim locks)
                : base(locks)
            {
                Locks.GetReadLock(this._Locks);
            }

            public override void Dispose()
            {
                Locks.ReleaseReadLock(this._Locks);
            }
        }

        class ReadOnlyLock : BaseLock
        {
            public ReadOnlyLock(ReaderWriterLockSlim locks)
                : base(locks)
            {
                Locks.GetReadOnlyLock(this._Locks);
            }

            public override void Dispose()
            {
                Locks.ReleaseReadOnlyLock(this._Locks);
            }
        }

        class WriteLock : BaseLock
        {
            public WriteLock(ReaderWriterLockSlim locks)
                : base(locks)
            {
                Locks.GetWriteLock(this._Locks);
            }

            public override void Dispose()
            {
                Locks.ReleaseWriteLock(this._Locks);
            }
        }

        /// <summary>
        /// An atom read action wrapper.
        /// </summary>
        public static void AtomRead(this ReaderWriterLockSlim readerWriterLockSlim, Action action)
        {
            using (new ReadOnlyLock(readerWriterLockSlim)) {
                action();
            }
        }
        /// <summary>
        /// An atom read action wrapper.
        /// </summary>
        public static void AtomRead(this ReaderWriterLockSlim readerWriterLockSlim, Action<ReaderWriterLockSlim> action)
        {
            using (new ReadLock(readerWriterLockSlim)) {
                action(readerWriterLockSlim);
            }
        }
        /// <summary>
        /// An atom read func wrapper.
        /// </summary>
        public static T AtomRead<T>(this ReaderWriterLockSlim readerWriterLockSlim, Func<T> function)
        {
            using (new ReadOnlyLock(readerWriterLockSlim)) {
                return function();
            }
        }
        /// <summary>
        /// An atom read func wrapper.
        /// </summary>
        public static T AtomRead<T>(this ReaderWriterLockSlim readerWriterLockSlim, Func<ReaderWriterLockSlim, T> function)
        {
            using (new ReadLock(readerWriterLockSlim)) {
                return function(readerWriterLockSlim);
            }
        }
        /// <summary>
        /// An atom write action wrapper.
        /// </summary>
        public static void AtomWrite(this ReaderWriterLockSlim readerWriterLockSlim, Action action)
        {
            using (new WriteLock(readerWriterLockSlim)) {
                action();
            }
        }
        /// <summary>
        /// An atom write func wrapper.
        /// </summary>
        public static T AtomWrite<T>(this ReaderWriterLockSlim readerWriterLockSlim, Func<T> function)
        {
            using (new WriteLock(readerWriterLockSlim)) {
                return function();
            }
        }
    }
}
