using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common.ExtensionMethods;
using Items.Data.EFCore.ExtensionMethods;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Items.Data.EFCore.Utility
{
    public static class SqliteUtility
    {
        public static void ExecuteContextActionAndCatchForeignKeyViolations<TContext>(TContext context, Action<TContext> contextAction)
            where TContext : DbContext
        {
            var (transaction, wasAlreadyInTransaction) = Setup(context);

            try
            {
                contextAction(context);

                if (context.Database.IsSqlite())
                {
                    transaction.Commit();

                    // if we were already in an outer transaction, start another one
                    if (wasAlreadyInTransaction)
                    {
                        BeginTransaction(context);
                    }
                }
            }
            catch (Exception ex) when (ex.Message.Contains("FOREIGN KEY constraint failed") || ex.InnerException?.Message.Contains("FOREIGN KEY constraint failed") == true)
            {
                HandleException(context);
            }
        }

        public static async Task ExecuteContextActionAndCatchForeignKeyViolationsAsync<TContext>(TContext context, Func<TContext, Task> contextActionAsync)
            where TContext : DbContext
        {
            var (transaction, wasAlreadyInTransaction) = Setup(context);

            try
            {
                await contextActionAsync(context);

                if (context.Database.IsSqlite())
                {
                    transaction.Commit();

                    // if we were already in an outer transaction, start another one
                    if (wasAlreadyInTransaction)
                    {
                        BeginTransaction(context);
                    }
                }
            }
            catch (Exception ex) when (ex.Message.Contains("FOREIGN KEY constraint failed") || ex.InnerException?.Message.Contains("FOREIGN KEY constraint failed") == true)
            {
                HandleException(context);
            }
        }

        /// <summary>
        /// SQLite does not support custom schemas, and will throw warnings in the log during testing for any table using one.
        /// Call this method to rename the tables with their schema as a prefix, and remove the custom schema, to prevent the warnings.
        /// </summary>
        public static void RenameCustomSchemaTables(DatabaseFacade database, ModelBuilder modelBuilder)
        {
            if (database.IsSqlite())
            {
                var entityTypesWithCustomSchema = modelBuilder.Model.GetEntityTypes()
                    .Where(x => !x.IsOwned()) // exclude owned entities
                    .Select(x => new { x.ClrType, TableName = x.GetTableName(), Schema = x.GetSchema() })
                    .Where(x => x.Schema != null)
                    .ToList();

                foreach (var entityType in entityTypesWithCustomSchema)
                {
                    modelBuilder.Entity(entityType.ClrType).ToTable($"{entityType.Schema}_{entityType.TableName}");
                }
            }
        }

        private static (IDbContextTransaction Transaction, bool WasAlreadyInTransaction) Setup<TContext>(TContext context)
            where TContext : DbContext
        {
            IDbContextTransaction transaction = null;

            if (context.Database.IsSqlite())
            {
                if (context.Database.CurrentTransaction != null)
                {
                    return (context.Database.CurrentTransaction, true);
                }

                transaction = BeginTransaction(context);
            }

            return (transaction, false);
        }

        private static IDbContextTransaction BeginTransaction(DbContext context)
        {
            // SQLite will unhelpfully just report "FOREIGN KEY constraint failed" when data is invalid, without telling you which one
            // if you run the code inside a transaction, and run some arcane command first, when the exception is caught, you can
            // query an internal table to find out exactly which foreign keys failed
            IDbContextTransaction transaction = context.Database.BeginTransaction();
            context.Database.ExecuteSqlRaw("PRAGMA defer_foreign_keys=1;");
            return transaction;
        }

        private static void HandleException<TContext>(TContext context)
            where TContext : DbContext
        {
            // we will only enter this catch block when using SQLite provider
            // find out which foreign key constraints failed
            var sql = @"select a.""table"" || '.'|| ""from"" from pragma_foreign_key_check a
                    inner join pragma_foreign_key_list(a.""table"") b on
                    b.id == a.fkid;";

            var foreignKeyViolations = context.RawSqlQuery(sql, reader => reader[0].ToString());

            throw new Exception($"The following foreign key violations occurred:\r\n{foreignKeyViolations.Distinct().StringJoin("\r\n")}");
        }
    }
}
