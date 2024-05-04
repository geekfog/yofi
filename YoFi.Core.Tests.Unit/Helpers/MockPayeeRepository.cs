﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YoFi.Core.Models;
using YoFi.Core.Repositories;

namespace YoFi.Tests.Helpers
{
    public class MockPayeeRepository : BaseMockRepository<Payee>, IPayeeRepository
    {
        public override IQueryable<Payee> ForQuery(string q) => string.IsNullOrEmpty(q) ? All : All.Where(x => x.Category.Contains(q) || x.Name.Contains(q));

        public Task BulkEditAsync(string category)
        {
            // We don't need to DO anything here.
            WasBulkEditCalled = true;
            return Task.CompletedTask;
        }

        public bool WasBulkEditCalled { get; private set; } = false;
        public bool WasBulkDeleteCalled { get; private set; } = false;

        public Task<Payee> NewFromTransactionAsync(int txid)
        {
            if (txid == 0)
                throw new InvalidOperationException();

            return Task.FromResult(new Payee() { Name = txid.ToString() });
        }

        public Task BulkDeleteAsync()
        {
            // We don't need to DO anything here.
            WasBulkDeleteCalled = true;
            return Task.CompletedTask;
        }

        public Task LoadCacheAsync()
        {
            // Mock payee repository doesn't support payee matching
            return Task.CompletedTask;
        }

        public Task<string> GetCategoryMatchingPayeeAsync(string Name)
        {
            // Only one category we match!
            string result = null;
            if ("NameMatch" == Name)
                result = "CategoryMatch";

            return Task.FromResult(result);
        }

        public Task SetSelectedAsync(int id, bool value) => throw new NotImplementedException();
    }
}
