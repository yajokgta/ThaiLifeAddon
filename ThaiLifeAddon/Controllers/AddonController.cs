using System.Web.Http;
using WolfApprove.Model.CustomClass;
using ThaiLifeAddon.Managers;

namespace ThaiLifeAddon
{
    [RoutePrefix("api/v1/addonThaiLife")]
    public class AddonController : ApiController
    {
        [HttpPost]
        [Route("StartAddon")]
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