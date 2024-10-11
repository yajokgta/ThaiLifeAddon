using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using ThaiLifeAddon.Helpers;
using WolfApprove.API2.Extension;
using WolfApprove.Model;
using WolfApprove.Model.CustomClass;
using WolfApprove.Model.Extension;
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
        public static List<string> _Document = new List<string>()
        {
            "PO CC",
            "PO CL",
            "PO01",
            "PO02",
            "PO03",
            "PO-Cat"
        };

        public class MAdvancveFormResponeModel
        {
            public int MemoId { get; set; }
            public string MAdvancveForm { get; set; }
            public string DocumentNo { get; set; }
        }

        public static List<MAdvancveFormResponeModel> MAdvancveFormByMemoIds(GetMAdvancveFormByMemoIdRequestBody body)
        {
            LogAddon($"Start MAdvancveFormByMemoIds");

            var result = new List<MAdvancveFormResponeModel>();

            using (WolfApproveModel db = DBContext.OpenConnection(body.connectionString))
            {
                var objTemplate = db.MSTTemplates.Where(x => _Document.Contains(x.DocumentCode));
                var refTemplateIds = new List<int>();
                foreach (var template in objTemplate)
                {
                    if (!string.IsNullOrEmpty(template.RefTemplate))
                    {
                        var listRefTemplate = JsonConvert.DeserializeObject<List<CustomTemplate>>(template.RefTemplate);
                        refTemplateIds.AddRange(listRefTemplate.Select(s => s.TemplateId ?? 0).ToList());
                    }
                }

                result = db.TRNMemoes.Where(x => body.MemoIds.Contains(x.MemoId)).Select(x => new MAdvancveFormResponeModel
                {
                    MemoId = x.MemoId,
                    MAdvancveForm = x.MAdvancveForm,
                    DocumentNo = x.DocumentNo,
                }).ToList();

                LogAddon($"result count : {result.Count}");

                foreach (var memo in result)
                {
                    try
                    {
                        if (db.TRNMemoes.Any(x => memo.MemoId == x.MemoId && refTemplateIds.Contains(x.TemplateId ?? 0)))
                        {
                            var documentNumber = ReserveCarExt.getValueAdvanceForm(memo.MAdvancveForm, "เลขที่ใบขอซื้อ");
                            var srcOrder_List = AdvanceFormExt.GetDataTable(memo.MAdvancveForm, "รายการ");
                            var memoRelate = db.TRNReferenceDocs.Where(x => x.MemoRefDocID == memo.MemoId)
                                .Join(db.TRNMemoes,
                                rm => rm.MemoID,
                                m => m.MemoId,
                                (rm, m) => m).ToList();
                            LogAddon($"memoRelate count : {memoRelate.Count}");

                            //var initialMemo = memoRelate.OrderBy(x => x.CompletedDate).FirstOrDefault();
                            var initialMemo = memo;

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

                            LogAddon($"initial_Orders : {initial_Orders.ToJson()}");

                            var total_Orders = new List<OrderModel>();

                            foreach (var memoItem in memos)
                            {
                                var memoItem_order_List = AdvanceFormExt.GetDataTable(memoItem.MAdvancveForm, "รายการ").row;
                                var memoItem_id_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, "รายการ")?.FirstOrDefault(x => x.Name == "id").Index ?? 0;
                                var memoItem_amount_Index = AdvanceFormExt.GetAllColumnIndexTable(memoItem.MAdvancveForm, "รายการ")?.FirstOrDefault(x => x.Name == "จำนวน").Index ?? 0;

                                var memoItem_initial_Orders = new List<OrderModel>();
                                foreach (var order in memoItem_order_List)
                                {
                                    total_Orders.Add(new OrderModel
                                    {
                                        Id = order[memoItem_id_Index].value,
                                        Amount = int.Parse(order[memoItem_amount_Index].value),
                                    });
                                }
                            }

                            total_Orders = total_Orders
                                .GroupBy(x => x.Id)
                                .Select(g =>
                                {
                                    var orderId = g.Key;
                                    var totalAmount = g.Sum(x => x.Amount);
                                    LogAddon($"Order ID: {orderId}, Total Amount: {totalAmount}");

                                    return new OrderModel
                                    {
                                        Id = orderId,
                                        Amount = totalAmount
                                    };
                                })
                                .ToList();

                            var orderDifferences = total_Orders
                                .Join(initial_Orders,
                                      total => total.Id,
                                      initial => initial.Id,
                                      (total, initial) =>
                                      {
                                          var orderId = total.Id;
                                          var totalAmount = total.Amount;
                                          var initialAmount = initial.Amount;
                                          var difference = initialAmount - totalAmount;

                                          LogAddon($"Order ID: {orderId}, Initial Amount: {initialAmount}, Total Amount: {totalAmount}, Difference: {difference}");

                                          return new OrderModel
                                          {
                                              Id = orderId,
                                              Amount = difference
                                          };
                                      })
                                .ToList();

                            var removeOders = orderDifferences.FindAll(x => x.Amount <= 0);
                            LogAddon($"result removeOders : {removeOders.ToJson()}");

                            foreach (var od in orderDifferences)
                            {
                                var rowIndex = AdvanceFormExt.FindRowIndexById(order_List, od.Id);
                                if (rowIndex == -1) continue;
                                order_List = AdvanceFormExt.ReplaceValueInTable(order_List, rowIndex, amount_Index, od.Amount.ToString());
                            }

                            foreach (var od in removeOders)
                            {
                                order_List = AdvanceFormExt.RemoveRowById(order_List, od.Id);
                            }

                            LogAddon($"result OrderList : {order_List.ToJson()}");
                            memo.MAdvancveForm = AdvanceFormExt.ReplaceDataTable(memo.MAdvancveForm, JObject.Parse(JsonConvert.SerializeObject(order_List)), "รายการ");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogAddon($"MemoId Error : {memo.MemoId}");
                        LogAddon($"{ex}");
                    }
                }
            }

            LogAddon($"Respone Count : {result.Count}");
            return result;
        }
    }
}