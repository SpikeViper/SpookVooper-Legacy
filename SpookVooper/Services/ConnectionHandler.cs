using System;
using System.Collections.Generic;


namespace SpookVooper.Data.Services
{
    public class ConnectionHandler : IConnectionHandler
    {
        public Dictionary<int, string> pairs = new Dictionary<int, string>();
        public Dictionary<string, string> apiPairs = new Dictionary<string, string>();
        public Random random = new Random();

        public int GenerateKey(string user)
        {
            int key = random.Next();

            while (pairs.ContainsKey(key))
            {
                key = random.Next();
            }

            pairs.Add(key, user);
            return key;
        }

        public string GetUserFromKey(int key)
        {
            string user;

            pairs.TryGetValue(key, out user);

            return user;
        }

        public void RemoveKey(int key)
        {
            pairs.Remove(key);
        }

        public string GenerateAPIKey(string user)
        {
            string key = Guid.NewGuid().ToString();

            while (apiPairs.ContainsKey(key))
            {
                key = Guid.NewGuid().ToString();
            }

            apiPairs.Add(key, user);
            return key;
        }

        public string GetUserFromAPIKey(string key)
        {
            string user;

            apiPairs.TryGetValue(key, out user);

            return user;
        }

        public void RemoveAPIKey(string key)
        {
            apiPairs.Remove(key);
        }
    }
}
