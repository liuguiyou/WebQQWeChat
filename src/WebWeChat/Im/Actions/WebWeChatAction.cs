﻿using FclEx;
using FclEx.Http.Actions;
using FclEx.Http.Event;
using FclEx.Http.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebWeChat.Im.Core;
using WebWeChat.Im.Event;
using WebWeChat.Im.Module.Impl;

namespace WebWeChat.Im.Actions
{
    public abstract class WebWeChatAction : AbstractHttpAction
    {
        // 为了防止通知层级混乱，其他action不应该直接操作Context，本action也只是在报告错误时用到了。
        // 其他通知应该先通知到调用action的模块，由模块决定是否需要进一步通知
        private readonly IWeChatContext _context;
        protected ILogger Logger => _context.GetSerivce<ILogger>();
        protected SessionModule Session => _context.GetModule<SessionModule>();
        protected StoreModule Store => _context.GetModule<StoreModule>();
        protected IConfigurationRoot Config => _context.GetSerivce<IConfigurationRoot>();
        protected long Timestamp => DateTime.Now.ToTimestampMilli();

        protected WebWeChatAction(IWeChatContext context, ActionEventListener listener = null) :
            base(context.GetSerivce<IHttpService>())
        {
            _context = context;
            OnActionEvent += listener;
        }

        protected override ValueTask<ActionEvent> HandleExceptionAsync(Exception ex)
        {
            var exception = ex as WeChatException ?? new WeChatException(ex);
            return base.HandleExceptionAsync(exception);
        }

        protected ValueTask<ActionEvent> NotifyErrorEventAsync(WeChatException ex)
        {
            return NotifyActionEventAsync(ActionEventType.EvtError, ex);
        }

        protected ValueTask<ActionEvent> NotifyErrorEventAsync(WeChatErrorCode code)
        {
            return NotifyErrorEventAsync(WeChatException.CreateException(code));
        }

        protected ValueTask<ActionEvent> NotifyErrorEventAsync(WeChatErrorCode code, string msg)
        {
            return NotifyErrorEventAsync(WeChatException.CreateException(code, msg));
        }

        protected override async ValueTask<ActionEvent> ExecuteInternalAsync(CancellationToken token)
        {
            Logger.LogTrace($"[Action={ActionName} Begin]");
            var result = await base.ExecuteInternalAsync(token).ConfigureAwait(false);
            Logger.LogTrace($"[Action={ActionName} End]");
            return result;
        }

        protected override async ValueTask<ActionEvent> NotifyActionEventAsync(ActionEvent actionEvent)
        {
            var type = actionEvent.Type;
            var typeName = type.GetDescription();
            var target = actionEvent.Target;

            switch (type)
            {
                case ActionEventType.EvtError:
                {
                    var ex = (WeChatException)target;
                    Logger.LogError($"[Action={ActionName}, Result={typeName}, {ex}");
                    await _context.FireNotifyAsync(WeChatNotifyEvent.CreateEvent(WeChatNotifyEventType.Error, ex));
                    break;
                }
                case ActionEventType.EvtRetry:
                {
                    var ex = (WeChatException)target;
                    Logger.LogWarning($"[Action={ActionName}, Result={typeName}, RetryTimes={ExcuteTimes}][{ex.ToSimpleString()}]");
                    break;
                }
                case ActionEventType.EvtCanceled:
                    Logger.LogWarning($"[Action={ActionName}, Result={typeName}, Target={target}]");
                    break;

                default:
                    Logger.LogInformation($"[Action={ActionName}, Result={typeName}]");
                    break;
            }
            return await base.NotifyActionEventAsync(actionEvent);
        }
    }
}
