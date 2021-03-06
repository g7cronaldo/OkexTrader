﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OkexTrader.Common;
using OkexTrader.Trade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace OkexTrader.FutureTrade
{
    class FutureTradeEntity : TradeEntity
    {
        public delegate void TradeEventHandler(long orderID, TradeQueryResult result, OkexFutureOrderBriefInfo info);

        private event TradeEventHandler m_tradeEventHandler;        

        private OkexFutureInstrumentType m_instrument;
        private OkexFutureContractType m_contract;
        
        public FutureTradeEntity(OkexFutureInstrumentType instrument, OkexFutureContractType contract, long queryInterval = 1000) : base(queryInterval)
        {
            m_instrument = instrument;
            m_contract = contract;     
        }

        public void setTradeEventHandler(TradeEventHandler handler)
        {
            m_tradeEventHandler += handler;
        }

        public void onTradeEvent(long orderID, TradeQueryResult result, OkexFutureOrderBriefInfo info)
        {
            if (m_tradeEventHandler != null)
            {
                m_tradeEventHandler(orderID, result, info);
            }
        }

        protected override void onTradeOrdered(String str)
        {
            JObject jo = (JObject)JsonConvert.DeserializeObject(str);
            bool ret = (bool)jo["result"];
            if (!ret)
            {
                onTradeEvent(m_orderID, TradeQueryResult.TQR_Failed, null);
                return;
            }

            m_orderID = (long)jo["order_id"];
            start();
        }

        protected override void query()
        {
            lock (m_lock)
            {
                if (!m_valid)
                {
                    return;
                }
                if (m_orderID == 0)
                {
                    onTradeEvent(m_orderID, TradeQueryResult.TQR_Timeout, null);
                    return;
                }
                OkexFutureOrderBriefInfo info;
                if (m_resultTimer != null)
                {
                    m_resultTimer.Start();
                }
                bool ret = OkexFutureTrader.Instance.getOrderInfoByID(m_instrument, m_contract, m_orderID, out info);
                if (ret)
                {
                    if (m_resultTimer != null)
                    {
                        m_resultTimer.Stop();
                    }
                    TradeQueryResult tqr = getResultType(info.status);
                    onTradeEvent(m_orderID, tqr, info);
                    if (tqr == TradeQueryResult.TQR_Finished)
                    {
                        stop();
                    }
                }
            }
        }

        protected override void timeout()
        {
            onTradeEvent(m_orderID, TradeQueryResult.TQR_Timeout, null);
        }
 
    }
}
