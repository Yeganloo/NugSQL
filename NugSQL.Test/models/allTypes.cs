using System;
using System.Collections;

namespace NugSQL.Test
{
    public class allTypes
    {
        public long my_int64 { get; set; }
        public int my_int32 { get; set; }
        public short my_int16 { get; set; }
        public string my_string { get; set; }
        public byte[] my_bytes { get; set; }
        public bool my_bool { get; set; }
        public Jsonb my_json { get; set; }
        public float my_single { get; set; }
        public double my_double { get; set; }
        public bool my_bit { get; set; }
        public BitArray my_bits { get; set; }
        public Guid my_guid { get; set; }
        public DateTime my_dateTime { get; set; }
    }
}