using System;
using Ninject;

namespace ServiceToServiceHost
{
    /// <summary>
    /// HostManager
    /// </summary>
    /// <typeparam name="TService">���������������� ������, ����������� �� BaseService</typeparam>
    /// <typeparam name="TImplementedContract">����������� ��������</typeparam>
    /// <typeparam name="TConnectionData">������ ������� ������������ � ������ ������������</typeparam>
    public interface IHostManager<TService, TImplementedContract, TConnectionData>
    {
        /// <summary>
        /// ����
        /// </summary>
        IHost Host { get; }

        /// <summary>
        /// ���������� ��� ����� �������� ����������
        /// </summary>
        event Action<NewIcomingConnectionEventArgs<TConnectionData>> IcomingConnection;
        /// <summary>
        /// ���������� ��� ����� �����������
        /// </summary>
        event Action<IConnectionData<TConnectionData>> Reconnect;
        /// <summary>
        /// ���������� ��� ������� �����
        /// </summary>
        event Action<IConnectionData<TConnectionData>> LostConnection;

        /// <summary>
        /// ����������� � ���������� �����
        /// </summary>
        /// <param name="remoteHostAdress">����� ���������� �����</param>
        /// <param name="incomingOperation">������ �������� ��������</param>
        /// <param name="connectionData">������ ������������</param>
        void CreateNewConnectToRemoteHost(HostAdress remoteHostAdress, IncomingOperation incomingOperation, TConnectionData connectionData);

        /// <summary>
        /// ������� ���������� � ���������� �����
        /// </summary>
        /// <param name="predicate">������� ��������</param>
        void RemoveConnectToRemoteHost(Predicate<IConnectionData<TConnectionData>> predicate);

        /// <summary>
        /// ��������� ����� �� ��������� �����
        /// </summary>
        /// <param name="predicate">������� ������</param>
        /// <param name="action">��������</param>
        void CallRemoteServiceMethod(Predicate<IConnectionData<TConnectionData>> predicate, Action<IOutcomingConnection<TConnectionData, TImplementedContract>> action);
    }


    /// <summary>
    /// ������ �������� ��������
    /// </summary>
    [Flags]
    public enum IncomingOperation
    {
        /// <summary>
        /// ���������
        /// </summary>
        NotAllow,

        /// <summary>
        /// ���������
        /// </summary> 
        Allow
    }

    /// <summary>
    /// ������ ��������� ����������
    /// </summary>
    public class NewIcomingConnectionEventArgs<TConnectionData>
    {
        /// <summary>
        /// ������ �������� ��������
        /// </summary>
        public IncomingOperation IncomingOperation { get; set; }

        /// <summary>
        /// ���������������� ������ ����������
        /// </summary>
        public IConnectionData<TConnectionData> ConnectionData { get; private set; }

        /// <summary>
        /// ����������� � ����� ���������� �����
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
        /// ������� HostManager
        /// </summary>
        /// <typeparam name="TService">���������������� ������, ����������� �� BaseService</typeparam>
        /// <typeparam name="TImplementedContract">����������� ��������</typeparam>
        /// <typeparam name="TConnectionData">������ ������� ������������ � ������ ������������</typeparam>
        /// <param name="hostingPort">���� ������� ������������ Host</param>
        /// <returns>HostManager</returns>
        public static IHostManager<TService, TImplementedContract, TConnectionData> Create
            <TService, TImplementedContract, TConnectionData>(string hostingPort) 
            where TService : BaseService<TService, TImplementedContract, TConnectionData>, TImplementedContract
        {
            return new HostManager<TService, TImplementedContract, TConnectionData>(hostingPort);
        }

        /// <summary>
        /// ������� HostManager
        /// </summary>
        /// <typeparam name="TService">���������������� ������, ����������� �� BaseService</typeparam>
        /// <typeparam name="TImplementedContract">����������� ��������</typeparam>
        /// <typeparam name="TConnectionData">������ ������� ������������ � ������ ������������</typeparam>
        /// <param name="hostingPort">���� ������� ������������ Host</param>
        /// <param name="ninjectKernel">NinjectKernel ��� ����������� IHostManager</param>
        /// <returns>HostManager</returns>
        public static IHostManager<TService, TImplementedContract, TConnectionData> Create
            <TService, TImplementedContract, TConnectionData>(string hostingPort, IKernel ninjectKernel)
            where TService : BaseService<TService, TImplementedContract, TConnectionData>, TImplementedContract
        {
            return new HostManager<TService, TImplementedContract, TConnectionData>(hostingPort, ninjectKernel);
        }
    }
}