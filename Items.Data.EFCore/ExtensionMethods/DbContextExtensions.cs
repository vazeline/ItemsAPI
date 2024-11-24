using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Items.Data.EFCore.ExtensionMethods
{
    public static class DbContextExtensions
    {
        public static string GetTableName<TEntity>(this DbContext context)
        {
            return context.Model.FindEntityType(typeof(TEntity)).GetTableName();
        }

        public static string GetTableName(this DbContext context, Type entityType)
        {
            return context.Model.FindEntityType(entityType).GetTableName();
        }

        public static string GetColumnName<TEntity>(
          this DbContext context,
          Expression<Func<TEntity, object>> propertySelector)
        {
            var member = propertySelector.Body as MemberExpression ?? ((UnaryExpression)propertySelector.Body).Operand as MemberExpression;

            if (member == null)
            {
                throw new ArgumentException("Invalid lambda expression", nameof(propertySelector));
            }

            var property = member.Member as PropertyInfo;

            if (property == null)
            {
                throw new ArgumentException("Expression is not a property", nameof(propertySelector));
            }

            return GetColumnName<TEntity>(context, property.Name);
        }

        public static string GetColumnName<TEntity>(
          this DbContext context,
          string propertyName)
        {
            return context.Model.FindEntityType(typeof(TEntity))
                .FindProperty(propertyName)
                .GetColumnName(StoreObjectIdentifier.Table(context.GetTableName<TEntity>()));
        }

        public static List<T> RawSqlQuery<T>(this DbContext context, string query, Func<DbDataReader, T> map)
        {
            using (var command = context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.Text;

                context.Database.OpenConnection();

                using (var result = command.ExecuteReader())
                {
                    var entities = new List<T>();

                    while (result.Read())
                    {
                        entities.Add(map(result));
                    }

                    return entities;
                }
            }
        }

        public static async Task<List<T>> RawSqlQueryAsync<T>(this DbContext context, string query, Func<DbDataReader, T> map)
        {
            using (var command = context.Database.GetDbConnection().CreateCommand())
            {
                command.CommandText = query;
                command.CommandType = CommandType.Text;

                context.Database.OpenConnection();

                using (var result = await command.ExecuteReaderAsync())
                {
                    var entities = new List<T>();

                    while (await result.ReadAsync())
                    {
                        entities.Add(map(result));
                    }

                    return entities;
                }
            }
        }
    }
}