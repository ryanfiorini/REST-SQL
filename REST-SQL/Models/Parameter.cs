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
        public string FriendlyName { set; get; }

        #endregion

        #region |Private Variables|

        private SqlParameter _sqlNativeParameter = null;

        #endregion

        #region |Constructors|

        /// <summary>
        /// Default contructor - instantiates private _sqlNativeParameter
        /// </summary>
        public Parameter()
        {
            //TODO: check if required
            //_sqlNativeParameter = new SqlParameter();
        }

        #endregion


        #region |Public Methods|

        /// <summary>
        /// Validates the parameter based on the attributes >> validate()
        /// </summary>
        public void Validate()
        {
            if (_sqlNativeParameter.Value == DBNull.Value)
            {
                validate(null);
            }
            else
            {
                validate(_sqlNativeParameter.Value);
            }
        }


        /// <summary>
        /// Sets the value of the parameter
        /// Using this Generic variation enforces type collusion between c# and sql
        /// </summary>
        /// <typeparam name="T">Type of parameter</typeparam>
        /// <param name="value">value of type T</param>
        public void SetValue<T>(T value)
        {
            validate(value);
            if (typeof(T) != getCSharpType())
            {
                throw new InvalidCastException($"Parameter {ParameterName} cannot be cast - Sql Data type {DataType} maps to C# type {getCSharpType()} and value provide was {typeof(T).ToString()}");
            }
            _sqlNativeParameter.Value = value;
        }

        /// <summary>
        /// Sets the value of the parameter
        /// </summary>
        /// <param name="value">value</param>
        public void SetValue(object value)
        {
            validate(value);
            _sqlNativeParameter.Value = value;
        }

        /// <summary>
        /// Gets the value of the parameter
        /// </summary>
        /// <returns>object value</returns>
        public object GetValue()
        {
            if(_sqlNativeParameter.Value == DBNull.Value)
            {
                return null;
            }
            return _sqlNativeParameter.Value;
        }

        /// <summary>
        /// Gets the value of the parameter
        /// <typeparam name="T">Type of parameter</typeparam>
        /// </summary>
        /// <returns>Parameter value as T or default(T) for DBNull.value</returns>
        public T GetValue<T>()
        {
            if (_sqlNativeParameter.Value == DBNull.Value)
            {
                return default(T);
            }
            if (typeof(T) != getCSharpType())
            {
                throw new InvalidCastException($"Sql Data type {DataType} maps to C# type {getCSharpType()} paramter name {ParameterName.FromSqlParameterName()}");
            }
            return (T)Convert.ChangeType(_sqlNativeParameter.Value, typeof(T));
        }

        /// <summary>
        /// If the native parameter is null it creates it
        /// </summary>
        /// <returns>Sql Parameter</returns>
        public SqlParameter NativeSqlParameter()
        {
            if(_sqlNativeParameter == null)
            {
                _sqlNativeParameter = new SqlParameter();
                _sqlNativeParameter.ParameterName = ParameterName;
                setParameterDirection();
                setDataType();
                _sqlNativeParameter.Value = DBNull.Value;
            }
            return _sqlNativeParameter;
        }

        #endregion

        #region |Private Methods|

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
        /// Sets the parameter direction of the _sqlNativeParameter
        /// </summary>
        private void setParameterDirection()
        {
            switch (ParameterMode)
            {
                case "IN":
                    _sqlNativeParameter.Direction = ParameterDirection.Input;
                    break;
                case "OUT":
                    _sqlNativeParameter.Direction = ParameterDirection.ReturnValue;
                    break;
                case "INOUT":
                    _sqlNativeParameter.Direction = ParameterDirection.Output;
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
        private void setDataType()
        {
            switch (DataType)
            {
                case "bigint":
                    _sqlNativeParameter.SqlDbType = SqlDbType.BigInt;
                    break;
                case "int":
                    _sqlNativeParameter.SqlDbType = SqlDbType.Int;
                    break;
                case "smallint":
                    _sqlNativeParameter.SqlDbType = SqlDbType.SmallInt;
                    break;
                case "tinyint":
                    _sqlNativeParameter.SqlDbType = SqlDbType.TinyInt;
                    break;
                case "bit":
                    _sqlNativeParameter.SqlDbType = SqlDbType.Bit;
                    break;
                case "char":
                    _sqlNativeParameter.SqlDbType = SqlDbType.Char;
                    _sqlNativeParameter.Size = CharacterMaximumLength.Value;
                    break;
                case "varchar":
                    _sqlNativeParameter.SqlDbType = SqlDbType.VarChar;
                    _sqlNativeParameter.Size = CharacterMaximumLength.Value;
                    break;
                case "nchar":
                    _sqlNativeParameter.SqlDbType = SqlDbType.NChar;
                    _sqlNativeParameter.Size = CharacterMaximumLength.Value;
                    break;
                case "nvarchar":
                    _sqlNativeParameter.SqlDbType = SqlDbType.NVarChar;
                    _sqlNativeParameter.Size = CharacterMaximumLength.Value;
                    break;
                case "smalldatetime":
                    _sqlNativeParameter.SqlDbType = SqlDbType.SmallDateTime;
                    break;
                case "date":
                    _sqlNativeParameter.SqlDbType = SqlDbType.Date;
                    break;
                case "datetime":
                    _sqlNativeParameter.SqlDbType = SqlDbType.DateTime;
                    break;
                case "datetime2":
                    _sqlNativeParameter.SqlDbType = SqlDbType.DateTime;
                    _sqlNativeParameter.Scale = (byte)DateTimePrecision;
                    break;
                case "time":
                    _sqlNativeParameter.SqlDbType = SqlDbType.Time;
                    _sqlNativeParameter.Scale = (byte)DateTimePrecision;
                    break;
                case "datetimeoffset":
                    _sqlNativeParameter.SqlDbType = SqlDbType.DateTimeOffset;
                    _sqlNativeParameter.Scale = (byte)DateTimePrecision;
                    break;
                case "uniqueidentifier":
                    _sqlNativeParameter.SqlDbType = SqlDbType.UniqueIdentifier;
                    break;
                default:
                    throw new ArgumentException($"data type {DataType} is not yet supported, add data type to Parameter.cs setDataType()");
            }
        }

        #endregion
    }
}
