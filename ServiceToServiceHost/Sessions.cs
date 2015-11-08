using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace ConsoleApplication2
{
    public class SessionData
    {
        public OperationContext OperationContext { get; set; }   
        public string FromAdress { get; set; }
    }

    public abstract class Ses
    {
        protected readonly Dictionary<string, SessionData> Sessions;
        public abstract void Add(string adress, OperationContext operationContext);

        protected Ses()
        {
            Sessions = new Dictionary<string, SessionData>();
        }
    }

    public class HostSessions : Ses
    {
        private readonly object _sync = new object();
        public override void Add(string fromPort, OperationContext operationContext)
        {
            if (operationContext == null)
                return;

            string sessionId = operationContext.SessionId;
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(fromPort))
                return;

            var ip = RequestIp(operationContext);
            var adressFrom = string.Format("{0}:{1}", ip, fromPort);
            Console.WriteLine("adressFrom: {0}", adressFrom);
            lock (_sync)
            {
                var session = Sessions.FirstOrDefault(_ => !string.IsNullOrEmpty(_.Value.FromAdress) && _.Value.FromAdress == adressFrom);
                if (session.Value != null)
                {
                    Console.WriteLine("Sessions contains key {0}, Host.Sessions.Count = {1}", sessionId, Sessions.Count);
                    var sessionData = session.Value;
                    try
                    {
                        sessionData.OperationContext.Channel.Abort();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                }

                operationContext.Channel.Faulted += channelOnFaulted;
                operationContext.Channel.Closing += channelOnClosing;

                Sessions.Add(sessionId, new SessionData { OperationContext = operationContext, FromAdress = adressFrom });
                Console.WriteLine("Register {0}, Host.Sessions.Count = {1}", sessionId, Sessions.Count);
            }
        }
        private void channelOnFaulted(object sender, EventArgs eventArgs)
        {
            removeSession(sender as IContextChannel);
        }
        private void channelOnClosing(object sender, EventArgs eventArgs)
        {
            removeSession(sender as IContextChannel);
        }
        private void removeSession(IContextChannel contextChannel)
        {
            if (contextChannel == null || string.IsNullOrEmpty(contextChannel.SessionId))
                return;

            lock (_sync)
            {
                contextChannel.Faulted -= channelOnFaulted;
                contextChannel.Closing -= channelOnClosing;
                Sessions.Remove(contextChannel.SessionId);
            }
        }

        public static string RequestIp(OperationContext operationContext)
        {
            var endpointProperty = operationContext.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
            var ip = endpointProperty != null ? endpointProperty.Address : string.Empty;
            if (string.IsNullOrEmpty(ip) || ip == "::1")
                return "localhost";

            return ip;
        }
    }
}