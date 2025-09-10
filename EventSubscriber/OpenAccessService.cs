using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;

namespace EventSubscriber
{
    /// <summary>
    /// A proxy for the OpenAccess service
    /// </summary>
    public class OpenAccessService
    {
        /// <summary>
        /// Constructor for <see cref="OpenAccessService"/>
        /// </summary>
        /// <param name="serviceUri">The URI to connect to OpenAccess</param>
        /// <param name="applicationId">Application Id connecting to OpenAccess</param>
        /// <param name="validateSslCertificate">True to validate the certificate</param>
        public OpenAccessService(string serviceUri, string applicationId, bool validateSslCertificate)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(serviceUri + "/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("application_id", applicationId);

            // support an untrusted certificate if validation is disabled
            if (!validateSslCertificate) ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;
        }

        /// <summary>
        /// Authenticates with the OpenAccess service
        /// </summary>
        /// <param name="username">The username of the user</param>
        /// <param name="password">The password of the user</param>
        /// <param name="directoryId">The directory id of the user</param>
        public void Authenticate(string username, string password, string directoryId)
        {
            var request = new AddAuthenticationRequest
            {
                application_id = ApplicationId,
                user_name = username,
                password = password,
                directory_id = directoryId
            };
            var httpResponse = client.PostAsJsonAsync("authentication", request).Result;
            ValidateSuccessResponse(httpResponse);

            var response = httpResponse.Content.ReadAsAsync<AddAuthenticationResponse>().Result;

            client.DefaultRequestHeaders.Remove("session_token");
            client.DefaultRequestHeaders.Add("session_token", response.session_token);
        }

        public void LogOut()
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, "authentication");
            var request = new DeleteAuthenticationRequest()
            {
                application_id = ApplicationId,
                session_token = SessionToken,
            };
            message.Content = new ObjectContent<DeleteAuthenticationRequest>(request, new JsonMediaTypeFormatter());

            var httpResponse = client.SendAsync(message).Result;
            ValidateSuccessResponse(httpResponse);
        }

        /// <summary>
        /// Adds an event subscription
        /// </summary>
        /// <param name="subscription">The event subscription to add</param>
        /// <returns>The new event subscription</returns>
        public EventSubscription AddSubscription(EventSubscription subscription)
        {
            var httpResponse = client.PostAsJsonAsync(EventSubscriptionsUri, ToAddEventSubscriptionRequest(subscription)).Result;
            ValidateSuccessResponse(httpResponse);

            return httpResponse.Content.ReadAsAsync<EventSubscription>().Result;
        }

        /// <summary>
        /// Modifies an event subscription
        /// </summary>
        /// <param name="subscription">The event subscription to modify</param>
        /// <returns>The modified event subscription</returns>
        public EventSubscription ModifySubscription(EventSubscription subscription)
        {
            var httpResponse = client.PutAsJsonAsync(GetUri(subscription), ToModifyEventSubscriptionRequest(subscription)).Result;
            ValidateSuccessResponse(httpResponse);
            
            return httpResponse.Content.ReadAsAsync<EventSubscription>().Result;
        }

        /// <summary>
        /// Disables an event subscription
        /// </summary>
        /// <param name="subscription">The event subscription to disable</param>
        /// <returns>The disabled event subscription</returns>
        public EventSubscription DisableSubscription(EventSubscription subscription)
        {
            var message = new HttpRequestMessage(HttpMethod.Delete, GetUri(subscription));
            var request = new DisableEventSubscriptionRequest()
            {
                application_id = ApplicationId,
                session_token = SessionToken,
            };
            message.Content = new ObjectContent<DisableEventSubscriptionRequest>(request, new JsonMediaTypeFormatter());
            var httpResponse = client.SendAsync(message).Result;
            ValidateSuccessResponse(httpResponse);
            
            return httpResponse.Content.ReadAsAsync<EventSubscription>().Result;
        }
        
        /// <summary>
        /// The OpenAccess application id
        /// </summary>
        public string ApplicationId
        {
            get { return client.DefaultRequestHeaders.GetValues("application_id").FirstOrDefault(); }
        }

        /// <summary>
        /// The OpenAccess session token
        /// </summary>
        public string SessionToken
        {
            get { return client.DefaultRequestHeaders.GetValues("session_token").FirstOrDefault(); }
        }

        /// <summary>
        /// Validates that an HTTP response does not represent an error.
        /// Throws an OpenAccessException if it's an error response.
        /// </summary>
        /// <param name="response">The HTTP response to validate</param>
        private static void ValidateSuccessResponse(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
                return;

            var errorResponse = response.Content.ReadAsAsync<ErrorResponse>().Result;
            throw new OpenAccessException(errorResponse.error.code, errorResponse.error.message);
        }

        /// <summary>
        /// Converts an event subscription to an AddEventSubscriptionRequest
        /// </summary>
        /// <param name="subscription">The subscription used to build the request</param>
        /// <returns>The add request</returns>
        private AddEventSubscriptionRequest ToAddEventSubscriptionRequest(EventSubscription subscription)
        {
            return new AddEventSubscriptionRequest
            {
                application_id = ApplicationId,
                session_token = SessionToken,
                description = subscription.description ?? string.Empty,
                filter = subscription.filter ?? string.Empty,
                is_durable = subscription.is_durable
            };
        }

        /// <summary>
        /// Converts an event subscription to a ModifyEventSubscriptionRequest
        /// </summary>
        /// <param name="subscription">The subscription used to build the request</param>
        /// <returns>The modify request</returns>
        private ModifyEventSubscriptionRequest ToModifyEventSubscriptionRequest(EventSubscription subscription)
        {
            return new ModifyEventSubscriptionRequest
            {
                application_id = ApplicationId,
                session_token = SessionToken,
                description = subscription.description ?? string.Empty,
                filter = subscription.filter ?? string.Empty
            };
        }

        /// <summary>
        /// Gets the URI for a specific event subscription.
        /// </summary>
        /// <param name="subscription"></param>
        /// <returns></returns>
        private static string GetUri(EventSubscription subscription)
        {
            return EventSubscriptionsUri + "/" + subscription.id;
        }

        /// <summary>
        /// The HTTP client used to communicate with the OpenAccess REST API
        /// </summary>
        private readonly HttpClient client;

        /// <summary>
        /// The relative URI for managing event subscriptions
        /// </summary>
        private const string EventSubscriptionsUri = "event_subscriptions";
    }
}
