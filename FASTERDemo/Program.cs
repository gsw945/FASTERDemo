using Nito.AsyncEx;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FASTERDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            
            var nicknameKVPath = Utility.GetFilePath("nicknamekv.log", out _);
            PersistenceKV<string, bool> nicknamesKV = new PersistenceKV<string, bool>(nicknameKVPath);

            AsyncContext.Run(async () => await Task.CompletedTask); // TODO: 待使用
            
            // 初始加载
            var demoData = new List<string>()
            {
                "zhangsan", "lisi", "wangwu", "zhaoliu", "007", "zhuchongba", "xiaojiujiu"
            };
            ConcurrentQueue<string> robotNicknameConfigSet = new ConcurrentQueue<string>();
            var nicknameSet = new HashSet<string>();
            using (var session = nicknamesKV.GetSession())
            {
                foreach (var row in demoData)
                {
                    var nicknameRead = row;
                    bool isUsed = false;
                    var statusRead = session.Read(ref nicknameRead, ref isUsed);
                    if (statusRead == FASTER.core.Status.OK && isUsed)
                    {
                        Console.WriteLine($"{nicknameRead} is used, so skip");
                        continue;
                    }
                    robotNicknameConfigSet.Enqueue(nicknameRead);
                }
            }
            // 模拟使用
            var nicknameWrite = "wangwu";
            var valueWrite = true;
            if (nicknamesKV.TrySetKV(nicknameWrite, valueWrite, out var statusWrite))
            {
                Console.WriteLine($"write {nicknameWrite} OK");
            }
            else
            {
                Console.Error.WriteLine($"write {nicknameWrite} {statusWrite}");
            }

            ManualResetEvent waitQuitEvent = new ManualResetEvent(initialState: false);
            Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs eventArgs) =>
            {
                eventArgs.Cancel = true;
                Console.WriteLine("[Ctrl + C] pressed, to quit.");
                waitQuitEvent.Set();
            };
            waitQuitEvent.WaitOne();

            nicknamesKV.Dispose();
        }

    }
}
