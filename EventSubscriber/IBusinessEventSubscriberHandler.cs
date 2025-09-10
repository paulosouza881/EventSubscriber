using System;
using System.Collections.Generic;

namespace EventSubscriber
{
    interface IBusinessEventSubscriberHandler
    {
        /// <summary>
        /// Called when a business event is received
        /// </summary>
        /// <param name="eventProperties">the business event properties</param>
        void OnBusinessEventReceived(IDictionary<string, object> eventProperties);

        /// <summary>
        /// Called when the connection to the message broker is lost.
        /// </summary>
        void OnConnectionLost();

        /// <summary>
        /// Called when the connection to the message broker is established.
        /// </summary>
        void OnConnectionEstablished();

        /// <summary>
        /// Called when an exception is raised.
        /// </summary>
        /// <param name="exception">the exception</param>
        void OnExceptionRaised(Exception exception);

        /// <summary>
        /// Called when a management event is received.
        /// </summary>
        /// <param name="message">the management message</param>
        void OnManagementEvent(string message);
    }
}
