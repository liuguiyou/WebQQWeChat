using FclEx.Http.Event;
using System.Threading.Tasks;

namespace WebWeChat.Im.Module.Interface
{
    public interface IContactModule : IWeChatModule
    {
        ValueTask<ActionEvent> GetContact(ActionEventListener listener = null);

        ValueTask<ActionEvent> GetGroupMember(ActionEventListener listener = null);
    }
}
