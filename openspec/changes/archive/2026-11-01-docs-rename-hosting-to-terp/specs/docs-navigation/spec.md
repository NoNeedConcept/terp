## ADDED Requirements

### Requirement: Terp package section uses "Terp" label
The docs site SHALL use "Terp" (not "Hosting") as the label for the Terp package section in the top navigation, sidebar group header, and landing page hero button.

#### Scenario: Top navigation shows Terp
- **WHEN** a user visits any page on the docs site
- **THEN** the top navigation bar displays "Terp" linking to `/terp/`

#### Scenario: Landing page hero button shows Terp
- **WHEN** a user visits the docs landing page
- **THEN** the secondary hero button reads "Terp" and links to `/terp/`

#### Scenario: Sidebar group header shows Terp
- **WHEN** a user views the sidebar on any page
- **THEN** the group containing Overview, Setups, and Startup gates is labeled "Terp"

### Requirement: Terp section served at /terp/ path
The docs site SHALL serve the Terp package pages under the `/terp/` URL path, symmetric with `/akka/` for the Terp.Akka section.

#### Scenario: Pages resolve at new path
- **WHEN** a user navigates to `/terp/`, `/terp/setups`, or `/terp/gates`
- **THEN** the corresponding page renders correctly

#### Scenario: Internal links use new path
- **WHEN** any docs page links to the Terp section
- **THEN** the link target uses `/terp/` prefix, not `/hosting/`
