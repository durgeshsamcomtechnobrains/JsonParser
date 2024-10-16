//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace test9
//{
//    public static class Class1
//    {
//        private const string _null = "null";
//        private const string _true = "true";
//        private const string _false = "false";
//        private const string _zeroArg = "{0}";
//        private const string _dateStartJs = "new Date(";
//        private const string _dateEndJs = ")";
//        private const string _dateStart = @"""\/Date(";
//        private const string _dateStart2 = @"/Date(";
//        private const string _dateEnd = @")\/""";
//        private const string _dateEnd2 = @")/";
//        private const string _roundTripFormat = "R";
//        private const string _enumFormat = "D";
//        private const string _x4Format = "{0:X4}";
//        private const string _d2Format = "D2";
//        private const string _scriptIgnore = "ScriptIgnore";
//        private const string _serializationTypeToken = "__type";

//        private static readonly string[] _dateFormatsUtc = { "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'", "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", "yyyy'-'MM'-'dd'T'HH':'mm'Z'", "yyyyMMdd'T'HH':'mm':'ss'Z'" };
//        private static readonly DateTime _minDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
//        private static readonly long _minDateTimeTicks = _minDateTime.Ticks;

//        public static T Deserialize<T>(string text, JsonOptions options = null) => (T)Deserialize(text, typeof(T), options);//1

//        public static object Deserialize(string text, Type targetType = null, JsonOptions options = null) //2
//        {
//            using (var reader = new StringReader(text))
//            {
//                return Deserialize(reader, targetType, options);
//            }
//        }

//        public static object Deserialize(TextReader reader, Type targetType = null, JsonOptions options = null) //3
//        {
//            if (reader == null)
//                throw new ArgumentNullException(nameof(reader));

//            options = options ?? new JsonOptions();
//            if (targetType == null || targetType == typeof(object))
//                return ReadValue(reader, options);

//            var value = ReadValue(reader, options);
//            if (value == null)
//            {
//                if (targetType.IsValueType)
//                    return CreateInstance(null, targetType, 0, options, value);

//                return null;
//            }

//            return ChangeType(null, value, targetType, options);
//        }

//    }     
//}
