# AI Harness Engineering & Claude Code: Comprehensive Guide

---

## Part 1: Core Failure Modes & Harness Fundamentals

### 1. The Core Problem: Why LLMs Need a Harness
Frontier foundation models are stochastic pattern-matchers. In complex codebases, unconstrained models predictably suffer from systematic failure modes:
* **Context Degradation & Drift:** Attention dilutes as context grows, leading to hallucinated interfaces and forgotten constraints.
* **The "Whack-a-Mole" Regression Cycle:** Fixing error A breaks test B; fixing error B re-introduces error A.
* **Cascading Drift:** Errors compound across steps, resulting in entire modules being rewritten unnecessarily.
* **Cheating / Assertion Deletion:** Under pressure to pass tests, models may delete or weaken failing test assertions.

An **AI Harness** provides a deterministic runtime around the stochastic model to enforce ground truth and provide strict guardrails.

---

### 2. Core Defensive Mechanics

#### A. Hierarchical Verification Pipeline
Never rely on the model's internal assessment of correctness. Enforce verification in structured stages:
1. **Level 1: Static / Build Verification:** Compiler checks, type checking (`tsc`, `dotnet build`), and linters.
2. **Level 2: Scoped Unit Tests:** Fast, isolated tests covering only the modified components.
3. **Level 3: Full Regression Suite:** End-to-end and integration tests run prior to final review.

#### B. State Isolation & Rollback
* **Automated Reset:** If an attempted fix fails verification, the harness automatically runs `git reset --hard` or `git checkout .` to return to the last known good state.
* **Ephemeral Worktrees:** Isolate agent experiments in dedicated Git worktrees so failed attempts never corrupt the developer's working directory.

#### C. Automated Diff Inspection & Human Gates
* **Diff Bounds Checking:** Inspect `git diff` programmatically to ensure forbidden configuration files (`.env`, CI workflows) remain untouched and test assertion line counts do not decrease.
* **Human-in-the-Loop Sign-off:** Autonomy pauses at completion, presenting a clean unified diff for developer review before any code is committed or pushed.

---

## Part 2: Claude Code’s Production Harness

Claude Code implements these concepts through workspace configurations, lifecycle hooks, and fine-grained permissions.

---

### 1. `CLAUDE.md` as an Operational System Prompt Extension
`CLAUDE.md` is automatically read by the harness and injected into system context on **every single turn**.

#### Optimization Rule: Maximize Actionability per Token
* **Do NOT include:** Full interface definitions, library overviews, or descriptions discoverable via tools (`package.json`, directory structure).
* **DO include:** Binary rules, non-standard CLI commands, and negative constraints ("Do NOT...").

#### Example `CLAUDE.md` Reference:
```markdown
# Repository Operational Playbook

## Build & Test Commands
- Build: `pnpm build`
- Type Check: `pnpm tsc --noEmit`
- Run Single Test: `pnpm vitest run path/to/test.ts`
- Format: `pnpm prettier --write <file>`

## Architecture & Code Flow
- Controllers must never query persistence directly; route all operations through `src/services/`.
- Return typed `Result<T, E>` objects; do not throw uncaught exceptions.

## Hard Constraints
- Do NOT modify `.env*`, `package.json`, or CI configs without explicit approval.
- Do NOT delete existing test assertions to make tests pass.
- When addressing bugs, always write or run a reproducing test first.

```

---

### 2. Guardrails & Permission Controls

* **Allowlisted Command Execution:** Grant autonomous access to safe, read-only or verification commands (`git status`, test runners) while requiring explicit human confirmation for destructive shell commands (`rm`, `curl`, arbitrary scripts).
* **Path & Boundary Enforcement:** Prevent agent access to sensitive files (`.env`, `.pem`, `.git/`) through canonical path normalization.

---

### 3. Lifecycle Hooks: Verify-and-Repair Loop

Automated scripts triggered on pre-tool or post-tool execution (e.g., immediately after a file write).

```
┌──────────────┐     Edit File      ┌────────────────────────────────┐
│  Agent Loop  │ ─────────────────► │ Post-Tool Hook:                │
│ (Claude Code)│                    │ 1. Format/Lint (Prettier)      │
│              │ ◄───────────────── │ 2. Type Check (tsc / compiler) │
└──────────────┘    Stdout / Stderr │ 3. Targeted Test Execution     │
                    Feedback Loop   └────────────────────────────────┘

```

When a hook fails, the harness captures exit codes and `stderr`, routing the failure directly into the next prompt for self-correction.

---

## Part 3: Building an Agent Harness from Scratch & Subagent Orchestration

---

### 1. The 4-Stage Agent Loop

An autonomous agent harness operates as a deterministic state machine:

```
┌────────────────────────────────────────────────────────┐
│                      Agent Loop                        │
│                                                        │
│   ┌─────────────┐       ┌─────────────┐                │
│   │  1. Ingest  │ ────► │ 2. Decide   │                │
│   │   Context   │       │ (Inference) │                │
│   └─────────────┘       └──────┬──────┘                │
│          ▲                     │ Tool Call             │
│          │ Feedback            ▼                       │
│   ┌─────────────┐       ┌─────────────┐                │
│   │ 4. Verify   │ ◄──── │ 3. Execute  │                │
│   │ & Feedback  │       │  & Protect  │                │
│   └─────────────┘       └─────────────┘                │
└────────────────────────────────────────────────────────┘

```

1. **Ingest & Assemble Context:** Load conversation history, environment state, and prompt extensions.
2. **Model Inference:** The model generates output or requests structured tool calls (`read_file`, `edit_file`, `bash`).
3. **Execute & Protect:** The harness validates arguments (e.g., path traversal checks) and executes tools safely.
4. **Verify & Feedback:** The harness runs automated checks, formats stdout/stderr as a structured tool response, and loops back.

---

### 2. Circuit Breakers

Deterministic safeguards prevent runaway executions, high API costs, and code corruption:

* **Consecutive Failure Limit:** Abort after 3–5 consecutive failed verification cycles.
* **Maximum Step Cap:** Hard boundary on turns per task (e.g., 20 turns maximum).
* **Stagnation / Loop Detection:** Detect repeated identical tool calls with identical parameters.
* **Cost & Token Budgets:** Real-time tracking of cumulative token consumption.

---

### 3. Path Traversal & Security Validation

Before executing file operations, paths must be resolved and checked against the repository root:

```python
from pathlib import Path

def validate_path(requested_path: str, repo_root: Path) -> Path:
    target = (repo_root / requested_path).resolve()
    
    # Boundary check
    if not target.is_relative_to(repo_root.resolve()):
        raise PermissionError(f"Access denied: {requested_path} is outside the workspace.")
        
    # Sensitive file protection
    forbidden = [".git", ".env", "node_modules"]
    if any(part in target.parts for part in forbidden):
        raise PermissionError(f"Access denied: modifying {target.name} is forbidden.")
        
    return target

```

---

### 4. Context Compaction & Memory

* **Sliding Window:** Prune repetitive tool outputs and long logs from active history.
* **Compaction / Summarization:** Consolidate older turns into a high-level summary when approaching token thresholds.
* **Persistent External Memory (`MEMORY.md`):** Store key architectural decisions across sessions in a standalone markdown file.

---

### 5. Subagent Orchestration

To eliminate context bloat and prevent attention dilution, delegate tasks across isolated, specialized agents:

```
                  ┌────────────────────────┐
                  │   Orchestrator Agent   │
                  │ (Maintains overall plan│
                  │   and task routing)    │
                  └───────────┬────────────┘
                              │
         ┌────────────────────┼────────────────────┐
         ▼                    ▼                    ▼
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│ Researcher Agent │ │   Coder Agent    │ │  Reviewer Agent  │
│ Tools: Read-only │ │ Tools: Edit/Write│ │ Tools: Git diff, │
│ Output: Findings │ │ Output: Code     │ │  test outputs    │
│  brief (compact) │ │  changes         │ │ Output: Pass/Fail│
└──────────────────┘ └──────────────────┘ └──────────────────┘

```

#### The Critique-and-Refine Loop:

1. **Researcher** inspects files and returns a **compact findings brief** (file path, line numbers, root cause) rather than entire file dumps.
2. **Coder** implements the change based strictly on the brief.
3. **Reviewer** inspects only the isolated `git diff` and test execution output.
4. If the Reviewer issues a `FAIL`, the Orchestrator routes the specific critique back to the Coder with a fixed retry cap (2–3 iterations) before escalating to a human developer.
