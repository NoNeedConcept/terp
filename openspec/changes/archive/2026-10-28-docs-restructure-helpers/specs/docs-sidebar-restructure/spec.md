## ADDED Requirements

### Requirement: Replace Terp helpers sidebar group

The VitePress sidebar in `docs/.vitepress/config.ts` SHALL replace the "Terp helpers" group with two new groups: "Observability" and "Utilities".

#### Scenario: Sidebar structure after change
- **WHEN** viewing the docs site sidebar
- **THEN** the groups in order SHALL be: Start, Hosting, Observability, Utilities, Terp.Akka, API Reference, About

#### Scenario: Observability group content
- **WHEN** viewing the "Observability" sidebar group
- **THEN** it SHALL contain one item: "Overview" linking to `/diagnostics/`

#### Scenario: Utilities group content
- **WHEN** viewing the "Utilities" sidebar group
- **THEN** it SHALL contain two items: "Resilience" linking to `/utilities/resilience` and "Helpers" linking to `/utilities/helpers`
