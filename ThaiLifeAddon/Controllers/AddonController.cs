using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using ThaiLifeAddon.Models.RequestModel;
using WolfApprove.API2.Controllers.Services;
using WolfApprove.Model.CustomClass;
using ThaiLifeAddon.Managers;

namespace ThaiLifeAddon
{
    [RoutePrefix("api/v1/addonThaiLife")]
    public class AddonController : ApiController
    {
        public AddonController()
        {

        }

        [HttpPost]
        [Route("MAdvancveFormByMemoIds")]
        public IHttpActionResult ValidatePurchase(GetMAdvancveFormByMemoIdRequestBody requestBody)
        {
            var result = AddonManager.MAdvancveFormByMemoIds(requestBody);
            return Ok(result);
        }

        [HttpGet]
        [Route("StartAddonv")]
        public IHttpActionResult StartAddon(GetMAdvancveFormByMemoIdRequestBody requestBody)
        {
            var result = AddonManager.MAdvancveFormByMemoIds(requestBody);
            return Ok(result);
        }
    }
}