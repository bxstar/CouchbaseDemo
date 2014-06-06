using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;

using Couchbase;
using Couchbase.Configuration;
using Enyim.Caching.Memcached;
using Newtonsoft.Json;

namespace CouchbaseConsole
{
    public static class CacheManager
    {
        private static readonly CouchbaseClient _instance;
        private static readonly object Lock1=new object();
        private static CouchbaseClient _instance1;
        private static readonly object Lock2 = new object();
        private static CouchbaseClient _instance2;

        private static readonly string BucketName = System.Configuration.ConfigurationSettings.AppSettings["bucketName"];
        private static readonly string BucketPassword = System.Configuration.ConfigurationSettings.AppSettings["bucketPassword"];
        static CacheManager()
        {
            //_instance=new CouchbaseClient("CentaWebPage","CentaWebPage");
            _instance = new CouchbaseClient();
        }

        public static CouchbaseClient Instance
        {
            get { return _instance; }
        }

        public static CouchbaseClient GetWriteInstance()
        {
            if (_instance1 == null)
            {
                lock (Lock1)
                {
                    if (_instance1 == null)
                    {
                        var uri = new Uri("http://10.4.18.26:8091/pools");
                        var config = new CouchbaseClientConfiguration();
                        config.Urls.Add(uri);
                        config.Bucket = BucketName;
                        config.BucketPassword = BucketPassword;
                        _instance1 = new CouchbaseClient(config);
                    }
                }
            }
            return _instance1;
        }
        public static CouchbaseClient GetReadInstance()
        {
            if (_instance2 == null)
            {
                lock (Lock2)
                {
                    if (_instance2 == null)
                    {
                        var uri = new Uri("http://10.4.18.101:8091/pools");
                        var config = new CouchbaseClientConfiguration();
                        config.Urls.Add(uri);
                        config.Bucket = BucketName;
                        config.BucketPassword = BucketPassword;
                        _instance2 = new CouchbaseClient(config);
                    }
                }
            }
            return _instance2;
        }
    }
}
