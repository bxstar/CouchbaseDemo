using System;

namespace CouchbaseConsole
{
    
    class Program
    {
        private static void Main(string[] args)
        {
            Console.WindowWidth = 120;
            Console.WindowHeight = 45;
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine("程序启动时间 :{0}", DateTime.Now);
            //Demo3.test1();
            //Demo2.test1();
            //Demo1.test1();
            CentaPostCacheRW.InitPostData();
            while (true)
            {
                
                Console.WriteLine("请选择以下操作：");
                Console.WriteLine("1.DualUri并发写缓存；2.DualUri顺序写缓存；3.DualUri并发读缓存；4.DualUri顺序读缓存；");
                Console.WriteLine("5.读写分离并发写缓存；6.读写分离顺序写缓存；7.读写分离并发读缓存；8.读写分离顺序读缓存");
                Console.Write("请输入（1，2，3，4，7，6，7，8，9）：");
                var key = Console.ReadLine();
                switch (key)
                {
                    case "1":
                        CentaPostCacheRW.ParallelWriteCache();
                        break;
                    case "2":
                        CentaPostCacheRW.SequenceWriteCache();
                        break;
                    case "3":
                        CentaPostCacheRW.ParallelReadCache();
                        break;
                    case "4":
                        CentaPostCacheRW.SequenceReadCache();
                        break;
                    case "5":
                        CentaPostCacheRW.ParallelSingalWriteCache();
                        break;
                    case "6":
                        CentaPostCacheRW.SequenceSingalWriteCache();
                        break;
                    case "7":
                        CentaPostCacheRW.ParallelSingalReadCache();
                        break;
                    case "8":
                        CentaPostCacheRW.SequenceSingalReadCache();
                        break;
                    default:
                        break;
                }
                Console.WriteLine();
                Console.WriteLine("Please enter any key to exit...");
                Console.ReadKey();
            }
        }

    }

    
}
