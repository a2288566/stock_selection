
using Dapper;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Stock.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using Stock.Repository;
using Stock.Services;

class CatchStockInfo
{
    private static Logger logger = LogManager.GetCurrentClassLogger();

    static void Main(string[] args)
    {
        //證交所撈資料要帶股號,所以資料庫有先設好股號去撈
        StockRepository stockRepository = new StockRepository();
        StockConditionServices stockConditionServices = new StockConditionServices();
        List<string> lowerTurnoverList = new List<string>();
        //先固定更新這些股票
        List<StockColumn> turnoverAVG = stockRepository.UpdateTable();

        List<string> numberList = new List<string>();
        string today = DateTime.Now.ToShortDateString();
        today = "20231203";
        for (int i = 0; i < turnoverAVG.Count; i++)
        {
            //if(Int32.Parse(turnoverAVG[i].number) > 2610)
            numberList.Add(turnoverAVG[i].number);
        }
        int updateCount = 0;
        logger.Info(today + "更新資料數:" + numberList.Count());
        Console.WriteLine("日期: " + today + " 更新資料數: " + numberList.Count());

        foreach (var stockNum in numberList)
        {
            stockJson jsonResult = new stockJson();
            try
            {
                jsonResult = CallStockExchangeApi(jsonResult, today, stockNum);
            }
            catch (Exception ex)
            {
                Console.WriteLine(stockNum + "連線有誤" + ex.Message);
                logger.Info(stockNum + "連線有誤" + ex.Message);
                return;
            }

            var stockColumn = new List<StockColumn>();
            try
            {
                stockColumn = GetStockInfo(jsonResult, stockNum, stockRepository, lowerTurnoverList);
            }
            catch (Exception ex)
            {
                Console.WriteLine(stockNum + "錯誤, 錯誤訊息:" + ex.Message);
                logger.Info(stockNum + "錯誤, 錯誤訊息:" + ex.Message);
            }

            //更新資料庫
            try
            {
                updateDB(stockColumn, stockRepository, stockConditionServices);
                updateCount++;
                Console.WriteLine("第" + updateCount + "個: " + stockNum + "更新成功" + DateTime.Now.ToString("HH:mm:ss"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("錯誤股號:" + stockColumn[0].number + "   資料庫更新錯誤錯誤訊息:" + ex.Message);
                logger.Info("錯誤股號:" + stockColumn[0].number + "   資料庫更新錯誤錯誤訊息:" + ex.Message);
            }

            //刪除成交量小的股票
            try
            {
                foreach (var lowerTurnoverStock in lowerTurnoverList)
                { 
                    
                }
            }
            catch 
            {
            
            }
        }
        Console.WriteLine("更新完成");
        logger.Info(today + "更新完成");
    }

    public static stockJson CallStockExchangeApi(stockJson jsonResult, string today, string stockNum)
    {
        string url = "http://www.twse.com.tw/exchangeReport/STOCK_DAY?response=json&date=" + today + "&stockNo=" + stockNum.Trim();
        //打證交所api
        HttpClient httpClient = new();
        HttpResponseMessage httpResponseMessage = httpClient.GetAsync(url).Result;
        string content = httpResponseMessage.Content.ReadAsStringAsync().Result;
        jsonResult = JsonConvert.DeserializeObject<stockJson>(content);
        Thread.Sleep(7500);

        return jsonResult;
    }

    public static List<StockColumn> GetStockInfo(stockJson jsonResult, string stockNum, StockRepository stockRepository, List<string> lowerTurnoverList)
    {
        List<StockColumn> stockColumn = new List<StockColumn>();
        //查無股號
        if (jsonResult.stat == "OK")
        {
            foreach (var stock in jsonResult.data)
            {
                StockColumn row = new StockColumn();
                row.name = stockRepository.NumFindStockName(stockNum);
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

            decimal stockTurnoverAVG = (stockColumn[stockColumn.Count() - 1].turnover + stockColumn[stockColumn.Count() - 2].turnover + stockColumn[stockColumn.Count() - 3].turnover) / 3;
            if (stockTurnoverAVG < 1000)
            {
                lowerTurnoverList.Add(stockNum);
            }
        }

        return stockColumn;
    }

    public static void updateDB(List<StockColumn> stockColumn, StockRepository stockRepository, StockConditionServices stockConditionServices)
    {
        string stockNumber = "";
        foreach (var stock in stockColumn)
        {
            stockNumber = stock.number;
            //避免重複更新
            if (stockRepository.CheckDBInfo(stock.number, stock.date))
            {
                List<StockColumn> get20DaysPriceList = new List<StockColumn>();
                List<StockColumn> get60DaysPriceList = new List<StockColumn>();
                get20DaysPriceList = stockRepository.Get20DaysPrice(stock.number, stock.date);
                get60DaysPriceList = stockRepository.Get60DaysPrice(stock.number, stock.date);

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
                stockRepository.Insert(stock);
            }

        }
    }
}



