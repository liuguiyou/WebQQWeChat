using FclEx.Http.Event;
using System.Threading.Tasks;
using WebWeChat.Im.Bean;

namespace WebWeChat.Im.Module.Interface
{
    public interface IChatModule : IWeChatModule
    {
        ValueTask<ActionEvent> SendMsg(MessageSent msg, ActionEventListener listener = null);

        ValueTask<ActionEvent> GetRobotReply(RobotType robotType, string input, ActionEventListener listener = null);
    }
}
