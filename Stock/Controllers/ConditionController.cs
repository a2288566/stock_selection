using Stock.Models;
using Stock.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Stock.Services;

namespace Stock.Controllers
{
    public class ConditionController : Controller
    {
        private readonly StockRepository _stockRepository;
        public ConditionController()
        {
            this._stockRepository = new StockRepository();
        }

        [HttpPost]
        public JsonResult condition(string condition, string customDays)
        {
            Resp resp = new Resp() { IsSuccess = true };
            StockConditionServices stockConditionServices = new StockConditionServices();

            //剔除成交量太小股票
            List<StockColumn> noSmallTradingStock = _stockRepository.UpdateTable();
            List<string> numberList = noSmallTradingStock.Select(stock => stock.number).ToList();

            //判斷篩選條件
            //創幾日新低
            if (condition == "lowerIn3Day")
            {
                resp = stockConditionServices.LowerIn3Day(numberList, customDays);
            }
            //創幾日新高
            if (condition == "higherIn3Day")
            {
                resp = stockConditionServices.HigherIn3Days(numberList, customDays);
            }
            if (condition == "largeTurnover")
            {
                resp = stockConditionServices.LargeTurnover(numberList);
            }
            if (condition == "nearMonthlyAverage")
            {
                resp = stockConditionServices.NearMonthlyAverage(numberList);
            }
            //股價靠近季均線
            if (condition == "nearQuarterlyAverage")
            {
                resp = stockConditionServices.NearQuarterlyAverage(numberList);
            }
            //股價遠離季均線
            if (condition == "farQuarterlyAverage")
            {
                resp = stockConditionServices.FarQuarterlyAverage(numberList);
            }
            ////股價在20日最高價附近
            //if (condition == "nearby20DayHighPrice")
            //{
            //    resp = stockConditionServices.Nearby20DayHighPrice(numberList);
            //}
            ////股價在20日最低價附近  
            //if (condition == "nearby20DayLowPrice")
            //{
            //    resp = stockConditionServices.Nearby20DayLowPrice(numberList);
            //}


            return Json(resp);
        }
    }
}