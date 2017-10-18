using System.Threading.Tasks;
using System.Net.WebSockets;
using CommonLibs;

namespace CommonNetwork
{
    public interface IAction
    {
        void Submit(string channel, UserConnTypeEnum connType, int accountid, WebPackage package);
        void Submit(string channel, UserData userData, WebPackage package);
        Task DoAction();
        WebPackage GetReturnPackage();
        //byte[] GetResponseData();
        WebPackage GetUnAuthorizedPackage(WebPackage package);
        Task AfterAction();
    }
}
