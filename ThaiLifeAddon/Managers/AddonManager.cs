using System;
using System.Collections.Generic;
using System.Linq;
using WolfApprove.API2.Extension;
using WolfApprove.Model.CustomClass;
using WolfApprove.Model;

namespace ThaiLifeAddon.Managers
{
    public class AddonManager
    {
        public static List<GetMAdvancveFormByMemoIdResponeBody> MAdvancveFormByMemoIds(GetMAdvancveFormByMemoIdRequestBody body)
        {
            var result = new List<GetMAdvancveFormByMemoIdResponeBody>();

            try
            {

                using (WolfApproveModel db = DBContext.OpenConnection(body.connectionString))
                {
                    result = db.TRNMemoes.Where(x => body.MemoIds.Contains(x.MemoId)).Select(x => new GetMAdvancveFormByMemoIdResponeBody { MemoId = x.MemoId, MAdvancveForm = x.MAdvancveForm }).ToList();
                }
            }

            catch
            {

            }
            return result;
        }
    }
}
