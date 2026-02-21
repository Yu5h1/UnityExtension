namespace Yu5h1Lib
{
    public class NotEmptyLogic : Logic
    {
        public override bool Evaluate(object value)
        {
            if (value == null) return false;
            if (value is string s) return !string.IsNullOrEmpty(s);
            return true;
        }
    }
}
