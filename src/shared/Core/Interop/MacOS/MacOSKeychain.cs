using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using GitCredentialManager.Interop.MacOS.Native;
using static GitCredentialManager.Interop.MacOS.Native.CoreFoundation;
using static GitCredentialManager.Interop.MacOS.Native.SecurityFramework;

namespace GitCredentialManager.Interop.MacOS
{
    public class MacOSKeychain : ICredentialStore
    {
        private readonly string _namespace;

        /// <summary>
        /// Open the default keychain (current user's login keychain).
        /// </summary>
        /// <param name="namespace">Optional namespace to scope credential operations.</param>
        /// <returns>Default keychain.</returns>
        public MacOSKeychain(string @namespace = null)
        {
            PlatformUtils.EnsureMacOS();
            _namespace = @namespace;
        }

        public ICredential Get(CredentialQuery query)
        {
            IntPtr queryPtr = IntPtr.Zero;
            IntPtr resultPtr = IntPtr.Zero;
            IntPtr servicePtr = IntPtr.Zero;
            IntPtr accountPtr = IntPtr.Zero;

            try
            {
                queryPtr = CFDictionaryCreateMutable(
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero, IntPtr.Zero);

                CFDictionaryAddValue(queryPtr, kSecClass, kSecClassGenericPassword);
                CFDictionaryAddValue(queryPtr, kSecMatchLimit, kSecMatchLimitOne);
                CFDictionaryAddValue(queryPtr, kSecReturnData, kCFBooleanTrue);
                CFDictionaryAddValue(queryPtr, kSecReturnAttributes, kCFBooleanTrue);

                if (!string.IsNullOrWhiteSpace(query.Service))
                {
                    string fullService = CreateServiceName(query.Service);
                    servicePtr = CreateCFStringUtf8(fullService);
                    CFDictionaryAddValue(queryPtr, kSecAttrService, servicePtr);
                }

                if (!string.IsNullOrWhiteSpace(query.Account))
                {
                    accountPtr = CreateCFStringUtf8(query.Account);
                    CFDictionaryAddValue(queryPtr, kSecAttrAccount, accountPtr);
                }

                int searchResult = SecItemCopyMatching(queryPtr, out resultPtr);

                switch (searchResult)
                {
                    case OK:
                        int typeId = CFGetTypeID(resultPtr);
                        Debug.Assert(typeId != CFArrayGetTypeID(), "Returned more than one keychain item in search");
                        if (typeId == CFDictionaryGetTypeID())
                        {
                            return CreateCredentialFromAttributes(resultPtr);
                        }

                        throw new InteropException($"Unknown keychain search result type CFTypeID: {typeId}.", -1);

                    case ErrorSecItemNotFound:
                        return null;

                    default:
                        ThrowIfError(searchResult);
                        return null;
                }
            }
            finally
            {
                if (queryPtr != IntPtr.Zero) CFRelease(queryPtr);
                if (servicePtr != IntPtr.Zero) CFRelease(servicePtr);
                if (accountPtr != IntPtr.Zero) CFRelease(accountPtr);
                if (resultPtr != IntPtr.Zero) CFRelease(resultPtr);
            }
        }

        public void AddOrUpdate(string service, string account, string secret)
        {
            EnsureArgument.NotNullOrWhiteSpace(service, nameof(service));

            IntPtr queryPtr = IntPtr.Zero;
            IntPtr servicePtr = IntPtr.Zero;
            IntPtr accountPtr = IntPtr.Zero;
            IntPtr resultPtr = IntPtr.Zero;

            try
            {
                // Check if an entry already exists in the keychain
                queryPtr = CFDictionaryCreateMutable(
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero, IntPtr.Zero);

                CFDictionaryAddValue(queryPtr, kSecClass, kSecClassGenericPassword);
                CFDictionaryAddValue(queryPtr, kSecMatchLimit, kSecMatchLimitOne);
                CFDictionaryAddValue(queryPtr, kSecReturnData, kCFBooleanTrue);
                CFDictionaryAddValue(queryPtr, kSecReturnAttributes, kCFBooleanTrue);

                if (!string.IsNullOrWhiteSpace(service))
                {
                    string fullService = CreateServiceName(service);
                    servicePtr = CreateCFStringUtf8(fullService);
                    CFDictionaryAddValue(queryPtr, kSecAttrService, servicePtr);
                }

                if (!string.IsNullOrWhiteSpace(account))
                {
                    accountPtr = CreateCFStringUtf8(account);
                    CFDictionaryAddValue(queryPtr, kSecAttrAccount, accountPtr);
                }

                int searchResult = SecItemCopyMatching(queryPtr, out resultPtr);
                switch (searchResult)
                {
                    // Update existing entry
                    case OK:
                        Update(service, account, secret);
                        break;

                    // Create new entry
                    case ErrorSecItemNotFound:
                        Add(service, account, secret);
                        break;

                    default:
                        ThrowIfError(searchResult);
                        break;
                }
            }
            finally
            {
                if (resultPtr  != IntPtr.Zero) CFRelease(resultPtr);
                if (accountPtr != IntPtr.Zero) CFRelease(accountPtr);
                if (servicePtr != IntPtr.Zero) CFRelease(servicePtr);
                if (queryPtr      != IntPtr.Zero) CFRelease(queryPtr);
            }
        }

        public bool Remove(CredentialQuery query)
        {
            IntPtr queryPtr = IntPtr.Zero;
            IntPtr servicePtr = IntPtr.Zero;
            IntPtr accountPtr = IntPtr.Zero;

            try
            {
                queryPtr = CFDictionaryCreateMutable(
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero, IntPtr.Zero);

                CFDictionaryAddValue(queryPtr, kSecClass, kSecClassGenericPassword);
                CFDictionaryAddValue(queryPtr, kSecMatchLimit, kSecMatchLimitOne);
                CFDictionaryAddValue(queryPtr, kSecReturnRef, kCFBooleanTrue);

                if (!string.IsNullOrWhiteSpace(query.Service))
                {
                    string fullService = CreateServiceName(query.Service);
                    servicePtr = CreateCFStringUtf8(fullService);
                    CFDictionaryAddValue(queryPtr, kSecAttrService, servicePtr);
                }

                if (!string.IsNullOrWhiteSpace(query.Account))
                {
                    accountPtr = CreateCFStringUtf8(query.Account);
                    CFDictionaryAddValue(queryPtr, kSecAttrAccount, accountPtr);
                }

                int deleteResult = SecItemDelete(queryPtr);
                switch (deleteResult)
                {
                    case OK:
                        // Item was deleted
                        return true;

                    case ErrorSecItemNotFound:
                        return false;

                    default:
                        ThrowIfError(deleteResult);
                        return false;
                }
            }
            finally
            {
                if (queryPtr != IntPtr.Zero) CFRelease(queryPtr);
                if (servicePtr != IntPtr.Zero) CFRelease(servicePtr);
                if (accountPtr != IntPtr.Zero) CFRelease(accountPtr);
            }
        }

        public void Add(string service, string account, string secret)
        {
            IntPtr attributes = IntPtr.Zero;
            IntPtr servicePtr = IntPtr.Zero;
            IntPtr accountPtr = IntPtr.Zero;
            IntPtr dataPtr = IntPtr.Zero;

            try
            {
                attributes = CFDictionaryCreateMutable(
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero, IntPtr.Zero);

                CFDictionaryAddValue(attributes, kSecClass, kSecClassGenericPassword);

                if (!string.IsNullOrWhiteSpace(service))
                {
                    string fullService = CreateServiceName(service);
                    servicePtr = CreateCFStringUtf8(fullService);
                    CFDictionaryAddValue(attributes, kSecAttrService, servicePtr);
                }

                if (!string.IsNullOrWhiteSpace(account))
                {
                    accountPtr = CreateCFStringUtf8(account);
                    CFDictionaryAddValue(attributes, kSecAttrAccount, accountPtr);
                }

                if (!string.IsNullOrWhiteSpace(secret))
                {
                    byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
                    dataPtr = CFDataCreate(IntPtr.Zero, secretBytes, secretBytes.Length);
                    CFDictionaryAddValue(attributes, kSecValueData, dataPtr);
                }

                var zeroPtr = IntPtr.Zero;
                int addResult = SecItemAdd(attributes, out zeroPtr);
                switch (addResult)
                {
                    case OK:
                        // Item was added
                        break;

                    default:
                        ThrowIfError(addResult);
                        break;
                }
            }
            finally
            {
                if (attributes != IntPtr.Zero) CFRelease(attributes);
                if (servicePtr != IntPtr.Zero) CFRelease(servicePtr);
                if (accountPtr != IntPtr.Zero) CFRelease(accountPtr);
                if (dataPtr    != IntPtr.Zero) CFRelease(dataPtr);
            }
        }

        public void Update(string service, string account, string secret)
        {
            IntPtr query = IntPtr.Zero;
            IntPtr updateDict = IntPtr.Zero;
            IntPtr servicePtr = IntPtr.Zero;
            IntPtr accountPtr = IntPtr.Zero;
            IntPtr dataPtr = IntPtr.Zero;

            try
            {
                query = CFDictionaryCreateMutable(
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero, IntPtr.Zero);
                updateDict = CFDictionaryCreateMutable(
                    IntPtr.Zero,
                    0,
                    IntPtr.Zero, IntPtr.Zero);

                CFDictionaryAddValue(query, kSecClass, kSecClassGenericPassword);

                if (!string.IsNullOrWhiteSpace(service))
                {
                    string fullService = CreateServiceName(service);
                    servicePtr = CreateCFStringUtf8(fullService);
                    CFDictionaryAddValue(query, kSecAttrService, servicePtr);
                }

                if (!string.IsNullOrWhiteSpace(account))
                {
                    accountPtr = CreateCFStringUtf8(account);
                    CFDictionaryAddValue(query, kSecAttrAccount, accountPtr);
                }

                if (!string.IsNullOrWhiteSpace(secret))
                {
                    byte[] secretBytes = Encoding.UTF8.GetBytes(secret);
                    dataPtr = CFDataCreate(IntPtr.Zero, secretBytes, secretBytes.Length);
                    CFDictionaryAddValue(updateDict, kSecValueData, dataPtr);
                }

                int addResult = SecItemUpdate(query, updateDict);
                switch (addResult)
                {
                    case OK:
                        // Item was updated
                        break;

                    default:
                        ThrowIfError(addResult);
                        break;
                }
            }
            finally
            {
                if (query      != IntPtr.Zero) CFRelease(query);
                if (updateDict != IntPtr.Zero) CFRelease(updateDict);
                if (servicePtr != IntPtr.Zero) CFRelease(servicePtr);
                if (accountPtr != IntPtr.Zero) CFRelease(accountPtr);
                if (dataPtr    != IntPtr.Zero) CFRelease(dataPtr);
            }
        }

        private static IntPtr CreateCFStringUtf8(string str)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            return CFStringCreateWithBytes(IntPtr.Zero,
                bytes, bytes.Length, CFStringEncoding.kCFStringEncodingUTF8, false);
        }

        private static ICredential CreateCredentialFromAttributes(IntPtr attributes)
        {
            string service = GetStringAttribute(attributes, kSecAttrService);
            string account = GetStringAttribute(attributes, kSecAttrAccount);
            string password = GetStringAttribute(attributes, kSecValueData);
            string label = GetStringAttribute(attributes, kSecAttrLabel);
            return new MacOSKeychainCredential(service, account, password, label);
        }

        private static string GetStringAttribute(IntPtr dict, IntPtr key)
        {
            if (dict == IntPtr.Zero)
            {
                return null;
            }

            IntPtr buffer = IntPtr.Zero;
            try
            {
                if (CFDictionaryGetValueIfPresent(dict, key, out IntPtr value) && value != IntPtr.Zero)
                {
                    if (CFGetTypeID(value) == CFStringGetTypeID())
                    {
                        int stringLength = (int)CFStringGetLength(value);
                        int bufferSize = stringLength + 1;
                        buffer = Marshal.AllocHGlobal(bufferSize);
                        if (CFStringGetCString(value, buffer, bufferSize, CFStringEncoding.kCFStringEncodingUTF8))
                        {
                            return Marshal.PtrToStringAuto(buffer, stringLength);
                        }
                    }

                    if (CFGetTypeID(value) == CFDataGetTypeID())
                    {
                        int length = CFDataGetLength(value);
                        IntPtr ptr = CFDataGetBytePtr(value);
                        return Marshal.PtrToStringAuto(ptr, length);
                    }
                }
            }
            finally
            {
                if (buffer != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            return null;
        }

        private string CreateServiceName(string service)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(_namespace))
            {
                sb.AppendFormat("{0}:", _namespace);
            }

            sb.Append(service);
            return sb.ToString();
        }
    }
}
