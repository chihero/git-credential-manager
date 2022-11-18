using System;

namespace GitCredentialManager.Authentication.OAuth
{
    public class OAuth2DeviceCodeResult
    {
        public OAuth2DeviceCodeResult(string deviceCode, string userCode, Uri verificationUri, int? interval, int? expiresIn)
        {
            DeviceCode = deviceCode;
            UserCode = userCode;
            VerificationUri = verificationUri;
            PollingInterval = interval.ToTimeSpanOrDefault(5, TimeUnit.Seconds);
            ExpiresIn = expiresIn.ToTimeSpan(TimeUnit.Seconds);
        }

        public string DeviceCode { get; }

        public string UserCode { get; }

        public Uri VerificationUri { get; }

        public TimeSpan PollingInterval { get; }

        public TimeSpan? ExpiresIn { get; }
    }
}
