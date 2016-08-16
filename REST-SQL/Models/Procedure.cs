using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Data;
using System.Net.Http;
using System.Text;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Net;
using REST_SQL.Extensions;

namespace REST_SQL.Models
{
    public class Procedure : IDisposable
    {

        #region |Public Properties|

        public string SpecificSchema { set; get; }
        public string SpecificName { set; get; }
        public string RoutineType { set; get; }
        public bool IsJsonResult { set; get; }
        public bool IsSingleResult { set; get; }
        public bool IsAlwaysEncrypted { set; get; }
        public bool IsNonQuery { set; get; }

        public List<Parameter> Parameters { set; get; }
        public string ConnectionString { set; get; }

        #endregion

        #region |Private Variables|

        private SqlCommand _sqlCommand = null;
        private bool disposedValue = false;


        #endregion

        #region |Public Methods|

        /// <summary>
        /// Called during the clone operation in ProcedureFactory
        /// Creates the command and wires up the connection
        /// </summary>
        public void Initialize()
        {
            if (RoutineType != "FUNCTION")
            {
                Parameters.Insert(0, new Parameter() { IsResult = "YES", ParameterName = "@RETURN_VALUE", DataType = "int", ParameterMode = "OUT" });
            }
            _sqlCommand = new SqlCommand();
            _sqlCommand.CommandText = $"[{this.SpecificSchema}].[{this.SpecificName}]";
            _sqlCommand.CommandType = CommandType.StoredProcedure;
            _sqlCommand.Connection = getConnection();

            foreach (Parameter parameter in Parameters)
            {
                _sqlCommand.Parameters.Add(parameter.NativeSqlParameter());
                parameter.FriendlyName = parameter.ParameterName.FromSqlParameterName();
            }
            _sqlCommand.Connection = getConnection();
        }

        

        /// <summary>
        /// Executes procedure and returns json string
        /// </summary>
        /// <returns></returns>
        public string ExecuteJson()
        {
            StringBuilder stringBuilder = new StringBuilder();
            SqlDataReader rdr = ExecuteReader();
            while (rdr.Read())
            {
                stringBuilder.Append(rdr.GetString(0));
            }
            rdr.Close();
            rdr = null;

            return stringBuilder.ToString();
        }


        /// <summary>
        /// Executes procedure and returns Data Table
        /// </summary>
        /// <returns>DataTable</returns>
        public DataTable ExecuteDataTable()
        {
            SqlDataReader rdr = ExecuteReader();
            DataTable result = new DataTable();
            result.Load(rdr);
            rdr.Close();
            rdr = null;
            return result;
        }

        /// <summary>
        /// Executes procedure and returns SqlDataReader
        /// </summary>
        /// <returns>SqlDataReader</returns>
        public SqlDataReader ExecuteReader()
        {
            validate();
            return _sqlCommand.ExecuteReader();
        }

        /// <summary>
        /// Executes procedure with no result set
        /// </summary>
        public void ExecuteNonQuery()
        {
            _sqlCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets the return value of the function or procedure
        /// </summary>
        /// <returns>return value</returns>
        public T ReturnValue<T>()
        {
            return getParameter(0).GetValue<T>();
        }

        /// <summary>
        /// Gets the parameter value as T based on the name
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="name">name of parameter</param>
        /// <returns>value as T</returns>
        public T GetValue<T>(string name)
        {
            return getParameter(name).GetValue<T>();
        }

        /// <summary>
        /// Gets the parameter value as T based on the ordinal position
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="position">ordinal position of parameter</param>
        /// <returns>value as T</returns>
        public T GetValue<T>(int position)
        {
            return getParameter(position).GetValue<T>();
        }

        /// <summary>
        /// Sets the value as T based on the name
        /// </summary>
        /// <typeparam name="T">type of value</typeparam>
        /// <param name="name">name of parameter</param>
        /// <param name="value">value of parameter</param>
        public void SetValue<T>(string name, T value)
        {
            getParameter(name).SetValue<T>(value);
        }

        /// <summary>
        /// Sets the value as T based on the ordinal position
        /// </summary>
        /// <typeparam name="T">type of value</typeparam>
        /// <param name="position">ordinal position of parameter</param>
        /// <param name="value">value of parameter</param>
        /// 
        public void SetValue<T>(int position, T value)
        {
            getParameter(position).SetValue<T>(value);
        }

        public void SetValue(string name, object value)
        {
            getParameter(name).SetValue(value);
        }

        #endregion

        #region |Private Methods|

        /// <summary>
        /// Creates and opens a connection
        /// </summary>
        /// <returns>SqlConnection</returns>
        private SqlConnection getConnection()
        {
            SqlConnection result = new SqlConnection(ConnectionString);
            result.Open();
            return result;
        }

        /// <summary>
        /// Validates the all the parameters in the collection
        /// </summary>
        private void validate()
        {
            foreach (Parameter parameter in Parameters)
            {
                parameter.Validate();
            }
        }

        /// <summary>
        /// Gets a parameter by name
        /// </summary>
        /// <param name="name">name of parameter</param>
        /// <returns>Parameter</returns>
        private Parameter getParameter(string name)
        {
            string parameterName = name.StartsWith("@") ? name : $"{name.ToSqlName()}";
            try
            {
                return Parameters.First(p => p.ParameterName == parameterName);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"No parameter was found for provided key {name} as {parameterName}", "name");
            }
        }

        /// <summary>
        /// Gets a parameter by position
        /// </summary>
        /// <param name="position"></param>
        /// <returns>Parameter</returns>
        private Parameter getParameter(int position)
        {
            return Parameters[position];
        }

        #endregion

        #region | IDisposable |

        /// <summary>
        /// Handles clean up
        /// </summary>
        /// <param name="disposing">to prevent redundant calls</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_sqlCommand.Connection.State != ConnectionState.Closed)
                    {
                        _sqlCommand.Connection.Close();
                        _sqlCommand.Connection.Dispose();
                    }
                    _sqlCommand.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }

        

        #endregion

    }
}
