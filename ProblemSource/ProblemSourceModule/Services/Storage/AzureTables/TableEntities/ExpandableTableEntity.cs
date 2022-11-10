﻿using Azure.Data.Tables;
using Newtonsoft.Json;
using System.Reflection;
//using System.Linq;
using Common;
using Newtonsoft.Json.Linq;

namespace ProblemSource.Services.Storage.AzureTables.TableEntities
{
    public class ExpandableTableEntity<T> where T : class, new()
    {
        private static readonly int maxLength = 32 * 1024;
        private static readonly string expandedColumnListPropertyName = "__ExpandedColumns";

        public static T ToPoco(TableEntity entity)
        {
            var poco = new T();
            var expanded = entity[expandedColumnListPropertyName] as Dictionary<string, int>;

            foreach (var prop in GetProps())
            {
                object? val;
                if (expanded?.TryGetValue(prop.Name, out var count) == true)
                {
                    val = string.Join("", Enumerable.Range(0, count).Select(index => entity[GetExpandedName(prop.Name, index)]));
                }
                else
                {
                    val = entity[prop.Name];
                }
                if (IsNativelySupportedType(prop.PropertyType) == false)
                {
                    if (val is string str)
                    {
                        try
                        {
                            val = JsonConvert.DeserializeObject(str, prop.PropertyType);
                        }
                        catch (Exception ex)
                        {
                            throw;
                        }
                    }
                    else
                    {
                        throw new Exception($"Unhandled type: {val?.GetType().Name}");
                    }
                }
                
                prop.SetValue(poco, val);
            }

            return poco;
        }

        public static TableEntity FromPoco(T obj)
        {
            var entity = new TableEntity();

            var expandedProps = new Dictionary<string, int>();
            foreach (var prop in GetProps())
            {
                var value = prop.GetValue(obj);
                if (IsNativelySupportedType(prop.PropertyType) == false)
                {
                    var serialized = JsonConvert.SerializeObject(value);
                    if (serialized.Length > maxLength)
                    {
                        var pairs = serialized.SplitByLength(maxLength)
                            .Select((o, i) => new { Key = GetExpandedName(prop.Name, i), Value = o });
                        foreach (var pair in pairs)
                            entity[pair.Key] = pair.Value;

                        expandedProps.Add(prop.Name, pairs.Count());
                        continue;
                    }
                    else
                        value = serialized;
                }
                entity[prop.Name] = value;
            }

            if (expandedProps.Any())
                entity[expandedColumnListPropertyName] = expandedProps;

            return entity;
        }

        private static List<PropertyInfo> GetProps()
        {
            // TODO: emit code instead (performance)?
            return typeof(T)
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.CanRead)
                .ToList();
        }

        private static string GetExpandedName(string propertyName, int index) => $"{propertyName}__{index}";

        private static bool IsNativelySupportedType(Type type)
        {
            return new[]
                { typeof(byte), typeof(BinaryData), typeof(DateTime), typeof(DateTimeOffset), typeof(double), typeof(Guid), typeof(int), typeof(long), typeof(string) }
            .Contains(type);
        }

        //private static bool IsBuiltIn(Type t)
        //{
        //    if (t.IsPrimitive || t == typeof(string) || t == typeof(DateTimeOffset) || t == typeof(DateTime))
        //        return true;

        //    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(t))
        //    {
        //        if (t.IsGenericType)
        //            return t.GenericTypeArguments.All(x => IsBuiltIn(x));
        //    }

        //    return false;
        //}
    }
}
