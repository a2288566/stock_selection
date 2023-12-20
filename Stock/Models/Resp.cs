using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Stock.Models
{
    public class Resp
    {
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        public List<StockRow> stockList { get; set; } = new List<StockRow>();
    }

    public class StockRow
    { 
        public string name { get; set; }
        public string number { get; set; }
        public decimal closeP { get; set; }
        public decimal turnover { get; set; }
    }

    public class UpdateDBResp : Resp
    {
        public  List<string> lastMonNoInfoStock { get; set; } = new List<string>();
    }
}