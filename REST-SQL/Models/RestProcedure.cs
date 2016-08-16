using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using REST_SQL.Extensions;
using System.Collections.Generic;
using System.Dynamic;
using System.Net;
using System.Net.Http;
using System.Text;

namespace REST_SQL.Models
{
    public class RestProcedure : Procedure
    {
        /// <summary>
        /// Sets the parameters from the query string
        /// Table Value Parameters are not supported
        /// </summary>
        /// <param name="keyValuePairs">From HttpRequest</param>
        public void LoadFromQuery(IEnumerable<KeyValuePair<string, string>> keyValuePairs)
        {
            //TODO: have to handle nested data and tables

            foreach (KeyValuePair<string, string> kp in keyValuePairs)
            {
                SetValue(kp.Key, (object)kp.Value);
            }
        }

        /// <summary>
        /// Sets the parameters from jsong
        /// Table Value parameters are not supported
        /// </summary>
        /// <param name="json"></param>
        public void LoadFromJson(string json)
        {
            dynamic jsonObject = JsonConvert.DeserializeObject<ExpandoObject>(json);

            foreach(KeyValuePair<string,object> keyPair in jsonObject)
            {
                SetValue(keyPair.Key, keyPair.Value);
                
                //TODO: have to handle nested data and tables

            }
        }

        public string GetQueryParameters()
        {
            //pull the method type off the paramater name

            StringBuilder bldr = new StringBuilder();
            string procedureName = SpecificName.Substring(SpecificName.IndexOf('_')).ToCamel();
            bldr.Append($"{SpecificSchema}/{SpecificName.FromSqlProcedureName()}");
            string delimiter = "?";

            foreach (Parameter parameter in Parameters)
            {
                if (parameter.ParameterMode == "IN")
                {
                    bldr.Append(delimiter);
                    delimiter = "+";
                    bldr.Append($"{parameter.ParameterName.FromSqlParameterName()}={{value}}");
                }
            }
            return bldr.ToString();
        }

        public string GetJsonParameters()
        {
            //pull the method type off the paramater name

            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            foreach (Parameter parameter in Parameters)
            {
                if (parameter.ParameterMode == "IN")
                {
                    dictionary.Add(parameter.ParameterName.FromSqlParameterName(), "value");
                }
            }

            var jsonSerializerSettings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
            return JsonConvert.SerializeObject(dictionary, jsonSerializerSettings);

        }

        public HttpResponseMessage GetResponse()
        {
            string json = ExecuteJson();
            HttpResponseMessage result = new HttpResponseMessage((HttpStatusCode)ReturnValue<int>());
            if (result.StatusCode != HttpStatusCode.OK)
            {
                json = GetValue<string>("@MESSAGE_RESULT");
            }
            result.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return result;
        }
    }
}
