using Infrastructure.EntityFramework.ReadModel.BatchProjection;
using Infrastructure.Utils;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Infrastructure.EntityFramework
{
    public static class DbSetExtensions
    {
        public static async Task<T?> FirstOrDefaultFromLocalAsync<T>(this DbSet<T> dbSet) where T : class =>
            dbSet.Local.FirstOrDefault() ?? await dbSet.FirstOrDefaultAsync();

        public static async Task<T?> FirstOrDefaultFromLocalAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class =>
            dbSet.Local.FirstOrDefault(predicate.Compile()) ?? await dbSet.FirstOrDefaultAsync(predicate);

        public static async Task<T> FirstFromLocalAsync<T>(this DbSet<T> dbSet) where T : class =>
            dbSet.Local.FirstOrDefault() ?? await dbSet.FirstAsync();

        public static async Task<T> FirstFromLocalAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        {
            var entity = dbSet.Local.FirstOrDefault(predicate.Compile());
            if (entity is null)
            {
                entity = await dbSet.FirstAsync(predicate);
            }
            return entity;
        }

        public static async Task<bool> AnyFromLocalAsync<T>(this DbSet<T> dbSet) where T : class
        {
            if (dbSet.Local.Any())
                return true;

            return await dbSet.AnyAsync();
        }

        public static async Task<bool> AnyFromLocalAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        {
            if (dbSet.Local.Any(predicate.Compile()))
                return true;

            return await dbSet.AnyAsync(predicate);
        }

        public static async Task<List<T>> ToListFromLocalAsync<T>(this DbSet<T> dbSet)
            where T : class, IMergeableEntity<T>
        {
            var listFromDisk = await dbSet.ToListAsync();
            var listFromMemory = dbSet.Local.ToList();
            listFromMemory.AddRange(listFromDisk.Where(x => !listFromMemory.Any(y => y.MergePredicate(x, y))));
            return listFromMemory;
        }

        public static async Task<List<T>> ToListFromLocalAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> wherePredicate)
            where T : class, IMergeableEntity<T>
        {
            var listFromDisk = await dbSet.Where(wherePredicate).ToListAsync();
            var listFromMemory = dbSet.Local.Where(wherePredicate.Compile()).ToList();
            listFromMemory.AddRange(listFromDisk.Where(x => !listFromMemory.Any(y => y.MergePredicate(x, y))));
            return listFromMemory;
        }

    }
}
