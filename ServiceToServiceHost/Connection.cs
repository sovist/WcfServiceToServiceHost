using System;
using System.ServiceModel;

namespace ServiceToServiceHost
{
    /// <summary>
    /// Данные соединения
    /// </summary>
    public interface IConnectionData<TData>
    {
        /// <summary>
        /// Пользовательские данные соединения
        /// </summary>
        TData Data { get; set; }

        /// <summary>
        /// Адрес удаленного хоста
        /// </summary>
        HostAdress RemoteHostAdress { get; }
    }

    /// <summary>
    /// Исходящее соединение
    /// </summary>
    /// <typeparam name="TImplementedContract"></typeparam>
    /// <typeparam name="TData"></typeparam>
    public interface IOutcomingConnection<TData, TImplementedContract> : IConnectionData<TData>
    {
        /// <summary>
        /// Подключение к удаленному хосту
        /// </summary>
        IConnectionToRemoteHost<TImplementedContract> Outcoming { get; set; }
    }

    /// <summary>
    /// Входящее соединение
    /// </summary>
    public interface IIncomingConnection<TData> : IConnectionData<TData>
    {
        /// <summary>
        /// входящее соединение с удаленного Host(та) к локальному
        /// </summary>
        OperationContext Incoming { get; set; }
    }

    /// <summary>
    /// Соединение
    /// </summary>
    /// <typeparam name="TData"></typeparam>
    /// <typeparam name="TImplementedContract"></typeparam>
    public interface IConnection<TData, TImplementedContract> : IOutcomingConnection<TData, TImplementedContract>, IIncomingConnection<TData>, IDisposable
    {
        IncomingOperationStatus IncomingOperationStatus { get; set; }
    }

    internal class Connection<TData, TImplementedContract> : IConnection<TData, TImplementedContract>
    {
        /// <summary>
        /// входящее соединение с удаленного Host(та) к локальному
        /// </summary>
        public OperationContext Incoming { get; set; }

        /// <summary>
        /// Адрес удаленного хоста
        /// </summary>
        public HostAdress RemoteHostAdress { get; set; }

        /// <summary>
        /// исходящее соединение к удаленному Host(ту)
        /// </summary>
        public IConnectionToRemoteHost<TImplementedContract> Outcoming { get; set; }

        /// <summary>
        /// дополнительные данные соединения
        /// </summary>
        public TData Data { get; set; }

        /// <summary>
        /// статус входящих операций
        /// </summary>
        public IncomingOperationStatus IncomingOperationStatus { get; set; }

        public void Dispose()
        {
            var disp = Outcoming?.Connect as IDisposable;
            disp?.Dispose();

            Incoming?.Channel.Abort();

            var dispose = Data as IDisposable;
            dispose?.Dispose();
        }
    }
}