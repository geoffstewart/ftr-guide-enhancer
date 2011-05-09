namespace GuideEnricher
{
    public static class StringExtensionMethods
    {
        public static string TruncateString(this string myString, int maxLength)
        {
            if (myString.Length > maxLength)
            {
                return myString.Substring(0, maxLength);
            }

            return myString;
        }
    }
}
