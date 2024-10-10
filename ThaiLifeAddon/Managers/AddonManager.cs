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

                            var memoQuery = db.TRNMemoes.Where(x =>
                            (db.TRNMemoForms.Any(a => a.obj_label == "เลขที่ใบขอซื้อ" && a.obj_value == documentNumber || a.obj_value == memo.DocumentNo) || x.DocumentNo == memo.DocumentNo) &&
                            x.StatusName != Ext.Status._Cancelled &&
                            x.StatusName != Ext.Status._Rejected &&
                            x.StatusName != Ext.Status._Draft);

                            var memoRelate = memoQuery.ToList();
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

                            LogAddon($"initial_Orders : {order_List.ToJson()}");

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

                            LogAddon($"total_Orders : {total_Orders.ToJson()}");

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

                            foreach (var od in removeOders)
                            {
                                order_List = AdvanceFormExt.RemoveRowById(order_List, od.Id);
                            }

                            LogAddon($"result OrderList : {order_List.ToJson()}");
                            memo.MAdvancveForm = AdvanceFormExt.ReplaceDataTable(memo.MAdvancveForm, JObject.Parse(JsonConvert.SerializeObject(order_List)), "รายการ");
                        }
                    }
                    catch(Exception ex)
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