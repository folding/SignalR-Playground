using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.SignalR.Hubs;
using System.Threading.Tasks;

namespace Microsoft.AspNet.SignalR.StockTicker
{
    [HubName("stockTicker")]
    public class StockTickerHub : Hub
    {
        private readonly StockTicker _stockTicker;

        public MarketState MarketState { get { return _stockTicker.MarketState; } }

        public StockTickerHub() : this(StockTicker.Instance) { }

        public StockTickerHub(StockTicker stockTicker)
        {
            _stockTicker = stockTicker;
        }

        public override Task OnConnected()
        {
            var uId = Context.ConnectionId;
            _stockTicker.UserConnected(uId);
            return Clients.All.joined(uId, DateTime.Now.ToString());
        }

        public override Task OnDisconnected()
        {
            var uId = Context.ConnectionId;
            _stockTicker.UserDisconnected(uId);
            return Clients.All.leave(uId, DateTime.Now.ToString());
        }

        public override Task OnReconnected()
        {
            var uId = Context.ConnectionId;
            _stockTicker.UserReconnected(uId);
            return Clients.All.rejoined(uId, DateTime.Now.ToString());
        }

        public IEnumerable<Stock> GetAllStocks()
        {
            return _stockTicker.GetAllStocks();
        }

        public string GetMarketState()
        {
            var uId = Context.ConnectionId;
            _stockTicker.SendUserMessage(uId, "You got the market state");
            _stockTicker.SendAllUsersExceptMessage(uId, uId + " got the market state");
            return _stockTicker.MarketState.ToString();
        }

        public void OpenMarket()
        {
            var uId = Context.ConnectionId;            
            _stockTicker.OpenMarket();
            _stockTicker.SendUserMessage(uId, "You opened the market");
            _stockTicker.SendAllUsersExceptMessage(uId, uId + " opened the market");
        }

        public void CloseMarket()
        {
            var uId = Context.ConnectionId;
            _stockTicker.CloseMarket();
            _stockTicker.SendUserMessage(uId, "You closed the market");
            _stockTicker.SendAllUsersExceptMessage(uId, uId + " closed the market");
        }

        public void UpdateStocks()
        {
            _stockTicker.UpdateStockPrices(null);
        }

        public void UploadCursor(int x, int y)
        {
            var uId = Context.ConnectionId;
            _stockTicker.ShareCursor(uId,x, y);
        }

        public void SendMessage(string msg)
        {
            var uId = Context.ConnectionId;
            _stockTicker.SendAllUsersExceptMessage(uId, uId.Substring(0,6)+": "+msg);
            _stockTicker.SendUserMessage(uId,"You: "+msg);
        }

        public void SendWave(string wave,bool complete)
        {
            var uId = Context.ConnectionId;
            _stockTicker.SendWave(uId, wave, complete);
        }

        public void SendClick(int x, int y)
        {
            var uId = Context.ConnectionId;
            _stockTicker.SendClick(uId, x, y);
        }

        public void Reset()
        {
            var uId = Context.ConnectionId;
            _stockTicker.Reset();
            _stockTicker.SendUserMessage(uId, "You reset the market");
            _stockTicker.SendAllUsersExceptMessage(uId, uId+" reset the market");
        }
    }
}