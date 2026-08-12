using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System.Text.Json;

namespace Raphael.Api.Services
{
    public class FirebaseMessagingService : IFirebaseMessagingService
    {
        public FirebaseMessagingService(IConfiguration config)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                // 1. We obtain the "Firebase:ServiceAccount" section as an object.
                var serviceAccountSection = config.GetSection("Firebase:ServiceAccount");

                if (serviceAccountSection.Exists())
                {
                    // 2. Convert the configuration object back into a JSON string.
                    // This is necessary because GoogleCredential.FromJson expects raw JSON.
                    var options = new Dictionary<string, string>();
                    foreach (var child in serviceAccountSection.GetChildren())
                    {
                        options[child.Key] = child.Value ?? "";
                    }

                    string jsonConfig = JsonSerializer.Serialize(options);

                    // 3. Initialize with the credentials from the string
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromJson(jsonConfig)
                    });
                }
                else
                {
                    
                    throw new Exception("Firebase Service Account configuration is missing in appsettings.");
                }
            }
        }

        public FirebaseMessagingService(IWebHostEnvironment env, IConfiguration config)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                // Read the entire JSON from an environment variable or configuration
                string jsonConfig = config["Firebase:ServiceAccountJson"];

                if (!string.IsNullOrEmpty(jsonConfig))
                {
                    FirebaseApp.Create(new AppOptions()
                    {
                        Credential = GoogleCredential.FromJson(jsonConfig) // <--- Load from string
                    });
                }
                else
                {
                    // Fallback to the physical file for local development only
                    var path = Path.Combine(env.ContentRootPath, "firebase-adminsdk.json");
                    if (File.Exists(path))
                    {
                        FirebaseApp.Create(new AppOptions()
                        {
                            Credential = GoogleCredential.FromFile(path)
                        });
                    }
                }
            }
        }

        public async Task<bool> SendNotificationToDriverAsync(string fcmToken, string title, string body, Dictionary<string, string>? data = null)
        {
            if (string.IsNullOrEmpty(fcmToken)) return false;

            var message = new Message()
            {
                Token = fcmToken,
                Notification = new FirebaseAdmin.Messaging.Notification { Title = title, Body = body },
                Data = data, // Útil para mandar TripId
                Android = new AndroidConfig { Priority = Priority.High },
                Apns = new ApnsConfig { Headers = new Dictionary<string, string> { { "apns-priority", "10" } } }
            };

            try
            {
                await FirebaseMessaging.DefaultInstance.SendAsync(message);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}