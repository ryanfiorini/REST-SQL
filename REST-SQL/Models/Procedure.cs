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
using System.Text.RegularExpressions;

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

        private SqlConnection _sqlConnection = null;
        private SqlCommand _sqlCommand = null;

        #region |Public Methods|

        /// <summary>
        /// Builds the SqlCommand based on the Parameter Collection
        /// Binds and open connection to the command
        /// </summary>
        public void Initialize()
        {
            _sqlCommand = new SqlCommand();
            if (RoutineType != "FUNCTION")
            {
                Parameters.Insert(0, new Parameter() { IsResult = "YES", ParameterName = "@RETURN_VALUE", DataType = "int", ParameterMode = "OUT" });
            }
            _sqlCommand.CommandText = $"[{this.SpecificSchema}].[{this.SpecificName}]";
            _sqlCommand.CommandType = CommandType.StoredProcedure;

            foreach (Parameter parameter in Parameters)
            {
                _sqlCommand.Parameters.Add(parameter.ToSqlParameter());
            }
            _sqlConnection = new SqlConnection(ConnectionString);
            _sqlCommand.Connection = _sqlConnection;
            _sqlConnection.Open();

        }

        /// <summary>
        /// Executes procedure and returns json string
        /// Requires a stored procedure to be marked with /*Returns Json*/
        /// </summary>
        /// <returns>Returns the string of json</returns>
        public string ExecuteJson()
        {
            if (IsJsonResult == false) throw new MethodAccessException("Procedure does not produce a json result - mark the stored procedure with /*Returns Json*/ if that is the intention");
            checkValues();
            StringBuilder stringBuilder = new StringBuilder();
            SqlDataReader rdr = _sqlCommand.ExecuteReader();
            while (rdr.Read())
            {
                stringBuilder.Append(rdr.GetString(0));
            }
            rdr.Close();
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Executes procedure and returns Data Table
        /// </summary>
        /// <returns>Data as DataTable</returns>
        public DataTable ExecuteDataTable()
        {
            if (IsJsonResult)
            {
                string JsonResult = ExecuteJson();
                return (DataTable)JsonConvert.DeserializeObject(JsonResult, typeof(DataTable));
            }
            else
            {
                checkValues();
                DataTable result = new DataTable();
                SqlDataReader rdr = _sqlCommand.ExecuteReader();
                result.Load(rdr);
                rdr.Close();
                return result;
            } 
        }

        /// <summary>
        /// Executes procedure and returns T
        /// </summary>
        /// <typeparam name="T">Type of result</typeparam>
        /// <returns>Data as T</returns>
        public T Execute<T>()
        {
            if (IsJsonResult)
            {
                string jsonResult = ExecuteJson();
                var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
                return (T)JsonConvert.DeserializeObject(jsonResult, typeof(T), jsonSerializerSettings);
            }
            //TODO Add reflection based obect generator
            checkValues();
            return default(T);
        }

        /// <summary>
        /// Executes procedure with no result set
        /// </summary>
        public void ExecuteNonQuery()
        {
            checkValues();
            _sqlCommand.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets the return value of the function or procedure
        /// </summary>
        /// <returns>return value</returns>
        public int ReturnValue()
        {
            return (int)_sqlCommand.Parameters[0].Value;
        }

        /// <summary>
        /// Gets the parameter value as T based on the ordinal position
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="position">ordinal position of parameter</param>
        /// <returns>value as T</returns>
        public virtual T GetValue<T>(int position)
        {
            object value = _sqlCommand.Parameters[position].Value;
            if (value == DBNull.Value)
            {
                value = null;
            }

            if (value == null)
            {
                return default(T);
            }
            checkCSharpTypeFromSqlDbType(_sqlCommand.Parameters[position].SqlDbType, typeof(T), _sqlCommand.Parameters[0].ParameterName);
            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Gets the parameter value as T based on the parameter name
        /// </summary>
        /// <typeparam name="T">type of return value</typeparam>
        /// <param name="position">ordinal position of parameter</param>
        /// <returns>value as T</returns>
        public virtual T GetValue<T>(string name)
        {
            string parameterName = name.StartsWith("@") ? name : $"@{name}";
            object value = _sqlCommand.Parameters[parameterName].Value;
            if (value == DBNull.Value)
            {
                value = null;
            }

            if (value == null)
            {
                return default(T);
            }
            checkCSharpTypeFromSqlDbType(_sqlCommand.Parameters[parameterName].SqlDbType, typeof(T), parameterName);
            return (T)Convert.ChangeType(value, typeof(T));
        }

        /// <summary>
        /// Sets the value as T based on the name
        /// </summary>
        /// <typeparam name="T">type of value</typeparam>
        /// <param name="name">name of parameter</param>
        /// <param name="value">value of parameter</param>
        public virtual void SetValue<T>(string name, T value)
        {
            string parameterName = name.StartsWith("@") ? name : $"@{name}";
            SqlParameter sqlParameter = _sqlCommand.Parameters[parameterName];
            if(typeof(T) != typeof(object))
            {
                checkCSharpTypeFromSqlDbType(sqlParameter.SqlDbType, typeof(T), parameterName);
            }
            if (value == null)
            {
                _sqlCommand.Parameters[parameterName].Value = DBNull.Value;
            }
            else
            {
                _sqlCommand.Parameters[parameterName].Value = value;
            }
        }

        #endregion

        #region |Validators|

        /// <summary>
        /// Checks the parameter against parameter attributes
        /// Character Max Length
        /// Numeric Range
        /// RegularExpression
        /// </summary>
        private void checkValues()
        {
            //Types are checked in the getters and setters

            foreach (Parameter parameter in Parameters)
            {
                if (parameter.ParameterMode == "IN" || parameter.ParameterMode == "INOUT")
                {
                    object value = _sqlCommand.Parameters[parameter.ParameterName].Value;
                    if (checkNull(parameter, value))
                    {
                        checkMaxLength(parameter, value);
                        checkNumericRange(parameter, value);
                        checkRegEx(parameter, value);
                    }
                }
            }
        }

        /// <summary>
        /// Checks the parameter against IsNullable
        /// </summary>
        /// <param name="parameter"></param>
        /// <param name="value"></param>
        /// <returns>True if the object is null</returns>
        private bool checkNull(Parameter parameter, object value)
        {
            if (parameter.IsNullable == false)
            {
                if(value == DBNull.Value || value == null)
                {
                    throw new ArgumentException($"A required parameter was not provided at Procedure.checkNull()", parameter.ParameterName);
                }
            }
            if(value == DBNull.Value || value == null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Checks the value against CharacterMaximumLength
        /// </summary>
        /// <param name="value"></param>
        private void checkMaxLength(Parameter parameter, object value)
        {
            if (parameter.CharacterMaximumLength.HasValue)
            {
                string s = value.ToString();
                if (s.Length > parameter.CharacterMaximumLength)
                {
                    throw new ArgumentException($"Value exceeds character maximum length of {parameter.CharacterMaximumLength}", parameter.ParameterName);
                }
            }
        }

        /// <summary>
        /// Checks the value against NumericMinimumValue property
        /// Checks the value against NumericMaximumValue property
        /// </summary>
        /// <param name="value"></param>
        private void checkNumericRange(Parameter parameter, object value)
        {
            if (parameter.NumericMinimumValue.HasValue || parameter.NumericMaximumValue.HasValue)
            {
                long test = Convert.ToInt64(value);
                if (parameter.NumericMinimumValue.HasValue)
                {
                    if (test < parameter.NumericMinimumValue.Value)
                    {
                        throw new ArgumentException($"Numeric Minumum Value failure for value {value} against limit of {parameter.NumericMaximumValue.Value}", parameter.ParameterName);
                    }
                }
                if (parameter.NumericMaximumValue.HasValue)
                {
                    if (test > parameter.NumericMaximumValue.Value)
                    {
                        throw new ArgumentException($"Numeric Maximum Value failure for value {value} against limit of {parameter.NumericMaximumValue.Value}", parameter.ParameterName);
                    }
                }
            }
        }
        

        /// <summary>
        /// Checks the value against the RegularExpression property
        /// </summary>
        /// <param name="value"></param>
        private void checkRegEx(Parameter parameter, object value)
        {
            if (parameter.RegularExpression == null) return;

            if (!Regex.IsMatch(value.ToString(), parameter.RegularExpression))
            {
                throw new ArgumentException($"Value {value.ToString()} does not conform to pattern {parameter.RegularExpression}", parameter.ParameterName);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sqlDbType">SqlParameter.SqlDbType</param>
        /// <param name="t">C Sharp Type</param>
        /// <param name="parameterName">Name of the parameter to test</param>
        private void checkCSharpTypeFromSqlDbType(SqlDbType sqlDbType, Type t, string parameterName)
        {
            if(getCSharpTypeFromSqlDbType(sqlDbType) != t)
            {
                throw new ArgumentException($"Sql Data Type {sqlDbType.ToString()} maps to c# type {getCSharpTypeFromSqlDbType(sqlDbType).ToString()}", parameterName);
            }
        }

        /// <summary>
        /// Gets the C Sharp type based on the SqlDbType
        /// </summary>
        /// <param name="sqlDbType">SqlParameter.SqlDbType</param>
        /// <returns>C Sharp Type</returns>
        private Type getCSharpTypeFromSqlDbType(SqlDbType sqlDbType)
        {
            switch (sqlDbType)
            {
                case SqlDbType.BigInt:
                    return typeof(long);
                case SqlDbType.Int:
                    return typeof(int);
                case SqlDbType.SmallInt:
                    return typeof(short);
                case SqlDbType.TinyInt:
                    return typeof(byte);
                case SqlDbType.Bit:
                    return typeof(bool);
                case SqlDbType.Char:
                case SqlDbType.VarChar:
                case SqlDbType.NChar:
                case SqlDbType.NVarChar:
                    return typeof(string);
                case SqlDbType.SmallDateTime:
                case SqlDbType.Date:
                case SqlDbType.DateTime:
                case SqlDbType.DateTime2:
                case SqlDbType.Time:
                    return typeof(DateTime);
                case SqlDbType.DateTimeOffset:
                    return typeof(TimeSpan);
                case SqlDbType.Decimal:
                    return typeof(decimal);
                case SqlDbType.UniqueIdentifier:
                    return typeof(Guid);
                default:
                    throw new ArgumentException($"Data type {sqlDbType.ToString()} is not yet supported at Procedure.getCSharpTypeSqlDbType.");
            }
        }

        #endregion

        #region | IDisposable |

        private bool disposedValue = false;
         /// <summary>
         /// Handles clean up particulary closing and disposing the connection and command objects
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
