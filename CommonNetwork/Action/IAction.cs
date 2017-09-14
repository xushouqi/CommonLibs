using System.Threading.Tasks;
using System.Net.WebSockets;
using CommonLibs;

namespace CommonNetwork
{
    public interface IAction
    {
        void Submit(WebSocket socket, int accountid, WebPackage package);
        void Submit(WebSocket socket, UserData userData, WebPackage package);
        Task DoAction();
        byte[] GetResponseData();
        byte[] GetUnAuthorizedData();
        Task AfterAction();
    }
}
