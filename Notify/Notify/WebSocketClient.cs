using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Bitmex.Client.Websocket;
using Bitmex.Client.Websocket.Client;
using Bitmex.Client.Websocket.Requests;
using Bitmex.Client.Websocket.Websockets;

namespace Notify
{
    public class WebSocketClient
    {
        public static string message;

        private static readonly ManualResetEvent ExitEvent = new ManualResetEvent(false);

        public static void Run()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;

            var url = BitmexValues.ApiWebsocketUrl;
            //var url = BitmexValues.ApiWebsocketTestnetUrl;
            using (var communicator = new BitmexWebsocketCommunicator(url))
            {
                using (var client = new BitmexWebsocketClient(communicator))
                {

                    client.Streams.InfoStream.Subscribe(info =>
                    {
                        client.Send(new PingRequest());
                        client.Send(new TradesSubscribeRequest("XBTUSD"));
                    });

                    client.Streams.ErrorStream.Subscribe(x =>
                        message = $"Error received, message: {x.Error}, status: {x.Status}");

                    client.Streams.AuthenticationStream.Subscribe(x =>
                    {
                        message = $"Authentication happened, success: {x.Success}";
                        client.Send(new WalletSubscribeRequest());
                        client.Send(new MarginSubscribeRequest());
                        client.Send(new OrderSubscribeRequest());
                        client.Send(new PositionSubscribeRequest());
                    });

                    client.Streams.PongStream.Subscribe(x =>
                        message = $"Pong received ({x.Message})");

                    client.Streams.InstrumentStream.Subscribe(y =>
                        y.Data.ToList().ForEach(x =>
                           Console.Write(x.ToString()))
                        );

                    //client.Streams.TradesStream.Subscribe(y =>
                    //   y.Data.ToList().ForEach(x =>
                    //       Log.Information($"Trade {x.Symbol} executed. Time: {x.Timestamp:mm:ss.fff}, Amount: {x.Size}, " +
                    //                       $"Price: {x.Price}, Direction: {x.TickDirection}"))
                    //    );

                    //client.Streams.WalletStream.Subscribe(y =>
                    //   y.Data.ToList().ForEach(x =>
                    //       Log.Information($"-Wallet- {x.Amount} executed. Time: {x.Timestamp:mm:ss.fff}, Amount: {x.Account}, " +
                    //                       $"Price: {x.Addr}, Direction: {x.BalanceBtc}"))
                    //    );

                    //client.Streams.MarginStream.Subscribe(y =>
                    //   y.Data.ToList().ForEach(x =>
                    //       Log.Information($"-Margin- {x.Amount} executed. Time: {x.Timestamp:mm:ss.fff}, Amount: {x.Account}, " +
                    //                       $"Price: {x.WalletBalance}, Direction: {x.MarginBalance}"))
                    //    );

                    //client.Streams.OrderStream.Subscribe(y =>
                    //   y.Data.ToList().ForEach(x =>
                    //       Log.Information($"-Order- {x.AvgPx} executed. Time: {x.Timestamp:mm:ss.fff}, Amount: {x.Account}, " +
                    //                       $"Price: {x.CumQty}, Direction: {x.OrderQty}"))
                    //    );

                    communicator.Start();

                    ExitEvent.WaitOne();
                }
            }

        }

        private static void CurrentDomainOnProcessExit(object sender, EventArgs eventArgs)
        {
            ExitEvent.Set();
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            ExitEvent.Set();
        }
    }
}
