using REST_SQL;
using REST_SQL.Models;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Threading;
using System.Web.Http.Controllers;
using System.Net;
using System.Text;

namespace SampleREST.Controllers
{
    public class OpenController : ApiController
    {

        [AcceptVerbs("GET")]
        [Route("api/help/open/")]
        public HttpResponseMessage GetHelp()
        {
            string json = ProcedureFactory.GetHelp("open");
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }


        [AcceptVerbs("GET")]
        [Route("api/open/{specificName}")]
        public HttpResponseMessage Get(string specificName)
        {
            using (RestProcedure procedure = ProcedureFactory.GetRestProcedure("GET", "open", specificName))
            {
                procedure.LoadFromQuery(Request.GetQueryNameValuePairs());
                return procedure.GetResponse();
            }
        }

        [AcceptVerbs("POST")]
        [Route("api/open/{specificName}")]
        public async Task<HttpResponseMessage> Post(string specificName)
        {
            string requestJson = await Request.Content.ReadAsStringAsync();
            using (RestProcedure procedure = ProcedureFactory.GetRestProcedure("POST", "open", specificName))
            {
                procedure.LoadFromJson(requestJson);
                return procedure.GetResponse();
            }
        }

        [AcceptVerbs("PUT")]
        [Route("api/{specificSchema}/{specificName}")]
        public async Task<HttpResponseMessage> Put(string specificName)
        {
            string requestJson = await Request.Content.ReadAsStringAsync();
            using (RestProcedure procedure = ProcedureFactory.GetRestProcedure("PUT", "open", specificName))
            {
                procedure.LoadFromJson(requestJson);
                return procedure.GetResponse();
            }
        }

        [AcceptVerbs("DELETE")]
        [Route("api/{specificSchema}/{specificName}")]
        public async Task<HttpResponseMessage> Delete(string specificName)
        {
            string requestJson = await Request.Content.ReadAsStringAsync();
            using (RestProcedure procedure = ProcedureFactory.GetRestProcedure("DELETE", "open", specificName))
            {
                procedure.LoadFromJson(requestJson);
                return procedure.GetResponse();
            }
        }
    }
}
