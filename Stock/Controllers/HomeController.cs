using NLog;
using Stock.Models;
using Stock.Repository;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Stock.Services;
using NLog.Fluent;
using System.Web.Razor.Text;

namespace Stock.Controllers
{
    public class HomeController : Controller
    {

        private readonly StockRepository _stockRepository;
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public HomeController()
        {
            this._stockRepository = new StockRepository();
        }

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult updateDBM(string formDate, string formStockNumber)
        {
            Resp resp = new Resp() { IsSuccess = false };

            if (string.IsNullOrEmpty(formDate))
            {
                resp.Message = "請輸入日期";
                return Json(resp);
            }
            if (formDate.Length != 8)
            {
                resp.Message = "日期格式為YYYYMMDD";
                return Json(resp);
            }

            //證交所撈資料要帶股號,所以資料庫有先設好股號去撈
            List<string> numberList = updateStockNumber(formStockNumber);

            foreach (var stockNnum in numberList)
            {
                stockJson jsonResult = null;
                try
                {
                    jsonResult = CallStockExchangeApi(formDate, stockNnum);
                }
                catch (Exception ex)
                {
                    resp.Message = "連線有誤,錯誤訊息:" + ex.Message;
                    logger.Info(resp.Message);
                    return Json(resp);
                }

                List<StockColumn> stockColumn = new List<StockColumn>();
                try
                {
                    stockColumn = GetStockInfo(jsonResult, stockNnum);
                }
                catch (Exception ex)
                {
                    resp.Message = stockNnum + ex.Message;
                    logger.Info(resp.Message);
                    return Json(resp);
                }

                //更新資料庫
                resp = updateDB(stockColumn);
                if (!resp.IsSuccess)
                {
                    logger.Info(resp.Message);
                    return Json(resp);
                }

            }

            resp.IsSuccess = true;
            resp.Message = "更新成功";
            return Json(resp);
        }

        public List<string> updateStockNumber(string stockNumber)
        {
            List<string> numberList = new List<string>();
            if (String.IsNullOrEmpty(stockNumber))
            {
                //成交量太小就不做更新
                List<StockColumn> turnoverAVG = _stockRepository.TurnoverAVG();
                for (int i = 0; i < turnoverAVG.Count; i++)
                {
                    numberList.Add(turnoverAVG[i].number);
                }
            }
            else
                numberList.Add(stockNumber);

            return numberList;
        }

        public stockJson CallStockExchangeApi(string stockDate, string stockNum)
        {
            string url = "http://www.twse.com.tw/exchangeReport/STOCK_DAY?response=json&date=" + stockDate + "&stockNo=" + stockNum.Trim();
            stockJson jsonResult = new stockJson();

            //打證交所api
            using (var httpClient = new HttpClient())
            {
                var response = httpClient.GetAsync(url).Result;
                var content = response.Content.ReadAsStringAsync().Result;
                jsonResult = new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<stockJson>(content);
                Thread.Sleep(8000);
            }

            return jsonResult;
        }

        public List<StockColumn> GetStockInfo(stockJson jsonResult, string stockNum)
        {
            List<StockColumn> stockColumn = new List<StockColumn>();
            //查無股號
            if (jsonResult.stat == "OK")
            {
                foreach (var stock in jsonResult.data)
                {
                    StockColumn row = new StockColumn();
                    row.name = _stockRepository.NumFindStockName(stockNum);
                    row.number = stockNum;
                    CultureInfo culture = new CultureInfo("zh-TW");
                    culture.DateTimeFormat.Calendar = new TaiwanCalendar();
                    row.date = DateTime.Parse(stock[0], culture);

                    //遇到暫停交易的話
                    if (stock[4].Trim() == "--")
                        continue;
                    row.openP = decimal.Parse(stock[3]); //開盤價
                    row.highP = decimal.Parse(stock[4]); //最高價
                    row.lowP = decimal.Parse(stock[5]); //最低價
                    row.closeP = decimal.Parse(stock[6]); //收盤價
                    row.turnover = decimal.Parse(stock[1].Substring(0, stock[1].Length - 4).Replace(",", "")); //成交量
                    row.create_date = DateTime.Now;
                    stockColumn.Add(row);
                }
            }

            return stockColumn;
        }

        public Resp updateDB(List<StockColumn> stockColumn)
        {
            Resp resp = new Resp() { IsSuccess = false };
            try
            {
                foreach (var stock in stockColumn)
                {
                    if (_stockRepository.CheckDBInfo(stock.number, stock.date))
                    {
                        StockConditionServices stockConditionServices = new StockConditionServices();
                        List<StockColumn> get20DaysPriceList = new List<StockColumn>();
                        List<StockColumn> get60DaysPriceList = new List<StockColumn>();
                        get20DaysPriceList = _stockRepository.Get20DaysPrice(stock.number, stock.date);
                        get60DaysPriceList = _stockRepository.Get60DaysPrice(stock.number, stock.date);

                        //月均線
                        if (get20DaysPriceList.Count() == 20)
                        {
                            stock.monthlyAverage = stockConditionServices.MonthlyAverage(get20DaysPriceList);
                        }

                        //季均線
                        if (get60DaysPriceList.Count() == 60)
                        {
                            stock.QuarterlyMovingAverage = stockConditionServices.QuarterlyMovingAverage(get60DaysPriceList);
                        }
                        _stockRepository.Insert(stock);
                    }
                }
            }
            catch (Exception ex)
            {
                resp.Message = ex.Message;
                return resp;
            }

            resp.IsSuccess = true;
            return resp;
        }
    }
}