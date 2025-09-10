using System;
using System.Text;

namespace EventSubscriber
{
    // Disable warnings about fields never initialized because they're initialized via reflection
#pragma warning disable 0649

    /// <summary>
    /// Base request for common parameters.
    /// </summary>
    class BaseRequest
    {
        private const string ApiVersion = "1.0";

        public string application_id;
        public string version = ApiVersion;
    }

    /// <summary>
    /// Represents the structure used to make an authentication request
    /// </summary>
    class AddAuthenticationRequest : BaseRequest
    {
        public string user_name;
        public string password;
        public string directory_id;
    }

    /// <summary>
    /// Represents the structure used to handle the authentication response
    /// </summary>
    class AddAuthenticationResponse
    {
        public string session_token;
        public DateTime token_expiration_time;
    }

    /// <summary>
    /// Represents the structure used to make a delete authentication (log out) request
    /// </summary>
    class DeleteAuthenticationRequest : BaseRequest
    {
        public string session_token;
    }

    /// <summary>
    /// Represents the structure used to make an add event subscription request
    /// </summary>
    class AddEventSubscriptionRequest : BaseRequest
    {
        public string session_token;
        public string description;
        public string filter;
        public bool is_durable;
    }

    /// <summary>
    /// Represents the structure used to make a modify event subscription request
    /// </summary>
    class ModifyEventSubscriptionRequest : BaseRequest
    {
        public string session_token;
        public string description;
        public string filter;
    }

    /// <summary>
    /// Represents the structure used to make a disable event subscription request
    /// </summary>
    class DisableEventSubscriptionRequest : BaseRequest
    {
        public string session_token;
    }

    /// <summary>
    /// Represents an event subscription.
    /// </summary>
    public class EventSubscription
    {
        public int id;
        public string user_id;
        public string description;
        public string filter;
        public bool is_durable;
        public string queue_name;
        public string exchange_name;
        public string binding_key;
        public string message_broker_hostname;
        public int message_broker_port;
        public bool requires_secure_connection;
        public DateTime? created_date;
        public DateTime? last_updated_date;

        /// <summary>
        /// Builds a string representation of the event subscription
        /// </summary>
        /// <returns>The string representation of the event subscription</returns>
        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.Format("id: {0}", id));
            builder.AppendLine(string.Format("user_id: {0}", user_id));
            builder.AppendLine(string.Format("description: {0}", description));
            builder.AppendLine(string.Format("filter: {0}", filter));
            builder.AppendLine(string.Format("is_durable: {0}", is_durable));
            builder.AppendLine(string.Format("queue_name: {0}", queue_name));
            builder.AppendLine(string.Format("exchange_name: {0}", exchange_name));
            builder.AppendLine(string.Format("binding_key: {0}", binding_key));
            builder.AppendLine(string.Format("message_broker_hostname: {0}", message_broker_hostname));
            builder.AppendLine(string.Format("message_broker_port: {0}", message_broker_port));
            builder.AppendLine(string.Format("requires_secure_connection: {0}", requires_secure_connection));
            builder.AppendLine(string.Format("created_date: {0}", created_date));
            builder.AppendLine(string.Format("last_updated_date: {0}", last_updated_date));
            return builder.ToString();
        }
    }

    /// <summary>
    /// The structure of an OpenAccess error
    /// </summary>
    class Error
    {
        public string code;
        public string message;
    }

    /// <summary>
    /// Represents the structure used to handle an error response
    /// </summary>
    class ErrorResponse
    {
        public Error error;
    }

    /// <summary>
    /// Represents an error from the OpenAccess API, with error code and message.
    /// </summary>
    class OpenAccessException : Exception
    {
        /// <summary>
        /// Creates an OpenAccessException given the OpenAccess error code and message.
        /// </summary>
        /// <param name="code">The error code</param>
        /// <param name="message">The error message</param>
        public OpenAccessException(string code, string message)
            : base(message)
        {
            Code = code;
        }

        /// <summary>
        /// The error code
        /// </summary>
        public string Code { get; private set; }
    }

#pragma warning restore 0649
}
