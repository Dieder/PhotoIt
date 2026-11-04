# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture and cross-cutting coordination | Lead | System design, technical trade-offs, scoped review |
| .NET services, EF Core, Cosmos DB, storage | Backend | Catalogs, persistence models, data access, blob integration |
| Regression tests and quality validation | Tester | Focused tests, test design, failure reproduction |
| Blazor UI and presentation | Frontend | Components, forms, client-side interaction, styling |
| DevOps, platform, deployment infrastructure, and CI/CD | Infra | Infrastructure-as-code, deployment pipelines, environment configuration |

Preset installation adds concrete routes for the configured team. Add or edit rows
here only when their agent names also exist in the casting registry.

## Issue Routing

| Label | Action | Who |
|-------|--------|-----|
| `squad` | Triage: analyze issue, assign `squad:{member}` label | Lead |
| `squad:{name}` | Pick up issue and complete the work | Named member |

### How Issue Assignment Works

1. When a GitHub issue gets the `squad` label, the **Lead** triages it — analyzing content, assigning the right `squad:{member}` label, and commenting with triage notes.
2. When a `squad:{member}` label is applied, that member picks up the issue in their next session.
3. Members can reassign by removing their label and adding another member's label.
4. The `squad` label is the "inbox" — untriaged issues waiting for Lead review.

## Rules

1. Assign exactly one accountable owner for each outcome; route by primary domain.
2. Split work only for independently deliverable outcomes with distinct owners.
3. Add a contributor or reviewer only for a concrete bounded need.
4. Quick factual questions may be answered directly without dispatch.
5. Scribe runs in the background only when accepted durable decisions need merging.
6. Route to the member whose domain is the primary concern.
7. **Issue-labeled work** — when a `squad:{member}` label is applied to an issue, route to that member. The Lead handles all `squad` (base label) triage.
