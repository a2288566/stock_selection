using Dapper;
using Stock.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace Stock.Repository
{
    public class StockRepository
    {
        private readonly string _connectString = "Data Source=DESKTOP-N890G0P;user id=sa;password=mssql;database=Stock;";

        //用代號找DB中的股名
        public string NumFindStockName(string number)
        {
            var sql =
                    @"
                SELECT * FROM Stock_code_name
                WHERE number = @number
                ";
            var parameters = new DynamicParameters();
            parameters.Add("number", number, System.Data.DbType.String);

            using (var conn = new SqlConnection(_connectString))
            {
                var result = conn.QueryFirstOrDefault(sql, parameters);
                return result.name;
            }
        }

        //檢查是否有重複資訊
        public bool CheckDBInfo(string number, DateTime date)
        {
            var sql =
            @"
            select * from Stock where number = @number and date = @date;
            ";

            var parameters = new DynamicParameters();
            parameters.Add("number", number, System.Data.DbType.String);
            parameters.Add("date", date, System.Data.DbType.DateTime);

            using (var conn = new SqlConnection(_connectString))
            {
                var result = conn.QueryFirstOrDefault(sql, parameters);
                //有資料就不更新
                if (result != null)
                    return false;
                return true;
            }
        }

        //第一次撈資料
        public IEnumerable<StockColumn> GetStockNum()
        {
            using (var conn = new SqlConnection(_connectString))
            {
                var result = conn.Query<StockColumn>("SELECT NUMBER FROM Stock_code_name");
                return result;
            }
        }


        //把股市資訊匯進資料表
        public int Insert(StockColumn stockColumn)
        {
            var sql =
                    @"
                INSERT INTO Stock 
                (
                    [number]
                   ,[name]
                   ,[openP]
                   ,[highP]
                   ,[lowP]
                   ,[closeP]
                   ,[turnover]
                   ,[monthlyAverage]
                   ,[QuarterlyMovingAverage]
                   ,[date]
                   ,[create_date]
                ) 
                VALUES 
                (
                    @number
                   ,@name
                   ,@openP
                   ,@highP
                   ,@lowP
                   ,@closeP
                   ,@turnover
                   ,@monthlyAverage
                   ,@QuarterlyMovingAverage
                   ,@date
                   ,@create_date
                );

                SELECT @@IDENTITY;
            ";

            using (var conn = new SqlConnection(_connectString))
            {
                var result = conn.Execute(sql, stockColumn);
                return result;
            }
        }

        //撈出所有股號
        public List<StockColumn> GetNumber()
        {
            var sql =
            @"
            select number from Stock_code_name ;
            ";

            using (var conn = new SqlConnection(_connectString))
            {
                var result = conn.Query<StockColumn>(sql);
                return (List<StockColumn>)result;
            }
        }


        //選當天以前幾筆
        public List<StockColumn> GetCustomDaysInfo(string number, string customDay)
        {
            string today = DateTime.Now.ToShortDateString();

            var sql =
            @"
            SELECT TOP(@customDay) *
            FROM Stock
            WHERE number = @number AND date <= @today 
            ORDER BY date DESC
            ";

            var parameters = new DynamicParameters();
            parameters.Add("number", number, System.Data.DbType.String);
            parameters.Add("today", today, System.Data.DbType.String);
            parameters.Add("customDay", customDay, System.Data.DbType.Int32);

            using (var conn = new SqlConnection(_connectString))
            {
                var result = conn.Query<StockColumn>(sql, parameters).ToList();
                return result;
            }
        }

        //撈月線資料
        public List<StockColumn> Get20DaysPrice(string number, DateTime date)
        {
            var sql =
            @"
            SELECT top 20*
            FROM Stock
            WHERE number = @number and date < @date 
            ORDER BY date DESC
            ";

            var parameters = new DynamicParameters();
            parameters.Add("number", number, System.Data.DbType.String);
            parameters.Add("date", date, System.Data.DbType.DateTime);

            using (var conn = new SqlConnection(_connectString))
            {
                var result = conn.Query<StockColumn>(sql, parameters);
                return (List<StockColumn>)result;
            }
        }


        //撈季線資料
        public List<StockColumn> Get60DaysPrice(string number, DateTime date)
        {
            var sql =
            @"
            SELECT top 60*
            FROM Stock
            WHERE number = @number and date < @date 
            ORDER BY date DESC
            ";

            var parameters = new DynamicParameters();
            parameters.Add("number", number, System.Data.DbType.String);
            parameters.Add("date", date, System.Data.DbType.DateTime);

            using (var conn = new SqlConnection(_connectString))
            {
                var result = conn.Query<StockColumn>(sql, parameters);
                return (List<StockColumn>)result;
            }
        }


        //算出近7日平均成交量
        public List<StockColumn> TurnoverAVG()
        {
            var sql =
            @"
                WITH NumberTurnover AS (
                    SELECT
                        number,
                        turnover,
                        ROW_NUMBER() OVER (PARTITION BY number ORDER BY Date DESC) AS rn
                    FROM Stock
                )
                , NumberAverage AS (
                    SELECT
                        number,
                        CAST(AVG(turnover) AS INT) AS turnover
                    FROM NumberTurnover
                    WHERE rn <= 7
                    GROUP BY number
                )
                SELECT
                    n.number,
                    n.turnover
                FROM NumberAverage n
                WHERE n.turnover > 1000
                ORDER BY n.number ASC
            ";

            using (var conn = new SqlConnection(_connectString))
            {
                var result = conn.Query<StockColumn>(sql);
                return (List<StockColumn>)result;
            }
        }

        //三個月更新一次更新表
        public List<StockColumn> UpdateTable()
        {
            var sql =
            @"
                select * from UpdateNum
            ";

            using (var conn = new SqlConnection(_connectString))
            {
                var result = conn.Query<StockColumn>(sql);
                return (List<StockColumn>)result;
            }
        }

        //抓當天股票資訊
        public List<StockColumn> GetStockInfoToday(string number)
        {
            string date = DateTime.Now.ToShortDateString();
            string sql = "";
            var parameters = new DynamicParameters();
            if (DateTime.Now < DateTime.Today.AddHours(15))
            {
                sql =
                @"
                    SELECT top(1) *from Stock
                    where number = @number
                    order by date desc
                ";

            }
            else
            {
                sql =
                @"
                SELECT * from Stock
                where date = @date and number = @number 
                ";
                parameters.Add("date", date, System.Data.DbType.String);
            }

            parameters.Add("number", number, System.Data.DbType.String);
            using (var conn = new SqlConnection(_connectString))
            {
                var result = conn.Query<StockColumn>(sql, parameters);
                return (List<StockColumn>)result;
            }
        }

        public bool DeleteLowerTurnoverStock(List<string> stockNumbers)
        {
            var sql =
            @"
            DELETE FROM Stock 
            WHERE number IN @numbers;
            ";

            var parameters = new DynamicParameters();
            parameters.Add("numbers", stockNumbers, System.Data.DbType.AnsiString);

            using (var conn = new SqlConnection(_connectString))
            {
                conn.Execute(sql, parameters);
                return true;
            }
        }

    }
}