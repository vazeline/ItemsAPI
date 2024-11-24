using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Items.Data.EFCore.ExtensionMethods
{
    public static class IQueryableExtensions
    {
        public static IQueryable<TEntity> IncludeConditional<TEntity, TProperty>(
            this IQueryable<TEntity> source,
            bool condition,
            Expression<Func<TEntity, TProperty>> navigationPropertyPath)
        where TEntity : class
        {
            if (condition)
            {
                return source.Include(navigationPropertyPath);
            }

            return source;
        }
    }
}
