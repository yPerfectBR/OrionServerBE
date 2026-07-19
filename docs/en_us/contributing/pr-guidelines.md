# PR Guidelines

Pull requests are welcome, including AI-assisted work, but they must be review-friendly.

## Branch and Source

- Open PRs from a fork of the `development` branch.
- `main` is reserved for official releases.

## Scope Rules

- Keep PRs focused on one concern.
- If changes affect unrelated systems, split them into separate PRs.

## Commit Quality

- Prefer small, objective commits.
- Use clear commit names that describe intent.
- Break large features into meaningful incremental commits.

## Extra Requirements by Change Type

- **Bug fixes**: explain how to reproduce and how the fix addresses it.
- **Structural/deep changes**: explain rationale and project impact in detail.
- **Performance changes**: provide before/after benchmarks.
- **Behavioral changes**: include tests when possible.

PRs can be declined even with good code quality if scope, commit structure, or reviewability rules are not followed.
