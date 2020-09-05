namespace NugSQL
{
    public class Json
    {
        public Json(string s)
        {
            this.Value = s;
        }
        private string Value;

        public static implicit operator string(Json j) => j.Value;
        public static implicit operator Json(string s) => new Json(s);
    }
}