# Spec Delta

## ADDED Requirements

### Requirement: Amounts can be converted between currencies
The system SHALL convert a monetary amount from one ISO 4217 currency to another using the current exchange rate, and SHALL report the converted amount together with the rate, the rate's source and its as-of time. Converting to the same currency SHALL return the amount unchanged with rate 1.

#### Scenario: Converting euros to dollars
- **WHEN** another part of the system converts 3,200,000,000 EUR to USD and a EUR to USD rate is available
- **THEN** it receives the USD amount equal to the euro amount multiplied by the rate, the rate itself, the rate's source and as-of time

#### Scenario: Same currency
- **WHEN** an amount in USD is converted to USD
- **THEN** the amount is returned unchanged with rate 1

### Requirement: Missing exchange rates are reported, not guessed
When no rate is available for a currency pair, the conversion SHALL report that the rate is not available, and SHALL NOT substitute an estimate, a stale default or a rate of 1.

#### Scenario: Unsupported currency
- **WHEN** an amount is converted from a currency the configured provider has no rate for
- **THEN** the conversion reports that the rate is not available

### Requirement: The exchange rate provider is chosen through settings
The system SHALL use the exchange rate provider named in its settings, and SHALL refuse to start when the setting names an unknown provider.

#### Scenario: Unknown exchange rate provider
- **WHEN** the settings name an exchange rate provider that does not exist
- **THEN** the application fails at startup with an error naming the setting and the valid providers

### Requirement: Fake exchange rate provider supplies fixed sample rates
The fake exchange rate provider SHALL return the same fixed sample rates on every call for USD, EUR, GBP, CHF, JPY and CAD, in both directions between any two of them, and SHALL be reported as sample data.

#### Scenario: Reverse direction
- **WHEN** the fake provider's rate from a supported currency to USD is r
- **THEN** its rate from USD to that currency equals 1/r rounded to six decimal places

#### Scenario: Cross rates
- **WHEN** the fake provider converts between two supported currencies other than USD
- **THEN** the rate is derived through USD, and the rates in the two directions multiply to 1 within 0.01 %

#### Scenario: Other currency
- **WHEN** an amount in a currency outside that set is converted with the fake provider
- **THEN** the conversion reports that the rate is not available
