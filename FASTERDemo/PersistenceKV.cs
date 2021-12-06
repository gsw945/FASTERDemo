using FASTER.core;
using System;
using System.IO;
using System.Threading;

namespace FASTERDemo
{
    public class PersistenceKV<TKey, TValue> : IDisposable
    {
        private FasterKV<TKey, TValue> store;
        private IDevice log;
        private IDevice objLog;
        private System.Timers.Timer timer;

        public PersistenceKV(string logPath = @"C:\Temp\hlog.log")
        {
            Console.WriteLine($"{GetType().Name} logPath: {logPath}");
            log = Devices.CreateLogDevice(logPath, preallocateFile: true, deleteOnClose: true, recoverDevice: false, useIoCompletionPort: true);
            string checkPointPath = Path.Combine(Path.GetDirectoryName(logPath), "CheckPoint");
            Console.WriteLine($"{GetType().Name} checkPointPath: {checkPointPath}");
            if (!Directory.Exists(checkPointPath))
            {
                Directory.CreateDirectory(checkPointPath);
            }
            var objLogPath = $"{logPath}.log";
            Console.WriteLine($"{GetType().Name} objLogPath: {objLogPath}");
            objLog = Devices.CreateLogDevice(objLogPath, preallocateFile: true, deleteOnClose: false, recoverDevice: true, useIoCompletionPort: true);
            store = new FasterKV<TKey, TValue>(
                size: 1L << 20,
                logSettings: new LogSettings()
                {
                    LogDevice = log,
                    ObjectLogDevice = objLog
                },
                checkpointSettings: new CheckpointSettings()
                {
                    CheckpointDir = checkPointPath,
                    RemoveOutdated = true,
                    CheckPointType = CheckpointType.FoldOver,
                }
            );
            try
            {
                store.Recover();
            }
            catch (FasterException ex)
            {
                Console.Error.WriteLine($"{ex.GetType()}: {ex.Message}");
            }
            timer = new System.Timers.Timer(interval: 512)
            {
                AutoReset = true,
                Enabled = false,
            };
            long isElapsing = 0;
            timer.Elapsed += async (object sender, System.Timers.ElapsedEventArgs eventArgs) =>
            {
                if (Interlocked.Read(ref isElapsing) > 0)
                {
                    return;
                }
                Interlocked.Increment(ref isElapsing);
                try
                {
                    await store.TakeHybridLogCheckpointAsync(CheckpointType.Snapshot);
                    await store.CompleteCheckpointAsync();
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{ex.GetType().Name}: {ex.Message}");
                }
                finally
                {
                    Interlocked.Decrement(ref isElapsing);
                }
            };
            timer.Start();
        }

        public ClientSession<TKey, TValue, TValue, TValue, Empty, IFunctions<TKey, TValue, TValue, TValue, Empty>> GetSession()
        {
            return store.NewSession(new SimpleFunctions<TKey, TValue>(), Guid.NewGuid().ToString());
        }

        public bool TrySetKV(TKey key, TValue value, out Status status)
        {
            using (var session = GetSession())
            {
                status = session.Upsert(ref key, ref value, Empty.Default);
                // session.CompletePending(wait: true);
                return status == Status.OK;
            }
        }

        public bool TryGetKV(TKey key, ref TValue value, out Status status)
        {
            using (var session = GetSession())
            {
                status = session.Read(ref key, ref value);
                return status == Status.OK;
            }
        }

        public bool TryDelKV(TKey key, out Status status)
        {
            using (var session = GetSession())
            {
                status = session.Delete(ref key);
                return status == Status.OK;
            }
        }

        public void Dispose()
        {
            timer.Stop();
            store.Dispose();
            log.Dispose();
            objLog.Dispose();
        }
    }
}
