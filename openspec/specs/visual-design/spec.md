# visual-design Specification

## Purpose

Defines how the web app looks and presents information on every screen, so that it is consistent, readable in light and dark surroundings, and usable with a keyboard, on small screens and with assistive technology.

## Requirements

### Requirement: Theme follows the system preference
The web app SHALL show a light theme or a dark theme according to the operating system's colour scheme preference, and SHALL switch when that preference changes, without a reload.

#### Scenario: Dark system preference
- **WHEN** a user whose system prefers a dark colour scheme opens the app
- **THEN** the app shows the dark theme, including form controls and scrollbars

#### Scenario: Preference changes while the app is open
- **WHEN** the system colour scheme changes from light to dark while the app is open
- **THEN** the app switches to the dark theme without a reload

#### Scenario: No preference
- **WHEN** the system expresses no colour scheme preference
- **THEN** the app shows the light theme

### Requirement: Text and controls meet contrast minimums
In both themes, text SHALL have a contrast ratio of at least 4.5:1 against its background, and the boundaries of form controls, focus indicators and status markers SHALL have at least 3:1 against adjacent colours.

#### Scenario: Secondary text
- **WHEN** secondary text such as the data source line is shown in either theme
- **THEN** its contrast against its background is at least 4.5:1

#### Scenario: Input boundary
- **WHEN** the symbol input is shown in either theme
- **THEN** its border has a contrast of at least 3:1 against the surface behind it

### Requirement: Keyboard focus is always visible
Every interactive element SHALL show a visible focus indicator when it receives keyboard focus, in both themes.

#### Scenario: Tabbing through the page
- **WHEN** a signed-in user moves through the page with the Tab key
- **THEN** the sign-out button, the symbol input and the look-up button each show a clearly visible focus indicator in turn

### Requirement: Numbers line up
Numeric values in metric displays SHALL use tabular (fixed-width) figures and be aligned consistently, so values can be compared down a column.

#### Scenario: Comparing metrics
- **WHEN** a stock's metrics are shown
- **THEN** the digits of the values use equal widths and the values share a common alignment edge

### Requirement: Status is not shown by colour alone
Errors, warnings and stale-data notes SHALL be identifiable by text or an icon with a text alternative, not only by colour.

#### Scenario: Stale data in greyscale
- **WHEN** a stale-data note is shown and the screen is viewed without colour
- **THEN** the note is still identifiable as a warning by its wording or labelled icon

### Requirement: Layout reflows on narrow screens
The web app SHALL be usable at viewport widths from 320 CSS pixels upwards, and with text enlarged to 200 %, without horizontal scrolling and without content being cut off.

#### Scenario: Phone width
- **WHEN** a signed-in user looks up a stock in a 320 px wide viewport
- **THEN** the header, form and every metric are readable without scrolling sideways

### Requirement: Motion respects the user's preference
The web app SHALL NOT animate transitions when the user's system asks for reduced motion.

#### Scenario: Reduced motion
- **WHEN** a user whose system requests reduced motion hovers over or focuses a button
- **THEN** its appearance changes immediately, without an animated transition

### Requirement: Header identifies the app and the signed-in user
Every screen SHALL show a header with the app's name and mark. When a user is signed in, the header SHALL show the user's display name (or email if no name is known) next to the sign-out action.

#### Scenario: Signed in
- **WHEN** a user named Alice Tester is signed in
- **THEN** the header shows "Alice Tester" next to a "Sign out" button

#### Scenario: Signed out
- **WHEN** no user is signed in
- **THEN** the header shows the app's name and a "Sign in" button, and no user name

### Requirement: Signed-out state explains the app
When no user is signed in, the web app SHALL show a short description of what the app does and a prominent sign-in action.

#### Scenario: First visit
- **WHEN** a user who is not signed in opens the app
- **THEN** the page describes the app in one or two sentences and offers a "Sign in" button

### Requirement: Styling works under the Content Security Policy
The built app SHALL load all styles, fonts and icons from its own origin, with no inline style elements, so it renders fully under the existing Content Security Policy.

#### Scenario: Production build
- **WHEN** the built app is served with its Content Security Policy
- **THEN** the browser reports no blocked style, font or image resources and the app renders with its full styling
