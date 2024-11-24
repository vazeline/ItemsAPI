using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Common.Utility
{
    public static class DataTableUtility
    {
        public static DataTable ObjectToDataTable(
            object obj,
            string dataTableName,
            Func<string, string> columnNameTransformer = null)
        {
            DataTable dt;

            if (obj is DataTable objDt)
            {
                dt = objDt;

                if (dt.DataSet != null)
                {
                    dt = dt.Copy();
                }
            }
            else
            {
                var objType = obj.GetType();

                dt = new DataTable();

                if (IsTypeGenericList(objType, out var enumerableType))
                {
                    var propertyInfos = AddDataTableColumns(dt, enumerableType);

                    var enumerator = (obj as IEnumerable).GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        AddDataTableRow(dt, propertyInfos, enumerator.Current, columnNameTransformer);
                    }
                }
                else if (obj is ExpandoObject dynamicObject)
                {
                    AddDataTableColumns(dt, dynamicObject);
                    AddDataTableRow(dt, dynamicObject, columnNameTransformer);
                }
                else
                {
                    var propertyInfos = AddDataTableColumns(dt, objType);
                    AddDataTableRow(dt, propertyInfos, obj, columnNameTransformer);
                }
            }

            dt.TableName = dataTableName;

            if (columnNameTransformer != null)
            {
                foreach (DataColumn column in dt.Columns)
                {
                    column.ColumnName = columnNameTransformer(column.ColumnName);
                }
            }

            return dt;
        }

        private static PropertyInfo[] AddDataTableColumns(DataTable dt, Type t)
        {
            var propertyInfos = t.GetProperties();

            foreach (var pi in propertyInfos)
            {
                if (IsTypeGenericList(pi.PropertyType, out _))
                {
                    dt.Columns.Add(pi.Name, typeof(DataTable));
                }
                else if (TypeUtility.IsSimpleType(pi.PropertyType) || pi.PropertyType == typeof(DataTable))
                {
                    dt.Columns.Add(pi.Name, pi.PropertyType);
                }
            }

            return propertyInfos;
        }

        private static void AddDataTableColumns(DataTable dt, ExpandoObject dynamicObject)
        {
            var asDict = dynamicObject as IDictionary<string, object>;

            foreach (var (key, value) in asDict)
            {
                if (value == null)
                {
                    dt.Columns.Add(key, typeof(object));
                }
                else
                {
                    var valueType = value.GetType();

                    if (IsTypeGenericList(valueType, out _))
                    {
                        dt.Columns.Add(key, typeof(DataTable));
                    }
                    else if (TypeUtility.IsSimpleType(valueType) || valueType == typeof(DataTable))
                    {
                        dt.Columns.Add(key, valueType);
                    }
                }
            }
        }

        private static void AddDataTableRow(
            DataTable dt,
            PropertyInfo[] propertyInfos,
            object rowObj,
            Func<string, string> columnNameTransformer = null)
        {
            var dr = dt.NewRow();
            var rowValues = new List<object>();

            foreach (var pi in propertyInfos)
            {
                if (IsTypeGenericList(pi.PropertyType, out _))
                {
                    rowValues.Add(ObjectToDataTable(pi.GetValue(rowObj), pi.Name, columnNameTransformer));
                }
                else if (TypeUtility.IsSimpleType(pi.PropertyType) || pi.PropertyType == typeof(DataTable))
                {
                    rowValues.Add(pi.GetValue(rowObj));
                }
            }

            dr.ItemArray = rowValues.ToArray();
            dt.Rows.Add(dr);
        }

        private static void AddDataTableRow(
            DataTable dt,
            ExpandoObject dynamicObject,
            Func<string, string> columnNameTransformer = null)
        {
            var asDict = dynamicObject as IDictionary<string, object>;

            var dr = dt.NewRow();
            var rowValues = new List<object>();

            foreach (DataColumn column in dt.Columns)
            {
                if (IsTypeGenericList(column.DataType, out _))
                {
                    rowValues.Add(ObjectToDataTable(asDict[column.ColumnName], column.ColumnName, columnNameTransformer));
                }
                else if (TypeUtility.IsSimpleType(column.DataType)
                    || column.DataType == typeof(DataTable)
                    || (column.DataType == typeof(object) && asDict[column.ColumnName] == null))
                {
                    rowValues.Add(asDict[column.ColumnName]);
                }
            }

            dr.ItemArray = rowValues.ToArray();
            dt.Rows.Add(dr);
        }

        private static bool IsTypeGenericList(Type t, out Type listType)
        {
            listType = null;

            var genericListType = t.GetInterfaces().SingleOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IList<>));

            if (genericListType != null)
            {
                listType = genericListType.GetGenericArguments()[0];
                return true;
            }

            return false;
        }
    }
}
