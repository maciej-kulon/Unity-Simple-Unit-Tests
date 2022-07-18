namespace SimpleTests.Extensions
{
    public static class Extensions
    {
        public static bool IsEmpty(this object value)
        {
            if (value == null)
                return false;
            return string.IsNullOrEmpty(value.ToString());
        }

        public static bool IsNotEmpty(this object value)
        {
            if (value == null)
                return false;
            return !string.IsNullOrEmpty(value.ToString());
        }
    }
}