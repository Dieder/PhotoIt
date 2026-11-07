# Squad Team

> Photo It organizes travel photos into albums, photo-book layouts, and shareable presentations.

## Coordinator

| Name | Role | Notes |
|------|------|-------|
| Squad | Coordinator | Routes work, enforces handoffs and reviewer gates. |

## Members

| Name | Role | Charter | Status |
|------|------|---------|--------|
| Lead | Architecture, Coordination & Review | `.squad/agents/lead/charter.md` | ✅ Active |
| Backend | .NET & Cosmos Persistence | `.squad/agents/backend/charter.md` | ✅ Active |
| Tester | Regression Tests & Quality | `.squad/agents/tester/charter.md` | ✅ Active |
| Frontend | Blazor UI | `.squad/agents/frontend/charter.md` | ✅ Active |
| Infra | DevOps & Platform | `.squad/agents/infra/charter.md` | ✅ Active |
| Scribe | Decision Merger | `.squad/agents/scribe/charter.md` | 📋 Silent |
| Ralph | Work Monitor | `.squad/agents/ralph/charter.md` | 🔄 Monitor |
| Rai | RAI Reviewer | `.squad/agents/Rai/charter.md` | 🛡️ RAI |
| Fact Checker | Verification & Devil's Advocate | `.squad/agents/fact-checker/charter.md` | 🔍 Verifier |

## Coding Agent

<!-- copilot-auto-assign: false -->

| Name | Role | Charter | Status |
|------|------|---------|--------|
| @copilot | Coding Agent | — | 🤖 Coding Agent |

### Capabilities

**🟢 Good fit — auto-route when enabled:**
- Bug fixes with clear reproduction steps
- Test coverage (adding missing tests, fixing flaky tests)
- Lint/format fixes and dependency updates
- Small isolated features with clear specs
- Boilerplate/scaffolding and documentation fixes

**🟡 Needs review — route to @copilot but flag for squad member PR review:**
- Medium features with clear specs and acceptance criteria
- Refactoring with existing test coverage
- API endpoint additions following established patterns
- Migration scripts with well-defined schemas

**🔴 Not suitable — route to a squad member instead:**
- Architecture decisions and system design
- Multi-system integration requiring coordination
- Ambiguous requirements needing clarification
- Security-critical or performance-critical paths

## Project Context

- **Owner:** Dieder Timmerman
- **Stack:** C#, .NET 10, Blazor, Entity Framework Core with Azure Cosmos DB, Azure Blob Storage, .NET Aspire, and ONNX Runtime GenAI.
- **Description:** Photo It organizes travel photos into albums, lays them out as photo books, and shares finished albums.
- **Created:** 2026-11-04T09:20:04.567+01:00
