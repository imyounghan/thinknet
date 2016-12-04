﻿using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using ThinkNet.Database;
using ThinkNet.Infrastructure;

namespace UserRegistration.ReadModel
{
    public interface IUserDao
    {
        void Save(UserModel user);

        UserModel Find(string loginid);

        IEnumerable<UserModel> GetAll();
    }

    [Register(typeof(IUserDao))]
    public class UserDao : IUserDao
    {
        private readonly IDataContextFactory _dataContextFactory;

        public UserDao(IDataContextFactory dataContextFactory)
        {
            this._dataContextFactory = dataContextFactory;
        }

        private readonly ConcurrentQueue<UserModel> cache = new ConcurrentQueue<UserModel>();

        #region IUserDao 成员
        public void Save(UserModel user)
        {
            using(var context = _dataContextFactory.Create()) {
                context.Save(user);
                context.Commit();
            }
            //cache.Enqueue(user);
        }


        public UserModel Find(string loginid)
        {
            using(var context = _dataContextFactory.Create()) {
                return context.Find<UserModel>(loginid);
            }
            //return cache.FirstOrDefault(p => p.LoginId == loginid);
        }

        public IEnumerable<UserModel> GetAll()
        {
            using(var context = _dataContextFactory.Create()) {
                return context.CreateQuery<UserModel>().ToArray();
            }
            //return cache;
        }

        #endregion
    }
}
