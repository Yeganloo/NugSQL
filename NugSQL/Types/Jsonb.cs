namespace NugSQL
{
    public class Jsonb
    {
        public Jsonb(string s)
        {
            this.Value = s;
        }
        private string Value;

        public static implicit operator string(Jsonb j) => j.Value;
        public static implicit operator Jsonb(string s) => new Jsonb(s);
        public override string ToString() => this.Value;
    }
}