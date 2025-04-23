using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace HomeCentre.PIM.Common
{
    public static class CommonHelper
    {
        public static decimal RoundOff(this decimal val, int decimals = 2)
        {
            return Math.Round(val, decimals, MidpointRounding.AwayFromZero);
        }

        public static string MakeFirstLetterCapital(this string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                return val;
            }

            return val.Substring(0, 1).ToUpper() + val.Substring(1).ToLower(); 
        }

        public static string Replace(this string input, Dictionary<string, object> replacement)
        {
            var regex = new Regex("{(?<placeholder>[a-z_][a-z0-9_]*?)}",
                RegexOptions.Compiled | RegexOptions.IgnoreCase);

            return regex.Replace(input, m =>
            {
                var key = m.Groups["placeholder"].Value;
                if (replacement.TryGetValue(key, out var value))
                {
                    return value.ToString();
                }

                throw new Exception($"Unknown key {key}");
            });
        }

        public static decimal RoundOff(this decimal? val, int decimals = 2)
        {
            if (val.HasValue)
            {
                return Math.Round(val.Value, decimals, MidpointRounding.AwayFromZero);
            }

            return 0;
        }

        public static string ToPascalCase(this string str)
        {
            if (!string.IsNullOrEmpty(str))
            {
                str = char.ToUpper(str[0]) + str.Substring(1).ToLower();
            }

            return str;
        }

        public static T DeepClone<T>(this T val)
        {
            var stringVal = JsonConvert.SerializeObject(val);
            return JsonConvert.DeserializeObject<T>(stringVal);
        }

        public static PropertyInfo GetPropertyInfo<TSource, TProperty>(TSource source, Expression<Func<TSource, TProperty>> propertyLambda)
        {
            var type = typeof(TSource);

            var member = propertyLambda.Body as MemberExpression;
            if (member == null)
            {
                throw new ArgumentException(
                    $"Expression '{propertyLambda.ToString()}' refers to a method, not a property.");
            }

            var propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
            {
                throw new ArgumentException(
                    $"Expression '{propertyLambda.ToString()}' refers to a field, not a property.");
            }

            if (type != propInfo.ReflectedType &&
                !type.IsSubclassOf(propInfo.ReflectedType))
            {
                throw new ArgumentException(
                    $"Expression '{propertyLambda.ToString()}' refers to a property that is not from type {type}.");
            }

            return propInfo;
        }

        public static void SetPropertyValue(this object obj, string propName, object value)
        {
            obj.GetType().GetProperty(propName).SetValue(obj, value, null);
        }
    }
}
