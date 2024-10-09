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
        [HttpPost]
        [Route("StartAddonv")]
        public IHttpActionResult StartAddon()
        {
            return Ok("Addon : Enable");
        }

        [HttpPost]
        [Route("MAdvancveFormByMemoIds")]
        public IHttpActionResult ValidatePurchase(GetMAdvancveFormByMemoIdRequestBody requestBody)
        {
            var result = AddonManager.MAdvancveFormByMemoIds(requestBody);
            return Ok(result);
        }
    }
}