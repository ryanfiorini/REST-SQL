using REST_SQL.Extensions;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;

namespace REST_SQL.Models
{
    public class Parameter
    {
        #region |Public Properties|
        
        //All properties populated in procedure factory init()

        public int OrdinalPosition { set; get; }
        public string ParameterMode { set; get; }
        public string IsResult { set; get; }
        public string ParameterName { set; get; }
        public string DataType { set; get; }
        public byte? NumericPrecision { set; get; }
        public int? NumericScale { set; get; }
        public int? CharacterMaximumLength { set; get; }
        public short? DateTimePrecision { set; get; }
        public long? NumericMinimumValue { set; get; }
        public long? NumericMaximumValue { set; get; }
        public string RegularExpression { set; get; }
        public bool IsNullable { set; get; } = true;

        private object _value;
        
        #endregion

        #region |Public Methods|

        /// <summary>
        /// Sets the value of the parameter
        /// Using this Generic variation enforces type collusion between c# and sql
        /// </summary>
        /// <typeparam name="T">Type of parameter</typeparam>
        /// <param name="value">value</param>
        public void SetValue<T>(T value)
        {
            validate(value);
            checkCSharpType(typeof(T));
            _value = value;
        }

        /// <summary>
        /// Gets the value of the parameter
        /// <typeparam name="T">Type of parameter</typeparam>
        /// </summary>
        /// <returns>Parameter value as T or default(T) for DBNull.value</returns>
        public T GetValue<T>()
        {
            checkCSharpType(typeof(T));
            if (_value == null)
            {
                return default(T);
            }
            return (T)Convert.ChangeType(_value, typeof(T));
        }

        /// <summary>
        /// If the native parameter is null it creates it
        /// </summary>
        /// <returns>Sql Parameter</returns>
        public SqlParameter ToSqlParameter()
        {
            SqlParameter result = null;
            result = new SqlParameter();
            result.ParameterName = ParameterName;
            setParameterDirection(result);
            setDataType(result);
            result.Value = DBNull.Value;
            return result;
        }

        #endregion

        #region |Private Methods|

        /// <summary>
        /// Calls the set of validation routines for the value passed
        /// </summary>
        /// <param name="value"></param>
        private void validate(object value)
        {
            checkNull(value);
            if (value == null) return;
            checkMaxLength(value);
            checkNumericRange(value);
            checkRegEx(value);
        }

        /// <summary>
        /// Validates the type against what is expected according to the DB type
        /// </summary>
        /// <param name="t"></param>
        private void checkCSharpType(Type t)
        {
            if (t == typeof(object)) return;

            if (t != getCSharpType())
            {
                throw new InvalidCastException($"Sql Data type {DataType} maps to C# type {getCSharpType()} paramter name {ParameterName.FromSqlParameterName()}");
            }
        }

        /// <summary>
        /// Checks the value against CharacterMaximumLength
        /// </summary>
        /// <param name="value"></param>
        private void checkMaxLength(object value)
        {
            if (CharacterMaximumLength.HasValue)
            {
                if (value != null)
                {
                    string s = value.ToString();
                    if (s.Length > CharacterMaximumLength)
                    {
                        throw new ArgumentException($"Value exceeds character maximum length of {CharacterMaximumLength}", ParameterName.ToJsonName());
                    }
                }
            }
        }

        /// <summary>
        /// Checks the value against NumericMinimumValue property
        /// Checks the value against NumericMaximumValue property
        /// </summary>
        /// <param name="value"></param>
        private void checkNumericRange(object value)
        {
            if (NumericMinimumValue.HasValue || NumericMaximumValue.HasValue)
            {
                long test = Convert.ToInt64(value);
                if (NumericMinimumValue.HasValue)
                {
                    if (test < NumericMinimumValue.Value)
                    {
                        throw new ArgumentException($"Numeric Minumum Value failure for value {value} against limit of {NumericMaximumValue.Value}", ParameterName.ToJsonName());
                    }
                }
                if (NumericMaximumValue.HasValue)
                {
                    if (test > NumericMaximumValue.Value)
                    {
                        throw new ArgumentException($"Numeric Maximum Value failure for value {value} against limit of {NumericMaximumValue.Value}", ParameterName.ToJsonName());
                    }

                }
            }
        }

        /// <summary>
        /// Checks the value against the RegularExpression property
        /// </summary>
        /// <param name="value"></param>
        private void checkRegEx(object value)
        {
            if (RegularExpression == null) return;

            if (!Regex.IsMatch(value.ToString(), RegularExpression))
            {
                throw new ArgumentException($"Value {value.ToString()} does not conform to pattern {RegularExpression}", ParameterName.ToJsonName());
            }
        }

        /// <summary>
        /// Checks the object value against the IsNullable property
        /// </summary>
        /// <param name="value"></param>
        private void checkNull(object value)
        {
            if (!IsNullable)
            {
                if (value == null)
                {
                    throw new ArgumentException("Value cannot be null", ParameterName.ToJsonName());
                }
            }
        }

        /// <summary>
        /// Sets the parameter direction of the _sqlNativeParameter
        /// </summary>
        private void setParameterDirection(SqlParameter parameter)
        {
            switch (ParameterMode)
            {
                case "IN":
                    parameter.Direction = ParameterDirection.Input;
                    break;
                case "OUT":
                    parameter.Direction = ParameterDirection.ReturnValue;
                    break;
                case "INOUT":
                    parameter.Direction = ParameterDirection.Output;
                    break;
            }
        }

        /// <summary>
        /// Gets the C# equivalent type of the parmater sql type
        /// </summary>
        /// <returns></returns>
        private Type getCSharpType()
        {
            switch (DataType)
            {
                case "bigint":
                    return typeof(long);
                case "int":
                    return typeof(int);
                case "smallint":
                    return typeof(short);
                case "tinyint":
                    return typeof(byte);
                case "bit":
                    return typeof(bool);
                case "char":
                case "varchar":
                case "nchar":
                case "nvarchar":
                    return typeof(string);
                case "smalldatetime":
                case "date":
                case "datetime":
                case "datetime2":
                case "time":
                    return typeof(DateTime);
                case "datetimeoffset":
                    return typeof(TimeSpan);
                case "uniqueidentifier":
                    return typeof(Guid);
                default:
                    throw new ArgumentException($"data type {DataType} is not yet supported, add data type to Parameter.cs setCSharpType()");
            }
        }

        /// <summary>
        /// Sets the data type of the _sqlNativeParameter
        /// </summary>
        private void setDataType(SqlParameter parameter)
        {
            switch (DataType)
            {
                case "bigint":
                    parameter.SqlDbType = SqlDbType.BigInt;
                    break;
                case "int":
                    parameter.SqlDbType = SqlDbType.Int;
                    break;
                case "smallint":
                    parameter.SqlDbType = SqlDbType.SmallInt;
                    break;
                case "tinyint":
                    parameter.SqlDbType = SqlDbType.TinyInt;
                    break;
                case "bit":
                    parameter.SqlDbType = SqlDbType.Bit;
                    break;
                case "char":
                    parameter.SqlDbType = SqlDbType.Char;
                    parameter.Size = CharacterMaximumLength.Value;
                    break;
                case "varchar":
                    parameter.SqlDbType = SqlDbType.VarChar;
                    parameter.Size = CharacterMaximumLength.Value;
                    break;
                case "nchar":
                    parameter.SqlDbType = SqlDbType.NChar;
                    parameter.Size = CharacterMaximumLength.Value;
                    break;
                case "nvarchar":
                    parameter.SqlDbType = SqlDbType.NVarChar;
                    parameter.Size = CharacterMaximumLength.Value;
                    break;
                case "smalldatetime":
                    parameter.SqlDbType = SqlDbType.SmallDateTime;
                    break;
                case "date":
                    parameter.SqlDbType = SqlDbType.Date;
                    break;
                case "datetime":
                    parameter.SqlDbType = SqlDbType.DateTime;
                    break;
                case "datetime2":
                    parameter.SqlDbType = SqlDbType.DateTime;
                    parameter.Scale = (byte)DateTimePrecision;
                    break;
                case "time":
                    parameter.SqlDbType = SqlDbType.Time;
                    parameter.Scale = (byte)DateTimePrecision;
                    break;
                case "datetimeoffset":
                    parameter.SqlDbType = SqlDbType.DateTimeOffset;
                    parameter.Scale = (byte)DateTimePrecision;
                    break;
                case "uniqueidentifier":
                    parameter.SqlDbType = SqlDbType.UniqueIdentifier;
                    break;
                default:
                    throw new ArgumentException($"data type {DataType} is not yet supported, add data type to Parameter.cs setDataType()");
            }
        }

        #endregion
    }
}
