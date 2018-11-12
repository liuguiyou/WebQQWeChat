﻿using FclEx.Helpers;
using FclEx.Http;
using FclEx.Http.Core;
using FclEx.Http.Event;
using Microsoft.Extensions.Logging;
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebWeChat.Im.Actions.ActionResult;
using WebWeChat.Im.Core;
using WebWeChat.Im.Module.Impl;

namespace WebWeChat.Im.Actions
{
    /// <summary>
    /// 同步检查
    /// </summary>
    [Description("同步检查")]
    public class SyncCheckAction : WebWeChatAction
    {
        private readonly Regex _reg = new Regex(@"window.synccheck={retcode:""(\d+)"",selector:""(\d+)""}");
        private int _hostIndex = 0;

        public SyncCheckAction(IWeChatContext context, ActionEventListener listener = null)
            : base(context, listener)
        {
        }

        protected override HttpReq BuildRequest()
        {
            var url = Session.SyncUrl;
            if (Session.SyncUrl == null)
            {
                var host = ApiUrls.SyncHosts[_hostIndex];
                url = $"https://{host}/cgi-bin/mmwebwx-bin/synccheck";
                Logger.LogDebug($"测试站点{_hostIndex + 1}: {host}");
            }
            else
            {
                Logger.LogInformation("Begin SyncCheck...");
            }
            var req = new HttpReq(HttpMethodType.Get, url)
            {
            };

            // 此处需要将key都变成小写，否则提交会失败
            foreach (var pair in Session.BaseRequest)
            {
                req.AddQueryValue(pair.Key.ToLower(), pair.Value);
            }
            req.AddQueryValue("r", Timestamp);
            req.AddQueryValue("synckey", Session.SyncKeyStr);
            req.AddQueryValue("_", Session.Seq++);

            return req;
        }

        private ValueTask<ActionEvent> TestNextHost()
        {
            if (++_hostIndex < ApiUrls.SyncHosts.Length)
            {
                return ActionEvent.EmptyOkEvent;
            }
            else
            {
                return NotifyErrorEventAsync(WeChatErrorCode.IoError);
            }
        }

        protected override ValueTask<ActionEvent> HandleResponse(HttpRes responseItem)
        {
            var str = responseItem.ResponseString;
            var match = _reg.Match(str);
            if (match.Success)
            {
                var retcode = match.Groups[1].Value;

                if (Session.SyncUrl == null)
                {
                    // retcode
                    // 1100-
                    // 1101-参数错误
                    // 1102-cookie错误
                    if (retcode != "0") return TestNextHost();
                    else
                    {
                        Session.SyncUrl = responseItem.Req.Uri.OriginalString;
                        return this.ExecuteAsync();
                    }
                }

                switch (retcode)
                {
                    case "1100":
                    case "1101": // 在手机上登出了微信
                        Session.State = SessionState.Offline;
                        return NotifyOkEventAsync(EnumHelper.ParseFromStrNum<SyncCheckResult>(retcode));

                    case "0":
                        var selector = match.Groups[2].Value;
                        return NotifyOkEventAsync(EnumHelper.ParseFromStrNum<SyncCheckResult>(selector,
                            s =>
                            {
                                Logger.LogWarning($"cannot convert {s} to enum type : {nameof(SyncCheckResult)}");
                                return SyncCheckResult.Nothing;
                            }));
                }
            }
            throw WeChatException.CreateException(WeChatErrorCode.ResponseError);
        }

        protected override ValueTask<ActionEvent> HandleExceptionAsync(Exception ex)
        {
            // SyncUrl为空说明正在测试host
            if (Session.SyncUrl == null)
            {
                if (++ExcuteTimes < MaxReTryTimes)
                {
                    return NotifyActionEventAsync(ActionEvent.Create(ActionEventType.EvtRetry, ex));
                }
                else
                {
                    ExcuteTimes = 0;
                    return TestNextHost();
                }
            }
            else return base.HandleExceptionAsync(ex);
        }
    }
}
