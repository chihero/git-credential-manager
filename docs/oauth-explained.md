# OAuth2 and OpenID Connect

OAuth2 and OpenID Connect (OIDC) are widely used authentication and
authorization standards. Git Credential Manager (GCM) supports multiple Git host
providers that support either (or both) of OAuth2 and OIDC to secure access to
Git repositories.

## Authentication vs Authorization

Authentication (AuthN) is often used interchangably with Authorization (AuthZ).
However, they are actually different concepts.

**Authentication** is concerned with verifying _who_ you are.

**Authorization** is concerned with verifying _what_ you are permitted to do.

For example, a user may be correctly _authenticated_ to a system using their
credentials, but is only _authorized_ to read certain files, and never allowed
to delete items.

## OAuth2

OAuth2 is an ground-up re-write of the older OAuth1 protocol. In practice OAuth1
is not used much anymore. OAuth2 is an _authorization_ standard for allowing a
user to grant limited access to a set of resources.

## OpenID Connect

OpenID Connect (OIDC) builds upon the OAuth2 authorization protocol in order
to provide identity services. It introduces a new type of token, the "identity
token" that provides

# Resources

Here are some more great resources on the topic of OAuth2 and OpenID Connect:

- https://oauth.net/2/
- https://auth0.com/intro-to-iam/what-is-oauth-2/
- https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/auth-oauth2
- https://openid.net/connect/
- https://openid.net/connect/faq/
- https://docs.microsoft.com/en-us/azure/active-directory/fundamentals/auth-oidc

