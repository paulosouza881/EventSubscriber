using System;
using System.Configuration;
using System.IO;
using System.Linq;
using Microsoft.AspNet.SignalR.Client;

namespace EventSubscriber
{
    /// <summary>
    /// Sample event subscriber application
    /// Usage: Lnl.Tools.OpenAccess.EventNotificationSubscriber.exe [subscriptionName] [add|modify|disable]
    /// 
    /// subscriptionName: The name of the subscription. Defaults to a GUID.
    ///
    /// add|modify|disable: If defined, adds, modifies, or disables the subscription. Defaults to add.
    ///
    /// Other properties are set in the app.config file.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Application entry point
        /// </summary>
        /// <param name="args">The command line arguments</param>
        static void Main(string[] args)
        {
            if (HelpRequested(args) || AreInvalidArguments(args))
            {
                PrintUsage();
                return;
            }

            var subscription = AddRequested(args) ? CreateSubscription() : CreateSubscription(GetSubscriptionId(args));
            
            try
            {
                var serviceUri = AppSettingOrDefault("OpenAccessServiceUri", DefaultOpenAccessServiceUri);
                var applicationId = AppSettingOrDefault("OpenAccessApplicationId", DefaultApplicationId);
                var validateSslCertificate = bool.Parse(AppSettingOrDefault("ValidateOpenAccessSslCertificate", bool.FalseString));

                Console.WriteLine("Connecting to the OpenAccess service at {0} with application id {1}...", serviceUri, applicationId);

                var service = new OpenAccessService(serviceUri, applicationId, validateSslCertificate);
                Authenticate(service);

                Console.WriteLine("Successfully connected to the OpenAccess service.");

                if (DisableRequested(args))
                    DisableSubscription(service, subscription);
                else
                    StartSubscriber(subscription, service, args);

                service.LogOut();
            }
            catch (OpenAccessException e)
            {
                Console.WriteLine("Error communicating with the OpenAccess API: {0} - {1}", e.Code, e.Message);
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (AggregateException aggregateException)
            {
                foreach (var e in aggregateException.InnerExceptions)
                    Console.WriteLine("Error: {0}", ToFullExceptionMessage(e));
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", ToFullExceptionMessage(e));
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        /// <summary>
        /// Authenticates with the service
        /// </summary>
        /// <param name="service">The OpenAccess service</param>
        public static void Authenticate(OpenAccessService service)
        {
            service.Authenticate(AppSettingOrDefault("Username", "sa"), AppSettingOrDefault("Password", "tis1290"), AppSettingOrDefault("DirectoryId", "id-1"));
        }

        /// <summary>
        /// Creates a new EventSubscription instance, given an optional Id and the configured settings from the app.config
        /// </summary>
        /// <param name="id">The id of the event subscription</param>
        /// <returns>the subscription</returns>
        private static EventSubscription CreateSubscription(int? id = null)
        {
            return new EventSubscription
            {
                id = id.GetValueOrDefault(),
                description = AppSettingOrDefault("EventSubscriptionDescription", "Sample Event Subscription"),
                filter = AppSettingOrDefault("EventSubscriptionFilter", ""),
                is_durable = bool.Parse(AppSettingOrDefault("EventSubscriptionIsDurable", bool.FalseString))
            };
        }

        /// <summary>
        /// Disables the given event subscription
        /// </summary>
        /// <param name="service">The OpenAccess service</param>
        /// <param name="subscription">The event subscription to disable</param>
        /// <returns>The disabled event subscription</returns>
        private static EventSubscription DisableSubscription(OpenAccessService service, EventSubscription subscription)
        {
            var disabledSubscription = service.DisableSubscription(subscription);
            Console.WriteLine("Disabled subscription...");
            Console.WriteLine(disabledSubscription);

            return disabledSubscription;
        }

        /// <summary>
        /// Starts a subscriber for the given subscription
        /// </summary>
        /// <param name="subscription">the event subscription</param>
        /// <param name="service">the OpenAccess service</param>
        /// <param name="args">the command line arguments</param>
        private static void StartSubscriber(EventSubscription subscription, OpenAccessService service, string[] args)
        {
            if (ModifyRequested(args))
            {
                subscription = service.ModifySubscription(subscription);
                Console.WriteLine("Updated subscription...");
                Console.WriteLine(subscription);
            }
            else
            {
                subscription = service.AddSubscription(subscription);
                Console.WriteLine("Created subscription...");
                Console.WriteLine(subscription);
            }

            ReceiveEvents(CreateWebSubscriber(subscription, service));
        }

        /// <summary>
        /// Starts a web subscriber, printing to the console
        /// </summary>
        /// <param name="subscription">the event subscription</param>
        /// <param name="service">the OpenAccess service</param>
        /// <returns>the web event subscriber</returns>
        private static WebEventSubscriber CreateWebSubscriber(EventSubscription subscription, OpenAccessService service)
        {
            var webEventBridgeUri = AppSettingOrDefault("WebEventBridgeUri", DefaultWebEventBridgeUri);

            Console.WriteLine("Connecting to the Web Event Bridge at {0}...", webEventBridgeUri);

            var subscriberHandler = new BusinessEventSubscriberHandler(subscription);

            return new WebEventSubscriber(webEventBridgeUri,
                                          service,
                                          subscription,
                                          subscriberHandler,
                                          GetSignalRConnectionTraceLevel());
        }
        
        /// <summary>
        /// Receive events with the given web event subscriber
        /// </summary>
        /// <param name="subscriber">the event subscriber</param>
        private static void ReceiveEvents(WebEventSubscriber subscriber)
        {
            subscriber.StartReceiving();
            Console.ReadKey();
            subscriber.StopReceiving();
        }

        /// <summary>
        /// Prints command line usage
        /// </summary>
        private static void PrintUsage()
        {
            var exeName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            Console.WriteLine("Usage: {0} [subscription id] [{1}|{2}|{3}]", exeName, AddArgument, ModifyArgument, DisableArgument);
            Console.WriteLine();
            Console.WriteLine("subscription id: The id of the subscription. Required for modify/disable.");
            Console.WriteLine();
            Console.WriteLine("{0}|{1}|{2}: If defined, adds, modifies, or disables the subscription. Defaults to add.", AddArgument, ModifyArgument, DisableArgument);
            Console.WriteLine();
            Console.WriteLine("Other properties are set in the {0}.config file.", exeName);
        }

        /// <summary>
        /// Get the subscription id from the command line arguments, if available
        /// </summary>
        /// <param name="args">The command line arguments</param>
        /// <returns>the subscription id if defined, otherwise null</returns>
        private static int? GetSubscriptionId(string[] args)
        {
            int subscriptionId;
            return TryGetSubscriptionIdRequested(args, out subscriptionId) ? (int?)subscriptionId : null;
        }

        /// <summary>
        /// Get the subscription Id from the command line arguments, if defined
        /// </summary>
        /// <param name="args">The command line arguments</param>
        /// <param name="id">The subscription Id</param>
        /// <returns>true if a subscription Id was defined</returns>
        private static bool TryGetSubscriptionIdRequested(string[] args, out int id)
        {
            var success = true;
            id = -1;
            var strValue = args.FirstOrDefault(arg => arg != AddArgument &&
                                              arg != ModifyArgument &&
                                              arg != DisableArgument);
            try
            {
                if (!string.IsNullOrEmpty(strValue))
                    id = Convert.ToInt32(strValue);
            }
            catch (Exception)
            {
                success = false;
            }
            
            return success;
        }

        /// <summary>
        /// Determines if the command line arguments indicate to add a subscription
        /// </summary>
        /// <param name="args">The command line arguments</param>
        /// <returns>true if adding the subscription is requested</returns>
        private static bool AddRequested(string[] args)
        {
            return args.Contains(AddArgument) || (!ModifyRequested(args) && !DisableRequested(args));
        }

        /// <summary>
        /// Determines if the command line arguments indicate to modify a subscription
        /// </summary>
        /// <param name="args">The command line arguments</param>
        /// <returns>true if modifying the subscription is requested</returns>
        private static bool ModifyRequested(string[] args)
        {
            return args.Contains(ModifyArgument);
        }

        /// <summary>
        /// Determines if the command line arguments indicate to disable a subscription
        /// </summary>
        /// <param name="args">The command line arguments</param>
        /// <returns>true if disabling the subscription is requested</returns>
        private static bool DisableRequested(string[] args)
        {
            return args.Contains(DisableArgument);
        }

        /// <summary>
        /// Determines if the command line arguments indicate to display help/usage information
        /// </summary>
        /// <param name="args">The command line arguments</param>
        /// <returns>true if help/usage information is requested</returns>
        private static bool HelpRequested(string[] args)
        {
            return args.Intersect(HelpArguments).Any();
        }

        /// <summary>
        /// Determines if the command line arguments are invalid.
        /// 
        /// Invalid scenarios include:
        /// 1. There's no subscription name, but a modify or disable was requested.
        /// </summary>
        /// <param name="args">The command line arguments</param>
        /// <returns>true if the command line arguments are invalid</returns>
        private static bool AreInvalidArguments(string[] args)
        {
            int subscriptionId;
            var subscriptionIdRequested = TryGetSubscriptionIdRequested(args, out subscriptionId);

            return !subscriptionIdRequested && (ModifyRequested(args) || DisableRequested(args));
        }

        /// <summary>
        /// Gets the SignalR connection trace level from the app.config
        /// </summary>
        /// <returns>the SignalR connection trace level</returns>
        private static TraceLevels GetSignalRConnectionTraceLevel()
        {
            return AppSettingOrDefault("SignalRConnectionTraceLevel", DefaultSignalRConnectionTraceLevel)
                .Split('|')
                .Select(level => level.Trim())
                .Where(traceLevelString => Enum.IsDefined(typeof(TraceLevels), traceLevelString))
                .Select(traceLevelString => Enum.Parse(typeof(TraceLevels), traceLevelString))
                .Cast<TraceLevels>()
                .Aggregate(TraceLevels.None, (level, result) => result |= level);
        }

        /// <summary>
        /// Create full exception message, including inner exceptions
        /// </summary>
        /// <param name="e">The exception</param>
        /// <returns>The full exception message, including inner exceptions</returns>
        private static string ToFullExceptionMessage(Exception e)
        {
            return e.InnerException == null ?
                e.Message :
                string.Format("{0} - {1}", e.Message, ToFullExceptionMessage(e.InnerException));
        }

        /// <summary>
        /// Looks up the application setting for the specified key. 
        /// If the setting is not specified the default value is returned.
        /// </summary>
        /// <param name="key">The key of the application setting</param>
        /// <param name="defaultValue">The default value of the application setting</param>
        /// <returns>A string that holds the value of the setting.</returns>
        private static string AppSettingOrDefault(string key, string defaultValue)
        {
            var appSetting = ConfigurationManager.AppSettings[key];
            if (string.IsNullOrEmpty(appSetting))
                appSetting = defaultValue;

            return appSetting;
        }

        /// <summary>
        /// The default OpenAccess service URI
        /// </summary>
        private const string DefaultOpenAccessServiceUri = "https://localhost:8080/api/access/onguard/openaccess";

        /// <summary>
        /// The default Web Event Bridge URI
        /// </summary>
        private const string DefaultWebEventBridgeUri = "https://localhost:8080/api/access/onguard/openaccess/eventbridge";

        /// <summary>
        /// The default SignalR tracing level
        /// </summary>
        private const string DefaultSignalRConnectionTraceLevel = "None";

        /// <summary>
        /// The default OpenAccess application id
        /// </summary>
        private const string DefaultApplicationId = "SYS_ADMIN_APP";

        /// <summary>
        /// Command line argument that indicates to add the subscription
        /// </summary>
        private const string AddArgument = "add";

        /// <summary>
        /// Command line argument that indicates to modify the subscription
        /// </summary>
        private const string ModifyArgument = "modify";

        /// <summary>
        /// Command line argument that indicates to disable the subscription
        /// </summary>
        private const string DisableArgument = "disable";

        /// <summary>
        /// Command line argument that indicates to display help/usage information
        /// </summary>
        private static readonly string[] HelpArguments = new[] {"help", "usage", "?", "--help", "--h", "-help", "-h", "/?"};
    }
}
