using System;
using System.Collections.Generic;
using System.Data;


namespace ThinkNet
{
    /// <summary>
    /// <see cref="IDbConnection"/> 的扩展类
    /// </summary>
    public static class DbConnectionExtentions
    {
        private static void AttachParameters(IDbCommand command, IEnumerable<IDbDataParameter> commandParameters)
        {
            if (commandParameters.IsEmpty())
                return;

            foreach (IDataParameter parame in commandParameters) {
                if (parame == null)
                    continue;

                if ((parame.Direction == ParameterDirection.InputOutput || parame.Direction == ParameterDirection.Input) && (parame.Value == null)) {
                    parame.Value = DBNull.Value;
                }
                command.Parameters.Add(parame);
            }
        }
        private static IDbCommand CreateCommandByCommandType(IDbConnection connection, IDbTransaction transaction, CommandType commandType, string commandText, IEnumerable<IDbDataParameter> commandParameters)
        {
            if (connection.State == ConnectionState.Closed)
                throw new InvalidOperationException("connection is closed.");

            IDbCommand command = connection.CreateCommand();
            command.CommandType = commandType;
            command.CommandText = commandText;
            command.Connection = connection;
            if (transaction != null) {
                command.Transaction = transaction;
            }

            if (commandParameters != null) {
                AttachParameters(command, commandParameters);
            }

            return command;
        }

        /// <summary>
        /// 执行当前数据库连接对象的命令,指定参数.
        /// </summary>
        public static int ExecuteNonQuery(this IDbConnection connection, CommandType commandType, string commandText, params IDbDataParameter[] commandParameters)
        {
            return connection.ExecuteNonQuery(commandType, commandText, (IEnumerable<IDbDataParameter>)commandParameters);
        }
        /// <summary>
        /// 执行当前数据库连接对象的命令,指定参数.
        /// </summary>
        public static int ExecuteNonQuery(this IDbConnection connection, CommandType commandType, string commandText, IEnumerable<IDbDataParameter> commandParameters)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");
            if (commandText == null)
                throw new ArgumentNullException("commandText");

            using (IDbCommand command = CreateCommandByCommandType(connection, null, commandType, commandText, commandParameters)) {
                try {
                    return command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    throw ex;
                }
                finally {
                    command.Parameters.Clear();
                }
            }
        }

        /// <summary>
        /// 执行当前数据库连接对象的命令,指定参数.
        /// </summary>
        public static int ExecuteNonQuery(this IDbTransaction transaction, CommandType commandType, string commandText, params IDbDataParameter[] commandParameters)
        {
            return transaction.ExecuteNonQuery(commandType, commandText, (IEnumerable<IDbDataParameter>)commandParameters);
        }

        /// <summary>
        /// 执行当前数据库连接对象的命令,指定参数.
        /// </summary>
        public static int ExecuteNonQuery(this IDbTransaction transaction, CommandType commandType, string commandText, IEnumerable<IDbDataParameter> commandParameters)
        {
            if (transaction == null)
                throw new ArgumentNullException("transaction");
            if (commandText == null)
                throw new ArgumentNullException("commandText");

            using (IDbCommand command = CreateCommandByCommandType(transaction.Connection, transaction, commandType, commandText, commandParameters)) {
                try {
                    return command.ExecuteNonQuery();
                }
                catch (Exception ex) {
                    throw ex;
                }
                finally {
                    command.Parameters.Clear();
                }
            }
        }

        /// <summary>
        /// 执行当前数据库连接对象的数据阅读器,指定参数.
        /// </summary>
        public static IDataReader ExecuteReader(this IDbConnection connection, CommandType commandType, string commandText, params IDbDataParameter[] commandParameters)
        {
            return connection.ExecuteReader(commandType, commandText, (IEnumerable<IDbDataParameter>)commandParameters);
        }

        /// <summary>
        /// 执行当前数据库连接对象的数据阅读器,指定参数.
        /// </summary>
        public static IDataReader ExecuteReader(this IDbConnection connection, CommandType commandType, string commandText, IEnumerable<IDbDataParameter> commandParameters)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");
            if (commandText == null)
                throw new ArgumentNullException("commandText");

            using (IDbCommand command = CreateCommandByCommandType(connection, null, commandType, commandText, commandParameters)) {
                try {
                    return command.ExecuteReader();
                }
                catch (Exception ex) {
                    throw ex;
                }
                finally {
                    bool canClear = true;
                    foreach (IDataParameter commandParameter in command.Parameters) {
                        if (commandParameter.Direction != ParameterDirection.Input)
                            canClear = false;
                    }
                    if (canClear) {
                        command.Parameters.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// 执行当前数据库连接对象的数据阅读器,指定参数.
        /// </summary>
        public static IDataReader ExecuteReader(this IDbTransaction transaction, CommandType commandType, string commandText, params IDbDataParameter[] commandParameters)
        {
            return transaction.ExecuteReader(commandType, commandText, (IEnumerable<IDbDataParameter>)commandParameters);
        }

        /// <summary>
        /// 执行当前数据库连接对象的数据阅读器,指定参数.
        /// </summary>
        public static IDataReader ExecuteReader(this IDbTransaction transaction, CommandType commandType, string commandText, IEnumerable<IDbDataParameter> commandParameters)
        {
            if (transaction == null)
                throw new ArgumentNullException("transaction");
            if (commandText == null)
                throw new ArgumentNullException("commandText");

            using (IDbCommand command = CreateCommandByCommandType(transaction.Connection, transaction, commandType, commandText, commandParameters)) {
                try {
                    return command.ExecuteReader();
                }
                catch (Exception ex) {
                    throw ex;
                }
                finally {
                    bool canClear = true;
                    foreach (IDataParameter commandParameter in command.Parameters) {
                        if (commandParameter.Direction != ParameterDirection.Input)
                            canClear = false;
                    }
                    if (canClear) {
                        command.Parameters.Clear();
                    }
                }
            }
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,指定参数,返回结果集中的第一行第一列.
        /// </summary>
        public static object ExecuteScalar(this IDbConnection connection, CommandType commandType, string commandText, params IDbDataParameter[] commandParameters)
        {
            return connection.ExecuteScalar(commandType, commandText, (IEnumerable<IDbDataParameter>)commandParameters);
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,指定参数,返回结果集中的第一行第一列.
        /// </summary>
        public static object ExecuteScalar(this IDbConnection connection, CommandType commandType, string commandText, IEnumerable<IDbDataParameter> commandParameters)
        {
            if (connection == null)
                throw new ArgumentNullException("connection");
            if (commandText == null)
                throw new ArgumentNullException("commandText");

            using (IDbCommand command = CreateCommandByCommandType(connection, null, commandType, commandText, commandParameters)) {
                try {
                    return command.ExecuteScalar();
                }
                catch (Exception ex) {
                    throw ex;
                }
                finally {
                    command.Parameters.Clear();
                }
            }
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,指定参数,返回结果集中的第一行第一列.
        /// </summary>
        public static object ExecuteScalar(this IDbTransaction transaction, CommandType commandType, string commandText, params IDbDataParameter[] commandParameters)
        {
            return transaction.ExecuteScalar(commandType, commandText, (IEnumerable<IDbDataParameter>)commandParameters);
        }

        /// <summary>
        /// 执行指定数据库连接对象的命令,指定参数,返回结果集中的第一行第一列.
        /// </summary>
        public static object ExecuteScalar(this IDbTransaction transaction, CommandType commandType, string commandText, IEnumerable<IDbDataParameter> commandParameters)
        {
            if (transaction == null)
                throw new ArgumentNullException("transaction");
            if (commandText == null)
                throw new ArgumentNullException("commandText");

            using (IDbCommand command = CreateCommandByCommandType(transaction.Connection, transaction, commandType, commandText, commandParameters)) {
                try {
                    return command.ExecuteScalar();
                }
                catch (Exception ex) {
                    throw ex;
                }
                finally {
                    command.Parameters.Clear();
                }
            }
        }

    }
}
