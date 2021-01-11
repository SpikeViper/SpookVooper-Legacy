namespace SpookVooper.Data.Services
{
    public interface IConnectionHandler
    {
        string GetUserFromKey(int key);
        int GenerateKey(string user);
        void RemoveKey(int key);

        string GetUserFromAPIKey(string key);
        string GenerateAPIKey(string user);
        void RemoveAPIKey(string key);
    }
}
