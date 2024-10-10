using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using ThaiLifeAddon.Helpers;
using WolfApprove.API2.Extension;
using WolfApprove.Model;
using WolfApprove.Model.CustomClass;
using static ThaiLifeAddon.Helpers.WriteLogFile;

namespace ThaiLifeAddon.Managers
{
    public class OrderModel
    {
        public string Id { get; set; }
        public int Amount { get; set; }
    }

    public class AddonManager
    {
        public static List<GetMAdvancveFormByMemoIdResponeBody> MAdvancveFormByMemoIds(GetMAdvancveFormByMemoIdRequestBody body)
        {
            LogAddon($"Start MAdvancveFormByMemoIds");

            var result = new List<GetMAdvancveFormByMemoIdResponeBody>();

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
                        var srcOrder_List = AdvanceFormExt.GetDataTable(memo.MAdvancveForm, "รายการ");

                        var memoQuery = db.TRNMemoes.Where(x =>
                        refTemplateIds.Contains(x.TemplateId) &&
                        db.TRNMemoForms.Any(a => a.obj_label == selColumn && a.obj_value == documentNumber) &&
                        x.StatusName != Ext.Status._Cancelled &&
                        x.StatusName != Ext.Status._Rejected);

                        var memoRelate = memoQuery.ToList();
                        var initialMemo = memoRelate.OrderBy(x => x.CompletedDate).FirstOrDefault();
                        var memos = memoRelate.FindAll(x => x.MemoId != initialMemo.MemoId);
                        var order_List = AdvanceFormExt.GetDataTable(initialMemo.MAdvancveForm, "รายการ");
                        var id_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, "รายการ").FirstOrDefault(x => x.Name == "id").Index;
                        var amount_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, "รายการ").FirstOrDefault(x => x.Name == "จำนวน").Index;

                        var initial_Orders = new List<OrderModel>();
                        foreach (var order in order_List.row)
                        {
                            initial_Orders.Add(new OrderModel
                            {
                                Id = order[id_Index].value,
                                Amount = int.Parse(order[amount_Index].value),
                            });
                        }

                        var total_Orders = new List<OrderModel>();

                        foreach (var memoItem in memos)
                        {
                            var memoItem_order_List = AdvanceFormExt.GetDataTable(initialMemo.MAdvancveForm, "รายการ").row;
                            var memoItem_id_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, "รายการ")?.FirstOrDefault(x => x.Name == "id").Index;
                            var memoItem_amount_Index = AdvanceFormExt.GetAllColumnIndexTable(initialMemo.MAdvancveForm, "รายการ")?.FirstOrDefault(x => x.Name == "จำนวน").Index;

                            var memoItem_initial_Orders = new List<OrderModel>();
                            foreach (var order in memoItem_order_List)
                            {
                                total_Orders.Add(new OrderModel
                                {
                                    Id = order[id_Index].value,
                                    Amount = int.Parse(order[amount_Index].value),
                                });
                            }
                        }

                        total_Orders = total_Orders
                            .GroupBy(x => x.Id)
                            .Select(g => new OrderModel
                            {
                                Id = g.Key,
                                Amount = g.Sum(x => x.Amount)
                            })
                            .ToList();

                        var orderDifferences = total_Orders
                            .Join(initial_Orders,
                                  total => total.Id,
                                  initial => initial.Id,
                                  (total, initial) => new OrderModel
                                  {
                                      Id = total.Id,
                                      Amount = initial.Amount - total.Amount,
                                  })
                            .ToList();

                        var removeOders = orderDifferences.FindAll(x => x.Amount <= 0);

                        foreach (var od in orderDifferences)
                        {
                            var rowIndex = AdvanceFormExt.FindRowIndexById(order_List, od.Id);
                            if (rowIndex == -1) continue;
                            order_List = AdvanceFormExt.ReplaceValueInTable(order_List, rowIndex, amount_Index, od.Amount.ToString());
                        }

                        foreach(var od in removeOders)
                        {
                            order_List = AdvanceFormExt.RemoveRowById(order_List, od.Id);
                        }

                        memo.MAdvancveForm = AdvanceFormExt.ReplaceDataTable(memo.MAdvancveForm, JObject.Parse(JsonConvert.SerializeObject(order_List)), "รายการ");
                    }
                }
            }

            return result;
        }
    }
}