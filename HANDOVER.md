# Source and Wiki Alignment Handover

## Scope and outcome

Compared the Wiki changes from `05bda1665301490a41d6c08d27762d17f45f554c` through the September 14 fixes with the source repository, then checked for broader path and setup inconsistencies. Changes are uncommitted in both repositories.

## Changes made

### Source repository

- Updated both completed Module 3 RAG samples to resolve venue policies from `AppContext.BaseDirectory`. The completed projects are nested one level deeper than the Wiki's `start` project, so they use five parent traversals to reach `exercises/module3/datasets/venue-policies`.
- Replaced the invalid Module 4 endpoint in `exercises/module4/start` and both completed exercise configurations with the workshop endpoint used by the Wiki and demos.
- Moved the Module 4.2 PDF skill from `pdf/` to `skills/pdf/`, matching `PdfSkillSupport.SkillsDirectory`, and configured the completed exercise project to copy skill files to its output directory.
- Added the same skill-copy rule to `demos/module-04/demo03`, which also reads skills from its output directory.

### Wiki repository

- Updated the Home page's folder overview to match the current repository layout and identify the Module 5 reference location.
- Fixed Lab 4.1's duplicate Step 2 numbering.
- Added a warning to Lab 5.1: its `src/GloboTicket` paths refer to a tree absent from the current source checkout.

## Review notes

- The Module 2.2 Wiki correction is accurate: the starter does not contain `AnonymousUserFilter.cs`, and the revised lab instructs participants to create it. No source change was needed.
- The Python dependencies described by Module 2.3/4.2 are already provisioned by `.devcontainer/devcontainer.json`; no additional environment change was needed.
- Module 3's Wiki path is correct for `exercises/module3/start`. The completed projects have an extra directory level, which is why their source path differs.
- The venue-policy Markdown files in `exercises/datasets`, `exercises/module3/datasets`, and `demos/module-03/datasets` were checked and match. Module 3's Wiki and completed exercises use the module-specific copy.

## Remaining alignment plan

Module 5 is the outstanding blocker. The current source repository contains `exercises/module5/completed` but neither `src/GloboTicket` nor `exercises/module5/start`; Lab 5.1 still describes the former starter layout. Before the next workshop, restore a genuine starter under `exercises/module5/start` and update the Wiki paths, keeping `completed` as the reference solution. If a starter cannot be restored, redesign Lab 5.1 around a separate working copy and update all its commands and file references accordingly. The existing completed solution is not a suitable substitute for an incremental starter, so it was not rewritten as one here.

## Validation

- Both completed Module 3 projects build with zero warnings and errors.
- The completed Module 4.2 exercise and `demo03` build successfully. They retain existing nullable warnings in ride models and the Python script runner.
- Verified that both Module 4 skill projects copy `skills/pdf/SKILL.md` and `scripts/extract_text.py` to their build output, and that both Module 3 output paths resolve to the tracked dataset.
- `git diff --check` passes in both repositories. No live model calls or commits were made.
