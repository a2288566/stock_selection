using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Stock.Models
{
    public class StockColumn
    {
        /// <summary>
        /// 股票代號
        /// </summary>
        public string number { get; set; }
        /// <summary>
        /// 股名
        /// </summary>
        public string name { get; set; }
        ///<summary>
        /// 開盤
        /// </summary>
        public decimal openP { get; set; }
        /// <summary>
        /// 最高價
        /// </summary>
        public decimal highP { get; set; }
        /// <summary>
        /// 最低價
        /// </summary>
        public decimal lowP { get; set; }
        /// <summary>
        /// 收盤價
        /// </summary>
        public decimal closeP { get; set; }
        /// <summary>
        /// 月均線
        /// </summary>
        public decimal? monthlyAverage { get; set; }
        /// <summary>
        /// 季均線
        /// </summary>
        public decimal? QuarterlyMovingAverage { get; set; }
        /// <summary>
        /// 成交量
        /// </summary>
        public decimal turnover { get; set; }
        /// <summary>
        /// 日期
        /// </summary>
        public DateTime date { get; set; }
        public DateTime create_date { get; set; }


    }
}