using System;
using System.ServiceModel;

namespace ServiceToServiceHost
{
    /// <summary>
    /// ������ ����������
    /// </summary>
    public interface IConnectionData<TData>
    {
        /// <summary>
        /// ���������������� ������ ����������
        /// </summary>
        TData Data { get; set; }

        /// <summary>
        /// ����� ���������� �����
        /// </summary>
        HostAdress RemoteHostAdress { get; }
    }

    /// <summary>
    /// ��������� ����������
    /// </summary>
    /// <typeparam name="TImplementedContract"></typeparam>
    /// <typeparam name="TData"></typeparam>
    public interface IOutcomingConnection<TData, TImplementedContract> : IConnectionData<TData>
    {
        /// <summary>
        /// ����������� � ���������� �����
        /// </summary>
        IConnectionToRemoteHost<TImplementedContract> Outcoming { get; set; }
    }

    /// <summary>
    /// �������� ����������
    /// </summary>
    public interface IIncomingConnection<TData> : IConnectionData<TData>
    {
        /// <summary>
        /// �������� ���������� � ���������� Host(��) � ����������
        /// </summary>
        OperationContext Incoming { get; set; }
    }

    /// <summary>
    /// ����������
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <typeparam name="TImplementedContract"></typeparam>
    public interface IConnection<TData, TImplementedContract> : IOutcomingConnection<TData, TImplementedContract>, IIncomingConnection<TData>, IDisposable
    {
        IncomingOperation IncomingOperation { get; set; }
    }

    internal class Connection<TData, TImplementedContract> : IConnection<TData, TImplementedContract>
    {
        /// <summary>
        /// �������� ���������� � ���������� Host(��) � ����������
        /// </summary>
        public OperationContext Incoming { get; set; }

        /// <summary>
        /// ����� ���������� �����
        /// </summary>
        public HostAdress RemoteHostAdress { get; set; }

        public IServiceSafeMethodCall<TImplementedContract> Outcoming0
        {
            get { return Outcoming.Connect; }
        }

        /// <summary>
        /// ��������� ���������� � ���������� Host(��)
        /// </summary>
        public IConnectionToRemoteHost<TImplementedContract> Outcoming { get; set; }

        /// <summary>
        /// �������������� ������ ����������
        /// </summary>
        public TData Data { get; set; }

        /// <summary>
        /// ������ �������� ��������
        /// </summary>
        public IncomingOperation IncomingOperation { get; set; }

        public void Dispose()
        {
            if (Outcoming != null)
            {
                var disp = Outcoming.Connect as IDisposable;
                if (disp != null)
                    disp.Dispose();
            }

            if (Incoming != null)
                Incoming.Channel.Abort();

            var dispose = Data as IDisposable;
            if (dispose != null)
                dispose.Dispose();
        }
    }
}