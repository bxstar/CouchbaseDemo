using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Couchbase;
using Couchbase.Extensions;
using Enyim.Caching.Memcached;
using Newtonsoft.Json;

namespace CouchbaseConsole
{
    class Sample
    {
    }

    public class Demo1
    {
        public static void test1()
        {
            var newBeer = @"{
                ""name"": ""Old Yankee Ale"",
                ""abv"": 5.00,
                ""ibu"": 0,
                ""srm"": 0,
                ""upc"": 0,
                ""type"": ""beer"",
                ""brewery_id"": ""cottrell_brewing"",
                ""updated"": ""2012-08-30 20:00:20"",
                ""description"": ""A medium-bodied Amber Ale"",
                ""style"": ""American-Style Amber"",
                ""category"": ""North American Ale""
                }";

            var key = "cottrell_brewing-old_yankee_ale";
            try
            {
                var result = CacheManager.Instance.Store(StoreMode.Add, key, newBeer);
                if (result)
                {
                    Console.WriteLine("Cache store ok!");
                    Console.WriteLine(CacheManager.Instance.Get(key));
                }

                //result = CacheManager.Instance.Remove(key);
                //if (result)
                //{
                //    Console.WriteLine("Cache remove ok!");
                //}
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
            }

        }
    }

    public class Demo2
    {
        private static CouchbaseClient client = CacheManager.Instance;
        public static void test1()
        {
            var tmp = client.Get<Post>("a09590d8-2e98-46b0-a07f-00cfedd3aefd");
            Console.WriteLine(tmp==null?"fail":"ok");
            var newBeer = new Beer()
            {
                Name = "Old Yankee Ale",
                ABV = 5.00f,
                BreweryId = "cottrell_brewing",
                Style = "American-Style Amber",
                Category = "North American Ale"

            };
            var key = "cottrell_brewing-old_yankee_ale-demo2";
            try
            {
                var handler = new StoreHandler(client);
                //client.Remove(key);
                //var result = client.StoreJson(StoreMode.Set, key, newBeer);
                var result = handler.Set(key, newBeer);
                if (result.Success)
                {
                    Console.WriteLine("Cache store ok!");
                    Console.WriteLine(client.Get(key));
                    var cacheBeer = handler.Get(key); //client.GetJson<Beer>(key);
                    Console.WriteLine("Beer's Name:" + cacheBeer.Value);

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        [Serializable]
        class Beer
        {
            [JsonProperty("name")]
            public string Name { get; set; }
            [JsonProperty("abv")]
            public float ABV { get; set; }

            [JsonProperty("type")]
            public string Type
            {
                get { return "beer"; }
            }

            [JsonProperty("brewery_id")]
            public string BreweryId { get; set; }

            [JsonProperty("style")]
            public string Style { get; set; }

            [JsonProperty("category")]
            public string Category { get; set; }

        }
    }

    public class Demo3
    {
        //private static readonly CouchbaseClient client = CacheManager.Instance;

        public static void test1()
        {
            var postid = Guid.NewGuid();
            var post = new Post
            {
                PostId = postid,
                Title = "test",
                Description = "11111111111111111111111111111111111111111111111111111111111111111111111111"
            };
            var result = CacheManager.GetWriteInstance().Store(StoreMode.Set, postid.ToString(), post, new TimeSpan(0, 15, 0));
            Console.WriteLine("Result:"+result);
            var tmp = CacheManager.GetReadInstance().Get<Post>(postid.ToString());
            Console.WriteLine("GetResult:"+(tmp==null?"Fail":"Ok"));
        }

    }
}
