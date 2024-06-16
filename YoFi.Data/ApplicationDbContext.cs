﻿using EFCore.BulkExtensions;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using YoFi.Core;
using YoFi.Core.Models;

namespace YoFi.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProvider
    {
        private readonly bool inmemory;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // Bulk operations cannot be completed on an in-memory database
            // TODO: I wish there was a cleaner way to do this.
            inmemory = Database.ProviderName.Contains("InMemory");
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            builder.Entity<Split>().ToTable("Split");

            builder.Entity<Transaction>().HasIndex(p => new { p.Timestamp, p.Hidden, p.Category });

            //
            // Note that this isn't how I'd do this anymore. Now, I would have a separate YoFi.Data.Sqlite
            // project with its own migrations.
            // 

            // https://stackoverflow.com/questions/60503553/ef-core-linq-to-sqlite-could-not-be-translated-works-on-sql-server
            if (Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
            {
                foreach (var entityType in builder.Model.GetEntityTypes())
                {
                    var properties = entityType.ClrType.GetProperties().Where(p => p.PropertyType == typeof(decimal));
                    foreach (var property in properties)
                    {
                        builder.Entity(entityType.Name).Property(property.Name).HasConversion<double>();
                    }
                }
            }
        }

        #region Entity Sets

        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<Payee> Payees { get; set; }
        public DbSet<Split> Splits { get; set; }
        public DbSet<BudgetTx> BudgetTxs { get; set; }
        public DbSet<Receipt> Receipts { get; set; }

        #endregion

        #region CRUD Entity Accessors

        IQueryable<T> IDataProvider.Get<T>() where T : class
        {
            return Set<T>();
        }

        IQueryable<TEntity> IDataProvider.GetIncluding<TEntity, TProperty>(Expression<Func<TEntity, TProperty>> navigationPropertyPath) where TEntity : class
            => base.Set<TEntity>().Include(navigationPropertyPath);

        void IDataProvider.Add(object item)
        {
            base.Add(item);
        }

        void IDataProvider.Update(object item)
        {
            base.Update(item);
        }

        void IDataProvider.Remove(object item) 
        {
            base.Remove(item); 
        }

        Task IDataProvider.SaveChangesAsync()
        {
            return base.SaveChangesAsync();
        }

        #endregion

        #region Async Queries

        Task<List<T>> IDataProvider.ToListNoTrackingAsync<T>(IQueryable<T> query) 
        {
            return query.AsNoTracking().ToListAsync(); 
        }

        Task<int> IDataProvider.CountAsync<T>(IQueryable<T> query)
        {
            return query.CountAsync();
        }

        Task<bool> IDataProvider.AnyAsync<T>(IQueryable<T> query) 
        {
            return query.AnyAsync(); 
        }

        #endregion

        #region Bulk Operations

        Task<int> IDataProvider.ClearAsync<T>() where T : class => Set<T>().BatchDeleteAsync();

        /// <summary>
        /// Insert many items en masse
        /// </summary>
        /// <remarks>
        /// This is much more efficient than doing it one at a time
        /// </remarks>
        /// <typeparam name="T">Type of items</typeparam>
        /// <param name="items">Items to be inserted</param>
        /// <returns>True if you could expect child items to have been inserted</returns>

        async Task IDataProvider.BulkInsertAsync<T>(IList<T> items)
        {
            if (inmemory)
            {
                base.Set<T>().AddRange(items);
                await base.SaveChangesAsync();
            }
            else
            {
                await this.BulkInsertAsync(items, b => b.SetOutputIdentity = true);

                // Fix for AB#1387: [Production Bug] Seed database with transactions does not save splits
                // Works around Issue #780 in EFCore.BulkExtensions
                // https://github.com/borisdj/EFCore.BulkExtensions/issues/780
                // Also see AB#1388: Revert fix for #1387

                // So, this might seem like a hack, checking for the type here.  However, this at least
                // localizes the impact to the smallest footprint. The previous solution impacted way too
                // much code, largely because many tests use an in memory database, which calls
                // AddRange directly, which DOES add the child items

                if (items is IList<Transaction> transactions)
                {
                    // Ensure there is object linkage from splits to transaction first
                    foreach (var tx in transactions.Where(x => x.HasSplits))
                        foreach (var split in tx.Splits)
                            split.Transaction = tx;

                    var splits = transactions.Where(x => x.HasSplits).SelectMany(x => x.Splits).ToList();
                    foreach (var split in splits)
                        split.TransactionID = split.Transaction.ID;

                    await this.BulkInsertAsync(splits);
                }
            }
        }

        async Task IDataProvider.BulkDeleteAsync<T>(IQueryable<T> items)
        {
            if (inmemory)
            {
                base.Set<T>().RemoveRange(items);
                await base.SaveChangesAsync();
            }
            else
                await items.BatchDeleteAsync();
        }

        async Task IDataProvider.BulkUpdateAsync<T>(IQueryable<T> items, T newvalues, List<string> columns)
        {
            if (inmemory)
            {
                // We support ONLY a very limited range of possibilities, which is where this
                // method is actually called.
                if (typeof(T) != typeof(Transaction))
                    throw new NotImplementedException("Bulk Update on in-memory DB is only implemented for transactions");

                var txvalues = newvalues as Transaction;
                var txitems = items as IQueryable<Transaction>;
                var txlist = await txitems.ToListAsync();
                foreach (var item in txlist)
                {
                    if (columns.Contains("Imported"))
                        item.Imported = txvalues.Imported;
                    if (columns.Contains("Hidden"))
                        item.Hidden = txvalues.Hidden;
                    if (columns.Contains("Selected"))
                        item.Selected = txvalues.Selected;
                }
                UpdateRange(txlist);

                await SaveChangesAsync();
            }
            else
                await items.BatchUpdateAsync(newvalues,columns);
        }

        #endregion
    }
}
