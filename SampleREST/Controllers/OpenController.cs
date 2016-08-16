using REST_SQL;
using REST_SQL.Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace SampleREST.Controllers
{
    public class OpenController : ApiController
    {
        [AcceptVerbs("GET")]
        [Route("api/{specificSchema}/{specificName}")]
        public HttpResponseMessage Get(string specificSchema, string specificName)
        {
            HttpResponseMessage result = null;

            using (RestProcedure procedure = ProcedureFactory.GetRestProcedure("GET", specificSchema, specificName))
            {
                procedure.LoadFromQuery(Request.GetQueryNameValuePairs());
                result = procedure.GetResponse();
            }
            return result;
        }

        [AcceptVerbs("POST")]
        [Route("api/{specificSchema}/{specificName}")]
        public async Task<HttpResponseMessage> Post(string specificSchema, string specificName)
        {
            string requestJson = await Request.Content.ReadAsStringAsync();
            HttpResponseMessage result = null;
            using (RestProcedure procedure = ProcedureFactory.GetRestProcedure("POST", specificSchema, specificName))
            {
                procedure.LoadFromJson(requestJson);
                result = procedure.GetResponse();
            }
            return result;
        }

        [AcceptVerbs("PUT")]
        [Route("api/{specificSchema}/{specificName}")]
        public async Task<HttpResponseMessage> Put(string specificSchema, string specificName)
        {
            string requestJson = await Request.Content.ReadAsStringAsync();
            HttpResponseMessage result = null;
            using (RestProcedure procedure = ProcedureFactory.GetRestProcedure("PUT", specificSchema, specificName))
            {
                procedure.LoadFromJson(requestJson);
                result = procedure.GetResponse();
            }
            return result;
        }

        [AcceptVerbs("DELETE")]
        [Route("api/{specificSchema}/{specificName}")]
        public async Task<HttpResponseMessage> Delete(string specificSchema, string specificName)
        {
            string requestJson = await Request.Content.ReadAsStringAsync();
            HttpResponseMessage result = null;
            using (RestProcedure procedure = ProcedureFactory.GetRestProcedure("DELETE", specificSchema, specificName))
            {
                procedure.LoadFromJson(requestJson);
                result = procedure.GetResponse();
            }
            return result;
        }
    }
}
