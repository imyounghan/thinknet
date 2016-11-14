using System.Collections;

namespace ThinkNet.Common.Caching
{
    internal sealed class HashtableCache : ICache
    {
        private Hashtable hashtable;

        public HashtableCache(string regionName) 
        {
            this.RegionName = regionName;
            this.hashtable = Hashtable.Synchronized(new Hashtable());
        }

        public object Get(string key)
        {
            return hashtable[key];
        }

        public void Put(string key, object value)
        {
            hashtable[key] = value;
        }

        public void Remove(string key)
        {
            hashtable.Remove(key);
        }

        public void Clear()
        {
            hashtable.Clear();
        }

        public void Destroy()
        {
            this.Clear();
            hashtable = null;
        }

        public string RegionName
        {
            get;
            private set;
        }
    }
}
