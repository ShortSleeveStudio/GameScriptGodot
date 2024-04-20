namespace GameScript
{
    static class DbHelper
    {
        public static string SqlitePathToURI(string path) => $"Data Source={path};Pooling=False";
    }
}
