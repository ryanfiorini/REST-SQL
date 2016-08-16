using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REST_SQL.Extensions
{
    public static class Extensions
    {
        /// <summary>
        /// Converts a camel case to underscore case
        /// </summary>
        /// <param name="value">this</param>
        /// <returns></returns>
        public static string ToUnderscore(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            int len = value.Length;
            int outLen = len * 2;
            int outPos = 0;
            char[] input = value.ToCharArray();
            char[] output = new char[outLen];
            bool previousCharWasLower = false;

            for (int idx = 0; idx != len; idx++)
            {
                char current = input[idx];
                if (previousCharWasLower)
                {
                    if (char.IsUpper(current))
                    {
                        output[outPos++] = '_';
                    }
                }
                output[outPos++] = char.ToUpper(input[idx]);
                previousCharWasLower = char.IsLower(current);
            }
            return new string(output, 0, outPos);
        }


        /// <summary>
        /// Converts underscore case to camel case
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string ToCamel(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            int len = value.Length;
            int outLen = len;
            int outPos = 0;
            char[] input = value.ToCharArray();
            char[] output = new char[outLen];
            bool previousCharUnderscore = true;

            for (int idx = 0; idx != len; idx++)
            {
                char current = input[idx];
                if (current == '_')
                {
                    previousCharUnderscore = true;
                    continue;
                }
                if (previousCharUnderscore)
                {
                    if (idx == 0)
                    {
                        output[outPos++] = char.ToLower(input[idx]);
                    }
                    else
                    {
                        output[outPos++] = char.ToUpper(input[idx]);
                    }
                }
                else
                {
                    output[outPos++] = char.ToLower(input[idx]);
                }
                previousCharUnderscore = false;
            }
            return new string(output, 0, outPos);
        }

        public static string ToJsonName(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (value.StartsWith("@")) value = value.Substring(1);
            return ToCamel(value);
        }

        public static string ToSqlName(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            if (!value.StartsWith("@")) value = $"@{value}";
            return ToUnderscore(value);
        }

        public static string FromSqlParameterName(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;

            string temp = value.StartsWith("@") ? value.Substring(1) : value;
            return ToCamel(temp);
        }

        public static string FromSqlProcedureName(this string value)
        {
            if (string.IsNullOrEmpty(value)) return value;
            string prefix = value.Substring(0, value.IndexOf('_'));
            string temp = value;
            if (prefix == "GET" || prefix == "POST" || prefix == "PUT" || prefix == "DELETE")
            {
                temp = value.Substring(value.IndexOf('_'));
            }
            return ToCamel(temp);
        }
    }
}
