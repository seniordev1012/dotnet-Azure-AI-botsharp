using System.Linq;
using System.Text.RegularExpressions;

namespace BotSharp.Abstraction.Utilities;

public static class StringExtensions
{
    public static string IfNullOrEmptyAs(this string str, string defaultValue)
        => string.IsNullOrEmpty(str) ? defaultValue : str;

    public static string SubstringMax(this string str, int maxLength)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        if (str.Length > maxLength)
            return str.Substring(0, maxLength);
        else
            return str;
    }

    public static string CleanPhoneNumber(this string phoneNumber)
    {
        if (phoneNumber != null && !phoneNumber.All(char.IsDigit))
        {
            phoneNumber = Regex.Replace(phoneNumber, @"[^\d]", "");
        }

        if (phoneNumber != null && phoneNumber.Length > 10)
        {
            phoneNumber = phoneNumber.Substring(1);
        }

        return phoneNumber;
    }

    public static string[] SplitByNewLine(this string input)
    {
        return input.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
    }
}
