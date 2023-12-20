using Stock.Models;
using Stock.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Stock.Services
{
    public class StockConditionServices
    {
        private readonly StockRepository _stockRepository;

        public StockConditionServices()
        {
            _stockRepository = new StockRepository();
        }

        public Resp LowerIn3Day(List<string> numberList, string customDays)
        {
            Resp resp = new Resp() { IsSuccess = true };
            List<StockColumn> priceList = new List<StockColumn>();
            //選出近20日
            foreach (var stock in numberList)
            {
                //篩出近三日最低價
                priceList = _stockRepository.GetCustomDaysInfo(stock, customDays);

                decimal minP = 0.0M;

                if (priceList.Count() != 0)
                {
                    try
                    {
                        decimal PriceLDay1 = priceList[0].lowP;
                        decimal PriceLDay2 = priceList[1].lowP;
                        decimal PriceLDay3 = priceList[2].lowP;
                        minP = Math.Min(PriceLDay1, PriceLDay2);
                        minP = Math.Min(minP, PriceLDay3);
                    }
                    catch (Exception ex)
                    {
                        resp.IsSuccess = false;
                        resp.Message = ex.Message;
                        return resp;
                    }

                    decimal[] priceLArr = new decimal[priceList.Count()];

                    for (int i = 0; i < priceList.Count(); i++)
                    {
                        priceLArr[i] = priceList[i].lowP;
                    }
                    Array.Sort(priceLArr);

                    if (minP <= priceLArr[0])
                    {
                        StockRow row = new StockRow();
                        row.name = priceList[0].name;
                        row.number = priceList[0].number;
                        row.closeP = priceList[0].closeP;
                        row.turnover = priceList[0].turnover;
                        resp.stockList.Add(row);
                    }
                }
            }
            return resp;
        }

        public Resp HigherIn3Days(List<string> numberList, string customDays)
        {
            Resp resp = new Resp() { IsSuccess = true };
            List<StockColumn> priceList = new List<StockColumn>();
            //選出近20日
            foreach (var stock in numberList)
            {
                priceList = _stockRepository.GetCustomDaysInfo(stock, customDays);
                decimal maxP = 0.0M;

                if (priceList.Count() != 0)
                {
                    try
                    {
                        //篩出近3日最高價
                        decimal PriceHDay1 = priceList[0].highP;
                        decimal PriceHDay2 = priceList[1].highP;
                        decimal PriceHDay3 = priceList[2].highP;
                        maxP = Math.Max(PriceHDay1, PriceHDay2);
                        maxP = Math.Max(maxP, PriceHDay3);
                    }
                    catch (Exception ex)
                    {
                        resp.IsSuccess = false;
                        resp.Message = ex.Message;
                        return resp;
                    }

                    decimal[] priceHArr = new decimal[priceList.Count()];

                    for (int i = 0; i < priceList.Count(); i++)
                    {
                        priceHArr[i] = priceList[i].highP;
                    }
                    Array.Sort(priceHArr);

                    if (maxP >= priceHArr[priceList.Count() - 1])
                    {
                        StockRow row = new StockRow();
                        row.name = priceList[0].name;
                        row.number = priceList[0].number;
                        row.closeP = priceList[0].closeP;
                        row.turnover = priceList[0].turnover;
                        resp.stockList.Add(row);
                    }
                }
            }

            return resp;
        }

        public Resp LargeTurnover(List<string> numberList)
        {
            Resp resp = new Resp() { IsSuccess = true };
            foreach (var stock in numberList)
            {
                List<StockColumn> priceList = new List<StockColumn>();
                //選出近20日
                priceList = _stockRepository.GetCustomDaysInfo(stock, "20");
                string stockDate = priceList[0].date.ToString("yyyy-MM-dd");
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                //if (stockDate == today)
                //{
                if (priceList.Count() != 0)
                {
                    //20天內的量
                    decimal[] stockTurnover = new decimal[priceList.Count()];
                    decimal turnoverSun = 0;
                    for (int i = 0; i < priceList.Count(); i++)
                    {
                        stockTurnover[i] = priceList[i].turnover;
                        turnoverSun += stockTurnover[i];
                    }
                    //當天量>平均的3倍
                    if (stockTurnover[0] > turnoverSun / priceList.Count() * 3)
                    {
                        StockRow row = new StockRow();
                        row.name = priceList[0].name;
                        row.number = priceList[0].number;
                        row.closeP = priceList[0].closeP;
                        row.turnover = priceList[0].turnover;
                        resp.stockList.Add(row);
                    }

                    //}
                }
            }
            return resp;
        }

        public Resp NearMonthlyAverage(List<string> numberList)
        {
            Resp resp = new Resp() { IsSuccess = true };
            List<StockColumn> stockInfo = new List<StockColumn>();
            foreach (var stock in numberList)
            {
                stockInfo = _stockRepository.GetStockInfoToday(stock);

                try
                {
                    if (stockInfo.Count() == 1)
                    {
                        decimal monthlyAverage = stockInfo[0].monthlyAverage.GetValueOrDefault();
                        //+-2%
                        decimal highPrice = monthlyAverage + monthlyAverage * 0.02m;
                        decimal lowPrice = monthlyAverage - monthlyAverage * 0.02m;

                        if (stockInfo[0].closeP > lowPrice && stockInfo[0].closeP < highPrice)
                        {
                            StockRow row = new StockRow();
                            row.name = stockInfo[0].name;
                            row.number = stockInfo[0].number;
                            row.closeP = stockInfo[0].closeP;
                            row.turnover = stockInfo[0].turnover;
                            resp.stockList.Add(row);
                        }
                    }
                }
                catch (Exception ex)
                {
                    resp.Message = "查詢股價靠近月均線股號:" + stock + "有誤";
                    resp.IsSuccess = false;
                }
            }
            return resp;
        }

        public Resp NearQuarterlyAverage(List<string> numberList)
        {
            Resp resp = new Resp() { IsSuccess = true };
            List<StockColumn> stockInfo = new List<StockColumn>();
            foreach (var stock in numberList)
            {
                stockInfo = _stockRepository.GetStockInfoToday(stock);

                try
                {
                    if (stockInfo.Count() == 1)
                    {
                        decimal quarterlyAverage = stockInfo[0].QuarterlyMovingAverage.GetValueOrDefault();
                        //+-2%
                        decimal highPrice = quarterlyAverage + quarterlyAverage * 0.02m;
                        decimal lowPrice = quarterlyAverage - quarterlyAverage * 0.02m;

                        if (stockInfo[0].closeP > lowPrice && stockInfo[0].closeP < highPrice)
                        {
                            StockRow row = new StockRow();
                            row.name = stockInfo[0].name;
                            row.number = stockInfo[0].number;
                            row.closeP = stockInfo[0].closeP;
                            row.turnover = stockInfo[0].turnover;
                            resp.stockList.Add(row);
                        }
                    }
                }
                catch (Exception ex)
                {
                    resp.Message = "查詢股價靠近季均線股號:" + stock + "有誤";
                    resp.IsSuccess = false;
                    return resp;
                }
            }
            return resp;
        }

        public Resp FarQuarterlyAverage(List<string> numberList)
        {
            Resp resp = new Resp() { IsSuccess = true };
            List<StockColumn> stockInfo = new List<StockColumn>();
            foreach (var stock in numberList)
            {
                try
                {
                    stockInfo = _stockRepository.GetStockInfoToday(stock);
                    if (stockInfo.Count() == 1)
                    {
                        decimal quarterlyAverage = stockInfo[0].QuarterlyMovingAverage.GetValueOrDefault();
                        if (quarterlyAverage != 0)
                        {
                            //+-15%
                            decimal todayPrice = stockInfo[0].closeP;
                            if (Math.Round(Math.Abs(todayPrice - quarterlyAverage), 2) / todayPrice > 0.15m)
                            {
                                StockRow row = new StockRow();
                                row.name = stockInfo[0].name;
                                row.number = stockInfo[0].number;
                                row.closeP = stockInfo[0].closeP;
                                row.turnover = stockInfo[0].turnover;
                                resp.stockList.Add(row);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    resp.Message = "查詢股價遠離季均線股號:" + stock + "有誤";
                    resp.IsSuccess = false;
                    return resp;
                }
            }
            return resp;
        }

        public Resp Nearby20DayHighPrice(List<string> numberList)
        {
            Resp resp = new Resp() { IsSuccess = true };
            List<StockColumn> priceList = new List<StockColumn>();
            //選出近20日
            foreach (var stock in numberList)
            {
                try
                {
                    priceList = _stockRepository.GetCustomDaysInfo(stock, "20");

                    decimal todayPrice = priceList[0].closeP;
                    decimal[] price20Days = new decimal[20];
                    for (int i = 0; i < priceList.Count(); i++)
                    {
                        //去掉前三天
                        if (i > 2)
                            price20Days[i] = priceList[i].highP;
                    }
                    Array.Sort(price20Days);

                    if (price20Days[19] * 0.98m < todayPrice && price20Days[19] * 1.02m > todayPrice)
                    {
                        List<StockColumn> stockInfo = new List<StockColumn>();
                        stockInfo = _stockRepository.GetStockInfoToday(stock);
                        if (stockInfo.Count() == 1)
                        {
                            StockRow row = new StockRow();
                            row.name = stockInfo[0].name;
                            row.number = stockInfo[0].number;
                            row.closeP = stockInfo[0].closeP;
                            row.turnover = stockInfo[0].turnover;
                            resp.stockList.Add(row);
                        }
                    }
                }
                catch (Exception ex)
                {
                    resp.Message = "股價在20日最高價附近 股號:" + stock + "有誤";
                    resp.IsSuccess = false;
                    return resp;
                }
            }
            return resp;
        }

        public Resp Nearby20DayLowPrice(List<string> numberList)
        {
            Resp resp = new Resp() { IsSuccess = true };
            List<StockColumn> priceList = new List<StockColumn>();
            //選出近20日
            foreach (var stock in numberList)
            {
                priceList = _stockRepository.GetCustomDaysInfo(stock, "20");

                decimal todayPrice = priceList[0].closeP;
                decimal[] price20Days = new decimal[20];
                for (int i = 0; i < priceList.Count(); i++)
                {
                    //去掉前三天
                    if (i > 2)
                        price20Days[i] = priceList[i].closeP;
                }
                Array.Sort(price20Days);

                if (price20Days[3] * 0.98m < todayPrice && price20Days[3] * 1.02m > todayPrice)
                {
                    List<StockColumn> stockInfo = new List<StockColumn>();
                    stockInfo = _stockRepository.GetStockInfoToday(stock);
                    if (stockInfo.Count() == 1)
                    {
                        StockRow row = new StockRow();
                        row.name = stockInfo[0].name;
                        row.number = stockInfo[0].number;
                        row.closeP = stockInfo[0].closeP;
                        row.turnover = stockInfo[0].turnover;
                        resp.stockList.Add(row);
                    }
                }
            }
            return resp;
        }

        //計算月線
        public decimal MonthlyAverage(List<StockColumn> get20DaysPriceList)
        {
            decimal monthlyAverage = 0;
            for (int i = 0; i < get20DaysPriceList.Count(); i++)
            {
                monthlyAverage += get20DaysPriceList[i].closeP;
            }
            monthlyAverage = monthlyAverage / 20;
            monthlyAverage = Math.Round(monthlyAverage, 2);
            return monthlyAverage;
        }

        //計算季線
        public decimal QuarterlyMovingAverage(List<StockColumn> get60DaysPriceList)
        {
            decimal quarterlyMovingAverage = 0;
            for (int i = 0; i < get60DaysPriceList.Count(); i++)
            {
                quarterlyMovingAverage += get60DaysPriceList[i].closeP;
            }
            quarterlyMovingAverage = quarterlyMovingAverage / 60;
            quarterlyMovingAverage = Math.Round(quarterlyMovingAverage, 2);
            return quarterlyMovingAverage;
        }
    }
}