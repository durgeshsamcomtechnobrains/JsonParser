using Microsoft.VisualBasic;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml.Serialization;
using Test9;

namespace test9
{
    public static class Test9JsonParse
    {
        public delegate void JsonCallback(JsonEventArgs e);
        private const string _null = "null";
        private const string _true = "true";
        private const string _false = "false";
        private const string _zeroArg = "{0}";
        private const string _dateStartJs = "new Date(";
        private const string _dateEndJs = ")";
        private const string _dateStart = @"""\/Date(";
        private const string _dateStart2 = @"/Date(";
        private const string _dateEnd = @")\/""";
        private const string _dateEnd2 = @")/";
        private const string _roundTripFormat = "R";
        private const string _enumFormat = "D";
        private const string _x4Format = "{0:X4}";
        private const string _d2Format = "D2";
        private const string _scriptIgnore = "ScriptIgnore";
        private const string _serializationTypeToken = "__type";

        private static readonly string[] _dateFormatsUtc = { "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'", "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'", "yyyy'-'MM'-'dd'T'HH':'mm'Z'", "yyyyMMdd'T'HH':'mm':'ss'Z'" };
        private static readonly DateTime _minDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly long _minDateTimeTicks = _minDateTime.Ticks;        
        
        public static T Deserialize<T>(string text, JsonOptions options = null) => (T)Deserialize(text, typeof(T), options);//1

        public static object Deserialize(string text, Type targetType = null, JsonOptions options = null) //2
        {
            if (text == null)
            {
                if (targetType == null)
                    return null;

                if (!targetType.IsValueType)
                    return null;

                return CreateInstance(null, targetType, 0, options, text);
            }

            using (var reader = new StringReader(text))
            {
                return Deserialize(reader, targetType, options);
            }
        }

        public static object Deserialize(TextReader reader, Type targetType = null, JsonOptions options = null) //3
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            options = options ?? new JsonOptions();
            if (targetType == null || targetType == typeof(object))
                return ReadValue(reader, options);

            var value = ReadValue(reader, options);
            if (value == null)
            {
                if (targetType.IsValueType)
                    return CreateInstance(null, targetType, 0, options, value);

                return null;
            }

            return ChangeType(null, value, targetType, options);
        }

        private static object ReadValue(TextReader reader, JsonOptions options) => ReadValue(reader, options, false, out var _);//5
        
        private static object ReadValue(TextReader reader, JsonOptions options, bool arrayMode, out bool arrayEnd)//6
        {
            arrayEnd = false;
            // 1st chance type is determined by format
            int i;
            do
            {
                i = reader.Peek();
                if (i < 0)
                    return null;

                if (i == 10 || i == 13 || i == 9 || i == 32)
                {
                    reader.Read();
                }
                else
                    break;
            }
            while (true);

            var c = (char)i;
            if (c == '"')
            {
                reader.Read();
                var s = ReadString(reader, options);
                if (options.SerializationOptions.HasFlag(JsonSerializationOptions.AutoParseDateTime))
                {
                    if (TryParseDateTime(s, options.DateTimeStyles, out var dt))
                        return dt;
                }
                return s;
            }

            if (c == '{')
            {
                var dic = ReadDictionary(reader, options);
#if !NET8_0_OR_GREATER
                if (options.SerializationOptions.HasFlag(JsonSerializationOptions.UseISerializable))
                {
                    if (dic.TryGetValue(_serializationTypeToken, out var o))
                    {
                        var typeName = string.Format(CultureInfo.InvariantCulture, "{0}", o);
                        if (!string.IsNullOrEmpty(typeName))
                        {
                            dic.Remove(_serializationTypeToken);
                            return ReadSerializable(reader, options, typeName, dic);
                        }
                    }
                }
#endif
                return dic;
            }

            if (c == '[')
                return ReadArray(reader, options);

            if (c == 'n')
                return ReadNew(reader, options, out arrayEnd);

            // handles the null/true/false cases
            if (char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '+')
                return ReadNumberOrLiteral(reader, options, out arrayEnd);

            if (arrayMode && (c == ']'))
            {
                reader.Read();
                arrayEnd = true;
                return DBNull.Value; // marks array end
            }

            if (arrayMode && (c == ','))
            {
                reader.Read();
                return DBNull.Value; // marks array end
            }

            HandleException(GetUnexpectedCharacterException(GetPosition(reader), c), options);
            return null;
        }

        private static long GetPosition(TextReader reader)
        {
            if (reader == null)
                return -1;

            if (reader is StreamReader sr && sr.BaseStream != null)
            {
                try
                {
                    return sr.BaseStream.Position;
                }
                catch
                {
                    return -1;
                }
            }

            if (reader is StringReader str)
            {
                var fi = typeof(StringReader).GetField("_pos", BindingFlags.Instance | BindingFlags.NonPublic);
                if (fi != null)
                    return (int)fi.GetValue(str);
            }
            return -1;
        }

        private static JsonException GetUnexpectedCharacterException(long pos, char c)
        {
            if (pos < 0)
                return new JsonException("JSO0004: JSON deserialization error detected. Unexpected '" + c + "' character."); ;

            return new JsonException("JSO0005: JSON deserialization error detected at position " + pos + ". Unexpected '" + c + "' character.");
        }

        private static object ReadNumberOrLiteral(TextReader reader, JsonOptions options, out bool arrayEnd)//12
        {
            arrayEnd = false;
            var sb = new StringBuilder();
            do
            {
                var i = reader.Peek();
                if (i < 0)
                    break;

                if (((char)i) == '}')
                    break;

                var c = (char)reader.Read();
                if (char.IsWhiteSpace(c) || c == ',')
                    break;

                if (c == ']')
                {
                    arrayEnd = true;
                    break;
                }

                sb.Append(c);
            }
            while (true);

            var text = sb.ToString();
            if (string.Compare(_null, text, StringComparison.OrdinalIgnoreCase) == 0)
                return null;

            if (string.Compare(_true, text, StringComparison.OrdinalIgnoreCase) == 0)
                return true;

            if (string.Compare(_false, text, StringComparison.OrdinalIgnoreCase) == 0)
                return false;

            if (text.LastIndexOf("e", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d))
                    return d;
            }
            else
            {
                if (text.IndexOf(".", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var de))
                        return de;
                }
                else
                {
                    if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                        return i;

                    if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                        return l;

                    if (decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var de))
                        return de;
                }
            }

            HandleException(GetUnexpectedCharacterException(GetPosition(reader), text[0]), options);
            return null;
        }

        private static object ReadNew(TextReader reader, JsonOptions options, out bool arrayEnd)
        {
            arrayEnd = false;
            var sb = new StringBuilder();
            do
            {
                var i = reader.Peek();
                if (i < 0)
                    break;

                if (((char)i) == '}')
                    break;

                var c = (char)reader.Read();
                if (c == ',')
                    break;

                if (c == ']')
                {
                    arrayEnd = true;
                    break;
                }

                sb.Append(c);
            }
            while (true);

            var text = sb.ToString();
            if (string.Compare(_null, text.Trim(), StringComparison.OrdinalIgnoreCase) == 0)
                return null;

            if (text.StartsWith(_dateStartJs) && text.EndsWith(_dateEndJs))
            {
                if (long.TryParse(text.Substring(_dateStartJs.Length, text.Length - _dateStartJs.Length - _dateEndJs.Length), out var l))
                    return new DateTime((l * 10000) + _minDateTimeTicks, DateTimeKind.Utc);
            }

            HandleException(GetUnexpectedCharacterException(GetPosition(reader), text[0]), options);
            return null;
        }
        private static object[] ReadArray(TextReader reader, JsonOptions options)//7
        {
            if (!ReadWhitespaces(reader))
                return null;

            reader.Read();
            var list = new List<object>();
            do
            {
                var value = ReadValue(reader, options, true, out var arrayEnd);
                if (!Convert.IsDBNull(value))
                {
                    list.Add(value);
                }
                if (arrayEnd)
                    return list.ToArray();

                if (reader.Peek() < 0)
                {
                    HandleException(GetExpectedCharacterException(GetPosition(reader), ']'), options);
                    return list.ToArray();
                }

            }
            while (true);
        }
        private static JsonException GetExpectedCharacterException(long pos, char c)
        {
            if (pos < 0)
                return new JsonException("JSO0002: JSON deserialization error detected. Expecting '" + c + "' character."); ;

            return new JsonException("JSO0003: JSON deserialization error detected at position " + pos + ". Expecting '" + c + "' character.");
        }
        private static bool ReadWhitespaces(TextReader reader) => ReadWhile(reader, char.IsWhiteSpace);//8
        private static bool ReadWhile(TextReader reader, Predicate<char> cont)//9
        {
            do
            {
                var i = reader.Peek();
                if (i < 0)
                    return false;

                if (!cont((char)i))
                    return true;

                reader.Read();
            }
            while (true);
        }
        private static JsonException GetEofException(char c) => new JsonException("JSO0012: JSON deserialization error detected at end of text. Expecting '" + c + "' character.");

        private static Dictionary<string, object> ReadDictionary(TextReader reader, JsonOptions options)//9
        {
            if (!ReadWhitespaces(reader))
                return null;

            reader.Read();
            var dictionary = new Dictionary<string, object>();
            do
            {
                var i = reader.Peek();
                if (i < 0)
                {
                    HandleException(GetEofException('}'), options);
                    return dictionary;
                }

                var c = (char)reader.Read();
                switch (c)
                {
                    case '}':
                        return dictionary;

                    case '"':
                        var text = ReadString(reader, options);
                        if (!ReadWhitespaces(reader))
                        {
                            HandleException(GetExpectedCharacterException(GetPosition(reader), ':'), options);
                            return dictionary;
                        }

                        c = (char)reader.Peek();
                        if (c != ':')
                        {
                            HandleException(GetExpectedCharacterException(GetPosition(reader), ':'), options);
                            return dictionary;
                        }

                        reader.Read();
                        dictionary[text] = ReadValue(reader, options);
                        break;

                    case ',':
                        break;

                    case '\r':
                    case '\n':
                    case '\t':
                    case ' ':
                        break;

                    default:
                        HandleException(GetUnexpectedCharacterException(GetPosition(reader), c), options);
                        return dictionary;
                }
            }
            while (true);
        }

        private static string ReadString(TextReader reader, JsonOptions options)//10
        {
            var sb = new StringBuilder();
            do
            {
                var i = reader.Peek();
                if (i < 0)
                {
                    HandleException(GetEofException('"'), options);
                    return null;
                }

                var c = (char)reader.Read();
                if (c == '"')
                    break;

                if (c == '\\')
                {
                    i = reader.Peek();
                    if (i < 0)
                    {
                        HandleException(GetEofException('"'), options);
                        return null;
                    }

                    var next = (char)reader.Read();
                    switch (next)
                    {
                        case 'b':
                            sb.Append('\b');
                            break;

                        case 't':
                            sb.Append('\t');
                            break;

                        case 'n':
                            sb.Append('\n');
                            break;

                        case 'f':
                            sb.Append('\f');
                            break;

                        case 'r':
                            sb.Append('\r');
                            break;

                        case '/':
                        case '\\':
                        case '"':
                            sb.Append(next);
                            break;

                        case 'u': // unicode
                            var us = ReadX4(reader, options);
                            sb.Append((char)us);
                            break;

                        default:
                            sb.Append(c);
                            sb.Append(next);
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            while (true);
            return sb.ToString();
        }

        private static ushort ReadX4(TextReader reader, JsonOptions options)
        {
            var u = 0;
            for (var i = 0; i < 4; i++)
            {
                u *= 16;
                if (reader.Peek() < 0)
                {
                    HandleException(new JsonException("JSO0008: JSON deserialization error detected at end of stream. Expecting hexadecimal character."), options);
                    return 0;
                }

                u += GetHexValue(reader, (char)reader.Read(), options);
            }
            return (ushort)u;
        }

        private static JsonException GetExpectedHexaCharacterException(long pos)
        {
            if (pos < 0)
                return new JsonException("JSO0006: JSON deserialization error detected. Expecting hexadecimal character."); ;

            return new JsonException("JSO0007: JSON deserialization error detected at position " + pos + ". Expecting hexadecimal character.");
        }
        private static byte GetHexValue(TextReader reader, char c, JsonOptions options)
        {
            c = char.ToLower(c);
            if (c < '0')
            {
                HandleException(GetExpectedHexaCharacterException(GetPosition(reader)), options);
                return 0;
            }

            if (c <= '9')
                return (byte)(c - '0');

            if (c < 'a')
            {
                HandleException(GetExpectedHexaCharacterException(GetPosition(reader)), options);
                return 0;
            }

            if (c <= 'f')
                return (byte)(c - 'a' + 10);

            HandleException(GetExpectedHexaCharacterException(GetPosition(reader)), options);
            return 0;
        }

        private static object CreateInstance(object target, Type type, int elementsCount, JsonOptions options, object value)
        {
            try
            {
                if (options.CreateInstanceCallback != null)
                {
                    var og = new Dictionary<object, object>
                    {
                        ["elementsCount"] = elementsCount,
                        ["value"] = value
                    };

                    var e = new JsonEventArgs(null, type, og, options, null, target)
                    {
                        EventType = JsonEventType.CreateInstance
                    };
                    options.CreateInstanceCallback(e);
                    if (e.Handled)
                        return e.Value;
                }

                if (type.IsArray)
                {
                    var elementType = type.GetElementType();
                    return Array.CreateInstance(elementType, elementsCount);
                }

                if (type.IsInterface)
                {
                    var elementType = GetGenericListElementType(type);
                    if (elementType != null)
                        return Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

                    var elementTypes = GetGenericDictionaryElementType(type);
                    if (elementTypes != null)
                        return Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(elementTypes));

                    elementType = GetListElementType(type);
                    if (elementType != null)
                        return new List<object>(elementsCount);
                }
                return Activator.CreateInstance(type);
            }
            catch (Exception e)
            {
                HandleException(new JsonException("JSO0001: JSON error detected. Cannot create an instance of the '" + type.Name + "' type.", e), options);
                return null;
            }
        }

        private static void HandleException(Exception ex, JsonOptions options)
        {
            if (options != null && !options.ThrowExceptions)
            {
                options.AddException(ex);
                return;
            }
            throw ex;
        }

        private static Type GetListElementType(Type type)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;

                if (iface.GetGenericTypeDefinition() == typeof(IList))
                    return iface.GetGenericArguments()[0];
            }
            return null;
        }

        private static Type GetGenericListElementType(Type type)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;

                if (iface.GetGenericTypeDefinition() == typeof(IList<>))
                    return iface.GetGenericArguments()[0];

                if (iface.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
                    return iface.GetGenericArguments()[0];

                if (iface.GetGenericTypeDefinition() == typeof(ICollection<>))
                    return iface.GetGenericArguments()[0];

                if (iface.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>))
                    return iface.GetGenericArguments()[0];
            }
            return null;
        }

        private static Type[] GetGenericDictionaryElementType(Type type)
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;

                if (iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    return iface.GetGenericArguments();

                if (iface.GetGenericTypeDefinition() == typeof(IReadOnlyDictionary<,>))
                    return iface.GetGenericArguments();
            }
            return null;
        }
       
        private static void Apply(IDictionary dictionary, object target, JsonOptions options)
        {
            if (dictionary == null || target == null)
                return;

            if (target is IDictionary dicTarget)
            {
                var itemType = GetItemType(dicTarget.GetType());
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key == null)
                        continue;

                    if (itemType == typeof(object))
                    {
                        dicTarget[entry.Key] = entry.Value;
                    }
                    else
                    {
                        dicTarget[entry.Key] = ChangeType(target, entry.Value, itemType, options);
                    }
                }
                return;
            }

           // var def = TypeDef.Get(target.GetType(), options);

            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key == null)
                    continue;

                var entryKey = string.Format(CultureInfo.InvariantCulture, "{0}", entry.Key);
                var entryValue = entry.Value;
                if (options.MapEntryCallback != null)
                {
                    var og = new Dictionary<object, object>
                    {
                        ["dictionary"] = dictionary
                    };

                    var e = new JsonEventArgs(null, entryValue, og, options, entryKey, target)
                    {
                        EventType = JsonEventType.MapEntry
                    };
                    options.MapEntryCallback(e);
                    if (e.Handled)
                        continue;

                    entryKey = e.Name;
                    entryValue = e.Value;
                }

                //def.ApplyEntry(dictionary, target, entryKey, entryValue, options);
            }
        }

        public static object ChangeType(object target, object value, Type conversionType, JsonOptions options = null)
        {
            if (conversionType == null)
                throw new ArgumentNullException(nameof(conversionType));

            if (conversionType == typeof(object))
                return value;

            options = options ?? new JsonOptions();
            if (!(value is string))
            {
                if (conversionType.IsArray)
                {
                    if (value is IEnumerable en)
                    {
                        var elementType = conversionType.GetElementType();
                        var list = new List<object>();
                        foreach (var obj in en)
                        {
                            list.Add(ChangeType(target, obj, elementType, options));
                        }

                        var array = Array.CreateInstance(elementType, list.Count);
                        if (array != null)
                        {
                            Array.Copy(list.ToArray(), array, list.Count);
                        }
                        return array;
                    }
                }

                var lo = GetListObject(conversionType, options, target, value, null, null);
                if (lo != null)
                {
                    if (value is IEnumerable en)
                    {
                        lo.List = CreateInstance(target, conversionType, en is ICollection coll ? coll.Count : 0, options, value);
                        ApplyToListTarget(target, en, lo, options);
                        return lo.List;
                    }
                }
            }

            if (value is IDictionary dic)
            {
                var instance = CreateInstance(target, conversionType, 0, options, value);
                if (instance != null)
                {
                    Apply(dic, instance, options);
                }
                return instance;
            }

            if (conversionType == typeof(byte[]) && value is string str)
            {
                if (options.SerializationOptions.HasFlag(JsonSerializationOptions.ByteArrayAsBase64))
                {
                    try
                    {
                        return Convert.FromBase64String(str);
                    }
                    catch (Exception e)
                    {
                        HandleException(new JsonException("JSO0013: JSON deserialization error with a base64 array as string.", e), options);
                        return null;
                    }
                }
            }

            if (conversionType == typeof(DateTime))
            {
                if (value is DateTime)
                    return value;

                var svalue = string.Format(CultureInfo.InvariantCulture, "{0}", value);
                if (!string.IsNullOrEmpty(svalue))
                {
                    if (TryParseDateTime(svalue, options.DateTimeStyles, out var dt))
                        return dt;
                }
            }

            if (conversionType == typeof(TimeSpan))
            {
                var svalue = string.Format(CultureInfo.InvariantCulture, "{0}", value);
                if (!string.IsNullOrEmpty(svalue))
                {
                    if (long.TryParse(svalue, out var ticks))
                        return new TimeSpan(ticks);
                }
            }

            return Conversions.ChangeType(value, conversionType, null, null);
        }

        private static ListObject GetListObject(Type type, JsonOptions options, object target, object value, IDictionary dictionary, string key)
        {
            if (options.GetListObjectCallback != null)
            {
                var og = new Dictionary<object, object>
                {
                    ["dictionary"] = dictionary,
                    ["type"] = type
                };

                var e = new JsonEventArgs(null, value, og, options, key, target)
                {
                    EventType = JsonEventType.GetListObject
                };
                options.GetListObjectCallback(e);
                if (e.Handled)
                {
                    og.TryGetValue("type", out var outType);
                    return outType as ListObject;
                }
            }

            if (type == typeof(byte[]))
                return null;

            if (typeof(IList).IsAssignableFrom(type))
                return new IListObject(); // also handles arrays

            if (type.IsGenericType)
            {
                if (type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                    type.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>))
                    return (ListObject)Activator.CreateInstance(typeof(ICollectionTObject<>).MakeGenericType(type.GetGenericArguments()[0]));
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;

                if (iface.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                    iface.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>))
                    return (ListObject)Activator.CreateInstance(typeof(ICollectionTObject<>).MakeGenericType(iface.GetGenericArguments()[0]));
            }
            return null;
        }

        public static object ChangeType(object input, Type conversionType, object defaultValue = null, IFormatProvider provider = null)
        {
            if (!TryChangeType(input, conversionType, provider, out var value))
            {
                if (TryChangeType(defaultValue, conversionType, provider, out var def))
                    return def;

                if (IsReallyValueType(conversionType))
                    return Activator.CreateInstance(conversionType);

                return null;
            }

            return value;
        }        

        public static bool TryChangeType(object input, Type conversionType, IFormatProvider provider, out object value)
        {
            if (conversionType == null)
                throw new ArgumentNullException(nameof(conversionType));

            if (conversionType == typeof(object))
            {
                value = input;
                return true;
            }

            if (IsNullable(conversionType))
            {
                if (input == null)
                {
                    value = null;
                    return true;
                }

                var type = conversionType.GetGenericArguments()[0];
                if (TryChangeType(input, type, provider, out var vtValue))
                {
                    var nt = typeof(Nullable<>).MakeGenericType(type);
                    value = Activator.CreateInstance(nt, vtValue);
                    return true;
                }

                value = null;
                return false;
            }

            value = IsReallyValueType(conversionType) ? Activator.CreateInstance(conversionType) : null;
            if (input == null)
                return !IsReallyValueType(conversionType);

            var inputType = input.GetType();
            if (conversionType.IsAssignableFrom(inputType))
            {
                value = input;
                return true;
            }           

            if (inputType.IsEnum)
            {
                var tc = Type.GetTypeCode(inputType);
                if (conversionType == typeof(int))
                {
                    switch (tc)
                    {
                        case TypeCode.Int32:
                            value = (int)input;
                            return true;

                        case TypeCode.Int16:
                            value = (int)(short)input;
                            return true;

                        case TypeCode.Int64:
                            value = (int)(long)input;
                            return true;

                        case TypeCode.UInt32:
                            value = (int)(uint)input;
                            return true;

                        case TypeCode.UInt16:
                            value = (int)(ushort)input;
                            return true;

                        case TypeCode.UInt64:
                            value = (int)(ulong)input;
                            return true;

                        case TypeCode.Byte:
                            value = (int)(byte)input;
                            return true;

                        case TypeCode.SByte:
                            value = (int)(sbyte)input;
                            return true;
                    }
                    return false;
                }

                if (conversionType == typeof(short))
                {
                    switch (tc)
                    {
                        case TypeCode.Int32:
                            value = (short)(int)input;
                            return true;

                        case TypeCode.Int16:
                            value = (short)input;
                            return true;

                        case TypeCode.Int64:
                            value = (short)(long)input;
                            return true;

                        case TypeCode.UInt32:
                            value = (short)(uint)input;
                            return true;

                        case TypeCode.UInt16:
                            value = (short)(ushort)input;
                            return true;

                        case TypeCode.UInt64:
                            value = (short)(ulong)input;
                            return true;

                        case TypeCode.Byte:
                            value = (short)(byte)input;
                            return true;

                        case TypeCode.SByte:
                            value = (short)(sbyte)input;
                            return true;
                    }
                    return false;
                }

                if (conversionType == typeof(long))
                {
                    switch (tc)
                    {
                        case TypeCode.Int32:
                            value = (long)(int)input;
                            return true;

                        case TypeCode.Int16:
                            value = (long)(short)input;
                            return true;

                        case TypeCode.Int64:
                            value = (long)input;
                            return true;

                        case TypeCode.UInt32:
                            value = (long)(uint)input;
                            return true;

                        case TypeCode.UInt16:
                            value = (long)(ushort)input;
                            return true;

                        case TypeCode.UInt64:
                            value = (long)(ulong)input;
                            return true;

                        case TypeCode.Byte:
                            value = (long)(byte)input;
                            return true;

                        case TypeCode.SByte:
                            value = (long)(sbyte)input;
                            return true;
                    }
                    return false;
                }

                if (conversionType == typeof(uint))
                {
                    switch (tc)
                    {
                        case TypeCode.Int32:
                            value = (uint)(int)input;
                            return true;

                        case TypeCode.Int16:
                            value = (uint)(short)input;
                            return true;

                        case TypeCode.Int64:
                            value = (uint)(long)input;
                            return true;

                        case TypeCode.UInt32:
                            value = (uint)input;
                            return true;

                        case TypeCode.UInt16:
                            value = (uint)(ushort)input;
                            return true;

                        case TypeCode.UInt64:
                            value = (uint)(ulong)input;
                            return true;

                        case TypeCode.Byte:
                            value = (uint)(byte)input;
                            return true;

                        case TypeCode.SByte:
                            value = (uint)(sbyte)input;
                            return true;
                    }
                    return false;
                }

                if (conversionType == typeof(ushort))
                {
                    switch (tc)
                    {
                        case TypeCode.Int32:
                            value = (ushort)(int)input;
                            return true;

                        case TypeCode.Int16:
                            value = (ushort)(short)input;
                            return true;

                        case TypeCode.Int64:
                            value = (ushort)(long)input;
                            return true;

                        case TypeCode.UInt32:
                            value = (ushort)(uint)input;
                            return true;

                        case TypeCode.UInt16:
                            value = (ushort)input;
                            return true;

                        case TypeCode.UInt64:
                            value = (ushort)(ulong)input;
                            return true;

                        case TypeCode.Byte:
                            value = (ushort)(byte)input;
                            return true;

                        case TypeCode.SByte:
                            value = (ushort)(sbyte)input;
                            return true;
                    }
                    return false;
                }

                if (conversionType == typeof(ulong))
                {
                    switch (tc)
                    {
                        case TypeCode.Int32:
                            value = (ulong)(int)input;
                            return true;

                        case TypeCode.Int16:
                            value = (ulong)(short)input;
                            return true;

                        case TypeCode.Int64:
                            value = (ulong)(long)input;
                            return true;

                        case TypeCode.UInt32:
                            value = (ulong)(uint)input;
                            return true;

                        case TypeCode.UInt16:
                            value = (ulong)(ushort)input;
                            return true;

                        case TypeCode.UInt64:
                            value = (ulong)input;
                            return true;

                        case TypeCode.Byte:
                            value = (ulong)(byte)input;
                            return true;

                        case TypeCode.SByte:
                            value = (ulong)(sbyte)input;
                            return true;
                    }
                    return false;
                }

                if (conversionType == typeof(byte))
                {
                    switch (tc)
                    {
                        case TypeCode.Int32:
                            value = (byte)(int)input;
                            return true;

                        case TypeCode.Int16:
                            value = (byte)(short)input;
                            return true;

                        case TypeCode.Int64:
                            value = (byte)(long)input;
                            return true;

                        case TypeCode.UInt32:
                            value = (byte)(uint)input;
                            return true;

                        case TypeCode.UInt16:
                            value = (byte)(ushort)input;
                            return true;

                        case TypeCode.UInt64:
                            value = (byte)(ulong)input;
                            return true;

                        case TypeCode.Byte:
                            value = (byte)input;
                            return true;

                        case TypeCode.SByte:
                            value = (byte)(sbyte)input;
                            return true;
                    }
                    return false;
                }

                if (conversionType == typeof(sbyte))
                {
                    switch (tc)
                    {
                        case TypeCode.Int32:
                            value = (sbyte)(int)input;
                            return true;

                        case TypeCode.Int16:
                            value = (sbyte)(short)input;
                            return true;

                        case TypeCode.Int64:
                            value = (sbyte)(long)input;
                            return true;

                        case TypeCode.UInt32:
                            value = (sbyte)(uint)input;
                            return true;

                        case TypeCode.UInt16:
                            value = (sbyte)(ushort)input;
                            return true;

                        case TypeCode.UInt64:
                            value = (sbyte)(ulong)input;
                            return true;

                        case TypeCode.Byte:
                            value = (sbyte)(byte)input;
                            return true;

                        case TypeCode.SByte:
                            value = (sbyte)input;
                            return true;
                    }
                    return false;
                }
            }

            if (conversionType == typeof(Guid))
            {
                var svalue = string.Format(provider, "{0}", input);
                if (svalue != null && Guid.TryParse(svalue, out var guid))
                {
                    value = guid;
                    return true;
                }
                return false;
            }

            if (conversionType == typeof(Uri))
            {
                var svalue = string.Format(provider, "{0}", input);
                if (svalue != null && Uri.TryCreate(svalue, UriKind.RelativeOrAbsolute, out var uri))
                {
                    value = uri;
                    return true;
                }
                return false;
            }

            if (conversionType == typeof(IntPtr))
            {
                if (IntPtr.Size == 8)
                {
                    
                }                
                return false;
            }

            if (conversionType == typeof(int))
            {
                if (inputType == typeof(uint))
                {
                    value = unchecked((int)(uint)input);
                    return true;
                }

                if (inputType == typeof(ulong))
                {
                    value = unchecked((int)(ulong)input);
                    return true;
                }

                if (inputType == typeof(ushort))
                {
                    value = unchecked((int)(ushort)input);
                    return true;
                }

                if (inputType == typeof(byte))
                {
                    value = unchecked((int)(byte)input);
                    return true;
                }
            }

            if (conversionType == typeof(long))
            {
                if (inputType == typeof(uint))
                {
                    value = unchecked((long)(uint)input);
                    return true;
                }

                if (inputType == typeof(ulong))
                {
                    value = unchecked((long)(ulong)input);
                    return true;
                }

                if (inputType == typeof(ushort))
                {
                    value = unchecked((long)(ushort)input);
                    return true;
                }

                if (inputType == typeof(byte))
                {
                    value = unchecked((long)(byte)input);
                    return true;
                }

                if (inputType == typeof(TimeSpan))
                {
                    value = ((TimeSpan)input).Ticks;
                    return true;
                }
            }

            if (conversionType == typeof(short))
            {
                if (inputType == typeof(uint))
                {
                    value = unchecked((short)(uint)input);
                    return true;
                }

                if (inputType == typeof(ulong))
                {
                    value = unchecked((short)(ulong)input);
                    return true;
                }

                if (inputType == typeof(ushort))
                {
                    value = unchecked((short)(ushort)input);
                    return true;
                }

                if (inputType == typeof(byte))
                {
                    value = unchecked((short)(byte)input);
                    return true;
                }
            }

            if (conversionType == typeof(sbyte))
            {
                if (inputType == typeof(uint))
                {
                    value = unchecked((sbyte)(uint)input);
                    return true;
                }

                if (inputType == typeof(ulong))
                {
                    value = unchecked((sbyte)(ulong)input);
                    return true;
                }

                if (inputType == typeof(ushort))
                {
                    value = unchecked((sbyte)(ushort)input);
                    return true;
                }

                if (inputType == typeof(byte))
                {
                    value = unchecked((sbyte)(byte)input);
                    return true;
                }
            }

            if (conversionType == typeof(uint))
            {
                if (inputType == typeof(int))
                {
                    value = unchecked((uint)(int)input);
                    return true;
                }

                if (inputType == typeof(long))
                {
                    value = unchecked((uint)(long)input);
                    return true;
                }

                if (inputType == typeof(short))
                {
                    value = unchecked((uint)(short)input);
                    return true;
                }

                if (inputType == typeof(sbyte))
                {
                    value = unchecked((uint)(sbyte)input);
                    return true;
                }
            }

            if (conversionType == typeof(ulong))
            {
                if (inputType == typeof(int))
                {
                    value = unchecked((ulong)(int)input);
                    return true;
                }

                if (inputType == typeof(long))
                {
                    value = unchecked((ulong)(long)input);
                    return true;
                }

                if (inputType == typeof(short))
                {
                    value = unchecked((ulong)(short)input);
                    return true;
                }

                if (inputType == typeof(sbyte))
                {
                    value = unchecked((ulong)(sbyte)input);
                    return true;
                }
            }

            if (conversionType == typeof(ushort))
            {
                if (inputType == typeof(int))
                {
                    value = unchecked((ushort)(int)input);
                    return true;
                }

                if (inputType == typeof(long))
                {
                    value = unchecked((ushort)(long)input);
                    return true;
                }

                if (inputType == typeof(short))
                {
                    value = unchecked((ushort)(short)input);
                    return true;
                }

                if (inputType == typeof(sbyte))
                {
                    value = unchecked((ushort)(sbyte)input);
                    return true;
                }
            }

            if (conversionType == typeof(byte))
            {
                if (inputType == typeof(int))
                {
                    value = unchecked((byte)(int)input);
                    return true;
                }

                if (inputType == typeof(long))
                {
                    value = unchecked((byte)(long)input);
                    return true;
                }

                if (inputType == typeof(short))
                {
                    value = unchecked((byte)(short)input);
                    return true;
                }

                if (inputType == typeof(sbyte))
                {
                    value = unchecked((byte)(sbyte)input);
                    return true;
                }
            }

            if (conversionType == typeof(DateTime))
            {
                if (inputType == typeof(long))
                {
                    value = new DateTime((long)input, DateTimeKind.Utc);
                    return true;
                }

                if (inputType == typeof(DateTimeOffset))
                {
                    value = ((DateTimeOffset)input).DateTime;
                    return true;
                }
            }

            if (conversionType == typeof(DateTimeOffset))
            {
                if (inputType == typeof(long))
                {
                    value = new DateTimeOffset(new DateTime((long)input, DateTimeKind.Utc));
                    return true;
                }

                if (inputType == typeof(DateTime))
                {
                    var dt = (DateTime)input;
                }
            }

            if (conversionType == typeof(TimeSpan))
            {
                if (inputType == typeof(long))
                {
                    value = new TimeSpan((long)input);
                    return true;
                }

                if (inputType == typeof(DateTime))
                {
                    value = ((DateTime)value).TimeOfDay;
                    return true;
                }

                if (inputType == typeof(DateTimeOffset))
                {
                    value = ((DateTimeOffset)value).TimeOfDay;
                    return true;
                }               
            }

            var isGenericList = IsGenericList(conversionType, out var elementType);
            if (conversionType.IsArray || isGenericList)
            {
                if (input is IEnumerable enumerable)
                {
                    if (!isGenericList)
                    {
                        elementType = conversionType.GetElementType();
                    }

                    var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                    var count = 0;
                    foreach (var obj in enumerable)
                    {
                        count++;
                        if (TryChangeType(obj, elementType, provider, out var element))
                        {
                            list.Add(element);
                        }
                    }

                    // at least one was converted
                    if (count > 0 && list.Count > 0)
                    {
                        if (isGenericList)
                        {
                            value = list;
                        }
                        else
                        {
                            value = list.GetType().GetMethod(nameof(List<object>.ToArray)).Invoke(list, null);
                        }
                        return true;
                    }
                }
            }

            if (conversionType == typeof(CultureInfo) || conversionType == typeof(IFormatProvider))
            {
                try
                {
                    if (input is int lcid)
                    {
                        value = CultureInfo.GetCultureInfo(lcid);
                        return true;
                    }
                    else
                    {
                        var si = input?.ToString();
                        if (si != null)
                        {
                            if (int.TryParse(si, out lcid))
                            {
                                value = CultureInfo.GetCultureInfo(lcid);
                                return true;
                            }

                            value = CultureInfo.GetCultureInfo(si);
                            return true;
                        }
                    }
                }
                catch
                {
                    // do nothing, wrong culture, etc.
                }
                return false;
            }

            if (conversionType == typeof(bool))
            {
                if (true.Equals(input))
                {
                    value = true;
                    return true;
                }

                if (false.Equals(input))
                {
                    value = false;
                    return true;
                }

                var svalue = string.Format(provider, "{0}", input);
                if (svalue == null)
                    return false;

                if (bool.TryParse(svalue, out var b))
                {
                    value = b;
                    return true;
                }               
                return false;
            }

            // in general, nothing is convertible to anything but one of these, IConvertible is 100% stupid thing
            bool isWellKnownConvertible()
            {
                return conversionType == typeof(short) || conversionType == typeof(int) ||
                    conversionType == typeof(string) || conversionType == typeof(byte) ||
                    conversionType == typeof(char) || conversionType == typeof(DateTime) ||
                    conversionType == typeof(DBNull) || conversionType == typeof(decimal) ||
                    conversionType == typeof(double) || conversionType.IsEnum ||
                    conversionType == typeof(short) || conversionType == typeof(int) ||
                    conversionType == typeof(long) || conversionType == typeof(sbyte) ||
                    conversionType == typeof(bool) || conversionType == typeof(float) ||
                    conversionType == typeof(ushort) || conversionType == typeof(uint) ||
                    conversionType == typeof(ulong);
            }

            if (isWellKnownConvertible() && input is IConvertible convertible)
            {
                try
                {
                    value = convertible.ToType(conversionType, provider);
                    if (value is DateTime dt && !IsValid(dt))
                        return false;

                    return true;
                }
                catch
                {
                    // continue;
                }
            }

            if (input != null)
            {
                var inputConverter = TypeDescriptor.GetConverter(input);
                if (inputConverter != null)
                {
                    if (inputConverter.CanConvertTo(conversionType))
                    {
                        try
                        {
                            value = inputConverter.ConvertTo(null, provider as CultureInfo, input, conversionType);
                            return true;
                        }
                        catch
                        {
                            // continue;
                        }
                    }
                }
            }

            var converter = TypeDescriptor.GetConverter(conversionType);
            if (converter != null)
            {
                if (converter.CanConvertTo(conversionType))
                {
                    try
                    {
                        value = converter.ConvertTo(null, provider as CultureInfo, input, conversionType);
                        return true;
                    }
                    catch
                    {
                        // continue;
                    }
                }

                if (converter.CanConvertFrom(inputType))
                {
                    try
                    {
                        value = converter.ConvertFrom(null, provider as CultureInfo, input);
                        return true;
                    }
                    catch
                    {
                        // continue;
                    }
                }
            }

            if (conversionType == typeof(string))
            {
                value = string.Format(provider, "{0}", input);
                return true;
            }

            return false;
        }

        private static bool IsValid(DateTime dt) => dt != DateTime.MinValue && dt != DateTime.MaxValue && dt.Kind != DateTimeKind.Unspecified;        

        public static bool IsGenericList(Type type, out Type elementType)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            elementType = null;
            return false;
        }

        private static bool IsReallyValueType(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.IsValueType && !IsNullable(type);
        }
        public static bool IsNullable(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static void ApplyToListTarget(object target, IEnumerable input, ListObject list, JsonOptions options)
        {
            if (list.List == null)
                return;

            if (list.Context != null)
            {
                list.Context["action"] = "init";
                list.Context["target"] = target;
                list.Context["input"] = input;
                list.Context["options"] = options;
            }

            if (input != null)
            {
                var array = list.List as Array;
                var max = 0;
                var i = 0;
                if (array != null)
                {
                    i = array.GetLowerBound(0);
                    max = array.GetUpperBound(0);
                }

                var itemType = GetItemType(list.List.GetType());
                foreach (var value in input)
                {
                    if (array != null)
                    {
                        if ((i - 1) == max)
                            break;

                        array.SetValue(ChangeType(target, value, itemType, options), i++);
                    }
                    else
                    {
                        var cvalue = ChangeType(target, value, itemType, options);
                        if (list.Context != null)
                        {
                            list.Context["action"] = "add";
                            list.Context["itemType"] = itemType;
                            list.Context["value"] = value;
                            list.Context["cvalue"] = cvalue;

                            if (!list.Context.TryGetValue("cvalue", out var newcvalue))
                                continue;

                            cvalue = newcvalue;
                        }

                        list.Add(cvalue, options);
                    }
                }
            }
            else
            {
                if (list.Context != null)
                {
                    list.Context["action"] = "clear";
                }
                list.Clear();
            }

            if (list.Context != null)
            {
                list.Context.Clear();
            }
        }

        public static Type GetItemType(Type collectionType)
        {
            if (collectionType == null)
                throw new ArgumentNullException(nameof(collectionType));

            foreach (var iface in collectionType.GetInterfaces())
            {
                if (!iface.IsGenericType)
                    continue;

                if (iface.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                    return iface.GetGenericArguments()[1];

                if (iface.GetGenericTypeDefinition() == typeof(IList<>))
                    return iface.GetGenericArguments()[0];

                if (iface.GetGenericTypeDefinition() == typeof(IReadOnlyList<>))
                    return iface.GetGenericArguments()[0];

                if (iface.GetGenericTypeDefinition() == typeof(ICollection<>))
                    return iface.GetGenericArguments()[0];

                if (iface.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>))
                    return iface.GetGenericArguments()[0];

                if (iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return iface.GetGenericArguments()[0];
            }
            return typeof(object);
        }
   

        public static bool TryParseDateTime(string text, DateTimeStyles styles, out DateTime dt)//12
        {
            dt = DateTime.MinValue;
            if (text == null)
                return false;

            if (text.Length > 2)
            {
                if (text[0] == '"' && text[text.Length - 1] == '"')
                {
                    using (var reader = new StringReader(text))
                    {
                        reader.Read(); // skip "
                        var options = new JsonOptions
                        {
                            ThrowExceptions = false
                        };
                        text = ReadString(reader, options);
                    }
                }
            }

            if (text.EndsWith("Z", StringComparison.OrdinalIgnoreCase))
            {
                if (DateTime.TryParseExact(text, _dateFormatsUtc, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out dt))
                    return true;
            }

            var offsetHours = 0;
            var offsetMinutes = 0;
            var kind = DateTimeKind.Utc;
            const int len = 19;

            // s format length is 19, as in '2012-02-21T17:07:14'
            // so we can do quick checks
            // this portion of code is needed because we assume UTC and the default DateTime parse behavior is not that (even with AssumeUniversal)
            if (text.Length >= len &&
                text[4] == '-' &&
                text[7] == '-' &&
                (text[10] == 'T' || text[10] == 't') &&
                text[13] == ':' &&
                text[16] == ':')
            {
                if (DateTime.TryParseExact(text, "o", null, DateTimeStyles.AssumeUniversal, out dt))
                    return true;

                var tz = text.Substring(len).IndexOfAny(new[] { '+', '-' });
                var text2 = text;
                if (tz >= 0)
                {
                    tz += len;
                    var offset = text.Substring(tz + 1).Trim();
                    if (int.TryParse(offset, out int i))
                    {
                        kind = DateTimeKind.Local;
                        offsetHours = i / 100;
                        offsetMinutes = i % 100;
                        if (text[tz] == '-')
                        {
                            offsetHours = -offsetHours;
                            offsetMinutes = -offsetMinutes;
                        }
                        text2 = text.Substring(0, tz);
                    }
                }

                if (tz >= 0)
                {
                    if (DateTime.TryParseExact(text2, "s", null, DateTimeStyles.AssumeLocal, out dt))
                    {
                        if (offsetHours != 0)
                        {
                            dt = dt.AddHours(offsetHours);
                        }

                        if (offsetMinutes != 0)
                        {
                            dt = dt.AddMinutes(offsetMinutes);
                        }
                        return true;
                    }
                }
                else
                {
                    if (DateTime.TryParseExact(text, "s", null, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out dt))
                        return true;
                }
            }

            // 01234567890123456
            // 20150525T15:50:00
            if (text != null && text.Length == 17)
            {
                if ((text[8] == 'T' || text[8] == 't') && text[11] == ':' && text[14] == ':')
                {
                    _ = int.TryParse(text.Substring(0, 4), out var year);
                    _ = int.TryParse(text.Substring(4, 2), out var month);
                    _ = int.TryParse(text.Substring(6, 2), out var day);
                    _ = int.TryParse(text.Substring(9, 2), out var hour);
                    _ = int.TryParse(text.Substring(12, 2), out var minute);
                    _ = int.TryParse(text.Substring(15, 2), out var second);
                    if (month > 0 && month < 13 &&
                        day > 0 && day < 32 &&
                        year >= 0 &&
                        hour >= 0 && hour < 24 &&
                        minute >= 0 && minute < 60 &&
                        second >= 0 && second < 60)
                    {
                        try
                        {
                            dt = new DateTime(year, month, day, hour, minute, second);
                            return true;
                        }
                        catch
                        {
                            // do nothing
                        }
                    }
                }
            }

            // read this http://weblogs.asp.net/bleroy/archive/2008/01/18/dates-and-json.aspx
            string ticks = null;
            if (text.StartsWith(_dateStartJs) && text.EndsWith(_dateEndJs))
            {
                ticks = text.Substring(_dateStartJs.Length, text.Length - _dateStartJs.Length - _dateEndJs.Length).Trim();
            }
            else if (text.StartsWith(_dateStart2, StringComparison.OrdinalIgnoreCase) && text.EndsWith(_dateEnd2, StringComparison.OrdinalIgnoreCase))
            {
                ticks = text.Substring(_dateStart2.Length, text.Length - _dateEnd2.Length - _dateStart2.Length).Trim();
            }

            if (!string.IsNullOrEmpty(ticks))
            {
                var startIndex = (ticks[0] == '-') || (ticks[0] == '+') ? 1 : 0;
                var pos = ticks.IndexOfAny(new[] { '+', '-' }, startIndex);
                if (pos >= 0)
                {
                    var neg = ticks[pos] == '-';
                    var offset = ticks.Substring(pos + 1).Trim();
                    ticks = ticks.Substring(0, pos).Trim();
                    if (int.TryParse(offset, out var i))
                    {
                        kind = DateTimeKind.Local;
                        offsetHours = i / 100;
                        offsetMinutes = i % 100;
                        if (neg)
                        {
                            offsetHours = -offsetHours;
                            offsetMinutes = -offsetMinutes;
                        }
                    }
                }

                if (long.TryParse(ticks, NumberStyles.Number, CultureInfo.InvariantCulture, out var l))
                {
                    dt = new DateTime((l * 10000) + _minDateTimeTicks, kind);
                    if (offsetHours != 0)
                    {
                        dt = dt.AddHours(offsetHours);
                    }

                    if (offsetMinutes != 0)
                    {
                        dt = dt.AddMinutes(offsetMinutes);
                    }
                    return true;
                }
            }

            // don't parse pure timespan style XX:YY:ZZ
            if (text.Length == 8 && text[2] == ':' && text[5] == ':')
            {
                dt = DateTime.MinValue;
                return false;
            }

            return DateTime.TryParse(text, null, styles, out dt);
        }

        private sealed class ICollectionTObject<T> : ListObject
        {
            private ICollection<T> _coll;

            public override object List
            {
                get => base.List;
                set
                {
                    base.List = value;
                    _coll = (ICollection<T>)value;
                }
            }

            public override void Clear() => _coll.Clear();
            public override void Add(object value, JsonOptions options = null)
            {
                if (value == null && typeof(T).IsValueType)
                {
                    HandleException(new JsonException("JSO0014: JSON error detected. Cannot add null to a collection of '" + typeof(T) + "' elements."), options);
                }

                _coll.Add((T)value);
            }
        }

        private sealed class IListObject : ListObject
        {
            private IList _list;

            public override object List
            {
                get => base.List;
                set
                {
                    base.List = value;
                    _list = (IList)value;
                }
            }

            public override void Clear() => _list.Clear();
            public override void Add(object value, JsonOptions options = null) => _list.Add(value);
        }

        public abstract class ListObject
        {
            public virtual object List { get; set; }
            public abstract void Clear();
            public abstract void Add(object value, JsonOptions options = null);
            public virtual IDictionary<string, object> Context => null;
        }
    }

    public delegate void JsonCallback(JsonEventArgs e);

    public class JsonOptions //4
    {
        private readonly List<Exception> _exceptions = new List<Exception>();
        internal static DateTimeStyles _defaultDateTimeStyles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AllowInnerWhite | DateTimeStyles.AllowLeadingWhite | DateTimeStyles.AllowTrailingWhite | DateTimeStyles.AllowWhiteSpaces;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonOptions" /> class.
        /// </summary>
        public JsonOptions()
        {
            SerializationOptions = JsonSerializationOptions.Default;
            ThrowExceptions = true;
            DateTimeStyles = _defaultDateTimeStyles;
            FormattingTab = " ";
            StreamingBufferChunkSize = ushort.MaxValue;
            MaximumExceptionsCount = 100;
        }

        /// <summary>
        /// Gets a value indicating the current serialization level.
        /// </summary>
        /// <value>
        /// The current serialization level.
        /// </value>
        public int SerializationLevel { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether exceptions can be thrown during serialization or deserialization.
        /// If this is set to false, exceptions will be stored in the Exceptions collection.
        /// However, if the number of exceptions is equal to or higher than MaximumExceptionsCount, an exception will be thrown.
        /// </summary>
        /// <value>
        /// <c>true</c> if exceptions can be thrown on serialization or deserialization; otherwise, <c>false</c>.
        /// </value>
        public virtual bool ThrowExceptions { get; set; }

        /// <summary>
        /// Gets or sets the maximum exceptions count.
        /// </summary>
        /// <value>
        /// The maximum exceptions count.
        /// </value>
        public virtual int MaximumExceptionsCount { get; set; }

        /// <summary>
        /// Gets or sets the JSONP callback. It will be added as wrapper around the result.
        /// Check this article for more: http://en.wikipedia.org/wiki/JSONP
        /// </summary>
        /// <value>
        /// The JSONP callback name.
        /// </value>
        public virtual string JsonPCallback { get; set; }
        
        public virtual string GuidFormat { get; set; }

        public virtual string DateTimeFormat { get; set; }
        public virtual string DateTimeOffsetFormat { get; set; }
        public virtual DateTimeStyles DateTimeStyles { get; set; }
        public virtual int StreamingBufferChunkSize { get; set; }

        public virtual string FormattingTab { get; set; }

        public virtual Exception[] Exceptions => _exceptions.ToArray();

        public virtual IEnumerable<NewJsonhead.MemberDefinition> FinalizeSerializationMembers(Type type, IEnumerable<NewJsonhead.MemberDefinition> members) => members;

        /// <summary>
        /// Finalizes the deserialization members from an initial setup of members.
        /// </summary>
        /// <param name="type">The input type. May not be null.</param>
        /// <param name="members">The members. May not be null.</param>
        /// <returns>A non-null list of members.</returns>
        public virtual IEnumerable<NewJsonhead.MemberDefinition> FinalizeDeserializationMembers(Type type, IEnumerable<NewJsonhead.MemberDefinition> members) => members;

        /// <summary>
        /// Gets or sets the serialization options.
        /// </summary>
        /// <value>The serialization options.</value>
        public virtual JsonSerializationOptions SerializationOptions { get; set; }

        /// <summary>
        /// Gets or sets a write value callback.
        /// </summary>
        /// <value>The callback.</value>
        public virtual JsonCallback WriteValueCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called before an object (not a value) is serialized.
        /// </summary>
        /// <value>The callback.</value>
        public virtual JsonCallback BeforeWriteObjectCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called before an object (not a value) is serialized.
        /// </summary>
        /// <value>The callback.</value>
        public virtual JsonCallback AfterWriteObjectCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called before an object field or property is serialized.
        /// </summary>
        /// <value>The callback.</value>
        public virtual JsonCallback WriteNamedValueObjectCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called before an instance of an object is created.
        /// </summary>
        /// <value>The callback.</value>
        public virtual JsonCallback CreateInstanceCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called during deserialization, before a dictionary entry is mapped to a target object.
        /// </summary>
        /// <value>The callback.</value>
        public virtual JsonCallback MapEntryCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called during deserialization, before a dictionary entry is applied to a target object.
        /// </summary>
        /// <value>The callback.</value>
        public virtual JsonCallback ApplyEntryCallback { get; set; }

        /// <summary>
        /// Gets or sets a callback that is called during deserialization, to deserialize a list object.
        /// </summary>
        /// <value>The callback.</value>
        public virtual JsonCallback GetListObjectCallback { get; set; }

        /// <summary>
        /// Gets or sets a utility class that will store an object graph to avoid serialization cycles.
        /// If null, a Dictionary&lt;object, object&gt; using an object reference comparer will be used.
        /// </summary>
        /// <value>The object graph instance.</value>
        public virtual IDictionary<object, object> ObjectGraph { get; set; }

        /// <summary>
        /// Adds an exception to the list of exceptions.
        /// </summary>
        /// <param name="error">The exception to add.</param>
        public virtual void AddException(Exception error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));

            if (_exceptions.Count >= MaximumExceptionsCount)
                throw new JsonException("JSO0015: Two many JSON errors detected (" + _exceptions.Count + ").", error);

            _exceptions.Add(error);
        }

        internal int FinalStreamingBufferChunkSize => Math.Max(512, StreamingBufferChunkSize);
        internal IDictionary<object, object> FinalObjectGraph => ObjectGraph ?? new Dictionary<object, object>(Test9.NewJsonhead.ReferenceComparer.Instance);

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns>A newly created insance of this class with all values copied.</returns>
        public virtual JsonOptions Clone()
        {
            var clone = new JsonOptions
            {
                AfterWriteObjectCallback = AfterWriteObjectCallback,
                ApplyEntryCallback = ApplyEntryCallback,
                BeforeWriteObjectCallback = BeforeWriteObjectCallback,
                CreateInstanceCallback = CreateInstanceCallback,
                DateTimeFormat = DateTimeFormat,
                DateTimeOffsetFormat = DateTimeOffsetFormat,
                DateTimeStyles = DateTimeStyles
            };
            clone._exceptions.AddRange(_exceptions);
            clone.FormattingTab = FormattingTab;
            clone.GetListObjectCallback = GetListObjectCallback;
            clone.GuidFormat = GuidFormat;
            clone.MapEntryCallback = MapEntryCallback;
            clone.MaximumExceptionsCount = MaximumExceptionsCount;
            clone.SerializationOptions = SerializationOptions;
            clone.StreamingBufferChunkSize = StreamingBufferChunkSize;
            clone.ThrowExceptions = ThrowExceptions;
            clone.WriteNamedValueObjectCallback = WriteNamedValueObjectCallback;
            clone.WriteValueCallback = WriteValueCallback;
            return clone;
        }        
        public virtual string GetCacheKey() => ((int)SerializationOptions).ToString();
    }
    
    public class JsonEventArgs : EventArgs
    {
        public JsonEventArgs(TextWriter writer, object value, IDictionary<object, object> objectGraph, JsonOptions options)
            : this(writer, value, objectGraph, options, null, null)
        {
        }
        public virtual JsonEventType EventType { get; set; }
        public JsonEventArgs(TextWriter writer, object value, IDictionary<object, object> objectGraph, JsonOptions options, string name, object component)
        {
            Options = options;
            Writer = writer;
            ObjectGraph = objectGraph;
            Value = value;
            Name = name;
            Component = component;
        }

        public virtual bool Handled { get; set; }
        public JsonOptions Options { get; }

        public TextWriter Writer { get; }

        public IDictionary<object, object> ObjectGraph { get; }
        public virtual object Component { get; }
        public virtual object Value { get; set; }
        public virtual string Name { get; set; }
    }
    
    public enum JsonEventType
    {

        Unspecified,
        WriteValue,
        BeforeWriteObject,
        AfterWriteObject,
        WriteNamedValueObject,
        CreateInstance,
        MapEntry,
        ApplyEntry,
        GetListObject,
    }
    
    public enum JsonSerializationOptions
    {
        None = 0x0,
        UseReflection = 0x1,
        UseXmlIgnore = 0x2,
        DateFormatCustom = 0x4,

        SerializeFields = 0x8,
        UseISerializable = 0x10,


        DateFormatJs = 0x20,
        DateFormatIso8601 = 0x40,
        UseScriptIgnore = 0x80,
        DateFormatRoundtripUtc = 0x100,

        EnumAsText = 0x200,
        ContinueOnCycle = 0x400,
        ContinueOnValueError = 0x800,

        SkipNullPropertyValues = 0x1000,

        DateTimeOffsetFormatCustom = 0x2000,

        SkipNullDateTimeValues = 0x4000,
        AutoParseDateTime = 0x8000,
        WriteKeysWithoutQuotes = 0x10000,

        ByteArrayAsBase64 = 0x20000,
        StreamsAsBase64 = 0x40000,
        SkipZeroValueTypes = 0x80000,
        UseJsonAttribute = 0x100000,
        SkipDefaultValues = 0x200000,
        TimeSpanAsText = 0x400000,
        SkipGetOnly = 0x800000,

        Default = UseXmlIgnore | UseScriptIgnore | SerializeFields | AutoParseDateTime | UseJsonAttribute | SkipGetOnly | SkipDefaultValues | SkipZeroValueTypes | SkipNullPropertyValues | SkipNullDateTimeValues,
    }

    public class JsonException : Exception
    {
        public const string Prefix = "JSO";
        public JsonException(string message)
            : base(message)
        {
        }

        public JsonException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }    

}
