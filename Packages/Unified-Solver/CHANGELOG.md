# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- `SolverLayerBoxCollider`: a box collider with a `Collide With` layer mask. A particle's layer is the layer of the GameObject of the body that owns it (emitter, or cloth/rope beside `SolverClothParticles`/`SolverRopeParticles`); particles of a layer left out of the mask pass through, the rest are pushed and carried along with the box. Runs once per FixedUpdate after the solver's step. The layer of every particle is kept in a package-owned buffer (`SolverParticleLayers`) that `Respawn`/`Park`/`ResetToRest` do not touch.

## [0.1.0] - 2026-07-28

### This is the first release of *\<Yu5h1Lib.FX.Unified-Solver\>*.

*Short description of this release*
