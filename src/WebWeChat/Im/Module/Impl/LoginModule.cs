using FclEx;
using FclEx.Http;
using FclEx.Http.Event;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebWeChat.Im.Actions;
using WebWeChat.Im.Actions.ActionResult;
using WebWeChat.Im.Bean;
using WebWeChat.Im.Core;
using WebWeChat.Im.Event;
using WebWeChat.Im.Module.Interface;

namespace WebWeChat.Im.Module.Impl
{
    public class LoginModule : WeChatModule, ILoginModule
    {
        public LoginModule(IWeChatContext context) : base(context)
        {
        }

        public ValueTask<ActionEvent> Login(ActionEventListener listener = null)
        {
            return new WebWeChatActionFuture(Context, listener)
                .PushAction<GetUuidAction>()
                .PushAction<GetQRCodeAction>(async (sender, @event) =>
                {
                    if (!@event.IsOk) return ActionEvent.EmptyOkEvent;
                    await Context.FireNotifyAsync(WeChatNotifyEvent.CreateEvent(WeChatNotifyEventType.QRCodeReady, @event.Target));
                    return ActionEvent.EmptyOkEvent;
                })
                .PushAction<WatiForLoginAction>(async (sender, @event) =>
                {
                    if (!@event.IsOk) return ActionEvent.EmptyOkEvent;

                    var result = (WatiForLoginResult)@event.Target;
                    switch (result)
                    {
                        case WatiForLoginResult.Success:
                            await Context.FireNotifyAsync(WeChatNotifyEvent.CreateEvent(WeChatNotifyEventType.QRCodeSuccess));
                            break;
                        case WatiForLoginResult.QRCodeInvalid:
                            await Context.FireNotifyAsync(WeChatNotifyEvent.CreateEvent(WeChatNotifyEventType.QRCodeInvalid));
                           // 令后续动作不再执行
                            return ActionEvent.Error("");
                        case WatiForLoginResult.ScanCode:
                            return ActionEvent.Repeat();
                    }
                    return ActionEvent.EmptyOkEvent;
                })
                .PushAction<WebLoginAction>()
                .PushAction<WebwxInitAction>()
                .PushAction<StatusNotifyAction>()
                .PushAction<GetContactAction>(async (sender, @event) =>
                {
                    if (@event.IsOk)
                    {
                        await Context.FireNotifyAsync(WeChatNotifyEvent.CreateEvent(WeChatNotifyEventType.LoginSuccess));
                    }

                    return ActionEvent.EmptyOkEvent;
                })
                .ExecuteAsync();
        }

        public void BeginSyncCheck()
        {
            var sync = new SyncCheckAction(Context);
            var wxSync = new WebwxSyncAction(Context, async (s, e) =>
            {
                if (e.Type == ActionEventType.EvtRetry) return ActionEvent.EmptyOkEvent;
                sync.ExecuteAsync().Forget();
                if (e.IsOk)
                {
                    var msgs = (IList<Message>)e.Target;
                    // if (msgs.Count == 0) await Task.Delay(5 * 1000);
                    foreach (var msg in msgs)
                    {
                        var notify = WeChatNotifyEvent.CreateEvent(WeChatNotifyEventType.Message, msg);
                        await Context.FireNotifyAsync(notify);
                    }
                }
                return ActionEvent.EmptyOkEvent;
            });

            sync.OnActionEvent += async (sender, @event) =>
            {
                if (@event.Type == ActionEventType.EvtError)
                {
                    Context.GetModule<SessionModule>().State = SessionState.Offline;
                    await Context.FireNotifyAsync(WeChatNotifyEvent.CreateEvent(WeChatNotifyEventType.Offline));
                }
                else if (@event.IsOk)
                {
                    var result = (SyncCheckResult)@event.Target;
                    switch (result)
                    {
                        case SyncCheckResult.Offline:
                        case SyncCheckResult.Kicked:
                            await Context.FireNotifyAsync(WeChatNotifyEvent.CreateEvent(WeChatNotifyEventType.Offline));
                            break;

                        case SyncCheckResult.UsingPhone:
                        case SyncCheckResult.NewMsg:
                            break;

                        case SyncCheckResult.RedEnvelope:
                        case SyncCheckResult.Nothing:
                            break;
                    }
                    (result == SyncCheckResult.Nothing ? sender : wxSync).ExecuteAutoAsync().Forget();
                }

                return ActionEvent.EmptyOkEvent;
            };

            sync.ExecuteAutoAsync().Forget();
        }
    }
}
