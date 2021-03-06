﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace CommonLibs
{
    public class CommonManager
    {
        public CommonManager()
        {

        }

        private object m_lock_me = new object();
        private Dictionary<string, Dictionary<int, object>> m_locks = new Dictionary<string, Dictionary<int, object>>();

        public ConcurrentDictionary<int, AutoResetEvent> RequestHnadles = new ConcurrentDictionary<int, AutoResetEvent>();

        private int activeReqId = 0;
        private ConcurrentDictionary<int, string> m_reqIds = new ConcurrentDictionary<int, string>();

        public int GetReqId(string pattern = "")
        {
            var id = System.Threading.Interlocked.Increment(ref activeReqId);
            m_reqIds.AddOrUpdate(id, pattern, (key, oldValue) => pattern);
            return id;
        }
        public bool CheckValidReqId(int reqId, out string pattern)
        {
            if (m_reqIds.ContainsKey(reqId))
            {
                m_reqIds.TryRemove(reqId, out pattern);
                return true;
            }
            else
            {
                pattern = string.Empty;
                return false;
            }
        }

        public object GetLockerById<T>(int id)
        {
            object locker = null;
            lock(m_lock_me)
            {
                string typeName = typeof(T).Name;
                if (!m_locks.ContainsKey(typeName))
                    m_locks[typeName] = new Dictionary<int, object>();
                if (!m_locks[typeName].ContainsKey(id))
                    m_locks[typeName][id] = new object();
                locker = m_locks[typeName][id];
            }
            return locker;
        }

        private Dictionary<string, string> m_pubKeys = new Dictionary<string, string>();
        public string GetPublicKey(string name)
        {
            string key = string.Empty;
            lock (m_pubKeys)
            {
                if (!m_pubKeys.TryGetValue(name, out key))
                {
                    string curpath = Directory.GetCurrentDirectory();
                    string filename = name + "public.key";
                    string file = curpath + @"/" + filename;

                    using (var fs = File.OpenRead(file))
                    {
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            key = sr.ReadToEnd().Replace("\n", "");
                            m_pubKeys[name] = key;
                            Console.WriteLine("PubKeyXML, file={0}, keyLength={1}", file, key.Length);
                        }
                    }
                }
            }
            return key;
        }
        private Dictionary<string, string> m_privKeys = new Dictionary<string, string>();
        public string GetPrivateKey(string name)
        {
            string key = string.Empty;
            lock (m_privKeys)
            {
                if (!m_privKeys.TryGetValue(name, out key))
                {
                    string curpath = Directory.GetCurrentDirectory();
                    string filename = name + "private.key";
                    string file = curpath + @"/" + filename;

                    using (var fs = File.OpenRead(file))
                    {
                        using (StreamReader sr = new StreamReader(fs))
                        {
                            key = sr.ReadToEnd().Replace("\n", "");
                            m_privKeys[name] = key;
                            Console.WriteLine("PrivKeyXML, file={0}, keyLength={1}", file, key.Length);
                        }
                    }
                }
            }
            return key;
        }
    }
}
