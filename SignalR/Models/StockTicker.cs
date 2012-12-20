using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNet.SignalR.Hubs;

namespace Microsoft.AspNet.SignalR.StockTicker
{
    public class StockTicker
    {
        private readonly static Lazy<StockTicker> _instance = new Lazy<StockTicker>(() => new StockTicker());
        private readonly static object _marketStateLock = new object();
        private readonly ConcurrentDictionary<string, Stock> _stocks = new ConcurrentDictionary<string, Stock>();
        private readonly double _rangePercent = .252; //stock can go up or down by a percentage of this factor on each change
      
        private readonly object _updateStockPricesLock = new object();
        private bool _updatingStockPrices = false;
        private readonly Random _updateOrNotRandom = new Random();
        private MarketState _marketState = MarketState.Closed;
        private readonly Lazy<IHubConnectionContext> _clientsInstance = new Lazy<IHubConnectionContext>(() => GlobalHost.ConnectionManager.GetHubContext<StockTickerHub>().Clients);

        private StockTicker()
        {
            LoadDefaultStocks();
        }

        public static StockTicker Instance
        {
            get
            {
                return _instance.Value;
            }
        }

        private IHubConnectionContext Clients
        {
            get { return _clientsInstance.Value; }
        }

        public MarketState MarketState
        {
            get { return _marketState; }
            private set { _marketState = value; }
        }

        public IEnumerable<Stock> GetAllStocks()
        {
            return _stocks.Values;
        }

        public void OpenMarket()
        {
            if (MarketState != MarketState.Open || MarketState != MarketState.Opening)
            {
                lock (_marketStateLock)
                {
                    if (MarketState != MarketState.Open || MarketState != MarketState.Opening)
                    {
                        MarketState = MarketState.Opening;
                        MarketState = MarketState.Open;
                        BroadcastMarketStateChange(MarketState.Open);
                    }
                }
            }
        }

        public void CloseMarket()
        {
            if (MarketState == MarketState.Open || MarketState == MarketState.Opening)
            {
                lock (_marketStateLock)
                {
                    if (MarketState == MarketState.Open || MarketState == MarketState.Opening)
                    {
                        MarketState = MarketState.Closing;
                        MarketState = MarketState.Closed;
                        BroadcastMarketStateChange(MarketState.Closed);
                    }
                }
            }
        }

        public void Reset()
        {
            lock (_marketStateLock)
            {
                if (MarketState != MarketState.Closed)
                {
                    throw new InvalidOperationException("Market must be closed before it can be reset.");
                }
                _stocks.Clear();
                LoadDefaultStocks();
                BroadcastMarketStateChange(MarketState.Reset);
            }
        }

        private void LoadDefaultStocks()
        {
            new List<Stock>
            {
                new Stock { Symbol = "MSFT", Price = 30.31m },
                new Stock { Symbol = "APPL", Price = 578.18m },
                new Stock { Symbol = "GOOG", Price = 570.30m }
            }.ForEach(stock => _stocks.TryAdd(stock.Symbol, stock));
        }

        public void UpdateStockPrices(object state)
        {
            // This function must be re-entrant as it's running as a timer interval handler
            if (_updatingStockPrices)
            {
                return;
            }

            lock (_updateStockPricesLock)
            {
                if (!_updatingStockPrices)
                {
                    _updatingStockPrices = true;

                    foreach (var stock in _stocks.Values)
                    {
                        if (UpdateStockPrice(stock))
                        {
                            BroadcastStockPrice(stock);
                        }
                    }

                    _updatingStockPrices = false;
                }
            }
        }

        private bool UpdateStockPrice(Stock stock)
        {
            // Randomly choose whether to udpate this stock or not
            var r = _updateOrNotRandom.NextDouble();
            if (r > .5)
            {
                return false;
            }

            // Update the stock price by a random factor of the range percent
            var random = new Random((int)Math.Floor(stock.Price));
            var percentChange = random.NextDouble() * _rangePercent;
            var pos = random.NextDouble() > .51;
            var change = Math.Round(stock.Price * (decimal)percentChange, 2);
            change = pos ? change : -change;

            stock.Price += change;
            return true;
        }

        private void BroadcastMarketStateChange(MarketState marketState)
        {
            switch (marketState)
            {
                case MarketState.Open:
                    Clients.All.marketOpened();
                    break;
                case MarketState.Closed:
                    Clients.All.marketClosed();
                    break;
                case MarketState.Reset:
                    Clients.All.marketReset();
                    break;
                default:
                    break;
            }
        }

        private void BroadcastStockPrice(Stock stock)
        {
            Clients.All.updateStockPrice(stock);
        }

        public void SendUserMessage(string uId, string msg)
        {
            Clients.Client(uId).postMessage(msg);
        }

        public void SendAllUsersExceptMessage(string uId, string msg)
        {
            Clients.AllExcept(new string[]{uId}).postMessage(msg);
        }

        public void UserConnected(string uId)
        {
            SendUserMessage(uId, "You connected");
            SendAllUsersExceptMessage(uId, uId + " connected");
        }


        public void UserDisconnected(string uId)
        {
            SendAllUsersExceptMessage(uId, uId + " disconnected");
            DisconnectCursor(uId);
        }


        public void UserReconnected(string uId)
        {
            SendUserMessage(uId, "You reconnected");
            SendAllUsersExceptMessage(uId, uId + " reconnected");
        }

        internal void ShareCursor(string uId, int x, int y)
        {
           //SendUserMessage(uId, "You moved yer mouse: "+x+" "+y);
           Clients.AllExcept(new string[]{uId}).updateSharedCursor(uId, x, y);
        }


        internal void DisconnectCursor(string uId)
        {
            Clients.All.deleteSharedCursor(uId);
        }

        internal void SendWave(string uId, string wave, bool complete)
        {
            Clients.AllExcept(new string[]{uId}).catchWave(uId, wave, complete);
        }

        internal void SendClick(string uId, int x, int y)
        {
            Clients.All.receiveClick(x, y);
        }
    }

    public enum MarketState
    {
        Open,
        Opening,
        Closing,
        Closed,
        Reset
    }
}