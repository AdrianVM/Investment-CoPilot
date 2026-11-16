# Spec Delta

## Purpose

Identifies who is using Investment Co-Pilot through an external OpenID Connect provider, keeps a local record of each user, and guarantees that data owned by one user is never visible to or changeable by another.

## ADDED Requirements

### Requirement: API requires authentication
The system SHALL reject every API request that does not carry a valid access token, except requests to the health check endpoints.

#### Scenario: Request without a token
- **WHEN** a client calls any API endpoint other than a health check without an access token
- **THEN** the system responds with HTTP 401 and returns no data

#### Scenario: Health check without a token
- **WHEN** a client calls a health check endpoint without an access token
- **THEN** the system responds with the health status and does not require authentication

### Requirement: Access tokens are validated against the configured provider
The system SHALL accept an access token only if it is a JWT signed by the configured issuer, intended for the configured audience, and not expired.

#### Scenario: Valid token
- **WHEN** a client calls the API with an unexpired token signed by the configured issuer for the configured audience
- **THEN** the system processes the request as that user

#### Scenario: Expired token
- **WHEN** a client calls the API with a token whose expiry time has passed
- **THEN** the system responds with HTTP 401

#### Scenario: Token from another issuer
- **WHEN** a client calls the API with a token issued by an issuer other than the configured one
- **THEN** the system responds with HTTP 401

#### Scenario: Token for another audience
- **WHEN** a client calls the API with a token whose audience does not include the configured audience
- **THEN** the system responds with HTTP 401

#### Scenario: Tampered token
- **WHEN** a client calls the API with a token whose signature does not verify against the issuer's published keys
- **THEN** the system responds with HTTP 401

### Requirement: Identity provider is set by configuration only
The system SHALL take the identity provider's issuer, audience and client ID from configuration, so that switching to another standards-compliant OpenID Connect provider requires no code change.

#### Scenario: Switching providers
- **WHEN** an operator changes the configured issuer, audience and client ID to those of a different OpenID Connect provider and restarts the application
- **THEN** users can sign in through the new provider and the API accepts its tokens, without any code change

### Requirement: Users sign in through the provider
The web app SHALL sign users in by redirecting them to the configured provider using the Authorization Code flow with PKCE, and SHALL never collect or store users' passwords itself.

#### Scenario: Signing in
- **WHEN** a signed-out user opens the web app and chooses to sign in
- **THEN** the app redirects them to the provider, and after they authenticate there, returns them to the app signed in

#### Scenario: Signing out
- **WHEN** a signed-in user chooses to sign out
- **THEN** the app discards their tokens and shows the signed-out state

### Requirement: Local user is created on first sign-in
The system SHALL create a local user record the first time it receives a valid token for a provider identity it has not seen, keyed by the token's issuer and subject.

#### Scenario: First request from a new user
- **WHEN** the API receives a valid token whose issuer and subject match no local user
- **THEN** the system creates exactly one local user for that identity and processes the request as that user

#### Scenario: Returning user
- **WHEN** the API receives a valid token whose issuer and subject match an existing local user
- **THEN** the system uses the existing user and does not create another

#### Scenario: Simultaneous first requests
- **WHEN** the API receives several concurrent requests with valid tokens for the same new identity
- **THEN** the system ends up with exactly one local user for that identity and every request succeeds

### Requirement: Current user can view their profile
The system SHALL let an authenticated user retrieve their own profile, containing their user ID, email address and display name as last provided by the identity provider.

#### Scenario: Fetching the profile
- **WHEN** an authenticated user requests their profile
- **THEN** the system returns their user ID, email address and display name

#### Scenario: Profile reflects provider changes
- **WHEN** a user's email address or display name has changed at the provider and they make a request with a token carrying the new values
- **THEN** their profile returns the updated values

### Requirement: User-owned data is accessible only to its owner
The system SHALL record the owning user on every user-owned record and SHALL return, change or delete such a record only for its owner.

#### Scenario: Listing own data
- **WHEN** an authenticated user lists records of a user-owned type
- **THEN** the system returns only records owned by that user

#### Scenario: Reading another user's record
- **WHEN** an authenticated user requests a user-owned record that belongs to another user
- **THEN** the system responds with HTTP 404, without revealing that the record exists

#### Scenario: Changing another user's record
- **WHEN** an authenticated user tries to update or delete a user-owned record that belongs to another user
- **THEN** the system responds with HTTP 404 and the record is unchanged

#### Scenario: Creating a record
- **WHEN** an authenticated user creates a user-owned record
- **THEN** the system sets that user as its owner, ignoring any owner supplied in the request
