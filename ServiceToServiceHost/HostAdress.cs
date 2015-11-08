namespace ServiceToServiceHost
{
    /// <summary>
    /// Адрес хоста
    /// </summary>
    public class HostAdress
    {
        protected bool Equals(HostAdress other)
        {
            return string.Equals(Port, other.Port) && string.Equals(Ip, other.Ip);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((HostAdress) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Port != null ? Port.GetHashCode() : 0)*397) ^ (Ip != null ? Ip.GetHashCode() : 0);
            }
        }

        public string Port { get; private set; }
        public string Ip { get; private set; }
        public string FullAdress { get { return string.Format("{0}:{1}", Ip, Port); }}
        public bool IsValid { get { return !(string.IsNullOrEmpty(Ip) || string.IsNullOrEmpty(Port)); } }
        public HostAdress(string ip, string port) : this()
        {
            if (!string.IsNullOrEmpty(ip))
                Ip = ip;

            if (!string.IsNullOrEmpty(port))
                Port = port;
        }

        public HostAdress()
        {
            Ip = string.Empty;
            Port = string.Empty;
        }

        public override string ToString()
        {
            return FullAdress;
        }
        public static bool operator ==(HostAdress adress1, HostAdress adress2)
        {
            if (ReferenceEquals(adress1, adress2))
                return true;

            if (((object)adress1 == null) || ((object)adress2 == null))           
                return false;
            
            return adress1.FullAdress.Equals(adress2.FullAdress);
        }

        public static bool operator !=(HostAdress adress1, HostAdress adress2)
        {
            return !(adress1 == adress2);
        }
    }
}