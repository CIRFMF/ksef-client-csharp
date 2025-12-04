# **Rules for co-creating the KSeF Client project**

Thank you for your interest in contributing to the KSeF Client!
 The project's goal is to provide a stable, open and widely usable C# client.
 Below you will find principles that will help us develop code together in a structured manner and in line with best practices.

---

## How to report changes?

### 1. Report a problem or suggestion

- Use the *Issues* tab to report a bug or propose new functionality.
- Describe the context and expected behavior as precisely as possible.
- If **you want to handle the issue yourself** , mark the issue as **`community-contribution`** or ask for it to be assigned to you.
- If you do not plan to solve the problem – do not add a label, then the issue remains open for others.

### 2. Make changes via Pull Request

- Fork the repository.
- Create a separate branch ( `feature/...` , `fix/...` ).
- Make changes and make sure all tests pass.
- Open a PR with a clear description of *why* and *how* the change was made.
- Each PR should address a specific issue.
- This will help us avoid situations where several people are working on the same task.

---

## Code Rules

### Backward compatibility

- Do not remove or change existing methods in a way that breaks compatibility.
- Introduce functionality extensions in a backward-compatible manner, e.g., as extension methods, overrides, or optional parameters.
- In the case of completely new functionalities, they should be implemented as separate, coherent modules that do not interfere with the existing API and do not cause incompatibility.

### Style and quality

- Code should follow C# rules and accepted project conventions.
- Apply SOLID principles and stick to the existing architecture.
- Every public method/class must have an XML comment.

### Tests

- Unit and/or integration tests are required for each new functionality.
- Don't reduce existing test coverage.

---

## Acceptance process

- Each PR is reviewed by the project owners.
- PR must pass automated CI checks (build, tests).
- Changes enter the main branch only via PR – we do not make commits directly.

### Pull Request Processing Time

Pull Requests and submissions will be reviewed as time allows.
 All reports are valuable to us, even if we cannot address them immediately.

---

## When can a Pull Request be rejected?

PR may be rejected if:

- **Breaks backward compatibility** without justification or migration plan.
- **Does not include tests** for new functionalities or reduces test coverage.
- **Does not pass automatic CI verification** (build, tests, style).
- **It changes the existing architecture** in a way that is contrary to the adopted assumptions.
- **Favors a narrow use case** at the expense of the overall usability of the library.
- **Does not contain required documentation** or XML comments for public APIs.
- **It's too big and difficult to review** – we recommend small, granular changes.
- **No response from the author** to comments in the review for a long time.

---

## Rules of cooperation

- Maintain civility and respect towards other contributors.
- Don't make changes that favor a narrow use case at the expense of the overall usability of the library.
- Any major architectural decisions will be discussed in public discussions or within the design team.

---

Thank you for your contribution!
