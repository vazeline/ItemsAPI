using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Common.Utility
{
    public static class TypeUtility
    {
        public static readonly List<Type> AllPrimitiveIntegerTypes = new()
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(char)
        };

        public static readonly List<Type> AllPrimitiveNonIntegerNumericTypes = new()
        {
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        private static readonly ConcurrentDictionary<Type, bool> IsSimpleTypeCache = new ConcurrentDictionary<Type, bool>();

        public static bool IsSimpleType(Type type)
        {
            return IsSimpleTypeCache.GetOrAdd(type, t =>
                type.IsPrimitive ||
                type.IsEnum ||
                type == typeof(string) ||
                type == typeof(decimal) ||
                type == typeof(DateTime) ||
                type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan) ||
                type == typeof(Guid) ||
                IsNullableSimpleType(type));

            static bool IsNullableSimpleType(Type t)
            {
                var underlyingType = Nullable.GetUnderlyingType(t);
                return underlyingType != null && IsSimpleType(underlyingType);
            }
        }

        public static bool IsSubclassOfGenericBaseClass(Type type, Type genericBaseClass)
        {
            while (type != null && type != typeof(object))
            {
                var cur = type.IsGenericType ? type.GetGenericTypeDefinition() : type;

                if (genericBaseClass == cur)
                {
                    return true;
                }

                type = type.BaseType;
            }

            return false;
        }

        public static bool ImplementsNonGenericInterface(Type type, Type interfaceType)
        {
            return interfaceType.IsAssignableFrom(type);
        }

        public static bool ImplementsGenericInterface(Type type, Type interfaceType)
        {
            return type
                .GetTypeInfo()
                .ImplementedInterfaces
                .Any(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == interfaceType);
        }

        public static List<Type> GetGenericInterfaceImplementations(Type type, Type interfaceType)
        {
            return type
                .GetTypeInfo()
                .ImplementedInterfaces
                .Where(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == interfaceType)
                .ToList();
        }

        public static Type GetFieldOrPropertyType(MemberInfo member)
        {
            return member.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)member).FieldType,
                MemberTypes.Property => ((PropertyInfo)member).PropertyType,
                _ => throw new NotSupportedException($"{nameof(MemberInfo.MemberType)} {member.MemberType} is not supported")
            };
        }

        public static object GetDefaultValue(Type type)
        {
            if (type.GetTypeInfo().IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        public static string GetFriendlyName(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type);

            if (underlying != null)
            {
                return $"Nullable<{underlying.Name}>";
            }

            return type.Name;
        }

        public static object ConvertToType(object value, Type convertToType)
        {
            var convertToUnderlyingType = Nullable.GetUnderlyingType(convertToType);

            if (value == null)
            {
                if (!convertToType.IsValueType)
                {
                    return Convert.ChangeType(value, convertToType);
                }

                if (convertToUnderlyingType == null)
                {
                    throw new InvalidCastException($"Cannot convert null object to non-nullable value type {convertToType}");
                }

                // convertToType is a nullable value type
                var converter = TypeDescriptor.GetConverter(convertToType);
                return converter.ConvertFrom(value);
            }

            var valueType = value.GetType();

            if (valueType == convertToType)
            {
                return value;
            }

            if (convertToUnderlyingType != null)
            {
                var converter = TypeDescriptor.GetConverter(convertToType);

                if (valueType == convertToUnderlyingType)
                {
                    return converter.ConvertFrom(value);
                }

                value = Convert.ChangeType(value, convertToUnderlyingType);
                return converter.ConvertFrom(value);
            }

            return Convert.ChangeType(value, convertToType);
        }
    }
}
