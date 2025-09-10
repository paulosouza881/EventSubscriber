using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace EventSubscriber
{
    public class BusinessEventSubscriberHandler : IBusinessEventSubscriberHandler
    {
        /// <summary>
        /// Creates a business event subscriber handler for the given event subscription
        /// </summary>
        /// <param name="subscription">the event subscription</param>
        public BusinessEventSubscriberHandler(EventSubscription subscription)
        {
            this.subscription = subscription;
        }

        /// <summary>
        /// Called when a business event is received.
        /// </summary>
        /// <param name="eventProperties">the business event properties</param>
        public void OnBusinessEventReceived(IDictionary<string, object> eventProperties)
        {
            Console.WriteLine("===========================================");
            foreach (var p in eventProperties)
                Console.WriteLine("{0}: {1}", p.Key, p.Value);
        }

        /// <summary>
        /// Called when the connection is lost.
        /// </summary>
        public void OnConnectionLost()
        {
            Console.WriteLine("Connection lost.");
        }

        /// <summary>
        /// Called when the connection is established.
        /// </summary>
        public void OnConnectionEstablished()
        {
            Console.WriteLine("Connection established.");
            Console.WriteLine("Subscriber {0} started listening to events with key '{1}' and filter '{2}'...",
                                       subscription.id, subscription.binding_key, subscription.filter);
        }

        /// <summary>
        /// Called when an exception is raised.
        /// </summary>
        /// <param name="exception">the exception</param>
        public void OnExceptionRaised(Exception exception)
        {
            Console.WriteLine("Error: {0}", GetErrorMessage(exception));
        }

        /// <summary>
        /// Called when a management event is received.
        /// </summary>
        /// <param name="message">the management message</param>
        public void OnManagementEvent(string message)
        {
            //Console.WriteLine("Management Event: {0}", message);
        }
        
        /// <summary>
        /// Gets the error message for an exception.
        /// </summary>
        /// <param name="exception">The exception</param>
        /// <returns>The error message</returns>
        private static string GetErrorMessage(Exception exception)
        {
            var message = exception.Message;

            // Errors from SignalR server method invocations are exposed in the Message property
            // of an Exception wrapped in an AggregateException; so, if we encounter such an
            // exception, we can be relatively confident that we are dealing with a server method
            // invocation error. -MGS, 7/30/13
            if ((exception is AggregateException) &&
                (exception.InnerException != null) &&
                !string.IsNullOrEmpty(exception.InnerException.Message))
            {
                message = GetServiceFaultMessage(exception.InnerException.Message);
            }

            return message;
        }

        /// <summary>
        /// Gets the service fault message from a JSON-encoded string.
        /// </summary>
        /// <param name="jsonStr">JSON-encoded string</param>
        /// <returns>Service fault message</returns>
        private static string GetServiceFaultMessage(string jsonStr)
        {
            var message = jsonStr;
            var serviceFault = CreateServiceFault(jsonStr);

            if (serviceFault != null)
            {
                if (serviceFault.Message != null)
                {
                    message = serviceFault.Message;
                }
                else if (serviceFault.ErrorCode != null)
                {
                    message = serviceFault.ErrorCode;
                }
            }

            return message;
        }

        /// <summary>
        /// Creates a service fault object from a JSON-encoded string.
        /// </summary>
        /// <param name="jsonStr">JSON-encoded string</param>
        /// <returns>Service fault object</returns>
        private static dynamic CreateServiceFault(string jsonStr)
        {
            dynamic serviceFault = null;

            try
            {
                serviceFault = JsonConvert.DeserializeObject(jsonStr);
            }
            // ReSharper disable EmptyGeneralCatchClause
            catch (Exception) // ignore any errors while parsing JSON
            // ReSharper restore EmptyGeneralCatchClause
            {
            }

            return serviceFault;
        }

        /// <summary>
        /// The event subscription
        /// </summary>
        private EventSubscription subscription;
    }
}
