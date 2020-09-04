namespace NugSQL.Providers
{
    using System;
    public class PgDatabaseProvider : DatabaseProvider
    {
        public PgDatabaseProvider()
        {
            var ass = "Npgsql.NpgsqlFactory, Npgsql";
            this._factory = GetFactory(ass);
            Console.WriteLine("create factory");
        }
    }
}