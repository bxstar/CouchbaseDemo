using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Couchbase;
using Couchbase.Configuration;
using Couchbase.Operations;
using Enyim.Caching.Memcached;
using Enyim.Caching.Memcached.Results;

namespace CouchbaseConsole
{
   public class StoreHandler
   {
       private readonly CouchbaseClient _client;


       public StoreHandler(CouchbaseClient client)
       {
           _client = client;
       }

       public StoreHandler(IEnumerable<Uri> uris, string bucketName, string bucketPassword)
       {
           var config = new CouchbaseClientConfiguration();
           foreach (var uri in uris)
           {
               config.Urls.Add(uri);
           }
           config.Bucket = bucketName;
           config.BucketPassword = bucketPassword;
           _client=new CouchbaseClient(config);
       }

       public IStoreOperationResult Set(string key, object value)
       {
           return _client.ExecuteStore(StoreMode.Set, key, value);
       }

       public IStoreOperationResult Set(string key, object value, int tries)
       {
           var backoffExp = 0;
           IStoreOperationResult result = null;
           try
           {
               var tryAgain = false;
               do
               {
                   if (backoffExp > tries)
                   {
                       throw new ApplicationException(string.Format("尝试{0}次均无法执行。", tries));
                   }

                   result = _client.ExecuteStore(StoreMode.Set, key, value);
                   if (result.Success) break;

                   if (backoffExp > 0)
                   {
                       var backoffMillis = Math.Pow(2, backoffExp);
                       backoffMillis = Math.Min(1000, backoffMillis);
                       Thread.Sleep((int) backoffMillis);
                   }
                   backoffExp++;
                   if (!result.Success)
                   {
                       var message = result.InnerResult != null ? result.InnerResult.Message : result.Message;
                       Console.WriteLine("错误信息：" + message);
                   }

                   tryAgain = (result.Message != null && result.Message.Contains("Temporary failure") ||
                               result.InnerResult != null && result.InnerResult.Message.Contains("Temporary failure"));
               } while (tryAgain);
           }
           catch (Exception exception)
           {
               Console.WriteLine(exception.Message);
           }
           return result;
       }

       public IGetOperationResult Get(string key)
       {
           return _client.ExecuteGet(key);
       }
   }
}
