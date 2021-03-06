﻿using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace SqlFu
{
    /// <summary>
    /// Base class to define query objects
    /// </summary>
    /// <typeparam name="TInput"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    public abstract class AQueryAsync<TInput, TResult>
    {
        private readonly IDbFactory _getDb;

        protected AQueryAsync(IDbFactory getDb)
        {
            _getDb = getDb;
        }

        public async Task<TResult> Get(TInput i, CancellationToken? cancel)
        {
            var c = cancel ?? CancellationToken.None;
            using (var db = await _getDb.CreateAsync(c).ConfigureFalse())
            {
                return await Execute(i, db,c).ConfigureFalse();
            }
        }

        protected abstract Task<TResult> Execute(TInput i, DbConnection db, CancellationToken cancel);
    }
}
  
   
