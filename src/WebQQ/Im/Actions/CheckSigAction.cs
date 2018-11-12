using FclEx.Http.Core;
using FclEx.Http.Event;
using System;
using System.Threading.Tasks;
using WebQQ.Im.Core;

namespace WebQQ.Im.Actions
{
    public class CheckSigAction : WebQQAction
    {
        public CheckSigAction(IQQContext context, ActionEventListener listener = null) : base(context, listener)
        {
        }

        protected override string Url => Session.CheckSigUrl;

        protected override HttpReqType ReqType { get; } = HttpReqType.Get;

        protected override ValueTask<ActionEvent> HandleResponse(HttpRes response)
        {
            var ptwebqq = HttpService.GetCookie(new Uri(Session.CheckSigUrl), "ptwebqq");
            ptwebqq.Expired = true;
            //Session.Ptwebqq = ptwebqq;
            return NotifyOkEventAsync();
        }
    }
}
