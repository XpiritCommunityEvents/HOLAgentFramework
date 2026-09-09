# Agent Framework assessment: module 04 completed 4.4 ITP

## Verdict

Migration status: **converted and simplified** for Agent Framework 1.20.0.

This optional checkpoint now focuses on one concept: an autonomous handoff workflow. A concierge
coordinates a hotel specialist and a ride specialist through four explicit routes. The shared
conversation carries each specialist's recommendation back to the concierge.

The workflow has two clear safeguards:

- autonomous execution stops after at most eight turns;
- normal completion requires the concierge to emit `ITINERARY COMPLETE`.

The lab mentions booking consent, so the only human-in-the-loop step is a single approval around
the final `book_itinerary` tool. Hotel and ride discovery remain tiny local functions so attendees
can see the handoff pattern without production-style services, data models, or state management.

Removed from this checkpoint: Semantic Kernel, checkpoint persistence and resume options,
credential-free fake clients and self-tests, separate booking services, retry bookkeeping, and
supporting helpers that do not teach handoffs.

## Verification

- Debug build: passed with 0 warnings and 0 errors.
- Release build: passed with 0 warnings and 0 errors.
- Live execution was not run because it requires the attendee's configured API credentials.
