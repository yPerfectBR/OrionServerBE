# Architecture and Philosophy

## DIY by Design

DIY in OrionServer means the runtime is a foundation, not a full packaged game server implementation. You can add only the mechanics your project needs.

## Minimal Core Boundary

The core is expected to handle:

- player lifecycle and session flow
- chunk view/render flow
- essential networking/runtime behavior

Everything beyond that (mobs, full vanilla blocks/commands/mechanics) is intentionally optional.

## Multithreading Direction

A major differentiator is support for running world workloads across dedicated workers, with project-defined distribution strategy, to improve throughput on modern CPUs.

The broader goal is to support:

- multithread-aware world execution
- plugin-friendly async/job patterns
- lower contention in heavy same-world scenarios
