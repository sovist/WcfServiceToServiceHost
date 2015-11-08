using System;
using Ninject;

namespace ServiceToServiceHost
{
    /// <summary>
    /// HostManager
    /// </summary>
    /// <typeparam name="TService">Пользовательский сервис, наследуется от BaseService</typeparam>
    /// <typeparam name="TImplementedContract">Реализуемый контракт</typeparam>
    /// <typeparam name="TConnectionData">Данные которые асоциируются с каждым подключением</typeparam>
    public interface IHostManager<TService, TImplementedContract, TConnectionData>
    {
        /// <summary>
        /// Хост
        /// </summary>
        IHost Host { get; }

        /// <summary>
        /// Происходит при новом входящем соединении
        /// </summary>
        event Action<NewIcomingConnectionEventArgs<TConnectionData>> IcomingConnection;
        /// <summary>
        /// Происходит при новом подключении
        /// </summary>
        event Action<IConnectionData<TConnectionData>> Reconnect;
        /// <summary>
        /// Происходит при разрыве связи
        /// </summary>
        event Action<IConnectionData<TConnectionData>> LostConnection;

        /// <summary>
        /// Подключится к удаленному хосту
        /// </summary>
        /// <param name="remoteHostAdress">Адрес удаленного хоста</param>
        /// <param name="incomingOperation">Статус Входящих операций</param>
        /// <param name="connectionData">Данные пользователя</param>
        void CreateNewConnectToRemoteHost(HostAdress remoteHostAdress, IncomingOperation incomingOperation, TConnectionData connectionData);

        /// <summary>
        /// Удалить соединение к удаленному хосту
        /// </summary>
        /// <param name="predicate">Условие удаления</param>
        void RemoveConnectToRemoteHost(Predicate<IConnectionData<TConnectionData>> predicate);

        /// <summary>
        /// Выполнить метод на удаленном хосте
        /// </summary>
        /// <param name="predicate">Условие вызова</param>
        /// <param name="action">Действие</param>
        void CallRemoteServiceMethod(Predicate<IConnectionData<TConnectionData>> predicate, Action<IOutcomingConnection<TConnectionData, TImplementedContract>> action);
    }


    /// <summary>
    /// Статус Входящих операций
    /// </summary>
    [Flags]
    public enum IncomingOperation
    {
        /// <summary>
        /// Запретить
        /// </summary>
        NotAllow,

        /// <summary>
        /// Разрешить
        /// </summary> 
        Allow
    }

    /// <summary>
    /// Данные входящего соединения
    /// </summary>
    public class NewIcomingConnectionEventArgs<TConnectionData>
    {
        /// <summary>
        /// Статус Входящих операций
        /// </summary>
        public IncomingOperation IncomingOperation { get; set; }

        /// <summary>
        /// Пользовательские данные соединения
        /// </summary>
        public IConnectionData<TConnectionData> ConnectionData { get; private set; }

        /// <summary>
        /// Подключится к этому удаленному хосту
        /// </summary>
        public bool CreateConnectionToThisRemoteHost { get; set; }

        public NewIcomingConnectionEventArgs(IConnectionData<TConnectionData> connectionData)
        {
            ConnectionData = connectionData;
        }
    }

    public static class HostManagerFactory
    {
        /// <summary>
        /// Создает HostManager
        /// </summary>
        /// <typeparam name="TService">Пользовательский сервис, наследуется от BaseService</typeparam>
        /// <typeparam name="TImplementedContract">Реализуемый контракт</typeparam>
        /// <typeparam name="TConnectionData">Данные которые асоциируются с каждым подключением</typeparam>
        /// <param name="hostingPort">Порт который прослушивает Host</param>
        /// <returns>HostManager</returns>
        public static IHostManager<TService, TImplementedContract, TConnectionData> Create
            <TService, TImplementedContract, TConnectionData>(string hostingPort) 
            where TService : BaseService<TService, TImplementedContract, TConnectionData>, TImplementedContract
        {
            return new HostManager<TService, TImplementedContract, TConnectionData>(hostingPort);
        }

        /// <summary>
        /// Создает HostManager
        /// </summary>
        /// <typeparam name="TService">Пользовательский сервис, наследуется от BaseService</typeparam>
        /// <typeparam name="TImplementedContract">Реализуемый контракт</typeparam>
        /// <typeparam name="TConnectionData">Данные которые асоциируются с каждым подключением</typeparam>
        /// <param name="hostingPort">Порт который прослушивает Host</param>
        /// <param name="ninjectKernel">NinjectKernel для регистрации IHostManager</param>
        /// <returns>HostManager</returns>
        public static IHostManager<TService, TImplementedContract, TConnectionData> Create
            <TService, TImplementedContract, TConnectionData>(string hostingPort, IKernel ninjectKernel)
            where TService : BaseService<TService, TImplementedContract, TConnectionData>, TImplementedContract
        {
            return new HostManager<TService, TImplementedContract, TConnectionData>(hostingPort, ninjectKernel);
        }
    }
}