using System;
using System.Collections.Generic;
using System.Linq;
using WolfApprove.API2.Extension;
using WolfApprove.Model.CustomClass;
using WolfApprove.Model;
using Newtonsoft.Json;

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
                    var objTemplate = db.MSTTemplates.AsNoTracking().FirstOrDefault(x => x.DocumentCode == "PO03");
                    var listRefTemplate = new List<CustomTemplate>();
                    if (!string.IsNullOrEmpty(objTemplate.RefTemplate))
                    {
                        listRefTemplate = JsonConvert.DeserializeObject<List<CustomTemplate>>(objTemplate.RefTemplate);
                    }
                    var refTemplateIds = listRefTemplate.Select(x => x.TemplateId).ToList();

                    result = db.TRNMemoes.Where(x => body.MemoIds.Contains(x.MemoId)).Select(x => new GetMAdvancveFormByMemoIdResponeBody { MemoId = x.MemoId, MAdvancveForm = x.MAdvancveForm }).ToList();

                    foreach (var memo in result)
                    {
                        if (db.TRNMemoes.Any(x => memo.MemoId == x.MemoId && refTemplateIds.Contains(x.TemplateId)))
                        {
                            var selColumn = objTemplate.RefDocDisplay.Split(',')[2];
                            var documentNumber = ReserveCarExt.getValueAdvanceForm(memo.MAdvancveForm, selColumn);

                            var memoQuery = db.TRNMemoes.Where(x => refTemplateIds.Contains(x.TemplateId) && db.TRNMemoForms.Any(a => a.obj_value == documentNumber));

                            var initialMemo = memoQuery.OrderByDescending(x => x.CompletedDate).FirstOrDefault();
                        }
                    }
                }
            }

            catch
            {

            }
            return result;
        }
    }
}
