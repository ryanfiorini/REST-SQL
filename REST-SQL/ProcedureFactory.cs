using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using REST_SQL.Extensions;
using REST_SQL.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace REST_SQL
{
    public static class ProcedureFactory
    {
        #region |Private Variables|

        private static Dictionary<string, Procedure> _procedures;
        private static Dictionary<string, string> _connectionStrings;

        #endregion

        #region |Constructors|

        /// <summary>
        /// Clears and repopulates the collections >> init()
        /// </summary>
        static ProcedureFactory()
        {
            init();
        }

        #endregion

        #region |Public Methods|

        /// <summary>
        /// Creates a new procedure based on the given parameters
        /// </summary>
        /// <param name="method">GET|PUT|POST|DELETE</param>
        /// <param name="specificSchema"></param>
        /// <param name="specificName"></param>
        /// <returns>Procedure</returns>
        public static Procedure GetProcedure(string specificSchema, string specificName)
        {
            string procedureName = $"[{specificSchema}].[{specificName}]";
            try
            {
                Procedure source = _procedures[procedureName];
                string serialized = JsonConvert.SerializeObject(source);
                Procedure result = JsonConvert.DeserializeObject<Procedure>(serialized);
                result.Initialize();
                return result;
            }
            catch (Exception ex)
            {
                throw new MissingMethodException($"No procedure found for Stored Procedure {procedureName}");
            }
        }


        /// <summary>
        /// Creates a new procedure based on the given parameters
        /// </summary>
        /// <param name="method">GET|PUT|POST|DELETE</param>
        /// <param name="specificSchema"></param>
        /// <param name="specificName"></param>
        /// <returns>Procedure</returns>
        public static RestProcedure GetRestProcedure(string method, string specificSchema, string specificName)
        {
            string procedureName = $"[{specificSchema}].[{method}_{specificName.ToUnderscore()}]";

            try
            {
                Procedure source = _procedures[procedureName];
                string serialized = JsonConvert.SerializeObject(source);
                RestProcedure result = JsonConvert.DeserializeObject<RestProcedure>(serialized);
                result.Initialize();
                return result;
            }
            catch (Exception ex)
            {
                throw new MissingMethodException($"No endpoint available at {procedureName}");
            }
        }

        public static string GetHelp(string specificShema)
        {
            List<Procedure> lst = new List<Procedure>();
            foreach (KeyValuePair<string, Procedure> kp in _procedures)
            {
                if (kp.Key.StartsWith($"[{specificShema}]"))
                {
                    lst.Add(kp.Value);
                }
            }
            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            return JsonConvert.SerializeObject(lst, jsonSerializerSettings);
        }

        /// <summary>
        /// Clears and repopulates the collections >> init()
        /// </summary>
        public static void Rebuild()
        {
            init();
        }

        #endregion

        #region |Private Methods|

        /// <summary>
        /// Loads connection strings
        /// Calls RESQL.GetProcedures
        /// Builds the Collection of procedures exposed via CreateProcedure()
        /// </summary>
        private static void init()
        {
            getConnectionStrings();
            _procedures = new Dictionary<string, Procedure>();

            StringBuilder bldr = new StringBuilder();

            string connectionString = _connectionStrings["restsql"];
            using (SqlConnection cnn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.CommandText = "restsql.PROCEDURES";
                    cmd.CommandType = System.Data.CommandType.StoredProcedure;
                    cmd.Connection = cnn;
                    cnn.Open();
                    SqlDataReader rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        bldr.Append(rdr.GetString(0));
                    }
                }
            }
            string json = bldr.ToString();
            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            List<Procedure> lst = JsonConvert.DeserializeObject<List<Procedure>>(json, jsonSerializerSettings);

            foreach (Procedure procedure in lst)
            {
                if (procedure.IsAlwaysEncrypted)
                {
                    procedure.ConnectionString = _connectionStrings[procedure.SpecificSchema] + ";Column Encryption Setting=enabled";
                }
                else
                {
                    procedure.ConnectionString = _connectionStrings[procedure.SpecificSchema];
                }
                _procedures.Add($"[{procedure.SpecificSchema}].[{procedure.SpecificName}]", procedure);
            }
        }

        /// <summary>
        /// Populates _connectionStrings
        /// </summary>
        private static void getConnectionStrings()
        {
            _connectionStrings = new Dictionary<string, string>();
            ConfigurationManager.RefreshSection("connectionStrings");

            foreach (ConnectionStringSettings connectionStringSetting in ConfigurationManager.ConnectionStrings)
            {
                _connectionStrings.Add(connectionStringSetting.Name, connectionStringSetting.ConnectionString);
            }
        }

        #endregion

    }
}
