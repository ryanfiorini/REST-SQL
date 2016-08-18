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
    public class Procedure
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

        #region |Public Methods|

        private SqlCommand buildCommand()
        {
            SqlCommand result = new SqlCommand();
            if (RoutineType != "FUNCTION")
            {
                Parameters.Insert(0, new Parameter() { IsResult = "YES", ParameterName = "@RETURN_VALUE", DataType = "int", ParameterMode = "OUT" });
            }
            result = new SqlCommand();
            result.CommandText = $"[{this.SpecificSchema}].[{this.SpecificName}]";
            result.CommandType = CommandType.StoredProcedure;

            foreach (Parameter parameter in Parameters)
            {
                result.Parameters.Add(parameter.ToSqlParameter());
            }
            return result;
        }

        public SqlCommand BuildSetValidate()
        {
            SqlCommand result = buildCommand();

            foreach (Parameter parameter in Parameters)
            {
                if(parameter.ParameterMode == "IN" || parameter.ParameterMode == "INOUT")
                {
                    if(parameter.GetValue<object>() == null && parameter.IsNullable == false )
                    {
                        throw new ArgumentException($"Required parameter {parameter.ParameterName} was not found in the collection");
                    }
                    result.Parameters[parameter.ParameterName].Value = parameter.GetValue<object>();
                }
            }
            return result;
        }
        
        /// <summary>
        /// Executes procedure and returns json string
        /// Requires a stored procedure to return a single row single column result ie Json
        /// </summary>
        /// <returns>Returns the string of json</returns>
        public string ExecuteJson()
        {
            StringBuilder stringBuilder = new StringBuilder();
            using (SqlConnection cnn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = BuildSetValidate())
                {
                    cmd.Connection = cnn;
                    cnn.Open();
                    SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    while (rdr.Read())
                    {
                        stringBuilder.Append(rdr.GetString(0));
                    }
                    rdr.Close();
                    retrieveValues(cmd);
                }
            }

            return stringBuilder.ToString();
        }


        /// <summary>
        /// Executes procedure and returns Data Table
        /// </summary>
        /// <returns>DataTable</returns>
        public DataTable ExecuteDataTable()
        {
            BuildSetValidate();
            DataTable result = new DataTable();
            using (SqlConnection cnn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = BuildSetValidate())
                {
                    cmd.Connection = cnn;
                    cnn.Open();
                    SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                    result.Load(rdr);
                    rdr.Close();
                    retrieveValues(cmd);
                }
                
            }
            return result;
        }

        /// <summary>
        /// Executes procedure with no result set
        /// </summary>
        public void ExecuteNonQuery()
        {
            using (SqlConnection cnn = new SqlConnection(ConnectionString))
            {
                using (SqlCommand cmd = BuildSetValidate())
                {
                    cmd.Connection = cnn;
                    cnn.Open();
                    cmd.ExecuteNonQuery();
                    cnn.Close();
                    retrieveValues(cmd);
                }
            }
        }

        private void retrieveValues(SqlCommand cmd)
        {
            foreach(Parameter parameter in Parameters)
            {
                if(parameter.ParameterMode == "OUT" || parameter.ParameterMode == "INOUT")
                {
                    if(cmd.Parameters[parameter.OrdinalPosition].Value == DBNull.Value)
                    {
                        parameter.SetValue<object>(null);
                    }
                    else
                    {
                        parameter.SetValue<object>(cmd.Parameters[parameter.OrdinalPosition].Value);
                    }
                }
            }
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
        public virtual T GetValue<T>(int position)
        {
            return getParameter(position).GetValue<T>();
        }

        /// <summary>
        /// Sets the value as T based on the name
        /// </summary>
        /// <typeparam name="T">type of value</typeparam>
        /// <param name="name">name of parameter</param>
        /// <param name="value">value of parameter</param>
        public virtual void SetValue<T>(string name, T value)
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
        /// Gets a parameter by name
        /// </summary>
        /// <param name="name">name of parameter</param>
        /// <returns>Parameter</returns>
        private Parameter getParameter(string name)
        {
            string parameterName = name.StartsWith("@") ? name : $"@{name}";
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

    }
}
