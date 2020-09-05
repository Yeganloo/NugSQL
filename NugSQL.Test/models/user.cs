namespace NugSQL.Test
{
    public class User
    {
        public int id { get; set; }

        public string user_name { get; set; }

        public string profile { get; set; }

        public byte[] salt { get; set; }

        public byte[] password { get; set; }

        public short status { get; set; }
    }
}