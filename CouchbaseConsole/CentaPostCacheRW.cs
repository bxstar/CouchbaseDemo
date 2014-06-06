using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IBatisNet.Common;
using IBatisNet.DataAccess;
using IBatisNet.DataMapper;
using IBatisNet.DataMapper.Configuration;
using System.Diagnostics;
using Couchbase;
using Couchbase.Extensions;
using Enyim.Caching.Memcached;
using IBatisNet.DataMapper.Configuration.Sql.Dynamic.Elements;
using Newtonsoft.Json;

namespace CouchbaseConsole
{
    public class CentaPostCacheRW
    {
        private static readonly SqlMapper Mapper = null;
        private static readonly CouchbaseClient Client = CacheManager.Instance;

       static IList<Post> _postList=new List<Post>();  

        private static readonly int PageIndex =
            Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["pageIndex"]);

        private static readonly int PageCount =
            Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings["pageCount"]);

        static CentaPostCacheRW()
        {
            try
            {
                var builder = new DomSqlMapBuilder();
                Mapper = builder.Configure("SqlMap.config") as SqlMapper;
                
            }
            catch (IBatisNet.DataMapper.Exceptions.DataMapperException ex)
            {
                throw ex;
            }
            
        }

        #region DualUri测试
        //多线程写缓存
        public static void ParallelWriteCache()
        {
            var bag = new ConcurrentBag<string>();
            var now = DateTime.Now;
            var watch = new Stopwatch();
            watch.Start();
            var count = Mapper.QueryForObject("GetPostCount", null);
            //var pageCount = (int)count / 100 + 1;
            var pageCount = PageCount;
            Console.WriteLine("当前共{0}条数据，可分页{1}", count, pageCount);

            var param = new QueryParams();

            int page = 0;
            Parallel.ForEach(_postList, item =>
            {
                if (page++%100 == 0)
                {
                    Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", (page/100)+1, (DateTime.Now - now).TotalSeconds,
                        pageCount);
                }
                var tmp = Client.Store(StoreMode.Set, item.PostId.ToString(), item, new TimeSpan(0, 0, 60, 0));
                if (!tmp) bag.Add(item.ToString());
                Console.WriteLine(tmp + ":" + item.PostId);
            });

            #region 

            //Parallel.For(PageIndex, pageCount, i =>
            //{
            //    int page = i;
            //    ++page;
            //    Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", page, (DateTime.Now - now).TotalSeconds,pageCount);
            //    param.index = i * 100;
            //    param.count = 100;
            //    var result = Mapper.QueryForList<Post>("SelectProductByPager", param);
            //    foreach (var item in result)
            //    {
            //        var tmp=Client.Store(StoreMode.Set, item.PostId.ToString(), item, new TimeSpan(0, 0, 5, 0));
            //        if(!tmp) bag.Add(item.ToString());
            //        Console.WriteLine(tmp+":"+item.PostId);
            //    }
            //});

            #endregion

            watch.Stop();
            Console.WriteLine("共执行{0},失败个数{1}", watch.Elapsed,bag.Count);
        }

        //单线程顺序写缓存
        public static void SequenceWriteCache()
        {
            var now = DateTime.Now;
            var watch = new Stopwatch();
            watch.Start();
            var count = Mapper.QueryForObject("GetPostCount", null);
            var pageCount = PageCount;
            Console.WriteLine("当前共{0}条数据，可分页{1}", count, pageCount);
            var errorCount = 0;
            var param = new QueryParams();
   
            for (int i = PageIndex; i < pageCount; i++)
            {
                int page = i;
                ++page;
                Console.WriteLine("begin execute page:{0}", page);
                Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", page, (DateTime.Now - now).TotalSeconds, pageCount);
                param.index = i * 100;
                param.count = 100;
                var result = Mapper.QueryForList<Post>("SelectProductByPager", param);
                foreach (var item in result)
                {
                    var tmp = Client.Store(StoreMode.Set, item.PostId.ToString(), item, new TimeSpan(0, 0, 60, 0));
                    if (!tmp) errorCount++;
                    Console.WriteLine(tmp + ":" + item.PostId);
                }
            }
            watch.Stop();
            Console.WriteLine("共执行{0}，失败个数", watch.Elapsed,errorCount);
        }

        //并发读缓存
        public static void ParallelReadCache()
        {
            var bag=new ConcurrentBag<string>();
            
            var now = DateTime.Now;
            var watch = new Stopwatch();
            watch.Start();
            var count = Mapper.QueryForObject("GetPostCount", null);
            var pageCount = PageCount;
            Console.WriteLine("当前共{0}条数据，可分页{1}", count, pageCount);

            var param = new QueryParams();

            int page = 0;
            Parallel.ForEach(_postList, item =>
            {
                if (page++ % 100 == 0)
                {
                    Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", (page / 100) + 1, (DateTime.Now - now).TotalSeconds,
                        pageCount);
                }
                var tmp = Client.Get(item.PostId.ToString());
                if (tmp == null)
                {
                    bag.Add(item.ToString());
                    Console.WriteLine("{0}:{1}", "Fail", item.PostId);
                }
                else
                {
                    Console.WriteLine("{0}:{1}", "OK", item.PostId);
                }
            });


            #region 

            //Parallel.For(PageIndex, pageCount, i =>
            //{
            //    int page = i;
            //    ++page;
            //    Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", page, (DateTime.Now - now).TotalSeconds, pageCount);
            //    param.index = i * 100;
            //    param.count = 100;
            //    var result = Mapper.QueryForList<Post>("SelectProductByPager", param);
            //    foreach (var item in result)
            //    {

            //        var tmp= Client.Get(item.PostId.ToString());
            //        if (tmp == null)
            //        {
            //            bag.Add(item.ToString());
            //            Console.WriteLine("{0}:{1}","Fail",item.PostId);
            //        }
            //        else
            //        {
            //            Console.WriteLine("{0}:{1}","OK",item.PostId);
            //        }

            //    }
            //});

            #endregion

            watch.Stop();
            Console.WriteLine("共执行{0}，失败个数{1}", watch.Elapsed,bag.Count);
        }

        //单线程顺序读缓存
        public static void SequenceReadCache()
        {
            var now = DateTime.Now;
            var watch = new Stopwatch();
            watch.Start();
            var count = Mapper.QueryForObject("GetPostCount", null);
            var pageCount = PageCount;
            Console.WriteLine("当前共{0}条数据，可分页{1}", count, pageCount);
            var errorCount = 0;
            var param = new QueryParams();
            for (int i = PageIndex; i < pageCount; i++)
            {
                int page = i;
                ++page;
                Console.WriteLine("begin execute page:{0}", page);
                Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", page, (DateTime.Now - now).TotalSeconds, pageCount);
                param.index = i * 100;
                param.count = 100;
                var result = Mapper.QueryForList<Post>("SelectProductByPager", param);
                foreach (var item in result)
                {
                    var tmp = Client.Get<Post>(item.PostId.ToString());
                    if (tmp == null)
                    {
                        errorCount++;
                        Console.WriteLine("{0}:{1}", "Fail" , item.PostId);
                    }
                    else
                    {
                        Console.WriteLine("{0}:{1}", "OK", item.PostId);
                    }
                }
            }
            watch.Stop();
            Console.WriteLine("共执行{0}，失败个数{1}", watch.Elapsed,errorCount);
        }
        #endregion

        #region  读写分离测试
        //多线程写缓存
        public static void ParallelSingalWriteCache()
        {
            var bag = new ConcurrentBag<string>();
            var client = CacheManager.GetWriteInstance();
            var now = DateTime.Now;
            var watch = new Stopwatch();
            watch.Start();
            var count = Mapper.QueryForObject("GetPostCount", null);
            //var pageCount = (int)count / 100 + 1;
            var pageCount = PageCount;
            Console.WriteLine("当前共{0}条数据，可分页{1}", count, pageCount);

            var param = new QueryParams();

            int page = 0;
            Parallel.ForEach(_postList, item =>
            {
                if (page++ % 100 == 0)
                {
                    Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", (page / 100) + 1, (DateTime.Now - now).TotalSeconds,
                        pageCount);
                }
                var tmp = client.Store(StoreMode.Set, item.PostId.ToString(), item, new TimeSpan(0, 0, 60, 0));
                if (!tmp) bag.Add(item.ToString());
                Console.WriteLine(tmp + ":" + item.PostId);
            });

            #region 

            //Parallel.For(PageIndex, pageCount, i =>
            //{
            //    int page = i;
            //    ++page;
            //    Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", page, (DateTime.Now - now).TotalSeconds,
            //        pageCount);
            //    param.index = i*100;
            //    param.count = 100;
            //    var result = Mapper.QueryForList<Post>("SelectProductByPager", param);
            //    foreach (var item in result)
            //    {
            //        var tmp = client.Store(StoreMode.Set, item.PostId.ToString(), item, new TimeSpan(0, 0, 5, 0));
            //        if (!tmp) bag.Add(item.ToString());
            //        Console.WriteLine(tmp + ":" + item.PostId);
            //    }
            //});

            #endregion

            watch.Stop();
            Console.WriteLine("共执行{0}，失败个数{1}", watch.Elapsed,bag.Count);
        }

        //单线程顺序写缓存
        public static void SequenceSingalWriteCache()
        {
            var client = CacheManager.GetWriteInstance();
            var now = DateTime.Now;
            var watch = new Stopwatch();
            watch.Start();
            var count = Mapper.QueryForObject("GetPostCount", null);
            var pageCount = PageCount;
            Console.WriteLine("当前共{0}条数据，可分页{1}", count, pageCount);
            var errorCount = 0;
            var param = new QueryParams();

            for (int i = PageIndex; i < pageCount; i++)
            {
                int page = i;
                ++page;
                Console.WriteLine("begin execute page:{0}", page);
                Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", page, (DateTime.Now - now).TotalSeconds, pageCount);
                param.index = i * 100;
                param.count = 100;
                var result = Mapper.QueryForList<Post>("SelectProductByPager", param);
                foreach (var item in result)
                {
                    var tmp = client.Store(StoreMode.Set, item.PostId.ToString(), item, new TimeSpan(0, 0, 60, 0));
                    if (!tmp) errorCount++;
                    Console.WriteLine(tmp + ":" + item.PostId);
                }
            }
            watch.Stop();
            Console.WriteLine("共执行{0}，失败个数{1}", watch.Elapsed,errorCount);
        }

        //并发读缓存
        public static void ParallelSingalReadCache()
        {
            var bag = new ConcurrentBag<string>();
            var client = CacheManager.GetReadInstance();
            var now = DateTime.Now;
            var watch = new Stopwatch();
            watch.Start();
            var count = Mapper.QueryForObject("GetPostCount", null);
            var pageCount = PageCount;
            Console.WriteLine("当前共{0}条数据，可分页{1}", count, pageCount);

            var param = new QueryParams();

            int page = 0;
            Parallel.ForEach(_postList, item =>
            {
                if (page++ % 100 == 0)
                {
                    Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", (page / 100) + 1, (DateTime.Now - now).TotalSeconds,
                        pageCount);
                }
                var tmp = client.Get(item.PostId.ToString());
                if (tmp == null)
                {
                    bag.Add(item.ToString());
                    Console.WriteLine("{0}:{1}", "Fail", item.PostId);
                }
                else
                {
                    Console.WriteLine("{0}:{1}", "OK", item.PostId);
                }
            });

            #region 

            //Parallel.For(PageIndex, pageCount, i =>
            //{
            //    int page = i;
            //    ++page;
            //    Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", page, (DateTime.Now - now).TotalSeconds,
            //        pageCount);
            //    param.index = i*100;
            //    param.count = 100;
            //    var result = Mapper.QueryForList<Post>("SelectProductByPager", param);
            //    foreach (var item in result)
            //    {

            //        var tmp = client.Get(item.PostId.ToString());
            //        if (tmp == null)
            //        {
            //            Console.WriteLine("{0}:{1}", "Fail", item.PostId);
            //            bag.Add(item.ToString());
            //        }
            //        else
            //        {
            //            Console.WriteLine("{0}:{1}", "OK", item.PostId);
            //        }
            //    }
            //});

            #endregion

            watch.Stop();
            Console.WriteLine("共执行{0}，失败个数{1}", watch.Elapsed,bag.Count);
        }

        //单线程顺序读缓存
        public static void SequenceSingalReadCache()
        {
            var client = CacheManager.GetReadInstance();
            var now = DateTime.Now;
            var watch = new Stopwatch();
            watch.Start();
            var count = Mapper.QueryForObject("GetPostCount", null);
            var pageCount = PageCount;
            Console.WriteLine("当前共{0}条数据，可分页{1}", count, pageCount);
            var errorCount = 0;
            var param = new QueryParams();
            for (int i = PageIndex; i < pageCount; i++)
            {
                int page = i;
                ++page;
                Console.WriteLine("begin execute page:{0}", page);
                Console.Title = string.Format("当前执行第{0}页,共{2}页,已执行时间{1}秒", page, (DateTime.Now - now).TotalSeconds, pageCount);
                param.index = i * 100;
                param.count = 100;
                var result = Mapper.QueryForList<Post>("SelectProductByPager", param);
                foreach (var item in result)
                {
                    var tmp = client.Get<Post>(item.PostId.ToString());
                    if (tmp == null)
                    {
                        errorCount++;
                        Console.WriteLine("{0}:{1}","Fail", item.PostId);
                    }
                    else
                    {
                        Console.WriteLine("{0}:{1}", "OK", item.PostId);
                    }
                }
            }
            watch.Stop();
            Console.WriteLine("共执行{0}，失败个数{1}", watch.Elapsed,errorCount);
        }

        #endregion

        public static void InitPostData()
        {
            var now = DateTime.Now;
            var pageCount = PageCount;
            Console.WriteLine("正在准备数据");
            var errorCount = 0;
            var param = new QueryParams();
            for (int i = PageIndex; i < pageCount; i++)
            {
                int page = i;
                ++page;
                Console.Write(".");
                Console.Title = string.Format("当前正在准备数据，执行第{0}页,共{2}页,已执行时间{1}秒", page, (DateTime.Now - now).TotalSeconds, pageCount);
                param.index = i * 100;
                param.count = 100;
                var result = Mapper.QueryForList<Post>("SelectProductByPager", param);
                foreach (var item in result)
                {
                    _postList.Add(item);
                }
            }
            Console.WriteLine();
            Console.WriteLine("数据准备完成，共{0}条数据",_postList.Count);
        }

    }

    public class QueryParams
    {
        public int index { get; set; }
        public int count { get; set; }
    }
}
