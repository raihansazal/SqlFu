﻿using FluentAssertions;
using SqlFu;
using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CavemanTools.Model;
using Tests.Usage;
using Xunit;
using Type = Tests.Usage.Type;

namespace Tests.SqlServer
{

    public abstract class ADBOperationsTests:IDisposable
    {
        protected DbConnection _db;


        public ADBOperationsTests()
        {
            _db = GetConnection();
      
            _db.Open();
            Init();
            AddData();
        }

        private User[] _inserted =
        {
            new User()
            {
                FirstName = "John",
                LastName = "Doe"
            }
            ,new User()
            {
                FirstName = "John1",
                LastName = "Doe",
                Posts = 3
            }
            ,
            new User()
            {
                FirstName = "Jane",
                LastName = "Doe",
                Category = Type.Page,
                Posts = 0
            }
        };
        private void AddData()
        {
            foreach (var d in _inserted) _db.Insert(d);
        }

        protected abstract DbConnection GetConnection();
        protected abstract void Init();

        [Fact]
        public void insert_and_return_id()
        {

            _db.Insert(new User()
            {
                FirstName = "Jane2",
                LastName = "Doe",
                Category = Type.Page,
                Posts = 0
            }).GetInsertedId<int>().Should().Be(4);
          

        }

        [Fact]
        public void use_simple_sql_builder()
        {
            var tname = _db.GetPocoInfo<User>().TableName.ToString();
            var posts = 3;
            var name=_db.SqlTo<string>(s=>s.AppendRaw($"select FirstName from {tname}").Append($" where Posts={posts}")).GetValue();
            name.Should().Be("John1");
        }

        [Fact]
        public void insert_ignore()
        {

            _db.Insert(new User()
            {
                FirstName = "Jane2",
                LastName = "Doe",
                Category = Type.Page,
                Posts = 0
            }).GetInsertedId<int>().Should().Be(4);

            _db.Invoking(d => d.Insert(new User()
            {
                FirstName = "Jane2",
                LastName = "Doe",
                Category = Type.Page,
                Posts = 0
            })).Should().Throw<DbException>();
           
         _db.Invoking(d=>d.InsertIgnore(new User()
            {
                FirstName = "Jane2",
                LastName = "Doe",
                Category = Type.Page,
                Posts = 0
            })).Should().NotThrow();
                     
        }

        [Fact]
        public void convert_value_at_writing()
        {
            
        }

        [Fact]
        public void get_user_with_fullname()
        {
           
            var user=_db.QueryRow(q => q.From<User>()
                .Where(d => !d.IsDeleted && d.Category==Type.Page)
                .And(d => d.InjectSql("Posts= @no", new {no = 0}))
                .Select(d=>new {d.Id,Fullname=d.Concat(d.FirstName," ",d.LastName)}));
            user.Fullname.Should().Be("Jane Doe");
            user.Id.Should().Be(3);
            
        }

        [Fact]
        public void count_users()
        {
            _db.QueryValue<int>(q => q.From<User>().Select(d => d.Count())).Should().Be(3);
        }

        [Fact]
        public void update_user_where_writable_converter()
        {
            _db.Update<User>().Set(d => d.Category ,Type.Post).Where(d => d.Category == Type.Page).Execute()
                .Should().Be(1);
        }

        [Fact]
        public void sum_of_posts()
        {
            var all=_db.QueryAs(q => q.From<User>()
                .GroupBy(d => d.Category)
                .Select(d => new {d.Category, total= d.Sum(d.Posts + 1)})
                ).ToDictionary(d=>d.Category,d=>d.total);
            all[Type.Post].Should().Be(5);
            all[Type.Page].Should().Be(1);
        }


        [Fact]
        public void delete_anon_where()
        {
            _db.DeleteFromAnonymous(new {Category = ""}, _db.GetTableName<User>(),
                d => d.Category == Type.Page.ToString())
                .Should().Be(1);
            _db.CountRows<User>().Should().Be(2);
        }


        [Fact]
        public void pagination()
        {
            var p=_db.QueryPaged(q => q.From<User>().OrderBy(d=>d.Id).SelectAll(), Pagination.Create(1, 1));
            p.Count.Should().Be(3);
            p.Items.First().Should().BeEquivalentTo(_inserted[1],d=>d.Excluding(s=>s.Id).Excluding(s=>s.CreatedOn));
        }
      
        [Fact]
        public async Task get_single_row_no_result()
        {
            var user=await _db.QueryRowAsync(q => q.From<User>().Where(d => d.Id == 1000).Limit(1).SelectAll(useAsterisk: true),
                CancellationToken.None);
            user.Should().BeNull();
        }

        [Fact]
        public void update_one_row()
        {
            _db.UpdateFrom(
                q => q.Data(new { Firstname = "John3", Id = 3 }).Ignore(d => d.Id)
                ,o => o.SetTableName(_db.GetTableName<User>())
                )
                .Where(d => d.Firstname == "John")
                .Execute().Should().Be(1);
            _db.CountRows<User>(d => d.FirstName != "John3").Should().Be(2);         
        }

        [Fact]
        public void simpler_update_from_values()
        {
            _db.UpdateFrom(new {FirstName = "John3"}, _db.GetTableName<User>()).Where(new {Id = 1}).Execute().Should()
                .Be(1);
            _db.CountRows<User>(d => d.FirstName != "John3").Should().Be(2);         
        }

        [Fact]
        public void simpler_update_from_values_with_criteria()
        {
            _db.UpdateFrom(new {FirstName = "Jane4"}, _db.GetTableName<User>())
                .Where(new {Id = 1,FirstName=""},d=>d.FirstName=="Jane")
                .Execute().Should().Be(1);
            _db.CountRows<User>(d => d.FirstName != "Jane4").Should().Be(2);         
            _db.CountRows<User>(d => d.FirstName == "Jane4").Should().Be(1);         
        }



        public void Dispose()
        {
            _db.Execute($"drop table {_db.GetTableName<User>()}");
           _db.Dispose();

        }
    }
}