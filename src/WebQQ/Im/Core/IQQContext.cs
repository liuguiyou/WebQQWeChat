using System.Threading.Tasks;
using WebIm.Im.Core;
using WebQQ.Im.Bean;
using WebQQ.Im.Event;

namespace WebQQ.Im.Core
{
    public interface IQQContext : IImContext, IQQNotifyEventHandler
    {
        T GetSerivce<T>();
        Task FireNotifyAsync(QQNotifyEvent notifyEvent);
    }
}
