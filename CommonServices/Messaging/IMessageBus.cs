using System;

namespace CommonServices.Messaging
{
    public interface IMessageBus : IMessagePublisher, IMessageSubscriber, IDisposable { }
}