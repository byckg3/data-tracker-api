---
description: "Review Data Tracker API changes with project-specific architecture and safety checks"
name: "code review - Data Tracker API"
agent: "agent"
argument-hint: "[PR diff, files, or selected code]"
---
Review the provided code, selected files, or diff for the Data Tracker API project.

Primary goal:
- Find bugs, regressions, and missing tests before merge.

Review output format:
1. Findings (ordered by severity)
- Severity: Critical | High | Medium | Low
- Location: file + line
- Problem: what is wrong
- Impact: how it can fail in production
- Fix: concrete suggestion
2. Open questions or assumptions
3. Brief summary of residual risk and testing gaps

Project-specific checklist:
- Layer boundaries:
  - Controllers should only orchestrate request/response and call Services.
  - Services should not perform HTTP/file I/O directly.
  - Repositories should own data access concerns.
- Async consistency:
  - Prefer async/await end-to-end.
  - Flag .Result, .Wait(), or sync-over-async patterns.
- Error handling:
  - Repository and Service methods should use try/catch with Console.WriteLine and rethrow.
  - Flag empty catch blocks or swallowed exceptions.
- JSON consistency:
  - Use JsonOptions.Default for serialize/deserialize.
- Security checks:
  - CORS must enumerate AllowedOrigins; do not allow AllowAnyOrigin().
  - Validate controller inputs for non-empty and valid format.
  - Resolve file paths safely (GetFullPath path traversal protection).
  - Ensure HTTP failures (IsSuccessStatusCode == false) throw and are not ignored.
  - Flag hardcoded API keys or connection strings.
- Testing expectations:
  - New behavior should include tests under tests/DataTrackerApi.Tests/.
  - Favor xUnit [Fact]/[Theory] coverage for changed logic.
  - Suggest [Trait("Tag", "TestOnly")] when external dependency tests are added.

Style checks (lower priority than correctness/safety):
- Spaces inside parentheses and before braces.
- Use var for local declarations.
- Prefer primary constructors when applicable.
- Use [] collection initialization style where relevant.

If scope is unclear, review the most recently changed files first.