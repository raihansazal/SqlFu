﻿using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using SqlFu.Providers;

namespace SqlFu
{
    public interface IDbFactory
    {
        IDbProvider Provider { get; }
        DbConnection Create(DbConnection db=null);
        DbConnection Create(string cnxString=null);
        Task<DbConnection> CreateAsync(CancellationToken cancel, DbConnection db = null);
        Task<DbConnection> CreateAsync(CancellationToken cancel, string cnxString = null);
    }
}