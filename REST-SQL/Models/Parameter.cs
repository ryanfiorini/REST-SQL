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
        public bool IsNullable { set; get; }
        
        #endregion

        #region |Public Methods|

        /// <summary>
        /// Creates an SqlParameter based on the Parameter properties
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
        /// Sets the SqlParameter direction based on ParameterMode
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
        /// Sets the SqlParameter SqlDbType including including size and scale where applicable
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
                case "numeric":
                case "decimal":
                    parameter.SqlDbType = SqlDbType.Decimal;
                    parameter.Scale = (byte)NumericScale;
                    parameter.Precision = (byte)NumericPrecision;
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
